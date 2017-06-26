// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
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

        public CommentSelectionInfo GetInfo(SourceText sourceText, TextSpan textSpan)
            => new CommentSelectionInfo(true, _commentUncommentService.SupportsBlockComment, _commentUncommentService.SingleLineCommentString, _commentUncommentService.BlockCommentStartString, _commentUncommentService.BlockCommentEndString);

        public Document Format(Document document, ImmutableArray<TextSpan> changes, CancellationToken cancellationToken)
            => _commentUncommentService.Format(document, changes, cancellationToken);
    }
}