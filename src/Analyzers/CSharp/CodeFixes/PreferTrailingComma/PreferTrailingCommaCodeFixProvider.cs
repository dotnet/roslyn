// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.PreferTrailingComma
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.PreferTrailingComma), Shared]
    internal sealed class PreferTrailingCommaCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PreferTrailingCommaCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.PreferTrailingCommaDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
                RegisterCodeFix(context, "", "", diagnostic);

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var node = diagnostic.Location.FindNode(cancellationToken).GetRequiredParent();
                var nodesAndTokens = PreferTrailingCommaDiagnosticAnalyzer.GetNodesWithSeparators(node);
                var lastNode = nodesAndTokens[^1];
                nodesAndTokens = nodesAndTokens.ReplaceRange(lastNode, ImmutableArray.Create(lastNode.WithTrailingTrivia(), SyntaxFactory.Token(leading: default, SyntaxKind.CommaToken, trailing: lastNode.GetTrailingTrivia())));
                editor.ReplaceNode(node, GetReplacement(node, nodesAndTokens));
            }

            return Task.CompletedTask;
        }

        private static SyntaxNode GetReplacement(SyntaxNode node, SyntaxNodeOrTokenList nodesAndTokens)
        {
            return node switch
            {
                EnumDeclarationSyntax enumDeclaration => enumDeclaration.WithMembers(SyntaxFactory.SeparatedList<EnumMemberDeclarationSyntax>(nodesAndTokens)),
                PropertyPatternClauseSyntax propertyPattern => propertyPattern.WithSubpatterns(SyntaxFactory.SeparatedList<SubpatternSyntax>(nodesAndTokens)),
                _ => throw ExceptionUtilities.Unreachable,
            };
        }
    }
}
