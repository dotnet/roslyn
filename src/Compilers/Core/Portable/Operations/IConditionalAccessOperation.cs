// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a conditionally accessed operation off an instance operation.
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
        /// Operation that will be evaulated and accessed if non null.
        /// </summary>
        IOperation Operation { get; }
        /// <summary>
        /// Operation to be evaluated if <see cref="Operation"/> is non null.
        /// </summary>
        IOperation WhenNotNull { get; }
    }
}

