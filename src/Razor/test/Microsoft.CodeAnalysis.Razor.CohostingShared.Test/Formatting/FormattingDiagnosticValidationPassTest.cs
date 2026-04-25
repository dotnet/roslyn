// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting;

public class FormattingDiagnosticValidationPassTest(ITestOutputHelper testOutput) : CohostEndpointTestBase(testOutput)
{
    [Fact]
    public async Task ExecuteAsync_NonDestructiveEdit_Allowed()
    {
        // Arrange
        TestCode source = """
            @code {
            [||]public class Foo { }
            }
            """;
        var context = await CreateFormattingContextAsync(source);
        var edits = ImmutableArray.Create(new TextChange(source.Span, "    "));
        var pass = GetPass();

        // Act
        var result = await pass.IsValidAsync(context, edits, DisposalToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExecuteAsync_DestructiveEdit_Rejected()
    {
        // Arrange
        // Arrange
        TestCode source = """
            [||]@code {
            public class Foo { }
            }
            """;
        var context = await CreateFormattingContextAsync(source);
        var badEdit = new TextChange(source.Span, "@ "); // Creates a diagnostic
        var pass = GetPass();

        await Assert.ThrowsAsync<InvalidOperationException>(() => pass.IsValidAsync(context, [badEdit], DisposalToken));
    }

    private FormattingDiagnosticValidationPass GetPass()
    {
        var pass = new FormattingDiagnosticValidationPass(LoggerFactory)
        {
            DebugAssertsEnabled = false
        };

        return pass;
    }

    private async Task<FormattingContext> CreateFormattingContextAsync(
        TestCode input,
        int tabSize = 4,
        bool insertSpaces = true,
        RazorFileKind? fileKind = null)
    {
        var source = SourceText.From(input.Text);
        var path = "file:///path/to/document.razor";
        var uri = new Uri(path);
        var (codeDocument, documentSnapshot) = await CreateCodeDocumentAndSnapshotAsync(source, uri.AbsolutePath, fileKind: fileKind);
        var options = new RazorFormattingOptions()
        {
            TabSize = tabSize,
            InsertSpaces = insertSpaces,
        };

        var context = FormattingContext.Create(
            documentSnapshot,
            codeDocument,
            options,
            logger: null);
        return context;
    }

    private async Task<(RazorCodeDocument, IDocumentSnapshot)> CreateCodeDocumentAndSnapshotAsync(
        SourceText text,
        string path,
        RazorFileKind? fileKind = null)
    {
        var fileKindValue = fileKind ?? RazorFileKind.Component;

        var document = CreateProjectAndRazorDocument(text.ToString(), fileKind: fileKindValue, documentFilePath: path);

        var snapshotManager = OOPExportProvider.GetExportedValue<RemoteSnapshotManager>();
        var documentSnapshot = snapshotManager.GetSnapshot(document);
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(DisposalToken);

        return (codeDocument, documentSnapshot);
    }
}
