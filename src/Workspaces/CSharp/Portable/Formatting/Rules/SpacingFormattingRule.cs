// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class SpacingFormattingRule : BaseFormattingRule
    {
        public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustSpacesOperation nextOperation)
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

            // For Generic Method Declaration
            if (currentToken.IsOpenParenInParameterList() && previousKind == SyntaxKind.GreaterThanToken && previousParentKind == SyntaxKind.TypeParameterList)
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpacingAfterMethodDeclarationName);
            }

            // Case: public static implicit operator string(Program p) { return null; }
            // Case: public static implicit operator int?(Program p) { return null; }
            // Case: public static implicit operator int*(Program p) { return null; }
            // Case: public static implicit operator int[](Program p) { return null; }
            // Case: public static implicit operator (int, int)(Program p) { return null; }
            // Case: public static implicit operator Action<int>(Program p) { return null; }
            if ((previousToken.IsKeyword() || previousToken.IsKind(SyntaxKind.QuestionToken, SyntaxKind.AsteriskToken, SyntaxKind.CloseBracketToken, SyntaxKind.CloseParenToken, SyntaxKind.GreaterThanToken))
                && currentToken.IsOpenParenInParameterListOfAConversionOperatorDeclaration())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpacingAfterMethodDeclarationName);
            }

            // Case: public static Program operator !(Program p) { return null; }
            if (previousToken.Parent.IsKind(SyntaxKind.OperatorDeclaration) && currentToken.IsOpenParenInParameterListOfAOperationDeclaration())
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
            //   MethodName ( args )
            // Or Positional Pattern
            //   x is TypeName ( args )
            if (currentToken.IsOpenParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterMethodCallName);
            }

            if (previousToken.IsOpenParenInArgumentListOrPositionalPattern() && currentToken.IsCloseParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses);
            }

            if (previousToken.IsOpenParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinMethodCallParentheses);
            }

            if (currentToken.IsCloseParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinMethodCallParentheses);
            }

            // For spacing around: typeof, default, and sizeof; treat like a Method Call
            if (currentKind == SyntaxKind.OpenParenToken && IsFunctionLikeKeywordExpressionKind(currentParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterMethodCallName);
            }

            if (previousKind == SyntaxKind.OpenParenToken && IsFunctionLikeKeywordExpressionKind(previousParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinMethodCallParentheses);
            }

            if (currentKind == SyntaxKind.CloseParenToken && IsFunctionLikeKeywordExpressionKind(currentParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinMethodCallParentheses);
            }

            // For Spacing b/n control flow keyword and paren. Parent check not needed.
            if (currentKind == SyntaxKind.OpenParenToken &&
                (previousKind == SyntaxKind.IfKeyword || previousKind == SyntaxKind.WhileKeyword || previousKind == SyntaxKind.SwitchKeyword ||
                previousKind == SyntaxKind.ForKeyword || previousKind == SyntaxKind.ForEachKeyword || previousKind == SyntaxKind.CatchKeyword ||
                previousKind == SyntaxKind.UsingKeyword || previousKind == SyntaxKind.WhenKeyword || previousKind == SyntaxKind.LockKeyword ||
                previousKind == SyntaxKind.FixedKeyword))
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

            // Semicolons in an empty for statement.  i.e.   for(;;)
            if (previousParentKind == SyntaxKind.ForStatement
                && this.IsEmptyForStatement((ForStatementSyntax)previousToken.Parent))
            {
                if (currentKind == SyntaxKind.SemicolonToken
                    && (previousKind != SyntaxKind.SemicolonToken
                        || optionSet.GetOption<bool>(CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement)))
                {
                    return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement);
                }

                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement);
            }

            // For spacing between the parenthesis and the expression inside the control flow expression
            if (previousKind == SyntaxKind.OpenParenToken && IsControlFlowLikeKeywordStatementKind(previousParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinOtherParentheses);
            }

            if (currentKind == SyntaxKind.CloseParenToken && IsControlFlowLikeKeywordStatementKind(currentParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinOtherParentheses);
            }

            // For spacing after the cast
            if (previousParentKind == SyntaxKind.CastExpression && previousKind == SyntaxKind.CloseParenToken)
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterCast);
            }

            // For spacing Before Square Braces
            if (currentKind == SyntaxKind.OpenBracketToken && HasFormattableBracketParent(currentToken) && !previousToken.IsOpenBraceOrCommaOfObjectInitializer())
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceBeforeOpenSquareBracket);
            }

            // For spacing empty square braces, also treat [,] as empty
            if (((currentKind == SyntaxKind.CloseBracketToken && previousKind == SyntaxKind.OpenBracketToken)
                || currentKind == SyntaxKind.OmittedArraySizeExpressionToken)
                && HasFormattableBracketParent(previousToken))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets);
            }

            // For spacing square brackets within
            if (previousKind == SyntaxKind.OpenBracketToken && HasFormattableBracketParent(previousToken))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinSquareBrackets);
            }

            if (currentKind == SyntaxKind.CloseBracketToken && previousKind != SyntaxKind.OmittedArraySizeExpressionToken && HasFormattableBracketParent(currentToken))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceWithinSquareBrackets);
            }

            // attribute case ] *
            // Place a space between the attribute and the next member if they're on the same line.
            if (previousKind == SyntaxKind.CloseBracketToken && previousToken.Parent.IsKind(SyntaxKind.AttributeList))
            {
                var attributeOwner = previousToken.Parent?.Parent;
                if (attributeOwner is MemberDeclarationSyntax)
                {
                    return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
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
            if ((previousToken.IsCommaInArgumentOrParameterList() && currentKind != SyntaxKind.OmittedTypeArgumentToken)
                || previousToken.IsCommaInInitializerExpression()
                || (previousKind == SyntaxKind.CommaToken
                    && currentKind != SyntaxKind.OmittedArraySizeExpressionToken
                    && HasFormattableBracketParent(previousToken)))
            {
                return AdjustSpacesOperationZeroOrOne(optionSet, CSharpFormattingOptions.SpaceAfterComma);
            }

            // For spacing delimiters - before comma
            if ((currentToken.IsCommaInArgumentOrParameterList() && previousKind != SyntaxKind.OmittedTypeArgumentToken)
                || currentToken.IsCommaInInitializerExpression()
                || (currentKind == SyntaxKind.CommaToken
                    && previousKind != SyntaxKind.OmittedArraySizeExpressionToken
                    && HasFormattableBracketParent(currentToken)))
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
                        if (currentKind == SyntaxKind.IsKeyword ||
                            currentKind == SyntaxKind.AsKeyword ||
                            previousKind == SyntaxKind.IsKeyword ||
                            previousKind == SyntaxKind.AsKeyword)
                        {
                            // User want spaces removed but at least one is required for the "as" & "is" keyword
                            return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                        }
                        else
                        {
                            return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                        }
                    case BinaryOperatorSpacingOptions.Ignore:
                        return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces);
                    default:
                        System.Diagnostics.Debug.Assert(false, "Invalid BinaryOperatorSpacingOptions");
                        break;
                }
            }

            // No space after $" and $@" and @$" at the start of an interpolated string
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

            // Always put a space in the var form of deconstruction-declaration
            if (currentToken.IsOpenParenInVarDeconstructionDeclaration())
            {
                return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
            }

            // Index expressions
            if (previousKind == SyntaxKind.CaretToken && previousParentKind == SyntaxKindEx.IndexExpression)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
            }

            // Right of Range expressions
            if (previousKind == SyntaxKindEx.DotDotToken && previousParentKind == SyntaxKindEx.RangeExpression)
            {
#if !CODE_STYLE
                var rangeExpression = (RangeExpressionSyntax)previousToken.Parent;
                var hasRightOperand = rangeExpression.RightOperand != null;
#else
                var childSyntax = previousToken.Parent.ChildNodesAndTokens();
                var hasRightOperand = childSyntax.Count > 1 && childSyntax[childSyntax.Count - 2].IsKind(SyntaxKindEx.DotDotToken);
#endif
                if (hasRightOperand)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
                }
            }

            // Left of Range expressions
            if (currentKind == SyntaxKindEx.DotDotToken && currentParentKind == SyntaxKindEx.RangeExpression)
            {
#if !CODE_STYLE
                var rangeExpression = (RangeExpressionSyntax)currentToken.Parent;
                var hasLeftOperand = rangeExpression.LeftOperand != null;
#else
                var childSyntax = currentToken.Parent.ChildNodesAndTokens();
                var hasLeftOperand = childSyntax.Count > 1 && childSyntax[1].IsKind(SyntaxKindEx.DotDotToken);
#endif
                if (hasLeftOperand)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
                }
            }

            return nextOperation.Invoke();
        }

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, in NextSuppressOperationAction nextOperation)
        {
            nextOperation.Invoke();

            SuppressVariableDeclaration(list, node, optionSet);
        }

        private bool IsEmptyForStatement(ForStatementSyntax forStatement) =>
            forStatement is
        {
            Initializers: { Count: 0 },
            Declaration: null,
            Condition: null,
            Incrementors: { Count: 0 }
        };

        private void SuppressVariableDeclaration(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet)
        {
            if (node.IsKind(SyntaxKind.FieldDeclaration) || node.IsKind(SyntaxKind.EventDeclaration) ||
                node.IsKind(SyntaxKind.EventFieldDeclaration) || node.IsKind(SyntaxKind.LocalDeclarationStatement) ||
                node.IsKind(SyntaxKind.EnumMemberDeclaration))
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

        private bool IsFunctionLikeKeywordExpressionKind(SyntaxKind syntaxKind)
        {
            return (syntaxKind == SyntaxKind.TypeOfExpression || syntaxKind == SyntaxKind.DefaultExpression || syntaxKind == SyntaxKind.SizeOfExpression);
        }

        private bool IsControlFlowLikeKeywordStatementKind(SyntaxKind syntaxKind)
        {
            return (syntaxKind == SyntaxKind.IfStatement || syntaxKind == SyntaxKind.WhileStatement || syntaxKind == SyntaxKind.SwitchStatement ||
                syntaxKind == SyntaxKind.ForStatement || syntaxKind == SyntaxKind.ForEachStatement || syntaxKind == SyntaxKind.ForEachVariableStatement ||
                syntaxKind == SyntaxKind.DoStatement ||
                syntaxKind == SyntaxKind.CatchDeclaration || syntaxKind == SyntaxKind.UsingStatement || syntaxKind == SyntaxKind.LockStatement ||
                syntaxKind == SyntaxKind.FixedStatement || syntaxKind == SyntaxKind.CatchFilterClause);
        }
    }
}
