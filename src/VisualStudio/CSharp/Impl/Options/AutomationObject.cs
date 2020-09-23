// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => _workspace = workspace;

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
            get { return GetBooleanOption(CompletionOptions.TriggerOnTypingLetters2); }
            set { SetBooleanOption(CompletionOptions.TriggerOnTypingLetters2, value); }
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

        [Obsolete("ClosedFileDiagnostics has been deprecated")]
        public int ClosedFileDiagnostics
        {
            get { return 0; }
            set { }
        }

        [Obsolete("CSharpClosedFileDiagnostics has been deprecated")]
        public int CSharpClosedFileDiagnostics
        {
            get { return 0; }
            set { }
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
            get
            {
                var option = _workspace.Options.GetOption(CSharpFormattingOptions2.LabelPositioning);
                return option == LabelPositionOptions.LeftMost ? 1 : 0;
            }

            set
            {
                _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                    .WithChangedOption(CSharpFormattingOptions2.LabelPositioning, value == 1 ? LabelPositionOptions.LeftMost : LabelPositionOptions.NoIndent)));
            }
        }

        public int Indent_UnindentLabels
        {
            get
            {
                return (int)_workspace.Options.GetOption(CSharpFormattingOptions2.LabelPositioning);
            }

            set
            {
                _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                    .WithChangedOption(CSharpFormattingOptions2.LabelPositioning, value)));
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
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes, value); }
        }

        public int NewLines_Braces_AnonymousMethod
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods, value); }
        }

        public int NewLines_Braces_AnonymousTypeInitializer
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes, value); }
        }

        public int NewLines_Braces_ControlFlow
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInControlBlocks); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInControlBlocks, value); }
        }

        public int NewLines_Braces_LambdaExpressionBody
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody, value); }
        }

        public int NewLines_Braces_Method
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInMethods); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInMethods, value); }
        }

        public int NewLines_Braces_Property
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInProperties); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInProperties, value); }
        }

        public int NewLines_Braces_Accessor
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInAccessors); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInAccessors, value); }
        }

        public int NewLines_Braces_ObjectInitializer
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers, value); }
        }

        public int NewLines_Braces_Type
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInTypes); }
            set { SetBooleanOption(CSharpFormattingOptions2.NewLinesForBracesInTypes, value); }
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
            get
            {
                var option = _workspace.Options.GetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator);
                return option == BinaryOperatorSpacingOptions.Single ? 1 : 0;
            }

            set
            {
                var option = value == 1 ? BinaryOperatorSpacingOptions.Single : BinaryOperatorSpacingOptions.Ignore;
                _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                    .WithChangedOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator, option)));
            }
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
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceWithinCastParentheses); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceWithinCastParentheses, value); }
        }

        public int Space_WithinExpressionParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceWithinExpressionParentheses); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceWithinExpressionParentheses, value); }
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

        public int Space_WithinOtherParentheses
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceWithinOtherParentheses); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceWithinOtherParentheses, value); }
        }

        public int Space_WithinSquares
        {
            get { return GetBooleanOption(CSharpFormattingOptions2.SpaceWithinSquareBrackets); }
            set { SetBooleanOption(CSharpFormattingOptions2.SpaceWithinSquareBrackets, value); }
        }

        public string Style_PreferIntrinsicPredefinedTypeKeywordInDeclaration_CodeStyle
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration); }
            set { SetXmlOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, value); }
        }

        public string Style_PreferIntrinsicPredefinedTypeKeywordInMemberAccess_CodeStyle
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess); }
            set { SetXmlOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, value); }
        }

        public string Style_NamingPreferences
        {
            get
            {
                return _workspace.Options.GetOption(NamingStyleOptions.NamingPreferences, LanguageNames.CSharp).CreateXElement().ToString();
            }

            set
            {
                try
                {
                    _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                        .WithChangedOption(NamingStyleOptions.NamingPreferences, LanguageNames.CSharp, NamingStylePreferences.FromXElement(XElement.Parse(value)))));
                }
                catch (Exception)
                {
                }
            }
        }

        public string Style_QualifyFieldAccess
        {
            get { return GetXmlOption(CodeStyleOptions2.QualifyFieldAccess); }
            set { SetXmlOption(CodeStyleOptions2.QualifyFieldAccess, value); }
        }

        public string Style_QualifyPropertyAccess
        {
            get { return GetXmlOption(CodeStyleOptions2.QualifyPropertyAccess); }
            set { SetXmlOption(CodeStyleOptions2.QualifyPropertyAccess, value); }
        }

        public string Style_QualifyMethodAccess
        {
            get { return GetXmlOption(CodeStyleOptions2.QualifyMethodAccess); }
            set { SetXmlOption(CodeStyleOptions2.QualifyMethodAccess, value); }
        }

        public string Style_QualifyEventAccess
        {
            get { return GetXmlOption(CodeStyleOptions2.QualifyEventAccess); }
            set { SetXmlOption(CodeStyleOptions2.QualifyEventAccess, value); }
        }

        public string Style_PreferThrowExpression
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferThrowExpression); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferThrowExpression, value); }
        }

        public string Style_PreferObjectInitializer
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferObjectInitializer); }
            set { SetXmlOption(CodeStyleOptions2.PreferObjectInitializer, value); }
        }

        public string Style_PreferCollectionInitializer
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferCollectionInitializer); }
            set { SetXmlOption(CodeStyleOptions2.PreferCollectionInitializer, value); }
        }

        public string Style_PreferCoalesceExpression
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferCoalesceExpression); }
            set { SetXmlOption(CodeStyleOptions2.PreferCoalesceExpression, value); }
        }

        public string Style_PreferNullPropagation
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferNullPropagation); }
            set { SetXmlOption(CodeStyleOptions2.PreferNullPropagation, value); }
        }

        public string Style_PreferInlinedVariableDeclaration
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration, value); }
        }

        public string Style_PreferExplicitTupleNames
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferExplicitTupleNames); }
            set { SetXmlOption(CodeStyleOptions2.PreferExplicitTupleNames, value); }
        }

        public string Style_PreferInferredTupleNames
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferInferredTupleNames); }
            set { SetXmlOption(CodeStyleOptions2.PreferInferredTupleNames, value); }
        }

        public string Style_PreferInferredAnonymousTypeMemberNames
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames); }
            set { SetXmlOption(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, value); }
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

        public string Style_PreferPatternMatching
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferPatternMatching); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferPatternMatching, value); }
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
            get { return GetXmlOption(CodeStyleOptions2.PreferReadonly); }
            set { SetXmlOption(CodeStyleOptions2.PreferReadonly, value); }
        }

        public int Wrapping_IgnoreSpacesAroundBinaryOperators
        {
            get
            {
                return (int)_workspace.Options.GetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator);
            }

            set
            {
                _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                    .WithChangedOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator, value)));
            }
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

        private int GetBooleanOption(Option2<bool> key)
            => _workspace.Options.GetOption(key) ? 1 : 0;

        private int GetBooleanOption(PerLanguageOption2<bool> key)
            => _workspace.Options.GetOption(key, LanguageNames.CSharp) ? 1 : 0;

        private T GetOption<T>(PerLanguageOption2<T> key)
            => _workspace.Options.GetOption(key, LanguageNames.CSharp);

        private void SetBooleanOption(Option2<bool> key, int value)
        {
            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(key, value != 0)));
        }

        private void SetBooleanOption(PerLanguageOption2<bool> key, int value)
        {
            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(key, LanguageNames.CSharp, value != 0)));
        }

        private void SetOption<T>(PerLanguageOption2<T> key, T value)
        {
            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(key, LanguageNames.CSharp, value)));
        }

        private int GetBooleanOption(PerLanguageOption2<bool?> key)
        {
            var option = _workspace.Options.GetOption(key, LanguageNames.CSharp);
            if (!option.HasValue)
            {
                return -1;
            }

            return option.Value ? 1 : 0;
        }

        private string GetXmlOption<T>(Option2<CodeStyleOption2<T>> option)
            => _workspace.Options.GetOption(option).ToXElement().ToString();

        private void SetBooleanOption(PerLanguageOption2<bool?> key, int value)
        {
            var boolValue = (value < 0) ? (bool?)null : (value > 0);
            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(key, LanguageNames.CSharp, boolValue)));
        }

        private string GetXmlOption(PerLanguageOption2<CodeStyleOption2<bool>> option)
            => _workspace.Options.GetOption(option, LanguageNames.CSharp).ToXElement().ToString();

        private void SetXmlOption<T>(Option2<CodeStyleOption2<T>> option, string value)
        {
            var convertedValue = CodeStyleOption2<T>.FromXElement(XElement.Parse(value));
            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(option, convertedValue)));
        }

        private void SetXmlOption(PerLanguageOption2<CodeStyleOption2<bool>> option, string value)
        {
            var convertedValue = CodeStyleOption2<bool>.FromXElement(XElement.Parse(value));
            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options
                .WithChangedOption(option, LanguageNames.CSharp, convertedValue)));
        }
    }
}
