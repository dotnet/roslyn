// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Completion;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    [ExportLanguageSpecificOptionSerializer(
        LanguageNames.CSharp,
        OrganizerOptions.FeatureName,
        CompletionOptions.FeatureName,
        CSharpCompletionOptions.FeatureName,
        CSharpCodeStyleOptions.FeatureName,
        SimplificationOptions.PerLanguageFeatureName,
        ExtractMethodOptions.FeatureName,
        CSharpFormattingOptions.IndentFeatureName,
        CSharpFormattingOptions.NewLineFormattingFeatureName,
        CSharpFormattingOptions.SpacingFeatureName,
        CSharpFormattingOptions.WrappingFeatureName,
        FormattingOptions.InternalTabFeatureName,
        FeatureOnOffOptions.OptionName,
        ServiceFeatureOnOffOptions.OptionName), Shared]
    internal sealed class CSharpSettingStoreOptionSerializer : AbstractSettingStoreOptionSerializer
    {
        [ImportingConstructor]
        public CSharpSettingStoreOptionSerializer(SVsServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        private const string FormattingPath = @"CSharp\Options\Formatting";
        private const string WrappingIgnoreSpacesAroundBinaryOperator = nameof(AutomationObject.Wrapping_IgnoreSpacesAroundBinaryOperators);
        private const string SpaceAroundBinaryOperator = nameof(AutomationObject.Space_AroundBinaryOperator);
        private const string UnindentLabels = nameof(AutomationObject.Indent_UnindentLabels);
        private const string FlushLabelsLeft = nameof(AutomationObject.Indent_FlushLabelsLeft);
        private const string CSharpRoot = @"CSharp\Options\Roslyn";

        private readonly Dictionary<IOption, string> _formattingOptionTable = new Dictionary<IOption, string>
            {
                { CSharpFormattingOptions.IndentBraces, nameof(AutomationObject.Indent_Braces) },
                { CSharpFormattingOptions.IndentBlock, nameof(AutomationObject.Indent_BlockContents) },
                { CSharpFormattingOptions.IndentSwitchSection, nameof(AutomationObject.Indent_CaseLabels) },
                { CSharpFormattingOptions.IndentSwitchCaseSection, nameof(AutomationObject.Indent_CaseContents) },
                { CSharpFormattingOptions.NewLinesForBracesInTypes, nameof(AutomationObject.NewLines_Braces_Type) },
                { CSharpFormattingOptions.NewLinesForBracesInMethods, nameof(AutomationObject.NewLines_Braces_Method) },
                { CSharpFormattingOptions.NewLinesForBracesInControlBlocks, nameof(AutomationObject.NewLines_Braces_ControlFlow) },
                { CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, nameof(AutomationObject.NewLines_Braces_AnonymousMethod) },
                { CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, nameof(AutomationObject.NewLines_Braces_AnonymousTypeInitializer) },
                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, nameof(AutomationObject.NewLines_Braces_ObjectInitializer) },
                { CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, nameof(AutomationObject.NewLines_Braces_LambdaExpressionBody) },
                { CSharpFormattingOptions.NewLineForElse, nameof(AutomationObject.NewLines_Keywords_Else) },
                { CSharpFormattingOptions.NewLineForCatch, nameof(AutomationObject.NewLines_Keywords_Catch) },
                { CSharpFormattingOptions.NewLineForFinally, nameof(AutomationObject.NewLines_Keywords_Finally) },
                { CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, nameof(AutomationObject.NewLines_AnonymousTypeInitializer_EachMember) },
                { CSharpFormattingOptions.NewLineForMembersInObjectInit, nameof(AutomationObject.NewLines_ObjectInitializer_EachMember) },
                { CSharpFormattingOptions.NewLineForClausesInQuery, nameof(AutomationObject.NewLines_QueryExpression_EachClause) },
                { CSharpFormattingOptions.WrappingPreserveSingleLine, nameof(AutomationObject.Wrapping_PreserveSingleLine) },
                { CSharpFormattingOptions.SpacingAfterMethodDeclarationName, nameof(AutomationObject.Space_AfterMethodDeclarationName) },
                { CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis, nameof(AutomationObject.Space_WithinMethodDeclarationParentheses) },
                { CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses, nameof(AutomationObject.Space_BetweenEmptyMethodDeclarationParentheses) },
                { CSharpFormattingOptions.SpaceAfterMethodCallName, nameof(AutomationObject.Space_AfterMethodCallName) },
                { CSharpFormattingOptions.SpaceWithinMethodCallParentheses, nameof(AutomationObject.Space_WithinMethodCallParentheses) },
                { CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, nameof(AutomationObject.Space_BetweenEmptyMethodCallParentheses) },
                { CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword, nameof(AutomationObject.Space_InControlFlowConstruct) },
                { CSharpFormattingOptions.SpaceWithinExpressionParentheses, nameof(AutomationObject.Space_WithinExpressionParentheses) },
                { CSharpFormattingOptions.SpaceWithinCastParentheses, nameof(AutomationObject.Space_WithinCastParentheses) },
                { CSharpFormattingOptions.SpaceWithinOtherParentheses, nameof(AutomationObject.Space_WithinOtherParentheses) },
                { CSharpFormattingOptions.SpaceAfterCast, nameof(AutomationObject.Space_AfterCast) },
                { CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, nameof(AutomationObject.Wrapping_IgnoreSpacesAroundVariableDeclaration) },
                { CSharpFormattingOptions.SpaceBeforeOpenSquareBracket, nameof(AutomationObject.Space_BeforeOpenSquare) },
                { CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets, nameof(AutomationObject.Space_BetweenEmptySquares) },
                { CSharpFormattingOptions.SpaceWithinSquareBrackets, nameof(AutomationObject.Space_WithinSquares) },
                { CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration, nameof(AutomationObject.Space_AfterBasesColon) },
                { CSharpFormattingOptions.SpaceAfterComma, nameof(AutomationObject.Space_AfterComma) },
                { CSharpFormattingOptions.SpaceAfterDot, nameof(AutomationObject.Space_AfterDot) },
                { CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement, nameof(AutomationObject.Space_AfterSemicolonsInForStatement) },
                { CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration, nameof(AutomationObject.Space_BeforeBasesColon) },
                { CSharpFormattingOptions.SpaceBeforeComma, nameof(AutomationObject.Space_BeforeComma) },
                { CSharpFormattingOptions.SpaceBeforeDot, nameof(AutomationObject.Space_BeforeDot) },
                { CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement, nameof(AutomationObject.Space_BeforeSemicolonsInForStatement) },
                { CSharpFormattingOptions.SpacingAroundBinaryOperator,  nameof(AutomationObject.Space_AroundBinaryOperator) },
                { CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, nameof(AutomationObject.Wrapping_KeepStatementsOnSingleLine) },
            };

        protected override Tuple<string, string> GetCollectionPathAndPropertyNameForOption(IOption option, string languageName)
        {
            if (option == OrganizerOptions.PlaceSystemNamespaceFirst)
            {
                return Tuple.Create(@"CSharp\Options\Organization", nameof(AutomationObject.SortUsings_PlaceSystemFirst));
            }
            else if (option == OrganizerOptions.WarnOnBuildErrors)
            {
                return Tuple.Create(@"CSharp\Options\Organization", nameof(AutomationObject.WarnOnBuildErrors));
            }
            else if (option == CSharpCompletionOptions.AddNewLineOnEnterAfterFullyTypedWord)
            {
                return Tuple.Create(@"CSharp\Options\CompletionLists", nameof(AutomationObject.InsertNewlineOnEnterWithWholeWord));
            }
            else if (option == CSharpCompletionOptions.IncludeSnippets)
            {
                return Tuple.Create(@"CSharp\Options\CompletionLists", nameof(AutomationObject.ShowSnippets));
            }
            else if (option == CompletionOptions.IncludeKeywords && languageName == LanguageNames.CSharp)
            {
                return Tuple.Create(@"CSharp\Options\CompletionLists", nameof(AutomationObject.ShowKeywords));
            }
            else if (option == CompletionOptions.TriggerOnTypingLetters && languageName == LanguageNames.CSharp)
            {
                return Tuple.Create(@"CSharp\Options\CompletionLists", nameof(AutomationObject.BringUpOnIdentifier));
            }
            else if (option.Feature == SimplificationOptions.PerLanguageFeatureName && languageName == LanguageNames.CSharp)
            {
                return Tuple.Create(@"CSharp\Options\Simplification", option.Name);
            }
            else if (option.Feature == CSharpCodeStyleOptions.FeatureName)
            {
                return Tuple.Create(@"CSharp\Options\Simplification", option.Name);
            }
            else if (option.Feature == ExtractMethodOptions.FeatureName && languageName == LanguageNames.CSharp)
            {
                return Tuple.Create(@"CSharp\Options\ExtractMethod", option.Name);
            }
            else if (option.Feature == ServiceFeatureOnOffOptions.OptionName && languageName == LanguageNames.CSharp)
            {
                return Tuple.Create(@"CSharp\Options\Diagnostics", option.Name);
            }
            else if (option.Feature == FeatureOnOffOptions.OptionName && languageName == LanguageNames.CSharp)
            {
                return GetCollectionPathAndPropertyNameForCSharpOption(option);
            }
            else if (option.Feature == FormattingOptions.InternalTabFeatureName && languageName == LanguageNames.CSharp)
            {
                return Tuple.Create(@"Roslyn\Internal\Formatting", option.Name);
            }

            if (option.Feature == CSharpFormattingOptions.WrappingFeatureName ||
                option.Feature == CSharpFormattingOptions.IndentFeatureName ||
                option.Feature == CSharpFormattingOptions.SpacingFeatureName ||
                option.Feature == CSharpFormattingOptions.NewLineFormattingFeatureName)
            {
                string propertyName;
                if (_formattingOptionTable.TryGetValue(option, out propertyName))
                {
                    return Tuple.Create(FormattingPath, propertyName);
                }
            }

            return null;
        }

        private Tuple<string, string> GetCollectionPathAndPropertyNameForCSharpOption(IOption key)
        {
            if (key == FeatureOnOffOptions.AutoFormattingOnCloseBrace)
            {
                return Tuple.Create(CSharpRoot, "AutoFormatting On CloseBrace");
            }
            else if (key == FeatureOnOffOptions.AutoFormattingOnSemicolon)
            {
                return Tuple.Create(CSharpRoot, "AutoFormatting On Semicolon");
            }
            else if (key == FeatureOnOffOptions.LineSeparator)
            {
                return Tuple.Create(CSharpRoot, "DisplayLineSeparators");
            }
            else if (key == FeatureOnOffOptions.Outlining)
            {
                return Tuple.Create(CSharpRoot, "Outlining");
            }
            else if (key == FeatureOnOffOptions.ReferenceHighlighting)
            {
                return Tuple.Create(CSharpRoot, "Highlight Reference");
            }
            else if (key == FeatureOnOffOptions.KeywordHighlighting)
            {
                return Tuple.Create(CSharpRoot, "Keyword Highlighting");
            }
            else if (key == FeatureOnOffOptions.FormatOnPaste)
            {
                return Tuple.Create(CSharpRoot, "Format On Paste");
            }
            else if (key == FeatureOnOffOptions.AutoXmlDocCommentGeneration)
            {
                return Tuple.Create(CSharpRoot, "AutoComment");
            }
            else if (key == FeatureOnOffOptions.RefactoringVerification)
            {
                return Tuple.Create(CSharpRoot, "RefactoringVerification");
            }
            else if (key == FeatureOnOffOptions.RenameTracking)
            {
                return Tuple.Create(CSharpRoot, "RenameTracking");
            }

            // Unsupported feature
            return null;
        }

        public override bool TryFetch(OptionKey optionKey, out object value)
        {
            lock (Gate)
            {
                using (var formattingPathSubKey = this.RegistryKey.OpenSubKey(FormattingPath))
                {
                    if (formattingPathSubKey != null)
                    {
                        if (optionKey.Option == CSharpFormattingOptions.SpacingAroundBinaryOperator)
                        {
                            // Remove space -> Space_AroundBinaryOperator = 0
                            // Insert space -> Space_AroundBinaryOperator and Wrapping_IgnoreSpacesAroundBinaryOperator both missing
                            // Ignore spacing -> Wrapping_IgnoreSpacesAroundBinaryOperator = 1

                            object ignoreSpacesAroundBinaryObjectValue = formattingPathSubKey.GetValue(WrappingIgnoreSpacesAroundBinaryOperator, defaultValue: 0);
                            if (ignoreSpacesAroundBinaryObjectValue.Equals(1))
                            {
                                value = BinaryOperatorSpacingOptions.Ignore;
                                return true;
                            }

                            object spaceAroundBinaryOperatorObjectValue = formattingPathSubKey.GetValue(SpaceAroundBinaryOperator, defaultValue: 1);
                            if (spaceAroundBinaryOperatorObjectValue.Equals(0))
                            {
                                value = BinaryOperatorSpacingOptions.Remove;
                                return true;
                            }

                            value = BinaryOperatorSpacingOptions.Single;
                            return true;
                        }

                        if (optionKey.Option == CSharpFormattingOptions.LabelPositioning)
                        {
                            object flushLabelLeftObjectValue = formattingPathSubKey.GetValue(FlushLabelsLeft, defaultValue: 0);
                            if (flushLabelLeftObjectValue.Equals(1))
                            {
                                value = LabelPositionOptions.LeftMost;
                                return true;
                            }

                            object unindentLabelsObjectValue = formattingPathSubKey.GetValue(UnindentLabels, defaultValue: 1);
                            if (unindentLabelsObjectValue.Equals(0))
                            {
                                value = LabelPositionOptions.NoIndent;
                                return true;
                            }

                            value = LabelPositionOptions.OneLess;
                            return true;
                        }
                    }
                }
            }

            return base.TryFetch(optionKey, out value);
        }

        public override bool TryPersist(OptionKey optionKey, object value)
        {
            lock (Gate)
            {
                using (var formattingPathSubKey = this.RegistryKey.CreateSubKey(FormattingPath))
                {
                    if (optionKey.Option == CSharpFormattingOptions.SpacingAroundBinaryOperator)
                    {
                        // Remove space -> Space_AroundBinaryOperator = 0
                        // Insert space -> Space_AroundBinaryOperator and Wrapping_IgnoreSpacesAroundBinaryOperator both missing
                        // Ignore spacing -> Wrapping_IgnoreSpacesAroundBinaryOperator = 1

                        switch ((BinaryOperatorSpacingOptions)value)
                        {
                            case BinaryOperatorSpacingOptions.Remove:
                                {
                                    formattingPathSubKey.DeleteValue(WrappingIgnoreSpacesAroundBinaryOperator, throwOnMissingValue: false);
                                    formattingPathSubKey.SetValue(SpaceAroundBinaryOperator, 0, RegistryValueKind.DWord);
                                    return true;
                                }

                            case BinaryOperatorSpacingOptions.Ignore:
                                {
                                    formattingPathSubKey.DeleteValue(SpaceAroundBinaryOperator, throwOnMissingValue: false);
                                    formattingPathSubKey.SetValue(WrappingIgnoreSpacesAroundBinaryOperator, 1, RegistryValueKind.DWord);
                                    return true;
                                }

                            case BinaryOperatorSpacingOptions.Single:
                                {
                                    formattingPathSubKey.DeleteValue(SpaceAroundBinaryOperator, throwOnMissingValue: false);
                                    formattingPathSubKey.DeleteValue(WrappingIgnoreSpacesAroundBinaryOperator, throwOnMissingValue: false);
                                    return true;
                                }
                        }
                    }
                    else if (optionKey.Option == CSharpFormattingOptions.LabelPositioning)
                    {
                        switch ((LabelPositionOptions)value)
                        {
                            case LabelPositionOptions.LeftMost:
                                {
                                    formattingPathSubKey.DeleteValue(UnindentLabels, throwOnMissingValue: false);
                                    formattingPathSubKey.SetValue(FlushLabelsLeft, 1, RegistryValueKind.DWord);
                                    return true;
                                }

                            case LabelPositionOptions.NoIndent:
                                {
                                    formattingPathSubKey.DeleteValue(FlushLabelsLeft, throwOnMissingValue: false);
                                    formattingPathSubKey.SetValue(UnindentLabels, 0, RegistryValueKind.DWord);
                                    return true;
                                }

                            case LabelPositionOptions.OneLess:
                                {
                                    formattingPathSubKey.DeleteValue(FlushLabelsLeft, throwOnMissingValue: false);
                                    formattingPathSubKey.DeleteValue(UnindentLabels, throwOnMissingValue: false);
                                    return true;
                                }
                        }
                    }
                }
            }

            return base.TryPersist(optionKey, value);
        }
    }
}
