// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.ContextProviders;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.ContextProviders;

[ExportLanguageService(typeof(ICopilotOverrideImplementationService), language: LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CSharpCopilotOverrideImplementationService() : ICopilotOverrideImplementationService
{
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

    private class CopilotOverrideImplementationFindUsagesContext(Func<Document, TextSpan, CancellationToken, ValueTask> reporter) : FindUsagesContext
    {
        public override async ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
        {
            // For each source span in the definition, write it to the channel
            foreach (var sourceSpan in definition.SourceSpans)
            {
                await reporter(sourceSpan.Document, sourceSpan.SourceSpan, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task FindOverrideImplementationsAsync(Document document, ISymbol member, Func<Document, TextSpan, CancellationToken, ValueTask> reporter, CancellationToken cancellationToken)
    {
        var context = new CopilotOverrideImplementationFindUsagesContext(reporter);
        await AbstractFindUsagesService.FindImplementationsAsync(context, member, document.Project, DefaultClassificationOptionsProvider.Instance, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<ISymbol>> GetPotentialOverridesAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var containingType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(position, cancellationToken);
        if (containingType == null)
        {
            return [];
        }

        var overrideableMembers = containingType.GetOverridableMembers(cancellationToken);
        return overrideableMembers;
    }
}
