// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an initialization of a field.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IFieldInitializer : ISymbolInitializer
    {
        /// <summary>
        /// Initialized fields. There can be multiple fields for Visual Basic fields declared with As New.
        /// </summary>
        ImmutableArray<IFieldSymbol> InitializedFields { get; }
    }
}

