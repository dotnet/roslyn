// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;

internal interface ICopilotSemanticSearchResultsObserver
{
    ValueTask OnUserCodeExceptionAsync(UserCodeExceptionInfo exception, CancellationToken cancellationToken);

    ValueTask OnSymbolFoundAsync(Solution solution, ISymbol symbol, CancellationToken cancellationToken);
    ValueTask OnSyntaxNodeFoundAsync(Document document, SyntaxNode node, CancellationToken cancellationToken);
    ValueTask OnLocationFoundAsync(Solution solution, Location location, CancellationToken cancellationToken);
    ValueTask OnValueFoundAsync(Solution solution, object value, CancellationToken cancellationToken);

    ValueTask OnDocumentUpdatedAsync(DocumentId documentId, ImmutableArray<TextChange> changes, CancellationToken cancellationToken);
    ValueTask OnTextFileUpdatedAsync(string filePath, string? newContent, CancellationToken cancellationToken);
    ValueTask OnLogMessageAsync(string message, CancellationToken cancellationToken);

    ValueTask AddItemsAsync(int itemCount, CancellationToken cancellationToken);
    ValueTask ItemsCompletedAsync(int itemCount, CancellationToken cancellationToken);

    internal readonly record struct UserCodeExceptionInfo(
        string ProjectDisplayName,
        string Message,
        ImmutableArray<TaggedText> TypeName,
        ImmutableArray<TaggedText> StackTrace,
        TextSpan Span);
}
