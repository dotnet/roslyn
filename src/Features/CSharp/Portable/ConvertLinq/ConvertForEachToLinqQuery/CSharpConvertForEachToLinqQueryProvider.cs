// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using SyntaxNodeOrTokenExtensions = Microsoft.CodeAnalysis.Shared.Extensions.SyntaxNodeOrTokenExtensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertForEachToLinqQuery), Shared]
    internal sealed class CSharpConvertForEachToLinqQueryProvider
        : AbstractConvertForEachToLinqQueryProvider<ForEachStatementSyntax, StatementSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertForEachToLinqQueryProvider()
        {
        }

        protected override IConverter<ForEachStatementSyntax, StatementSyntax> CreateDefaultConverter(
            ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo)
            => new DefaultConverter(forEachInfo);

        protected override ForEachInfo<ForEachStatementSyntax, StatementSyntax> CreateForEachInfo(
            ForEachStatementSyntax forEachStatement,
            SemanticModel semanticModel,
            bool convertLocalDeclarations)
        {
            var identifiersBuilder = ArrayBuilder<SyntaxToken>.GetInstance();
            identifiersBuilder.Add(forEachStatement.Identifier);
            var convertingNodesBuilder = ArrayBuilder<ExtendedSyntaxNode>.GetInstance();
            IEnumerable<StatementSyntax>? statementsCannotBeConverted = null;
            var trailingTokensBuilder = ArrayBuilder<SyntaxToken>.GetInstance();
            var currentLeadingTokens = ArrayBuilder<SyntaxToken>.GetInstance();

            var current = forEachStatement.Statement;
            // Traverse descendants of the forEachStatement.
            // If a statement traversed can be converted into a query clause, 
            //  a. Add it to convertingNodesBuilder.
            //  b. set the current to its nested statement and proceed.
            // Otherwise, set statementsCannotBeConverted and stop processing.
            while (statementsCannotBeConverted == null)
            {
                switch (current.Kind())
                {
                    case SyntaxKind.Block:
                        var block = (BlockSyntax)current;
                        // Keep comment trivia from braces to attach them to the qeury created.
                        currentLeadingTokens.Add(block.OpenBraceToken);
                        trailingTokensBuilder.Add(block.CloseBraceToken);
                        var array = block.Statements.ToArray();
                        if (array.Length > 0)
                        {
                            // All except the last one can be local declaration statements like
                            // {
                            //   var a = 0;
                            //   var b = 0;
                            //   if (x != y) <- this is the last one in the block. 
                            // We can support it to be a complex foreach or if or whatever. So, set the current to it.
                            //   ...
                            // }
                            for (var i = 0; i < array.Length - 1; i++)
                            {
                                var statement = array[i];
                                if (!(statement is LocalDeclarationStatementSyntax localDeclarationStatement &&
                                    TryProcessLocalDeclarationStatement(localDeclarationStatement)))
                                {
                                    // If this one is a local function declaration or has an empty initializer, stop processing.
                                    statementsCannotBeConverted = array.Skip(i).ToArray();
                                    break;
                                }
                            }

                            // Process the last statement separately.
                            current = array.Last();
                        }
                        else
                        {
                            // Silly case: the block is empty. Stop processing.
                            statementsCannotBeConverted = Enumerable.Empty<StatementSyntax>();
                        }

                        break;

                    case SyntaxKind.ForEachStatement:
                        // foreach can always be converted to a from clause.
                        var currentForEachStatement = (ForEachStatementSyntax)current;
                        identifiersBuilder.Add(currentForEachStatement.Identifier);
                        convertingNodesBuilder.Add(new ExtendedSyntaxNode(currentForEachStatement, currentLeadingTokens.ToImmutableAndFree(), Enumerable.Empty<SyntaxToken>()));
                        currentLeadingTokens = ArrayBuilder<SyntaxToken>.GetInstance();
                        // Proceed the loop with the nested statement.
                        current = currentForEachStatement.Statement;
                        break;

                    case SyntaxKind.IfStatement:
                        // Prepare conversion of 'if (condition)' into where clauses.
                        // Do not support if-else statements in the conversion.
                        var ifStatement = (IfStatementSyntax)current;
                        if (ifStatement.Else == null)
                        {
                            convertingNodesBuilder.Add(new ExtendedSyntaxNode(
                                ifStatement, currentLeadingTokens.ToImmutableAndFree(), Enumerable.Empty<SyntaxToken>()));
                            currentLeadingTokens = ArrayBuilder<SyntaxToken>.GetInstance();
                            // Proceed the loop with the nested statement.
                            current = ifStatement.Statement;
                            break;
                        }
                        else
                        {
                            statementsCannotBeConverted = new[] { current };
                            break;
                        }

                    case SyntaxKind.LocalDeclarationStatement:
                        // This is a situation with "var a = something;" is the innermost statements inside the loop.
                        var localDeclaration = (LocalDeclarationStatementSyntax)current;
                        if (TryProcessLocalDeclarationStatement(localDeclaration))
                        {
                            statementsCannotBeConverted = Enumerable.Empty<StatementSyntax>();
                        }
                        else
                        {
                            // As above, if there is an empty initializer, stop processing.
                            statementsCannotBeConverted = new[] { current };
                        }

                        break;

                    case SyntaxKind.EmptyStatement:
                        // The innermost statement is an empty statement, stop processing
                        // Example:
                        // foreach(...)
                        // {
                        //    ;<- empty statement
                        // }
                        statementsCannotBeConverted = Enumerable.Empty<StatementSyntax>();
                        break;

                    default:
                        // If no specific case found, stop processing.
                        statementsCannotBeConverted = new[] { current };
                        break;
                }
            }

            // Trailing tokens are collected in the reverse order: from external block down to internal ones. Reverse them.
            trailingTokensBuilder.ReverseContents();

            return new ForEachInfo<ForEachStatementSyntax, StatementSyntax>(
                forEachStatement,
                semanticModel,
                convertingNodesBuilder.ToImmutableAndFree(),
                identifiersBuilder.ToImmutableAndFree(),
                statementsCannotBeConverted.ToImmutableArray(),
                currentLeadingTokens.ToImmutableAndFree(),
                trailingTokensBuilder.ToImmutableAndFree());

            // Try to prepare variable declarations to be converted into separate let clauses.
            bool TryProcessLocalDeclarationStatement(LocalDeclarationStatementSyntax localDeclarationStatement)
            {
                if (!convertLocalDeclarations)
                {
                    return false;
                }

                // Do not support declarations without initialization.
                // int a = 0, b, c = 0;
                if (localDeclarationStatement.Declaration.Variables.All(variable => variable.Initializer != null))
                {
                    var localDeclarationLeadingTrivia = new IEnumerable<SyntaxTrivia>[] {
                    currentLeadingTokens.ToImmutableAndFree().GetTrivia(),
                    localDeclarationStatement.Declaration.Type.GetLeadingTrivia(),
                    localDeclarationStatement.Declaration.Type.GetTrailingTrivia() }.Flatten();
                    currentLeadingTokens = ArrayBuilder<SyntaxToken>.GetInstance();
                    var localDeclarationTrailingTrivia = SyntaxNodeOrTokenExtensions.GetTrivia(localDeclarationStatement.SemicolonToken);
                    var separators = localDeclarationStatement.Declaration.Variables.GetSeparators().ToArray();
                    for (var i = 0; i < localDeclarationStatement.Declaration.Variables.Count; i++)
                    {
                        var variable = localDeclarationStatement.Declaration.Variables[i];
                        convertingNodesBuilder.Add(new ExtendedSyntaxNode(
                            variable,
                            i == 0 ? localDeclarationLeadingTrivia : separators[i - 1].TrailingTrivia,
                            i == localDeclarationStatement.Declaration.Variables.Count - 1
                                ? localDeclarationTrailingTrivia
                                : separators[i].LeadingTrivia));
                        identifiersBuilder.Add(variable.Identifier);
                    }

                    return true;
                }

                return false;
            }
        }

        protected override bool TryBuildSpecificConverter(
            ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo,
            SemanticModel semanticModel,
            StatementSyntax statementCannotBeConverted,
            CancellationToken cancellationToken,
            [NotNullWhen(true)] out IConverter<ForEachStatementSyntax, StatementSyntax>? converter)
        {
            switch (statementCannotBeConverted.Kind())
            {
                case SyntaxKind.ExpressionStatement:
                    var expresisonStatement = (ExpressionStatementSyntax)statementCannotBeConverted;
                    var expression = expresisonStatement.Expression;
                    switch (expression.Kind())
                    {
                        case SyntaxKind.PostIncrementExpression:
                            // Input:
                            // foreach (var x in a)
                            // {
                            //     ...
                            //     c++;
                            // }
                            // Output:
                            // (from x in a ... select x).Count();
                            // Here we put SyntaxFactory.IdentifierName(forEachStatement.Identifier) ('x' in the example) 
                            // into the select clause.
                            var postfixUnaryExpression = (PostfixUnaryExpressionSyntax)expression;
                            var operand = postfixUnaryExpression.Operand;
                            converter = new ToCountConverter(
                                forEachInfo,
                                selectExpression: SyntaxFactory.IdentifierName(forEachInfo.ForEachStatement.Identifier),
                                modifyingExpression: operand,
                                trivia: SyntaxNodeOrTokenExtensions.GetTrivia(
                                    operand, postfixUnaryExpression.OperatorToken, expresisonStatement.SemicolonToken));
                            return true;

                        case SyntaxKind.InvocationExpression:
                            var invocationExpression = (InvocationExpressionSyntax)expression;
                            // Check that there is 'list.Add(item)'.
                            if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression &&
                                semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                                TypeSymbolIsList(methodSymbol.ContainingType, semanticModel) &&
                                methodSymbol.Name == nameof(IList.Add) &&
                                methodSymbol.Parameters.Length == 1 &&
                                invocationExpression.ArgumentList.Arguments.Count == 1)
                            {
                                // Input:
                                // foreach (var x in a)
                                // {
                                //     ...
                                //     list.Add(...);
                                // }
                                // Output:
                                // (from x in a ... select x).ToList();
                                var selectExpression = invocationExpression.ArgumentList.Arguments.Single().Expression;
                                converter = new ToToListConverter(
                                    forEachInfo,
                                    selectExpression,
                                    modifyingExpression: memberAccessExpression.Expression,
                                    trivia: SyntaxNodeOrTokenExtensions.GetTrivia(
                                        memberAccessExpression,
                                        invocationExpression.ArgumentList.OpenParenToken,
                                        invocationExpression.ArgumentList.CloseParenToken,
                                        expresisonStatement.SemicolonToken));
                                return true;
                            }

                            break;
                    }

                    break;

                case SyntaxKind.YieldReturnStatement:
                    var memberDeclarationSymbol = semanticModel.GetEnclosingSymbol(
                        forEachInfo.ForEachStatement.SpanStart, cancellationToken)!;

                    // Using Single() is valid even for partial methods.
                    var memberDeclarationSyntax = memberDeclarationSymbol.DeclaringSyntaxReferences.Single().GetSyntax();

                    var yieldStatementsCount = memberDeclarationSyntax.DescendantNodes().OfType<YieldStatementSyntax>()
                        // Exclude yield statements from nested local functions.
                        .Where(statement => Equals(semanticModel.GetEnclosingSymbol(
                            statement.SpanStart, cancellationToken), memberDeclarationSymbol)).Count();

                    if (forEachInfo.ForEachStatement?.Parent is BlockSyntax block &&
                        block.Parent == memberDeclarationSyntax)
                    {
                        // Check that 
                        // a. There are either just a single 'yield return' or 'yield return' with 'yield break' just after.
                        // b. Those foreach and 'yield break' (if exists) are last statements in the method (do not count local function declaration statements).
                        var statementsOnBlockWithForEach = block.Statements
                            .Where(statement => statement.Kind() != SyntaxKind.LocalFunctionStatement).ToArray();
                        var lastNonLocalFunctionStatement = statementsOnBlockWithForEach.Last();
                        if (yieldStatementsCount == 1 && lastNonLocalFunctionStatement == forEachInfo.ForEachStatement)
                        {
                            converter = new YieldReturnConverter(
                                forEachInfo,
                                (YieldStatementSyntax)statementCannotBeConverted,
                                yieldBreakStatement: null);
                            return true;
                        }

                        // foreach()
                        // {
                        //    yield return ...;
                        // }
                        // yield break;
                        // end of member
                        if (yieldStatementsCount == 2 &&
                            lastNonLocalFunctionStatement.Kind() == SyntaxKind.YieldBreakStatement &&
                            !lastNonLocalFunctionStatement.ContainsDirectives &&
                            statementsOnBlockWithForEach[statementsOnBlockWithForEach.Length - 2] == forEachInfo.ForEachStatement)
                        {
                            // This removes the yield break.
                            converter = new YieldReturnConverter(
                                forEachInfo,
                                (YieldStatementSyntax)statementCannotBeConverted,
                                yieldBreakStatement: (YieldStatementSyntax)lastNonLocalFunctionStatement);
                            return true;
                        }
                    }

                    break;
            }

            converter = null;
            return false;
        }

        protected override SyntaxNode AddLinqUsing(
            IConverter<ForEachStatementSyntax, StatementSyntax> converter,
            SemanticModel semanticModel,
            SyntaxNode root)
        {
            var namespaces = semanticModel.GetUsingNamespacesInScope(converter.ForEachInfo.ForEachStatement);
            if (!namespaces.Any(namespaceSymbol => namespaceSymbol.Name == nameof(System.Linq) &&
                namespaceSymbol.ContainingNamespace.Name == nameof(System)) &&
                root is CompilationUnitSyntax compilationUnit)
            {
                return compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")));
            }

            return root;
        }

        internal static bool TypeSymbolIsList(ITypeSymbol typeSymbol, SemanticModel semanticModel)
            => Equals(typeSymbol?.OriginalDefinition, semanticModel.Compilation.ListOfTType());
    }
}
