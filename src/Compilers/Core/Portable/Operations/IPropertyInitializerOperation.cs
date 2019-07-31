// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an initialization of a property.
    /// <para>
    /// Current usage:
    ///  (1) C# property initializer with equals value clause.
    ///  (2) VB property initializer with equals value clause or AsNew clause. Multiple properties can be initialized with 'WithEvents' declaration with AsNew clause in VB.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IPropertyInitializerOperation : ISymbolInitializerOperation
    {
        /// <summary>
        /// Initialized properties. There can be multiple properties for Visual Basic 'WithEvents' declaration with AsNew clause.
        /// </summary>
        ImmutableArray<IPropertySymbol> InitializedProperties { get; }
    }
}
