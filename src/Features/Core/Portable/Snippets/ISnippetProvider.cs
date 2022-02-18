// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal interface ISnippetProvider
    {
        Task<SnippetData?> GetSnippetDataAsync(Document document, int position, CancellationToken cancellationToken);
        Task<Snippet> GetSnippetAsync(Document document, TextSpan span, int tokenSpanStart, int tokenSpanEnd, CancellationToken cancellationToken);
        string GetSnippetText();
    }
}
