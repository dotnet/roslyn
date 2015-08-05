// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class TextCompletionProvider : CompletionListProvider
    {
        /// <summary>
        /// Returns a CompletionItemGroup for the specified position in the text.
        /// </summary>
        public abstract CompletionList GetCompletionList(SourceText text, int position, CompletionTrigger trigger, CancellationToken cancellationToken = default(CancellationToken));
    }
}
