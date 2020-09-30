﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp.TypeStyle
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExplicitType), Shared]
    internal class UseExplicitTypeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
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
            var declarationContext = node.Parent;

            if (declarationContext is RefTypeSyntax)
            {
                declarationContext = declarationContext.Parent;
            }

            if (declarationContext is VariableDeclarationSyntax varDecl)
            {
                await HandleVariableDeclarationAsync(document, editor, varDecl, cancellationToken).ConfigureAwait(false);
            }
            else if (declarationContext is ForEachStatementSyntax forEach)
            {
                await HandleForEachStatementAsync(document, editor, forEach, cancellationToken).ConfigureAwait(false);
            }
            else if (declarationContext is DeclarationExpressionSyntax declarationExpression)
            {
                await HandleDeclarationExpressionAsync(document, editor, declarationExpression, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(declarationContext?.Kind());
            }
        }

        private static async Task HandleDeclarationExpressionAsync(Document document, SyntaxEditor editor, DeclarationExpressionSyntax declarationExpression, CancellationToken cancellationToken)
        {
            var typeSyntax = declarationExpression.Type;
            typeSyntax = typeSyntax.StripRefIfNeeded();

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (declarationExpression.Designation.IsKind(SyntaxKind.ParenthesizedVariableDesignation, out ParenthesizedVariableDesignationSyntax? variableDesignation))
            {
                RoslynDebug.AssertNotNull(typeSyntax.Parent);

                var tupleTypeSymbol = semanticModel.GetTypeInfo(typeSyntax.Parent, cancellationToken).ConvertedType;
                RoslynDebug.AssertNotNull(tupleTypeSymbol);

                var leadingTrivia = declarationExpression.GetLeadingTrivia()
                    .Concat(variableDesignation.GetAllPrecedingTriviaToPreviousToken().Where(t => !t.IsWhitespace()).Select(t => t.WithoutAnnotations(SyntaxAnnotation.ElasticAnnotation)));

                var tupleDeclaration = GenerateTupleDeclaration(tupleTypeSymbol, variableDesignation).WithLeadingTrivia(leadingTrivia);

                editor.ReplaceNode(declarationExpression, tupleDeclaration);
            }
            else
            {
                var typeSymbol = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).ConvertedType;
                RoslynDebug.AssertNotNull(typeSymbol);

                editor.ReplaceNode(typeSyntax, GenerateTypeDeclaration(typeSyntax, typeSymbol));
            }
        }

        private static async Task HandleForEachStatementAsync(Document document, SyntaxEditor editor, ForEachStatementSyntax forEach, CancellationToken cancellationToken)
        {
            var typeSyntax = forEach.Type.StripRefIfNeeded();
            var declarationSyntax = forEach.Identifier.Parent;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeSymbol = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).ConvertedType;

            RoslynDebug.AssertNotNull(typeSymbol);
            RoslynDebug.AssertNotNull(declarationSyntax);

            typeSymbol = AdjustNullabilityOfTypeSymbol(
                typeSymbol,
                document.GetRequiredLanguageService<ISyntaxFactsService>(),
                semanticModel,
                declarationSyntax,
                cancellationToken);

            editor.ReplaceNode(typeSyntax, GenerateTypeDeclaration(typeSyntax, typeSymbol));
        }

        private static async Task HandleVariableDeclarationAsync(Document document, SyntaxEditor editor, VariableDeclarationSyntax varDecl, CancellationToken cancellationToken)
        {
            var typeSyntax = varDecl.Type.StripRefIfNeeded();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var typeSymbol = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).ConvertedType;
            RoslynDebug.AssertNotNull(typeSymbol);

            if (varDecl.Variables.Count == 1)
            {
                var declarationSyntax = varDecl.Variables.Single().Identifier.Parent;
                RoslynDebug.AssertNotNull(declarationSyntax);

                typeSymbol = AdjustNullabilityOfTypeSymbol(
                    typeSymbol,
                    document.GetRequiredLanguageService<ISyntaxFactsService>(),
                    semanticModel,
                    declarationSyntax,
                    cancellationToken);
            }

            editor.ReplaceNode(typeSyntax, GenerateTypeDeclaration(typeSyntax, typeSymbol));
        }

        private static ITypeSymbol AdjustNullabilityOfTypeSymbol(
            ITypeSymbol typeSymbol,
            ISyntaxFacts syntaxFacts,
            SemanticModel semanticModel,
            SyntaxNode declarationSyntax,
            CancellationToken cancellationToken)
        {
            if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
            {
                // It's possible that the var shouldn't be annotated nullable, check assignments to the variable and 
                // determine if it needs to be null
                var encapsulatingNode = syntaxFacts.GetIOperationRootNode(declarationSyntax);
                Contract.ThrowIfNull(encapsulatingNode);

                var operationScope = semanticModel.GetRequiredOperation(encapsulatingNode, cancellationToken);
                var declSymbol = semanticModel.GetRequiredDeclaredSymbol(declarationSyntax, cancellationToken);

                if (NullableHelpers.IsSymbolAssignedPossiblyNullValue(semanticModel, operationScope, declSymbol) == false)
                {
                    // If the symbol is never assigned null we can update the type symbol to also be non-null
                    return typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                }
            }

            return typeSymbol;
        }

        private static SyntaxNode GenerateTypeDeclaration(TypeSyntax typeSyntax, ITypeSymbol newTypeSymbol)
        {
            // We're going to be passed through the simplifier.  Tell it to not just convert this back to var (as
            // that would defeat the purpose of this refactoring entirely).
            var newTypeSyntax = newTypeSymbol
                         .GenerateTypeSyntax(allowVar: false)
                         .WithTriviaFrom(typeSyntax);

            Debug.Assert(!newTypeSyntax.ContainsDiagnostics, "Explicit type replacement likely introduced an error in code");

            return newTypeSyntax;
        }

        private static ExpressionSyntax GenerateTupleDeclaration(ITypeSymbol typeSymbol, ParenthesizedVariableDesignationSyntax parensDesignation)
        {
            Debug.Assert(typeSymbol.IsTupleType);
            var elements = ((INamedTypeSymbol)typeSymbol).TupleElements;
            Debug.Assert(elements.Length == parensDesignation.Variables.Count);

            using var builderDisposer = ArrayBuilder<SyntaxNode>.GetInstance(elements.Length, out var builder);
            for (var i = 0; i < elements.Length; i++)
            {
                var designation = parensDesignation.Variables[i];
                var type = elements[i].Type;
                ExpressionSyntax newDeclaration;
                switch (designation.Kind())
                {
                    case SyntaxKind.SingleVariableDesignation:
                    case SyntaxKind.DiscardDesignation:
                        var typeName = type.GenerateTypeSyntax(allowVar: false);
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

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Use_explicit_type_instead_of_var,
                       createChangedDocument,
                       CSharpAnalyzersResources.Use_explicit_type_instead_of_var)
            {
            }
        }
    }
}
