// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets
{
    internal abstract class AbstractSnippetCompletionProvider : CommonCompletionProvider
    {
        public AbstractSnippetCompletionProvider()
        {

        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {

        }

        private async Task<Document> DetermineNewDocumentAsync(Document document, CompletionItem completionItem, CancellationToken cancellationToken)
        {

        }
    }
}
