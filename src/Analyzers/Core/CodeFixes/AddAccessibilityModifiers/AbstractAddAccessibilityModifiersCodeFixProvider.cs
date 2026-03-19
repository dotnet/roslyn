// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddOrRemoveAccessibilityModifiers;

internal abstract class AbstractAddOrRemoveAccessibilityModifiersCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    protected abstract SyntaxNode MapToDeclarator(SyntaxNode declaration);

    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.AddOrRemoveAccessibilityModifiersDiagnosticId];

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();

        var priority = diagnostic.Severity == DiagnosticSeverity.Hidden
            ? CodeActionPriority.Low
            : CodeActionPriority.Default;

        var (title, key) = diagnostic.Properties.ContainsKey(AddOrRemoveAccessibilityModifiersConstants.ModifiersAdded)
            ? (AnalyzersResources.Add_accessibility_modifiers, nameof(AnalyzersResources.Add_accessibility_modifiers))
            : (AnalyzersResources.Remove_accessibility_modifiers, nameof(AnalyzersResources.Remove_accessibility_modifiers));

        RegisterCodeFix(context, title, key, priority);
    }

    protected sealed override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in diagnostics)
        {
            var declaration = diagnostic.AdditionalLocations[0].FindNode(cancellationToken);
            var declarator = MapToDeclarator(declaration);
            var symbol = semanticModel.GetDeclaredSymbol(declarator, cancellationToken);
            Contract.ThrowIfNull(symbol);
            AddOrRemoveAccessibilityModifiersHelpers.UpdateDeclaration(editor, symbol, declaration);
        }
    }
}
