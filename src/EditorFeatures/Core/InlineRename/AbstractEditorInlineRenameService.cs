// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractEditorInlineRenameService : IEditorInlineRenameService
{
    private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;

    protected AbstractEditorInlineRenameService(IEnumerable<IRefactorNotifyService> refactorNotifyServices)
    {
        _refactorNotifyServices = refactorNotifyServices;
    }

    public bool IsEnabled => true;

    public async Task<IInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var symbolicInfo = await SymbolicRenameInfo.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (symbolicInfo.LocalizedErrorMessage != null)
            return new FailureInlineRenameInfo(symbolicInfo.LocalizedErrorMessage);

        return new SymbolInlineRenameInfo(
            _refactorNotifyServices, symbolicInfo, cancellationToken);
    }

    public virtual Task<ImmutableDictionary<string, ImmutableArray<string>>> GetRenameContextAsync(IInlineRenameInfo inlineRenameInfo, IInlineRenameLocationSet inlineRenameLocationSet, CancellationToken cancellationToken)
    {
        return Task.FromResult(ImmutableDictionary<string, ImmutableArray<string>>.Empty);
    }

    /// <summary>
    /// Returns the <see cref="TextSpan"/> of the nearest encompassing <see cref="SyntaxNode"/> of type
    /// <typeparamref name="T"/> of which the supplied <paramref name="textSpan"/> is a part within the supplied
    /// <paramref name="document"/>.
    /// </summary>
    protected static async Task<TextSpan?> TryGetSurroundingNodeSpanAsync<T>(
        Document document,
        TextSpan textSpan,
        CancellationToken cancellationToken)
            where T : SyntaxNode
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return null;
        }

        var containingNode = root.FindNode(textSpan);
        var targetNode = containingNode.FirstAncestorOrSelf<T>() ?? containingNode;

        return targetNode.Span;
    }
}
