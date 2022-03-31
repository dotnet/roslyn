﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal interface ISyntaxFormatting
    {
        SyntaxFormattingOptions GetFormattingOptions(AnalyzerConfigOptions options);
        ImmutableArray<AbstractFormattingRule> GetDefaultFormattingRules();
        IFormattingResult GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan>? spans, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken);
    }

    internal abstract partial class SyntaxFormattingOptions
    {
        public readonly bool UseTabs;
        public readonly int TabSize;
        public readonly int IndentationSize;
        public readonly string NewLine;

        public readonly bool SeparateImportDirectiveGroups;

        protected SyntaxFormattingOptions(
            bool useTabs,
            int tabSize,
            int indentationSize,
            string newLine,
            bool separateImportDirectiveGroups)
        {
            UseTabs = useTabs;
            TabSize = tabSize;
            IndentationSize = indentationSize;
            NewLine = newLine;
            SeparateImportDirectiveGroups = separateImportDirectiveGroups;
        }
    }
}
