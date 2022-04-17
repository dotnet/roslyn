// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.ChangeNamespace;

[DataContract]
internal readonly record struct ChangeNamespaceOptions(
    [property: DataMember(Order = 0)] SyntaxFormattingOptions FormattingOptions,
    [property: DataMember(Order = 1)] AddImportPlacementOptions AddImportOptions,
    [property: DataMember(Order = 2)] SimplifierOptions SimplifierOptions)
{
    public static async ValueTask<ChangeNamespaceOptions> FromDocumentAsync(Document document, ChangeNamespaceOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var formattingOptions = await SyntaxFormattingOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
        var addImportsOptions = await AddImportPlacementOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
        var simplifierOptions = await SimplifierOptions.FromDocumentAsync(document, fallbackOptions?.SimplifierOptions, cancellationToken).ConfigureAwait(false);

        return new ChangeNamespaceOptions(formattingOptions, addImportsOptions, simplifierOptions);
    }

    public static ChangeNamespaceOptions GetDefault(HostLanguageServices languageServices)
    {
        var formattingOptions = languageServices.GetRequiredService<ISyntaxFormattingService>().DefaultOptions;
        var addImportsOptions = AddImportPlacementOptions.Default;
        var simplifierOptions = languageServices.GetRequiredService<ISimplificationService>().DefaultOptions;

        return new ChangeNamespaceOptions(formattingOptions, addImportsOptions, simplifierOptions);
    }

    public static ChangeNamespaceOptionsProvider CreateProvider(CodeActionOptionsProvider options)
        => new(languageServices => new ChangeNamespaceOptions(
            FormattingOptions: languageServices.GetRequiredService<ISyntaxFormattingService>().DefaultOptions,
            AddImportOptions: AddImportPlacementOptions.Default,
            SimplifierOptions: options(languageServices).SimplifierOptions ?? languageServices.GetRequiredService<ISimplificationService>().DefaultOptions));
}

internal delegate ChangeNamespaceOptions ChangeNamespaceOptionsProvider(HostLanguageServices languageServices);
