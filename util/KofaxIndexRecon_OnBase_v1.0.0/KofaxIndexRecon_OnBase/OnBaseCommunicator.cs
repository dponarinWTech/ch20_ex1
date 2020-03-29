using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using log4net;

namespace KofaxIndexRecon_OnBase
{
    public class OnBaseCommunicator
    {
        private static ILog log;
        internal static string spacer;
        private SqlConnection onBaseConnection;

        public OnBaseCommunicator(ILog logger)
        {
            log = logger;
            spacer = "    ";
        }

        public DataSet FetchOnBaseData(List<String> uidList)
        {
            onBaseConnection = Recon.OpenConnection(Properties.Settings.Default.OnBaseConnString);
            if (onBaseConnection == null || onBaseConnection.State != ConnectionState.Open)
            {
                log?.Error("Failed to open DB connection to OnBase helper database");
                Recon.SendErrorEmailMessage("Failed to open DB connection to OnBase helper database");
                return null;
            }

            log?.Info(spacer + "Start fetching data from OnBase");
            string tblName = Properties.Settings.Default.TableName_MemdocRecords; // for convenience
            try
            {
                DataSet resultDataset = new DataSet();

                using (SqlCommand selectCommand = onBaseConnection.CreateCommand())
                {
                    selectCommand.CommandText = Properties.Settings.Default.SP_OnBaseFetching;
                    selectCommand.CommandType = CommandType.StoredProcedure;
                    selectCommand.CommandTimeout = Properties.Settings.Default.DbCmdTimeout;

                    SqlParameter parameter = selectCommand.Parameters.AddWithValue("@id_list", CreateDataTable(uidList));
                    parameter.Direction = ParameterDirection.Input;
                    parameter.SqlDbType = SqlDbType.Structured;
                    parameter.TypeName = "dbo.list_varchar";

                    SqlParameter inpStartDate = selectCommand.Parameters.Add("@StartDate", SqlDbType.DateTime);
                    inpStartDate.Value = GetLastSuccessfulRunDate(Properties.Settings.Default.LastSuccessfullRun, 
                                                                  Recon.SendWarningEmailMessage);
                    inpStartDate.Direction = ParameterDirection.Input;

                    SqlParameter retVal = selectCommand.Parameters.AddWithValue("@ReturnResult", false);
                    retVal.Direction = ParameterDirection.Output;
                    retVal.SqlDbType = SqlDbType.Bit;

                    SqlParameter retCount = selectCommand.Parameters.AddWithValue("@ReturnRowCounts", 0);
                    retCount.Direction = ParameterDirection.Output;
                    retCount.SqlDbType = SqlDbType.Int;

                    // create and run DataAdapter with this SelectCommand
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = selectCommand;

                    adapter.Fill(resultDataset, tblName);
                    if (!(bool)retVal.Value)
                    {
                        log?.Error(spacer + "Failed to populate DataSet with records from OnBase");
                        return null;
                    }
                    else
                    {
                        int receivedRowsCount = resultDataset.Tables[tblName].Rows.Count;
                        if ((int)retCount.Value != receivedRowsCount)
                        {
                            string msg = $"Row count returned by stored procedure [{Properties.Settings.Default.SP_OnBaseFetching}] is {(int)retCount.Value}, ";
                            msg += $"while row count in DataSet is {receivedRowsCount}";
                            log?.Warn(spacer + msg);
                        }
                        else
                        {                            
                            int count = resultDataset.Tables[tblName].Rows.Count;
                            log?.Info(spacer + $"Added to DataSet table [{tblName}] with {count} rows");
                        }
                        
                    }

                    return resultDataset;
                }

            }
            catch (SqlException s)
            {
                log?.Error(spacer + "FetchOnBaseData - SQL Exception: " + s.ToString());
                return null;
            }
            catch (TimeoutException t)
            {
                log?.Error(spacer + "FetchOnBaseData - Timeout Exception: " + t.ToString());
                return null;
            }
            catch (Exception e)
            {
                log?.Error(spacer + "FetchOnBaseData - Exception: " + e.ToString());
                return null;
            }
            finally
            {
                if(onBaseConnection != null && onBaseConnection.State != ConnectionState.Open)
                {
                    onBaseConnection.Close();
                }
                log?.Info(spacer + "End fetching data from OnBase");
            }

        }

        public DateTime GetLastSuccessfulRunDate(string inputDate, Action<string> emailSender)
        {
            DateTime startDate;
            try
            {
                startDate = DateTime.ParseExact(inputDate, "MM-dd-yyyy", 
                                                System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                log?.Error(spacer + "Error (non-fatal for the application) in GetLastSuccessfulRunDate. Exception: " + ex);
                startDate = DateTime.Now.Date.AddDays(-7);
                log?.Info(spacer + $"Use date {startDate} as a date of last succesful run");                
            }

            // if last successful run is more than 10 days back send warning
            if(DateTime.Now.Date.AddDays(-10) > startDate)
            {
                string msg = $"Date of last successful run read from config file {startDate} is more than 10 days back. " + Environment.NewLine;
                msg += $"Please make sure that service account {System.Security.Principal.WindowsIdentity.GetCurrent().Name} ";
                msg += "has Modify permission on application installation folder. If this problem persist please notify BIS.";
                emailSender(msg);
            }

            if(startDate > DateTime.Now.Date)
            {
                log?.Warn(spacer + $"Future date {startDate} as a date of last succesful run was read from config file.");
                return DateTime.Now.Date.AddDays(-7);
            }
            else
            {
                return startDate;
            }            
        }

        public DataTable CreateDataTable(IEnumerable<String> uidList)
        {
            DataTable table = new DataTable();
            table.Columns.Add("id", typeof(string));
            foreach (String uid in uidList)
            {
                table.Rows.Add(uid);
            }
            return table;
        }

    }
}
