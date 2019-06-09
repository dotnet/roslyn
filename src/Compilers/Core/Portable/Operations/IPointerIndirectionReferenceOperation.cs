// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a reference through a pointer.
    /// <para>
    /// Current usage:
    ///  (1) C# pointer indirection reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    // API moved internal for V1
    // https://github.com/dotnet/roslyn/issues/21295
    internal interface IPointerIndirectionReferenceOperation : IOperation
    {
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        IOperation Pointer { get; }
    }
}
