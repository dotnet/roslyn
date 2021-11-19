// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
