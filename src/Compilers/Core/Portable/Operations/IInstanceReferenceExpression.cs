// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# this or base expression, or a VB Me, MyClass, or MyBase expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInstanceReferenceExpression : IOperation
    {
        /// <summary>
        /// True if this is a reference to base or MyBase
        /// </summary>
        bool IsBaseReference { get; }
    }
}

