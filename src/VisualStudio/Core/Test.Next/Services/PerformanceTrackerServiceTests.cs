// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Services
{
    public class PerformanceTrackerServiceTests
    {
        [Fact]
        public void TestTooFewSamples()
        {
            // minimum sample is 100
            var badAnalyzers = GetBadAnalyzers(@"TestFiles\analyzer_input.csv", to: 80);

            Assert.Empty(badAnalyzers);
        }

        [Fact]
        public void TestTracking()
        {
            var badAnalyzers = GetBadAnalyzers(@"TestFiles\analyzer_input.csv", to: 200);

            VerifyBadAnalyzer(badAnalyzers[0], "CSharpRemoveUnnecessaryCastDiagnosticAnalyzer", 101.244432561581, 54.48, 21.8163001442628);
            VerifyBadAnalyzer(badAnalyzers[1], "CSharpInlineDeclarationDiagnosticAnalyzer", 49.9389715502954, 26.6686092715232, 9.2987133054884);
            VerifyBadAnalyzer(badAnalyzers[2], "VisualBasicRemoveUnnecessaryCastDiagnosticAnalyzer", 42.0967360557792, 23.277619047619, 7.25464266261805);
        }

        [Fact]
        public void TestTrackingMaxSample()
        {
            var badAnalyzers = GetBadAnalyzers(@"TestFiles\analyzer_input.csv", to: 300);

            VerifyBadAnalyzer(badAnalyzers[0], "CSharpRemoveUnnecessaryCastDiagnosticAnalyzer", 85.6039521236341, 58.4542358078603, 18.4245217226717);
            VerifyBadAnalyzer(badAnalyzers[1], "VisualBasic.UseAutoProperty.UseAutoPropertyAnalyzer", 45.0918385052674, 29.0622535211268, 9.13728667060397);
            VerifyBadAnalyzer(badAnalyzers[2], "CSharpInlineDeclarationDiagnosticAnalyzer", 42.2014208750466, 28.7935371179039, 7.99261581900397);
        }

        [Fact]
        public void TestTrackingRolling()
        {
            // data starting to rolling at 300 data points
            var badAnalyzers = GetBadAnalyzers(@"TestFiles\analyzer_input.csv", to: 400);

            VerifyBadAnalyzer(badAnalyzers[0], "CSharpRemoveUnnecessaryCastDiagnosticAnalyzer", 76.2748443491852, 51.1698695652174, 17.3819563479479);
            VerifyBadAnalyzer(badAnalyzers[1], "VisualBasic.UseAutoProperty.UseAutoPropertyAnalyzer", 43.5700167914005, 29.2597857142857, 9.21213873850298);
            VerifyBadAnalyzer(badAnalyzers[2], "InlineDeclaration.CSharpInlineDeclarationDiagnosticAnalyzer", 36.4336594793033, 23.9764782608696, 7.43956680199015);
        }

        [Fact]
        public void TestBadAnalyzerInfoPII()
        {
            var badAnalyzer1 = new ExpensiveAnalyzerInfo(true, "test", 0.1, 0.1, 0.1);
            Assert.True(badAnalyzer1.PIISafeAnalyzerId == badAnalyzer1.AnalyzerId);
            Assert.True(badAnalyzer1.PIISafeAnalyzerId == "test");

            var badAnalyzer2 = new ExpensiveAnalyzerInfo(false, "test", 0.1, 0.1, 0.1);
            Assert.True(badAnalyzer2.PIISafeAnalyzerId == badAnalyzer2.AnalyzerIdHash);
            Assert.True(badAnalyzer2.PIISafeAnalyzerId == "test".GetHashCode().ToString());
        }

        private void VerifyBadAnalyzer(ExpensiveAnalyzerInfo analyzer, string analyzerId, double lof, double mean, double stddev)
        {
            Assert.True(analyzer.PIISafeAnalyzerId.IndexOf(analyzerId, StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.Equal(lof, analyzer.LocalOutlierFactor, precision: 4);
            Assert.Equal(mean, analyzer.Average, precision: 4);
            Assert.Equal(stddev, analyzer.AdjustedStandardDeviation, precision: 4);
        }

        private List<ExpensiveAnalyzerInfo> GetBadAnalyzers(string testFileName, int to)
        {
            var testFile = ReadTestFile(testFileName);

            var (matrix, dataCount) = CreateMatrix(testFile);

            to = Math.Min(to, dataCount);

            var service = new PerformanceTrackerService(minLOFValue: 0, averageThreshold: 0, stddevThreshold: 0);

            for (var i = 0; i < to; i++)
            {
                service.AddSnapshot(CreateSnapshots(matrix, i), unitCount: 100);
            }

            var badAnalyzerInfo = new List<ExpensiveAnalyzerInfo>();
            service.GenerateReport(badAnalyzerInfo);

            return badAnalyzerInfo;
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

        private (Dictionary<string, double[]> matrix, int dataCount) CreateMatrix(string testFile)
        {
            var matrix = new Dictionary<string, double[]>();

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
                    if (!double.TryParse(data[j], NumberStyles.Float | NumberStyles.AllowThousands, EnsureEnglishUICulture.PreferredOrNull, out result))
                    {
                        // no data for this analyzer for this particular run
                        result = double.NaN;
                    }

                    timeSpans[j] = result;
                }

                matrix[analyzerId] = timeSpans;
            }

            return (matrix, expectedDataCount);
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

        private string ReadTestFile(string name)
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
