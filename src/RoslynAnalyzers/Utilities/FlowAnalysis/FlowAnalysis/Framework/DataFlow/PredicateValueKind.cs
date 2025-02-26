// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    public enum PredicateValueKind
    {
        /// <summary>
        /// Predicate always evaluates to true.
        /// </summary>
        AlwaysTrue,

        /// <summary>
        /// Predicate always evaluates to false.
        /// </summary>
        AlwaysFalse,

        /// <summary>
        /// Predicate might evaluate to true or false.
        /// </summary>
        Unknown
    }
}
