// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public sealed class MetadataServiceTests : TestBase
{
    [Fact]
    public void GetReference_ValidAssembly_ReturnsPortableExecutableReference()
    {
        using var workspace = SolutionTestHelpers.CreateWorkspace();
        var metadataService = workspace.Services.GetRequiredService<IMetadataService>();

        var properties = MetadataReferenceProperties.Assembly.WithAliases(["global", "MyAlias"]).WithEmbedInteropTypes(true);

        var mscorlibPath = typeof(object).Assembly.Location;
        var reference = metadataService.GetReference(mscorlibPath, properties);

        Assert.NotNull(reference);
        Assert.Equal(mscorlibPath, reference.FilePath);
        Assert.Equal(properties, reference.Properties);

        Assert.NotNull(reference.GetMetadata());
    }

    [Fact]
    public void GetReference_SamePathAndProperties_ReturnsCachedReference()
    {
        using var workspace = SolutionTestHelpers.CreateWorkspace();
        var metadataService = workspace.Services.GetRequiredService<IMetadataService>();

        var mscorlibPath = typeof(object).Assembly.Location;
        var reference1 = metadataService.GetReference(mscorlibPath, MetadataReferenceProperties.Assembly);
        var reference2 = metadataService.GetReference(mscorlibPath, MetadataReferenceProperties.Assembly);

        Assert.Same(reference1, reference2);
    }

    [Fact]
    public void GetReference_NonExistentFile_ReturnsThrowingReference()
    {
        using var workspace = SolutionTestHelpers.CreateWorkspace();
        var metadataService = workspace.Services.GetRequiredService<IMetadataService>();

        var nonExistentPath = Path.Combine(TempRoot.Root, Guid.NewGuid().ToString() + ".dll");
        var reference1 = metadataService.GetReference(nonExistentPath, MetadataReferenceProperties.Assembly);
        var reference2 = metadataService.GetReference(nonExistentPath, MetadataReferenceProperties.Assembly);

        // Failure is cached:
        Assert.Same(reference1, reference2);

        // Reference is returned even for non-existent files
        Assert.NotNull(reference1);
        Assert.Equal(nonExistentPath, reference1.FilePath);

        // Accessing metadata should throw the stored IOException
        Assert.Throws<FileNotFoundException>(reference1.GetMetadata);
    }
}
