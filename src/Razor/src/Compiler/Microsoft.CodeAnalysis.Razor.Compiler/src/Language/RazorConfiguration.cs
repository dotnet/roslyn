// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed record class RazorConfiguration(
    RazorLanguageVersion LanguageVersion,
    string ConfigurationName,
    ImmutableArray<RazorExtension> Extensions,
    LanguageVersion CSharpLanguageVersion = LanguageVersion.Default,
    bool UseConsolidatedMvcViews = true,
    bool SuppressAddComponentParameter = false,
    bool UseRoslynTokenizer = false,
    ImmutableArray<string> PreprocessorSymbols = default,
    int RazorWarningLevel = 0)
{
    public ImmutableArray<string> PreprocessorSymbols
    {
        get;
        init => field = value.NullToEmpty();
    } = PreprocessorSymbols.NullToEmpty();

    public static readonly RazorConfiguration Default = new(
        RazorLanguageVersion.Latest,
        ConfigurationName: "unnamed",
        Extensions: [],
        CSharpLanguageVersion: CodeAnalysis.CSharp.LanguageVersion.Default,
        UseConsolidatedMvcViews: true,
        SuppressAddComponentParameter: false,
        UseRoslynTokenizer: false,
        PreprocessorSymbols: []);

    public bool Equals(RazorConfiguration? other)
        => other is not null &&
           LanguageVersion == other.LanguageVersion &&
           ConfigurationName == other.ConfigurationName &&
           CSharpLanguageVersion == other.CSharpLanguageVersion &&
           SuppressAddComponentParameter == other.SuppressAddComponentParameter &&
           UseConsolidatedMvcViews == other.UseConsolidatedMvcViews &&
           UseRoslynTokenizer == other.UseRoslynTokenizer &&
           RazorWarningLevel == other.RazorWarningLevel &&
           PreprocessorSymbols.SequenceEqual(other.PreprocessorSymbols) &&
           Extensions.SequenceEqual(other.Extensions);

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();
        hash.Add(LanguageVersion);
        hash.Add(ConfigurationName);
        hash.Add(CSharpLanguageVersion);
        hash.Add(Extensions);
        hash.Add(SuppressAddComponentParameter);
        hash.Add(UseConsolidatedMvcViews);
        hash.Add(UseRoslynTokenizer);
        hash.Add(RazorWarningLevel);
        hash.Add(PreprocessorSymbols);
        return hash;
    }
}
