// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a coalesce operation with two operands:
    /// (1) <see cref="Value" />, which is the first operand that is unconditionally evaluated and is the result of the operation if non null.
    /// (2) <see cref="WhenNull" />, which is the second operand that is conditionally evaluated and is the result of the operation if <see cref="Value" /> is null.
    /// <para>
    /// Current usage:
    ///  (1) C# null-coalescing expression "Value ?? WhenNull".
    ///  (2) VB binary conditional expression "If(Value, WhenNull)".
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ICoalesceOperation : IOperation
    {
        /// <summary>
        /// Operation to be unconditionally evaluated.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Operation to be conditionally evaluated if <see cref="Value" /> evaluates to null/Nothing.
        /// </summary>
        IOperation WhenNull { get; }
        /// <summary>
        /// Conversion associated with <see cref="Value" /> when it is not null/Nothing.
        /// Identity if result type of the operation is the same as type of <see cref="Value" />.
        /// Otherwise, if type of <see cref="Value" /> is nullable, then conversion is applied to an
        /// unwrapped <see cref="Value" />, otherwise to the <see cref="Value" /> itself.
        /// </summary>
        CommonConversion ValueConversion { get; }
    }
}
