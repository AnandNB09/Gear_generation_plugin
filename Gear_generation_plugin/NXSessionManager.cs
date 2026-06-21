using System;
using NXOpen;

namespace GearGenerationPlugin
{
    /// <summary>
    /// Handles all core NX communication, safe part access, and Undo (Transaction) management.
    /// This prevents database corruption and allows the user to cleanly "Ctrl+Z" the plugin's work.
    /// </summary>
    public static class NXSessionManager
    {
        // Easy global access to the main NX interfaces
        public static Session Session => Session.GetSession();
        public static UI UI => UI.GetUI();

        public static Part WorkPart => Session.Parts.Work;
        public static Part DisplayPart => Session.Parts.Display;

        /// <summary>
        /// Creates a visible Undo mark in the NX history tree. 
        /// Think of this as a "Save State" before we start drawing geometry.
        /// </summary>
        public static Session.UndoMarkId SetUndoMark(string markName)
        {
            return Session.SetUndoMark(Session.MarkVisibility.Visible, markName);
        }

        /// <summary>
        /// Instantly rolls back the NX file to the exact state it was in when the mark was created.
        /// Used primarily in 'catch' blocks when an error occurs.
        /// </summary>
        public static void UndoToMark(Session.UndoMarkId markId, string markName)
        {
            Session.UndoToMark(markId, markName);
        }

        /// <summary>
        /// Safely displays a message to the user using the native NX dialog boxes.
        /// </summary>
        public static void ShowMessage(string title, string message, NXMessageBox.DialogType type = NXMessageBox.DialogType.Information)
        {
            UI.NXMessageBox.Show(title, type, message);
        }

        /// <summary>
        /// Validates that a work part actually exists before running any heavy logic.
        /// </summary>
        public static bool EnsureWorkPartExists()
        {
            if (WorkPart == null)
            {
                ShowMessage("No Active Part", "Please open or create a 3D part file before running the Gear Generator.", NXMessageBox.DialogType.Error);
                return false;
            }
            return true;
        }
    }
}