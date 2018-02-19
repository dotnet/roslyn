// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class CompletionItemEventArgs : EventArgs
    {
        public CompletionItem CompletionItem { get; }

        public CompletionItemEventArgs(
            CompletionItem completionItem)
        {
            CompletionItem = completionItem;
        }
    }
}
