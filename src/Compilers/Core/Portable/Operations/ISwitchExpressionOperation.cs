// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a switch expression.
    /// <para>
    /// Current usage:
    ///  (1) C# switch expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISwitchExpressionOperation : IOperation
    {
        /// <summary>
        /// Value to be switched upon.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Arms of the switch expression.
        /// </summary>
        ImmutableArray<ISwitchExpressionArmOperation> Arms { get; }
    }
}
