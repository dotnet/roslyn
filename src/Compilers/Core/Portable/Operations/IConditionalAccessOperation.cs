// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a conditionally accessed operation. Note that <see cref="IConditionalAccessInstanceOperation" /> is used to refer to the value
    /// of <see cref="Operation" /> within <see cref="WhenNotNull" />.
    /// <para>
    /// Current usage:
    ///  (1) C# conditional access expression (? or ?. operator).
    ///  (2) VB conditional access expression (? or ?. operator).
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IConditionalAccessOperation : IOperation
    {
        /// <summary>
        /// Operation that will be evaluated and accessed if non null.
        /// </summary>
        IOperation Operation { get; }
        /// <summary>
        /// Operation to be evaluated if <see cref="Operation" /> is non null.
        /// </summary>
        IOperation WhenNotNull { get; }
    }
}
