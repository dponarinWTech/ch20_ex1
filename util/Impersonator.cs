#region Using directives.
using System;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.ComponentModel;
#endregion

namespace HR_Sync
{
    /// <summary>
    /// Impersonation of a user. Allows to execute code under another user context.
    /// Please note that the account that instantiates the Impersonator class
    /// needs to have the 'Act as part of operating system' privilege set.
    /// </summary>
    /// <remarks>	
    /// Obtained from http://www.codeproject.com/Articles/10090/A-small-C-Class-for-impersonating-a-User
    /// This class is based on the information in the Microsoft knowledge base article "How to implement impersonation in an ASP.NET application"
    /// https://support.microsoft.com/en-us/help/306158/how-to-implement-impersonation-in-an-asp-net-application 
    /// and on example in article https://docs.microsoft.com/en-us/dotnet/api/system.security.principal.windowsidentity.impersonate 
    /// 
    /// Encapsulate an instance into a using-directive like e.g.:
    /// 
    ///		...
    ///		using ( new Impersonator( "myUserName", "myDomainName", "myPassword" ) )
    ///		{
    ///			...
    ///			[code that executes under the new context]
    ///			...
    ///		}
    ///		...
    /// 
    /// </remarks>
    class Impersonator : IDisposable
    {
        private readonly Logger Log;
        private WindowsImpersonationContext impersonationContext = null;

        /// <summary>
        /// Constructor. Starts the impersonation with the given credentials.
        /// Please note that the account that instantiates the Impersonator class
        /// needs to have the 'Act as part of operating system' privilege set.
        /// </summary>
        /// <param name="userName">The name of the user to act as.</param>
        /// <param name="domainName">The domain name of the user to act as.</param>
        /// <param name="password">The password of the user to act as.</param>
        /// <param name="logger">Instance of Logger</param>
        public Impersonator(
            string userName,
            string domainName,
            string password,
            Logger logger)
        {
            Log = logger;
            ImpersonateValidUser(userName, domainName, password);
        }

        #region P/Invoke
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int LogonUser(
            string lpszUserName,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            ref IntPtr phToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int DuplicateToken(
            IntPtr hToken,
            int impersonationLevel,
            ref IntPtr hNewToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool RevertToSelf();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool CloseHandle(
            IntPtr handle);

        private const int LOGON32_PROVIDER_DEFAULT = 0;
        //This parameter causes LogonUser to create a primary token
        private const int LOGON32_LOGON_INTERACTIVE = 2;

        // DVP: with LOGON32_LOGON_NEW_CREDENTIALS impersonation fails!
        //private const int LOGON32_LOGON_NEW_CREDENTIALS = 9; 
        #endregion


        #region Private members
        /// <summary>
        /// Does the actual impersonation.
        /// </summary>
        /// <param name="userName">The name of the user to act as.</param>
        /// <param name="domain">The domain name of the user to act as.</param>/// <param name="password">The password of the user to act as.</param>
        private void ImpersonateValidUser(string userName, string domain, string password)
        {
            Log?.LogDebug("ImpersonateValidUser: Begin");
            IntPtr token = IntPtr.Zero;
            var tokenDuplicate = IntPtr.Zero;
            //Log.LogDebug($"ImpersonateValidUser:  {domain}\\{userName}");
            try
            {
                if (RevertToSelf())
                {
                    if (LogonUser(
                        userName,
                        domain,
                        password,
                        LOGON32_LOGON_INTERACTIVE,
                        LOGON32_PROVIDER_DEFAULT,
                        ref token) != 0)
                    {
                        if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
                        {
                            WindowsIdentity tempWindowsIdentity = new WindowsIdentity(tokenDuplicate);
                            impersonationContext = tempWindowsIdentity.Impersonate();
                        }
                        else
                        {
                            Log?.LogError("ImpersonateValidUser: Unable to impersonate.");
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                    }
                    else
                    {
                        Log?.LogError("ImpersonateValidUser: Unable to set LogonUser.");
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                else
                {
                    Log?.LogError("ImpersonateValidUser: RevertToSelf is false.");
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                if (token != IntPtr.Zero)
                {
                    CloseHandle(token);
                }
                if (tokenDuplicate != IntPtr.Zero)
                {
                    CloseHandle(tokenDuplicate);
                }
                Log?.LogDebug("ImpersonateValidUser: End");
            }
        }

        /// <summary>
        /// Reverts the impersonation.
        /// </summary>
        private void UndoImpersonation()
        {
            Log?.LogDebug("UndoImpersonation: Begin");
            impersonationContext?.Undo();  // releasing the context object stops the impersonation
            Log?.LogDebug("UndoImpersonation: End");
        }
        #endregion


        #region IDisposable member
        public void Dispose()
        {
            UndoImpersonation();
        }
        #endregion
    }
}
