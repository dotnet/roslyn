// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion.Triggers
{
    internal class DisplayListCompletionTrigger : CompletionTrigger
    {
        public DisplayListCompletionTrigger(ImmutableArray<string> customTags = default(ImmutableArray<string>))
            : base(customTags)
        {
        }
    }
}
