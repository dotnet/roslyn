// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Preferences if a foreach statement is allowed to have an explicit cast not visible in source.
    /// </summary>
    internal enum ForEachExplicitCastInSourcePreference
    {
        /// <summary>
        /// Hidden explicit casts are not allowed.  In any location where one might be emitted, users must supply their
        /// own explicit cast to make it apparent that the code may fail at runtime.
        /// </summary>
        Always,

        /// <summary>
        /// Hidden casts are allowed on legacy APIs but not allowed on strongly-typed modern APIs.  An API is considered
        /// legacy if enumerating it would produce values of type <see cref="object"/> or itself does not implement <see
        /// cref="IEnumerable{T}"/>.  These represent APIs that existed prior to the widespread adoption of generics and
        /// are the reason the language allowed this explicit conversion to not be stated for convenience.  With
        /// generics though it is more likely that an explicit cast emitted is an error and the user put in an incorrect
        /// type errantly and would benefit from an alert about the issue.
        /// </summary>
        WhenStronglyTyped,
    }
}
