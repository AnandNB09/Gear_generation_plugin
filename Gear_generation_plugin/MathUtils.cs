using System;
using System.Collections.Generic;
using NXOpen;
using NXOpen.Features;

namespace GearGenerationPlugin
{
    /// <summary>
    /// This file holds all pure mathematical and geometric calculations. 
    /// By keeping these separate, any future part (like a Shaft or Bearing) can easily use them.
    /// </summary>
    public class MathUtils
    {
        public static Spline CreateSplineFromPoints(Part workPart, List<Point3d> pts)
        {
            if (pts == null || pts.Count < 2)
                return null;

            SketchSplineBuilder splineBuilder = workPart.Features.CreateSketchSplineBuilder(null);
            splineBuilder.Degree = 3;
            splineBuilder.IsAssociative = false;
            splineBuilder.MatchKnotsType = StudioSplineBuilderEx.MatchKnotsTypes.None;

            foreach (Point3d coord in pts)
            {
                Point pt = workPart.Points.CreatePoint(coord);

                GeometricConstraintData constraintData = splineBuilder.ConstraintManager.CreateGeometricConstraintData();
                constraintData.Point = pt;
                splineBuilder.ConstraintManager.Append(constraintData);
            }

            NXObject nXObject = splineBuilder.Commit();
            splineBuilder.Destroy();

            return nXObject as Spline;
        }

        public static Point3d FilletArcMidPoint(Point3d center, double radius, Point3d start, Point3d end)
        {
            double ux = start.X - center.X;
            double uy = start.Y - center.Y;

            double vx = end.X - center.X;
            double vy = end.Y - center.Y;

            double sx = ux + vx;
            double sy = uy + vy;

            double len = Math.Sqrt(sx * sx + sy * sy);
            if (len < 1e-9)
            {
                return new Point3d(center.X + radius, center.Y, 0.0);
            }
            double nx = sx / len;
            double ny = sy / len;

            return new Point3d(
                center.X + radius * nx,
                center.Y + radius * ny,
                0.0
            );
        }

        public static Point3d RotatePoint(Point3d p, double angle)
        {
            double xNew = p.X * Math.Cos(angle) - p.Y * Math.Sin(angle);
            double yNew = p.X * Math.Sin(angle) + p.Y * Math.Cos(angle);
            return new Point3d(xNew, yNew, p.Z);
        }

        public static Point3d ProjectToRadius(Point3d p, double radius)
        {
            double ang = Math.Atan2(p.Y, p.X);
            return new Point3d(
                radius * Math.Cos(ang),
                radius * Math.Sin(ang),
                0.0
            );
        }

        public static Point3d MidPointOnRadius(Point3d p1, Point3d p2, double radius)
        {
            double a1 = Math.Atan2(p1.Y, p1.X);
            double a2 = Math.Atan2(p2.Y, p2.X);

            if (a2 < a1)
                a2 += 2.0 * Math.PI;

            double amid = 0.5 * (a1 + a2);

            return new Point3d(
                radius * Math.Cos(amid),
                radius * Math.Sin(amid),
                0.0
            );
        }

        public static double DegToRad(double deg)
        {
            return deg * Math.PI / 180.0;
        }
    }
}