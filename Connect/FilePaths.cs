using System.IO;


namespace Connect
{
    public static class FilePaths
    {
        /// <summary>
        /// The executing assembly is one folder removed from the main folder.
        /// </summary>
        private static string modifier { get; set; } = @"..\";

        public static string Base
        {
            get => modifier;
        }

        /// <summary>
        /// Holds all resources and images connected to app.
        /// </summary>        
        public static string Assets
        {
            get => Path.Combine(modifier, @"Assets\");
        }

        /// <summary>
        /// Subdirectory of Assets, holds all HTML/PDF templates.
        /// </summary>
        public static string Templates
        {
            get => Path.Combine(Assets, @"Templates\");
        }

        /// <summary>
        /// Holds all authentication files connected to app.
        /// </summary>
        public static string Authentication
        {
            get => Path.Combine(modifier, @"Authentication\");
        }

        /// <summary>
        /// Records significant client actions.
        /// </summary>
        public static string History
        {
            get => Path.Combine(modifier, @"History\");
        }

        /// <summary>
        /// Records app events.
        /// </summary>
        public static string Logs
        {
            get => Path.Combine(modifier, @"Logs\");
        }

        #region Authentication paths.

        internal static string DropboxAppKeys { get => Path.Combine(Authentication, @"DropboxAppKeys.json"); }
        internal static string CalendarAppKeys { get => Path.Combine(Authentication, @"GoogleAppKeys.json"); }
        internal static string CalendarToken { get => Path.Combine(Authentication, @"GoogleCalendarToken\"); }
        internal static string MailAppKeys { get => Path.Combine(Authentication, @"GoogleMailKeys.json"); }
        internal static string MailToken { get => Path.Combine(Authentication, @"GoogleMailToken\"); }
        internal static string RingCentralToken { get => Path.Combine(Authentication, @"RingCentralToken.json"); }

        #endregion

        #region Deprecated.
        internal static string ClientData { get => Path.Combine(modifier, @"Data\ClientData.json"); }
        internal static string Deleted { get => Path.Combine(modifier, @"Data\Deleted.json"); }
        #endregion

        public static void ResetPaths(string origination)
        {
            modifier = origination;
        }
    }
}
