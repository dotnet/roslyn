// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal partial class Session
        {
            private struct FilterResult
            {
                public readonly CompletionItem CompletionItem;
                public readonly bool MatchedFilterText;
                public readonly string FilterText;

                public FilterResult(CompletionItem completionItem, string filterText, bool matchedFilterText)
                {
                    CompletionItem = completionItem;
                    MatchedFilterText = matchedFilterText;
                    FilterText = filterText;
                }
            }
        }
    }
}
