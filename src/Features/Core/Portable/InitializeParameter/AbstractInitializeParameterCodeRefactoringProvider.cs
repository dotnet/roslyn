// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

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

        protected abstract Task<ImmutableArray<CodeAction>> GetRefactoringsForAllParametersAsync(
            Document document,
            SyntaxNode functionDeclaration,
            IMethodSymbol method,
            IBlockOperation blockStatementOpt,
            ImmutableArray<SyntaxNode> listOfParameterNodes,
            TextSpan parameterSpan,
            CancellationToken cancellationToken);

        protected abstract Task<ImmutableArray<CodeAction>> GetRefactoringsForSingleParameterAsync(
            Document document,
            IParameterSymbol parameter,
            SyntaxNode functionDeclaration,
            IMethodSymbol methodSymbol,
            IBlockOperation blockStatementOpt,
            CancellationToken cancellationToken);

        protected abstract void InsertStatement(
            SyntaxEditor editor, SyntaxNode functionDeclaration, IMethodSymbol method,
            SyntaxNode statementToAddAfterOpt, TStatementSyntax statement);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;

            // TODO: One could try to retrieve TParameterList and then filter out parameters that intersect with
            // textSpan and use that as `parameterNodes`, where `selectedParameter` would be the first one.

            var selectedParameter = await context.TryGetRelevantNodeAsync<TParameterSyntax>().ConfigureAwait(false);
            if (selectedParameter == null)
            {
                return;
            }

            var functionDeclaration = selectedParameter.FirstAncestorOrSelf<SyntaxNode>(IsFunctionDeclaration);
            if (functionDeclaration is null)
            {
                return;
            }

            var generator = SyntaxGenerator.GetGenerator(document);
            var parameterNodes = generator.GetParameters(functionDeclaration);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var parameterDefault = syntaxFacts.GetDefaultOfParameter(selectedParameter);

            // we can't just call GetDeclaredSymbol on functionDeclaration because it could an anonymous function,
            // so first we have to get the parameter symbol and then its containing method symbol
            if (!TryGetParameterSymbol(selectedParameter, semanticModel, out var parameter, cancellationToken))
            {
                return;
            }

            var methodSymbol = (IMethodSymbol)parameter.ContainingSymbol;
            if (methodSymbol.IsAbstract ||
                methodSymbol.IsExtern ||
                methodSymbol.PartialImplementationPart != null ||
                methodSymbol.ContainingType.TypeKind == TypeKind.Interface)
            {
                return;
            }

            if (CanOfferRefactoring(functionDeclaration, semanticModel, syntaxFacts, cancellationToken, out var blockStatementOpt))
            {
                // Ok.  Looks like the selected parameter could be refactored. Defer to subclass to 
                // actually determine if there are any viable refactorings here.
                context.RegisterRefactorings(await GetRefactoringsForSingleParameterAsync(
                    document, parameter, functionDeclaration, methodSymbol, blockStatementOpt, cancellationToken).ConfigureAwait(false));
            }

            // List with parameterNodes that pass all checks
            var listOfPotentiallyValidParametersNodes = ArrayBuilder<SyntaxNode>.GetInstance();
            foreach (var parameterNode in parameterNodes)
            {
                if (!TryGetParameterSymbol(parameterNode, semanticModel, out parameter, cancellationToken))
                {
                    return;
                }

                // Update the list of valid parameter nodes
                listOfPotentiallyValidParametersNodes.Add(parameterNode);
            }

            if (listOfPotentiallyValidParametersNodes.Count > 1)
            {
                // Looks like we can offer a refactoring for more than one parameter. Defer to subclass to 
                // actually determine if there are any viable refactorings here.
                context.RegisterRefactorings(await GetRefactoringsForAllParametersAsync(
                    document, functionDeclaration, methodSymbol, blockStatementOpt, listOfPotentiallyValidParametersNodes.ToImmutableAndFree(), selectedParameter.Span, cancellationToken).ConfigureAwait(false));
            }

            static bool TryGetParameterSymbol(
                SyntaxNode parameterNode,
                SemanticModel semanticModel,
                out IParameterSymbol parameter,
                CancellationToken cancellationToken)
            {
                parameter = (IParameterSymbol)semanticModel.GetDeclaredSymbol(parameterNode, cancellationToken);
                return parameter.Name != "";
            }
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

            // In order to get the block operation for the body of an anonymous function, we need to
            // get it via `IAnonymousFunctionOperation.Body` instead of getting it directly from the body syntax.

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
