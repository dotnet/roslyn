// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidAllocationWithArrayEmptyCodeFix)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    internal sealed class AvoidAllocationWithArrayEmptyCodeFix() : CodeFixProvider
    {
        private readonly string _title = CodeFixesResources.AvoidAllocationByUsingArrayEmpty;

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(ExplicitAllocationAnalyzer.ObjectCreationRuleId, ExplicitAllocationAnalyzer.ArrayCreationRuleId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            if (IsReturnStatement(node))
            {
                await TryToRegisterCodeFixesForReturnStatementAsync(context, node, diagnostic).ConfigureAwait(false);
                return;
            }

            if (IsMethodInvocationParameter(node))
            {
                await TryToRegisterCodeFixesForMethodInvocationParameterAsync(context, node, diagnostic).ConfigureAwait(false);
                return;
            }
        }

        private async Task TryToRegisterCodeFixesForMethodInvocationParameterAsync(CodeFixContext context, SyntaxNode node, Diagnostic diagnostic)
        {
            var semanticModel = await context.Document.GetRequiredSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (IsExpectedParameterReadonlySequence(node, semanticModel) && node is ArgumentSyntax argument)
            {
                TryRegisterCodeFix(context, node, diagnostic, argument.Expression, semanticModel);
            }
        }

        private async Task TryToRegisterCodeFixesForReturnStatementAsync(CodeFixContext context, SyntaxNode node, Diagnostic diagnostic)
        {
            var semanticModel = await context.Document.GetRequiredSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (IsInsideMemberReturningEnumerable(node, semanticModel))
            {
                TryRegisterCodeFix(context, node, diagnostic, node, semanticModel);
            }
        }

        private void TryRegisterCodeFix(CodeFixContext context, SyntaxNode node, Diagnostic diagnostic, SyntaxNode creationExpression, SemanticModel semanticModel)
        {
            switch (creationExpression)
            {
                case ObjectCreationExpressionSyntax objectCreation:
                    {
                        if (CanBeReplaceWithEnumerableEmpty(objectCreation, semanticModel) &&
                            objectCreation.Type is GenericNameSyntax genericName)
                        {
                            var codeAction = CodeAction.Create(_title,
                                token => TransformAsync(context.Document, node, genericName.TypeArgumentList.Arguments[0], token),
                                _title);
                            context.RegisterCodeFix(codeAction, diagnostic);
                        }
                    }

                    break;
                case ArrayCreationExpressionSyntax arrayCreation:
                    {
                        if (CanBeReplaceWithEnumerableEmpty(arrayCreation))
                        {
                            var codeAction = CodeAction.Create(_title,
                                token => TransformAsync(context.Document, node, arrayCreation.Type.ElementType, token),
                                _title);
                            context.RegisterCodeFix(codeAction, diagnostic);
                        }
                    }

                    break;
            }
        }

        private static bool IsMethodInvocationParameter(SyntaxNode node) => node is ArgumentSyntax;

        private static bool IsReturnStatement(SyntaxNode node)
            => node.Parent is ReturnStatementSyntax or YieldStatementSyntax or ArrowExpressionClauseSyntax;

        private static bool IsInsideMemberReturningEnumerable(SyntaxNode node, SemanticModel semanticModel)
            => IsInsideMethodReturningEnumerable(node, semanticModel) || IsInsidePropertyDeclaration(node, semanticModel);

        private static bool IsInsidePropertyDeclaration(SyntaxNode node, SemanticModel semanticModel)
        {
            var propertyDeclaration = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            return propertyDeclaration != null &&
                IsPropertyTypeReadonlySequence(semanticModel, propertyDeclaration) &&
                (IsAutoPropertyWithGetter(node) || IsArrowExpression(node));
        }

        private static bool IsAutoPropertyWithGetter(SyntaxNode node)
        {
            var accessorDeclaration = node.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
            return accessorDeclaration != null && accessorDeclaration.Keyword.Text == "get";
        }

        private static bool IsArrowExpression(SyntaxNode node)
            => node.FirstAncestorOrSelf<ArrowExpressionClauseSyntax>() != null;

        private static bool CanBeReplaceWithEnumerableEmpty(ArrayCreationExpressionSyntax arrayCreation)
            => IsInitializationBlockEmpty(arrayCreation.Initializer);

        private static bool CanBeReplaceWithEnumerableEmpty(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel)
            => IsCollectionType(semanticModel, objectCreation) &&
               IsInitializationBlockEmpty(objectCreation.Initializer) &&
               IsCopyConstructor(semanticModel, objectCreation) == false;

        private static bool IsInsideMethodReturningEnumerable(SyntaxNode node, SemanticModel semanticModel)
        {
            var methodDeclaration = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            return methodDeclaration != null && IsReturnTypeReadonlySequence(semanticModel, methodDeclaration);
        }

        private static async Task<Document> TransformAsync(Document contextDocument, SyntaxNode node, TypeSyntax typeArgument, CancellationToken cancellationToken)
        {
            var noAllocation = SyntaxFactory.ParseExpression($"Array.Empty<{typeArgument}>()");
            var newNode = ReplaceExpression(node, noAllocation);
            if (newNode == null)
            {
                return contextDocument;
            }

            var syntaxRootAsync = await contextDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newSyntaxRoot = syntaxRootAsync.ReplaceNode(node.Parent!, newNode);
            return contextDocument.WithSyntaxRoot(newSyntaxRoot);
        }

        private static SyntaxNode? ReplaceExpression(SyntaxNode node, ExpressionSyntax newExpression)
        {
            switch (node.Parent)
            {
                case ReturnStatementSyntax parentReturn:
                    return parentReturn.WithExpression(newExpression);
                case ArrowExpressionClauseSyntax arrowStatement:
                    return arrowStatement.WithExpression(newExpression);
                case ArgumentListSyntax argumentList:
                    var newArguments = argumentList.Arguments.Select(x => x == node ? SyntaxFactory.Argument(newExpression) : x);
                    return argumentList.WithArguments(SyntaxFactory.SeparatedList(newArguments));
                default:
                    return null;
            }
        }

        private static bool IsCopyConstructor(SemanticModel semanticModel, ObjectCreationExpressionSyntax objectCreation)
            => objectCreation.ArgumentList?.Arguments.Count > 0 &&
               semanticModel.GetSymbolInfo(objectCreation).Symbol is IMethodSymbol methodSymbol &&
               methodSymbol.Parameters.Any(x => x.Type is INamedTypeSymbol namedType && ImplementsGenericICollectionInterface(namedType));

        private static bool IsInitializationBlockEmpty(InitializerExpressionSyntax? initializer)
            => initializer == null || initializer.Expressions.Count == 0;

        private static bool IsCollectionType(SemanticModel semanticModel, ObjectCreationExpressionSyntax objectCreationExpressionSyntax)
            => semanticModel.GetTypeInfo(objectCreationExpressionSyntax).Type is INamedTypeSymbol createdType &&
               (createdType.TypeKind == TypeKind.Array || ImplementsGenericICollectionInterface(createdType));

        private static bool ImplementsGenericICollectionInterface(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.ConstructedFrom.Interfaces.Any(x => x.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_ICollection_T);
        }

        private static bool IsPropertyTypeReadonlySequence(SemanticModel semanticModel, PropertyDeclarationSyntax propertyDeclaration)
            => IsTypeReadonlySequence(semanticModel, propertyDeclaration.Type);

        private static bool IsReturnTypeReadonlySequence(SemanticModel semanticModel, MethodDeclarationSyntax methodDeclarationSyntax)
            => IsTypeReadonlySequence(semanticModel, methodDeclarationSyntax.ReturnType);

        private static bool IsExpectedParameterReadonlySequence(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node is ArgumentSyntax argument && node.Parent is ArgumentListSyntax argumentList)
            {
                var argumentIndex = argumentList.Arguments.IndexOf(argument);
                if (semanticModel.GetSymbolInfo(argumentList.Parent!).Symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.Parameters.Length > argumentIndex)
                {
                    var parameterType = methodSymbol.Parameters[argumentIndex].Type;
                    if (IsTypeReadonlySequence(parameterType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsTypeReadonlySequence(SemanticModel semanticModel, TypeSyntax typeSyntax)
        {
            var returnType = ModelExtensions.GetTypeInfo(semanticModel, typeSyntax).Type!;
            return IsTypeReadonlySequence(returnType);
        }

        private static bool IsTypeReadonlySequence(ITypeSymbol type)
        {
            if (type.Kind == SymbolKind.ArrayType)
            {
                return true;
            }

            return type is INamedTypeSymbol namedType &&
                namedType.IsGenericType &&
                _readonlySequenceSpecialTypes.Any(readonlySequence => namedType.ConstructedFrom.SpecialType == readonlySequence);
        }

        private static readonly ImmutableArray<SpecialType> _readonlySequenceSpecialTypes = ImmutableArray.Create(
            SpecialType.System_Collections_Generic_IEnumerable_T,
            SpecialType.System_Collections_Generic_IReadOnlyList_T,
            SpecialType.System_Collections_Generic_IReadOnlyCollection_T);
    }
}
