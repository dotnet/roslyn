// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseDefaultLiteral), Shared]
    internal partial class CSharpUseDefaultLiteralCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseDefaultLiteralCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(IDEDiagnosticIds.UseDefaultLiteralDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CSharpAnalyzersResources.Simplify_default_expression, nameof(CSharpAnalyzersResources.Simplify_default_expression));
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            // Fix-All for this feature is somewhat complicated.  Each time we fix one case, it
            // may make the next case unfixable.  For example:
            //
            //    'var v = x ? default(string) : default(string)'.
            //
            // Here, we can replace either of the default expressions, but not both. So we have 
            // to replace one at a time, and only actually replace if it's still safe to do so.

            var parseOptions = (CSharpParseOptions)document.Project.ParseOptions;
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var preferSimpleDefaultExpression = document.Project.AnalyzerOptions.GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, tree, cancellationToken).Value;

            var workspace = document.Project.Solution.Workspace;
            var originalRoot = editor.OriginalRoot;

            var originalNodes = diagnostics.SelectAsArray(
                d => (DefaultExpressionSyntax)originalRoot.FindNode(d.Location.SourceSpan, getInnermostNodeForTie: true));

            await editor.ApplyExpressionLevelSemanticEditsAsync(
                document, originalNodes,
                (semanticModel, defaultExpression) => defaultExpression.CanReplaceWithDefaultLiteral(parseOptions, preferSimpleDefaultExpression, semanticModel, cancellationToken),
                (_, currentRoot, defaultExpression) => currentRoot.ReplaceNode(
                    defaultExpression,
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression).WithTriviaFrom(defaultExpression)),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
