// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpFormattingAnalyzer
        : AbstractFormattingAnalyzer
    {
        protected override OptionSet ApplyFormattingOptions(OptionSet optionSet, ICodingConventionContext codingConventionContext)
        {
            return optionSet.WithChangedOption(CSharpFormattingOptions.IndentBlock, GetBoolOrDefault(codingConventionContext.CurrentConventions, "csharp_indent_block_contents", CSharpFormattingOptions.IndentBlock.DefaultValue));
        }

        private bool GetBoolOrDefault(ICodingConventionsSnapshot currentConventions, string key, bool defaultValue)
        {
            if (currentConventions.TryGetConventionValue(key, out string rawValue)
                && bool.TryParse(rawValue, out var value))
            {
                return value;
            }

            return defaultValue;
        }
    }
}
