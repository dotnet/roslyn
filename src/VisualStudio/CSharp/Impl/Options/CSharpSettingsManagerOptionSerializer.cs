// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
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
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;

using Task = System.Threading.Tasks.Task;

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
    internal sealed class CSharpSettingsManagerOptionSerializer : AbstractSettingsManagerOptionSerializer
    {
        [ImportingConstructor]
        public CSharpSettingsManagerOptionSerializer(SVsServiceProvider serviceProvider, IOptionService optionService)
            : base(serviceProvider, optionService)
        {
        }

        private const string WrappingIgnoreSpacesAroundBinaryOperator = nameof(AutomationObject.Wrapping_IgnoreSpacesAroundBinaryOperators);
        private const string SpaceAroundBinaryOperator = nameof(AutomationObject.Space_AroundBinaryOperator);
        private const string UnindentLabels = nameof(AutomationObject.Indent_UnindentLabels);
        private const string FlushLabelsLeft = nameof(AutomationObject.Indent_FlushLabelsLeft);
        private const string Style_UseVarForIntrinsicTypes = nameof(AutomationObject.Style_UseVarForIntrinsicTypes);
        private const string Style_UseVarWhenTypeIsApparent = nameof(AutomationObject.Style_UseVarWhenTypeIsApparent);
        private const string Style_UseVarWherePossible = nameof(AutomationObject.Style_UseVarWherePossible);

        private KeyValuePair<string, IOption> GetOptionInfoForOnOffOptions(FieldInfo fieldInfo)
        {
            var value = (IOption)fieldInfo.GetValue(obj: null);
            return new KeyValuePair<string, IOption>(GetStorageKeyForOption(value), value);
        }

        private bool ShouldIncludeOnOffOption(FieldInfo fieldInfo)
        {
            return SupportsOnOffOption((IOption)fieldInfo.GetValue(obj: null));
        }

        protected override ImmutableDictionary<string, IOption> CreateStorageKeyToOptionMap()
        {
            var result = ImmutableDictionary.Create<string, IOption>(StringComparer.OrdinalIgnoreCase).ToBuilder();

            result.AddRange(new[]
                {
                    new KeyValuePair<string, IOption>(GetStorageKeyForOption(CompletionOptions.IncludeKeywords), CompletionOptions.IncludeKeywords),
                    new KeyValuePair<string, IOption>(GetStorageKeyForOption(CompletionOptions.TriggerOnTypingLetters), CompletionOptions.TriggerOnTypingLetters),
                });

            Type[] types = new[]
                {
                    typeof(OrganizerOptions),
                    typeof(CSharpCompletionOptions),
                    typeof(SimplificationOptions),
                    typeof(CSharpCodeStyleOptions),
                    typeof(ExtractMethodOptions),
                    typeof(ServiceFeatureOnOffOptions),
                    typeof(CSharpFormattingOptions)
                };

            var bindingFlags = BindingFlags.Public | BindingFlags.Static;
            result.AddRange(AbstractSettingsManagerOptionSerializer.GetOptionInfoFromTypeFields(types, bindingFlags, this.GetOptionInfo));

            types = new[] { typeof(FeatureOnOffOptions) };
            result.AddRange(AbstractSettingsManagerOptionSerializer.GetOptionInfoFromTypeFields(types, bindingFlags, this.GetOptionInfoForOnOffOptions, this.ShouldIncludeOnOffOption));

            return result.ToImmutable();
        }

        protected override string LanguageName { get { return LanguageNames.CSharp; } }

        protected override string SettingStorageRoot { get { return "TextEditor.CSharp.Specific."; } }

        protected override bool SupportsOption(IOption option, string languageName)
        {
            if (option == OrganizerOptions.PlaceSystemNamespaceFirst ||
                option == OrganizerOptions.WarnOnBuildErrors ||
                option == CSharpCompletionOptions.AddNewLineOnEnterAfterFullyTypedWord ||
                option == CSharpCompletionOptions.IncludeSnippets ||
                option.Feature == CSharpCodeStyleOptions.FeatureName ||
                option.Feature == CSharpFormattingOptions.WrappingFeatureName ||
                option.Feature == CSharpFormattingOptions.IndentFeatureName ||
                option.Feature == CSharpFormattingOptions.SpacingFeatureName ||
                option.Feature == CSharpFormattingOptions.NewLineFormattingFeatureName)
            {
                return true;
            }
            else if (languageName == LanguageNames.CSharp)
            {
                if (option == CompletionOptions.IncludeKeywords ||
                    option == CompletionOptions.TriggerOnTypingLetters ||
                    option.Feature == SimplificationOptions.PerLanguageFeatureName ||
                    option.Feature == ExtractMethodOptions.FeatureName ||
                    option.Feature == ServiceFeatureOnOffOptions.OptionName ||
                    option.Feature == FormattingOptions.InternalTabFeatureName)
                {
                    return true;
                }
                else if (option.Feature == FeatureOnOffOptions.OptionName)
                {
                    return SupportsOnOffOption(option);
                }
            }

            return false;
        }

        private bool SupportsOnOffOption(IOption option)
        {
            return option == FeatureOnOffOptions.AutoFormattingOnCloseBrace ||
                   option == FeatureOnOffOptions.AutoFormattingOnSemicolon ||
                   option == FeatureOnOffOptions.LineSeparator ||
                   option == FeatureOnOffOptions.Outlining ||
                   option == FeatureOnOffOptions.ReferenceHighlighting ||
                   option == FeatureOnOffOptions.KeywordHighlighting ||
                   option == FeatureOnOffOptions.FormatOnPaste ||
                   option == FeatureOnOffOptions.AutoXmlDocCommentGeneration ||
                   option == FeatureOnOffOptions.RefactoringVerification ||
                   option == FeatureOnOffOptions.RenameTracking ||
                   option == FeatureOnOffOptions.RenameTrackingPreview;
        }

        public override bool TryFetch(OptionKey optionKey, out object value)
        {
            value = null;

            if (this.Manager == null)
            {
                Debug.Fail("Manager field is unexpectedly null.");
                return false;
            }

            if (optionKey.Option == CSharpFormattingOptions.SpacingAroundBinaryOperator)
            {
                // Remove space -> Space_AroundBinaryOperator = 0
                // Insert space -> Space_AroundBinaryOperator and Wrapping_IgnoreSpacesAroundBinaryOperator both missing
                // Ignore spacing -> Wrapping_IgnoreSpacesAroundBinaryOperator = 1

                object ignoreSpacesAroundBinaryObjectValue = this.Manager.GetValueOrDefault(WrappingIgnoreSpacesAroundBinaryOperator, defaultValue: 0);
                if (ignoreSpacesAroundBinaryObjectValue.Equals(1))
                {
                    value = BinaryOperatorSpacingOptions.Ignore;
                    return true;
                }

                object spaceAroundBinaryOperatorObjectValue = this.Manager.GetValueOrDefault(SpaceAroundBinaryOperator, defaultValue: 1);
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
                object flushLabelLeftObjectValue = this.Manager.GetValueOrDefault(FlushLabelsLeft, defaultValue: 0);
                if (flushLabelLeftObjectValue.Equals(1))
                {
                    value = LabelPositionOptions.LeftMost;
                    return true;
                }

                object unindentLabelsObjectValue = this.Manager.GetValueOrDefault(UnindentLabels, defaultValue: 1);
                if (unindentLabelsObjectValue.Equals(0))
                {
                    value = LabelPositionOptions.NoIndent;
                    return true;
                }

                value = LabelPositionOptions.OneLess;
                return true;
            }

            // code style: use var options.
            if (optionKey.Option == CSharpCodeStyleOptions.UseVarForIntrinsicTypes)
            {
                var useVarValue = this.Manager.GetValueOrDefault(Style_UseVarForIntrinsicTypes, defaultValue: 0);
                return FetchUseVarOption(useVarValue, out value);
            }
            else if (optionKey.Option == CSharpCodeStyleOptions.UseVarWhenTypeIsApparent)
            {
                var useVarValue = this.Manager.GetValueOrDefault(Style_UseVarWhenTypeIsApparent, defaultValue: 0);
                return FetchUseVarOption(useVarValue, out value);
            }
            else if (optionKey.Option == CSharpCodeStyleOptions.UseVarWherePossible)
            {
                var useVarValue = this.Manager.GetValueOrDefault(Style_UseVarWherePossible, defaultValue: 0);
                return FetchUseVarOption(useVarValue, out value);
            }

            return base.TryFetch(optionKey, out value);
        }


        public override bool TryPersist(OptionKey optionKey, object value)
        {
            if (this.Manager == null)
            {
                Debug.Fail("Manager field is unexpectedly null.");
                return false;
            }

            if (optionKey.Option == CSharpFormattingOptions.SpacingAroundBinaryOperator)
            {
                // Remove space -> Space_AroundBinaryOperator = 0
                // Insert space -> Space_AroundBinaryOperator and Wrapping_IgnoreSpacesAroundBinaryOperator both missing
                // Ignore spacing -> Wrapping_IgnoreSpacesAroundBinaryOperator = 1

                switch ((BinaryOperatorSpacingOptions)value)
                {
                    case BinaryOperatorSpacingOptions.Remove:
                        {
                            this.Manager.SetValueAsync(WrappingIgnoreSpacesAroundBinaryOperator, value: 0, isMachineLocal: false);
                            this.Manager.SetValueAsync(SpaceAroundBinaryOperator, 0, isMachineLocal: false);
                            return true;
                        }

                    case BinaryOperatorSpacingOptions.Ignore:
                        {
                            this.Manager.SetValueAsync(SpaceAroundBinaryOperator, value: 1, isMachineLocal: false);
                            this.Manager.SetValueAsync(WrappingIgnoreSpacesAroundBinaryOperator, 1, isMachineLocal: false);
                            return true;
                        }

                    case BinaryOperatorSpacingOptions.Single:
                        {
                            this.Manager.SetValueAsync(SpaceAroundBinaryOperator, value: 1, isMachineLocal: false);
                            this.Manager.SetValueAsync(WrappingIgnoreSpacesAroundBinaryOperator, value: 0, isMachineLocal: false);
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
                            this.Manager.SetValueAsync(UnindentLabels, value: 1, isMachineLocal: false);
                            this.Manager.SetValueAsync(FlushLabelsLeft, 1, isMachineLocal: false);
                            return true;
                        }

                    case LabelPositionOptions.NoIndent:
                        {
                            this.Manager.SetValueAsync(FlushLabelsLeft, value: 0, isMachineLocal: false);
                            this.Manager.SetValueAsync(UnindentLabels, 0, isMachineLocal: false);
                            return true;
                        }

                    case LabelPositionOptions.OneLess:
                        {
                            this.Manager.SetValueAsync(FlushLabelsLeft, value: 0, isMachineLocal: false);
                            this.Manager.SetValueAsync(UnindentLabels, value: 1, isMachineLocal: false);
                            return true;
                        }
                }
            }

            // code style: use var options.
            if (optionKey.Option == CSharpCodeStyleOptions.UseVarForIntrinsicTypes)
            {
                return PersistUseVarOption(Style_UseVarForIntrinsicTypes, value);
            }
            else if (optionKey.Option == CSharpCodeStyleOptions.UseVarWhenTypeIsApparent)
            {
                return PersistUseVarOption(Style_UseVarWhenTypeIsApparent, value);
            }
            else if (optionKey.Option == CSharpCodeStyleOptions.UseVarWherePossible)
            {
                return PersistUseVarOption(Style_UseVarWherePossible, value);
            }

            return base.TryPersist(optionKey, value);
        }

        private bool PersistUseVarOption(string option, object value)
        {
            var convertedValue = (CodeStyleOption)value;

            if (!convertedValue.IsChecked)
            {
                this.Manager.SetValueAsync(option, value: 0, isMachineLocal: false);
                return true;
            }
            else
            {
                switch (convertedValue.Notification.Value)
                {
                    case DiagnosticSeverity.Hidden:
                        this.Manager.SetValueAsync(option, value: 1, isMachineLocal: false);
                        break;
                    case DiagnosticSeverity.Info:
                        this.Manager.SetValueAsync(option, value: 2, isMachineLocal: false);
                        break;
                    case DiagnosticSeverity.Warning:
                        this.Manager.SetValueAsync(option, value: 3, isMachineLocal: false);
                        break;
                    case DiagnosticSeverity.Error:
                        this.Manager.SetValueAsync(option, value: 4, isMachineLocal: false);
                        break;
                    default:
                        break;
                }
                return true;
            }
        }

        private static bool FetchUseVarOption(int useVarOptionValue, out object value)
        {
            switch (useVarOptionValue)
            {
                case 0:
                    value = new CodeStyleOption(false, NotificationOption.None);
                    break;
                case 1:
                    value = new CodeStyleOption(true, NotificationOption.None);
                    break;
                case 2:
                    value = new CodeStyleOption(true, NotificationOption.Info);
                    break;
                case 3:
                    value = new CodeStyleOption(true, NotificationOption.Warning);
                    break;
                case 4:
                    value = new CodeStyleOption(true, NotificationOption.Error);
                    break;
                default:
                    value = null;
                    break;
            }

            return value != null;
        }
    }
}