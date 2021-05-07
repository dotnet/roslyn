// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ValueTracking
{
    [DataContract]
    internal sealed class SerializableValueTrackedItem
    {
        [DataMember(Order = 0)]
        public SerializableSymbolAndProjectId SymbolAndProjectId { get; }

        [DataMember(Order = 1)]
        public SerializableValueTrackedItem? Parent { get; }

        [DataMember(Order = 2)]
        public TextSpan TextSpan { get; }

        [DataMember(Order = 3)]
        public DocumentId DocumentId { get; }

        public SerializableValueTrackedItem(
            SerializableSymbolAndProjectId symbolAndProjectId,
            TextSpan textSpan,
            DocumentId documentId,
            SerializableValueTrackedItem? parent = null)
        {
            SymbolAndProjectId = symbolAndProjectId;
            Parent = parent;
            TextSpan = textSpan;
            DocumentId = documentId;
        }

        public static SerializableValueTrackedItem Dehydrate(ValueTrackedItem valueTrackedItem, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbolAndProjectId = SerializableSymbolAndProjectId.Dehydrate(valueTrackedItem.Document.Project.Solution, valueTrackedItem.Symbol, cancellationToken);
            var parent = valueTrackedItem.Parent is null
                ? null
                : Dehydrate(valueTrackedItem.Parent, cancellationToken);

            return new SerializableValueTrackedItem(symbolAndProjectId, valueTrackedItem.Span, valueTrackedItem.Document.Id, parent);
        }

        public async Task<ValueTrackedItem?> RehydrateAsync(Solution solution, CancellationToken cancellationToken)
        {
            var document = solution.GetRequiredDocument(DocumentId);

            var symbol = await SymbolAndProjectId.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
            if (symbol is null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var parent = Parent is null ? null : await Parent.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);

            return await ValueTrackedItem.TryCreateAsync(document, TextSpan, symbol, parent, cancellationToken).ConfigureAwait(false);
        }
    }
}
