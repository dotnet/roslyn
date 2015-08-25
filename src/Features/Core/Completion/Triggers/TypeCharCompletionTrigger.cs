// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion.Triggers
{
    internal class TypeCharCompletionTrigger : CompletionTrigger
    {
        public char TypedCharacter { get; }

        public TypeCharCompletionTrigger(
            char typedCharacter,
            ImmutableArray<string> customTags = default(ImmutableArray<string>))
            : base(customTags)
        {
            this.TypedCharacter = typedCharacter;
        }
    }
}
