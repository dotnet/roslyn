﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InitializeParameter
{
    internal abstract partial class AbstractAddParameterCheckCodeRefactoringProvider<
        TParameterSyntax,
        TStatementSyntax,
        TExpressionSyntax,
        TBinaryExpressionSyntax> : AbstractInitializeParameterCodeRefactoringProvider<
            TParameterSyntax,
            TStatementSyntax,
            TExpressionSyntax>
        where TParameterSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TBinaryExpressionSyntax : TExpressionSyntax
    {
        protected override async Task<ImmutableArray<CodeAction>> GetRefactoringsAsync(
            Document document, IParameterSymbol parameter, SyntaxNode functionDeclaration, IMethodSymbol method,
            IBlockOperation blockStatementOpt, CancellationToken cancellationToken)
        {
            // Only should provide null-checks for reference types and nullable types.
            if (!parameter.Type.IsReferenceType &&
                !parameter.Type.IsNullable())
            {
                return ImmutableArray<CodeAction>.Empty;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Look for an existing "if (p == null)" statement, or "p ?? throw" check.  If we already
            // have one, we don't want to offer to generate a new null check.
            //
            // Note: we only check the top level statements of the block.  I think that's sufficient
            // as this will catch the 90% case, while not being that bad an experience even when 
            // people do strange things in their constructors.
            if (blockStatementOpt != null)
            {
                if (!CanOffer(blockStatementOpt.Syntax))
                {
                    return ImmutableArray<CodeAction>.Empty;
                }

                foreach (var statement in blockStatementOpt.Operations)
                {
                    if (IsIfNullCheck(statement, parameter))
                    {
                        return ImmutableArray<CodeAction>.Empty;
                    }

                    if (ContainsNullCoalesceCheck(
                            syntaxFacts, semanticModel, statement,
                            parameter, cancellationToken))
                    {
                        return ImmutableArray<CodeAction>.Empty;
                    }
                }
            }

            // Great.  There was no null check.  Offer to add one.
            var result = ArrayBuilder<CodeAction>.GetInstance();
            result.Add(new MyCodeAction(
                FeaturesResources.Add_null_check,
                c => AddNullCheckAsync(document, parameter, functionDeclaration, method, blockStatementOpt, c)));

            // Also, if this was a string, offer to add the special checks to 
            // string.IsNullOrEmpty and string.IsNullOrWhitespace.
            if (parameter.Type.SpecialType == SpecialType.System_String)
            {
                result.Add(new MyCodeAction(
                    FeaturesResources.Add_string_IsNullOrEmpty_check,
                    c => AddStringCheckAsync(document, parameter, functionDeclaration, method, blockStatementOpt, nameof(string.IsNullOrEmpty), c)));

                result.Add(new MyCodeAction(
                    FeaturesResources.Add_string_IsNullOrWhiteSpace_check,
                    c => AddStringCheckAsync(document, parameter, functionDeclaration, method, blockStatementOpt, nameof(string.IsNullOrWhiteSpace), c)));
            }

            return result.ToImmutableAndFree();
        }

        protected abstract bool CanOffer(SyntaxNode body);

        private bool ContainsNullCoalesceCheck(
            ISyntaxFactsService syntaxFacts, SemanticModel semanticModel,
            IOperation statement, IParameterSymbol parameter,
            CancellationToken cancellationToken)
        {
            // Look for anything in this statement of the form "p ?? throw ...".
            // If so, we'll consider this parameter checked for null and we can stop immediately.

            var syntax = statement.Syntax;
            foreach (var coalesceNode in syntax.DescendantNodes().OfType<TBinaryExpressionSyntax>())
            {
                var operation = semanticModel.GetOperation(coalesceNode, cancellationToken);
                if (operation is ICoalesceOperation coalesceExpression)
                {
                    if (IsParameterReference(coalesceExpression.Value, parameter) &&
                        syntaxFacts.IsThrowExpression(coalesceExpression.WhenNull.Syntax))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsIfNullCheck(IOperation statement, IParameterSymbol parameter)
        {
            if (statement is IConditionalOperation ifStatement)
            {
                var condition = ifStatement.Condition;
                condition = UnwrapImplicitConversion(condition);

                if (condition is IBinaryOperation binaryOperator)
                {
                    // Look for code of the form "if (p == null)" or "if (null == p)"
                    if (IsNullCheck(binaryOperator.LeftOperand, binaryOperator.RightOperand, parameter) ||
                        IsNullCheck(binaryOperator.RightOperand, binaryOperator.LeftOperand, parameter))
                    {
                        return true;
                    }
                }
                else if (condition is IIsPatternOperation isPatternOperation &&
                         isPatternOperation.Pattern is IConstantPatternOperation constantPattern)
                {
                    // Look for code of the form "if (p is null)"
                    if (IsNullCheck(constantPattern.Value, isPatternOperation.Value, parameter))
                    {
                        return true;
                    }
                }
                else if (parameter.Type.SpecialType == SpecialType.System_String &&
                         IsStringCheck(condition, parameter))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsStringCheck(IOperation condition, IParameterSymbol parameter)
        {
            if (condition is IInvocationOperation invocation &&
                invocation.Arguments.Length == 1 &&
                IsParameterReference(invocation.Arguments[0].Value, parameter))
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
            => IsNullLiteral(UnwrapImplicitConversion(operand1)) && IsParameterReference(operand2, parameter);

        private bool IsNullLiteral(IOperation operand)
            => operand is ILiteralOperation literal &&
               literal.ConstantValue.HasValue &&
               literal.ConstantValue.Value == null;

        private async Task<Document> AddNullCheckAsync(
            Document document,
            IParameterSymbol parameter,
            SyntaxNode functionDeclaration,
            IMethodSymbol method,
            IBlockOperation blockStatementOpt,
            CancellationToken cancellationToken)
        {
            // First see if we can convert a statement of the form "this.s = s" into "this.s = s ?? throw ...".
            var documentOpt = await TryAddNullCheckToAssignmentAsync(
                document, parameter, blockStatementOpt, cancellationToken).ConfigureAwait(false);

            if (documentOpt != null)
            {
                return documentOpt;
            }

            // If we can't, then just offer to add an "if (s == null)" statement.
            return await AddNullCheckStatementAsync(
                document, parameter, functionDeclaration, method, blockStatementOpt,
                (c, g) => CreateNullCheckStatement(c, g, parameter),
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<Document> AddStringCheckAsync(
            Document document,
            IParameterSymbol parameter,
            SyntaxNode functionDeclaration,
            IMethodSymbol method,
            IBlockOperation blockStatementOpt,
            string methodName,
            CancellationToken cancellationToken)
        {
            return await AddNullCheckStatementAsync(
                document, parameter, functionDeclaration, method, blockStatementOpt,
                (c, g) => CreateStringCheckStatement(c, g, parameter, methodName),
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<Document> AddNullCheckStatementAsync(
            Document document,
            IParameterSymbol parameter,
            SyntaxNode functionDeclaration,
            IMethodSymbol method,
            IBlockOperation blockStatementOpt,
            Func<Compilation, SyntaxGenerator, TStatementSyntax> generateNullCheck,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var nullCheckStatement = generateNullCheck(compilation, editor.Generator);

            // We may be inserting a statement into a single-line container.  In that case,
            // we don't want the formatting engine to think this construct should stay single-line
            // so add a newline after the check to help dissuade it from thinking we should stay
            // on a single line.
            nullCheckStatement = nullCheckStatement.WithAppendedTrailingTrivia(
                editor.Generator.ElasticCarriageReturnLineFeed);

            // Find a good location to add the null check. In general, we want the order of checks
            // and assignments in the constructor to match the order of parameters in the method
            // signature.
            var statementToAddAfter = GetStatementToAddNullCheckAfter(
                semanticModel, parameter, blockStatementOpt, cancellationToken);
            InsertStatement(editor, functionDeclaration, method, statementToAddAfter, nullCheckStatement);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private static TStatementSyntax CreateNullCheckStatement(
            Compilation compilation, SyntaxGenerator generator, IParameterSymbol parameter)
        {
            // generates: if (s == null) throw new ArgumentNullException(nameof(s))
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
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // generates: if (string.IsXXX(s)) throw new ArgumentException("message", nameof(s))
            return (TStatementSyntax)generator.IfStatement(
                generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.TypeExpression(stringType),
                        generator.IdentifierName(methodName)),
                    generator.Argument(generator.IdentifierName(parameter.Name))),
                SpecializedCollections.SingletonEnumerable(
                    generator.ThrowStatement(
                        CreateArgumentException(compilation, generator, parameter))));
        }

        private SyntaxNode GetStatementToAddNullCheckAfter(
            SemanticModel semanticModel,
            IParameterSymbol parameter,
            IBlockOperation blockStatementOpt,
            CancellationToken cancellationToken)
        {
            if (blockStatementOpt == null)
            {
                return null;
            }

            var methodSymbol = (IMethodSymbol)parameter.ContainingSymbol;
            var parameterIndex = methodSymbol.Parameters.IndexOf(parameter);

            // look for an existing check for a parameter that comes before us.
            // If we find one, we'll add ourselves after that parameter check.
            for (var i = parameterIndex - 1; i >= 0; i--)
            {
                var checkStatement = TryFindParameterCheckStatement(
                    semanticModel, methodSymbol.Parameters[i], blockStatementOpt, cancellationToken);
                if (checkStatement != null)
                {
                    return checkStatement.Syntax;
                }
            }

            // look for an existing check for a parameter that comes before us.
            // If we find one, we'll add ourselves after that parameter check.
            for (var i = parameterIndex + 1; i < methodSymbol.Parameters.Length; i++)
            {
                var checkStatement = TryFindParameterCheckStatement(
                    semanticModel, methodSymbol.Parameters[i], blockStatementOpt, cancellationToken);
                if (checkStatement != null)
                {
                    var statementIndex = blockStatementOpt.Operations.IndexOf(checkStatement);
                    return statementIndex > 0 ? blockStatementOpt.Operations[statementIndex - 1].Syntax : null;
                }
            }

            // Just place the null check at the start of the block
            return null;
        }

        /// <summary>
        /// Tries to find an if-statement that looks like it is checking the provided parameter
        /// in some way.  If we find a match, we'll place our new null-check statement before/after
        /// this statement as appropriate.
        /// </summary>
        private IOperation TryFindParameterCheckStatement(
            SemanticModel semanticModel,
            IParameterSymbol parameterSymbol,
            IBlockOperation blockStatementOpt,
            CancellationToken cancellationToken)
        {
            if (blockStatementOpt != null)
            {
                foreach (var statement in blockStatementOpt.Operations)
                {
                    if (statement is IConditionalOperation ifStatement)
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
            }

            return null;
        }

        private async Task<Document> TryAddNullCheckToAssignmentAsync(
            Document document,
            IParameterSymbol parameter,
            IBlockOperation blockStatementOpt,
            CancellationToken cancellationToken)
        {
            // tries to convert "this.s = s" into "this.s = s ?? throw ...".  Only supported
            // in languages that have a throw-expression, and only if the user has set the
            // preference that they like throw-expressions.

            if (blockStatementOpt == null)
            {
                return null;
            }

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

            // Look through all the top level statements in the block to see if we can
            // find an existing field/property assignment involving this parameter.
            var containingType = parameter.ContainingType;
            foreach (var statement in blockStatementOpt.Operations)
            {
                if (IsFieldOrPropertyAssignment(statement, containingType, out var assignmentExpression) &&
                    IsParameterReference(assignmentExpression.Value, parameter))
                {
                    // Found one.  Convert it to a coalesce expression with an appropriate 
                    // throw expression.
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

        private static SyntaxNode GetTypeNode(
            Compilation compilation, SyntaxGenerator generator, Type type)
        {
            var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);
            if (typeSymbol == null)
            {
                return generator.QualifiedName(
                    generator.IdentifierName(nameof(System)),
                    generator.IdentifierName(type.Name));
            }

            return generator.TypeExpression(typeSymbol);
        }

        private static SyntaxNode CreateArgumentNullException(
            Compilation compilation, SyntaxGenerator generator, IParameterSymbol parameter)
        {
            return generator.ObjectCreationExpression(
                GetTypeNode(compilation, generator, typeof(ArgumentNullException)),
                generator.NameOfExpression(generator.IdentifierName(parameter.Name)));
        }

        private static SyntaxNode CreateArgumentException(
            Compilation compilation, SyntaxGenerator generator, IParameterSymbol parameter)
        {
            // Note "message" is not localized.  It is the name of the first parameter of 
            // "ArgumentException"
            return generator.ObjectCreationExpression(
                GetTypeNode(compilation, generator, typeof(ArgumentException)),
                generator.LiteralExpression("message"),
                generator.NameOfExpression(generator.IdentifierName(parameter.Name)));
        }
    }
}
