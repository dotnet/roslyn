// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ReferenceManager;

[UseExportProvider]
public sealed class VisualStudioMetadataReferenceManagerTests : TestBase
{
    [ConditionalTheory(typeof(WindowsOnly))]
    [InlineData(0)]
    [InlineData(4095)]
    [InlineData(4096)]
    [InlineData(65535)]
    [InlineData(65536)]
    [InlineData(65537)]
    [InlineData(262145)]
    public void CopiesOnlyMetadataToTemporaryStorage(int blobSize)
    {
        using var workspace = new AdhocWorkspace();
        var service = (TemporaryStorageService)workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>();
        var image = CreateAssemblyImage(blobSize);
        byte[] imageWithTrailingBytes = [.. image, 0xAB, 0xCD, 0xEF];
        var file = Temp.CreateFile().WriteAllBytes(imageWithTrailingBytes);

        var (metadata, handles) = VisualStudioMetadataReferenceManager.TestAccessor.CreateAssemblyMetadata(file.Path, service);
        using (metadata)
        {
            File.Delete(file.Path);

            Assert.NotNull(handles);
            VerifyMetadata(image, Assert.Single(metadata.GetModules()), Assert.Single(handles));
        }
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void CopiesAllModulesToTemporaryStorage()
    {
        using var workspace = new AdhocWorkspace();
        var service = (TemporaryStorageService)workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>();
        var directory = Temp.CreateDirectory();
        var images = new[]
        {
            (name: "MultiModule.dll", image: TestResources.SymbolsTests.MultiModule.MultiModuleDll),
            (name: "mod2.netmodule", image: TestResources.SymbolsTests.MultiModule.mod2),
            (name: "mod3.netmodule", image: TestResources.SymbolsTests.MultiModule.mod3),
        };

        foreach (var (name, image) in images)
            directory.CreateFile(name).WriteAllBytes(image);

        var (metadata, handles) = VisualStudioMetadataReferenceManager.TestAccessor.CreateAssemblyMetadata(
            Path.Combine(directory.Path, images[0].name), service);
        using (metadata)
        {
            var modules = metadata.GetModules();
            Assert.NotNull(handles);
            Assert.Equal(images.Length, modules.Length);
            Assert.Equal(images.Length, handles.Count);

            for (var i = 0; i < images.Length; i++)
                VerifyMetadata(images[i].image, modules[i], handles[i]);
        }
    }

    [ConditionalTheory(typeof(WindowsOnly))]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RejectsTruncatedImage(int truncation)
    {
        using var workspace = new AdhocWorkspace();
        var service = (TemporaryStorageService)workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>();
        var image = CreateAssemblyImage(65537);
        using var peReader = new PEReader(new MemoryStream(image));
        var length = truncation switch
        {
            0 => 0,
            1 => peReader.PEHeaders.MetadataStartOffset,
            _ => peReader.PEHeaders.MetadataStartOffset + peReader.PEHeaders.MetadataSize - 1,
        };
        var file = Temp.CreateFile().WriteAllBytes(image.Take(length).ToArray());

        Assert.Throws<BadImageFormatException>(
            () => VisualStudioMetadataReferenceManager.TestAccessor.CreateAssemblyMetadata(file.Path, service));
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void RejectsCorruptMetadata()
    {
        using var workspace = new AdhocWorkspace();
        var service = (TemporaryStorageService)workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>();
        var image = CreateAssemblyImage(65537);
        using (var peReader = new PEReader(new MemoryStream(image)))
            image[peReader.PEHeaders.MetadataStartOffset] = 0;

        var file = Temp.CreateFile().WriteAllBytes(image);

        Assert.Throws<BadImageFormatException>(
            () => VisualStudioMetadataReferenceManager.TestAccessor.CreateAssemblyMetadata(file.Path, service));
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void PropagatesFileReadFailure()
    {
        using var workspace = new AdhocWorkspace();
        var service = (TemporaryStorageService)workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>();
        var file = Temp.CreateFile().WriteAllBytes(CreateAssemblyImage(65537));
        using var locked = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.None);

        Assert.Throws<IOException>(
            () => VisualStudioMetadataReferenceManager.TestAccessor.CreateAssemblyMetadata(file.Path, service));
    }

    private static void VerifyMetadata(
        byte[] image,
        ModuleMetadata module,
        TemporaryStorageService.TemporaryStorageStreamHandle handle)
    {
        using var peReader = new PEReader(new MemoryStream(image));
        var expectedBytes = peReader.GetMetadata().GetContent();
        using var stored = handle.ReadFromTemporaryStorage();
        Assert.Equal(expectedBytes.ToArray(), stored.ReadAllBytes());

        var expectedReader = peReader.GetMetadataReader();
        var actualReader = module.GetMetadataReader();
        Assert.Equal(expectedBytes.Length, actualReader.MetadataLength);
        Assert.Equal(
            expectedReader.GetString(expectedReader.GetModuleDefinition().Name),
            actualReader.GetString(actualReader.GetModuleDefinition().Name));
        Assert.Equal(
            expectedReader.GetGuid(expectedReader.GetModuleDefinition().Mvid),
            actualReader.GetGuid(actualReader.GetModuleDefinition().Mvid));
    }

    private static byte[] CreateAssemblyImage(int blobSize)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(
            generation: 0,
            moduleName: metadata.GetOrAddString("MetadataCopy.dll"),
            mvid: metadata.GetOrAddGuid(new Guid("21f5c26f-e8c2-44a9-bf2f-c5f8bdd72f85")),
            encId: default,
            encBaseId: default);
        metadata.AddAssembly(
            metadata.GetOrAddString("MetadataCopy"), new Version(1, 0, 0, 0),
            culture: default, publicKey: default, flags: default, hashAlgorithm: default);
        metadata.AddTypeDefinition(
            TypeAttributes.NotPublic, @namespace: default, metadata.GetOrAddString("<Module>"),
            baseType: default, fieldList: default, methodList: default);
        metadata.GetOrAddBlob(Enumerable.Range(0, blobSize).Select(static i => (byte)i).ToArray());

        var builder = new ManagedPEBuilder(
            new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll),
            new MetadataRootBuilder(metadata),
            new BlobBuilder());
        var image = new BlobBuilder();
        builder.Serialize(image);
        return image.ToArray();
    }
}
