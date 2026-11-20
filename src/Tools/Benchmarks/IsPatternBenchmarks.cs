// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

public enum IsPatternInputPosition
{
    FirstHit,
    LastHit,
    Miss,
}

public enum IsPatternPathInput
{
    Empty,
    Slash,
    Backslash,
    Other,
}

/// <summary>
/// The pattern and comparison forms are expected to have equivalent performance when short patterns use linear
/// lowering. This benchmark detects regressions in that equivalence.
/// </summary>
/// <seealso href="https://github.com/dotnet/roslyn/issues/80052"/>
[EvaluateOverhead(false)]
public class IsPatternLeadingSlashBenchmarks
{
    private string _path = null!;

    [Params(IsPatternPathInput.Empty, IsPatternPathInput.Slash, IsPatternPathInput.Backslash, IsPatternPathInput.Other)]
    public IsPatternPathInput Input { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _path = Input switch
        {
            IsPatternPathInput.Empty => "",
            IsPatternPathInput.Slash => "/path",
            IsPatternPathInput.Backslash => "\\path",
            IsPatternPathInput.Other => "path",
            _ => throw new InvalidOperationException(),
        };
    }

    [Benchmark(Baseline = true)]
    public bool Comparisons() => HasLeadingSlashWithComparisons(_path);

    [Benchmark]
    public bool Pattern() => HasLeadingSlashWithPattern(_path);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool HasLeadingSlashWithComparisons(ReadOnlySpan<char> path)
    {
        if (path.Length > 0 && (path[0] == '/' || path[0] == '\\'))
            return true;

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool HasLeadingSlashWithPattern(ReadOnlySpan<char> path)
    {
        if (path.Length > 0 && path[0] is '/' or '\\')
            return true;

        return false;
    }
}

/// <summary>
/// The pattern and comparison forms are expected to have equivalent performance when short patterns use linear
/// lowering. This benchmark detects regressions in that equivalence.
/// </summary>
/// <seealso href="https://github.com/dotnet/runtime/pull/132452"/>
[EvaluateOverhead(false)]
public class IsPatternJsonTerminatorBenchmarks : IsPatternByteInputBenchmarks
{
    protected override byte FirstCase => (byte)'.';
    protected override byte LastCase => (byte)'e';

    [Benchmark(Baseline = true)]
    public int Comparisons()
    {
        var value = Value;
        return value != (byte)'.' && value != (byte)'E' && value != (byte)'e'
            ? value + 1
            : value - 1;
    }

    [Benchmark]
    public int Pattern()
    {
        var value = Value;
        return value is not ((byte)'.' or (byte)'E' or (byte)'e')
            ? value + 1
            : value - 1;
    }
}

/// <summary>
/// Compares explicit linear tests with general decision-DAG dispatch for four sparse values.
/// </summary>
/// <seealso href="https://github.com/dotnet/roslyn/pull/84961"/>
[EvaluateOverhead(false)]
public class IsPatternSparseFourValueBenchmarks : IsPatternByteInputBenchmarks
{
    protected override byte FirstCase => (byte)'+';
    protected override byte LastCase => (byte)'e';

    [Benchmark(Baseline = true)]
    public bool Comparisons() => Guard && (Value == (byte)'+' || Value == (byte)'.' || Value == (byte)'E' || Value == (byte)'e');

    [Benchmark]
    public bool Pattern() => Guard && Value is (byte)'+' or (byte)'.' or (byte)'E' or (byte)'e';
}

/// <summary>
/// Compares explicit linear tests with general decision-DAG dispatch for four dense values.
/// </summary>
/// <seealso href="https://github.com/dotnet/roslyn/pull/84961"/>
[EvaluateOverhead(false)]
public class IsPatternDenseFourValueBenchmarks : IsPatternByteInputBenchmarks
{
    protected override byte FirstCase => 1;
    protected override byte LastCase => 4;

    [Benchmark(Baseline = true)]
    public bool Comparisons() => Guard && (Value == 1 || Value == 2 || Value == 3 || Value == 4);

    [Benchmark]
    public bool Pattern() => Guard && Value is 1 or 2 or 3 or 4;
}

public abstract class IsPatternByteInputBenchmarks
{
    [Params(IsPatternInputPosition.FirstHit, IsPatternInputPosition.LastHit, IsPatternInputPosition.Miss)]
    public IsPatternInputPosition Input { get; set; }

    protected bool Guard;
    protected byte Value;

    protected abstract byte FirstCase { get; }
    protected abstract byte LastCase { get; }

    [GlobalSetup]
    public void Setup()
    {
        Guard = true;
        Value = Input switch
        {
            IsPatternInputPosition.FirstHit => FirstCase,
            IsPatternInputPosition.LastHit => LastCase,
            IsPatternInputPosition.Miss => byte.MaxValue,
            _ => throw new InvalidOperationException(),
        };
    }
}
