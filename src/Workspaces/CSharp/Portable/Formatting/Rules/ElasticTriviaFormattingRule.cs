// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class ElasticTriviaFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp Elastic trivia Formatting Rule";

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, in NextSuppressOperationAction nextOperation)
        {
            nextOperation.Invoke();

            if (!node.ContainsAnnotations)
            {
                return;
            }

            AddPropertyDeclarationSuppressOperations(list, node);

            AddInitializerSuppressOperations(list, node);
        }

        private static void AddPropertyDeclarationSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            if (node is BasePropertyDeclarationSyntax {
                AccessorList: {
                }
            } basePropertyDeclaration && basePropertyDeclaration.AccessorList.Accessors.All(a => a.Body == null) && basePropertyDeclaration.GetAnnotatedTrivia(SyntaxAnnotation.ElasticAnnotation).Any())
            {
                var tokens = basePropertyDeclaration.GetFirstAndLastMemberDeclarationTokensAfterAttributes();

                list.Add(FormattingOperations.CreateSuppressOperation(tokens.Item1, tokens.Item2, SuppressOption.NoWrapping | SuppressOption.IgnoreElasticWrapping));
            }
        }

        private void AddInitializerSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            var initializer = GetInitializerNode(node);
            var lastTokenOfType = GetLastTokenOfType(node);
            if (initializer != null && lastTokenOfType != null)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, lastTokenOfType.Value, initializer.CloseBraceToken, SuppressOption.IgnoreElasticWrapping);
                return;
            }

            if (node is AnonymousObjectCreationExpressionSyntax anonymousCreationNode)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, anonymousCreationNode.NewKeyword, anonymousCreationNode.CloseBraceToken, SuppressOption.IgnoreElasticWrapping);
                return;
            }
        }

        private InitializerExpressionSyntax GetInitializerNode(SyntaxNode node)
            => node switch
            {
                ObjectCreationExpressionSyntax objectCreationNode => objectCreationNode.Initializer,
                ArrayCreationExpressionSyntax arrayCreationNode => arrayCreationNode.Initializer,
                ImplicitArrayCreationExpressionSyntax implicitArrayNode => implicitArrayNode.Initializer,
                _ => null,
            };

        private SyntaxToken? GetLastTokenOfType(SyntaxNode node)
        {
            if (node is ObjectCreationExpressionSyntax objectCreationNode)
            {
                return objectCreationNode.Type.GetLastToken();
            }

            if (node is ArrayCreationExpressionSyntax arrayCreationNode)
            {
                return arrayCreationNode.Type.GetLastToken();
            }

            if (node is ImplicitArrayCreationExpressionSyntax implicitArrayNode)
            {
                return implicitArrayNode.CloseBracketToken;
            }

            return null;
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
        {
            var operation = nextOperation.Invoke();
            if (operation == null)
            {
                // If there are more than one Type Parameter Constraint Clause then each go in separate line
                if (CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken) &&
                    currentToken.IsKind(SyntaxKind.WhereKeyword) &&
                    currentToken.Parent.IsKind(SyntaxKind.TypeParameterConstraintClause))
                {
                    // Check if there is another TypeParameterConstraintClause before
                    if (previousToken.Parent.Ancestors().OfType<TypeParameterConstraintClauseSyntax>().Any())
                    {
                        return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                    }

                    // Check if there is another TypeParameterConstraintClause after
                    var firstTokenAfterTypeConstraint = currentToken.Parent.GetLastToken().GetNextToken();
                    var lastTokenForTypeConstraint = currentToken.Parent.GetLastToken().GetNextToken();
                    if (CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(lastTokenForTypeConstraint, firstTokenAfterTypeConstraint) &&
                        firstTokenAfterTypeConstraint.IsKind(SyntaxKind.WhereKeyword) &&
                        firstTokenAfterTypeConstraint.Parent.IsKind(SyntaxKind.TypeParameterConstraintClause))
                    {
                        return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                    }
                }

                return null;
            }

            // if operation is already forced, return as it is.
            if (operation.Option == AdjustNewLinesOption.ForceLines)
            {
                return operation;
            }

            if (!CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken))
            {
                return operation;
            }

            var betweenMemberOperation = GetAdjustNewLinesOperationBetweenMembers(previousToken, currentToken);
            if (betweenMemberOperation != null)
            {
                return betweenMemberOperation;
            }

            var line = Math.Max(LineBreaksAfter(previousToken, currentToken), operation.Line);
            if (line == 0)
            {
                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
            }

            return CreateAdjustNewLinesOperation(line, AdjustNewLinesOption.ForceLines);
        }

        private AdjustNewLinesOperation GetAdjustNewLinesOperationBetweenMembers(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            if (!FormattingRangeHelper.InBetweenTwoMembers(previousToken, currentToken))
            {
                return null;
            }

            var previousMember = FormattingRangeHelper.GetEnclosingMember(previousToken);
            var nextMember = FormattingRangeHelper.GetEnclosingMember(currentToken);
            if (previousMember == null || nextMember == null)
            {
                return null;
            }

            // see whether first non whitespace trivia after before the current member is a comment or not
            var triviaList = currentToken.LeadingTrivia;
            var firstNonWhitespaceTrivia = triviaList.FirstOrDefault(trivia => !IsWhitespace(trivia));
            if (firstNonWhitespaceTrivia.IsRegularOrDocComment())
            {
                // the first one is a comment, add two more lines than existing number of lines
                var numberOfLines = GetNumberOfLines(triviaList);
                var numberOfLinesBeforeComment = GetNumberOfLines(triviaList.Take(triviaList.IndexOf(firstNonWhitespaceTrivia)));
                var addedLines = (numberOfLinesBeforeComment < 1) ? 2 : 1;
                return CreateAdjustNewLinesOperation(numberOfLines + addedLines, AdjustNewLinesOption.ForceLines);
            }

            // If we have two members of the same kind, we won't insert a blank line if both members
            // have any content (e.g. accessors bodies, non-empty method bodies, etc.).
            if (previousMember.Kind() == nextMember.Kind())
            {
                // Easy cases:
                if (previousMember.Kind() == SyntaxKind.FieldDeclaration ||
                    previousMember.Kind() == SyntaxKind.EventFieldDeclaration)
                {
                    // Ensure that fields and events are each declared on a separate line.
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines);
                }

                // Don't insert a blank line between properties, indexers or events with no accessors
                if (previousMember is BasePropertyDeclarationSyntax previousProperty)
                {
                    var nextProperty = (BasePropertyDeclarationSyntax)nextMember;

                    if (previousProperty?.AccessorList?.Accessors.All(a => a.Body == null) == true &&
                        nextProperty?.AccessorList?.Accessors.All(a => a.Body == null) == true)
                    {
                        return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                    }
                }

                // Don't insert a blank line between methods with no bodies
                if (previousMember is BaseMethodDeclarationSyntax previousMethod)
                {
                    var nextMethod = (BaseMethodDeclarationSyntax)nextMember;

                    if (previousMethod.Body == null &&
                        nextMethod.Body == null)
                    {
                        return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                    }
                }
            }

            return FormattingOperations.CreateAdjustNewLinesOperation(2 /* +1 for member itself and +1 for a blank line*/, AdjustNewLinesOption.ForceLines);
        }

        public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustSpacesOperation nextOperation)
        {
            var operation = nextOperation.Invoke();
            if (operation == null)
            {
                return null;
            }

            // if operation is already forced, return as it is.
            if (operation.Option == AdjustSpacesOption.ForceSpaces)
            {
                return operation;
            }

            if (CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken))
            {
                // current implementation of engine gives higher priority on new line operations over space operations if
                // two are conflicting.
                // ex) new line operation says add 1 line between tokens, and
                //     space operation says give 1 space between two tokens (basically means remove new lines)
                //     then, engine will pick new line operation and ignore space operation

                // make attributes have a space following
                if (previousToken.IsKind(SyntaxKind.CloseBracketToken) && previousToken.Parent is AttributeListSyntax
                    && !(currentToken.Parent is AttributeListSyntax))
                {
                    return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }

                // make every operation forced
                return CreateAdjustSpacesOperation(Math.Max(0, operation.Space), AdjustSpacesOption.ForceSpaces);
            }

            return operation;
        }

        // copied from compiler formatter to have same base forced format
        private int LineBreaksAfter(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            if (currentToken.Kind() == SyntaxKind.None)
            {
                return 0;
            }

            switch (previousToken.Kind())
            {
                case SyntaxKind.None:
                    return 0;

                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.FinallyKeyword:
                    return 1;

                case SyntaxKind.CloseBraceToken:
                    return LineBreaksAfterCloseBrace(currentToken);

                case SyntaxKind.CloseParenToken:
                    return (((previousToken.Parent is StatementSyntax) && currentToken.Parent != previousToken.Parent)
                        || currentToken.Kind() == SyntaxKind.OpenBraceToken) ? 1 : 0;

                case SyntaxKind.CloseBracketToken:
                    // Assembly and module-level attributes followed by non-attributes should have a blank line after
                    // them, unless it's the end of the file which will already have a blank line.
                    if (previousToken.Parent is AttributeListSyntax parent)
                    {
                        if (parent.Target != null &&
                            (parent.Target.Identifier.IsKindOrHasMatchingText(SyntaxKind.AssemblyKeyword) ||
                             parent.Target.Identifier.IsKindOrHasMatchingText(SyntaxKind.ModuleKeyword)))
                        {
                            if (!currentToken.IsKind(SyntaxKind.EndOfFileToken) && !(currentToken.Parent is AttributeListSyntax))
                            {
                                return 2;
                            }
                        }

                        if (previousToken.GetAncestor<ParameterSyntax>() == null
                            && previousToken.GetAncestor<TypeParameterSyntax>() == null)
                        {
                            return 1;
                        }
                    }

                    break;

                case SyntaxKind.SemicolonToken:
                    return LineBreaksAfterSemicolon(previousToken, currentToken);

                case SyntaxKind.CommaToken:
                    return previousToken.Parent is EnumDeclarationSyntax ? 1 : 0;

                case SyntaxKind.ElseKeyword:
                    return currentToken.Kind() != SyntaxKind.IfKeyword ? 1 : 0;

                case SyntaxKind.ColonToken:
                    if (previousToken.Parent is LabeledStatementSyntax || previousToken.Parent is SwitchLabelSyntax)
                    {
                        return 1;
                    }

                    break;
            }

            if ((currentToken.Kind() == SyntaxKind.FromKeyword && currentToken.Parent.Kind() == SyntaxKind.FromClause) ||
                (currentToken.Kind() == SyntaxKind.LetKeyword && currentToken.Parent.Kind() == SyntaxKind.LetClause) ||
                (currentToken.Kind() == SyntaxKind.WhereKeyword && currentToken.Parent.Kind() == SyntaxKind.WhereClause) ||
                (currentToken.Kind() == SyntaxKind.JoinKeyword && currentToken.Parent.Kind() == SyntaxKind.JoinClause) ||
                (currentToken.Kind() == SyntaxKind.JoinKeyword && currentToken.Parent.Kind() == SyntaxKind.JoinIntoClause) ||
                (currentToken.Kind() == SyntaxKind.OrderByKeyword && currentToken.Parent.Kind() == SyntaxKind.OrderByClause) ||
                (currentToken.Kind() == SyntaxKind.SelectKeyword && currentToken.Parent.Kind() == SyntaxKind.SelectClause) ||
                (currentToken.Kind() == SyntaxKind.GroupKeyword && currentToken.Parent.Kind() == SyntaxKind.GroupClause))
            {
                return 1;
            }

            switch (currentToken.Kind())
            {
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.CloseBraceToken:
                case SyntaxKind.ElseKeyword:
                case SyntaxKind.FinallyKeyword:
                    return 1;

                case SyntaxKind.OpenBracketToken:
                    // Assembly and module-level attributes preceded by non-attributes should have
                    // a blank line separating them.
                    if (currentToken.Parent is AttributeListSyntax parent)
                    {
                        if (parent.Target != null)
                        {
                            if (parent.Target.Identifier == SyntaxFactory.Token(SyntaxKind.AssemblyKeyword) ||
                                parent.Target.Identifier == SyntaxFactory.Token(SyntaxKind.ModuleKeyword))
                            {
                                if (!(previousToken.Parent is AttributeListSyntax))
                                {
                                    return 2;
                                }
                            }
                        }

                        // Attributes on parameters should have no lines between them.
                        if (parent.Parent is ParameterSyntax)
                        {
                            return 0;
                        }

                        return 1;
                    }

                    break;

                case SyntaxKind.WhereKeyword:
                    return previousToken.Parent is TypeParameterListSyntax ? 1 : 0;
            }

            return 0;
        }

        private static int LineBreaksAfterCloseBrace(SyntaxToken nextToken)
        {
            if (nextToken.Kind() == SyntaxKind.CloseBraceToken)
            {
                return 1;
            }
            else if (
                nextToken.Kind() == SyntaxKind.CatchKeyword ||
                nextToken.Kind() == SyntaxKind.FinallyKeyword ||
                nextToken.Kind() == SyntaxKind.ElseKeyword)
            {
                return 1;
            }
            else if (
                nextToken.Kind() == SyntaxKind.WhileKeyword &&
                nextToken.Parent.Kind() == SyntaxKind.DoStatement)
            {
                return 1;
            }
            else if (nextToken.Kind() == SyntaxKind.EndOfFileToken)
            {
                return 0;
            }
            else
            {
                return 2;
            }
        }

        private static int LineBreaksAfterSemicolon(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            if (previousToken.Parent is ForStatementSyntax)
            {
                return 0;
            }
            else if (currentToken.Kind() == SyntaxKind.CloseBraceToken)
            {
                return 1;
            }
            else if (previousToken.Parent is UsingDirectiveSyntax)
            {
                return currentToken.Parent is UsingDirectiveSyntax ? 1 : 2;
            }
            else if (previousToken.Parent is ExternAliasDirectiveSyntax)
            {
                return currentToken.Parent is ExternAliasDirectiveSyntax ? 1 : 2;
            }
            else if (currentToken.Parent is LocalFunctionStatementSyntax)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }

        private bool IsWhitespace(SyntaxTrivia trivia)
        {
            return trivia.Kind() == SyntaxKind.WhitespaceTrivia
                || trivia.Kind() == SyntaxKind.EndOfLineTrivia;
        }

        private int GetNumberOfLines(IEnumerable<SyntaxTrivia> triviaList)
        {
            return triviaList.Sum(t => t.ToFullString().Replace("\r\n", "\r").Cast<char>().Count(c => SyntaxFacts.IsNewLine(c)));
        }
    }
}
