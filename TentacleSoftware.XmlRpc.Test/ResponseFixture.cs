using System;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using TentacleSoftware.XmlRpc.Core;
using TentacleSoftware.XmlRpc.Sample;

namespace TentacleSoftware.XmlRpc.Test
{
    [TestFixture]
    public class ResponseFixture
    {
        [Test, TestCaseSource(typeof(MethodCallTestData), nameof(MethodCallTestData.TestCases))]
        public void Respond(string xml)
        {
            XmlRpcRequestHandler handler = new XmlRpcRequestHandler()
                .Add<SampleResponder>();

            XmlRpcResponseHandler responder = new XmlRpcResponseHandler();

            using (MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    responder.RespondWith(
                        handler.RespondTo(input).Result, 
                        output
                    )
                    .Wait();

                    output.Position = 0;

                    using (StreamReader reader = new StreamReader(output))
                    {
                        Console.WriteLine(reader.ReadToEnd());
                    }
                }
            }
        }

        [Test, TestCaseSource(typeof(DateParseTestData), nameof(DateParseTestData.TestCases))]
        public void DateParse(string input, DateTime output)
        {
            const string iso8601 = "yyyyMMdd'T'HH':'mm':'ss";

            DateTime deserialized = DateTime.ParseExact(input, iso8601, CultureInfo.InvariantCulture);

            Assert.AreEqual(deserialized, output);

            string serialized = deserialized.ToString(iso8601, CultureInfo.InvariantCulture);

            Assert.AreEqual(serialized, input);
        }
    }
}
