// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class SpacingFormattingRule : BaseFormattingRule
    {
        public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustSpacesOperation> nextOperation)
        {
            if (optionSet == null)
            {
                return nextOperation.Invoke();
            }

            System.Diagnostics.Debug.Assert(previousToken.Parent != null && currentToken.Parent != null);

            var previousKind = previousToken.Kind();
            var currentKind = currentToken.Kind();
            var previousParentKind = previousToken.Parent.Kind();
            var currentParentKind = currentToken.Parent.Kind();

            // For Method Declaration
            if (currentToken.IsOpenParenInParameterList() && previousKind == SyntaxKind.IdentifierToken)
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpacingAfterMethodDeclarationName);
            }

            if (previousToken.IsOpenParenInParameterList() && currentToken.IsCloseParenInParameterList())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses);
            }

            if (previousToken.IsOpenParenInParameterList())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis);
            }

            if (currentToken.IsCloseParenInParameterList())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis);
            }

            // For Method Call
            if (currentToken.IsOpenParenInArgumentList())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterMethodCallName);
            }

            if (previousToken.IsOpenParenInArgumentList() && currentToken.IsCloseParenInArgumentList())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses);
            }

            if (previousToken.IsOpenParenInArgumentList())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinMethodCallParentheses);
            }

            if (currentToken.IsCloseParenInArgumentList())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinMethodCallParentheses);
            }

            // For spacing in the parenthesis of typeof, treat like a Method Call
            if (currentKind == SyntaxKind.OpenParenToken && currentParentKind == SyntaxKind.TypeOfExpression)
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterMethodCallName);
            }

            if (previousKind == SyntaxKind.OpenParenToken && previousParentKind == SyntaxKind.TypeOfExpression)
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinMethodCallParentheses);
            }

            if (currentKind == SyntaxKind.CloseParenToken && currentParentKind == SyntaxKind.TypeOfExpression)
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinMethodCallParentheses);
            }

            // For Spacing b/n control flow keyword and paren. Parent check not needed.
            if (currentKind == SyntaxKind.OpenParenToken &&
                (previousKind == SyntaxKind.IfKeyword || previousKind == SyntaxKind.WhileKeyword || previousKind == SyntaxKind.SwitchKeyword ||
                previousKind == SyntaxKind.ForKeyword || previousKind == SyntaxKind.ForEachKeyword || previousKind == SyntaxKind.CatchKeyword ||
                previousKind == SyntaxKind.UsingKeyword))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword);
            }

            // For spacing between parenthesis and expression
            if ((previousParentKind == SyntaxKind.ParenthesizedExpression && previousKind == SyntaxKind.OpenParenToken) ||
                (currentParentKind == SyntaxKind.ParenthesizedExpression && currentKind == SyntaxKind.CloseParenToken))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinExpressionParentheses);
            }

            // For spacing between the parenthesis and the cast expression
            if ((previousParentKind == SyntaxKind.CastExpression && previousKind == SyntaxKind.OpenParenToken) ||
                (currentParentKind == SyntaxKind.CastExpression && currentKind == SyntaxKind.CloseParenToken))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinCastParentheses);
            }

            // For spacing between the parenthesis and the expression inside the control flow expression
            if (previousKind == SyntaxKind.OpenParenToken &&
                (previousParentKind == SyntaxKind.IfStatement || previousParentKind == SyntaxKind.WhileStatement || previousParentKind == SyntaxKind.SwitchStatement ||
                previousParentKind == SyntaxKind.ForStatement || previousParentKind == SyntaxKind.ForEachStatement || previousParentKind == SyntaxKind.DoStatement ||
                previousParentKind == SyntaxKind.CatchDeclaration || previousParentKind == SyntaxKind.UsingStatement || previousParentKind == SyntaxKind.LockStatement))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinOtherParentheses);
            }

            // Semicolons in an empty for statement.  i.e.   for(;;)
            if (previousKind == SyntaxKind.OpenParenToken || previousKind == SyntaxKind.SemicolonToken)
            {
                if (previousToken.Parent.Kind() == SyntaxKind.ForStatement)
                {
                    var forStatement = (ForStatementSyntax)previousToken.Parent;
                    if (forStatement.Initializers.Count == 0 &&
                        forStatement.Declaration == null &&
                        forStatement.Condition == null &&
                        forStatement.Incrementors.Count == 0)
                    {
                        return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
                    }
                }
            }

            if (currentKind == SyntaxKind.CloseParenToken &&
                (currentParentKind == SyntaxKind.IfStatement || currentParentKind == SyntaxKind.WhileStatement || currentParentKind == SyntaxKind.SwitchStatement ||
                currentParentKind == SyntaxKind.ForStatement || currentParentKind == SyntaxKind.ForEachStatement || currentParentKind == SyntaxKind.DoStatement ||
                currentParentKind == SyntaxKind.CatchDeclaration || currentParentKind == SyntaxKind.UsingStatement || currentParentKind == SyntaxKind.LockStatement))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinOtherParentheses);
            }

            // For spacing after the cast
            if (previousParentKind == SyntaxKind.CastExpression && previousKind == SyntaxKind.CloseParenToken)
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterCast);
            }

            // For spacing Before Square Braces
            if (currentKind == SyntaxKind.OpenBracketToken && HasFormattableBracketParent(currentToken))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceBeforeOpenSquareBracket);
            }

            // For spacing empty square braces
            if (previousKind == SyntaxKind.OpenBracketToken && (currentKind == SyntaxKind.CloseBracketToken || currentKind == SyntaxKind.OmittedArraySizeExpressionToken) && HasFormattableBracketParent(previousToken))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets);
            }

            // For spacing square brackets within
            if (previousKind == SyntaxKind.OpenBracketToken && HasFormattableBracketParent(previousToken))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinSquareBrackets);
            }
            else if (currentKind == SyntaxKind.CloseBracketToken && HasFormattableBracketParent(currentToken))
            {
                if (currentToken.Parent is ArrayRankSpecifierSyntax)
                {
                    var parent = currentToken.Parent as ArrayRankSpecifierSyntax;
                    if ((parent.Sizes.Any() && parent.Sizes.First().Kind() != SyntaxKind.OmittedArraySizeExpression) || parent.Sizes.SeparatorCount > 0)
                    {
                        // int []: added spacing operation on open [
                        // int[1], int[,]: need spacing operation
                        return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinSquareBrackets);
                    }
                }
                else
                {
                    return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinSquareBrackets);
                }
            }

            // For spacing delimiters - after colon
            if (previousToken.IsColonInTypeBaseList())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration);
            }

            // For spacing delimiters - before colon
            if (currentToken.IsColonInTypeBaseList())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration);
            }

            // For spacing delimiters - after comma
            if ((previousToken.IsCommaInArgumentOrParameterList() && currentKind != SyntaxKind.OmittedTypeArgumentToken) ||
                previousToken.IsCommaInInitializerExpression())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterComma);
            }

            // For spacing delimiters - before comma
            if ((currentToken.IsCommaInArgumentOrParameterList() && previousKind != SyntaxKind.OmittedTypeArgumentToken) ||
                currentToken.IsCommaInInitializerExpression())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceBeforeComma);
            }

            // For Spacing delimiters - after Dot
            if (previousToken.IsDotInMemberAccessOrQualifiedName())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterDot);
            }

            // For spacing delimiters - before Dot
            if (currentToken.IsDotInMemberAccessOrQualifiedName())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceBeforeDot);
            }

            // For spacing delimiters - after semicolon
            if (previousToken.IsSemicolonInForStatement() && currentKind != SyntaxKind.CloseParenToken)
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement);
            }

            // For spacing delimiters - before semicolon
            if (currentToken.IsSemicolonInForStatement())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement);
            }

            // For spacing around the binary operators
            if (currentToken.Parent is BinaryExpressionSyntax ||
                previousToken.Parent is BinaryExpressionSyntax ||
                currentToken.Parent is AssignmentExpressionSyntax ||
                previousToken.Parent is AssignmentExpressionSyntax)
            {
                switch (optionSet.GetOption(CSharpFormattingOptions.SpacingAroundBinaryOperator))
                {
                    case BinaryOperatorSpacingOptions.Single:
                        return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                    case BinaryOperatorSpacingOptions.Remove:
                        return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                    case BinaryOperatorSpacingOptions.Ignore:
                        return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces);
                    default:
                        System.Diagnostics.Debug.Assert(false, "Invalid BinaryOperatorSpacingOptions");
                        break;
                }
            }

            // No space after $" and $@" at the start of an interpolated string
            if (previousKind == SyntaxKind.InterpolatedStringStartToken ||
                previousKind == SyntaxKind.InterpolatedVerbatimStringStartToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
            }

            // No space before " at the end of an interpolated string
            if (currentKind == SyntaxKind.InterpolatedStringEndToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
            }

            // No space before { or after } in interpolations
            if ((currentKind == SyntaxKind.OpenBraceToken && currentToken.Parent is InterpolationSyntax) ||
                (previousKind == SyntaxKind.CloseBraceToken && previousToken.Parent is InterpolationSyntax))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
            }

            // Preserve space after { or before } in interpolations (i.e. between the braces and the expression)
            if ((previousKind == SyntaxKind.OpenBraceToken && previousToken.Parent is InterpolationSyntax) ||
                (currentKind == SyntaxKind.CloseBraceToken && currentToken.Parent is InterpolationSyntax))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces);
            }

            // No space before or after , in interpolation alignment clause
            if ((previousKind == SyntaxKind.CommaToken && previousToken.Parent is InterpolationAlignmentClauseSyntax) ||
                (currentKind == SyntaxKind.CommaToken && currentToken.Parent is InterpolationAlignmentClauseSyntax))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
            }

            // No space before or after : in interpolation format clause
            if ((previousKind == SyntaxKind.ColonToken && previousToken.Parent is InterpolationFormatClauseSyntax) ||
                (currentKind == SyntaxKind.ColonToken && currentToken.Parent is InterpolationFormatClauseSyntax))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
            }

            return nextOperation.Invoke();
        }

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<SuppressOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            SuppressVariableDeclaration(list, node, optionSet);
        }

        private void SuppressVariableDeclaration(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet)
        {
            if (node.IsKind(SyntaxKind.FieldDeclaration) || node.IsKind(SyntaxKind.EventDeclaration) ||
                node.IsKind(SyntaxKind.EventFieldDeclaration) || node.IsKind(SyntaxKind.LocalDeclarationStatement))
            {
                if (optionSet.GetOption(CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration))
                {
                    var firstToken = node.GetFirstToken(includeZeroWidth: true);
                    var lastToken = node.GetLastToken(includeZeroWidth: true);

                    list.Add(FormattingOperations.CreateSuppressOperation(firstToken, lastToken, SuppressOption.NoSpacing));
                }
            }
        }

        private AdjustSpacesOperation AdjustSpacesOperationZeroOrOne(OptionSet optionSet, Option<bool> option, AdjustSpacesOption explicitOption = AdjustSpacesOption.ForceSpacesIfOnSingleLine)
        {
            if (optionSet.GetOption(option))
            {
                return CreateAdjustSpacesOperation(1, explicitOption);
            }
            else
            {
                return CreateAdjustSpacesOperation(0, explicitOption);
            }
        }

        private bool HasFormattableBracketParent(SyntaxToken token)
        {
            return token.Parent.IsKind(SyntaxKind.ArrayRankSpecifier, SyntaxKind.BracketedArgumentList, SyntaxKind.BracketedParameterList, SyntaxKind.ImplicitArrayCreationExpression);
        }
    }
}
