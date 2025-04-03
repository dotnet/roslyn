// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.ContextProviders;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.ContextProviders;

[ExportLanguageService(typeof(ICopilotMethodCallersService), language: LanguageNames.CSharp), Shared]
internal class CSharpCopilotMethodCallersService : ICopilotMethodCallersService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCopilotMethodCallersService()
    {
    }

    public async Task<ISymbol?> GetContainingMethodSymbolAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return null;
        }

        return semanticModel.GetEnclosingSymbol<IMethodSymbol>(position, cancellationToken);
    }

    public async Task GetMethodCallersAsync(Document document, ISymbol symbol, Func<Document, TextSpan, CancellationToken, ValueTask> reporter, CancellationToken cancellationToken)
    {
        var context = new CopilotFindMethodCallersContext(reporter);
        await AbstractFindUsagesService.FindReferencesAsync(context, symbol, document.Project, FindReferencesSearchOptions.Default, DefaultClassificationOptionsProvider.Instance, cancellationToken).ConfigureAwait(false);
    }

    private class CopilotFindMethodCallersContext(Func<Document, TextSpan, CancellationToken, ValueTask> reporter) : FindUsagesContext
    {
        public override async ValueTask OnReferencesFoundAsync(IAsyncEnumerable<SourceReferenceItem> references, CancellationToken cancellationToken)
        {
            // For each source span in the definition, write it to the channel
            await foreach (var reference in references)
            {
                await reporter(reference.SourceSpan.Document, reference.SourceSpan.SourceSpan, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Provider that returns default classification options.  Classification is not relevant to this use case, so the values don't matter.
    /// </summary>
    private class DefaultClassificationOptionsProvider : OptionsProvider<ClassificationOptions>
    {
        public static OptionsProvider<ClassificationOptions> Instance { get; } = new DefaultClassificationOptionsProvider();
        public ValueTask<ClassificationOptions> GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        {
            return new ValueTask<ClassificationOptions>(ClassificationOptions.Default);
        }
    }
}
