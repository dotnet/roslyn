// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(IRazorCodeActionResolver)), Shared]
internal sealed class AddUsingsCodeActionResolver : IRazorCodeActionResolver
{
    public string Action => LanguageServerConstants.CodeActions.AddUsing;

    public async Task<WorkspaceEdit?> ResolveAsync(RemoteDocumentContext documentContext, JsonElement data, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        var actionParams = data.Deserialize<AddUsingsCodeActionParams>();
        if (actionParams is null)
        {
            return null;
        }

        var documentSnapshot = documentContext.Snapshot;

        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        var codeDocumentIdentifier = new OptionalVersionedTextDocumentIdentifier() { DocumentUri = documentContext.Uri };

        using var documentChanges = new PooledArrayBuilder<TextDocumentEdit>();

        // Need to add the additional edit first, as the actual usings go at the top of the file, and would
        // change the ranges needed in the additional edit if they went in first
        if (actionParams.AdditionalEdit is not null)
        {
            documentChanges.Add(actionParams.AdditionalEdit);
        }

        documentChanges.Add(new TextDocumentEdit()
        {
            TextDocument = codeDocumentIdentifier,
            Edits = [UsingDirectiveHelper.CreateAddUsingTextEdit(actionParams.Namespace, codeDocument)]
        });

        return new WorkspaceEdit()
        {
            DocumentChanges = documentChanges.ToArray(),
        };
    }

    internal static bool TryCreateAddUsingResolutionParams(string fullyQualifiedName, VSTextDocumentIdentifier textDocument, TextDocumentEdit? additionalEdit, Uri? delegatedDocumentUri, [NotNullWhen(true)] out string? @namespace, [NotNullWhen(true)] out RazorCodeActionResolutionParams? resolutionParams)
    {
        @namespace = GetNamespaceFromFQN(fullyQualifiedName);
        if (string.IsNullOrEmpty(@namespace))
        {
            @namespace = null;
            resolutionParams = null;
            return false;
        }

        resolutionParams = CreateAddUsingResolutionParams(@namespace, textDocument, additionalEdit, delegatedDocumentUri);
        return true;
    }

    internal static RazorCodeActionResolutionParams CreateAddUsingResolutionParams(string @namespace, VSTextDocumentIdentifier textDocument, TextDocumentEdit? additionalEdit, Uri? delegatedDocumentUri)
    {
        var actionParams = new AddUsingsCodeActionParams
        {
            Namespace = @namespace,
            AdditionalEdit = additionalEdit
        };

        return new RazorCodeActionResolutionParams(textDocument)
        {
            Action = LanguageServerConstants.CodeActions.AddUsing,
            Language = RazorLanguageKind.Razor,
            DelegatedDocumentUri = delegatedDocumentUri,
            Data = actionParams,
        };
    }

    // Internal for testing
    internal static string GetNamespaceFromFQN(string fullyQualifiedName)
    {
        if (!TrySplitNamespaceAndType(fullyQualifiedName.AsSpan(), out var namespaceName, out _))
        {
            return string.Empty;
        }

        return namespaceName.ToString();
    }

    private static bool TrySplitNamespaceAndType(ReadOnlySpan<char> fullTypeName, out ReadOnlySpan<char> @namespace, out ReadOnlySpan<char> typeName)
    {
        @namespace = default;
        typeName = default;

        if (fullTypeName.IsEmpty)
        {
            return false;
        }

        var nestingLevel = 0;
        var splitLocation = -1;
        for (var i = fullTypeName.Length - 1; i >= 0; i--)
        {
            var c = fullTypeName[i];
            if (c == Type.Delimiter && nestingLevel == 0)
            {
                splitLocation = i;
                break;
            }
            else if (c == '>')
            {
                nestingLevel++;
            }
            else if (c == '<')
            {
                nestingLevel--;
            }
        }

        if (splitLocation == -1)
        {
            typeName = fullTypeName;
            return true;
        }

        @namespace = fullTypeName[..splitLocation];

        var typeNameStartLocation = splitLocation + 1;
        if (typeNameStartLocation < fullTypeName.Length)
        {
            typeName = fullTypeName[typeNameStartLocation..];
        }

        return true;
    }
}
