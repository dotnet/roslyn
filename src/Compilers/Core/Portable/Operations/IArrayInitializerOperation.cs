// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents the initialization of an array instance.
    /// <para>
    /// Current usage:
    ///  (1) C# array initializer.
    ///  (2) VB array initializer.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IArrayInitializerOperation : IOperation
    {
        /// <summary>
        /// Values to initialize array elements.
        /// </summary>
        ImmutableArray<IOperation> ElementValues { get; }
    }
}
