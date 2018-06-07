// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;
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
            SetNestedCheckboxesEnabled();

            FormatDocumentSettingsGroupBox.Header = CSharpVSResources.Format_document_settings;
            AllCSharpFormattingRulesCheckBox.Content = CSharpVSResources.All_csharp_formatting_rules;
            RemoveUnusedUsingsCheckBox.Content = CSharpVSResources.Remove_unnecessary_usings;
            SortUsingsCheckBox.Content = CSharpVSResources.Sort_usings;
            FixImplicitExplicitTypeCheckBox.Content = CSharpVSResources.Fix_implicit_explicit_type;
            FixThisQualificationCheckBox.Content = CSharpVSResources.Fix_this_qualification;
            FixFrameworkTypesCheckBox.Content = CSharpVSResources.Fix_framework_types;
            FixAddRemoveBracesCheckBox.Content = CSharpVSResources.Fix_add_remove_braces;
            FixAccessibilityModifiersCheckBox.Content = CSharpVSResources.Fix_accessibility_modifiers;
            SortAccessibilityModifiersCheckBox.Content = CSharpVSResources.Sort_accessibility_modifiers;
            MakeReadonlyCheckBox.Content = CSharpVSResources.Make_readonly;
            RemoveUnnecessaryCastsCheckBox.Content = CSharpVSResources.Remove_unnecessary_casts;
            FixExpressionBodiedMembersCheckBox.Content = CSharpVSResources.Fix_expression_bodied_members;
            FixInlineVariableDeclarationsCheckBox.Content = CSharpVSResources.Fix_inline_variable_declarations;
            RemoveUnusedVariablesCheckBox.Content = CSharpVSResources.Remove_unused_variables;
            FixObjectCollectionInitializationCheckBox.Content = CSharpVSResources.Fix_object_collection_initialization;
            FixLanguageFeaturesCheckBox.Content = CSharpVSResources.Fix_language_features;

            BindToOption(RemoveUnusedUsingsCheckBox, CodeCleanupOptions.RemoveUnusedImports, LanguageNames.CSharp);
            BindToOption(SortUsingsCheckBox, CodeCleanupOptions.SortImports, LanguageNames.CSharp);
            BindToOption(FixImplicitExplicitTypeCheckBox, CodeCleanupOptions.FixImplicitExplicitType, LanguageNames.CSharp);
            BindToOption(FixFrameworkTypesCheckBox, CodeCleanupOptions.FixFrameworkTypes, LanguageNames.CSharp);
            BindToOption(FixThisQualificationCheckBox, CodeCleanupOptions.FixThisQualification, LanguageNames.CSharp);
            BindToOption(FixAddRemoveBracesCheckBox, CodeCleanupOptions.FixAddRemoveBraces, LanguageNames.CSharp);
            BindToOption(FixAccessibilityModifiersCheckBox, CodeCleanupOptions.FixAccessibilityModifiers, LanguageNames.CSharp);
            BindToOption(SortAccessibilityModifiersCheckBox, CodeCleanupOptions.SortAccessibilityModifiers, LanguageNames.CSharp);
            BindToOption(MakeReadonlyCheckBox, CodeCleanupOptions.MakeReadonly, LanguageNames.CSharp);
            BindToOption(RemoveUnnecessaryCastsCheckBox, CodeCleanupOptions.RemoveUnnecessaryCasts, LanguageNames.CSharp);
            BindToOption(FixExpressionBodiedMembersCheckBox, CodeCleanupOptions.FixExpressionBodiedMembers, LanguageNames.CSharp);
            BindToOption(FixInlineVariableDeclarationsCheckBox, CodeCleanupOptions.FixInlineVariableDeclarations, LanguageNames.CSharp);
            BindToOption(RemoveUnusedVariablesCheckBox, CodeCleanupOptions.RemoveUnusedVariables, LanguageNames.CSharp);
            BindToOption(FixObjectCollectionInitializationCheckBox, CodeCleanupOptions.FixObjectCollectionInitialization, LanguageNames.CSharp);
            BindToOption(FixLanguageFeaturesCheckBox, CodeCleanupOptions.FixLanguageFeatures, LanguageNames.CSharp);
        }

        private void FormatWhenTypingCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FormatOnCloseBraceCheckBox.IsChecked = true;
            FormatOnSemicolonCheckBox.IsChecked = true;

            SetNestedCheckboxesEnabled();
        }

        private void FormatWhenTypingCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            FormatOnCloseBraceCheckBox.IsChecked = false;
            FormatOnSemicolonCheckBox.IsChecked = false;

            SetNestedCheckboxesEnabled();
        }

        private void SetNestedCheckboxesEnabled()
        {
            FormatOnCloseBraceCheckBox.IsEnabled = FormatWhenTypingCheckBox.IsChecked == true;
            FormatOnSemicolonCheckBox.IsEnabled = FormatWhenTypingCheckBox.IsChecked == true;
        }

        internal void SetCodeCleanupAsConfigured()
        {
            var areCodeCleanupRulesConfigured = OptionService.GetOption<bool>(CodeCleanupOptions.AreCodeCleanupRulesConfigured, LanguageNames.CSharp);

            if (!areCodeCleanupRulesConfigured)
            {
                var oldOptions = OptionService.GetOptions();
                var newOptions = oldOptions.WithChangedOption(CodeCleanupOptions.AreCodeCleanupRulesConfigured, LanguageNames.CSharp, true);

                OptionService.SetOptions(newOptions);
                OptionLogger.Log(oldOptions, newOptions);
            }
        }
    }
}
