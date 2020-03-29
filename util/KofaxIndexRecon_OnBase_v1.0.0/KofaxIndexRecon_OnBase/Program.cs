using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;
using log4net.Config;

namespace KofaxIndexRecon_OnBase
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType.FullName);
        static void Main(string[] args)
        {
            // setup logger
            XmlConfigurator.Configure();

            Recon recon = new Recon(log);
            recon.StartRecon();
        }


        /// <summary>
        /// Writes message to EventLog without requiring elevated permissions.
        /// It assumes that EventLog exists, while the Source may not exist. 
        /// </summary>
        /// <remarks>If this method fails, it only writes message to console.</remarks>
        /// <param name="message"></param>
        /// <param name="entryType"></param>
        public static void WriteEventLog(string message, EventLogEntryType entryType)
        {
            try
            {
                using (EventLog evLog = new EventLog())
                {
                    evLog.Log = Properties.Settings.Default.EventLogName;
                    evLog.Source = Properties.Settings.Default.EventSourceName;
                    evLog.WriteEntry(message, entryType);
                }
            }
            catch (Exception ex)
            {
                string msg = "Failed to write to EventLog message: " + message + Environment.NewLine;
                msg += "Exception:  " + ex.ToString();
                Console.Error.WriteLine(msg);
            }
        }
    }
}
