// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const int TestMinSampleSizeForDocumentAnalysis = 100;
        private const int TestMinSampleSizeForSpanAnalysis = 10;

        [Theory, CombinatorialData]
        public void TestSampleSize(bool forSpanAnalysis)
        {
            var minSampleSize = forSpanAnalysis
                ? TestMinSampleSizeForSpanAnalysis
                : TestMinSampleSizeForDocumentAnalysis;

            // Verify no analyzer infos reported when sampleSize < minSampleSize
            var sampleSize = minSampleSize - 1;
            var analyzerInfos = GetAnalyzerInfos(sampleSize, forSpanAnalysis);
            Assert.Empty(analyzerInfos);

            // Verify analyzer infos reported when sampleSize >= minSampleSize
            sampleSize = minSampleSize + 1;
            analyzerInfos = GetAnalyzerInfos(sampleSize, forSpanAnalysis);
            Assert.NotEmpty(analyzerInfos);
        }

        [Theory, CombinatorialData]
        public void TestNoDuplicateReportGeneration(bool forSpanAnalysis)
        {
            var minSampleSize = forSpanAnalysis
                ? TestMinSampleSizeForSpanAnalysis
                : TestMinSampleSizeForDocumentAnalysis;

            var service = new PerformanceTrackerService(TestMinSampleSizeForDocumentAnalysis, TestMinSampleSizeForSpanAnalysis);

            // Verify analyzer infos reported when sampleSize >= minSampleSize
            var sampleSize = minSampleSize + 1;
            ReadTestFileAndAddSnapshots(service, sampleSize, forSpanAnalysis);
            var analyzerInfos = GenerateReport(service, forSpanAnalysis);
            Assert.NotEmpty(analyzerInfos);

            // Verify no analyzer infos reported when attempting to generate a duplicate
            // report without adding any new samples.
            analyzerInfos = GenerateReport(service, forSpanAnalysis);
            Assert.Empty(analyzerInfos);

            // Verify no analyzer infos reported after adding less than minSampleSize snapshots
            sampleSize = minSampleSize - 1;
            ReadTestFileAndAddSnapshots(service, sampleSize, forSpanAnalysis);
            analyzerInfos = GenerateReport(service, forSpanAnalysis);
            Assert.Empty(analyzerInfos);

            // Verify analyzer infos reported once we the added snapshots exceeds sample size
            sampleSize = 2;
            ReadTestFileAndAddSnapshots(service, sampleSize, forSpanAnalysis);
            analyzerInfos = GenerateReport(service, forSpanAnalysis);
            Assert.NotEmpty(analyzerInfos);
        }

        [Theory, CombinatorialData]
        public void TestTracking(bool forSpanAnalysis)
        {
            var analyzerInfos = GetAnalyzerInfos(to: 200, forSpanAnalysis);

            VerifyAnalyzerInfo(analyzerInfos, "CSharpRemoveUnnecessaryCastDiagnosticAnalyzer",
                mean: forSpanAnalysis ? 89.2416 : 54.48,
                stddev: forSpanAnalysis ? 78.8177 : 21.8163001442628);
            VerifyAnalyzerInfo(analyzerInfos, "CSharpInlineDeclarationDiagnosticAnalyzer",
                mean: forSpanAnalysis ? 48.4284 : 26.6686092715232,
                stddev: forSpanAnalysis ? 38.5010107523771 : 9.2987133054884);
            VerifyAnalyzerInfo(analyzerInfos, "VisualBasicRemoveUnnecessaryCastDiagnosticAnalyzer",
                mean: forSpanAnalysis ? 26.8618181818182 : 23.277619047619,
                stddev: forSpanAnalysis ? 6.62917030049974 : 7.25464266261805);
        }

        [Theory, CombinatorialData]
        public void TestTrackingMaxSample(bool forSpanAnalysis)
        {
            var analyzerInfos = GetAnalyzerInfos(to: 300, forSpanAnalysis);

            VerifyAnalyzerInfo(analyzerInfos, "CSharpRemoveUnnecessaryCastDiagnosticAnalyzer",
                mean: forSpanAnalysis ? 75.3678260869565 : 58.4542358078603,
                stddev: forSpanAnalysis ? 64.7106979026339 : 18.4245217226717);
            VerifyAnalyzerInfo(analyzerInfos, "VisualBasic.UseAutoProperty.UseAutoPropertyAnalyzer",
                mean: forSpanAnalysis ? 0.375 : 29.0622535211268,
                stddev: forSpanAnalysis ? 0.144592142784807 : 9.13728667060397);
            VerifyAnalyzerInfo(analyzerInfos, "CSharpInlineDeclarationDiagnosticAnalyzer",
                mean: forSpanAnalysis ? 37.6895652173913 : 28.7935371179039,
                stddev: forSpanAnalysis ? 29.5179969292277 : 7.99261581900397);
        }

        [Theory, CombinatorialData]
        public void TestTrackingRolling(bool forSpanAnalysis)
        {
            // data starting to rolling at 300 data points
            var analyzerInfos = GetAnalyzerInfos(to: 400, forSpanAnalysis);

            VerifyAnalyzerInfo(analyzerInfos, "CSharpRemoveUnnecessaryCastDiagnosticAnalyzer",
                mean: forSpanAnalysis ? 0.24304347826087 : 51.1698695652174,
                stddev: forSpanAnalysis ? 0.1511123654363 : 17.3819563479479);
            VerifyAnalyzerInfo(analyzerInfos, "VisualBasic.UseAutoProperty.UseAutoPropertyAnalyzer",
                mean: forSpanAnalysis ? 44.5428571428571 : 29.2597857142857,
                stddev: forSpanAnalysis ? 34.6949934844539 : 9.21213873850298);
            VerifyAnalyzerInfo(analyzerInfos, "InlineDeclaration.CSharpInlineDeclarationDiagnosticAnalyzer",
                mean: forSpanAnalysis ? 0.842608695652174 : 23.9764782608696,
                stddev: forSpanAnalysis ? 0.312682894617701 : 7.43956680199015);
        }

        [Fact]
        public void TestBadAnalyzerInfoPII()
        {
            var analyzerInfo1 = new AnalyzerInfoForPerformanceReporting(true, "test", 0.1, 0.1);
            Assert.True(analyzerInfo1.PIISafeAnalyzerId == analyzerInfo1.AnalyzerId);
            Assert.True(analyzerInfo1.PIISafeAnalyzerId == "test");

            var analyzerInfo2 = new AnalyzerInfoForPerformanceReporting(false, "test", 0.1, 0.1);
            Assert.True(analyzerInfo2.PIISafeAnalyzerId == analyzerInfo2.AnalyzerIdHash);
            Assert.True(analyzerInfo2.PIISafeAnalyzerId == "test".GetHashCode().ToString());
        }

        private static void VerifyAnalyzerInfo(List<AnalyzerInfoForPerformanceReporting> analyzerInfos, string analyzerName, double mean, double stddev)
        {
            var analyzerInfo = analyzerInfos.Single(i => i.AnalyzerId.Contains(analyzerName));
            Assert.True(analyzerInfo.PIISafeAnalyzerId.IndexOf(analyzerName, StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.Equal(mean, analyzerInfo.Average, precision: 4);
            Assert.Equal(stddev, analyzerInfo.AdjustedStandardDeviation, precision: 4);
        }

        private static List<AnalyzerInfoForPerformanceReporting> GetAnalyzerInfos(int to, bool forSpanAnalysis)
        {
            var service = new PerformanceTrackerService(TestMinSampleSizeForDocumentAnalysis, TestMinSampleSizeForSpanAnalysis);
            ReadTestFileAndAddSnapshots(service, to, forSpanAnalysis);
            return GenerateReport(service, forSpanAnalysis);
        }

        private static void ReadTestFileAndAddSnapshots(PerformanceTrackerService service, int to, bool forSpanAnalysis)
        {
            var testFile = ReadTestFile(@"TestFiles\analyzer_input.csv");

            var (matrix, dataCount) = CreateMatrix(testFile);

            to = Math.Min(to, dataCount);

            for (var i = 0; i < to; i++)
            {
                service.AddSnapshot(CreateSnapshots(matrix, i), unitCount: 100, forSpanAnalysis);
            }
        }

        private static List<AnalyzerInfoForPerformanceReporting> GenerateReport(PerformanceTrackerService service, bool forSpanAnalysis)
        {
            var analyzerInfos = new List<AnalyzerInfoForPerformanceReporting>();
            service.GenerateReport(analyzerInfos, forSpanAnalysis);

            return analyzerInfos;
        }

        private static IEnumerable<AnalyzerPerformanceInfo> CreateSnapshots(Dictionary<string, double[]> matrix, int index)
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

        private static (Dictionary<string, double[]> matrix, int dataCount) CreateMatrix(string testFile)
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
                    if (!double.TryParse(data[j], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
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

        private static string GetAnalyzerId(string line)
        {
            return line[1..line.LastIndexOf('"')];
        }

        private static int GetExpectedDataCount(string header)
        {
            var data = header.Split(',');
            return data.Length - 1;
        }

        private static string SkipAnalyzerId(string line)
        {
            return line[(line.LastIndexOf('"') + 2)..];
        }

        private static string ReadTestFile(string name)
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
