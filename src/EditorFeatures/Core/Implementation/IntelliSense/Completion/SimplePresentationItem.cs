// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal class SimplePresentationItem : PresentationItem
    {
        public override CompletionItem Item { get; }
        public override bool IsSuggestionModeItem { get; }
        public override CompletionService CompletionService { get; }

        public SimplePresentationItem(CompletionItem item, CompletionService completionService, bool isSuggestionModeItem = false)
        {
            Debug.Assert(item != null);
            Debug.Assert(completionService != null);

            this.Item = item;
            this.CompletionService = completionService;
            this.IsSuggestionModeItem = isSuggestionModeItem;
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CancellationToken cancellationToken)
        {
            return this.CompletionService.GetDescriptionAsync(document, this.Item, cancellationToken);
        }
    }
}
