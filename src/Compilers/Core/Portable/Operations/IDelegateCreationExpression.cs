// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a delegate creation expression. This is created whenever a new delegate is created.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDelegateCreationExpression : IOperation
    {
        /// <summary>
        /// The conversion and lambda body or method binding that this delegate is created from.
        /// </summary>
        IOperation Target { get; }
    }
}
