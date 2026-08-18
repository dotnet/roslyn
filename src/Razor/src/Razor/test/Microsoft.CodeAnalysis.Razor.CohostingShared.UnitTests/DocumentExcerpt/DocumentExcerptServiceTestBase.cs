// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Xunit;
using Xunit.Abstractions;
using TestFileMarkupParser = Microsoft.CodeAnalysis.Testing.TestFileMarkupParser;

namespace Microsoft.AspNetCore.Razor.Test.Common.Workspaces;

public abstract class DocumentExcerptServiceTestBase(ITestOutputHelper testOutput) : CohostEndpointTestBase(testOutput)
{
    public static (SourceText sourceText, TextSpan span) CreateText(string text)
    {
        // Since we're using positions, normalize to Windows style
        text = text.Replace("\r", "").Replace("\n", "\r\n");

        TestFileMarkupParser.GetSpan(text, out text, out var span);
        return (SourceText.From(text), span);
    }

    // Adds the text to a ProjectSnapshot, generates code, and updates the workspace.
    private async Task<(IDocumentSnapshot primary, SourceGeneratedDocument secondary)> InitializeDocumentAsync(SourceText sourceText)
    {
        var document = CreateProjectAndRazorDocument(sourceText.ToString());

        var snapshotManager = OOPExportProvider.GetExportedValue<RemoteSnapshotManager>();
        var snapshot = snapshotManager.GetSnapshot(document);
        var generatedDocument = await document.Project.TryGetSourceGeneratedDocumentForRazorDocumentAsync(document, DisposalToken);

        return (snapshot, generatedDocument.AssumeNotNull());
    }

    // Maps a span in the primary buffer to the secondary buffer. This is only valid for C# code
    // that appears in the primary buffer.
    private static async Task<TextSpan> GetSecondarySpanAsync(IDocumentSnapshot primary, TextSpan primarySpan, Document secondary, CancellationToken cancellationToken)
    {
        var output = await primary.GetGeneratedOutputAsync(cancellationToken);

        foreach (var mapping in output.GetRequiredCSharpDocument().SourceMappingsSortedByOriginal)
        {
            if (mapping.OriginalSpan.AbsoluteIndex <= primarySpan.Start &&
                (mapping.OriginalSpan.AbsoluteIndex + mapping.OriginalSpan.Length) >= primarySpan.End)
            {
                var offset = mapping.GeneratedSpan.AbsoluteIndex - mapping.OriginalSpan.AbsoluteIndex;
                var secondarySpan = new TextSpan(primarySpan.Start + offset, primarySpan.Length);
                Assert.Equal(
                    (await primary.GetTextAsync(cancellationToken)).ToString(primarySpan),
                    (await secondary.GetTextAsync(cancellationToken)).ToString(secondarySpan));
                return secondarySpan;
            }
        }

        throw new InvalidOperationException("Could not map the primary span to the generated code.");
    }

    public async Task<(SourceGeneratedDocument generatedDocument, SourceText razorSourceText, TextSpan primarySpan, TextSpan generatedSpan)> InitializeAsync(string razorSource, CancellationToken cancellationToken)
    {
        var (razorSourceText, primarySpan) = CreateText(razorSource);
        var (primary, generatedDocument) = await InitializeDocumentAsync(razorSourceText);
        var generatedSpan = await GetSecondarySpanAsync(primary, primarySpan, generatedDocument, cancellationToken);
        return (generatedDocument, razorSourceText, primarySpan, generatedSpan);
    }

    internal async Task<(IDocumentSnapshot primary, SourceGeneratedDocument generatedDocument, TextSpan generatedSpan)> InitializeWithSnapshotAsync(string razorSource, CancellationToken cancellationToken)
    {
        var (razorSourceText, primarySpan) = CreateText(razorSource);
        var (primary, generatedDocument) = await InitializeDocumentAsync(razorSourceText);
        var generatedSpan = await GetSecondarySpanAsync(primary, primarySpan, generatedDocument, cancellationToken);
        return (primary, generatedDocument, generatedSpan);
    }
}
