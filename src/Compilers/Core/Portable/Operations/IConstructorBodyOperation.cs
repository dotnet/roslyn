// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a constructor method body operation.
    /// <para>
    /// Current usage:
    ///  (1) C# method body for constructor declaration
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IConstructorBodyOperation : IMethodBodyBaseOperation
    {
        /// <summary>
        /// Local declarations contained within the <see cref="Initializer" />.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Constructor initializer, if any.
        /// </summary>
        IOperation Initializer { get; }
    }
}
