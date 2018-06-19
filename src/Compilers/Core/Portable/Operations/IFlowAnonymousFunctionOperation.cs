// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Represents an anonymous function operation in context of a <see cref="ControlFlowGraph"/>.
    /// <para>
    /// Current usage:
    ///  (1) C# lambda expression.
    ///  (2) VB anonymous delegate expression.
    /// </para>
    /// A <see cref="ControlFlowGraph"/> for the body of the anonymous function is available from 
    /// the enclosing <see cref="ControlFlowGraph"/>.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IFlowAnonymousFunctionOperation : IOperation
    {
        /// <summary>
        /// Symbol of the anonymous function. 
        /// </summary>
        IMethodSymbol Symbol { get; }
    }
}

