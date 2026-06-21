using System;
using System.Collections.Generic;
using NXOpen;
using NXOpen.Features;

namespace GearGenerationPlugin
{
    /// <summary>
    /// This is the Engine. It takes the validated GearParameters and does the heavy lifting
    /// to generate the 2D sketch and 3D extrusion in NX.
    /// </summary>
    public class SpurGearBuilder
    {
        public static Feature BuildGear(Part workPart, GearParameters param)
        {
            int pointCount = 40; // Number of points to generate for the involute curve

            // 1. Setup the Sketch
            Point3d origin = new Point3d(0.0, 0.0, 0.0);
            Vector3d normal = new Vector3d(0.0, 0.0, 1.0);

            Plane plane = workPart.Planes.CreatePlane(origin, normal, SmartObject.UpdateOption.WithinModeling);
            SketchInPlaceBuilder sketchBuilder = workPart.Sketches.CreateSketchInPlaceBuilder2(null);
            sketchBuilder.PlaneReference = plane;

            Sketch sketch = sketchBuilder.Commit() as Sketch;
            sketchBuilder.Destroy();

            if (sketch == null) throw new Exception("Sketch creation failed.");

            sketch.SetName($"SpurGear_M{param.Module}_Z{param.Teeth}");
            sketch.Activate(Sketch.ViewReorient.True);

            // 2. Gear Mathematical Setup (Using data directly from param object!)
            double pressureAngleRad = MathUtils.DegToRad(param.PressureAngle);
            double baseRadius = param.PitchRadius * Math.Cos(pressureAngleRad);

            double toothThickness = Math.PI * param.Module / 2.0;
            double halfToothAngle = toothThickness / (2.0 * param.PitchRadius);
            double toothPitchAngle = 2.0 * Math.PI / param.Teeth;

            double tMax = Math.Sqrt((param.AddendumRadius * param.AddendumRadius) / (baseRadius * baseRadius) - 1.0);
            double tPitch = Math.Sqrt((param.PitchRadius * param.PitchRadius) / (baseRadius * baseRadius) - 1.0);

            double xPitch = baseRadius * (Math.Cos(tPitch) + tPitch * Math.Sin(tPitch));
            double yPitch = baseRadius * (Math.Sin(tPitch) - tPitch * Math.Cos(tPitch));
            double thetaPitch = Math.Atan2(yPitch, xPitch);

            double rightRotationBase = -halfToothAngle - thetaPitch;
            double leftRotationBase = halfToothAngle + thetaPitch;

            double filletRadius = 0.38 * param.Module;

            // 3. Generate Base Involute
            List<Point3d> baseInvolute = new List<Point3d>();
            for (int i = 0; i < pointCount; i++)
            {
                double t = tMax * i / (pointCount - 1);
                double x = baseRadius * (Math.Cos(t) + t * Math.Sin(t));
                double y = baseRadius * (Math.Sin(t) - t * Math.Cos(t));
                baseInvolute.Add(new Point3d(x, y, 0.0));
            }

            List<List<Point3d>> allLeftFlanks = new List<List<Point3d>>();
            List<List<Point3d>> allRightFlanks = new List<List<Point3d>>();

            // 4. Build All Teeth Profiles
            for (int toothIndex = 0; toothIndex < param.Teeth; toothIndex++)
            {
                double toothRotation = toothIndex * toothPitchAngle;
                List<Point3d> rightFlank = new List<Point3d>();
                List<Point3d> leftFlank = new List<Point3d>();

                for (int i = 0; i < baseInvolute.Count; i++)
                {
                    Point3d p = baseInvolute[i];
                    Point3d pRight = MathUtils.RotatePoint(p, rightRotationBase);
                    Point3d pLeftBase = new Point3d(p.X, -p.Y, 0.0);
                    Point3d pLeft = MathUtils.RotatePoint(pLeftBase, leftRotationBase);

                    pRight = MathUtils.RotatePoint(pRight, toothRotation);
                    pLeft = MathUtils.RotatePoint(pLeft, toothRotation);

                    rightFlank.Add(pRight);
                    leftFlank.Add(pLeft);
                }

                allRightFlanks.Add(rightFlank);
                allLeftFlanks.Add(leftFlank);

                Spline rightSpline = MathUtils.CreateSplineFromPoints(workPart, rightFlank);
                if (rightSpline != null) sketch.AddGeometry(rightSpline, Sketch.InferConstraintsOption.InferNoConstraints);

                Spline leftSpline = MathUtils.CreateSplineFromPoints(workPart, leftFlank);
                if (leftSpline != null) sketch.AddGeometry(leftSpline, Sketch.InferConstraintsOption.InferNoConstraints);

                Point3d addStart = rightFlank[rightFlank.Count - 1];
                Point3d addEnd = leftFlank[leftFlank.Count - 1];
                Point3d addMid = MathUtils.MidPointOnRadius(addStart, addEnd, param.AddendumRadius);

                bool flippedAdd;
                Arc topArc = workPart.Curves.CreateArc(addStart, addMid, addEnd, false, out flippedAdd);
                sketch.AddGeometry(topArc, Sketch.InferConstraintsOption.InferNoConstraints);
            }

            // 5. Build Root Fillets
            for (int toothIndex = 0; toothIndex < param.Teeth; toothIndex++)
            {
                int nextTooth = (toothIndex + 1) % param.Teeth;
                Point3d rootStart = allLeftFlanks[toothIndex][0];
                Point3d rootEnd = allRightFlanks[nextTooth][0];

                double phi1 = Math.Atan2(rootStart.Y, rootStart.X);
                double phi2 = Math.Atan2(rootEnd.Y, rootEnd.X);
                if (phi2 < phi1) phi2 += 2.0 * Math.PI;

                double dc = param.DedendumRadius + filletRadius;
                double deltaPhi = Math.Asin(filletRadius / dc);

                double phiC1 = phi1 + deltaPhi;
                Point3d c1 = new Point3d(dc * Math.Cos(phiC1), dc * Math.Sin(phiC1), 0.0);
                double tDist = dc * Math.Cos(deltaPhi);

                Point3d t1 = new Point3d(tDist * Math.Cos(phi1), tDist * Math.Sin(phi1), 0.0);
                Point3d tDed1 = new Point3d(param.DedendumRadius * Math.Cos(phiC1), param.DedendumRadius * Math.Sin(phiC1), 0.0);
                Point3d tMid1 = MathUtils.FilletArcMidPoint(c1, filletRadius, t1, tDed1);

                double phiC2 = phi2 - deltaPhi;
                Point3d c2 = new Point3d(dc * Math.Cos(phiC2), dc * Math.Sin(phiC2), 0.0);

                Point3d t2 = new Point3d(tDist * Math.Cos(phi2), tDist * Math.Sin(phi2), 0.0);
                Point3d tDed2 = new Point3d(param.DedendumRadius * Math.Cos(phiC2), param.DedendumRadius * Math.Sin(phiC2), 0.0);
                Point3d tMid2 = MathUtils.FilletArcMidPoint(c2, filletRadius, tDed2, t2);

                Point3d rootMid = MathUtils.MidPointOnRadius(tDed1, tDed2, param.DedendumRadius);

                Line conn1 = workPart.Curves.CreateLine(rootStart, t1);
                sketch.AddGeometry(conn1, Sketch.InferConstraintsOption.InferNoConstraints);

                bool flippedFillet1;
                Arc fillet1 = workPart.Curves.CreateArc(t1, tMid1, tDed1, false, out flippedFillet1);
                sketch.AddGeometry(fillet1, Sketch.InferConstraintsOption.InferNoConstraints);

                bool flippedRoot;
                Arc rootArc = workPart.Curves.CreateArc(tDed1, rootMid, tDed2, false, out flippedRoot);
                sketch.AddGeometry(rootArc, Sketch.InferConstraintsOption.InferNoConstraints);

                bool flippedFillet2;
                Arc fillet2 = workPart.Curves.CreateArc(tDed2, tMid2, t2, false, out flippedFillet2);
                sketch.AddGeometry(fillet2, Sketch.InferConstraintsOption.InferNoConstraints);

                Line conn2 = workPart.Curves.CreateLine(t2, rootEnd);
                sketch.AddGeometry(conn2, Sketch.InferConstraintsOption.InferNoConstraints);
            }

            // 6. Build Keyway & Shaft Hole Dynamically based on DIN 6885-1
            double shaftDia = param.ShaftHoleDiameter;
            double shaftHoleRadius = shaftDia / 2.0;

            double keywayWidth = 0.0;
            double hubDepth = 0.0; // The 't2' depth cut into the gear hub

            // Determine standard dimensions based on shaft diameter ranges
            if (shaftDia > 10 && shaftDia <= 12) { keywayWidth = 4.0; hubDepth = 1.8; }
            else if (shaftDia > 12 && shaftDia <= 17) { keywayWidth = 5.0; hubDepth = 2.3; }
            else if (shaftDia > 17 && shaftDia <= 22) { keywayWidth = 6.0; hubDepth = 2.8; }
            else if (shaftDia > 22 && shaftDia <= 30) { keywayWidth = 8.0; hubDepth = 3.3; }
            else if (shaftDia > 30 && shaftDia <= 38) { keywayWidth = 10.0; hubDepth = 3.3; }
            else
            {
                // Fallback for non-standard/unsupported shafts: calculate roughly proportional sizes
                keywayWidth = shaftDia * 0.25;
                hubDepth = shaftDia * 0.15;
            }

            double keywayHalfWidth = keywayWidth / 2.0;
            double keywayDepthFromCenter = shaftHoleRadius + hubDepth;

            // Dynamically calculate coordinates
            double Y = Math.Sqrt((shaftHoleRadius * shaftHoleRadius) - (keywayHalfWidth * keywayHalfWidth));
            Point3d leftIntersectPt = new Point3d(-keywayHalfWidth, Y, 0.0);
            Point3d rightIntersectPt = new Point3d(keywayHalfWidth, Y, 0.0);
            Point3d bottomPt = new Point3d(0.0, -shaftHoleRadius, 0.0);

            bool arcReversed = false;
            Arc shaftArc = workPart.Curves.CreateArc(leftIntersectPt, bottomPt, rightIntersectPt, false, out arcReversed);
            sketch.AddGeometry(shaftArc, Sketch.InferConstraintsOption.InferNoConstraints);

            Line line1 = workPart.Curves.CreateLine(leftIntersectPt, new Point3d(-keywayHalfWidth, keywayDepthFromCenter, 0.0));
            sketch.AddGeometry(line1, Sketch.InferConstraintsOption.InferNoConstraints);

            Line line2 = workPart.Curves.CreateLine(new Point3d(-keywayHalfWidth, keywayDepthFromCenter, 0.0), new Point3d(keywayHalfWidth, keywayDepthFromCenter, 0.0));
            sketch.AddGeometry(line2, Sketch.InferConstraintsOption.InferNoConstraints);

            Line line3 = workPart.Curves.CreateLine(new Point3d(keywayHalfWidth, keywayDepthFromCenter, 0.0), rightIntersectPt);
            sketch.AddGeometry(line3, Sketch.InferConstraintsOption.InferNoConstraints);

            // 7. Update and Extrude
            sketch.Update();
            sketch.Deactivate(Sketch.ViewReorient.True, Sketch.UpdateLevel.Model);

            ExtrudeBuilder extrudeBuilder = workPart.Features.CreateExtrudeBuilder(null);
            Section section = workPart.Sections.CreateSection();

            List<Curve> curveList = new List<Curve>();
            foreach (NXObject obj in sketch.GetAllGeometry())
            {
                if (obj is Curve) curveList.Add((Curve)obj);
            }

            SelectionIntentRule[] rules = new SelectionIntentRule[1];
            rules[0] = workPart.ScRuleFactory.CreateRuleCurveDumb(curveList.ToArray());

            Point3d helpPoint = new Point3d(0.0, 0.0, 0.0);
            section.AddToSection(rules, null, null, null, helpPoint, Section.Mode.Create);
            extrudeBuilder.Section = section;

            Direction dir = workPart.Directions.CreateDirection(origin, normal, SmartObject.UpdateOption.WithinModeling);
            extrudeBuilder.Direction = dir;

            extrudeBuilder.Limits.StartExtend.Value.SetFormula("0.0");
            extrudeBuilder.Limits.EndExtend.Value.SetFormula(param.FaceWidth.ToString()); // Use FaceWidth from param

            Feature extrudeFeature = (Feature)extrudeBuilder.Commit();
            extrudeBuilder.Destroy();

            return extrudeFeature;
        }
    }
}