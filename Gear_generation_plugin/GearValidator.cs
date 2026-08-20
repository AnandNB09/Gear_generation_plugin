using System;
using System.Collections.Generic;

namespace GearGenerationPlugin
{
    /// <summary>
    /// This class acts as the "Bouncer" for your plugin. 
    /// It inspects the GearParameters and prevents the builder from running 
    /// if the inputs are physically impossible or will cause manufacturing defects.
    /// </summary>
    public class GearValidator
    {
        /// <summary>
        /// Validates the gear parameters. Throws an exception with a user-friendly message if a problem is found.
        /// </summary>
        public static void Validate(GearParameters param)
        {
            // 1. Basic sanity checks (No negative or zero values)
            if (param.Teeth <= 0)
                throw new ArgumentException("Number of teeth must be greater than zero.");

            if (param.Module <= 0.0)
                throw new ArgumentException("Module must be greater than zero.");

            if (param.FaceWidth <= 0.0)
                throw new ArgumentException("Face width must be greater than zero.");

            if (param.PressureAngle <= 0.0 || param.PressureAngle>= 90.0 )
                throw new ArgumentException("Face width must be in range of 0.0 to 90.0.");



            // 2. Undercutting Check (The math you asked for!)
            // Convert pressure angle to radians for the Math.Sin function
            double pressureAngleRad = param.PressureAngle * (Math.PI / 180.0);

            // Formula: Z_min = 2 / (sin^2(alpha))
            double minTeethExact = 2.0 / Math.Pow(Math.Sin(pressureAngleRad), 2);
            int minTeeth = (int)Math.Ceiling(minTeethExact);

            if (param.Teeth < minTeeth)
            {
                // We don't block the generation, but we could throw a strict error if we wanted to.
                // For a plugin, it's usually better to warn the user but let them proceed if they really want to.
                // In this case, we'll throw an error to force them to acknowledge it.
                throw new ArgumentException($"Warning: Undercutting will occur! For a {param.PressureAngle}° pressure angle, the minimum number of teeth without undercutting is {minTeeth}. You entered {param.Teeth}.");
            }

            // 3. Shaft Hole Clearance Check
            // The shaft hole diameter MUST be smaller than the root of the gear (Dedendum Diameter)
            // We also want to leave at least a little bit of solid metal (wall thickness) around the shaft.
            double dedendumDiameter = param.DedendumRadius * 2.0;
            double minimumWallThickness = param.Module; // A good rule of thumb is 1 module of wall thickness

            if (param.ShaftHoleDiameter >= (dedendumDiameter - (2 * minimumWallThickness)))
            {
                throw new ArgumentException($"Shaft hole is too large! A shaft diameter of {param.ShaftHoleDiameter}mm leaves insufficient solid material at the root of the gear (Dedendum Dia: {dedendumDiameter:F2}mm).");
            }
        }
    }
}