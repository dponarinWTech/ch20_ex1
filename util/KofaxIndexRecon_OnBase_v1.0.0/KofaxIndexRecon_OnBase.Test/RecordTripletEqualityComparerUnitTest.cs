using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace KofaxIndexRecon_OnBase.Test
{
    [TestFixture]
    class RecordTripletEqualityComparerUnitTest
    {
        private RecordTripletEqualityComparer comparer;
        private RecordTriplet one;
        private RecordTriplet two;

        [OneTimeSetUp]
        public void SetupOnce()
        {
            comparer = new RecordTripletEqualityComparer();
        }

        [SetUp]
        public void Setup()
        {
            one = null;
            two = null;
        }

        [TestCase("111111111", "111111111", "121212", "121212", "05/01/2019", "05/01/2019", true)]
        [TestCase("111111111", "111111111", "121212", "121212", "05/01/2019 13:45:21", "05/01/2019 02:02:20", true)]
        [TestCase(null, null, null, null, "05/01/2019", "05/01/2019", false)]
        [TestCase(null, null, "121212", "121212", "05/01/2019", "05/01/2019", true)]
        [TestCase("111111111", "111111111", "121212", "121212", "05/01/2019", "12/12/2019", false)]
        [TestCase("111111111", "222222222", "121212", "121212", "05/01/2019", "05/01/2019", false)]
        [TestCase("111111111", null, "121212", "121212", "05/01/2019", "05/01/2019", true)]
        [TestCase("111111111", "111111111", "121212", null, "05/01/2019", "05/01/2019", true)]
        [TestCase("111111111", null, "121212", null, "05/01/2019", "05/01/2019", false)]
        [TestCase(null, "111111111", "121212", "121212", "05/01/2019", "05/01/2019", true)]
        [TestCase("111111111", "111111111", null, "121212", "05/01/2019", "05/01/2019", true)]
        public void EqualsTest(string ssn1, string ssn2, string acc1, string acc2, string dt1, string dt2, bool expectedValue)
        {
            DateTime? date1, date2;
            if (String.IsNullOrEmpty(dt1)) { date1 = null; }
            else { date1 = DateTime.Parse(dt1); }
            if (String.IsNullOrEmpty(dt2)) { date2 = null; }
            else { date2 = DateTime.Parse(dt2); }

            one = new RecordTriplet(ssn1, acc1, date1);
            two = new RecordTriplet(ssn2, acc2, date2);
            Assert.AreEqual(expectedValue, comparer.Equals(one, two));
        }

        [Test]
        public void EqualsTestNullArgs()
        {
            DateTime now = DateTime.Now;
            one = new RecordTriplet("111111111", "121212", now);
            two = new RecordTriplet("111111111", "121212", now);
            NUnit.Framework.Assert.AreEqual(one.CreateDate, $"{now.Month:D2}-{now.Day:D2}-{now.Year}");

            NUnit.Framework.Assert.IsTrue(comparer.Equals(null, null));
            NUnit.Framework.Assert.IsFalse(comparer.Equals(one, null));
            NUnit.Framework.Assert.IsFalse(comparer.Equals(null, two));
        }

    }
}
