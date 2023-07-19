// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// An additional hint to the matching algorithm that can
    /// augment or override the existing text-based matching.
    /// </summary>
    public static class MatchPriority
    {
        /// <summary>
        /// The matching algorithm should not select this item unless it's a dramatically 
        /// better text-based match than other items.
        /// </summary>
        internal const int Deprioritize = int.MinValue / 2;

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
