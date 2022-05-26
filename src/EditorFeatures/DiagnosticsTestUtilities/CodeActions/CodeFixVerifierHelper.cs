// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    internal static class CodeFixVerifierHelper
    {
        public static void VerifyStandardProperty(DiagnosticAnalyzer analyzer, AnalyzerProperty property)
        {
            switch (property)
            {
                case AnalyzerProperty.Title:
                    VerifyMessageTitle(analyzer);
                    return;

                case AnalyzerProperty.Description:
                    VerifyMessageDescription(analyzer);
                    return;

                case AnalyzerProperty.HelpLink:
                    VerifyMessageHelpLinkUri(analyzer);
                    return;

                default:
                    throw ExceptionUtilities.UnexpectedValue(property);
            }
        }

        private static void VerifyMessageTitle(DiagnosticAnalyzer analyzer)
        {
            foreach (var descriptor in analyzer.SupportedDiagnostics)
            {
                if (descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.NotConfigurable))
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
                if (descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.NotConfigurable))
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

        public static string? GetEditorConfigText(this OptionsCollection options)
        {
            var (text, _) = ConvertOptionsToAnalyzerConfig(options.DefaultExtension, explicitEditorConfig: string.Empty, options);
            return text?.ToString();
        }

        public static (SourceText? analyzerConfig, IEnumerable<KeyValuePair<OptionKey2, object?>> options) ConvertOptionsToAnalyzerConfig(string defaultFileExtension, string? explicitEditorConfig, OptionsCollection options)
        {
            if (options.Count == 0)
            {
                var result = explicitEditorConfig is object ? SourceText.From(explicitEditorConfig, Encoding.UTF8) : null;
                return (result, options);
            }

            var remainingOptions = new List<KeyValuePair<OptionKey2, object?>>();

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

#if CODE_STYLE
            var optionSet = new DummyAnalyzerConfigOptions();
#else
            var optionSet = options.ToOptionSet();
#endif

            foreach (var (key, value) in options)
            {
                if (value is NamingStylePreferences namingStylePreferences)
                {
                    EditorConfigFileGenerator.AppendNamingStylePreferencesToEditorConfig(namingStylePreferences, key.Language!, analyzerConfig);
                    continue;
                }

                var editorConfigStorageLocation = key.Option.StorageLocations.OfType<IEditorConfigStorageLocation2>().FirstOrDefault();
                if (editorConfigStorageLocation is null)
                {
                    remainingOptions.Add(KeyValuePairUtil.Create<OptionKey2, object?>(key, value));
                    continue;
                }

                analyzerConfig.AppendLine(editorConfigStorageLocation.GetEditorConfigString(value, optionSet));
            }

            return (SourceText.From(analyzerConfig.ToString(), Encoding.UTF8), remainingOptions);
        }

#if CODE_STYLE
        internal sealed class DummyAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
                => throw new NotImplementedException();
        }
#endif
    }
}
