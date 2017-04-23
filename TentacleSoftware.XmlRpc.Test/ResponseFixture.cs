﻿using System;
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

            using (MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    handler.Respond(input, output).Wait();

                    output.Position = 0;

                    using (StreamReader reader = new StreamReader(output))
                    {
                        Console.WriteLine(reader.ReadToEnd());
                    }
                }
            }
        }
    }
}