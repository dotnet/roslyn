// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an invalid operation with one or more child operations.
    /// <para>
    /// Current usage:
    ///  (1) C# invalid expression or invalid statement.
    ///  (2) VB invalid expression or invalid statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInvalidOperation : IOperation
    {
    }
}
