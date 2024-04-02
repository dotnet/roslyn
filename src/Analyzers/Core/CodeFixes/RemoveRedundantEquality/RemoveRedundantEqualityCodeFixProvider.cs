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
internal sealed class RemoveRedundantEqualityCodeFixProvider() : ForkingSyntaxEditorBasedCodeFixProvider<SyntaxNode>
{
    public override ImmutableArray<string> FixableDiagnosticIds => [IDEDiagnosticIds.RemoveRedundantEqualityDiagnosticId];

    protected override (string title, string equivalenceKey) GetTitleAndEquivalenceKey(CodeFixContext context)
        => (AnalyzersResources.Remove_redundant_equality, nameof(AnalyzersResources.Remove_redundant_equality));

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        SyntaxNode node,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var generatorInternal = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        editor.ReplaceNode(node, WithElasticTrailingTrivia(RewriteNode()));

        return;

        SyntaxNode RewriteNode()
        {
            // This should happen only in error cases.
            if (!syntaxFacts.IsBinaryExpression(node))
                return node;

            syntaxFacts.GetPartsOfBinaryExpression(node, out var left, out var right);
            var rewritten =
                properties[RedundantEqualityConstants.RedundantSide] == RedundantEqualityConstants.Right ? left :
                properties[RedundantEqualityConstants.RedundantSide] == RedundantEqualityConstants.Left ? right : node;

            if (properties.ContainsKey(RedundantEqualityConstants.Negate))
                rewritten = generator.Negate(generatorInternal, rewritten, semanticModel, cancellationToken);

            return rewritten;
        }

        static SyntaxNode WithElasticTrailingTrivia(SyntaxNode node)
        {
            return node.WithTrailingTrivia(node.GetTrailingTrivia().Select(SyntaxTriviaExtensions.AsElastic));
        }
    }
}
