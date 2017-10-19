// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an initialization of member within an object initializer.
    /// <para>
    /// Current usage:
    ///  (1) C# member initializer expression.
    ///  (2) VB member initializer expression.
    /// For example, given an object creation with initializer "new Class() { X = x, Y = { x, y, 3 }, Z = { X = z } }",
    /// member initializers for Y and Z, i.e. "Y = { x, y, 3 }", and "Z = { X = z }" are nested member initializers represented by this operation.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IMemberInitializerOperation : IOperation
    {
        /// <summary>
        /// Initialized member.
        /// </summary>
        IMemberReferenceOperation InitializedMember { get; }
        
        /// <summary>
        /// Member initializer.
        /// </summary>
        IObjectOrCollectionInitializerOperation Initializer { get; }
    }
}
