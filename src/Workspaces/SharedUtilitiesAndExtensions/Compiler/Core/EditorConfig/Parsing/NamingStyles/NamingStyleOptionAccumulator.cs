// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.EditorConfigNamingStyleParser;

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles;

internal sealed class NamingStyleOptionAccumulator : IEditorConfigOptionAccumulator<EditorConfigNamingStyles, NamingStyleOption>
{
    private ArrayBuilder<NamingStyleOption>? _rules;

    public EditorConfigNamingStyles Complete(string? fileName)
    {
        var editorConfigNamingStyles = new EditorConfigNamingStyles(fileName, _rules.ToImmutableOrEmptyAndFree());
        _rules = null;
        return editorConfigNamingStyles;
    }

    public void ProcessSection(Section section, IReadOnlyDictionary<string, string> values, IReadOnlyDictionary<string, TextLine> lines)
    {
        foreach (var ruleTitle in GetRuleTitles(values))
        {
            if (TryGetSymbolSpecification(section, ruleTitle, values, lines, out var applicableSymbolInfo) &&
                TryGetNamingStyle(section, ruleTitle, values, lines, out var namingScheme) &&
                TryGetRule(section, ruleTitle, applicableSymbolInfo, namingScheme, values, lines, out var rule, out _))
            {
                _rules ??= ArrayBuilder<NamingStyleOption>.GetInstance();
                _rules.Add(rule);
            }
        }
    }
}
