// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CommentSelection
{
    internal abstract class AbstractCommentSelectionService : ICommentSelectionService
    {
        public abstract string BlockCommentEndString { get; }
        public abstract string BlockCommentStartString { get; }
        public abstract string SingleLineCommentString { get; }
        public abstract bool SupportsBlockComment { get; }

        public Task<Document> FormatAsync(Document document, ImmutableArray<TextSpan> changes, SyntaxFormattingOptions formattingOptions, CancellationToken cancellationToken)
        {
            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var formattingSpans = changes.Select(s => CommonFormattingHelpers.GetFormattingSpan(root, s));

            return Formatter.FormatAsync(document, formattingSpans, formattingOptions, rules: null, cancellationToken);
        }

        public Task<CommentSelectionInfo> GetInfoAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
            => Task.FromResult(SupportsBlockComment
                ? new CommentSelectionInfo(true, SupportsBlockComment, SingleLineCommentString, BlockCommentStartString, BlockCommentEndString)
                : new CommentSelectionInfo(true, SupportsBlockComment, SingleLineCommentString, "", ""));
    }
}
