// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class SpacingFormattingRule : BaseFormattingRule
    {
        public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, AnalyzerConfigOptions options, in NextGetAdjustSpacesOperation nextOperation)
        {
            if (options == null)
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
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpacingAfterMethodDeclarationName);
            }

            // For Generic Method Declaration
            if (currentToken.IsOpenParenInParameterList() && previousKind == SyntaxKind.GreaterThanToken && previousParentKind == SyntaxKind.TypeParameterList)
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpacingAfterMethodDeclarationName);
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
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpacingAfterMethodDeclarationName);
            }

            // Case: public static Program operator !(Program p) { return null; }
            if (previousToken.Parent.IsKind(SyntaxKind.OperatorDeclaration) && currentToken.IsOpenParenInParameterListOfAOperationDeclaration())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpacingAfterMethodDeclarationName);
            }

            if (previousToken.IsOpenParenInParameterList() && currentToken.IsCloseParenInParameterList())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses);
            }

            if (previousToken.IsOpenParenInParameterList())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis);
            }

            if (currentToken.IsCloseParenInParameterList())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis);
            }

            // For Method Call
            //   MethodName ( args )
            // Or Positional Pattern
            //   x is TypeName ( args )
            if (currentToken.IsOpenParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceAfterMethodCallName);
            }

            if (previousToken.IsOpenParenInArgumentListOrPositionalPattern() && currentToken.IsCloseParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses);
            }

            if (previousToken.IsOpenParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceWithinMethodCallParentheses);
            }

            if (currentToken.IsCloseParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceWithinMethodCallParentheses);
            }

            // For spacing around: typeof, default, and sizeof; treat like a Method Call
            if (currentKind == SyntaxKind.OpenParenToken && IsFunctionLikeKeywordExpressionKind(currentParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceAfterMethodCallName);
            }

            if (previousKind == SyntaxKind.OpenParenToken && IsFunctionLikeKeywordExpressionKind(previousParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceWithinMethodCallParentheses);
            }

            if (currentKind == SyntaxKind.CloseParenToken && IsFunctionLikeKeywordExpressionKind(currentParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceWithinMethodCallParentheses);
            }

            // For Spacing b/n control flow keyword and paren. Parent check not needed.
            if (currentKind == SyntaxKind.OpenParenToken &&
                (previousKind == SyntaxKind.IfKeyword || previousKind == SyntaxKind.WhileKeyword || previousKind == SyntaxKind.SwitchKeyword ||
                previousKind == SyntaxKind.ForKeyword || previousKind == SyntaxKind.ForEachKeyword || previousKind == SyntaxKind.CatchKeyword ||
                previousKind == SyntaxKind.UsingKeyword || previousKind == SyntaxKind.WhenKeyword || previousKind == SyntaxKind.LockKeyword ||
                previousKind == SyntaxKind.FixedKeyword))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword);
            }

            // For spacing between parenthesis and expression
            if ((previousParentKind == SyntaxKind.ParenthesizedExpression && previousKind == SyntaxKind.OpenParenToken) ||
                (currentParentKind == SyntaxKind.ParenthesizedExpression && currentKind == SyntaxKind.CloseParenToken))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceWithinExpressionParentheses);
            }

            // For spacing between the parenthesis and the cast expression
            if ((previousParentKind == SyntaxKind.CastExpression && previousKind == SyntaxKind.OpenParenToken) ||
                (currentParentKind == SyntaxKind.CastExpression && currentKind == SyntaxKind.CloseParenToken))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceWithinCastParentheses);
            }

            // Semicolons in an empty for statement.  i.e.   for(;;)
            if (previousParentKind == SyntaxKind.ForStatement
                && this.IsEmptyForStatement((ForStatementSyntax)previousToken.Parent))
            {
                if (currentKind == SyntaxKind.SemicolonToken
                    && (previousKind != SyntaxKind.SemicolonToken
                        || options.GetOption<bool>(CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement)))
                {
                    return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement);
                }

                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement);
            }

            // For spacing between the parenthesis and the expression inside the control flow expression
            if (previousKind == SyntaxKind.OpenParenToken && IsControlFlowLikeKeywordStatementKind(previousParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceWithinOtherParentheses);
            }

            if (currentKind == SyntaxKind.CloseParenToken && IsControlFlowLikeKeywordStatementKind(currentParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceWithinOtherParentheses);
            }

            // For spacing after the cast
            if (previousParentKind == SyntaxKind.CastExpression && previousKind == SyntaxKind.CloseParenToken)
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceAfterCast);
            }

            // For spacing Before Square Braces
            if (currentKind == SyntaxKind.OpenBracketToken && HasFormattableBracketParent(currentToken) && !previousToken.IsOpenBraceOrCommaOfObjectInitializer())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket);
            }

            // For spacing empty square braces, also treat [,] as empty
            if (((currentKind == SyntaxKind.CloseBracketToken && previousKind == SyntaxKind.OpenBracketToken)
                || currentKind == SyntaxKind.OmittedArraySizeExpressionToken)
                && HasFormattableBracketParent(previousToken))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets);
            }

            // For spacing square brackets within
            if (previousKind == SyntaxKind.OpenBracketToken && HasFormattableBracketParent(previousToken))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceWithinSquareBrackets);
            }

            if (currentKind == SyntaxKind.CloseBracketToken && previousKind != SyntaxKind.OmittedArraySizeExpressionToken && HasFormattableBracketParent(currentToken))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceWithinSquareBrackets);
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
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration);
            }

            // For spacing delimiters - before colon
            if (currentToken.IsColonInTypeBaseList())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration);
            }

            // For spacing delimiters - after comma
            if ((previousToken.IsCommaInArgumentOrParameterList() && currentKind != SyntaxKind.OmittedTypeArgumentToken)
                || previousToken.IsCommaInInitializerExpression()
                || (previousKind == SyntaxKind.CommaToken
                    && currentKind != SyntaxKind.OmittedArraySizeExpressionToken
                    && HasFormattableBracketParent(previousToken)))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceAfterComma);
            }

            // For spacing delimiters - before comma
            if ((currentToken.IsCommaInArgumentOrParameterList() && previousKind != SyntaxKind.OmittedTypeArgumentToken)
                || currentToken.IsCommaInInitializerExpression()
                || (currentKind == SyntaxKind.CommaToken
                    && previousKind != SyntaxKind.OmittedArraySizeExpressionToken
                    && HasFormattableBracketParent(currentToken)))
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceBeforeComma);
            }

            // For Spacing delimiters - after Dot
            if (previousToken.IsDotInMemberAccessOrQualifiedName())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceAfterDot);
            }

            // For spacing delimiters - before Dot
            if (currentToken.IsDotInMemberAccessOrQualifiedName())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceBeforeDot);
            }

            // For spacing delimiters - after semicolon
            if (previousToken.IsSemicolonInForStatement() && currentKind != SyntaxKind.CloseParenToken)
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement);
            }

            // For spacing delimiters - before semicolon
            if (currentToken.IsSemicolonInForStatement())
            {
                return AdjustSpacesOperationZeroOrOne(options, CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement);
            }

            // For spacing around the binary operators
            if (currentToken.Parent is BinaryExpressionSyntax ||
                previousToken.Parent is BinaryExpressionSyntax ||
                currentToken.Parent is AssignmentExpressionSyntax ||
                previousToken.Parent is AssignmentExpressionSyntax)
            {
                switch (options.GetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator))
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
            if (previousKind == SyntaxKind.CaretToken && previousParentKind == SyntaxKind.IndexExpression)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
            }

            // Right of Range expressions
            if (previousKind == SyntaxKind.DotDotToken && previousParentKind == SyntaxKind.RangeExpression)
            {
                var rangeExpression = (RangeExpressionSyntax)previousToken.Parent;
                var hasRightOperand = rangeExpression.RightOperand != null;
                if (hasRightOperand)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
                }
            }

            // Left of Range expressions
            if (currentKind == SyntaxKind.DotDotToken && currentParentKind == SyntaxKind.RangeExpression)
            {
                var rangeExpression = (RangeExpressionSyntax)currentToken.Parent;
                var hasLeftOperand = rangeExpression.LeftOperand != null;
                if (hasLeftOperand)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
                }
            }

            return nextOperation.Invoke();
        }

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, AnalyzerConfigOptions options, in NextSuppressOperationAction nextOperation)
        {
            nextOperation.Invoke();

            SuppressVariableDeclaration(list, node, options);
        }

        private bool IsEmptyForStatement(ForStatementSyntax forStatement) =>
            forStatement.Initializers.Count == 0
            && forStatement.Declaration == null
            && forStatement.Condition == null
            && forStatement.Incrementors.Count == 0;

        private void SuppressVariableDeclaration(List<SuppressOperation> list, SyntaxNode node, AnalyzerConfigOptions options)
        {
            if (node.IsKind(SyntaxKind.FieldDeclaration) || node.IsKind(SyntaxKind.EventDeclaration) ||
                node.IsKind(SyntaxKind.EventFieldDeclaration) || node.IsKind(SyntaxKind.LocalDeclarationStatement) ||
                node.IsKind(SyntaxKind.EnumMemberDeclaration))
            {
                if (options.GetOption(CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration))
                {
                    var firstToken = node.GetFirstToken(includeZeroWidth: true);
                    var lastToken = node.GetLastToken(includeZeroWidth: true);

                    list.Add(FormattingOperations.CreateSuppressOperation(firstToken, lastToken, SuppressOption.NoSpacing));
                }
            }
        }

        private AdjustSpacesOperation AdjustSpacesOperationZeroOrOne(AnalyzerConfigOptions options, Option2<bool> option, AdjustSpacesOption explicitOption = AdjustSpacesOption.ForceSpacesIfOnSingleLine)
        {
            if (options.GetOption(option))
            {
                return CreateAdjustSpacesOperation(1, explicitOption);
            }
            else
            {
                return CreateAdjustSpacesOperation(0, explicitOption);
            }
        }

        private bool HasFormattableBracketParent(SyntaxToken token)
            => token.Parent.IsKind(SyntaxKind.ArrayRankSpecifier, SyntaxKind.BracketedArgumentList, SyntaxKind.BracketedParameterList, SyntaxKind.ImplicitArrayCreationExpression);

        private bool IsFunctionLikeKeywordExpressionKind(SyntaxKind syntaxKind)
            => (syntaxKind == SyntaxKind.TypeOfExpression || syntaxKind == SyntaxKind.DefaultExpression || syntaxKind == SyntaxKind.SizeOfExpression);

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
