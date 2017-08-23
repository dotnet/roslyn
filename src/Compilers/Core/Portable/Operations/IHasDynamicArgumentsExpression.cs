// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IHasDynamicArgumentsExpression : IOperation
    {
        /// <summary>
        /// List of applicable symbols that are dynamically bound.
        /// </summary>
        ImmutableArray<ISymbol> ApplicableSymbols { get; }

        /// <summary>
        /// Dynamically bound arguments, excluding the instance argument.
        /// </summary>
        ImmutableArray<IOperation> Arguments { get; }
        
        /// <summary>
        /// Optional argument names for named arguments.
        /// </summary>
        ImmutableArray<string> ArgumentNames { get; }

        /// <summary>
        /// Optional argument ref kinds.
        /// </summary>
        ImmutableArray<RefKind> ArgumentRefKinds { get; }
    }
}

