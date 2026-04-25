// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion.Delegation;

/// <summary>
///  Removes Razor design-time helpers from a C# completion list.
/// </summary>
internal class DesignTimeHelperResponseRewriter : IDelegatedCSharpCompletionResponseRewriter
{
    private static readonly FrozenSet<string> s_designTimeHelpers = new[]
    {
        "__builder",
        "__o",
        "__RazorDirectiveTokenHelpers__",
        "__tagHelperExecutionContext",
        "__tagHelperRunner",
        "__typeHelper",
        "_Imports",
        "BuildRenderTree"
    }.ToFrozenSet();

    public RazorVSInternalCompletionList Rewrite(
        RazorVSInternalCompletionList completionList,
        RazorCodeDocument codeDocument,
        int hostDocumentIndex,
        Position projectedPosition,
        RazorCompletionOptions completionOptions)
    {
        var owner = codeDocument
            .GetRequiredSyntaxRoot()
            .FindInnermostNode(hostDocumentIndex);

        if (owner is null)
        {
            Debug.Fail("Owner should never be null.");
            return completionList;
        }

        var sourceText = codeDocument.Source.Text;

        // We should remove Razor design-time helpers from C#'s completion list. If the current identifier
        // being targeted does not start with a double underscore, we trim out all items starting with "__"
        // from the completion list. If the current identifier does start with a double underscore (e.g. "__ab[||]"),
        // we only trim out common design time helpers from the completion list.

        using var _ = ListPool<VSInternalCompletionItem>.GetPooledObject(out var filteredItems);

        var items = completionList.Items;
        filteredItems.SetCapacityIfLarger(items.Length);

        // If the current identifier doesn't start with "__", we remove common design-time helpers *and*
        // any item starting with "__" from the completion list. Otherwise, we only remove the common
        // design-time helpers.
        var removeAllDoubleUnderscoreItems = !StartsWithDoubleUnderscore(owner, sourceText);

        foreach (var item in items)
        {
            if (s_designTimeHelpers.Contains(item.Label) || (removeAllDoubleUnderscoreItems && item.Label.StartsWith("__")))
            {
                continue;
            }

            filteredItems.Add(item);
        }

        // Avoid allocating array if nothing was filtered.
        if (items.Length != filteredItems.Count)
        {
            completionList.Items = filteredItems.ToArray();
        }

        return completionList;
    }

    private static bool StartsWithDoubleUnderscore(RazorSyntaxNode owner, SourceText sourceText)
    {
        var span = owner.Span;
        if (span.Length < 2)
        {
            return false;
        }

        var start = span.Start;
        return sourceText[start] == '_' || sourceText[start + 1] == '_';
    }
}
