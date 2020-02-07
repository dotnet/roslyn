// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidAllocationWithArrayEmptyCodeFix)), Shared]
    public class AvoidAllocationWithArrayEmptyCodeFix : CodeFixProvider
    {
        private readonly string _title = AnalyzersResources.AvoidAllocationByUsingArrayEmpty;

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ExplicitAllocationAnalyzer.ObjectCreationRuleId, ExplicitAllocationAnalyzer.ArrayCreationRuleId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            if (IsReturnStatement(node))
            {
                await TryToRegisterCodeFixesForReturnStatement(context, node, diagnostic).ConfigureAwait(false);
                return;
            }

            if (IsMethodInvocationParameter(node))
            {
                await TryToRegisterCodeFixesForMethodInvocationParameter(context, node, diagnostic).ConfigureAwait(false);
                return;
            }
        }

        private async Task TryToRegisterCodeFixesForMethodInvocationParameter(CodeFixContext context, SyntaxNode node, Diagnostic diagnostic)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (IsExpectedParameterReadonlySequence(node, semanticModel) && node is ArgumentSyntax argument)
            {
                TryRegisterCodeFix(context, node, diagnostic, argument.Expression, semanticModel);
            }
        }

        private async Task TryToRegisterCodeFixesForReturnStatement(CodeFixContext context, SyntaxNode node, Diagnostic diagnostic)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

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
                        if (CanBeReplaceWithEnumerableEmpty(objectCreation, semanticModel))
                        {
                            if (objectCreation.Type is GenericNameSyntax genericName)
                            {
                                var codeAction = CodeAction.Create(_title,
                                    token => Transform(context.Document, node, genericName.TypeArgumentList.Arguments[0], token),
                                    _title);
                                context.RegisterCodeFix(codeAction, diagnostic);
                            }
                        }
                    }
                    break;
                case ArrayCreationExpressionSyntax arrayCreation:
                    {
                        if (CanBeReplaceWithEnumerableEmpty(arrayCreation))
                        {
                            var codeAction = CodeAction.Create(_title,
                                token => Transform(context.Document, node, arrayCreation.Type.ElementType, token),
                                _title);
                            context.RegisterCodeFix(codeAction, diagnostic);
                        }
                    }
                    break;
            }
        }


        private static bool IsMethodInvocationParameter(SyntaxNode node) => node is ArgumentSyntax;

        private static bool IsReturnStatement(SyntaxNode node)
        {
            return node.Parent is ReturnStatementSyntax || node.Parent is YieldStatementSyntax || node.Parent is ArrowExpressionClauseSyntax;
        }

        private static bool IsInsideMemberReturningEnumerable(SyntaxNode node, SemanticModel semanticModel)
        {
            return IsInsideMethodReturningEnumerable(node, semanticModel) ||
                   IsInsidePropertyDeclaration(node, semanticModel);

        }

        private static bool IsInsidePropertyDeclaration(SyntaxNode node, SemanticModel semanticModel)
        {
            var propertyDeclaration = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (propertyDeclaration != null && IsPropertyTypeReadonlySequence(semanticModel, propertyDeclaration))
            {
                return IsAutoPropertyWithGetter(node) || IsArrowExpression(node);
            }

            return false;
        }

        private static bool IsAutoPropertyWithGetter(SyntaxNode node)
        {
            var accessorDeclaration = node.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
            return accessorDeclaration != null && accessorDeclaration.Keyword.Text == "get";
        }

        private static bool IsArrowExpression(SyntaxNode node)
        {
            return node.FirstAncestorOrSelf<ArrowExpressionClauseSyntax>() != null;
        }

        private static bool CanBeReplaceWithEnumerableEmpty(ArrayCreationExpressionSyntax arrayCreation)
        {
            return IsInitializationBlockEmpty(arrayCreation.Initializer);
        }

        private static bool CanBeReplaceWithEnumerableEmpty(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel)
        {
            return IsCollectionType(semanticModel, objectCreation) &&
                   IsInitializationBlockEmpty(objectCreation.Initializer) &&
                   IsCopyConstructor(semanticModel, objectCreation) == false;
        }

        private static bool IsInsideMethodReturningEnumerable(SyntaxNode node, SemanticModel semanticModel)
        {
            var methodDeclaration = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            return methodDeclaration != null && IsReturnTypeReadonlySequence(semanticModel, methodDeclaration);
        }

        private static async Task<Document> Transform(Document contextDocument, SyntaxNode node, TypeSyntax typeArgument, CancellationToken cancellationToken)
        {
            var noAllocation = SyntaxFactory.ParseExpression($"Array.Empty<{typeArgument}>()");
            var newNode = ReplaceExpression(node, noAllocation);
            if (newNode == null)
            {
                return contextDocument;
            }
            var syntaxRootAsync = await contextDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newSyntaxRoot = syntaxRootAsync.ReplaceNode(node.Parent, newNode);
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
        {
            if (objectCreation.ArgumentList == null || objectCreation.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            if (semanticModel.GetSymbolInfo(objectCreation).Symbol is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.Parameters.Any(x => x.Type is INamedTypeSymbol namedType && IsCollectionType(namedType)))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsInitializationBlockEmpty(InitializerExpressionSyntax initializer)
        {
            return initializer == null || initializer.Expressions.Count == 0;
        }

        private static bool IsCollectionType(SemanticModel semanticModel, ObjectCreationExpressionSyntax objectCreationExpressionSyntax)
        {
            return semanticModel.GetTypeInfo(objectCreationExpressionSyntax).Type is INamedTypeSymbol createdType &&
                   (createdType.TypeKind == TypeKind.Array || IsCollectionType(createdType));
        }

        private static bool IsCollectionType(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.ConstructedFrom.Interfaces.Any(x =>
                x.IsGenericType && x.ToString().StartsWith("System.Collections.Generic.ICollection", System.StringComparison.Ordinal));
        }

        private static bool IsPropertyTypeReadonlySequence(SemanticModel semanticModel, PropertyDeclarationSyntax propertyDeclaration)
        {
            return IsTypeReadonlySequence(semanticModel, propertyDeclaration.Type);
        }

        private static bool IsReturnTypeReadonlySequence(SemanticModel semanticModel, MethodDeclarationSyntax methodDeclarationSyntax)
        {
            var typeSyntax = methodDeclarationSyntax.ReturnType;
            return IsTypeReadonlySequence(semanticModel, typeSyntax);
        }

        private static bool IsExpectedParameterReadonlySequence(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node is ArgumentSyntax argument && node.Parent is ArgumentListSyntax argumentList)
            {
                var argumentIndex = argumentList.Arguments.IndexOf(argument);
                if (semanticModel.GetSymbolInfo(argumentList.Parent).Symbol is IMethodSymbol methodSymbol)
                {
                    if (methodSymbol.Parameters.Length > argumentIndex)
                    {
                        var parameterType = methodSymbol.Parameters[argumentIndex].Type;
                        if (IsTypeReadonlySequence(semanticModel, parameterType))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsTypeReadonlySequence(SemanticModel semanticModel, TypeSyntax typeSyntax)
        {
            var returnType = ModelExtensions.GetTypeInfo(semanticModel, typeSyntax).Type;
            return IsTypeReadonlySequence(semanticModel, returnType);
        }

        private static bool IsTypeReadonlySequence(SemanticModel semanticModel, ITypeSymbol type)
        {
            if (type.Kind == SymbolKind.ArrayType)
            {
                return true;
            }

            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                foreach (var readonlySequence in GetReadonlySequenceTypes(semanticModel))
                {
                    if (namedType.ConstructedFrom.Equals(readonlySequence))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static readonly ImmutableArray<string> _readonlySequenceTypeNames = ImmutableArray.Create(
            "System.Collections.Generic.IEnumerable`1",
            "System.Collections.Generic.IReadOnlyList`1",
            "System.Collections.Generic.IReadOnlyCollection`1");

        private static IEnumerable<INamedTypeSymbol?> GetReadonlySequenceTypes(SemanticModel semanticModel)
        {
            var provider = WellKnownTypeProvider.GetOrCreate(semanticModel.Compilation);
            return _readonlySequenceTypeNames.Select(name => provider.GetOrCreateTypeByMetadataName(name));
        }
    }
}
