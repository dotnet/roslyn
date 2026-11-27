// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
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
    public void GetReference_PreservesRecursiveAliases()
    {
        using var workspace = SolutionTestHelpers.CreateWorkspace();
        var metadataService = workspace.Services.GetRequiredService<IMetadataService>();
        var recordingMetadataService = new RecordingMetadataService(metadataService);
        var resolver = new WorkspaceMetadataFileReferenceResolver(
            recordingMetadataService,
            new RelativePathResolver([], baseDirectory: null));
        var path = typeof(object).Assembly.Location.Replace(@"\", @"\\");
        var syntaxTree = CSharpSyntaxTree.ParseText(
            $"""#r "{path}" """,
            CSharpParseOptions.Default.WithKind(SourceCodeKind.Script));
        var compilation = CSharpCompilation.CreateScriptCompilation(
            "Test",
            syntaxTree,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithMetadataReferenceResolver(resolver));

        _ = compilation.GetDiagnostics();

        Assert.True(recordingMetadataService.RequestedProperties.HasValue);
        Assert.NotNull(recordingMetadataService.Reference);
        Assert.Equal(recordingMetadataService.RequestedProperties.GetValueOrDefault(), recordingMetadataService.Reference.Properties);
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
    public void GetReference_DifferentWorkspaces_DoNotShareMetadata()
    {
        using var workspace1 = SolutionTestHelpers.CreateWorkspace();
        using var workspace2 = SolutionTestHelpers.CreateWorkspace();
        var mscorlibPath = typeof(object).Assembly.Location;

        var reference1 = workspace1.Services.GetRequiredService<IMetadataService>()
            .GetReference(mscorlibPath, MetadataReferenceProperties.Assembly);
        var reference2 = workspace2.Services.GetRequiredService<IMetadataService>()
            .GetReference(mscorlibPath, MetadataReferenceProperties.Assembly);

        Assert.NotSame(reference1, reference2);
        Assert.NotSame(reference1.GetMetadataId(), reference2.GetMetadataId());
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

    [Fact]
    public void GetReference_InvalidModuleName_DefersBadImageFailure()
    {
        using var workspace = SolutionTestHelpers.CreateWorkspace();
        var metadataService = workspace.Services.GetRequiredService<IMetadataService>();
        var invalidModuleName = Temp.CreateFile().WriteAllBytes(TestResources.MetadataTests.Invalid.InvalidModuleName);

        var reference = metadataService.GetReference(invalidModuleName.Path, MetadataReferenceProperties.Assembly);
        var metadata = Assert.IsType<AssemblyMetadata>(reference.GetMetadata());

        Assert.Throws<BadImageFormatException>(() => metadata.GetModules());
    }

    private sealed class RecordingMetadataService(IMetadataService underlyingService) : IMetadataService
    {
        public MetadataReferenceProperties? RequestedProperties { get; private set; }
        public PortableExecutableReference? Reference { get; private set; }

        public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
        {
            RequestedProperties = properties;
            return Reference = underlyingService.GetReference(resolvedPath, properties);
        }
    }
}
