// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed class FindReferencesOptions
    {
        public static readonly FindReferencesOptions Default = new FindReferencesOptions(
            cascade: true);

        /// <summary>
        /// Whether or not we should automatically cascade to members when doing a find-references
        /// search.  Default to <see langword="true"/>.
        /// </summary>
        public readonly bool Cascade;

        public FindReferencesOptions(bool cascade)
        {
            Cascade = cascade;
        }
    }
}
