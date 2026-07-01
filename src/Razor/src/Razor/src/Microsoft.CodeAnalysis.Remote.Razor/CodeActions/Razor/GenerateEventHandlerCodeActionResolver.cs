// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.Formatting;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(IRazorCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class GenerateEventHandlerCodeActionResolver(
    IRazorFormattingService razorFormattingService,
    RemoteSnapshotManager snapshotManager) : IRazorCodeActionResolver
{
    private readonly IRazorFormattingService _razorFormattingService = razorFormattingService;
    private readonly RemoteSnapshotManager _snapshotManager = snapshotManager;

    public string Action => LanguageServerConstants.CodeActions.GenerateEventHandler;

    public async Task<WorkspaceEdit?> ResolveAsync(RemoteDocumentSnapshot documentSnapshot, JsonElement data, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        var actionParams = data.Deserialize<GenerateEventHandlerCodeActionParams>();
        if (actionParams is null)
        {
            return null;
        }

        var code = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        var razorFilePath = documentSnapshot.Uri.GetDocumentFilePathFromUri();
        var razorClassName = Path.GetFileNameWithoutExtension(razorFilePath);
        var codeBehindPath = $"{razorFilePath}.cs";

        // If we can't get the namespace, or the syntax tree (possibly because the file doesn't exist), or if the file does exist
        // but doesn't have the class declaration we'd expect, then we don't risk it and just generate a code block.
        if (!code.TryGetNamespace(fallbackToRootNamespace: true, out var razorNamespace) ||
            !TryGetCodeBehindDocument(documentSnapshot, codeBehindPath, out var codeBehindDocument) ||
            await codeBehindDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false) is not { } syntaxTree ||
            GetCSharpClassDeclarationSyntax(syntaxTree, razorNamespace, razorClassName) is not { } classDecl)
        {
            return await GenerateEventHandlerInCodeBlockAsync(
                code,
                actionParams,
                documentSnapshot,
                options,
                cancellationToken).ConfigureAwait(false);
        }

        var classLocationLineSpan = classDecl.GetLocation().GetLineSpan();
        var text = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
        // We use the class declarations indentation, plus one level, as the base indent for the new method
        var baseIndentation = text.Lines[classLocationLineSpan.StartLinePosition.Line].GetIndentationSize(options.TabSize) + options.TabSize;
        var eventHandler = GetEventHandler(actionParams, options, baseIndentation);

        var edit = LspFactory.CreateTextEdit(
            line: classLocationLineSpan.EndLinePosition.Line,
            character: 0,
            eventHandler);

        var result = await RoslynCodeActionHelpers.GetSimplifiedEditsAsync(codeBehindDocument, edit, cancellationToken).ConfigureAwait(false);

        var codeBehindTextDocumentIdentifier = new OptionalVersionedTextDocumentIdentifier() { DocumentUri = codeBehindDocument.GetURI() };
        var codeBehindTextDocEdit = new TextDocumentEdit()
        {
            TextDocument = codeBehindTextDocumentIdentifier,
            Edits = [.. result ?? [edit]]
        };

        return new WorkspaceEdit() { DocumentChanges = new[] { codeBehindTextDocEdit } };
    }

    private async Task<WorkspaceEdit?> GenerateEventHandlerInCodeBlockAsync(
        RazorCodeDocument code,
        GenerateEventHandlerCodeActionParams actionParams,
        RemoteDocumentSnapshot documentSnapshot,
        RazorFormattingOptions options,
        CancellationToken cancellationToken)
    {
        // We're essentially pretending to be a C# code action, so our new method ends up going through the same pipeline as the Roslyn Generate Method code
        // action. That's the magic that knows how to create a code block if one doesn't exist, or put the method in an existing one, and it will also ensure
        // the method gets properly formatted. That means it doesn't matter which C# document we use for the edit, so we'll just use the impl document since
        // it always exists.
        var csharpSyntaxTree = await documentSnapshot.GetCSharpSyntaxTreeAsync(declarationDocument: false, cancellationToken).ConfigureAwait(false);
        var csharpSyntaxRoot = await csharpSyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        if (!csharpSyntaxRoot.TryGetClassDeclaration(out var classDecl))
        {
            return null;
        }

        // We are going to arbitrarily place the method at the end of the class in the generated C# file. The formatting service will fix indentation, and put it in an appropriate
        // place in the Razor file, so we can just use 0 as the base.
        var eventHandler = GetEventHandler(actionParams, options, baseIndentation: 0);
        var classLocationLineSpan = classDecl.CloseBraceToken.GetLocation().GetLineSpan();
        var tempTextEdit = LspFactory.CreateTextEdit(
            line: classLocationLineSpan.StartLinePosition.Line,
            character: 0,
            eventHandler);

        var generatedDocument = await documentSnapshot.GetGeneratedDocumentAsync(declarationDocument: false, cancellationToken).ConfigureAwait(false);

        // Call the simplifier to reduce things like `global::System.Threading.Task` down to just `Task`, if possible.
        var result = await RoslynCodeActionHelpers.GetSimplifiedEditsAsync(generatedDocument, tempTextEdit, cancellationToken).ConfigureAwait(false);
        if (result is not { } edits)
        {
            return null;
        }

        // Now we run the changes through the formatter, via the TryGetCSharpCodeActionEditAsync. This is the same as what the CSharpCodeActionResolver does.
        var csharpDocument = code.GetRequiredCSharpDocument(declarationDocument: false);
        var csharpTextChanges = edits.SelectAsArray(csharpDocument.Text.GetTextChange);
        var formattedChange = await _razorFormattingService.TryGetCSharpCodeActionEditAsync(documentSnapshot, csharpTextChanges,
            declarationDocument: false,
            options, cancellationToken).ConfigureAwait(false);
        if (formattedChange is not { } razorChange)
        {
            return null;
        }

        return new WorkspaceEdit()
        {
            DocumentChanges = new[] {
                new TextDocumentEdit()
                {
                    TextDocument = new OptionalVersionedTextDocumentIdentifier() { DocumentUri = documentSnapshot.Uri },
                    Edits = [code.Source.Text.GetTextEdit(razorChange)],
                }
            }
        };
    }

    private static string GetEventHandler(
        GenerateEventHandlerCodeActionParams actionParams,
        RazorFormattingOptions options,
        int baseIndentation)
    {
        var returnType = actionParams.IsAsync
            ? "global::System.Threading.Tasks.Task"
            : "void";

        var parameters = actionParams.EventParameterType is null
            ? string.Empty // Couldn't find the params, generate no params instead.
            : $"global::{actionParams.EventParameterType} args";

        var eventHandlerIndentation = FormattingUtilities.GetIndentationString(baseIndentation, options.InsertSpaces, options.TabSize);
        var eventHandlerBodyIndentation = FormattingUtilities.GetIndentationString(baseIndentation + options.TabSize, options.InsertSpaces, options.TabSize);

        return $$"""
            {{eventHandlerIndentation}}private {{returnType}} {{actionParams.MethodName}}({{parameters}})
            {{eventHandlerIndentation}}{
            {{eventHandlerBodyIndentation}}throw new global::System.NotImplementedException();
            {{eventHandlerIndentation}}}

            """;
    }

    private bool TryGetCodeBehindDocument(RemoteDocumentSnapshot documentSnapshot, string codeBehindPath, [NotNullWhen(true)] out Document? document)
    {
        document = null;
        var razorDocumentSnapshot = _snapshotManager.GetSnapshot(documentSnapshot.TextDocument);
        var solution = razorDocumentSnapshot.TextDocument.Project.Solution;
        var projectId = razorDocumentSnapshot.TextDocument.Project.Id;

        if (solution.GetDocumentIdsWithFilePath(codeBehindPath).FirstOrDefault(id => id.ProjectId == projectId) is not { } codeBehindDocumentId)
        {
            return false;
        }

        if (!solution.TryGetDocument(codeBehindDocumentId, out var codeBehindDocument))
        {
            return false;
        }

        document = codeBehindDocument;
        return true;
    }

    private static ClassDeclarationSyntax? GetCSharpClassDeclarationSyntax(SyntaxTree syntaxTree, string razorNamespace, string razorClassName)
    {
        var compilationUnit = syntaxTree.GetCompilationUnitRoot();
        var @namespace = compilationUnit.Members
            .FirstOrDefault(m => m is BaseNamespaceDeclarationSyntax { } @namespace && @namespace.Name.ToString() == razorNamespace);
        if (@namespace is null)
        {
            return null;
        }

        var @class = ((BaseNamespaceDeclarationSyntax)@namespace).Members
            .FirstOrDefault(m => m is ClassDeclarationSyntax { } @class && razorClassName == @class.Identifier.Text);
        if (@class is null)
        {
            return null;
        }

        return (ClassDeclarationSyntax)@class;
    }
}
