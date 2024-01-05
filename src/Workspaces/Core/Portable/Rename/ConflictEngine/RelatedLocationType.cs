// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    [Flags]
    internal enum RelatedLocationType
    {
        /// <summary>
        /// There was no conflict. 
        /// </summary>
        NoConflict = 0x0,

        /// <summary>
        /// A conflict was resolved at a location that references the symbol being renamed.
        /// </summary>
        ResolvedReferenceConflict = 0x1,

        /// <summary>
        /// A conflict was resolved in a piece of code that does not reference the symbol being
        /// renamed.
        /// </summary>
        ResolvedNonReferenceConflict = 0x2,

        /// <summary>
        /// There was a conflict that could not be resolved.
        /// </summary>
        PossiblyResolvableConflict = 0x4,

        /// <summary>
        /// These are the conflicts that cannot be resolved. E.g.: Declaration Conflict
        /// </summary>
        UnresolvableConflict = 0x8,

        UnresolvedConflict = PossiblyResolvableConflict | UnresolvableConflict
    }
}
