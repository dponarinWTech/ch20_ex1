using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = NUnit.Framework.Assert;
using System.Configuration;

namespace KofaxIndexRecon_OnBase.Test
{
    [TestFixture]
    class KofaxIndexRecon_OnBaseUnitTests
    {
        private Recon reconTest;

        private DataSet ds;
        private List<string> uidList;

        private int recordCount;
        int uniqueId;
        private string tblOBRecords = Properties.Settings.Default.TableName_MemdocRecords;
        private string tblKofaxRecords = Properties.Settings.Default.TableName_KofaxRecords;

        [OneTimeSetUp]
        public void SetupOnce()
        {

            reconTest = new Recon(null);  // we will not use logger

            ds = new DataSet();
            uidList = new List<string>();

            DataTable tblOnBaseMemdocRecords = ds.Tables.Add(tblOBRecords);
            DataColumn spstr3 = tblOnBaseMemdocRecords.Columns.Add("SpStr3", typeof(string));
            spstr3.AllowDBNull = false;
            tblOnBaseMemdocRecords.Columns.Add("DocDate", typeof(DateTime)); // default value of AllowDBNull is true
            tblOnBaseMemdocRecords.Columns.Add("Account", typeof(string));
            tblOnBaseMemdocRecords.Columns.Add("SSN", typeof(string));

            DataTable tblFormIDsToProcess = ds.Tables.Add(tblKofaxRecords);
            DataColumn id = tblFormIDsToProcess.Columns.Add("Id", typeof(int));
            tblFormIDsToProcess.PrimaryKey = new DataColumn[] { id }; // automatically sets AllowDBNull = false, Unique = true
            DataColumn uidNumber = tblFormIDsToProcess.Columns.Add("UIDNumber", typeof(string));
            uidNumber.AllowDBNull = false;
            DataColumn uid = tblFormIDsToProcess.Columns.Add("UID", typeof(int));
            uid.AllowDBNull = false;
            tblFormIDsToProcess.Columns.Add("SSN", typeof(string));
            tblFormIDsToProcess.Columns.Add("Account", typeof(string));
            tblFormIDsToProcess.Columns.Add("CreateDate", typeof(DateTime));
            tblFormIDsToProcess.Columns.Add("UpdateDate", typeof(DateTime));
            tblFormIDsToProcess.Columns.Add("UpdateBy", typeof(string));
            tblFormIDsToProcess.Columns.Add("ScanDate", typeof(DateTime));
            tblFormIDsToProcess.Columns.Add("Status", typeof(string));
            tblFormIDsToProcess.Columns.Add("Reason", typeof(string));
        }

        [SetUp]
        public void Setup()
        {
            foreach (DataTable tbl in ds.Tables)
            {
                tbl.Rows.Clear();
            }
            uidList.Clear();
            recordCount = 0;
            uniqueId = 1;
        }

        [Test]
        public void IsDocDateBlankTest()
        {
            // add one record with blank DocDate in table 'OnBase_MemdocRecords'
            DataRow rowOnBase = AddOneRecordToOBRecords(++recordCount);
            var rowsOnBase = ds.Tables[tblOBRecords].AsEnumerable()
                .Where(r => r.Field<String>("SpStr3") == rowOnBase.Field<string>("SpStr3"));
            Assert.IsTrue((bool)reconTest.IsDocDateBlank(rowsOnBase), "One record with blank DocDate failed");

            // add one record with explicit NULL in DocDate in table 'OnBase_MemdocRecords'
            rowOnBase = AddOneRecordToOBRecords(++recordCount, null);
            rowsOnBase = ds.Tables[tblOBRecords].AsEnumerable()
                .Where(r => r.Field<String>("SpStr3") == rowOnBase.Field<string>("SpStr3"));
            Assert.IsTrue((bool)reconTest.IsDocDateBlank(rowsOnBase), "One record with explicit NULL in DocDate failed");

            // one record with non-blank DocDate
            DateTime date = DateTime.Now.AddDays(-3);
            rowOnBase = AddOneRecordToOBRecords(++recordCount, date);
            rowsOnBase = ds.Tables[tblOBRecords].AsEnumerable()
                .Where(r => r.Field<String>("SpStr3") == rowOnBase.Field<string>("SpStr3"));
            Assert.IsFalse((bool)reconTest.IsDocDateBlank(rowsOnBase), "One record with non-blank DocDate failed");

            // use all existing records: 2 with balnk date, one with non-blank
            var allRowsTower = ds.Tables[tblOBRecords].AsEnumerable();
            Assert.IsTrue((bool)reconTest.IsDocDateBlank(allRowsTower), 
                "2 records with blank Docdate and 1 record with non-blank failed");

            // 2 records with non-blank data
            rowOnBase = AddOneRecordToOBRecords(++recordCount, date);
            rowsOnBase = ds.Tables[tblOBRecords].AsEnumerable().Where(r => !DBNull.Value.Equals(r["DocDate"]));
            Assert.IsFalse((bool)reconTest.IsDocDateBlank(rowsOnBase), "2 records with non-blank DocDate failed");

            // 2 records with blank data
            rowsOnBase = ds.Tables[tblOBRecords].AsEnumerable().Where(r => DBNull.Value.Equals(r["DocDate"]));
            Assert.IsTrue((bool)reconTest.IsDocDateBlank(rowsOnBase), "2 records with lank DocDate failed");

            // null argument
            try
            {
                reconTest.IsDocDateBlank(null);

                //need this line to make TestCase fail if exception was not thrown
                Assert.Fail("Expected an exception to be thrown, but it was not");
            }
            catch (Exception ex)
            {
                // verify that Exception is of the type we expect
                Assert.IsInstanceOf<ArgumentNullException>(ex, $"Expected exception ArgumentNullException but got '{ex.Message}'");
            }
        }

        [Test]
        public void UpdateRecordsTest()
        {
            //++recordCount;
            DataRow aRow = AddOneRecordToKofax(++recordCount, status: "BLANK", reason: "No reason");
            //++recordCount;
            aRow = AddOneRecordToKofax(++recordCount, status: "BLANK", reason: "No reason");
            var rowsToUpdate = ds.Tables[tblKofaxRecords].AsEnumerable();

            // Update-1
            reconTest.UpdateRecords(rowsToUpdate, "UPDATE-1", "Good Reason", null);
            Assert.IsTrue(rowsToUpdate.All(r => (string)r["Status"] == "UPDATE-1"), "Update-1, Status is wrong.");
            Assert.IsTrue(rowsToUpdate.All(r => (string)r["Reason"] == "Good Reason"), "Update-1, Reason is wrong.");
            Assert.IsTrue(rowsToUpdate.All(r => DBNull.Value.Equals(r["UpdateDate"])), "Update-1, UpdateDate is wrong.");

            // Update-2, Reason = null so it should not be updated
            reconTest.UpdateRecords(rowsToUpdate, "UPDATE-2", null, null);
            Assert.IsTrue(rowsToUpdate.All(r => (string)r["Status"] == "UPDATE-2"), "Update-2, Status is wrong.");
            Assert.IsTrue(rowsToUpdate.All(r => (string)r["Reason"] == "Good Reason"), "Update-2, Reason is wrong.");
            Assert.IsTrue(rowsToUpdate.All(r => DBNull.Value.Equals(r["UpdateDate"])), "Update-2, UpdateDate is wrong.");

            // Update-3, Reason is empty string so it should be set to null
            reconTest.UpdateRecords(rowsToUpdate, "UPDATE-3", String.Empty, null);
            Assert.IsTrue(rowsToUpdate.All(r => (string)r["Status"] == "UPDATE-3"), "Update-3, Status is wrong.");
            Assert.IsTrue(rowsToUpdate.All(r => DBNull.Value.Equals(r["Reason"])), "Update-3, Reason is wrong.");
            Assert.IsTrue(rowsToUpdate.All(r => DBNull.Value.Equals(r["UpdateDate"])), "Update-3, UpdateDate is wrong.");

            // Update-4, non-null date
            DateTime? dtNow = DateTime.Now;
            reconTest.UpdateRecords(rowsToUpdate, "UPDATE-4", "No-Reason", dtNow);
            Assert.IsTrue(rowsToUpdate.All(r => (string)r["Status"] == "UPDATE-4"), "Update-4, Status is wrong.");
            Assert.IsTrue(rowsToUpdate.All(r => (string)r["Reason"] == "No-Reason"), "Update-4, Reason is wrong.");
            Assert.IsTrue(rowsToUpdate.All(r => (DateTime?)r["UpdateDate"] == dtNow), "Update-4, UpdateDate is wrong.");

            // Update-5, skip updateDate parameter so it should not be updated
            reconTest.UpdateRecords(rowsToUpdate, status:"UPDATE-5", reason:"nothing");
            Assert.IsTrue(rowsToUpdate.All(r => (string)r["Status"] == "UPDATE-5"), "Update-5, Status is wrong.");
            Assert.IsTrue(rowsToUpdate.All(r => (string)r["Reason"] == "nothing"), "Update-5, Reason is wrong.");
            Assert.IsTrue(rowsToUpdate.All(r => (DateTime?)r["UpdateDate"] == dtNow), "Update-5, UpdateDate is wrong.");

            // does not throw exception if rowsToUpdate is null
            Assert.DoesNotThrow(() => reconTest.UpdateRecords(rowsToUpdate, "UPDATE-6", "No-Reason", dtNow));
        }

        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", "0002", "0001", "0002", "2019-02-15", "2019-02-15", true)]
        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", "0002", "0001", "0002", "2019-02-15 14:35:00", "2019-02-15 10:00:00", true)]
        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", "0002", "0001", "0002", "2019-02-15", null, false)]
        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", "0002", "0001", "0002", null, "2019-02-15", false)]
        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", null, "0001", "0002", "2019-02-15", "2019-02-15", true)]
        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", "0002", "0001", null, "2019-02-15", "2019-02-15", false)]
        [TestCase("111111111", "222222222", "111111111", "222222222", null, null, "0001", "0002", "2019-02-15", "2019-02-15", true)]
        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", "0002", null, null, "2019-02-15", "2019-02-15", true)]
        [TestCase("111111111", null, "111111111", "222222222", "0001", "0002", "0001", "0002", "2019-02-15", "2019-02-15", true)]
        [TestCase("111111111", "222222222", "111111111", null, "0001", "0002", "0001", "0002", "2019-02-15", "2019-02-15", false)]
        [TestCase(null, null, "111111111", "222222222", "0001", "0002", "0001", "0002", "2019-02-15", "2019-02-15", true)]
        [TestCase("111111111", "222222222", null, null, "0001", "0002", "0001", "0002", "2019-02-15", "2019-02-15", true)]
        public void IsFoundAllTest(string ssn1, string ssn2, string ssn3, string ssn4,
                                   string acc1, string acc2, string acc3, string acc4,
                                   string dt1, string dt2, bool expect)
        {
            RecordTripletEqualityComparer comparer = new RecordTripletEqualityComparer();
            EnumerableRowCollection<DataRow> rowsKofax = AddKofaxRecorsdWithManySSNsAndAccounts(++recordCount, 
                                                                                                ListFromArgs(ssn1, ssn2).ToArray(), 
                                                                                                ListFromArgs(acc1, acc2).ToArray(), 
                                                                                                DateTimeFromString(dt1) );
            EnumerableRowCollection<DataRow> rowsOnBase = AddOnBaseRecorsdWithManySSNsAndAccounts(recordCount, 
                                                                                                  ListFromArgs(ssn3, ssn4).ToArray(), 
                                                                                                  ListFromArgs(acc3, acc4).ToArray(), 
                                                                                                  DateTimeFromString(dt2));

            Assert.AreEqual(expect, reconTest.IsFoundAll(rowsKofax, rowsOnBase, comparer));
        }

        [Test]
        public void IsFoundAllTestNullArgs()
        {
            RecordTripletEqualityComparer comparer = new RecordTripletEqualityComparer();
            DateTime now = DateTime.Now;

            var rowsKofax = AddKofaxRecorsdWithManySSNsAndAccounts(++recordCount, 
                                                                   new string[] { "111111111", "222222222" }, 
                                                                   new string[] { "000001", "000002" }, 
                                                                   createDate: now);
            var rowsOnBase = AddOnBaseRecorsdWithManySSNsAndAccounts(recordCount, 
                                                                     new string[] { "111111111", "222222222" },
                                                                     new string[] { "000001", "000002" }, 
                                                                     now);

            // expect false if rowsKofax, or rowsTower, or both is null
            Assert.AreEqual(false, reconTest.IsFoundAll( null, rowsOnBase, comparer), "Wrong result when rowsKofax is null");
            Assert.AreEqual(false, reconTest.IsFoundAll(rowsKofax, null, comparer), "Wrong result when rowsOnBase is null");
            Assert.AreEqual(false, reconTest.IsFoundAll(null, null, comparer), "Wrong result when both rowsKofax and rowsOnBase is null");
        }

        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", "0002", "0001", "0002", "2019-02-15", "2019-02-15", true)]
        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", "0002", "0001", "0002", "2019-02-15 14:35:00", "2019-02-15 10:00:00", true)]
        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", "0002", "0001", "0002", "2019-02-15", "", false)]
        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", "0002", "0001", "0002", null, "2019-02-15", false)]
        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", "0002", "0001", null, "2019-02-15", "2019-02-15", true)]
        [TestCase("111111111", "222222222", "111111111", "222222222", "0001", "0002", null, null, "2019-02-15", "2019-02-15", true)]
        [TestCase("111111111", null, "111111111", "222222222", "0001", "0002", "0001", "0002", "2019-02-15", "2019-02-15", true)]
        [TestCase(null, null, "111111111", "222222222", "0001", "0002", "0001", "0002", "2019-02-15", "2019-02-15", true)]
        [TestCase("111111111", "333333333", "111111111", "222222222", "0009", "0008", "0007", "0008", "2019-02-15", "2019-02-15", true)]
        public void IsFoundAnyTest(string ssn1, string ssn2, string ssn3, string ssn4,
                                   string acc1, string acc2, string acc3, string acc4,
                                   string dt1, string dt2, bool expect)
        {
            RecordTripletEqualityComparer comparer = new RecordTripletEqualityComparer();
            EnumerableRowCollection<DataRow> rowsKofax = AddKofaxRecorsdWithManySSNsAndAccounts(++recordCount,
                                                                                    ListFromArgs(ssn1, ssn2).ToArray(),
                                                                                    ListFromArgs(acc1, acc2).ToArray(),
                                                                                    DateTimeFromString(dt1));
            EnumerableRowCollection<DataRow> rowsOnBase = AddOnBaseRecorsdWithManySSNsAndAccounts(recordCount,
                                                                                                  ListFromArgs(ssn3, ssn4).ToArray(),
                                                                                                  ListFromArgs(acc3, acc4).ToArray(),
                                                                                                  DateTimeFromString(dt2));
            Assert.AreEqual(expect, reconTest.IsFoundAny(rowsKofax, rowsOnBase, comparer));
        }

        [Test]
        public void IsFoundAnyTestNullArgs()
        {
            RecordTripletEqualityComparer comparer = new RecordTripletEqualityComparer();
            var rowsKofax = AddKofaxRecorsdWithManySSNsAndAccounts(++recordCount, 
                                                                   new string[] { "111111111", "222222222" }, 
                                                                   new string[] { "000001", "000002" }, 
                                                                   createDate: DateTime.Now);
            var rowsOnBase = AddOnBaseRecorsdWithManySSNsAndAccounts(recordCount, 
                                                                     new string[] { "111111111", "222222222" }, 
                                                                     new string[] { "000001", "000002" }, 
                                                                     DateTime.Now);

            Assert.AreEqual(false, reconTest.IsFoundAny(null, rowsOnBase, comparer), "Wrong result when rowsKofax is null");
            Assert.AreEqual(false, reconTest.IsFoundAny(rowsKofax, null, comparer), "Wrong result when rowsOnBase is null");
            Assert.AreEqual(false, reconTest.IsFoundAny(null, null, comparer), "Wrong result when both rowsKofax and rowsOnBase is null");
        }

        [TestCase(" Missing ", true)]
        [TestCase("MISSING", true)]
        [TestCase("found", false)]
        [TestCase("  not found ", true)]
        [TestCase("MISSING-PROCESSED-T", true)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsSatusInListTest(string status, bool expectedValue)
        {
            DateTime dt = DateTime.Now.AddDays(-1);
            AddKofaxRecorsdWithManySSNsAndAccounts(++recordCount,  new string[] { "111111111" }, 
                                                                   new string[] { "0111" }, 
                                                                   dt, dt, dt, 
                                                                   status, "a reason");
            // add second record with same uid and different status
            AddKofaxRecorsdWithManySSNsAndAccounts(recordCount, new string[] { "222222222" }, 
                                                                    new string[] { "0222" }, 
                                                                    dt, dt, dt, 
                                                                    "Lost Cause", "a reason");

            var rowsKofax = ds.Tables[tblKofaxRecords].AsEnumerable().Where(r => r.Field<String>("UIDNumber") == uidList.Last());
            Assert.AreEqual(2, rowsKofax.Count(), "Wrong number of records in rowsKofax");
            Assert.AreEqual(expectedValue, reconTest.IsSatusInList(rowsKofax, "missing", "NOT Found", "MISSING-PROCESSED-T"));
        }

        [Test]
        public void IsSatusInListTestNullArgs()
        {
            DateTime dt = DateTime.Now;
            var rowsKofax = AddKofaxRecorsdWithManySSNsAndAccounts(++recordCount, 
                                                                   new string[] { "111111111" }, 
                                                                   new string[] { "0111" }, 
                                                                   dt, dt, dt, 
                                                                   "COMPLETED");
            Assert.AreEqual(false, reconTest.IsSatusInList( rowsKofax, null), "test failed when Status is null");
            Assert.AreEqual(false, reconTest.IsSatusInList(null, "missing", "NOT Found"), "test failed when rowsKofax is null");
        }

        [Test]
        public void AreDBDateTimesEqualTest()
        {
            DateTime dt = DateTime.Now.AddDays(-3);

            // Null in Kofax table, not null in OnBase
            DataRow rowKofax = AddOneRecordToKofax(++recordCount, status: "BLANK", reason: "No reason");
            DataRow rowOnBase = AddOneRecordToOBRecords(recordCount, dt);
            bool returnVal = reconTest.AreDBDateTimesEqual(rowKofax, "CreateDate", rowOnBase, "DocDate");
            Assert.AreEqual(false, returnVal, "null in rowKofax, not null in rowOnBase");

            // not null in Kofax, null in OnBase
            rowKofax = AddOneRecordToKofax(++recordCount, dt, dt, dt, status: "BLANK", reason: "No reason");
            rowOnBase = AddOneRecordToOBRecords(recordCount);
            returnVal = reconTest.AreDBDateTimesEqual(rowKofax, "CreateDate", rowOnBase, "DocDate");
            Assert.AreEqual(false, returnVal, "null in rowOnBase, not null in rowKofax");

            // nulls in both tables
            rowKofax = AddOneRecordToKofax(++recordCount, status: "BLANK", reason: "No reason");
            rowOnBase = AddOneRecordToOBRecords(recordCount);
            returnVal = reconTest.AreDBDateTimesEqual(rowKofax, "CreateDate", rowOnBase, "DocDate");
            Assert.AreEqual(false, returnVal, "nulls in both rowKofax and rowOnBase");

            // same date in both tables (in Kofax - with time, in OnBase - without)
            rowKofax = AddOneRecordToKofax(++recordCount, dt, dt, dt, status: "BLANK", reason: "No reason");
            rowOnBase = AddOneRecordToOBRecords(recordCount, dt);
            returnVal = reconTest.AreDBDateTimesEqual(rowKofax, "CreateDate", rowOnBase, "DocDate");
            Assert.AreEqual(true, returnVal, "same date in both tables");

            // different dates
            rowKofax = AddOneRecordToKofax(++recordCount, dt, dt, dt, status: "BLANK", reason: "No reason");
            dt = DateTime.Now.AddDays(-5);
            rowOnBase = AddOneRecordToOBRecords(recordCount, dt);
            Assert.AreNotEqual(rowKofax.Field<DateTime>("CreateDate"), rowOnBase.Field<DateTime>("DocDate"), "dates in Kofax and OnBase should be different");
            returnVal = reconTest.AreDBDateTimesEqual(rowKofax, "CreateDate", rowOnBase, "DocDate");
            Assert.AreEqual(false, returnVal, "Different dates in rowKofax and rowOnBase");
        }

        [TestCase("CreateDate", "DocDate", true, false)]
        [TestCase("CreateDate", "DocDate", false, true)]
        [TestCase(null, "DocDate", false, false)]
        [TestCase("CreateDate", null, false, false)]
        [TestCase(null, null, true, true)]
        public void AreDBDateTimesEqualTestNullArgs(string fieldNameKofax, string fieldNameOnBase, bool rowKofaxNull, bool rowOnBaseNull)
        {
            DateTime dt = DateTime.Now;
            DataRow rowKofax = rowKofaxNull ? null : AddOneRecordToKofax(++recordCount, dt, dt, dt, status: "BLANK", reason: "No reason");
            DataRow rowOnBase = rowOnBaseNull ? null : AddOneRecordToOBRecords(recordCount, dt);
            try
            {
                reconTest.AreDBDateTimesEqual(rowKofax, fieldNameKofax, rowOnBase, fieldNameOnBase);
                //need this line to make TestCase fail if exception was not thrown
                Assert.Fail("Expected an exception to be thrown, but it was not");
            }
            catch (Exception ex)
            {
                // verify that Exception is of the type we expect
                Assert.IsInstanceOf<ArgumentNullException>(ex, $"Expected ArgumentNullException but got '{ex.Message}'");
            }
        }

        [TestCase(null, null, null, null, false)]  // all dates are null
        [TestCase(null, "0", null, null, false)]  // one Kofax date is not null, all other dates are null
        [TestCase(null, null, "1", null, false)]  // one Tower date is not null, all other dates are null
        [TestCase("1", "1", "1", null, false)]    // one Tower date is null, all other dates are the same
        [TestCase("-2", null, "-2", null, false)]  // one of dates is not null in both Kofax and Tower
        [TestCase("1", "1", "1", "1", true)]     // equal dates
        [TestCase("-1", "0", "0", "0", false)]   // one of dates is different from others
        [TestCase("1", "1", "1", "0", false)]
        public void AreScanDatesMatchTest(int? dtKofax1, int? dtKofax2, int? dtOnBase1, int? dtOnBase2, bool expect)
        {
            DateTime? dtKof1, dtKof2, dtOB1, dtOB2;
            if (dtKofax1.HasValue) { dtKof1 = DateTime.Now.AddDays(dtKofax1.Value); }
            else { dtKof1 = null; }
            if (dtKofax2.HasValue) { dtKof2 = DateTime.Now.AddDays(dtKofax2.Value); }
            else { dtKof2 = null; }
            if (dtOnBase1.HasValue) { dtOB1 = DateTime.Now.AddDays(dtOnBase1.Value); }
            else { dtOB1 = null; }
            if (dtOnBase2.HasValue) { dtOB2 = DateTime.Now.AddDays(dtOnBase2.Value); }
            else { dtOB2 = null; }

            DataRow row = AddOneRecordToKofax(++recordCount, dtKof1, dtKof1, dtKof1, "BLANK", "No reason");
            AddOneRecordToOBRecords(recordCount, dtOB1);

            AddOneRecordToKofax(recordCount, dtKof2, dtKof2, dtKof2, "BLANK", "No reason"); // add second Kofax record with same UID
            AddOneRecordToOBRecords(recordCount, dtOB2);  // add OnBasde record with same UID

            Assert.AreEqual(expect, CallIsScanDateMismatch(row.Field<string>("UIDNumber")));
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void AreScanDatesMatchTestNullArgs(bool isKofaxNull, bool isTowerNull)
        {
            DateTime dt = DateTime.Now;
            EnumerableRowCollection<DataRow> rowsKofax = isKofaxNull ? null : 
                AddKofaxRecorsdWithManySSNsAndAccounts(++recordCount, null, null, dt, dt, dt);
            EnumerableRowCollection<DataRow> rowsOnBase = isTowerNull ? null : 
                AddOnBaseRecorsdWithManySSNsAndAccounts(recordCount, null, null, dt);

            AddOneRecordToOBRecords(recordCount, dt);
            try
            {
                reconTest.AreScanDatesMatch(rowsKofax, rowsOnBase);

                //need this line to make TestCase fail if exception was not thrown
                Assert.Fail("Expected an exception to be thrown, but it was not");
            }
            catch(Exception ex)
            {
                Assert.IsInstanceOf<ArgumentNullException>(ex, $"Expected ArgumentNullException but got '{ex.Message}'");
            }
        }

        [TestCase("111111111", "0011", "Not Found", false)]
        [TestCase("111111111", "0011", " Not FounD  ", false)]
        [TestCase("111111111", "0011", null, true)]
        [TestCase("111111111", null, null, true)]
        [TestCase(null, "0011", null, true)]
        [TestCase(null, null, "ZZZ", false)]
        [TestCase(null, null, null, false)]
        [TestCase("111111111", "0011", "Missing", false)]
        [TestCase("111111111", "0011", " MissinG ", false)]
        [TestCase("111111111", "0011", "Partial", false)]
        [TestCase("111111111", "0011", "  Partial  ", false)]
        [TestCase(null, "0011", "AAA", true)]
        [TestCase("111111111", "0011", "Not   Found", true)]
        public void IsStillUnprocessedTest(string ssn, string acc, string status, bool expectedValue)
        {
            DateTime now = DateTime.Now;
            DataRow row = AddKofaxRecorsdWithManySSNsAndAccounts(++recordCount, 
                                                                 new string[] { ssn }, 
                                                                 new string[] { acc }, 
                                                                 now, now, now, status).Last();
            Assert.AreEqual(expectedValue, reconTest.IsStillUnprocessed(row));
        }

        [Test]
        public void IsStillUnprocessedTestNullArg()
        {
            try
            {
                reconTest.IsStillUnprocessed(null);

                //need this line to make TestCase fail if exception was not thrown
                Assert.Fail("Expected an exception to be thrown, but it was not");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<ArgumentNullException>(ex, $"Expected ArgumentNullException but got '{ex.Message}'");
            }
        }
               
        // giving null as expReason tells the method to skip checking Reason

        // 1. Blank 'DocDate' in OnBase in 2nd record. Expected status: MISSING
        [TestCase("111111111", "222222222", "0011", "0022", "111111111", "222222222", "0011", "0022", "2019-02-13", null, "MISSING", "NO CREATION DATE FOUND IN ONBASE")]
        // 2. Date mismatch in 1st record. Expected status: MISSING
        [TestCase("111111111", "222222222", "0011", "0022", "111111111", "222222222", "0011", "0022", "2019-02-13", "2019-02-15", "MISSING", "CREATION DATE DOES NOT MATCH.")]
        // 3. Same Date but different Time in CreateDate. Expected status: FOUND
        [TestCase(null, null, "0011", "0022", null, null, "0011", "0022", "2019-02-15 14:35:00", "2019-02-15 10:00:00", "FOUND", null)]
        // 4. SSN mismatch in 2nd record. Expected status: PARTIAL
        [TestCase("111111111", "222222222", "0011", "0022", "111111111", "333333333", "0011", "0022", "2019-02-15", "2019-02-15", "PARTIAL", "MISSING OR MISMATCHING SSN, ACCOUNT NUMBER OR CREATEDATE")]
        // 5. Account mismatch in 2nd record. Expected status: PARTIAL
        [TestCase("111111111", "222222222", "0011", "0022", "111111111", "222222222", "0011", "0033", "2019-02-15", "2019-02-15", "PARTIAL", "MISSING OR MISMATCHING SSN, ACCOUNT NUMBER OR CREATEDATE")]
        // 6. Complete mismatch of SSNs and null Accounts. Expected status: MISSING
        [TestCase("111111111", "222222222", null, null, "999999999", "888888888", null, null, "2019-02-15", "2019-02-15", "MISSING", "MISSING RECORD")]
        // 7. 2 records with null both SSN and Account. Expected status: NOT FOUND
        [TestCase(null, null, null, null, null, null, null, null, "2019-02-15", "2019-02-15", "Not found", null)]
        // 8. In both records one of fields, SSN or Account, is blank in one table and non-blank in other (the rest is matching). Expected status: FOUND
        [TestCase(null, "222222222", "0011", "0022", "111111111", "222222222", "0011", null, "2019-02-15", "2019-02-15", "FOUND", null)]
        // 9. In both records one of fields, SSN or Account, is blank in both tables. Expected status: FOUND
        [TestCase(null, "222222222", "0011", null, null, "222222222", "0011", null, "2019-02-15", "2019-02-15", "FOUND", null)]
        public void AssignStatusesTest_1(string ssnKof1, string ssnKof2, string accKof1, string accKof2, 
                                               string ssnOb1, string ssnOb2, string accOb1, string accOb2,
                                               string date1, string date2, string expStatus, string expReason)
        {
            DateTime? dt1 = DateTimeFromString(date1);
            DateTime? dt2 = DateTimeFromString(date2);
            AddOneRecordToKofax(++recordCount, dt1, dt1, dt1, null, null, ssnKof1, accKof1);
            AddOneRecordToOBRecords(recordCount, dt1, ssnOb1, accOb1);

            AddOneRecordToKofax(recordCount, dt2, dt2, dt2, null, null, ssnKof2, accKof2);
            AddOneRecordToOBRecords(recordCount, dt2, ssnOb2, accOb2);

            Assert.IsTrue(reconTest.AssignStatuses(ds, uidList), "AssignStatuses returned false");

            var rows = ds.Tables[tblKofaxRecords].AsEnumerable().Where(r => r.Field<String>("UIDNumber") == uidList.Last());
            Assert.AreEqual(2, rows.Count(), $"Wrong number of rows with UID [{uidList.Last()}]");         
            bool allStatuses = rows.All(row => !String.IsNullOrEmpty(row.Field<string>("Status")) &&
                                               row.Field<string>("Status").Trim().ToUpper() == expStatus.ToUpper());
            Assert.IsTrue(allStatuses, $"Not all statuses have expected value {expStatus}");

            if (expReason != null)
            {
                bool allReasons = rows.All(row => !String.IsNullOrEmpty(row.Field<string>("Reason")) &&
                                      row.Field<string>("Reason").Trim().ToUpper() == expReason.ToUpper());
                Assert.IsTrue(allReasons, $"Not all reasons have expected value '{expReason}'");
            }
        }

        // 2 records, both are missing in OnBase. Expected status: MISSING
        [Test]
        public void AssignStatusesTest_2()
        {
            DateTime dt = DateTime.Now;
            AddKofaxRecorsdWithManySSNsAndAccounts(++recordCount, new string[] { "111111111" },
                                                   new string[] { "0011" }, dt, dt, dt, "BBBB");
            AddKofaxRecorsdWithManySSNsAndAccounts(recordCount, new string[] { "222222222" }, 
                                                   new string[] { "0022" }, dt, dt, dt, null, null);

            Assert.IsTrue(reconTest.AssignStatuses(ds, uidList), "AssignStatuses returned false");
            var rows = ds.Tables[tblKofaxRecords].AsEnumerable().Where(r => r.Field<String>("UIDNumber") == uidList.Last());
            Assert.AreEqual(2, rows.Count(), $"Wrong number of rows with UID [{uidList.Last()}]");

            bool allStatuses = rows.All(row => !String.IsNullOrEmpty(row.Field<string>("Status")) &&
                                               row.Field<string>("Status").Trim().ToUpper() == "MISSING");
            bool allReasons = rows.All(row => !String.IsNullOrEmpty(row.Field<string>("Reason")) &&
                                               row.Field<string>("Reason").Trim().ToUpper() == "MISSING RECORD");
            Assert.IsTrue(allStatuses, $"Not all statuses have expected value 'MISSING'");
            Assert.IsTrue(allReasons, "Not all reasons have expected value 'MISSING RECORD'");
        }

        // 2 records with matching fields in both tables, one have status MISSING-PROCESSED-T. Expected status: MISSING
        [Test]
        public void AssignStatusesTest_3()
        {
            DateTime dt = DateTime.Now;
            AddKofaxRecorsdWithManySSNsAndAccounts(++recordCount, new string[] { "111111111" },
                                                   new string[] { "0011" }, dt, dt, dt, "MISSING-PROCESSED-T", "Good Reason");
            AddOnBaseRecorsdWithManySSNsAndAccounts(recordCount, new string[] { "111111111" }, new string[] { "0011" }, dt);

            AddKofaxRecorsdWithManySSNsAndAccounts(recordCount, new string[] { "222222222" }, 
                                                   new string[] { "0022" }, dt, dt, dt, "MISSING-PROCESSED-T", "Good Reason");
            AddOnBaseRecorsdWithManySSNsAndAccounts(recordCount, new string[] { "222222222" }, new string[] { "0022" }, dt);


            Assert.IsTrue(reconTest.AssignStatuses(ds, uidList), "AssignStatuses() returned false");

            string status = "MISSING";
            string reason = "Good Reason";
            var rows = ds.Tables[tblKofaxRecords].AsEnumerable().Where(r => r.Field<String>("UIDNumber") == uidList.Last());
            Assert.AreEqual(2, rows.Count(), $"Wrong number of rows with UID [{uidList.Last()}]");
            Assert.AreEqual(1, uidList.Count, "Wrong number of uidList entries");

            bool allStatuses = rows.All(row => !String.IsNullOrEmpty(row.Field<string>("Status")) &&
                                   row.Field<string>("Status").Trim().ToUpper() == status.ToUpper());
            bool allReasons = rows.All(row => !String.IsNullOrEmpty(row.Field<string>("Reason")) &&
                                               row.Field<string>("Reason").Trim().ToUpper() == reason.ToUpper());

            Assert.IsTrue(allStatuses, $"Not all statuses have expected value '{status}'");
            Assert.IsTrue(allReasons, $"Not all reasons have expected value '{reason}'");
        }

        // One record in Kofax without match in OB with status 'Not Found'. Expected status: Not Found
        [Test]
        public void AssignStatusesTest_4()
        {
            string status = "Not Found";
            string reason = "Good Reason";
            DateTime dt = DateTime.Now;
            AddKofaxRecorsdWithManySSNsAndAccounts(++recordCount, new string[] { "999999999" }, 
                                                   new string[] { "0099" }, dt, dt, dt, status, reason);

            Assert.IsTrue(reconTest.AssignStatuses(ds, uidList), "AssignStatuses() returned false");
            Assert.AreEqual(1, ds.Tables[tblKofaxRecords].Rows.Count, $"Wrong number of rows in table [{tblKofaxRecords}]");

            var row = ds.Tables[tblKofaxRecords].AsEnumerable().First();
            Assert.AreEqual(status.ToUpper(), row.Field<string>("Status")?.Trim().ToUpper(), $"Wrong status '{row.Field<string>("Status")}'");
            Assert.IsNull(row.Field<string>("Reason"), $"Reason is '{row.Field<string>("Reason")}' instead of null");
        }

        // Two records in Kofax table without matching records in OnBase, both have status Partial. Expected status: Partial
        [TestCase("Partial", "ZZZ", "Partial", "ZZZ", "Partial", "ZZZ")]
        // Two records in Kofax table without matching records in OnBase, one has status Partial, another - not. Expected status: Missing
        [TestCase("Partial", "ZZZ", "aaa", "ZZZ", "Missing", "Missing record")]
        public void AssignStatusesTest_5(string status1, string reason1, string status2, string reason2, string expStatus, string expReason)
        {
            DateTime dt = DateTime.Now;
            AddOneRecordToKofax(++recordCount, dt, dt, dt, status1, reason1,"111111111", "0011");
            AddOneRecordToKofax(recordCount, dt, dt, dt, status2, reason2, "222222222", "0011");

            Assert.IsTrue(reconTest.AssignStatuses(ds, uidList), "AssignStatuses() returned false");
            Assert.AreEqual(2, ds.Tables[tblKofaxRecords].Rows.Count, $"Wrong number of rows in table [{tblKofaxRecords}]");
            Assert.AreEqual(1, uidList.Count, "Wrong number of uidList entries");            

            var rows = ds.Tables[tblKofaxRecords].AsEnumerable().Where(r => r.Field<String>("UIDNumber") == uidList.Last());

            bool allStatuses = rows.All(row => !String.IsNullOrEmpty(row.Field<string>("Status")) &&
                                   row.Field<string>("Status").Trim().ToUpper() == expStatus?.ToUpper());
            bool allReasons = rows.All(row => !String.IsNullOrEmpty(row.Field<string>("Reason")) &&
                                               row.Field<string>("Reason").Trim().ToUpper() == expReason?.ToUpper());

            Assert.IsTrue(allStatuses, $"Not all statuses have expected value '{expStatus}'");
            Assert.IsTrue(allReasons, $"Not all reasons have expected value '{expReason}'");
        }



        #region // Helper methods
        /// <summary>
        /// Adds to table 'FormIDs_To_Process' one record with UID based on count (Id is takedn from uniqueId field). 
        /// If SSN or Account value in not specified it will be generated based on count. Does not increment/decrement count.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="createDate">By default null</param>
        /// <param name="updateDate">By default null</param>
        /// <param name="scanDate">By default null</param>
        /// <param name="status">By default null</param>
        /// <param name="reason">By default null</param>
        /// <returns></returns>
        public DataRow AddOneRecordToKofax(int count,
                                   DateTime? createDate = null,
                                   DateTime? updateDate = null,
                                   DateTime? scanDate = null,
                                   string status = null,
                                   string reason = null,
                                   string ssn = "",
                                   string acc = "")
        {
            string uidStr = "TST" + count.ToString("D8");
            string mySsn;
            if (ssn != null && string.IsNullOrEmpty(ssn)) { mySsn = count.ToString("D9"); }
            else { mySsn = ssn; }
            string myAcc;
            if (acc != null && string.IsNullOrEmpty(acc)) { myAcc = count.ToString("D6"); }
            else { myAcc = acc; }
               
            DataRow row = ds.Tables[tblKofaxRecords].Rows.Add(uniqueId++, uidStr, count, mySsn, myAcc, createDate,
                updateDate, "s24715d", scanDate, status, reason);

            if (!uidList.Contains(uidStr)) { uidList.Add(uidStr); }
            return row;
        }

        /// <summary>
        /// Adds to table 'OnBase_MemdocRecords' one record with UID based on count. 
        /// If SSN or Account value in not specified it will be generated based on count. Does not increment/decrement count.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="docdate">By default null</param>
        /// <returns></returns>
        public DataRow AddOneRecordToOBRecords(int count, DateTime? docdate = null, string ssn = "", string acc = "")
        {
            string spStr3 = "TST" + count.ToString("D8");
            string mySsn;
            if (ssn != null && string.IsNullOrEmpty(ssn)) { mySsn = count.ToString("D9"); }
            else { mySsn = ssn; }
            string myAcc;
            if (acc != null && string.IsNullOrEmpty(acc)) { myAcc = count.ToString("D6"); }
            else { myAcc = acc; }
            if (docdate.HasValue)
            {
                docdate = docdate.Value;
            }

            if (!uidList.Contains(spStr3)) { uidList.Add(spStr3); }

            return ds.Tables[tblOBRecords].Rows.Add(spStr3, docdate, myAcc, mySsn);
        }

        public EnumerableRowCollection<DataRow> AddKofaxRecorsdWithManySSNsAndAccounts(int count, 
                                                                                        string[] SSNs = null, 
                                                                                        string[] accounts = null, 
                                                                                        DateTime? createDate = null, 
                                                                                        DateTime? updateDate = null, 
                                                                                        DateTime? scanDate = null, 
                                                                                        string status = null, 
                                                                                        string reason = null)
        {
            string uidStr = "TST" + count.ToString("D8");
            if (!uidList.Contains(uidStr)) { uidList.Add(uidStr); }

            if (SSNs != null && SSNs.Length > 0 && accounts != null && accounts.Length > 0)
            {
                foreach (string ssn in SSNs)
                {
                    foreach (string acc in accounts)
                    {
                        ds.Tables[tblKofaxRecords].Rows.Add(uniqueId++, uidStr, count, ssn, acc,
                            createDate, updateDate, "ONBASE", scanDate, status, reason);
                    }
                }
            }
            else if (SSNs != null && SSNs.Length > 0)
            {
                foreach (string ssn in SSNs)
                {
                    ds.Tables[tblKofaxRecords].Rows.Add(uniqueId++, uidStr, count, ssn, null, createDate, updateDate,
                                                             "ONBASE", scanDate, status, reason);
                }
            }
            else if (accounts != null && accounts.Length > 0)
            {
                foreach (string acc in accounts)
                {
                    ds.Tables[tblKofaxRecords].Rows.Add(uniqueId++, uidStr, count, null, acc, createDate, updateDate,
                                                             "ONBASE", scanDate, status, reason);
                }
            }
            else
            {
                ds.Tables[tblKofaxRecords].Rows.Add(uniqueId++, uidStr, count, null, null, createDate, updateDate,
                                                         "ONBASE", scanDate, status, reason);
            }

            return ds.Tables[tblKofaxRecords].AsEnumerable().Where(r => r.Field<String>("UIDNumber") == uidStr);
        }

        private EnumerableRowCollection<DataRow> AddOnBaseRecorsdWithManySSNsAndAccounts(int count, 
                                                                                         string[] SSNs = null, 
                                                                                         string[] accounts = null, 
                                                                                         DateTime? docdate = null)
        {
            string spStr3 = "TST" + count.ToString("D8");

            if (SSNs != null && SSNs.Length > 0 && accounts != null && accounts.Length > 0)
            {
                foreach (string ssn in SSNs)
                {
                    foreach (string acc in accounts)
                    {
                        ds.Tables[tblOBRecords].Rows.Add(spStr3, docdate, acc, ssn);
                    }
                }
            }
            else if (SSNs != null && SSNs.Length > 0)
            {
                foreach (string ssn in SSNs)
                {
                    ds.Tables[tblOBRecords].Rows.Add(spStr3, docdate, null, ssn);
                }
            }
            else if (accounts != null && accounts.Length > 0)
            {
                foreach (string acc in accounts)
                {
                    ds.Tables[tblOBRecords].Rows.Add(spStr3, docdate, acc, null);
                }
            }
            else
            {
                ds.Tables[tblOBRecords].Rows.Add(spStr3, docdate, null, null);
            }

            return ds.Tables[tblOBRecords].AsEnumerable().Where(r => r.Field<String>("SpStr3") == spStr3);
        }


        /// <summary>
        /// Returns list of 2 strings received as arguments. If any of strings is null or empty it is not added to list.
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        private List<string> ListFromArgs(string str1, string str2)
        {
            List<string> result = new List<string>();
            if (!String.IsNullOrEmpty(str1)) result.Add(str1);
            if (!String.IsNullOrEmpty(str2)) result.Add(str2);
            return result;
        }

        public DateTime? DateTimeFromString(string dt)
        {
            if (String.IsNullOrEmpty(dt)) return null;
            return DateTime.Parse(dt);
        }

        public bool CallIsScanDateMismatch(string uid)
        {
            EnumerableRowCollection<DataRow> rowsKofax = 
                ds.Tables[tblKofaxRecords].AsEnumerable().Where(r => r.Field<String>("UIDNumber") == uid);
            EnumerableRowCollection<DataRow> rowsOnBase = 
                ds.Tables[tblOBRecords].AsEnumerable().Where(r => r.Field<String>("SpStr3") == uid);

            return reconTest.AreScanDatesMatch(rowsKofax, rowsOnBase);
        }


        #endregion // Helper methods
    }
}
