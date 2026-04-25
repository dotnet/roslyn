// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

internal class Program
{
    internal static int Main(string[] args)
    {
        var argList = new List<string>(args);

        bool getDiffableDisasm;

        try
        {
            ParseAndRemoveBooleanParameter(argList, "--disasm-diff", out getDiffableDisasm);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine("ArgumentException: {0}", ex.Message);
            return 1;
        }

        return BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, GetConfig(getDiffableDisasm))
            .ToExitCode();
    }

    private static IConfig GetConfig(bool getDiffableDisasm)
    {
        if (Debugger.IsAttached)
        {
            return new DebugInProcessConfig();
        }

        var config = ManualConfig.CreateEmpty()
            .WithBuildTimeout(TimeSpan.FromMinutes(15)) // for slow machines
            .AddLogger(ConsoleLogger.Default) // log output to console
            .AddValidator(DefaultConfig.Instance.GetValidators().ToArray()) // copy default validators
            .AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray()) // copy default analysers
            .AddExporter(MarkdownExporter.GitHub) // export to GitHub markdown
            .AddColumnProvider(DefaultColumnProviders.Instance) // display default columns (method name, args etc)
            .AddJob(GetJob(CsProjCoreToolchain.NetCoreApp80)) // tell BDN that these are our default settings
            .AddJob(GetJob(CsProjClassicNetToolchain.Net472))
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddExporter(JsonExporter.Full)
            .AddColumn(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max)
            .WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(36)); // the default is 20 and trims too aggressively some benchmark results

        if (getDiffableDisasm)
        {
            config = config.AddDiagnoser(CreateDisassembler());
        }

        return config;
    }

    private static Job GetJob(IToolchain toolchain)
        => Job.Default
            .WithToolchain(toolchain)
            .DontEnforcePowerPlan(); // make sure BDN does not try to enforce High Performance power plan on Windows

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

    private static void ParseAndRemoveBooleanParameter(List<string> argsList, string parameter, out bool parameterValue)
    {
        var parameterIndex = argsList.IndexOf(parameter);

        if (parameterIndex != -1)
        {
            argsList.RemoveAt(parameterIndex);

            parameterValue = true;
        }
        else
        {
            parameterValue = false;
        }
    }
}
