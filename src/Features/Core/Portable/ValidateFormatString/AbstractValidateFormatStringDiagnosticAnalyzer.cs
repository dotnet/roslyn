// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.ValidateFormatString
{
    internal abstract class AbstractValidateFormatStringDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        public const string DiagnosticId = IDEDiagnosticIds.ValidateFormatStringDiagnosticID;
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(FeaturesResources.Invalid_format_string), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(FeaturesResources.Format_string_0_contains_invalid_placeholder_1), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(FeaturesResources.Invalid_format_string), FeaturesResources.ResourceManager, typeof(FeaturesResources));

        protected static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            DiagnosticCategory.Compiler,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public bool OpenFileOnly(Workspace workspace) => false;

        protected static bool TryGetFormatMethod(SyntaxNodeAnalysisContext context, int numberOfArguments, SymbolInfo symbolInfo, out IMethodSymbol method)
        {
            method = null;

            if (symbolInfo.Symbol == null)
            {
                return false;
            }

            if (!symbolInfo.Symbol.ContainingSymbol.Equals(context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(string).FullName)))
            {
                return false;
            }

            if (symbolInfo.Symbol.Kind != SymbolKind.Method)
            {
                return false;
            }

            method = (IMethodSymbol)symbolInfo.Symbol;
            if (method == null)
            {
                return false;
            }

            // If the chosen overload ends with a params object[] but there are the same number of
            // arguments and parameters, then an actual object[] must be being passed instead of
            // passing one argument at a time. This object[] may have been created somewhere we
            // can't analyze, so never squiggle in this case.
            if (method.Parameters[method.Parameters.Length - 1].IsParams && numberOfArguments == method.Parameters.Length)
            {
                return false;
            }

            return true;
        }

        private static string RemoveEscapedBrackets(string formatString)
        {
            var removeEscapedBracketsRegex = new Regex("{{|}}");
            var modifiedFormatString = removeEscapedBracketsRegex.Replace(formatString, "  ");
            return modifiedFormatString;
        }

        protected static void ValidateAndReportDiagnostic(SyntaxNodeAnalysisContext context, bool hasIFormatProvider, int numberOfArguments, string formatString, Location formatStringLocation)
        {
            // The maximum expected placeholder value depends on the number of arguments passed.
            // All overloads have a format string argument and possibly an IFormatProvider argument
            // so the maximum expected placeholder value is calculated accordingly.
            var maxExpectedPlaceholder = hasIFormatProvider ? numberOfArguments - 3 : numberOfArguments - 2;

            if (!AllPlaceholdersInRange(formatString, maxExpectedPlaceholder, out var invalidPlaceholderText))
            {
                var diagnostic = Diagnostic.Create(Rule, formatStringLocation, formatString.Replace("\n", "\\n").Replace("\r", "\\r"), invalidPlaceholderText);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool AllPlaceholdersInRange(string formatString, int maxExpectedPlaceholder, out string invalidPlaceholder)
        {
            var inRange = true;
            var textInsideBrackets = "";
            string placeholderText_NumberOnly;

            var formatStringWithEscapedBracketsRemoved = RemoveEscapedBrackets(formatString);

            MatchCollection matches = Regex.Matches(formatStringWithEscapedBracketsRemoved, "{(.*?)}");
            foreach (Match match in matches)
            {
                textInsideBrackets = match.Groups[1].Value;

                if (textInsideBrackets.IndexOf(",") > 0)
                {
                    placeholderText_NumberOnly = textInsideBrackets.Split(',')[0];
                }
                else
                {
                    placeholderText_NumberOnly = textInsideBrackets.Split(':')[0];
                }

                if (textInsideBrackets.Length > 0 && char.IsWhiteSpace(textInsideBrackets, 0))
                {
                    inRange = false;
                    break;
                }

                var parsed = int.TryParse(placeholderText_NumberOnly, out var placeholderNumber);
                if (!parsed)
                {
                    inRange = false;
                    break;
                }

                if (placeholderNumber > maxExpectedPlaceholder)
                {
                    inRange = false;
                    break;
                }
            }

            invalidPlaceholder = "{" + textInsideBrackets + "}";
            return inRange;
        }
    }
}
