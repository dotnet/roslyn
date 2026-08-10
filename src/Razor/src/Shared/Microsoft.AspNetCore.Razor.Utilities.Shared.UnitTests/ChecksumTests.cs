// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using RazorChecksum = Microsoft.AspNetCore.Razor.Utilities.Checksum;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class ChecksumTests
{
    public static IEnumerable<object[]> Checksums
    {
        get
        {
            yield return new object[] { true, s_empty, s_empty };
            yield return new object[] { true, s_falseValue, s_falseValue };
            yield return new object[] { false, s_falseValue, s_trueValue };
            yield return new object[] { true, s_trueValue, s_trueValue };
            yield return new object[] { true, CreateInt32(0), CreateInt32(0) };
            yield return new object[] { true, CreateInt32(int.MaxValue), CreateInt32(int.MaxValue) };
            yield return new object[] { false, CreateInt32(0), CreateInt32(int.MaxValue) };
            yield return new object[] { false, CreateInt32(0), s_falseValue };
            yield return new object[] { false, CreateInt32(0), CreateInt64(0) };
            yield return new object[] { true, CreateInt64(0), CreateInt64(0) };
            yield return new object[] { true, CreateInt64(long.MaxValue), CreateInt64(long.MaxValue) };
            yield return new object[] { false, CreateInt64(0), CreateInt64(long.MaxValue) };
            yield return new object[] { false, CreateInt64(0), s_falseValue };
            yield return new object[] { false, CreateInt64(int.MaxValue), CreateInt32(int.MaxValue) };
            yield return new object[] { true, CreateString(null), CreateString(null) };
            yield return new object[] { false, CreateString("test"), CreateString(null) };
            yield return new object[] { true, CreateString("test"), CreateString("test") };
            yield return new object[] { true, Combine(s_falseValue, s_trueValue), Combine(s_falseValue, s_trueValue) };
            yield return new object[] { false, Combine(s_trueValue, s_falseValue), Combine(s_falseValue, s_trueValue) };
        }
    }

    private static readonly Func<RazorChecksum> s_empty = () =>
    {
        var builder = new RazorChecksum.Builder();
        return builder.FreeAndGetChecksum();
    };

    private static readonly Func<RazorChecksum> s_falseValue = () =>
    {
        var builder = new RazorChecksum.Builder();
        builder.Append(false);
        return builder.FreeAndGetChecksum();
    };

    private static readonly Func<RazorChecksum> s_trueValue = () =>
    {
        var builder = new RazorChecksum.Builder();
        builder.Append(true);
        return builder.FreeAndGetChecksum();
    };

    private static Func<RazorChecksum> CreateInt32(int value)
    {
        return () =>
        {
            var builder = new RazorChecksum.Builder();
            builder.Append(value);
            return builder.FreeAndGetChecksum();
        };
    }

    private static Func<RazorChecksum> CreateInt64(long value)
    {
        return () =>
        {
            var builder = new RazorChecksum.Builder();
            builder.Append(value);
            return builder.FreeAndGetChecksum();
        };
    }

    private static Func<RazorChecksum> CreateString(string? value)
    {
        return () =>
        {
            var builder = new RazorChecksum.Builder();
            builder.Append(value);
            return builder.FreeAndGetChecksum();
        };
    }

    private static Func<RazorChecksum> Combine(params Func<RazorChecksum>[] producers)
    {
        return () =>
        {
            var builder = new RazorChecksum.Builder();
            foreach (var producer in producers)
            {
                builder.Append(producer());
            }

            return builder.FreeAndGetChecksum();
        };
    }

    [Theory]
    [MemberData(nameof(Checksums))]
    internal void TestEquality(bool areEqual, Func<RazorChecksum> producer1, Func<RazorChecksum> producer2)
    {
        var checksum1 = producer1();
        var checksum2 = producer2();

        if (areEqual)
        {
            Assert.Equal(checksum1, checksum2);
        }
        else
        {
            Assert.NotEqual(checksum1, checksum2);
        }
    }

    [Fact]
    public void TestLargeString()
    {
        var largeString = new string('a', 100_000);

        var builder = new RazorChecksum.Builder();
        builder.Append(largeString);

        var result = builder.FreeAndGetChecksum();

        Assert.NotEqual(result, RazorChecksum.Null);
    }
}
