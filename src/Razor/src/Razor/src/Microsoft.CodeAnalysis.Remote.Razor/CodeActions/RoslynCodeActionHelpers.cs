// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

[Export(typeof(IRoslynCodeActionHelpers)), Shared]
internal sealed class RoslynCodeActionHelpers : IRoslynCodeActionHelpers
{
    public Task<string> GetFormattedNewFileContentsAsync(IProjectSnapshot projectSnapshot, Uri csharpFileUri, string newFileContent, CancellationToken cancellationToken)
    {
        Debug.Assert(projectSnapshot is RemoteProjectSnapshot);
        var project = ((RemoteProjectSnapshot)projectSnapshot).Project;

        var filePath = csharpFileUri.GetDocumentFilePathFromUri();
        var source = SourceText.From(newFileContent, Encoding.UTF8, checksumAlgorithm: SourceHashAlgorithm.Sha256);
        var document = project.AddDocument(filePath, source, filePath: filePath);

        return GetFormattedNewFileContentAsync(document, cancellationToken);
    }

    public async Task<TextEdit[]?> GetSimplifiedTextEditsAsync(RemoteDocumentContext documentContext, Uri? codeBehindUri, TextEdit edit, CancellationToken cancellationToken)
    {
        Document document;
        if (codeBehindUri is null)
        {
            // Edit is for inserting into the generated document
            document = await documentContext.Snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Edit is for inserting into a C# document
            var solution = documentContext.TextDocument.Project.Solution;
            var documentIds = solution.GetDocumentIdsWithUri(codeBehindUri);
            if (documentIds.Length == 0)
            {
                return null;
            }

            document = solution.GetRequiredDocument(documentIds.First(d => d.ProjectId == documentContext.TextDocument.Project.Id));
        }

        return await GetSimplifiedEditsAsync(document, edit, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> GetFormattedNewFileContentAsync(Document document, CancellationToken cancellationToken)
    {
        var project = document.Project;
        // Run the new document formatting service, to make sure the right namespace type is used, among other things
        var formattingService = document.GetLanguageService<INewDocumentFormattingService>();
        if (formattingService is not null)
        {
            var hintDocument = project.Documents.FirstOrDefault();
            var cleanupOptions = await document.GetCodeCleanupOptionsAsync(cancellationToken).ConfigureAwait(false);
            document = await formattingService.FormatNewDocumentAsync(document, hintDocument, cleanupOptions, cancellationToken).ConfigureAwait(false);
        }

        // Unlike normal new file formatting, Razor also wants to remove unnecessary usings
        var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var removeImportsService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
        if (removeImportsService is not null)
        {
            document = await removeImportsService.RemoveUnnecessaryImportsAsync(document, cancellationToken).ConfigureAwait(false);
        }

        // Now format the document so indentation etc. is correct
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        root = Formatter.Format(root, project.Solution.Services, syntaxFormattingOptions, cancellationToken);

        return root.ToFullString();
    }

    private static async Task<TextEdit[]> GetSimplifiedEditsAsync(Document document, TextEdit textEdit, CancellationToken cancellationToken)
    {
        // Create a temporary syntax tree that includes the text edit.
        var originalSourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var pendingChange = ProtocolConversions.TextEditToTextChange(textEdit, originalSourceText);
        var newSourceText = originalSourceText.WithChanges(pendingChange);
        var originalTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var newTree = originalTree.WithChangedText(newSourceText);

        // Find the node that represents the text edit in the new syntax tree and annotate it for the simplifier.
        // Then create a document with a new syntax tree that has the annotated node.
        var node = newTree.FindNode(pendingChange.Span, findInTrivia: false, getInnermostNodeForTie: false, cancellationToken);
        var annotatedSyntaxRoot = newTree.GetRoot(cancellationToken).ReplaceNode(node, node.WithAdditionalAnnotations(Simplifier.Annotation));
        var annotatedDocument = document.WithSyntaxRoot(annotatedSyntaxRoot);

        // Call to the Simplifier and pass back the edits.
        var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        var simplificationService = document.Project.Services.GetRequiredService<ISimplificationService>();
        var options = simplificationService.GetSimplifierOptions(configOptions);
        var newDocument = await Simplifier.ReduceAsync(annotatedDocument, options, cancellationToken).ConfigureAwait(false);
        var changes = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
        return [.. changes.Select(change => ProtocolConversions.TextChangeToTextEdit(change, originalSourceText))];
    }
}
