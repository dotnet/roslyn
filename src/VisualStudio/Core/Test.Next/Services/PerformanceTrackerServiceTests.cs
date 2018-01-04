// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Services
{
    public class PerformanceTrackerServiceTests
    {
        [Fact]
        public void TestTooSmallSamples()
        {
            // minimum sample is 100
            var badAnalyzers = GetBadAnalyzers(@"TestFiles\analyzer_input.csv", to: 80);

            Assert.True(badAnalyzers.Length == 0);
        }

        [Fact]
        public void TestTracking()
        {
            var badAnalyzers = GetBadAnalyzers(@"TestFiles\analyzer_input.csv", to: 200);

            Assert.True(badAnalyzers.Contains(@"[Microsoft.CodeAnalysis.CSharp.Diagnostics.RemoveUnnecessaryCast.CSharpRemoveUnnecessaryCastDiagnosticAnalyzer,Microsoft.CodeAnalysis.CSharp.Features,Version=42.42.42.42,Culture=neutral,PublicKeyToken=31bf3856ad364e35] 101.244432561581 5448 2181.63001442628"));
            Assert.True(badAnalyzers.Contains(@"[Microsoft.CodeAnalysis.CSharp.InlineDeclaration.CSharpInlineDeclarationDiagnosticAnalyzer,Microsoft.CodeAnalysis.CSharp.Features,Version=42.42.42.42,Culture=neutral,PublicKeyToken=31bf3856ad364e35] 49.9389715502954 2666.86092715232 929.871330548841"));
            Assert.True(badAnalyzers.Contains(@"[Microsoft.CodeAnalysis.VisualBasic.Diagnostics.RemoveUnnecessaryCast.VisualBasicRemoveUnnecessaryCastDiagnosticAnalyzer,Microsoft.CodeAnalysis.VisualBasic.Features,Version=42.42.42.42,Culture=neutral,PublicKeyToken=31bf3856ad364e35] 42.0967360557792 2327.7619047619 725.464266261805"));
        }

        [Fact]
        public void TestTrackingMaxSample()
        {
            var badAnalyzers = GetBadAnalyzers(@"TestFiles\analyzer_input.csv", to: 300);

            Assert.True(badAnalyzers.Contains(@"[Microsoft.CodeAnalysis.CSharp.Diagnostics.RemoveUnnecessaryCast.CSharpRemoveUnnecessaryCastDiagnosticAnalyzer,Microsoft.CodeAnalysis.CSharp.Features,Version=42.42.42.42,Culture=neutral,PublicKeyToken=31bf3856ad364e35] 85.6039521236341 5845.42358078603 1842.45217226717"));
            Assert.True(badAnalyzers.Contains(@"[Microsoft.CodeAnalysis.CSharp.InlineDeclaration.CSharpInlineDeclarationDiagnosticAnalyzer,Microsoft.CodeAnalysis.CSharp.Features,Version=42.42.42.42,Culture=neutral,PublicKeyToken=31bf3856ad364e35] 42.2014208750466 2879.35371179039 799.261581900397"));
            Assert.True(badAnalyzers.Contains(@"[Microsoft.CodeAnalysis.Editor.VisualBasic.UseAutoProperty.UseAutoPropertyAnalyzer,Microsoft.CodeAnalysis.VisualBasic.EditorFeatures,Version=42.42.42.42,Culture=neutral,PublicKeyToken=31bf3856ad364e35] 45.0918385052674 2906.22535211268 913.728667060397"));
        }

        [Fact]
        public void TestTrackingRolling()
        {
            // data starting to rolling at 300 data points
            var badAnalyzers = GetBadAnalyzers(@"TestFiles\analyzer_input.csv", to: 400);

            Assert.True(badAnalyzers.Contains(@"[Microsoft.CodeAnalysis.CSharp.Diagnostics.RemoveUnnecessaryCast.CSharpRemoveUnnecessaryCastDiagnosticAnalyzer,Microsoft.CodeAnalysis.CSharp.Features,Version=42.42.42.42,Culture=neutral,PublicKeyToken=31bf3856ad364e35] 76.2748443491853 5116.98695652174 1738.19563479479"));
            Assert.True(badAnalyzers.Contains(@"[Microsoft.CodeAnalysis.Editor.VisualBasic.UseAutoProperty.UseAutoPropertyAnalyzer,Microsoft.CodeAnalysis.VisualBasic.EditorFeatures,Version=42.42.42.42,Culture=neutral,PublicKeyToken=31bf3856ad364e35] 43.5700167914005 2925.97857142857 921.213873850298"));
            Assert.True(badAnalyzers.Contains(@"[Microsoft.CodeAnalysis.CSharp.InlineDeclaration.CSharpInlineDeclarationDiagnosticAnalyzer,Microsoft.CodeAnalysis.CSharp.Features,Version=42.42.42.42,Culture=neutral,PublicKeyToken=31bf3856ad364e35] 36.4336594793033 2397.64782608696 743.956680199015"));
        }

        [Fact]
        public void TestBadAnalyzerInfoPII()
        {
            var badAnalyzer1 = new BadAnalyzerInfo(true, "test", 0.1, 0.1, 0.1);
            Assert.True(badAnalyzer1.PIISafeAnalyzerId == badAnalyzer1.AnalyzerId);
            Assert.True(badAnalyzer1.PIISafeAnalyzerId == "test");

            var badAnalyzer2 = new BadAnalyzerInfo(false, "test", 0.1, 0.1, 0.1);
            Assert.True(badAnalyzer2.PIISafeAnalyzerId == badAnalyzer2.Hash);
            Assert.True(badAnalyzer2.PIISafeAnalyzerId == "test".GetHashCode().ToString());
        }

        private string[] GetBadAnalyzers(string testFileName, int to)
        {
            var testFile = GetTestFile(testFileName);

            var (matrix, dataCount) = CreateMatrix(testFile);

            to = Math.Min(to, dataCount);

            var service = new PerformanceTrackerService();

            for (var i = 0; i < to; i++)
            {
                service.AddSnapshot(CreateSnapshots(matrix, i));
            }

            var badAnalyzerInfo = new List<BadAnalyzerInfo>();
            service.GenerateReport(badAnalyzerInfo);

            var badAnalyzers = badAnalyzerInfo.Select(i => Convert(i)).ToArray();
            return badAnalyzers;
        }

        private string Convert(BadAnalyzerInfo info)
        {
            return $"[{info.PIISafeAnalyzerId}] {info.LOF} {info.Mean} {info.Stddev}";
        }

        private IEnumerable<AnalyzerPerformanceInfo> CreateSnapshots(Dictionary<string, double[]> matrix, int index)
        {
            foreach (var kv in matrix)
            {
                var timeSpan = kv.Value[index];
                if (double.IsNaN(timeSpan))
                {
                    continue;
                }

                yield return new AnalyzerPerformanceInfo(kv.Key, true, TimeSpan.FromMilliseconds(timeSpan));
            }
        }

        private (Dictionary<string, double[]>, int) CreateMatrix(string testFile)
        {
            var map = new Dictionary<string, double[]>();

            var lines = testFile.Split('\n');
            var expectedDataCount = GetExpectedDataCount(lines[0]);

            for (var i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim().Length == 0)
                {
                    continue;
                }

                var data = SkipAnalyzerId(lines[i]).Split(',');
                Assert.Equal(data.Length, expectedDataCount);

                var analyzerId = GetAnalyzerId(lines[i]);

                var timeSpans = new double[expectedDataCount];
                for (var j = 0; j < data.Length; j++)
                {
                    double result;
                    if (!double.TryParse(data[j], out result))
                    {
                        // no data for this analyzer for this particular run
                        result = double.NaN;
                    }

                    timeSpans[j] = result;
                }

                map[analyzerId] = timeSpans;
            }

            return (map, expectedDataCount);
        }

        private string GetAnalyzerId(string line)
        {
            return line.Substring(1, line.LastIndexOf('"') - 1);
        }

        private int GetExpectedDataCount(string header)
        {
            var data = header.Split(',');
            return data.Length - 1;
        }

        private string SkipAnalyzerId(string line)
        {
            return line.Substring(line.LastIndexOf('"') + 2);
        }

        private string GetTestFile(string name)
        {
            var assembly = typeof(PerformanceTrackerServiceTests).Assembly;
            var resourceName = GetResourceName(assembly, name);

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException($"Resource '{resourceName}' not found in {assembly.FullName}.");
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static string GetResourceName(Assembly assembly, string name)
        {
            var convert = name.Replace(@"\", ".");

            return assembly.GetManifestResourceNames().Where(n => n.EndsWith(convert, StringComparison.OrdinalIgnoreCase)).First();
        }
    }
}
