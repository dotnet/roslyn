// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# or VB object or collection initializer expression.
    /// For example, object initializer "{ X = x }" within object creation "new Class() { X = x }" and
    /// collection initializer "{ x, y, 3 }" within collection creation "new MyList() { x, y, 3 }".
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IObjectOrCollectionInitializerExpression : IOperation
    {
        /// <summary>
        /// Object member or collection initializers.
        /// </summary>
        ImmutableArray<IOperation> Initializers { get; }
    }
}
