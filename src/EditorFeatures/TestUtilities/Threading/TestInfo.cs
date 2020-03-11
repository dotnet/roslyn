﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Roslyn.Test.Utilities
{
    public readonly struct TestInfo
    {
        public decimal Time { get; }

        public TestInfo(decimal time)
        {
            Time = time;
        }

        public static ImmutableDictionary<string, TestInfo> GetPassedTestsInfo()
        {
            var result = ImmutableDictionary.CreateBuilder<string, TestInfo>();
            var filePath = System.Environment.GetEnvironmentVariable("OutputXmlFilePath");
            if (filePath != null)
            {
                if (File.Exists(filePath))
                {
                    var doc = XDocument.Load(filePath);
                    foreach (var test in doc.XPathSelectElements("/assemblies/assembly/collection/test[@result='Pass']"))
                    {
                        if (decimal.TryParse(test.Attribute("time").Value, out var time))
                        {
                            result.Add(test.Attribute("name").Value, new TestInfo(time));
                        }
                    }
                }
            }

            return result.ToImmutable();
        }
    }
}
