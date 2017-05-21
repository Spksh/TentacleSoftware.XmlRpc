using System;
using System.Collections;
using NUnit.Framework;

namespace TentacleSoftware.XmlRpc.Test
{
    public static class DateParseTestData
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData("20170531T05:06:00", new DateTime(2017, 05, 31, 5, 6, 0, DateTimeKind.Utc)) { TestName = "20170531T05:06:00" };
            }
        }
    }
}
