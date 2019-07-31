// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a delegate creation. This is created whenever a new delegate is created.
    /// <para>
    /// Current usage:
    ///  (1) C# delegate creation expression.
    ///  (2) VB delegate creation expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDelegateCreationOperation : IOperation
    {
        /// <summary>
        /// The lambda or method binding that this delegate is created from.
        /// </summary>
        IOperation Target { get; }
    }
}
