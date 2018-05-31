// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertForEachToLinqQueryProvider)), Shared]
    internal sealed class CSharpConvertForEachToLinqQueryProvider : AbstractConvertForEachToLinqQueryProvider<ForEachStatementSyntax, StatementSyntax>
    {
        protected override IConverter CreateDefaultConverter(ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo)
            => new DefaultConverter(forEachInfo);

        protected override ForEachInfo<ForEachStatementSyntax, StatementSyntax> CreateForEachInfo(ForEachStatementSyntax forEachStatement)
        {
            var identifiers = new List<SyntaxToken>();
            identifiers.Add(forEachStatement.Identifier);
            var convertingNodes = new List<ExtendedSyntaxNode>();
            var current = forEachStatement.Statement;
            IEnumerable<StatementSyntax> statementsCannotBeConverted = null;
            var trailingComments = new List<SyntaxTrivia>();
            // TODO shoild be filterted laters. should be renamed from comments to trivia
            var currentLeadingComments = new List<SyntaxTrivia>();

            // Setting statementsCannotBeConverted to anything means that we stop processing.
            while (statementsCannotBeConverted == null)
            {
                switch (current.Kind())
                {
                    case SyntaxKind.Block:
                        // TODO we're losing a lot of comments here by breaking the block
                        var block = (BlockSyntax)current;
                        currentLeadingComments.AddRange(block.OpenBraceToken.LeadingTrivia);
                        currentLeadingComments.AddRange(block.OpenBraceToken.TrailingTrivia);
                        trailingComments.AddRange(block.CloseBraceToken.TrailingTrivia);
                        trailingComments.AddRange(block.CloseBraceToken.LeadingTrivia);
                        var array = block.Statements.ToArray();
                        if (array.Any())
                        {
                            // Process all statements except the last one.
                            for (int i = 0; i < array.Length - 1; i++)
                            {
                                var statement = array[i];
                                if (statement is LocalDeclarationStatementSyntax localDeclarationStatement)
                                {
                                    ProcessLocalDeclarationStatement(localDeclarationStatement);
                                }
                                else
                                {
                                    statementsCannotBeConverted = array.Skip(i);
                                    break;
                                }
                            }

                            // Process the last statement separately.
                            current = array.Last();
                        }
                        else
                        {
                            statementsCannotBeConverted = Enumerable.Empty<StatementSyntax>();
                        }

                        break;

                    case SyntaxKind.ForEachStatement:
                        var currentForEachStatement = (ForEachStatementSyntax)current;
                        identifiers.Add(currentForEachStatement.Identifier);
                        convertingNodes.Add(new ExtendedSyntaxNode(currentForEachStatement, currentLeadingComments, Enumerable.Empty<SyntaxTrivia>()));
                        currentLeadingComments = new List<SyntaxTrivia>();
                        current = currentForEachStatement.Statement;
                        break;

                    case SyntaxKind.IfStatement:
                        var ifStatement = (IfStatementSyntax)current;
                        if (ifStatement.Else == null)
                        {
                            convertingNodes.Add(new ExtendedSyntaxNode(ifStatement, currentLeadingComments, Enumerable.Empty<SyntaxTrivia>()));
                            currentLeadingComments = new List<SyntaxTrivia>();
                            current = ifStatement.Statement;
                            break;
                        }
                        else
                        {
                            statementsCannotBeConverted = new[] { current };
                            break;
                        }

                    case SyntaxKind.LocalDeclarationStatement:
                        // This is a situation with "var a = something;" s the most internal statements inside the loop.
                        ProcessLocalDeclarationStatement((LocalDeclarationStatementSyntax)current);
                        statementsCannotBeConverted = Enumerable.Empty<StatementSyntax>();
                        break;

                    case SyntaxKind.EmptyStatement:
                        statementsCannotBeConverted = Enumerable.Empty<StatementSyntax>();
                        break;

                    default:
                        statementsCannotBeConverted = new[] { current };
                        break;
                }
            }

            // TODO should we reverse trailing comments?
            return new ForEachInfo<ForEachStatementSyntax, StatementSyntax>(forEachStatement, convertingNodes, identifiers, statementsCannotBeConverted, currentLeadingComments, trailingComments);

            void ProcessLocalDeclarationStatement(LocalDeclarationStatementSyntax localDeclarationStatement)
            {
                // TODO either should be getleadetingcomments for node, or better filter trivial later when applying.
                var localDeclarationLeadingComments = new IEnumerable<SyntaxTrivia>[] { currentLeadingComments, localDeclarationStatement.Declaration.Type.GetLeadingTrivia(), localDeclarationStatement.Declaration.GetTrailingTrivia() }.SelectMany(x => x);
                var localDeclarationTrailingComments = new[] { localDeclarationStatement.SemicolonToken.LeadingTrivia, localDeclarationStatement.SemicolonToken.TrailingTrivia }.SelectMany(x => x);
                for (int i = 0; i < localDeclarationStatement.Declaration.Variables.Count; i++)
                {
                    var variable = localDeclarationStatement.Declaration.Variables[i];
                    convertingNodes.Add(new ExtendedSyntaxNode(
                        variable,
                        i == 0 ? localDeclarationLeadingComments : Enumerable.Empty<SyntaxTrivia>(),
                        i == localDeclarationStatement.Declaration.Variables.Count - 1 ? localDeclarationTrailingComments : Enumerable.Empty<SyntaxTrivia>()));
                    identifiers.Add(variable.Identifier);
                }

                currentLeadingComments = new List<SyntaxTrivia>();
            }
        }

        protected override bool TryBuildSpecificConverter(
            ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo,
            SemanticModel semanticModel,
            StatementSyntax statementCannotBeConverted,
            CancellationToken cancellationToken,
            out IConverter converter)
        {
            switch (statementCannotBeConverted.Kind())
            {
                case SyntaxKind.ExpressionStatement:
                    var expression = ((ExpressionStatementSyntax)statementCannotBeConverted).Expression;
                    switch (expression.Kind())
                    {
                        case SyntaxKind.PostIncrementExpression:
                            // No matter what can be used as the last select statement for the case of Count. We use SyntaxFactory.IdentifierName(forEachStatement.Identifier).
                            converter = new ToCountConterter(
                                forEachInfo,
                                SyntaxFactory.IdentifierName(forEachInfo.ForEachStatement.Identifier), // TOOD comments
                                ((PostfixUnaryExpressionSyntax)expression).Operand);
                            return true;

                        case SyntaxKind.InvocationExpression:
                            var invocationExpression = (InvocationExpressionSyntax)expression;
                            if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression &&
                                semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                                IsList(methodSymbol.ContainingType, semanticModel) &&
                                methodSymbol.Name.Equals(nameof(IList.Add)) &&
                                methodSymbol.Parameters.Length == 1)
                            {
                                converter = new ToToListConverter(
                                    forEachInfo,
                                    invocationExpression.ArgumentList.Arguments.Single().Expression, // TODO comments
                                    memberAccessExpression.Expression);
                                return true;
                            }

                            break;
                    }

                    break;

                case SyntaxKind.YieldReturnStatement:
                    var memberDeclaration = semanticModel.GetEnclosingSymbol(forEachInfo.ForEachStatement.SpanStart, cancellationToken).DeclaringSyntaxReferences.Single().GetSyntax();
                    var yieldStatements = memberDeclaration.DescendantNodes().OfType<YieldStatementSyntax>();

                    if (forEachInfo.ForEachStatement.IsParentKind(SyntaxKind.Block) && forEachInfo.ForEachStatement.Parent.Parent == memberDeclaration)
                    {
                        var statementsOnBlockWithForEach = ((BlockSyntax)forEachInfo.ForEachStatement.Parent).Statements;
                        var lastStatement = statementsOnBlockWithForEach.Last();
                        if (yieldStatements.Count() == 1 && lastStatement == forEachInfo.ForEachStatement)
                        {
                            converter = new YieldReturnConverter(forEachInfo, (YieldStatementSyntax)statementCannotBeConverted, nodeToDelete: null);
                            return true;
                        }

                        // foreach()
                        // {
                        //   yield return ...;
                        // }
                        // yield break;
                        // end of member
                        if (yieldStatements.Count() == 2 &&
                            lastStatement.Kind() == SyntaxKind.YieldBreakStatement &&
                            !lastStatement.ContainsDirectives &&
                            statementsOnBlockWithForEach.ElementAt(statementsOnBlockWithForEach.Count - 2) == forEachInfo.ForEachStatement)
                        {
                            // This removes the yield break.
                            converter = new YieldReturnConverter(forEachInfo, (YieldStatementSyntax)statementCannotBeConverted, nodeToDelete: lastStatement);
                            return true;
                        }
                    }

                    break;
            }

            converter = default;
            return false;
        }

        protected override SyntaxNode AddLinqUsing(SyntaxNode root)
        {
            const string linqNamespaceName = "System.Linq";
            if (root is CompilationUnitSyntax compilationUnit &&
                !compilationUnit.Usings.Any(existingUsing => existingUsing.Name.ToString().Equals(linqNamespaceName)))
            {
                return compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(linqNamespaceName)));
            }

            return root;
        }

        internal static bool IsList(ITypeSymbol typeSymbol, SemanticModel semanticModel)
            => Equals(typeSymbol?.OriginalDefinition, semanticModel.Compilation.GetTypeByMetadataName(typeof(List<>).FullName));
    }
}
