// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Test;

public class ChecksumTests(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
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

    private static readonly Func<Checksum> s_empty = () =>
    {
        var builder = new Checksum.Builder();
        return builder.FreeAndGetChecksum();
    };

    private static readonly Func<Checksum> s_falseValue = () =>
    {
        var builder = new Checksum.Builder();
        builder.Append(false);
        return builder.FreeAndGetChecksum();
    };

    private static readonly Func<Checksum> s_trueValue = () =>
    {
        var builder = new Checksum.Builder();
        builder.Append(true);
        return builder.FreeAndGetChecksum();
    };

    private static Func<Checksum> CreateInt32(int value)
    {
        return () =>
        {
            var builder = new Checksum.Builder();
            builder.Append(value);
            return builder.FreeAndGetChecksum();
        };
    }

    private static Func<Checksum> CreateInt64(long value)
    {
        return () =>
        {
            var builder = new Checksum.Builder();
            builder.Append(value);
            return builder.FreeAndGetChecksum();
        };
    }

    private static Func<Checksum> CreateString(string? value)
    {
        return () =>
        {
            var builder = new Checksum.Builder();
            builder.Append(value);
            return builder.FreeAndGetChecksum();
        };
    }

    private static Func<Checksum> Combine(params Func<Checksum>[] producers)
    {
        return () =>
        {
            var builder = new Checksum.Builder();
            foreach (var producer in producers)
            {
                builder.Append(producer());
            }

            return builder.FreeAndGetChecksum();
        };
    }

    [Theory]
    [MemberData(nameof(Checksums))]
    internal void TestEquality(bool areEqual, Func<Checksum> producer1, Func<Checksum> producer2)
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

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1909377")]
    public void TestLargeString()
    {
        object? largeString = RazorTestResources.GetResourceText("FormattingTest.razor");

        var builder = new Checksum.Builder();
        builder.Append(largeString);

        var result = builder.FreeAndGetChecksum();

        Assert.NotEqual(result, Checksum.Null);
    }
}
