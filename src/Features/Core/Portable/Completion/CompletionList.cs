// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// A completion list to be presented to the user.
    /// </summary>
    internal class CompletionList
    {
        /// <summary>
        /// True if this list is exclusive.  If a list is exclusive, then only those items in that
        /// group list will be presented to the user.  If a list is non-exclusive, then all other
        /// completion providers will be asked to augment the list of items it has provided.
        /// 
        /// If multiple lists are marked as exclusive, only the first one returned from a provider
        /// will be used.  Providers can be ordered using the OrderAttribute.
        /// </summary>
        public bool IsExclusive { get; }

        /// <summary>
        /// The completion items to present to the user.  Can not be empty.
        /// </summary>
        public ImmutableArray<CompletionItem> Items { get; }

        /// <summary>
        /// A completion builder to present to the user.  Can be null if no builder need be
        /// presented.  A builder is generally useful when the location in source where completion is
        /// requested represents a place where both completion items can be offered, and new items
        /// can be declared.  By offering a builder the user has the option of declaring a new item
        /// in an unimpeded manner.
        /// </summary>
        public CompletionItem Builder { get; }

        public CompletionList(ImmutableArray<CompletionItem> items, CompletionItem builder = null, bool isExclusive = false)
        {
            this.Items = items;
            this.Builder = builder;
            this.IsExclusive = isExclusive;
        }
    }
}
