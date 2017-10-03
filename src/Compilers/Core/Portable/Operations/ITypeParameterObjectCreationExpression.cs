// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a type parameter object creation expression, i.e. new T(), where T is a type parameter with new constraint.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ITypeParameterObjectCreationExpression : IOperation
    {
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        IObjectOrCollectionInitializerExpression Initializer { get; }
    }
}

