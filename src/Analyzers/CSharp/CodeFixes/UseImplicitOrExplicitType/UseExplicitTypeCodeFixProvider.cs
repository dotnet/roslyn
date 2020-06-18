// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

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
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declarationContext = node.Parent;

            TypeSyntax typeSyntax = null;
            ParenthesizedVariableDesignationSyntax parensDesignation = null;
            SyntaxToken? variableIdentifier = null;

            if (declarationContext is RefTypeSyntax refType)
            {
                declarationContext = declarationContext.Parent;
            }

            if (declarationContext is VariableDeclarationSyntax varDecl)
            {
                typeSyntax = varDecl.Type;
                variableIdentifier = varDecl.Variables.First().Identifier;
            }
            else if (declarationContext is ForEachStatementSyntax forEach)
            {
                typeSyntax = forEach.Type;
                variableIdentifier = forEach.Identifier;
            }
            else if (declarationContext is DeclarationExpressionSyntax declarationExpression)
            {
                typeSyntax = declarationExpression.Type;
                if (declarationExpression.Designation.IsKind(SyntaxKind.ParenthesizedVariableDesignation, out ParenthesizedVariableDesignationSyntax variableDesignation))
                {
                    parensDesignation = variableDesignation;
                }
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(declarationContext.Kind());
            }

            if (parensDesignation is null)
            {
                typeSyntax = typeSyntax.StripRefIfNeeded();

                var newTypeSymbol = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).ConvertedType;

                if (newTypeSymbol.NullableAnnotation == NullableAnnotation.Annotated && variableIdentifier.HasValue)
                {
                    var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

                    // It's possible that the var shouldn't be annotated nullable, check assignments to the variable and 
                    // determine if it needs to be null
                    var methodBody = node.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsMethodBody);
                    var operationScope = semanticModel.GetOperation(methodBody, cancellationToken);
                    var declSymbol = semanticModel.GetDeclaredSymbol(variableIdentifier.Value.Parent);
                    newTypeSymbol = NullabilityHelper.TryRemoveNullableAnnotationForScope(semanticModel, operationScope, declSymbol);
                }

                // We're going to be passed through the simplifier.  Tell it to not just convert this back to var (as
                // that would defeat the purpose of this refactoring entirely).
                var newTypeSyntax = newTypeSymbol
                             .GenerateTypeSyntax(allowVar: false)
                             .WithTriviaFrom(typeSyntax);

                Debug.Assert(!newTypeSyntax.ContainsDiagnostics, "Explicit type replacement likely introduced an error in code");

                editor.ReplaceNode(typeSyntax, newTypeSyntax);
            }
            else
            {
                var tupleTypeSymbol = semanticModel.GetTypeInfo(typeSyntax.Parent, cancellationToken).ConvertedType;

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

    internal static class NullabilityHelper
    {
        public static ITypeSymbol TryRemoveNullableAnnotationForScope(SemanticModel semanticModel, IOperation containingOperation, ISymbol symbol)
        {
            var typeSymbol = symbol switch
            {
                ILocalSymbol localSymbol => localSymbol.Type,
                IParameterSymbol parameterSymbol => parameterSymbol.Type,
                _ => throw ExceptionUtilities.UnexpectedValue(symbol)
            };

            // For local symbols and parameters, we can check what the flow state 
            // for refences to the symbols are and determine if we can change 
            // the nullability to a less permissive state.
            var references = containingOperation.DescendantsAndSelf()
                .Where(o => IsSymbolReferencedByOperation(o, symbol));

            if (AreAllReferencesNotNull(semanticModel, references))
            {
                return typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            }

            return typeSymbol;
        }

        private static bool AreAllReferencesNotNull(SemanticModel semanticModel, IEnumerable<IOperation> references)
             => references.All(r => semanticModel.GetTypeInfo(r.Syntax).Nullability.FlowState == NullableFlowState.NotNull);

        private static bool IsSymbolReferencedByOperation(IOperation operation, ISymbol symbol)
            => operation switch
            {
                ILocalReferenceOperation localReference => localReference.Local.Equals(symbol),
                IParameterReferenceOperation parameterReference => parameterReference.Parameter.Equals(symbol),
                IAssignmentOperation assignment => IsSymbolReferencedByOperation(assignment.Target, symbol),
                _ => false
            };
    }
}
