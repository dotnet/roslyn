// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CompilerBenchmarks;

[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev", "Median", "RatioSD")]
public class ErrorFactsStrings
{
    private ErrorCode[] _errorCodes = Array.Empty<ErrorCode>();

    [GlobalSetup]
    public void Setup()
    {
        _errorCodes = Enum.GetValues(typeof(ErrorCode)).Cast<ErrorCode>().ToArray();
    }

    [Benchmark(Baseline = true)]
    public void EnumToString()
    {
        foreach (ErrorCode val in _errorCodes)
        {
            Use(val.ToString());
        }
    }

    [Benchmark]
    public void EnumToStringHelper()
    {
        foreach (ErrorCode val in _errorCodes)
        {
            Use(ErrorFacts.ToString(val));
        }
    }

#pragma warning disable IDE0060
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Use(string? str)
    {
    }
#pragma warning restore IDE0060
}
