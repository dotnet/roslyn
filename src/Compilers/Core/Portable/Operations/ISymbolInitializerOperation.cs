// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an initializer for a field, property, parameter or a local variable declaration.
    /// <para>
    /// Current usage:
    ///  (1) C# field, property, parameter or local variable initializer.
    ///  (2) VB field(s), property, parameter or local variable initializer.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISymbolInitializerOperation : IOperation
    {
        /// <summary>
        /// Underlying initializer value.
        /// </summary>
        IOperation Value { get; }
    }
}

