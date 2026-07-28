// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using Microsoft.Diagnostics.Tracing.Session;

Job baseJob = Job.Default;
#if DEBUG
baseJob = baseJob
        .WithIterationCount(1)
        .RunOncePerIteration()
        .WithToolchain(new BenchmarkDotNet.Toolchains.InProcess.Emit.InProcessEmitToolchain(TimeSpan.FromHours(1.0), logOutput: true));
#endif

var config = ManualConfig.CreateMinimumViable()
            .AddJob(baseJob.WithCustomBuildConfiguration("Release").WithId("Current"))
            .AddJob(baseJob.WithCustomBuildConfiguration("Release_Nuget").WithId("Baseline").WithBaseline(true))
            .StopOnFirstError(true)
            .AddExporter(CsvExporter.Default)
            .AddDiagnoser(MemoryDiagnoser.Default);

if (TraceEventSession.IsElevated() == true)
{
    config = config.AddDiagnoser(new EtwProfiler());
}

var results = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

var reports =
    from summary in results
    from report in summary.Reports
    where !summary.IsBaseline(report.BenchmarkCase)
    let baselineCase = summary.GetBaseline(summary.GetLogicalGroupKey(report.BenchmarkCase)!)
    let baseline = summary.Reports.Single(r => r.BenchmarkCase == baselineCase)
    select (report, baseline);

int exitCode = 0;
foreach ((var benchmark, var baseline) in reports)
{
    // Note: there are actual statistical tests we could do here, but this should suffice.
    // We can invest more if we see consistent false positives

    var ratio = benchmark.ResultStatistics!.Mean / baseline.ResultStatistics!.Mean;

    if (ratio > 1.1)
    {
        Console.WriteLine();
        Console.WriteLine("Benchmark may have regressed!");
        Console.WriteLine(benchmark.BenchmarkCase.DisplayInfo);
        exitCode--;
    }
}

return exitCode;
