// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    internal abstract class AbstractCommentUncommentService : ICommentUncommentService
    {
        public abstract string BlockCommentEndString { get; }
        public abstract string BlockCommentStartString { get; }
        public abstract string SingleLineCommentString { get; }
        public abstract bool SupportsBlockComment { get; }

        public Document Format(Document document, IEnumerable<TextSpan> changes, CancellationToken cancellationToken)
        {
            var snapshot = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken).FindCorrespondingEditorTextSnapshot();

            var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var formattingSpans = changes.Select(s => s.ToSnapshotSpan(snapshot))
                                         .Select(s => CommonFormattingHelpers.GetFormattingSpan(root, s.Span.ToTextSpan()));

            return Formatter.FormatAsync(document, formattingSpans, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
        }
    }
}
