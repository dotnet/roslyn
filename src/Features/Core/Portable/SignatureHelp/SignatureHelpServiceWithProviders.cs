// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp;

/// <summary>
/// Base class for <see cref="SignatureHelpService"/>'s that delegate to <see cref="ISignatureHelpProvider"/>'s.
/// </summary>
internal abstract class SignatureHelpServiceWithProviders : SignatureHelpService
{
    private readonly LanguageServices _services;
    private ImmutableArray<ISignatureHelpProvider> _providers;

    protected SignatureHelpServiceWithProviders(LanguageServices services)
    {
        _services = services;
    }

    private ImmutableArray<ISignatureHelpProvider> GetProviders()
    {
        if (_providers.IsDefault)
        {
            var mefExporter = _services.SolutionServices.ExportProvider;

            var providers = ExtensionOrderer
                .Order(mefExporter.GetExports<ISignatureHelpProvider, OrderableLanguageMetadata>()
                    .Where(lz => lz.Metadata.Language == _services.Language))
                .Select(lz => lz.Value)
                .ToImmutableArray();

            ImmutableInterlocked.InterlockedCompareExchange(ref _providers, providers, default);
        }

        return _providers;
    }

    public override Task<(ISignatureHelpProvider? provider, SignatureHelpItems? bestItems)> GetSignatureHelpAsync(
        Document document,
        int position,
        SignatureHelpTriggerInfo triggerInfo,
        SignatureHelpOptions options,
        CancellationToken cancellationToken)
    {
        return GetSignatureHelpAsync(
            GetProviders(),
            document,
            position,
            triggerInfo,
            options,
            cancellationToken);
    }
}

