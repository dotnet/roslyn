// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class SourceGeneratorExecutionVersionMapTests
{
    [Fact]
    public void TestOrderingDoesNotMatter()
    {
        var projectId1 = ProjectId.CreateNewId();
        var projectId2 = ProjectId.CreateNewId();
        Assert.NotEqual(projectId1, projectId2);

        var project1Kvp = new KeyValuePair<ProjectId, SourceGeneratorExecutionVersion>(projectId1, new(MajorVersion: 1, MinorVersion: 1));
        var project2Kvp = new KeyValuePair<ProjectId, SourceGeneratorExecutionVersion>(projectId2, new(MajorVersion: 2, MinorVersion: 2));

        var map1 = new SourceGeneratorExecutionVersionMap(ImmutableSortedDictionary.CreateRange([project1Kvp, project2Kvp]));
        var map2 = new SourceGeneratorExecutionVersionMap(ImmutableSortedDictionary.CreateRange([project2Kvp, project1Kvp]));
        Assert.True(map1.Map.SequenceEqual(map2.Map));

        using var memoryStream1 = SerializableBytes.CreateWritableStream();
        using var memoryStream2 = SerializableBytes.CreateWritableStream();
        {
            using var writer1 = new ObjectWriter(memoryStream1, leaveOpen: true);
            {
                map1.WriteTo(writer1);
            }

            using var writer2 = new ObjectWriter(memoryStream2, leaveOpen: true);
            {
                map2.WriteTo(writer2);
            }

            memoryStream1.Position = 0;
            memoryStream2.Position = 0;

            var array1 = memoryStream1.ToArray();
            var array2 = memoryStream2.ToArray();

            Assert.Equal(array1.Length, array2.Length);
            Assert.True(array1.Length > 0);

            Assert.True(array1.AsSpan().SequenceEqual(array2));

            memoryStream1.Position = 0;
            memoryStream2.Position = 0;

            var rehydrated1 = SourceGeneratorExecutionVersionMap.Deserialize(ObjectReader.GetReader(memoryStream1, leaveOpen: true));
            var rehydrated2 = SourceGeneratorExecutionVersionMap.Deserialize(ObjectReader.GetReader(memoryStream2, leaveOpen: true));

            Assert.True(rehydrated1.Map.SequenceEqual(rehydrated2.Map));
            Assert.True(rehydrated1.Map.SequenceEqual(map1.Map));
        }
    }
}
