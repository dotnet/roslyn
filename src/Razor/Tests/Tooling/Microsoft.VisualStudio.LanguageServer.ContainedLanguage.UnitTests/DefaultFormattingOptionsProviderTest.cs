// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public class DefaultFormattingOptionsProviderTest : ToolingTestBase
{
    public DefaultFormattingOptionsProviderTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
    }

    [Fact]
    public void GetOptions_UsesIndentationManagerInformation()
    {
        // Arrange
        var documentUri = new Uri("C:/path/to/razorfile.razor");
        var documentSnapshot = new TestLSPDocumentSnapshot(documentUri, version: 0);
        var documentManager = new TestLSPDocumentManager(documentSnapshot);
        var expectedInsertSpaces = true;
        var expectedTabSize = 1337;
        var unneededIndentSize = 123;
        var indentationManagerService = new Mock<IIndentationManagerService>(MockBehavior.Strict);
        indentationManagerService
            .Setup(service => service.GetIndentation(documentSnapshot.Snapshot.TextBuffer, false, out expectedInsertSpaces, out expectedTabSize, out unneededIndentSize))
            .Verifiable();
        var provider = new DefaultFormattingOptionsProvider(documentManager, indentationManagerService.Object);

        // Act
        var options = provider.GetOptions(documentUri);

        // Assert
        indentationManagerService.VerifyAll();
        Assert.Equal(expectedInsertSpaces, options.InsertSpaces);
        Assert.Equal(expectedTabSize, options.TabSize);
    }

    private class TestLSPDocumentManager : LSPDocumentManager
    {
        private readonly LSPDocumentSnapshot _snapshot;

        public TestLSPDocumentManager(LSPDocumentSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public override bool TryGetDocument(Uri uri, out LSPDocumentSnapshot lspDocumentSnapshot)
        {
            lspDocumentSnapshot = _snapshot;
            return true;
        }
    }
}
