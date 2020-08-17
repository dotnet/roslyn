// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal sealed class SpacingFormattingRule : BaseFormattingRule
    {
        private readonly CachedOptions _options;

        public SpacingFormattingRule()
            : this(new CachedOptions(null))
        {
        }

        private SpacingFormattingRule(CachedOptions options)
        {
            _options = options;
        }

        public override AbstractFormattingRule WithOptions(AnalyzerConfigOptions options)
        {
            var cachedOptions = new CachedOptions(options);

            if (cachedOptions == _options)
            {
                return this;
            }

            return new SpacingFormattingRule(cachedOptions);
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
                return AdjustSpacesOperationZeroOrOne(_options.SpacingAfterMethodDeclarationName);
            }

            // For Generic Method Declaration
            if (currentToken.IsOpenParenInParameterList() && previousKind == SyntaxKind.GreaterThanToken && previousParentKind == SyntaxKind.TypeParameterList)
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpacingAfterMethodDeclarationName);
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
                return AdjustSpacesOperationZeroOrOne(_options.SpacingAfterMethodDeclarationName);
            }

            // Case: public static Program operator !(Program p) { return null; }
            if (previousToken.Parent.IsKind(SyntaxKind.OperatorDeclaration) && currentToken.IsOpenParenInParameterListOfAOperationDeclaration())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpacingAfterMethodDeclarationName);
            }

            if (previousToken.IsOpenParenInParameterList() && currentToken.IsCloseParenInParameterList())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceBetweenEmptyMethodDeclarationParentheses);
            }

            if (previousToken.IsOpenParenInParameterList())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinMethodDeclarationParenthesis);
            }

            if (currentToken.IsCloseParenInParameterList())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinMethodDeclarationParenthesis);
            }

            // For Method Call
            //   MethodName ( args )
            // Or Positional Pattern
            //   x is TypeName ( args )
            if (currentToken.IsOpenParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceAfterMethodCallName);
            }

            if (previousToken.IsOpenParenInArgumentListOrPositionalPattern() && currentToken.IsCloseParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceBetweenEmptyMethodCallParentheses);
            }

            if (previousToken.IsOpenParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinMethodCallParentheses);
            }

            if (currentToken.IsCloseParenInArgumentListOrPositionalPattern())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinMethodCallParentheses);
            }

            // For spacing around: typeof, default, and sizeof; treat like a Method Call
            if (currentKind == SyntaxKind.OpenParenToken && IsFunctionLikeKeywordExpressionKind(currentParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceAfterMethodCallName);
            }

            if (previousKind == SyntaxKind.OpenParenToken && IsFunctionLikeKeywordExpressionKind(previousParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinMethodCallParentheses);
            }

            if (currentKind == SyntaxKind.CloseParenToken && IsFunctionLikeKeywordExpressionKind(currentParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinMethodCallParentheses);
            }

            // For Spacing b/n control flow keyword and paren. Parent check not needed.
            if (currentKind == SyntaxKind.OpenParenToken &&
                (previousKind == SyntaxKind.IfKeyword || previousKind == SyntaxKind.WhileKeyword || previousKind == SyntaxKind.SwitchKeyword ||
                previousKind == SyntaxKind.ForKeyword || previousKind == SyntaxKind.ForEachKeyword || previousKind == SyntaxKind.CatchKeyword ||
                previousKind == SyntaxKind.UsingKeyword || previousKind == SyntaxKind.WhenKeyword || previousKind == SyntaxKind.LockKeyword ||
                previousKind == SyntaxKind.FixedKeyword))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceAfterControlFlowStatementKeyword);
            }

            // For spacing between parenthesis and expression
            if ((previousToken.Parent.IsKind(SyntaxKind.ParenthesizedExpression, SyntaxKind.ParenthesizedPattern) && previousKind == SyntaxKind.OpenParenToken) ||
                (currentToken.Parent.IsKind(SyntaxKind.ParenthesizedExpression, SyntaxKind.ParenthesizedPattern) && currentKind == SyntaxKind.CloseParenToken))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinExpressionParentheses);
            }

            // For spacing between the parenthesis and the cast expression
            if ((previousParentKind == SyntaxKind.CastExpression && previousKind == SyntaxKind.OpenParenToken) ||
                (currentParentKind == SyntaxKind.CastExpression && currentKind == SyntaxKind.CloseParenToken))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinCastParentheses);
            }

            // Semicolons in an empty for statement.  i.e.   for(;;)
            if (previousParentKind == SyntaxKind.ForStatement
                && IsEmptyForStatement((ForStatementSyntax)previousToken.Parent!))
            {
                if (currentKind == SyntaxKind.SemicolonToken
                    && (previousKind != SyntaxKind.SemicolonToken
                        || _options.SpaceBeforeSemicolonsInForStatement))
                {
                    return AdjustSpacesOperationZeroOrOne(_options.SpaceBeforeSemicolonsInForStatement);
                }

                return AdjustSpacesOperationZeroOrOne(_options.SpaceAfterSemicolonsInForStatement);
            }

            // For spacing between the parenthesis and the expression inside the control flow expression
            if (previousKind == SyntaxKind.OpenParenToken && IsControlFlowLikeKeywordStatementKind(previousParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinOtherParentheses);
            }

            if (currentKind == SyntaxKind.CloseParenToken && IsControlFlowLikeKeywordStatementKind(currentParentKind))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinOtherParentheses);
            }

            // For spacing after the cast
            if (previousParentKind == SyntaxKind.CastExpression && previousKind == SyntaxKind.CloseParenToken)
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceAfterCast);
            }

            // For spacing Before Square Braces
            if (currentKind == SyntaxKind.OpenBracketToken && HasFormattableBracketParent(currentToken) && !previousToken.IsOpenBraceOrCommaOfObjectInitializer())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceBeforeOpenSquareBracket);
            }

            // For spacing empty square braces, also treat [,] as empty
            if (((currentKind == SyntaxKind.CloseBracketToken && previousKind == SyntaxKind.OpenBracketToken)
                || currentKind == SyntaxKind.OmittedArraySizeExpressionToken)
                && HasFormattableBracketParent(previousToken))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceBetweenEmptySquareBrackets);
            }

            // For spacing square brackets within
            if (previousKind == SyntaxKind.OpenBracketToken && HasFormattableBracketParent(previousToken))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinSquareBrackets);
            }

            if (currentKind == SyntaxKind.CloseBracketToken && previousKind != SyntaxKind.OmittedArraySizeExpressionToken && HasFormattableBracketParent(currentToken))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinSquareBrackets);
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
                return AdjustSpacesOperationZeroOrOne(_options.SpaceAfterColonInBaseTypeDeclaration);
            }

            // For spacing delimiters - before colon
            if (currentToken.IsColonInTypeBaseList())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceBeforeColonInBaseTypeDeclaration);
            }

            // For spacing delimiters - after comma
            if ((previousToken.IsCommaInArgumentOrParameterList() && currentKind != SyntaxKind.OmittedTypeArgumentToken)
                || previousToken.IsCommaInInitializerExpression()
                || (previousKind == SyntaxKind.CommaToken
                    && currentKind != SyntaxKind.OmittedArraySizeExpressionToken
                    && HasFormattableBracketParent(previousToken)))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceAfterComma);
            }

            // For spacing delimiters - before comma
            if ((currentToken.IsCommaInArgumentOrParameterList() && previousKind != SyntaxKind.OmittedTypeArgumentToken)
                || currentToken.IsCommaInInitializerExpression()
                || (currentKind == SyntaxKind.CommaToken
                    && previousKind != SyntaxKind.OmittedArraySizeExpressionToken
                    && HasFormattableBracketParent(currentToken)))
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceBeforeComma);
            }

            // For Spacing delimiters - after Dot
            if (previousToken.IsDotInMemberAccessOrQualifiedName())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceAfterDot);
            }

            // For spacing delimiters - before Dot
            if (currentToken.IsDotInMemberAccessOrQualifiedName())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceBeforeDot);
            }

            // For spacing delimiters - after semicolon
            if (previousToken.IsSemicolonInForStatement() && currentKind != SyntaxKind.CloseParenToken)
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceAfterSemicolonsInForStatement);
            }

            // For spacing delimiters - before semicolon
            if (currentToken.IsSemicolonInForStatement())
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceBeforeSemicolonsInForStatement);
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
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinMethodDeclarationParenthesis);
            }

            // Respect spacing setting for before the > in function pointer parameter lists
            // delegate*<void>
            if (currentKind == SyntaxKind.GreaterThanToken && currentParentKind == SyntaxKind.FunctionPointerParameterList)
            {
                return AdjustSpacesOperationZeroOrOne(_options.SpaceWithinMethodDeclarationParenthesis);
            }

            // For spacing after the 'not' pattern operator
            if (previousToken.Parent.IsKind(SyntaxKind.NotPattern))
            {
                return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
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
                if (_options.SpacesIgnoreAroundVariableDeclaration)
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
            => token.Parent.IsKind(SyntaxKind.ArrayRankSpecifier, SyntaxKind.BracketedArgumentList, SyntaxKind.BracketedParameterList, SyntaxKind.ImplicitArrayCreationExpression);

        private static bool IsFunctionLikeKeywordExpressionKind(SyntaxKind syntaxKind)
            => (syntaxKind == SyntaxKind.TypeOfExpression || syntaxKind == SyntaxKind.DefaultExpression || syntaxKind == SyntaxKind.SizeOfExpression);

        private static bool IsControlFlowLikeKeywordStatementKind(SyntaxKind syntaxKind)
        {
            return (syntaxKind == SyntaxKind.IfStatement || syntaxKind == SyntaxKind.WhileStatement || syntaxKind == SyntaxKind.SwitchStatement ||
                syntaxKind == SyntaxKind.ForStatement || syntaxKind == SyntaxKind.ForEachStatement || syntaxKind == SyntaxKind.ForEachVariableStatement ||
                syntaxKind == SyntaxKind.DoStatement ||
                syntaxKind == SyntaxKind.CatchDeclaration || syntaxKind == SyntaxKind.UsingStatement || syntaxKind == SyntaxKind.LockStatement ||
                syntaxKind == SyntaxKind.FixedStatement || syntaxKind == SyntaxKind.CatchFilterClause);
        }

        private readonly struct CachedOptions : IEquatable<CachedOptions>
        {
            public readonly bool SpacesIgnoreAroundVariableDeclaration;
            public readonly bool SpacingAfterMethodDeclarationName;
            public readonly bool SpaceBetweenEmptyMethodDeclarationParentheses;
            public readonly bool SpaceWithinMethodDeclarationParenthesis;
            public readonly bool SpaceAfterMethodCallName;
            public readonly bool SpaceBetweenEmptyMethodCallParentheses;
            public readonly bool SpaceWithinMethodCallParentheses;
            public readonly bool SpaceAfterControlFlowStatementKeyword;
            public readonly bool SpaceWithinExpressionParentheses;
            public readonly bool SpaceWithinCastParentheses;
            public readonly bool SpaceBeforeSemicolonsInForStatement;
            public readonly bool SpaceAfterSemicolonsInForStatement;
            public readonly bool SpaceWithinOtherParentheses;
            public readonly bool SpaceAfterCast;
            public readonly bool SpaceBeforeOpenSquareBracket;
            public readonly bool SpaceBetweenEmptySquareBrackets;
            public readonly bool SpaceWithinSquareBrackets;
            public readonly bool SpaceAfterColonInBaseTypeDeclaration;
            public readonly bool SpaceBeforeColonInBaseTypeDeclaration;
            public readonly bool SpaceAfterComma;
            public readonly bool SpaceBeforeComma;
            public readonly bool SpaceAfterDot;
            public readonly bool SpaceBeforeDot;
            public readonly BinaryOperatorSpacingOptions SpacingAroundBinaryOperator;

            public CachedOptions(AnalyzerConfigOptions? options)
            {
                SpacesIgnoreAroundVariableDeclaration = GetOptionOrDefault(options, CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration);
                SpacingAfterMethodDeclarationName = GetOptionOrDefault(options, CSharpFormattingOptions2.SpacingAfterMethodDeclarationName);
                SpaceBetweenEmptyMethodDeclarationParentheses = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses);
                SpaceWithinMethodDeclarationParenthesis = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis);
                SpaceAfterMethodCallName = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceAfterMethodCallName);
                SpaceBetweenEmptyMethodCallParentheses = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses);
                SpaceWithinMethodCallParentheses = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceWithinMethodCallParentheses);
                SpaceAfterControlFlowStatementKeyword = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword);
                SpaceWithinExpressionParentheses = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceWithinExpressionParentheses);
                SpaceWithinCastParentheses = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceWithinCastParentheses);
                SpaceBeforeSemicolonsInForStatement = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement);
                SpaceAfterSemicolonsInForStatement = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement);
                SpaceWithinOtherParentheses = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceWithinOtherParentheses);
                SpaceAfterCast = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceAfterCast);
                SpaceBeforeOpenSquareBracket = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket);
                SpaceBetweenEmptySquareBrackets = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets);
                SpaceWithinSquareBrackets = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceWithinSquareBrackets);
                SpaceAfterColonInBaseTypeDeclaration = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration);
                SpaceBeforeColonInBaseTypeDeclaration = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration);
                SpaceAfterComma = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceAfterComma);
                SpaceBeforeComma = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceBeforeComma);
                SpaceAfterDot = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceAfterDot);
                SpaceBeforeDot = GetOptionOrDefault(options, CSharpFormattingOptions2.SpaceBeforeDot);
                SpacingAroundBinaryOperator = GetOptionOrDefault(options, CSharpFormattingOptions2.SpacingAroundBinaryOperator);
            }

            public static bool operator ==(CachedOptions left, CachedOptions right)
                => left.Equals(right);

            public static bool operator !=(CachedOptions left, CachedOptions right)
                => !(left == right);

            private static T GetOptionOrDefault<T>(AnalyzerConfigOptions? options, Option2<T> option)
            {
                if (options is null)
                    return option.DefaultValue;

                return options.GetOption(option);
            }

            public override bool Equals(object? obj)
                => obj is CachedOptions options && Equals(options);

            public bool Equals(CachedOptions other)
            {
                return SpacesIgnoreAroundVariableDeclaration == other.SpacesIgnoreAroundVariableDeclaration
                    && SpacingAfterMethodDeclarationName == other.SpacingAfterMethodDeclarationName
                    && SpaceBetweenEmptyMethodDeclarationParentheses == other.SpaceBetweenEmptyMethodDeclarationParentheses
                    && SpaceWithinMethodDeclarationParenthesis == other.SpaceWithinMethodDeclarationParenthesis
                    && SpaceAfterMethodCallName == other.SpaceAfterMethodCallName
                    && SpaceBetweenEmptyMethodCallParentheses == other.SpaceBetweenEmptyMethodCallParentheses
                    && SpaceWithinMethodCallParentheses == other.SpaceWithinMethodCallParentheses
                    && SpaceAfterControlFlowStatementKeyword == other.SpaceAfterControlFlowStatementKeyword
                    && SpaceWithinExpressionParentheses == other.SpaceWithinExpressionParentheses
                    && SpaceWithinCastParentheses == other.SpaceWithinCastParentheses
                    && SpaceBeforeSemicolonsInForStatement == other.SpaceBeforeSemicolonsInForStatement
                    && SpaceAfterSemicolonsInForStatement == other.SpaceAfterSemicolonsInForStatement
                    && SpaceWithinOtherParentheses == other.SpaceWithinOtherParentheses
                    && SpaceAfterCast == other.SpaceAfterCast
                    && SpaceBeforeOpenSquareBracket == other.SpaceBeforeOpenSquareBracket
                    && SpaceBetweenEmptySquareBrackets == other.SpaceBetweenEmptySquareBrackets
                    && SpaceWithinSquareBrackets == other.SpaceWithinSquareBrackets
                    && SpaceAfterColonInBaseTypeDeclaration == other.SpaceAfterColonInBaseTypeDeclaration
                    && SpaceBeforeColonInBaseTypeDeclaration == other.SpaceBeforeColonInBaseTypeDeclaration
                    && SpaceAfterComma == other.SpaceAfterComma
                    && SpaceBeforeComma == other.SpaceBeforeComma
                    && SpaceAfterDot == other.SpaceAfterDot
                    && SpaceBeforeDot == other.SpaceBeforeDot
                    && SpacingAroundBinaryOperator == other.SpacingAroundBinaryOperator;
            }

            public override int GetHashCode()
            {
                var hashCode = 0;
                hashCode = (hashCode << 1) + (SpacesIgnoreAroundVariableDeclaration ? 1 : 0);
                hashCode = (hashCode << 1) + (SpacingAfterMethodDeclarationName ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceBetweenEmptyMethodDeclarationParentheses ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceWithinMethodDeclarationParenthesis ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceAfterMethodCallName ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceBetweenEmptyMethodCallParentheses ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceWithinMethodCallParentheses ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceAfterControlFlowStatementKeyword ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceWithinExpressionParentheses ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceWithinCastParentheses ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceBeforeSemicolonsInForStatement ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceAfterSemicolonsInForStatement ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceWithinOtherParentheses ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceAfterCast ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceBeforeOpenSquareBracket ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceBetweenEmptySquareBrackets ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceWithinSquareBrackets ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceAfterColonInBaseTypeDeclaration ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceBeforeColonInBaseTypeDeclaration ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceAfterComma ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceBeforeComma ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceAfterDot ? 1 : 0);
                hashCode = (hashCode << 1) + (SpaceBeforeDot ? 1 : 0);
                hashCode = (hashCode << 2) + (int)SpacingAroundBinaryOperator;
                return hashCode;
            }
        }
    }
}
