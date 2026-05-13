// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Editor;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage.Extensions;
using Microsoft.VisualStudio.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public class VirtualDocumentBaseTest : ToolingTestBase
{
    private readonly Uri _uri;

    public VirtualDocumentBaseTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _uri = new Uri("C:/path/to/file.razor__virtual.test");
    }

    [Fact]
    public void Update_AlwaysSetsHostDocumentSyncVersion_AndUpdatesSnapshot_AndSetsState()
    {
        // Arrange
        var textBuffer = new TestTextBuffer(StringTextSnapshot.Empty);
        using var document = new TestVirtualDocument(_uri, textBuffer);
        var originalSnapshot = document.CurrentSnapshot;
        var originalState = new object();

        // Act
        document.Update(Array.Empty<ITextChange>(), hostDocumentVersion: 1337, state: originalState);

        // Assert
        Assert.NotSame(originalSnapshot, document.CurrentSnapshot);
        Assert.Equal(1337, document.HostDocumentVersion);
        Assert.Same(originalState, (document.CurrentSnapshot as TestVirtualDocumentSnapshot)?.State);
    }

    [Fact]
    public void Update_Insert()
    {
        // Arrange
        var insert = new VisualStudioTextChange(0, 0, "inserted text");
        var textBuffer = new TestTextBuffer(StringTextSnapshot.Empty);
        using var document = new TestVirtualDocument(_uri, textBuffer);

        // Act
        document.Update(new[] { insert }, hostDocumentVersion: 1, state: null);

        // Assert
        var text = textBuffer.CurrentSnapshot.GetText();
        Assert.Equal(insert.NewText, text);
    }

    [Fact]
    public void Update_Replace()
    {
        // Arrange
        var textBuffer = new TestTextBuffer(new StringTextSnapshot("original"));
        var replace = new VisualStudioTextChange(0, textBuffer.CurrentSnapshot.Length, "replaced text");
        using var document = new TestVirtualDocument(_uri, textBuffer);

        // Act
        document.Update(new[] { replace }, hostDocumentVersion: 1, state: null);

        // Assert
        var text = textBuffer.CurrentSnapshot.GetText();
        Assert.Equal(replace.NewText, text);
    }

    [Fact]
    public void Update_Delete()
    {
        // Arrange
        var textBuffer = new TestTextBuffer(new StringTextSnapshot("Hello World"));
        var delete = new VisualStudioTextChange(6, 5, string.Empty);
        using var document = new TestVirtualDocument(_uri, textBuffer);

        // Act
        document.Update(new[] { delete }, hostDocumentVersion: 1, state: null);

        // Assert
        var text = textBuffer.CurrentSnapshot.GetText();
        Assert.Equal("Hello ", text);
    }

    [Fact]
    public void Update_MultipleEdits()
    {
        // Arrange
        var textBuffer = new TestTextBuffer(new StringTextSnapshot("Hello World"));
        var replace = new VisualStudioTextChange(6, 5, "Replaced");
        var delete = new VisualStudioTextChange(0, 6, string.Empty);
        using var document = new TestVirtualDocument(_uri, textBuffer);

        // Act
        document.Update(new[] { replace, delete }, hostDocumentVersion: 1, state: null);

        // Assert
        var text = textBuffer.CurrentSnapshot.GetText();
        Assert.Equal("Replaced", text);
    }

    [Fact]
    public void Update_NoChanges_InvokesPostChangedEventZeroTimes_NoEffectiveChanges()
    {
        // Arrange
        var textBuffer = new TestTextBuffer(new StringTextSnapshot("Hello World"));
        var called = 0;
        textBuffer.PostChanged += (s, a) =>
        {
            textBuffer.TryGetHostDocumentSyncVersion(out var version);
            Assert.Equal(1, version);

            called += 1;
        };

        using var document = new TestVirtualDocument(_uri, textBuffer);

        // Act
        document.Update(Array.Empty<ITextChange>(), hostDocumentVersion: 1, state: null);

        // Assert
        Assert.Equal(0, called);
        var text = textBuffer.CurrentSnapshot.GetText();
        Assert.Equal("Hello World", text);
    }
}
