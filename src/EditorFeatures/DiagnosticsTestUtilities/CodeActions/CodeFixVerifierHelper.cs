// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
using Microsoft.CodeAnalysis.Internal.Options;
#else
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    internal static class CodeFixVerifierHelper
    {
        public static void VerifyStandardProperties(DiagnosticAnalyzer analyzer, bool verifyHelpLink = false)
        {
            VerifyMessageTitle(analyzer);
            VerifyMessageDescription(analyzer);

            if (verifyHelpLink)
            {
                VerifyMessageHelpLinkUri(analyzer);
            }
        }

        private static void VerifyMessageTitle(DiagnosticAnalyzer analyzer)
        {
            foreach (var descriptor in analyzer.SupportedDiagnostics)
            {
                if (descriptor.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))
                {
                    // The title only displayed for rule configuration
                    continue;
                }

                Assert.NotEqual("", descriptor.Title?.ToString() ?? "");
            }
        }

        private static void VerifyMessageDescription(DiagnosticAnalyzer analyzer)
        {
            foreach (var descriptor in analyzer.SupportedDiagnostics)
            {
                if (ShouldSkipMessageDescriptionVerification(descriptor))
                {
                    continue;
                }

                Assert.NotEqual("", descriptor.MessageFormat?.ToString() ?? "");
            }

            return;

            // Local function
            static bool ShouldSkipMessageDescriptionVerification(DiagnosticDescriptor descriptor)
            {
                if (descriptor.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))
                {
                    if (!descriptor.IsEnabledByDefault || descriptor.DefaultSeverity == DiagnosticSeverity.Hidden)
                    {
                        // The message only displayed if either enabled and not hidden, or configurable
                        return true;
                    }
                }

                return false;
            }
        }

        private static void VerifyMessageHelpLinkUri(DiagnosticAnalyzer analyzer)
        {
            foreach (var descriptor in analyzer.SupportedDiagnostics)
            {
                Assert.NotEqual("", descriptor.HelpLinkUri ?? "");
            }
        }

        public static (SourceText? analyzerConfig, IEnumerable<KeyValuePair<OptionKey, object?>> options) ConvertOptionsToAnalyzerConfig(string defaultFileExtension, string? explicitEditorConfig, OptionsCollection options)
        {
            if (options.Count == 0)
            {
                var result = explicitEditorConfig is object ? SourceText.From(explicitEditorConfig, Encoding.UTF8) : null;
                return (result, options);
            }

            var optionSet = new OptionSetWrapper(options.ToDictionary<KeyValuePair<OptionKey, object?>, OptionKey, object?>(option => option.Key, option => option.Value));
            var remainingOptions = new List<KeyValuePair<OptionKey, object?>>();

            var analyzerConfig = new StringBuilder();
            if (explicitEditorConfig is object)
            {
                analyzerConfig.AppendLine(explicitEditorConfig);
            }
            else
            {
                analyzerConfig.AppendLine("root = true");
            }

            analyzerConfig.AppendLine();
            analyzerConfig.AppendLine($"[*.{defaultFileExtension}]");
            foreach (var (key, value) in options)
            {
                var editorConfigStorageLocation = key.Option.StorageLocations.OfType<IEditorConfigStorageLocation2>().FirstOrDefault();
                if (editorConfigStorageLocation is null)
                {
                    remainingOptions.Add(KeyValuePairUtil.Create<OptionKey, object?>(key, value));
                    continue;
                }

                analyzerConfig.AppendLine(editorConfigStorageLocation.GetEditorConfigString(value, optionSet));
            }

            return (SourceText.From(analyzerConfig.ToString(), Encoding.UTF8), remainingOptions);
        }

        private sealed class OptionSetWrapper : OptionSet
        {
            private readonly Dictionary<OptionKey, object?> _options;

            public OptionSetWrapper(Dictionary<OptionKey, object?> options)
            {
                _options = options;
            }

#if CODE_STYLE
            public override bool TryGetValue(string key, out string value)
            {
                throw new NotImplementedException();
            }
#else
            public override object? GetOption(OptionKey optionKey)
            {
                if (!_options.TryGetValue(optionKey, out var value))
                {
                    value = optionKey.Option.DefaultValue;
                }

                return value;
            }

            public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value)
                => throw new NotSupportedException();

            internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
                => SpecializedCollections.EmptyEnumerable<OptionKey>();

#endif
        }
    }
}
