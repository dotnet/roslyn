// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Roslyn.Test.Utilities
{
    public struct TestInfo
    {
        public decimal Time { get; }

        public TestInfo(decimal time)
        {
            Time = time;
        }

        public static IDictionary<string, TestInfo> GetPassedTestsInfo()
        {
            var result = new Dictionary<string, TestInfo>();
            var filePath = System.Environment.GetEnvironmentVariable("OutputXmlFilePath");
            if (filePath != null)
            {
                if (File.Exists(filePath))
                {
                    var doc = new XmlDocument();
                    doc.Load(filePath);
                    foreach (XmlNode assemblies in doc.SelectNodes("assemblies"))
                    {
                        foreach (XmlNode assembly in assemblies.SelectNodes("assembly"))
                        {
                            foreach (XmlNode collection in assembly.SelectNodes("collection"))
                            {
                                foreach (XmlNode test in assembly.SelectNodes("test"))
                                {
                                    if (test.SelectNodes("failure").Count == 0)
                                    {
                                        if (decimal.TryParse(test.Attributes["time"].InnerText, out var time))
                                        {
                                            result.Add(test.Name, new TestInfo(time));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
