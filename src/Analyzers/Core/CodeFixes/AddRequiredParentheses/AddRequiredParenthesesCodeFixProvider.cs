// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddRequiredParentheses;

[ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.AddRequiredParentheses), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class AddRequiredParenthesesCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId];

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic, Document document, string? equivalenceKey, CancellationToken cancellationToken)
        => diagnostic.Properties.ContainsKey(AddRequiredParenthesesConstants.IncludeInFixAll) &&
           diagnostic.Properties[AddRequiredParenthesesConstants.EquivalenceKey] == equivalenceKey;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var firstDiagnostic = context.Diagnostics[0];
        context.RegisterCodeFix(
            CodeAction.Create(
                AnalyzersResources.Add_parentheses_for_clarity,
                GetDocumentUpdater(context),
                firstDiagnostic.Properties[AddRequiredParenthesesConstants.EquivalenceKey]!),
            context.Diagnostics);
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var generator = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();

        foreach (var diagnostic in diagnostics)
        {
            var location = diagnostic.AdditionalLocations[0];
            var node = location.FindNode(findInsideTrivia: true, getInnermostNodeForTie: true, cancellationToken);

            // Do not add the simplifier annotation.  We do not want the simplifier undoing the 
            // work we just did.
            editor.ReplaceNode(node,
                (current, _) => generator.AddParentheses(
                    current, includeElasticTrivia: false, addSimplifierAnnotation: false));
        }

        return Task.CompletedTask;
    }
}
