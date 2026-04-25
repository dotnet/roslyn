// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Microsoft.AspNetCore.BenchmarkDotNet.Runner;

partial class Program
{
    private static int Main(string[] args)
    {
        var config = GetConfig();

        var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args, config);

        foreach (var summary in summaries)
        {
            if (summary.HasCriticalValidationErrors)
            {
                return Fail(summary, nameof(summary.HasCriticalValidationErrors));
            }

            foreach (var report in summary.Reports)
            {
                if (!report.BuildResult.IsGenerateSuccess)
                {
                    return Fail(report, nameof(report.BuildResult.IsGenerateSuccess));
                }

                if (!report.BuildResult.IsBuildSuccess)
                {
                    return Fail(report, nameof(report.BuildResult.IsBuildSuccess));
                }

                if (!report.AllMeasurements.Any())
                {
                    return Fail(report, nameof(report.AllMeasurements));
                }
            }
        }

        return 0;
    }

    private static int Fail(object o, string message)
    {
        Console.Error.WriteLine("'{0}' failed, reason: '{1}'", o, message);
        return 1;
    }

    private static IConfig GetConfig()
    {
        if (Debugger.IsAttached)
        {
            return new DebugInProcessConfig();
        }

        return ManualConfig.CreateEmpty()
            .WithBuildTimeout(TimeSpan.FromMinutes(15)) // for slow machines
            .AddLogger(ConsoleLogger.Default) // log output to console
            .AddValidator(DefaultConfig.Instance.GetValidators().ToArray()) // copy default validators
            .AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray()) // copy default analysers
            .AddExporter(MarkdownExporter.GitHub) // export to GitHub markdown
            .AddColumnProvider(DefaultColumnProviders.Instance) // display default columns (method name, args etc)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddExporter(JsonExporter.Full)
            .AddColumn(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max)
            .WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(36)) // the default is 20 and trims too aggressively some benchmark results
            .AddDiagnoser(CreateDisassembler());
    }

    private static DisassemblyDiagnoser CreateDisassembler()
        => new(new DisassemblyDiagnoserConfig(
            maxDepth: 1, // TODO: is depth == 1 enough?
            syntax: DisassemblySyntax.Masm, // TODO: enable diffable format
            printSource: false, // we are not interested in getting C#
            printInstructionAddresses: false, // would make the diffing hard, however could be useful to determine alignment
            exportGithubMarkdown: false,
            exportHtml: false,
            exportCombinedDisassemblyReport: false,
            exportDiff: false));
}
