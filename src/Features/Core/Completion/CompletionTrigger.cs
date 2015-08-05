// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    internal abstract class CompletionTrigger
    {
        public ImmutableArray<string> CustomTags { get; }

        protected CompletionTrigger(ImmutableArray<string> customTags)
        {
            this.CustomTags = customTags.ToImmutableArrayOrEmpty();
        }
    }
}
