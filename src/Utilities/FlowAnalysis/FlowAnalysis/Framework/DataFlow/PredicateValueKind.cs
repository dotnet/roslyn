// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
