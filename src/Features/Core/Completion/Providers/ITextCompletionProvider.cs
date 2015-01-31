// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal interface ITextCompletionProvider : ICompletionProvider
    {
        /// <summary>
        /// Returns a CompletionItemGroup for the specified position in the text.
        /// </summary>
        CompletionItemGroup GetGroup(SourceText text, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Called if another completion provider has provided a completion group to give this
        /// provider an opportunity to add elements to that group, even if it would not have
        /// produced elements otherwise.
        /// </summary>
        CompletionItemGroup GetAugmentGroup(SourceText text, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken = default(CancellationToken));
    }
}
