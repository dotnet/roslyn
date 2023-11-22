// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal static class ReportAnalyzerUtil
    {
        public static void Report(
            TextWriter consoleOutput,
            AnalyzerDriver? analyzerDriver,
            GeneratorDriverTimingInfo? driverTimingInfo,
            CultureInfo culture,
            bool isConcurrentBuild)
        {
            if (isConcurrentBuild && (analyzerDriver is { } || driverTimingInfo is { }))
            {
                consoleOutput.WriteLine(CodeAnalysisResources.MultithreadedAnalyzerExecutionNote);
                consoleOutput.WriteLine();
            }

            if (analyzerDriver is { })
            {
                ReportAnalyzerExecutionTime(consoleOutput, analyzerDriver, culture);
            }

            if (driverTimingInfo is { } info)
            {
                ReportGeneratorExecutionTime(consoleOutput, info, culture);
            }
        }

        public static string GetFormattedAnalyzerExecutionTime(double executionTime, CultureInfo culture) =>
            executionTime < 0.001 ?
                string.Format(culture, "{0,8:<0.000}", 0.001) :
                string.Format(culture, "{0,8:##0.000}", executionTime);

        public static string GetFormattedAnalyzerExecutionPercentage(int percentage, CultureInfo culture) =>
            string.Format("{0,5}", percentage < 1 ? "<1" : percentage.ToString(culture));

        private static string GetColumnHeader(string kind)
        {
            var time = string.Format("{0,8}", CodeAnalysisResources.AnalyzerExecutionTimeColumnHeader);
            var percent = string.Format("{0,5}", "%");
            return time + percent + "   " + kind;
        }

        private static string GetColumnEntry(double totalSeconds, int percentage, string? name, CultureInfo culture)
        {
            var time = GetFormattedAnalyzerExecutionTime(totalSeconds, culture);
            var percent = GetFormattedAnalyzerExecutionPercentage(percentage, culture);

            return time + percent + "   " + name;
        }

        private static void ReportAnalyzerExecutionTime(TextWriter consoleOutput, AnalyzerDriver analyzerDriver, CultureInfo culture)
        {
            Debug.Assert(analyzerDriver.AnalyzerExecutionTimes != null);
            if (analyzerDriver.AnalyzerExecutionTimes.IsEmpty)
            {
                return;
            }

            var totalAnalyzerExecutionTime = analyzerDriver.AnalyzerExecutionTimes.Sum(kvp => kvp.Value.TotalSeconds);
            consoleOutput.WriteLine(string.Format(CodeAnalysisResources.AnalyzerTotalExecutionTime, totalAnalyzerExecutionTime.ToString("##0.000", culture)));
            consoleOutput.WriteLine();

            // Table header
            consoleOutput.WriteLine(GetColumnHeader(CodeAnalysisResources.AnalyzerNameColumnHeader));

            // Table rows grouped by assembly.
            var analyzersByAssembly = analyzerDriver.AnalyzerExecutionTimes
                .GroupBy(kvp => kvp.Key.GetType().Assembly)
                .OrderByDescending(kvp => kvp.Sum(entry => entry.Value.Ticks));
            foreach (var analyzerGroup in analyzersByAssembly)
            {
                var executionTime = analyzerGroup.Sum(kvp => kvp.Value.TotalSeconds);
                var percentage = (int)(executionTime * 100 / totalAnalyzerExecutionTime);
                consoleOutput.WriteLine(GetColumnEntry(executionTime, percentage, analyzerGroup.Key.FullName, culture));

                // Rows for each diagnostic analyzer in the assembly.
                foreach (var kvp in analyzerGroup.OrderByDescending(kvp => kvp.Value))
                {
                    executionTime = kvp.Value.TotalSeconds;
                    percentage = (int)(executionTime * 100 / totalAnalyzerExecutionTime);

                    var analyzerIds = string.Join(", ", kvp.Key.SupportedDiagnostics.Select(d => d.Id).Distinct().OrderBy(id => id));
                    var analyzerNameColumn = $"   {kvp.Key} ({analyzerIds})";
                    consoleOutput.WriteLine(GetColumnEntry(executionTime, percentage, analyzerNameColumn, culture));
                }

                consoleOutput.WriteLine();
            }
        }

        private static void ReportGeneratorExecutionTime(TextWriter consoleOutput, GeneratorDriverTimingInfo driverTimingInfo, CultureInfo culture)
        {
            if (driverTimingInfo.GeneratorTimes.IsEmpty)
            {
                return;
            }

            var totalTime = driverTimingInfo.ElapsedTime.TotalSeconds;
            consoleOutput.WriteLine(string.Format(CodeAnalysisResources.GeneratorTotalExecutionTime, totalTime.ToString("##0.000", culture)));
            consoleOutput.WriteLine();

            // Table header
            consoleOutput.WriteLine(GetColumnHeader(CodeAnalysisResources.GeneratorNameColumnHeader));

            // Table rows grouped by assembly.
            var generatorsByAssembly = driverTimingInfo.GeneratorTimes
                .GroupBy(t => t.Generator.GetGeneratorType().Assembly)
                .OrderByDescending(kvp => kvp.Sum(entry => entry.ElapsedTime.Ticks));

            foreach (var generatorGroup in generatorsByAssembly)
            {
                var executionTime = generatorGroup.Sum(x => x.ElapsedTime.TotalSeconds);
                var percentage = (int)(executionTime * 100 / totalTime);
                consoleOutput.WriteLine(GetColumnEntry(executionTime, percentage, generatorGroup.Key.FullName, culture));

                foreach (var timingInfo in generatorGroup.OrderByDescending(x => x.ElapsedTime))
                {
                    executionTime = timingInfo.ElapsedTime.TotalSeconds;
                    percentage = (int)(executionTime * 100 / totalTime);
                    consoleOutput.WriteLine(GetColumnEntry(executionTime, percentage, "   " + timingInfo.Generator.GetGeneratorType().FullName, culture));
                }
            }
        }
    }
}
