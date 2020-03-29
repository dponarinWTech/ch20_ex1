using System;
using System.Data.SqlClient;
using log4net;

namespace KofaxIndexRecon_OnBase
{
    /// <summary>
    /// Helper class that configures SqlConnection to write SQL Server errors into caller's log
    /// </summary>
    public class SqlHelper
    {
        private ILog log;  // log can be null
        private string spacer;

        public SqlHelper(ILog logger, string spacer)
        {
            log = logger;
            this.spacer = spacer;
        }

        public void SqlInfoMessage(object sender, SqlInfoMessageEventArgs ea)
        {
            WriteSqlMsgs(ea.Errors);
        }

        public void WriteSqlMsgs(SqlErrorCollection msgs)
        {
            string message = "";
            foreach (SqlError e in msgs)
            {

                message += spacer + $"Message: '{e.Message}', Error Number: {e.Number}, Severity: {e.Class}, State: {e.State}, Procedure: {e.Procedure}, Line no: {e.LineNumber}" + Environment.NewLine;
                message += spacer + e.Message + Environment.NewLine;
            }

            log?.Error(spacer + "SQL Error: " + Environment.NewLine + message);
        }

        /// <summary>
        /// Returns opened SqlConnection which writes user SQL errors into caller application log
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public SqlConnection SetupConnection(string connectionString)
        {
            SqlConnection cn;
            string dbName = GetDbInfo(connectionString);
            log?.Debug(spacer + $"SetupConnection - connect to {dbName}");

            cn = new SqlConnection(connectionString);

            // Handle user errors with callbacks, rather than exception.
            cn.InfoMessage += SqlInfoMessage;
            cn.FireInfoMessageEventOnUserErrors = true;

            cn.Open();
            log?.Debug(spacer + $"Connection to database {dbName} successfully open");

            return cn;
        }


        public string GetDbInfo(string connectionString)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
            string server = builder.DataSource;
            string db = builder.InitialCatalog;

            return $"database [{db}] on server [{server}]";
        }


        /// <summary>
        /// Returns current user from DB query using specified Sql connection
        /// </summary>
        /// <param name="conn">Sql connection</param>
        /// <returns></returns>
        public string GetDBUser(SqlConnection conn)
        {
            try
            {
                string result = String.Empty;
                using (SqlCommand selectCommand = conn.CreateCommand())
                {
                    selectCommand.CommandText = "SELECT SYSTEM_USER";

                    SqlDataReader myReader;
                    myReader = selectCommand.ExecuteReader();

                    while (myReader.Read())
                    {
                        result += myReader[0].ToString() + "  ";
                    }
                    myReader.Close();
                }

                return result;
            }
            catch (SqlException s)
            {
                log?.Error("GetDBUser - SQL Exception: " + s.ToString());
                return null;
            }
            catch (TimeoutException t)
            {
                log?.Error("GetDBUser - Timeout Exception: " + t.ToString());
                return null;
            }
            catch (Exception e)
            {
                log?.Error("GetDBUser - Exception: " + e.ToString());
                return null;
            }
        }
    }
}
