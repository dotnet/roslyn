// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# or VB member initializer expression within an object initializer expression.
    /// For example, given an object creation with initializer "new Class() { X = x, Y = { x, y, 3 }, Z = { X = z } }",
    /// member initializers for Y and Z, i.e. "Y = { x, y, 3 }", and "Z = { X = z }" are nested member initializers represented by this operation.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IMemberInitializerExpression : IOperation
    {
        /// <summary>
        /// Initialized member.
        /// </summary>
        IMemberReferenceExpression InitializedMember { get; }
        
        /// <summary>
        /// Member initializer.
        /// </summary>
        IObjectOrCollectionInitializerExpression Initializer { get; }
    }
}
