using System;

namespace GearGenerationPlugin
{
    /// <summary>
    /// This class acts as a data container. It stores the user's inputs from the UI 
    /// so they can be securely passed to the Validator and the Builder.
    /// </summary>
    public class GearParameters
    {
        // 1. Core Properties (The data the user types in)
        // 'get' means other files can read this value. 'set' means they can change it.
        public int Teeth { get; set; }
        public double Module { get; set; }
        public double FaceWidth { get; set; }
        public double PressureAngle { get; set; } // In degrees
        public double ShaftHoleDiameter { get; set; }

        // 2. Constructor
        // This is the "Factory" that creates a new GearParameters object when we call 'new GearParameters(...)'
        public GearParameters(int teeth, double module, double faceWidth, double pressureAngle, double shaftHoleDia)
        {
            this.Teeth = teeth;
            this.Module = module;
            this.FaceWidth = faceWidth;
            this.PressureAngle = pressureAngle;
            this.ShaftHoleDiameter = shaftHoleDia;
        }

        // 3. Derived Properties (The magic of OOP)
        // These don't have a 'set' because they calculate themselves automatically based on the core inputs!
        public double PitchDiameter
        {
            get { return this.Module * this.Teeth; }
        }

        public double PitchRadius
        {
            get { return this.PitchDiameter / 2.0; }
        }

        public double AddendumRadius
        {
            get { return this.PitchRadius + this.Module; }
        }

        public double DedendumRadius
        {
            get { return this.PitchRadius - (1.25 * this.Module); }
        }
    }
}