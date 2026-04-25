// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Razor;

internal class SimplifyFullyQualifiedComponentCodeActionResolver : IRazorCodeActionResolver
{
    public string Action => LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent;

    public async Task<WorkspaceEdit?> ResolveAsync(DocumentContext documentContext, JsonElement data, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        if (data.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var actionParams = JsonSerializer.Deserialize<SimplifyFullyQualifiedComponentCodeActionParams>(data.GetRawText());
        if (actionParams is null)
        {
            return null;
        }

        var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var text = codeDocument.Source.Text;

        var codeDocumentIdentifier = new OptionalVersionedTextDocumentIdentifier() { DocumentUri = new(documentContext.Uri) };

        // Check if we need to add a using directive.
        // We check the tag helpers available in the document to see if the simple component name
        // can already be used without qualification. This would be the case if the namespace is
        // already imported via a @using directive in this file or an _Imports.razor file.
        var needsUsing = true;
        var tagHelpers = codeDocument.GetRequiredTagHelperContext().TagHelpers;

        // Look through all tag helpers to find one that matches our component and can be used
        // with the simple (non-fully-qualified) name. The presence of such a tag helper indicates
        // that the namespace is already in scope.
        foreach (var tagHelper in tagHelpers)
        {
            // We need a component tag helper that:
            // 1. Is not a fully qualified name match (can be used with simple name)
            // 2. Would match the unqualified tag name we'll be transforming to
            // 3. Is from the same namespace we would add a using for
            if (tagHelper.Kind == TagHelperKind.Component &&
                !tagHelper.IsFullyQualifiedNameMatch &&
                tagHelper.TagMatchingRules is [{ TagName: { } matchingTagName }] &&
                matchingTagName == actionParams.ComponentName &&
                tagHelper.TypeNamespace == actionParams.Namespace)
            {
                // Found it - the namespace is already in scope
                needsUsing = false;
                break;
            }
        }

        // Build the tag simplification edits (at the original positions in the document)
        // No capacity needed - tagEdits will never contain more than 3 elements (start tag, end tag, and using directive)
        using var tagEdits = new PooledArrayBuilder<SumType<TextEdit, AnnotatedTextEdit>>();

        // Replace the fully qualified name with the simple component name in end tag first (if it exists)
        // The end tag edit must come before the start tag edit, as clients may not re-order them
        if (actionParams.EndTagSpanStart >= 0 && actionParams.EndTagSpanEnd >= 0)
        {
            var endTagRange = text.GetRange(actionParams.EndTagSpanStart, actionParams.EndTagSpanEnd);
            tagEdits.Add(new TextEdit
            {
                NewText = actionParams.ComponentName,
                Range = endTagRange,
            });
        }

        // Replace the fully qualified name with the simple component name in start tag
        var startTagRange = text.GetRange(actionParams.StartTagSpanStart, actionParams.StartTagSpanEnd);
        tagEdits.Add(new TextEdit
        {
            NewText = actionParams.ComponentName,
            Range = startTagRange,
        });

        // Add using directive if needed (at the top of the file)
        // This must come after the tag edits because the using directive will be inserted at the top,
        // which would change line numbers for subsequent edits
        if (needsUsing)
        {
            var addUsingEdit = UsingDirectiveHelper.CreateAddUsingTextEdit(actionParams.Namespace, codeDocument);
            tagEdits.Add(addUsingEdit);
        }

        return new WorkspaceEdit()
        {
            DocumentChanges = new TextDocumentEdit[]
            {
                new TextDocumentEdit()
                {
                    TextDocument = codeDocumentIdentifier,
                    Edits = tagEdits.ToArray()
                }
            }
        };
    }
}
