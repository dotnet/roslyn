﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CommentSelection
{
    internal interface ICommentSelectionService : ILanguageService
    {
        Task<CommentSelectionInfo> GetInfoAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);

        Task<Document> FormatAsync(Document document, ImmutableArray<TextSpan> changes, CancellationToken cancellationToken);
    }
}
