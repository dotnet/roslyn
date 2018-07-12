// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    /// <summary>
    /// Interaction logic for FormattingOptionPageControl.xaml
    /// </summary>
    internal partial class FormattingOptionPageControl : AbstractOptionPageControl
    {
        public FormattingOptionPageControl(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            InitializeComponent();

            FormatWhenTypingCheckBox.Content = CSharpVSResources.Automatically_format_when_typing;
            FormatOnSemicolonCheckBox.Content = CSharpVSResources.Automatically_format_statement_on_semicolon;
            FormatOnCloseBraceCheckBox.Content = CSharpVSResources.Automatically_format_block_on_close_brace;
            FormatOnReturnCheckBox.Content = CSharpVSResources.Automatically_format_on_return;
            FormatOnPasteCheckBox.Content = CSharpVSResources.Automatically_format_on_paste;

            BindToOption(FormatWhenTypingCheckBox, FeatureOnOffOptions.AutoFormattingOnTyping, LanguageNames.CSharp);
            BindToOption(FormatOnCloseBraceCheckBox, FeatureOnOffOptions.AutoFormattingOnCloseBrace, LanguageNames.CSharp);
            BindToOption(FormatOnSemicolonCheckBox, FeatureOnOffOptions.AutoFormattingOnSemicolon, LanguageNames.CSharp);
            BindToOption(FormatOnReturnCheckBox, FeatureOnOffOptions.AutoFormattingOnReturn, LanguageNames.CSharp);
            BindToOption(FormatOnPasteCheckBox, FeatureOnOffOptions.FormatOnPaste, LanguageNames.CSharp);

            FormatDocumentSettingsGroupBox.Header = CSharpVSResources.Format_document_settings;
            AllCSharpFormattingRulesCheckBox.Content = CSharpVSResources.Apply_all_csharp_formatting_rules_indentation_wrapping_spacing;
            PerformAdditionalCodeCleanupDuringFormattingCheckBox.Content = CSharpVSResources.Perform_additional_code_cleanup_during_formatting;
            RemoveUnusedUsingsCheckBox.Content = CSharpVSResources.Remove_unnecessary_usings;
            SortUsingsCheckBox.Content = CSharpVSResources.Sort_usings;
            AddRemoveBracesForSingleLineControlStatementsCheckBox.Content = CSharpVSResources.Add_remove_braces_for_single_line_control_statements;
            AddAccessibilityModifiersCheckBox.Content = CSharpVSResources.Add_accessibility_modifiers;
            SortAccessibilityModifiersCheckBox.Content = CSharpVSResources.Sort_accessibility_modifiers;
            ApplyExpressionBlockBodyPreferencesCheckBox.Content = CSharpVSResources.Apply_expression_block_body_preferences;
            ApplyImplicitExplicitTypePreferencesCheckBox.Content = CSharpVSResources.Apply_implicit_explicit_type_preferences;
            ApplyInlineOutVariablePreferencesCheckBox.Content = CSharpVSResources.Apply_inline_out_variable_preferences;
            ApplyLanguageFrameworkTypePreferencesCheckBox.Content = CSharpVSResources.Apply_language_framework_type_preferences;
            ApplyObjectCollectionInitializationPreferencesCheckBox.Content = CSharpVSResources.Apply_object_collection_initialization_preferences;
            ApplyThisQualificationPreferencesCheckBox.Content = CSharpVSResources.Apply_this_qualification_preferences;
            MakePrivateFieldReadonlyWhenPossibleCheckBox.Content = CSharpVSResources.Make_private_field_readonly_when_possible;
            RemoveUnnecessaryCastsCheckBox.Content = CSharpVSResources.Remove_unnecessary_casts;
            RemoveUnusedVariablesCheckBox.Content = CSharpVSResources.Remove_unused_variables;

            BindToOption(PerformAdditionalCodeCleanupDuringFormattingCheckBox, CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting, LanguageNames.CSharp);

            BindToOption(RemoveUnusedUsingsCheckBox, CodeCleanupOptions.RemoveUnusedImports, LanguageNames.CSharp);
            BindToOption(SortUsingsCheckBox, CodeCleanupOptions.SortImports, LanguageNames.CSharp);
            BindToOption(AddRemoveBracesForSingleLineControlStatementsCheckBox, CodeCleanupOptions.AddRemoveBracesForSingleLineControlStatements, LanguageNames.CSharp);
            BindToOption(AddAccessibilityModifiersCheckBox, CodeCleanupOptions.AddAccessibilityModifiers, LanguageNames.CSharp);
            BindToOption(SortAccessibilityModifiersCheckBox, CodeCleanupOptions.SortAccessibilityModifiers, LanguageNames.CSharp);
            BindToOption(ApplyExpressionBlockBodyPreferencesCheckBox, CodeCleanupOptions.ApplyExpressionBlockBodyPreferences, LanguageNames.CSharp);
            BindToOption(ApplyImplicitExplicitTypePreferencesCheckBox, CodeCleanupOptions.ApplyImplicitExplicitTypePreferences, LanguageNames.CSharp);
            BindToOption(ApplyInlineOutVariablePreferencesCheckBox, CodeCleanupOptions.ApplyInlineOutVariablePreferences, LanguageNames.CSharp);
            BindToOption(ApplyLanguageFrameworkTypePreferencesCheckBox, CodeCleanupOptions.ApplyLanguageFrameworkTypePreferences, LanguageNames.CSharp);
            BindToOption(ApplyObjectCollectionInitializationPreferencesCheckBox, CodeCleanupOptions.ApplyObjectCollectionInitializationPreferences, LanguageNames.CSharp);
            BindToOption(ApplyThisQualificationPreferencesCheckBox, CodeCleanupOptions.ApplyThisQualificationPreferences, LanguageNames.CSharp);
            BindToOption(MakePrivateFieldReadonlyWhenPossibleCheckBox, CodeCleanupOptions.MakePrivateFieldReadonlyWhenPossible, LanguageNames.CSharp);
            BindToOption(RemoveUnnecessaryCastsCheckBox, CodeCleanupOptions.RemoveUnnecessaryCasts, LanguageNames.CSharp);
            BindToOption(RemoveUnusedVariablesCheckBox, CodeCleanupOptions.RemoveUnusedVariables, LanguageNames.CSharp);
        }

        internal override void SaveSettings()
        {
            base.SaveSettings();

            // once formatting option is explicitly set (regardless codeclean is on or off), 
            // we never show code cleanup info bar again
            var oldOptions = OptionService.GetOptions();
            var newOptions = oldOptions.WithChangedOption(
                CodeCleanupOptions.NeverShowCodeCleanupInfoBarAgain, LanguageNames.CSharp, value: true);

            OptionService.SetOptions(newOptions);
            OptionLogger.Log(oldOptions, newOptions);
        }
    }
}
