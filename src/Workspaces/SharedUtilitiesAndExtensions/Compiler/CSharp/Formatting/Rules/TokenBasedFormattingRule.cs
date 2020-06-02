﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal sealed class TokenBasedFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp Token Based Formatting Rule";

        private readonly CachedOptions _options;

        public TokenBasedFormattingRule()
            : this(new CachedOptions(null))
        {
        }

        private TokenBasedFormattingRule(CachedOptions options)
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

            return new TokenBasedFormattingRule(cachedOptions);
        }

        public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
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
                            !currentToken.IsCommaInTupleExpression() &&
                            !currentToken.IsParenInArgumentList() &&
                            !currentToken.IsDotInMemberAccess() &&
                            !currentToken.IsCloseParenInStatement() &&
                            !currentToken.IsEqualsTokenInAutoPropertyInitializers() &&
                            !currentToken.IsColonInCasePatternSwitchLabel() && // no newline required before colon in pattern-switch-label (ex: `case {<pattern>}:`)
                            !currentToken.IsColonInSwitchExpressionArm())  // no newline required before colon in switch-expression-arm (ex: `{<pattern>}: expression`)
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

            // , * in switch expression arm
            // ```
            // e switch
            // {
            //     pattern1: expression1, // newline with minimum of 1 line (each arm must be on its own line)
            //     pattern2: expression2 ...
            // ```
            if (previousToken.IsCommaInSwitchExpression())
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            // , * in property sub-pattern
            // ```
            // e is
            // {
            //     property1: pattern1, // newline so the next line should be indented same as this one
            //     property2: pattern2, property3: pattern3, ... // but with minimum 0 lines so each property isn't forced to its own line
            // ```
            if (previousToken.IsCommaInPropertyPatternClause())
            {
                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
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

            if (previousToken.Kind() == SyntaxKind.DoKeyword && previousToken.Parent.IsKind(SyntaxKind.DoStatement))
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            // for (int i = 10; i < 10; i++) case
            if (previousToken.IsSemicolonInForStatement())
            {
                return nextOperation.Invoke(in previousToken, in currentToken);
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
                return AdjustNewLinesAfterSemicolonToken(previousToken, currentToken);
            }

            // attribute case ] *
            // force to next line for top level attributes
            if (previousToken.Kind() == SyntaxKind.CloseBracketToken && previousToken.Parent is AttributeListSyntax)
            {
                var attributeOwner = previousToken.Parent?.Parent;

                if (attributeOwner is CompilationUnitSyntax ||
                    attributeOwner is MemberDeclarationSyntax ||
                    attributeOwner is AccessorDeclarationSyntax)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }

                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
            }

            return nextOperation.Invoke(in previousToken, in currentToken);
        }

        private AdjustNewLinesOperation AdjustNewLinesAfterSemicolonToken(
            SyntaxToken previousToken, SyntaxToken currentToken)
        {
            // between anything that isn't a using directive, we don't touch newlines after a semicolon
            if (!(previousToken.Parent is UsingDirectiveSyntax previousUsing))
                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);

            // if the user is separating using-groups, and we're between two usings, and these
            // usings *should* be separated, then do so (if the usings were already properly
            // sorted).
            if (_options.SeparateImportDirectiveGroups &&
                currentToken.Parent is UsingDirectiveSyntax currentUsing &&
                UsingsAndExternAliasesOrganizer.NeedsGrouping(previousUsing, currentUsing))
            {
                RoslynDebug.AssertNotNull(currentUsing.Parent);

                var usings = GetUsings(currentUsing.Parent);
                if (usings.IsSorted(UsingsAndExternAliasesDirectiveComparer.SystemFirstInstance) ||
                    usings.IsSorted(UsingsAndExternAliasesDirectiveComparer.NormalInstance))
                {
                    // Force at least one blank line here.
                    return CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.PreserveLines);
                }
            }

            // For all other cases where we have a using-directive, just make sure it's followed by
            // a new-line.
            return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
        }

        private static SyntaxList<UsingDirectiveSyntax> GetUsings(SyntaxNode node)
            => node switch
            {
                CompilationUnitSyntax compilationUnit => compilationUnit.Usings,
                NamespaceDeclarationSyntax namespaceDecl => namespaceDecl.Usings,
                _ => throw ExceptionUtilities.UnexpectedValue(node.Kind()),
            };

        public override AdjustSpacesOperation? GetAdjustSpacesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustSpacesOperation nextOperation)
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

            if (previousToken.IsKind(SyntaxKind.CloseBracketToken) &&
                previousToken.Parent.IsKind(SyntaxKind.AttributeList) &&
                previousToken.Parent.IsParentKind(SyntaxKind.Parameter))
            {
                if (currentToken.IsKind(SyntaxKind.OpenBracketToken))
                {
                    // multiple attribute on parameter stick together
                    // void M([...][...]
                    return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }
                else
                {
                    // attribute is spaced from parameter type
                    // void M([...] int
                    return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }
            }

            // extension method on tuple type
            // M(this (
            if (currentToken.Kind() == SyntaxKind.OpenParenToken &&
                previousToken.Kind() == SyntaxKind.ThisKeyword)
            {
                return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // new (int, int)[]
            if (currentToken.Kind() == SyntaxKind.OpenParenToken &&
                previousToken.Kind() == SyntaxKind.NewKeyword &&
                previousToken.Parent.IsKind(SyntaxKind.ObjectCreationExpression, SyntaxKind.ArrayCreationExpression))
            {
                return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
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
                                               SyntaxKind.NameColon,
                                               SyntaxKind.SwitchExpressionArm))
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
            if (previousToken.Parent.IsKind(SyntaxKind.TypeArgumentList, SyntaxKind.TypeParameterList, SyntaxKindEx.FunctionPointerType))
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
                currentToken.Parent.IsKind(SyntaxKind.TypeArgumentList, SyntaxKind.TypeParameterList))
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
            if (currentToken.IsKind(SyntaxKind.QuestionToken) && currentToken.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // nullable
            if (currentToken.Kind() == SyntaxKind.QuestionToken &&
                currentToken.Parent.IsKind(SyntaxKind.NullableType, SyntaxKind.ClassConstraint))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // No space between an array type and ?
            if (currentToken.IsKind(SyntaxKind.QuestionToken) &&
                previousToken.Parent?.IsParentKind(SyntaxKind.ArrayType) == true)
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces);
            }

            // suppress warning operator: null! or x! or x++! or x[i]! or (x)! or ...
            if (currentToken.Kind() == SyntaxKind.ExclamationToken &&
                currentToken.Parent.IsKind(SyntaxKind.SuppressNullableWarningExpression))
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
                    var space = (previousToken.Kind() == currentToken.Kind()) ? 0 : 1;
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

            // ! *, except where ! is the suppress nullable warning operator
            if (previousToken.Kind() == SyntaxKind.ExclamationToken
                && !previousToken.Parent.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            {
                return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
            }

            // pointer case for regular pointers
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

            return nextOperation.Invoke(in previousToken, in currentToken);
        }

        private readonly struct CachedOptions : IEquatable<CachedOptions>
        {
            public readonly bool SeparateImportDirectiveGroups;

            public CachedOptions(AnalyzerConfigOptions? options)
            {
                SeparateImportDirectiveGroups = GetOptionOrDefault(options, GenerationOptions.SeparateImportDirectiveGroups);
            }

            public static bool operator ==(CachedOptions left, CachedOptions right)
                => left.Equals(right);

            public static bool operator !=(CachedOptions left, CachedOptions right)
                => !(left == right);

            private static T GetOptionOrDefault<T>(AnalyzerConfigOptions? options, PerLanguageOption2<T> option)
            {
                if (options is null)
                    return option.DefaultValue;

                return options.GetOption(option);
            }

            public override bool Equals(object? obj)
                => obj is CachedOptions options && Equals(options);

            public bool Equals(CachedOptions other)
            {
                return SeparateImportDirectiveGroups == other.SeparateImportDirectiveGroups;
            }

            public override int GetHashCode()
            {
                var hashCode = 0;
                hashCode = (hashCode << 1) + (SeparateImportDirectiveGroups ? 1 : 0);
                return hashCode;
            }
        }
    }
}
