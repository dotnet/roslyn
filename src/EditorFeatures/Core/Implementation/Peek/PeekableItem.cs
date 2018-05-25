// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Peek
{
    internal abstract class PeekableItem : IPeekableItem
    {
        protected readonly IPeekResultFactory PeekResultFactory;

        protected PeekableItem(IPeekResultFactory peekResultFactory)
        {
            this.PeekResultFactory = peekResultFactory;
        }

        public string DisplayName =>
                // This is unused, and was supposed to have been removed from IPeekableItem.
                null;

        public abstract IEnumerable<IPeekRelationship> Relationships { get; }

        public abstract IPeekResultSource GetOrCreateResultSource(string relationshipName);
    }
}
