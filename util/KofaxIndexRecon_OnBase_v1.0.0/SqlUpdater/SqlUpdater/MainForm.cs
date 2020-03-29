using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SqlUpdater
{
    public partial class MainForm : Form
    {
        private string connString;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            lblError.Text = string.Empty;
            lblError.Visible = false;

            lblDbServer.Text = "DB Server \n(SERVER\\INSTACE if multiple SQL Server instances are installed on the server)";

            lblConnString.Visible = false;
            tbConnString.Visible = false;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnVerify_Click(object sender, EventArgs e)
        {
            lblError.Text = string.Empty;
            if (!VerifyInputFields()) { return; }
            UpdateConnectionString();

            tbConnString.Text = connString;
            lblConnString.Visible = true;
            tbConnString.Visible = true;

            if (TestDBConnection())
            {
                lblError.ForeColor = Color.Green;
                lblError.Text = "Connection successful";
            }
            else
            {
                lblError.ForeColor = Color.Red;
                lblError.Text = "Connection FAILED. See ErrorLog file for details.";
            }
            lblError.Visible = true;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!VerifyInputFields()) { return; }
            UpdateConnectionString();

            lblConnString.Visible = false;
            tbConnString.Visible = false;
            lblError.ForeColor = Color.Black;
            lblError.Text = string.Empty;
            lblError.Visible = true;

            SqlConnection connection = null;
            try
            {
                connection = OpenConnection();
                if (!TestDBConnection(connection))
                {
                    lblError.ForeColor = Color.Red;
                    lblError.Text = "Database connection failed!";
                    tbConnString.Text = connString;
                    tbConnString.Visible = true;
                    return;
                }

                // get table names
                string uidTableName = GetQueryResult(Properties.Settings.Default.SQL_GetUIDTableName, connection);
                if (!ProcessError(uidTableName)) return;
                string accTableName = GetQueryResult(Properties.Settings.Default.SQL_GetAccTableName, connection);
                if (!ProcessError(accTableName)) return;
                string ssnTableName = GetQueryResult(Properties.Settings.Default.SQL_GetSsnTableName, connection);
                if (!ProcessError(ssnTableName)) return;
                string dateTableName = GetQueryResult(Properties.Settings.Default.SQL_GetDateStoredTableName, connection);
                if (!ProcessError(dateTableName)) return;

                //update Stored Procedure text
                string spText = Properties.Settings.Default.SP_Text.Replace(@"__uidTable__", uidTableName);
                spText = spText.Replace(@"__accTable__", accTableName);
                spText = spText.Replace(@"__ssnTable__", ssnTableName);
                spText = spText.Replace(@"__dateTable__", dateTableName);

                // write stored procedure to file "sql.sql" located in the same folder as executable
                string fullPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                fullPath = Path.Combine(Path.GetDirectoryName(fullPath), "SQL.sql");
                File.WriteAllText(fullPath, spText);

                string server = tbDbServer.Text?.Trim(); 
                string outputFile = Path.Combine(Path.GetDirectoryName(fullPath), "sqlcmd_Output.txt");

                // prepare command
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false; // will start the process in a new window - to see progress and terminate if it hangs
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.UseShellExecute = false;
                startInfo.FileName = "sqlcmd.exe";
                startInfo.Arguments = $" -S { server} -i \"{fullPath}\" -o \"{outputFile}\"";

                // run the command
                try
                {
                    using(Process runSqlProcess = Process.Start(startInfo))
                    {
                        runSqlProcess.WaitForExit();
                    }

                    lblError.ForeColor = Color.Green;
                    lblError.Text = "Sql update completed";
                }
                catch(Exception exc)
                {
                    string msg = $"Exception thrown in btnOK_Click when trying to run script [{fullPath}] by SQLCMD.exe. See SQLCMD output in file [{outputFile}]";
                    msg += Environment.NewLine + "Exception: " + exc + Environment.NewLine;
                    LogError(msg);
                }

            }
            catch (Exception ex)
            {
                string msg = $"Exception thrown in btnOK_Click: " + Environment.NewLine + ex + Environment.NewLine;
                LogError(msg);
            }
            finally
            {
                if (connection != null && connection.State == ConnectionState.Open) { connection.Close(); }
            }

        }

        /// <summary>
        /// When OnBase query failed (signalled by blank input string) reflects it on front-end elements. 
        /// Returns whether input string is blank.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool ProcessError(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                lblConnString.Visible = true;
                tbConnString.Text = connString;
                tbConnString.Visible = true;
                lblError.ForeColor = Color.Red;
                lblError.Text = "Failed to read table name from OnBase. See ErrorLog file for details.";
                return false;
            }
            return true;
        }

        private string GetQueryResult(string query, SqlConnection conn)
        {
            string result = null;
            try
            {
                long num = -1;
                SqlCommand cmd = new SqlCommand(query, conn);
                num = (long)cmd.ExecuteScalar();
                result = $"OnBase.hsi.keyitem{num}";
                return result;
            }
            catch (Exception ex)
            {
                string msg = "Exception in GetQueryResult. Query: " + Environment.NewLine + "   " + query + Environment.NewLine;
                msg += "Exception:  " + ex + Environment.NewLine;
                LogError(msg);
                return string.Empty;
            }
        }

        /// <summary>
        /// It assumes that SqlConnection is open and does not close it.
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public bool TestDBConnection(SqlConnection conn)
        {
            string dbUser = null;
            try
            {
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    string errLog = string.Empty;
                    dbUser = SqlHelper.GetDBUser(conn, ref errLog);
                    if (!string.IsNullOrEmpty(errLog))
                    {
                        LogError(errLog);
                    }
                }

                return !string.IsNullOrEmpty(dbUser);
            }
            catch (Exception ex)
            {
                LogError("Exception in TestDBConnection: " + ex);
                MessageBox.Show("Exception in TestDBConnection(): " + ex);
                return false;
            }
        }

        public bool TestDBConnection()
        {
            SqlConnection conn = null;
            string dbUser = null;
            try
            {
                conn = OpenConnection();
                if (conn != null && conn.State == ConnectionState.Open)
                {
                    string errLog = string.Empty;
                    dbUser = SqlHelper.GetDBUser(conn, ref errLog);
                    if (!string.IsNullOrEmpty(errLog))
                    {
                        LogError(errLog);
                    }
                }

                return !string.IsNullOrEmpty(dbUser);
            }
            catch (Exception ex)
            {
                LogError("Exception in TestDBConnection: " + ex);
                MessageBox.Show("Exception in TestDBConnection(): " + ex);
                return false;
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open) { conn.Close(); }
            }
        }

        private SqlConnection OpenConnection()
        {
            SqlConnection result = null;
            string errorLog = string.Empty;

            SqlHelper sqlHelper = new SqlHelper(ref errorLog);
            result = sqlHelper.SetupConnection(connString);
            if (!string.IsNullOrEmpty(errorLog))
            {
                LogError(errorLog);
            }

            return result;
        }

        private void UpdateConnectionString()
        {
            connString = $"Data Source={tbDbServer.Text?.Trim()};Initial Catalog={tbDatabaseName.Text?.Trim()};Integrated Security=True;";
        }

        /// <summary>
        /// Returns true if all requred TextBoxes have non-blank input
        /// </summary>
        /// <returns></returns>
        private bool VerifyInputFields()
        {
            List<TextBox> inputs = new List<TextBox>() { tbDbServer, tbDatabaseName};
            var emptyTextBox = inputs.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Text));
            if (emptyTextBox != null)
            {
                MessageBox.Show(new Form { TopMost = true }, "All input fields are required.");
                emptyTextBox.Focus();
                return false;
            }
            else { return true; }
        }

        private void LogError(string message)
        {
            try
            {
                string msg = string.Empty;
                if (!File.Exists(Properties.Settings.Default.ErrorLogFile))
                {
                    msg = "********** SqlUpdater failed! **********" + Environment.NewLine;
                    msg += DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss    ") + "Current thread is running under credentials: " +
                        System.Security.Principal.WindowsIdentity.GetCurrent().Name + Environment.NewLine;
                    msg += DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss    ") + "Connection string: " +
                        connString + Environment.NewLine;
                }
                else
                {
                    msg += Environment.NewLine;
                }
                msg += Environment.NewLine + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss    ") + message;
                File.AppendAllText(Properties.Settings.Default.ErrorLogFile, msg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to write error log file. Exception: " + Environment.NewLine + ex, 
                                "Failed to Write ErrorLog File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
