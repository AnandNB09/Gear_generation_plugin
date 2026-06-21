using System;
using NXOpen;

namespace GearGenerationPlugin
{
    public class EntryPoint
    {
        /// <summary>
        /// This is the absolute first method NX calls when the plugin is executed.
        /// It acts as the trigger to launch the user interface.
        /// </summary>
        public static void Main(string[] args)
        {
            // We declare the UI object here so we can access it in the 'finally' block
            Block_UI_for_Gear theGearUI = null;

            try
            {
                // 1. Instantiate the View (The Dialog)
                theGearUI = new Block_UI_for_Gear();

                // 2. Launch the dialog to the user's screen
                // The code will "pause" here until the user clicks OK, Apply, or Cancel
                theGearUI.Launch();
            }
            catch (Exception ex)
            {
                // If anything goes critically wrong at startup, tell the user
                UI.GetUI().NXMessageBox.Show("Fatal Startup Error", NXMessageBox.DialogType.Error, ex.ToString());
            }
            finally
            {
                // 3. Cleanup Memory
                // Once the dialog is closed, we MUST dispose of it to prevent memory leaks in NX
                if (theGearUI != null)
                {
                    theGearUI.Dispose();
                    theGearUI = null;
                }
            }
        }

        /// <summary>
        /// This method tells NX what to do with your .dll file after the program finishes running.
        /// Returning 'Immediately' unlocks the file so you can recompile in Visual Studio without having to restart NX.
        /// </summary>
        public static int GetUnloadOption(string dummyString)
        {
            return (int)Session.LibraryUnloadOption.Immediately;
        }
    }
}