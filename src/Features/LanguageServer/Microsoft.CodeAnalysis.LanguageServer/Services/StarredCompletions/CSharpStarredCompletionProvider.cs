// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.StarredSuggestions;

[ExportCompletionProvider("CSharpStarredCompletionProvider", LanguageNames.CSharp), Shared]
internal class CSharpStarredCompletionProvider : CompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpStarredCompletionProvider() { }

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var provider = await StarredCompletionAssemblyHelper.GetCompletionProviderAsync(context.CancellationToken);
        if (provider == null)
        {
            return; //no-op if provider cannot be retrieved from assembly
        }
        await provider.ProvideCompletionsAsync(context);
    }

    public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
    {
        var provider = await StarredCompletionAssemblyHelper.GetCompletionProviderAsync(cancellationToken);
        if (provider == null)
        {
            throw ExceptionUtilities.Unreachable(); //unreachable since ProvideCompletionsAsync must have completed successfully for GetChangeAsync to be called
        }
        return await provider.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
    }
}
