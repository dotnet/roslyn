// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InitializeParameter
{
    internal abstract partial class AbstractInitializeParameterCodeRefactoringProvider<
        TParameterSyntax,
        TMemberDeclarationSyntax,
        TStatementSyntax,
        TExpressionSyntax,
        TBinaryExpressionSyntax>
    {
        private async Task RegisterNullCheckRefactoringsAsync(
            CodeRefactoringContext context, IParameterSymbol parameter, IBlockStatement blockStatement)
        {
            if (!parameter.Type.IsReferenceType &&
                !parameter.Type.IsNullable())
            {
                return;
            }

            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var statement in blockStatement.Statements)
            {
                if (IsNullCheck(statement, parameter))
                {
                    return;
                }

                if (ContainsNullCoalesceCheck(
                        syntaxFacts, semanticModel, statement,
                        parameter, cancellationToken))
                {
                    return;
                }
            }

            context.RegisterRefactoring(new MyCodeAction(
                FeaturesResources.Add_null_check,
                c => AddNullCheckAsync(document, blockStatement, parameter, c)));

            if (parameter.Type.SpecialType == SpecialType.System_String)
            {
                context.RegisterRefactoring(new MyCodeAction(
                    FeaturesResources.Add_string_IsNullOrEmpty_check,
                    c => AddStringCheckAsync(document, blockStatement, parameter, nameof(string.IsNullOrEmpty), c)));

                context.RegisterRefactoring(new MyCodeAction(
                    FeaturesResources.Add_string_IsNullOrWhiteSpace_check,
                    c => AddStringCheckAsync(document, blockStatement, parameter, nameof(string.IsNullOrWhiteSpace), c)));
            }
        }

        private bool ContainsNullCoalesceCheck(
            ISyntaxFactsService syntaxFacts, SemanticModel semanticModel,
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
                        syntaxFacts.IsThrowExpression(coalesceExpression.SecondaryOperand.Syntax))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

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
                    return targetMethod.ContainingType.SpecialType == SpecialType.System_String;
                }
            }

            return false;
        }

        private bool IsNullCheck(IOperation operand1, IOperation operand2, IParameterSymbol parameter)
            => IsNullLiteral(UnwrapConversion(operand1)) && IsParameterReference(operand2, parameter);

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

            return await AddNullCheckAsync(
                document, blockStatement, parameter,
                (c, g) => CreateNullCheckStatement(c, g, parameter),
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<Document> AddStringCheckAsync(
            Document document,
            IBlockStatement blockStatement,
            IParameterSymbol parameter,
            string methodName,
            CancellationToken cancellationToken)
        {
            return await AddNullCheckAsync(
                document, blockStatement, parameter,
                (c, g) => CreateStringCheckStatement(c, g, parameter, methodName),
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<Document> AddNullCheckAsync(
            Document document, 
            IBlockStatement blockStatement, 
            IParameterSymbol parameter, 
            Func<Compilation, SyntaxGenerator, TStatementSyntax> generateNullCheck,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var nullCheckStatement = generateNullCheck(compilation, editor.Generator);

            var statementToAddAfter = GetStatementToAddNullCheckAfter(
                semanticModel, blockStatement, parameter, cancellationToken);
            InsertStatement(editor, blockStatement.Syntax, statementToAddAfter, nullCheckStatement);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private static TStatementSyntax CreateNullCheckStatement(
            Compilation compilation, SyntaxGenerator generator, IParameterSymbol parameter)
        {
            return (TStatementSyntax)generator.IfStatement(
                generator.ReferenceEqualsExpression(
                    generator.IdentifierName(parameter.Name),
                    generator.NullLiteralExpression()),
                SpecializedCollections.SingletonEnumerable(
                    generator.ThrowStatement(
                        CreateArgumentNullException(compilation, generator, parameter))));
        }

        private static TStatementSyntax CreateStringCheckStatement(
            Compilation compilation, SyntaxGenerator generator,
            IParameterSymbol parameter, string methodName)
        {
            return (TStatementSyntax)generator.IfStatement(
                generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.TypeExpression(SpecialType.System_String),
                        generator.IdentifierName(methodName)),
                    generator.Argument(generator.IdentifierName(parameter.Name))),
                SpecializedCollections.SingletonEnumerable(
                    generator.ThrowStatement(
                        CreateArgumentException(compilation, generator, parameter))));
        }

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

                    continue;
                }

                // Stop hunting after we hit something that isn't an if-statement
                break;
            }

            return null;
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
            return generator.ObjectCreationExpression(
                compilation.GetTypeByMetadataName("System.ArgumentNullException"),
                generator.NameOfExpression(generator.IdentifierName(parameter.Name)));
        }

        private static SyntaxNode CreateArgumentException(
            Compilation compilation, SyntaxGenerator generator, IParameterSymbol parameter)
        {
            // Note "message" is not localized.  It is the name of the first parameter of 
            // "ArgumentException"
            return generator.ObjectCreationExpression(
                compilation.GetTypeByMetadataName("System.ArgumentException"),
                generator.LiteralExpression("message"),
                generator.NameOfExpression(generator.IdentifierName(parameter.Name)));
        }
    }
}