// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Indentation;

namespace Microsoft.CodeAnalysis.Wrapping
{
    /// <summary>
    /// Base type for all wrappers that involve wrapping a comma-separated list of items.
    /// </summary>
    internal abstract partial class AbstractSeparatedListWrapper<
        TListSyntax,
        TListItemSyntax>
        : AbstractSyntaxWrapper
        where TListSyntax : SyntaxNode
        where TListItemSyntax : SyntaxNode
    {
        protected abstract string Unwrap_list { get; }
        protected abstract string Wrap_long_list { get; }
        protected abstract string Unwrap_all_items { get; }
        protected abstract string Indent_all_items { get; }
        protected abstract string Wrap_every_item { get; }

        public AbstractSeparatedListWrapper(IIndentationService indentationService)
            : base(indentationService)
        {
        }
    }
}
