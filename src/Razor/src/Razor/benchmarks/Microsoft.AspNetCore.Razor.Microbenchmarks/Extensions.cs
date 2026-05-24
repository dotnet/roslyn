// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

internal static class SummaryExtensions
{
    public static int ToExitCode(this IEnumerable<Summary> summaries)
    {
        // an empty summary means that initial filtering and validation did not allow
        // any benchmarks to run.
        if (!summaries.Any())
        {
            return 1;
        }

        // If anything has failed, it's an error.
        if (summaries.Any(summary => summary.HasAnyErrors()))
        {
            return 1;
        }

        return 0;
    }

    public static bool HasAnyErrors(this Summary summary)
        => summary.HasCriticalValidationErrors ||
           summary.Reports.Any(report => report.HasAnyErrors());

    public static bool HasAnyErrors(this BenchmarkReport report)
        => !report.BuildResult.IsBuildSuccess ||
           !report.AllMeasurements.Any();
}
