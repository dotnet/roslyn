// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal abstract partial class AbstractUseExpressionBodyCodeFixProvider<TDeclaration> : 
        SyntaxEditorBasedCodeFixProvider
        where TDeclaration : SyntaxNode
    {
        private readonly Option<CodeStyleOption<ExpressionBodyPreference>> _option;
        private readonly string _useExpressionBodyTitle;
        private readonly string _useBlockBodyTitle;

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

        protected AbstractUseExpressionBodyCodeFixProvider(
            string diagnosticId,
            Option<CodeStyleOption<ExpressionBodyPreference>> option,
            string useExpressionBodyTitle,
            string useBlockBodyTitle)
        {
            FixableDiagnosticIds = ImmutableArray.Create(diagnosticId);
            _option = option;
            _useExpressionBodyTitle = useExpressionBodyTitle;
            _useBlockBodyTitle = useBlockBodyTitle;
        }

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => diagnostic.Severity != DiagnosticSeverity.Hidden;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var documentOptionSet = await context.Document.GetOptionsAsync(context.CancellationToken).ConfigureAwait(false);

            var priority = diagnostic.Severity == DiagnosticSeverity.Hidden
                ? CodeActionPriority.Low
                : CodeActionPriority.Medium;

            context.RegisterCodeFix(
                new MyCodeAction(diagnostic.GetMessage(), priority, c => FixAsync(context.Document, diagnostic, c)),
                diagnostic);
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(editor, diagnostic, options, cancellationToken);
            }
        }

        private void AddEdits(
            SyntaxEditor editor, Diagnostic diagnostic,
            OptionSet options, CancellationToken cancellationToken)
        {
            var declarationLocation = diagnostic.AdditionalLocations[0];
            var declaration = (TDeclaration)declarationLocation.FindNode(cancellationToken);

            var updatedDeclaration = this.Update(declaration, options)
                                         .WithAdditionalAnnotations(Formatter.Annotation);

            editor.ReplaceNode(declaration, updatedDeclaration);
        }

        private TDeclaration Update(TDeclaration declaration, OptionSet options)
        {
            var preferExpressionBody = GetBody(declaration) != null;
            if (preferExpressionBody)
            {
                GetBody(declaration).TryConvertToExpressionBody(declaration.SyntaxTree.Options,
                    ExpressionBodyPreference.WhenPossible, out var expressionBody, out var semicolonToken);

                var trailingTrivia = semicolonToken.TrailingTrivia
                                                   .Where(t => t.Kind() != SyntaxKind.EndOfLineTrivia)
                                                   .Concat(declaration.GetTrailingTrivia());
                semicolonToken = semicolonToken.WithTrailingTrivia(trailingTrivia);

                return WithSemicolonToken(
                           WithExpressionBody(
                               WithBody(declaration, null),
                               expressionBody),
                           semicolonToken);
            }
            else
            {
                return WithSemicolonToken(
                           WithExpressionBody(
                               WithGenerateBody(declaration, options),
                               null),
                           default(SyntaxToken));
            }
        }

        protected abstract bool CreateReturnStatementForExpression(TDeclaration declaration);

        protected abstract SyntaxToken GetSemicolonToken(TDeclaration declaration);
        protected abstract ArrowExpressionClauseSyntax GetExpressionBody(TDeclaration declaration);
        protected abstract BlockSyntax GetBody(TDeclaration declaration);

        protected abstract TDeclaration WithSemicolonToken(TDeclaration declaration, SyntaxToken token);
        protected abstract TDeclaration WithExpressionBody(TDeclaration declaration, ArrowExpressionClauseSyntax expressionBody);
        protected abstract TDeclaration WithBody(TDeclaration declaration, BlockSyntax body);

        protected virtual TDeclaration WithGenerateBody(
            TDeclaration declaration, OptionSet options)
        {
            var expressionBody = GetExpressionBody(declaration);
            var semicolonToken = GetSemicolonToken(declaration);
            var block = expressionBody.ConvertToBlock(
                GetSemicolonToken(declaration),
                CreateReturnStatementForExpression(declaration));

            return WithBody(declaration, block);
        }

        protected TDeclaration WithAccessorList(
            TDeclaration declaration, OptionSet options)
        {
            var expressionBody = GetExpressionBody(declaration);
            var semicolonToken = GetSemicolonToken(declaration);

            var expressionBodyPreference = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors).Value;

            AccessorDeclarationSyntax accessor;
            if (expressionBodyPreference != ExpressionBodyPreference.Never)
            {
                accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithExpressionBody(expressionBody)
                                        .WithSemicolonToken(semicolonToken);
            }
            else
            {
                var block = expressionBody.ConvertToBlock(
                    GetSemicolonToken(declaration),
                    CreateReturnStatementForExpression(declaration));
                accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, block);
            }

            return WithAccessorList(declaration, SyntaxFactory.AccessorList(
                SyntaxFactory.SingletonList(accessor)));
        }

        protected virtual TDeclaration WithAccessorList(TDeclaration declaration, AccessorListSyntax accessorListSyntax)
        {
            throw new NotImplementedException();
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            internal override CodeActionPriority Priority { get; }

            public MyCodeAction(string title, CodeActionPriority priority, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
                this.Priority = priority;
            }
        }
    }
}