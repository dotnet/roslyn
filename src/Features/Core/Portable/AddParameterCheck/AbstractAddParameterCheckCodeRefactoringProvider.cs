// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddParameterCheck
{
    internal abstract class AbstractAddParameterCheckCodeRefactoringProvider<
        TParameterSyntax,
        TMemberDeclarationSyntax,
        TStatementSyntax,
        TExpressionSyntax,
        TBinaryExpressionSyntax,
        TThrowExpressionSyntax> : CodeRefactoringProvider
        where TParameterSyntax : SyntaxNode
        where TMemberDeclarationSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TBinaryExpressionSyntax : TExpressionSyntax
        where TThrowExpressionSyntax : TExpressionSyntax
    {
        //private static MethodInfo s_registerOperationActionInfo =
        //    typeof(CompilationStartAnalysisContext).GetTypeInfo().GetDeclaredMethod("RegisterOperationActionImmutableArrayInternal");

        private static MethodInfo s_getOperationInfo =
            typeof(SemanticModel).GetTypeInfo().GetDeclaredMethod("GetOperationInternal");

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

            if (parameter.Name == "")
            {
                return;
            }

            if (!parameter.Type.IsReferenceType &&
                !parameter.Type.IsNullable())
            {
                return;
            }

            if (!method.Parameters.Contains(parameter))
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

            var supportsThrowExpression = syntaxFacts.SupportsThrowExpression(syntaxTree.Options);
            foreach (var statement in blockStatement.Statements)
            {
                if (IsNullCheck(statement, parameter))
                {
                    return;
                }

                if (supportsThrowExpression &&
                     ContainsNullCoalesceCheck(semanticModel, statement, parameter, cancellationToken))
                {
                    return;
                }
            }

            context.RegisterRefactoring(new MyCodeAction(
                FeaturesResources.Add_null_check,
                c => AddNullCheckAsync(document, blockStatement, parameter, c)));
        }

        private bool ContainsNullCoalesceCheck(
            SemanticModel semanticModel,
            IOperation statement, IParameterSymbol parameter,
            CancellationToken cancellationToken)
        {
            var syntax = statement.Syntax;
            foreach (var coalesceNode in syntax.DescendantNodes().OfType<TBinaryExpressionSyntax>())
            {
                var operation = GetOperation(semanticModel, coalesceNode, cancellationToken);
                if (operation is INullCoalescingExpression coalesceExpression)
                {
                    if (IsParameterReference(coalesceExpression.PrimaryOperand, parameter) &&
                        coalesceExpression.SecondaryOperand.Syntax is TThrowExpressionSyntax)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected abstract SyntaxNode GetBody(TMemberDeclarationSyntax containingMember);

        private bool IsNullCheck(IOperation statement, IParameterSymbol parameter)
        {
            if (statement is IIfStatement ifStatement)
            {
                if (ifStatement.Condition is IBinaryOperatorExpression binaryOperator)
                {
                    if (IsNullCheck(binaryOperator.LeftOperand, binaryOperator.RightOperand, parameter) ||
                        IsNullCheck(binaryOperator.RightOperand, binaryOperator.LeftOperand, parameter))
                    {
                        return true;
                    }
                }
                else if (parameter.Type.SpecialType == SpecialType.System_String &&
                         IsStringCheck(ifStatement.Condition, parameter))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsStringCheck(IOperation condition, IParameterSymbol parameter)
        {
            if (condition is IInvocationExpression invocation &&
                invocation.ArgumentsInSourceOrder.Length == 1 &&
                IsParameterReference(invocation.ArgumentsInSourceOrder[0].Value, parameter))
            {
                var targetMethod = invocation.TargetMethod;
                if (targetMethod?.Name == nameof(string.IsNullOrEmpty) ||
                    targetMethod?.Name == nameof(string.IsNullOrWhiteSpace))
                {
                    if (targetMethod.ContainingType.SpecialType == SpecialType.System_String)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsNullCheck(IOperation operand1, IOperation operand2, IParameterSymbol parameter)
            => IsNullLiteral(UnwrapConversion(operand1)) && IsParameterReference(operand2, parameter);

        private bool IsParameterReference(IOperation operation, IParameterSymbol parameter)
            => UnwrapConversion(operation) is IParameterReferenceExpression parameterReference &&
               parameter.Equals(parameterReference.Parameter);

        private IOperation UnwrapConversion(IOperation operation)
            => operation is IConversionExpression conversion ? conversion.Operand : operation;

        private bool IsNullLiteral(IOperation operand)
            => operand is ILiteralExpression literal &&
               literal.ConstantValue.HasValue &&
               literal.ConstantValue.Value == null;

        private async Task<Document> AddNullCheckAsync(
            Document document,
            IBlockStatement blockStatement,
            IParameterSymbol parameter,
            CancellationToken cancellationToken)
        {
            var documentOpt = await TryAddNullCheckToAssignmentAsync(
                document, blockStatement, parameter, cancellationToken).ConfigureAwait(false);

            if (documentOpt != null)
            {
                return documentOpt;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var generator = editor.Generator;
            var nullCheckStatement = generator.IfStatement(
                generator.ReferenceEqualsExpression(
                    generator.IdentifierName(parameter.Name),
                    generator.NullLiteralExpression()),
                SpecializedCollections.SingletonEnumerable(
                    generator.ThrowStatement(
                        CreateArgumentNullException(compilation, generator, parameter))));

            var statementToAddAfter = GetStatementToAddNullCheckAfter(
                semanticModel, blockStatement, parameter, cancellationToken);
            InsertStatement(editor, blockStatement.Syntax, statementToAddAfter, nullCheckStatement);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        protected abstract void InsertStatement(
            SyntaxEditor editor, SyntaxNode body,
            IOperation statementToAddAfterOpt, SyntaxNode nullCheckStatement);

        private IOperation GetStatementToAddNullCheckAfter(
            SemanticModel semanticModel,
            IBlockStatement blockStatement,
            IParameterSymbol parameter,
            CancellationToken cancellationToken)
        {
            if (parameter.ContainingSymbol is IMethodSymbol methodSymbol)
            {
                var parameterIndex = methodSymbol.Parameters.IndexOf(parameter);

                // look for an existing check for a parameter that comes before us.
                // If we find one, we'll add ourselves after that parameter check.
                for (var i = parameterIndex - 1; i >= 0; i--)
                {
                    var checkStatement = TryFindParameterCheckStatement(
                        semanticModel, blockStatement, methodSymbol.Parameters[i], cancellationToken);
                    if (checkStatement != null)
                    {
                        return checkStatement;
                    }
                }

                // look for an existing check for a parameter that comes before us.
                // If we find one, we'll add ourselves after that parameter check.
                for (var i = parameterIndex + 1; i < methodSymbol.Parameters.Length; i++)
                {
                    var checkStatement = TryFindParameterCheckStatement(
                        semanticModel, blockStatement, methodSymbol.Parameters[i], cancellationToken);
                    if (checkStatement != null)
                    {
                        var statementIndex = blockStatement.Statements.IndexOf(checkStatement);
                        return statementIndex > 0 ? blockStatement.Statements[statementIndex - 1] : null;
                    }
                }
            }

            return null;
        }

        private IOperation TryFindParameterCheckStatement(
            SemanticModel semanticModel,
            IBlockStatement blockStatement,
            IParameterSymbol parameterSymbol,
            CancellationToken cancellationToken)
        {
            foreach (var statement in blockStatement.Statements)
            {
                if (statement is IIfStatement ifStatement)
                {
                    if (ContainsParameterReference(semanticModel, ifStatement.Condition, parameterSymbol, cancellationToken))
                    {
                        return statement;
                    }
                }
                else
                {
                    // Stop hunting after we hit something that isn't an if-statement
                    return null;
                }
            }

            return null;
        }

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

        private async Task<Document> TryAddNullCheckToAssignmentAsync(
            Document document,
            IBlockStatement blockStatement,
            IParameterSymbol parameter,
            CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (!syntaxFacts.SupportsThrowExpression(syntaxTree.Options))
            {
                return null;
            }

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            if (!options.GetOption(CodeStyleOptions.PreferThrowExpression).Value)
            {
                return null;
            }

            foreach (var statement in blockStatement.Statements)
            {
                if (statement is IExpressionStatement expressionStatement &&
                    expressionStatement.Expression is IAssignmentExpression assignmentExpression &&
                    IsParameterReference(assignmentExpression.Value, parameter) &&
                    IsFieldOrPropertyReference(assignmentExpression.Target, parameter))
                {
                    var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    var generator = SyntaxGenerator.GetGenerator(document);
                    var coalesce = generator.CoalesceExpression(
                        assignmentExpression.Value.Syntax,
                        generator.ThrowExpression(
                            CreateArgumentNullException(compilation, generator, parameter)));

                    var newRoot = root.ReplaceNode(assignmentExpression.Value.Syntax, coalesce);
                    return document.WithSyntaxRoot(newRoot);
                }
            }

            return null;
        }

        private static SyntaxNode CreateArgumentNullException(
            Compilation compilation, SyntaxGenerator generator, IParameterSymbol parameter)
        {
            var argumentNullException = compilation.GetTypeByMetadataName("System.ArgumentNullException");

            return generator.ObjectCreationExpression(
                argumentNullException,
                generator.NameOfExpression(generator.IdentifierName(parameter.Name)));
        }

        private bool IsFieldOrPropertyReference(IOperation operation, IParameterSymbol parameter)
        {
            if (operation is IFieldReferenceExpression fieldReference &&
                fieldReference.Field.ContainingType.Equals(parameter.ContainingType))
            {
                return true;
            }

            if (operation is IPropertyReferenceExpression propertyReference &&
                propertyReference.Property.ContainingType.Equals(parameter.ContainingType))
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