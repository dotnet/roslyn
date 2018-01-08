﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an argument value that has been omitted in an invocation.
    /// <para>
    /// Current usage:
    ///  (1) VB omitted argument in an invocation expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IOmittedArgumentOperation : IOperation
    {
    }
}

