﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
