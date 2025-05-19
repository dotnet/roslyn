// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;
using Microsoft.CodeAnalysis.SemanticSearch;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.SemanticSearch;

[Export(typeof(ISemanticSearchCopilotService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SemanticSearchCopilotServiceWrapper(
    [Import(AllowDefault = true)] Lazy<ISemanticSearchCopilotServiceImpl>? impl) : ISemanticSearchCopilotService
{
    bool ISemanticSearchCopilotService.IsAvailable
        => impl != null;

    async ValueTask<SemanticSearchCopilotGeneratedQuery> ISemanticSearchCopilotService.TryGetQueryAsync(string text, SemanticSearchCopilotContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(impl);

        var result = await impl.Value.TryGetQueryAsync(
            text,
            new SemanticSearchCopilotContextImpl()
            {
                ModelName = context.ModelName,
                AvailablePackages = context.AvailablePackages,
            },
            cancellationToken).ConfigureAwait(false);

        return new SemanticSearchCopilotGeneratedQuery()
        {
            IsError = result.IsError,
            Text = result.Text,
        };
    }
}
