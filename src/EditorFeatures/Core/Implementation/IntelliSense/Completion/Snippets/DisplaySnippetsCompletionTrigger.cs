// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Snippets
{
    internal class DisplaySnippetsCompletionTrigger : CompletionTrigger
    {
        public static DisplaySnippetsCompletionTrigger Instance = new DisplaySnippetsCompletionTrigger();

        private DisplaySnippetsCompletionTrigger()
            : base(default(ImmutableArray<string>))
        {
        }
    }
}
