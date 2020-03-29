using System;
using NUnit.Framework;
using System.IO;
using Assert = NUnit.Framework.Assert;

namespace KofaxIndexRecon_OnBase.Test
{
    [TestFixture]
    class OnBaseCommunicatorUnitTests
    {
        private OnBaseCommunicator obCommunicator;

        // file where mock of email sender will write email content - it will appear in Solution root folder
        private string emailFile = "email.txt";  

        [OneTimeSetUp]
        public void SetupOnce()
        {
            obCommunicator = new OnBaseCommunicator(null);  // we will not use logger
        }

        public void Setup()
        {
            if (File.Exists(emailFile))
            {
                File.Delete(emailFile);
            }
        }

        [Test]
        public void GetLastSuccessfulRunDateTest()
        {
            DateTime inputDate = DateTime.Now.Date.AddDays(-2);
            string input = inputDate.ToString("MM-dd-yyyy");

            DateTime result = obCommunicator.GetLastSuccessfulRunDate(input, MailSenderMock);
            Assert.AreEqual(inputDate, result, $"Wrong date when input date is 2 days back");

            // when input is invalid it should return date 7 days before current
            DateTime weekBack = DateTime.Now.Date.AddDays(-7);
            result = obCommunicator.GetLastSuccessfulRunDate("zzz", MailSenderMock);
            Assert.AreEqual(weekBack, result, $"Wrong result when input string is invalid");

            // when input date is in the future it should return date 7 days before current
            DateTime futureDate = DateTime.Now.Date.AddDays(3);
            result = obCommunicator.GetLastSuccessfulRunDate(futureDate.ToString("MM-dd-yyyy"), MailSenderMock);
            Assert.AreEqual(weekBack, result, $"Wrong result when input date is in future");
        }

        // When inputDate is more than 10 days back it will return the input date AND send warning email 
        [Test]
        public void GetLastSuccessfulRunDateTest_WithEmail()
        {
            DateTime inputDate = DateTime.Now.Date.AddDays(-12);
            string input = inputDate.ToString("MM-dd-yyyy");
            DateTime result = obCommunicator.GetLastSuccessfulRunDate(input, MailSenderMock);

            Assert.AreEqual(inputDate, result, "Wrong date");
            string emailText = File.ReadAllText(emailFile);
            Assert.IsTrue(emailText.Contains("Date of last successful run read from config file") && 
                          emailText.Contains("is more than 10 days back"));
        }


        #region     // Helper method
        public void MailSenderMock(string msg)
        {
            File.WriteAllText(emailFile, msg);
        }
        #endregion  // Helper method
    }
}
