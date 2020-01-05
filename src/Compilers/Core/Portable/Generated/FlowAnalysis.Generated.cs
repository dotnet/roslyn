// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
// < auto-generated />
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    #region Interfaces
    /// <summary>
    /// Represents that an intermediate result is being captured.
    /// This node is produced only as part of a <see cref="ControlFlowGraph" />.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IFlowCaptureOperation : IOperation
    {
        /// <summary>
        /// An id used to match references to the same intermediate result.
        /// </summary>
        CaptureId Id { get; }
        /// <summary>
        /// Value to be captured.
        /// </summary>
        IOperation Value { get; }
    }
    /// <summary>
    /// Represents a point of use of an intermediate result captured earlier.
    /// The fact of capturing the result is represented by <see cref="IFlowCaptureOperation" />.
    /// This node is produced only as part of a <see cref="ControlFlowGraph" />.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IFlowCaptureReferenceOperation : IOperation
    {
        /// <summary>
        /// An id used to match references to the same intermediate result.
        /// </summary>
        CaptureId Id { get; }
    }
    /// <summary>
    /// Represents result of checking whether the <see cref="Operand" /> is null.
    /// For reference types this checks if the <see cref="Operand" /> is a null reference,
    /// for nullable types this checks if the <see cref="Operand" /> doesn’t have a value.
    /// The node is produced as part of a flow graph during rewrite of <see cref="ICoalesceOperation" />
    /// and <see cref="IConditionalAccessOperation" /> nodes.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IIsNullOperation : IOperation
    {
        /// <summary>
        /// Value to check.
        /// </summary>
        IOperation Operand { get; }
    }
    /// <summary>
    /// Represents a exception instance passed by an execution environment to an exception filter or handler.
    /// This node is produced only as part of a <see cref="ControlFlowGraph" />.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ICaughtExceptionOperation : IOperation
    {
    }
    /// <summary>
    /// Represents the check during initialization of a VB static local that is initialized on the first call of the function, and never again.
    /// If the semaphore operation returns true, the static local has not yet been initialized, and the initializer will be run. If it returns
    /// false, then the local has already been initialized, and the static local initializer region will be skipped.
    /// This node is produced only as part of a <see cref="ControlFlowGraph" />.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IStaticLocalInitializationSemaphoreOperation : IOperation
    {
        /// <summary>
        /// The static local variable that is possibly initialized.
        /// </summary>
        ILocalSymbol Local { get; }
    }
    /// <summary>
    /// Represents an anonymous function operation in context of a <see cref="ControlFlowGraph" />.
    /// <para>
    /// Current usage:
    ///  (1) C# lambda expression.
    ///  (2) VB anonymous delegate expression.
    /// </para>
    /// A <see cref="ControlFlowGraph" /> for the body of the anonymous function is available from
    /// the enclosing <see cref="ControlFlowGraph" />.
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
    #endregion
}
