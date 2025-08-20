// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log;

internal readonly struct StatisticResult(int max, int min, double mean, int range, int? mode, int count)
{
    public static StatisticResult FromList(List<int> values)
    {
        if (values.Count == 0)
        {
            return default;
        }

        var max = int.MinValue;
        var min = int.MaxValue;

        var total = 0;
        for (var i = 0; i < values.Count; i++)
        {
            var current = values[i];
            max = max < current ? current : max;
            min = min > current ? current : min;

            total += current;
        }

        var mean = (double)total / values.Count;

        var range = max - min;
        var mode = values.GroupBy(i => i).OrderByDescending(g => g.Count()).First().Key;

        return new StatisticResult(max, min, mean, range, mode, values.Count);
    }

    /// <summary>
    /// maximum value
    /// </summary>
    public readonly int Maximum = max;

    /// <summary>
    /// minimum value
    /// </summary>
    public readonly int Minimum = min;

    /// <summary>
    /// average value of the total data set
    /// </summary>
    public readonly double Mean = mean;

    /// <summary>
    /// most frequent value in the total data set
    /// </summary>
    public readonly int? Mode = mode;

    /// <summary>
    /// difference between max and min value
    /// </summary>
    public readonly int Range = range;

    /// <summary>
    /// number of data points in the total data set
    /// </summary>
    public readonly int Count = count;

    /// <summary>
    /// Writes out these statistics to a property bag for sending to telemetry.
    /// </summary>
    /// <param name="prefix">The prefix given to any properties written. A period is used to delimit between the 
    /// prefix and the value.</param>
    public void WriteTelemetryPropertiesTo(IDictionary<string, object?> properties, string prefix)
    {
        prefix += ".";

        properties.Add(prefix + nameof(Maximum), Maximum);
        properties.Add(prefix + nameof(Minimum), Minimum);
        properties.Add(prefix + nameof(Mean), Mean);
        properties.Add(prefix + nameof(Range), Range);
        properties.Add(prefix + nameof(Count), Count);

        if (Mode.HasValue)
            properties.Add(prefix + nameof(Mode), Mode.Value);
    }
}
