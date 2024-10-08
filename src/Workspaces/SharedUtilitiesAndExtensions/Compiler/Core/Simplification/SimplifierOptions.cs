// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Simplification;

internal record class SimplifierOptions
{
    public static readonly CodeStyleOption2<bool> DefaultQualifyAccess = CodeStyleOption2.FalseWithSilentEnforcement;

    /// <summary>
    /// Language agnostic defaults.
    /// </summary>
    internal static readonly SimplifierOptions CommonDefaults = new();

    [DataMember] public CodeStyleOption2<bool> QualifyFieldAccess { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> QualifyPropertyAccess { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> QualifyMethodAccess { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> QualifyEventAccess { get; init; } = CodeStyleOption2.FalseWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInMemberAccess { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;
    [DataMember] public CodeStyleOption2<bool> PreferPredefinedTypeKeywordInDeclaration { get; init; } = CodeStyleOption2.TrueWithSilentEnforcement;

    private protected SimplifierOptions()
    {
    }

    private protected SimplifierOptions(IOptionsReader options, string language)
    {
        QualifyFieldAccess = options.GetOption(CodeStyleOptions2.QualifyFieldAccess, language);
        QualifyPropertyAccess = options.GetOption(CodeStyleOptions2.QualifyPropertyAccess, language);
        QualifyMethodAccess = options.GetOption(CodeStyleOptions2.QualifyMethodAccess, language);
        QualifyEventAccess = options.GetOption(CodeStyleOptions2.QualifyEventAccess, language);
        PreferPredefinedTypeKeywordInMemberAccess = options.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, language);
        PreferPredefinedTypeKeywordInDeclaration = options.GetOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, language);
    }

    public bool TryGetQualifyMemberAccessOption(SymbolKind symbolKind, [NotNullWhen(true)] out CodeStyleOption2<bool>? option)
    {
        option = symbolKind switch
        {
            SymbolKind.Field => QualifyFieldAccess,
            SymbolKind.Property => QualifyPropertyAccess,
            SymbolKind.Method => QualifyMethodAccess,
            SymbolKind.Event => QualifyEventAccess,
            _ => null,
        };

        return option != null;
    }
}
