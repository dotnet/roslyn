// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
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
        protected abstract SyntaxNode GetTypeBlock(SyntaxNode node);

        protected abstract void InsertStatement(
            SyntaxEditor editor, TMemberDeclarationSyntax memberDeclaration,
            SyntaxNode statementToAddAfterOpt, TStatementSyntax statement);

        protected abstract Task<ImmutableArray<CodeAction>> GetRefactoringsAsync(
            Document document, IParameterSymbol parameter, TMemberDeclarationSyntax containingMember,
            IBlockStatement blockStatementOpt, CancellationToken cancellationToken);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // Only offered when there isn't a selection.
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

            var parameterNode = GetParameterNode(token, position);
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

            // Don't offer inside the "=initializer" of a parameter
            if (parameterDefault?.Span.Contains(position) == true)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var method = (IMethodSymbol)semanticModel.GetDeclaredSymbol(containingMember, cancellationToken);
            if (method.IsAbstract ||
                method.IsExtern ||
                method.PartialImplementationPart != null ||
                method.ContainingType.TypeKind == TypeKind.Interface)
            {
                return;
            }

            var parameter = (IParameterSymbol)semanticModel.GetDeclaredSymbol(parameterNode, cancellationToken);
            if (!method.Parameters.Contains(parameter))
            {
                return;
            }

            if (parameter.Name == "")
            {
                return;
            }

            // Only offered on method-like things that have a body (i.e. non-interface/non-abstract).
            var bodyOpt = GetBody(containingMember);

            // We support initializing parameters, even when the containing member doesn't have a
            // body. This is useful for when the user is typing a new constructor and hasn't written
            // the body yet.
            var blockStatementOpt = default(IBlockStatement);
            if (bodyOpt != null)
            {
                blockStatementOpt = GetOperation(semanticModel, bodyOpt, cancellationToken) as IBlockStatement;
                if (blockStatementOpt == null)
                {
                    return;
                }
            }

            // Ok.  Looks like a reasonable parameter to analyze.  Defer to subclass to 
            // actually determine if there are any viable refactorings here.
            context.RegisterRefactorings(await GetRefactoringsAsync(
                document, parameter, containingMember, blockStatementOpt, cancellationToken).ConfigureAwait(false));
        }

        private TParameterSyntax GetParameterNode(SyntaxToken token, int position)
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
            => UnwrapImplicitConversion(operation) is IParameterReferenceExpression parameterReference &&
               parameter.Equals(parameterReference.Parameter);

        protected static IOperation UnwrapImplicitConversion(IOperation operation)
            => operation is IConversionExpression conversion && !conversion.IsExplicitInCode
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
                var childOperation = GetOperation(semanticModel, child, cancellationToken);
                if (IsParameterReference(childOperation, parameter))
                {
                    return true;
                }
            }

            return false;
        }

        protected static bool IsFieldOrPropertyAssignment(IOperation statement, INamedTypeSymbol containingType, out IAssignmentExpression assignmentExpression)
            => IsFieldOrPropertyAssignment(statement, containingType, out assignmentExpression, out var fieldOrProperty);

        protected static bool IsFieldOrPropertyAssignment(
            IOperation statement, INamedTypeSymbol containingType, 
            out IAssignmentExpression assignmentExpression, out ISymbol fieldOrProperty)
        {
            if (statement is IExpressionStatement expressionStatement)
            {
                assignmentExpression = expressionStatement.Expression as IAssignmentExpression;
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
            if (operation is IMemberReferenceExpression memberReference &&
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

        protected static IOperation GetOperation(
            SemanticModel semanticModel,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            return (IOperation)s_getOperationInfo.Invoke(
                semanticModel, new object[] { node, cancellationToken });
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
