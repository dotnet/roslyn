// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
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

    public void ProcessSection(Section section, IReadOnlyDictionary<string, (string value, TextLine? line)> properties)
    {
        foreach (var ruleTitle in GetRuleTitles(properties))
        {
            if (TryGetSymbolSpec(section, ruleTitle, properties, out var applicableSymbolInfo) &&
                TryGetNamingStyleData(section, ruleTitle, properties, out var namingScheme) &&
                TryGetRuleSeverity(ruleTitle, properties, out var severity))
            {
                _rules ??= ArrayBuilder<NamingStyleOption>.GetInstance();
                _rules.Add(new NamingStyleOption(
                    Section: section,
                    RuleName: (section, severity.line?.Span, ruleTitle), // all rules must have a severity so we consider this its location
                    ApplicableSymbolInfo: applicableSymbolInfo,
                    NamingScheme: namingScheme,
                    Severity: (section, severity.line?.Span, severity.severity)));
            }
        }
    }
}
