// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ValueTracking
{
    [DataContract]
    internal sealed class SerializableValueTrackedItem
    {
        [DataMember(Order = 0)]
        public SymbolKey SymbolKey { get; }

        [DataMember(Order = 1)]
        public TextSpan TextSpan { get; }

        [DataMember(Order = 2)]
        public DocumentId DocumentId { get; }

        [DataMember(Order = 3)]
        public SerializableValueTrackedItem? Parent { get; }

        public SerializableValueTrackedItem(
            SymbolKey symbolKey,
            TextSpan textSpan,
            DocumentId documentId,
            SerializableValueTrackedItem? parent = null)
        {
            SymbolKey = symbolKey;
            Parent = parent;
            TextSpan = textSpan;
            DocumentId = documentId;
        }

        public static SerializableValueTrackedItem Dehydrate(Solution solution, ValueTrackedItem valueTrackedItem, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parent = valueTrackedItem.Parent is null
                ? null
                : Dehydrate(solution, valueTrackedItem.Parent, cancellationToken);

            return new SerializableValueTrackedItem(valueTrackedItem.SymbolKey, valueTrackedItem.Span, valueTrackedItem.DocumentId, parent);
        }

        public async Task<ValueTrackedItem?> RehydrateAsync(Solution solution, CancellationToken cancellationToken)
        {
            var document = solution.GetRequiredDocument(DocumentId);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbolResolution = SymbolKey.Resolve(semanticModel.Compilation, cancellationToken: cancellationToken);

            if (symbolResolution.Symbol is null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var parent = Parent is null ? null : await Parent.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);

            return await ValueTrackedItem.TryCreateAsync(document, TextSpan, symbolResolution.Symbol, parent, cancellationToken).ConfigureAwait(false);
        }
    }
}
