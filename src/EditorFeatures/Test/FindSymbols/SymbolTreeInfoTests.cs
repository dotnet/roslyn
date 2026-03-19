// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.FindSymbols;

[UseExportProvider]
public sealed class SymbolTreeInfoTests
{
    [Fact]
    public async Task TestSymbolTreeInfoForMetadataWithDifferentProperties1()
    {
        using var workspace = TestWorkspace.CreateCSharp("");
        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var reference1 = (PortableExecutableReference)project.MetadataReferences.First();
        var reference2 = reference1.WithAliases(["Alias"]);

        var info1 = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
            solution, reference1, checksum: null, CancellationToken.None);

        var info2 = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
            solution, reference2, checksum: null, CancellationToken.None);

        Assert.NotEqual(info1.Checksum, info2.Checksum);
        Assert.Equal(info1.Checksum, SymbolTreeInfo.GetMetadataChecksum(solution.Services, reference1, CancellationToken.None));
        Assert.Equal(info2.Checksum, SymbolTreeInfo.GetMetadataChecksum(solution.Services, reference2, CancellationToken.None));
    }

    [Fact]
    public async Task TestSymbolTreeInfoForMetadataWithDifferentProperties2()
    {
        using var workspace = TestWorkspace.CreateCSharp("");
        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var reference1 = (PortableExecutableReference)project.MetadataReferences.First();
        var reference2 = reference1.WithAliases(["Alias"]);

        var checksum1 = SymbolTreeInfo.GetMetadataChecksum(solution.Services, reference1, CancellationToken.None);
        var info1 = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
            solution, reference1, checksum1, CancellationToken.None);

        var checksum2 = SymbolTreeInfo.GetMetadataChecksum(solution.Services, reference2, CancellationToken.None);
        var info2 = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
            solution, reference2, checksum2, CancellationToken.None);

        Assert.NotEqual(info1.Checksum, info2.Checksum);
        Assert.Equal(info1.Checksum, checksum1);
        Assert.Equal(info2.Checksum, checksum2);
    }

    [Fact]
    public async Task TestSymbolTreeInfoForMetadataWithDifferentProperties3()
    {
        using var workspace = TestWorkspace.CreateCSharp("");
        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var reference1 = (PortableExecutableReference)project.MetadataReferences.First();
        var reference2 = reference1.WithAliases(["Alias"]);

        var checksum1 = SymbolTreeInfo.GetMetadataChecksum(solution.Services, reference1, CancellationToken.None);
        var info1 = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
            solution, reference1, checksum1, CancellationToken.None);

        var info2 = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
            solution, reference2, checksum: null, CancellationToken.None);

        Assert.NotEqual(info1.Checksum, info2.Checksum);
        Assert.Equal(info1.Checksum, checksum1);
        Assert.Equal(info2.Checksum, SymbolTreeInfo.GetMetadataChecksum(solution.Services, reference2, CancellationToken.None));
    }

    [Fact]
    public async Task TestSymbolTreeInfoForMetadataWithDifferentProperties4()
    {
        using var workspace = TestWorkspace.CreateCSharp("");
        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var reference1 = (PortableExecutableReference)project.MetadataReferences.First();
        var reference2 = reference1.WithAliases(["Alias"]);

        var info1 = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
            solution, reference1, checksum: null, CancellationToken.None);

        var checksum2 = SymbolTreeInfo.GetMetadataChecksum(solution.Services, reference2, CancellationToken.None);
        var info2 = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
            solution, reference2, checksum2, CancellationToken.None);

        Assert.NotEqual(info1.Checksum, info2.Checksum);
        Assert.Equal(info1.Checksum, SymbolTreeInfo.GetMetadataChecksum(solution.Services, reference1, CancellationToken.None));
        Assert.Equal(info2.Checksum, checksum2);
    }
}
