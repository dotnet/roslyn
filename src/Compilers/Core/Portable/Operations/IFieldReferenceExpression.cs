// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a reference to a field.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IFieldReferenceExpression : IMemberReferenceExpression
    {
        /// <summary>
        /// Referenced field.
        /// </summary>
        IFieldSymbol Field { get; }
        /// <summary>
        /// If the field reference is also where the field was declared.
        /// </summary>
        /// <remarks>
        /// This is only ever true in CSharp scripts, where a top-level statement creates a new variable
        /// in a reference, such as an out variable declaration or a deconstruction declaration.
        /// </remarks>
        bool IsDeclaration { get; }
    }
}

