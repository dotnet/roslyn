// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [ExportFormattingRule(Name, LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = QueryExpressionFormattingRule.Name)]
    internal class TokenBasedFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp Token Based Formatting Rule";

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
        {
            ////////////////////////////////////////////////////
            // brace related operations
            // * { or * }
            switch (currentToken.CSharpKind())
            {
                case SyntaxKind.OpenBraceToken:
                    if (!previousToken.IsParenInParenthesizedExpression() && previousToken.Parent != null && !previousToken.Parent.IsKind(SyntaxKind.ArrayRankSpecifier))
                    {
                        return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                    }

                    break;

                case SyntaxKind.CloseBraceToken:
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            // do { } while case
            if (previousToken.CSharpKind() == SyntaxKind.CloseBraceToken && currentToken.CSharpKind() == SyntaxKind.WhileKeyword)
            {
                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
            }

            // { * or } *
            switch (previousToken.CSharpKind())
            {
                case SyntaxKind.CloseBraceToken:
                    if (!previousToken.IsCloseBraceOfExpression())
                    {
                        if (currentToken.CSharpKind() != SyntaxKind.SemicolonToken &&
                            !currentToken.IsParenInParenthesizedExpression() &&
                            !currentToken.IsCommaInInitializerExpression() &&
                            !currentToken.IsCommaInAnyArgumentsList() &&
                            !currentToken.IsParenInArgumentList() &&
                            !currentToken.IsDotInMemberAccess() &&
                            !currentToken.IsCloseParenInStatement())
                        {
                            return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                        }
                    }

                    break;

                case SyntaxKind.OpenBraceToken:
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            ///////////////////////////////////////////////////
            // statement related operations
            // object and anonymous initializer "," case
            if (previousToken.IsCommaInInitializerExpression())
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            // else * except else if case
            if (previousToken.CSharpKind() == SyntaxKind.ElseKeyword && currentToken.CSharpKind() != SyntaxKind.IfKeyword)
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            // , * in enum declarations
            if (previousToken.IsCommaInEnumDeclaration())
            {
                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
            }

            // : cases
            if (previousToken.IsColonInSwitchLabel() ||
                previousToken.IsColonInLabeledStatement())
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            // embedded statement 
            if (previousToken.CSharpKind() == SyntaxKind.CloseParenToken && previousToken.Parent.IsEmbeddedStatementOwnerWithCloseParen())
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            if (previousToken.CSharpKind() == SyntaxKind.DoKeyword && previousToken.Parent.CSharpKind() == SyntaxKind.DoStatement)
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            // for (int i = 10; i < 10; i++) case
            if (previousToken.IsSemicolonInForStatement())
            {
                return nextOperation.Invoke();
            }

            // ; case in the switch case statement and else condition
            if (previousToken.CSharpKind() == SyntaxKind.SemicolonToken &&
                (currentToken.CSharpKind() == SyntaxKind.CaseKeyword || currentToken.CSharpKind() == SyntaxKind.DefaultKeyword || currentToken.CSharpKind() == SyntaxKind.ElseKeyword))
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            // ; * or ; * for using directive
            if (previousToken.CSharpKind() == SyntaxKind.SemicolonToken)
            {
                var line = (previousToken.Parent is UsingDirectiveSyntax) ? 1 : 0;
                return CreateAdjustNewLinesOperation(line, AdjustNewLinesOption.PreserveLines);
            }

            // attribute case ] *
            // force to next line for top level attributes
            if (previousToken.CSharpKind() == SyntaxKind.CloseBracketToken && previousToken.Parent is AttributeListSyntax)
            {
                var attributeOwner = (previousToken.Parent != null) ? previousToken.Parent.Parent : null;

                if (attributeOwner is CompilationUnitSyntax ||
                    attributeOwner is MemberDeclarationSyntax ||
                    attributeOwner is AccessorDeclarationSyntax)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }

                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
            }

            return nextOperation.Invoke();
        }

        public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustSpacesOperation> nextOperation)
        {
            //////////////////////////////////////////////////////
            // ";" related operations
            if (currentToken.CSharpKind() == SyntaxKind.SemicolonToken)
            {
                // ; ;
                if (previousToken.CSharpKind() == SyntaxKind.SemicolonToken)
                {
                    return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                // ) ; with embedded statement case
                if (previousToken.CSharpKind() == SyntaxKind.CloseParenToken && previousToken.Parent.IsEmbeddedStatementOwnerWithCloseParen())
                {
                    return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                // * ;
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // omitted tokens case
            if (previousToken.CSharpKind() == SyntaxKind.OmittedArraySizeExpressionToken ||
                previousToken.CSharpKind() == SyntaxKind.OmittedTypeArgumentToken ||
                currentToken.CSharpKind() == SyntaxKind.OmittedArraySizeExpressionToken ||
                currentToken.CSharpKind() == SyntaxKind.OmittedTypeArgumentToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // some * "(" cases
            if (currentToken.CSharpKind() == SyntaxKind.OpenParenToken)
            {
                if (previousToken.CSharpKind() == SyntaxKind.IdentifierToken ||
                    previousToken.CSharpKind() == SyntaxKind.DefaultKeyword ||
                    previousToken.CSharpKind() == SyntaxKind.BaseKeyword ||
                    previousToken.CSharpKind() == SyntaxKind.ThisKeyword ||
                    previousToken.CSharpKind() == SyntaxKind.NewKeyword ||
                    previousToken.Parent.CSharpKind() == SyntaxKind.OperatorDeclaration ||
                    previousToken.IsGenericGreaterThanToken() ||
                    currentToken.IsParenInArgumentList())
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }
            }

            // empty () or []
            if (previousToken.ParenOrBracketContainsNothing(currentToken))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // attribute case
            // , [
            if (previousToken.CSharpKind() == SyntaxKind.CommaToken && currentToken.CSharpKind() == SyntaxKind.OpenBracketToken && currentToken.Parent is AttributeListSyntax)
            {
                return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // ] *
            if (previousToken.CSharpKind() == SyntaxKind.CloseBracketToken && previousToken.Parent is AttributeListSyntax)
            {
                // preserving dev10 behavior, in dev10 we didn't touch space after attribute
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces);
            }

            // * )
            // * [
            // * ]
            // * ,
            // * .
            // * ->
            switch (currentToken.CSharpKind())
            {
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.OpenBracketToken:
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.CommaToken:
                case SyntaxKind.DotToken:
                case SyntaxKind.MinusGreaterThanToken:
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // case * :
            // default:
            // <label> :
            if (currentToken.IsKind(SyntaxKind.ColonToken))
            {
                if (currentToken.Parent.IsKind(SyntaxKind.CaseSwitchLabel,
                                               SyntaxKind.DefaultSwitchLabel,
                                               SyntaxKind.LabeledStatement,
                                               SyntaxKind.AttributeTargetSpecifier,
                                               SyntaxKind.NameColon))
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }
            }

            // [cast expression] * case
            if (previousToken.Parent is CastExpressionSyntax &&
                previousToken.CSharpKind() == SyntaxKind.CloseParenToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // generic name
            if (previousToken.Parent.CSharpKind() == SyntaxKind.TypeArgumentList || previousToken.Parent.CSharpKind() == SyntaxKind.TypeParameterList)
            {
                // generic name < * 
                if (previousToken.CSharpKind() == SyntaxKind.LessThanToken)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                // generic name > *
                if (previousToken.CSharpKind() == SyntaxKind.GreaterThanToken && currentToken.CSharpKind() == SyntaxKind.GreaterThanToken)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }
            }

            // generic name * < or * >
            if ((currentToken.CSharpKind() == SyntaxKind.LessThanToken || currentToken.CSharpKind() == SyntaxKind.GreaterThanToken) &&
                (currentToken.Parent.CSharpKind() == SyntaxKind.TypeArgumentList || currentToken.Parent.CSharpKind() == SyntaxKind.TypeParameterList))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // ++ * or -- *
            if ((previousToken.CSharpKind() == SyntaxKind.PlusPlusToken || previousToken.CSharpKind() == SyntaxKind.MinusMinusToken) &&
                 previousToken.Parent is PrefixUnaryExpressionSyntax)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // * ++ or * --
            if ((currentToken.CSharpKind() == SyntaxKind.PlusPlusToken || currentToken.CSharpKind() == SyntaxKind.MinusMinusToken) &&
                 currentToken.Parent is PostfixUnaryExpressionSyntax)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // For spacing between the identifer and the conditional operator 
            if (currentToken.IsKind(SyntaxKind.QuestionToken) && currentToken.Parent.CSharpKind() == SyntaxKind.ConditionalAccessExpression)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // ( * or ) * or [ * or ] * or . * or -> *
            switch (previousToken.CSharpKind())
            {
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.OpenBracketToken:
                case SyntaxKind.DotToken:
                case SyntaxKind.MinusGreaterThanToken:
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);

                case SyntaxKind.CloseParenToken:
                case SyntaxKind.CloseBracketToken:
                    int space = (previousToken.CSharpKind() == currentToken.CSharpKind()) ? 0 : 1;
                    return CreateAdjustSpacesOperation(space, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // +1 or -1
            if (previousToken.IsPlusOrMinusExpression() && !currentToken.IsPlusOrMinusExpression())
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // +- or -+ 
            if (previousToken.IsPlusOrMinusExpression() && currentToken.IsPlusOrMinusExpression() &&
                previousToken.CSharpKind() != currentToken.CSharpKind())
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // ! *
            if (previousToken.CSharpKind() == SyntaxKind.ExclamationToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // nullable
            if (currentToken.CSharpKind() == SyntaxKind.QuestionToken &&
                currentToken.Parent.CSharpKind() == SyntaxKind.NullableType)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // pointer case
            if ((currentToken.CSharpKind() == SyntaxKind.AsteriskToken && currentToken.Parent is PointerTypeSyntax) ||
                (previousToken.CSharpKind() == SyntaxKind.AsteriskToken && previousToken.Parent is PrefixUnaryExpressionSyntax))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // ~ * case
            if (previousToken.CSharpKind() == SyntaxKind.TildeToken && (previousToken.Parent is PrefixUnaryExpressionSyntax || previousToken.Parent is DestructorDeclarationSyntax))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // & * case
            if (previousToken.CSharpKind() == SyntaxKind.AmpersandToken &&
                previousToken.Parent is PrefixUnaryExpressionSyntax)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // * :: or :: * case
            if (previousToken.CSharpKind() == SyntaxKind.ColonColonToken || currentToken.CSharpKind() == SyntaxKind.ColonColonToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            return nextOperation.Invoke();
        }
    }
}