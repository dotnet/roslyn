using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Indentation;

namespace Microsoft.CodeAnalysis.Wrapping
{
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

        public AbstractSeparatedListWrapper(IIndentationService indentationService) : base(indentationService)
        {
        }
    }
}
