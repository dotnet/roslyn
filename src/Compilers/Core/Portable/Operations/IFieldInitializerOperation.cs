// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an initialization of a field.
    /// <para>
    /// Current usage:
    ///  (1) C# field initializer with equals value clause.
    ///  (2) VB field(s) initializer with equals value clause or AsNew clause. Multiple fields can be initialized with AsNew clause in VB.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IFieldInitializerOperation : ISymbolInitializerOperation
    {
        /// <summary>
        /// Initialized fields. There can be multiple fields for Visual Basic fields declared with AsNew clause.
        /// </summary>
        ImmutableArray<IFieldSymbol> InitializedFields { get; }
    }
}
