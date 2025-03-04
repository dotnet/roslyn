// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Abstract value domain for a <see cref="DataFlowAnalysis"/> to merge and compare values.
    /// </summary>
    public abstract class AbstractValueDomain<T> : AbstractDomain<T>
    {
        /// <summary>
        /// Returns the major Unknown or MayBe top value of the domain.
        /// </summary>
        public abstract T UnknownOrMayBeValue { get; }
    }
}
