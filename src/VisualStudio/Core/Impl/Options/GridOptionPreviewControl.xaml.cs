// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal partial class GridOptionPreviewControl : AbstractOptionPageControl
    {
        private const string UseEditorConfigUrl = "https://go.microsoft.com/fwlink/?linkid=866541";
        internal AbstractOptionPreviewViewModel ViewModel;
        private readonly IServiceProvider _serviceProvider;
        private readonly Func<OptionSet, IServiceProvider, AbstractOptionPreviewViewModel> _createViewModel;
        private readonly Action<OptionSet, StringBuilder> _getLangaugeSpecificEditorConfigOptions;
        private readonly string _language;

        public static readonly Uri CodeStylePageHeaderLearnMoreUri = new Uri(UseEditorConfigUrl);
        public static string CodeStylePageHeader => ServicesVSResources.Code_style_header_use_editor_config;
        public static string CodeStylePageHeaderLearnMoreText => ServicesVSResources.Learn_more;
        public static string DescriptionHeader => ServicesVSResources.Description;
        public static string PreferenceHeader => ServicesVSResources.Preference;
        public static string SeverityHeader => ServicesVSResources.Severity;

        internal GridOptionPreviewControl(IServiceProvider serviceProvider,
            Func<OptionSet, IServiceProvider,
            AbstractOptionPreviewViewModel> createViewModel,
            Action<OptionSet, StringBuilder> getLangaugeSpecificEditorConfigOptions,
            string language)
            : base(serviceProvider)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _createViewModel = createViewModel;
            _getLangaugeSpecificEditorConfigOptions = getLangaugeSpecificEditorConfigOptions;
            _language = language;
        }

        private void LearnMoreHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri == null)
            {
                return;
            }

            BrowserHelper.StartBrowser(e.Uri);
            e.Handled = true;
        }

        private void Options_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            var codeStyleItem = (AbstractCodeStyleOptionViewModel)dataGrid.SelectedItem;

            if (codeStyleItem != null)
            {
                ViewModel.UpdatePreview(codeStyleItem.GetPreview());
            }
        }

        private void Options_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // TODO: make the combo to drop down on space or some key.
            if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
            }
        }

        internal override void SaveSettings()
        {
            var optionSet = this.OptionService.GetOptions();
            var changedOptions = this.ViewModel.ApplyChangedOptions(optionSet);

            this.OptionService.SetOptions(changedOptions);
            OptionLogger.Log(optionSet, changedOptions);
        }

        internal override void LoadSettings()
        {
            this.ViewModel = _createViewModel(this.OptionService.GetOptions(), _serviceProvider);

            var firstItem = this.ViewModel.CodeStyleItems.OfType<AbstractCodeStyleOptionViewModel>().First();
            this.ViewModel.SetOptionAndUpdatePreview(firstItem.SelectedPreference.IsChecked, firstItem.Option, firstItem.GetPreview());

            DataContext = ViewModel;
        }

        internal override void Close()
        {
            base.Close();

            if (this.ViewModel != null)
            {
                this.ViewModel.Dispose();
            }
        }

        internal void Generate_Save_Editorconfig(object sender, System.Windows.RoutedEventArgs e)
        {
            var optionSet = this.ViewModel.ApplyChangedOptions(this.OptionService.GetOptions());
            var editorconfig = new StringBuilder();
            Generate_Editorconfig(optionSet, _language, editorconfig, _getLangaugeSpecificEditorConfigOptions);
            var sfd = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "All files (*.*)|",
                FileName = ".editorconfig",
                Title = "Save .editorconfig file"
            };
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                IOUtilities.PerformIO(() =>
                {
                    var path = sfd.FileName;
                    using (var sw = new StreamWriter(File.Create(path)))
                    {
                        sw.Write(editorconfig);
                        sw.Close();
                    }
                });
            }
        }

        internal static void Generate_Editorconfig(
            OptionSet optionSet,
            string language,
            StringBuilder editorconfig,
            Action<OptionSet, StringBuilder> getLangaugeSpecificEditorConfigOptions)
        {
            GenerateEditorconfig_CoreSettings(optionSet, language, editorconfig);
            GenerateEditorconfig_DotNetSettings(optionSet, language, editorconfig);

            getLangaugeSpecificEditorConfigOptions(optionSet, editorconfig);
        }

        private static void GenerateEditorconfig_CoreSettings(OptionSet optionSet, string language, StringBuilder editorconfig)
        {
            editorconfig.AppendLine("# Core EditorConfig Options");
            editorconfig.AppendLine("# Comment the line below if you want to inherit parent .editorconfig settings.");
            editorconfig.AppendLine("root = true");

            editorconfig.AppendLine();
            if (language == LanguageNames.CSharp)
            {
                editorconfig.AppendLine("# C# files");
                editorconfig.AppendLine("[*.cs]");
            }
            else if (language == LanguageNames.VisualBasic)
            {
                editorconfig.AppendLine("# Basic files");
                editorconfig.AppendLine("[*.vb]");
            }

            // indent_style
            CoreCodeStyleOptions_GenerateEditorconfig(optionSet, FormattingOptions.UseTabs, language, editorconfig);
            // indent_size
            CoreCodeStyleOptions_GenerateEditorconfig(optionSet, FormattingOptions.IndentationSize, language, editorconfig);
            // insert_final_newline
            CoreCodeStyleOptions_GenerateEditorconfig(optionSet, FormattingOptions.InsertFinalNewLine, editorconfig);
        }

        private static void CoreCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, PerLanguageOption<bool> option, string language, StringBuilder editorconfig)
        {
            var element = option.StorageLocations.OfType<EditorConfigStorageLocation<bool>>().FirstOrDefault();
            if (element != null)
            {
                editorconfig.Append(element.KeyName + " = ");
                var curSetting = optionSet.GetOption(option, language);
                if (curSetting)
                {
                    editorconfig.AppendLine("tab");
                }
                else
                {
                    editorconfig.AppendLine("space");
                }
            }
        }

        private static void CoreCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, PerLanguageOption<int> option, string language, StringBuilder editorconfig)
        {
            var element = option.StorageLocations.OfType<EditorConfigStorageLocation<int>>().FirstOrDefault();
            if (element != null)
            {
                editorconfig.Append(element.KeyName + " = ");
                editorconfig.AppendLine(optionSet.GetOption(option, language).ToString());
            }
        }

        private static void CoreCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, Option<bool> option, StringBuilder editorconfig)
        {
            var element = option.StorageLocations.OfType<EditorConfigStorageLocation<bool>>().FirstOrDefault();
            if (element != null)
            {
                editorconfig.Append(element.KeyName + " = ");
                editorconfig.AppendLine(optionSet.GetOption(option).ToString().ToLowerInvariant());
            }
        }

        private static void GenerateEditorconfig_DotNetSettings(OptionSet optionSet, string language, StringBuilder editorconfig)
        {
            editorconfig.AppendLine();
            editorconfig.AppendLine("# .NET Coding Conventions");
  
            editorconfig.AppendLine("# Organize usings:");
            // dotnet_sort_system_directives_first
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, GenerationOptions.PlaceSystemNamespaceFirst, language, editorconfig);

            editorconfig.AppendLine();
            if (language == LanguageNames.CSharp)
            {
                editorconfig.AppendLine("# this. preferences:");
            }
            else if (language == LanguageNames.VisualBasic)
            {
                editorconfig.AppendLine("# Me. preferences:");
            }

            // dotnet_style_qualification_for_field
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.QualifyFieldAccess, language, editorconfig);
            // dotnet_style_qualification_for_property
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.QualifyPropertyAccess, language, editorconfig);
            // dotnet_style_qualification_for_method
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.QualifyMethodAccess, language, editorconfig);
            // dotnet_style_qualification_for_event
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.QualifyEventAccess, language, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# Language keywords vs BCL types preferences:");
            // dotnet_style_predefined_type_for_locals_parameters_members
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, language, editorconfig);
            // dotnet_style_predefined_type_for_member_access
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, language, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# " + ServicesVSResources.Parentheses_preferences_colon);
            // dotnet_style_parentheses_in_arithmetic_binary_operators
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.ArithmeticBinaryParentheses, language, editorconfig);
            // dotnet_style_parentheses_in_relational_binary_operators
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.RelationalBinaryParentheses, language, editorconfig);
            // dotnet_style_parentheses_in_other_binary_operators
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.OtherBinaryParentheses, language, editorconfig);
            // dotnet_style_parentheses_in_other_operators
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.OtherParentheses, language, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# Modifier preferences:");
            // dotnet_style_require_accessibility_modifiers
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.RequireAccessibilityModifiers, language, editorconfig);
            // dotnet_style_readonly_field
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferReadonly, language, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# Expression-level preferences:");
            // dotnet_style_object_initializer
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferObjectInitializer, language, editorconfig);
            // dotnet_style_collection_initializer
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferCollectionInitializer, language, editorconfig);
            // dotnet_style_explicit_tuple_names
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferExplicitTupleNames, language, editorconfig);
            // dotnet_style_null_propagation
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferNullPropagation, language, editorconfig);
            // dotnet_style_coalesce_expression
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferCoalesceExpression, language, editorconfig);
            // dotnet_style_prefer_is_null_check_over_reference_equality_method
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferIsNullCheckOverReferenceEqualityMethod, language, editorconfig);
            // dotnet_prefer_inferred_tuple_names
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferInferredTupleNames, language, editorconfig);
            // dotnet_prefer_inferred_anonymous_type_member_names
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferInferredAnonymousTypeMemberNames, language, editorconfig);
            // dotnet_style_prefer_auto_properties
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferAutoProperties, language, editorconfig);
            // dotnet_style_prefer_conditional_expression_over_assignment
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferConditionalExpressionOverAssignment, language, editorconfig);
            // dotnet_style_prefer_conditional_expression_over_return
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferConditionalExpressionOverReturn, language, editorconfig);
        }
        private static void DotNetCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, PerLanguageOption<bool> option, string language, StringBuilder editorconfig)
        {
            var element = option.StorageLocations.OfType<EditorConfigStorageLocation<bool>>().FirstOrDefault();
            if (element != null)
            {
                editorconfig.Append(element.KeyName + " = ");
                editorconfig.AppendLine(optionSet.GetOption(option, language).ToString().ToLowerInvariant());
            }
        }

        private static void DotNetCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, PerLanguageOption<CodeStyleOption<bool>> option, string language, StringBuilder editorconfig)
        {
            var element = option.StorageLocations.OfType<EditorConfigStorageLocation<CodeStyleOption<bool>>>().FirstOrDefault();
            if (element != null)
            {
                editorconfig.Append(element.KeyName + " = ");

                var curSetting = optionSet.GetOption(option, language);
                editorconfig.AppendLine(curSetting.Value.ToString().ToLowerInvariant() + ":" + curSetting.Notification.ToString().ToLowerInvariant());
            }
        }

        private static void DotNetCodeStyleOptions_GenerateEditorconfig(
            OptionSet optionSet,
            PerLanguageOption<CodeStyleOption<ParenthesesPreference>> option,
            string language,
            StringBuilder editorconfig)
        {
            var element = option.StorageLocations.OfType<EditorConfigStorageLocation<CodeStyleOption<ParenthesesPreference>>>().FirstOrDefault();
            if (element != null)
            {
                editorconfig.Append(element.KeyName + " = ");

                var curSetting = optionSet.GetOption(option, language);
                if (curSetting.Value == ParenthesesPreference.AlwaysForClarity)
                {
                    editorconfig.AppendLine("always_for_clarity:" + curSetting.Notification.ToString().ToLowerInvariant());
                }
                else if (curSetting.Value == ParenthesesPreference.NeverIfUnnecessary)
                {
                    editorconfig.AppendLine("never_if_unnecessary:" + curSetting.Notification.ToString().ToLowerInvariant());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        private static void DotNetCodeStyleOptions_GenerateEditorconfig(
            OptionSet optionSet,
            PerLanguageOption<CodeStyleOption<AccessibilityModifiersRequired>> option,
            string language,
            StringBuilder editorconfig)
        {
            var element = option.StorageLocations.OfType<EditorConfigStorageLocation<CodeStyleOption<AccessibilityModifiersRequired>>>().FirstOrDefault();
            if (element != null)
            {
                editorconfig.Append(element.KeyName + " = ");

                var curSetting = optionSet.GetOption(option, language);
                if (curSetting.Value == AccessibilityModifiersRequired.ForNonInterfaceMembers)
                {
                    editorconfig.AppendLine("for_non_interface_members:" + curSetting.Notification.ToString().ToLowerInvariant());
                }
                else if (curSetting.Value == AccessibilityModifiersRequired.OmitIfDefault)
                {
                    editorconfig.AppendLine("omit_if_default:" + curSetting.Notification.ToString().ToLowerInvariant());
                }
                else if (curSetting.Value == AccessibilityModifiersRequired.Always || curSetting.Value == AccessibilityModifiersRequired.Never)
                {
                    editorconfig.AppendLine(curSetting.Value.ToString().ToLowerInvariant() + ":" + curSetting.Notification.ToString().ToLowerInvariant());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
