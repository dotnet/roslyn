// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS1012 // Start action has no registered actions.

namespace Analyzer.Utilities
{
    internal static class AnalyzerOptionsExtensions
    {
        public static SymbolVisibilityGroup GetSymbolVisibilityGroupOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SymbolVisibilityGroup defaultValue,
            CancellationToken cancellationToken)
            => options.GetEnumOptionValue("api_surface", rule, defaultValue, cancellationToken);

        private static TEnum GetEnumOptionValue<TEnum>(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            TEnum defaultValue,
            CancellationToken cancellationToken)
            where TEnum: struct
        {
            var analyzerConfigOptions = options.GetCategorizedAnalyzerConfigOptions(cancellationToken);
            return analyzerConfigOptions.GetEnumOptionValue(optionName, rule, defaultValue);
        }

        private static CategorizedAnalyzerConfigOptions GetCategorizedAnalyzerConfigOptions(
            this AnalyzerOptions options,
            CancellationToken cancellationToken)
        {
            foreach (var additionalFile in options.AdditionalFiles)
            {
                if (additionalFile.Path.EndsWith(".editorconfig", StringComparison.OrdinalIgnoreCase))
                {
                    var text = additionalFile.GetText(cancellationToken);
                    return EditorConfigParser.Parse(text);
                }
            }

            return CategorizedAnalyzerConfigOptions.Empty;
        }
    }
}
