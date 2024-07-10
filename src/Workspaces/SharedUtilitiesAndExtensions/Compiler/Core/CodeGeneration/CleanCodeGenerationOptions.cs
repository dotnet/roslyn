// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration;

[DataContract]
internal readonly record struct CleanCodeGenerationOptions
{
    [DataMember]
    public required CodeGenerationOptions GenerationOptions { get; init; }

    [DataMember]
    public required CodeCleanupOptions CleanupOptions { get; init; }

#if !CODE_STYLE
    public static CleanCodeGenerationOptions GetDefault(LanguageServices languageServices)
        => new()
        {
            GenerationOptions = CodeGenerationOptions.GetDefault(languageServices),
            CleanupOptions = CodeCleanupOptions.GetDefault(languageServices)
        };

    public CodeAndImportGenerationOptions CodeAndImportGenerationOptions
        => new()
        {
            GenerationOptions = GenerationOptions,
            AddImportOptions = CleanupOptions.AddImportOptions
        };
#endif
}

#if !CODE_STYLE
internal static class CleanCodeGenerationOptionsProviders
{
    public static async ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(this Document document, CancellationToken cancellationToken)
        => new()
        {
            GenerationOptions = await document.GetCodeGenerationOptionsAsync(cancellationToken).ConfigureAwait(false),
            CleanupOptions = await document.GetCodeCleanupOptionsAsync(cancellationToken).ConfigureAwait(false)
        };
}
#endif
