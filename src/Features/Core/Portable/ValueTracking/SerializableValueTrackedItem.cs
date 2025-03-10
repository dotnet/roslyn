// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ValueTracking;

[DataContract]
internal sealed class SerializableValueTrackedItem(
    SymbolKey symbolKey,
    TextSpan textSpan,
    DocumentId documentId,
    SerializableValueTrackedItem? parent = null)
{
    [DataMember(Order = 0)]
    public SymbolKey SymbolKey { get; } = symbolKey;

    [DataMember(Order = 1)]
    public TextSpan TextSpan { get; } = textSpan;

    [DataMember(Order = 2)]
    public DocumentId DocumentId { get; } = documentId;

    [DataMember(Order = 3)]
    public SerializableValueTrackedItem? Parent { get; } = parent;

    public static SerializableValueTrackedItem Dehydrate(Solution solution, ValueTrackedItem valueTrackedItem, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var parent = valueTrackedItem.Parent is null
            ? null
            : Dehydrate(solution, valueTrackedItem.Parent, cancellationToken);

        return new SerializableValueTrackedItem(valueTrackedItem.SymbolKey, valueTrackedItem.Span, valueTrackedItem.DocumentId, parent);
    }

    public async ValueTask<ValueTrackedItem> RehydrateAsync(Solution solution, CancellationToken cancellationToken)
    {
        var document = solution.GetRequiredDocument(DocumentId);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbolResolution = SymbolKey.Resolve(semanticModel.Compilation, cancellationToken: cancellationToken);
        Contract.ThrowIfNull(symbolResolution.Symbol);

        cancellationToken.ThrowIfCancellationRequested();
        var parent = Parent is null ? null : await Parent.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);

        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var sourceText = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

        return new ValueTrackedItem(SymbolKey, sourceText, TextSpan, DocumentId, symbolResolution.Symbol.GetGlyph(), parent);
    }
}
