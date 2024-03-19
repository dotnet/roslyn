// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Peek
{
    internal abstract class PeekableItem : IPeekableItem
    {
        protected readonly IPeekResultFactory PeekResultFactory;

        protected PeekableItem(IPeekResultFactory peekResultFactory)
            => this.PeekResultFactory = peekResultFactory;

        public string DisplayName
                // This is unused, and was supposed to have been removed from IPeekableItem.
                => null;

        public abstract IEnumerable<IPeekRelationship> Relationships { get; }

        public abstract IPeekResultSource GetOrCreateResultSource(string relationshipName);
    }
}
