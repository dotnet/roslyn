﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public class ChecksumTests
{
    [Fact]
    public void ValidateChecksumFromSpanSameAsChecksumFromBytes1()
    {
        var checksum1 = Checksum.Create("Goo");
        var checksum2 = Checksum.Create("Bar");

        var checksumA = Checksum.Create(checksum1, checksum2);

        // Running this test on multiple target frameworks with the same expectation ensures the results match
        Assert.Equal(Checksum.FromBase64String("RRbspG+E4ziBC5hOWyrfCQ=="), checksumA);

        Assert.NotEqual(checksum1, checksum2);
        Assert.NotEqual(checksum1, checksumA);
        Assert.NotEqual(checksum2, checksumA);
    }

    [Fact]
    public void ValidateChecksumFromSpanSameAsChecksumFromBytes2()
    {
        var checksum1 = Checksum.Create("Goo");
        var checksum2 = Checksum.Create("Bar");
        var checksum3 = Checksum.Create("Baz");

        var checksumA = Checksum.Create(checksum1, checksum2, checksum3);

        // Running this test on multiple target frameworks with the same expectation ensures the results match
        Assert.Equal(Checksum.FromBase64String("DvHp7gyz/hBKY1/Q7A1NCg=="), checksumA);

        Assert.NotEqual(checksum1, checksum2);
        Assert.NotEqual(checksum2, checksum3);
        Assert.NotEqual(checksum3, checksum1);

        Assert.NotEqual(checksum1, checksumA);
        Assert.NotEqual(checksum2, checksumA);
        Assert.NotEqual(checksum3, checksumA);
    }

    [Fact]
    public void ValidateChecksumFromSpanSameAsChecksumFromBytes10()
    {
        const int max = 10;
        var checksums = Enumerable.Range(0, max).Select(i => Checksum.Create($"{i}")).ToArray();

        var checksumA = Checksum.Create(checksums.AsSpan());

        // Running this test on multiple target frameworks with the same expectation ensures the results match
        Assert.Equal(Checksum.FromBase64String("umt6tOdNsvIArs4OY7MFCg=="), checksumA);

        for (var i = 0; i < max; i++)
        {
            for (var j = 0; j < max; j++)
            {
                Assert.True((i == j) == (checksums[i] == checksums[j]));
            }

            Assert.NotEqual(checksums[i], checksumA);
        }
    }

    [Fact]
    public void StringArraysProduceDifferentResultsThanConcatenation()
    {
        var checksum1 = Checksum.Create(["goo", "bar"]);
        var checksum2 = Checksum.Create(["go", "obar"]);
        var checksum3 = Checksum.Create("goobar");
        Assert.NotEqual(checksum1, checksum2);
        Assert.NotEqual(checksum2, checksum3);
        Assert.NotEqual(checksum3, checksum1);
    }

    [Fact]
    public void DoNotProduceNullChecksum()
    {
        // Ensure empty sources don't create the Null checksum.
        Assert.NotEqual(Checksum.Null, Checksum.Create(Array.Empty<string>()));
        Assert.NotEqual(Checksum.Null, Checksum.Create(""));
        Assert.NotEqual(Checksum.Null, Checksum.Create("\0"));
        Assert.NotEqual(Checksum.Null, Checksum.Create((string?)null));
        Assert.NotEqual(Checksum.Null, Checksum.Create(new MemoryStream()));
        Assert.NotEqual(Checksum.Null, Checksum.Create(stackalloc Checksum[0]));
        Assert.NotEqual(Checksum.Null, Checksum.Create(ImmutableArray<Checksum>.Empty));
        Assert.NotEqual(Checksum.Null, Checksum.Create(ImmutableArray<byte>.Empty));

        Assert.NotEqual(Checksum.Null, Checksum.Create([""]));
        Assert.NotEqual(Checksum.Null, Checksum.Create(["\0"]));
        Assert.NotEqual(Checksum.Null, Checksum.Create(new string?[] { null }));
        Assert.NotEqual(Checksum.Null, Checksum.Create(new MemoryStream()));
        Assert.NotEqual(Checksum.Null, Checksum.Create(stackalloc Checksum[] { Checksum.Null }));
        Assert.NotEqual(Checksum.Null, Checksum.Create(ImmutableArray.Create(Checksum.Null)));
        Assert.NotEqual(Checksum.Null, Checksum.Create(ImmutableArray.Create((byte)0)));
        Assert.NotEqual(Checksum.Null, Checksum.Create(Checksum.Null, Checksum.Null));
        Assert.NotEqual(Checksum.Null, Checksum.Create(Checksum.Null, Checksum.Null, Checksum.Null));

        var stream = new MemoryStream();
        stream.WriteByte(0);
        stream.Position = 0;
        Assert.NotEqual(Checksum.Null, Checksum.Create(stream));
    }
}
