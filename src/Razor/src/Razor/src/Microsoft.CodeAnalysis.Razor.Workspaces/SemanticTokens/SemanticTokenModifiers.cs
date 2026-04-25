// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor.SemanticTokens;

internal sealed class SemanticTokenModifiers
{
    private static readonly string s_razorCode = "razorCode";

    public int RazorCodeModifier => _modifierMap[s_razorCode];

    public string[] All { get; }

    private readonly Dictionary<string, int> _modifierMap;

    public SemanticTokenModifiers(string[] tokenModifiers)
    {
        var modifierMap = new Dictionary<string, int>();
        foreach (var modifier in tokenModifiers)
        {
            // Modifiers is a flags enum, so numeric values are powers of 2, and we skip 0
            modifierMap.Add(modifier, (int)Math.Pow(2, modifierMap.Count));
        }

        _modifierMap = modifierMap;

        All = tokenModifiers;
    }
}
