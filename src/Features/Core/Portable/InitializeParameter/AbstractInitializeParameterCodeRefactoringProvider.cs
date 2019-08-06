// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InitializeParameter
{
    internal abstract partial class AbstractInitializeParameterCodeRefactoringProvider<
        TParameterSyntax,
        TStatementSyntax,
        TExpressionSyntax> : CodeRefactoringProvider
        where TParameterSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract bool IsFunctionDeclaration(SyntaxNode node);
        protected abstract bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination);

        protected abstract SyntaxNode GetBody(SyntaxNode functionDeclaration);
        protected abstract SyntaxNode GetTypeBlock(SyntaxNode node);

        protected virtual ImmutableArray<SyntaxNode> GetParameters(SyntaxNode parameterNode)
        {
            return default;
        }
        protected abstract void InsertStatement(
            SyntaxEditor editor, SyntaxNode functionDeclaration, IMethodSymbol method,
            SyntaxNode statementToAddAfterOpt, TStatementSyntax statement);

        protected abstract Task<ImmutableArray<CodeAction>> GetRefactoringsForAllParametersAsync(
            Document document, IParameterSymbol parameter, SyntaxNode functionDeclaration, IMethodSymbol method,
            IBlockOperation blockStatementOpt, CancellationToken cancellationToken);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var position = context.Span.Start;
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);
            var firstparameterNode = GetParameterNode(token, position);

            var parameterNodes = GetParameters(firstparameterNode);
            if (parameterNodes.IsDefault)
            {
                return;
            }

            // List with parameterNodes that pass all checks
            var listOfPotentiallyValidParametersNodes = new ImmutableArray<SyntaxNode>();
            var counter = 0;

            foreach (var parameterNode in parameterNodes)
            {
                ++counter;
                if (parameterNode == null)
                {
                    return;
                }

                // Only offered when there isn't a selection, or the selection exactly selects a parameter name.
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                if (!context.Span.IsEmpty)
                {
                    var parameterName = syntaxFacts.GetNameOfParameter(parameterNode);
                    if (parameterName == null || parameterName.Value.Span != context.Span)
                    {
                        continue;
                    }
                }

                var functionDeclaration = parameterNode.FirstAncestorOrSelf<SyntaxNode>(IsFunctionDeclaration);
                if (functionDeclaration == null)
                {
                    continue;
                }

                var parameterDefault = syntaxFacts.GetDefaultOfParameter(parameterNode);

                // Don't offer inside the "=initializer" of a parameter
                if (parameterDefault?.Span.Contains(position) == true)
                {
                    continue;
                }

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // we can't just call GetDeclaredSymbol on functionDeclaration because it could an anonymous function,
                // so first we have to get the parameter symbol and then its containing method symbol
                var parameter = (IParameterSymbol)semanticModel.GetDeclaredSymbol(parameterNode, cancellationToken);
                if (parameter == null || parameter.Name == "")
                {
                    return;
                }

                var methodSymbol = (IMethodSymbol)parameter.ContainingSymbol;  // foo(objact a,
                if (methodSymbol.IsAbstract ||
                    methodSymbol.IsExtern ||
                    methodSymbol.PartialImplementationPart != null ||
                    methodSymbol.ContainingType.TypeKind == TypeKind.Interface)
                {
                    continue;
                }

                if (CanOfferRefactoring(functionDeclaration, semanticModel, syntaxFacts, cancellationToken, out var blockStatementOpt))
                {
                    // Ok.  Looks like there is at least one reasonable parameter to analyze.  Defer to subclass to 
                    // actually determine if there are any viable refactorings here.

                    // Update the list of valid parameter nodes
                    listOfPotentiallyValidParametersNodes.Add(parameterNode);

                    // For single parameter - Only offers for the parameter cursor is on
                    if (firstparameterNode == parameterNode)
                    {
                        context.RegisterRefactorings(await GetRefactoringsForSingleParameterAsync(
                    document, parameter, functionDeclaration, methodSymbol, blockStatementOpt, cancellationToken).ConfigureAwait(false));
                    }

                    // Calls for a multiple null check only when this is the last iteration and there's more than one possible valid parameter parameter
                    if (listOfPotentiallyValidParametersNodes.Length > 1 && counter == parameterNodes.Count())
                    {
                        context.RegisterRefactorings(await GetRefactoringsForAllParametersAsync(
                            document, functionDeclaration, methodSymbol, blockStatementOpt, listOfPotentiallyValidParametersNodes, position, cancellationToken).ConfigureAwait(false));
                    }
                }
            }
        }

        protected virtual Task<ImmutableArray<CodeAction>> GetRefactoringsForAllParametersAsync(Document document, SyntaxNode functionDeclaration, IMethodSymbol method, IBlockOperation blockStatementOpt, ImmutableArray<SyntaxNode> listOfParameterNodes, int position, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray<CodeAction>.Empty);
        }

        protected virtual Task<ImmutableArray<CodeAction>> GetRefactoringsForSingleParameterAsync(Document document, IParameterSymbol parameter, SyntaxNode functionDeclaration, IMethodSymbol methodSymbol, IBlockOperation blockStatementOpt, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray<CodeAction>.Empty);
        }

        protected bool CanOfferRefactoring(SyntaxNode functionDeclaration, SemanticModel semanticModel, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken, out IBlockOperation blockStatementOpt)
        {
            blockStatementOpt = null;

            var functionBody = GetBody(functionDeclaration);
            if (functionBody == null)
            {
                // We support initializing parameters, even when the containing member doesn't have a
                // body. This is useful for when the user is typing a new constructor and hasn't written
                // the body yet.
                return true;
            }

            //In order to get the block operation for the body of an anonymous function, we need to
            //get it via `IAnonymousFunctionOperation.Body` instead of getting it directly from the body syntax.GetBlockStatmentOpt does that.

            var operation = semanticModel.GetOperation(
                syntaxFacts.IsAnonymousFunction(functionDeclaration) ? functionDeclaration : functionBody,
                cancellationToken);

            if (operation == null)
            {
                return false;
            }

            switch (operation.Kind)
            {
                case OperationKind.AnonymousFunction:
                    blockStatementOpt = ((IAnonymousFunctionOperation)operation).Body;
                    break;
                case OperationKind.Block:
                    blockStatementOpt = (IBlockOperation)operation;
                    break;
                default:
                    return false;
            }

            return true;
        }

        protected TParameterSyntax GetParameterNode(SyntaxToken token, int position)
        {
            var parameterNode = token.Parent?.FirstAncestorOrSelf<TParameterSyntax>();
            if (parameterNode != null)
            {
                return parameterNode;
            }

            // We may be on the comma of a param list.  Try the position before us.
            token = token.GetPreviousToken();
            if (position == token.FullSpan.End)
            {
                return token.Parent?.FirstAncestorOrSelf<TParameterSyntax>();
            }

            return null;
        }

        protected static bool IsParameterReference(IOperation operation, IParameterSymbol parameter)
            => UnwrapImplicitConversion(operation) is IParameterReferenceOperation parameterReference &&
               parameter.Equals(parameterReference.Parameter);

        protected static IOperation UnwrapImplicitConversion(IOperation operation)
            => operation is IConversionOperation conversion && conversion.IsImplicit
                ? conversion.Operand
                : operation;

        protected static bool ContainsParameterReference(
            SemanticModel semanticModel,
            IOperation condition,
            IParameterSymbol parameter,
            CancellationToken cancellationToken)
        {
            foreach (var child in condition.Syntax.DescendantNodes().OfType<TExpressionSyntax>())
            {
                var childOperation = semanticModel.GetOperation(child, cancellationToken);
                if (IsParameterReference(childOperation, parameter))
                {
                    return true;
                }
            }

            return false;
        }

        protected static bool IsFieldOrPropertyAssignment(IOperation statement, INamedTypeSymbol containingType, out IAssignmentOperation assignmentExpression)
            => IsFieldOrPropertyAssignment(statement, containingType, out assignmentExpression, out var fieldOrProperty);

        protected static bool IsFieldOrPropertyAssignment(
            IOperation statement, INamedTypeSymbol containingType,
            out IAssignmentOperation assignmentExpression, out ISymbol fieldOrProperty)
        {
            if (statement is IExpressionStatementOperation expressionStatement)
            {
                assignmentExpression = expressionStatement.Operation as IAssignmentOperation;
                return IsFieldOrPropertyReference(assignmentExpression?.Target, containingType, out fieldOrProperty);
            }

            fieldOrProperty = null;
            assignmentExpression = null;
            return false;
        }

        protected static bool IsFieldOrPropertyReference(IOperation operation, INamedTypeSymbol containingType)
            => IsFieldOrPropertyAssignment(operation, containingType, out var fieldOrProperty);

        protected static bool IsFieldOrPropertyReference(
            IOperation operation, INamedTypeSymbol containingType, out ISymbol fieldOrProperty)
        {
            if (operation is IMemberReferenceOperation memberReference &&
                memberReference.Member.ContainingType.Equals(containingType))
            {
                if (memberReference.Member is IFieldSymbol ||
                    memberReference.Member is IPropertySymbol)
                {
                    fieldOrProperty = memberReference.Member;
                    return true;
                }
            }

            fieldOrProperty = null;
            return false;
        }

        protected class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
