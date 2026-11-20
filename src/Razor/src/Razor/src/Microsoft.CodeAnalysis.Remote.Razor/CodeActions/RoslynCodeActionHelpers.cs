// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal static class RoslynCodeActionHelpers
{
    public static async Task<string> GetFormattedNewFileContentsAsync(Project project, string csharpFilePath, string newFileContent, CancellationToken cancellationToken)
    {
        var source = SourceText.From(newFileContent, Encoding.UTF8, checksumAlgorithm: SourceHashAlgorithm.Sha256);
        var document = project.AddDocument(csharpFilePath, source, filePath: csharpFilePath);

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

    public static async Task<SumType<TextEdit, AnnotatedTextEdit>[]> GetSimplifiedEditsAsync(Document document, TextEdit textEdit, CancellationToken cancellationToken)
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
        return [.. changes.Select(change => new SumType<TextEdit, AnnotatedTextEdit>(ProtocolConversions.TextChangeToTextEdit(change, originalSourceText)))];
    }
}
