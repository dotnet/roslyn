// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an initialization for an object or collection creation.
    /// <para>
    /// Current usage:
    ///  (1) C# object or collection initializer expression.
    ///  (2) VB object or collection initializer expression.
    /// For example, object initializer "{ X = x }" within object creation "new Class() { X = x }" and
    /// collection initializer "{ x, y, 3 }" within collection creation "new MyList() { x, y, 3 }".
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IObjectOrCollectionInitializerOperation : IOperation
    {
        /// <summary>
        /// Object member or collection initializers.
        /// </summary>
        ImmutableArray<IOperation> Initializers { get; }
    }
}
