// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// A group of items to be presented to a user in a completion list.
    /// </summary>
    internal class CompletionItemGroup
    {
        /// <summary>
        /// True if this group is exclusive.  If a group is exclusive, then only those items in that
        /// group will be presented to the user.  If a group is non-exclusive, then all other
        /// completion providers will be asked to augment the list of items it has provided.
        /// 
        /// If multiple groups are marked as exclusive, only the first one returned from a provider
        /// will be used.  Providers can be ordered using the OrderAttribute.
        /// </summary>
        public bool IsExclusive { get; }

        /// <summary>
        /// The list of completion items to present to the user.  Can not be empty.
        /// </summary>
        public IEnumerable<CompletionItem> Items { get; }

        /// <summary>
        /// A completion builder to present to the user.  Can be null if no builder need be
        /// presented.  A builder is generally useful when the location in source where completion is
        /// requested represents a place where both completion items can be offered, and new items
        /// can be declared.  By offering a builder the user has the option of declaring a new item
        /// in an unimpeded manner.
        /// </summary>
        public CompletionItem Builder { get; }

        public CompletionItemGroup(IEnumerable<CompletionItem> items, CompletionItem builder = null, bool isExclusive = false)
        {
            this.Items = items.ToImmutableArray();
            this.Builder = builder;
            this.IsExclusive = isExclusive;
        }
    }
}
