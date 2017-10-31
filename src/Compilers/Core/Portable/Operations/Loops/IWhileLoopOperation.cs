﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a while loop.
    /// </summary>
    /// <para>
    /// Current usage:
    ///  (1) C# 'while' loop statement
    ///  (2) VB 'While' loop statement
    /// </para>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IWhileLoopOperation : ILoopOperation
    {
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        IOperation Condition { get; }
    }
}

