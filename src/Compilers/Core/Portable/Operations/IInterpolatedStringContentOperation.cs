// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a constituent part of an interpolated string.
    /// <para>
    /// Current usage:
    ///  (1) C# interpolated string content.
    ///  (2) VB interpolated string content.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInterpolatedStringContentOperation : IOperation
    {
    }
}
