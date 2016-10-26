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
    internal abstract partial class AbstractUseExpressionBodyCodeFixProvider<TDeclaration> : CodeFixProvider
        where TDeclaration : SyntaxNode
    {
        private readonly Option<CodeStyleOption<bool>> _option;
        private readonly string _useExpressionBodyTitle;
        private readonly string _useBlockBodyTitle;

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

        protected AbstractUseExpressionBodyCodeFixProvider(
            string diagnosticId,
            Option<CodeStyleOption<bool>> option,
            string useExpressionBodyTitle,
            string useBlockBodyTitle)
        {
            FixableDiagnosticIds = ImmutableArray.Create(diagnosticId);
            _option = option;
            _useExpressionBodyTitle = useExpressionBodyTitle;
            _useBlockBodyTitle = useBlockBodyTitle;
        }

        public sealed override FixAllProvider GetFixAllProvider() => new UseExpressionBodyFixAllProvider(this);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var option = context.Document.Project.Solution.Workspace.Options.GetOption(_option);
            var title = option.Value
                ? _useExpressionBodyTitle
                : _useBlockBodyTitle;

            context.RegisterCodeFix(
                new MyCodeAction(title, c => FixAsync(context.Document, diagnostic, c)),
                diagnostic);

            return SpecializedTasks.EmptyTask;
        }

        private Task<Document> FixAsync(
            Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            return FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken);
        }

        private async Task<Document> FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var options = document.Project.Solution.Workspace.Options;
            var preferExpressionBody = document.Project.Solution.Workspace.Options.GetOption(_option).Value;

            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(root, editor, diagnostic, options, preferExpressionBody, cancellationToken);
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private void AddEdits(
            SyntaxNode root, SyntaxEditor editor, Diagnostic diagnostic,
            OptionSet options, bool preferExpressionBody, CancellationToken cancellationToken)
        {
            var declarationLocation = diagnostic.AdditionalLocations[0];
            var declaration = (TDeclaration)declarationLocation.FindNode(cancellationToken);

            var updatedDeclaration = this.Update(declaration, preferExpressionBody, options)
                                         .WithAdditionalAnnotations(Formatter.Annotation);

            editor.ReplaceNode(declaration, updatedDeclaration);
        }

        private TDeclaration Update(TDeclaration declaration, bool preferExpressionBody, OptionSet options)
        {
            if (preferExpressionBody)
            {
                return WithSemicolonToken(
                           WithExpressionBody(
                               WithBody(declaration, null),
                               GetBody(declaration).TryConvertToExpressionBody(declaration.SyntaxTree.Options)),
                           GetFirstStatementSemicolon(GetBody(declaration)));
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

        private SyntaxToken GetFirstStatementSemicolon(BlockSyntax body)
        {
            var firstStatement = body.Statements[0];
            if (firstStatement.IsKind(SyntaxKind.ExpressionStatement))
            {
                return ((ExpressionStatementSyntax)firstStatement).SemicolonToken;
            }
            else if (firstStatement.IsKind(SyntaxKind.ReturnStatement))
            {
                return ((ReturnStatementSyntax)firstStatement).SemicolonToken;
            }
            else if (firstStatement.IsKind(SyntaxKind.ThrowStatement))
            {
                return ((ThrowStatementSyntax)firstStatement).SemicolonToken;
            }
            else
            {
                return SyntaxFactory.Token(SyntaxKind.SemicolonToken);
            }
        }

        protected TDeclaration WithAccessorList(
            TDeclaration declaration, OptionSet options)
        {
            var expressionBody = GetExpressionBody(declaration);
            var semicolonToken = GetSemicolonToken(declaration);

            var preferExpressionBodiedAccessors = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors).Value;

            AccessorDeclarationSyntax accessor;
            if (preferExpressionBodiedAccessors)
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
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}