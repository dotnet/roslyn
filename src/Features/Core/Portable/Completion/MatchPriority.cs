// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// An additional hint to the matching algorithm that can
    /// augment or override the existing text-based matching.
    /// </summary>
    public static class MatchPriority
    {
        /// <summary>
        /// The matching algorithm should give this item no special treatment.
        /// 
        /// Ordinary <see cref="CompletionProvider"/>s typically specify this.
        /// </summary>
        public static readonly int Default = 0;

        /// <summary>
        /// The matching algorithm will tend to prefer this item unless
        /// a dramatically better text-based match is available.
        /// 
        /// With no filter text, this item (or the first item alphabetically 
        /// with this priority) should always be selected.
        ///
        /// This is used for specific IDE scenarios like "Object creation preselection"
        /// or "Enum preselection" or "Completion list tag preselection".
        /// </summary>
        public static readonly int Preselect = int.MaxValue / 2;
    }
}
