// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.UseCollectionExpression;

internal abstract class AbstractUseCollectionExpressionCodeFixProvider<TExpressionSyntax>(string title, string equivalenceKey)
    : ForkingSyntaxEditorBasedCodeFixProvider<TExpressionSyntax>
    where TExpressionSyntax : SyntaxNode
{
    private readonly string _title = title;
    private readonly string _titleChangesSemantics = string.Format(CodeFixesResources._0_may_change_semantics, title);
    private readonly string _equivalenceKey = equivalenceKey;
    private readonly string _equivalenceKeyChangesSemantics = equivalenceKey + "_ChangesSemantics";

    protected override (string title, string equivalenceKey) GetTitleAndEquivalenceKey(CodeFixContext context)
    {
        var changesSemantics = UseCollectionInitializerHelpers.ChangesSemantics(context.Diagnostics[0]);
        return changesSemantics ? (_titleChangesSemantics, _equivalenceKeyChangesSemantics) : (_title, _equivalenceKey);
    }

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic, Document document, string? equivalenceKey, CancellationToken cancellationToken)
    {
        // Never try to fix the secondary diagnostics that were produced just to fade out code.
        if (diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary))
            return false;

        // If we're allowing the changing of semantics, then we can fix any diagnostic.  If we're not allowing the
        // changing of semantics, then we can only fixup diagnostics that don't change semantics.
        if (equivalenceKey == _equivalenceKeyChangesSemantics)
            return true;

        return !UseCollectionInitializerHelpers.ChangesSemantics(diagnostic);
    }
}
