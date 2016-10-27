﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
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
            switch (currentToken.Kind())
            {
                case SyntaxKind.OpenBraceToken:
                    if (currentToken.IsInterpolation())
                    {
                        return null;
                    }

                    if (!previousToken.IsParenInParenthesizedExpression())
                    {
                        return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                    }

                    break;

                case SyntaxKind.CloseBraceToken:
                    if (currentToken.IsInterpolation())
                    {
                        return null;
                    }

                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            // do { } while case
            if (previousToken.Kind() == SyntaxKind.CloseBraceToken && currentToken.Kind() == SyntaxKind.WhileKeyword)
            {
                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
            }

            // { * or } *
            switch (previousToken.Kind())
            {
                case SyntaxKind.CloseBraceToken:
                    if (previousToken.IsInterpolation())
                    {
                        return null;
                    }

                    if (!previousToken.IsCloseBraceOfExpression())
                    {
                        if (!currentToken.IsKind(SyntaxKind.SemicolonToken) &&
                            !currentToken.IsParenInParenthesizedExpression() &&
                            !currentToken.IsCommaInInitializerExpression() &&
                            !currentToken.IsCommaInAnyArgumentsList() &&
                            !currentToken.IsParenInArgumentList() &&
                            !currentToken.IsDotInMemberAccess() &&
                            !currentToken.IsCloseParenInStatement() &&
                            !currentToken.IsEqualsTokenInAutoPropertyInitializers())
                        {
                            return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                        }
                    }

                    break;

                case SyntaxKind.OpenBraceToken:
                    if (previousToken.IsInterpolation())
                    {
                        return null;
                    }

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
            if (previousToken.Kind() == SyntaxKind.ElseKeyword && currentToken.Kind() != SyntaxKind.IfKeyword)
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
            if (previousToken.Kind() == SyntaxKind.CloseParenToken && previousToken.Parent.IsEmbeddedStatementOwnerWithCloseParen())
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            if (previousToken.Kind() == SyntaxKind.DoKeyword && previousToken.Parent.Kind() == SyntaxKind.DoStatement)
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            // for (int i = 10; i < 10; i++) case
            if (previousToken.IsSemicolonInForStatement())
            {
                return nextOperation.Invoke();
            }

            // ; case in the switch case statement and else condition
            if (previousToken.Kind() == SyntaxKind.SemicolonToken &&
                (currentToken.Kind() == SyntaxKind.CaseKeyword || currentToken.Kind() == SyntaxKind.DefaultKeyword || currentToken.Kind() == SyntaxKind.ElseKeyword))
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            // ; * or ; * for using directive
            if (previousToken.Kind() == SyntaxKind.SemicolonToken)
            {
                var line = (previousToken.Parent is UsingDirectiveSyntax) ? 1 : 0;
                return CreateAdjustNewLinesOperation(line, AdjustNewLinesOption.PreserveLines);
            }

            // attribute case ] *
            // force to next line for top level attributes
            if (previousToken.Kind() == SyntaxKind.CloseBracketToken && previousToken.Parent is AttributeListSyntax)
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
            if (currentToken.Kind() == SyntaxKind.SemicolonToken)
            {
                // ; ;
                if (previousToken.Kind() == SyntaxKind.SemicolonToken)
                {
                    return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                // ) ; with embedded statement case
                if (previousToken.Kind() == SyntaxKind.CloseParenToken && previousToken.Parent.IsEmbeddedStatementOwnerWithCloseParen())
                {
                    return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                // * ;
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // omitted tokens case
            if (previousToken.Kind() == SyntaxKind.OmittedArraySizeExpressionToken ||
                previousToken.Kind() == SyntaxKind.OmittedTypeArgumentToken ||
                currentToken.Kind() == SyntaxKind.OmittedArraySizeExpressionToken ||
                currentToken.Kind() == SyntaxKind.OmittedTypeArgumentToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // some * "(" cases
            if (currentToken.Kind() == SyntaxKind.OpenParenToken)
            {
                if (previousToken.Kind() == SyntaxKind.IdentifierToken ||
                    previousToken.Kind() == SyntaxKind.DefaultKeyword ||
                    previousToken.Kind() == SyntaxKind.BaseKeyword ||
                    previousToken.Kind() == SyntaxKind.ThisKeyword ||
                    previousToken.Kind() == SyntaxKind.NewKeyword ||
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
            if (previousToken.Kind() == SyntaxKind.CommaToken && currentToken.Kind() == SyntaxKind.OpenBracketToken && currentToken.Parent is AttributeListSyntax)
            {
                return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // ] *
            if (previousToken.Kind() == SyntaxKind.CloseBracketToken && previousToken.Parent is AttributeListSyntax)
            {
                // preserving dev10 behavior, in dev10 we didn't touch space after attribute
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces);
            }

            // * )
            // * ]
            // * ,
            // * .
            // * ->
            switch (currentToken.Kind())
            {
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.CommaToken:
                case SyntaxKind.DotToken:
                case SyntaxKind.MinusGreaterThanToken:
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // * [
            if (currentToken.IsKind(SyntaxKind.OpenBracketToken) &&
                !previousToken.IsOpenBraceOrCommaOfObjectInitializer())
            {
                if (previousToken.IsOpenBraceOfAccessorList() ||
                    previousToken.IsLastTokenOfNode<AccessorDeclarationSyntax>())
                {
                    return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }
                else
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }
            }

            // case * :
            // default:
            // <label> :
            if (currentToken.IsKind(SyntaxKind.ColonToken))
            {
                if (currentToken.Parent.IsKind(SyntaxKind.CaseSwitchLabel,
                                               SyntaxKind.CasePatternSwitchLabel,
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
                previousToken.Kind() == SyntaxKind.CloseParenToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // generic name
            if (previousToken.Parent.Kind() == SyntaxKind.TypeArgumentList || previousToken.Parent.Kind() == SyntaxKind.TypeParameterList)
            {
                // generic name < * 
                if (previousToken.Kind() == SyntaxKind.LessThanToken)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                // generic name > *
                if (previousToken.Kind() == SyntaxKind.GreaterThanToken && currentToken.Kind() == SyntaxKind.GreaterThanToken)
                {
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }
            }

            // generic name * < or * >
            if ((currentToken.Kind() == SyntaxKind.LessThanToken || currentToken.Kind() == SyntaxKind.GreaterThanToken) &&
                (currentToken.Parent.Kind() == SyntaxKind.TypeArgumentList || currentToken.Parent.Kind() == SyntaxKind.TypeParameterList))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // ++ * or -- *
            if ((previousToken.Kind() == SyntaxKind.PlusPlusToken || previousToken.Kind() == SyntaxKind.MinusMinusToken) &&
                 previousToken.Parent is PrefixUnaryExpressionSyntax)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // * ++ or * --
            if ((currentToken.Kind() == SyntaxKind.PlusPlusToken || currentToken.Kind() == SyntaxKind.MinusMinusToken) &&
                 currentToken.Parent is PostfixUnaryExpressionSyntax)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // For spacing between the identifier and the conditional operator 
            if (currentToken.IsKind(SyntaxKind.QuestionToken) && currentToken.Parent.Kind() == SyntaxKind.ConditionalAccessExpression)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // nullable
            if (currentToken.Kind() == SyntaxKind.QuestionToken &&
                currentToken.Parent.Kind() == SyntaxKind.NullableType)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // ( * or ) * or [ * or ] * or . * or -> *
            switch (previousToken.Kind())
            {
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.OpenBracketToken:
                case SyntaxKind.DotToken:
                case SyntaxKind.MinusGreaterThanToken:
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);

                case SyntaxKind.CloseParenToken:
                case SyntaxKind.CloseBracketToken:
                    int space = (previousToken.Kind() == currentToken.Kind()) ? 0 : 1;
                    return CreateAdjustSpacesOperation(space, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // +1 or -1
            if (previousToken.IsPlusOrMinusExpression() && !currentToken.IsPlusOrMinusExpression())
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // +- or -+ 
            if (previousToken.IsPlusOrMinusExpression() && currentToken.IsPlusOrMinusExpression() &&
                previousToken.Kind() != currentToken.Kind())
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // ! *
            if (previousToken.Kind() == SyntaxKind.ExclamationToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // pointer case
            if ((currentToken.Kind() == SyntaxKind.AsteriskToken && currentToken.Parent is PointerTypeSyntax) ||
                (previousToken.Kind() == SyntaxKind.AsteriskToken && previousToken.Parent is PrefixUnaryExpressionSyntax))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // ~ * case
            if (previousToken.Kind() == SyntaxKind.TildeToken && (previousToken.Parent is PrefixUnaryExpressionSyntax || previousToken.Parent is DestructorDeclarationSyntax))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // & * case
            if (previousToken.Kind() == SyntaxKind.AmpersandToken &&
                previousToken.Parent is PrefixUnaryExpressionSyntax)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // * :: or :: * case
            if (previousToken.Kind() == SyntaxKind.ColonColonToken || currentToken.Kind() == SyntaxKind.ColonColonToken)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            return nextOperation.Invoke();
        }
    }
}
