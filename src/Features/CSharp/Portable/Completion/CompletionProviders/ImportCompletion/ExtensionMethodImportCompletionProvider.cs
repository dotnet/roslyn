// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class ExtensionMethodImportCompletionProvider : AbstractExtensionMethodImportCompletionProvider
    {
        protected override string GenericSuffix => "<>";

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
            => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

        protected override ImmutableArray<string> GetImportedNamespaces(
            SyntaxNode location,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
            => ImportCompletionProviderHelper.GetImportedNamespaces(location, semanticModel, cancellationToken);

        protected override Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken)
            => ImportCompletionProviderHelper.CreateContextAsync(document, position, cancellationToken);
    }
}
