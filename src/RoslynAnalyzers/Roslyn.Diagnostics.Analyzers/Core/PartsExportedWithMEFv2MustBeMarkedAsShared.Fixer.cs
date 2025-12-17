// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Roslyn.Diagnostics.Analyzers
{
    /// <summary>
    /// RS0023: Parts exported with MEFv2 must be marked as Shared
    /// </summary>
    public abstract class PartsExportedWithMEFv2MustBeMarkedAsSharedFixer<TTypeSyntax> : CodeFixProvider
        where TTypeSyntax : SyntaxNode
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(RoslynDiagnosticIds.MissingSharedAttributeRuleId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                // The following doesn't seem to work correctly due to a possible Roslyn bug.
                //var symbol = semanticModel!.GetEnclosingSymbol(diagnostic.Location.SourceSpan.Start);
                //if (symbol.DeclaringSyntaxReferences.IsEmpty)
                //{
                //    continue;
                //}
                //var declaration = await symbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

                if (TryGetDeclaration(root!.FindNode(diagnostic.Location.SourceSpan), out var declaration))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            RoslynDiagnosticsAnalyzersResources.AddSharedAttribute,
                            _ => AddSharedAttributeAsync(document, root!, declaration),
                            equivalenceKey: nameof(RoslynDiagnosticsAnalyzersResources.AddSharedAttribute)),
                        diagnostic);
                }
            }
        }

        private static bool TryGetDeclaration(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? declaration)
            => (declaration = node.FirstAncestorOrSelf<TTypeSyntax>()) is not null;

        private static async Task<Document> AddSharedAttributeAsync(Document document, SyntaxNode root, SyntaxNode declaration)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var newDeclaration = generator.AddAttributes(declaration, generator.Attribute(typeof(SharedAttribute).FullName));
            return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDeclaration));
        }
    }
}
