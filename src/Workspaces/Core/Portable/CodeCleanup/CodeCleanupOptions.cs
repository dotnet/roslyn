// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeCleanup;

[DataContract]
internal readonly record struct CodeCleanupOptions(
    [property: DataMember(Order = 0)] SyntaxFormattingOptions FormattingOptions,
    [property: DataMember(Order = 1)] SimplifierOptions SimplifierOptions,
    [property: DataMember(Order = 2)] AddImportPlacementOptions AddImportOptions)
{
    public static CodeCleanupOptions GetDefault(HostLanguageServices languageServices)
        => new(
            FormattingOptions: SyntaxFormattingOptions.GetDefault(languageServices),
            SimplifierOptions: SimplifierOptions.GetDefault(languageServices),
            AddImportOptions: AddImportPlacementOptions.Default);

    public static ValueTask<CodeCleanupOptions> GetDefaultAsync(HostLanguageServices language, CancellationToken _)
        => ValueTaskFactory.FromResult(GetDefault(language));

    public static CodeCleanupOptionsProvider CreateProvider(CodeActionOptionsProvider options)
        => new((languageServices, cancellationToken) => ValueTaskFactory.FromResult(options(languageServices).CleanupOptions ?? GetDefault(languageServices)));
}

internal delegate ValueTask<CodeCleanupOptions> CodeCleanupOptionsProvider(HostLanguageServices languageServices, CancellationToken cancellationToken);

internal static class CodeCleanupOptionsProviders
{
    public static async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, CodeCleanupOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions?.FormattingOptions, cancellationToken).ConfigureAwait(false);
        var simplifierOptions = await document.GetSimplifierOptionsAsync(fallbackOptions?.SimplifierOptions, cancellationToken).ConfigureAwait(false);
        var addImportOptions = await document.GetAddImportPlacementOptionsAsync(fallbackOptions?.AddImportOptions, cancellationToken).ConfigureAwait(false);
        return new CodeCleanupOptions(formattingOptions, simplifierOptions, addImportOptions);
    }

    public static async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, CodeActionOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await GetCodeCleanupOptionsAsync(document, fallbackOptionsProvider(document.Project.LanguageServices).CleanupOptions, cancellationToken).ConfigureAwait(false);

    public static async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, CodeCleanupOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await GetCodeCleanupOptionsAsync(document, await fallbackOptionsProvider(document.Project.LanguageServices, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
}

