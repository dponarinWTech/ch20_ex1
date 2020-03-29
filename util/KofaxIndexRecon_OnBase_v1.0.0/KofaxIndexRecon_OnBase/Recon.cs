using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using log4net;

namespace KofaxIndexRecon_OnBase
{
    public class Recon
    {
        private static ILog log;
        internal static string spacer;
        internal SqlConnection kofaxConnection;

        public Recon(ILog logger)
        {
            log = logger;
            spacer = "    ";
        }

        public void StartRecon()
        {
            // opens SqlConnection to Kofax database and truncates tables
            if (!InitialSetup()) return;

            if (!TruncateTables())
            {
                if (kofaxConnection.State == ConnectionState.Open) { kofaxConnection.Close(); }
                log?.Error("Failed to truncate tables FormIDs_To_Process and OnBase_MemdocRecords");
                SendErrorEmailMessage("Failed to truncate tables FormIDs_To_Process and OnBase_MemdocRecords");
                return;
            }

            if (!Populate_FormIDs_To_Process(Properties.Settings.Default.SP_PopulateFormIdsToProcess))
            {
                if (kofaxConnection.State == ConnectionState.Open) { kofaxConnection.Close(); }
                SendErrorEmailMessage($"Failed to populate table FormIDs_To_Process by running stored procedure [{Properties.Settings.Default.SP_PopulateFormIdsToProcess}]");
                return;
            }

            if (!ExecSP(Properties.Settings.Default.SP_UpdateRecNotScanned, "FormInfo"))
            {
                if (kofaxConnection.State == ConnectionState.Open) { kofaxConnection.Close(); }
                SendErrorEmailMessage("Failed to update not scanned records in the table FormInfo");
                return;
            }

            string kofaxSqlQuery = "SELECT DISTINCT	UIDNumber FROM dbo.FormIDs_To_Process WITH(NOLOCK) ORDER BY UIDNumber";
            List<String> uidList = GetUniqueIDList(kofaxConnection, kofaxSqlQuery);
            if (uidList == null)
            {
                if (kofaxConnection.State == ConnectionState.Open) { kofaxConnection.Close(); }
                SendErrorEmailMessage("Failed to retrieve the list of UniqueID");
                return;
            }

            DataSet dSet;
            OnBaseCommunicator obCommunicator = new OnBaseCommunicator(log);
            dSet = obCommunicator.FetchOnBaseData(uidList);
            if (dSet == null)
            {
                if (kofaxConnection.State == ConnectionState.Open) { kofaxConnection.Close(); }
                SendErrorEmailMessage("Failed to retrieve records from OnBase");
                return;
            }

            if (!BulkCopyDataToKofaxTable(dSet.Tables[Properties.Settings.Default.TableName_MemdocRecords], kofaxConnection))
            {
                if (kofaxConnection.State == ConnectionState.Open) { kofaxConnection.Close(); }
                SendErrorEmailMessage($"Failed to BulkCopy from DataSet to DB table [{dSet.Tables[0].TableName}]");
                return;
            }

            if (!ExecSP(Properties.Settings.Default.SP_UpdtRecScannedButMissingInCMS, 
                        Properties.Settings.Default.TableName_KofaxRecords))
            {
                if (kofaxConnection.State == ConnectionState.Open) { kofaxConnection.Close(); }
                SendErrorEmailMessage("Failed to update status of Kofax records without matching UID in OnBase");
                return;
            }

            dSet = AddSecondTableToDataSet(dSet, Properties.Settings.Default.TableName_KofaxRecords);
            if (dSet == null)
            {
                if (kofaxConnection.State == ConnectionState.Open) { kofaxConnection.Close(); }
                SendErrorEmailMessage($"Failed to add records from DB table {Properties.Settings.Default.TableName_KofaxRecords} to DataSet");
                return;
            }

            if (!AssignStatuses(dSet, uidList))
            {
                if (kofaxConnection.State == ConnectionState.Open) { kofaxConnection.Close(); }
                SendErrorEmailMessage("Failed to update statuses in DataSet");
                return;
            }

            if (!UpdateKofaxTableFromDataSet(dSet, Properties.Settings.Default.TableName_KofaxRecords))
            {
                if (kofaxConnection.State == ConnectionState.Open) { kofaxConnection.Close(); }
                SendErrorEmailMessage($"Failed to update DB table '{Properties.Settings.Default.TableName_KofaxRecords}' from DataSet");
                return;
            }

            if (!ExecSP(Properties.Settings.Default.SP_UpdateFormInfoTable, "FormInfo"))
            {
                if (kofaxConnection.State == ConnectionState.Open) { kofaxConnection.Close(); }
                SendErrorEmailMessage($"Failed to copy Status and Reason values from temp table '{Properties.Settings.Default.TableName_KofaxRecords}' to permanent table 'FormInfo'.");
                return;
            }

            if (!RecordLastSuccessfullDate(DateTime.Now))
            {
                string msg = "Failed to update in configuration file LastSuccessfullRun date. " + Environment.NewLine;
                msg += $"Please make sure that service account {System.Security.Principal.WindowsIdentity.GetCurrent().Name} ";
                msg += "has Modify permission on application installation folder. If this problem persist please notify BIS.";
                SendWarningEmailMessage(msg);
            }

            if (kofaxConnection.State == ConnectionState.Open) { kofaxConnection.Close(); }
            Console.WriteLine("Daily Recon Completed.\n");
            log?.Info("Daily Recon has completed at: " + DateTime.Now.ToString());
        }

        #region // Update XML 
        public bool RecordLastSuccessfullDate(DateTime dt)
        {
            try
            {
                string configFile = Assembly.GetEntryAssembly().Location + ".config";
                XDocument doc = XDocument.Load(configFile);

                List<XElement> settings = GetAllSettings(doc);
                XElement lastSuccessfulRun = GetSettingElement(settings, "LastSuccessfulRun");
                if (lastSuccessfulRun != null) lastSuccessfulRun.Value = dt.ToString("MM-dd-yyyy");
                else
                {
                    string msg = $"Failed to update setting value 'LastSuccessfulRun' to today's date.";
                    log?.Error(msg);
                    return false;
                }

                doc.Save(configFile);
                return true;
            }
            catch (Exception e)
            {
                string msg = $"Failed to update setting value 'LastSuccessfulRun' to today's date." + Environment.NewLine;
                msg += "Exception in RecordLastSuccessfullDate: " + e;
                log?.Error(msg);
                return false;
            }
        }

        public List<XElement> GetAllSettings(XDocument doc)
        {
            try
            {
                return doc.Element("configuration")?.Element("applicationSettings")
                    ?.Element($"KofaxIndexRecon_OnBase.Properties.Settings")?.Elements("setting").ToList();
            }
            catch { return new List<XElement>(); }
        }

        public XElement GetSettingElement(List<XElement> settings, string settingName)
        {
            if (settings == null || settings.Count == 0 || string.IsNullOrWhiteSpace(settingName)) return null;
            settingName = settingName.Trim();

            try
            {
                return settings.FirstOrDefault(x => x.Attribute("name")?.Value?.ToLower() == settingName?.ToLower())?.Element("value");
            }
            catch { return null; }
        }
        #endregion // Update XML 


        /// <summary>
        /// Contains logic assigning statuses FOUND, MISSING PARTIAL and Not Found to records in the given DataSet whose UIDs are in the given list. 
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="uidList"></param>
        /// <returns></returns>
        public bool AssignStatuses(DataSet ds, List<string> uidList)
        {
            log?.Info(spacer + "AssignStatuses(): Start");
            try
            {
                // Predicate to be used when we compare for equality RecordTriples - it includes logic that returns True if the 
                // only non-matching field is SSN or Account which is blank in one record and non-blank in other.
                // Added for bug 46040
                RecordTripletEqualityComparer comparer = new RecordTripletEqualityComparer();

                foreach (string uid in uidList) // processing records one group corresponding to one UID at a time 
                {
                    DataTable tblKofax = ds.Tables[Properties.Settings.Default.TableName_KofaxRecords];
                    DataTable tblOnBase = ds.Tables[Properties.Settings.Default.TableName_MemdocRecords];
                    var rowsKofax = tblKofax.AsEnumerable().Where(r => r.Field<String>("UIDNumber") == uid);
                    var rowsOnBase = tblOnBase.AsEnumerable().Where(r => r.Field<String>("SpStr3") == uid);

                    // if this record has status 'MISSING-PROCESSED-T' make it 'MISSING'
                    if (IsSatusInList(rowsKofax, "MISSING-PROCESSED-T"))
                    {
                        UpdateRecords(rowsKofax, "MISSING", null, DateTime.Now);
                        continue;
                    }

                    // find Kofax records corresponding to Tower records with blank "docdate" - what SP KfxIndxRcon_UpdtRecCreatDateNotFoundInTower failed to do
                    if (IsDocDateBlank(rowsOnBase))
                    {
                        UpdateRecords(rowsKofax, "MISSING", "No Creation Date found in OnBase", DateTime.Now);
                        continue;
                    }

                    if (!AreScanDatesMatch(rowsKofax, rowsOnBase))
                    {
                        UpdateRecords(rowsKofax, "MISSING", "Creation Date does not match.", DateTime.Now);
                        continue;
                    }

                    if (String.IsNullOrEmpty(rowsKofax.First().Field<string>("SSN")) && String.IsNullOrEmpty(rowsKofax.First().Field<string>("Account")))
                    {
                        UpdateRecords(rowsKofax, "Not Found", "Both SSN and Account are blank", DateTime.Now);
                        continue;
                    }

                    // if list of all triplets (SSN, Account, CreateDate) from Kofax is a subsets of the corresponding Tower list – set Status to 'FOUND';
                    if (IsFoundAll(rowsKofax, rowsOnBase, comparer))
                    {
                        UpdateRecords(rowsKofax, "FOUND", String.Empty, DateTime.Now);
                    }
                    // if at least one Kofax triplet is found in the tower list, but not all triplets are found, then set Status to "PARTIAL".
                    else if (IsFoundAny(rowsKofax, rowsOnBase, comparer))
                    { 
                        UpdateRecords(rowsKofax, "PARTIAL", "Missing or mismatching SSN, Account number or CreateDate", DateTime.Now);
                    }
                    else if (IsSatusInList(rowsKofax, "Not Found"))
                    {
                        UpdateRecords(rowsKofax, "Not Found", String.Empty);
                    }
                    else if (rowsKofax.Any(IsStillUnprocessed))
                    {
                        UpdateRecords(rowsKofax, "MISSING", "Missing record", DateTime.Now);
                    }

                }

                return true;
            }
            catch (Exception e)
            {
                log?.Error("AssignStatuses - Exception: " + e.ToString());
                return false;
            }
            finally
            {
                log?.Info(spacer + "AssignStatuses(): End");
            }
        }

        /// <summary>
        /// Updates the table in the DB with information from the corresponding table in the DataSet using automatically generated
        /// UpdateCommand of the SqlDataAdapter. 
        /// Note: table '<paramref name="tableName"/>' must have a Primary Key, otherwise updateCommand couldn't be generated.
        /// </summary>
        /// <param name="ds">DataSet</param>
        /// <param name="tableName">Name of the table in the DB and in the DataSet. </param>
        /// <returns>True if updated successfully</returns>
        public bool UpdateKofaxTableFromDataSet(DataSet ds, String tableName)
        {
            // Select command: we'll set it, but we wouldn't execute it. Setting SelectCommand is a minimum requirement for
            // automatic command generation to work. We'll use automatic command generation to generate UpdateCommand.
            string selectDummyCommand = $"SELECT * FROM {tableName}"; 

            try
            {
                log?.Info(spacer + "UpdateTableFromDataSet(): Start");

                int dbCount = 0; // number of rows successfully updated in the database
                using (SqlDataAdapter sqlDtaAdapter = new SqlDataAdapter())
                {
                    sqlDtaAdapter.SelectCommand = new SqlCommand(selectDummyCommand, kofaxConnection);

                    using (SqlCommandBuilder cb = new SqlCommandBuilder(sqlDtaAdapter))
                    {
                        sqlDtaAdapter.UpdateCommand = cb.GetUpdateCommand();
                        dbCount = sqlDtaAdapter.Update(ds, tableName);
                    }
                }

                int dsCount = ds.Tables[tableName].Rows.Count;
                log?.Debug(spacer + "    " + $"Updated {dbCount} rows in the DB table '{tableName}'; DataSet row count is {dsCount}");
                return true;
            }
            catch (SqlException s)
            {
                log?.Error("UpdateTableFromDataSet - SQL Exception: " + s.ToString());
                return false;
            }
            catch (TimeoutException t)
            {
                log?.Error("UpdateTableFromDataSet - Timeout Exception: " + t.ToString());
                return false;
            }
            catch (Exception e)
            {
                log?.Error("UpdateTableFromDataSet - Exception: " + e.ToString());
                return false;
            }
            finally
            {
                log?.Info(spacer + "UpdateTableFromDataSet(): End");
            }
        }

        /// <summary>
        /// Returns TRUE if for any element of collection '<paramref name="rowsKofax"/>' Status is equal to 
        /// one of statuses in '<paramref name="keyStatuses"/>'
        /// </summary>
        /// <param name="rowsKofax"></param>
        /// <param name="keyStatuses"></param>
        /// <returns></returns>
        public bool IsSatusInList(EnumerableRowCollection<DataRow> rowsKofax, params string[] keyStatuses)
        {
            if (keyStatuses == null || keyStatuses.Length == 0 || rowsKofax == null || rowsKofax.Count() == 0)
            {
                return false;
            }

            // before comparison convert all strings in keyStatuses to upper case 
            List<string> statusList = keyStatuses.ToList().ConvertAll(s => s?.ToUpper());

            return rowsKofax.Select(row => row.Field<String>("Status")?.Trim().ToUpper())
                .Any(s => !String.IsNullOrEmpty(s) && statusList.Contains(s));
        }

        /// <summary>
        /// Updates Status, Reason [optional] and UpdateDate [optional] fields in all rows in the given collection.
        /// To leave Reason unupdated skip argument '<paramref name="reason"/>' or give it null; to set Reason to null give it String.Empty.
        /// To leave UpdateDate unupdated skip argument '<paramref name="updateDate"/>' or give it null; 
        /// </summary>
        /// <param name="rowsToUpdate"></param>
        /// <param name="status"></param>
        /// <param name="reason"></param>
        /// <param name="updateDate"></param>
        public void UpdateRecords(EnumerableRowCollection<DataRow> rowsToUpdate, string status, string reason = null, DateTime? updateDate = null)
        {
            if (rowsToUpdate == null) return;

            foreach (var row in rowsToUpdate)
            {
                row.SetField("Status", status);
                if (reason != null) { row.SetField("Reason", String.IsNullOrEmpty(reason) ? null : reason.Trim()); }
                if (updateDate.HasValue) { row.SetField("UpdateDate", updateDate.Value); }
            }
        }

        /// <summary>
        /// Check if any Tower records corresponding to given uid have blank "DocDate" field
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool IsDocDateBlank(EnumerableRowCollection<DataRow> rowsTower)
        {
            if (rowsTower == null)
            {
                throw new ArgumentNullException("Argument of method IsDocDateBlank cannot be null");
            }
            return rowsTower
                .Where(row => DBNull.Value.Equals(row["DocDate"]))
                .Any();
        }

        /// <summary>
        /// Returns TRUE if CreateDate for all records in '<paramref name="rowsKofax"/>' is the same, 
        /// DocDate in all records in '<paramref name="rowsTower"/>' are the same, and these ScanDate and DocDate 
        /// match (based on Date only).
        /// </summary>
        /// <param name="rowsKofax"></param>
        /// <param name="rowsTower"></param>
        /// <returns></returns>
        public bool AreScanDatesMatch(EnumerableRowCollection<DataRow> rowsKofax, EnumerableRowCollection<DataRow> rowsTower)
        {
            if (rowsKofax == null || rowsTower == null)
            {
                throw new ArgumentNullException("No argument of method AreScanDatesMatch can be null");
            }
            return rowsKofax.All(r => rowsTower.All(s => AreDBDateTimesEqual(s, "DocDate", r, "CreateDate")));
        }

        /// <summary>
        /// Returns TRUE if dates in given fields of two given DatRows are the same (compares dates only, time is ignored). 
        /// If both dates are blank returns FALSE.
        /// </summary>
        /// <remarks>
        /// All arguments should not be null, otherwise ArgumentNullException is thrown.
        /// </remarks>
        /// <param name="rowKofax"></param>
        /// <param name="fieldNameKofax"></param>
        /// <param name="rowTower"></param>
        /// <param name="fieldNameTower"></param>
        /// <returns></returns>
        public bool AreDBDateTimesEqual(DataRow rowKofax, string fieldNameKofax, DataRow rowTower, string fieldNameTower)
        {
            if (rowKofax == null || rowTower == null || String.IsNullOrEmpty(fieldNameKofax) || String.IsNullOrEmpty(fieldNameTower))
            {
                throw new ArgumentNullException("No argument of method AreDBDateTimesEqual can be null");
            }

            if (DBNull.Value.Equals(rowKofax[fieldNameKofax.Trim()]) ||
                DBNull.Value.Equals(rowTower[fieldNameTower.Trim()])) { return false; }
            else
            {
                return rowKofax.Field<DateTime>(fieldNameKofax.Trim()).Date == rowTower.Field<DateTime>(fieldNameTower.Trim()).Date;
            }
        }

        /// <summary>
        /// Returns TRUE if for all records in Kofax collection '<paramref name="rowsKofax"/>' there are records 
        /// with matching combinations (ssn, account, date) in Tower collection '<paramref name="rowsTower"/>'.
        /// </summary>
        /// 
        /// <remarks>
        /// 1. There may be more combinations (ssn, account, date) in Tower collection than in Kofax collection, it will still return true.
        /// 2. Dates are compared based on Date only.
        /// </remarks>
        ///
        /// <param name="rowsKofax"></param>
        /// <param name="rowsTower"></param>
        /// <param name="comparer">Custom predicate to be used when we compare for equality RecordTriples</param>
        /// <returns></returns>
        public bool IsFoundAll(EnumerableRowCollection<DataRow> rowsKofax, EnumerableRowCollection<DataRow> rowsTower, RecordTripletEqualityComparer comparer)
        {
            if (rowsKofax == null || rowsTower == null) { return false; }

            var kofaxRecords = rowsKofax
                .Select(row => new RecordTriplet(row.Field<String>("SSN"), row.Field<String>("Account"), row.Field<DateTime?>("CreateDate")));

            var towerRecords = rowsTower
                .Select(row => new RecordTriplet(row.Field<String>("SSN"), row.Field<String>("Account"), row.Field<DateTime?>("docdate")));

            return kofaxRecords.All(recordKofax => towerRecords.Contains(recordKofax, comparer));
        }

        /// <summary>
        /// Returns TRUE if for at least one record in kofax collection '<paramref name="rowsKofax"/>' there is
        /// a record with matching combination (ssn, account, date) in Tower collection '<paramref name="rowsTower"/>'.
        /// </summary>
        /// 
        /// <remarks>
        /// Dates are compared based on Date only.
        /// </remarks>
        /// 
        /// <param name="rowsKofax"></param>
        /// <param name="rowsTower"></param>
        /// <param name="comparer">Custom predicate to be used when we compare for equality RecordTriples</param>
        /// <returns></returns>
        public bool IsFoundAny(EnumerableRowCollection<DataRow> rowsKofax, EnumerableRowCollection<DataRow> rowsTower, RecordTripletEqualityComparer comparer)
        {
            if (rowsKofax == null || rowsTower == null) { return false; }
            var kofaxRecords = rowsKofax
                .Select(row => new RecordTriplet(row.Field<String>("SSN"), row.Field<String>("Account"), row.Field<DateTime?>("CreateDate")));
            var towerRecords = rowsTower
                .Select(row => new RecordTriplet(row.Field<String>("SSN"), row.Field<String>("Account"), row.Field<DateTime?>("docdate")));

            return kofaxRecords.Any(recordKofax => towerRecords.Contains(recordKofax, comparer));
        }

        /// <summary>
        /// This Func&lt;DateRow, bool&gt;  returns true for the given row if either SSN or Account is not blank and Satatus is null
        /// or Status is anything but "Not Found" or "Missing"
        /// </summary>
        /// <remarks>
        /// We are not checking for satus 'Not Found' here; we assume that case was already processed.
        /// </remarks>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool IsStillUnprocessed(DataRow row)
        {
            if (row == null)
            {
                throw new ArgumentNullException("Argument of method IsStillUnprocessed cannot be null");
            }

            bool notBlankSSNOrAcc = !String.IsNullOrWhiteSpace(row.Field<String>("SSN")) || !String.IsNullOrWhiteSpace(row.Field<String>("Account"));
            string status = row.Field<String>("Status");
            if (DBNull.Value.Equals(status) || status == null) { status = String.Empty; }
            status = status.Trim();
            bool isBlank = String.IsNullOrEmpty(status);
            // we don't allow to change status from Partial to Missing (by including Partial to 'statusShouldNotChange') because we only process OnBase records 
            // added since last run of this application, and if Partial document was not rescaned since then its status would change to Missing - we have
            // to avoid it.
            bool statusShouldNotChange = status.ToUpper() == "PARTIAL" || status.ToUpper() == "NOT FOUND" || status.ToUpper() == "MISSING";

            return notBlankSSNOrAcc && (isBlank || !statusShouldNotChange);
        }

        /// <summary>
        /// Performs bulk copy of data from specified DataTable to Kofax DB table with the same name.
        /// </summary>
        /// <param name="tbl"></param>
        /// <returns></returns>
        public bool BulkCopyDataToKofaxTable(DataTable tbl, SqlConnection conn)
        {
            try
            {
                log?.Info(spacer + "BulkCopyDataToKofaxTable(): Start");
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    // since column positions in source DataTable match column positions in destination table there is no need to map columns.
                    bulkCopy.DestinationTableName = tbl.TableName;

                    bulkCopy.WriteToServer(tbl);
                }

                // because bulkCopy.WriteToServer() does not throw exception on SQL Server errors, to detect error
                // we compare numbers of rows in the database after insertion with number of rows in the DataSet
                int dbCount = GetRowCountFromDBTable(tbl.TableName, kofaxConnection);
                if (dbCount < 0)
                {
                    throw new Exception($"Failed to get count of rows in the table {tbl.TableName} after BulkCopy");
                }

                if (tbl.Rows.Count != dbCount)
                {
                    string msg = $"BulkCopy into table {tbl.TableName} failed: number of rows in DB table is {dbCount}, ";
                    msg += $"number of rows in DataSet is {tbl.Rows.Count}";
                    throw new Exception(msg);
                }
                log?.Debug(spacer + $"Performed BulkCopy of {dbCount} rows from DataSet to DB table '{tbl.TableName}'");
                return true;
            }
            catch (SqlException s)
            {
                log?.Error("BulkCopyDataToKofaxTable - SQL Exception: " + s.ToString());
                return false;
            }
            catch (TimeoutException t)
            {
                log?.Error("BulkCopyDataToKofaxTable - Timeout Exception: " + t.ToString());
                return false;
            }
            catch (Exception e)
            {
                log?.Error("BulkCopyDataToKofaxTable - Exception: " + e.ToString());
                return false;
            }
            finally
            {
                log?.Info(spacer + "BulkCopyDataToKofaxTable(): End");
            }
        }

        private int GetRowCountFromDBTable(string tableName, SqlConnection connection)
        {
            try
            {
                string kofaxSqlQuery = $"SELECT COUNT(*) FROM {tableName} WITH(NOLOCK)";
                int count = -1;
                using (SqlCommand cmdCount = new SqlCommand(kofaxSqlQuery, connection))
                {
                    count = (int)cmdCount.ExecuteScalar();
                }
                return count;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Reads data from DB table '<paramref name="tableName"/>' and adds DataTable with these data and same table name to the specified DataSet
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public DataSet AddSecondTableToDataSet(DataSet ds, string tableName)
        {
            log?.Info(spacer + "AddSecondTableToDataSet(): Start");
            string selectSqlCommand = $"SELECT * FROM {tableName} ORDER BY UIDNumber";
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter();
                using (SqlCommand selectCommand = kofaxConnection.CreateCommand())
                {
                    selectCommand.CommandText = selectSqlCommand;
                    adapter.SelectCommand = selectCommand;
                    adapter.Fill(ds, tableName);
                }

                int rowCount = ds.Tables[tableName].Rows.Count;
                log?.Debug(spacer + "    " + $"Added to DataSet table '{tableName}' with {rowCount} rows");
                return ds;
            }
            catch (SqlException s)
            {
                log?.Error("AddSecondTableToDataSet - SQL Exception: " + s.ToString());
                return null;
            }
            catch (TimeoutException t)
            {
                log?.Error("AddSecondTableToDataSet - Timeout Exception: " + t.ToString());
                return null;
            }
            catch (Exception e)
            {
                log?.Error("AddSecondTableToDataSet - Exception: " + e.ToString());
                return null;
            }
            finally
            {
                log?.Info(spacer + "AddSecondTableToDataSet(): End");
            }
        }

        /// <summary>
        /// Opens SqlConnection to Kofax_FormInfo database.
        /// </summary>
        /// <returns></returns>
        public bool InitialSetup()
        {
            log?.Info(Environment.NewLine +
                     $"\t\t ***** KofaxIndexRecon_OnBase  v.{Assembly.GetExecutingAssembly().GetName().Version.ToString()} *****");
            log?.Info("Current thread is running under credentials: " +
                     System.Security.Principal.WindowsIdentity.GetCurrent().Name);

            kofaxConnection = OpenConnection(Properties.Settings.Default.KofaxConnString);
            if (kofaxConnection == null || kofaxConnection.State != ConnectionState.Open)
            {
                log?.Error("Failed to open DB connection to database Kofax_FormInfo");
                SendErrorEmailMessage("Failed to open DB connection to database Kofax_FormInfo");
                return false;
            }
            return true;
        }

        public static SqlConnection OpenConnection(string connString)
        {
            try
            {
                SqlHelper sqlHelper = new SqlHelper(log, spacer);
                SqlConnection resultConnection = sqlHelper.SetupConnection(connString);
                if (resultConnection == null || resultConnection.State == ConnectionState.Closed)
                {
                    log?.Error($"Failed to open DB connection with connection string [{connString}]");
                    return null;
                }
                return resultConnection;
            }
            catch (SqlException s)
            {
                log?.Error($"OpenConnection with connection string [{connString}] - SQL Exception: " + s.ToString());
                return null;
            }
            catch (TimeoutException t)
            {
                log?.Error($"OpenConnection with connection string [{connString}] - Timeout Exception: " + t.ToString());
                return null;
            }
            catch (Exception e)
            {
                log?.Error($"OpenConnection with connection string [{connString}] - Exception: " + e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Calling stored procedure which truncates tables.
        /// It truncates tables 'FormIDs_To_Process' and 'OnBase_MemdocRecords'.
        /// </summary>
        /// <returns>True on success</returns>
        public bool TruncateTables()
        {
            try
            {
                log?.Info(spacer + "TruncateTables(): Start");
                using (SqlCommand sqlCommand = kofaxConnection.CreateCommand())
                {
                    sqlCommand.CommandText = Properties.Settings.Default.SP_TruncateTables;
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandTimeout = Properties.Settings.Default.DbCmdTimeout;

                    SqlParameter retVal = sqlCommand.Parameters.AddWithValue("@ReturnResult", false);
                    retVal.Direction = ParameterDirection.Output;
                    retVal.SqlDbType = SqlDbType.Bit;

                    sqlCommand.ExecuteNonQuery();
                    if ((bool)retVal.Value)
                    {
                        log?.Info(spacer + "    " + "Successfully truncated tables FormIDs_To_Process and OnBases_MemdocRecords");
                        return true;   // leave connection kofaxConnection open
                    }
                    else
                    {
                        log?.Error("Failed to truncate tables FormIDs_To_Process and OnBase_MemdocRecords");
                        return false;
                    }
                }
            }
            catch (SqlException s)
            {
                log?.Error("TruncateTables - SQL Exception: " + s.ToString());
                return false;
            }
            catch (TimeoutException t)
            {
                log?.Error("TruncateTables - Timeout Exception: " + t.ToString());
                return false;
            }
            catch (Exception e)
            {
                log?.Error("TruncateTables - Exception: " + e.ToString());
                return false;
            }
            finally
            {
                log?.Info(spacer + "TruncateTables(): End");
            }
        }

        /// <summary>
        /// Populates table 'FormIDs_To_Process' with records from XML in 'FormInfo' (one record for each SSN/Account pair)
        /// by running stored procedure <paramref name="storedProcedureName"/>
        /// </summary>
        /// <remarks>
        /// Selected are records which have:
        ///   (ScanDate not null) AND ( (Status is 'MISSING', 'PARTIAL' or 'Not Found') OR
        ///   (Status is null AND ScanDate is more than 1 day back) )
        /// </remarks>
        /// <param name="storedProcedureName">Name of stored procedure populating table FormIDs_To_Process</param>
        /// <returns></returns>
        public bool Populate_FormIDs_To_Process(string storedProcedureName)
        {
            try
            {
                log?.Info(spacer + "Populate_FormIDs_To_Process(): Start");
                log?.Info(spacer + $"  Stored procedure populating table FormIDs_To_Process: [{storedProcedureName}]");
                using (SqlCommand sqlCommand = kofaxConnection.CreateCommand())
                {
                    sqlCommand.CommandText = storedProcedureName;
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandTimeout = Properties.Settings.Default.DbCmdTimeout;

                    SqlParameter dayDiff = sqlCommand.Parameters.AddWithValue("@DayDifference", Properties.Settings.Default.NumDaysBeforeEmptyStatusScanned);
                    dayDiff.Direction = ParameterDirection.Input;
                    dayDiff.SqlDbType = SqlDbType.Int;

                    SqlParameter retCount = sqlCommand.Parameters.AddWithValue("@ReturnRowCounts", 0);
                    retCount.Direction = ParameterDirection.Output;
                    retCount.SqlDbType = SqlDbType.Int;

                    SqlParameter retVal = sqlCommand.Parameters.AddWithValue("@ReturnResult", false);
                    retVal.Direction = ParameterDirection.Output;
                    retVal.SqlDbType = SqlDbType.Bit;

                    sqlCommand.ExecuteNonQuery();

                    if ((bool)retVal.Value)
                    {
                        int affectedRows = (int)retCount.Value;
                        log?.Info(spacer + "    " + $"Successfully inserted {affectedRows} rows into table FormIDs_To_Process");
                        return true;
                    }
                    else
                    {
                        log?.Error($"Failed to populate table FormIDs_To_Process by running stored procedure [{storedProcedureName}]");
                        return false;
                    }
                }

                    
            }
            catch (SqlException s)
            {
                log?.Error("Populate_FormIDs_To_Process - SQL Exception: " + s.ToString());
                return false;
            }
            catch (TimeoutException t)
            {
                log?.Error("Populate_FormIDs_To_Process - Timeout Exception: " + t.ToString());
                return false;
            }
            catch (Exception e)
            {
                log?.Error("Populate_FormIDs_To_Process - Exception: " + e.ToString());
                return false;
            }
            finally
            {
                log?.Info(spacer + "Populate_FormIDs_To_Process(): End");
            }
        }

        public bool ExecSP(string spName, string tableName)
        {
            try
            {
                log?.Info(spacer + $"ExecSP({spName}): Start");
                using (SqlCommand sqlCommand = kofaxConnection.CreateCommand())
                {
                    sqlCommand.CommandText = spName;
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandTimeout = Properties.Settings.Default.DbCmdTimeout;

                    SqlParameter retCount = sqlCommand.Parameters.AddWithValue("@ReturnRowCounts", 0);
                    retCount.Direction = ParameterDirection.Output;
                    retCount.SqlDbType = SqlDbType.Int;

                    SqlParameter retVal = sqlCommand.Parameters.AddWithValue("@ReturnResult", false);
                    retVal.Direction = ParameterDirection.Output;
                    retVal.SqlDbType = SqlDbType.Bit;

                    sqlCommand.ExecuteNonQuery();

                    if ((bool)retVal.Value)
                    {
                        int affectedRows = (int)retCount.Value;
                        log?.Info(spacer + "    " + $"SP '{spName}' successfully updated {affectedRows} rows in the table {tableName}");
                        return true;
                    }
                    else
                    {
                        log?.Error($"SP '{spName}' has failed.");
                        return false;
                    }
                }

            }
            catch (SqlException s)
            {
                log?.Error($"ExecSP({spName}) - SQL Exception: " + s.ToString());
                return false;
            }
            catch (TimeoutException t)
            {
                log?.Error($"ExecSP({spName}) - Timeout Exception: " + t.ToString());
                return false;
            }
            catch (Exception e)
            {
                log?.Error($"ExecSP({spName}) - Exception: " + e.ToString());
                return false;
            }
            finally
            {
                log?.Info(spacer + $"ExecSP({spName}): End");
            }
        }

        /// <summary>
        /// Queries DB table FormIDs_To_Process using specified query DB connection (connection assumed to be open). Produces a list of distinct
        /// values in the column UIDNumber.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<String> GetUniqueIDList(SqlConnection connection, string query)
        {
            log?.Info(spacer + "GetUniqueIDList(): Start");
            List<String> result = new List<String>();
            try
            {
                using (DataSet uidDataSet = new DataSet())
                {
                    using (SqlDataAdapter uidAdapter = new SqlDataAdapter(query, connection))
                    {
                        uidAdapter.SelectCommand.CommandTimeout = Properties.Settings.Default.DbCmdTimeout;
                        uidAdapter.FillSchema(uidDataSet, SchemaType.Source, "UniqueID_Table");
                        uidAdapter.Fill(uidDataSet, "UniqueID_Table");
                        log?.Debug(spacer + "    " + "GetUniqueIDList: Number of unique IDs fetched: " + uidDataSet.Tables["UniqueID_Table"].Rows.Count);

                        foreach (DataRow row in uidDataSet.Tables["UniqueID_Table"].Rows)
                        {
                            result.Add(row["UIDNumber"].ToString());
                        }

                    }
                }
                return result;
            }
            catch (SqlException s)
            {
                log?.Error("GetUniqueIDList - SQL Exception: " + s.ToString());
                return null;
            }
            catch (TimeoutException t)
            {
                log?.Error("GetUniqueIDList - Timeout Exception: " + t.ToString());
                return null;
            }
            catch (Exception e)
            {
                log?.Error("GetUniqueIDList - Exception: " + e.ToString());
                return null;
            }
            finally
            {
                log?.Info(spacer + "GetUniqueIDList(): End");
            }
        }



        public static void SendErrorEmailMessage(string msg)
        {
                log?.Info(spacer + "SendErrorEmailMessage(): Start");

                SECUEmailEWS secuEmailEWS = new SECUEmailEWS(log, "    ");

                // emails 'From' and 'To' are set from config values in the class SECUEmailEWS
                secuEmailEWS.Subject = Properties.Settings.Default.EmailErrorSubject;
                secuEmailEWS.Body = "Please do not reply to this message." + Environment.NewLine + Environment.NewLine +
                                    "KofaxIndexRecon_OnBase has failed." + Environment.NewLine + "Error details:  " + msg;
            try
            {
                secuEmailEWS.SendEmail();
                if(!string.IsNullOrEmpty(secuEmailEWS.ErrorMessage))
                {
                    string message = "Failed to send email. Error message: " + secuEmailEWS.ErrorMessage;
                    log?.Error(message);
                }
            }
            catch (Exception ex)
            {
                log?.Error("Exception caught in SendErrorEmailMessage(): " + ex.ToString());
            }
            finally
            {
                log?.Info(spacer + "SendErrorEmailMessage(): end");
            }
        }

        public static void SendWarningEmailMessage(string msg)
        {
            log?.Info(spacer + "SendWarningEmailMessage(): Start");

            SECUEmailEWS secuEmailEWS = new SECUEmailEWS(log, "    ");

            // emails 'From' and 'To' are set from config values in the class SECUEmailEWS
            secuEmailEWS.Subject = Properties.Settings.Default.EmailWarningSubject;
            secuEmailEWS.Body = "Please do not reply to this message." + Environment.NewLine + Environment.NewLine +
                                    "KofaxIndexRecon_OnBase has encountered a non-fatal issue." + Environment.NewLine + "Details:  " + msg;

            try
            {
                secuEmailEWS.SendEmail();
                if (!string.IsNullOrEmpty(secuEmailEWS.ErrorMessage))
                {
                    string message = "Failed to send email. Error message: " + secuEmailEWS.ErrorMessage;
                    log?.Error(message);
                }
            }
            catch (Exception ex)
            {
                log?.Error("Exception caught in SendWarningEmailMessage(): " + ex.ToString());
            }
            finally
            {
                log?.Info(spacer + "SendWarningEmailMessage(): end");
            }
        }


    }


    /// <summary>
    /// Represents record object with 3 string fields: SSN, Account and CreateDate. Null values are represented by empty strings in object fields. 
    /// Field CreateDate contains string representation of the date without time.
    /// </summary>
    public class RecordTriplet
    {
        public string SSN { get; private set; }

        public string Account { get; private set; }

        public string CreateDate { get; private set; }

        // constructor with 3 parameters
        public RecordTriplet(string ssn, string acc, DateTime? createDate)
        {
            SSN = ssn == null ? String.Empty : ssn.Trim();
            Account = acc == null ? String.Empty : acc.Trim();
            CreateDate = createDate.HasValue ? createDate.Value.Date.ToString("MM-dd-yyyy") : String.Empty;
        }
    } //end class RecordTriplet

    /// <summary>
    /// Predicate for custom equality comparison of RecordTriplet objects. Objects are considered equal when only one of fields SSN or Account 
    /// don't match and in one of objects this field is blank while in other object it is non-blank.
    /// </summary>
    public class RecordTripletEqualityComparer : EqualityComparer<RecordTriplet>
    {
        // Derived from class EqualityComparer<T> (https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.equalitycomparer-1)
        // which is recommended base class for custom implementation of IEqualityComparer<T> interface.

        public override bool Equals(RecordTriplet first, RecordTriplet second)
        {
            if (first == null && second == null)
                return true;
            else if (first == null || second == null)
                return false;

            if (first.CreateDate.Trim() != second.CreateDate.Trim())
            {
                return false;
            }

            if ((first.SSN == string.Empty && first.Account == String.Empty) || (second.SSN == string.Empty && second.Account == String.Empty))
            {
                return false;
            }

            if (first.SSN == second.SSN && first.Account == second.Account)
            {
                return true;
            }

            if (first.SSN != second.SSN && first.Account != second.Account)
            {
                return false;
            }

            if (first.SSN == second.SSN && (first.Account == String.Empty || second.Account == String.Empty))
            {
                return true;
            }
            else if (first.Account == second.Account && (first.SSN == String.Empty || second.SSN == String.Empty))
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Method required for interface implementation, but is not used in our application
        /// </summary>
        /// <param name="tr"></param>
        /// <returns></returns>
        public override int GetHashCode(RecordTriplet tr)
        {
            if (tr == null) { return 0; }
            return tr.SSN.GetHashCode() ^ tr.Account.GetHashCode() ^ tr.CreateDate.GetHashCode();
        }
    }

}
