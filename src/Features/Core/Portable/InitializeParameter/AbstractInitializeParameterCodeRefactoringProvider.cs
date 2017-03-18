// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InitializeParameter
{
    internal abstract partial class AbstractInitializeParameterCodeRefactoringProvider<
        TParameterSyntax,
        TMemberDeclarationSyntax,
        TStatementSyntax,
        TExpressionSyntax,
        TBinaryExpressionSyntax> : CodeRefactoringProvider
        where TParameterSyntax : SyntaxNode
        where TMemberDeclarationSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TBinaryExpressionSyntax : TExpressionSyntax
    {
        private static MethodInfo s_getOperationInfo =
            typeof(SemanticModel).GetTypeInfo().GetDeclaredMethod("GetOperationInternal");

        protected abstract SyntaxNode GetBody(TMemberDeclarationSyntax containingMember);
        protected abstract bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination);

        protected abstract void InsertStatement(
            SyntaxEditor editor, SyntaxNode body,
            IOperation statementToAddAfterOpt, TStatementSyntax statement);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            if (context.Span.Length > 0)
            {
                return;
            }

            var position = context.Span.Start;
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            var parameterNode = token.Parent.FirstAncestorOrSelf<TParameterSyntax>();
            if (parameterNode == null)
            {
                return;
            }

            var containingMember = parameterNode.FirstAncestorOrSelf<TMemberDeclarationSyntax>();
            if (containingMember == null)
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var parameterDefault = syntaxFacts.GetDefaultOfParameter(parameterNode);

            // Don't offer inside the =initializer of a parameter
            if (parameterDefault?.Span.Contains(position) == true)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var method = (IMethodSymbol)semanticModel.GetDeclaredSymbol(containingMember, cancellationToken);
            var parameter = (IParameterSymbol)semanticModel.GetDeclaredSymbol(parameterNode, cancellationToken);

            if (!method.Parameters.Contains(parameter))
            {
                return;
            }

            if (parameter.Name == "")
            {
                return;
            }

            var body = GetBody(containingMember);
            if (body == null)
            {
                return;
            }

            var memberOperation = GetOperation(semanticModel, body, cancellationToken);
            if (!(memberOperation is IBlockStatement blockStatement))
            {
                return;
            }

            context.RegisterRefactorings(await GetMemberCreationAndInitializationRefactoringsAsync(
                document, parameter, blockStatement, cancellationToken).ConfigureAwait(false));

            context.RegisterRefactorings(await GetNullCheckRefactoringsAsync(
                document, parameter, blockStatement, cancellationToken).ConfigureAwait(false));
        }

        private static bool IsParameterReference(IOperation operation, IParameterSymbol parameter)
            => UnwrapConversion(operation) is IParameterReferenceExpression parameterReference &&
               parameter.Equals(parameterReference.Parameter);

        private static IOperation UnwrapConversion(IOperation operation)
            => operation is IConversionExpression conversion ? conversion.Operand : operation;

        private bool ContainsParameterReference(
            SemanticModel semanticModel,
            IOperation condition,
            IParameterSymbol parameter,
            CancellationToken cancellationToken)
        {
            foreach (var child in condition.Syntax.DescendantNodes().OfType<TExpressionSyntax>())
            {
                var childOperation = GetOperation(semanticModel, child, cancellationToken);
                if (IsParameterReference(childOperation, parameter))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFieldOrPropertyAssignment(
            IOperation statement, INamedTypeSymbol containingType, out IAssignmentExpression assignmentExpression)
        {
            if (statement is IExpressionStatement expressionStatement)
            {
                assignmentExpression = expressionStatement.Expression as IAssignmentExpression;
                return IsFieldOrPropertyReference(assignmentExpression?.Target, containingType);
            }

            assignmentExpression = null;
            return false;
        }

        private bool IsFieldOrPropertyReference(IOperation operation, INamedTypeSymbol containingType)
        {
            if (operation is IFieldReferenceExpression fieldReference &&
                fieldReference.Field.ContainingType.Equals(containingType))
            {
                return true;
            }

            if (operation is IPropertyReferenceExpression propertyReference &&
                propertyReference.Property.ContainingType.Equals(containingType))
            {
                return true;
            }

            return false;
        }

        private static IOperation GetOperation(
            SemanticModel semanticModel,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            return (IOperation)s_getOperationInfo.Invoke(
                semanticModel, new object[] { node, cancellationToken });
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