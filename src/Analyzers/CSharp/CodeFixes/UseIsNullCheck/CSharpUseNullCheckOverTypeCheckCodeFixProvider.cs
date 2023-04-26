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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseNullCheckOverTypeCheck), Shared]
    internal sealed class CSharpUseNullCheckOverTypeCheckCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private static readonly ConstantPatternSyntax s_nullConstantPattern = ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression));

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpUseNullCheckOverTypeCheckCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseNullCheckOverTypeCheckDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CSharpAnalyzersResources.Prefer_null_check_over_type_check, nameof(CSharpAnalyzersResources.Prefer_null_check_over_type_check));
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken: cancellationToken);
                SyntaxNode replacement = node switch
                {
                    // Replace 'x is object' with 'x is not null'
                    BinaryExpressionSyntax binary =>
                        IsPatternExpression(
                            expression: binary.Left,
                            pattern: UnaryPattern(s_nullConstantPattern)),
                    UnaryPatternSyntax =>
                        s_nullConstantPattern,
                    // The analyzer reports diagnostic only on BinaryExpressionSyntax and UnaryPatternSyntax.
                    _ => throw ExceptionUtilities.Unreachable()
                };

                editor.ReplaceNode(node, replacement.WithTriviaFrom(node));
            }

            return Task.CompletedTask;
        }
    }
}
