// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class CreateComponentCodeActionResolver(LanguageServerFeatureOptions languageServerFeatureOptions) : IRazorCodeActionResolver
{
    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions = languageServerFeatureOptions;

    public string Action => LanguageServerConstants.CodeActions.CreateComponentFromTag;

    public async Task<WorkspaceEdit?> ResolveAsync(DocumentContext documentContext, JsonElement data, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        var actionParams = data.Deserialize<CreateComponentCodeActionParams>();
        if (actionParams is null)
        {
            return null;
        }

        if (!documentContext.Snapshot.FileKind.IsComponent())
        {
            return null;
        }

        var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var newComponentUri = LspFactory.CreateFilePathUri(actionParams.Path, _languageServerFeatureOptions);

        using var documentChanges = new PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>();
        documentChanges.Add(new CreateFile() { DocumentUri = new(newComponentUri) });

        TryAddNamespaceDirective(codeDocument, newComponentUri, ref documentChanges.AsRef());

        return new WorkspaceEdit()
        {
            DocumentChanges = documentChanges.ToArray(),
        };
    }

    private static void TryAddNamespaceDirective(RazorCodeDocument codeDocument, Uri newComponentUri, ref PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>> documentChanges)
    {
        var syntaxRoot = codeDocument.GetRequiredSyntaxRoot();
        var namespaceDirective = syntaxRoot.DescendantNodes()
            .OfType<RazorDirectiveSyntax>()
            .FirstOrDefault(static n => n.IsDirective(NamespaceDirective.Directive));

        if (namespaceDirective != null)
        {
            var documentIdentifier = new OptionalVersionedTextDocumentIdentifier { DocumentUri = new(newComponentUri) };
            documentChanges.Add(new TextDocumentEdit
            {
                TextDocument = documentIdentifier,
                Edits = [LspFactory.CreateTextEdit(position: (0, 0), namespaceDirective.GetContent())]
            });
        }
    }
}
