// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseParameterNullChecking
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseIsNullCheck), Shared]
    internal class CSharpUseParameterNullCheckingCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseParameterNullCheckingCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseParameterNullCheckingId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            context.RegisterCodeFix(
                new MyCodeAction(CSharpAnalyzersResources.Use_parameter_null_checking,
                c => FixAsync(context.Document, diagnostic, c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken: cancellationToken);
                switch (node)
                {
                    case IfStatementSyntax ifStatement:
                        // if (item == null) throw new ArgumentNullException(nameof(item));
                        // if (item is null) throw new ArgumentNullException(nameof(item));
                        // if (object.ReferenceEquals(item, null)) throw new ArgumentNullException(nameof(item));
                        var (left, right) = ifStatement.Condition switch
                        {
                            BinaryExpressionSyntax binary => (binary.Left, binary.Right),
                            IsPatternExpressionSyntax isPattern => (isPattern.Expression, ((ConstantPatternSyntax)isPattern.Pattern).Expression),
                            InvocationExpressionSyntax { ArgumentList.Arguments: var arguments } => (arguments[0].Expression, arguments[1].Expression),
                            _ => throw ExceptionUtilities.UnexpectedValue(ifStatement.Kind())
                        };

                        // one of the sides of the binary must be a parameter
                        var parameterInIf = (IParameterSymbol?)model.GetSymbolInfo(unwrapCast(left), cancellationToken).Symbol
                            ?? (IParameterSymbol)model.GetSymbolInfo(unwrapCast(right), cancellationToken).Symbol!;

                        var parameterSyntax = (ParameterSyntax)parameterInIf.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                        editor.RemoveNode(ifStatement);
                        editor.ReplaceNode(parameterSyntax, parameterSyntax.WithExclamationExclamationToken(SyntaxFactory.Token(SyntaxKind.ExclamationExclamationToken)));
                        break;
                    case ExpressionStatementSyntax expressionStatement:
                        // this.item = item ?? throw new ArgumentNullException(nameof(item));
                        var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;
                        var nullCoalescing = (BinaryExpressionSyntax)assignment.Right;
                        var parameterReferenceSyntax = nullCoalescing.Left;

                        var parameterSymbol = (IParameterSymbol)model.GetSymbolInfo(unwrapCast(parameterReferenceSyntax), cancellationToken).Symbol!;
                        var parameterDeclarationSyntax = (ParameterSyntax)parameterSymbol.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);

                        editor.ReplaceNode(nullCoalescing, parameterReferenceSyntax.WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker));
                        editor.ReplaceNode(parameterDeclarationSyntax, parameterDeclarationSyntax.WithExclamationExclamationToken(SyntaxFactory.Token(SyntaxKind.ExclamationExclamationToken)));
                        break;
                }
            }

            static ExpressionSyntax unwrapCast(ExpressionSyntax expression)
            {
                if (expression is CastExpressionSyntax { Expression: var operand })
                {
                    return operand;
                }

                return expression;
            }
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, equivalenceKey: nameof(CSharpAnalyzersResources.Use_parameter_null_checking))
            {
            }
        }
    }
}
