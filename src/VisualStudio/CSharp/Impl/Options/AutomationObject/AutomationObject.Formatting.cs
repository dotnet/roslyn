// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int Indent_BlockContents
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.IndentBlock); }
            set { SetBooleanOption(CSharpFormattingOptions2.IndentBlock, value); }
        }

        public int Indent_Braces
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.IndentBraces); }
            set { SetBooleanOption(CSharpFormattingOptions2.IndentBraces, value); }
        }

        public int Indent_CaseContents
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.IndentSwitchCaseSection); }
            set { SetBooleanOption(CSharpFormattingOptions2.IndentSwitchCaseSection, value); }
        }

        public int Indent_CaseContentsWhenBlock
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock); }
            set { SetBooleanOption(CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock, value); }
        }

        public int Indent_CaseLabels
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.IndentSwitchSection); }
            set { SetBooleanOption(CSharpFormattingOptions2.IndentSwitchSection, value); }
        }

        public int Indent_FlushLabelsLeft
        {
            get { return (int)GetOption(CSharpFormattingOptions2.LabelPositioning); }
            set { SetOption(CSharpFormattingOptions2.LabelPositioning, (LabelPositionOptions)value); }
        }

        public int Indent_UnindentLabels
        {
            get { return (int)GetOption(CSharpFormattingOptions2.LabelPositioning); }
            set { SetOption(CSharpFormattingOptions2.LabelPositioning, (LabelPositionOptions)value); }
        }

        public int NewLines_AnonymousTypeInitializer_EachMember
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes, value); }
        }

        public int NewLines_Braces_AnonymousMethod
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.AnonymousMethods); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.AnonymousMethods, value); }
        }

        public int NewLines_Braces_AnonymousTypeInitializer
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.AnonymousTypes); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.AnonymousTypes, value); }
        }

        public int NewLines_Braces_ControlFlow
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.ControlBlocks); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.ControlBlocks, value); }
        }

        public int NewLines_Braces_LambdaExpressionBody
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.LambdaExpressionBody); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.LambdaExpressionBody, value); }
        }

        public int NewLines_Braces_Method
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.Methods); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.Methods, value); }
        }

        public int NewLines_Braces_Property
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.Properties); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.Properties, value); }
        }

        public int NewLines_Braces_Accessor
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.Accessors); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.Accessors, value); }
        }

        public int NewLines_Braces_ObjectInitializer
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, value); }
        }

        public int NewLines_Braces_Type
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.Types); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.Types, value); }
        }

        public int NewLines_Keywords_Catch
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineForCatch); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineForCatch, value); }
        }

        public int NewLines_Keywords_Else
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineForElse); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineForElse, value); }
        }

        public int NewLines_Keywords_Finally
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineForFinally); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineForFinally, value); }
        }

        public int NewLines_ObjectInitializer_EachMember
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineForMembersInObjectInit); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineForMembersInObjectInit, value); }
        }

        public int NewLines_QueryExpression_EachClause
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineForClausesInQuery); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineForClausesInQuery, value); }
        }

        public int Space_AfterBasesColon
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration, value); }
        }

        public int Space_AfterCast
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceAfterCast); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceAfterCast, value); }
        }

        public int Space_AfterComma
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceAfterComma); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceAfterComma, value); }
        }

        public int Space_AfterDot
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceAfterDot); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceAfterDot, value); }
        }

        public int Space_AfterMethodCallName
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceAfterMethodCallName); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceAfterMethodCallName, value); }
        }

        public int Space_AfterMethodDeclarationName
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName, value); }
        }

        public int Space_AfterSemicolonsInForStatement
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement, value); }
        }

        public int Space_AroundBinaryOperator
        {
            get { return (int)GetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator); }
            set { SetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator, (BinaryOperatorSpacingOptions)value); }
        }

        public int Space_BeforeBasesColon
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration, value); }
        }

        public int Space_BeforeComma
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceBeforeComma); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceBeforeComma, value); }
        }

        public int Space_BeforeDot
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceBeforeDot); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceBeforeDot, value); }
        }

        public int Space_BeforeOpenSquare
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket, value); }
        }

        public int Space_BeforeSemicolonsInForStatement
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement, value); }
        }

        public int Space_BetweenEmptyMethodCallParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses, value); }
        }

        public int Space_BetweenEmptyMethodDeclarationParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses, value); }
        }

        public int Space_BetweenEmptySquares
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets, value); }
        }

        public int Space_InControlFlowConstruct
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword, value); }
        }

        public int Space_WithinCastParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceBetweenParentheses, SpacePlacementWithinParentheses.TypeCasts); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceBetweenParentheses, SpacePlacementWithinParentheses.TypeCasts, value); }
        }

        public int Space_WithinExpressionParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceBetweenParentheses, SpacePlacementWithinParentheses.Expressions); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceBetweenParentheses, SpacePlacementWithinParentheses.Expressions, value); }
        }

        public int Space_WithinOtherParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceBetweenParentheses, SpacePlacementWithinParentheses.ControlFlowStatements); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceBetweenParentheses, SpacePlacementWithinParentheses.ControlFlowStatements, value); }
        }

        public int Space_WithinMethodCallParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceWithinMethodCallParentheses); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceWithinMethodCallParentheses, value); }
        }

        public int Space_WithinMethodDeclarationParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis, value); }
        }

        public int Space_WithinSquares
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceWithinSquareBrackets); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceWithinSquareBrackets, value); }
        }

        public int Wrapping_IgnoreSpacesAroundBinaryOperators
        {
            get { return (int)GetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator); }
            set { SetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator, (BinaryOperatorSpacingOptions)value); }
        }

        public int Wrapping_IgnoreSpacesAroundVariableDeclaration
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration, value); }
        }

        public int Wrapping_KeepStatementsOnSingleLine
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine); }
            set { SetBooleanOption(CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, value); }
        }

        public int Wrapping_PreserveSingleLine
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.WrappingPreserveSingleLine); }
            set { SetBooleanOption(CSharpFormattingOptions2.WrappingPreserveSingleLine, value); }
        }

        public int Formatting_TriggerOnPaste
        {
            get { return GetBooleanOption(FormattingOptionsStorage.FormatOnPaste); }
            set { SetBooleanOption(FormattingOptionsStorage.FormatOnPaste, value); }
        }

        public int Formatting_TriggerOnStatementCompletion
        {
            get { return GetBooleanOption(AutoFormattingOptionsStorage.FormatOnSemicolon); }
            set { SetBooleanOption(AutoFormattingOptionsStorage.FormatOnSemicolon, value); }
        }

        public int AutoFormattingOnTyping
        {
            get { return GetBooleanOption(AutoFormattingOptionsStorage.FormatOnTyping); }
            set { SetBooleanOption(AutoFormattingOptionsStorage.FormatOnTyping, value); }
        }
    }
}
