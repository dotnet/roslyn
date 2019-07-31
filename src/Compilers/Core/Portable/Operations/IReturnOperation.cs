// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a return from the method with an optional return value.
    /// <para>
    /// Current usage:
    ///  (1) C# return statement and yield statement.
    ///  (2) VB Return statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IReturnOperation : IOperation
    {
        /// <summary>
        /// Value to be returned.
        /// </summary>
        IOperation ReturnedValue { get; }
    }
}
