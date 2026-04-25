// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public class DefaultLSPDocumentTest : ToolingTestBase
{
    private readonly Uri _uri;
    private readonly IContentType _notInertContentType;

    public DefaultLSPDocumentTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _uri = new Uri("C:/path/to/file.razor__virtual.cs");
        _notInertContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType("inert") == false, MockBehavior.Strict);
    }

    [Fact]
    public void InertTextBuffer_DoesNotCreateSnapshot()
    {
        // Arrange
        var textBuffer = new TestTextBuffer(new StringTextSnapshot(string.Empty));
        textBuffer.ChangeContentType(_notInertContentType, editTag: null);
        using var document = new DefaultLSPDocument(_uri, textBuffer, virtualDocuments: Array.Empty<VirtualDocument>());
        var originalSnapshot = document.CurrentSnapshot;
        textBuffer.ChangeContentType(TestInertContentType.Instance, editTag: null);

        // Act
        var edit = textBuffer.CreateEdit();
        edit.Insert(0, "New!");
        edit.Apply();

        // Assert
        var newSnapshot = document.CurrentSnapshot;
        Assert.Same(originalSnapshot, newSnapshot);
    }

    [Fact]
    public void CurrentSnapshot_ChangesWhenTextBufferChanges()
    {
        // Arrange
        var textBuffer = new TestTextBuffer(new StringTextSnapshot(string.Empty));
        textBuffer.ChangeContentType(_notInertContentType, editTag: null);
        using var document = new DefaultLSPDocument(_uri, textBuffer, virtualDocuments: Array.Empty<VirtualDocument>());
        var originalSnapshot = document.CurrentSnapshot;

        // Act
        var edit = textBuffer.CreateEdit();
        edit.Insert(0, "New!");
        edit.Apply();

        // Assert
        var newSnapshot = document.CurrentSnapshot;
        Assert.NotSame(originalSnapshot, newSnapshot);
        Assert.Equal(1, originalSnapshot.Version);
        Assert.Equal(2, newSnapshot.Version);
    }

    [Fact]
    public void UpdateVirtualDocument_UpdatesProvidedVirtualDocumentWithProvidedArgs_AndRecalcsSnapshot()
    {
        // Arrange
        var textBuffer = new TestTextBuffer(new StringTextSnapshot(string.Empty));
        var virtualDocument = new TestVirtualDocument();
        using var document = new DefaultLSPDocument(_uri, textBuffer, new[] { virtualDocument });
        var changes = Array.Empty<ITextChange>();
        var originalSnapshot = document.CurrentSnapshot;

        // Act
        document.UpdateVirtualDocument<TestVirtualDocument>(changes, hostDocumentVersion: 1337, state: null);

        // Assert
        Assert.Equal(1337, virtualDocument.HostDocumentVersion);
        Assert.Same(changes, virtualDocument.Changes);
        Assert.NotEqual(originalSnapshot, document.CurrentSnapshot);
    }

    private class TestVirtualDocument : VirtualDocument
    {
        private int _hostDocumentVersion;

        public IReadOnlyList<ITextChange> Changes { get; private set; }

        public override Uri Uri => throw new NotImplementedException();

        public override ITextBuffer TextBuffer => throw new NotImplementedException();

        public override VirtualDocumentSnapshot CurrentSnapshot => null;

        public override int HostDocumentVersion => _hostDocumentVersion;

        public override VirtualDocumentSnapshot Update(IReadOnlyList<ITextChange> changes, int hostDocumentVersion, object state)
        {
            _hostDocumentVersion = hostDocumentVersion;
            Changes = changes;

            return null;
        }

        public override void Dispose()
        {
        }
    }
}
