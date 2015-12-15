// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// An additional hint to the matching algorithm that can
    /// augment or override the existing text-based matching.
    /// </summary>
    internal enum MatchPriority
    {
        /// <summary>
        /// The matching algorithm should give this item no special treatment.
        /// 
        /// Ordinary <see cref="CompletionListProvider"/>s typically specify this.
        /// </summary>
        Default = 0,

        /// <summary>
        /// The matching algorithm will tend to prefer this item unless
        /// a dramatically better text-based match is available or relevant
        /// items are marked with <see cref="Preselect"/> or <see cref="Prefer"/>.
        /// 
        /// With no filter text, this item (or the first item alphabetically 
        /// with this priority) should always be selected unless other items
        /// are marked with <see cref="Preselect"/> or <see cref="Prefer"/>.
        /// 
        /// This flag should be used when the user is more likely to want
        /// to match certain items, but the IDE can't specifically guess what
        /// item (like target type preselection).
        /// </summary>
        PreferLess = 1,

        /// <summary>
        /// The matching algorithm will tend to prefer this item unless
        /// a dramatically better text-based match is available or relevant
        /// items are marked with <see cref="Preselect"/>.
        /// 
        /// With no filter text, this item (or the first item alphabetically 
        /// with this priority) should always be selected unless other items
        /// are marked with <see cref="Preselect"/>.
        /// 
        /// This flag should be used when the user is more likely to want
        /// to match certain items, but the IDE can't specifically guess what
        /// item (like target type preselection).
        /// </summary>
        Prefer = 2,

        /// <summary>
        /// The matching algorithm will tend to prefer this item unless
        /// a dramatically better text-based match is available.
        /// 
        /// With no filter text, this item (or the first item alphabeitcally 
        /// with this priority) should always be selected.
        /// 
        /// This is used for specific IDE scenarios like "Object creation preselection"
        /// or "Enum preselection" or "Completion list tag preselection".
        /// </summary>
        Preselect = 3,
    }
}