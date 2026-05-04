// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Razor;

internal abstract class GenerateEventHandlerCodeActionResolver(
    IRoslynCodeActionHelpers roslynCodeActionHelpers,
    IRazorFormattingService razorFormattingService) : IRazorCodeActionResolver
{
    private readonly IRoslynCodeActionHelpers _roslynCodeActionHelpers = roslynCodeActionHelpers;
    private readonly IRazorFormattingService _razorFormattingService = razorFormattingService;

    public string Action => LanguageServerConstants.CodeActions.GenerateEventHandler;

    public async Task<WorkspaceEdit?> ResolveAsync(DocumentContext documentContext, JsonElement data, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        var actionParams = data.Deserialize<GenerateEventHandlerCodeActionParams>();
        if (actionParams is null)
        {
            return null;
        }

        var code = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var razorFilePath = documentContext.Uri.GetDocumentFilePath();
        var razorClassName = Path.GetFileNameWithoutExtension(razorFilePath);
        var codeBehindPath = $"{razorFilePath}.cs";

        // If we can't get the namespace, or the syntax tree (possibly because the file doesn't exist), or if the file does exist
        // but doesn't have the class declaration we'd expect, then we don't risk it and just generate a code block.
        if (!code.TryGetNamespace(fallbackToRootNamespace: true, out var razorNamespace) ||
            await GetCodeBehindSyntaxTreeAsync(documentContext, codeBehindPath, cancellationToken).ConfigureAwait(false) is not { } syntaxTree ||
            GetCSharpClassDeclarationSyntax(syntaxTree, razorNamespace, razorClassName) is not { } classDecl)
        {
            return await GenerateEventHandlerInCodeBlockAsync(
                code,
                actionParams,
                documentContext,
                options,
                cancellationToken).ConfigureAwait(false);
        }

        var codeBehindUri = LspFactory.CreateFilePathUri(codeBehindPath);

        var codeBehindTextDocumentIdentifier = new OptionalVersionedTextDocumentIdentifier() { DocumentUri = new(codeBehindUri) };

        var classLocationLineSpan = classDecl.GetLocation().GetLineSpan();
        var text = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
        // We use the class declarations indentation, plus one level, as the base indent for the new method
        var baseIndentation = text.Lines[classLocationLineSpan.StartLinePosition.Line].GetIndentationSize(options.TabSize) + options.TabSize;
        var eventHandler = GetEventHandler(actionParams, options, baseIndentation);

        var edit = LspFactory.CreateTextEdit(
            line: classLocationLineSpan.EndLinePosition.Line,
            character: 0,
            eventHandler);

        var result = await _roslynCodeActionHelpers.GetSimplifiedTextEditsAsync(documentContext, codeBehindUri, edit, cancellationToken).ConfigureAwait(false);

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
        DocumentContext documentContext,
        RazorFormattingOptions options,
        CancellationToken cancellationToken)
    {
        var csharpSyntaxTree = await documentContext.Snapshot.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
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

        // Call the simplifier to reduce things like `global::System.Threading.Task` down to just `Task`, if possible.
        var result = await _roslynCodeActionHelpers.GetSimplifiedTextEditsAsync(documentContext, codeBehindUri: null, tempTextEdit, cancellationToken).ConfigureAwait(false);
        if (result is not { } edits)
        {
            return null;
        }

        // Now we run the changes through the formatter, via the TryGetCSharpCodeActionEditAsync. This is the same as what the CSharpCodeActionResolver does, and we're essentially
        // pretending to be a C# code action, so our new method ends up going through the same pipeline as the Roslyn Generate Method code action. That's the magic that knows how
        // to create a code block if one doesn't exist, or put the method in an existing one, and it will also ensure the method gets properly formatted.
        var csharpSourceText = code.GetCSharpSourceText();
        var csharpTextChanges = edits.SelectAsArray(csharpSourceText.GetTextChange);
        var formattedChange = await _razorFormattingService.TryGetCSharpCodeActionEditAsync(documentContext, csharpTextChanges, options, cancellationToken).ConfigureAwait(false);
        if (formattedChange is not { } razorChange)
        {
            return null;
        }

        return new WorkspaceEdit()
        {
            DocumentChanges = new[] {
                new TextDocumentEdit()
                {
                    TextDocument = new OptionalVersionedTextDocumentIdentifier() { DocumentUri = new(documentContext.Uri) },
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

    protected abstract Task<SyntaxTree?> GetCodeBehindSyntaxTreeAsync(DocumentContext documentContext, string codeBehindPath, CancellationToken cancellationToken);

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
