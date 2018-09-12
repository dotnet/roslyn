// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a switch operation with a value to be switched upon and switch cases.
    /// <para>
    /// Current usage:
    ///  (1) C# switch statement.
    ///  (2) VB Select Case statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISwitchOperation : IOperation
    {
        /// <summary>
        /// Value to be switched upon.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Cases of the switch.
        /// </summary>
        ImmutableArray<ISwitchCaseOperation> Cases { get; }
        /// <summary>
        /// Exit label for the switch statement.
        /// </summary>
        ILabelSymbol ExitLabel { get; }
        /// <summary>
        /// Locals declared within the switch operation with scope spanning across all <see cref="Cases"/>.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }
}

