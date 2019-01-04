// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    /// <summary>
    /// Bridge between the new <see cref="ICommentSelectionService"/> and an existing
    /// language which only supplies the old <see cref="ICommentUncommentService"/> service.
    /// </summary>
    internal class CommentSelectionServiceProxy : ICommentSelectionService
    {
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly ICommentUncommentService _commentUncommentService;

        public CommentSelectionServiceProxy(ICommentUncommentService commentUncommentService)
        {
            _commentUncommentService = commentUncommentService;
        }
#pragma warning restore CS0618 // Type or member is obsolete

        public Task<CommentSelectionInfo> GetInfoAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
            => Task.FromResult(new CommentSelectionInfo(
                    true, _commentUncommentService.SupportsBlockComment, _commentUncommentService.SingleLineCommentString,
                    _commentUncommentService.BlockCommentStartString, _commentUncommentService.BlockCommentEndString));

        public Task<Document> FormatAsync(Document document, ImmutableArray<TextSpan> changes, CancellationToken cancellationToken)
            => Task.FromResult(_commentUncommentService.Format(document, changes, cancellationToken));
    }
}
