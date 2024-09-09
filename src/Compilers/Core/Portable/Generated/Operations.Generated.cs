// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// < auto-generated />
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    #region Interfaces
    /// <summary>
    /// Represents an invalid operation with one or more child operations.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# invalid expression or invalid statement</description></item>
    ///   <item><description>VB invalid expression or invalid statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Invalid"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IInvalidOperation : IOperation
    {
    }
    /// <summary>
    /// Represents a block containing a sequence of operations and local declarations.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# "{ ... }" block statement</description></item>
    ///   <item><description>VB implicit block statement for method bodies and other block scoped statements</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Block"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IBlockOperation : IOperation
    {
        /// <summary>
        /// Operations contained within the block.
        /// </summary>
        ImmutableArray<IOperation> Operations { get; }
        /// <summary>
        /// Local declarations contained within the block.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }
    /// <summary>
    /// Represents a variable declaration statement.
    /// <para>
    /// Current Usage:
    /// <list type="number">
    ///   <item><description>C# local declaration statement</description></item>
    ///   <item><description>C# fixed statement</description></item>
    ///   <item><description>C# using statement</description></item>
    ///   <item><description>C# using declaration</description></item>
    ///   <item><description>VB Dim statement</description></item>
    ///   <item><description>VB Using statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.VariableDeclarationGroup"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IVariableDeclarationGroupOperation : IOperation
    {
        /// <summary>
        /// Variable declaration in the statement.
        /// </summary>
        /// <remarks>
        /// In C#, this will always be a single declaration, with all variables in <see cref="IVariableDeclarationOperation.Declarators" />.
        /// </remarks>
        ImmutableArray<IVariableDeclarationOperation> Declarations { get; }
    }
    /// <summary>
    /// Represents a switch operation with a value to be switched upon and switch cases.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# switch statement</description></item>
    ///   <item><description>VB Select Case statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Switch"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ISwitchOperation : IOperation
    {
        /// <summary>
        /// Locals declared within the switch operation with scope spanning across all <see cref="Cases" />.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
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
    }
    /// <summary>
    /// Represents a loop operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# 'while', 'for', 'foreach' and 'do' loop statements</description></item>
    ///   <item><description>VB 'While', 'ForTo', 'ForEach', 'Do While' and 'Do Until' loop statements</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ILoopOperation : IOperation
    {
        /// <summary>
        /// Kind of the loop.
        /// </summary>
        LoopKind LoopKind { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        IOperation Body { get; }
        /// <summary>
        /// Declared locals.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Loop continue label.
        /// </summary>
        ILabelSymbol ContinueLabel { get; }
        /// <summary>
        /// Loop exit/break label.
        /// </summary>
        ILabelSymbol ExitLabel { get; }
    }
    /// <summary>
    /// Represents a for each loop.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# 'foreach' loop statement</description></item>
    ///   <item><description>VB 'For Each' loop statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IForEachLoopOperation : ILoopOperation
    {
        /// <summary>
        /// Refers to the operation for declaring a new local variable or reference an existing variable or an expression.
        /// </summary>
        IOperation LoopControlVariable { get; }
        /// <summary>
        /// Collection value over which the loop iterates.
        /// </summary>
        IOperation Collection { get; }
        /// <summary>
        /// Optional list of comma separated next variables at loop bottom in VB.
        /// This list is always empty for C#.
        /// </summary>
        ImmutableArray<IOperation> NextVariables { get; }
        /// <summary>
        /// Whether this for each loop is asynchronous.
        /// Always false for VB.
        /// </summary>
        bool IsAsynchronous { get; }
    }
    /// <summary>
    /// Represents a for loop.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# 'for' loop statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IForLoopOperation : ILoopOperation
    {
        /// <summary>
        /// List of operations to execute before entry to the loop. For C#, this comes from the first clause of the for statement.
        /// </summary>
        ImmutableArray<IOperation> Before { get; }
        /// <summary>
        /// Locals declared within the loop Condition and are in scope throughout the <see cref="Condition" />,
        /// <see cref="ILoopOperation.Body" /> and <see cref="AtLoopBottom" />.
        /// They are considered to be declared per iteration.
        /// </summary>
        ImmutableArray<ILocalSymbol> ConditionLocals { get; }
        /// <summary>
        /// Condition of the loop. For C#, this comes from the second clause of the for statement.
        /// </summary>
        IOperation? Condition { get; }
        /// <summary>
        /// List of operations to execute at the bottom of the loop. For C#, this comes from the third clause of the for statement.
        /// </summary>
        ImmutableArray<IOperation> AtLoopBottom { get; }
    }
    /// <summary>
    /// Represents a for to loop with loop control variable and initial, limit and step values for the control variable.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>VB 'For ... To ... Step' loop statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IForToLoopOperation : ILoopOperation
    {
        /// <summary>
        /// Refers to the operation for declaring a new local variable or reference an existing variable or an expression.
        /// </summary>
        IOperation LoopControlVariable { get; }
        /// <summary>
        /// Operation for setting the initial value of the loop control variable. This comes from the expression between the 'For' and 'To' keywords.
        /// </summary>
        IOperation InitialValue { get; }
        /// <summary>
        /// Operation for the limit value of the loop control variable. This comes from the expression after the 'To' keyword.
        /// </summary>
        IOperation LimitValue { get; }
        /// <summary>
        /// Operation for the step value of the loop control variable. This comes from the expression after the 'Step' keyword,
        /// or inferred by the compiler if 'Step' clause is omitted.
        /// </summary>
        IOperation StepValue { get; }
        /// <summary>
        /// <see langword="true" /> if arithmetic operations behind this loop are 'checked'.
        /// </summary>
        bool IsChecked { get; }
        /// <summary>
        /// Optional list of comma separated next variables at loop bottom.
        /// </summary>
        ImmutableArray<IOperation> NextVariables { get; }
    }
    /// <summary>
    /// Represents a while or do while loop.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# 'while' and 'do while' loop statements</description></item>
    ///   <item><description>VB 'While', 'Do While' and 'Do Until' loop statements</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IWhileLoopOperation : ILoopOperation
    {
        /// <summary>
        /// Condition of the loop. This can only be null in error scenarios.
        /// </summary>
        IOperation? Condition { get; }
        /// <summary>
        /// True if the <see cref="Condition" /> is evaluated at start of each loop iteration.
        /// False if it is evaluated at the end of each loop iteration.
        /// </summary>
        bool ConditionIsTop { get; }
        /// <summary>
        /// True if the loop has 'Until' loop semantics and the loop is executed while <see cref="Condition" /> is false.
        /// </summary>
        bool ConditionIsUntil { get; }
        /// <summary>
        /// Additional conditional supplied for loop in error cases, which is ignored by the compiler.
        /// For example, for VB 'Do While' or 'Do Until' loop with syntax errors where both the top and bottom conditions are provided.
        /// The top condition is preferred and exposed as <see cref="Condition" /> and the bottom condition is ignored and exposed by this property.
        /// This property should be null for all non-error cases.
        /// </summary>
        IOperation? IgnoredCondition { get; }
    }
    /// <summary>
    /// Represents an operation with a label.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# labeled statement</description></item>
    ///   <item><description>VB label statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Labeled"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ILabeledOperation : IOperation
    {
        /// <summary>
        /// Label that can be the target of branches.
        /// </summary>
        ILabelSymbol Label { get; }
        /// <summary>
        /// Operation that has been labeled. In VB, this is always null.
        /// </summary>
        IOperation? Operation { get; }
    }
    /// <summary>
    /// Represents a branch operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# goto, break, or continue statement</description></item>
    ///   <item><description>VB GoTo, Exit ***, or Continue *** statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Branch"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IBranchOperation : IOperation
    {
        /// <summary>
        /// Label that is the target of the branch.
        /// </summary>
        ILabelSymbol Target { get; }
        /// <summary>
        /// Kind of the branch.
        /// </summary>
        BranchKind BranchKind { get; }
    }
    /// <summary>
    /// Represents an empty or no-op operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# empty statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Empty"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IEmptyOperation : IOperation
    {
    }
    /// <summary>
    /// Represents a return from the method with an optional return value.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# return statement and yield statement</description></item>
    ///   <item><description>VB Return statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Return"/></description></item>
    /// <item><description><see cref="OperationKind.YieldBreak"/></description></item>
    /// <item><description><see cref="OperationKind.YieldReturn"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IReturnOperation : IOperation
    {
        /// <summary>
        /// Value to be returned.
        /// </summary>
        IOperation? ReturnedValue { get; }
    }
    /// <summary>
    /// Represents a <see cref="Body" /> of operations that are executed while holding a lock onto the <see cref="LockedValue" />.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# lock statement</description></item>
    ///   <item><description>VB SyncLock statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Lock"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ILockOperation : IOperation
    {
        /// <summary>
        /// Operation producing a value to be locked.
        /// </summary>
        IOperation LockedValue { get; }
        /// <summary>
        /// Body of the lock, to be executed while holding the lock.
        /// </summary>
        IOperation Body { get; }
    }
    /// <summary>
    /// Represents a try operation for exception handling code with a body, catch clauses and a finally handler.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# try statement</description></item>
    ///   <item><description>VB Try statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Try"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ITryOperation : IOperation
    {
        /// <summary>
        /// Body of the try, over which the handlers are active.
        /// </summary>
        IBlockOperation Body { get; }
        /// <summary>
        /// Catch clauses of the try.
        /// </summary>
        ImmutableArray<ICatchClauseOperation> Catches { get; }
        /// <summary>
        /// Finally handler of the try.
        /// </summary>
        IBlockOperation? Finally { get; }
        /// <summary>
        /// Exit label for the try. This will always be null for C#.
        /// </summary>
        ILabelSymbol? ExitLabel { get; }
    }
    /// <summary>
    /// Represents a <see cref="Body" /> of operations that are executed while using disposable <see cref="Resources" />.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# using statement</description></item>
    ///   <item><description>VB Using statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Using"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IUsingOperation : IOperation
    {
        /// <summary>
        /// Declaration introduced or resource held by the using.
        /// </summary>
        IOperation Resources { get; }
        /// <summary>
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        IOperation Body { get; }
        /// <summary>
        /// Locals declared within the <see cref="Resources" /> with scope spanning across this entire <see cref="IUsingOperation" />.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Whether this using is asynchronous.
        /// Always false for VB.
        /// </summary>
        bool IsAsynchronous { get; }
    }
    /// <summary>
    /// Represents an operation that drops the resulting value and the type of the underlying wrapped <see cref="Operation" />.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# expression statement</description></item>
    ///   <item><description>VB expression statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ExpressionStatement"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IExpressionStatementOperation : IOperation
    {
        /// <summary>
        /// Underlying operation with a value and type.
        /// </summary>
        IOperation Operation { get; }
    }
    /// <summary>
    /// Represents a local function defined within a method.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# local function statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.LocalFunction"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ILocalFunctionOperation : IOperation
    {
        /// <summary>
        /// Local function symbol.
        /// </summary>
        IMethodSymbol Symbol { get; }
        /// <summary>
        /// Body of the local function.
        /// </summary>
        /// <remarks>
        /// This can be null in error scenarios, or when the method is an extern method.
        /// </remarks>
        IBlockOperation? Body { get; }
        /// <summary>
        /// An extra body for the local function, if both a block body and expression body are specified in source.
        /// </summary>
        /// <remarks>
        /// This is only ever non-null in error situations.
        /// </remarks>
        IBlockOperation? IgnoredBody { get; }
    }
    /// <summary>
    /// Represents an operation to stop or suspend execution of code.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>VB Stop statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Stop"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IStopOperation : IOperation
    {
    }
    /// <summary>
    /// Represents an operation that stops the execution of code abruptly.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>VB End Statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.End"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IEndOperation : IOperation
    {
    }
    /// <summary>
    /// Represents an operation for raising an event.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>VB raise event statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.RaiseEvent"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IRaiseEventOperation : IOperation
    {
        /// <summary>
        /// Reference to the event to be raised.
        /// </summary>
        IEventReferenceOperation EventReference { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays.
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        ImmutableArray<IArgumentOperation> Arguments { get; }
    }
    /// <summary>
    /// Represents a textual literal numeric, string, etc.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# literal expression</description></item>
    ///   <item><description>VB literal expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Literal"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ILiteralOperation : IOperation
    {
    }
    /// <summary>
    /// Represents a type conversion.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# conversion expression</description></item>
    ///   <item><description>VB conversion expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Conversion"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IConversionOperation : IOperation
    {
        /// <summary>
        /// Value to be converted.
        /// </summary>
        IOperation Operand { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol? OperatorMethod { get; }
        /// <summary>
        /// Type parameter which runtime type will be used to resolve virtual invocation of the <see cref="OperatorMethod" />, if any.
        /// Null if <see cref="OperatorMethod" /> is resolved statically, or is null.
        /// </summary>
        ITypeSymbol? ConstrainedToType { get; }
        /// <summary>
        /// Gets the underlying common conversion information.
        /// </summary>
        /// <remarks>
        /// If you need conversion information that is language specific, use either
        /// <see cref="T:Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetConversion(IConversionOperation)" /> or
        /// <see cref="T:Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions.GetConversion(IConversionOperation)" />.
        /// </remarks>
        CommonConversion Conversion { get; }
        /// <summary>
        /// False if the conversion will fail with a <see cref="InvalidCastException" /> at runtime if the cast fails. This is true for C#'s
        /// <c>as</c> operator and for VB's <c>TryCast</c> operator.
        /// </summary>
        bool IsTryCast { get; }
        /// <summary>
        /// True if the conversion can fail at runtime with an overflow exception. This corresponds to C# checked and unchecked blocks.
        /// </summary>
        bool IsChecked { get; }
    }
    /// <summary>
    /// Represents an invocation of a method.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# method invocation expression</description></item>
    ///   <item>
    ///     <description>
    ///       C# collection element initializer.
    ///       For example, in the following collection initializer: <c>new C() { 1, 2, 3 }</c>, we will have
    ///       3 <see cref="IInvocationOperation" /> nodes, each of which will be a call to the corresponding <c>Add</c> method
    ///       with either 1, 2, 3 as the argument
    ///     </description>
    ///   </item>
    ///   <item><description>VB method invocation expression</description></item>
    ///   <item>
    ///     <description>
    ///       VB collection element initializer.
    ///       Similar to the C# example, <c>New C() From {1, 2, 3}</c> will have 3 <see cref="IInvocationOperation" />
    ///       nodes with 1, 2, and 3 as their arguments, respectively
    ///     </description>
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Invocation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IInvocationOperation : IOperation
    {
        /// <summary>
        /// Method to be invoked.
        /// </summary>
        IMethodSymbol TargetMethod { get; }
        /// <summary>
        /// Type parameter which runtime type will be used to resolve virtual invocation of the <see cref="TargetMethod" />.
        /// Null if <see cref="TargetMethod" /> is resolved statically, or is an instance method.
        /// </summary>
        ITypeSymbol? ConstrainedToType { get; }
        /// <summary>
        /// 'This' or 'Me' instance to be supplied to the method, or null if the method is static.
        /// </summary>
        IOperation? Instance { get; }
        /// <summary>
        /// True if the invocation uses a virtual mechanism, and false otherwise.
        /// </summary>
        bool IsVirtual { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays.
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        ImmutableArray<IArgumentOperation> Arguments { get; }
    }
    /// <summary>
    /// Represents a reference to an array element.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# array element reference expression</description></item>
    ///   <item><description>VB array element reference expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ArrayElementReference"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IArrayElementReferenceOperation : IOperation
    {
        /// <summary>
        /// Array to be indexed.
        /// </summary>
        IOperation ArrayReference { get; }
        /// <summary>
        /// Indices that specify an individual element.
        /// </summary>
        ImmutableArray<IOperation> Indices { get; }
    }
    /// <summary>
    /// Represents a reference to a declared local variable.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# local reference expression</description></item>
    ///   <item><description>VB local reference expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.LocalReference"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ILocalReferenceOperation : IOperation
    {
        /// <summary>
        /// Referenced local variable.
        /// </summary>
        ILocalSymbol Local { get; }
        /// <summary>
        /// True if this reference is also the declaration site of this variable. This is true in out variable declarations
        /// and in deconstruction operations where a new variable is being declared.
        /// </summary>
        bool IsDeclaration { get; }
    }
    /// <summary>
    /// Represents a reference to a parameter.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# parameter reference expression</description></item>
    ///   <item><description>VB parameter reference expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ParameterReference"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IParameterReferenceOperation : IOperation
    {
        /// <summary>
        /// Referenced parameter.
        /// </summary>
        IParameterSymbol Parameter { get; }
    }
    /// <summary>
    /// Represents a reference to a member of a class, struct, or interface.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# member reference expression</description></item>
    ///   <item><description>VB member reference expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IMemberReferenceOperation : IOperation
    {
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        IOperation? Instance { get; }
        /// <summary>
        /// Referenced member.
        /// </summary>
        ISymbol Member { get; }
        /// <summary>
        /// Type parameter which runtime type will be used to resolve virtual invocation of the <see cref="Member" />.
        /// Null if <see cref="Member" /> is resolved statically, or is an instance member.
        /// </summary>
        ITypeSymbol? ConstrainedToType { get; }
    }
    /// <summary>
    /// Represents a reference to a field.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# field reference expression</description></item>
    ///   <item><description>VB field reference expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.FieldReference"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IFieldReferenceOperation : IMemberReferenceOperation
    {
        /// <summary>
        /// Referenced field.
        /// </summary>
        IFieldSymbol Field { get; }
        /// <summary>
        /// If the field reference is also where the field was declared.
        /// </summary>
        /// <remarks>
        /// This is only ever true in CSharp scripts, where a top-level statement creates a new variable
        /// in a reference, such as an out variable declaration or a deconstruction declaration.
        /// </remarks>
        bool IsDeclaration { get; }
    }
    /// <summary>
    /// Represents a reference to a method other than as the target of an invocation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# method reference expression</description></item>
    ///   <item><description>VB method reference expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.MethodReference"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IMethodReferenceOperation : IMemberReferenceOperation
    {
        /// <summary>
        /// Referenced method.
        /// </summary>
        IMethodSymbol Method { get; }
        /// <summary>
        /// Indicates whether the reference uses virtual semantics.
        /// </summary>
        bool IsVirtual { get; }
    }
    /// <summary>
    /// Represents a reference to a property.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# property reference expression</description></item>
    ///   <item><description>VB property reference expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.PropertyReference"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IPropertyReferenceOperation : IMemberReferenceOperation
    {
        /// <summary>
        /// Referenced property.
        /// </summary>
        IPropertySymbol Property { get; }
        /// <summary>
        /// Arguments of the indexer property reference, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays.
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        ImmutableArray<IArgumentOperation> Arguments { get; }
    }
    /// <summary>
    /// Represents a reference to an event.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# event reference expression</description></item>
    ///   <item><description>VB event reference expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.EventReference"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IEventReferenceOperation : IMemberReferenceOperation
    {
        /// <summary>
        /// Referenced event.
        /// </summary>
        IEventSymbol Event { get; }
    }
    /// <summary>
    /// Represents an operation with one operand and a unary operator.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# unary operation expression</description></item>
    ///   <item><description>VB unary operation expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Unary"/></description></item>
    /// <item><description><see cref="OperationKind.UnaryOperator"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IUnaryOperation : IOperation
    {
        /// <summary>
        /// Kind of unary operation.
        /// </summary>
        UnaryOperatorKind OperatorKind { get; }
        /// <summary>
        /// Operand.
        /// </summary>
        IOperation Operand { get; }
        /// <summary>
        /// <see langword="true" /> if this is a 'lifted' unary operator.  When there is an
        /// operator that is defined to work on a value type, 'lifted' operators are
        /// created to work on the <see cref="System.Nullable{T}" /> versions of those
        /// value types.
        /// </summary>
        bool IsLifted { get; }
        /// <summary>
        /// <see langword="true" /> if overflow checking is performed for the arithmetic operation.
        /// </summary>
        bool IsChecked { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol? OperatorMethod { get; }
        /// <summary>
        /// Type parameter which runtime type will be used to resolve virtual invocation of the <see cref="OperatorMethod" />, if any.
        /// Null if <see cref="OperatorMethod" /> is resolved statically, or is null.
        /// </summary>
        ITypeSymbol? ConstrainedToType { get; }
    }
    /// <summary>
    /// Represents an operation with two operands and a binary operator that produces a result with a non-null type.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# binary operator expression</description></item>
    ///   <item><description>VB binary operator expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Binary"/></description></item>
    /// <item><description><see cref="OperationKind.BinaryOperator"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IBinaryOperation : IOperation
    {
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        BinaryOperatorKind OperatorKind { get; }
        /// <summary>
        /// Left operand.
        /// </summary>
        IOperation LeftOperand { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        IOperation RightOperand { get; }
        /// <summary>
        /// <see langword="true" /> if this is a 'lifted' binary operator.  When there is an
        /// operator that is defined to work on a value type, 'lifted' operators are
        /// created to work on the <see cref="System.Nullable{T}" /> versions of those
        /// value types.
        /// </summary>
        bool IsLifted { get; }
        /// <summary>
        /// <see langword="true" /> if this is a 'checked' binary operator.
        /// </summary>
        bool IsChecked { get; }
        /// <summary>
        /// <see langword="true" /> if the comparison is text based for string or object comparison in VB.
        /// </summary>
        bool IsCompareText { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol? OperatorMethod { get; }
        /// <summary>
        /// Type parameter which runtime type will be used to resolve virtual invocation of the <see cref="OperatorMethod" />
        /// or corresponding true/false operator, if any.
        /// Null if operators are resolved statically, or are not used.
        /// </summary>
        ITypeSymbol? ConstrainedToType { get; }
    }
    /// <summary>
    /// Represents a conditional operation with:
    /// <list type="number">
    ///   <item><description><see cref="Condition" /> to be tested</description></item>
    ///   <item><description><see cref="WhenTrue" /> operation to be executed when <see cref="Condition" /> is true and</description></item>
    ///   <item><description><see cref="WhenFalse" /> operation to be executed when the <see cref="Condition" /> is false</description></item>
    /// </list>
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# ternary expression <c>a ? b : c</c> and if statement</description></item>
    ///   <item><description>VB ternary expression <c>If(a, b, c)</c> and If Else statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Conditional"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IConditionalOperation : IOperation
    {
        /// <summary>
        /// Condition to be tested.
        /// </summary>
        IOperation Condition { get; }
        /// <summary>
        /// Operation to be executed if the <see cref="Condition" /> is true.
        /// </summary>
        IOperation WhenTrue { get; }
        /// <summary>
        /// Operation to be executed if the <see cref="Condition" /> is false.
        /// </summary>
        IOperation? WhenFalse { get; }
        /// <summary>
        /// Is result a managed reference
        /// </summary>
        bool IsRef { get; }
    }
    /// <summary>
    /// Represents a coalesce operation with two operands:
    /// <list type="number">
    ///   <item><description><see cref="Value" />, which is the first operand that is unconditionally evaluated and is the result of the operation if non null</description></item>
    ///   <item><description><see cref="WhenNull" />, which is the second operand that is conditionally evaluated and is the result of the operation if <see cref="Value" /> is null</description></item>
    /// </list>
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# null-coalescing expression <c>Value ?? WhenNull</c></description></item>
    ///   <item><description>VB binary conditional expression <c>If(Value, WhenNull)</c></description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Coalesce"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
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
    /// <summary>
    /// Represents an anonymous function operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# lambda expression</description></item>
    ///   <item><description>VB anonymous delegate expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.AnonymousFunction"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IAnonymousFunctionOperation : IOperation
    {
        /// <summary>
        /// Symbol of the anonymous function.
        /// </summary>
        IMethodSymbol Symbol { get; }
        /// <summary>
        /// Body of the anonymous function.
        /// </summary>
        IBlockOperation Body { get; }
    }
    /// <summary>
    /// Represents creation of an object instance.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# new expression</description></item>
    ///   <item><description>VB New expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ObjectCreation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IObjectCreationOperation : IOperation
    {
        /// <summary>
        /// Constructor to be invoked on the created instance.
        /// </summary>
        IMethodSymbol? Constructor { get; }
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        IObjectOrCollectionInitializerOperation? Initializer { get; }
        /// <summary>
        /// Arguments of the object creation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays.
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        ImmutableArray<IArgumentOperation> Arguments { get; }
    }
    /// <summary>
    /// Represents a creation of a type parameter object, i.e. new T(), where T is a type parameter with new constraint.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# type parameter object creation expression</description></item>
    ///   <item><description>VB type parameter object creation expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.TypeParameterObjectCreation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ITypeParameterObjectCreationOperation : IOperation
    {
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        IObjectOrCollectionInitializerOperation? Initializer { get; }
    }
    /// <summary>
    /// Represents the creation of an array instance.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# array creation expression</description></item>
    ///   <item><description>VB array creation expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ArrayCreation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IArrayCreationOperation : IOperation
    {
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        ImmutableArray<IOperation> DimensionSizes { get; }
        /// <summary>
        /// Values of elements of the created array instance.
        /// </summary>
        IArrayInitializerOperation? Initializer { get; }
    }
    /// <summary>
    /// Represents an implicit/explicit reference to an instance.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# this or base expression</description></item>
    ///   <item><description>VB Me, MyClass, or MyBase expression</description></item>
    ///   <item><description>C# object or collection or 'with' expression initializers</description></item>
    ///   <item><description>VB With statements, object or collection initializers</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.InstanceReference"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IInstanceReferenceOperation : IOperation
    {
        /// <summary>
        /// The kind of reference that is being made.
        /// </summary>
        InstanceReferenceKind ReferenceKind { get; }
    }
    /// <summary>
    /// Represents an operation that tests if a value is of a specific type.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# "is" operator expression</description></item>
    ///   <item><description>VB "TypeOf" and "TypeOf IsNot" expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.IsType"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IIsTypeOperation : IOperation
    {
        /// <summary>
        /// Value to test.
        /// </summary>
        IOperation ValueOperand { get; }
        /// <summary>
        /// Type for which to test.
        /// </summary>
        ITypeSymbol TypeOperand { get; }
        /// <summary>
        /// Flag indicating if this is an "is not" type expression.
        /// True for VB "TypeOf ... IsNot ..." expression.
        /// False, otherwise.
        /// </summary>
        bool IsNegated { get; }
    }
    /// <summary>
    /// Represents an await operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# await expression</description></item>
    ///   <item><description>VB await expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Await"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IAwaitOperation : IOperation
    {
        /// <summary>
        /// Awaited operation.
        /// </summary>
        IOperation Operation { get; }
    }
    /// <summary>
    /// Represents a base interface for assignments.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# simple, compound and deconstruction assignment expressions</description></item>
    ///   <item><description>VB simple and compound assignment expressions</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IAssignmentOperation : IOperation
    {
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        IOperation Target { get; }
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        IOperation Value { get; }
    }
    /// <summary>
    /// Represents a simple assignment operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# simple assignment expression</description></item>
    ///   <item><description>VB simple assignment expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.SimpleAssignment"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ISimpleAssignmentOperation : IAssignmentOperation
    {
        /// <summary>
        /// Is this a ref assignment
        /// </summary>
        bool IsRef { get; }
    }
    /// <summary>
    /// Represents a compound assignment that mutates the target with the result of a binary operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# compound assignment expression</description></item>
    ///   <item><description>VB compound assignment expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.CompoundAssignment"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ICompoundAssignmentOperation : IAssignmentOperation
    {
        /// <summary>
        /// Conversion applied to <see cref="IAssignmentOperation.Target" /> before the operation occurs.
        /// </summary>
        CommonConversion InConversion { get; }
        /// <summary>
        /// Conversion applied to the result of the binary operation, before it is assigned back to
        /// <see cref="IAssignmentOperation.Target" />.
        /// </summary>
        CommonConversion OutConversion { get; }
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        BinaryOperatorKind OperatorKind { get; }
        /// <summary>
        /// <see langword="true" /> if this assignment contains a 'lifted' binary operation.
        /// </summary>
        bool IsLifted { get; }
        /// <summary>
        /// <see langword="true" /> if overflow checking is performed for the arithmetic operation.
        /// </summary>
        bool IsChecked { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol? OperatorMethod { get; }
        /// <summary>
        /// Type parameter which runtime type will be used to resolve virtual invocation of the <see cref="OperatorMethod" />, if any.
        /// Null if <see cref="OperatorMethod" /> is resolved statically, or is null.
        /// </summary>
        ITypeSymbol? ConstrainedToType { get; }
    }
    /// <summary>
    /// Represents a parenthesized operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>VB parenthesized expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Parenthesized"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IParenthesizedOperation : IOperation
    {
        /// <summary>
        /// Operand enclosed in parentheses.
        /// </summary>
        IOperation Operand { get; }
    }
    /// <summary>
    /// Represents a binding of an event.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# event assignment expression</description></item>
    ///   <item><description>VB Add/Remove handler statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.EventAssignment"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IEventAssignmentOperation : IOperation
    {
        /// <summary>
        /// Reference to the event being bound.
        /// </summary>
        IOperation EventReference { get; }
        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        IOperation HandlerValue { get; }
        /// <summary>
        /// True for adding a binding, false for removing one.
        /// </summary>
        bool Adds { get; }
    }
    /// <summary>
    /// Represents a conditionally accessed operation. Note that <see cref="IConditionalAccessInstanceOperation" /> is used to refer to the value
    /// of <see cref="Operation" /> within <see cref="WhenNotNull" />.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# conditional access expression (<c>?</c> or <c>?.</c> operator)</description></item>
    ///   <item><description>VB conditional access expression (<c>?</c> or <c>?.</c> operator)</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ConditionalAccess"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IConditionalAccessOperation : IOperation
    {
        /// <summary>
        /// Operation that will be evaluated and accessed if non null.
        /// </summary>
        IOperation Operation { get; }
        /// <summary>
        /// Operation to be evaluated if <see cref="Operation" /> is non null.
        /// </summary>
        IOperation WhenNotNull { get; }
    }
    /// <summary>
    /// Represents the value of a conditionally-accessed operation within <see cref="IConditionalAccessOperation.WhenNotNull" />.
    /// For a conditional access operation of the form <c>someExpr?.Member</c>, this operation is used as the InstanceReceiver for the right operation <c>Member</c>.
    /// See https://github.com/dotnet/roslyn/issues/21279#issuecomment-323153041 for more details.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# conditional access instance expression</description></item>
    ///   <item><description>VB conditional access instance expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ConditionalAccessInstance"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IConditionalAccessInstanceOperation : IOperation
    {
    }
    /// <summary>
    /// Represents an interpolated string.
    /// <para>
    /// Current usage:
    ///  (1) C# interpolated string expression.
    ///  (2) VB interpolated string expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.InterpolatedString"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IInterpolatedStringOperation : IOperation
    {
        /// <summary>
        /// Constituent parts of interpolated string, each of which is an <see cref="IInterpolatedStringContentOperation" />.
        /// </summary>
        ImmutableArray<IInterpolatedStringContentOperation> Parts { get; }
    }
    /// <summary>
    /// Represents a creation of anonymous object.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# <c>new { ... }</c> expression</description></item>
    ///   <item><description>VB <c>New With { ... }</c> expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.AnonymousObjectCreation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IAnonymousObjectCreationOperation : IOperation
    {
        /// <summary>
        /// Property initializers.
        /// Each initializer is an <see cref="ISimpleAssignmentOperation" />, with an <see cref="IPropertyReferenceOperation" />
        /// as the target whose Instance is an <see cref="IInstanceReferenceOperation" /> with <see cref="InstanceReferenceKind.ImplicitReceiver" /> kind.
        /// </summary>
        ImmutableArray<IOperation> Initializers { get; }
    }
    /// <summary>
    /// Represents an initialization for an object or collection creation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       C# object or collection initializer expression.
    ///       For example, object initializer <c>{ X = x }</c> within object creation <c>new Class() { X = x }</c> and
    ///       collection initializer <c>{ x, y, 3 }</c> within collection creation <c>new MyList() { x, y, 3 }</c>
    ///     </description>
    ///   </item>
    ///   <item><description>VB object or collection initializer expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ObjectOrCollectionInitializer"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IObjectOrCollectionInitializerOperation : IOperation
    {
        /// <summary>
        /// Object member or collection initializers.
        /// </summary>
        ImmutableArray<IOperation> Initializers { get; }
    }
    /// <summary>
    /// Represents an initialization of member within an object initializer with a nested object or collection initializer.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       C# nested member initializer expression.
    ///       For example, given an object creation with initializer <c>new Class() { X = x, Y = { x, y, 3 }, Z = { X = z } }</c>,
    ///       member initializers for Y and Z, i.e. <c>Y = { x, y, 3 }</c>, and <c>Z = { X = z }</c> are nested member initializers represented by this operation
    ///     </description>
    ///   </item>
    ///   <item><description>VB object or collection initializer expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.MemberInitializer"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IMemberInitializerOperation : IOperation
    {
        /// <summary>
        /// Initialized member reference <see cref="IMemberReferenceOperation" /> or an invalid operation for error cases.
        /// </summary>
        IOperation InitializedMember { get; }
        /// <summary>
        /// Member initializer.
        /// </summary>
        IObjectOrCollectionInitializerOperation Initializer { get; }
    }
    /// <summary>
    /// Obsolete interface that used to represent a collection element initializer. It has been replaced by
    /// <see cref="IInvocationOperation" /> and <see cref="IDynamicInvocationOperation" />, as appropriate.
    /// <para>
    /// Current usage:
    ///   None. This API has been obsoleted in favor of <see cref="IInvocationOperation" /> and <see cref="IDynamicInvocationOperation" />.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.CollectionElementInitializer"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    [Obsolete("ICollectionElementInitializerOperation has been replaced with " + nameof(IInvocationOperation) + " and " + nameof(IDynamicInvocationOperation), error: true)]
    public interface ICollectionElementInitializerOperation : IOperation
    {
        IMethodSymbol AddMethod { get; }
        ImmutableArray<IOperation> Arguments { get; }
        bool IsDynamic { get; }
    }
    /// <summary>
    /// Represents an operation that gets a string value for the <see cref="Argument" /> name.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# nameof expression</description></item>
    ///   <item><description>VB NameOf expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.NameOf"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface INameOfOperation : IOperation
    {
        /// <summary>
        /// Argument to the name of operation.
        /// </summary>
        IOperation Argument { get; }
    }
    /// <summary>
    /// Represents a tuple with one or more elements.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# tuple expression</description></item>
    ///   <item><description>VB tuple expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Tuple"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ITupleOperation : IOperation
    {
        /// <summary>
        /// Tuple elements.
        /// </summary>
        ImmutableArray<IOperation> Elements { get; }
        /// <summary>
        /// Natural type of the tuple, or null if tuple doesn't have a natural type.
        /// Natural type can be different from <see cref="IOperation.Type" /> depending on the
        /// conversion context, in which the tuple is used.
        /// </summary>
        ITypeSymbol? NaturalType { get; }
    }
    /// <summary>
    /// Represents an object creation with a dynamically bound constructor.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# <c>new</c> expression with dynamic argument(s)</description></item>
    ///   <item><description>VB late bound <c>New</c> expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.DynamicObjectCreation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IDynamicObjectCreationOperation : IOperation
    {
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        IObjectOrCollectionInitializerOperation? Initializer { get; }
        /// <summary>
        /// Dynamically bound arguments, excluding the instance argument.
        /// </summary>
        ImmutableArray<IOperation> Arguments { get; }
    }
    /// <summary>
    /// Represents a reference to a member of a class, struct, or module that is dynamically bound.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# dynamic member reference expression</description></item>
    ///   <item><description>VB late bound member reference expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.DynamicMemberReference"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IDynamicMemberReferenceOperation : IOperation
    {
        /// <summary>
        /// Instance receiver, if it exists.
        /// </summary>
        IOperation? Instance { get; }
        /// <summary>
        /// Referenced member.
        /// </summary>
        string MemberName { get; }
        /// <summary>
        /// Type arguments.
        /// </summary>
        ImmutableArray<ITypeSymbol> TypeArguments { get; }
        /// <summary>
        /// The containing type of the referenced member, if different from type of the <see cref="Instance" />.
        /// </summary>
        ITypeSymbol? ContainingType { get; }
    }
    /// <summary>
    /// Represents a invocation that is dynamically bound.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# dynamic invocation expression</description></item>
    ///   <item>
    ///     <description>
    ///       C# dynamic collection element initializer.
    ///       For example, in the following collection initializer: <c>new C() { do1, do2, do3 }</c> where
    ///       the doX objects are of type dynamic, we'll have 3 <see cref="IDynamicInvocationOperation" /> with do1, do2, and
    ///       do3 as their arguments
    ///     </description>
    ///   </item>
    ///   <item><description>VB late bound invocation expression</description></item>
    ///   <item>
    ///     <description>
    ///       VB dynamic collection element initializer.
    ///       Similar to the C# example, <c>New C() From {do1, do2, do3}</c> will generate 3 <see cref="IDynamicInvocationOperation" />
    ///       nodes with do1, do2, and do3 as their arguments, respectively
    ///     </description>
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.DynamicInvocation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IDynamicInvocationOperation : IOperation
    {
        /// <summary>
        /// Dynamically or late bound operation.
        /// </summary>
        IOperation Operation { get; }
        /// <summary>
        /// Dynamically bound arguments, excluding the instance argument.
        /// </summary>
        ImmutableArray<IOperation> Arguments { get; }
    }
    /// <summary>
    /// Represents an indexer access that is dynamically bound.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# dynamic indexer access expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.DynamicIndexerAccess"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IDynamicIndexerAccessOperation : IOperation
    {
        /// <summary>
        /// Dynamically indexed operation.
        /// </summary>
        IOperation Operation { get; }
        /// <summary>
        /// Dynamically bound arguments, excluding the instance argument.
        /// </summary>
        ImmutableArray<IOperation> Arguments { get; }
    }
    /// <summary>
    /// Represents an unrolled/lowered query operation.
    /// For example, for a C# query expression "from x in set where x.Name != null select x.Name", the Operation tree has the following shape:
    ///   ITranslatedQueryExpression
    ///     IInvocationExpression ('Select' invocation for "select x.Name")
    ///       IInvocationExpression ('Where' invocation for "where x.Name != null")
    ///         IInvocationExpression ('From' invocation for "from x in set")
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# query expression</description></item>
    ///   <item><description>VB query expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.TranslatedQuery"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ITranslatedQueryOperation : IOperation
    {
        /// <summary>
        /// Underlying unrolled operation.
        /// </summary>
        IOperation Operation { get; }
    }
    /// <summary>
    /// Represents a delegate creation. This is created whenever a new delegate is created.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# delegate creation expression</description></item>
    ///   <item><description>VB delegate creation expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.DelegateCreation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IDelegateCreationOperation : IOperation
    {
        /// <summary>
        /// The lambda or method binding that this delegate is created from.
        /// </summary>
        IOperation Target { get; }
    }
    /// <summary>
    /// Represents a default value operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# default value expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.DefaultValue"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IDefaultValueOperation : IOperation
    {
    }
    /// <summary>
    /// Represents an operation that gets <see cref="System.Type" /> for the given <see cref="TypeOperand" />.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# typeof expression</description></item>
    ///   <item><description>VB GetType expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.TypeOf"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ITypeOfOperation : IOperation
    {
        /// <summary>
        /// Type operand.
        /// </summary>
        ITypeSymbol TypeOperand { get; }
    }
    /// <summary>
    /// Represents an operation to compute the size of a given type.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# sizeof expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.SizeOf"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ISizeOfOperation : IOperation
    {
        /// <summary>
        /// Type operand.
        /// </summary>
        ITypeSymbol TypeOperand { get; }
    }
    /// <summary>
    /// Represents an operation that creates a pointer value by taking the address of a reference.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# address of expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.AddressOf"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IAddressOfOperation : IOperation
    {
        /// <summary>
        /// Addressed reference.
        /// </summary>
        IOperation Reference { get; }
    }
    /// <summary>
    /// Represents an operation that tests if a value matches a specific pattern.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# is pattern expression. For example, <c>x is int i</c></description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.IsPattern"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IIsPatternOperation : IOperation
    {
        /// <summary>
        /// Underlying operation to test.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Pattern.
        /// </summary>
        IPatternOperation Pattern { get; }
    }
    /// <summary>
    /// Represents an <see cref="OperationKind.Increment" /> or <see cref="OperationKind.Decrement" /> operation.
    /// Note that this operation is different from an <see cref="IUnaryOperation" /> as it mutates the <see cref="Target" />,
    /// while unary operator expression does not mutate it's operand.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# increment expression or decrement expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Increment"/></description></item>
    /// <item><description><see cref="OperationKind.Decrement"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IIncrementOrDecrementOperation : IOperation
    {
        /// <summary>
        /// <see langword="true" /> if this is a postfix expression. <see langword="false" /> if this is a prefix expression.
        /// </summary>
        bool IsPostfix { get; }
        /// <summary>
        /// <see langword="true" /> if this is a 'lifted' increment operator.  When there
        /// is an operator that is defined to work on a value type, 'lifted' operators are
        /// created to work on the <see cref="System.Nullable{T}" /> versions of those
        /// value types.
        /// </summary>
        bool IsLifted { get; }
        /// <summary>
        /// <see langword="true" /> if overflow checking is performed for the arithmetic operation.
        /// </summary>
        bool IsChecked { get; }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        IOperation Target { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol? OperatorMethod { get; }
        /// <summary>
        /// Type parameter which runtime type will be used to resolve virtual invocation of the <see cref="OperatorMethod" />, if any.
        /// Null if <see cref="OperatorMethod" /> is resolved statically, or is null.
        /// </summary>
        ITypeSymbol? ConstrainedToType { get; }
    }
    /// <summary>
    /// Represents an operation to throw an exception.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# throw expression</description></item>
    ///   <item><description>C# throw statement</description></item>
    ///   <item><description>VB Throw statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Throw"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IThrowOperation : IOperation
    {
        /// <summary>
        /// Instance of an exception being thrown.
        /// </summary>
        IOperation? Exception { get; }
    }
    /// <summary>
    /// Represents a assignment with a deconstruction.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# deconstruction assignment expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.DeconstructionAssignment"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IDeconstructionAssignmentOperation : IAssignmentOperation
    {
    }
    /// <summary>
    /// Represents a declaration expression operation. Unlike a regular variable declaration <see cref="IVariableDeclaratorOperation" /> and <see cref="IVariableDeclarationOperation" />, this operation represents an "expression" declaring a variable.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       C# deconstruction assignment expression. For example:
    ///       <list type="bullet">
    ///         <item><description><c>var (x, y)</c> is a deconstruction declaration expression with variables <c>x</c> and <c>y</c></description></item>
    ///         <item><description><c>(var x, var y)</c> is a tuple expression with two declaration expressions</description></item>
    ///         <item><description><c>M(out var x);</c> is an invocation expression with an out <c>var x</c> declaration expression</description></item>
    ///       </list>
    ///     </description>
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.DeclarationExpression"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IDeclarationExpressionOperation : IOperation
    {
        /// <summary>
        /// Underlying expression.
        /// </summary>
        IOperation Expression { get; }
    }
    /// <summary>
    /// Represents an argument value that has been omitted in an invocation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>VB omitted argument in an invocation expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.OmittedArgument"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IOmittedArgumentOperation : IOperation
    {
    }
    /// <summary>
    /// Represents an initializer for a field, property, parameter or a local variable declaration.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# field, property, parameter or local variable initializer</description></item>
    ///   <item><description>VB field(s), property, parameter or local variable initializer</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ISymbolInitializerOperation : IOperation
    {
        /// <summary>
        /// Local declared in and scoped to the <see cref="Value" />.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Underlying initializer value.
        /// </summary>
        IOperation Value { get; }
    }
    /// <summary>
    /// Represents an initialization of a field.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# field initializer with equals value clause</description></item>
    ///   <item><description>VB field(s) initializer with equals value clause or AsNew clause. Multiple fields can be initialized with AsNew clause in VB</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.FieldInitializer"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IFieldInitializerOperation : ISymbolInitializerOperation
    {
        /// <summary>
        /// Initialized fields. There can be multiple fields for Visual Basic fields declared with AsNew clause.
        /// </summary>
        ImmutableArray<IFieldSymbol> InitializedFields { get; }
    }
    /// <summary>
    /// Represents an initialization of a local variable.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# local variable initializer with equals value clause</description></item>
    ///   <item><description>VB local variable initializer with equals value clause or AsNew clause</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.VariableInitializer"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IVariableInitializerOperation : ISymbolInitializerOperation
    {
    }
    /// <summary>
    /// Represents an initialization of a property.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# property initializer with equals value clause</description></item>
    ///   <item><description>VB property initializer with equals value clause or AsNew clause. Multiple properties can be initialized with 'WithEvents' declaration with AsNew clause in VB</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.PropertyInitializer"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IPropertyInitializerOperation : ISymbolInitializerOperation
    {
        /// <summary>
        /// Initialized properties. There can be multiple properties for Visual Basic 'WithEvents' declaration with AsNew clause.
        /// </summary>
        ImmutableArray<IPropertySymbol> InitializedProperties { get; }
    }
    /// <summary>
    /// Represents an initialization of a parameter at the point of declaration.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# parameter initializer with equals value clause</description></item>
    ///   <item><description>VB parameter initializer with equals value clause</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ParameterInitializer"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IParameterInitializerOperation : ISymbolInitializerOperation
    {
        /// <summary>
        /// Initialized parameter.
        /// </summary>
        IParameterSymbol Parameter { get; }
    }
    /// <summary>
    /// Represents the initialization of an array instance.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# array initializer</description></item>
    ///   <item><description>VB array initializer</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ArrayInitializer"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IArrayInitializerOperation : IOperation
    {
        /// <summary>
        /// Values to initialize array elements.
        /// </summary>
        ImmutableArray<IOperation> ElementValues { get; }
    }
    /// <summary>
    /// Represents a single variable declarator and initializer.
    /// <para>
    /// Current Usage:
    /// <list type="number">
    ///   <item><description>C# variable declarator</description></item>
    ///   <item><description>C# catch variable declaration</description></item>
    ///   <item><description>VB single variable declaration</description></item>
    ///   <item><description>VB catch variable declaration</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// In VB, the initializer for this node is only ever used for explicit array bounds initializers. This node corresponds to
    /// the VariableDeclaratorSyntax in C# and the ModifiedIdentifierSyntax in VB.
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.VariableDeclarator"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IVariableDeclaratorOperation : IOperation
    {
        /// <summary>
        /// Symbol declared by this variable declaration
        /// </summary>
        ILocalSymbol Symbol { get; }
        /// <summary>
        /// Optional initializer of the variable.
        /// </summary>
        /// <remarks>
        /// If this variable is in an <see cref="IVariableDeclarationOperation" />, the initializer may be located
        /// in the parent operation. Call <see cref="OperationExtensions.GetVariableInitializer(IVariableDeclaratorOperation)" />
        /// to check in all locations. It is only possible to have initializers in both locations in VB invalid code scenarios.
        /// </remarks>
        IVariableInitializerOperation? Initializer { get; }
        /// <summary>
        /// Additional arguments supplied to the declarator in error cases, ignored by the compiler. This only used for the C# case of
        /// DeclaredArgumentSyntax nodes on a VariableDeclaratorSyntax.
        /// </summary>
        ImmutableArray<IOperation> IgnoredArguments { get; }
    }
    /// <summary>
    /// Represents a declarator that declares multiple individual variables.
    /// <para>
    /// Current Usage:
    /// <list type="number">
    ///   <item><description>C# VariableDeclaration</description></item>
    ///   <item><description>C# fixed declarations</description></item>
    ///   <item><description>VB Dim statement declaration groups</description></item>
    ///   <item><description>VB Using statement variable declarations</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// The initializer of this node is applied to all individual declarations in <see cref="Declarators" />. There cannot
    /// be initializers in both locations except in invalid code scenarios.
    /// In C#, this node will never have an initializer.
    /// This corresponds to the VariableDeclarationSyntax in C#, and the VariableDeclaratorSyntax in Visual Basic.
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.VariableDeclaration"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IVariableDeclarationOperation : IOperation
    {
        /// <summary>
        /// Individual variable declarations declared by this multiple declaration.
        /// </summary>
        /// <remarks>
        /// All <see cref="IVariableDeclarationGroupOperation" /> will have at least 1 <see cref="IVariableDeclarationOperation" />,
        /// even if the declaration group only declares 1 variable.
        /// </remarks>
        ImmutableArray<IVariableDeclaratorOperation> Declarators { get; }
        /// <summary>
        /// Optional initializer of the variable.
        /// </summary>
        /// <remarks>
        /// In C#, this will always be null.
        /// </remarks>
        IVariableInitializerOperation? Initializer { get; }
        /// <summary>
        /// Array dimensions supplied to an array declaration in error cases, ignored by the compiler. This is only used for the C# case of
        /// RankSpecifierSyntax nodes on an ArrayTypeSyntax.
        /// </summary>
        ImmutableArray<IOperation> IgnoredDimensions { get; }
    }
    /// <summary>
    /// Represents an argument to a method invocation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# argument to an invocation expression, object creation expression, etc.</description></item>
    ///   <item><description>VB argument to an invocation expression, object creation expression, etc.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Argument"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IArgumentOperation : IOperation
    {
        /// <summary>
        /// Kind of argument.
        /// </summary>
        ArgumentKind ArgumentKind { get; }
        /// <summary>
        /// Parameter the argument matches. This can be null for __arglist parameters.
        /// </summary>
        IParameterSymbol? Parameter { get; }
        /// <summary>
        /// Value supplied for the argument.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Information of the conversion applied to the argument value passing it into the target method. Applicable only to VB Reference arguments.
        /// </summary>
        CommonConversion InConversion { get; }
        /// <summary>
        /// Information of the conversion applied to the argument value after the invocation. Applicable only to VB Reference arguments.
        /// </summary>
        CommonConversion OutConversion { get; }
    }
    /// <summary>
    /// Represents a catch clause.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# catch clause</description></item>
    ///   <item><description>VB Catch clause</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.CatchClause"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ICatchClauseOperation : IOperation
    {
        /// <summary>
        /// Optional source for exception. This could be any of the following operation:
        /// 1. Declaration for the local catch variable bound to the caught exception (C# and VB) OR
        /// 2. Null, indicating no declaration or expression (C# and VB)
        /// 3. Reference to an existing local or parameter (VB) OR
        /// 4. Other expression for error scenarios (VB)
        /// </summary>
        IOperation? ExceptionDeclarationOrExpression { get; }
        /// <summary>
        /// Type of the exception handled by the catch clause.
        /// </summary>
        ITypeSymbol ExceptionType { get; }
        /// <summary>
        /// Locals declared by the <see cref="ExceptionDeclarationOrExpression" /> and/or <see cref="Filter" /> clause.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Filter operation to be executed to determine whether to handle the exception.
        /// </summary>
        IOperation? Filter { get; }
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        IBlockOperation Handler { get; }
    }
    /// <summary>
    /// Represents a switch case section with one or more case clauses to match and one or more operations to execute within the section.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# switch section for one or more case clause and set of statements to execute</description></item>
    ///   <item><description>VB case block with a case statement for one or more case clause and set of statements to execute</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.SwitchCase"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ISwitchCaseOperation : IOperation
    {
        /// <summary>
        /// Clauses of the case.
        /// </summary>
        ImmutableArray<ICaseClauseOperation> Clauses { get; }
        /// <summary>
        /// One or more operations to execute within the switch section.
        /// </summary>
        ImmutableArray<IOperation> Body { get; }
        /// <summary>
        /// Locals declared within the switch case section scoped to the section.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }
    /// <summary>
    /// Represents a case clause.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# case clause</description></item>
    ///   <item><description>VB Case clause</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ICaseClauseOperation : IOperation
    {
        /// <summary>
        /// Kind of the clause.
        /// </summary>
        CaseKind CaseKind { get; }
        /// <summary>
        /// Label associated with the case clause, if any.
        /// </summary>
        ILabelSymbol? Label { get; }
    }
    /// <summary>
    /// Represents a default case clause.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# default clause</description></item>
    ///   <item><description>VB Case Else clause</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IDefaultCaseClauseOperation : ICaseClauseOperation
    {
    }
    /// <summary>
    /// Represents a case clause with a pattern and an optional guard operation.
    /// <para>
    /// Current usage:
    ///  (1) C# pattern case clause.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IPatternCaseClauseOperation : ICaseClauseOperation
    {
        /// <summary>
        /// Label associated with the case clause.
        /// </summary>
        new ILabelSymbol Label { get; }
        /// <summary>
        /// Pattern associated with case clause.
        /// </summary>
        IPatternOperation Pattern { get; }
        /// <summary>
        /// Guard associated with the pattern case clause.
        /// </summary>
        IOperation? Guard { get; }
    }
    /// <summary>
    /// Represents a case clause with range of values for comparison.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>VB range case clause of the form <c>Case x To y</c></description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IRangeCaseClauseOperation : ICaseClauseOperation
    {
        /// <summary>
        /// Minimum value of the case range.
        /// </summary>
        IOperation MinimumValue { get; }
        /// <summary>
        /// Maximum value of the case range.
        /// </summary>
        IOperation MaximumValue { get; }
    }
    /// <summary>
    /// Represents a case clause with custom relational operator for comparison.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>VB relational case clause of the form <c>Case Is op x</c></description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IRelationalCaseClauseOperation : ICaseClauseOperation
    {
        /// <summary>
        /// Case value.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Relational operator used to compare the switch value with the case value.
        /// </summary>
        BinaryOperatorKind Relation { get; }
    }
    /// <summary>
    /// Represents a case clause with a single value for comparison.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# case clause of the form <c>case x</c></description></item>
    ///   <item><description>VB case clause of the form <c>Case x</c></description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ISingleValueCaseClauseOperation : ICaseClauseOperation
    {
        /// <summary>
        /// Case value.
        /// </summary>
        IOperation Value { get; }
    }
    /// <summary>
    /// Represents a constituent part of an interpolated string.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# interpolated string content</description></item>
    ///   <item><description>VB interpolated string content</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IInterpolatedStringContentOperation : IOperation
    {
    }
    /// <summary>
    /// Represents a constituent string literal part of an interpolated string operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# interpolated string text</description></item>
    ///   <item><description>VB interpolated string text</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.InterpolatedStringText"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IInterpolatedStringTextOperation : IInterpolatedStringContentOperation
    {
        /// <summary>
        /// Text content.
        /// </summary>
        IOperation Text { get; }
    }
    /// <summary>
    /// Represents a constituent interpolation part of an interpolated string operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# interpolation part</description></item>
    ///   <item><description>VB interpolation part</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Interpolation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IInterpolationOperation : IInterpolatedStringContentOperation
    {
        /// <summary>
        /// Expression of the interpolation.
        /// </summary>
        IOperation Expression { get; }
        /// <summary>
        /// Optional alignment of the interpolation.
        /// </summary>
        IOperation? Alignment { get; }
        /// <summary>
        /// Optional format string of the interpolation.
        /// </summary>
        IOperation? FormatString { get; }
    }
    /// <summary>
    /// Represents a pattern matching operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# pattern</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IPatternOperation : IOperation
    {
        /// <summary>
        /// The input type to the pattern-matching operation.
        /// </summary>
        ITypeSymbol InputType { get; }
        /// <summary>
        /// The narrowed type of the pattern-matching operation.
        /// </summary>
        ITypeSymbol NarrowedType { get; }
    }
    /// <summary>
    /// Represents a pattern with a constant value.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# constant pattern</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ConstantPattern"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IConstantPatternOperation : IPatternOperation
    {
        /// <summary>
        /// Constant value of the pattern operation.
        /// </summary>
        IOperation Value { get; }
    }
    /// <summary>
    /// Represents a pattern that declares a symbol.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# declaration pattern</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.DeclarationPattern"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IDeclarationPatternOperation : IPatternOperation
    {
        /// <summary>
        /// The type explicitly specified, or null if it was inferred (e.g. using <see langword="var" /> in C#).
        /// </summary>
        ITypeSymbol? MatchedType { get; }
        /// <summary>
        /// True if the pattern is of a form that accepts null.
        /// For example, in C# the pattern `var x` will match a null input,
        /// while the pattern `string x` will not.
        /// </summary>
        bool MatchesNull { get; }
        /// <summary>
        /// Symbol declared by the pattern, if any.
        /// </summary>
        ISymbol? DeclaredSymbol { get; }
    }
    /// <summary>
    /// Represents a comparison of two operands that returns a bool type.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# tuple binary operator expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.TupleBinary"/></description></item>
    /// <item><description><see cref="OperationKind.TupleBinaryOperator"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ITupleBinaryOperation : IOperation
    {
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        BinaryOperatorKind OperatorKind { get; }
        /// <summary>
        /// Left operand.
        /// </summary>
        IOperation LeftOperand { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        IOperation RightOperand { get; }
    }
    /// <summary>
    /// Represents a method body operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# method body</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IMethodBodyBaseOperation : IOperation
    {
        /// <summary>
        /// Method body corresponding to BaseMethodDeclarationSyntax.Body or AccessorDeclarationSyntax.Body
        /// </summary>
        IBlockOperation? BlockBody { get; }
        /// <summary>
        /// Method body corresponding to BaseMethodDeclarationSyntax.ExpressionBody or AccessorDeclarationSyntax.ExpressionBody
        /// </summary>
        IBlockOperation? ExpressionBody { get; }
    }
    /// <summary>
    /// Represents a method body operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# method body for non-constructor</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.MethodBody"/></description></item>
    /// <item><description><see cref="OperationKind.MethodBodyOperation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IMethodBodyOperation : IMethodBodyBaseOperation
    {
    }
    /// <summary>
    /// Represents a constructor method body operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# method body for constructor declaration</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ConstructorBody"/></description></item>
    /// <item><description><see cref="OperationKind.ConstructorBodyOperation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IConstructorBodyOperation : IMethodBodyBaseOperation
    {
        /// <summary>
        /// Local declarations contained within the <see cref="Initializer" />.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Constructor initializer, if any.
        /// </summary>
        IOperation? Initializer { get; }
    }
    /// <summary>
    /// Represents a discard operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# discard expressions</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Discard"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IDiscardOperation : IOperation
    {
        /// <summary>
        /// The symbol of the discard operation.
        /// </summary>
        IDiscardSymbol DiscardSymbol { get; }
    }
    /// <summary>
    /// Represents a coalesce assignment operation with a target and a conditionally-evaluated value:
    /// <list type="number">
    ///   <item><description><see cref="IAssignmentOperation.Target" /> is evaluated for null. If it is null, <see cref="IAssignmentOperation.Value" /> is evaluated and assigned to target</description></item>
    ///   <item><description><see cref="IAssignmentOperation.Value" /> is conditionally evaluated if <see cref="IAssignmentOperation.Target" /> is null, and the result is assigned into <see cref="IAssignmentOperation.Target" /></description></item>
    /// </list>
    /// The result of the entire expression is <see cref="IAssignmentOperation.Target" />, which is only evaluated once.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# null-coalescing assignment operation <c>Target ??= Value</c></description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.CoalesceAssignment"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ICoalesceAssignmentOperation : IAssignmentOperation
    {
    }
    /// <summary>
    /// Represents a range operation.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# range expressions</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Range"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IRangeOperation : IOperation
    {
        /// <summary>
        /// Left operand.
        /// </summary>
        IOperation? LeftOperand { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        IOperation? RightOperand { get; }
        /// <summary>
        /// <see langword="true" /> if this is a 'lifted' range operation.  When there is an
        /// operator that is defined to work on a value type, 'lifted' operators are
        /// created to work on the <see cref="System.Nullable{T}" /> versions of those
        /// value types.
        /// </summary>
        bool IsLifted { get; }
        /// <summary>
        /// Factory method used to create this Range value. Can be null if appropriate
        /// symbol was not found.
        /// </summary>
        IMethodSymbol? Method { get; }
    }
    /// <summary>
    /// Represents the ReDim operation to re-allocate storage space for array variables.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>VB ReDim statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ReDim"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IReDimOperation : IOperation
    {
        /// <summary>
        /// Individual clauses of the ReDim operation.
        /// </summary>
        ImmutableArray<IReDimClauseOperation> Clauses { get; }
        /// <summary>
        /// Modifier used to preserve the data in the existing array when you change the size of only the last dimension.
        /// </summary>
        bool Preserve { get; }
    }
    /// <summary>
    /// Represents an individual clause of an <see cref="IReDimOperation" /> to re-allocate storage space for a single array variable.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>VB ReDim clause</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ReDimClause"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IReDimClauseOperation : IOperation
    {
        /// <summary>
        /// Operand whose storage space needs to be re-allocated.
        /// </summary>
        IOperation Operand { get; }
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        ImmutableArray<IOperation> DimensionSizes { get; }
    }
    /// <summary>
    /// Represents a C# recursive pattern.
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.RecursivePattern"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IRecursivePatternOperation : IPatternOperation
    {
        /// <summary>
        /// The type accepted for the recursive pattern.
        /// </summary>
        ITypeSymbol MatchedType { get; }
        /// <summary>
        /// The symbol, if any, used for the fetching values for subpatterns. This is either a <c>Deconstruct</c>
        /// method, the type <c>System.Runtime.CompilerServices.ITuple</c>, or null (for example, in
        /// error cases or when matching a tuple type).
        /// </summary>
        ISymbol? DeconstructSymbol { get; }
        /// <summary>
        /// This contains the patterns contained within a deconstruction or positional subpattern.
        /// </summary>
        ImmutableArray<IPatternOperation> DeconstructionSubpatterns { get; }
        /// <summary>
        /// This contains the (symbol, property) pairs within a property subpattern.
        /// </summary>
        ImmutableArray<IPropertySubpatternOperation> PropertySubpatterns { get; }
        /// <summary>
        /// Symbol declared by the pattern.
        /// </summary>
        ISymbol? DeclaredSymbol { get; }
    }
    /// <summary>
    /// Represents a discard pattern.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# discard pattern</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.DiscardPattern"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IDiscardPatternOperation : IPatternOperation
    {
    }
    /// <summary>
    /// Represents a switch expression.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# switch expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.SwitchExpression"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
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
        /// <summary>
        /// True if the switch expressions arms cover every possible input value.
        /// </summary>
        bool IsExhaustive { get; }
    }
    /// <summary>
    /// Represents one arm of a switch expression.
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.SwitchExpressionArm"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ISwitchExpressionArmOperation : IOperation
    {
        /// <summary>
        /// The pattern to match.
        /// </summary>
        IPatternOperation Pattern { get; }
        /// <summary>
        /// Guard (when clause expression) associated with the switch arm, if any.
        /// </summary>
        IOperation? Guard { get; }
        /// <summary>
        /// Result value of the enclosing switch expression when this arm matches.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Locals declared within the switch arm (e.g. pattern locals and locals declared in the guard) scoped to the arm.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }
    /// <summary>
    /// Represents an element of a property subpattern, which identifies a member to be matched and the
    /// pattern to match it against.
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.PropertySubpattern"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IPropertySubpatternOperation : IOperation
    {
        /// <summary>
        /// The member being matched in a property subpattern.  This can be a <see cref="IMemberReferenceOperation" />
        /// in non-error cases, or an <see cref="IInvalidOperation" /> in error cases.
        /// </summary>
        IOperation Member { get; }
        /// <summary>
        /// The pattern to which the member is matched in a property subpattern.
        /// </summary>
        IPatternOperation Pattern { get; }
    }
    /// <summary>
    /// Represents a standalone VB query Aggregate operation with more than one item in Into clause.
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    internal interface IAggregateQueryOperation : IOperation
    {
        IOperation Group { get; }
        IOperation Aggregation { get; }
    }
    /// <summary>
    /// Represents a C# fixed statement.
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    internal interface IFixedOperation : IOperation
    {
        /// <summary>
        /// Locals declared.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Variables to be fixed.
        /// </summary>
        IVariableDeclarationGroupOperation Variables { get; }
        /// <summary>
        /// Body of the fixed, over which the variables are fixed.
        /// </summary>
        IOperation Body { get; }
    }
    /// <summary>
    /// Represents a creation of an instance of a NoPia interface, i.e. new I(), where I is an embedded NoPia interface.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# NoPia interface instance creation expression</description></item>
    ///   <item><description>VB NoPia interface instance creation expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    internal interface INoPiaObjectCreationOperation : IOperation
    {
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        IObjectOrCollectionInitializerOperation? Initializer { get; }
    }
    /// <summary>
    /// Represents a general placeholder when no more specific kind of placeholder is available.
    /// A placeholder is an expression whose meaning is inferred from context.
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    internal interface IPlaceholderOperation : IOperation
    {
        PlaceholderKind PlaceholderKind { get; }
    }
    /// <summary>
    /// Represents a reference through a pointer.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# pointer indirection reference expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    internal interface IPointerIndirectionReferenceOperation : IOperation
    {
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        IOperation Pointer { get; }
    }
    /// <summary>
    /// Represents a <see cref="Body" /> of operations that are executed with implicit reference to the <see cref="Value" /> for member references.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>VB With statement</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    internal interface IWithStatementOperation : IOperation
    {
        /// <summary>
        /// Body of the with.
        /// </summary>
        IOperation Body { get; }
        /// <summary>
        /// Value to whose members leading-dot-qualified references within the with body bind.
        /// </summary>
        IOperation Value { get; }
    }
    /// <summary>
    /// Represents using variable declaration, with scope spanning across the parent <see cref="IBlockOperation" />.
    /// <para>
    /// Current Usage:
    /// <list type="number">
    ///   <item><description>C# using declaration</description></item>
    ///   <item><description>C# asynchronous using declaration</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.UsingDeclaration"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IUsingDeclarationOperation : IOperation
    {
        /// <summary>
        /// The variables declared by this using declaration.
        /// </summary>
        IVariableDeclarationGroupOperation DeclarationGroup { get; }
        /// <summary>
        /// True if this is an asynchronous using declaration.
        /// </summary>
        bool IsAsynchronous { get; }
    }
    /// <summary>
    /// Represents a negated pattern.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# negated pattern</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.NegatedPattern"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface INegatedPatternOperation : IPatternOperation
    {
        /// <summary>
        /// The negated pattern.
        /// </summary>
        IPatternOperation Pattern { get; }
    }
    /// <summary>
    /// Represents a binary ("and" or "or") pattern.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# "and" and "or" patterns</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.BinaryPattern"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IBinaryPatternOperation : IPatternOperation
    {
        /// <summary>
        /// Kind of binary pattern; either <see cref="BinaryOperatorKind.And" /> or <see cref="BinaryOperatorKind.Or" />.
        /// </summary>
        BinaryOperatorKind OperatorKind { get; }
        /// <summary>
        /// The pattern on the left.
        /// </summary>
        IPatternOperation LeftPattern { get; }
        /// <summary>
        /// The pattern on the right.
        /// </summary>
        IPatternOperation RightPattern { get; }
    }
    /// <summary>
    /// Represents a pattern comparing the input with a given type.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# type pattern</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.TypePattern"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ITypePatternOperation : IPatternOperation
    {
        /// <summary>
        /// The type explicitly specified, or null if it was inferred (e.g. using <see langword="var" /> in C#).
        /// </summary>
        ITypeSymbol MatchedType { get; }
    }
    /// <summary>
    /// Represents a pattern comparing the input with a constant value using a relational operator.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# relational pattern</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.RelationalPattern"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IRelationalPatternOperation : IPatternOperation
    {
        /// <summary>
        /// The kind of the relational operator.
        /// </summary>
        BinaryOperatorKind OperatorKind { get; }
        /// <summary>
        /// Constant value of the pattern operation.
        /// </summary>
        IOperation Value { get; }
    }
    /// <summary>
    /// Represents cloning of an object instance.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# with expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.With"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IWithOperation : IOperation
    {
        /// <summary>
        /// Operand to be cloned.
        /// </summary>
        IOperation Operand { get; }
        /// <summary>
        /// Clone method to be invoked on the value. This can be null in error scenarios.
        /// </summary>
        IMethodSymbol? CloneMethod { get; }
        /// <summary>
        /// With collection initializer.
        /// </summary>
        IObjectOrCollectionInitializerOperation Initializer { get; }
    }
    /// <summary>
    /// Represents an interpolated string converted to a custom interpolated string handler type.
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.InterpolatedStringHandlerCreation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IInterpolatedStringHandlerCreationOperation : IOperation
    {
        /// <summary>
        /// The construction of the interpolated string handler instance. This can be an <see cref="IObjectCreationOperation" /> for valid code, and
        /// <see cref="IDynamicObjectCreationOperation" /> or <see cref="IInvalidOperation" /> for invalid code.
        /// </summary>
        IOperation HandlerCreation { get; }
        /// <summary>
        /// True if the last parameter of <see cref="HandlerCreation" /> is an out <see langword="bool" /> parameter that will be checked before executing the code in
        /// <see cref="Content" />. False otherwise.
        /// </summary>
        bool HandlerCreationHasSuccessParameter { get; }
        /// <summary>
        /// True if the AppendLiteral or AppendFormatted calls in nested <see cref="IInterpolatedStringOperation.Parts" /> return <see langword="bool" />. When that is true, each part
        /// will be conditional on the return of the part before it, only being executed when the Append call returns true. False otherwise.
        /// </summary>
        /// <remarks>
        /// when this is true and <see cref="HandlerCreationHasSuccessParameter" /> is true, then the first part in nested <see cref="IInterpolatedStringOperation.Parts" /> is conditionally
        /// run. If this is true and <see cref="HandlerCreationHasSuccessParameter" /> is false, then the first part is unconditionally run.
        /// <br />
        /// Just because this is true or false does not guarantee that all Append calls actually do return boolean values, as there could be dynamic calls or errors.
        /// It only governs what the compiler was expecting, based on the first calls it did see.
        /// </remarks>
        bool HandlerAppendCallsReturnBool { get; }
        /// <summary>
        /// The interpolated string expression or addition operation that makes up the content of this string. This is either an <see cref="IInterpolatedStringOperation" />
        /// or an <see cref="IInterpolatedStringAdditionOperation" /> operation.
        /// </summary>
        IOperation Content { get; }
    }
    /// <summary>
    /// Represents an addition of multiple interpolated string literals being converted to an interpolated string handler type.
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.InterpolatedStringAddition"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IInterpolatedStringAdditionOperation : IOperation
    {
        /// <summary>
        /// The interpolated string expression or addition operation on the left side of the operator. This is either an <see cref="IInterpolatedStringOperation" />
        /// or an <see cref="IInterpolatedStringAdditionOperation" /> operation.
        /// </summary>
        IOperation Left { get; }
        /// <summary>
        /// The interpolated string expression or addition operation on the right side of the operator. This is either an <see cref="IInterpolatedStringOperation" />
        /// or an <see cref="IInterpolatedStringAdditionOperation" /> operation.
        /// </summary>
        IOperation Right { get; }
    }
    /// <summary>
    /// Represents a call to either AppendLiteral or AppendFormatted as part of an interpolated string handler conversion.
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.InterpolatedStringAppendLiteral"/></description></item>
    /// <item><description><see cref="OperationKind.InterpolatedStringAppendFormatted"/></description></item>
    /// <item><description><see cref="OperationKind.InterpolatedStringAppendInvalid"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IInterpolatedStringAppendOperation : IInterpolatedStringContentOperation
    {
        /// <summary>
        /// If this interpolated string is subject to an interpolated string handler conversion, the construction of the interpolated string handler instance.
        /// This can be an <see cref="IInvocationOperation" />  or <see cref="IDynamicInvocationOperation" /> for valid code, and <see cref="IInvalidOperation" /> for invalid code.
        /// </summary>
        IOperation AppendCall { get; }
    }
    /// <summary>
    /// Represents an argument from the method call, indexer access, or constructor invocation that is creating the containing <see cref="IInterpolatedStringHandlerCreationOperation" />
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.InterpolatedStringHandlerArgumentPlaceholder"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IInterpolatedStringHandlerArgumentPlaceholderOperation : IOperation
    {
        /// <summary>
        /// The index of the argument of the method call, indexer, or object creation containing the interpolated string handler conversion this placeholder is referencing.
        /// -1 if <see cref="PlaceholderKind" /> is anything other than <see cref="InterpolatedStringArgumentPlaceholderKind.CallsiteArgument" />.
        /// </summary>
        int ArgumentIndex { get; }
        /// <summary>
        /// The component this placeholder represents.
        /// </summary>
        InterpolatedStringArgumentPlaceholderKind PlaceholderKind { get; }
    }
    /// <summary>
    /// Represents an invocation of a function pointer.
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.FunctionPointerInvocation"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IFunctionPointerInvocationOperation : IOperation
    {
        /// <summary>
        /// Invoked pointer.
        /// </summary>
        IOperation Target { get; }
        /// <summary>
        /// Arguments of the invocation. Arguments are in evaluation order.
        /// </summary>
        ImmutableArray<IArgumentOperation> Arguments { get; }
    }
    /// <summary>
    /// Represents a C# list pattern.
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ListPattern"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IListPatternOperation : IPatternOperation
    {
        /// <summary>
        /// The <c>Length</c> or <c>Count</c> property that is used to fetch the length value.
        /// Returns <c>null</c> if no such property is found.
        /// </summary>
        ISymbol? LengthSymbol { get; }
        /// <summary>
        /// The indexer that is used to fetch elements.
        /// Returns <c>null</c> for an array input.
        /// </summary>
        ISymbol? IndexerSymbol { get; }
        /// <summary>
        /// Returns subpatterns contained within the list pattern.
        /// </summary>
        ImmutableArray<IPatternOperation> Patterns { get; }
        /// <summary>
        /// Symbol declared by the pattern, if any.
        /// </summary>
        ISymbol? DeclaredSymbol { get; }
    }
    /// <summary>
    /// Represents a C# slice pattern.
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.SlicePattern"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ISlicePatternOperation : IPatternOperation
    {
        /// <summary>
        /// The range indexer or the <c>Slice</c> method used to fetch the slice value.
        /// </summary>
        ISymbol? SliceSymbol { get; }
        /// <summary>
        /// The pattern that the slice value is matched with, if any.
        /// </summary>
        IPatternOperation? Pattern { get; }
    }
    /// <summary>
    /// Represents a reference to an implicit System.Index or System.Range indexer over a non-array type.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# implicit System.Index or System.Range indexer reference expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.ImplicitIndexerReference"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IImplicitIndexerReferenceOperation : IOperation
    {
        /// <summary>
        /// Instance of the type to be indexed.
        /// </summary>
        IOperation Instance { get; }
        /// <summary>
        /// System.Index or System.Range value.
        /// </summary>
        IOperation Argument { get; }
        /// <summary>
        /// The <c>Length</c> or <c>Count</c> property that might be used to fetch the length value.
        /// </summary>
        ISymbol LengthSymbol { get; }
        /// <summary>
        /// Symbol for the underlying indexer or a slice method that is used to implement the implicit indexer.
        /// </summary>
        ISymbol IndexerSymbol { get; }
    }
    /// <summary>
    /// Represents a UTF-8 encoded byte representation of a string.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# UTF-8 string literal expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Utf8String"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IUtf8StringOperation : IOperation
    {
        /// <summary>
        /// The underlying string value.
        /// </summary>
        string Value { get; }
    }
    /// <summary>
    /// Represents the application of an attribute.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# attribute application</description></item>
    ///   <item><description>VB attribute application</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Attribute"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IAttributeOperation : IOperation
    {
        /// <summary>
        /// The operation representing the attribute. This can be a <see cref="IObjectCreationOperation" /> in non-error cases, or an <see cref="IInvalidOperation" /> in error cases.
        /// </summary>
        IOperation Operation { get; }
    }
    /// <summary>
    /// Represents an element reference or a slice operation over an inline array type.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# inline array access</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.InlineArrayAccess"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface IInlineArrayAccessOperation : IOperation
    {
        /// <summary>
        /// Instance of the inline array type to be accessed.
        /// </summary>
        IOperation Instance { get; }
        /// <summary>
        /// System.Int32, System.Index or System.Range value.
        /// </summary>
        IOperation Argument { get; }
    }
    /// <summary>
    /// Represents a collection expression.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# collection expression</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.CollectionExpression"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ICollectionExpressionOperation : IOperation
    {
        /// <summary>
        /// Method used to construct the collection.
        /// <para>
        ///   If the collection type is an array, span, array interface, or type parameter, the method is null;
        ///   if the collection type has a [CollectionBuilder] attribute, the method is the builder method;
        ///   otherwise, the method is the collection type constructor.
        /// </para>
        /// </summary>
        IMethodSymbol? ConstructMethod { get; }
        /// <summary>
        /// Collection expression elements.
        /// <para>
        ///   If the element is an expression, the entry is the expression, with a conversion to
        ///   the target element type if necessary;
        ///   otherwise, the entry is an ISpreadOperation.
        /// </para>
        /// </summary>
        ImmutableArray<IOperation> Elements { get; }
    }
    /// <summary>
    /// Represents a collection expression spread element.
    /// <para>
    /// Current usage:
    /// <list type="number">
    ///   <item><description>C# spread element</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>This node is associated with the following operation kinds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationKind.Spread"/></description></item>
    /// </list>
    /// <para>This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.</para>
    /// </remarks>
    public interface ISpreadOperation : IOperation
    {
        /// <summary>
        /// Collection being spread.
        /// </summary>
        IOperation Operand { get; }
        /// <summary>
        /// Type of the elements in the collection.
        /// </summary>
        ITypeSymbol? ElementType { get; }
        /// <summary>
        /// Conversion from the type of the collection element to the target element type
        /// of the containing collection expression.
        /// </summary>
        CommonConversion ElementConversion { get; }
    }
    #endregion

    #region Implementations
    internal sealed partial class BlockOperation : Operation, IBlockOperation
    {
        internal BlockOperation(ImmutableArray<IOperation> operations, ImmutableArray<ILocalSymbol> locals, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Operations = SetParentOperation(operations, this);
            Locals = locals;
        }
        public ImmutableArray<IOperation> Operations { get; }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        internal override int ChildOperationsCount =>
            Operations.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Operations.Length
                    => Operations[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Operations.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Operations.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Operations.IsEmpty) return (true, 0, Operations.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Block;
        public override void Accept(OperationVisitor visitor) => visitor.VisitBlock(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitBlock(this, argument);
    }
    internal sealed partial class VariableDeclarationGroupOperation : Operation, IVariableDeclarationGroupOperation
    {
        internal VariableDeclarationGroupOperation(ImmutableArray<IVariableDeclarationOperation> declarations, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Declarations = SetParentOperation(declarations, this);
        }
        public ImmutableArray<IVariableDeclarationOperation> Declarations { get; }
        internal override int ChildOperationsCount =>
            Declarations.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Declarations.Length
                    => Declarations[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Declarations.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Declarations.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Declarations.IsEmpty) return (true, 0, Declarations.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.VariableDeclarationGroup;
        public override void Accept(OperationVisitor visitor) => visitor.VisitVariableDeclarationGroup(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitVariableDeclarationGroup(this, argument);
    }
    internal sealed partial class SwitchOperation : Operation, ISwitchOperation
    {
        internal SwitchOperation(ImmutableArray<ILocalSymbol> locals, IOperation value, ImmutableArray<ISwitchCaseOperation> cases, ILabelSymbol exitLabel, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Locals = locals;
            Value = SetParentOperation(value, this);
            Cases = SetParentOperation(cases, this);
            ExitLabel = exitLabel;
        }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public IOperation Value { get; }
        public ImmutableArray<ISwitchCaseOperation> Cases { get; }
        public ILabelSymbol ExitLabel { get; }
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1) +
            Cases.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                1 when index < Cases.Length
                    => Cases[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (!Cases.IsEmpty) return (true, 1, 0);
                    else goto case 1;
                case 1 when previousIndex + 1 < Cases.Length:
                    return (true, 1, previousIndex + 1);
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Cases.IsEmpty) return (true, 1, Cases.Length - 1);
                    else goto case 1;
                case 1 when previousIndex > 0:
                    return (true, 1, previousIndex - 1);
                case 1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Switch;
        public override void Accept(OperationVisitor visitor) => visitor.VisitSwitch(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitSwitch(this, argument);
    }
    internal abstract partial class BaseLoopOperation : Operation, ILoopOperation
    {
        protected BaseLoopOperation(IOperation body, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Body = SetParentOperation(body, this);
            Locals = locals;
            ContinueLabel = continueLabel;
            ExitLabel = exitLabel;
        }
        public abstract LoopKind LoopKind { get; }
        public IOperation Body { get; }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public ILabelSymbol ContinueLabel { get; }
        public ILabelSymbol ExitLabel { get; }
    }
    internal sealed partial class ForEachLoopOperation : BaseLoopOperation, IForEachLoopOperation
    {
        internal ForEachLoopOperation(IOperation loopControlVariable, IOperation collection, ImmutableArray<IOperation> nextVariables, ForEachLoopOperationInfo? info, bool isAsynchronous, IOperation body, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(body, locals, continueLabel, exitLabel, semanticModel, syntax, isImplicit)
        {
            LoopControlVariable = SetParentOperation(loopControlVariable, this);
            Collection = SetParentOperation(collection, this);
            NextVariables = SetParentOperation(nextVariables, this);
            Info = info;
            IsAsynchronous = isAsynchronous;
        }
        public IOperation LoopControlVariable { get; }
        public IOperation Collection { get; }
        public ImmutableArray<IOperation> NextVariables { get; }
        public ForEachLoopOperationInfo? Info { get; }
        public bool IsAsynchronous { get; }
        internal override int ChildOperationsCount =>
            (LoopControlVariable is null ? 0 : 1) +
            (Collection is null ? 0 : 1) +
            NextVariables.Length +
            (Body is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Collection != null
                    => Collection,
                1 when LoopControlVariable != null
                    => LoopControlVariable,
                2 when Body != null
                    => Body,
                3 when index < NextVariables.Length
                    => NextVariables[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Collection != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (LoopControlVariable != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Body != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (!NextVariables.IsEmpty) return (true, 3, 0);
                    else goto case 3;
                case 3 when previousIndex + 1 < NextVariables.Length:
                    return (true, 3, previousIndex + 1);
                case 3:
                case 4:
                    return (false, 4, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!NextVariables.IsEmpty) return (true, 3, NextVariables.Length - 1);
                    else goto case 3;
                case 3 when previousIndex > 0:
                    return (true, 3, previousIndex - 1);
                case 3:
                    if (Body != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (LoopControlVariable != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Collection != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Loop;
        public override void Accept(OperationVisitor visitor) => visitor.VisitForEachLoop(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitForEachLoop(this, argument);
    }
    internal sealed partial class ForLoopOperation : BaseLoopOperation, IForLoopOperation
    {
        internal ForLoopOperation(ImmutableArray<IOperation> before, ImmutableArray<ILocalSymbol> conditionLocals, IOperation? condition, ImmutableArray<IOperation> atLoopBottom, IOperation body, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(body, locals, continueLabel, exitLabel, semanticModel, syntax, isImplicit)
        {
            Before = SetParentOperation(before, this);
            ConditionLocals = conditionLocals;
            Condition = SetParentOperation(condition, this);
            AtLoopBottom = SetParentOperation(atLoopBottom, this);
        }
        public ImmutableArray<IOperation> Before { get; }
        public ImmutableArray<ILocalSymbol> ConditionLocals { get; }
        public IOperation? Condition { get; }
        public ImmutableArray<IOperation> AtLoopBottom { get; }
        internal override int ChildOperationsCount =>
            Before.Length +
            (Condition is null ? 0 : 1) +
            AtLoopBottom.Length +
            (Body is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Before.Length
                    => Before[index],
                1 when Condition != null
                    => Condition,
                2 when Body != null
                    => Body,
                3 when index < AtLoopBottom.Length
                    => AtLoopBottom[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Before.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Before.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                    if (Condition != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Body != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (!AtLoopBottom.IsEmpty) return (true, 3, 0);
                    else goto case 3;
                case 3 when previousIndex + 1 < AtLoopBottom.Length:
                    return (true, 3, previousIndex + 1);
                case 3:
                case 4:
                    return (false, 4, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!AtLoopBottom.IsEmpty) return (true, 3, AtLoopBottom.Length - 1);
                    else goto case 3;
                case 3 when previousIndex > 0:
                    return (true, 3, previousIndex - 1);
                case 3:
                    if (Body != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (Condition != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (!Before.IsEmpty) return (true, 0, Before.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Loop;
        public override void Accept(OperationVisitor visitor) => visitor.VisitForLoop(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitForLoop(this, argument);
    }
    internal sealed partial class ForToLoopOperation : BaseLoopOperation, IForToLoopOperation
    {
        internal ForToLoopOperation(IOperation loopControlVariable, IOperation initialValue, IOperation limitValue, IOperation stepValue, bool isChecked, ImmutableArray<IOperation> nextVariables, (ILocalSymbol LoopObject, ForToLoopOperationUserDefinedInfo UserDefinedInfo) info, IOperation body, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(body, locals, continueLabel, exitLabel, semanticModel, syntax, isImplicit)
        {
            LoopControlVariable = SetParentOperation(loopControlVariable, this);
            InitialValue = SetParentOperation(initialValue, this);
            LimitValue = SetParentOperation(limitValue, this);
            StepValue = SetParentOperation(stepValue, this);
            IsChecked = isChecked;
            NextVariables = SetParentOperation(nextVariables, this);
            Info = info;
        }
        public IOperation LoopControlVariable { get; }
        public IOperation InitialValue { get; }
        public IOperation LimitValue { get; }
        public IOperation StepValue { get; }
        public bool IsChecked { get; }
        public ImmutableArray<IOperation> NextVariables { get; }
        public (ILocalSymbol LoopObject, ForToLoopOperationUserDefinedInfo UserDefinedInfo) Info { get; }
        internal override int ChildOperationsCount =>
            (LoopControlVariable is null ? 0 : 1) +
            (InitialValue is null ? 0 : 1) +
            (LimitValue is null ? 0 : 1) +
            (StepValue is null ? 0 : 1) +
            NextVariables.Length +
            (Body is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when LoopControlVariable != null
                    => LoopControlVariable,
                1 when InitialValue != null
                    => InitialValue,
                2 when LimitValue != null
                    => LimitValue,
                3 when StepValue != null
                    => StepValue,
                4 when Body != null
                    => Body,
                5 when index < NextVariables.Length
                    => NextVariables[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (LoopControlVariable != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (InitialValue != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (LimitValue != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (StepValue != null) return (true, 3, 0);
                    else goto case 3;
                case 3:
                    if (Body != null) return (true, 4, 0);
                    else goto case 4;
                case 4:
                    if (!NextVariables.IsEmpty) return (true, 5, 0);
                    else goto case 5;
                case 5 when previousIndex + 1 < NextVariables.Length:
                    return (true, 5, previousIndex + 1);
                case 5:
                case 6:
                    return (false, 6, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!NextVariables.IsEmpty) return (true, 5, NextVariables.Length - 1);
                    else goto case 5;
                case 5 when previousIndex > 0:
                    return (true, 5, previousIndex - 1);
                case 5:
                    if (Body != null) return (true, 4, 0);
                    else goto case 4;
                case 4:
                    if (StepValue != null) return (true, 3, 0);
                    else goto case 3;
                case 3:
                    if (LimitValue != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (InitialValue != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (LoopControlVariable != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Loop;
        public override void Accept(OperationVisitor visitor) => visitor.VisitForToLoop(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitForToLoop(this, argument);
    }
    internal sealed partial class WhileLoopOperation : BaseLoopOperation, IWhileLoopOperation
    {
        internal WhileLoopOperation(IOperation? condition, bool conditionIsTop, bool conditionIsUntil, IOperation? ignoredCondition, IOperation body, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(body, locals, continueLabel, exitLabel, semanticModel, syntax, isImplicit)
        {
            Condition = SetParentOperation(condition, this);
            ConditionIsTop = conditionIsTop;
            ConditionIsUntil = conditionIsUntil;
            IgnoredCondition = SetParentOperation(ignoredCondition, this);
        }
        public IOperation? Condition { get; }
        public bool ConditionIsTop { get; }
        public bool ConditionIsUntil { get; }
        public IOperation? IgnoredCondition { get; }
        internal override int ChildOperationsCount =>
            (Condition is null ? 0 : 1) +
            (IgnoredCondition is null ? 0 : 1) +
            (Body is null ? 0 : 1);
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Loop;
        public override void Accept(OperationVisitor visitor) => visitor.VisitWhileLoop(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitWhileLoop(this, argument);
    }
    internal sealed partial class LabeledOperation : Operation, ILabeledOperation
    {
        internal LabeledOperation(ILabelSymbol label, IOperation? operation, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Label = label;
            Operation = SetParentOperation(operation, this);
        }
        public ILabelSymbol Label { get; }
        public IOperation? Operation { get; }
        internal override int ChildOperationsCount =>
            (Operation is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operation != null
                    => Operation,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Labeled;
        public override void Accept(OperationVisitor visitor) => visitor.VisitLabeled(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitLabeled(this, argument);
    }
    internal sealed partial class BranchOperation : Operation, IBranchOperation
    {
        internal BranchOperation(ILabelSymbol target, BranchKind branchKind, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Target = target;
            BranchKind = branchKind;
        }
        public ILabelSymbol Target { get; }
        public BranchKind BranchKind { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Branch;
        public override void Accept(OperationVisitor visitor) => visitor.VisitBranch(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitBranch(this, argument);
    }
    internal sealed partial class EmptyOperation : Operation, IEmptyOperation
    {
        internal EmptyOperation(SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit) { }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Empty;
        public override void Accept(OperationVisitor visitor) => visitor.VisitEmpty(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitEmpty(this, argument);
    }
    internal sealed partial class ReturnOperation : Operation, IReturnOperation
    {
        internal ReturnOperation(IOperation? returnedValue, OperationKind kind, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            ReturnedValue = SetParentOperation(returnedValue, this);
            Kind = kind;
        }
        public IOperation? ReturnedValue { get; }
        internal override int ChildOperationsCount =>
            (ReturnedValue is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when ReturnedValue != null
                    => ReturnedValue,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (ReturnedValue != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (ReturnedValue != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind { get; }
        public override void Accept(OperationVisitor visitor) => visitor.VisitReturn(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitReturn(this, argument);
    }
    internal sealed partial class LockOperation : Operation, ILockOperation
    {
        internal LockOperation(IOperation lockedValue, IOperation body, ILocalSymbol? lockTakenSymbol, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            LockedValue = SetParentOperation(lockedValue, this);
            Body = SetParentOperation(body, this);
            LockTakenSymbol = lockTakenSymbol;
        }
        public IOperation LockedValue { get; }
        public IOperation Body { get; }
        public ILocalSymbol? LockTakenSymbol { get; }
        internal override int ChildOperationsCount =>
            (LockedValue is null ? 0 : 1) +
            (Body is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when LockedValue != null
                    => LockedValue,
                1 when Body != null
                    => Body,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (LockedValue != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Body != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Body != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (LockedValue != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Lock;
        public override void Accept(OperationVisitor visitor) => visitor.VisitLock(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitLock(this, argument);
    }
    internal sealed partial class TryOperation : Operation, ITryOperation
    {
        internal TryOperation(IBlockOperation body, ImmutableArray<ICatchClauseOperation> catches, IBlockOperation? @finally, ILabelSymbol? exitLabel, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Body = SetParentOperation(body, this);
            Catches = SetParentOperation(catches, this);
            Finally = SetParentOperation(@finally, this);
            ExitLabel = exitLabel;
        }
        public IBlockOperation Body { get; }
        public ImmutableArray<ICatchClauseOperation> Catches { get; }
        public IBlockOperation? Finally { get; }
        public ILabelSymbol? ExitLabel { get; }
        internal override int ChildOperationsCount =>
            (Body is null ? 0 : 1) +
            Catches.Length +
            (Finally is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Body != null
                    => Body,
                1 when index < Catches.Length
                    => Catches[index],
                2 when Finally != null
                    => Finally,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Body != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (!Catches.IsEmpty) return (true, 1, 0);
                    else goto case 1;
                case 1 when previousIndex + 1 < Catches.Length:
                    return (true, 1, previousIndex + 1);
                case 1:
                    if (Finally != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                case 3:
                    return (false, 3, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Finally != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (!Catches.IsEmpty) return (true, 1, Catches.Length - 1);
                    else goto case 1;
                case 1 when previousIndex > 0:
                    return (true, 1, previousIndex - 1);
                case 1:
                    if (Body != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Try;
        public override void Accept(OperationVisitor visitor) => visitor.VisitTry(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitTry(this, argument);
    }
    internal sealed partial class UsingOperation : Operation, IUsingOperation
    {
        internal UsingOperation(IOperation resources, IOperation body, ImmutableArray<ILocalSymbol> locals, bool isAsynchronous, DisposeOperationInfo disposeInfo, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Resources = SetParentOperation(resources, this);
            Body = SetParentOperation(body, this);
            Locals = locals;
            IsAsynchronous = isAsynchronous;
            DisposeInfo = disposeInfo;
        }
        public IOperation Resources { get; }
        public IOperation Body { get; }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public bool IsAsynchronous { get; }
        public DisposeOperationInfo DisposeInfo { get; }
        internal override int ChildOperationsCount =>
            (Resources is null ? 0 : 1) +
            (Body is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Resources != null
                    => Resources,
                1 when Body != null
                    => Body,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Resources != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Body != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Body != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Resources != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Using;
        public override void Accept(OperationVisitor visitor) => visitor.VisitUsing(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitUsing(this, argument);
    }
    internal sealed partial class ExpressionStatementOperation : Operation, IExpressionStatementOperation
    {
        internal ExpressionStatementOperation(IOperation operation, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
        }
        public IOperation Operation { get; }
        internal override int ChildOperationsCount =>
            (Operation is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operation != null
                    => Operation,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ExpressionStatement;
        public override void Accept(OperationVisitor visitor) => visitor.VisitExpressionStatement(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitExpressionStatement(this, argument);
    }
    internal sealed partial class LocalFunctionOperation : Operation, ILocalFunctionOperation
    {
        internal LocalFunctionOperation(IMethodSymbol symbol, IBlockOperation? body, IBlockOperation? ignoredBody, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Symbol = symbol;
            Body = SetParentOperation(body, this);
            IgnoredBody = SetParentOperation(ignoredBody, this);
        }
        public IMethodSymbol Symbol { get; }
        public IBlockOperation? Body { get; }
        public IBlockOperation? IgnoredBody { get; }
        internal override int ChildOperationsCount =>
            (Body is null ? 0 : 1) +
            (IgnoredBody is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Body != null
                    => Body,
                1 when IgnoredBody != null
                    => IgnoredBody,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Body != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (IgnoredBody != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (IgnoredBody != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Body != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.LocalFunction;
        public override void Accept(OperationVisitor visitor) => visitor.VisitLocalFunction(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitLocalFunction(this, argument);
    }
    internal sealed partial class StopOperation : Operation, IStopOperation
    {
        internal StopOperation(SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit) { }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Stop;
        public override void Accept(OperationVisitor visitor) => visitor.VisitStop(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitStop(this, argument);
    }
    internal sealed partial class EndOperation : Operation, IEndOperation
    {
        internal EndOperation(SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit) { }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.End;
        public override void Accept(OperationVisitor visitor) => visitor.VisitEnd(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitEnd(this, argument);
    }
    internal sealed partial class RaiseEventOperation : Operation, IRaiseEventOperation
    {
        internal RaiseEventOperation(IEventReferenceOperation eventReference, ImmutableArray<IArgumentOperation> arguments, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            EventReference = SetParentOperation(eventReference, this);
            Arguments = SetParentOperation(arguments, this);
        }
        public IEventReferenceOperation EventReference { get; }
        public ImmutableArray<IArgumentOperation> Arguments { get; }
        internal override int ChildOperationsCount =>
            (EventReference is null ? 0 : 1) +
            Arguments.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when EventReference != null
                    => EventReference,
                1 when index < Arguments.Length
                    => Arguments[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (EventReference != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (!Arguments.IsEmpty) return (true, 1, 0);
                    else goto case 1;
                case 1 when previousIndex + 1 < Arguments.Length:
                    return (true, 1, previousIndex + 1);
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Arguments.IsEmpty) return (true, 1, Arguments.Length - 1);
                    else goto case 1;
                case 1 when previousIndex > 0:
                    return (true, 1, previousIndex - 1);
                case 1:
                    if (EventReference != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.RaiseEvent;
        public override void Accept(OperationVisitor visitor) => visitor.VisitRaiseEvent(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitRaiseEvent(this, argument);
    }
    internal sealed partial class LiteralOperation : Operation, ILiteralOperation
    {
        internal LiteralOperation(SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            OperationConstantValue = constantValue;
            Type = type;
        }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.Literal;
        public override void Accept(OperationVisitor visitor) => visitor.VisitLiteral(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitLiteral(this, argument);
    }
    internal sealed partial class ConversionOperation : Operation, IConversionOperation
    {
        internal ConversionOperation(IOperation operand, IConvertibleConversion conversion, bool isTryCast, bool isChecked, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Operand = SetParentOperation(operand, this);
            ConversionConvertible = conversion;
            IsTryCast = isTryCast;
            IsChecked = isChecked;
            OperationConstantValue = constantValue;
            Type = type;
        }
        public IOperation Operand { get; }
        internal IConvertibleConversion ConversionConvertible { get; }
        public CommonConversion Conversion => ConversionConvertible.ToCommonConversion();
        public bool IsTryCast { get; }
        public bool IsChecked { get; }
        internal override int ChildOperationsCount =>
            (Operand is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operand != null
                    => Operand,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.Conversion;
        public override void Accept(OperationVisitor visitor) => visitor.VisitConversion(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitConversion(this, argument);
    }
    internal sealed partial class InvocationOperation : Operation, IInvocationOperation
    {
        internal InvocationOperation(IMethodSymbol targetMethod, ITypeSymbol? constrainedToType, IOperation? instance, bool isVirtual, ImmutableArray<IArgumentOperation> arguments, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            TargetMethod = targetMethod;
            ConstrainedToType = constrainedToType;
            Instance = SetParentOperation(instance, this);
            IsVirtual = isVirtual;
            Arguments = SetParentOperation(arguments, this);
            Type = type;
        }
        public IMethodSymbol TargetMethod { get; }
        public ITypeSymbol? ConstrainedToType { get; }
        public IOperation? Instance { get; }
        public bool IsVirtual { get; }
        public ImmutableArray<IArgumentOperation> Arguments { get; }
        internal override int ChildOperationsCount =>
            (Instance is null ? 0 : 1) +
            Arguments.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Instance != null
                    => Instance,
                1 when index < Arguments.Length
                    => Arguments[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (!Arguments.IsEmpty) return (true, 1, 0);
                    else goto case 1;
                case 1 when previousIndex + 1 < Arguments.Length:
                    return (true, 1, previousIndex + 1);
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Arguments.IsEmpty) return (true, 1, Arguments.Length - 1);
                    else goto case 1;
                case 1 when previousIndex > 0:
                    return (true, 1, previousIndex - 1);
                case 1:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Invocation;
        public override void Accept(OperationVisitor visitor) => visitor.VisitInvocation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitInvocation(this, argument);
    }
    internal sealed partial class ArrayElementReferenceOperation : Operation, IArrayElementReferenceOperation
    {
        internal ArrayElementReferenceOperation(IOperation arrayReference, ImmutableArray<IOperation> indices, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            ArrayReference = SetParentOperation(arrayReference, this);
            Indices = SetParentOperation(indices, this);
            Type = type;
        }
        public IOperation ArrayReference { get; }
        public ImmutableArray<IOperation> Indices { get; }
        internal override int ChildOperationsCount =>
            (ArrayReference is null ? 0 : 1) +
            Indices.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when ArrayReference != null
                    => ArrayReference,
                1 when index < Indices.Length
                    => Indices[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (ArrayReference != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (!Indices.IsEmpty) return (true, 1, 0);
                    else goto case 1;
                case 1 when previousIndex + 1 < Indices.Length:
                    return (true, 1, previousIndex + 1);
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Indices.IsEmpty) return (true, 1, Indices.Length - 1);
                    else goto case 1;
                case 1 when previousIndex > 0:
                    return (true, 1, previousIndex - 1);
                case 1:
                    if (ArrayReference != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ArrayElementReference;
        public override void Accept(OperationVisitor visitor) => visitor.VisitArrayElementReference(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitArrayElementReference(this, argument);
    }
    internal sealed partial class LocalReferenceOperation : Operation, ILocalReferenceOperation
    {
        internal LocalReferenceOperation(ILocalSymbol local, bool isDeclaration, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Local = local;
            IsDeclaration = isDeclaration;
            OperationConstantValue = constantValue;
            Type = type;
        }
        public ILocalSymbol Local { get; }
        public bool IsDeclaration { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.LocalReference;
        public override void Accept(OperationVisitor visitor) => visitor.VisitLocalReference(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitLocalReference(this, argument);
    }
    internal sealed partial class ParameterReferenceOperation : Operation, IParameterReferenceOperation
    {
        internal ParameterReferenceOperation(IParameterSymbol parameter, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Parameter = parameter;
            Type = type;
        }
        public IParameterSymbol Parameter { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ParameterReference;
        public override void Accept(OperationVisitor visitor) => visitor.VisitParameterReference(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitParameterReference(this, argument);
    }
    internal abstract partial class BaseMemberReferenceOperation : Operation, IMemberReferenceOperation
    {
        protected BaseMemberReferenceOperation(IOperation? instance, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Instance = SetParentOperation(instance, this);
        }
        public IOperation? Instance { get; }
        public abstract ITypeSymbol? ConstrainedToType { get; }
    }
    internal sealed partial class FieldReferenceOperation : BaseMemberReferenceOperation, IFieldReferenceOperation
    {
        internal FieldReferenceOperation(IFieldSymbol field, bool isDeclaration, IOperation? instance, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(instance, semanticModel, syntax, isImplicit)
        {
            Field = field;
            IsDeclaration = isDeclaration;
            OperationConstantValue = constantValue;
            Type = type;
        }
        public IFieldSymbol Field { get; }
        public bool IsDeclaration { get; }
        internal override int ChildOperationsCount =>
            (Instance is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Instance != null
                    => Instance,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.FieldReference;
        public override void Accept(OperationVisitor visitor) => visitor.VisitFieldReference(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitFieldReference(this, argument);
    }
    internal sealed partial class MethodReferenceOperation : BaseMemberReferenceOperation, IMethodReferenceOperation
    {
        internal MethodReferenceOperation(IMethodSymbol method, ITypeSymbol? constrainedToType, bool isVirtual, IOperation? instance, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(instance, semanticModel, syntax, isImplicit)
        {
            Method = method;
            ConstrainedToType = constrainedToType;
            IsVirtual = isVirtual;
            Type = type;
        }
        public IMethodSymbol Method { get; }
        public override ITypeSymbol? ConstrainedToType { get; }
        public bool IsVirtual { get; }
        internal override int ChildOperationsCount =>
            (Instance is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Instance != null
                    => Instance,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.MethodReference;
        public override void Accept(OperationVisitor visitor) => visitor.VisitMethodReference(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitMethodReference(this, argument);
    }
    internal sealed partial class PropertyReferenceOperation : BaseMemberReferenceOperation, IPropertyReferenceOperation
    {
        internal PropertyReferenceOperation(IPropertySymbol property, ITypeSymbol? constrainedToType, ImmutableArray<IArgumentOperation> arguments, IOperation? instance, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(instance, semanticModel, syntax, isImplicit)
        {
            Property = property;
            ConstrainedToType = constrainedToType;
            Arguments = SetParentOperation(arguments, this);
            Type = type;
        }
        public IPropertySymbol Property { get; }
        public override ITypeSymbol? ConstrainedToType { get; }
        public ImmutableArray<IArgumentOperation> Arguments { get; }
        internal override int ChildOperationsCount =>
            Arguments.Length +
            (Instance is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Instance != null
                    => Instance,
                1 when index < Arguments.Length
                    => Arguments[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (!Arguments.IsEmpty) return (true, 1, 0);
                    else goto case 1;
                case 1 when previousIndex + 1 < Arguments.Length:
                    return (true, 1, previousIndex + 1);
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Arguments.IsEmpty) return (true, 1, Arguments.Length - 1);
                    else goto case 1;
                case 1 when previousIndex > 0:
                    return (true, 1, previousIndex - 1);
                case 1:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.PropertyReference;
        public override void Accept(OperationVisitor visitor) => visitor.VisitPropertyReference(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitPropertyReference(this, argument);
    }
    internal sealed partial class EventReferenceOperation : BaseMemberReferenceOperation, IEventReferenceOperation
    {
        internal EventReferenceOperation(IEventSymbol @event, ITypeSymbol? constrainedToType, IOperation? instance, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(instance, semanticModel, syntax, isImplicit)
        {
            Event = @event;
            ConstrainedToType = constrainedToType;
            Type = type;
        }
        public IEventSymbol Event { get; }
        public override ITypeSymbol? ConstrainedToType { get; }
        internal override int ChildOperationsCount =>
            (Instance is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Instance != null
                    => Instance,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.EventReference;
        public override void Accept(OperationVisitor visitor) => visitor.VisitEventReference(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitEventReference(this, argument);
    }
    internal sealed partial class UnaryOperation : Operation, IUnaryOperation
    {
        internal UnaryOperation(UnaryOperatorKind operatorKind, IOperation operand, bool isLifted, bool isChecked, IMethodSymbol? operatorMethod, ITypeSymbol? constrainedToType, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            OperatorKind = operatorKind;
            Operand = SetParentOperation(operand, this);
            IsLifted = isLifted;
            IsChecked = isChecked;
            OperatorMethod = operatorMethod;
            ConstrainedToType = constrainedToType;
            OperationConstantValue = constantValue;
            Type = type;
        }
        public UnaryOperatorKind OperatorKind { get; }
        public IOperation Operand { get; }
        public bool IsLifted { get; }
        public bool IsChecked { get; }
        public IMethodSymbol? OperatorMethod { get; }
        public ITypeSymbol? ConstrainedToType { get; }
        internal override int ChildOperationsCount =>
            (Operand is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operand != null
                    => Operand,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.Unary;
        public override void Accept(OperationVisitor visitor) => visitor.VisitUnaryOperator(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitUnaryOperator(this, argument);
    }
    internal sealed partial class BinaryOperation : Operation, IBinaryOperation
    {
        internal BinaryOperation(BinaryOperatorKind operatorKind, IOperation leftOperand, IOperation rightOperand, bool isLifted, bool isChecked, bool isCompareText, IMethodSymbol? operatorMethod, ITypeSymbol? constrainedToType, IMethodSymbol? unaryOperatorMethod, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            OperatorKind = operatorKind;
            LeftOperand = SetParentOperation(leftOperand, this);
            RightOperand = SetParentOperation(rightOperand, this);
            IsLifted = isLifted;
            IsChecked = isChecked;
            IsCompareText = isCompareText;
            OperatorMethod = operatorMethod;
            ConstrainedToType = constrainedToType;
            UnaryOperatorMethod = unaryOperatorMethod;
            OperationConstantValue = constantValue;
            Type = type;
        }
        public BinaryOperatorKind OperatorKind { get; }
        public IOperation LeftOperand { get; }
        public IOperation RightOperand { get; }
        public bool IsLifted { get; }
        public bool IsChecked { get; }
        public bool IsCompareText { get; }
        public IMethodSymbol? OperatorMethod { get; }
        public ITypeSymbol? ConstrainedToType { get; }
        public IMethodSymbol? UnaryOperatorMethod { get; }
        internal override int ChildOperationsCount =>
            (LeftOperand is null ? 0 : 1) +
            (RightOperand is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when LeftOperand != null
                    => LeftOperand,
                1 when RightOperand != null
                    => RightOperand,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (LeftOperand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (RightOperand != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (RightOperand != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (LeftOperand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.Binary;
        public override void Accept(OperationVisitor visitor) => visitor.VisitBinaryOperator(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitBinaryOperator(this, argument);
    }
    internal sealed partial class ConditionalOperation : Operation, IConditionalOperation
    {
        internal ConditionalOperation(IOperation condition, IOperation whenTrue, IOperation? whenFalse, bool isRef, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Condition = SetParentOperation(condition, this);
            WhenTrue = SetParentOperation(whenTrue, this);
            WhenFalse = SetParentOperation(whenFalse, this);
            IsRef = isRef;
            OperationConstantValue = constantValue;
            Type = type;
        }
        public IOperation Condition { get; }
        public IOperation WhenTrue { get; }
        public IOperation? WhenFalse { get; }
        public bool IsRef { get; }
        internal override int ChildOperationsCount =>
            (Condition is null ? 0 : 1) +
            (WhenTrue is null ? 0 : 1) +
            (WhenFalse is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Condition != null
                    => Condition,
                1 when WhenTrue != null
                    => WhenTrue,
                2 when WhenFalse != null
                    => WhenFalse,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Condition != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (WhenTrue != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (WhenFalse != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                case 3:
                    return (false, 3, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (WhenFalse != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (WhenTrue != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Condition != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.Conditional;
        public override void Accept(OperationVisitor visitor) => visitor.VisitConditional(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitConditional(this, argument);
    }
    internal sealed partial class CoalesceOperation : Operation, ICoalesceOperation
    {
        internal CoalesceOperation(IOperation value, IOperation whenNull, IConvertibleConversion valueConversion, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Value = SetParentOperation(value, this);
            WhenNull = SetParentOperation(whenNull, this);
            ValueConversionConvertible = valueConversion;
            OperationConstantValue = constantValue;
            Type = type;
        }
        public IOperation Value { get; }
        public IOperation WhenNull { get; }
        internal IConvertibleConversion ValueConversionConvertible { get; }
        public CommonConversion ValueConversion => ValueConversionConvertible.ToCommonConversion();
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1) +
            (WhenNull is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                1 when WhenNull != null
                    => WhenNull,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (WhenNull != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (WhenNull != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.Coalesce;
        public override void Accept(OperationVisitor visitor) => visitor.VisitCoalesce(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitCoalesce(this, argument);
    }
    internal sealed partial class AnonymousFunctionOperation : Operation, IAnonymousFunctionOperation
    {
        internal AnonymousFunctionOperation(IMethodSymbol symbol, IBlockOperation body, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Symbol = symbol;
            Body = SetParentOperation(body, this);
        }
        public IMethodSymbol Symbol { get; }
        public IBlockOperation Body { get; }
        internal override int ChildOperationsCount =>
            (Body is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Body != null
                    => Body,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Body != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Body != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.AnonymousFunction;
        public override void Accept(OperationVisitor visitor) => visitor.VisitAnonymousFunction(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitAnonymousFunction(this, argument);
    }
    internal sealed partial class ObjectCreationOperation : Operation, IObjectCreationOperation
    {
        internal ObjectCreationOperation(IMethodSymbol? constructor, IObjectOrCollectionInitializerOperation? initializer, ImmutableArray<IArgumentOperation> arguments, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Constructor = constructor;
            Initializer = SetParentOperation(initializer, this);
            Arguments = SetParentOperation(arguments, this);
            OperationConstantValue = constantValue;
            Type = type;
        }
        public IMethodSymbol? Constructor { get; }
        public IObjectOrCollectionInitializerOperation? Initializer { get; }
        public ImmutableArray<IArgumentOperation> Arguments { get; }
        internal override int ChildOperationsCount =>
            (Initializer is null ? 0 : 1) +
            Arguments.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Arguments.Length
                    => Arguments[index],
                1 when Initializer != null
                    => Initializer,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Arguments.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Arguments.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                    if (Initializer != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Initializer != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (!Arguments.IsEmpty) return (true, 0, Arguments.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.ObjectCreation;
        public override void Accept(OperationVisitor visitor) => visitor.VisitObjectCreation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitObjectCreation(this, argument);
    }
    internal sealed partial class TypeParameterObjectCreationOperation : Operation, ITypeParameterObjectCreationOperation
    {
        internal TypeParameterObjectCreationOperation(IObjectOrCollectionInitializerOperation? initializer, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Initializer = SetParentOperation(initializer, this);
            Type = type;
        }
        public IObjectOrCollectionInitializerOperation? Initializer { get; }
        internal override int ChildOperationsCount =>
            (Initializer is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Initializer != null
                    => Initializer,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Initializer != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Initializer != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.TypeParameterObjectCreation;
        public override void Accept(OperationVisitor visitor) => visitor.VisitTypeParameterObjectCreation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitTypeParameterObjectCreation(this, argument);
    }
    internal sealed partial class ArrayCreationOperation : Operation, IArrayCreationOperation
    {
        internal ArrayCreationOperation(ImmutableArray<IOperation> dimensionSizes, IArrayInitializerOperation? initializer, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            DimensionSizes = SetParentOperation(dimensionSizes, this);
            Initializer = SetParentOperation(initializer, this);
            Type = type;
        }
        public ImmutableArray<IOperation> DimensionSizes { get; }
        public IArrayInitializerOperation? Initializer { get; }
        internal override int ChildOperationsCount =>
            DimensionSizes.Length +
            (Initializer is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < DimensionSizes.Length
                    => DimensionSizes[index],
                1 when Initializer != null
                    => Initializer,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!DimensionSizes.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < DimensionSizes.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                    if (Initializer != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Initializer != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (!DimensionSizes.IsEmpty) return (true, 0, DimensionSizes.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ArrayCreation;
        public override void Accept(OperationVisitor visitor) => visitor.VisitArrayCreation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitArrayCreation(this, argument);
    }
    internal sealed partial class InstanceReferenceOperation : Operation, IInstanceReferenceOperation
    {
        internal InstanceReferenceOperation(InstanceReferenceKind referenceKind, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            ReferenceKind = referenceKind;
            Type = type;
        }
        public InstanceReferenceKind ReferenceKind { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.InstanceReference;
        public override void Accept(OperationVisitor visitor) => visitor.VisitInstanceReference(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitInstanceReference(this, argument);
    }
    internal sealed partial class IsTypeOperation : Operation, IIsTypeOperation
    {
        internal IsTypeOperation(IOperation valueOperand, ITypeSymbol typeOperand, bool isNegated, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            ValueOperand = SetParentOperation(valueOperand, this);
            TypeOperand = typeOperand;
            IsNegated = isNegated;
            Type = type;
        }
        public IOperation ValueOperand { get; }
        public ITypeSymbol TypeOperand { get; }
        public bool IsNegated { get; }
        internal override int ChildOperationsCount =>
            (ValueOperand is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when ValueOperand != null
                    => ValueOperand,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (ValueOperand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (ValueOperand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.IsType;
        public override void Accept(OperationVisitor visitor) => visitor.VisitIsType(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitIsType(this, argument);
    }
    internal sealed partial class AwaitOperation : Operation, IAwaitOperation
    {
        internal AwaitOperation(IOperation operation, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
            Type = type;
        }
        public IOperation Operation { get; }
        internal override int ChildOperationsCount =>
            (Operation is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operation != null
                    => Operation,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Await;
        public override void Accept(OperationVisitor visitor) => visitor.VisitAwait(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitAwait(this, argument);
    }
    internal abstract partial class BaseAssignmentOperation : Operation, IAssignmentOperation
    {
        protected BaseAssignmentOperation(IOperation target, IOperation value, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Target = SetParentOperation(target, this);
            Value = SetParentOperation(value, this);
        }
        public IOperation Target { get; }
        public IOperation Value { get; }
    }
    internal sealed partial class SimpleAssignmentOperation : BaseAssignmentOperation, ISimpleAssignmentOperation
    {
        internal SimpleAssignmentOperation(bool isRef, IOperation target, IOperation value, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(target, value, semanticModel, syntax, isImplicit)
        {
            IsRef = isRef;
            OperationConstantValue = constantValue;
            Type = type;
        }
        public bool IsRef { get; }
        internal override int ChildOperationsCount =>
            (Target is null ? 0 : 1) +
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Target != null
                    => Target,
                1 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Value != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.SimpleAssignment;
        public override void Accept(OperationVisitor visitor) => visitor.VisitSimpleAssignment(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitSimpleAssignment(this, argument);
    }
    internal sealed partial class CompoundAssignmentOperation : BaseAssignmentOperation, ICompoundAssignmentOperation
    {
        internal CompoundAssignmentOperation(IConvertibleConversion inConversion, IConvertibleConversion outConversion, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol? operatorMethod, ITypeSymbol? constrainedToType, IOperation target, IOperation value, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(target, value, semanticModel, syntax, isImplicit)
        {
            InConversionConvertible = inConversion;
            OutConversionConvertible = outConversion;
            OperatorKind = operatorKind;
            IsLifted = isLifted;
            IsChecked = isChecked;
            OperatorMethod = operatorMethod;
            ConstrainedToType = constrainedToType;
            Type = type;
        }
        internal IConvertibleConversion InConversionConvertible { get; }
        public CommonConversion InConversion => InConversionConvertible.ToCommonConversion();
        internal IConvertibleConversion OutConversionConvertible { get; }
        public CommonConversion OutConversion => OutConversionConvertible.ToCommonConversion();
        public BinaryOperatorKind OperatorKind { get; }
        public bool IsLifted { get; }
        public bool IsChecked { get; }
        public IMethodSymbol? OperatorMethod { get; }
        public ITypeSymbol? ConstrainedToType { get; }
        internal override int ChildOperationsCount =>
            (Target is null ? 0 : 1) +
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Target != null
                    => Target,
                1 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Value != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.CompoundAssignment;
        public override void Accept(OperationVisitor visitor) => visitor.VisitCompoundAssignment(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitCompoundAssignment(this, argument);
    }
    internal sealed partial class ParenthesizedOperation : Operation, IParenthesizedOperation
    {
        internal ParenthesizedOperation(IOperation operand, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Operand = SetParentOperation(operand, this);
            OperationConstantValue = constantValue;
            Type = type;
        }
        public IOperation Operand { get; }
        internal override int ChildOperationsCount =>
            (Operand is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operand != null
                    => Operand,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.Parenthesized;
        public override void Accept(OperationVisitor visitor) => visitor.VisitParenthesized(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitParenthesized(this, argument);
    }
    internal sealed partial class EventAssignmentOperation : Operation, IEventAssignmentOperation
    {
        internal EventAssignmentOperation(IOperation eventReference, IOperation handlerValue, bool adds, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            EventReference = SetParentOperation(eventReference, this);
            HandlerValue = SetParentOperation(handlerValue, this);
            Adds = adds;
            Type = type;
        }
        public IOperation EventReference { get; }
        public IOperation HandlerValue { get; }
        public bool Adds { get; }
        internal override int ChildOperationsCount =>
            (EventReference is null ? 0 : 1) +
            (HandlerValue is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when EventReference != null
                    => EventReference,
                1 when HandlerValue != null
                    => HandlerValue,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (EventReference != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (HandlerValue != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (HandlerValue != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (EventReference != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.EventAssignment;
        public override void Accept(OperationVisitor visitor) => visitor.VisitEventAssignment(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitEventAssignment(this, argument);
    }
    internal sealed partial class ConditionalAccessOperation : Operation, IConditionalAccessOperation
    {
        internal ConditionalAccessOperation(IOperation operation, IOperation whenNotNull, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
            WhenNotNull = SetParentOperation(whenNotNull, this);
            Type = type;
        }
        public IOperation Operation { get; }
        public IOperation WhenNotNull { get; }
        internal override int ChildOperationsCount =>
            (Operation is null ? 0 : 1) +
            (WhenNotNull is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operation != null
                    => Operation,
                1 when WhenNotNull != null
                    => WhenNotNull,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (WhenNotNull != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (WhenNotNull != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ConditionalAccess;
        public override void Accept(OperationVisitor visitor) => visitor.VisitConditionalAccess(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitConditionalAccess(this, argument);
    }
    internal sealed partial class ConditionalAccessInstanceOperation : Operation, IConditionalAccessInstanceOperation
    {
        internal ConditionalAccessInstanceOperation(SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Type = type;
        }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ConditionalAccessInstance;
        public override void Accept(OperationVisitor visitor) => visitor.VisitConditionalAccessInstance(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitConditionalAccessInstance(this, argument);
    }
    internal sealed partial class InterpolatedStringOperation : Operation, IInterpolatedStringOperation
    {
        internal InterpolatedStringOperation(ImmutableArray<IInterpolatedStringContentOperation> parts, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Parts = SetParentOperation(parts, this);
            OperationConstantValue = constantValue;
            Type = type;
        }
        public ImmutableArray<IInterpolatedStringContentOperation> Parts { get; }
        internal override int ChildOperationsCount =>
            Parts.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Parts.Length
                    => Parts[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Parts.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Parts.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Parts.IsEmpty) return (true, 0, Parts.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.InterpolatedString;
        public override void Accept(OperationVisitor visitor) => visitor.VisitInterpolatedString(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitInterpolatedString(this, argument);
    }
    internal sealed partial class AnonymousObjectCreationOperation : Operation, IAnonymousObjectCreationOperation
    {
        internal AnonymousObjectCreationOperation(ImmutableArray<IOperation> initializers, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Initializers = SetParentOperation(initializers, this);
            Type = type;
        }
        public ImmutableArray<IOperation> Initializers { get; }
        internal override int ChildOperationsCount =>
            Initializers.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Initializers.Length
                    => Initializers[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Initializers.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Initializers.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Initializers.IsEmpty) return (true, 0, Initializers.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.AnonymousObjectCreation;
        public override void Accept(OperationVisitor visitor) => visitor.VisitAnonymousObjectCreation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitAnonymousObjectCreation(this, argument);
    }
    internal sealed partial class ObjectOrCollectionInitializerOperation : Operation, IObjectOrCollectionInitializerOperation
    {
        internal ObjectOrCollectionInitializerOperation(ImmutableArray<IOperation> initializers, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Initializers = SetParentOperation(initializers, this);
            Type = type;
        }
        public ImmutableArray<IOperation> Initializers { get; }
        internal override int ChildOperationsCount =>
            Initializers.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Initializers.Length
                    => Initializers[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Initializers.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Initializers.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Initializers.IsEmpty) return (true, 0, Initializers.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ObjectOrCollectionInitializer;
        public override void Accept(OperationVisitor visitor) => visitor.VisitObjectOrCollectionInitializer(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitObjectOrCollectionInitializer(this, argument);
    }
    internal sealed partial class MemberInitializerOperation : Operation, IMemberInitializerOperation
    {
        internal MemberInitializerOperation(IOperation initializedMember, IObjectOrCollectionInitializerOperation initializer, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            InitializedMember = SetParentOperation(initializedMember, this);
            Initializer = SetParentOperation(initializer, this);
            Type = type;
        }
        public IOperation InitializedMember { get; }
        public IObjectOrCollectionInitializerOperation Initializer { get; }
        internal override int ChildOperationsCount =>
            (InitializedMember is null ? 0 : 1) +
            (Initializer is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when InitializedMember != null
                    => InitializedMember,
                1 when Initializer != null
                    => Initializer,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (InitializedMember != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Initializer != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Initializer != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (InitializedMember != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.MemberInitializer;
        public override void Accept(OperationVisitor visitor) => visitor.VisitMemberInitializer(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitMemberInitializer(this, argument);
    }
    internal sealed partial class NameOfOperation : Operation, INameOfOperation
    {
        internal NameOfOperation(IOperation argument, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Argument = SetParentOperation(argument, this);
            OperationConstantValue = constantValue;
            Type = type;
        }
        public IOperation Argument { get; }
        internal override int ChildOperationsCount =>
            (Argument is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Argument != null
                    => Argument,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Argument != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Argument != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.NameOf;
        public override void Accept(OperationVisitor visitor) => visitor.VisitNameOf(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitNameOf(this, argument);
    }
    internal sealed partial class TupleOperation : Operation, ITupleOperation
    {
        internal TupleOperation(ImmutableArray<IOperation> elements, ITypeSymbol? naturalType, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Elements = SetParentOperation(elements, this);
            NaturalType = naturalType;
            Type = type;
        }
        public ImmutableArray<IOperation> Elements { get; }
        public ITypeSymbol? NaturalType { get; }
        internal override int ChildOperationsCount =>
            Elements.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Elements.Length
                    => Elements[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Elements.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Elements.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Elements.IsEmpty) return (true, 0, Elements.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Tuple;
        public override void Accept(OperationVisitor visitor) => visitor.VisitTuple(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitTuple(this, argument);
    }
    internal sealed partial class DynamicMemberReferenceOperation : Operation, IDynamicMemberReferenceOperation
    {
        internal DynamicMemberReferenceOperation(IOperation? instance, string memberName, ImmutableArray<ITypeSymbol> typeArguments, ITypeSymbol? containingType, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Instance = SetParentOperation(instance, this);
            MemberName = memberName;
            TypeArguments = typeArguments;
            ContainingType = containingType;
            Type = type;
        }
        public IOperation? Instance { get; }
        public string MemberName { get; }
        public ImmutableArray<ITypeSymbol> TypeArguments { get; }
        public ITypeSymbol? ContainingType { get; }
        internal override int ChildOperationsCount =>
            (Instance is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Instance != null
                    => Instance,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.DynamicMemberReference;
        public override void Accept(OperationVisitor visitor) => visitor.VisitDynamicMemberReference(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitDynamicMemberReference(this, argument);
    }
    internal sealed partial class TranslatedQueryOperation : Operation, ITranslatedQueryOperation
    {
        internal TranslatedQueryOperation(IOperation operation, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
            Type = type;
        }
        public IOperation Operation { get; }
        internal override int ChildOperationsCount =>
            (Operation is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operation != null
                    => Operation,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.TranslatedQuery;
        public override void Accept(OperationVisitor visitor) => visitor.VisitTranslatedQuery(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitTranslatedQuery(this, argument);
    }
    internal sealed partial class DelegateCreationOperation : Operation, IDelegateCreationOperation
    {
        internal DelegateCreationOperation(IOperation target, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Target = SetParentOperation(target, this);
            Type = type;
        }
        public IOperation Target { get; }
        internal override int ChildOperationsCount =>
            (Target is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Target != null
                    => Target,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.DelegateCreation;
        public override void Accept(OperationVisitor visitor) => visitor.VisitDelegateCreation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitDelegateCreation(this, argument);
    }
    internal sealed partial class DefaultValueOperation : Operation, IDefaultValueOperation
    {
        internal DefaultValueOperation(SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            OperationConstantValue = constantValue;
            Type = type;
        }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.DefaultValue;
        public override void Accept(OperationVisitor visitor) => visitor.VisitDefaultValue(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitDefaultValue(this, argument);
    }
    internal sealed partial class TypeOfOperation : Operation, ITypeOfOperation
    {
        internal TypeOfOperation(ITypeSymbol typeOperand, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            TypeOperand = typeOperand;
            Type = type;
        }
        public ITypeSymbol TypeOperand { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.TypeOf;
        public override void Accept(OperationVisitor visitor) => visitor.VisitTypeOf(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitTypeOf(this, argument);
    }
    internal sealed partial class SizeOfOperation : Operation, ISizeOfOperation
    {
        internal SizeOfOperation(ITypeSymbol typeOperand, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            TypeOperand = typeOperand;
            OperationConstantValue = constantValue;
            Type = type;
        }
        public ITypeSymbol TypeOperand { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.SizeOf;
        public override void Accept(OperationVisitor visitor) => visitor.VisitSizeOf(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitSizeOf(this, argument);
    }
    internal sealed partial class AddressOfOperation : Operation, IAddressOfOperation
    {
        internal AddressOfOperation(IOperation reference, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Reference = SetParentOperation(reference, this);
            Type = type;
        }
        public IOperation Reference { get; }
        internal override int ChildOperationsCount =>
            (Reference is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Reference != null
                    => Reference,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Reference != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Reference != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.AddressOf;
        public override void Accept(OperationVisitor visitor) => visitor.VisitAddressOf(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitAddressOf(this, argument);
    }
    internal sealed partial class IsPatternOperation : Operation, IIsPatternOperation
    {
        internal IsPatternOperation(IOperation value, IPatternOperation pattern, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Value = SetParentOperation(value, this);
            Pattern = SetParentOperation(pattern, this);
            Type = type;
        }
        public IOperation Value { get; }
        public IPatternOperation Pattern { get; }
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1) +
            (Pattern is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                1 when Pattern != null
                    => Pattern,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Pattern != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Pattern != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.IsPattern;
        public override void Accept(OperationVisitor visitor) => visitor.VisitIsPattern(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitIsPattern(this, argument);
    }
    internal sealed partial class IncrementOrDecrementOperation : Operation, IIncrementOrDecrementOperation
    {
        internal IncrementOrDecrementOperation(bool isPostfix, bool isLifted, bool isChecked, IOperation target, IMethodSymbol? operatorMethod, ITypeSymbol? constrainedToType, OperationKind kind, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            IsPostfix = isPostfix;
            IsLifted = isLifted;
            IsChecked = isChecked;
            Target = SetParentOperation(target, this);
            OperatorMethod = operatorMethod;
            ConstrainedToType = constrainedToType;
            Type = type;
            Kind = kind;
        }
        public bool IsPostfix { get; }
        public bool IsLifted { get; }
        public bool IsChecked { get; }
        public IOperation Target { get; }
        public IMethodSymbol? OperatorMethod { get; }
        public ITypeSymbol? ConstrainedToType { get; }
        internal override int ChildOperationsCount =>
            (Target is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Target != null
                    => Target,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind { get; }
        public override void Accept(OperationVisitor visitor) => visitor.VisitIncrementOrDecrement(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitIncrementOrDecrement(this, argument);
    }
    internal sealed partial class ThrowOperation : Operation, IThrowOperation
    {
        internal ThrowOperation(IOperation? exception, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Exception = SetParentOperation(exception, this);
            Type = type;
        }
        public IOperation? Exception { get; }
        internal override int ChildOperationsCount =>
            (Exception is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Exception != null
                    => Exception,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Exception != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Exception != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Throw;
        public override void Accept(OperationVisitor visitor) => visitor.VisitThrow(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitThrow(this, argument);
    }
    internal sealed partial class DeconstructionAssignmentOperation : BaseAssignmentOperation, IDeconstructionAssignmentOperation
    {
        internal DeconstructionAssignmentOperation(IOperation target, IOperation value, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(target, value, semanticModel, syntax, isImplicit)
        {
            Type = type;
        }
        internal override int ChildOperationsCount =>
            (Target is null ? 0 : 1) +
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Target != null
                    => Target,
                1 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Value != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.DeconstructionAssignment;
        public override void Accept(OperationVisitor visitor) => visitor.VisitDeconstructionAssignment(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitDeconstructionAssignment(this, argument);
    }
    internal sealed partial class DeclarationExpressionOperation : Operation, IDeclarationExpressionOperation
    {
        internal DeclarationExpressionOperation(IOperation expression, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Expression = SetParentOperation(expression, this);
            Type = type;
        }
        public IOperation Expression { get; }
        internal override int ChildOperationsCount =>
            (Expression is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Expression != null
                    => Expression,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Expression != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Expression != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.DeclarationExpression;
        public override void Accept(OperationVisitor visitor) => visitor.VisitDeclarationExpression(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitDeclarationExpression(this, argument);
    }
    internal sealed partial class OmittedArgumentOperation : Operation, IOmittedArgumentOperation
    {
        internal OmittedArgumentOperation(SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Type = type;
        }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.OmittedArgument;
        public override void Accept(OperationVisitor visitor) => visitor.VisitOmittedArgument(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitOmittedArgument(this, argument);
    }
    internal abstract partial class BaseSymbolInitializerOperation : Operation, ISymbolInitializerOperation
    {
        protected BaseSymbolInitializerOperation(ImmutableArray<ILocalSymbol> locals, IOperation value, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Locals = locals;
            Value = SetParentOperation(value, this);
        }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public IOperation Value { get; }
    }
    internal sealed partial class FieldInitializerOperation : BaseSymbolInitializerOperation, IFieldInitializerOperation
    {
        internal FieldInitializerOperation(ImmutableArray<IFieldSymbol> initializedFields, ImmutableArray<ILocalSymbol> locals, IOperation value, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(locals, value, semanticModel, syntax, isImplicit)
        {
            InitializedFields = initializedFields;
        }
        public ImmutableArray<IFieldSymbol> InitializedFields { get; }
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.FieldInitializer;
        public override void Accept(OperationVisitor visitor) => visitor.VisitFieldInitializer(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitFieldInitializer(this, argument);
    }
    internal sealed partial class VariableInitializerOperation : BaseSymbolInitializerOperation, IVariableInitializerOperation
    {
        internal VariableInitializerOperation(ImmutableArray<ILocalSymbol> locals, IOperation value, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(locals, value, semanticModel, syntax, isImplicit) { }
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.VariableInitializer;
        public override void Accept(OperationVisitor visitor) => visitor.VisitVariableInitializer(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitVariableInitializer(this, argument);
    }
    internal sealed partial class PropertyInitializerOperation : BaseSymbolInitializerOperation, IPropertyInitializerOperation
    {
        internal PropertyInitializerOperation(ImmutableArray<IPropertySymbol> initializedProperties, ImmutableArray<ILocalSymbol> locals, IOperation value, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(locals, value, semanticModel, syntax, isImplicit)
        {
            InitializedProperties = initializedProperties;
        }
        public ImmutableArray<IPropertySymbol> InitializedProperties { get; }
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.PropertyInitializer;
        public override void Accept(OperationVisitor visitor) => visitor.VisitPropertyInitializer(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitPropertyInitializer(this, argument);
    }
    internal sealed partial class ParameterInitializerOperation : BaseSymbolInitializerOperation, IParameterInitializerOperation
    {
        internal ParameterInitializerOperation(IParameterSymbol parameter, ImmutableArray<ILocalSymbol> locals, IOperation value, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(locals, value, semanticModel, syntax, isImplicit)
        {
            Parameter = parameter;
        }
        public IParameterSymbol Parameter { get; }
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ParameterInitializer;
        public override void Accept(OperationVisitor visitor) => visitor.VisitParameterInitializer(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitParameterInitializer(this, argument);
    }
    internal sealed partial class ArrayInitializerOperation : Operation, IArrayInitializerOperation
    {
        internal ArrayInitializerOperation(ImmutableArray<IOperation> elementValues, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            ElementValues = SetParentOperation(elementValues, this);
        }
        public ImmutableArray<IOperation> ElementValues { get; }
        internal override int ChildOperationsCount =>
            ElementValues.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < ElementValues.Length
                    => ElementValues[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!ElementValues.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < ElementValues.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!ElementValues.IsEmpty) return (true, 0, ElementValues.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ArrayInitializer;
        public override void Accept(OperationVisitor visitor) => visitor.VisitArrayInitializer(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitArrayInitializer(this, argument);
    }
    internal sealed partial class VariableDeclaratorOperation : Operation, IVariableDeclaratorOperation
    {
        internal VariableDeclaratorOperation(ILocalSymbol symbol, IVariableInitializerOperation? initializer, ImmutableArray<IOperation> ignoredArguments, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Symbol = symbol;
            Initializer = SetParentOperation(initializer, this);
            IgnoredArguments = SetParentOperation(ignoredArguments, this);
        }
        public ILocalSymbol Symbol { get; }
        public IVariableInitializerOperation? Initializer { get; }
        public ImmutableArray<IOperation> IgnoredArguments { get; }
        internal override int ChildOperationsCount =>
            (Initializer is null ? 0 : 1) +
            IgnoredArguments.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < IgnoredArguments.Length
                    => IgnoredArguments[index],
                1 when Initializer != null
                    => Initializer,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!IgnoredArguments.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < IgnoredArguments.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                    if (Initializer != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Initializer != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (!IgnoredArguments.IsEmpty) return (true, 0, IgnoredArguments.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.VariableDeclarator;
        public override void Accept(OperationVisitor visitor) => visitor.VisitVariableDeclarator(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitVariableDeclarator(this, argument);
    }
    internal sealed partial class VariableDeclarationOperation : Operation, IVariableDeclarationOperation
    {
        internal VariableDeclarationOperation(ImmutableArray<IVariableDeclaratorOperation> declarators, IVariableInitializerOperation? initializer, ImmutableArray<IOperation> ignoredDimensions, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Declarators = SetParentOperation(declarators, this);
            Initializer = SetParentOperation(initializer, this);
            IgnoredDimensions = SetParentOperation(ignoredDimensions, this);
        }
        public ImmutableArray<IVariableDeclaratorOperation> Declarators { get; }
        public IVariableInitializerOperation? Initializer { get; }
        public ImmutableArray<IOperation> IgnoredDimensions { get; }
        internal override int ChildOperationsCount =>
            Declarators.Length +
            (Initializer is null ? 0 : 1) +
            IgnoredDimensions.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < IgnoredDimensions.Length
                    => IgnoredDimensions[index],
                1 when index < Declarators.Length
                    => Declarators[index],
                2 when Initializer != null
                    => Initializer,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!IgnoredDimensions.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < IgnoredDimensions.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                    if (!Declarators.IsEmpty) return (true, 1, 0);
                    else goto case 1;
                case 1 when previousIndex + 1 < Declarators.Length:
                    return (true, 1, previousIndex + 1);
                case 1:
                    if (Initializer != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                case 3:
                    return (false, 3, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Initializer != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (!Declarators.IsEmpty) return (true, 1, Declarators.Length - 1);
                    else goto case 1;
                case 1 when previousIndex > 0:
                    return (true, 1, previousIndex - 1);
                case 1:
                    if (!IgnoredDimensions.IsEmpty) return (true, 0, IgnoredDimensions.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.VariableDeclaration;
        public override void Accept(OperationVisitor visitor) => visitor.VisitVariableDeclaration(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitVariableDeclaration(this, argument);
    }
    internal sealed partial class ArgumentOperation : Operation, IArgumentOperation
    {
        internal ArgumentOperation(ArgumentKind argumentKind, IParameterSymbol? parameter, IOperation value, IConvertibleConversion inConversion, IConvertibleConversion outConversion, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            ArgumentKind = argumentKind;
            Parameter = parameter;
            Value = SetParentOperation(value, this);
            InConversionConvertible = inConversion;
            OutConversionConvertible = outConversion;
        }
        public ArgumentKind ArgumentKind { get; }
        public IParameterSymbol? Parameter { get; }
        public IOperation Value { get; }
        internal IConvertibleConversion InConversionConvertible { get; }
        public CommonConversion InConversion => InConversionConvertible.ToCommonConversion();
        internal IConvertibleConversion OutConversionConvertible { get; }
        public CommonConversion OutConversion => OutConversionConvertible.ToCommonConversion();
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Argument;
        public override void Accept(OperationVisitor visitor) => visitor.VisitArgument(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitArgument(this, argument);
    }
    internal sealed partial class CatchClauseOperation : Operation, ICatchClauseOperation
    {
        internal CatchClauseOperation(IOperation? exceptionDeclarationOrExpression, ITypeSymbol exceptionType, ImmutableArray<ILocalSymbol> locals, IOperation? filter, IBlockOperation handler, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            ExceptionDeclarationOrExpression = SetParentOperation(exceptionDeclarationOrExpression, this);
            ExceptionType = exceptionType;
            Locals = locals;
            Filter = SetParentOperation(filter, this);
            Handler = SetParentOperation(handler, this);
        }
        public IOperation? ExceptionDeclarationOrExpression { get; }
        public ITypeSymbol ExceptionType { get; }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public IOperation? Filter { get; }
        public IBlockOperation Handler { get; }
        internal override int ChildOperationsCount =>
            (ExceptionDeclarationOrExpression is null ? 0 : 1) +
            (Filter is null ? 0 : 1) +
            (Handler is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when ExceptionDeclarationOrExpression != null
                    => ExceptionDeclarationOrExpression,
                1 when Filter != null
                    => Filter,
                2 when Handler != null
                    => Handler,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (ExceptionDeclarationOrExpression != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Filter != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Handler != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                case 3:
                    return (false, 3, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Handler != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (Filter != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (ExceptionDeclarationOrExpression != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.CatchClause;
        public override void Accept(OperationVisitor visitor) => visitor.VisitCatchClause(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitCatchClause(this, argument);
    }
    internal sealed partial class SwitchCaseOperation : Operation, ISwitchCaseOperation
    {
        internal SwitchCaseOperation(ImmutableArray<ICaseClauseOperation> clauses, ImmutableArray<IOperation> body, ImmutableArray<ILocalSymbol> locals, IOperation? condition, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Clauses = SetParentOperation(clauses, this);
            Body = SetParentOperation(body, this);
            Locals = locals;
            Condition = SetParentOperation(condition, this);
        }
        public ImmutableArray<ICaseClauseOperation> Clauses { get; }
        public ImmutableArray<IOperation> Body { get; }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public IOperation? Condition { get; }
        internal override int ChildOperationsCount =>
            Clauses.Length +
            Body.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Clauses.Length
                    => Clauses[index],
                1 when index < Body.Length
                    => Body[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Clauses.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Clauses.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                    if (!Body.IsEmpty) return (true, 1, 0);
                    else goto case 1;
                case 1 when previousIndex + 1 < Body.Length:
                    return (true, 1, previousIndex + 1);
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Body.IsEmpty) return (true, 1, Body.Length - 1);
                    else goto case 1;
                case 1 when previousIndex > 0:
                    return (true, 1, previousIndex - 1);
                case 1:
                    if (!Clauses.IsEmpty) return (true, 0, Clauses.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.SwitchCase;
        public override void Accept(OperationVisitor visitor) => visitor.VisitSwitchCase(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitSwitchCase(this, argument);
    }
    internal abstract partial class BaseCaseClauseOperation : Operation, ICaseClauseOperation
    {
        protected BaseCaseClauseOperation(ILabelSymbol? label, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Label = label;
        }
        public abstract CaseKind CaseKind { get; }
        public ILabelSymbol? Label { get; }
    }
    internal sealed partial class DefaultCaseClauseOperation : BaseCaseClauseOperation, IDefaultCaseClauseOperation
    {
        internal DefaultCaseClauseOperation(ILabelSymbol? label, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(label, semanticModel, syntax, isImplicit) { }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.CaseClause;
        public override void Accept(OperationVisitor visitor) => visitor.VisitDefaultCaseClause(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitDefaultCaseClause(this, argument);
    }
    internal sealed partial class PatternCaseClauseOperation : BaseCaseClauseOperation, IPatternCaseClauseOperation
    {
        internal PatternCaseClauseOperation(ILabelSymbol label, IPatternOperation pattern, IOperation? guard, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(label, semanticModel, syntax, isImplicit)
        {
            Pattern = SetParentOperation(pattern, this);
            Guard = SetParentOperation(guard, this);
        }
        public new ILabelSymbol Label => base.Label!;
        public IPatternOperation Pattern { get; }
        public IOperation? Guard { get; }
        internal override int ChildOperationsCount =>
            (Pattern is null ? 0 : 1) +
            (Guard is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Pattern != null
                    => Pattern,
                1 when Guard != null
                    => Guard,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Pattern != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Guard != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Guard != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Pattern != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.CaseClause;
        public override void Accept(OperationVisitor visitor) => visitor.VisitPatternCaseClause(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitPatternCaseClause(this, argument);
    }
    internal sealed partial class RangeCaseClauseOperation : BaseCaseClauseOperation, IRangeCaseClauseOperation
    {
        internal RangeCaseClauseOperation(IOperation minimumValue, IOperation maximumValue, ILabelSymbol? label, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(label, semanticModel, syntax, isImplicit)
        {
            MinimumValue = SetParentOperation(minimumValue, this);
            MaximumValue = SetParentOperation(maximumValue, this);
        }
        public IOperation MinimumValue { get; }
        public IOperation MaximumValue { get; }
        internal override int ChildOperationsCount =>
            (MinimumValue is null ? 0 : 1) +
            (MaximumValue is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when MinimumValue != null
                    => MinimumValue,
                1 when MaximumValue != null
                    => MaximumValue,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (MinimumValue != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (MaximumValue != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (MaximumValue != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (MinimumValue != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.CaseClause;
        public override void Accept(OperationVisitor visitor) => visitor.VisitRangeCaseClause(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitRangeCaseClause(this, argument);
    }
    internal sealed partial class RelationalCaseClauseOperation : BaseCaseClauseOperation, IRelationalCaseClauseOperation
    {
        internal RelationalCaseClauseOperation(IOperation value, BinaryOperatorKind relation, ILabelSymbol? label, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(label, semanticModel, syntax, isImplicit)
        {
            Value = SetParentOperation(value, this);
            Relation = relation;
        }
        public IOperation Value { get; }
        public BinaryOperatorKind Relation { get; }
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.CaseClause;
        public override void Accept(OperationVisitor visitor) => visitor.VisitRelationalCaseClause(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitRelationalCaseClause(this, argument);
    }
    internal sealed partial class SingleValueCaseClauseOperation : BaseCaseClauseOperation, ISingleValueCaseClauseOperation
    {
        internal SingleValueCaseClauseOperation(IOperation value, ILabelSymbol? label, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(label, semanticModel, syntax, isImplicit)
        {
            Value = SetParentOperation(value, this);
        }
        public IOperation Value { get; }
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.CaseClause;
        public override void Accept(OperationVisitor visitor) => visitor.VisitSingleValueCaseClause(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitSingleValueCaseClause(this, argument);
    }
    internal abstract partial class BaseInterpolatedStringContentOperation : Operation, IInterpolatedStringContentOperation
    {
        protected BaseInterpolatedStringContentOperation(SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit) { }
    }
    internal sealed partial class InterpolatedStringTextOperation : BaseInterpolatedStringContentOperation, IInterpolatedStringTextOperation
    {
        internal InterpolatedStringTextOperation(IOperation text, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Text = SetParentOperation(text, this);
        }
        public IOperation Text { get; }
        internal override int ChildOperationsCount =>
            (Text is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Text != null
                    => Text,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Text != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Text != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.InterpolatedStringText;
        public override void Accept(OperationVisitor visitor) => visitor.VisitInterpolatedStringText(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitInterpolatedStringText(this, argument);
    }
    internal sealed partial class InterpolationOperation : BaseInterpolatedStringContentOperation, IInterpolationOperation
    {
        internal InterpolationOperation(IOperation expression, IOperation? alignment, IOperation? formatString, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Expression = SetParentOperation(expression, this);
            Alignment = SetParentOperation(alignment, this);
            FormatString = SetParentOperation(formatString, this);
        }
        public IOperation Expression { get; }
        public IOperation? Alignment { get; }
        public IOperation? FormatString { get; }
        internal override int ChildOperationsCount =>
            (Expression is null ? 0 : 1) +
            (Alignment is null ? 0 : 1) +
            (FormatString is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Expression != null
                    => Expression,
                1 when Alignment != null
                    => Alignment,
                2 when FormatString != null
                    => FormatString,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Expression != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Alignment != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (FormatString != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                case 3:
                    return (false, 3, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (FormatString != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (Alignment != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Expression != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Interpolation;
        public override void Accept(OperationVisitor visitor) => visitor.VisitInterpolation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitInterpolation(this, argument);
    }
    internal abstract partial class BasePatternOperation : Operation, IPatternOperation
    {
        protected BasePatternOperation(ITypeSymbol inputType, ITypeSymbol narrowedType, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            InputType = inputType;
            NarrowedType = narrowedType;
        }
        public ITypeSymbol InputType { get; }
        public ITypeSymbol NarrowedType { get; }
    }
    internal sealed partial class ConstantPatternOperation : BasePatternOperation, IConstantPatternOperation
    {
        internal ConstantPatternOperation(IOperation value, ITypeSymbol inputType, ITypeSymbol narrowedType, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(inputType, narrowedType, semanticModel, syntax, isImplicit)
        {
            Value = SetParentOperation(value, this);
        }
        public IOperation Value { get; }
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ConstantPattern;
        public override void Accept(OperationVisitor visitor) => visitor.VisitConstantPattern(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitConstantPattern(this, argument);
    }
    internal sealed partial class DeclarationPatternOperation : BasePatternOperation, IDeclarationPatternOperation
    {
        internal DeclarationPatternOperation(ITypeSymbol? matchedType, bool matchesNull, ISymbol? declaredSymbol, ITypeSymbol inputType, ITypeSymbol narrowedType, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(inputType, narrowedType, semanticModel, syntax, isImplicit)
        {
            MatchedType = matchedType;
            MatchesNull = matchesNull;
            DeclaredSymbol = declaredSymbol;
        }
        public ITypeSymbol? MatchedType { get; }
        public bool MatchesNull { get; }
        public ISymbol? DeclaredSymbol { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.DeclarationPattern;
        public override void Accept(OperationVisitor visitor) => visitor.VisitDeclarationPattern(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitDeclarationPattern(this, argument);
    }
    internal sealed partial class TupleBinaryOperation : Operation, ITupleBinaryOperation
    {
        internal TupleBinaryOperation(BinaryOperatorKind operatorKind, IOperation leftOperand, IOperation rightOperand, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            OperatorKind = operatorKind;
            LeftOperand = SetParentOperation(leftOperand, this);
            RightOperand = SetParentOperation(rightOperand, this);
            Type = type;
        }
        public BinaryOperatorKind OperatorKind { get; }
        public IOperation LeftOperand { get; }
        public IOperation RightOperand { get; }
        internal override int ChildOperationsCount =>
            (LeftOperand is null ? 0 : 1) +
            (RightOperand is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when LeftOperand != null
                    => LeftOperand,
                1 when RightOperand != null
                    => RightOperand,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (LeftOperand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (RightOperand != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (RightOperand != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (LeftOperand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.TupleBinary;
        public override void Accept(OperationVisitor visitor) => visitor.VisitTupleBinaryOperator(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitTupleBinaryOperator(this, argument);
    }
    internal abstract partial class BaseMethodBodyBaseOperation : Operation, IMethodBodyBaseOperation
    {
        protected BaseMethodBodyBaseOperation(IBlockOperation? blockBody, IBlockOperation? expressionBody, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            BlockBody = SetParentOperation(blockBody, this);
            ExpressionBody = SetParentOperation(expressionBody, this);
        }
        public IBlockOperation? BlockBody { get; }
        public IBlockOperation? ExpressionBody { get; }
    }
    internal sealed partial class MethodBodyOperation : BaseMethodBodyBaseOperation, IMethodBodyOperation
    {
        internal MethodBodyOperation(IBlockOperation? blockBody, IBlockOperation? expressionBody, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(blockBody, expressionBody, semanticModel, syntax, isImplicit) { }
        internal override int ChildOperationsCount =>
            (BlockBody is null ? 0 : 1) +
            (ExpressionBody is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when BlockBody != null
                    => BlockBody,
                1 when ExpressionBody != null
                    => ExpressionBody,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (BlockBody != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (ExpressionBody != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (ExpressionBody != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (BlockBody != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.MethodBody;
        public override void Accept(OperationVisitor visitor) => visitor.VisitMethodBodyOperation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitMethodBodyOperation(this, argument);
    }
    internal sealed partial class ConstructorBodyOperation : BaseMethodBodyBaseOperation, IConstructorBodyOperation
    {
        internal ConstructorBodyOperation(ImmutableArray<ILocalSymbol> locals, IOperation? initializer, IBlockOperation? blockBody, IBlockOperation? expressionBody, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(blockBody, expressionBody, semanticModel, syntax, isImplicit)
        {
            Locals = locals;
            Initializer = SetParentOperation(initializer, this);
        }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public IOperation? Initializer { get; }
        internal override int ChildOperationsCount =>
            (Initializer is null ? 0 : 1) +
            (BlockBody is null ? 0 : 1) +
            (ExpressionBody is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Initializer != null
                    => Initializer,
                1 when BlockBody != null
                    => BlockBody,
                2 when ExpressionBody != null
                    => ExpressionBody,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Initializer != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (BlockBody != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (ExpressionBody != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                case 3:
                    return (false, 3, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (ExpressionBody != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (BlockBody != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Initializer != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ConstructorBody;
        public override void Accept(OperationVisitor visitor) => visitor.VisitConstructorBodyOperation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitConstructorBodyOperation(this, argument);
    }
    internal sealed partial class DiscardOperation : Operation, IDiscardOperation
    {
        internal DiscardOperation(IDiscardSymbol discardSymbol, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            DiscardSymbol = discardSymbol;
            Type = type;
        }
        public IDiscardSymbol DiscardSymbol { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Discard;
        public override void Accept(OperationVisitor visitor) => visitor.VisitDiscardOperation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitDiscardOperation(this, argument);
    }
    internal sealed partial class FlowCaptureOperation : Operation, IFlowCaptureOperation
    {
        internal FlowCaptureOperation(CaptureId id, IOperation value, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Id = id;
            Value = SetParentOperation(value, this);
        }
        public CaptureId Id { get; }
        public IOperation Value { get; }
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.FlowCapture;
        public override void Accept(OperationVisitor visitor) => visitor.VisitFlowCapture(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitFlowCapture(this, argument);
    }
    internal sealed partial class FlowCaptureReferenceOperation : Operation, IFlowCaptureReferenceOperation
    {
        internal FlowCaptureReferenceOperation(CaptureId id, bool isInitialization, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Id = id;
            IsInitialization = isInitialization;
            OperationConstantValue = constantValue;
            Type = type;
        }
        public CaptureId Id { get; }
        public bool IsInitialization { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.FlowCaptureReference;
        public override void Accept(OperationVisitor visitor) => visitor.VisitFlowCaptureReference(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitFlowCaptureReference(this, argument);
    }
    internal sealed partial class IsNullOperation : Operation, IIsNullOperation
    {
        internal IsNullOperation(IOperation operand, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, ConstantValue? constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Operand = SetParentOperation(operand, this);
            OperationConstantValue = constantValue;
            Type = type;
        }
        public IOperation Operand { get; }
        internal override int ChildOperationsCount =>
            (Operand is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operand != null
                    => Operand,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue { get; }
        public override OperationKind Kind => OperationKind.IsNull;
        public override void Accept(OperationVisitor visitor) => visitor.VisitIsNull(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitIsNull(this, argument);
    }
    internal sealed partial class CaughtExceptionOperation : Operation, ICaughtExceptionOperation
    {
        internal CaughtExceptionOperation(SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Type = type;
        }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.CaughtException;
        public override void Accept(OperationVisitor visitor) => visitor.VisitCaughtException(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitCaughtException(this, argument);
    }
    internal sealed partial class StaticLocalInitializationSemaphoreOperation : Operation, IStaticLocalInitializationSemaphoreOperation
    {
        internal StaticLocalInitializationSemaphoreOperation(ILocalSymbol local, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Local = local;
            Type = type;
        }
        public ILocalSymbol Local { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.StaticLocalInitializationSemaphore;
        public override void Accept(OperationVisitor visitor) => visitor.VisitStaticLocalInitializationSemaphore(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitStaticLocalInitializationSemaphore(this, argument);
    }
    internal sealed partial class CoalesceAssignmentOperation : BaseAssignmentOperation, ICoalesceAssignmentOperation
    {
        internal CoalesceAssignmentOperation(IOperation target, IOperation value, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(target, value, semanticModel, syntax, isImplicit)
        {
            Type = type;
        }
        internal override int ChildOperationsCount =>
            (Target is null ? 0 : 1) +
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Target != null
                    => Target,
                1 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Value != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.CoalesceAssignment;
        public override void Accept(OperationVisitor visitor) => visitor.VisitCoalesceAssignment(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitCoalesceAssignment(this, argument);
    }
    internal sealed partial class RangeOperation : Operation, IRangeOperation
    {
        internal RangeOperation(IOperation? leftOperand, IOperation? rightOperand, bool isLifted, IMethodSymbol? method, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            LeftOperand = SetParentOperation(leftOperand, this);
            RightOperand = SetParentOperation(rightOperand, this);
            IsLifted = isLifted;
            Method = method;
            Type = type;
        }
        public IOperation? LeftOperand { get; }
        public IOperation? RightOperand { get; }
        public bool IsLifted { get; }
        public IMethodSymbol? Method { get; }
        internal override int ChildOperationsCount =>
            (LeftOperand is null ? 0 : 1) +
            (RightOperand is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when LeftOperand != null
                    => LeftOperand,
                1 when RightOperand != null
                    => RightOperand,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (LeftOperand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (RightOperand != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (RightOperand != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (LeftOperand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Range;
        public override void Accept(OperationVisitor visitor) => visitor.VisitRangeOperation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitRangeOperation(this, argument);
    }
    internal sealed partial class ReDimOperation : Operation, IReDimOperation
    {
        internal ReDimOperation(ImmutableArray<IReDimClauseOperation> clauses, bool preserve, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Clauses = SetParentOperation(clauses, this);
            Preserve = preserve;
        }
        public ImmutableArray<IReDimClauseOperation> Clauses { get; }
        public bool Preserve { get; }
        internal override int ChildOperationsCount =>
            Clauses.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Clauses.Length
                    => Clauses[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Clauses.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Clauses.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Clauses.IsEmpty) return (true, 0, Clauses.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ReDim;
        public override void Accept(OperationVisitor visitor) => visitor.VisitReDim(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitReDim(this, argument);
    }
    internal sealed partial class ReDimClauseOperation : Operation, IReDimClauseOperation
    {
        internal ReDimClauseOperation(IOperation operand, ImmutableArray<IOperation> dimensionSizes, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Operand = SetParentOperation(operand, this);
            DimensionSizes = SetParentOperation(dimensionSizes, this);
        }
        public IOperation Operand { get; }
        public ImmutableArray<IOperation> DimensionSizes { get; }
        internal override int ChildOperationsCount =>
            (Operand is null ? 0 : 1) +
            DimensionSizes.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operand != null
                    => Operand,
                1 when index < DimensionSizes.Length
                    => DimensionSizes[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (!DimensionSizes.IsEmpty) return (true, 1, 0);
                    else goto case 1;
                case 1 when previousIndex + 1 < DimensionSizes.Length:
                    return (true, 1, previousIndex + 1);
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!DimensionSizes.IsEmpty) return (true, 1, DimensionSizes.Length - 1);
                    else goto case 1;
                case 1 when previousIndex > 0:
                    return (true, 1, previousIndex - 1);
                case 1:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ReDimClause;
        public override void Accept(OperationVisitor visitor) => visitor.VisitReDimClause(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitReDimClause(this, argument);
    }
    internal sealed partial class RecursivePatternOperation : BasePatternOperation, IRecursivePatternOperation
    {
        internal RecursivePatternOperation(ITypeSymbol matchedType, ISymbol? deconstructSymbol, ImmutableArray<IPatternOperation> deconstructionSubpatterns, ImmutableArray<IPropertySubpatternOperation> propertySubpatterns, ISymbol? declaredSymbol, ITypeSymbol inputType, ITypeSymbol narrowedType, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(inputType, narrowedType, semanticModel, syntax, isImplicit)
        {
            MatchedType = matchedType;
            DeconstructSymbol = deconstructSymbol;
            DeconstructionSubpatterns = SetParentOperation(deconstructionSubpatterns, this);
            PropertySubpatterns = SetParentOperation(propertySubpatterns, this);
            DeclaredSymbol = declaredSymbol;
        }
        public ITypeSymbol MatchedType { get; }
        public ISymbol? DeconstructSymbol { get; }
        public ImmutableArray<IPatternOperation> DeconstructionSubpatterns { get; }
        public ImmutableArray<IPropertySubpatternOperation> PropertySubpatterns { get; }
        public ISymbol? DeclaredSymbol { get; }
        internal override int ChildOperationsCount =>
            DeconstructionSubpatterns.Length +
            PropertySubpatterns.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < DeconstructionSubpatterns.Length
                    => DeconstructionSubpatterns[index],
                1 when index < PropertySubpatterns.Length
                    => PropertySubpatterns[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!DeconstructionSubpatterns.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < DeconstructionSubpatterns.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                    if (!PropertySubpatterns.IsEmpty) return (true, 1, 0);
                    else goto case 1;
                case 1 when previousIndex + 1 < PropertySubpatterns.Length:
                    return (true, 1, previousIndex + 1);
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!PropertySubpatterns.IsEmpty) return (true, 1, PropertySubpatterns.Length - 1);
                    else goto case 1;
                case 1 when previousIndex > 0:
                    return (true, 1, previousIndex - 1);
                case 1:
                    if (!DeconstructionSubpatterns.IsEmpty) return (true, 0, DeconstructionSubpatterns.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.RecursivePattern;
        public override void Accept(OperationVisitor visitor) => visitor.VisitRecursivePattern(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitRecursivePattern(this, argument);
    }
    internal sealed partial class DiscardPatternOperation : BasePatternOperation, IDiscardPatternOperation
    {
        internal DiscardPatternOperation(ITypeSymbol inputType, ITypeSymbol narrowedType, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(inputType, narrowedType, semanticModel, syntax, isImplicit) { }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.DiscardPattern;
        public override void Accept(OperationVisitor visitor) => visitor.VisitDiscardPattern(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitDiscardPattern(this, argument);
    }
    internal sealed partial class SwitchExpressionOperation : Operation, ISwitchExpressionOperation
    {
        internal SwitchExpressionOperation(IOperation value, ImmutableArray<ISwitchExpressionArmOperation> arms, bool isExhaustive, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Value = SetParentOperation(value, this);
            Arms = SetParentOperation(arms, this);
            IsExhaustive = isExhaustive;
            Type = type;
        }
        public IOperation Value { get; }
        public ImmutableArray<ISwitchExpressionArmOperation> Arms { get; }
        public bool IsExhaustive { get; }
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1) +
            Arms.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                1 when index < Arms.Length
                    => Arms[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (!Arms.IsEmpty) return (true, 1, 0);
                    else goto case 1;
                case 1 when previousIndex + 1 < Arms.Length:
                    return (true, 1, previousIndex + 1);
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Arms.IsEmpty) return (true, 1, Arms.Length - 1);
                    else goto case 1;
                case 1 when previousIndex > 0:
                    return (true, 1, previousIndex - 1);
                case 1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.SwitchExpression;
        public override void Accept(OperationVisitor visitor) => visitor.VisitSwitchExpression(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitSwitchExpression(this, argument);
    }
    internal sealed partial class SwitchExpressionArmOperation : Operation, ISwitchExpressionArmOperation
    {
        internal SwitchExpressionArmOperation(IPatternOperation pattern, IOperation? guard, IOperation value, ImmutableArray<ILocalSymbol> locals, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Pattern = SetParentOperation(pattern, this);
            Guard = SetParentOperation(guard, this);
            Value = SetParentOperation(value, this);
            Locals = locals;
        }
        public IPatternOperation Pattern { get; }
        public IOperation? Guard { get; }
        public IOperation Value { get; }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        internal override int ChildOperationsCount =>
            (Pattern is null ? 0 : 1) +
            (Guard is null ? 0 : 1) +
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Pattern != null
                    => Pattern,
                1 when Guard != null
                    => Guard,
                2 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Pattern != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Guard != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Value != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                case 3:
                    return (false, 3, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 2, 0);
                    else goto case 2;
                case 2:
                    if (Guard != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Pattern != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.SwitchExpressionArm;
        public override void Accept(OperationVisitor visitor) => visitor.VisitSwitchExpressionArm(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitSwitchExpressionArm(this, argument);
    }
    internal sealed partial class PropertySubpatternOperation : Operation, IPropertySubpatternOperation
    {
        internal PropertySubpatternOperation(IOperation member, IPatternOperation pattern, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Member = SetParentOperation(member, this);
            Pattern = SetParentOperation(pattern, this);
        }
        public IOperation Member { get; }
        public IPatternOperation Pattern { get; }
        internal override int ChildOperationsCount =>
            (Member is null ? 0 : 1) +
            (Pattern is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Member != null
                    => Member,
                1 when Pattern != null
                    => Pattern,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Member != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Pattern != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Pattern != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Member != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.PropertySubpattern;
        public override void Accept(OperationVisitor visitor) => visitor.VisitPropertySubpattern(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitPropertySubpattern(this, argument);
    }
    internal sealed partial class AggregateQueryOperation : Operation, IAggregateQueryOperation
    {
        internal AggregateQueryOperation(IOperation group, IOperation aggregation, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Group = SetParentOperation(group, this);
            Aggregation = SetParentOperation(aggregation, this);
            Type = type;
        }
        public IOperation Group { get; }
        public IOperation Aggregation { get; }
        internal override int ChildOperationsCount =>
            (Group is null ? 0 : 1) +
            (Aggregation is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Group != null
                    => Group,
                1 when Aggregation != null
                    => Aggregation,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Group != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Aggregation != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Aggregation != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Group != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.None;
        public override void Accept(OperationVisitor visitor) => visitor.VisitAggregateQuery(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitAggregateQuery(this, argument);
    }
    internal sealed partial class FixedOperation : Operation, IFixedOperation
    {
        internal FixedOperation(ImmutableArray<ILocalSymbol> locals, IVariableDeclarationGroupOperation variables, IOperation body, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Locals = locals;
            Variables = SetParentOperation(variables, this);
            Body = SetParentOperation(body, this);
        }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public IVariableDeclarationGroupOperation Variables { get; }
        public IOperation Body { get; }
        internal override int ChildOperationsCount =>
            (Variables is null ? 0 : 1) +
            (Body is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Variables != null
                    => Variables,
                1 when Body != null
                    => Body,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Variables != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Body != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Body != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Variables != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.None;
        public override void Accept(OperationVisitor visitor) => visitor.VisitFixed(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitFixed(this, argument);
    }
    internal sealed partial class NoPiaObjectCreationOperation : Operation, INoPiaObjectCreationOperation
    {
        internal NoPiaObjectCreationOperation(IObjectOrCollectionInitializerOperation? initializer, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Initializer = SetParentOperation(initializer, this);
            Type = type;
        }
        public IObjectOrCollectionInitializerOperation? Initializer { get; }
        internal override int ChildOperationsCount =>
            (Initializer is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Initializer != null
                    => Initializer,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Initializer != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Initializer != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.None;
        public override void Accept(OperationVisitor visitor) => visitor.VisitNoPiaObjectCreation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitNoPiaObjectCreation(this, argument);
    }
    internal sealed partial class PlaceholderOperation : Operation, IPlaceholderOperation
    {
        internal PlaceholderOperation(PlaceholderKind placeholderKind, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            PlaceholderKind = placeholderKind;
            Type = type;
        }
        public PlaceholderKind PlaceholderKind { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.None;
        public override void Accept(OperationVisitor visitor) => visitor.VisitPlaceholder(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitPlaceholder(this, argument);
    }
    internal sealed partial class WithStatementOperation : Operation, IWithStatementOperation
    {
        internal WithStatementOperation(IOperation body, IOperation value, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Body = SetParentOperation(body, this);
            Value = SetParentOperation(value, this);
        }
        public IOperation Body { get; }
        public IOperation Value { get; }
        internal override int ChildOperationsCount =>
            (Body is null ? 0 : 1) +
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                1 when Body != null
                    => Body,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Body != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Body != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.None;
        public override void Accept(OperationVisitor visitor) => visitor.VisitWithStatement(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitWithStatement(this, argument);
    }
    internal sealed partial class UsingDeclarationOperation : Operation, IUsingDeclarationOperation
    {
        internal UsingDeclarationOperation(IVariableDeclarationGroupOperation declarationGroup, bool isAsynchronous, DisposeOperationInfo disposeInfo, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            DeclarationGroup = SetParentOperation(declarationGroup, this);
            IsAsynchronous = isAsynchronous;
            DisposeInfo = disposeInfo;
        }
        public IVariableDeclarationGroupOperation DeclarationGroup { get; }
        public bool IsAsynchronous { get; }
        public DisposeOperationInfo DisposeInfo { get; }
        internal override int ChildOperationsCount =>
            (DeclarationGroup is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when DeclarationGroup != null
                    => DeclarationGroup,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (DeclarationGroup != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (DeclarationGroup != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.UsingDeclaration;
        public override void Accept(OperationVisitor visitor) => visitor.VisitUsingDeclaration(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitUsingDeclaration(this, argument);
    }
    internal sealed partial class NegatedPatternOperation : BasePatternOperation, INegatedPatternOperation
    {
        internal NegatedPatternOperation(IPatternOperation pattern, ITypeSymbol inputType, ITypeSymbol narrowedType, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(inputType, narrowedType, semanticModel, syntax, isImplicit)
        {
            Pattern = SetParentOperation(pattern, this);
        }
        public IPatternOperation Pattern { get; }
        internal override int ChildOperationsCount =>
            (Pattern is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Pattern != null
                    => Pattern,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Pattern != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Pattern != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.NegatedPattern;
        public override void Accept(OperationVisitor visitor) => visitor.VisitNegatedPattern(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitNegatedPattern(this, argument);
    }
    internal sealed partial class BinaryPatternOperation : BasePatternOperation, IBinaryPatternOperation
    {
        internal BinaryPatternOperation(BinaryOperatorKind operatorKind, IPatternOperation leftPattern, IPatternOperation rightPattern, ITypeSymbol inputType, ITypeSymbol narrowedType, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(inputType, narrowedType, semanticModel, syntax, isImplicit)
        {
            OperatorKind = operatorKind;
            LeftPattern = SetParentOperation(leftPattern, this);
            RightPattern = SetParentOperation(rightPattern, this);
        }
        public BinaryOperatorKind OperatorKind { get; }
        public IPatternOperation LeftPattern { get; }
        public IPatternOperation RightPattern { get; }
        internal override int ChildOperationsCount =>
            (LeftPattern is null ? 0 : 1) +
            (RightPattern is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when LeftPattern != null
                    => LeftPattern,
                1 when RightPattern != null
                    => RightPattern,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (LeftPattern != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (RightPattern != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (RightPattern != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (LeftPattern != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.BinaryPattern;
        public override void Accept(OperationVisitor visitor) => visitor.VisitBinaryPattern(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitBinaryPattern(this, argument);
    }
    internal sealed partial class TypePatternOperation : BasePatternOperation, ITypePatternOperation
    {
        internal TypePatternOperation(ITypeSymbol matchedType, ITypeSymbol inputType, ITypeSymbol narrowedType, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(inputType, narrowedType, semanticModel, syntax, isImplicit)
        {
            MatchedType = matchedType;
        }
        public ITypeSymbol MatchedType { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.TypePattern;
        public override void Accept(OperationVisitor visitor) => visitor.VisitTypePattern(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitTypePattern(this, argument);
    }
    internal sealed partial class RelationalPatternOperation : BasePatternOperation, IRelationalPatternOperation
    {
        internal RelationalPatternOperation(BinaryOperatorKind operatorKind, IOperation value, ITypeSymbol inputType, ITypeSymbol narrowedType, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(inputType, narrowedType, semanticModel, syntax, isImplicit)
        {
            OperatorKind = operatorKind;
            Value = SetParentOperation(value, this);
        }
        public BinaryOperatorKind OperatorKind { get; }
        public IOperation Value { get; }
        internal override int ChildOperationsCount =>
            (Value is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Value != null
                    => Value,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Value != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.RelationalPattern;
        public override void Accept(OperationVisitor visitor) => visitor.VisitRelationalPattern(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitRelationalPattern(this, argument);
    }
    internal sealed partial class WithOperation : Operation, IWithOperation
    {
        internal WithOperation(IOperation operand, IMethodSymbol? cloneMethod, IObjectOrCollectionInitializerOperation initializer, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Operand = SetParentOperation(operand, this);
            CloneMethod = cloneMethod;
            Initializer = SetParentOperation(initializer, this);
            Type = type;
        }
        public IOperation Operand { get; }
        public IMethodSymbol? CloneMethod { get; }
        public IObjectOrCollectionInitializerOperation Initializer { get; }
        internal override int ChildOperationsCount =>
            (Operand is null ? 0 : 1) +
            (Initializer is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operand != null
                    => Operand,
                1 when Initializer != null
                    => Initializer,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Initializer != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Initializer != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.With;
        public override void Accept(OperationVisitor visitor) => visitor.VisitWith(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitWith(this, argument);
    }
    internal sealed partial class InterpolatedStringHandlerCreationOperation : Operation, IInterpolatedStringHandlerCreationOperation
    {
        internal InterpolatedStringHandlerCreationOperation(IOperation handlerCreation, bool handlerCreationHasSuccessParameter, bool handlerAppendCallsReturnBool, IOperation content, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            HandlerCreation = SetParentOperation(handlerCreation, this);
            HandlerCreationHasSuccessParameter = handlerCreationHasSuccessParameter;
            HandlerAppendCallsReturnBool = handlerAppendCallsReturnBool;
            Content = SetParentOperation(content, this);
            Type = type;
        }
        public IOperation HandlerCreation { get; }
        public bool HandlerCreationHasSuccessParameter { get; }
        public bool HandlerAppendCallsReturnBool { get; }
        public IOperation Content { get; }
        internal override int ChildOperationsCount =>
            (HandlerCreation is null ? 0 : 1) +
            (Content is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when HandlerCreation != null
                    => HandlerCreation,
                1 when Content != null
                    => Content,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (HandlerCreation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Content != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Content != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (HandlerCreation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.InterpolatedStringHandlerCreation;
        public override void Accept(OperationVisitor visitor) => visitor.VisitInterpolatedStringHandlerCreation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitInterpolatedStringHandlerCreation(this, argument);
    }
    internal sealed partial class InterpolatedStringAdditionOperation : Operation, IInterpolatedStringAdditionOperation
    {
        internal InterpolatedStringAdditionOperation(IOperation left, IOperation right, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Left = SetParentOperation(left, this);
            Right = SetParentOperation(right, this);
        }
        public IOperation Left { get; }
        public IOperation Right { get; }
        internal override int ChildOperationsCount =>
            (Left is null ? 0 : 1) +
            (Right is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Left != null
                    => Left,
                1 when Right != null
                    => Right,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Left != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Right != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Right != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Left != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.InterpolatedStringAddition;
        public override void Accept(OperationVisitor visitor) => visitor.VisitInterpolatedStringAddition(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitInterpolatedStringAddition(this, argument);
    }
    internal sealed partial class InterpolatedStringAppendOperation : BaseInterpolatedStringContentOperation, IInterpolatedStringAppendOperation
    {
        internal InterpolatedStringAppendOperation(IOperation appendCall, OperationKind kind, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            AppendCall = SetParentOperation(appendCall, this);
            Kind = kind;
        }
        public IOperation AppendCall { get; }
        internal override int ChildOperationsCount =>
            (AppendCall is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when AppendCall != null
                    => AppendCall,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (AppendCall != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (AppendCall != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind { get; }
        public override void Accept(OperationVisitor visitor) => visitor.VisitInterpolatedStringAppend(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitInterpolatedStringAppend(this, argument);
    }
    internal sealed partial class InterpolatedStringHandlerArgumentPlaceholderOperation : Operation, IInterpolatedStringHandlerArgumentPlaceholderOperation
    {
        internal InterpolatedStringHandlerArgumentPlaceholderOperation(int argumentIndex, InterpolatedStringArgumentPlaceholderKind placeholderKind, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            ArgumentIndex = argumentIndex;
            PlaceholderKind = placeholderKind;
        }
        public int ArgumentIndex { get; }
        public InterpolatedStringArgumentPlaceholderKind PlaceholderKind { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.InterpolatedStringHandlerArgumentPlaceholder;
        public override void Accept(OperationVisitor visitor) => visitor.VisitInterpolatedStringHandlerArgumentPlaceholder(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitInterpolatedStringHandlerArgumentPlaceholder(this, argument);
    }
    internal sealed partial class FunctionPointerInvocationOperation : Operation, IFunctionPointerInvocationOperation
    {
        internal FunctionPointerInvocationOperation(IOperation target, ImmutableArray<IArgumentOperation> arguments, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Target = SetParentOperation(target, this);
            Arguments = SetParentOperation(arguments, this);
            Type = type;
        }
        public IOperation Target { get; }
        public ImmutableArray<IArgumentOperation> Arguments { get; }
        internal override int ChildOperationsCount =>
            (Target is null ? 0 : 1) +
            Arguments.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Target != null
                    => Target,
                1 when index < Arguments.Length
                    => Arguments[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (!Arguments.IsEmpty) return (true, 1, 0);
                    else goto case 1;
                case 1 when previousIndex + 1 < Arguments.Length:
                    return (true, 1, previousIndex + 1);
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Arguments.IsEmpty) return (true, 1, Arguments.Length - 1);
                    else goto case 1;
                case 1 when previousIndex > 0:
                    return (true, 1, previousIndex - 1);
                case 1:
                    if (Target != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.FunctionPointerInvocation;
        public override void Accept(OperationVisitor visitor) => visitor.VisitFunctionPointerInvocation(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitFunctionPointerInvocation(this, argument);
    }
    internal sealed partial class ListPatternOperation : BasePatternOperation, IListPatternOperation
    {
        internal ListPatternOperation(ISymbol? lengthSymbol, ISymbol? indexerSymbol, ImmutableArray<IPatternOperation> patterns, ISymbol? declaredSymbol, ITypeSymbol inputType, ITypeSymbol narrowedType, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(inputType, narrowedType, semanticModel, syntax, isImplicit)
        {
            LengthSymbol = lengthSymbol;
            IndexerSymbol = indexerSymbol;
            Patterns = SetParentOperation(patterns, this);
            DeclaredSymbol = declaredSymbol;
        }
        public ISymbol? LengthSymbol { get; }
        public ISymbol? IndexerSymbol { get; }
        public ImmutableArray<IPatternOperation> Patterns { get; }
        public ISymbol? DeclaredSymbol { get; }
        internal override int ChildOperationsCount =>
            Patterns.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Patterns.Length
                    => Patterns[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Patterns.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Patterns.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Patterns.IsEmpty) return (true, 0, Patterns.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ListPattern;
        public override void Accept(OperationVisitor visitor) => visitor.VisitListPattern(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitListPattern(this, argument);
    }
    internal sealed partial class SlicePatternOperation : BasePatternOperation, ISlicePatternOperation
    {
        internal SlicePatternOperation(ISymbol? sliceSymbol, IPatternOperation? pattern, ITypeSymbol inputType, ITypeSymbol narrowedType, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(inputType, narrowedType, semanticModel, syntax, isImplicit)
        {
            SliceSymbol = sliceSymbol;
            Pattern = SetParentOperation(pattern, this);
        }
        public ISymbol? SliceSymbol { get; }
        public IPatternOperation? Pattern { get; }
        internal override int ChildOperationsCount =>
            (Pattern is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Pattern != null
                    => Pattern,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Pattern != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Pattern != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.SlicePattern;
        public override void Accept(OperationVisitor visitor) => visitor.VisitSlicePattern(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitSlicePattern(this, argument);
    }
    internal sealed partial class ImplicitIndexerReferenceOperation : Operation, IImplicitIndexerReferenceOperation
    {
        internal ImplicitIndexerReferenceOperation(IOperation instance, IOperation argument, ISymbol lengthSymbol, ISymbol indexerSymbol, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Instance = SetParentOperation(instance, this);
            Argument = SetParentOperation(argument, this);
            LengthSymbol = lengthSymbol;
            IndexerSymbol = indexerSymbol;
            Type = type;
        }
        public IOperation Instance { get; }
        public IOperation Argument { get; }
        public ISymbol LengthSymbol { get; }
        public ISymbol IndexerSymbol { get; }
        internal override int ChildOperationsCount =>
            (Instance is null ? 0 : 1) +
            (Argument is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Instance != null
                    => Instance,
                1 when Argument != null
                    => Argument,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Argument != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Argument != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.ImplicitIndexerReference;
        public override void Accept(OperationVisitor visitor) => visitor.VisitImplicitIndexerReference(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitImplicitIndexerReference(this, argument);
    }
    internal sealed partial class Utf8StringOperation : Operation, IUtf8StringOperation
    {
        internal Utf8StringOperation(string value, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Value = value;
            Type = type;
        }
        public string Value { get; }
        internal override int ChildOperationsCount => 0;
        internal override IOperation GetCurrent(int slot, int index) => throw ExceptionUtilities.UnexpectedValue((slot, index));
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex) => (false, int.MinValue, int.MinValue);
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Utf8String;
        public override void Accept(OperationVisitor visitor) => visitor.VisitUtf8String(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitUtf8String(this, argument);
    }
    internal sealed partial class AttributeOperation : Operation, IAttributeOperation
    {
        internal AttributeOperation(IOperation operation, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
        }
        public IOperation Operation { get; }
        internal override int ChildOperationsCount =>
            (Operation is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operation != null
                    => Operation,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Operation != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Attribute;
        public override void Accept(OperationVisitor visitor) => visitor.VisitAttribute(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitAttribute(this, argument);
    }
    internal sealed partial class InlineArrayAccessOperation : Operation, IInlineArrayAccessOperation
    {
        internal InlineArrayAccessOperation(IOperation instance, IOperation argument, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Instance = SetParentOperation(instance, this);
            Argument = SetParentOperation(argument, this);
            Type = type;
        }
        public IOperation Instance { get; }
        public IOperation Argument { get; }
        internal override int ChildOperationsCount =>
            (Instance is null ? 0 : 1) +
            (Argument is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Instance != null
                    => Instance,
                1 when Argument != null
                    => Argument,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                    if (Argument != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                case 2:
                    return (false, 2, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Argument != null) return (true, 1, 0);
                    else goto case 1;
                case 1:
                    if (Instance != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.InlineArrayAccess;
        public override void Accept(OperationVisitor visitor) => visitor.VisitInlineArrayAccess(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitInlineArrayAccess(this, argument);
    }
    internal sealed partial class CollectionExpressionOperation : Operation, ICollectionExpressionOperation
    {
        internal CollectionExpressionOperation(IMethodSymbol? constructMethod, ImmutableArray<IOperation> elements, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol? type, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            ConstructMethod = constructMethod;
            Elements = SetParentOperation(elements, this);
            Type = type;
        }
        public IMethodSymbol? ConstructMethod { get; }
        public ImmutableArray<IOperation> Elements { get; }
        internal override int ChildOperationsCount =>
            Elements.Length;
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when index < Elements.Length
                    => Elements[index],
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (!Elements.IsEmpty) return (true, 0, 0);
                    else goto case 0;
                case 0 when previousIndex + 1 < Elements.Length:
                    return (true, 0, previousIndex + 1);
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (!Elements.IsEmpty) return (true, 0, Elements.Length - 1);
                    else goto case 0;
                case 0 when previousIndex > 0:
                    return (true, 0, previousIndex - 1);
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type { get; }
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.CollectionExpression;
        public override void Accept(OperationVisitor visitor) => visitor.VisitCollectionExpression(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitCollectionExpression(this, argument);
    }
    internal sealed partial class SpreadOperation : Operation, ISpreadOperation
    {
        internal SpreadOperation(IOperation operand, ITypeSymbol? elementType, IConvertibleConversion elementConversion, SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            Operand = SetParentOperation(operand, this);
            ElementType = elementType;
            ElementConversionConvertible = elementConversion;
        }
        public IOperation Operand { get; }
        public ITypeSymbol? ElementType { get; }
        internal IConvertibleConversion ElementConversionConvertible { get; }
        public CommonConversion ElementConversion => ElementConversionConvertible.ToCommonConversion();
        internal override int ChildOperationsCount =>
            (Operand is null ? 0 : 1);
        internal override IOperation GetCurrent(int slot, int index)
            => slot switch
            {
                0 when Operand != null
                    => Operand,
                _ => throw ExceptionUtilities.UnexpectedValue((slot, index)),
            };
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case -1:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case 1:
                    return (false, 1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        internal override (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex)
        {
            switch (previousSlot)
            {
                case int.MaxValue:
                    if (Operand != null) return (true, 0, 0);
                    else goto case 0;
                case 0:
                case -1:
                    return (false, -1, 0);
                default:
                    throw ExceptionUtilities.UnexpectedValue((previousSlot, previousIndex));
            }
        }
        public override ITypeSymbol? Type => null;
        internal override ConstantValue? OperationConstantValue => null;
        public override OperationKind Kind => OperationKind.Spread;
        public override void Accept(OperationVisitor visitor) => visitor.VisitSpread(this);
        public override TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) where TResult : default => visitor.VisitSpread(this, argument);
    }
    #endregion
    #region Cloner
    internal sealed partial class OperationCloner : OperationVisitor<object?, IOperation>
    {
        private static readonly OperationCloner s_instance = new OperationCloner();
        /// <summary>Deep clone given IOperation</summary>
        public static T CloneOperation<T>(T operation) where T : IOperation => s_instance.Visit(operation);
        public OperationCloner() { }
        [return: NotNullIfNotNull("node")]
        private T? Visit<T>(T? node) where T : IOperation? => (T?)Visit(node, argument: null);
        public override IOperation DefaultVisit(IOperation operation, object? argument) => throw ExceptionUtilities.Unreachable();
        private ImmutableArray<T> VisitArray<T>(ImmutableArray<T> nodes) where T : IOperation => nodes.SelectAsArray((n, @this) => @this.Visit(n), this)!;
        private ImmutableArray<(ISymbol, T)> VisitArray<T>(ImmutableArray<(ISymbol, T)> nodes) where T : IOperation => nodes.SelectAsArray((n, @this) => (n.Item1, @this.Visit(n.Item2)), this)!;
        public override IOperation VisitBlock(IBlockOperation operation, object? argument)
        {
            var internalOperation = (BlockOperation)operation;
            return new BlockOperation(VisitArray(internalOperation.Operations), internalOperation.Locals, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, object? argument)
        {
            var internalOperation = (VariableDeclarationGroupOperation)operation;
            return new VariableDeclarationGroupOperation(VisitArray(internalOperation.Declarations), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitSwitch(ISwitchOperation operation, object? argument)
        {
            var internalOperation = (SwitchOperation)operation;
            return new SwitchOperation(internalOperation.Locals, Visit(internalOperation.Value), VisitArray(internalOperation.Cases), internalOperation.ExitLabel, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitForEachLoop(IForEachLoopOperation operation, object? argument)
        {
            var internalOperation = (ForEachLoopOperation)operation;
            return new ForEachLoopOperation(Visit(internalOperation.LoopControlVariable), Visit(internalOperation.Collection), VisitArray(internalOperation.NextVariables), internalOperation.Info, internalOperation.IsAsynchronous, Visit(internalOperation.Body), internalOperation.Locals, internalOperation.ContinueLabel, internalOperation.ExitLabel, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitForLoop(IForLoopOperation operation, object? argument)
        {
            var internalOperation = (ForLoopOperation)operation;
            return new ForLoopOperation(VisitArray(internalOperation.Before), internalOperation.ConditionLocals, Visit(internalOperation.Condition), VisitArray(internalOperation.AtLoopBottom), Visit(internalOperation.Body), internalOperation.Locals, internalOperation.ContinueLabel, internalOperation.ExitLabel, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitForToLoop(IForToLoopOperation operation, object? argument)
        {
            var internalOperation = (ForToLoopOperation)operation;
            return new ForToLoopOperation(Visit(internalOperation.LoopControlVariable), Visit(internalOperation.InitialValue), Visit(internalOperation.LimitValue), Visit(internalOperation.StepValue), internalOperation.IsChecked, VisitArray(internalOperation.NextVariables), internalOperation.Info, Visit(internalOperation.Body), internalOperation.Locals, internalOperation.ContinueLabel, internalOperation.ExitLabel, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitWhileLoop(IWhileLoopOperation operation, object? argument)
        {
            var internalOperation = (WhileLoopOperation)operation;
            return new WhileLoopOperation(Visit(internalOperation.Condition), internalOperation.ConditionIsTop, internalOperation.ConditionIsUntil, Visit(internalOperation.IgnoredCondition), Visit(internalOperation.Body), internalOperation.Locals, internalOperation.ContinueLabel, internalOperation.ExitLabel, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitLabeled(ILabeledOperation operation, object? argument)
        {
            var internalOperation = (LabeledOperation)operation;
            return new LabeledOperation(internalOperation.Label, Visit(internalOperation.Operation), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitBranch(IBranchOperation operation, object? argument)
        {
            var internalOperation = (BranchOperation)operation;
            return new BranchOperation(internalOperation.Target, internalOperation.BranchKind, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitEmpty(IEmptyOperation operation, object? argument)
        {
            var internalOperation = (EmptyOperation)operation;
            return new EmptyOperation(internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitReturn(IReturnOperation operation, object? argument)
        {
            var internalOperation = (ReturnOperation)operation;
            return new ReturnOperation(Visit(internalOperation.ReturnedValue), internalOperation.Kind, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitLock(ILockOperation operation, object? argument)
        {
            var internalOperation = (LockOperation)operation;
            return new LockOperation(Visit(internalOperation.LockedValue), Visit(internalOperation.Body), internalOperation.LockTakenSymbol, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitTry(ITryOperation operation, object? argument)
        {
            var internalOperation = (TryOperation)operation;
            return new TryOperation(Visit(internalOperation.Body), VisitArray(internalOperation.Catches), Visit(internalOperation.Finally), internalOperation.ExitLabel, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitUsing(IUsingOperation operation, object? argument)
        {
            var internalOperation = (UsingOperation)operation;
            return new UsingOperation(Visit(internalOperation.Resources), Visit(internalOperation.Body), internalOperation.Locals, internalOperation.IsAsynchronous, internalOperation.DisposeInfo, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitExpressionStatement(IExpressionStatementOperation operation, object? argument)
        {
            var internalOperation = (ExpressionStatementOperation)operation;
            return new ExpressionStatementOperation(Visit(internalOperation.Operation), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitLocalFunction(ILocalFunctionOperation operation, object? argument)
        {
            var internalOperation = (LocalFunctionOperation)operation;
            return new LocalFunctionOperation(internalOperation.Symbol, Visit(internalOperation.Body), Visit(internalOperation.IgnoredBody), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitStop(IStopOperation operation, object? argument)
        {
            var internalOperation = (StopOperation)operation;
            return new StopOperation(internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitEnd(IEndOperation operation, object? argument)
        {
            var internalOperation = (EndOperation)operation;
            return new EndOperation(internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitRaiseEvent(IRaiseEventOperation operation, object? argument)
        {
            var internalOperation = (RaiseEventOperation)operation;
            return new RaiseEventOperation(Visit(internalOperation.EventReference), VisitArray(internalOperation.Arguments), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitLiteral(ILiteralOperation operation, object? argument)
        {
            var internalOperation = (LiteralOperation)operation;
            return new LiteralOperation(internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitConversion(IConversionOperation operation, object? argument)
        {
            var internalOperation = (ConversionOperation)operation;
            return new ConversionOperation(Visit(internalOperation.Operand), internalOperation.ConversionConvertible, internalOperation.IsTryCast, internalOperation.IsChecked, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitInvocation(IInvocationOperation operation, object? argument)
        {
            var internalOperation = (InvocationOperation)operation;
            return new InvocationOperation(internalOperation.TargetMethod, internalOperation.ConstrainedToType, Visit(internalOperation.Instance), internalOperation.IsVirtual, VisitArray(internalOperation.Arguments), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitArrayElementReference(IArrayElementReferenceOperation operation, object? argument)
        {
            var internalOperation = (ArrayElementReferenceOperation)operation;
            return new ArrayElementReferenceOperation(Visit(internalOperation.ArrayReference), VisitArray(internalOperation.Indices), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitLocalReference(ILocalReferenceOperation operation, object? argument)
        {
            var internalOperation = (LocalReferenceOperation)operation;
            return new LocalReferenceOperation(internalOperation.Local, internalOperation.IsDeclaration, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitParameterReference(IParameterReferenceOperation operation, object? argument)
        {
            var internalOperation = (ParameterReferenceOperation)operation;
            return new ParameterReferenceOperation(internalOperation.Parameter, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitFieldReference(IFieldReferenceOperation operation, object? argument)
        {
            var internalOperation = (FieldReferenceOperation)operation;
            return new FieldReferenceOperation(internalOperation.Field, internalOperation.IsDeclaration, Visit(internalOperation.Instance), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitMethodReference(IMethodReferenceOperation operation, object? argument)
        {
            var internalOperation = (MethodReferenceOperation)operation;
            return new MethodReferenceOperation(internalOperation.Method, internalOperation.ConstrainedToType, internalOperation.IsVirtual, Visit(internalOperation.Instance), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitPropertyReference(IPropertyReferenceOperation operation, object? argument)
        {
            var internalOperation = (PropertyReferenceOperation)operation;
            return new PropertyReferenceOperation(internalOperation.Property, internalOperation.ConstrainedToType, VisitArray(internalOperation.Arguments), Visit(internalOperation.Instance), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitEventReference(IEventReferenceOperation operation, object? argument)
        {
            var internalOperation = (EventReferenceOperation)operation;
            return new EventReferenceOperation(internalOperation.Event, internalOperation.ConstrainedToType, Visit(internalOperation.Instance), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitUnaryOperator(IUnaryOperation operation, object? argument)
        {
            var internalOperation = (UnaryOperation)operation;
            return new UnaryOperation(internalOperation.OperatorKind, Visit(internalOperation.Operand), internalOperation.IsLifted, internalOperation.IsChecked, internalOperation.OperatorMethod, internalOperation.ConstrainedToType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitBinaryOperator(IBinaryOperation operation, object? argument)
        {
            var internalOperation = (BinaryOperation)operation;
            return new BinaryOperation(internalOperation.OperatorKind, Visit(internalOperation.LeftOperand), Visit(internalOperation.RightOperand), internalOperation.IsLifted, internalOperation.IsChecked, internalOperation.IsCompareText, internalOperation.OperatorMethod, internalOperation.ConstrainedToType, internalOperation.UnaryOperatorMethod, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitConditional(IConditionalOperation operation, object? argument)
        {
            var internalOperation = (ConditionalOperation)operation;
            return new ConditionalOperation(Visit(internalOperation.Condition), Visit(internalOperation.WhenTrue), Visit(internalOperation.WhenFalse), internalOperation.IsRef, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitCoalesce(ICoalesceOperation operation, object? argument)
        {
            var internalOperation = (CoalesceOperation)operation;
            return new CoalesceOperation(Visit(internalOperation.Value), Visit(internalOperation.WhenNull), internalOperation.ValueConversionConvertible, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitAnonymousFunction(IAnonymousFunctionOperation operation, object? argument)
        {
            var internalOperation = (AnonymousFunctionOperation)operation;
            return new AnonymousFunctionOperation(internalOperation.Symbol, Visit(internalOperation.Body), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitObjectCreation(IObjectCreationOperation operation, object? argument)
        {
            var internalOperation = (ObjectCreationOperation)operation;
            return new ObjectCreationOperation(internalOperation.Constructor, Visit(internalOperation.Initializer), VisitArray(internalOperation.Arguments), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, object? argument)
        {
            var internalOperation = (TypeParameterObjectCreationOperation)operation;
            return new TypeParameterObjectCreationOperation(Visit(internalOperation.Initializer), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitArrayCreation(IArrayCreationOperation operation, object? argument)
        {
            var internalOperation = (ArrayCreationOperation)operation;
            return new ArrayCreationOperation(VisitArray(internalOperation.DimensionSizes), Visit(internalOperation.Initializer), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitInstanceReference(IInstanceReferenceOperation operation, object? argument)
        {
            var internalOperation = (InstanceReferenceOperation)operation;
            return new InstanceReferenceOperation(internalOperation.ReferenceKind, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitIsType(IIsTypeOperation operation, object? argument)
        {
            var internalOperation = (IsTypeOperation)operation;
            return new IsTypeOperation(Visit(internalOperation.ValueOperand), internalOperation.TypeOperand, internalOperation.IsNegated, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitAwait(IAwaitOperation operation, object? argument)
        {
            var internalOperation = (AwaitOperation)operation;
            return new AwaitOperation(Visit(internalOperation.Operation), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitSimpleAssignment(ISimpleAssignmentOperation operation, object? argument)
        {
            var internalOperation = (SimpleAssignmentOperation)operation;
            return new SimpleAssignmentOperation(internalOperation.IsRef, Visit(internalOperation.Target), Visit(internalOperation.Value), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitCompoundAssignment(ICompoundAssignmentOperation operation, object? argument)
        {
            var internalOperation = (CompoundAssignmentOperation)operation;
            return new CompoundAssignmentOperation(internalOperation.InConversionConvertible, internalOperation.OutConversionConvertible, internalOperation.OperatorKind, internalOperation.IsLifted, internalOperation.IsChecked, internalOperation.OperatorMethod, internalOperation.ConstrainedToType, Visit(internalOperation.Target), Visit(internalOperation.Value), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitParenthesized(IParenthesizedOperation operation, object? argument)
        {
            var internalOperation = (ParenthesizedOperation)operation;
            return new ParenthesizedOperation(Visit(internalOperation.Operand), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitEventAssignment(IEventAssignmentOperation operation, object? argument)
        {
            var internalOperation = (EventAssignmentOperation)operation;
            return new EventAssignmentOperation(Visit(internalOperation.EventReference), Visit(internalOperation.HandlerValue), internalOperation.Adds, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitConditionalAccess(IConditionalAccessOperation operation, object? argument)
        {
            var internalOperation = (ConditionalAccessOperation)operation;
            return new ConditionalAccessOperation(Visit(internalOperation.Operation), Visit(internalOperation.WhenNotNull), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation, object? argument)
        {
            var internalOperation = (ConditionalAccessInstanceOperation)operation;
            return new ConditionalAccessInstanceOperation(internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitInterpolatedString(IInterpolatedStringOperation operation, object? argument)
        {
            var internalOperation = (InterpolatedStringOperation)operation;
            return new InterpolatedStringOperation(VisitArray(internalOperation.Parts), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, object? argument)
        {
            var internalOperation = (AnonymousObjectCreationOperation)operation;
            return new AnonymousObjectCreationOperation(VisitArray(internalOperation.Initializers), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation, object? argument)
        {
            var internalOperation = (ObjectOrCollectionInitializerOperation)operation;
            return new ObjectOrCollectionInitializerOperation(VisitArray(internalOperation.Initializers), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitMemberInitializer(IMemberInitializerOperation operation, object? argument)
        {
            var internalOperation = (MemberInitializerOperation)operation;
            return new MemberInitializerOperation(Visit(internalOperation.InitializedMember), Visit(internalOperation.Initializer), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitNameOf(INameOfOperation operation, object? argument)
        {
            var internalOperation = (NameOfOperation)operation;
            return new NameOfOperation(Visit(internalOperation.Argument), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitTuple(ITupleOperation operation, object? argument)
        {
            var internalOperation = (TupleOperation)operation;
            return new TupleOperation(VisitArray(internalOperation.Elements), internalOperation.NaturalType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, object? argument)
        {
            var internalOperation = (DynamicMemberReferenceOperation)operation;
            return new DynamicMemberReferenceOperation(Visit(internalOperation.Instance), internalOperation.MemberName, internalOperation.TypeArguments, internalOperation.ContainingType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitTranslatedQuery(ITranslatedQueryOperation operation, object? argument)
        {
            var internalOperation = (TranslatedQueryOperation)operation;
            return new TranslatedQueryOperation(Visit(internalOperation.Operation), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitDelegateCreation(IDelegateCreationOperation operation, object? argument)
        {
            var internalOperation = (DelegateCreationOperation)operation;
            return new DelegateCreationOperation(Visit(internalOperation.Target), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitDefaultValue(IDefaultValueOperation operation, object? argument)
        {
            var internalOperation = (DefaultValueOperation)operation;
            return new DefaultValueOperation(internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitTypeOf(ITypeOfOperation operation, object? argument)
        {
            var internalOperation = (TypeOfOperation)operation;
            return new TypeOfOperation(internalOperation.TypeOperand, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitSizeOf(ISizeOfOperation operation, object? argument)
        {
            var internalOperation = (SizeOfOperation)operation;
            return new SizeOfOperation(internalOperation.TypeOperand, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitAddressOf(IAddressOfOperation operation, object? argument)
        {
            var internalOperation = (AddressOfOperation)operation;
            return new AddressOfOperation(Visit(internalOperation.Reference), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitIsPattern(IIsPatternOperation operation, object? argument)
        {
            var internalOperation = (IsPatternOperation)operation;
            return new IsPatternOperation(Visit(internalOperation.Value), Visit(internalOperation.Pattern), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, object? argument)
        {
            var internalOperation = (IncrementOrDecrementOperation)operation;
            return new IncrementOrDecrementOperation(internalOperation.IsPostfix, internalOperation.IsLifted, internalOperation.IsChecked, Visit(internalOperation.Target), internalOperation.OperatorMethod, internalOperation.ConstrainedToType, internalOperation.Kind, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitThrow(IThrowOperation operation, object? argument)
        {
            var internalOperation = (ThrowOperation)operation;
            return new ThrowOperation(Visit(internalOperation.Exception), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, object? argument)
        {
            var internalOperation = (DeconstructionAssignmentOperation)operation;
            return new DeconstructionAssignmentOperation(Visit(internalOperation.Target), Visit(internalOperation.Value), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitDeclarationExpression(IDeclarationExpressionOperation operation, object? argument)
        {
            var internalOperation = (DeclarationExpressionOperation)operation;
            return new DeclarationExpressionOperation(Visit(internalOperation.Expression), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitOmittedArgument(IOmittedArgumentOperation operation, object? argument)
        {
            var internalOperation = (OmittedArgumentOperation)operation;
            return new OmittedArgumentOperation(internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitFieldInitializer(IFieldInitializerOperation operation, object? argument)
        {
            var internalOperation = (FieldInitializerOperation)operation;
            return new FieldInitializerOperation(internalOperation.InitializedFields, internalOperation.Locals, Visit(internalOperation.Value), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitVariableInitializer(IVariableInitializerOperation operation, object? argument)
        {
            var internalOperation = (VariableInitializerOperation)operation;
            return new VariableInitializerOperation(internalOperation.Locals, Visit(internalOperation.Value), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitPropertyInitializer(IPropertyInitializerOperation operation, object? argument)
        {
            var internalOperation = (PropertyInitializerOperation)operation;
            return new PropertyInitializerOperation(internalOperation.InitializedProperties, internalOperation.Locals, Visit(internalOperation.Value), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitParameterInitializer(IParameterInitializerOperation operation, object? argument)
        {
            var internalOperation = (ParameterInitializerOperation)operation;
            return new ParameterInitializerOperation(internalOperation.Parameter, internalOperation.Locals, Visit(internalOperation.Value), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitArrayInitializer(IArrayInitializerOperation operation, object? argument)
        {
            var internalOperation = (ArrayInitializerOperation)operation;
            return new ArrayInitializerOperation(VisitArray(internalOperation.ElementValues), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitVariableDeclarator(IVariableDeclaratorOperation operation, object? argument)
        {
            var internalOperation = (VariableDeclaratorOperation)operation;
            return new VariableDeclaratorOperation(internalOperation.Symbol, Visit(internalOperation.Initializer), VisitArray(internalOperation.IgnoredArguments), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitVariableDeclaration(IVariableDeclarationOperation operation, object? argument)
        {
            var internalOperation = (VariableDeclarationOperation)operation;
            return new VariableDeclarationOperation(VisitArray(internalOperation.Declarators), Visit(internalOperation.Initializer), VisitArray(internalOperation.IgnoredDimensions), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitArgument(IArgumentOperation operation, object? argument)
        {
            var internalOperation = (ArgumentOperation)operation;
            return new ArgumentOperation(internalOperation.ArgumentKind, internalOperation.Parameter, Visit(internalOperation.Value), internalOperation.InConversionConvertible, internalOperation.OutConversionConvertible, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitCatchClause(ICatchClauseOperation operation, object? argument)
        {
            var internalOperation = (CatchClauseOperation)operation;
            return new CatchClauseOperation(Visit(internalOperation.ExceptionDeclarationOrExpression), internalOperation.ExceptionType, internalOperation.Locals, Visit(internalOperation.Filter), Visit(internalOperation.Handler), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitSwitchCase(ISwitchCaseOperation operation, object? argument)
        {
            var internalOperation = (SwitchCaseOperation)operation;
            return new SwitchCaseOperation(VisitArray(internalOperation.Clauses), VisitArray(internalOperation.Body), internalOperation.Locals, Visit(internalOperation.Condition), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitDefaultCaseClause(IDefaultCaseClauseOperation operation, object? argument)
        {
            var internalOperation = (DefaultCaseClauseOperation)operation;
            return new DefaultCaseClauseOperation(internalOperation.Label, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitPatternCaseClause(IPatternCaseClauseOperation operation, object? argument)
        {
            var internalOperation = (PatternCaseClauseOperation)operation;
            return new PatternCaseClauseOperation(internalOperation.Label, Visit(internalOperation.Pattern), Visit(internalOperation.Guard), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitRangeCaseClause(IRangeCaseClauseOperation operation, object? argument)
        {
            var internalOperation = (RangeCaseClauseOperation)operation;
            return new RangeCaseClauseOperation(Visit(internalOperation.MinimumValue), Visit(internalOperation.MaximumValue), internalOperation.Label, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitRelationalCaseClause(IRelationalCaseClauseOperation operation, object? argument)
        {
            var internalOperation = (RelationalCaseClauseOperation)operation;
            return new RelationalCaseClauseOperation(Visit(internalOperation.Value), internalOperation.Relation, internalOperation.Label, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation, object? argument)
        {
            var internalOperation = (SingleValueCaseClauseOperation)operation;
            return new SingleValueCaseClauseOperation(Visit(internalOperation.Value), internalOperation.Label, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, object? argument)
        {
            var internalOperation = (InterpolatedStringTextOperation)operation;
            return new InterpolatedStringTextOperation(Visit(internalOperation.Text), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitInterpolation(IInterpolationOperation operation, object? argument)
        {
            var internalOperation = (InterpolationOperation)operation;
            return new InterpolationOperation(Visit(internalOperation.Expression), Visit(internalOperation.Alignment), Visit(internalOperation.FormatString), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitConstantPattern(IConstantPatternOperation operation, object? argument)
        {
            var internalOperation = (ConstantPatternOperation)operation;
            return new ConstantPatternOperation(Visit(internalOperation.Value), internalOperation.InputType, internalOperation.NarrowedType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitDeclarationPattern(IDeclarationPatternOperation operation, object? argument)
        {
            var internalOperation = (DeclarationPatternOperation)operation;
            return new DeclarationPatternOperation(internalOperation.MatchedType, internalOperation.MatchesNull, internalOperation.DeclaredSymbol, internalOperation.InputType, internalOperation.NarrowedType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitTupleBinaryOperator(ITupleBinaryOperation operation, object? argument)
        {
            var internalOperation = (TupleBinaryOperation)operation;
            return new TupleBinaryOperation(internalOperation.OperatorKind, Visit(internalOperation.LeftOperand), Visit(internalOperation.RightOperand), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitMethodBodyOperation(IMethodBodyOperation operation, object? argument)
        {
            var internalOperation = (MethodBodyOperation)operation;
            return new MethodBodyOperation(Visit(internalOperation.BlockBody), Visit(internalOperation.ExpressionBody), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitConstructorBodyOperation(IConstructorBodyOperation operation, object? argument)
        {
            var internalOperation = (ConstructorBodyOperation)operation;
            return new ConstructorBodyOperation(internalOperation.Locals, Visit(internalOperation.Initializer), Visit(internalOperation.BlockBody), Visit(internalOperation.ExpressionBody), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitDiscardOperation(IDiscardOperation operation, object? argument)
        {
            var internalOperation = (DiscardOperation)operation;
            return new DiscardOperation(internalOperation.DiscardSymbol, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, object? argument)
        {
            var internalOperation = (FlowCaptureReferenceOperation)operation;
            return new FlowCaptureReferenceOperation(internalOperation.Id, internalOperation.IsInitialization, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.OperationConstantValue, internalOperation.IsImplicit);
        }
        public override IOperation VisitCoalesceAssignment(ICoalesceAssignmentOperation operation, object? argument)
        {
            var internalOperation = (CoalesceAssignmentOperation)operation;
            return new CoalesceAssignmentOperation(Visit(internalOperation.Target), Visit(internalOperation.Value), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitRangeOperation(IRangeOperation operation, object? argument)
        {
            var internalOperation = (RangeOperation)operation;
            return new RangeOperation(Visit(internalOperation.LeftOperand), Visit(internalOperation.RightOperand), internalOperation.IsLifted, internalOperation.Method, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitReDim(IReDimOperation operation, object? argument)
        {
            var internalOperation = (ReDimOperation)operation;
            return new ReDimOperation(VisitArray(internalOperation.Clauses), internalOperation.Preserve, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitReDimClause(IReDimClauseOperation operation, object? argument)
        {
            var internalOperation = (ReDimClauseOperation)operation;
            return new ReDimClauseOperation(Visit(internalOperation.Operand), VisitArray(internalOperation.DimensionSizes), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitRecursivePattern(IRecursivePatternOperation operation, object? argument)
        {
            var internalOperation = (RecursivePatternOperation)operation;
            return new RecursivePatternOperation(internalOperation.MatchedType, internalOperation.DeconstructSymbol, VisitArray(internalOperation.DeconstructionSubpatterns), VisitArray(internalOperation.PropertySubpatterns), internalOperation.DeclaredSymbol, internalOperation.InputType, internalOperation.NarrowedType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitDiscardPattern(IDiscardPatternOperation operation, object? argument)
        {
            var internalOperation = (DiscardPatternOperation)operation;
            return new DiscardPatternOperation(internalOperation.InputType, internalOperation.NarrowedType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitSwitchExpression(ISwitchExpressionOperation operation, object? argument)
        {
            var internalOperation = (SwitchExpressionOperation)operation;
            return new SwitchExpressionOperation(Visit(internalOperation.Value), VisitArray(internalOperation.Arms), internalOperation.IsExhaustive, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitSwitchExpressionArm(ISwitchExpressionArmOperation operation, object? argument)
        {
            var internalOperation = (SwitchExpressionArmOperation)operation;
            return new SwitchExpressionArmOperation(Visit(internalOperation.Pattern), Visit(internalOperation.Guard), Visit(internalOperation.Value), internalOperation.Locals, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitPropertySubpattern(IPropertySubpatternOperation operation, object? argument)
        {
            var internalOperation = (PropertySubpatternOperation)operation;
            return new PropertySubpatternOperation(Visit(internalOperation.Member), Visit(internalOperation.Pattern), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        internal override IOperation VisitAggregateQuery(IAggregateQueryOperation operation, object? argument)
        {
            var internalOperation = (AggregateQueryOperation)operation;
            return new AggregateQueryOperation(Visit(internalOperation.Group), Visit(internalOperation.Aggregation), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        internal override IOperation VisitFixed(IFixedOperation operation, object? argument)
        {
            var internalOperation = (FixedOperation)operation;
            return new FixedOperation(internalOperation.Locals, Visit(internalOperation.Variables), Visit(internalOperation.Body), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        internal override IOperation VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation, object? argument)
        {
            var internalOperation = (NoPiaObjectCreationOperation)operation;
            return new NoPiaObjectCreationOperation(Visit(internalOperation.Initializer), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        internal override IOperation VisitPlaceholder(IPlaceholderOperation operation, object? argument)
        {
            var internalOperation = (PlaceholderOperation)operation;
            return new PlaceholderOperation(internalOperation.PlaceholderKind, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        internal override IOperation VisitWithStatement(IWithStatementOperation operation, object? argument)
        {
            var internalOperation = (WithStatementOperation)operation;
            return new WithStatementOperation(Visit(internalOperation.Body), Visit(internalOperation.Value), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitUsingDeclaration(IUsingDeclarationOperation operation, object? argument)
        {
            var internalOperation = (UsingDeclarationOperation)operation;
            return new UsingDeclarationOperation(Visit(internalOperation.DeclarationGroup), internalOperation.IsAsynchronous, internalOperation.DisposeInfo, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitNegatedPattern(INegatedPatternOperation operation, object? argument)
        {
            var internalOperation = (NegatedPatternOperation)operation;
            return new NegatedPatternOperation(Visit(internalOperation.Pattern), internalOperation.InputType, internalOperation.NarrowedType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitBinaryPattern(IBinaryPatternOperation operation, object? argument)
        {
            var internalOperation = (BinaryPatternOperation)operation;
            return new BinaryPatternOperation(internalOperation.OperatorKind, Visit(internalOperation.LeftPattern), Visit(internalOperation.RightPattern), internalOperation.InputType, internalOperation.NarrowedType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitTypePattern(ITypePatternOperation operation, object? argument)
        {
            var internalOperation = (TypePatternOperation)operation;
            return new TypePatternOperation(internalOperation.MatchedType, internalOperation.InputType, internalOperation.NarrowedType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitRelationalPattern(IRelationalPatternOperation operation, object? argument)
        {
            var internalOperation = (RelationalPatternOperation)operation;
            return new RelationalPatternOperation(internalOperation.OperatorKind, Visit(internalOperation.Value), internalOperation.InputType, internalOperation.NarrowedType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitWith(IWithOperation operation, object? argument)
        {
            var internalOperation = (WithOperation)operation;
            return new WithOperation(Visit(internalOperation.Operand), internalOperation.CloneMethod, Visit(internalOperation.Initializer), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitInterpolatedStringHandlerCreation(IInterpolatedStringHandlerCreationOperation operation, object? argument)
        {
            var internalOperation = (InterpolatedStringHandlerCreationOperation)operation;
            return new InterpolatedStringHandlerCreationOperation(Visit(internalOperation.HandlerCreation), internalOperation.HandlerCreationHasSuccessParameter, internalOperation.HandlerAppendCallsReturnBool, Visit(internalOperation.Content), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitInterpolatedStringAddition(IInterpolatedStringAdditionOperation operation, object? argument)
        {
            var internalOperation = (InterpolatedStringAdditionOperation)operation;
            return new InterpolatedStringAdditionOperation(Visit(internalOperation.Left), Visit(internalOperation.Right), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitInterpolatedStringAppend(IInterpolatedStringAppendOperation operation, object? argument)
        {
            var internalOperation = (InterpolatedStringAppendOperation)operation;
            return new InterpolatedStringAppendOperation(Visit(internalOperation.AppendCall), internalOperation.Kind, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitInterpolatedStringHandlerArgumentPlaceholder(IInterpolatedStringHandlerArgumentPlaceholderOperation operation, object? argument)
        {
            var internalOperation = (InterpolatedStringHandlerArgumentPlaceholderOperation)operation;
            return new InterpolatedStringHandlerArgumentPlaceholderOperation(internalOperation.ArgumentIndex, internalOperation.PlaceholderKind, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitFunctionPointerInvocation(IFunctionPointerInvocationOperation operation, object? argument)
        {
            var internalOperation = (FunctionPointerInvocationOperation)operation;
            return new FunctionPointerInvocationOperation(Visit(internalOperation.Target), VisitArray(internalOperation.Arguments), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitListPattern(IListPatternOperation operation, object? argument)
        {
            var internalOperation = (ListPatternOperation)operation;
            return new ListPatternOperation(internalOperation.LengthSymbol, internalOperation.IndexerSymbol, VisitArray(internalOperation.Patterns), internalOperation.DeclaredSymbol, internalOperation.InputType, internalOperation.NarrowedType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitSlicePattern(ISlicePatternOperation operation, object? argument)
        {
            var internalOperation = (SlicePatternOperation)operation;
            return new SlicePatternOperation(internalOperation.SliceSymbol, Visit(internalOperation.Pattern), internalOperation.InputType, internalOperation.NarrowedType, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitImplicitIndexerReference(IImplicitIndexerReferenceOperation operation, object? argument)
        {
            var internalOperation = (ImplicitIndexerReferenceOperation)operation;
            return new ImplicitIndexerReferenceOperation(Visit(internalOperation.Instance), Visit(internalOperation.Argument), internalOperation.LengthSymbol, internalOperation.IndexerSymbol, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitUtf8String(IUtf8StringOperation operation, object? argument)
        {
            var internalOperation = (Utf8StringOperation)operation;
            return new Utf8StringOperation(internalOperation.Value, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitAttribute(IAttributeOperation operation, object? argument)
        {
            var internalOperation = (AttributeOperation)operation;
            return new AttributeOperation(Visit(internalOperation.Operation), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
        public override IOperation VisitInlineArrayAccess(IInlineArrayAccessOperation operation, object? argument)
        {
            var internalOperation = (InlineArrayAccessOperation)operation;
            return new InlineArrayAccessOperation(Visit(internalOperation.Instance), Visit(internalOperation.Argument), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitCollectionExpression(ICollectionExpressionOperation operation, object? argument)
        {
            var internalOperation = (CollectionExpressionOperation)operation;
            return new CollectionExpressionOperation(internalOperation.ConstructMethod, VisitArray(internalOperation.Elements), internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.Type, internalOperation.IsImplicit);
        }
        public override IOperation VisitSpread(ISpreadOperation operation, object? argument)
        {
            var internalOperation = (SpreadOperation)operation;
            return new SpreadOperation(Visit(internalOperation.Operand), internalOperation.ElementType, internalOperation.ElementConversionConvertible, internalOperation.OwningSemanticModel, internalOperation.Syntax, internalOperation.IsImplicit);
        }
    }
    #endregion
    
    #region Visitors
    public abstract partial class OperationVisitor
    {
        public virtual void Visit(IOperation? operation) => operation?.Accept(this);
        public virtual void DefaultVisit(IOperation operation) { /* no-op */ }
        internal virtual void VisitNoneOperation(IOperation operation) { /* no-op */ }
        public virtual void VisitInvalid(IInvalidOperation operation) => DefaultVisit(operation);
        public virtual void VisitBlock(IBlockOperation operation) => DefaultVisit(operation);
        public virtual void VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation) => DefaultVisit(operation);
        public virtual void VisitSwitch(ISwitchOperation operation) => DefaultVisit(operation);
        public virtual void VisitForEachLoop(IForEachLoopOperation operation) => DefaultVisit(operation);
        public virtual void VisitForLoop(IForLoopOperation operation) => DefaultVisit(operation);
        public virtual void VisitForToLoop(IForToLoopOperation operation) => DefaultVisit(operation);
        public virtual void VisitWhileLoop(IWhileLoopOperation operation) => DefaultVisit(operation);
        public virtual void VisitLabeled(ILabeledOperation operation) => DefaultVisit(operation);
        public virtual void VisitBranch(IBranchOperation operation) => DefaultVisit(operation);
        public virtual void VisitEmpty(IEmptyOperation operation) => DefaultVisit(operation);
        public virtual void VisitReturn(IReturnOperation operation) => DefaultVisit(operation);
        public virtual void VisitLock(ILockOperation operation) => DefaultVisit(operation);
        public virtual void VisitTry(ITryOperation operation) => DefaultVisit(operation);
        public virtual void VisitUsing(IUsingOperation operation) => DefaultVisit(operation);
        public virtual void VisitExpressionStatement(IExpressionStatementOperation operation) => DefaultVisit(operation);
        public virtual void VisitLocalFunction(ILocalFunctionOperation operation) => DefaultVisit(operation);
        public virtual void VisitStop(IStopOperation operation) => DefaultVisit(operation);
        public virtual void VisitEnd(IEndOperation operation) => DefaultVisit(operation);
        public virtual void VisitRaiseEvent(IRaiseEventOperation operation) => DefaultVisit(operation);
        public virtual void VisitLiteral(ILiteralOperation operation) => DefaultVisit(operation);
        public virtual void VisitConversion(IConversionOperation operation) => DefaultVisit(operation);
        public virtual void VisitInvocation(IInvocationOperation operation) => DefaultVisit(operation);
        public virtual void VisitArrayElementReference(IArrayElementReferenceOperation operation) => DefaultVisit(operation);
        public virtual void VisitLocalReference(ILocalReferenceOperation operation) => DefaultVisit(operation);
        public virtual void VisitParameterReference(IParameterReferenceOperation operation) => DefaultVisit(operation);
        public virtual void VisitFieldReference(IFieldReferenceOperation operation) => DefaultVisit(operation);
        public virtual void VisitMethodReference(IMethodReferenceOperation operation) => DefaultVisit(operation);
        public virtual void VisitPropertyReference(IPropertyReferenceOperation operation) => DefaultVisit(operation);
        public virtual void VisitEventReference(IEventReferenceOperation operation) => DefaultVisit(operation);
        public virtual void VisitUnaryOperator(IUnaryOperation operation) => DefaultVisit(operation);
        public virtual void VisitBinaryOperator(IBinaryOperation operation) => DefaultVisit(operation);
        public virtual void VisitConditional(IConditionalOperation operation) => DefaultVisit(operation);
        public virtual void VisitCoalesce(ICoalesceOperation operation) => DefaultVisit(operation);
        public virtual void VisitAnonymousFunction(IAnonymousFunctionOperation operation) => DefaultVisit(operation);
        public virtual void VisitObjectCreation(IObjectCreationOperation operation) => DefaultVisit(operation);
        public virtual void VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation) => DefaultVisit(operation);
        public virtual void VisitArrayCreation(IArrayCreationOperation operation) => DefaultVisit(operation);
        public virtual void VisitInstanceReference(IInstanceReferenceOperation operation) => DefaultVisit(operation);
        public virtual void VisitIsType(IIsTypeOperation operation) => DefaultVisit(operation);
        public virtual void VisitAwait(IAwaitOperation operation) => DefaultVisit(operation);
        public virtual void VisitSimpleAssignment(ISimpleAssignmentOperation operation) => DefaultVisit(operation);
        public virtual void VisitCompoundAssignment(ICompoundAssignmentOperation operation) => DefaultVisit(operation);
        public virtual void VisitParenthesized(IParenthesizedOperation operation) => DefaultVisit(operation);
        public virtual void VisitEventAssignment(IEventAssignmentOperation operation) => DefaultVisit(operation);
        public virtual void VisitConditionalAccess(IConditionalAccessOperation operation) => DefaultVisit(operation);
        public virtual void VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation) => DefaultVisit(operation);
        public virtual void VisitInterpolatedString(IInterpolatedStringOperation operation) => DefaultVisit(operation);
        public virtual void VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation) => DefaultVisit(operation);
        public virtual void VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation) => DefaultVisit(operation);
        public virtual void VisitMemberInitializer(IMemberInitializerOperation operation) => DefaultVisit(operation);
        [Obsolete("ICollectionElementInitializerOperation has been replaced with " + nameof(IInvocationOperation) + " and " + nameof(IDynamicInvocationOperation), error: true)]
        public virtual void VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation) => DefaultVisit(operation);
        public virtual void VisitNameOf(INameOfOperation operation) => DefaultVisit(operation);
        public virtual void VisitTuple(ITupleOperation operation) => DefaultVisit(operation);
        public virtual void VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation) => DefaultVisit(operation);
        public virtual void VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation) => DefaultVisit(operation);
        public virtual void VisitDynamicInvocation(IDynamicInvocationOperation operation) => DefaultVisit(operation);
        public virtual void VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation) => DefaultVisit(operation);
        public virtual void VisitTranslatedQuery(ITranslatedQueryOperation operation) => DefaultVisit(operation);
        public virtual void VisitDelegateCreation(IDelegateCreationOperation operation) => DefaultVisit(operation);
        public virtual void VisitDefaultValue(IDefaultValueOperation operation) => DefaultVisit(operation);
        public virtual void VisitTypeOf(ITypeOfOperation operation) => DefaultVisit(operation);
        public virtual void VisitSizeOf(ISizeOfOperation operation) => DefaultVisit(operation);
        public virtual void VisitAddressOf(IAddressOfOperation operation) => DefaultVisit(operation);
        public virtual void VisitIsPattern(IIsPatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation) => DefaultVisit(operation);
        public virtual void VisitThrow(IThrowOperation operation) => DefaultVisit(operation);
        public virtual void VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation) => DefaultVisit(operation);
        public virtual void VisitDeclarationExpression(IDeclarationExpressionOperation operation) => DefaultVisit(operation);
        public virtual void VisitOmittedArgument(IOmittedArgumentOperation operation) => DefaultVisit(operation);
        public virtual void VisitFieldInitializer(IFieldInitializerOperation operation) => DefaultVisit(operation);
        public virtual void VisitVariableInitializer(IVariableInitializerOperation operation) => DefaultVisit(operation);
        public virtual void VisitPropertyInitializer(IPropertyInitializerOperation operation) => DefaultVisit(operation);
        public virtual void VisitParameterInitializer(IParameterInitializerOperation operation) => DefaultVisit(operation);
        public virtual void VisitArrayInitializer(IArrayInitializerOperation operation) => DefaultVisit(operation);
        public virtual void VisitVariableDeclarator(IVariableDeclaratorOperation operation) => DefaultVisit(operation);
        public virtual void VisitVariableDeclaration(IVariableDeclarationOperation operation) => DefaultVisit(operation);
        public virtual void VisitArgument(IArgumentOperation operation) => DefaultVisit(operation);
        public virtual void VisitCatchClause(ICatchClauseOperation operation) => DefaultVisit(operation);
        public virtual void VisitSwitchCase(ISwitchCaseOperation operation) => DefaultVisit(operation);
        public virtual void VisitDefaultCaseClause(IDefaultCaseClauseOperation operation) => DefaultVisit(operation);
        public virtual void VisitPatternCaseClause(IPatternCaseClauseOperation operation) => DefaultVisit(operation);
        public virtual void VisitRangeCaseClause(IRangeCaseClauseOperation operation) => DefaultVisit(operation);
        public virtual void VisitRelationalCaseClause(IRelationalCaseClauseOperation operation) => DefaultVisit(operation);
        public virtual void VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation) => DefaultVisit(operation);
        public virtual void VisitInterpolatedStringText(IInterpolatedStringTextOperation operation) => DefaultVisit(operation);
        public virtual void VisitInterpolation(IInterpolationOperation operation) => DefaultVisit(operation);
        public virtual void VisitConstantPattern(IConstantPatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitDeclarationPattern(IDeclarationPatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitTupleBinaryOperator(ITupleBinaryOperation operation) => DefaultVisit(operation);
        public virtual void VisitMethodBodyOperation(IMethodBodyOperation operation) => DefaultVisit(operation);
        public virtual void VisitConstructorBodyOperation(IConstructorBodyOperation operation) => DefaultVisit(operation);
        public virtual void VisitDiscardOperation(IDiscardOperation operation) => DefaultVisit(operation);
        public virtual void VisitFlowCapture(IFlowCaptureOperation operation) => DefaultVisit(operation);
        public virtual void VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation) => DefaultVisit(operation);
        public virtual void VisitIsNull(IIsNullOperation operation) => DefaultVisit(operation);
        public virtual void VisitCaughtException(ICaughtExceptionOperation operation) => DefaultVisit(operation);
        public virtual void VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation) => DefaultVisit(operation);
        public virtual void VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation) => DefaultVisit(operation);
        public virtual void VisitCoalesceAssignment(ICoalesceAssignmentOperation operation) => DefaultVisit(operation);
        public virtual void VisitRangeOperation(IRangeOperation operation) => DefaultVisit(operation);
        public virtual void VisitReDim(IReDimOperation operation) => DefaultVisit(operation);
        public virtual void VisitReDimClause(IReDimClauseOperation operation) => DefaultVisit(operation);
        public virtual void VisitRecursivePattern(IRecursivePatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitDiscardPattern(IDiscardPatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitSwitchExpression(ISwitchExpressionOperation operation) => DefaultVisit(operation);
        public virtual void VisitSwitchExpressionArm(ISwitchExpressionArmOperation operation) => DefaultVisit(operation);
        public virtual void VisitPropertySubpattern(IPropertySubpatternOperation operation) => DefaultVisit(operation);
        internal virtual void VisitAggregateQuery(IAggregateQueryOperation operation) => DefaultVisit(operation);
        internal virtual void VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation) => DefaultVisit(operation);
        internal virtual void VisitPlaceholder(IPlaceholderOperation operation) => DefaultVisit(operation);
        internal virtual void VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation) => DefaultVisit(operation);
        internal virtual void VisitWithStatement(IWithStatementOperation operation) => DefaultVisit(operation);
        public virtual void VisitUsingDeclaration(IUsingDeclarationOperation operation) => DefaultVisit(operation);
        public virtual void VisitNegatedPattern(INegatedPatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitBinaryPattern(IBinaryPatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitTypePattern(ITypePatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitRelationalPattern(IRelationalPatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitWith(IWithOperation operation) => DefaultVisit(operation);
        public virtual void VisitInterpolatedStringHandlerCreation(IInterpolatedStringHandlerCreationOperation operation) => DefaultVisit(operation);
        public virtual void VisitInterpolatedStringAddition(IInterpolatedStringAdditionOperation operation) => DefaultVisit(operation);
        public virtual void VisitInterpolatedStringAppend(IInterpolatedStringAppendOperation operation) => DefaultVisit(operation);
        public virtual void VisitInterpolatedStringHandlerArgumentPlaceholder(IInterpolatedStringHandlerArgumentPlaceholderOperation operation) => DefaultVisit(operation);
        public virtual void VisitFunctionPointerInvocation(IFunctionPointerInvocationOperation operation) => DefaultVisit(operation);
        public virtual void VisitListPattern(IListPatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitSlicePattern(ISlicePatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitImplicitIndexerReference(IImplicitIndexerReferenceOperation operation) => DefaultVisit(operation);
        public virtual void VisitUtf8String(IUtf8StringOperation operation) => DefaultVisit(operation);
        public virtual void VisitAttribute(IAttributeOperation operation) => DefaultVisit(operation);
        public virtual void VisitInlineArrayAccess(IInlineArrayAccessOperation operation) => DefaultVisit(operation);
        public virtual void VisitCollectionExpression(ICollectionExpressionOperation operation) => DefaultVisit(operation);
        public virtual void VisitSpread(ISpreadOperation operation) => DefaultVisit(operation);
    }
    public abstract partial class OperationVisitor<TArgument, TResult>
    {
        public virtual TResult? Visit(IOperation? operation, TArgument argument) => operation is null ? default(TResult) : operation.Accept(this, argument);
        public virtual TResult? DefaultVisit(IOperation operation, TArgument argument) => default(TResult);
        internal virtual TResult? VisitNoneOperation(IOperation operation, TArgument argument) => default(TResult);
        public virtual TResult? VisitInvalid(IInvalidOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitBlock(IBlockOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitSwitch(ISwitchOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitForEachLoop(IForEachLoopOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitForLoop(IForLoopOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitForToLoop(IForToLoopOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitWhileLoop(IWhileLoopOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitLabeled(ILabeledOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitBranch(IBranchOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitEmpty(IEmptyOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitReturn(IReturnOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitLock(ILockOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitTry(ITryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitUsing(IUsingOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitExpressionStatement(IExpressionStatementOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitLocalFunction(ILocalFunctionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitStop(IStopOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitEnd(IEndOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitRaiseEvent(IRaiseEventOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitLiteral(ILiteralOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitConversion(IConversionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitInvocation(IInvocationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitArrayElementReference(IArrayElementReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitLocalReference(ILocalReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitParameterReference(IParameterReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitFieldReference(IFieldReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitMethodReference(IMethodReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitPropertyReference(IPropertyReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitEventReference(IEventReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitUnaryOperator(IUnaryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitBinaryOperator(IBinaryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitConditional(IConditionalOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitCoalesce(ICoalesceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitAnonymousFunction(IAnonymousFunctionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitObjectCreation(IObjectCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitArrayCreation(IArrayCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitInstanceReference(IInstanceReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitIsType(IIsTypeOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitAwait(IAwaitOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitSimpleAssignment(ISimpleAssignmentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitCompoundAssignment(ICompoundAssignmentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitParenthesized(IParenthesizedOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitEventAssignment(IEventAssignmentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitConditionalAccess(IConditionalAccessOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitInterpolatedString(IInterpolatedStringOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitMemberInitializer(IMemberInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        [Obsolete("ICollectionElementInitializerOperation has been replaced with " + nameof(IInvocationOperation) + " and " + nameof(IDynamicInvocationOperation), error: true)]
        public virtual TResult? VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitNameOf(INameOfOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitTuple(ITupleOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitDynamicInvocation(IDynamicInvocationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitTranslatedQuery(ITranslatedQueryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitDelegateCreation(IDelegateCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitDefaultValue(IDefaultValueOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitTypeOf(ITypeOfOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitSizeOf(ISizeOfOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitAddressOf(IAddressOfOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitIsPattern(IIsPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitThrow(IThrowOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitDeclarationExpression(IDeclarationExpressionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitOmittedArgument(IOmittedArgumentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitFieldInitializer(IFieldInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitVariableInitializer(IVariableInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitPropertyInitializer(IPropertyInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitParameterInitializer(IParameterInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitArrayInitializer(IArrayInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitVariableDeclarator(IVariableDeclaratorOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitVariableDeclaration(IVariableDeclarationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitArgument(IArgumentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitCatchClause(ICatchClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitSwitchCase(ISwitchCaseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitDefaultCaseClause(IDefaultCaseClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitPatternCaseClause(IPatternCaseClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitRangeCaseClause(IRangeCaseClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitRelationalCaseClause(IRelationalCaseClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitInterpolation(IInterpolationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitConstantPattern(IConstantPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitDeclarationPattern(IDeclarationPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitTupleBinaryOperator(ITupleBinaryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitMethodBodyOperation(IMethodBodyOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitConstructorBodyOperation(IConstructorBodyOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitDiscardOperation(IDiscardOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitFlowCapture(IFlowCaptureOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitIsNull(IIsNullOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitCaughtException(ICaughtExceptionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitCoalesceAssignment(ICoalesceAssignmentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitRangeOperation(IRangeOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitReDim(IReDimOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitReDimClause(IReDimClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitRecursivePattern(IRecursivePatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitDiscardPattern(IDiscardPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitSwitchExpression(ISwitchExpressionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitSwitchExpressionArm(ISwitchExpressionArmOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitPropertySubpattern(IPropertySubpatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult? VisitAggregateQuery(IAggregateQueryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult? VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult? VisitPlaceholder(IPlaceholderOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult? VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult? VisitWithStatement(IWithStatementOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitUsingDeclaration(IUsingDeclarationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitNegatedPattern(INegatedPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitBinaryPattern(IBinaryPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitTypePattern(ITypePatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitRelationalPattern(IRelationalPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitWith(IWithOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitInterpolatedStringHandlerCreation(IInterpolatedStringHandlerCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitInterpolatedStringAddition(IInterpolatedStringAdditionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitInterpolatedStringAppend(IInterpolatedStringAppendOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitInterpolatedStringHandlerArgumentPlaceholder(IInterpolatedStringHandlerArgumentPlaceholderOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitFunctionPointerInvocation(IFunctionPointerInvocationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitListPattern(IListPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitSlicePattern(ISlicePatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitImplicitIndexerReference(IImplicitIndexerReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitUtf8String(IUtf8StringOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitAttribute(IAttributeOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitInlineArrayAccess(IInlineArrayAccessOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitCollectionExpression(ICollectionExpressionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult? VisitSpread(ISpreadOperation operation, TArgument argument) => DefaultVisit(operation, argument);
    }
    #endregion
}
