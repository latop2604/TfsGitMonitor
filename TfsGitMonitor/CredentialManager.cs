using CredentialManagement;

namespace TfsGitMonitor
{
    internal static class CredentialManager
    {
        private const string CredentialName = "TfsGitMonitor";
        public class UserPass
        {
            public string UserName { get; set; }
            public string Password { get; set; }
        }
        public static UserPass GetCredentials()
        {
            using (var cm = new Credential {Target = CredentialName})
            {
                if (!cm.Exists())
                    return null;

                cm.Load();
                var up = new UserPass {UserName = cm.Username, Password = cm.Password};
                return up;
            }
        }

        public static bool SetCredentials(UserPass up)
        {
            using (
                var cm = new Credential
                {
                    Target = CredentialName,
                    PersistanceType = PersistanceType.LocalComputer,
                    Username = up.UserName,
                    Password = up.Password
                })
            {
                return cm.Save();
            }
        }

        public static void RemoveCredentials()
        {
            using (var cm = new Credential {Target = CredentialName})
            {
                cm.Delete();
            }
        }
    }
}
