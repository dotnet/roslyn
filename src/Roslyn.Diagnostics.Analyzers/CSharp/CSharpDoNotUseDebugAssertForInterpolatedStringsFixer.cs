// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpDoNotUseDebugAssertForInterpolatedStringsFixer))]
    [Shared]
    public sealed class CSharpDoNotUseDebugAssertForInterpolatedStringsFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(RoslynDiagnosticIds.DoNotUseInterpolatedStringsWithDebugAssertRuleId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken);

            if (compilation is null)
            {
                return;
            }

            var roslynDebugSymbol = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.RoslynDebug);

            if (roslynDebugSymbol is null)
            {
                return;
            }

            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        RoslynDiagnosticsAnalyzersResources.DoNotUseInterpolatedStringsWithDebugAssertCodeFix,
                        ct => ReplaceWithDebugAssertAsync(context.Document, diagnostic.Location, roslynDebugSymbol, ct),
                        equivalenceKey: nameof(CSharpDoNotUseDebugAssertForInterpolatedStringsFixer)),
                    diagnostic);
            }
        }

        private static async Task<Document> ReplaceWithDebugAssertAsync(Document document, Location location, INamedTypeSymbol roslynDebugSymbol, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntax = root.FindNode(location.SourceSpan, getInnermostNodeForTie: true);
            var generator = SyntaxGenerator.GetGenerator(document);

            if (syntax is not InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Expression: IdentifierNameSyntax { Identifier.ValueText: "Debug" } debugIdentifierNode,
                        Name.Identifier.ValueText: "Assert"
                    },
                })
            {
                return document;
            }

            var roslynDebugNode = generator.TypeExpression(roslynDebugSymbol)
                .WithAddImportsAnnotation()
                .WithLeadingTrivia(debugIdentifierNode.GetLeadingTrivia())
                .WithTrailingTrivia(debugIdentifierNode.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(debugIdentifierNode, roslynDebugNode);
            return document.WithSyntaxRoot(newRoot);
        }

        public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
    }
}
