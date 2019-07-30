// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.TypeStyle
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExplicitType), Shared]
    internal class UseExplicitTypeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public UseExplicitTypeCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(IDEDiagnosticIds.UseExplicitTypeDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;

            foreach (var diagnostic in diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                await HandleDeclarationAsync(document, editor, node, cancellationToken).ConfigureAwait(false);
            }
        }

        internal static async Task HandleDeclarationAsync(
            Document document, SyntaxEditor editor,
            SyntaxNode node, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declarationContext = node.Parent;

            TypeSyntax typeSyntax = null;
            ParenthesizedVariableDesignationSyntax parensDesignation = null;
            if (declarationContext is RefTypeSyntax refType)
            {
                declarationContext = declarationContext.Parent;
            }

            if (declarationContext is VariableDeclarationSyntax varDecl)
            {
                typeSyntax = varDecl.Type;
            }
            else if (declarationContext is ForEachStatementSyntax forEach)
            {
                typeSyntax = forEach.Type;
            }
            else if (declarationContext is DeclarationExpressionSyntax declarationExpression)
            {
                typeSyntax = declarationExpression.Type;
                if (declarationExpression.Designation.IsKind(SyntaxKind.ParenthesizedVariableDesignation))
                {
                    parensDesignation = (ParenthesizedVariableDesignationSyntax)declarationExpression.Designation;
                }
            }
            else
            {
                Contract.Fail($"unhandled kind {declarationContext.Kind().ToString()}");
            }

            if (parensDesignation is null)
            {
                var typeSymbol = semanticModel.GetTypeInfo(typeSyntax.StripRefIfNeeded()).ConvertedType;

                // We're going to be passed through the simplifier.  Tell it to not just convert
                // this back to var (as that would defeat the purpose of this refactoring entirely).
                var typeName = typeSymbol.GenerateTypeSyntax(allowVar: false)
                    .WithLeadingTrivia(node.GetLeadingTrivia())
                    .WithTrailingTrivia(node.GetTrailingTrivia());
                Debug.Assert(!typeName.ContainsDiagnostics, "Explicit type replacement likely introduced an error in code");

                editor.ReplaceNode(node, typeName);
            }
            else
            {
                var tupleTypeSymbol = semanticModel.GetTypeInfo(typeSyntax.Parent).ConvertedType;

                var leadingTrivia = node.GetLeadingTrivia()
                    .Concat(parensDesignation.GetAllPrecedingTriviaToPreviousToken().Where(t => !t.IsWhitespace()).Select(t => t.WithoutAnnotations(SyntaxAnnotation.ElasticAnnotation)));

                var tupleDeclaration = GenerateTupleDeclaration(tupleTypeSymbol, parensDesignation).WithLeadingTrivia(leadingTrivia);

                editor.ReplaceNode(declarationContext, tupleDeclaration);
            }
        }

        private static ExpressionSyntax GenerateTupleDeclaration(ITypeSymbol typeSymbol, ParenthesizedVariableDesignationSyntax parensDesignation)
        {
            Debug.Assert(typeSymbol.IsTupleType);
            var elements = ((INamedTypeSymbol)typeSymbol).TupleElements;
            Debug.Assert(elements.Length == parensDesignation.Variables.Count);

            using var builder = ArrayBuilder<SyntaxNode>.GetInstance(elements.Length);
            for (var i = 0; i < elements.Length; i++)
            {
                var designation = parensDesignation.Variables[i];
                var type = elements[i].Type;
                ExpressionSyntax newDeclaration;
                switch (designation.Kind())
                {
                    case SyntaxKind.SingleVariableDesignation:
                    case SyntaxKind.DiscardDesignation:
                        var typeName = type.GenerateTypeSyntax();
                        newDeclaration = SyntaxFactory.DeclarationExpression(typeName, designation);
                        break;
                    case SyntaxKind.ParenthesizedVariableDesignation:
                        newDeclaration = GenerateTupleDeclaration(type, (ParenthesizedVariableDesignationSyntax)designation);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(designation.Kind());
                }

                newDeclaration = newDeclaration
                    .WithLeadingTrivia(designation.GetAllPrecedingTriviaToPreviousToken())
                    .WithTrailingTrivia(designation.GetTrailingTrivia());

                builder.Add(SyntaxFactory.Argument(newDeclaration));
            }

            var separatorBuilder = ArrayBuilder<SyntaxToken>.GetInstance(builder.Count - 1, SyntaxFactory.Token(leading: default, SyntaxKind.CommaToken, trailing: default));

            return SyntaxFactory.TupleExpression(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithTrailingTrivia(),
                SyntaxFactory.SeparatedList(builder.ToImmutable(), separatorBuilder.ToImmutableAndFree()),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken))
                .WithTrailingTrivia(parensDesignation.GetTrailingTrivia());
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Use_explicit_type_instead_of_var,
                       createChangedDocument,
                       CSharpFeaturesResources.Use_explicit_type_instead_of_var)
            {
            }
        }
    }
}
