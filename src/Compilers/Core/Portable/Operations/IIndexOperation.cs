// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an index operation.
    /// <para>
    /// Current Usage:
    ///  (1) C# index expressions
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to change it in the future.
    /// PROTOTYPE: (from AlekseyTS) expose the method symbol it is lowered into, for the benefit of GetSymbolInfo and IOperation APIs
    /// </remarks>
    public interface IIndexOperation : IOperation
    {
        /// <summary>
        /// The operand.
        /// </summary>
        IOperation Operand { get; }

        /// <summary>
        /// <code>true</code> if this is a 'lifted' index operation.  When there is an 
        /// operator that is defined to work on a value type, 'lifted' operators are 
        /// created to work on the <see cref="System.Nullable{T}"/> versions of those
        /// value types.
        /// </summary>
        bool IsLifted { get; }
    }
}
