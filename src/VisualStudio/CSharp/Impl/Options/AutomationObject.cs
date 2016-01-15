// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Completion;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    [ComVisible(true)]
    public class AutomationObject
    {
        private readonly IOptionService _optionService;

        internal AutomationObject(IOptionService optionService)
        {
            _optionService = optionService;
        }

        public int AutoComment
        {
            get { return GetBooleanOption(FeatureOnOffOptions.AutoXmlDocCommentGeneration); }
            set { SetBooleanOption(FeatureOnOffOptions.AutoXmlDocCommentGeneration, value); }
        }

        public int BringUpOnIdentifier
        {
            get { return GetBooleanOption(CompletionOptions.TriggerOnTypingLetters); }
            set { SetBooleanOption(CompletionOptions.TriggerOnTypingLetters, value); }
        }

        public int ClosedFileDiagnostics
        {
            get { return GetBooleanOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic); }
            set { SetBooleanOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, value); }
        }

        public int DisplayLineSeparators
        {
            get { return GetBooleanOption(FeatureOnOffOptions.LineSeparator); }
            set { SetBooleanOption(FeatureOnOffOptions.LineSeparator, value); }
        }

        public int EnableHighlightRelatedKeywords
        {
            get { return GetBooleanOption(FeatureOnOffOptions.KeywordHighlighting); }
            set { SetBooleanOption(FeatureOnOffOptions.KeywordHighlighting, value); }
        }

        public int EnterOutliningModeOnOpen
        {
            get { return GetBooleanOption(FeatureOnOffOptions.Outlining); }
            set { SetBooleanOption(FeatureOnOffOptions.Outlining, value); }
        }

        public int ExtractMethod_AllowMovingDeclaration
        {
            get { return GetBooleanOption(ExtractMethodOptions.AllowMovingDeclaration); }
            set { SetBooleanOption(ExtractMethodOptions.AllowMovingDeclaration, value); }
        }

        public int ExtractMethod_DoNotPutOutOrRefOnStruct
        {
            get { return GetBooleanOption(ExtractMethodOptions.DontPutOutOrRefOnStruct); }
            set { SetBooleanOption(ExtractMethodOptions.DontPutOutOrRefOnStruct, value); }
        }

        public int Formatting_TriggerOnBlockCompletion
        {
            get { return GetBooleanOption(FeatureOnOffOptions.AutoFormattingOnCloseBrace); }
            set { SetBooleanOption(FeatureOnOffOptions.AutoFormattingOnCloseBrace, value); }
        }

        public int Formatting_TriggerOnPaste
        {
            get { return GetBooleanOption(FeatureOnOffOptions.FormatOnPaste); }
            set { SetBooleanOption(FeatureOnOffOptions.FormatOnPaste, value); }
        }

        public int Formatting_TriggerOnStatementCompletion
        {
            get { return GetBooleanOption(FeatureOnOffOptions.AutoFormattingOnSemicolon); }
            set { SetBooleanOption(FeatureOnOffOptions.AutoFormattingOnSemicolon, value); }
        }

        public int HighlightReferences
        {
            get { return GetBooleanOption(FeatureOnOffOptions.ReferenceHighlighting); }
            set { SetBooleanOption(FeatureOnOffOptions.ReferenceHighlighting, value); }
        }

        public int Indent_BlockContents
        {
            get { return GetBooleanOption(CSharpFormattingOptions.IndentBlock); }
            set { SetBooleanOption(CSharpFormattingOptions.IndentBlock, value); }
        }

        public int Indent_Braces
        {
            get { return GetBooleanOption(CSharpFormattingOptions.IndentBraces); }
            set { SetBooleanOption(CSharpFormattingOptions.IndentBraces, value); }
        }

        public int Indent_CaseContents
        {
            get { return GetBooleanOption(CSharpFormattingOptions.IndentSwitchCaseSection); }
            set { SetBooleanOption(CSharpFormattingOptions.IndentSwitchCaseSection, value); }
        }

        public int Indent_CaseLabels
        {
            get { return GetBooleanOption(CSharpFormattingOptions.IndentSwitchSection); }
            set { SetBooleanOption(CSharpFormattingOptions.IndentSwitchSection, value); }
        }

        public int Indent_FlushLabelsLeft
        {
            get
            {
                var option = _optionService.GetOption(CSharpFormattingOptions.LabelPositioning);
                return option == LabelPositionOptions.LeftMost ? 1 : 0;
            }

            set
            {
                var optionSet = _optionService.GetOptions();
                optionSet = optionSet.WithChangedOption(CSharpFormattingOptions.LabelPositioning, value == 1 ? LabelPositionOptions.LeftMost : LabelPositionOptions.NoIndent);
                _optionService.SetOptions(optionSet);
            }
        }

        public int Indent_UnindentLabels
        {
            get
            {
                var option = _optionService.GetOption(CSharpFormattingOptions.LabelPositioning);
                return (int)option;
            }

            set
            {
                var optionSet = _optionService.GetOptions();
                optionSet = optionSet.WithChangedOption(CSharpFormattingOptions.LabelPositioning, value);
                _optionService.SetOptions(optionSet);
            }
        }

        public int InsertNewlineOnEnterWithWholeWord
        {
            get { return GetBooleanOption(CSharpCompletionOptions.AddNewLineOnEnterAfterFullyTypedWord); }
            set { SetBooleanOption(CSharpCompletionOptions.AddNewLineOnEnterAfterFullyTypedWord, value); }
        }

        public int NewLines_AnonymousTypeInitializer_EachMember
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLineForMembersInAnonymousTypes); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, value); }
        }

        public int NewLines_Braces_AnonymousMethod
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, value); }
        }

        public int NewLines_Braces_AnonymousTypeInitializer
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, value); }
        }

        public int NewLines_Braces_ControlFlow
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, value); }
        }

        public int NewLines_Braces_LambdaExpressionBody
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, value); }
        }

        public int NewLines_Braces_Method
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInMethods); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInMethods, value); }
        }

        public int NewLines_Braces_Property
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInProperties); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInProperties, value); }
        }

        public int NewLines_Braces_Accessor
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInAccessors); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInAccessors, value); }
        }

        public int NewLines_Braces_ObjectInitializer
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, value); }
        }

        public int NewLines_Braces_Type
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInTypes); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLinesForBracesInTypes, value); }
        }

        public int NewLines_Keywords_Catch
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLineForCatch); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLineForCatch, value); }
        }

        public int NewLines_Keywords_Else
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLineForElse); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLineForElse, value); }
        }

        public int NewLines_Keywords_Finally
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLineForFinally); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLineForFinally, value); }
        }

        public int NewLines_ObjectInitializer_EachMember
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLineForMembersInObjectInit); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLineForMembersInObjectInit, value); }
        }

        public int NewLines_QueryExpression_EachClause
        {
            get { return GetBooleanOption(CSharpFormattingOptions.NewLineForClausesInQuery); }
            set { SetBooleanOption(CSharpFormattingOptions.NewLineForClausesInQuery, value); }
        }

        public int Refactoring_Verification_Enabled
        {
            get { return GetBooleanOption(FeatureOnOffOptions.RefactoringVerification); }
            set { SetBooleanOption(FeatureOnOffOptions.RefactoringVerification, value); }
        }

        public int RenameSmartTagEnabled
        {
            get { return GetBooleanOption(FeatureOnOffOptions.RenameTracking); }
            set { SetBooleanOption(FeatureOnOffOptions.RenameTracking, value); }
        }

        public int RenameTrackingPreview
        {
            get { return GetBooleanOption(FeatureOnOffOptions.RenameTrackingPreview); }
            set { SetBooleanOption(FeatureOnOffOptions.RenameTrackingPreview, value); }
        }

        public int ShowKeywords
        {
            get { return GetBooleanOption(CompletionOptions.IncludeKeywords); }
            set { SetBooleanOption(CompletionOptions.IncludeKeywords, value); }
        }

        public int ShowSnippets
        {
            get { return GetBooleanOption(CSharpCompletionOptions.IncludeSnippets); }
            set { SetBooleanOption(CSharpCompletionOptions.IncludeSnippets, value); }
        }

        public int SortUsings_PlaceSystemFirst
        {
            get { return GetBooleanOption(OrganizerOptions.PlaceSystemNamespaceFirst); }
            set { SetBooleanOption(OrganizerOptions.PlaceSystemNamespaceFirst, value); }
        }

        public int Space_AfterBasesColon
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration, value); }
        }

        public int Space_AfterCast
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceAfterCast); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceAfterCast, value); }
        }

        public int Space_AfterComma
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceAfterComma); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceAfterComma, value); }
        }

        public int Space_AfterDot
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceAfterDot); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceAfterDot, value); }
        }

        public int Space_AfterMethodCallName
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceAfterMethodCallName); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceAfterMethodCallName, value); }
        }

        public int Space_AfterMethodDeclarationName
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpacingAfterMethodDeclarationName); }
            set { SetBooleanOption(CSharpFormattingOptions.SpacingAfterMethodDeclarationName, value); }
        }

        public int Space_AfterSemicolonsInForStatement
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement, value); }
        }

        public int Space_AroundBinaryOperator
        {
            get
            {
                var option = _optionService.GetOption(CSharpFormattingOptions.SpacingAroundBinaryOperator);
                return option == BinaryOperatorSpacingOptions.Single ? 1 : 0;
            }

            set
            {
                var option = value == 1 ? BinaryOperatorSpacingOptions.Single : BinaryOperatorSpacingOptions.Ignore;
                var optionSet = _optionService.GetOptions();
                optionSet = optionSet.WithChangedOption(CSharpFormattingOptions.SpacingAroundBinaryOperator, option);
                _optionService.SetOptions(optionSet);
            }
        }

        public int Space_BeforeBasesColon
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration, value); }
        }

        public int Space_BeforeComma
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceBeforeComma); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceBeforeComma, value); }
        }

        public int Space_BeforeDot
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceBeforeDot); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceBeforeDot, value); }
        }

        public int Space_BeforeOpenSquare
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceBeforeOpenSquareBracket); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceBeforeOpenSquareBracket, value); }
        }

        public int Space_BeforeSemicolonsInForStatement
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement, value); }
        }

        public int Space_BetweenEmptyMethodCallParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, value); }
        }

        public int Space_BetweenEmptyMethodDeclarationParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses, value); }
        }

        public int Space_BetweenEmptySquares
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets, value); }
        }

        public int Space_InControlFlowConstruct
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword, value); }
        }

        public int Space_WithinCastParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceWithinCastParentheses); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceWithinCastParentheses, value); }
        }

        public int Space_WithinExpressionParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceWithinExpressionParentheses); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceWithinExpressionParentheses, value); }
        }

        public int Space_WithinMethodCallParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceWithinMethodCallParentheses); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceWithinMethodCallParentheses, value); }
        }

        public int Space_WithinMethodDeclarationParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis, value); }
        }

        public int Space_WithinOtherParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceWithinOtherParentheses); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceWithinOtherParentheses, value); }
        }

        public int Space_WithinSquares
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpaceWithinSquareBrackets); }
            set { SetBooleanOption(CSharpFormattingOptions.SpaceWithinSquareBrackets, value); }
        }

        public int Style_PreferIntrinsicPredefinedTypeKeywordInDeclaration
        {
            get { return GetBooleanOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration); }
            set { SetBooleanOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, value); }
        }

        public int Style_PreferIntrinsicPredefinedTypeKeywordInMemberAccess
        {
            get { return GetBooleanOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess); }
            set { SetBooleanOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, value); }
        }

        public int Style_QualifyMemberAccessWithThisOrMe
        {
            get { return GetBooleanOption(SimplificationOptions.QualifyMemberAccessWithThisOrMe); }
            set { SetBooleanOption(SimplificationOptions.QualifyMemberAccessWithThisOrMe, value); }
        }

        public int Style_UseVarWhenDeclaringLocals
        {
            get { return GetBooleanOption(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals); }
            set { SetBooleanOption(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals, value); }
        }

        public int Style_UseVarWherePossible
        {
            get { return GetBooleanOption(CSharpCodeStyleOptions.UseVarWherePossible); }
            set { SetBooleanOption(CSharpCodeStyleOptions.UseVarWherePossible, value); }
        }

        public int Style_UseVarWhenTypeIsApparent
        {
            get { return GetBooleanOption(CSharpCodeStyleOptions.UseVarWhenTypeIsApparent); }
            set { SetBooleanOption(CSharpCodeStyleOptions.UseVarWhenTypeIsApparent, value); }
        }

        public int Style_UseVarForIntrinsicTypes
        {
            get { return GetBooleanOption(CSharpCodeStyleOptions.UseVarForIntrinsicTypes); }
            set { SetBooleanOption(CSharpCodeStyleOptions.UseVarForIntrinsicTypes, value); }
        }

        public int WarnOnBuildErrors
        {
            get { return GetBooleanOption(OrganizerOptions.WarnOnBuildErrors); }
            set { SetBooleanOption(OrganizerOptions.WarnOnBuildErrors, value); }
        }

        public int Wrapping_IgnoreSpacesAroundBinaryOperators
        {
            get
            {
                var option = _optionService.GetOption(CSharpFormattingOptions.SpacingAroundBinaryOperator);
                return (int)option;
            }

            set
            {
                var optionSet = _optionService.GetOptions();
                optionSet = optionSet.WithChangedOption(CSharpFormattingOptions.SpacingAroundBinaryOperator, value);
                _optionService.SetOptions(optionSet);
            }
        }

        public int Wrapping_IgnoreSpacesAroundVariableDeclaration
        {
            get { return GetBooleanOption(CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration); }
            set { SetBooleanOption(CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, value); }
        }

        public int Wrapping_KeepStatementsOnSingleLine
        {
            get { return GetBooleanOption(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine); }
            set { SetBooleanOption(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, value); }
        }

        public int Wrapping_PreserveSingleLine
        {
            get { return GetBooleanOption(CSharpFormattingOptions.WrappingPreserveSingleLine); }
            set { SetBooleanOption(CSharpFormattingOptions.WrappingPreserveSingleLine, value); }
        }

        private int GetBooleanOption(Option<bool> key)
        {
            return _optionService.GetOption(key) ? 1 : 0;
        }

        private int GetBooleanOption(PerLanguageOption<bool> key)
        {
            return _optionService.GetOption(key, LanguageNames.CSharp) ? 1 : 0;
        }

        private void SetBooleanOption(Option<bool> key, int value)
        {
            var optionSet = _optionService.GetOptions();
            optionSet = optionSet.WithChangedOption(key, value != 0);
            _optionService.SetOptions(optionSet);
        }

        private void SetBooleanOption(PerLanguageOption<bool> key, int value)
        {
            var optionSet = _optionService.GetOptions();
            optionSet = optionSet.WithChangedOption(key, LanguageNames.CSharp, value != 0);
            _optionService.SetOptions(optionSet);
        }
    }
}
