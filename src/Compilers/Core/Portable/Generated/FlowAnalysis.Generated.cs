// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// < auto-generated />
#nullable enable
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    #region Interfaces
    /// <summary>
    /// Represents that an intermediate result is being captured.
    /// This node is produced only as part of a <see cref="ControlFlowGraph" />.
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.FlowCapture"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
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
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.FlowCaptureReference"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IFlowCaptureReferenceOperation : IOperation
    {
        /// <summary>
        /// An id used to match references to the same intermediate result.
        /// </summary>
        CaptureId Id { get; }
        /// <summary>
        /// True if this reference to the capture initializes the capture. Used when the capture is being initialized by being passed as an <see langword="out" /> parameter.
        /// </summary>
        bool IsInitialization { get; }
    }
    /// <summary>
    /// Represents result of checking whether the <see cref="Operand" /> is null.
    /// For reference types this checks if the <see cref="Operand" /> is a null reference,
    /// for nullable types this checks if the <see cref="Operand" /> doesn’t have a value.
    /// The node is produced as part of a flow graph during rewrite of <see cref="ICoalesceOperation" />
    /// and <see cref="IConditionalAccessOperation" /> nodes.
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.IsNull"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
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
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.CaughtException"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
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
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.StaticLocalInitializationSemaphore"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
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
    /// <list type="number">
    ///   <item><description>C# lambda expression</description></item>
    ///   <item><description>VB anonymous delegate expression</description></item>
    /// </list>
    /// </para>
    /// A <see cref="ControlFlowGraph" /> for the body of the anonymous function is available from
    /// the enclosing <see cref="ControlFlowGraph" />.
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.FlowAnonymousFunction"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
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
