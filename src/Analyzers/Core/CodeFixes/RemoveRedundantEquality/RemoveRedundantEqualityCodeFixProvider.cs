// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.RemoveRedundantEquality;

[ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.RemoveRedundantEquality), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class RemoveRedundantEqualityCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.RemoveRedundantEqualityDiagnosticId);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            RegisterCodeFix(context, AnalyzersResources.Remove_redundant_equality, nameof(AnalyzersResources.Remove_redundant_equality), diagnostic);
        }

        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in diagnostics)
        {
            var node = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);

            editor.ReplaceNode(node, (n, _) =>
            {
                if (!syntaxFacts.IsBinaryExpression(n))
                {
                    // This should happen only in error cases.
                    return n;
                }

                syntaxFacts.GetPartsOfBinaryExpression(n, out var left, out var right);
                if (diagnostic.Properties[RedundantEqualityConstants.RedundantSide] == RedundantEqualityConstants.Right)
                {
                    return WithElasticTrailingTrivia(left);
                }
                else if (diagnostic.Properties[RedundantEqualityConstants.RedundantSide] == RedundantEqualityConstants.Left)
                {
                    return WithElasticTrailingTrivia(right);
                }

                return n;
            });
        }

        return;

        static SyntaxNode WithElasticTrailingTrivia(SyntaxNode node)
        {
            return node.WithTrailingTrivia(node.GetTrailingTrivia().Select(SyntaxTriviaExtensions.AsElastic));
        }
    }
}
