// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

[UseExportProvider]
public sealed class TextFactoryTests
{
    private readonly byte[] _nonUtf8StringBytes = [0x80, 0x92, 0xA4, 0xB6, 0xC9, 0xDB, 0xED, 0xFF];

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1038018"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1041792")]
    public void TestCreateTextFallsBackToSystemDefaultEncoding()
    {
        using var workspace = new AdhocWorkspace(EditorTestCompositions.EditorFeatures.GetHostServices());
        var textFactoryService = Assert.IsType<EditorTextFactoryService>(workspace.Services.GetRequiredService<ITextFactoryService>());

        TestCreateTextInferredEncoding(
            textFactoryService,
            _nonUtf8StringBytes,
            defaultEncoding: null,
            expectedEncoding: Encoding.Default);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1038018")]
    public void TestCreateTextFallsBackToUTF8Encoding()
    {
        using var workspace = new AdhocWorkspace(EditorTestCompositions.EditorFeatures.GetHostServices());
        var textFactoryService = Assert.IsType<EditorTextFactoryService>(workspace.Services.GetRequiredService<ITextFactoryService>());

        TestCreateTextInferredEncoding(
            textFactoryService,
            new ASCIIEncoding().GetBytes("Test"),
            defaultEncoding: null,
            expectedEncoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1038018")]
    public void TestCreateTextFallsBackToProvidedDefaultEncoding()
    {
        using var workspace = new AdhocWorkspace(EditorTestCompositions.EditorFeatures.GetHostServices());
        var textFactoryService = Assert.IsType<EditorTextFactoryService>(workspace.Services.GetRequiredService<ITextFactoryService>());

        TestCreateTextInferredEncoding(
            textFactoryService,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetBytes("Test"),
            defaultEncoding: Encoding.GetEncoding(1254),
            expectedEncoding: Encoding.GetEncoding(1254));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1038018")]
    public void TestCreateTextUsesByteOrderMarkIfPresent()
    {
        using var workspace = new AdhocWorkspace(EditorTestCompositions.EditorFeatures.GetHostServices());
        var textFactoryService = Assert.IsType<EditorTextFactoryService>(workspace.Services.GetRequiredService<ITextFactoryService>());

        TestCreateTextInferredEncoding(
            textFactoryService,
            [
                .. Encoding.UTF8.GetPreamble(),
                .. new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetBytes("Test"),
            ],
            defaultEncoding: Encoding.GetEncoding(1254),
            expectedEncoding: Encoding.UTF8);
    }

    [Fact]
    public async Task TestCreateFromTemporaryStorage()
    {
        using var workspace = new AdhocWorkspace(EditorTestCompositions.EditorFeatures.GetHostServices());

        var temporaryStorageService = Assert.IsType<TemporaryStorageService>(workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>());

        var text = SourceText.From("Hello, World!");

        // Write text into it
        var handle = await temporaryStorageService.WriteToTemporaryStorageAsync(text, CancellationToken.None);

        // Read text back from it
        var text2 = await handle.ReadFromTemporaryStorageAsync(CancellationToken.None);

        Assert.NotSame(text, text2);
        Assert.Equal(text.ToString(), text2.ToString());
        Assert.Null(text2.Encoding);
    }

    [Fact]
    public async Task TestCreateFromTemporaryStorageWithEncoding()
    {
        using var workspace = new AdhocWorkspace(EditorTestCompositions.EditorFeatures.GetHostServices());

        var temporaryStorageService = Assert.IsType<TemporaryStorageService>(workspace.Services.GetRequiredService<ITemporaryStorageServiceInternal>());

        var text = SourceText.From("Hello, World!", Encoding.ASCII);

        // Write text into it
        var handle = await temporaryStorageService.WriteToTemporaryStorageAsync(text, CancellationToken.None);

        // Read text back from it
        var text2 = await handle.ReadFromTemporaryStorageAsync(CancellationToken.None);

        Assert.NotSame(text, text2);
        Assert.Equal(text.ToString(), text2.ToString());
        Assert.Equal(text2.Encoding, Encoding.ASCII);
    }

    private static void TestCreateTextInferredEncoding(ITextFactoryService textFactoryService, byte[] bytes, Encoding? defaultEncoding, Encoding expectedEncoding)
    {
        using var stream = new MemoryStream(bytes);
        var text = textFactoryService.CreateText(stream, defaultEncoding, SourceHashAlgorithms.Default, CancellationToken.None);
        Assert.Equal(expectedEncoding, text.Encoding);
        Assert.Equal(SourceHashAlgorithms.Default, text.ChecksumAlgorithm);
    }
}
