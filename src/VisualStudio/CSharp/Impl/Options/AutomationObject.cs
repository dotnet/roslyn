// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [ComVisible(true)]
    public class AutomationObject
    {
        private readonly Workspace _workspace;

        internal AutomationObject(Workspace workspace)
        {
            _workspace = workspace;
        }

        /// <summary>
        /// Unused.  But kept around for back compat.  Note this option is not about
        /// turning warning into errors.  It's about an aspect of 'remove unused using'
        /// functionality we don't support anymore.  Namely whether or not 'remove unused
        /// using' should warn if you have any build errors as that might mean we 
        /// remove some usings inappropriately.
        /// </summary>
        public int WarnOnBuildErrors
        {
            get { return 0; }
            set { }
        }

        public int AutoComment
        {
            get { return GetBooleanOption(FeatureOnOffOptions.AutoXmlDocCommentGeneration); }
            set { SetBooleanOption(FeatureOnOffOptions.AutoXmlDocCommentGeneration, value); }
        }

        public int AutoInsertAsteriskForNewLinesOfBlockComments
        {
            get { return GetBooleanOption(FeatureOnOffOptions.AutoInsertBlockCommentStartString); }
            set { SetBooleanOption(FeatureOnOffOptions.AutoInsertBlockCommentStartString, value); }
        }

        public int BringUpOnIdentifier
        {
            get { return GetBooleanOption(CompletionOptions.TriggerOnTypingLetters); }
            set { SetBooleanOption(CompletionOptions.TriggerOnTypingLetters, value); }
        }

        public int HighlightMatchingPortionsOfCompletionListItems
        {
            get { return GetBooleanOption(CompletionOptions.HighlightMatchingPortionsOfCompletionListItems); }
            set { SetBooleanOption(CompletionOptions.HighlightMatchingPortionsOfCompletionListItems, value); }
        }

        public int ShowCompletionItemFilters
        {
            get { return GetBooleanOption(CompletionOptions.ShowCompletionItemFilters); }
            set { SetBooleanOption(CompletionOptions.ShowCompletionItemFilters, value); }
        }

        public int ShowItemsFromUnimportedNamespaces
        {
            get { return GetBooleanOption(CompletionOptions.ShowItemsFromUnimportedNamespaces); }
            set { SetBooleanOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, value); }
        }

        [Obsolete("This SettingStore option has now been deprecated in favor of CSharpClosedFileDiagnostics")]
        public int ClosedFileDiagnostics
        {
            get { return GetBooleanOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic); }
            set
            {
                // Even though this option has been deprecated, we want to respect the setting if the user has explicitly turned off closed file diagnostics (which is the non-default value for 'ClosedFileDiagnostics').
                // So, we invoke the setter only for value = 0.
                if (value == 0)
                {
                    SetBooleanOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, value);
                }
            }
        }

        public int CSharpClosedFileDiagnostics
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

        public int Indent_CaseContentsWhenBlock
        {
            get { return GetBooleanOption(CSharpFormattingOptions.IndentSwitchCaseSectionWhenBlock); }
            set { SetBooleanOption(CSharpFormattingOptions.IndentSwitchCaseSectionWhenBlock, value); }
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
                var option = _workspace.Options.GetOption(CSharpFormattingOptions.LabelPositioning);
                return option == LabelPositionOptions.LeftMost ? 1 : 0;
            }

            set
            {
                _workspace.Options = _workspace.Options.WithChangedOption(CSharpFormattingOptions.LabelPositioning, value == 1 ? LabelPositionOptions.LeftMost : LabelPositionOptions.NoIndent);
            }
        }

        public int Indent_NamespaceContents
        {
            get { return GetBooleanOption(CSharpFormattingOptions.IndentNamespace); }
            set { SetBooleanOption(CSharpFormattingOptions.IndentNamespace, value); }
        }

        public int Indent_UnindentLabels
        {
            get
            {
                return (int)_workspace.Options.GetOption(CSharpFormattingOptions.LabelPositioning);
            }

            set
            {
                _workspace.Options = _workspace.Options.WithChangedOption(CSharpFormattingOptions.LabelPositioning, value);
            }
        }

        public int InsertNewlineOnEnterWithWholeWord
        {
            get { return (int)GetOption(CompletionOptions.EnterKeyBehavior); }
            set { SetOption(CompletionOptions.EnterKeyBehavior, (EnterKeyRule)value); }
        }

        public int EnterKeyBehavior
        {
            get { return (int)GetOption(CompletionOptions.EnterKeyBehavior); }
            set { SetOption(CompletionOptions.EnterKeyBehavior, (EnterKeyRule)value); }
        }

        public int SnippetsBehavior
        {
            get { return (int)GetOption(CompletionOptions.SnippetsBehavior); }
            set { SetOption(CompletionOptions.SnippetsBehavior, (SnippetsRule)value); }
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
            get { return 0; }
            set { }
        }

        [Obsolete("Use SnippetsBehavior instead")]
        public int ShowSnippets
        {
            get
            {
                return GetOption(CompletionOptions.SnippetsBehavior) == SnippetsRule.AlwaysInclude
                    ? 1 : 0;
            }

            set
            {
                if (value == 0)
                {
                    SetOption(CompletionOptions.SnippetsBehavior, SnippetsRule.NeverInclude);
                }
                else
                {
                    SetOption(CompletionOptions.SnippetsBehavior, SnippetsRule.AlwaysInclude);
                }
            }
        }

        public int SortUsings_PlaceSystemFirst
        {
            get { return GetBooleanOption(GenerationOptions.PlaceSystemNamespaceFirst); }
            set { SetBooleanOption(GenerationOptions.PlaceSystemNamespaceFirst, value); }
        }

        public int AddImport_SuggestForTypesInReferenceAssemblies
        {
            get { return GetBooleanOption(SymbolSearchOptions.SuggestForTypesInReferenceAssemblies); }
            set { SetBooleanOption(SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, value); }
        }

        public int AddImport_SuggestForTypesInNuGetPackages
        {
            get { return GetBooleanOption(SymbolSearchOptions.SuggestForTypesInNuGetPackages); }
            set { SetBooleanOption(SymbolSearchOptions.SuggestForTypesInNuGetPackages, value); }
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
                var option = _workspace.Options.GetOption(CSharpFormattingOptions.SpacingAroundBinaryOperator);
                return option == BinaryOperatorSpacingOptions.Single ? 1 : 0;
            }

            set
            {
                var option = value == 1 ? BinaryOperatorSpacingOptions.Single : BinaryOperatorSpacingOptions.Ignore;
                _workspace.Options = _workspace.Options.WithChangedOption(CSharpFormattingOptions.SpacingAroundBinaryOperator, option);
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

        public string Style_PreferIntrinsicPredefinedTypeKeywordInDeclaration_CodeStyle
        {
            get { return GetXmlOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration); }
            set { SetXmlOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, value); }
        }

        public string Style_PreferIntrinsicPredefinedTypeKeywordInMemberAccess_CodeStyle
        {
            get { return GetXmlOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess); }
            set { SetXmlOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, value); }
        }

        public string Style_NamingPreferences
        {
            get
            {
                return _workspace.Options.GetOption(SimplificationOptions.NamingPreferences, LanguageNames.CSharp).CreateXElement().ToString();
            }

            set
            {
                try
                {
                    _workspace.Options = _workspace.Options.WithChangedOption(SimplificationOptions.NamingPreferences, LanguageNames.CSharp, NamingStylePreferences.FromXElement(XElement.Parse(value)));
                }
                catch (Exception)
                {
                }
            }
        }

        public string Style_QualifyFieldAccess
        {
            get { return GetXmlOption(CodeStyleOptions.QualifyFieldAccess); }
            set { SetXmlOption(CodeStyleOptions.QualifyFieldAccess, value); }
        }

        public string Style_QualifyPropertyAccess
        {
            get { return GetXmlOption(CodeStyleOptions.QualifyPropertyAccess); }
            set { SetXmlOption(CodeStyleOptions.QualifyPropertyAccess, value); }
        }

        public string Style_QualifyMethodAccess
        {
            get { return GetXmlOption(CodeStyleOptions.QualifyMethodAccess); }
            set { SetXmlOption(CodeStyleOptions.QualifyMethodAccess, value); }
        }

        public string Style_QualifyEventAccess
        {
            get { return GetXmlOption(CodeStyleOptions.QualifyEventAccess); }
            set { SetXmlOption(CodeStyleOptions.QualifyEventAccess, value); }
        }

        public string Style_PreferThrowExpression
        {
            get { return GetXmlOption(CodeStyleOptions.PreferThrowExpression); }
            set { SetXmlOption(CodeStyleOptions.PreferThrowExpression, value); }
        }

        public string Style_PreferObjectInitializer
        {
            get { return GetXmlOption(CodeStyleOptions.PreferObjectInitializer); }
            set { SetXmlOption(CodeStyleOptions.PreferObjectInitializer, value); }
        }

        public string Style_PreferCollectionInitializer
        {
            get { return GetXmlOption(CodeStyleOptions.PreferCollectionInitializer); }
            set { SetXmlOption(CodeStyleOptions.PreferCollectionInitializer, value); }
        }

        public string Style_PreferCoalesceExpression
        {
            get { return GetXmlOption(CodeStyleOptions.PreferCoalesceExpression); }
            set { SetXmlOption(CodeStyleOptions.PreferCoalesceExpression, value); }
        }

        public string Style_PreferNullPropagation
        {
            get { return GetXmlOption(CodeStyleOptions.PreferNullPropagation); }
            set { SetXmlOption(CodeStyleOptions.PreferNullPropagation, value); }
        }

        public string Style_PreferInlinedVariableDeclaration
        {
            get { return GetXmlOption(CodeStyleOptions.PreferInlinedVariableDeclaration); }
            set { SetXmlOption(CodeStyleOptions.PreferInlinedVariableDeclaration, value); }
        }

        public string Style_PreferExplicitTupleNames
        {
            get { return GetXmlOption(CodeStyleOptions.PreferExplicitTupleNames); }
            set { SetXmlOption(CodeStyleOptions.PreferExplicitTupleNames, value); }
        }

        public string Style_PreferInferredTupleNames
        {
            get { return GetXmlOption(CodeStyleOptions.PreferInferredTupleNames); }
            set { SetXmlOption(CodeStyleOptions.PreferInferredTupleNames, value); }
        }

        public string Style_PreferInferredAnonymousTypeMemberNames
        {
            get { return GetXmlOption(CodeStyleOptions.PreferInferredAnonymousTypeMemberNames); }
            set { SetXmlOption(CodeStyleOptions.PreferInferredAnonymousTypeMemberNames, value); }
        }

        [Obsolete("Use Style_UseImplicitTypeWherePossible, Style_UseImplicitTypeWhereApparent or Style_UseImplicitTypeForIntrinsicTypes", error: true)]
        public int Style_UseVarWhenDeclaringLocals
        {
            get { return 0; }
            set { }
        }

        public string Style_UseImplicitTypeWherePossible
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.VarElsewhere); }
            set { SetXmlOption(CSharpCodeStyleOptions.VarElsewhere, value); }
        }

        public string Style_UseImplicitTypeWhereApparent
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent); }
            set { SetXmlOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, value); }
        }

        public string Style_UseImplicitTypeForIntrinsicTypes
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.VarForBuiltInTypes); }
            set { SetXmlOption(CSharpCodeStyleOptions.VarForBuiltInTypes, value); }
        }

        public string Style_PreferConditionalDelegateCall
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferConditionalDelegateCall); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferConditionalDelegateCall, value); }
        }

        public string Style_PreferSwitchExpression
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferSwitchExpression); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferSwitchExpression, value); }
        }

        public string Style_PreferPatternMatchingOverAsWithNullCheck
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, value); }
        }

        public string Style_PreferPatternMatchingOverIsWithCastCheck
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, value); }
        }

        public string Style_PreferExpressionBodiedConstructors
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, value); }
        }

        public string Style_PreferExpressionBodiedMethods
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, value); }
        }

        public string Style_PreferExpressionBodiedOperators
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, value); }
        }

        public string Style_PreferExpressionBodiedProperties
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, value); }
        }

        public string Style_PreferExpressionBodiedIndexers
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, value); }
        }

        public string Style_PreferExpressionBodiedAccessors
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, value); }
        }

        public string Style_PreferBraces
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferBraces); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferBraces, value); }
        }

        public string Style_PreferReadonly
        {
            get { return GetXmlOption(CodeStyleOptions.PreferReadonly); }
            set { SetXmlOption(CodeStyleOptions.PreferReadonly, value); }
        }

        public int Wrapping_IgnoreSpacesAroundBinaryOperators
        {
            get
            {
                return (int)_workspace.Options.GetOption(CSharpFormattingOptions.SpacingAroundBinaryOperator);
            }

            set
            {
                _workspace.Options = _workspace.Options.WithChangedOption(CSharpFormattingOptions.SpacingAroundBinaryOperator, value);
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
            return _workspace.Options.GetOption(key) ? 1 : 0;
        }

        private int GetBooleanOption(PerLanguageOption<bool> key)
        {
            return _workspace.Options.GetOption(key, LanguageNames.CSharp) ? 1 : 0;
        }

        private T GetOption<T>(PerLanguageOption<T> key)
        {
            return _workspace.Options.GetOption(key, LanguageNames.CSharp);
        }

        private void SetBooleanOption(Option<bool> key, int value)
        {
            _workspace.Options = _workspace.Options.WithChangedOption(key, value != 0);
        }

        private void SetBooleanOption(PerLanguageOption<bool> key, int value)
        {
            _workspace.Options = _workspace.Options.WithChangedOption(key, LanguageNames.CSharp, value != 0);
        }

        private void SetOption<T>(PerLanguageOption<T> key, T value)
        {
            _workspace.Options = _workspace.Options.WithChangedOption(key, LanguageNames.CSharp, value);
        }

        private int GetBooleanOption(PerLanguageOption<bool?> key)
        {
            var option = _workspace.Options.GetOption(key, LanguageNames.CSharp);
            if (!option.HasValue)
            {
                return -1;
            }

            return option.Value ? 1 : 0;
        }

        private string GetXmlOption<T>(Option<CodeStyleOption<T>> option)
        {
            return _workspace.Options.GetOption(option).ToXElement().ToString();
        }

        private void SetBooleanOption(PerLanguageOption<bool?> key, int value)
        {
            var boolValue = (value < 0) ? (bool?)null : (value > 0);
            _workspace.Options = _workspace.Options.WithChangedOption(key, LanguageNames.CSharp, boolValue);
        }

        private string GetXmlOption(PerLanguageOption<CodeStyleOption<bool>> option)
        {
            return _workspace.Options.GetOption(option, LanguageNames.CSharp).ToXElement().ToString();
        }

        private void SetXmlOption<T>(Option<CodeStyleOption<T>> option, string value)
        {
            var convertedValue = CodeStyleOption<T>.FromXElement(XElement.Parse(value));
            _workspace.Options = _workspace.Options.WithChangedOption(option, convertedValue);
        }

        private void SetXmlOption(PerLanguageOption<CodeStyleOption<bool>> option, string value)
        {
            var convertedValue = CodeStyleOption<bool>.FromXElement(XElement.Parse(value));
            _workspace.Options = _workspace.Options.WithChangedOption(option, LanguageNames.CSharp, convertedValue);
        }
    }
}
