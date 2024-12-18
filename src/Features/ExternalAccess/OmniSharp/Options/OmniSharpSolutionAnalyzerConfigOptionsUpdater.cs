// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.ImplementType;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Options;

using Workspace = CodeAnalysis.Workspace;

internal static class OmniSharpSolutionAnalyzerConfigOptionsUpdater
{
    internal static void UpdateOptions(Workspace workspace, OmniSharpEditorConfigOptions editorConfigOptions)
    {
        try
        {
            workspace.SetCurrentSolution(UpdateOptions, changeKind: WorkspaceChangeKind.SolutionChanged);

            Solution UpdateOptions(Solution oldSolution)
            {
                var oldFallbackOptions = oldSolution.FallbackAnalyzerOptions;
                oldFallbackOptions.TryGetValue(LanguageNames.CSharp, out var csharpFallbackOptions);

                var changedOptions = DetermineChangedOptions(csharpFallbackOptions, editorConfigOptions);
                if (changedOptions.IsEmpty)
                {
                    return oldSolution;
                }

                var builder = ImmutableDictionary.CreateBuilder<string, string>(AnalyzerConfigOptions.KeyComparer);
                if (csharpFallbackOptions is not null)
                {
                    // copy existing option values:
                    foreach (var oldKey in csharpFallbackOptions.Keys)
                    {
                        if (csharpFallbackOptions.TryGetValue(oldKey, out var oldValue))
                        {
                            builder.Add(oldKey, oldValue);
                        }
                    }
                }

                // update changed values:
                foreach (var (key, value) in changedOptions)
                {
                    builder[key] = value;
                }

                var newFallbackOptions = oldFallbackOptions.SetItem(
                    LanguageNames.CSharp,
                    StructuredAnalyzerConfigOptions.Create(new DictionaryAnalyzerConfigOptions(builder.ToImmutable())));

                return oldSolution.WithFallbackAnalyzerOptions(newFallbackOptions);
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e, ErrorSeverity.Diagnostic))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    private static ImmutableDictionary<string, string> DetermineChangedOptions(
        StructuredAnalyzerConfigOptions? csharpFallbackOptions,
        OmniSharpEditorConfigOptions editorConfigOptions)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>();

        AddOptionIfChanged(FormattingOptions2.UseTabs, csharpFallbackOptions, editorConfigOptions.LineFormattingOptions.UseTabs, builder);
        AddOptionIfChanged(FormattingOptions2.UseTabs, csharpFallbackOptions, editorConfigOptions.LineFormattingOptions.UseTabs, builder);
        AddOptionIfChanged(FormattingOptions2.TabSize, csharpFallbackOptions, editorConfigOptions.LineFormattingOptions.TabSize, builder);
        AddOptionIfChanged(FormattingOptions2.IndentationSize, csharpFallbackOptions, editorConfigOptions.LineFormattingOptions.IndentationSize, builder);
        AddOptionIfChanged(FormattingOptions2.NewLine, csharpFallbackOptions, editorConfigOptions.LineFormattingOptions.NewLine, builder);

        AddOptionIfChanged(ImplementTypeOptionsStorage.InsertionBehavior, csharpFallbackOptions, (ImplementTypeInsertionBehavior)editorConfigOptions.ImplementTypeOptions.InsertionBehavior, builder);
        AddOptionIfChanged(ImplementTypeOptionsStorage.PropertyGenerationBehavior, csharpFallbackOptions, (ImplementTypePropertyGenerationBehavior)editorConfigOptions.ImplementTypeOptions.PropertyGenerationBehavior, builder);

        return builder.ToImmutable();

        static void AddOptionIfChanged<T>(
            PerLanguageOption2<T> option,
            StructuredAnalyzerConfigOptions? analyzerConfigOptions,
            T value,
            ImmutableDictionary<string, string>.Builder builder)
        {
            var configName = option.Definition.ConfigName;
            var configValue = option.Definition.Serializer.Serialize(value);

            if (analyzerConfigOptions?.TryGetValue(configName, out var existingValue) == true &&
                existingValue == configValue)
            {
                return;
            }

            builder.Add(configName, configValue);
        }
    }
}
