// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.Wrapping
{
    internal abstract partial class AbstractWrapper
    {
        protected readonly struct WrappingGroup
        {
            public readonly string Title;
            public readonly bool IsInlinable;
            public readonly ImmutableArray<WrapItemsAction> WrappingActions;

            public WrappingGroup(string title, bool isInlinable, ImmutableArray<WrapItemsAction> wrappingActions)
            {
                Title = title;
                IsInlinable = isInlinable;
                WrappingActions = wrappingActions;
            }
        }
    }
}
