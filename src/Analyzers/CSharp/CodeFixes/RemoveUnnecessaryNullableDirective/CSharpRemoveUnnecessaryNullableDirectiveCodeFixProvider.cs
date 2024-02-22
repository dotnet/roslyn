// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnnecessaryNullableDirective
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryNullableDirective)]
    [Shared]
    internal sealed class CSharpRemoveUnnecessaryNullableDirectiveCodeFixProvider
        : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpRemoveUnnecessaryNullableDirectiveCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => [
                IDEDiagnosticIds.RemoveRedundantNullableDirectiveDiagnosticId,
                IDEDiagnosticIds.RemoveUnnecessaryNullableDirectiveDiagnosticId,
            ];

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == IDEDiagnosticIds.RemoveRedundantNullableDirectiveDiagnosticId)
                    RegisterCodeFix(context, CSharpAnalyzersResources.Remove_redundant_nullable_directive, nameof(CSharpAnalyzersResources.Remove_redundant_nullable_directive), diagnostic);
                else
                    RegisterCodeFix(context, CSharpAnalyzersResources.Remove_unnecessary_nullable_directive, nameof(CSharpAnalyzersResources.Remove_unnecessary_nullable_directive), diagnostic);
            }

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor,
            CodeActionOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            // We first group the nullable directives by the syntax node they are attached
            // This allows to replace each node separately even if they have multiple nullable directives
            var nullableDirectivesByNodes = diagnostics
                .GroupBy(
                    x => x.Location.FindNode(findInsideTrivia: false, getInnermostNodeForTie: false, cancellationToken),
                    x => x.Location.FindNode(findInsideTrivia: true, getInnermostNodeForTie: true, cancellationToken))
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var (node, nullableDirectives) in nullableDirectivesByNodes)
            {
                // Cases where the nullable directive is at the end of the file where the unit is its syntax node.
                // In these cases, we only need to remove the directive itself
                if (node is CompilationUnitSyntax)
                {
                    foreach (var nullableDirective in nullableDirectives)
                        editor.RemoveNode(nullableDirective, SyntaxRemoveOptions.KeepNoTrivia);
                    continue;
                }

                // We obtain the indexes of each nullable directives relative to their node's leading trivia.
                // This is done so we can remove them on top of their preceding whitespaces / end of lines if any
                var leadingTrivia = node.GetLeadingTrivia();
                var indexes = nullableDirectives.Select(x => leadingTrivia.IndexOf(x.ParentTrivia))
                                                .OrderByDescending(x => x).ToArray();

                // If we don't have an end of line after the nullable directive, then we don't need to remove the
                // preceding line and simply remove the directives.
                if (leadingTrivia.Count <= indexes[^1] + 1 || !leadingTrivia[indexes[^1] + 1].IsEndOfLine())
                {
                    foreach (var nullableDirective in nullableDirectives)
                        editor.RemoveNode(nullableDirective, SyntaxRemoveOptions.KeepNoTrivia);
                    continue;
                }

                foreach (var index in indexes)
                {
                    var i = index;
                    leadingTrivia = leadingTrivia.RemoveAt(i);

                    // All whitespaces before the directives are removed up to at most one end of line
                    // so no blank lines remains. A typical case is:
                    // using System;
                    // 
                    // #nullable enable
                    // 
                    // namespace N
                    // ...
                    // If we were only to remove the directive and its trivias, a preceding blank line would remain.
                    // To avoid this, we remove at most 1 by checking all the leading trivias of its node and scan
                    // backwards.
                    i--;
                    while (i >= 0)
                    {
                        if (leadingTrivia[i].IsEndOfLine())
                        {
                            leadingTrivia = leadingTrivia.RemoveAt(i);
                            break;
                        }
                        if (leadingTrivia[i].IsWhitespace())
                        {
                            leadingTrivia = leadingTrivia.RemoveAt(i);
                        }
                        i--;
                    }
                }

                // We replace the leading trivias of the token the nullable directive was attached,
                // then replace the token on the node and finally, replace the node with its new token.
                var newToken = nullableDirectives[0].ParentTrivia.Token.WithLeadingTrivia(leadingTrivia);
                var newNode = node.ReplaceToken(nullableDirectives[0].ParentTrivia.Token, newToken);
                editor.ReplaceNode(node, newNode);
            }

            return Task.CompletedTask;
        }
    }
}
