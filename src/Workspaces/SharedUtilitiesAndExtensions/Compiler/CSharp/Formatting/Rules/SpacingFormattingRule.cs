// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal sealed class SpacingFormattingRule : BaseFormattingRule
    {
        private readonly CSharpSyntaxFormattingOptions _options;

        public SpacingFormattingRule()
            : this(CSharpSyntaxFormattingOptions.Default)
        {
        }

        private SpacingFormattingRule(CSharpSyntaxFormattingOptions options)
        {
            _options = options;
        }

        public override AbstractFormattingRule WithOptions(SyntaxFormattingOptions options)
        {
            var newOptions = options as CSharpSyntaxFormattingOptions ?? CSharpSyntaxFormattingOptions.Default;

            if (_options.Spacing == newOptions.Spacing &&
                _options.SpacingAroundBinaryOperator == newOptions.SpacingAroundBinaryOperator)
            {
                return this;
            }

            return new SpacingFormattingRule(newOptions);
        }

        public override AdjustSpacesOperation? GetAdjustSpacesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustSpacesOperation nextOperation)
        {
            RoslynDebug.Assert(previousToken.Parent != null && currentToken.Parent != null);

            var previousKind = previousToken.Kind();
            var currentKind = currentToken.Kind();
            var previousParentKind = previousToken.Parent.Kind();
            var currentParentKind = currentToken.Parent.Kind();

            // For Method Declaration
            if (currentToken.IsOpenParenInParameterList() && previousKind == SyntaxKind.IdentifierToken)
            {
                // Parenthesized lambda with explicit return type.
                if (currentToken.IsOpenParenInParameterListOfParenthesizedLambdaExpression())
                {
                    return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterMethodDeclarationName));
            }

            // For Generic Method Declaration
            if (currentToken.IsOpenParenInParameterList() && previousKind == SyntaxKind.GreaterThanToken)
            {
                // Parenthesized lambda with explicit generic return type.
                if (currentToken.IsOpenParenInParameterListOfParenthesizedLambdaExpression() && previousParentKind == SyntaxKind.TypeArgumentList)
                {
                    return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                if (previousParentKind == SyntaxKind.TypeParameterList)
                {
                    return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterMethodDeclarationName));
                }
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
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterMethodDeclarationName));
            }

            // Case: public static Program operator !(Program p) { return null; }
            if (previousToken.Parent.IsKind(SyntaxKind.OperatorDeclaration) && currentToken.IsOpenParenInParameterListOfAOperationDeclaration())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterMethodDeclarationName));
            }

            if (previousToken.IsOpenParenInParameterList() && currentToken.IsCloseParenInParameterList())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.BetweenEmptyMethodDeclarationParentheses));
            }

            if (previousToken.IsOpenParenInParameterList())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinMethodDeclarationParenthesis));
            }

            if (currentToken.IsCloseParenInParameterList())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinMethodDeclarationParenthesis));
            }

            // For Method Call
            //   MethodName ( args )
            // Or Positional Pattern
            //   x is TypeName ( args )
            if (currentToken.IsOpenParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterMethodCallName));
            }

            if (previousToken.IsOpenParenInArgumentListOrPositionalPattern() && currentToken.IsCloseParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.BetweenEmptyMethodCallParentheses));
            }

            if (previousToken.IsOpenParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinMethodCallParentheses));
            }

            if (currentToken.IsCloseParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinMethodCallParentheses));
            }

            // For spacing around: typeof, default, and sizeof; treat like a Method Call
            if (currentKind == SyntaxKind.OpenParenToken && IsFunctionLikeKeywordExpressionKind(currentParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterMethodCallName));
            }

            if (previousKind == SyntaxKind.OpenParenToken && IsFunctionLikeKeywordExpressionKind(previousParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinMethodCallParentheses));
            }

            if (currentKind == SyntaxKind.CloseParenToken && IsFunctionLikeKeywordExpressionKind(currentParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinMethodCallParentheses));
            }

            // For Spacing b/n control flow keyword and paren. Parent check not needed.
            if (currentKind == SyntaxKind.OpenParenToken &&
                (previousKind == SyntaxKind.IfKeyword || previousKind == SyntaxKind.WhileKeyword || previousKind == SyntaxKind.SwitchKeyword ||
                previousKind == SyntaxKind.ForKeyword || previousKind == SyntaxKind.ForEachKeyword || previousKind == SyntaxKind.CatchKeyword ||
                previousKind == SyntaxKind.UsingKeyword || previousKind == SyntaxKind.WhenKeyword || previousKind == SyntaxKind.LockKeyword ||
                previousKind == SyntaxKind.FixedKeyword))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterControlFlowStatementKeyword));
            }

            // For spacing between parenthesis and expression
            if ((previousToken.Parent.IsKind(SyntaxKind.ParenthesizedExpression, SyntaxKind.ParenthesizedPattern) && previousKind == SyntaxKind.OpenParenToken) ||
                (currentToken.Parent.IsKind(SyntaxKind.ParenthesizedExpression, SyntaxKind.ParenthesizedPattern) && currentKind == SyntaxKind.CloseParenToken))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinExpressionParentheses));
            }

            // For spacing between the parenthesis and the cast expression
            if ((previousParentKind == SyntaxKind.CastExpression && previousKind == SyntaxKind.OpenParenToken) ||
                (currentParentKind == SyntaxKind.CastExpression && currentKind == SyntaxKind.CloseParenToken))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinCastParentheses));
            }

            // Semicolons in an empty for statement.  i.e.   for(;;)
            if (previousParentKind == SyntaxKind.ForStatement
                && IsEmptyForStatement((ForStatementSyntax)previousToken.Parent!))
            {
                if (currentKind == SyntaxKind.SemicolonToken
                    && (previousKind != SyntaxKind.SemicolonToken
                        || _options.Spacing.HasFlag(SpacePlacement.BeforeSemicolonsInForStatement)))
                {
                    return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.BeforeSemicolonsInForStatement));
                }

                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterSemicolonsInForStatement));
            }

            // For spacing between the parenthesis and the expression inside the control flow expression
            if (previousKind == SyntaxKind.OpenParenToken && IsControlFlowLikeKeywordStatementKind(previousParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinOtherParentheses));
            }

            if (currentKind == SyntaxKind.CloseParenToken && IsControlFlowLikeKeywordStatementKind(currentParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinOtherParentheses));
            }

            // For spacing after the cast
            if (previousParentKind == SyntaxKind.CastExpression && previousKind == SyntaxKind.CloseParenToken)
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterCast));
            }

            if (currentKind == SyntaxKind.OpenBracketToken && currentToken.Parent.IsKind(SyntaxKind.ListPattern))
            {
                // is [
                // and [
                return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // For spacing Before Square Braces
            if (currentKind == SyntaxKind.OpenBracketToken && HasFormattableBracketParent(currentToken) && !previousToken.IsOpenBraceOrCommaOfObjectInitializer())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.BeforeOpenSquareBracket));
            }

            // For spacing empty square braces, also treat [,] as empty
            if (((currentKind == SyntaxKind.CloseBracketToken && previousKind == SyntaxKind.OpenBracketToken)
                || currentKind == SyntaxKind.OmittedArraySizeExpressionToken)
                && HasFormattableBracketParent(previousToken))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.BetweenEmptySquareBrackets));
            }

            // For spacing square brackets within
            if (previousKind == SyntaxKind.OpenBracketToken && HasFormattableBracketParent(previousToken))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinSquareBrackets));
            }

            if (currentKind == SyntaxKind.CloseBracketToken && previousKind != SyntaxKind.OmittedArraySizeExpressionToken && HasFormattableBracketParent(currentToken))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinSquareBrackets));
            }

            // attribute case ] *
            if (previousKind == SyntaxKind.CloseBracketToken && previousToken.Parent.IsKind(SyntaxKind.AttributeList))
            {
                // [Attribute1]$$[Attribute2]
                if (currentToken.IsKind(SyntaxKind.OpenBracketToken) &&
                    currentToken.Parent.IsKind(SyntaxKind.AttributeList))
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                // [Attribute1]$${EOF}
                if (currentToken.IsKind(SyntaxKind.EndOfFileToken))
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                // [Attribute]$$ int Prop { ... }
                return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // For spacing delimiters - after colon
            if (previousToken.IsColonInTypeBaseList())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterColonInBaseTypeDeclaration));
            }

            // For spacing delimiters - before colon
            if (currentToken.IsColonInTypeBaseList())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.BeforeColonInBaseTypeDeclaration));
            }

            // For spacing delimiters - after comma
            if ((previousToken.IsCommaInArgumentOrParameterList() && currentKind != SyntaxKind.OmittedTypeArgumentToken)
                || previousToken.IsCommaInInitializerExpression()
                || (previousKind == SyntaxKind.CommaToken
                    && currentKind != SyntaxKind.OmittedArraySizeExpressionToken
                    && HasFormattableBracketParent(previousToken)))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterComma));
            }

            // For spacing delimiters - before comma
            if ((currentToken.IsCommaInArgumentOrParameterList() && previousKind != SyntaxKind.OmittedTypeArgumentToken)
                || currentToken.IsCommaInInitializerExpression()
                || (currentKind == SyntaxKind.CommaToken
                    && previousKind != SyntaxKind.OmittedArraySizeExpressionToken
                    && HasFormattableBracketParent(currentToken)))
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.BeforeComma));
            }

            // For Spacing delimiters - after Dot
            if (previousToken.IsDotInMemberAccessOrQualifiedName())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterDot));
            }

            // For spacing delimiters - before Dot
            if (currentToken.IsDotInMemberAccessOrQualifiedName())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.BeforeDot));
            }

            // For spacing delimiters - after semicolon
            if (previousToken.IsSemicolonInForStatement() && currentKind != SyntaxKind.CloseParenToken)
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.AfterSemicolonsInForStatement));
            }

            // For spacing delimiters - before semicolon
            if (currentToken.IsSemicolonInForStatement())
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.BeforeSemicolonsInForStatement));
            }

            // For spacing around the binary operators
            if (currentToken.Parent is BinaryExpressionSyntax ||
                previousToken.Parent is BinaryExpressionSyntax ||
                currentToken.Parent is AssignmentExpressionSyntax ||
                previousToken.Parent is AssignmentExpressionSyntax ||
                currentToken.Parent.IsKind(SyntaxKind.AndPattern, SyntaxKind.OrPattern, SyntaxKind.RelationalPattern) ||
                previousToken.Parent.IsKind(SyntaxKind.AndPattern, SyntaxKind.OrPattern, SyntaxKind.RelationalPattern))
            {
                switch (_options.SpacingAroundBinaryOperator)
                {
                    case BinaryOperatorSpacingOptions.Single:
                        return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                    case BinaryOperatorSpacingOptions.Remove:
                        if (currentKind == SyntaxKind.IsKeyword ||
                            currentKind == SyntaxKind.AsKeyword ||
                            currentKind == SyntaxKind.AndKeyword ||
                            currentKind == SyntaxKind.OrKeyword ||
                            previousKind == SyntaxKind.IsKeyword ||
                            previousKind == SyntaxKind.AsKeyword ||
                            previousKind == SyntaxKind.AndKeyword ||
                            previousKind == SyntaxKind.OrKeyword)
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

            // Function pointer type adjustments
            if (previousParentKind == SyntaxKind.FunctionPointerType)
            {
                // No spacing between delegate and *
                if (currentKind == SyntaxKind.AsteriskToken && previousKind == SyntaxKind.DelegateKeyword)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                // Force a space between * and the calling convention
                if (previousKind == SyntaxKind.AsteriskToken && currentParentKind == SyntaxKind.FunctionPointerCallingConvention)
                {
                    switch (currentKind)
                    {
                        case SyntaxKind.IdentifierToken:
                        case SyntaxKind.ManagedKeyword:
                        case SyntaxKind.UnmanagedKeyword:
                            return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                    }
                }
            }

            if (currentParentKind == SyntaxKind.FunctionPointerParameterList && currentKind == SyntaxKind.LessThanToken)
            {
                switch (previousKind)
                {
                    // No spacing between the * and < tokens if there is no calling convention
                    case SyntaxKind.AsteriskToken:
                    // No spacing between the calling convention and opening angle bracket of function pointer types:
                    // delegate* managed<
                    case SyntaxKind.ManagedKeyword:
                    case SyntaxKind.UnmanagedKeyword:
                    // No spacing between the calling convention specifier and the opening angle
                    // delegate* unmanaged[Cdecl]<
                    case SyntaxKind.CloseBracketToken when previousParentKind == SyntaxKind.FunctionPointerUnmanagedCallingConventionList:
                        return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }
            }

            // No space between unmanaged and the [
            // delegate* unmanaged[
            if (previousParentKind == SyntaxKind.FunctionPointerCallingConvention && currentParentKind == SyntaxKind.FunctionPointerUnmanagedCallingConventionList && currentKind == SyntaxKind.OpenBracketToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // Function pointer calling convention adjustments
            if (currentParentKind == SyntaxKind.FunctionPointerUnmanagedCallingConventionList && previousParentKind == SyntaxKind.FunctionPointerUnmanagedCallingConventionList)
            {
                if (currentKind == SyntaxKind.IdentifierToken)
                {
                    // No space after the [
                    // unmanaged[Cdecl
                    if (previousKind == SyntaxKind.OpenBracketToken)
                    {
                        return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                    }
                    // Space after the ,
                    // unmanaged[Cdecl, Thiscall
                    else if (previousKind == SyntaxKind.CommaToken)
                    {
                        return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                    }
                }

                // No space between identifier and comma
                // unmanaged[Cdecl,
                if (currentKind == SyntaxKind.CommaToken)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                // No space before the ]
                // unmanaged[Cdecl]
                if (currentKind == SyntaxKind.CloseBracketToken)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }
            }

            // Respect spacing setting for after the < in function pointer parameter lists
            // delegate*<void
            if (previousKind == SyntaxKind.LessThanToken && previousParentKind == SyntaxKind.FunctionPointerParameterList)
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinMethodDeclarationParenthesis));
            }

            // Respect spacing setting for before the > in function pointer parameter lists
            // delegate*<void>
            if (currentKind == SyntaxKind.GreaterThanToken && currentParentKind == SyntaxKind.FunctionPointerParameterList)
            {
                return AdjustSpacesOperationZeroOrOne(_options.Spacing.HasFlag(SpacePlacement.WithinMethodDeclarationParenthesis));
            }

            // For spacing after the 'not' pattern operator
            if (previousToken.Parent.IsKind(SyntaxKind.NotPattern))
            {
                return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // Slice pattern:
            // .. var x
            if (previousKind == SyntaxKind.DotDotToken && previousParentKind == SyntaxKind.SlicePattern)
            {
                return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // No space after $" and $@" and @$" at the start of an interpolated string
            if (previousKind is SyntaxKind.InterpolatedStringStartToken or
                                SyntaxKind.InterpolatedVerbatimStringStartToken or
                                SyntaxKind.InterpolatedSingleLineRawStringStartToken or
                                SyntaxKind.InterpolatedMultiLineRawStringStartToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
            }

            // No space before " at the end of an interpolated string
            if (currentKind is SyntaxKind.InterpolatedStringEndToken or
                               SyntaxKind.InterpolatedRawStringEndToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
            }

            // No space before { or after } in interpolations
            if ((currentKind == SyntaxKind.OpenBraceToken && currentToken.Parent is InterpolationSyntax) ||
                (previousKind == SyntaxKind.CloseBraceToken && previousToken.Parent is InterpolationSyntax))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
            }

            // No space after { in interpolations (i.e. between the braces and the expression)
            if (previousKind == SyntaxKind.OpenBraceToken && previousToken.Parent is InterpolationSyntax)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
            }

            // Handle space before } in interpolations (i.e. between the braces and the expression)
            if (currentKind == SyntaxKind.CloseBraceToken && currentToken.Parent is InterpolationSyntax interpolation)
            {
                // If there is no format specifier (i.e. a colon) remove spaces
                if (interpolation.FormatClause is null)
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);

                // If there is a format specifier then whitespace is significant so preserve it
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
                var rangeExpression = (RangeExpressionSyntax)previousToken.Parent!;
                var hasRightOperand = rangeExpression.RightOperand != null;
                if (hasRightOperand)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
                }
            }

            // Left of Range expressions
            if (currentKind == SyntaxKind.DotDotToken && currentParentKind == SyntaxKind.RangeExpression)
            {
                var rangeExpression = (RangeExpressionSyntax)currentToken.Parent!;
                var hasLeftOperand = rangeExpression.LeftOperand != null;
                if (hasLeftOperand)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
                }
            }

            return nextOperation.Invoke(in previousToken, in currentToken);
        }

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, in NextSuppressOperationAction nextOperation)
        {
            nextOperation.Invoke();

            SuppressVariableDeclaration(list, node);
        }

        private static bool IsEmptyForStatement(ForStatementSyntax forStatement) =>
            forStatement.Initializers.Count == 0
            && forStatement.Declaration == null
            && forStatement.Condition == null
            && forStatement.Incrementors.Count == 0;

        private void SuppressVariableDeclaration(List<SuppressOperation> list, SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.FieldDeclaration) || node.IsKind(SyntaxKind.EventDeclaration) ||
                node.IsKind(SyntaxKind.EventFieldDeclaration) || node.IsKind(SyntaxKind.LocalDeclarationStatement) ||
                node.IsKind(SyntaxKind.EnumMemberDeclaration))
            {
                if (_options.Spacing.HasFlag(SpacePlacement.IgnoreAroundVariableDeclaration))
                {
                    var firstToken = node.GetFirstToken(includeZeroWidth: true);
                    var lastToken = node.GetLastToken(includeZeroWidth: true);

                    list.Add(FormattingOperations.CreateSuppressOperation(firstToken, lastToken, SuppressOption.NoSpacing));
                }
            }
        }

        private static AdjustSpacesOperation AdjustSpacesOperationZeroOrOne(bool option, AdjustSpacesOption explicitOption = AdjustSpacesOption.ForceSpacesIfOnSingleLine)
        {
            if (option)
            {
                return CreateAdjustSpacesOperation(1, explicitOption);
            }
            else
            {
                return CreateAdjustSpacesOperation(0, explicitOption);
            }
        }

        private static bool HasFormattableBracketParent(SyntaxToken token)
            => token.Parent.IsKind(SyntaxKind.ArrayRankSpecifier, SyntaxKind.BracketedArgumentList, SyntaxKind.BracketedParameterList, SyntaxKind.ImplicitArrayCreationExpression, SyntaxKind.ListPattern);

        private static bool IsFunctionLikeKeywordExpressionKind(SyntaxKind syntaxKind)
            => (syntaxKind is SyntaxKind.TypeOfExpression or SyntaxKind.DefaultExpression or SyntaxKind.SizeOfExpression);

        private static bool IsControlFlowLikeKeywordStatementKind(SyntaxKind syntaxKind)
        {
            return syntaxKind is SyntaxKind.IfStatement or SyntaxKind.WhileStatement or SyntaxKind.SwitchStatement or
                SyntaxKind.ForStatement or SyntaxKind.ForEachStatement or SyntaxKind.ForEachVariableStatement or
                SyntaxKind.DoStatement or
                SyntaxKind.CatchDeclaration or SyntaxKind.UsingStatement or SyntaxKind.LockStatement or
                SyntaxKind.FixedStatement or SyntaxKind.CatchFilterClause;
        }
    }
}
