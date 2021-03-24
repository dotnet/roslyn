// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    /// <summary>
    /// Indicate the relationship between the member and its inheritance target
    /// </summary>
    [Flags]
    internal enum InheritanceRelationship
    {
        /// <summary>
        /// A default case that should not be used.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicate the target is implementing the member. It would be shown as I↑.
        /// </summary>
        Implementing = 1,

        /// <summary>
        /// Indicate the target is implemented by the member. It would be shown as I↓.
        /// </summary>
        Implemented = 2,

        /// <summary>
        /// Indicate the target is overriding the member. It would be shown as O↑.
        /// </summary>
        Overriding = 4,

        /// <summary>
        /// Indicate the target is overridden by the member. It would be shown as O↓.
        /// </summary>
        Overridden = 8,

        /// <summary>
        /// A compound value for indicating there are multiple targets both implementing and overriding the member.
        /// </summary>
        ImplementingOverriding = InheritanceRelationship.Implementing | InheritanceRelationship.Overriding,

        /// <summary>
        /// A compound value for indicating there are multiple targets both implementing the member and overriden by the member.
        /// </summary>
        ImplementingOverridden = InheritanceRelationship.Implementing | InheritanceRelationship.Overridden

    }
}
