// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.CodeCleanup;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration;

/// <summary>
/// Document-specific options for controlling the code produced by code generation.
/// </summary>
internal record CodeGenerationOptions
{
    /// <summary>
    /// Language agnostic defaults.
    /// </summary>
    internal static readonly CodeGenerationOptions CommonDefaults = new();

    [DataMember] public NamingStylePreferences NamingStyle { get; init; } = NamingStylePreferences.Default;

    private protected CodeGenerationOptions()
    {
    }

    private protected CodeGenerationOptions(IOptionsReader options, string language)
    {
        NamingStyle = options.GetOption(NamingStyleOptions.NamingPreferences, language);
    }

#if !CODE_STYLE
    public static CodeGenerationOptions GetDefault(LanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeGenerationService>().DefaultOptions;
#endif
}

[DataContract]
internal readonly record struct CodeAndImportGenerationOptions
{
    [DataMember]
    public required CodeGenerationOptions GenerationOptions { get; init; }

    [DataMember]
    public required AddImportPlacementOptions AddImportOptions { get; init; }

#if !CODE_STYLE
    internal static CodeAndImportGenerationOptions GetDefault(LanguageServices languageServices)
        => new()
        {
            GenerationOptions = CodeGenerationOptions.GetDefault(languageServices),
            AddImportOptions = AddImportPlacementOptions.Default
        };
#endif
}
