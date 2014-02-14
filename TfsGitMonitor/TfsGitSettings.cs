using System;
using System.Configuration;

namespace TfsGitMonitor
{
    internal static class TfsGitSettings
    {
        public static int IntervalBetweenRemoteCheck
        {
            get { return int.Parse(ConfigurationManager.AppSettings["Interval"]); }
        }
        public static int PopUpVisibleDuration
        {
            get { return int.Parse(ConfigurationManager.AppSettings["PopUpVisibleDuration"]); }
        }

        public static string CredentialDomain
        {
            get { return ConfigurationManager.AppSettings["CredentialDomain"]; }
        }

        public static Uri TfsUrl
        {
            get { return new Uri(ConfigurationManager.AppSettings["TfsUrl"]); }
        }

        
    }
}
