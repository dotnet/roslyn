// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.CommentSelection
{
    [Shared]
    [ExportLanguageService(typeof(ICommentSelectionService), LanguageNames.FSharp)]
    internal class FSharpCommentSelectionService : ICommentSelectionService
    {
        public Task<Document> FormatAsync(Document document, ImmutableArray<TextSpan> changes, CancellationToken cancellationToken)
        {
            return Task.FromResult(document);
        }

        public Task<CommentSelectionInfo> GetInfoAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return Task.FromResult(new CommentSelectionInfo(
                supportsSingleLineComment: true,
                supportsBlockComment: true,
                singleLineCommentString: "//",
                blockCommentStartString: "(*",
                blockCommentEndString: "*)"));
        }
    }
}
