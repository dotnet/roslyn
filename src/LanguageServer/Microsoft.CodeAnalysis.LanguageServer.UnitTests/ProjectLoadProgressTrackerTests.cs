// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ProjectLoadProgressTrackerTests
{
    private sealed class TestProgressReporter : IProgress<WorkDoneProgress>
    {
        private readonly object _gate = new();
        private readonly List<WorkDoneProgressReport> _reports = [];

        public void Report(WorkDoneProgress value)
        {
            lock (_gate)
            {
                if (value is WorkDoneProgressReport report)
                    _reports.Add(report);
            }
        }

        public IReadOnlyList<WorkDoneProgressReport> GetReports()
        {
            lock (_gate)
            {
                return [.. _reports];
            }
        }
    }

    [Fact]
    public void SingleProject_ReportsHundredPercent()
    {
        var reporter = new TestProgressReporter();
        var tracker = new LanguageServerProjectLoader.ProjectLoadProgressTracker(reporter, totalProjects: 1);

        tracker.OnProjectProcessed();

        var reports = reporter.GetReports();
        var report = Assert.Single(reports);
        Assert.Equal(100, report.Percentage);
    }

    [Fact]
    public void MultipleProjects_PercentagesAreMonotonicallyIncreasing()
    {
        var reporter = new TestProgressReporter();
        var totalProjects = 10;
        var tracker = new LanguageServerProjectLoader.ProjectLoadProgressTracker(reporter, totalProjects);

        for (var i = 0; i < totalProjects; i++)
        {
            tracker.OnProjectProcessed();
        }

        var reports = reporter.GetReports();
        Assert.NotEmpty(reports);

        var previousPercentage = -1;
        foreach (var report in reports)
        {
            Assert.True(report.Percentage > previousPercentage,
                $"Expected percentage {report.Percentage} to be greater than previous {previousPercentage}");
            previousPercentage = report.Percentage!.Value;
        }
    }

    [Fact]
    public void MultipleProjects_FinalPercentageIsHundred()
    {
        var reporter = new TestProgressReporter();
        var totalProjects = 7;
        var tracker = new LanguageServerProjectLoader.ProjectLoadProgressTracker(reporter, totalProjects);

        for (var i = 0; i < totalProjects; i++)
        {
            tracker.OnProjectProcessed();
        }

        var reports = reporter.GetReports();
        Assert.Equal(100, reports[^1].Percentage);
    }

    [Fact]
    public void LargeProjectCount_SuppressesDuplicatePercentages()
    {
        var reporter = new TestProgressReporter();
        var totalProjects = 200;
        var tracker = new LanguageServerProjectLoader.ProjectLoadProgressTracker(reporter, totalProjects);

        for (var i = 0; i < totalProjects; i++)
        {
            tracker.OnProjectProcessed();
        }

        var reports = reporter.GetReports();

        Assert.True(reports.Count <= 101,
            $"Expected at most 101 reports but got {reports.Count}");

        var percentages = reports.Select(r => r.Percentage!.Value).ToList();
        Assert.Equal(percentages.Count, percentages.Distinct().Count());
        Assert.Equal(100, percentages[^1]);
    }

    [Fact]
    public void AllPercentagesWithinValidRange()
    {
        var reporter = new TestProgressReporter();
        var totalProjects = 50;
        var tracker = new LanguageServerProjectLoader.ProjectLoadProgressTracker(reporter, totalProjects);

        for (var i = 0; i < totalProjects; i++)
        {
            tracker.OnProjectProcessed();
        }

        var reports = reporter.GetReports();
        Assert.All(reports, report =>
        {
            Assert.NotNull(report.Percentage);
            Assert.InRange(report.Percentage!.Value, 0, 100);
        });
    }

    [Fact]
    public void TwoProjects_ReportsFiftyAndHundred()
    {
        var reporter = new TestProgressReporter();
        var tracker = new LanguageServerProjectLoader.ProjectLoadProgressTracker(reporter, totalProjects: 2);

        tracker.OnProjectProcessed();
        tracker.OnProjectProcessed();

        var reports = reporter.GetReports();
        Assert.Equal(2, reports.Count);
        Assert.Equal(50, reports[0].Percentage);
        Assert.Equal(100, reports[1].Percentage);
    }

    [Fact]
    public void ThreeProjects_ReportsExpectedPercentages()
    {
        var reporter = new TestProgressReporter();
        var tracker = new LanguageServerProjectLoader.ProjectLoadProgressTracker(reporter, totalProjects: 3);

        tracker.OnProjectProcessed();
        tracker.OnProjectProcessed();
        tracker.OnProjectProcessed();

        var reports = reporter.GetReports();
        Assert.Equal(3, reports.Count);
        Assert.Equal(33, reports[0].Percentage);
        Assert.Equal(66, reports[1].Percentage);
        Assert.Equal(100, reports[2].Percentage);
    }

    [Fact]
    public async Task ConcurrentProcessing_AllPercentagesWithinRangeAndReachHundred()
    {
        var reporter = new TestProgressReporter();
        var totalProjects = 100;
        var tracker = new LanguageServerProjectLoader.ProjectLoadProgressTracker(reporter, totalProjects);

        var tasks = Enumerable.Range(0, totalProjects).Select(_ =>
            Task.Run(() => tracker.OnProjectProcessed()));
        await Task.WhenAll(tasks);

        var reports = reporter.GetReports();
        Assert.NotEmpty(reports);

        Assert.All(reports, report =>
        {
            Assert.NotNull(report.Percentage);
            Assert.InRange(report.Percentage!.Value, 0, 100);
        });

        Assert.Equal(100, reports.Max(r => r.Percentage!.Value));
    }
}
