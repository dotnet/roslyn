// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

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
        var text = ConvertOptionsToAnalyzerConfig(options.DefaultExtension, explicitEditorConfig: string.Empty, options);
        return text?.ToString();
    }

    public static SourceText? ConvertOptionsToAnalyzerConfig(string defaultFileExtension, string? explicitEditorConfig, OptionsCollection options)
    {
        if (options.Count == 0)
        {
            return explicitEditorConfig is object ? SourceText.From(explicitEditorConfig, Encoding.UTF8) : null;
        }

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

        foreach (var (optionKey, value) in options)
        {
            Assert.True(optionKey.Option.Definition.IsEditorConfigOption);

            if (value is NamingStylePreferences namingStylePreferences)
            {
                namingStylePreferences.AppendToEditorConfig(optionKey.Language!, analyzerConfig);
                continue;
            }

            analyzerConfig.AppendLine($"{optionKey.Option.Definition.ConfigName} = {optionKey.Option.Definition.Serializer.Serialize(value)}");
        }

        return SourceText.From(analyzerConfig.ToString(), Encoding.UTF8);
    }
}
