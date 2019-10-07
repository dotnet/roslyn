// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
// < auto-generated />
using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Microsoft.CodeAnalysis.Operations
{
    #region Interfaces
    /// <summary>
    /// Represents an invalid operation with one or more child operations.
    /// <para>
    /// Current usage:
    ///  (1) C# invalid expression or invalid statement.
    ///  (2) VB invalid expression or invalid statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInvalidOperation : IOperation
    {
    }
    /// <summary>
    /// Represents a block containing a sequence of operations and local declarations.
    /// <para>
    /// Current usage:
    ///  (1) C# "{ ... }" block statement.
    ///  (2) VB implicit block statement for method bodies and other block scoped statements.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    /// </summary>
    /// <para>
    /// Current Usage:
    ///   (1) C# local declaration statement
    ///   (2) C# fixed statement
    ///   (3) C# using statement
    ///   (4) VB Dim statement
    ///   (5) VB Using statement
    ///   (6) C# using declaration statement
    /// </para>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///   (1) C# 'while', 'for', 'foreach' and 'do' loop statements
    ///   (2) VB 'While', 'ForTo', 'ForEach', 'Do While' and 'Do Until' loop statements
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# 'foreach' loop statement
    ///  (2) VB 'For Each' loop statement
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# 'for' loop statement
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IOperation Condition { get; }
        /// <summary>
        /// List of operations to execute at the bottom of the loop. For C#, this comes from the third clause of the for statement.
        /// </summary>
        ImmutableArray<IOperation> AtLoopBottom { get; }
    }
    /// <summary>
    /// Represents a for to loop with loop control variable and initial, limit and step values for the control variable.
    /// <para>
    /// Current usage:
    ///  (1) VB 'For ... To ... Step' loop statement
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        /// <code>true</code> if arithmetic operations behind this loop are 'checked'.
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
    ///  (1) C# 'while' and 'do while' loop statements.
    ///  (2) VB 'While', 'Do While' and 'Do Until' loop statements.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IWhileLoopOperation : ILoopOperation
    {
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        IOperation Condition { get; }
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
        IOperation IgnoredCondition { get; }
    }
    /// <summary>
    /// Represents an operation with a label.
    /// <para>
    /// Current usage:
    ///  (1) C# labeled statement.
    ///  (2) VB label statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IOperation Operation { get; }
    }
    /// <summary>
    /// Represents a branch operation.
    /// <para>
    /// Current usage:
    ///  (1) C# goto, break, or continue statement.
    ///  (2) VB GoTo, Exit ***, or Continue *** statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# empty statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IEmptyOperation : IOperation
    {
    }
    /// <summary>
    /// Represents a return from the method with an optional return value.
    /// <para>
    /// Current usage:
    ///  (1) C# return statement and yield statement.
    ///  (2) VB Return statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IReturnOperation : IOperation
    {
        /// <summary>
        /// Value to be returned.
        /// </summary>
        IOperation ReturnedValue { get; }
    }
    /// <summary>
    /// Represents a <see cref="Body" /> of operations that are executed while holding a lock onto the <see cref="LockedValue" />.
    /// <para>
    /// Current usage:
    ///  (1) C# lock statement.
    ///  (2) VB SyncLock statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# try statement.
    ///  (2) VB Try statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IBlockOperation Finally { get; }
        /// <summary>
        /// Exit label for the try. This will always be null for C#.
        /// </summary>
        ILabelSymbol ExitLabel { get; }
    }
    /// <summary>
    /// Represents a <see cref="Body" /> of operations that are executed while using disposable <see cref="Resources" />.
    /// <para>
    /// Current usage:
    ///  (1) C# using statement.
    ///  (2) VB Using statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# expression statement.
    ///  (2) VB expression statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# local function statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IBlockOperation Body { get; }
        /// <summary>
        /// An extra body for the local function, if both a block body and expression body are specified in source.
        /// </summary>
        /// <remarks>
        /// This is only ever non-null in error situations.
        /// </remarks>
        IBlockOperation IgnoredBody { get; }
    }
    /// <summary>
    /// Represents an operation to stop or suspend execution of code.
    /// <para>
    /// Current usage:
    ///  (1) VB Stop statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IStopOperation : IOperation
    {
    }
    /// <summary>
    /// Represents an operation that stops the execution of code abruptly.
    /// <para>
    /// Current usage:
    ///  (1) VB End Statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IEndOperation : IOperation
    {
    }
    /// <summary>
    /// Represents an operation for raising an event.
    /// <para>
    /// Current usage:
    ///  (1) VB raise event statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# literal expression.
    ///  (2) VB literal expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ILiteralOperation : IOperation
    {
    }
    /// <summary>
    /// Represents a type conversion.
    /// <para>
    /// Current usage:
    ///  (1) C# conversion expression.
    ///  (2) VB conversion expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IMethodSymbol OperatorMethod { get; }
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
    ///  (1) C# method invocation expression.
    ///  (2) C# collection element initializer.
    ///      For example, in the following collection initializer: <code>new C() { 1, 2, 3 }</code>, we will have
    ///      3 <see cref="IInvocationOperation" /> nodes, each of which will be a call to the corresponding Add method
    ///      with either 1, 2, 3 as the argument.
    ///  (3) VB method invocation expression.
    ///  (4) VB collection element initializer.
    ///      Similar to the C# example, <code>New C() From {1, 2, 3}</code> will have 3 <see cref="IInvocationOperation" />
    ///      nodes with 1, 2, and 3 as their arguments, respectively.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInvocationOperation : IOperation
    {
        /// <summary>
        /// Method to be invoked.
        /// </summary>
        IMethodSymbol TargetMethod { get; }
        /// <summary>
        /// 'This' or 'Me' instance to be supplied to the method, or null if the method is static.
        /// </summary>
        IOperation Instance { get; }
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
    ///  (1) C# array element reference expression.
    ///  (2) VB array element reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# local reference expression.
    ///  (2) VB local reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# parameter reference expression.
    ///  (2) VB parameter reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# member reference expression.
    ///  (2) VB member reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IMemberReferenceOperation : IOperation
    {
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        IOperation Instance { get; }
        /// <summary>
        /// Referenced member.
        /// </summary>
        ISymbol Member { get; }
    }
    /// <summary>
    /// Represents a reference to a field.
    /// <para>
    /// Current usage:
    ///  (1) C# field reference expression.
    ///  (2) VB field reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# method reference expression.
    ///  (2) VB method reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# property reference expression.
    ///  (2) VB property reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# event reference expression.
    ///  (2) VB event reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# unary operation expression.
    ///  (2) VB unary operation expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IMethodSymbol OperatorMethod { get; }
    }
    /// <summary>
    /// Represents an operation with two operands and a binary operator that produces a result with a non-null type.
    /// <para>
    /// Current usage:
    ///  (1) C# binary operator expression.
    ///  (2) VB binary operator expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IMethodSymbol OperatorMethod { get; }
    }
    /// <summary>
    /// Represents a conditional operation with:
    /// (1) <see cref="Condition" /> to be tested,
    /// (2) <see cref="WhenTrue" /> operation to be executed when <see cref="Condition" /> is true and
    /// (3) <see cref="WhenFalse" /> operation to be executed when the <see cref="Condition" /> is false.
    /// <para>
    /// Current usage:
    ///  (1) C# ternary expression "a ? b : c" and if statement.
    ///  (2) VB ternary expression "If(a, b, c)" and If Else statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IOperation WhenFalse { get; }
        /// <summary>
        /// Is result a managed reference
        /// </summary>
        bool IsRef { get; }
    }
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
    /// <summary>
    /// Represents an anonymous function operation.
    /// <para>
    /// Current usage:
    ///  (1) C# lambda expression.
    ///  (2) VB anonymous delegate expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# new expression.
    ///  (2) VB New expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IObjectCreationOperation : IOperation
    {
        /// <summary>
        /// Constructor to be invoked on the created instance.
        /// </summary>
        IMethodSymbol Constructor { get; }
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        IObjectOrCollectionInitializerOperation Initializer { get; }
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
    ///  (1) C# type parameter object creation expression.
    ///  (2) VB type parameter object creation expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ITypeParameterObjectCreationOperation : IOperation
    {
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        IObjectOrCollectionInitializerOperation Initializer { get; }
    }
    /// <summary>
    /// Represents the creation of an array instance.
    /// <para>
    /// Current usage:
    ///  (1) C# array creation expression.
    ///  (2) VB array creation expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IArrayInitializerOperation Initializer { get; }
    }
    /// <summary>
    /// Represents an implicit/explicit reference to an instance.
    /// <para>
    /// Current usage:
    ///  (1) C# this or base expression.
    ///  (2) VB Me, MyClass, or MyBase expression.
    ///  (3) C# object or collection initializers.
    ///  (4) VB With statements, object or collection initializers.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# "is" operator expression.
    ///  (2) VB "TypeOf" and "TypeOf IsNot" expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# await expression.
    ///  (2) VB await expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# simple, compound and deconstruction assignment expressions.
    ///  (2) VB simple and compound assignment expressions.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# simple assignment expression.
    ///  (2) VB simple assignment expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# compound assignment expression.
    ///  (2) VB compound assignment expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IMethodSymbol OperatorMethod { get; }
    }
    /// <summary>
    /// Represents a parenthesized operation.
    /// <para>
    /// Current usage:
    ///  (1) VB parenthesized expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# event assignment expression.
    ///  (2) VB Add/Remove handler statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# conditional access expression (? or ?. operator).
    ///  (2) VB conditional access expression (? or ?. operator).
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# conditional access instance expression.
    ///  (2) VB conditional access instance expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# "new { ... }" expression
    ///  (2) VB "New With { ... }" expression
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# object or collection initializer expression.
    ///  (2) VB object or collection initializer expression.
    /// For example, object initializer "{ X = x }" within object creation "new Class() { X = x }" and
    /// collection initializer "{ x, y, 3 }" within collection creation "new MyList() { x, y, 3 }".
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# nested member initializer expression.
    ///   For example, given an object creation with initializer "new Class() { X = x, Y = { x, y, 3 }, Z = { X = z } }",
    ///   member initializers for Y and Z, i.e. "Y = { x, y, 3 }", and "Z = { X = z }" are nested member initializers represented by this operation.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# nameof expression.
    ///  (2) VB NameOf expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# tuple expression.
    ///  (2) VB tuple expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        ITypeSymbol NaturalType { get; }
    }
    /// <summary>
    /// Represents an object creation with a dynamically bound constructor.
    /// <para>
    /// Current usage:
    ///  (1) C# "new" expression with dynamic argument(s).
    ///  (2) VB late bound "New" expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDynamicObjectCreationOperation : IOperation
    {
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        IObjectOrCollectionInitializerOperation Initializer { get; }
        /// <summary>
        /// Dynamically bound arguments, excluding the instance argument.
        /// </summary>
        ImmutableArray<IOperation> Arguments { get; }
    }
    /// <summary>
    /// Represents a reference to a member of a class, struct, or module that is dynamically bound.
    /// <para>
    /// Current usage:
    ///  (1) C# dynamic member reference expression.
    ///  (2) VB late bound member reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDynamicMemberReferenceOperation : IOperation
    {
        /// <summary>
        /// Instance receiver. In VB, this can be null.
        /// </summary>
        IOperation Instance { get; }
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
        ITypeSymbol ContainingType { get; }
    }
    /// <summary>
    /// Represents a invocation that is dynamically bound.
    /// <para>
    /// Current usage:
    ///  (1) C# dynamic invocation expression.
    ///  (2) C# dynamic collection element initializer.
    ///      For example, in the following collection initializer: <code>new C() { do1, do2, do3 }</code> where
    ///      the doX objects are of type dynamic, we'll have 3 <see cref="IDynamicInvocationOperation" /> with do1, do2, and
    ///      do3 as their arguments.
    ///  (3) VB late bound invocation expression.
    ///  (4) VB dynamic collection element initializer.
    ///      Similar to the C# example, <code>New C() From {do1, do2, do3}</code> will generate 3 <see cref="IDynamicInvocationOperation" />
    ///      nodes with do1, do2, and do3 as their arguments, respectively.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# dynamic indexer access expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# query expression.
    ///  (2) VB query expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# delegate creation expression.
    ///  (2) VB delegate creation expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# default value expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDefaultValueOperation : IOperation
    {
    }
    /// <summary>
    /// Represents an operation that gets <see cref="System.Type" /> for the given <see cref="TypeOperand" />.
    /// <para>
    /// Current usage:
    ///  (1) C# typeof expression.
    ///  (2) VB GetType expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# sizeof expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# address of expression
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# is pattern expression. For example, "x is int i".
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# increment expression or decrement expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IMethodSymbol OperatorMethod { get; }
    }
    /// <summary>
    /// Represents an operation to throw an exception.
    /// <para>
    /// Current usage:
    ///  (1) C# throw expression.
    ///  (2) C# throw statement.
    ///  (2) VB Throw statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IThrowOperation : IOperation
    {
        /// <summary>
        /// Instance of an exception being thrown.
        /// </summary>
        IOperation Exception { get; }
    }
    /// <summary>
    /// Represents a assignment with a deconstruction.
    /// <para>
    /// Current usage:
    ///  (1) C# deconstruction assignment expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDeconstructionAssignmentOperation : IAssignmentOperation
    {
    }
    /// <summary>
    /// Represents a declaration expression operation. Unlike a regular variable declaration <see cref="IVariableDeclaratorOperation" /> and <see cref="IVariableDeclarationOperation" />, this operation represents an "expression" declaring a variable.
    /// <para>
    /// Current usage:
    ///  (1) C# declaration expression. For example,
    ///  (a) "var (x, y)" is a deconstruction declaration expression with variables x and y.
    ///  (b) "(var x, var y)" is a tuple expression with two declaration expressions.
    ///  (c) "M(out var x);" is an invocation expression with an out "var x" declaration expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    /// <summary>
    /// Represents an initializer for a field, property, parameter or a local variable declaration.
    /// <para>
    /// Current usage:
    ///  (1) C# field, property, parameter or local variable initializer.
    ///  (2) VB field(s), property, parameter or local variable initializer.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# field initializer with equals value clause.
    ///  (2) VB field(s) initializer with equals value clause or AsNew clause. Multiple fields can be initialized with AsNew clause in VB.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# local variable initializer with equals value clause.
    ///  (2) VB local variable initializer with equals value clause or AsNew clause.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IVariableInitializerOperation : ISymbolInitializerOperation
    {
    }
    /// <summary>
    /// Represents an initialization of a property.
    /// <para>
    /// Current usage:
    ///  (1) C# property initializer with equals value clause.
    ///  (2) VB property initializer with equals value clause or AsNew clause. Multiple properties can be initialized with 'WithEvents' declaration with AsNew clause in VB.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# parameter initializer with equals value clause.
    ///  (2) VB parameter initializer with equals value clause.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# array initializer.
    ///  (2) VB array initializer.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    /// </summary>
    /// <para>
    /// Current Usage:
    ///   (1) C# variable declarator
    ///   (2) C# catch variable declaration
    ///   (3) VB single variable declaration
    ///   (4) VB catch variable declaration
    /// </para>
    /// <remarks>
    /// In VB, the initializer for this node is only ever used for explicit array bounds initializers. This node corresponds to
    /// the VariableDeclaratorSyntax in C# and the ModifiedIdentifierSyntax in VB.
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IVariableInitializerOperation Initializer { get; }
        /// <summary>
        /// Additional arguments supplied to the declarator in error cases, ignored by the compiler. This only used for the C# case of
        /// DeclaredArgumentSyntax nodes on a VariableDeclaratorSyntax.
        /// </summary>
        ImmutableArray<IOperation> IgnoredArguments { get; }
    }
    /// <summary>
    /// Represents a declarator that declares multiple individual variables.
    /// </summary>
    /// <para>
    /// Current Usage:
    ///   (1) C# VariableDeclaration
    ///   (2) C# fixed declarations
    ///   (3) C# using declarations
    ///   (4) VB Dim statement declaration groups
    ///   (5) VB Using statement variable declarations
    /// </para>
    /// <remarks>
    /// The initializer of this node is applied to all individual declarations in <see cref="Declarators" />. There cannot
    /// be initializers in both locations except in invalid code scenarios.
    /// In C#, this node will never have an initializer.
    /// This corresponds to the VariableDeclarationSyntax in C#, and the VariableDeclaratorSyntax in Visual Basic.
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IVariableInitializerOperation Initializer { get; }
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
    ///  (1) C# argument to an invocation expression, object creation expression, etc.
    ///  (2) VB argument to an invocation expression, object creation expression, etc.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IArgumentOperation : IOperation
    {
        /// <summary>
        /// Kind of argument.
        /// </summary>
        ArgumentKind ArgumentKind { get; }
        /// <summary>
        /// Parameter the argument matches.
        /// </summary>
        IParameterSymbol Parameter { get; }
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
    ///  (1) C# catch clause.
    ///  (2) VB Catch clause.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IOperation ExceptionDeclarationOrExpression { get; }
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
        IOperation Filter { get; }
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        IBlockOperation Handler { get; }
    }
    /// <summary>
    /// Represents a switch case section with one or more case clauses to match and one or more operations to execute within the section.
    /// <para>
    /// Current usage:
    ///  (1) C# switch section for one or more case clause and set of statements to execute.
    ///  (2) VB case block with a case statement for one or more case clause and set of statements to execute.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# case clause.
    ///  (2) VB Case clause.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        ILabelSymbol Label { get; }
    }
    /// <summary>
    /// Represents a default case clause.
    /// <para>
    /// Current usage:
    ///  (1) C# default clause.
    ///  (2) VB Case Else clause.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IPatternCaseClauseOperation : ICaseClauseOperation
    {
        /// <summary>
        /// Label associated with the case clause.
        /// https://github.com/dotnet/roslyn/issues/27602: Similar property was added to the base interface, consider if we can remove this one.
        /// </summary>
        new ILabelSymbol Label { get; }
        /// <summary>
        /// Pattern associated with case clause.
        /// </summary>
        IPatternOperation Pattern { get; }
        /// <summary>
        /// Guard associated with the pattern case clause.
        /// </summary>
        IOperation Guard { get; }
    }
    /// <summary>
    /// Represents a case clause with range of values for comparison.
    /// <para>
    /// Current usage:
    ///  (1) VB range case clause of the form "Case x To y".
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) VB relational case clause of the form "Case Is op x".
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# case clause of the form "case x"
    ///  (2) VB case clause of the form "Case x".
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    /// <summary>
    /// Represents a constituent string literal part of an interpolated string operation.
    /// <para>
    /// Current usage:
    ///  (1) C# interpolated string text.
    ///  (2) VB interpolated string text.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# interpolation part.
    ///  (2) VB interpolation part.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IOperation Alignment { get; }
        /// <summary>
        /// Optional format string of the interpolation.
        /// </summary>
        IOperation FormatString { get; }
    }
    /// <summary>
    /// Represents a pattern matching operation.
    /// <para>
    /// Current usage:
    ///  (1) C# pattern.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IPatternOperation : IOperation
    {
        /// <summary>
        /// The input type to the pattern-matching operation.
        /// </summary>
        ITypeSymbol InputType { get; }
    }
    /// <summary>
    /// Represents a pattern with a constant value.
    /// <para>
    /// Current usage:
    ///  (1) C# constant pattern.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# declaration pattern.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDeclarationPatternOperation : IPatternOperation
    {
        /// <summary>
        /// The type explicitly specified, or null if it was inferred (e.g. using <code>var</code> in C#).
        /// </summary>
        ITypeSymbol MatchedType { get; }
        /// <summary>
        /// True if the pattern is of a form that accepts null.
        /// For example, in C# the pattern `var x` will match a null input,
        /// while the pattern `string x` will not.
        /// </summary>
        bool MatchesNull { get; }
        /// <summary>
        /// Symbol declared by the pattern, if any.
        /// </summary>
        ISymbol DeclaredSymbol { get; }
    }
    /// <summary>
    /// Represents a comparison of two operands that returns a bool type.
    /// <para>
    /// Current usage:
    ///  (1) C# tuple binary operator expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# method body
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IMethodBodyBaseOperation : IOperation
    {
        /// <summary>
        /// Method body corresponding to BaseMethodDeclarationSyntax.Body or AccessorDeclarationSyntax.Body
        /// </summary>
        IBlockOperation BlockBody { get; }
        /// <summary>
        /// Method body corresponding to BaseMethodDeclarationSyntax.ExpressionBody or AccessorDeclarationSyntax.ExpressionBody
        /// </summary>
        IBlockOperation ExpressionBody { get; }
    }
    /// <summary>
    /// Represents a method body operation.
    /// <para>
    /// Current usage:
    ///  (1) C# method body for non-constructor
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IMethodBodyOperation : IMethodBodyBaseOperation
    {
    }
    /// <summary>
    /// Represents a constructor method body operation.
    /// <para>
    /// Current usage:
    ///  (1) C# method body for constructor declaration
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IOperation Initializer { get; }
    }
    /// <summary>
    /// Represents a discard operation.
    /// <para>
    /// Current usage: C# discard expressions
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    /// (1) <see cref="IAssignmentOperation.Target" /> is evaluated for null. If it is null, <see cref="IAssignmentOperation.Value" /> is evaluated and assigned to target.
    /// (2) <see cref="IAssignmentOperation.Value" /> is conditionally evaluated if <see cref="IAssignmentOperation.Target" /> is null, and the result is assigned into <see cref="IAssignmentOperation.Target" />.
    /// The result of the entire expression is<see cref="IAssignmentOperation.Target" />, which is only evaluated once.
    /// <para>
    /// Current usage:
    ///  (1) C# null-coalescing assignment operation <code>Target ??= Value</code>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ICoalesceAssignmentOperation : IAssignmentOperation
    {
    }
    /// <summary>
    /// Represents a range operation.
    /// <para>
    /// Current usage:
    ///  (1) C# range expressions
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IRangeOperation : IOperation
    {
        /// <summary>
        /// Left operand.
        /// </summary>
        IOperation LeftOperand { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        IOperation RightOperand { get; }
        /// <summary>
        /// <code>true</code> if this is a 'lifted' range operation.  When there is an
        /// operator that is defined to work on a value type, 'lifted' operators are
        /// created to work on the <see cref="System.Nullable{T}" /> versions of those
        /// value types.
        /// </summary>
        bool IsLifted { get; }
        /// <summary>
        /// Factory method used to create this Range value. Can be null if appropriate
        /// symbol was not found.
        /// </summary>
        IMethodSymbol Method { get; }
    }
    /// <summary>
    /// Represents the ReDim operation to re-allocate storage space for array variables.
    /// <para>
    /// Current usage:
    ///  (1) VB ReDim statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) VB ReDim clause.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IRecursivePatternOperation : IPatternOperation
    {
        /// <summary>
        /// The type accepted for the recursive pattern.
        /// </summary>
        ITypeSymbol MatchedType { get; }
        /// <summary>
        /// The symbol, if any, used for the fetching values for subpatterns. This is either a <code>Deconstruct</code>
        /// method, the type <code>System.Runtime.CompilerServices.ITuple</code>, or null (for example, in
        /// error cases or when matching a tuple type).
        /// </summary>
        ISymbol DeconstructSymbol { get; }
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
        ISymbol DeclaredSymbol { get; }
    }
    /// <summary>
    /// Represents a discard pattern.
    /// <para>
    /// Current usage: C# discard pattern
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDiscardPatternOperation : IPatternOperation
    {
    }
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
    /// <summary>
    /// Represents one arm of a switch expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
        IOperation Guard { get; }
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
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) C# NoPia interface instance creation expression.
    ///  (2) VB NoPia interface instance creation expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal interface INoPiaObjectCreationOperation : IOperation
    {
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        IObjectOrCollectionInitializerOperation Initializer { get; }
    }
    /// <summary>
    /// Represents a general placeholder when no more specific kind of placeholder is available.
    /// A placeholder is an expression whose meaning is inferred from context.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal interface IPlaceholderOperation : IOperation
    {
        PlaceholderKind PlaceholderKind { get; }
    }
    /// <summary>
    /// Represents a reference through a pointer.
    /// <para>
    /// Current usage:
    ///  (1) C# pointer indirection reference expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
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
    ///  (1) VB With statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal interface IWithOperation : IOperation
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
    /// Represents a using variable declaration statement.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IUsingVariableDeclarationOperation : IOperation
    {
        /// <summary>
        /// Whether this using declaration is asynchronous.
        /// </summary>
        bool IsAsynchronous { get; }
        /// <summary>
        /// An <see cref="IVariableDeclarationGroupOperation" /> that contains the variables declared by this statement.
        /// </summary>
        IVariableDeclarationGroupOperation DeclarationGroup { get; }
    }
    #endregion

    #region Implementations
    internal abstract partial class BaseBlockOperation : Operation, IBlockOperation
    {
        internal BaseBlockOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Block, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Locals = locals;
        }
        public abstract ImmutableArray<IOperation> Operations { get; }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in Operations)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitBlock(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitBlock(this, argument);
    }
    internal sealed partial class BlockOperation : BaseBlockOperation, IBlockOperation
    {
        internal BlockOperation(ImmutableArray<IOperation> operations, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Operations = SetParentOperation(operations, this);
        }
        public override ImmutableArray<IOperation> Operations { get; }
    }
    internal abstract partial class LazyBlockOperation : BaseBlockOperation, IBlockOperation
    {
        private ImmutableArray<IOperation> _lazyOperations;
        internal LazyBlockOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IOperation> CreateOperations();
        public override ImmutableArray<IOperation> Operations
        {
            get
            {
                if (_lazyOperations.IsDefault)
                {
                    ImmutableArray<IOperation> operations = CreateOperations();
                    SetParentOperation(operations, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyOperations, operations);
                }
                return _lazyOperations;
            }
        }
    }
    internal abstract partial class BaseVariableDeclarationGroupOperation : Operation, IVariableDeclarationGroupOperation
    {
        internal BaseVariableDeclarationGroupOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.VariableDeclarationGroup, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract ImmutableArray<IVariableDeclarationOperation> Declarations { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in Declarations)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitVariableDeclarationGroup(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitVariableDeclarationGroup(this, argument);
    }
    internal sealed partial class VariableDeclarationGroupOperation : BaseVariableDeclarationGroupOperation, IVariableDeclarationGroupOperation
    {
        internal VariableDeclarationGroupOperation(ImmutableArray<IVariableDeclarationOperation> declarations, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Declarations = SetParentOperation(declarations, this);
        }
        public override ImmutableArray<IVariableDeclarationOperation> Declarations { get; }
    }
    internal abstract partial class LazyVariableDeclarationGroupOperation : BaseVariableDeclarationGroupOperation, IVariableDeclarationGroupOperation
    {
        private ImmutableArray<IVariableDeclarationOperation> _lazyDeclarations;
        internal LazyVariableDeclarationGroupOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IVariableDeclarationOperation> CreateDeclarations();
        public override ImmutableArray<IVariableDeclarationOperation> Declarations
        {
            get
            {
                if (_lazyDeclarations.IsDefault)
                {
                    ImmutableArray<IVariableDeclarationOperation> declarations = CreateDeclarations();
                    SetParentOperation(declarations, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyDeclarations, declarations);
                }
                return _lazyDeclarations;
            }
        }
    }
    internal abstract partial class BaseSwitchOperation : Operation, ISwitchOperation
    {
        internal BaseSwitchOperation(ImmutableArray<ILocalSymbol> locals, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Switch, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Locals = locals;
            ExitLabel = exitLabel;
        }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public abstract IOperation Value { get; }
        public abstract ImmutableArray<ISwitchCaseOperation> Cases { get; }
        public ILabelSymbol ExitLabel { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
                foreach (var child in Cases)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitSwitch(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitSwitch(this, argument);
    }
    internal sealed partial class SwitchOperation : BaseSwitchOperation, ISwitchOperation
    {
        internal SwitchOperation(ImmutableArray<ILocalSymbol> locals, IOperation value, ImmutableArray<ISwitchCaseOperation> cases, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Value = SetParentOperation(value, this);
            Cases = SetParentOperation(cases, this);
        }
        public override IOperation Value { get; }
        public override ImmutableArray<ISwitchCaseOperation> Cases { get; }
    }
    internal abstract partial class LazySwitchOperation : BaseSwitchOperation, ISwitchOperation
    {
        private IOperation _lazyValue = s_unset;
        private ImmutableArray<ISwitchCaseOperation> _lazyCases;
        internal LazySwitchOperation(ImmutableArray<ILocalSymbol> locals, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, exitLabel, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
        protected abstract ImmutableArray<ISwitchCaseOperation> CreateCases();
        public override ImmutableArray<ISwitchCaseOperation> Cases
        {
            get
            {
                if (_lazyCases.IsDefault)
                {
                    ImmutableArray<ISwitchCaseOperation> cases = CreateCases();
                    SetParentOperation(cases, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyCases, cases);
                }
                return _lazyCases;
            }
        }
    }
    internal abstract partial class BaseLoopOperation : Operation, ILoopOperation
    {
        protected BaseLoopOperation(LoopKind loopKind, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LoopKind = loopKind;
            Locals = locals;
            ContinueLabel = continueLabel;
            ExitLabel = exitLabel;
        }
        public LoopKind LoopKind { get; }
        public abstract IOperation Body { get; }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public ILabelSymbol ContinueLabel { get; }
        public ILabelSymbol ExitLabel { get; }
    }
    internal abstract partial class BaseForEachLoopOperation : BaseLoopOperation, IForEachLoopOperation
    {
        internal BaseForEachLoopOperation(bool isAsynchronous, LoopKind loopKind, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(loopKind, locals, continueLabel, exitLabel, OperationKind.Loop, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsAsynchronous = isAsynchronous;
        }
        public abstract IOperation LoopControlVariable { get; }
        public abstract IOperation Collection { get; }
        public abstract ImmutableArray<IOperation> NextVariables { get; }
        public bool IsAsynchronous { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Collection is object) yield return Collection;
                if (LoopControlVariable is object) yield return LoopControlVariable;
                if (Body is object) yield return Body;
                foreach (var child in NextVariables)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitForEachLoop(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitForEachLoop(this, argument);
    }
    internal sealed partial class ForEachLoopOperation : BaseForEachLoopOperation, IForEachLoopOperation
    {
        internal ForEachLoopOperation(IOperation loopControlVariable, IOperation collection, ImmutableArray<IOperation> nextVariables, bool isAsynchronous, LoopKind loopKind, IOperation body, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isAsynchronous, loopKind, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LoopControlVariable = SetParentOperation(loopControlVariable, this);
            Collection = SetParentOperation(collection, this);
            NextVariables = SetParentOperation(nextVariables, this);
            Body = SetParentOperation(body, this);
        }
        public override IOperation LoopControlVariable { get; }
        public override IOperation Collection { get; }
        public override ImmutableArray<IOperation> NextVariables { get; }
        public override IOperation Body { get; }
    }
    internal abstract partial class LazyForEachLoopOperation : BaseForEachLoopOperation, IForEachLoopOperation
    {
        private IOperation _lazyLoopControlVariable = s_unset;
        private IOperation _lazyCollection = s_unset;
        private ImmutableArray<IOperation> _lazyNextVariables;
        private IOperation _lazyBody = s_unset;
        internal LazyForEachLoopOperation(bool isAsynchronous, LoopKind loopKind, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isAsynchronous, loopKind, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateLoopControlVariable();
        public override IOperation LoopControlVariable
        {
            get
            {
                if (_lazyLoopControlVariable == s_unset)
                {
                    IOperation loopControlVariable = CreateLoopControlVariable();
                    SetParentOperation(loopControlVariable, this);
                    Interlocked.CompareExchange(ref _lazyLoopControlVariable, loopControlVariable, s_unset);
                }
                return _lazyLoopControlVariable;
            }
        }
        protected abstract IOperation CreateCollection();
        public override IOperation Collection
        {
            get
            {
                if (_lazyCollection == s_unset)
                {
                    IOperation collection = CreateCollection();
                    SetParentOperation(collection, this);
                    Interlocked.CompareExchange(ref _lazyCollection, collection, s_unset);
                }
                return _lazyCollection;
            }
        }
        protected abstract ImmutableArray<IOperation> CreateNextVariables();
        public override ImmutableArray<IOperation> NextVariables
        {
            get
            {
                if (_lazyNextVariables.IsDefault)
                {
                    ImmutableArray<IOperation> nextVariables = CreateNextVariables();
                    SetParentOperation(nextVariables, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyNextVariables, nextVariables);
                }
                return _lazyNextVariables;
            }
        }
        protected abstract IOperation CreateBody();
        public override IOperation Body
        {
            get
            {
                if (_lazyBody == s_unset)
                {
                    IOperation body = CreateBody();
                    SetParentOperation(body, this);
                    Interlocked.CompareExchange(ref _lazyBody, body, s_unset);
                }
                return _lazyBody;
            }
        }
    }
    internal abstract partial class BaseForLoopOperation : BaseLoopOperation, IForLoopOperation
    {
        internal BaseForLoopOperation(ImmutableArray<ILocalSymbol> conditionLocals, LoopKind loopKind, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(loopKind, locals, continueLabel, exitLabel, OperationKind.Loop, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ConditionLocals = conditionLocals;
        }
        public abstract ImmutableArray<IOperation> Before { get; }
        public ImmutableArray<ILocalSymbol> ConditionLocals { get; }
        public abstract IOperation Condition { get; }
        public abstract ImmutableArray<IOperation> AtLoopBottom { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in Before)
                {
                    if (child is object) yield return child;
                }
                if (Condition is object) yield return Condition;
                if (Body is object) yield return Body;
                foreach (var child in AtLoopBottom)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitForLoop(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitForLoop(this, argument);
    }
    internal sealed partial class ForLoopOperation : BaseForLoopOperation, IForLoopOperation
    {
        internal ForLoopOperation(ImmutableArray<IOperation> before, ImmutableArray<ILocalSymbol> conditionLocals, IOperation condition, ImmutableArray<IOperation> atLoopBottom, LoopKind loopKind, IOperation body, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(conditionLocals, loopKind, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Before = SetParentOperation(before, this);
            Condition = SetParentOperation(condition, this);
            AtLoopBottom = SetParentOperation(atLoopBottom, this);
            Body = SetParentOperation(body, this);
        }
        public override ImmutableArray<IOperation> Before { get; }
        public override IOperation Condition { get; }
        public override ImmutableArray<IOperation> AtLoopBottom { get; }
        public override IOperation Body { get; }
    }
    internal abstract partial class LazyForLoopOperation : BaseForLoopOperation, IForLoopOperation
    {
        private ImmutableArray<IOperation> _lazyBefore;
        private IOperation _lazyCondition = s_unset;
        private ImmutableArray<IOperation> _lazyAtLoopBottom;
        private IOperation _lazyBody = s_unset;
        internal LazyForLoopOperation(ImmutableArray<ILocalSymbol> conditionLocals, LoopKind loopKind, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(conditionLocals, loopKind, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IOperation> CreateBefore();
        public override ImmutableArray<IOperation> Before
        {
            get
            {
                if (_lazyBefore.IsDefault)
                {
                    ImmutableArray<IOperation> before = CreateBefore();
                    SetParentOperation(before, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyBefore, before);
                }
                return _lazyBefore;
            }
        }
        protected abstract IOperation CreateCondition();
        public override IOperation Condition
        {
            get
            {
                if (_lazyCondition == s_unset)
                {
                    IOperation condition = CreateCondition();
                    SetParentOperation(condition, this);
                    Interlocked.CompareExchange(ref _lazyCondition, condition, s_unset);
                }
                return _lazyCondition;
            }
        }
        protected abstract ImmutableArray<IOperation> CreateAtLoopBottom();
        public override ImmutableArray<IOperation> AtLoopBottom
        {
            get
            {
                if (_lazyAtLoopBottom.IsDefault)
                {
                    ImmutableArray<IOperation> atLoopBottom = CreateAtLoopBottom();
                    SetParentOperation(atLoopBottom, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyAtLoopBottom, atLoopBottom);
                }
                return _lazyAtLoopBottom;
            }
        }
        protected abstract IOperation CreateBody();
        public override IOperation Body
        {
            get
            {
                if (_lazyBody == s_unset)
                {
                    IOperation body = CreateBody();
                    SetParentOperation(body, this);
                    Interlocked.CompareExchange(ref _lazyBody, body, s_unset);
                }
                return _lazyBody;
            }
        }
    }
    internal abstract partial class BaseForToLoopOperation : BaseLoopOperation, IForToLoopOperation
    {
        internal BaseForToLoopOperation(bool isChecked, (ILocalSymbol LoopObject, ForToLoopOperationUserDefinedInfo UserDefinedInfo) info, LoopKind loopKind, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(loopKind, locals, continueLabel, exitLabel, OperationKind.Loop, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsChecked = isChecked;
            Info = info;
        }
        public abstract IOperation LoopControlVariable { get; }
        public abstract IOperation InitialValue { get; }
        public abstract IOperation LimitValue { get; }
        public abstract IOperation StepValue { get; }
        public bool IsChecked { get; }
        public abstract ImmutableArray<IOperation> NextVariables { get; }
        public (ILocalSymbol LoopObject, ForToLoopOperationUserDefinedInfo UserDefinedInfo) Info { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (LoopControlVariable is object) yield return LoopControlVariable;
                if (InitialValue is object) yield return InitialValue;
                if (LimitValue is object) yield return LimitValue;
                if (StepValue is object) yield return StepValue;
                if (Body is object) yield return Body;
                foreach (var child in NextVariables)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitForToLoop(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitForToLoop(this, argument);
    }
    internal sealed partial class ForToLoopOperation : BaseForToLoopOperation, IForToLoopOperation
    {
        internal ForToLoopOperation(IOperation loopControlVariable, IOperation initialValue, IOperation limitValue, IOperation stepValue, bool isChecked, ImmutableArray<IOperation> nextVariables, (ILocalSymbol LoopObject, ForToLoopOperationUserDefinedInfo UserDefinedInfo) info, LoopKind loopKind, IOperation body, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isChecked, info, loopKind, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LoopControlVariable = SetParentOperation(loopControlVariable, this);
            InitialValue = SetParentOperation(initialValue, this);
            LimitValue = SetParentOperation(limitValue, this);
            StepValue = SetParentOperation(stepValue, this);
            NextVariables = SetParentOperation(nextVariables, this);
            Body = SetParentOperation(body, this);
        }
        public override IOperation LoopControlVariable { get; }
        public override IOperation InitialValue { get; }
        public override IOperation LimitValue { get; }
        public override IOperation StepValue { get; }
        public override ImmutableArray<IOperation> NextVariables { get; }
        public override IOperation Body { get; }
    }
    internal abstract partial class LazyForToLoopOperation : BaseForToLoopOperation, IForToLoopOperation
    {
        private IOperation _lazyLoopControlVariable = s_unset;
        private IOperation _lazyInitialValue = s_unset;
        private IOperation _lazyLimitValue = s_unset;
        private IOperation _lazyStepValue = s_unset;
        private ImmutableArray<IOperation> _lazyNextVariables;
        private IOperation _lazyBody = s_unset;
        internal LazyForToLoopOperation(bool isChecked, (ILocalSymbol LoopObject, ForToLoopOperationUserDefinedInfo UserDefinedInfo) info, LoopKind loopKind, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isChecked, info, loopKind, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateLoopControlVariable();
        public override IOperation LoopControlVariable
        {
            get
            {
                if (_lazyLoopControlVariable == s_unset)
                {
                    IOperation loopControlVariable = CreateLoopControlVariable();
                    SetParentOperation(loopControlVariable, this);
                    Interlocked.CompareExchange(ref _lazyLoopControlVariable, loopControlVariable, s_unset);
                }
                return _lazyLoopControlVariable;
            }
        }
        protected abstract IOperation CreateInitialValue();
        public override IOperation InitialValue
        {
            get
            {
                if (_lazyInitialValue == s_unset)
                {
                    IOperation initialValue = CreateInitialValue();
                    SetParentOperation(initialValue, this);
                    Interlocked.CompareExchange(ref _lazyInitialValue, initialValue, s_unset);
                }
                return _lazyInitialValue;
            }
        }
        protected abstract IOperation CreateLimitValue();
        public override IOperation LimitValue
        {
            get
            {
                if (_lazyLimitValue == s_unset)
                {
                    IOperation limitValue = CreateLimitValue();
                    SetParentOperation(limitValue, this);
                    Interlocked.CompareExchange(ref _lazyLimitValue, limitValue, s_unset);
                }
                return _lazyLimitValue;
            }
        }
        protected abstract IOperation CreateStepValue();
        public override IOperation StepValue
        {
            get
            {
                if (_lazyStepValue == s_unset)
                {
                    IOperation stepValue = CreateStepValue();
                    SetParentOperation(stepValue, this);
                    Interlocked.CompareExchange(ref _lazyStepValue, stepValue, s_unset);
                }
                return _lazyStepValue;
            }
        }
        protected abstract ImmutableArray<IOperation> CreateNextVariables();
        public override ImmutableArray<IOperation> NextVariables
        {
            get
            {
                if (_lazyNextVariables.IsDefault)
                {
                    ImmutableArray<IOperation> nextVariables = CreateNextVariables();
                    SetParentOperation(nextVariables, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyNextVariables, nextVariables);
                }
                return _lazyNextVariables;
            }
        }
        protected abstract IOperation CreateBody();
        public override IOperation Body
        {
            get
            {
                if (_lazyBody == s_unset)
                {
                    IOperation body = CreateBody();
                    SetParentOperation(body, this);
                    Interlocked.CompareExchange(ref _lazyBody, body, s_unset);
                }
                return _lazyBody;
            }
        }
    }
    internal abstract partial class BaseWhileLoopOperation : BaseLoopOperation, IWhileLoopOperation
    {
        internal BaseWhileLoopOperation(bool conditionIsTop, bool conditionIsUntil, LoopKind loopKind, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(loopKind, locals, continueLabel, exitLabel, OperationKind.Loop, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ConditionIsTop = conditionIsTop;
            ConditionIsUntil = conditionIsUntil;
        }
        public abstract IOperation Condition { get; }
        public bool ConditionIsTop { get; }
        public bool ConditionIsUntil { get; }
        public abstract IOperation IgnoredCondition { get; }
        public override void Accept(OperationVisitor visitor) => visitor.VisitWhileLoop(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitWhileLoop(this, argument);
    }
    internal sealed partial class WhileLoopOperation : BaseWhileLoopOperation, IWhileLoopOperation
    {
        internal WhileLoopOperation(IOperation condition, bool conditionIsTop, bool conditionIsUntil, IOperation ignoredCondition, LoopKind loopKind, IOperation body, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(conditionIsTop, conditionIsUntil, loopKind, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Condition = SetParentOperation(condition, this);
            IgnoredCondition = SetParentOperation(ignoredCondition, this);
            Body = SetParentOperation(body, this);
        }
        public override IOperation Condition { get; }
        public override IOperation IgnoredCondition { get; }
        public override IOperation Body { get; }
    }
    internal abstract partial class LazyWhileLoopOperation : BaseWhileLoopOperation, IWhileLoopOperation
    {
        private IOperation _lazyCondition = s_unset;
        private IOperation _lazyIgnoredCondition = s_unset;
        private IOperation _lazyBody = s_unset;
        internal LazyWhileLoopOperation(bool conditionIsTop, bool conditionIsUntil, LoopKind loopKind, ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(conditionIsTop, conditionIsUntil, loopKind, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateCondition();
        public override IOperation Condition
        {
            get
            {
                if (_lazyCondition == s_unset)
                {
                    IOperation condition = CreateCondition();
                    SetParentOperation(condition, this);
                    Interlocked.CompareExchange(ref _lazyCondition, condition, s_unset);
                }
                return _lazyCondition;
            }
        }
        protected abstract IOperation CreateIgnoredCondition();
        public override IOperation IgnoredCondition
        {
            get
            {
                if (_lazyIgnoredCondition == s_unset)
                {
                    IOperation ignoredCondition = CreateIgnoredCondition();
                    SetParentOperation(ignoredCondition, this);
                    Interlocked.CompareExchange(ref _lazyIgnoredCondition, ignoredCondition, s_unset);
                }
                return _lazyIgnoredCondition;
            }
        }
        protected abstract IOperation CreateBody();
        public override IOperation Body
        {
            get
            {
                if (_lazyBody == s_unset)
                {
                    IOperation body = CreateBody();
                    SetParentOperation(body, this);
                    Interlocked.CompareExchange(ref _lazyBody, body, s_unset);
                }
                return _lazyBody;
            }
        }
    }
    internal abstract partial class BaseLabeledOperation : Operation, ILabeledOperation
    {
        internal BaseLabeledOperation(ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Labeled, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Label = label;
        }
        public ILabelSymbol Label { get; }
        public abstract IOperation Operation { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation is object) yield return Operation;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitLabeled(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitLabeled(this, argument);
    }
    internal sealed partial class LabeledOperation : BaseLabeledOperation, ILabeledOperation
    {
        internal LabeledOperation(ILabelSymbol label, IOperation operation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
        }
        public override IOperation Operation { get; }
    }
    internal abstract partial class LazyLabeledOperation : BaseLabeledOperation, ILabeledOperation
    {
        private IOperation _lazyOperation = s_unset;
        internal LazyLabeledOperation(ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(label, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateOperation();
        public override IOperation Operation
        {
            get
            {
                if (_lazyOperation == s_unset)
                {
                    IOperation operation = CreateOperation();
                    SetParentOperation(operation, this);
                    Interlocked.CompareExchange(ref _lazyOperation, operation, s_unset);
                }
                return _lazyOperation;
            }
        }
    }
    internal sealed partial class BranchOperation : Operation, IBranchOperation
    {
        internal BranchOperation(ILabelSymbol target, BranchKind branchKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Branch, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Target = target;
            BranchKind = branchKind;
        }
        public ILabelSymbol Target { get; }
        public BranchKind BranchKind { get; }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitBranch(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitBranch(this, argument);
    }
    internal sealed partial class EmptyOperation : Operation, IEmptyOperation
    {
        internal EmptyOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Empty, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitEmpty(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitEmpty(this, argument);
    }
    internal abstract partial class BaseReturnOperation : Operation, IReturnOperation
    {
        internal BaseReturnOperation(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(kind, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation ReturnedValue { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (ReturnedValue is object) yield return ReturnedValue;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitReturn(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitReturn(this, argument);
    }
    internal sealed partial class ReturnOperation : BaseReturnOperation, IReturnOperation
    {
        internal ReturnOperation(IOperation returnedValue, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ReturnedValue = SetParentOperation(returnedValue, this);
        }
        public override IOperation ReturnedValue { get; }
    }
    internal abstract partial class LazyReturnOperation : BaseReturnOperation, IReturnOperation
    {
        private IOperation _lazyReturnedValue = s_unset;
        internal LazyReturnOperation(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(kind, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateReturnedValue();
        public override IOperation ReturnedValue
        {
            get
            {
                if (_lazyReturnedValue == s_unset)
                {
                    IOperation returnedValue = CreateReturnedValue();
                    SetParentOperation(returnedValue, this);
                    Interlocked.CompareExchange(ref _lazyReturnedValue, returnedValue, s_unset);
                }
                return _lazyReturnedValue;
            }
        }
    }
    internal abstract partial class BaseLockOperation : Operation, ILockOperation
    {
        internal BaseLockOperation(ILocalSymbol lockTakenSymbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Lock, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LockTakenSymbol = lockTakenSymbol;
        }
        public abstract IOperation LockedValue { get; }
        public abstract IOperation Body { get; }
        public ILocalSymbol LockTakenSymbol { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (LockedValue is object) yield return LockedValue;
                if (Body is object) yield return Body;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitLock(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitLock(this, argument);
    }
    internal sealed partial class LockOperation : BaseLockOperation, ILockOperation
    {
        internal LockOperation(IOperation lockedValue, IOperation body, ILocalSymbol lockTakenSymbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(lockTakenSymbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LockedValue = SetParentOperation(lockedValue, this);
            Body = SetParentOperation(body, this);
        }
        public override IOperation LockedValue { get; }
        public override IOperation Body { get; }
    }
    internal abstract partial class LazyLockOperation : BaseLockOperation, ILockOperation
    {
        private IOperation _lazyLockedValue = s_unset;
        private IOperation _lazyBody = s_unset;
        internal LazyLockOperation(ILocalSymbol lockTakenSymbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(lockTakenSymbol, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateLockedValue();
        public override IOperation LockedValue
        {
            get
            {
                if (_lazyLockedValue == s_unset)
                {
                    IOperation lockedValue = CreateLockedValue();
                    SetParentOperation(lockedValue, this);
                    Interlocked.CompareExchange(ref _lazyLockedValue, lockedValue, s_unset);
                }
                return _lazyLockedValue;
            }
        }
        protected abstract IOperation CreateBody();
        public override IOperation Body
        {
            get
            {
                if (_lazyBody == s_unset)
                {
                    IOperation body = CreateBody();
                    SetParentOperation(body, this);
                    Interlocked.CompareExchange(ref _lazyBody, body, s_unset);
                }
                return _lazyBody;
            }
        }
    }
    internal abstract partial class BaseTryOperation : Operation, ITryOperation
    {
        internal BaseTryOperation(ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Try, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExitLabel = exitLabel;
        }
        public abstract IBlockOperation Body { get; }
        public abstract ImmutableArray<ICatchClauseOperation> Catches { get; }
        public abstract IBlockOperation Finally { get; }
        public ILabelSymbol ExitLabel { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Body is object) yield return Body;
                foreach (var child in Catches)
                {
                    if (child is object) yield return child;
                }
                if (Finally is object) yield return Finally;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitTry(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitTry(this, argument);
    }
    internal sealed partial class TryOperation : BaseTryOperation, ITryOperation
    {
        internal TryOperation(IBlockOperation body, ImmutableArray<ICatchClauseOperation> catches, IBlockOperation @finally, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Body = SetParentOperation(body, this);
            Catches = SetParentOperation(catches, this);
            Finally = SetParentOperation(@finally, this);
        }
        public override IBlockOperation Body { get; }
        public override ImmutableArray<ICatchClauseOperation> Catches { get; }
        public override IBlockOperation Finally { get; }
    }
    internal abstract partial class LazyTryOperation : BaseTryOperation, ITryOperation
    {
        private IBlockOperation _lazyBody = s_unsetBlock;
        private ImmutableArray<ICatchClauseOperation> _lazyCatches;
        private IBlockOperation _lazyFinally = s_unsetBlock;
        internal LazyTryOperation(ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(exitLabel, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IBlockOperation CreateBody();
        public override IBlockOperation Body
        {
            get
            {
                if (_lazyBody == s_unsetBlock)
                {
                    IBlockOperation body = CreateBody();
                    SetParentOperation(body, this);
                    Interlocked.CompareExchange(ref _lazyBody, body, s_unsetBlock);
                }
                return _lazyBody;
            }
        }
        protected abstract ImmutableArray<ICatchClauseOperation> CreateCatches();
        public override ImmutableArray<ICatchClauseOperation> Catches
        {
            get
            {
                if (_lazyCatches.IsDefault)
                {
                    ImmutableArray<ICatchClauseOperation> catches = CreateCatches();
                    SetParentOperation(catches, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyCatches, catches);
                }
                return _lazyCatches;
            }
        }
        protected abstract IBlockOperation CreateFinally();
        public override IBlockOperation Finally
        {
            get
            {
                if (_lazyFinally == s_unsetBlock)
                {
                    IBlockOperation @finally = CreateFinally();
                    SetParentOperation(@finally, this);
                    Interlocked.CompareExchange(ref _lazyFinally, @finally, s_unsetBlock);
                }
                return _lazyFinally;
            }
        }
    }
    internal abstract partial class BaseUsingOperation : Operation, IUsingOperation
    {
        internal BaseUsingOperation(ImmutableArray<ILocalSymbol> locals, bool isAsynchronous, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Using, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Locals = locals;
            IsAsynchronous = isAsynchronous;
        }
        public abstract IOperation Resources { get; }
        public abstract IOperation Body { get; }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public bool IsAsynchronous { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Resources is object) yield return Resources;
                if (Body is object) yield return Body;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitUsing(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitUsing(this, argument);
    }
    internal sealed partial class UsingOperation : BaseUsingOperation, IUsingOperation
    {
        internal UsingOperation(IOperation resources, IOperation body, ImmutableArray<ILocalSymbol> locals, bool isAsynchronous, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, isAsynchronous, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Resources = SetParentOperation(resources, this);
            Body = SetParentOperation(body, this);
        }
        public override IOperation Resources { get; }
        public override IOperation Body { get; }
    }
    internal abstract partial class LazyUsingOperation : BaseUsingOperation, IUsingOperation
    {
        private IOperation _lazyResources = s_unset;
        private IOperation _lazyBody = s_unset;
        internal LazyUsingOperation(ImmutableArray<ILocalSymbol> locals, bool isAsynchronous, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, isAsynchronous, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateResources();
        public override IOperation Resources
        {
            get
            {
                if (_lazyResources == s_unset)
                {
                    IOperation resources = CreateResources();
                    SetParentOperation(resources, this);
                    Interlocked.CompareExchange(ref _lazyResources, resources, s_unset);
                }
                return _lazyResources;
            }
        }
        protected abstract IOperation CreateBody();
        public override IOperation Body
        {
            get
            {
                if (_lazyBody == s_unset)
                {
                    IOperation body = CreateBody();
                    SetParentOperation(body, this);
                    Interlocked.CompareExchange(ref _lazyBody, body, s_unset);
                }
                return _lazyBody;
            }
        }
    }
    internal abstract partial class BaseExpressionStatementOperation : Operation, IExpressionStatementOperation
    {
        internal BaseExpressionStatementOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.ExpressionStatement, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Operation { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation is object) yield return Operation;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitExpressionStatement(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitExpressionStatement(this, argument);
    }
    internal sealed partial class ExpressionStatementOperation : BaseExpressionStatementOperation, IExpressionStatementOperation
    {
        internal ExpressionStatementOperation(IOperation operation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
        }
        public override IOperation Operation { get; }
    }
    internal abstract partial class LazyExpressionStatementOperation : BaseExpressionStatementOperation, IExpressionStatementOperation
    {
        private IOperation _lazyOperation = s_unset;
        internal LazyExpressionStatementOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateOperation();
        public override IOperation Operation
        {
            get
            {
                if (_lazyOperation == s_unset)
                {
                    IOperation operation = CreateOperation();
                    SetParentOperation(operation, this);
                    Interlocked.CompareExchange(ref _lazyOperation, operation, s_unset);
                }
                return _lazyOperation;
            }
        }
    }
    internal abstract partial class BaseLocalFunctionOperation : Operation, ILocalFunctionOperation
    {
        internal BaseLocalFunctionOperation(IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.LocalFunction, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Symbol = symbol;
        }
        public IMethodSymbol Symbol { get; }
        public abstract IBlockOperation Body { get; }
        public abstract IBlockOperation IgnoredBody { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Body is object) yield return Body;
                if (IgnoredBody is object) yield return IgnoredBody;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitLocalFunction(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitLocalFunction(this, argument);
    }
    internal sealed partial class LocalFunctionOperation : BaseLocalFunctionOperation, ILocalFunctionOperation
    {
        internal LocalFunctionOperation(IMethodSymbol symbol, IBlockOperation body, IBlockOperation ignoredBody, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Body = SetParentOperation(body, this);
            IgnoredBody = SetParentOperation(ignoredBody, this);
        }
        public override IBlockOperation Body { get; }
        public override IBlockOperation IgnoredBody { get; }
    }
    internal abstract partial class LazyLocalFunctionOperation : BaseLocalFunctionOperation, ILocalFunctionOperation
    {
        private IBlockOperation _lazyBody = s_unsetBlock;
        private IBlockOperation _lazyIgnoredBody = s_unsetBlock;
        internal LazyLocalFunctionOperation(IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(symbol, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IBlockOperation CreateBody();
        public override IBlockOperation Body
        {
            get
            {
                if (_lazyBody == s_unsetBlock)
                {
                    IBlockOperation body = CreateBody();
                    SetParentOperation(body, this);
                    Interlocked.CompareExchange(ref _lazyBody, body, s_unsetBlock);
                }
                return _lazyBody;
            }
        }
        protected abstract IBlockOperation CreateIgnoredBody();
        public override IBlockOperation IgnoredBody
        {
            get
            {
                if (_lazyIgnoredBody == s_unsetBlock)
                {
                    IBlockOperation ignoredBody = CreateIgnoredBody();
                    SetParentOperation(ignoredBody, this);
                    Interlocked.CompareExchange(ref _lazyIgnoredBody, ignoredBody, s_unsetBlock);
                }
                return _lazyIgnoredBody;
            }
        }
    }
    internal sealed partial class StopOperation : Operation, IStopOperation
    {
        internal StopOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Stop, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitStop(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitStop(this, argument);
    }
    internal sealed partial class EndOperation : Operation, IEndOperation
    {
        internal EndOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.End, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitEnd(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitEnd(this, argument);
    }
    internal abstract partial class BaseRaiseEventOperation : Operation, IRaiseEventOperation
    {
        internal BaseRaiseEventOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.RaiseEvent, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IEventReferenceOperation EventReference { get; }
        public abstract ImmutableArray<IArgumentOperation> Arguments { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (EventReference is object) yield return EventReference;
                foreach (var child in Arguments)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitRaiseEvent(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitRaiseEvent(this, argument);
    }
    internal sealed partial class RaiseEventOperation : BaseRaiseEventOperation, IRaiseEventOperation
    {
        internal RaiseEventOperation(IEventReferenceOperation eventReference, ImmutableArray<IArgumentOperation> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            EventReference = SetParentOperation(eventReference, this);
            Arguments = SetParentOperation(arguments, this);
        }
        public override IEventReferenceOperation EventReference { get; }
        public override ImmutableArray<IArgumentOperation> Arguments { get; }
    }
    internal abstract partial class LazyRaiseEventOperation : BaseRaiseEventOperation, IRaiseEventOperation
    {
        private IEventReferenceOperation _lazyEventReference = s_unsetEventReference;
        private ImmutableArray<IArgumentOperation> _lazyArguments;
        internal LazyRaiseEventOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IEventReferenceOperation CreateEventReference();
        public override IEventReferenceOperation EventReference
        {
            get
            {
                if (_lazyEventReference == s_unsetEventReference)
                {
                    IEventReferenceOperation eventReference = CreateEventReference();
                    SetParentOperation(eventReference, this);
                    Interlocked.CompareExchange(ref _lazyEventReference, eventReference, s_unsetEventReference);
                }
                return _lazyEventReference;
            }
        }
        protected abstract ImmutableArray<IArgumentOperation> CreateArguments();
        public override ImmutableArray<IArgumentOperation> Arguments
        {
            get
            {
                if (_lazyArguments.IsDefault)
                {
                    ImmutableArray<IArgumentOperation> arguments = CreateArguments();
                    SetParentOperation(arguments, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyArguments, arguments);
                }
                return _lazyArguments;
            }
        }
    }
    internal sealed partial class LiteralOperation : Operation, ILiteralOperation
    {
        internal LiteralOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Literal, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitLiteral(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitLiteral(this, argument);
    }
    internal abstract partial class BaseConversionOperation : Operation, IConversionOperation
    {
        internal BaseConversionOperation(IConvertibleConversion conversion, bool isTryCast, bool isChecked, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Conversion, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ConversionConvertible = conversion;
            IsTryCast = isTryCast;
            IsChecked = isChecked;
        }
        public abstract IOperation Operand { get; }
        internal IConvertibleConversion ConversionConvertible { get; }
        public CommonConversion Conversion => ConversionConvertible.ToCommonConversion();
        public bool IsTryCast { get; }
        public bool IsChecked { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operand is object) yield return Operand;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitConversion(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitConversion(this, argument);
    }
    internal sealed partial class ConversionOperation : BaseConversionOperation, IConversionOperation
    {
        internal ConversionOperation(IOperation operand, IConvertibleConversion conversion, bool isTryCast, bool isChecked, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(conversion, isTryCast, isChecked, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Operand = SetParentOperation(operand, this);
        }
        public override IOperation Operand { get; }
    }
    internal abstract partial class LazyConversionOperation : BaseConversionOperation, IConversionOperation
    {
        private IOperation _lazyOperand = s_unset;
        internal LazyConversionOperation(IConvertibleConversion conversion, bool isTryCast, bool isChecked, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(conversion, isTryCast, isChecked, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateOperand();
        public override IOperation Operand
        {
            get
            {
                if (_lazyOperand == s_unset)
                {
                    IOperation operand = CreateOperand();
                    SetParentOperation(operand, this);
                    Interlocked.CompareExchange(ref _lazyOperand, operand, s_unset);
                }
                return _lazyOperand;
            }
        }
    }
    internal abstract partial class BaseInvocationOperation : Operation, IInvocationOperation
    {
        internal BaseInvocationOperation(IMethodSymbol targetMethod, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Invocation, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TargetMethod = targetMethod;
            IsVirtual = isVirtual;
        }
        public IMethodSymbol TargetMethod { get; }
        public abstract IOperation Instance { get; }
        public bool IsVirtual { get; }
        public abstract ImmutableArray<IArgumentOperation> Arguments { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Instance is object) yield return Instance;
                foreach (var child in Arguments)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitInvocation(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitInvocation(this, argument);
    }
    internal sealed partial class InvocationOperation : BaseInvocationOperation, IInvocationOperation
    {
        internal InvocationOperation(IMethodSymbol targetMethod, IOperation instance, bool isVirtual, ImmutableArray<IArgumentOperation> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Instance = SetParentOperation(instance, this);
            Arguments = SetParentOperation(arguments, this);
        }
        public override IOperation Instance { get; }
        public override ImmutableArray<IArgumentOperation> Arguments { get; }
    }
    internal abstract partial class LazyInvocationOperation : BaseInvocationOperation, IInvocationOperation
    {
        private IOperation _lazyInstance = s_unset;
        private ImmutableArray<IArgumentOperation> _lazyArguments;
        internal LazyInvocationOperation(IMethodSymbol targetMethod, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateInstance();
        public override IOperation Instance
        {
            get
            {
                if (_lazyInstance == s_unset)
                {
                    IOperation instance = CreateInstance();
                    SetParentOperation(instance, this);
                    Interlocked.CompareExchange(ref _lazyInstance, instance, s_unset);
                }
                return _lazyInstance;
            }
        }
        protected abstract ImmutableArray<IArgumentOperation> CreateArguments();
        public override ImmutableArray<IArgumentOperation> Arguments
        {
            get
            {
                if (_lazyArguments.IsDefault)
                {
                    ImmutableArray<IArgumentOperation> arguments = CreateArguments();
                    SetParentOperation(arguments, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyArguments, arguments);
                }
                return _lazyArguments;
            }
        }
    }
    internal abstract partial class BaseArrayElementReferenceOperation : Operation, IArrayElementReferenceOperation
    {
        internal BaseArrayElementReferenceOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.ArrayElementReference, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation ArrayReference { get; }
        public abstract ImmutableArray<IOperation> Indices { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (ArrayReference is object) yield return ArrayReference;
                foreach (var child in Indices)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitArrayElementReference(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitArrayElementReference(this, argument);
    }
    internal sealed partial class ArrayElementReferenceOperation : BaseArrayElementReferenceOperation, IArrayElementReferenceOperation
    {
        internal ArrayElementReferenceOperation(IOperation arrayReference, ImmutableArray<IOperation> indices, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ArrayReference = SetParentOperation(arrayReference, this);
            Indices = SetParentOperation(indices, this);
        }
        public override IOperation ArrayReference { get; }
        public override ImmutableArray<IOperation> Indices { get; }
    }
    internal abstract partial class LazyArrayElementReferenceOperation : BaseArrayElementReferenceOperation, IArrayElementReferenceOperation
    {
        private IOperation _lazyArrayReference = s_unset;
        private ImmutableArray<IOperation> _lazyIndices;
        internal LazyArrayElementReferenceOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateArrayReference();
        public override IOperation ArrayReference
        {
            get
            {
                if (_lazyArrayReference == s_unset)
                {
                    IOperation arrayReference = CreateArrayReference();
                    SetParentOperation(arrayReference, this);
                    Interlocked.CompareExchange(ref _lazyArrayReference, arrayReference, s_unset);
                }
                return _lazyArrayReference;
            }
        }
        protected abstract ImmutableArray<IOperation> CreateIndices();
        public override ImmutableArray<IOperation> Indices
        {
            get
            {
                if (_lazyIndices.IsDefault)
                {
                    ImmutableArray<IOperation> indices = CreateIndices();
                    SetParentOperation(indices, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyIndices, indices);
                }
                return _lazyIndices;
            }
        }
    }
    internal sealed partial class LocalReferenceOperation : Operation, ILocalReferenceOperation
    {
        internal LocalReferenceOperation(ILocalSymbol local, bool isDeclaration, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.LocalReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Local = local;
            IsDeclaration = isDeclaration;
        }
        public ILocalSymbol Local { get; }
        public bool IsDeclaration { get; }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitLocalReference(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitLocalReference(this, argument);
    }
    internal sealed partial class ParameterReferenceOperation : Operation, IParameterReferenceOperation
    {
        internal ParameterReferenceOperation(IParameterSymbol parameter, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.ParameterReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Parameter = parameter;
        }
        public IParameterSymbol Parameter { get; }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitParameterReference(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitParameterReference(this, argument);
    }
    internal abstract partial class BaseMemberReferenceOperation : Operation, IMemberReferenceOperation
    {
        protected BaseMemberReferenceOperation(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(kind, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Instance { get; }
    }
    internal abstract partial class BaseFieldReferenceOperation : BaseMemberReferenceOperation, IFieldReferenceOperation
    {
        internal BaseFieldReferenceOperation(IFieldSymbol field, bool isDeclaration, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.FieldReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Field = field;
            IsDeclaration = isDeclaration;
        }
        public IFieldSymbol Field { get; }
        public bool IsDeclaration { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Instance is object) yield return Instance;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitFieldReference(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitFieldReference(this, argument);
    }
    internal sealed partial class FieldReferenceOperation : BaseFieldReferenceOperation, IFieldReferenceOperation
    {
        internal FieldReferenceOperation(IFieldSymbol field, bool isDeclaration, IOperation instance, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(field, isDeclaration, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Instance = SetParentOperation(instance, this);
        }
        public override IOperation Instance { get; }
    }
    internal abstract partial class LazyFieldReferenceOperation : BaseFieldReferenceOperation, IFieldReferenceOperation
    {
        private IOperation _lazyInstance = s_unset;
        internal LazyFieldReferenceOperation(IFieldSymbol field, bool isDeclaration, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(field, isDeclaration, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateInstance();
        public override IOperation Instance
        {
            get
            {
                if (_lazyInstance == s_unset)
                {
                    IOperation instance = CreateInstance();
                    SetParentOperation(instance, this);
                    Interlocked.CompareExchange(ref _lazyInstance, instance, s_unset);
                }
                return _lazyInstance;
            }
        }
    }
    internal abstract partial class BaseMethodReferenceOperation : BaseMemberReferenceOperation, IMethodReferenceOperation
    {
        internal BaseMethodReferenceOperation(IMethodSymbol method, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.MethodReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Method = method;
            IsVirtual = isVirtual;
        }
        public IMethodSymbol Method { get; }
        public bool IsVirtual { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Instance is object) yield return Instance;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitMethodReference(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitMethodReference(this, argument);
    }
    internal sealed partial class MethodReferenceOperation : BaseMethodReferenceOperation, IMethodReferenceOperation
    {
        internal MethodReferenceOperation(IMethodSymbol method, bool isVirtual, IOperation instance, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(method, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Instance = SetParentOperation(instance, this);
        }
        public override IOperation Instance { get; }
    }
    internal abstract partial class LazyMethodReferenceOperation : BaseMethodReferenceOperation, IMethodReferenceOperation
    {
        private IOperation _lazyInstance = s_unset;
        internal LazyMethodReferenceOperation(IMethodSymbol method, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(method, isVirtual, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateInstance();
        public override IOperation Instance
        {
            get
            {
                if (_lazyInstance == s_unset)
                {
                    IOperation instance = CreateInstance();
                    SetParentOperation(instance, this);
                    Interlocked.CompareExchange(ref _lazyInstance, instance, s_unset);
                }
                return _lazyInstance;
            }
        }
    }
    internal abstract partial class BasePropertyReferenceOperation : BaseMemberReferenceOperation, IPropertyReferenceOperation
    {
        internal BasePropertyReferenceOperation(IPropertySymbol property, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.PropertyReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Property = property;
        }
        public IPropertySymbol Property { get; }
        public abstract ImmutableArray<IArgumentOperation> Arguments { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Instance is object) yield return Instance;
                foreach (var child in Arguments)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitPropertyReference(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitPropertyReference(this, argument);
    }
    internal sealed partial class PropertyReferenceOperation : BasePropertyReferenceOperation, IPropertyReferenceOperation
    {
        internal PropertyReferenceOperation(IPropertySymbol property, ImmutableArray<IArgumentOperation> arguments, IOperation instance, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(property, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Arguments = SetParentOperation(arguments, this);
            Instance = SetParentOperation(instance, this);
        }
        public override ImmutableArray<IArgumentOperation> Arguments { get; }
        public override IOperation Instance { get; }
    }
    internal abstract partial class LazyPropertyReferenceOperation : BasePropertyReferenceOperation, IPropertyReferenceOperation
    {
        private ImmutableArray<IArgumentOperation> _lazyArguments;
        private IOperation _lazyInstance = s_unset;
        internal LazyPropertyReferenceOperation(IPropertySymbol property, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(property, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IArgumentOperation> CreateArguments();
        public override ImmutableArray<IArgumentOperation> Arguments
        {
            get
            {
                if (_lazyArguments.IsDefault)
                {
                    ImmutableArray<IArgumentOperation> arguments = CreateArguments();
                    SetParentOperation(arguments, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyArguments, arguments);
                }
                return _lazyArguments;
            }
        }
        protected abstract IOperation CreateInstance();
        public override IOperation Instance
        {
            get
            {
                if (_lazyInstance == s_unset)
                {
                    IOperation instance = CreateInstance();
                    SetParentOperation(instance, this);
                    Interlocked.CompareExchange(ref _lazyInstance, instance, s_unset);
                }
                return _lazyInstance;
            }
        }
    }
    internal abstract partial class BaseEventReferenceOperation : BaseMemberReferenceOperation, IEventReferenceOperation
    {
        internal BaseEventReferenceOperation(IEventSymbol @event, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.EventReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Event = @event;
        }
        public IEventSymbol Event { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Instance is object) yield return Instance;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitEventReference(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitEventReference(this, argument);
    }
    internal sealed partial class EventReferenceOperation : BaseEventReferenceOperation, IEventReferenceOperation
    {
        internal EventReferenceOperation(IEventSymbol @event, IOperation instance, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(@event, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Instance = SetParentOperation(instance, this);
        }
        public override IOperation Instance { get; }
    }
    internal abstract partial class LazyEventReferenceOperation : BaseEventReferenceOperation, IEventReferenceOperation
    {
        private IOperation _lazyInstance = s_unset;
        internal LazyEventReferenceOperation(IEventSymbol @event, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(@event, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateInstance();
        public override IOperation Instance
        {
            get
            {
                if (_lazyInstance == s_unset)
                {
                    IOperation instance = CreateInstance();
                    SetParentOperation(instance, this);
                    Interlocked.CompareExchange(ref _lazyInstance, instance, s_unset);
                }
                return _lazyInstance;
            }
        }
    }
    internal abstract partial class BaseUnaryOperation : Operation, IUnaryOperation
    {
        internal BaseUnaryOperation(UnaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Unary, semanticModel, syntax, type, constantValue, isImplicit)
        {
            OperatorKind = operatorKind;
            IsLifted = isLifted;
            IsChecked = isChecked;
            OperatorMethod = operatorMethod;
        }
        public UnaryOperatorKind OperatorKind { get; }
        public abstract IOperation Operand { get; }
        public bool IsLifted { get; }
        public bool IsChecked { get; }
        public IMethodSymbol OperatorMethod { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operand is object) yield return Operand;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitUnaryOperator(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitUnaryOperator(this, argument);
    }
    internal sealed partial class UnaryOperation : BaseUnaryOperation, IUnaryOperation
    {
        internal UnaryOperation(UnaryOperatorKind operatorKind, IOperation operand, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Operand = SetParentOperation(operand, this);
        }
        public override IOperation Operand { get; }
    }
    internal abstract partial class LazyUnaryOperation : BaseUnaryOperation, IUnaryOperation
    {
        private IOperation _lazyOperand = s_unset;
        internal LazyUnaryOperation(UnaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateOperand();
        public override IOperation Operand
        {
            get
            {
                if (_lazyOperand == s_unset)
                {
                    IOperation operand = CreateOperand();
                    SetParentOperation(operand, this);
                    Interlocked.CompareExchange(ref _lazyOperand, operand, s_unset);
                }
                return _lazyOperand;
            }
        }
    }
    internal abstract partial class BaseBinaryOperation : Operation, IBinaryOperation
    {
        internal BaseBinaryOperation(BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, bool isCompareText, IMethodSymbol operatorMethod, IMethodSymbol unaryOperatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Binary, semanticModel, syntax, type, constantValue, isImplicit)
        {
            OperatorKind = operatorKind;
            IsLifted = isLifted;
            IsChecked = isChecked;
            IsCompareText = isCompareText;
            OperatorMethod = operatorMethod;
            UnaryOperatorMethod = unaryOperatorMethod;
        }
        public BinaryOperatorKind OperatorKind { get; }
        public abstract IOperation LeftOperand { get; }
        public abstract IOperation RightOperand { get; }
        public bool IsLifted { get; }
        public bool IsChecked { get; }
        public bool IsCompareText { get; }
        public IMethodSymbol OperatorMethod { get; }
        public IMethodSymbol UnaryOperatorMethod { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (LeftOperand is object) yield return LeftOperand;
                if (RightOperand is object) yield return RightOperand;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitBinaryOperator(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitBinaryOperator(this, argument);
    }
    internal sealed partial class BinaryOperation : BaseBinaryOperation, IBinaryOperation
    {
        internal BinaryOperation(BinaryOperatorKind operatorKind, IOperation leftOperand, IOperation rightOperand, bool isLifted, bool isChecked, bool isCompareText, IMethodSymbol operatorMethod, IMethodSymbol unaryOperatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(operatorKind, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LeftOperand = SetParentOperation(leftOperand, this);
            RightOperand = SetParentOperation(rightOperand, this);
        }
        public override IOperation LeftOperand { get; }
        public override IOperation RightOperand { get; }
    }
    internal abstract partial class LazyBinaryOperation : BaseBinaryOperation, IBinaryOperation
    {
        private IOperation _lazyLeftOperand = s_unset;
        private IOperation _lazyRightOperand = s_unset;
        internal LazyBinaryOperation(BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, bool isCompareText, IMethodSymbol operatorMethod, IMethodSymbol unaryOperatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(operatorKind, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateLeftOperand();
        public override IOperation LeftOperand
        {
            get
            {
                if (_lazyLeftOperand == s_unset)
                {
                    IOperation leftOperand = CreateLeftOperand();
                    SetParentOperation(leftOperand, this);
                    Interlocked.CompareExchange(ref _lazyLeftOperand, leftOperand, s_unset);
                }
                return _lazyLeftOperand;
            }
        }
        protected abstract IOperation CreateRightOperand();
        public override IOperation RightOperand
        {
            get
            {
                if (_lazyRightOperand == s_unset)
                {
                    IOperation rightOperand = CreateRightOperand();
                    SetParentOperation(rightOperand, this);
                    Interlocked.CompareExchange(ref _lazyRightOperand, rightOperand, s_unset);
                }
                return _lazyRightOperand;
            }
        }
    }
    internal abstract partial class BaseConditionalOperation : Operation, IConditionalOperation
    {
        internal BaseConditionalOperation(bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Conditional, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsRef = isRef;
        }
        public abstract IOperation Condition { get; }
        public abstract IOperation WhenTrue { get; }
        public abstract IOperation WhenFalse { get; }
        public bool IsRef { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Condition is object) yield return Condition;
                if (WhenTrue is object) yield return WhenTrue;
                if (WhenFalse is object) yield return WhenFalse;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitConditional(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitConditional(this, argument);
    }
    internal sealed partial class ConditionalOperation : BaseConditionalOperation, IConditionalOperation
    {
        internal ConditionalOperation(IOperation condition, IOperation whenTrue, IOperation whenFalse, bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Condition = SetParentOperation(condition, this);
            WhenTrue = SetParentOperation(whenTrue, this);
            WhenFalse = SetParentOperation(whenFalse, this);
        }
        public override IOperation Condition { get; }
        public override IOperation WhenTrue { get; }
        public override IOperation WhenFalse { get; }
    }
    internal abstract partial class LazyConditionalOperation : BaseConditionalOperation, IConditionalOperation
    {
        private IOperation _lazyCondition = s_unset;
        private IOperation _lazyWhenTrue = s_unset;
        private IOperation _lazyWhenFalse = s_unset;
        internal LazyConditionalOperation(bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isRef, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateCondition();
        public override IOperation Condition
        {
            get
            {
                if (_lazyCondition == s_unset)
                {
                    IOperation condition = CreateCondition();
                    SetParentOperation(condition, this);
                    Interlocked.CompareExchange(ref _lazyCondition, condition, s_unset);
                }
                return _lazyCondition;
            }
        }
        protected abstract IOperation CreateWhenTrue();
        public override IOperation WhenTrue
        {
            get
            {
                if (_lazyWhenTrue == s_unset)
                {
                    IOperation whenTrue = CreateWhenTrue();
                    SetParentOperation(whenTrue, this);
                    Interlocked.CompareExchange(ref _lazyWhenTrue, whenTrue, s_unset);
                }
                return _lazyWhenTrue;
            }
        }
        protected abstract IOperation CreateWhenFalse();
        public override IOperation WhenFalse
        {
            get
            {
                if (_lazyWhenFalse == s_unset)
                {
                    IOperation whenFalse = CreateWhenFalse();
                    SetParentOperation(whenFalse, this);
                    Interlocked.CompareExchange(ref _lazyWhenFalse, whenFalse, s_unset);
                }
                return _lazyWhenFalse;
            }
        }
    }
    internal abstract partial class BaseCoalesceOperation : Operation, ICoalesceOperation
    {
        internal BaseCoalesceOperation(IConvertibleConversion valueConversion, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Coalesce, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueConversionConvertible = valueConversion;
        }
        public abstract IOperation Value { get; }
        public abstract IOperation WhenNull { get; }
        internal IConvertibleConversion ValueConversionConvertible { get; }
        public CommonConversion ValueConversion => ValueConversionConvertible.ToCommonConversion();
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
                if (WhenNull is object) yield return WhenNull;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitCoalesce(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitCoalesce(this, argument);
    }
    internal sealed partial class CoalesceOperation : BaseCoalesceOperation, ICoalesceOperation
    {
        internal CoalesceOperation(IOperation value, IOperation whenNull, IConvertibleConversion valueConversion, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(valueConversion, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Value = SetParentOperation(value, this);
            WhenNull = SetParentOperation(whenNull, this);
        }
        public override IOperation Value { get; }
        public override IOperation WhenNull { get; }
    }
    internal abstract partial class LazyCoalesceOperation : BaseCoalesceOperation, ICoalesceOperation
    {
        private IOperation _lazyValue = s_unset;
        private IOperation _lazyWhenNull = s_unset;
        internal LazyCoalesceOperation(IConvertibleConversion valueConversion, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(valueConversion, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
        protected abstract IOperation CreateWhenNull();
        public override IOperation WhenNull
        {
            get
            {
                if (_lazyWhenNull == s_unset)
                {
                    IOperation whenNull = CreateWhenNull();
                    SetParentOperation(whenNull, this);
                    Interlocked.CompareExchange(ref _lazyWhenNull, whenNull, s_unset);
                }
                return _lazyWhenNull;
            }
        }
    }
    internal abstract partial class BaseAnonymousFunctionOperation : Operation, IAnonymousFunctionOperation
    {
        internal BaseAnonymousFunctionOperation(IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.AnonymousFunction, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Symbol = symbol;
        }
        public IMethodSymbol Symbol { get; }
        public abstract IBlockOperation Body { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Body is object) yield return Body;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitAnonymousFunction(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitAnonymousFunction(this, argument);
    }
    internal sealed partial class AnonymousFunctionOperation : BaseAnonymousFunctionOperation, IAnonymousFunctionOperation
    {
        internal AnonymousFunctionOperation(IMethodSymbol symbol, IBlockOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Body = SetParentOperation(body, this);
        }
        public override IBlockOperation Body { get; }
    }
    internal abstract partial class LazyAnonymousFunctionOperation : BaseAnonymousFunctionOperation, IAnonymousFunctionOperation
    {
        private IBlockOperation _lazyBody = s_unsetBlock;
        internal LazyAnonymousFunctionOperation(IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(symbol, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IBlockOperation CreateBody();
        public override IBlockOperation Body
        {
            get
            {
                if (_lazyBody == s_unsetBlock)
                {
                    IBlockOperation body = CreateBody();
                    SetParentOperation(body, this);
                    Interlocked.CompareExchange(ref _lazyBody, body, s_unsetBlock);
                }
                return _lazyBody;
            }
        }
    }
    internal abstract partial class BaseObjectCreationOperation : Operation, IObjectCreationOperation
    {
        internal BaseObjectCreationOperation(IMethodSymbol constructor, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.ObjectCreation, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Constructor = constructor;
        }
        public IMethodSymbol Constructor { get; }
        public abstract IObjectOrCollectionInitializerOperation Initializer { get; }
        public abstract ImmutableArray<IArgumentOperation> Arguments { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in Arguments)
                {
                    if (child is object) yield return child;
                }
                if (Initializer is object) yield return Initializer;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitObjectCreation(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitObjectCreation(this, argument);
    }
    internal sealed partial class ObjectCreationOperation : BaseObjectCreationOperation, IObjectCreationOperation
    {
        internal ObjectCreationOperation(IMethodSymbol constructor, IObjectOrCollectionInitializerOperation initializer, ImmutableArray<IArgumentOperation> arguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(constructor, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Initializer = SetParentOperation(initializer, this);
            Arguments = SetParentOperation(arguments, this);
        }
        public override IObjectOrCollectionInitializerOperation Initializer { get; }
        public override ImmutableArray<IArgumentOperation> Arguments { get; }
    }
    internal abstract partial class LazyObjectCreationOperation : BaseObjectCreationOperation, IObjectCreationOperation
    {
        private IObjectOrCollectionInitializerOperation _lazyInitializer = s_unsetObjectOrCollectionInitializer;
        private ImmutableArray<IArgumentOperation> _lazyArguments;
        internal LazyObjectCreationOperation(IMethodSymbol constructor, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(constructor, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IObjectOrCollectionInitializerOperation CreateInitializer();
        public override IObjectOrCollectionInitializerOperation Initializer
        {
            get
            {
                if (_lazyInitializer == s_unsetObjectOrCollectionInitializer)
                {
                    IObjectOrCollectionInitializerOperation initializer = CreateInitializer();
                    SetParentOperation(initializer, this);
                    Interlocked.CompareExchange(ref _lazyInitializer, initializer, s_unsetObjectOrCollectionInitializer);
                }
                return _lazyInitializer;
            }
        }
        protected abstract ImmutableArray<IArgumentOperation> CreateArguments();
        public override ImmutableArray<IArgumentOperation> Arguments
        {
            get
            {
                if (_lazyArguments.IsDefault)
                {
                    ImmutableArray<IArgumentOperation> arguments = CreateArguments();
                    SetParentOperation(arguments, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyArguments, arguments);
                }
                return _lazyArguments;
            }
        }
    }
    internal abstract partial class BaseTypeParameterObjectCreationOperation : Operation, ITypeParameterObjectCreationOperation
    {
        internal BaseTypeParameterObjectCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.TypeParameterObjectCreation, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IObjectOrCollectionInitializerOperation Initializer { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Initializer is object) yield return Initializer;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitTypeParameterObjectCreation(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitTypeParameterObjectCreation(this, argument);
    }
    internal sealed partial class TypeParameterObjectCreationOperation : BaseTypeParameterObjectCreationOperation, ITypeParameterObjectCreationOperation
    {
        internal TypeParameterObjectCreationOperation(IObjectOrCollectionInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Initializer = SetParentOperation(initializer, this);
        }
        public override IObjectOrCollectionInitializerOperation Initializer { get; }
    }
    internal abstract partial class LazyTypeParameterObjectCreationOperation : BaseTypeParameterObjectCreationOperation, ITypeParameterObjectCreationOperation
    {
        private IObjectOrCollectionInitializerOperation _lazyInitializer = s_unsetObjectOrCollectionInitializer;
        internal LazyTypeParameterObjectCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IObjectOrCollectionInitializerOperation CreateInitializer();
        public override IObjectOrCollectionInitializerOperation Initializer
        {
            get
            {
                if (_lazyInitializer == s_unsetObjectOrCollectionInitializer)
                {
                    IObjectOrCollectionInitializerOperation initializer = CreateInitializer();
                    SetParentOperation(initializer, this);
                    Interlocked.CompareExchange(ref _lazyInitializer, initializer, s_unsetObjectOrCollectionInitializer);
                }
                return _lazyInitializer;
            }
        }
    }
    internal abstract partial class BaseArrayCreationOperation : Operation, IArrayCreationOperation
    {
        internal BaseArrayCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.ArrayCreation, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract ImmutableArray<IOperation> DimensionSizes { get; }
        public abstract IArrayInitializerOperation Initializer { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in DimensionSizes)
                {
                    if (child is object) yield return child;
                }
                if (Initializer is object) yield return Initializer;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitArrayCreation(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitArrayCreation(this, argument);
    }
    internal sealed partial class ArrayCreationOperation : BaseArrayCreationOperation, IArrayCreationOperation
    {
        internal ArrayCreationOperation(ImmutableArray<IOperation> dimensionSizes, IArrayInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            DimensionSizes = SetParentOperation(dimensionSizes, this);
            Initializer = SetParentOperation(initializer, this);
        }
        public override ImmutableArray<IOperation> DimensionSizes { get; }
        public override IArrayInitializerOperation Initializer { get; }
    }
    internal abstract partial class LazyArrayCreationOperation : BaseArrayCreationOperation, IArrayCreationOperation
    {
        private ImmutableArray<IOperation> _lazyDimensionSizes;
        private IArrayInitializerOperation _lazyInitializer = s_unsetArrayInitializer;
        internal LazyArrayCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IOperation> CreateDimensionSizes();
        public override ImmutableArray<IOperation> DimensionSizes
        {
            get
            {
                if (_lazyDimensionSizes.IsDefault)
                {
                    ImmutableArray<IOperation> dimensionSizes = CreateDimensionSizes();
                    SetParentOperation(dimensionSizes, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyDimensionSizes, dimensionSizes);
                }
                return _lazyDimensionSizes;
            }
        }
        protected abstract IArrayInitializerOperation CreateInitializer();
        public override IArrayInitializerOperation Initializer
        {
            get
            {
                if (_lazyInitializer == s_unsetArrayInitializer)
                {
                    IArrayInitializerOperation initializer = CreateInitializer();
                    SetParentOperation(initializer, this);
                    Interlocked.CompareExchange(ref _lazyInitializer, initializer, s_unsetArrayInitializer);
                }
                return _lazyInitializer;
            }
        }
    }
    internal sealed partial class InstanceReferenceOperation : Operation, IInstanceReferenceOperation
    {
        internal InstanceReferenceOperation(InstanceReferenceKind referenceKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.InstanceReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ReferenceKind = referenceKind;
        }
        public InstanceReferenceKind ReferenceKind { get; }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitInstanceReference(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitInstanceReference(this, argument);
    }
    internal abstract partial class BaseIsTypeOperation : Operation, IIsTypeOperation
    {
        internal BaseIsTypeOperation(ITypeSymbol typeOperand, bool isNegated, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.IsType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TypeOperand = typeOperand;
            IsNegated = isNegated;
        }
        public abstract IOperation ValueOperand { get; }
        public ITypeSymbol TypeOperand { get; }
        public bool IsNegated { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (ValueOperand is object) yield return ValueOperand;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitIsType(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitIsType(this, argument);
    }
    internal sealed partial class IsTypeOperation : BaseIsTypeOperation, IIsTypeOperation
    {
        internal IsTypeOperation(IOperation valueOperand, ITypeSymbol typeOperand, bool isNegated, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(typeOperand, isNegated, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueOperand = SetParentOperation(valueOperand, this);
        }
        public override IOperation ValueOperand { get; }
    }
    internal abstract partial class LazyIsTypeOperation : BaseIsTypeOperation, IIsTypeOperation
    {
        private IOperation _lazyValueOperand = s_unset;
        internal LazyIsTypeOperation(ITypeSymbol typeOperand, bool isNegated, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(typeOperand, isNegated, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValueOperand();
        public override IOperation ValueOperand
        {
            get
            {
                if (_lazyValueOperand == s_unset)
                {
                    IOperation valueOperand = CreateValueOperand();
                    SetParentOperation(valueOperand, this);
                    Interlocked.CompareExchange(ref _lazyValueOperand, valueOperand, s_unset);
                }
                return _lazyValueOperand;
            }
        }
    }
    internal abstract partial class BaseAwaitOperation : Operation, IAwaitOperation
    {
        internal BaseAwaitOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Await, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Operation { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation is object) yield return Operation;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitAwait(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitAwait(this, argument);
    }
    internal sealed partial class AwaitOperation : BaseAwaitOperation, IAwaitOperation
    {
        internal AwaitOperation(IOperation operation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
        }
        public override IOperation Operation { get; }
    }
    internal abstract partial class LazyAwaitOperation : BaseAwaitOperation, IAwaitOperation
    {
        private IOperation _lazyOperation = s_unset;
        internal LazyAwaitOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateOperation();
        public override IOperation Operation
        {
            get
            {
                if (_lazyOperation == s_unset)
                {
                    IOperation operation = CreateOperation();
                    SetParentOperation(operation, this);
                    Interlocked.CompareExchange(ref _lazyOperation, operation, s_unset);
                }
                return _lazyOperation;
            }
        }
    }
    internal abstract partial class BaseAssignmentOperation : Operation, IAssignmentOperation
    {
        protected BaseAssignmentOperation(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(kind, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Target { get; }
        public abstract IOperation Value { get; }
    }
    internal abstract partial class BaseSimpleAssignmentOperation : BaseAssignmentOperation, ISimpleAssignmentOperation
    {
        internal BaseSimpleAssignmentOperation(bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.SimpleAssignment, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsRef = isRef;
        }
        public bool IsRef { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Target is object) yield return Target;
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitSimpleAssignment(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitSimpleAssignment(this, argument);
    }
    internal sealed partial class SimpleAssignmentOperation : BaseSimpleAssignmentOperation, ISimpleAssignmentOperation
    {
        internal SimpleAssignmentOperation(bool isRef, IOperation target, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Target = SetParentOperation(target, this);
            Value = SetParentOperation(value, this);
        }
        public override IOperation Target { get; }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazySimpleAssignmentOperation : BaseSimpleAssignmentOperation, ISimpleAssignmentOperation
    {
        private IOperation _lazyTarget = s_unset;
        private IOperation _lazyValue = s_unset;
        internal LazySimpleAssignmentOperation(bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isRef, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateTarget();
        public override IOperation Target
        {
            get
            {
                if (_lazyTarget == s_unset)
                {
                    IOperation target = CreateTarget();
                    SetParentOperation(target, this);
                    Interlocked.CompareExchange(ref _lazyTarget, target, s_unset);
                }
                return _lazyTarget;
            }
        }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BaseCompoundAssignmentOperation : BaseAssignmentOperation, ICompoundAssignmentOperation
    {
        internal BaseCompoundAssignmentOperation(IConvertibleConversion inConversion, IConvertibleConversion outConversion, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.CompoundAssignment, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InConversionConvertible = inConversion;
            OutConversionConvertible = outConversion;
            OperatorKind = operatorKind;
            IsLifted = isLifted;
            IsChecked = isChecked;
            OperatorMethod = operatorMethod;
        }
        internal IConvertibleConversion InConversionConvertible { get; }
        public CommonConversion InConversion => InConversionConvertible.ToCommonConversion();
        internal IConvertibleConversion OutConversionConvertible { get; }
        public CommonConversion OutConversion => OutConversionConvertible.ToCommonConversion();
        public BinaryOperatorKind OperatorKind { get; }
        public bool IsLifted { get; }
        public bool IsChecked { get; }
        public IMethodSymbol OperatorMethod { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Target is object) yield return Target;
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitCompoundAssignment(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitCompoundAssignment(this, argument);
    }
    internal sealed partial class CompoundAssignmentOperation : BaseCompoundAssignmentOperation, ICompoundAssignmentOperation
    {
        internal CompoundAssignmentOperation(IConvertibleConversion inConversion, IConvertibleConversion outConversion, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, IOperation target, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(inConversion, outConversion, operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Target = SetParentOperation(target, this);
            Value = SetParentOperation(value, this);
        }
        public override IOperation Target { get; }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazyCompoundAssignmentOperation : BaseCompoundAssignmentOperation, ICompoundAssignmentOperation
    {
        private IOperation _lazyTarget = s_unset;
        private IOperation _lazyValue = s_unset;
        internal LazyCompoundAssignmentOperation(IConvertibleConversion inConversion, IConvertibleConversion outConversion, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(inConversion, outConversion, operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateTarget();
        public override IOperation Target
        {
            get
            {
                if (_lazyTarget == s_unset)
                {
                    IOperation target = CreateTarget();
                    SetParentOperation(target, this);
                    Interlocked.CompareExchange(ref _lazyTarget, target, s_unset);
                }
                return _lazyTarget;
            }
        }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BaseParenthesizedOperation : Operation, IParenthesizedOperation
    {
        internal BaseParenthesizedOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Parenthesized, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Operand { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operand is object) yield return Operand;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitParenthesized(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitParenthesized(this, argument);
    }
    internal sealed partial class ParenthesizedOperation : BaseParenthesizedOperation, IParenthesizedOperation
    {
        internal ParenthesizedOperation(IOperation operand, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Operand = SetParentOperation(operand, this);
        }
        public override IOperation Operand { get; }
    }
    internal abstract partial class LazyParenthesizedOperation : BaseParenthesizedOperation, IParenthesizedOperation
    {
        private IOperation _lazyOperand = s_unset;
        internal LazyParenthesizedOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateOperand();
        public override IOperation Operand
        {
            get
            {
                if (_lazyOperand == s_unset)
                {
                    IOperation operand = CreateOperand();
                    SetParentOperation(operand, this);
                    Interlocked.CompareExchange(ref _lazyOperand, operand, s_unset);
                }
                return _lazyOperand;
            }
        }
    }
    internal abstract partial class BaseEventAssignmentOperation : Operation, IEventAssignmentOperation
    {
        internal BaseEventAssignmentOperation(bool adds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.EventAssignment, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Adds = adds;
        }
        public abstract IOperation EventReference { get; }
        public abstract IOperation HandlerValue { get; }
        public bool Adds { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (EventReference is object) yield return EventReference;
                if (HandlerValue is object) yield return HandlerValue;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitEventAssignment(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitEventAssignment(this, argument);
    }
    internal sealed partial class EventAssignmentOperation : BaseEventAssignmentOperation, IEventAssignmentOperation
    {
        internal EventAssignmentOperation(IOperation eventReference, IOperation handlerValue, bool adds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(adds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            EventReference = SetParentOperation(eventReference, this);
            HandlerValue = SetParentOperation(handlerValue, this);
        }
        public override IOperation EventReference { get; }
        public override IOperation HandlerValue { get; }
    }
    internal abstract partial class LazyEventAssignmentOperation : BaseEventAssignmentOperation, IEventAssignmentOperation
    {
        private IOperation _lazyEventReference = s_unset;
        private IOperation _lazyHandlerValue = s_unset;
        internal LazyEventAssignmentOperation(bool adds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(adds, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateEventReference();
        public override IOperation EventReference
        {
            get
            {
                if (_lazyEventReference == s_unset)
                {
                    IOperation eventReference = CreateEventReference();
                    SetParentOperation(eventReference, this);
                    Interlocked.CompareExchange(ref _lazyEventReference, eventReference, s_unset);
                }
                return _lazyEventReference;
            }
        }
        protected abstract IOperation CreateHandlerValue();
        public override IOperation HandlerValue
        {
            get
            {
                if (_lazyHandlerValue == s_unset)
                {
                    IOperation handlerValue = CreateHandlerValue();
                    SetParentOperation(handlerValue, this);
                    Interlocked.CompareExchange(ref _lazyHandlerValue, handlerValue, s_unset);
                }
                return _lazyHandlerValue;
            }
        }
    }
    internal abstract partial class BaseConditionalAccessOperation : Operation, IConditionalAccessOperation
    {
        internal BaseConditionalAccessOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.ConditionalAccess, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Operation { get; }
        public abstract IOperation WhenNotNull { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation is object) yield return Operation;
                if (WhenNotNull is object) yield return WhenNotNull;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitConditionalAccess(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitConditionalAccess(this, argument);
    }
    internal sealed partial class ConditionalAccessOperation : BaseConditionalAccessOperation, IConditionalAccessOperation
    {
        internal ConditionalAccessOperation(IOperation operation, IOperation whenNotNull, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
            WhenNotNull = SetParentOperation(whenNotNull, this);
        }
        public override IOperation Operation { get; }
        public override IOperation WhenNotNull { get; }
    }
    internal abstract partial class LazyConditionalAccessOperation : BaseConditionalAccessOperation, IConditionalAccessOperation
    {
        private IOperation _lazyOperation = s_unset;
        private IOperation _lazyWhenNotNull = s_unset;
        internal LazyConditionalAccessOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateOperation();
        public override IOperation Operation
        {
            get
            {
                if (_lazyOperation == s_unset)
                {
                    IOperation operation = CreateOperation();
                    SetParentOperation(operation, this);
                    Interlocked.CompareExchange(ref _lazyOperation, operation, s_unset);
                }
                return _lazyOperation;
            }
        }
        protected abstract IOperation CreateWhenNotNull();
        public override IOperation WhenNotNull
        {
            get
            {
                if (_lazyWhenNotNull == s_unset)
                {
                    IOperation whenNotNull = CreateWhenNotNull();
                    SetParentOperation(whenNotNull, this);
                    Interlocked.CompareExchange(ref _lazyWhenNotNull, whenNotNull, s_unset);
                }
                return _lazyWhenNotNull;
            }
        }
    }
    internal sealed partial class ConditionalAccessInstanceOperation : Operation, IConditionalAccessInstanceOperation
    {
        internal ConditionalAccessInstanceOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.ConditionalAccessInstance, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitConditionalAccessInstance(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitConditionalAccessInstance(this, argument);
    }
    internal abstract partial class BaseInterpolatedStringOperation : Operation, IInterpolatedStringOperation
    {
        internal BaseInterpolatedStringOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.InterpolatedString, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract ImmutableArray<IInterpolatedStringContentOperation> Parts { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in Parts)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitInterpolatedString(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitInterpolatedString(this, argument);
    }
    internal sealed partial class InterpolatedStringOperation : BaseInterpolatedStringOperation, IInterpolatedStringOperation
    {
        internal InterpolatedStringOperation(ImmutableArray<IInterpolatedStringContentOperation> parts, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Parts = SetParentOperation(parts, this);
        }
        public override ImmutableArray<IInterpolatedStringContentOperation> Parts { get; }
    }
    internal abstract partial class LazyInterpolatedStringOperation : BaseInterpolatedStringOperation, IInterpolatedStringOperation
    {
        private ImmutableArray<IInterpolatedStringContentOperation> _lazyParts;
        internal LazyInterpolatedStringOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IInterpolatedStringContentOperation> CreateParts();
        public override ImmutableArray<IInterpolatedStringContentOperation> Parts
        {
            get
            {
                if (_lazyParts.IsDefault)
                {
                    ImmutableArray<IInterpolatedStringContentOperation> parts = CreateParts();
                    SetParentOperation(parts, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyParts, parts);
                }
                return _lazyParts;
            }
        }
    }
    internal abstract partial class BaseAnonymousObjectCreationOperation : Operation, IAnonymousObjectCreationOperation
    {
        internal BaseAnonymousObjectCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.AnonymousObjectCreation, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract ImmutableArray<IOperation> Initializers { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in Initializers)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitAnonymousObjectCreation(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitAnonymousObjectCreation(this, argument);
    }
    internal sealed partial class AnonymousObjectCreationOperation : BaseAnonymousObjectCreationOperation, IAnonymousObjectCreationOperation
    {
        internal AnonymousObjectCreationOperation(ImmutableArray<IOperation> initializers, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Initializers = SetParentOperation(initializers, this);
        }
        public override ImmutableArray<IOperation> Initializers { get; }
    }
    internal abstract partial class LazyAnonymousObjectCreationOperation : BaseAnonymousObjectCreationOperation, IAnonymousObjectCreationOperation
    {
        private ImmutableArray<IOperation> _lazyInitializers;
        internal LazyAnonymousObjectCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IOperation> CreateInitializers();
        public override ImmutableArray<IOperation> Initializers
        {
            get
            {
                if (_lazyInitializers.IsDefault)
                {
                    ImmutableArray<IOperation> initializers = CreateInitializers();
                    SetParentOperation(initializers, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyInitializers, initializers);
                }
                return _lazyInitializers;
            }
        }
    }
    internal abstract partial class BaseObjectOrCollectionInitializerOperation : Operation, IObjectOrCollectionInitializerOperation
    {
        internal BaseObjectOrCollectionInitializerOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.ObjectOrCollectionInitializer, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract ImmutableArray<IOperation> Initializers { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in Initializers)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitObjectOrCollectionInitializer(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitObjectOrCollectionInitializer(this, argument);
    }
    internal sealed partial class ObjectOrCollectionInitializerOperation : BaseObjectOrCollectionInitializerOperation, IObjectOrCollectionInitializerOperation
    {
        internal ObjectOrCollectionInitializerOperation(ImmutableArray<IOperation> initializers, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Initializers = SetParentOperation(initializers, this);
        }
        public override ImmutableArray<IOperation> Initializers { get; }
    }
    internal abstract partial class LazyObjectOrCollectionInitializerOperation : BaseObjectOrCollectionInitializerOperation, IObjectOrCollectionInitializerOperation
    {
        private ImmutableArray<IOperation> _lazyInitializers;
        internal LazyObjectOrCollectionInitializerOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IOperation> CreateInitializers();
        public override ImmutableArray<IOperation> Initializers
        {
            get
            {
                if (_lazyInitializers.IsDefault)
                {
                    ImmutableArray<IOperation> initializers = CreateInitializers();
                    SetParentOperation(initializers, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyInitializers, initializers);
                }
                return _lazyInitializers;
            }
        }
    }
    internal abstract partial class BaseMemberInitializerOperation : Operation, IMemberInitializerOperation
    {
        internal BaseMemberInitializerOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.MemberInitializer, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation InitializedMember { get; }
        public abstract IObjectOrCollectionInitializerOperation Initializer { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (InitializedMember is object) yield return InitializedMember;
                if (Initializer is object) yield return Initializer;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitMemberInitializer(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitMemberInitializer(this, argument);
    }
    internal sealed partial class MemberInitializerOperation : BaseMemberInitializerOperation, IMemberInitializerOperation
    {
        internal MemberInitializerOperation(IOperation initializedMember, IObjectOrCollectionInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializedMember = SetParentOperation(initializedMember, this);
            Initializer = SetParentOperation(initializer, this);
        }
        public override IOperation InitializedMember { get; }
        public override IObjectOrCollectionInitializerOperation Initializer { get; }
    }
    internal abstract partial class LazyMemberInitializerOperation : BaseMemberInitializerOperation, IMemberInitializerOperation
    {
        private IOperation _lazyInitializedMember = s_unset;
        private IObjectOrCollectionInitializerOperation _lazyInitializer = s_unsetObjectOrCollectionInitializer;
        internal LazyMemberInitializerOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateInitializedMember();
        public override IOperation InitializedMember
        {
            get
            {
                if (_lazyInitializedMember == s_unset)
                {
                    IOperation initializedMember = CreateInitializedMember();
                    SetParentOperation(initializedMember, this);
                    Interlocked.CompareExchange(ref _lazyInitializedMember, initializedMember, s_unset);
                }
                return _lazyInitializedMember;
            }
        }
        protected abstract IObjectOrCollectionInitializerOperation CreateInitializer();
        public override IObjectOrCollectionInitializerOperation Initializer
        {
            get
            {
                if (_lazyInitializer == s_unsetObjectOrCollectionInitializer)
                {
                    IObjectOrCollectionInitializerOperation initializer = CreateInitializer();
                    SetParentOperation(initializer, this);
                    Interlocked.CompareExchange(ref _lazyInitializer, initializer, s_unsetObjectOrCollectionInitializer);
                }
                return _lazyInitializer;
            }
        }
    }
    internal abstract partial class BaseNameOfOperation : Operation, INameOfOperation
    {
        internal BaseNameOfOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.NameOf, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Argument { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Argument is object) yield return Argument;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitNameOf(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitNameOf(this, argument);
    }
    internal sealed partial class NameOfOperation : BaseNameOfOperation, INameOfOperation
    {
        internal NameOfOperation(IOperation argument, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Argument = SetParentOperation(argument, this);
        }
        public override IOperation Argument { get; }
    }
    internal abstract partial class LazyNameOfOperation : BaseNameOfOperation, INameOfOperation
    {
        private IOperation _lazyArgument = s_unset;
        internal LazyNameOfOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateArgument();
        public override IOperation Argument
        {
            get
            {
                if (_lazyArgument == s_unset)
                {
                    IOperation argument = CreateArgument();
                    SetParentOperation(argument, this);
                    Interlocked.CompareExchange(ref _lazyArgument, argument, s_unset);
                }
                return _lazyArgument;
            }
        }
    }
    internal abstract partial class BaseTupleOperation : Operation, ITupleOperation
    {
        internal BaseTupleOperation(ITypeSymbol naturalType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Tuple, semanticModel, syntax, type, constantValue, isImplicit)
        {
            NaturalType = naturalType;
        }
        public abstract ImmutableArray<IOperation> Elements { get; }
        public ITypeSymbol NaturalType { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in Elements)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitTuple(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitTuple(this, argument);
    }
    internal sealed partial class TupleOperation : BaseTupleOperation, ITupleOperation
    {
        internal TupleOperation(ImmutableArray<IOperation> elements, ITypeSymbol naturalType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(naturalType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Elements = SetParentOperation(elements, this);
        }
        public override ImmutableArray<IOperation> Elements { get; }
    }
    internal abstract partial class LazyTupleOperation : BaseTupleOperation, ITupleOperation
    {
        private ImmutableArray<IOperation> _lazyElements;
        internal LazyTupleOperation(ITypeSymbol naturalType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(naturalType, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IOperation> CreateElements();
        public override ImmutableArray<IOperation> Elements
        {
            get
            {
                if (_lazyElements.IsDefault)
                {
                    ImmutableArray<IOperation> elements = CreateElements();
                    SetParentOperation(elements, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyElements, elements);
                }
                return _lazyElements;
            }
        }
    }
    internal abstract partial class BaseDynamicMemberReferenceOperation : Operation, IDynamicMemberReferenceOperation
    {
        internal BaseDynamicMemberReferenceOperation(string memberName, ImmutableArray<ITypeSymbol> typeArguments, ITypeSymbol containingType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.DynamicMemberReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
            MemberName = memberName;
            TypeArguments = typeArguments;
            ContainingType = containingType;
        }
        public abstract IOperation Instance { get; }
        public string MemberName { get; }
        public ImmutableArray<ITypeSymbol> TypeArguments { get; }
        public ITypeSymbol ContainingType { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Instance is object) yield return Instance;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitDynamicMemberReference(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitDynamicMemberReference(this, argument);
    }
    internal sealed partial class DynamicMemberReferenceOperation : BaseDynamicMemberReferenceOperation, IDynamicMemberReferenceOperation
    {
        internal DynamicMemberReferenceOperation(IOperation instance, string memberName, ImmutableArray<ITypeSymbol> typeArguments, ITypeSymbol containingType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(memberName, typeArguments, containingType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Instance = SetParentOperation(instance, this);
        }
        public override IOperation Instance { get; }
    }
    internal abstract partial class LazyDynamicMemberReferenceOperation : BaseDynamicMemberReferenceOperation, IDynamicMemberReferenceOperation
    {
        private IOperation _lazyInstance = s_unset;
        internal LazyDynamicMemberReferenceOperation(string memberName, ImmutableArray<ITypeSymbol> typeArguments, ITypeSymbol containingType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(memberName, typeArguments, containingType, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateInstance();
        public override IOperation Instance
        {
            get
            {
                if (_lazyInstance == s_unset)
                {
                    IOperation instance = CreateInstance();
                    SetParentOperation(instance, this);
                    Interlocked.CompareExchange(ref _lazyInstance, instance, s_unset);
                }
                return _lazyInstance;
            }
        }
    }
    internal abstract partial class BaseTranslatedQueryOperation : Operation, ITranslatedQueryOperation
    {
        internal BaseTranslatedQueryOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.TranslatedQuery, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Operation { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operation is object) yield return Operation;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitTranslatedQuery(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitTranslatedQuery(this, argument);
    }
    internal sealed partial class TranslatedQueryOperation : BaseTranslatedQueryOperation, ITranslatedQueryOperation
    {
        internal TranslatedQueryOperation(IOperation operation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Operation = SetParentOperation(operation, this);
        }
        public override IOperation Operation { get; }
    }
    internal abstract partial class LazyTranslatedQueryOperation : BaseTranslatedQueryOperation, ITranslatedQueryOperation
    {
        private IOperation _lazyOperation = s_unset;
        internal LazyTranslatedQueryOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateOperation();
        public override IOperation Operation
        {
            get
            {
                if (_lazyOperation == s_unset)
                {
                    IOperation operation = CreateOperation();
                    SetParentOperation(operation, this);
                    Interlocked.CompareExchange(ref _lazyOperation, operation, s_unset);
                }
                return _lazyOperation;
            }
        }
    }
    internal abstract partial class BaseDelegateCreationOperation : Operation, IDelegateCreationOperation
    {
        internal BaseDelegateCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.DelegateCreation, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Target { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Target is object) yield return Target;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitDelegateCreation(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitDelegateCreation(this, argument);
    }
    internal sealed partial class DelegateCreationOperation : BaseDelegateCreationOperation, IDelegateCreationOperation
    {
        internal DelegateCreationOperation(IOperation target, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Target = SetParentOperation(target, this);
        }
        public override IOperation Target { get; }
    }
    internal abstract partial class LazyDelegateCreationOperation : BaseDelegateCreationOperation, IDelegateCreationOperation
    {
        private IOperation _lazyTarget = s_unset;
        internal LazyDelegateCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateTarget();
        public override IOperation Target
        {
            get
            {
                if (_lazyTarget == s_unset)
                {
                    IOperation target = CreateTarget();
                    SetParentOperation(target, this);
                    Interlocked.CompareExchange(ref _lazyTarget, target, s_unset);
                }
                return _lazyTarget;
            }
        }
    }
    internal sealed partial class DefaultValueOperation : Operation, IDefaultValueOperation
    {
        internal DefaultValueOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.DefaultValue, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitDefaultValue(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitDefaultValue(this, argument);
    }
    internal sealed partial class TypeOfOperation : Operation, ITypeOfOperation
    {
        internal TypeOfOperation(ITypeSymbol typeOperand, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.TypeOf, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TypeOperand = typeOperand;
        }
        public ITypeSymbol TypeOperand { get; }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitTypeOf(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitTypeOf(this, argument);
    }
    internal sealed partial class SizeOfOperation : Operation, ISizeOfOperation
    {
        internal SizeOfOperation(ITypeSymbol typeOperand, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.SizeOf, semanticModel, syntax, type, constantValue, isImplicit)
        {
            TypeOperand = typeOperand;
        }
        public ITypeSymbol TypeOperand { get; }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitSizeOf(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitSizeOf(this, argument);
    }
    internal abstract partial class BaseAddressOfOperation : Operation, IAddressOfOperation
    {
        internal BaseAddressOfOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.AddressOf, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Reference { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Reference is object) yield return Reference;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitAddressOf(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitAddressOf(this, argument);
    }
    internal sealed partial class AddressOfOperation : BaseAddressOfOperation, IAddressOfOperation
    {
        internal AddressOfOperation(IOperation reference, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Reference = SetParentOperation(reference, this);
        }
        public override IOperation Reference { get; }
    }
    internal abstract partial class LazyAddressOfOperation : BaseAddressOfOperation, IAddressOfOperation
    {
        private IOperation _lazyReference = s_unset;
        internal LazyAddressOfOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateReference();
        public override IOperation Reference
        {
            get
            {
                if (_lazyReference == s_unset)
                {
                    IOperation reference = CreateReference();
                    SetParentOperation(reference, this);
                    Interlocked.CompareExchange(ref _lazyReference, reference, s_unset);
                }
                return _lazyReference;
            }
        }
    }
    internal abstract partial class BaseIsPatternOperation : Operation, IIsPatternOperation
    {
        internal BaseIsPatternOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.IsPattern, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Value { get; }
        public abstract IPatternOperation Pattern { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
                if (Pattern is object) yield return Pattern;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitIsPattern(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitIsPattern(this, argument);
    }
    internal sealed partial class IsPatternOperation : BaseIsPatternOperation, IIsPatternOperation
    {
        internal IsPatternOperation(IOperation value, IPatternOperation pattern, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Value = SetParentOperation(value, this);
            Pattern = SetParentOperation(pattern, this);
        }
        public override IOperation Value { get; }
        public override IPatternOperation Pattern { get; }
    }
    internal abstract partial class LazyIsPatternOperation : BaseIsPatternOperation, IIsPatternOperation
    {
        private IOperation _lazyValue = s_unset;
        private IPatternOperation _lazyPattern = s_unsetPattern;
        internal LazyIsPatternOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
        protected abstract IPatternOperation CreatePattern();
        public override IPatternOperation Pattern
        {
            get
            {
                if (_lazyPattern == s_unsetPattern)
                {
                    IPatternOperation pattern = CreatePattern();
                    SetParentOperation(pattern, this);
                    Interlocked.CompareExchange(ref _lazyPattern, pattern, s_unsetPattern);
                }
                return _lazyPattern;
            }
        }
    }
    internal abstract partial class BaseIncrementOrDecrementOperation : Operation, IIncrementOrDecrementOperation
    {
        internal BaseIncrementOrDecrementOperation(bool isPostfix, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsPostfix = isPostfix;
            IsLifted = isLifted;
            IsChecked = isChecked;
            OperatorMethod = operatorMethod;
        }
        public bool IsPostfix { get; }
        public bool IsLifted { get; }
        public bool IsChecked { get; }
        public abstract IOperation Target { get; }
        public IMethodSymbol OperatorMethod { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Target is object) yield return Target;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitIncrementOrDecrement(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitIncrementOrDecrement(this, argument);
    }
    internal sealed partial class IncrementOrDecrementOperation : BaseIncrementOrDecrementOperation, IIncrementOrDecrementOperation
    {
        internal IncrementOrDecrementOperation(bool isPostfix, bool isLifted, bool isChecked, IOperation target, IMethodSymbol operatorMethod, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isPostfix, isLifted, isChecked, operatorMethod, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Target = SetParentOperation(target, this);
        }
        public override IOperation Target { get; }
    }
    internal abstract partial class LazyIncrementOrDecrementOperation : BaseIncrementOrDecrementOperation, IIncrementOrDecrementOperation
    {
        private IOperation _lazyTarget = s_unset;
        internal LazyIncrementOrDecrementOperation(bool isPostfix, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isPostfix, isLifted, isChecked, operatorMethod, kind, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateTarget();
        public override IOperation Target
        {
            get
            {
                if (_lazyTarget == s_unset)
                {
                    IOperation target = CreateTarget();
                    SetParentOperation(target, this);
                    Interlocked.CompareExchange(ref _lazyTarget, target, s_unset);
                }
                return _lazyTarget;
            }
        }
    }
    internal abstract partial class BaseThrowOperation : Operation, IThrowOperation
    {
        internal BaseThrowOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Throw, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Exception { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Exception is object) yield return Exception;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitThrow(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitThrow(this, argument);
    }
    internal sealed partial class ThrowOperation : BaseThrowOperation, IThrowOperation
    {
        internal ThrowOperation(IOperation exception, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Exception = SetParentOperation(exception, this);
        }
        public override IOperation Exception { get; }
    }
    internal abstract partial class LazyThrowOperation : BaseThrowOperation, IThrowOperation
    {
        private IOperation _lazyException = s_unset;
        internal LazyThrowOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateException();
        public override IOperation Exception
        {
            get
            {
                if (_lazyException == s_unset)
                {
                    IOperation exception = CreateException();
                    SetParentOperation(exception, this);
                    Interlocked.CompareExchange(ref _lazyException, exception, s_unset);
                }
                return _lazyException;
            }
        }
    }
    internal abstract partial class BaseDeconstructionAssignmentOperation : BaseAssignmentOperation, IDeconstructionAssignmentOperation
    {
        internal BaseDeconstructionAssignmentOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.DeconstructionAssignment, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Target is object) yield return Target;
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitDeconstructionAssignment(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitDeconstructionAssignment(this, argument);
    }
    internal sealed partial class DeconstructionAssignmentOperation : BaseDeconstructionAssignmentOperation, IDeconstructionAssignmentOperation
    {
        internal DeconstructionAssignmentOperation(IOperation target, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Target = SetParentOperation(target, this);
            Value = SetParentOperation(value, this);
        }
        public override IOperation Target { get; }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazyDeconstructionAssignmentOperation : BaseDeconstructionAssignmentOperation, IDeconstructionAssignmentOperation
    {
        private IOperation _lazyTarget = s_unset;
        private IOperation _lazyValue = s_unset;
        internal LazyDeconstructionAssignmentOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateTarget();
        public override IOperation Target
        {
            get
            {
                if (_lazyTarget == s_unset)
                {
                    IOperation target = CreateTarget();
                    SetParentOperation(target, this);
                    Interlocked.CompareExchange(ref _lazyTarget, target, s_unset);
                }
                return _lazyTarget;
            }
        }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BaseDeclarationExpressionOperation : Operation, IDeclarationExpressionOperation
    {
        internal BaseDeclarationExpressionOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.DeclarationExpression, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Expression { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Expression is object) yield return Expression;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitDeclarationExpression(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitDeclarationExpression(this, argument);
    }
    internal sealed partial class DeclarationExpressionOperation : BaseDeclarationExpressionOperation, IDeclarationExpressionOperation
    {
        internal DeclarationExpressionOperation(IOperation expression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Expression = SetParentOperation(expression, this);
        }
        public override IOperation Expression { get; }
    }
    internal abstract partial class LazyDeclarationExpressionOperation : BaseDeclarationExpressionOperation, IDeclarationExpressionOperation
    {
        private IOperation _lazyExpression = s_unset;
        internal LazyDeclarationExpressionOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateExpression();
        public override IOperation Expression
        {
            get
            {
                if (_lazyExpression == s_unset)
                {
                    IOperation expression = CreateExpression();
                    SetParentOperation(expression, this);
                    Interlocked.CompareExchange(ref _lazyExpression, expression, s_unset);
                }
                return _lazyExpression;
            }
        }
    }
    internal sealed partial class OmittedArgumentOperation : Operation, IOmittedArgumentOperation
    {
        internal OmittedArgumentOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.OmittedArgument, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitOmittedArgument(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitOmittedArgument(this, argument);
    }
    internal abstract partial class BaseSymbolInitializerOperation : Operation, ISymbolInitializerOperation
    {
        protected BaseSymbolInitializerOperation(ImmutableArray<ILocalSymbol> locals, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Locals = locals;
        }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public abstract IOperation Value { get; }
    }
    internal abstract partial class BaseFieldInitializerOperation : BaseSymbolInitializerOperation, IFieldInitializerOperation
    {
        internal BaseFieldInitializerOperation(ImmutableArray<IFieldSymbol> initializedFields, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, OperationKind.FieldInitializer, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializedFields = initializedFields;
        }
        public ImmutableArray<IFieldSymbol> InitializedFields { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitFieldInitializer(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitFieldInitializer(this, argument);
    }
    internal sealed partial class FieldInitializerOperation : BaseFieldInitializerOperation, IFieldInitializerOperation
    {
        internal FieldInitializerOperation(ImmutableArray<IFieldSymbol> initializedFields, ImmutableArray<ILocalSymbol> locals, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(initializedFields, locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Value = SetParentOperation(value, this);
        }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazyFieldInitializerOperation : BaseFieldInitializerOperation, IFieldInitializerOperation
    {
        private IOperation _lazyValue = s_unset;
        internal LazyFieldInitializerOperation(ImmutableArray<IFieldSymbol> initializedFields, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(initializedFields, locals, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BaseVariableInitializerOperation : BaseSymbolInitializerOperation, IVariableInitializerOperation
    {
        internal BaseVariableInitializerOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, OperationKind.VariableInitializer, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitVariableInitializer(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitVariableInitializer(this, argument);
    }
    internal sealed partial class VariableInitializerOperation : BaseVariableInitializerOperation, IVariableInitializerOperation
    {
        internal VariableInitializerOperation(ImmutableArray<ILocalSymbol> locals, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Value = SetParentOperation(value, this);
        }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazyVariableInitializerOperation : BaseVariableInitializerOperation, IVariableInitializerOperation
    {
        private IOperation _lazyValue = s_unset;
        internal LazyVariableInitializerOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BasePropertyInitializerOperation : BaseSymbolInitializerOperation, IPropertyInitializerOperation
    {
        internal BasePropertyInitializerOperation(ImmutableArray<IPropertySymbol> initializedProperties, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, OperationKind.PropertyInitializer, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InitializedProperties = initializedProperties;
        }
        public ImmutableArray<IPropertySymbol> InitializedProperties { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitPropertyInitializer(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitPropertyInitializer(this, argument);
    }
    internal sealed partial class PropertyInitializerOperation : BasePropertyInitializerOperation, IPropertyInitializerOperation
    {
        internal PropertyInitializerOperation(ImmutableArray<IPropertySymbol> initializedProperties, ImmutableArray<ILocalSymbol> locals, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(initializedProperties, locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Value = SetParentOperation(value, this);
        }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazyPropertyInitializerOperation : BasePropertyInitializerOperation, IPropertyInitializerOperation
    {
        private IOperation _lazyValue = s_unset;
        internal LazyPropertyInitializerOperation(ImmutableArray<IPropertySymbol> initializedProperties, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(initializedProperties, locals, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BaseParameterInitializerOperation : BaseSymbolInitializerOperation, IParameterInitializerOperation
    {
        internal BaseParameterInitializerOperation(IParameterSymbol parameter, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, OperationKind.ParameterInitializer, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Parameter = parameter;
        }
        public IParameterSymbol Parameter { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitParameterInitializer(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitParameterInitializer(this, argument);
    }
    internal sealed partial class ParameterInitializerOperation : BaseParameterInitializerOperation, IParameterInitializerOperation
    {
        internal ParameterInitializerOperation(IParameterSymbol parameter, ImmutableArray<ILocalSymbol> locals, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(parameter, locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Value = SetParentOperation(value, this);
        }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazyParameterInitializerOperation : BaseParameterInitializerOperation, IParameterInitializerOperation
    {
        private IOperation _lazyValue = s_unset;
        internal LazyParameterInitializerOperation(IParameterSymbol parameter, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(parameter, locals, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BaseArrayInitializerOperation : Operation, IArrayInitializerOperation
    {
        internal BaseArrayInitializerOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.ArrayInitializer, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract ImmutableArray<IOperation> ElementValues { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in ElementValues)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitArrayInitializer(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitArrayInitializer(this, argument);
    }
    internal sealed partial class ArrayInitializerOperation : BaseArrayInitializerOperation, IArrayInitializerOperation
    {
        internal ArrayInitializerOperation(ImmutableArray<IOperation> elementValues, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            ElementValues = SetParentOperation(elementValues, this);
        }
        public override ImmutableArray<IOperation> ElementValues { get; }
    }
    internal abstract partial class LazyArrayInitializerOperation : BaseArrayInitializerOperation, IArrayInitializerOperation
    {
        private ImmutableArray<IOperation> _lazyElementValues;
        internal LazyArrayInitializerOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IOperation> CreateElementValues();
        public override ImmutableArray<IOperation> ElementValues
        {
            get
            {
                if (_lazyElementValues.IsDefault)
                {
                    ImmutableArray<IOperation> elementValues = CreateElementValues();
                    SetParentOperation(elementValues, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyElementValues, elementValues);
                }
                return _lazyElementValues;
            }
        }
    }
    internal abstract partial class BaseVariableDeclaratorOperation : Operation, IVariableDeclaratorOperation
    {
        internal BaseVariableDeclaratorOperation(ILocalSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.VariableDeclarator, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Symbol = symbol;
        }
        public ILocalSymbol Symbol { get; }
        public abstract IVariableInitializerOperation Initializer { get; }
        public abstract ImmutableArray<IOperation> IgnoredArguments { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in IgnoredArguments)
                {
                    if (child is object) yield return child;
                }
                if (Initializer is object) yield return Initializer;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitVariableDeclarator(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitVariableDeclarator(this, argument);
    }
    internal sealed partial class VariableDeclaratorOperation : BaseVariableDeclaratorOperation, IVariableDeclaratorOperation
    {
        internal VariableDeclaratorOperation(ILocalSymbol symbol, IVariableInitializerOperation initializer, ImmutableArray<IOperation> ignoredArguments, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Initializer = SetParentOperation(initializer, this);
            IgnoredArguments = SetParentOperation(ignoredArguments, this);
        }
        public override IVariableInitializerOperation Initializer { get; }
        public override ImmutableArray<IOperation> IgnoredArguments { get; }
    }
    internal abstract partial class LazyVariableDeclaratorOperation : BaseVariableDeclaratorOperation, IVariableDeclaratorOperation
    {
        private IVariableInitializerOperation _lazyInitializer = s_unsetVariableInitializer;
        private ImmutableArray<IOperation> _lazyIgnoredArguments;
        internal LazyVariableDeclaratorOperation(ILocalSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(symbol, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IVariableInitializerOperation CreateInitializer();
        public override IVariableInitializerOperation Initializer
        {
            get
            {
                if (_lazyInitializer == s_unsetVariableInitializer)
                {
                    IVariableInitializerOperation initializer = CreateInitializer();
                    SetParentOperation(initializer, this);
                    Interlocked.CompareExchange(ref _lazyInitializer, initializer, s_unsetVariableInitializer);
                }
                return _lazyInitializer;
            }
        }
        protected abstract ImmutableArray<IOperation> CreateIgnoredArguments();
        public override ImmutableArray<IOperation> IgnoredArguments
        {
            get
            {
                if (_lazyIgnoredArguments.IsDefault)
                {
                    ImmutableArray<IOperation> ignoredArguments = CreateIgnoredArguments();
                    SetParentOperation(ignoredArguments, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyIgnoredArguments, ignoredArguments);
                }
                return _lazyIgnoredArguments;
            }
        }
    }
    internal abstract partial class BaseVariableDeclarationOperation : Operation, IVariableDeclarationOperation
    {
        internal BaseVariableDeclarationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.VariableDeclaration, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract ImmutableArray<IVariableDeclaratorOperation> Declarators { get; }
        public abstract IVariableInitializerOperation Initializer { get; }
        public abstract ImmutableArray<IOperation> IgnoredDimensions { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in IgnoredDimensions)
                {
                    if (child is object) yield return child;
                }
                foreach (var child in Declarators)
                {
                    if (child is object) yield return child;
                }
                if (Initializer is object) yield return Initializer;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitVariableDeclaration(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitVariableDeclaration(this, argument);
    }
    internal sealed partial class VariableDeclarationOperation : BaseVariableDeclarationOperation, IVariableDeclarationOperation
    {
        internal VariableDeclarationOperation(ImmutableArray<IVariableDeclaratorOperation> declarators, IVariableInitializerOperation initializer, ImmutableArray<IOperation> ignoredDimensions, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Declarators = SetParentOperation(declarators, this);
            Initializer = SetParentOperation(initializer, this);
            IgnoredDimensions = SetParentOperation(ignoredDimensions, this);
        }
        public override ImmutableArray<IVariableDeclaratorOperation> Declarators { get; }
        public override IVariableInitializerOperation Initializer { get; }
        public override ImmutableArray<IOperation> IgnoredDimensions { get; }
    }
    internal abstract partial class LazyVariableDeclarationOperation : BaseVariableDeclarationOperation, IVariableDeclarationOperation
    {
        private ImmutableArray<IVariableDeclaratorOperation> _lazyDeclarators;
        private IVariableInitializerOperation _lazyInitializer = s_unsetVariableInitializer;
        private ImmutableArray<IOperation> _lazyIgnoredDimensions;
        internal LazyVariableDeclarationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IVariableDeclaratorOperation> CreateDeclarators();
        public override ImmutableArray<IVariableDeclaratorOperation> Declarators
        {
            get
            {
                if (_lazyDeclarators.IsDefault)
                {
                    ImmutableArray<IVariableDeclaratorOperation> declarators = CreateDeclarators();
                    SetParentOperation(declarators, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyDeclarators, declarators);
                }
                return _lazyDeclarators;
            }
        }
        protected abstract IVariableInitializerOperation CreateInitializer();
        public override IVariableInitializerOperation Initializer
        {
            get
            {
                if (_lazyInitializer == s_unsetVariableInitializer)
                {
                    IVariableInitializerOperation initializer = CreateInitializer();
                    SetParentOperation(initializer, this);
                    Interlocked.CompareExchange(ref _lazyInitializer, initializer, s_unsetVariableInitializer);
                }
                return _lazyInitializer;
            }
        }
        protected abstract ImmutableArray<IOperation> CreateIgnoredDimensions();
        public override ImmutableArray<IOperation> IgnoredDimensions
        {
            get
            {
                if (_lazyIgnoredDimensions.IsDefault)
                {
                    ImmutableArray<IOperation> ignoredDimensions = CreateIgnoredDimensions();
                    SetParentOperation(ignoredDimensions, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyIgnoredDimensions, ignoredDimensions);
                }
                return _lazyIgnoredDimensions;
            }
        }
    }
    internal abstract partial class BaseArgumentOperation : Operation, IArgumentOperation
    {
        internal BaseArgumentOperation(ArgumentKind argumentKind, IParameterSymbol parameter, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Argument, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ArgumentKind = argumentKind;
            Parameter = parameter;
        }
        public ArgumentKind ArgumentKind { get; }
        public IParameterSymbol Parameter { get; }
        public abstract IOperation Value { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitArgument(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitArgument(this, argument);
    }
    internal sealed partial class ArgumentOperation : BaseArgumentOperation, IArgumentOperation
    {
        internal ArgumentOperation(ArgumentKind argumentKind, IParameterSymbol parameter, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(argumentKind, parameter, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Value = SetParentOperation(value, this);
        }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazyArgumentOperation : BaseArgumentOperation, IArgumentOperation
    {
        private IOperation _lazyValue = s_unset;
        internal LazyArgumentOperation(ArgumentKind argumentKind, IParameterSymbol parameter, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(argumentKind, parameter, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BaseCatchClauseOperation : Operation, ICatchClauseOperation
    {
        internal BaseCatchClauseOperation(ITypeSymbol exceptionType, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.CatchClause, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExceptionType = exceptionType;
            Locals = locals;
        }
        public abstract IOperation ExceptionDeclarationOrExpression { get; }
        public ITypeSymbol ExceptionType { get; }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public abstract IOperation Filter { get; }
        public abstract IBlockOperation Handler { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (ExceptionDeclarationOrExpression is object) yield return ExceptionDeclarationOrExpression;
                if (Filter is object) yield return Filter;
                if (Handler is object) yield return Handler;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitCatchClause(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitCatchClause(this, argument);
    }
    internal sealed partial class CatchClauseOperation : BaseCatchClauseOperation, ICatchClauseOperation
    {
        internal CatchClauseOperation(IOperation exceptionDeclarationOrExpression, ITypeSymbol exceptionType, ImmutableArray<ILocalSymbol> locals, IOperation filter, IBlockOperation handler, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(exceptionType, locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ExceptionDeclarationOrExpression = SetParentOperation(exceptionDeclarationOrExpression, this);
            Filter = SetParentOperation(filter, this);
            Handler = SetParentOperation(handler, this);
        }
        public override IOperation ExceptionDeclarationOrExpression { get; }
        public override IOperation Filter { get; }
        public override IBlockOperation Handler { get; }
    }
    internal abstract partial class LazyCatchClauseOperation : BaseCatchClauseOperation, ICatchClauseOperation
    {
        private IOperation _lazyExceptionDeclarationOrExpression = s_unset;
        private IOperation _lazyFilter = s_unset;
        private IBlockOperation _lazyHandler = s_unsetBlock;
        internal LazyCatchClauseOperation(ITypeSymbol exceptionType, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(exceptionType, locals, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateExceptionDeclarationOrExpression();
        public override IOperation ExceptionDeclarationOrExpression
        {
            get
            {
                if (_lazyExceptionDeclarationOrExpression == s_unset)
                {
                    IOperation exceptionDeclarationOrExpression = CreateExceptionDeclarationOrExpression();
                    SetParentOperation(exceptionDeclarationOrExpression, this);
                    Interlocked.CompareExchange(ref _lazyExceptionDeclarationOrExpression, exceptionDeclarationOrExpression, s_unset);
                }
                return _lazyExceptionDeclarationOrExpression;
            }
        }
        protected abstract IOperation CreateFilter();
        public override IOperation Filter
        {
            get
            {
                if (_lazyFilter == s_unset)
                {
                    IOperation filter = CreateFilter();
                    SetParentOperation(filter, this);
                    Interlocked.CompareExchange(ref _lazyFilter, filter, s_unset);
                }
                return _lazyFilter;
            }
        }
        protected abstract IBlockOperation CreateHandler();
        public override IBlockOperation Handler
        {
            get
            {
                if (_lazyHandler == s_unsetBlock)
                {
                    IBlockOperation handler = CreateHandler();
                    SetParentOperation(handler, this);
                    Interlocked.CompareExchange(ref _lazyHandler, handler, s_unsetBlock);
                }
                return _lazyHandler;
            }
        }
    }
    internal abstract partial class BaseSwitchCaseOperation : Operation, ISwitchCaseOperation
    {
        internal BaseSwitchCaseOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.SwitchCase, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Locals = locals;
        }
        public abstract ImmutableArray<ICaseClauseOperation> Clauses { get; }
        public abstract ImmutableArray<IOperation> Body { get; }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in Clauses)
                {
                    if (child is object) yield return child;
                }
                foreach (var child in Body)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitSwitchCase(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitSwitchCase(this, argument);
    }
    internal sealed partial class SwitchCaseOperation : BaseSwitchCaseOperation, ISwitchCaseOperation
    {
        internal SwitchCaseOperation(ImmutableArray<ICaseClauseOperation> clauses, ImmutableArray<IOperation> body, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Clauses = SetParentOperation(clauses, this);
            Body = SetParentOperation(body, this);
        }
        public override ImmutableArray<ICaseClauseOperation> Clauses { get; }
        public override ImmutableArray<IOperation> Body { get; }
    }
    internal abstract partial class LazySwitchCaseOperation : BaseSwitchCaseOperation, ISwitchCaseOperation
    {
        private ImmutableArray<ICaseClauseOperation> _lazyClauses;
        private ImmutableArray<IOperation> _lazyBody;
        internal LazySwitchCaseOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<ICaseClauseOperation> CreateClauses();
        public override ImmutableArray<ICaseClauseOperation> Clauses
        {
            get
            {
                if (_lazyClauses.IsDefault)
                {
                    ImmutableArray<ICaseClauseOperation> clauses = CreateClauses();
                    SetParentOperation(clauses, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyClauses, clauses);
                }
                return _lazyClauses;
            }
        }
        protected abstract ImmutableArray<IOperation> CreateBody();
        public override ImmutableArray<IOperation> Body
        {
            get
            {
                if (_lazyBody.IsDefault)
                {
                    ImmutableArray<IOperation> body = CreateBody();
                    SetParentOperation(body, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyBody, body);
                }
                return _lazyBody;
            }
        }
    }
    internal abstract partial class BaseCaseClauseOperation : Operation, ICaseClauseOperation
    {
        protected BaseCaseClauseOperation(CaseKind caseKind, ILabelSymbol label, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            CaseKind = caseKind;
            Label = label;
        }
        public CaseKind CaseKind { get; }
        public ILabelSymbol Label { get; }
    }
    internal sealed partial class DefaultCaseClauseOperation : BaseCaseClauseOperation, IDefaultCaseClauseOperation
    {
        internal DefaultCaseClauseOperation(CaseKind caseKind, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(caseKind, label, OperationKind.CaseClause, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitDefaultCaseClause(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitDefaultCaseClause(this, argument);
    }
    internal abstract partial class BaseRangeCaseClauseOperation : BaseCaseClauseOperation, IRangeCaseClauseOperation
    {
        internal BaseRangeCaseClauseOperation(CaseKind caseKind, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(caseKind, label, OperationKind.CaseClause, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation MinimumValue { get; }
        public abstract IOperation MaximumValue { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (MinimumValue is object) yield return MinimumValue;
                if (MaximumValue is object) yield return MaximumValue;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitRangeCaseClause(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitRangeCaseClause(this, argument);
    }
    internal sealed partial class RangeCaseClauseOperation : BaseRangeCaseClauseOperation, IRangeCaseClauseOperation
    {
        internal RangeCaseClauseOperation(IOperation minimumValue, IOperation maximumValue, CaseKind caseKind, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(caseKind, label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            MinimumValue = SetParentOperation(minimumValue, this);
            MaximumValue = SetParentOperation(maximumValue, this);
        }
        public override IOperation MinimumValue { get; }
        public override IOperation MaximumValue { get; }
    }
    internal abstract partial class LazyRangeCaseClauseOperation : BaseRangeCaseClauseOperation, IRangeCaseClauseOperation
    {
        private IOperation _lazyMinimumValue = s_unset;
        private IOperation _lazyMaximumValue = s_unset;
        internal LazyRangeCaseClauseOperation(CaseKind caseKind, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(caseKind, label, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateMinimumValue();
        public override IOperation MinimumValue
        {
            get
            {
                if (_lazyMinimumValue == s_unset)
                {
                    IOperation minimumValue = CreateMinimumValue();
                    SetParentOperation(minimumValue, this);
                    Interlocked.CompareExchange(ref _lazyMinimumValue, minimumValue, s_unset);
                }
                return _lazyMinimumValue;
            }
        }
        protected abstract IOperation CreateMaximumValue();
        public override IOperation MaximumValue
        {
            get
            {
                if (_lazyMaximumValue == s_unset)
                {
                    IOperation maximumValue = CreateMaximumValue();
                    SetParentOperation(maximumValue, this);
                    Interlocked.CompareExchange(ref _lazyMaximumValue, maximumValue, s_unset);
                }
                return _lazyMaximumValue;
            }
        }
    }
    internal abstract partial class BaseRelationalCaseClauseOperation : BaseCaseClauseOperation, IRelationalCaseClauseOperation
    {
        internal BaseRelationalCaseClauseOperation(BinaryOperatorKind relation, CaseKind caseKind, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(caseKind, label, OperationKind.CaseClause, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Relation = relation;
        }
        public abstract IOperation Value { get; }
        public BinaryOperatorKind Relation { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitRelationalCaseClause(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitRelationalCaseClause(this, argument);
    }
    internal sealed partial class RelationalCaseClauseOperation : BaseRelationalCaseClauseOperation, IRelationalCaseClauseOperation
    {
        internal RelationalCaseClauseOperation(IOperation value, BinaryOperatorKind relation, CaseKind caseKind, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(relation, caseKind, label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Value = SetParentOperation(value, this);
        }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazyRelationalCaseClauseOperation : BaseRelationalCaseClauseOperation, IRelationalCaseClauseOperation
    {
        private IOperation _lazyValue = s_unset;
        internal LazyRelationalCaseClauseOperation(BinaryOperatorKind relation, CaseKind caseKind, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(relation, caseKind, label, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BaseSingleValueCaseClauseOperation : BaseCaseClauseOperation, ISingleValueCaseClauseOperation
    {
        internal BaseSingleValueCaseClauseOperation(CaseKind caseKind, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(caseKind, label, OperationKind.CaseClause, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Value { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitSingleValueCaseClause(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitSingleValueCaseClause(this, argument);
    }
    internal sealed partial class SingleValueCaseClauseOperation : BaseSingleValueCaseClauseOperation, ISingleValueCaseClauseOperation
    {
        internal SingleValueCaseClauseOperation(IOperation value, CaseKind caseKind, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(caseKind, label, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Value = SetParentOperation(value, this);
        }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazySingleValueCaseClauseOperation : BaseSingleValueCaseClauseOperation, ISingleValueCaseClauseOperation
    {
        private IOperation _lazyValue = s_unset;
        internal LazySingleValueCaseClauseOperation(CaseKind caseKind, ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(caseKind, label, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BaseInterpolatedStringContentOperation : Operation, IInterpolatedStringContentOperation
    {
        protected BaseInterpolatedStringContentOperation(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(kind, semanticModel, syntax, type, constantValue, isImplicit) { }
    }
    internal abstract partial class BaseInterpolatedStringTextOperation : BaseInterpolatedStringContentOperation, IInterpolatedStringTextOperation
    {
        internal BaseInterpolatedStringTextOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.InterpolatedStringText, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Text { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Text is object) yield return Text;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitInterpolatedStringText(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitInterpolatedStringText(this, argument);
    }
    internal sealed partial class InterpolatedStringTextOperation : BaseInterpolatedStringTextOperation, IInterpolatedStringTextOperation
    {
        internal InterpolatedStringTextOperation(IOperation text, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Text = SetParentOperation(text, this);
        }
        public override IOperation Text { get; }
    }
    internal abstract partial class LazyInterpolatedStringTextOperation : BaseInterpolatedStringTextOperation, IInterpolatedStringTextOperation
    {
        private IOperation _lazyText = s_unset;
        internal LazyInterpolatedStringTextOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateText();
        public override IOperation Text
        {
            get
            {
                if (_lazyText == s_unset)
                {
                    IOperation text = CreateText();
                    SetParentOperation(text, this);
                    Interlocked.CompareExchange(ref _lazyText, text, s_unset);
                }
                return _lazyText;
            }
        }
    }
    internal abstract partial class BaseInterpolationOperation : BaseInterpolatedStringContentOperation, IInterpolationOperation
    {
        internal BaseInterpolationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Interpolation, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Expression { get; }
        public abstract IOperation Alignment { get; }
        public abstract IOperation FormatString { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Expression is object) yield return Expression;
                if (Alignment is object) yield return Alignment;
                if (FormatString is object) yield return FormatString;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitInterpolation(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitInterpolation(this, argument);
    }
    internal sealed partial class InterpolationOperation : BaseInterpolationOperation, IInterpolationOperation
    {
        internal InterpolationOperation(IOperation expression, IOperation alignment, IOperation formatString, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Expression = SetParentOperation(expression, this);
            Alignment = SetParentOperation(alignment, this);
            FormatString = SetParentOperation(formatString, this);
        }
        public override IOperation Expression { get; }
        public override IOperation Alignment { get; }
        public override IOperation FormatString { get; }
    }
    internal abstract partial class LazyInterpolationOperation : BaseInterpolationOperation, IInterpolationOperation
    {
        private IOperation _lazyExpression = s_unset;
        private IOperation _lazyAlignment = s_unset;
        private IOperation _lazyFormatString = s_unset;
        internal LazyInterpolationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateExpression();
        public override IOperation Expression
        {
            get
            {
                if (_lazyExpression == s_unset)
                {
                    IOperation expression = CreateExpression();
                    SetParentOperation(expression, this);
                    Interlocked.CompareExchange(ref _lazyExpression, expression, s_unset);
                }
                return _lazyExpression;
            }
        }
        protected abstract IOperation CreateAlignment();
        public override IOperation Alignment
        {
            get
            {
                if (_lazyAlignment == s_unset)
                {
                    IOperation alignment = CreateAlignment();
                    SetParentOperation(alignment, this);
                    Interlocked.CompareExchange(ref _lazyAlignment, alignment, s_unset);
                }
                return _lazyAlignment;
            }
        }
        protected abstract IOperation CreateFormatString();
        public override IOperation FormatString
        {
            get
            {
                if (_lazyFormatString == s_unset)
                {
                    IOperation formatString = CreateFormatString();
                    SetParentOperation(formatString, this);
                    Interlocked.CompareExchange(ref _lazyFormatString, formatString, s_unset);
                }
                return _lazyFormatString;
            }
        }
    }
    internal abstract partial class BasePatternOperation : Operation, IPatternOperation
    {
        protected BasePatternOperation(ITypeSymbol inputType, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            InputType = inputType;
        }
        public ITypeSymbol InputType { get; }
    }
    internal abstract partial class BaseConstantPatternOperation : BasePatternOperation, IConstantPatternOperation
    {
        internal BaseConstantPatternOperation(ITypeSymbol inputType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(inputType, OperationKind.ConstantPattern, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Value { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitConstantPattern(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitConstantPattern(this, argument);
    }
    internal sealed partial class ConstantPatternOperation : BaseConstantPatternOperation, IConstantPatternOperation
    {
        internal ConstantPatternOperation(IOperation value, ITypeSymbol inputType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(inputType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Value = SetParentOperation(value, this);
        }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazyConstantPatternOperation : BaseConstantPatternOperation, IConstantPatternOperation
    {
        private IOperation _lazyValue = s_unset;
        internal LazyConstantPatternOperation(ITypeSymbol inputType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(inputType, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal sealed partial class DeclarationPatternOperation : BasePatternOperation, IDeclarationPatternOperation
    {
        internal DeclarationPatternOperation(ITypeSymbol matchedType, bool matchesNull, ISymbol declaredSymbol, ITypeSymbol inputType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(inputType, OperationKind.DeclarationPattern, semanticModel, syntax, type, constantValue, isImplicit)
        {
            MatchedType = matchedType;
            MatchesNull = matchesNull;
            DeclaredSymbol = declaredSymbol;
        }
        public ITypeSymbol MatchedType { get; }
        public bool MatchesNull { get; }
        public ISymbol DeclaredSymbol { get; }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitDeclarationPattern(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitDeclarationPattern(this, argument);
    }
    internal abstract partial class BaseTupleBinaryOperation : Operation, ITupleBinaryOperation
    {
        internal BaseTupleBinaryOperation(BinaryOperatorKind operatorKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.TupleBinary, semanticModel, syntax, type, constantValue, isImplicit)
        {
            OperatorKind = operatorKind;
        }
        public BinaryOperatorKind OperatorKind { get; }
        public abstract IOperation LeftOperand { get; }
        public abstract IOperation RightOperand { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (LeftOperand is object) yield return LeftOperand;
                if (RightOperand is object) yield return RightOperand;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitTupleBinaryOperator(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitTupleBinaryOperator(this, argument);
    }
    internal sealed partial class TupleBinaryOperation : BaseTupleBinaryOperation, ITupleBinaryOperation
    {
        internal TupleBinaryOperation(BinaryOperatorKind operatorKind, IOperation leftOperand, IOperation rightOperand, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(operatorKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LeftOperand = SetParentOperation(leftOperand, this);
            RightOperand = SetParentOperation(rightOperand, this);
        }
        public override IOperation LeftOperand { get; }
        public override IOperation RightOperand { get; }
    }
    internal abstract partial class LazyTupleBinaryOperation : BaseTupleBinaryOperation, ITupleBinaryOperation
    {
        private IOperation _lazyLeftOperand = s_unset;
        private IOperation _lazyRightOperand = s_unset;
        internal LazyTupleBinaryOperation(BinaryOperatorKind operatorKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(operatorKind, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateLeftOperand();
        public override IOperation LeftOperand
        {
            get
            {
                if (_lazyLeftOperand == s_unset)
                {
                    IOperation leftOperand = CreateLeftOperand();
                    SetParentOperation(leftOperand, this);
                    Interlocked.CompareExchange(ref _lazyLeftOperand, leftOperand, s_unset);
                }
                return _lazyLeftOperand;
            }
        }
        protected abstract IOperation CreateRightOperand();
        public override IOperation RightOperand
        {
            get
            {
                if (_lazyRightOperand == s_unset)
                {
                    IOperation rightOperand = CreateRightOperand();
                    SetParentOperation(rightOperand, this);
                    Interlocked.CompareExchange(ref _lazyRightOperand, rightOperand, s_unset);
                }
                return _lazyRightOperand;
            }
        }
    }
    internal abstract partial class BaseMethodBodyBaseOperation : Operation, IMethodBodyBaseOperation
    {
        protected BaseMethodBodyBaseOperation(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(kind, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IBlockOperation BlockBody { get; }
        public abstract IBlockOperation ExpressionBody { get; }
    }
    internal abstract partial class BaseMethodBodyOperation : BaseMethodBodyBaseOperation, IMethodBodyOperation
    {
        internal BaseMethodBodyOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.MethodBody, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (BlockBody is object) yield return BlockBody;
                if (ExpressionBody is object) yield return ExpressionBody;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitMethodBodyOperation(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitMethodBodyOperation(this, argument);
    }
    internal sealed partial class MethodBodyOperation : BaseMethodBodyOperation, IMethodBodyOperation
    {
        internal MethodBodyOperation(IBlockOperation blockBody, IBlockOperation expressionBody, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            BlockBody = SetParentOperation(blockBody, this);
            ExpressionBody = SetParentOperation(expressionBody, this);
        }
        public override IBlockOperation BlockBody { get; }
        public override IBlockOperation ExpressionBody { get; }
    }
    internal abstract partial class LazyMethodBodyOperation : BaseMethodBodyOperation, IMethodBodyOperation
    {
        private IBlockOperation _lazyBlockBody = s_unsetBlock;
        private IBlockOperation _lazyExpressionBody = s_unsetBlock;
        internal LazyMethodBodyOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IBlockOperation CreateBlockBody();
        public override IBlockOperation BlockBody
        {
            get
            {
                if (_lazyBlockBody == s_unsetBlock)
                {
                    IBlockOperation blockBody = CreateBlockBody();
                    SetParentOperation(blockBody, this);
                    Interlocked.CompareExchange(ref _lazyBlockBody, blockBody, s_unsetBlock);
                }
                return _lazyBlockBody;
            }
        }
        protected abstract IBlockOperation CreateExpressionBody();
        public override IBlockOperation ExpressionBody
        {
            get
            {
                if (_lazyExpressionBody == s_unsetBlock)
                {
                    IBlockOperation expressionBody = CreateExpressionBody();
                    SetParentOperation(expressionBody, this);
                    Interlocked.CompareExchange(ref _lazyExpressionBody, expressionBody, s_unsetBlock);
                }
                return _lazyExpressionBody;
            }
        }
    }
    internal abstract partial class BaseConstructorBodyOperation : BaseMethodBodyBaseOperation, IConstructorBodyOperation
    {
        internal BaseConstructorBodyOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.ConstructorBody, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Locals = locals;
        }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public abstract IOperation Initializer { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Initializer is object) yield return Initializer;
                if (BlockBody is object) yield return BlockBody;
                if (ExpressionBody is object) yield return ExpressionBody;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitConstructorBodyOperation(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitConstructorBodyOperation(this, argument);
    }
    internal sealed partial class ConstructorBodyOperation : BaseConstructorBodyOperation, IConstructorBodyOperation
    {
        internal ConstructorBodyOperation(ImmutableArray<ILocalSymbol> locals, IOperation initializer, IBlockOperation blockBody, IBlockOperation expressionBody, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Initializer = SetParentOperation(initializer, this);
            BlockBody = SetParentOperation(blockBody, this);
            ExpressionBody = SetParentOperation(expressionBody, this);
        }
        public override IOperation Initializer { get; }
        public override IBlockOperation BlockBody { get; }
        public override IBlockOperation ExpressionBody { get; }
    }
    internal abstract partial class LazyConstructorBodyOperation : BaseConstructorBodyOperation, IConstructorBodyOperation
    {
        private IOperation _lazyInitializer = s_unset;
        private IBlockOperation _lazyBlockBody = s_unsetBlock;
        private IBlockOperation _lazyExpressionBody = s_unsetBlock;
        internal LazyConstructorBodyOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateInitializer();
        public override IOperation Initializer
        {
            get
            {
                if (_lazyInitializer == s_unset)
                {
                    IOperation initializer = CreateInitializer();
                    SetParentOperation(initializer, this);
                    Interlocked.CompareExchange(ref _lazyInitializer, initializer, s_unset);
                }
                return _lazyInitializer;
            }
        }
        protected abstract IBlockOperation CreateBlockBody();
        public override IBlockOperation BlockBody
        {
            get
            {
                if (_lazyBlockBody == s_unsetBlock)
                {
                    IBlockOperation blockBody = CreateBlockBody();
                    SetParentOperation(blockBody, this);
                    Interlocked.CompareExchange(ref _lazyBlockBody, blockBody, s_unsetBlock);
                }
                return _lazyBlockBody;
            }
        }
        protected abstract IBlockOperation CreateExpressionBody();
        public override IBlockOperation ExpressionBody
        {
            get
            {
                if (_lazyExpressionBody == s_unsetBlock)
                {
                    IBlockOperation expressionBody = CreateExpressionBody();
                    SetParentOperation(expressionBody, this);
                    Interlocked.CompareExchange(ref _lazyExpressionBody, expressionBody, s_unsetBlock);
                }
                return _lazyExpressionBody;
            }
        }
    }
    internal sealed partial class DiscardOperation : Operation, IDiscardOperation
    {
        internal DiscardOperation(IDiscardSymbol discardSymbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Discard, semanticModel, syntax, type, constantValue, isImplicit)
        {
            DiscardSymbol = discardSymbol;
        }
        public IDiscardSymbol DiscardSymbol { get; }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitDiscardOperation(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitDiscardOperation(this, argument);
    }
    internal sealed partial class FlowCaptureReferenceOperation : Operation, IFlowCaptureReferenceOperation
    {
        internal FlowCaptureReferenceOperation(CaptureId id, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.FlowCaptureReference, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Id = id;
        }
        public CaptureId Id { get; }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitFlowCaptureReference(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitFlowCaptureReference(this, argument);
    }
    internal sealed partial class CaughtExceptionOperation : Operation, ICaughtExceptionOperation
    {
        internal CaughtExceptionOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.CaughtException, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitCaughtException(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitCaughtException(this, argument);
    }
    internal sealed partial class StaticLocalInitializationSemaphoreOperation : Operation, IStaticLocalInitializationSemaphoreOperation
    {
        internal StaticLocalInitializationSemaphoreOperation(ILocalSymbol local, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.StaticLocalInitializationSemaphore, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Local = local;
        }
        public ILocalSymbol Local { get; }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitStaticLocalInitializationSemaphore(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitStaticLocalInitializationSemaphore(this, argument);
    }
    internal abstract partial class BaseCoalesceAssignmentOperation : BaseAssignmentOperation, ICoalesceAssignmentOperation
    {
        internal BaseCoalesceAssignmentOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.CoalesceAssignment, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Target is object) yield return Target;
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitCoalesceAssignment(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitCoalesceAssignment(this, argument);
    }
    internal sealed partial class CoalesceAssignmentOperation : BaseCoalesceAssignmentOperation, ICoalesceAssignmentOperation
    {
        internal CoalesceAssignmentOperation(IOperation target, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Target = SetParentOperation(target, this);
            Value = SetParentOperation(value, this);
        }
        public override IOperation Target { get; }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazyCoalesceAssignmentOperation : BaseCoalesceAssignmentOperation, ICoalesceAssignmentOperation
    {
        private IOperation _lazyTarget = s_unset;
        private IOperation _lazyValue = s_unset;
        internal LazyCoalesceAssignmentOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateTarget();
        public override IOperation Target
        {
            get
            {
                if (_lazyTarget == s_unset)
                {
                    IOperation target = CreateTarget();
                    SetParentOperation(target, this);
                    Interlocked.CompareExchange(ref _lazyTarget, target, s_unset);
                }
                return _lazyTarget;
            }
        }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BaseRangeOperation : Operation, IRangeOperation
    {
        internal BaseRangeOperation(bool isLifted, IMethodSymbol method, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.Range, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsLifted = isLifted;
            Method = method;
        }
        public abstract IOperation LeftOperand { get; }
        public abstract IOperation RightOperand { get; }
        public bool IsLifted { get; }
        public IMethodSymbol Method { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (LeftOperand is object) yield return LeftOperand;
                if (RightOperand is object) yield return RightOperand;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitRangeOperation(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitRangeOperation(this, argument);
    }
    internal sealed partial class RangeOperation : BaseRangeOperation, IRangeOperation
    {
        internal RangeOperation(IOperation leftOperand, IOperation rightOperand, bool isLifted, IMethodSymbol method, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isLifted, method, semanticModel, syntax, type, constantValue, isImplicit)
        {
            LeftOperand = SetParentOperation(leftOperand, this);
            RightOperand = SetParentOperation(rightOperand, this);
        }
        public override IOperation LeftOperand { get; }
        public override IOperation RightOperand { get; }
    }
    internal abstract partial class LazyRangeOperation : BaseRangeOperation, IRangeOperation
    {
        private IOperation _lazyLeftOperand = s_unset;
        private IOperation _lazyRightOperand = s_unset;
        internal LazyRangeOperation(bool isLifted, IMethodSymbol method, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isLifted, method, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateLeftOperand();
        public override IOperation LeftOperand
        {
            get
            {
                if (_lazyLeftOperand == s_unset)
                {
                    IOperation leftOperand = CreateLeftOperand();
                    SetParentOperation(leftOperand, this);
                    Interlocked.CompareExchange(ref _lazyLeftOperand, leftOperand, s_unset);
                }
                return _lazyLeftOperand;
            }
        }
        protected abstract IOperation CreateRightOperand();
        public override IOperation RightOperand
        {
            get
            {
                if (_lazyRightOperand == s_unset)
                {
                    IOperation rightOperand = CreateRightOperand();
                    SetParentOperation(rightOperand, this);
                    Interlocked.CompareExchange(ref _lazyRightOperand, rightOperand, s_unset);
                }
                return _lazyRightOperand;
            }
        }
    }
    internal abstract partial class BaseReDimOperation : Operation, IReDimOperation
    {
        internal BaseReDimOperation(bool preserve, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.ReDim, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Preserve = preserve;
        }
        public abstract ImmutableArray<IReDimClauseOperation> Clauses { get; }
        public bool Preserve { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in Clauses)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitReDim(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitReDim(this, argument);
    }
    internal sealed partial class ReDimOperation : BaseReDimOperation, IReDimOperation
    {
        internal ReDimOperation(ImmutableArray<IReDimClauseOperation> clauses, bool preserve, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(preserve, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Clauses = SetParentOperation(clauses, this);
        }
        public override ImmutableArray<IReDimClauseOperation> Clauses { get; }
    }
    internal abstract partial class LazyReDimOperation : BaseReDimOperation, IReDimOperation
    {
        private ImmutableArray<IReDimClauseOperation> _lazyClauses;
        internal LazyReDimOperation(bool preserve, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(preserve, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IReDimClauseOperation> CreateClauses();
        public override ImmutableArray<IReDimClauseOperation> Clauses
        {
            get
            {
                if (_lazyClauses.IsDefault)
                {
                    ImmutableArray<IReDimClauseOperation> clauses = CreateClauses();
                    SetParentOperation(clauses, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyClauses, clauses);
                }
                return _lazyClauses;
            }
        }
    }
    internal abstract partial class BaseReDimClauseOperation : Operation, IReDimClauseOperation
    {
        internal BaseReDimClauseOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.ReDimClause, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Operand { get; }
        public abstract ImmutableArray<IOperation> DimensionSizes { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Operand is object) yield return Operand;
                foreach (var child in DimensionSizes)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitReDimClause(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitReDimClause(this, argument);
    }
    internal sealed partial class ReDimClauseOperation : BaseReDimClauseOperation, IReDimClauseOperation
    {
        internal ReDimClauseOperation(IOperation operand, ImmutableArray<IOperation> dimensionSizes, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Operand = SetParentOperation(operand, this);
            DimensionSizes = SetParentOperation(dimensionSizes, this);
        }
        public override IOperation Operand { get; }
        public override ImmutableArray<IOperation> DimensionSizes { get; }
    }
    internal abstract partial class LazyReDimClauseOperation : BaseReDimClauseOperation, IReDimClauseOperation
    {
        private IOperation _lazyOperand = s_unset;
        private ImmutableArray<IOperation> _lazyDimensionSizes;
        internal LazyReDimClauseOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateOperand();
        public override IOperation Operand
        {
            get
            {
                if (_lazyOperand == s_unset)
                {
                    IOperation operand = CreateOperand();
                    SetParentOperation(operand, this);
                    Interlocked.CompareExchange(ref _lazyOperand, operand, s_unset);
                }
                return _lazyOperand;
            }
        }
        protected abstract ImmutableArray<IOperation> CreateDimensionSizes();
        public override ImmutableArray<IOperation> DimensionSizes
        {
            get
            {
                if (_lazyDimensionSizes.IsDefault)
                {
                    ImmutableArray<IOperation> dimensionSizes = CreateDimensionSizes();
                    SetParentOperation(dimensionSizes, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyDimensionSizes, dimensionSizes);
                }
                return _lazyDimensionSizes;
            }
        }
    }
    internal abstract partial class BaseRecursivePatternOperation : BasePatternOperation, IRecursivePatternOperation
    {
        internal BaseRecursivePatternOperation(ITypeSymbol matchedType, ISymbol deconstructSymbol, ISymbol declaredSymbol, ITypeSymbol inputType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(inputType, OperationKind.RecursivePattern, semanticModel, syntax, type, constantValue, isImplicit)
        {
            MatchedType = matchedType;
            DeconstructSymbol = deconstructSymbol;
            DeclaredSymbol = declaredSymbol;
        }
        public ITypeSymbol MatchedType { get; }
        public ISymbol DeconstructSymbol { get; }
        public abstract ImmutableArray<IPatternOperation> DeconstructionSubpatterns { get; }
        public abstract ImmutableArray<IPropertySubpatternOperation> PropertySubpatterns { get; }
        public ISymbol DeclaredSymbol { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                foreach (var child in DeconstructionSubpatterns)
                {
                    if (child is object) yield return child;
                }
                foreach (var child in PropertySubpatterns)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitRecursivePattern(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitRecursivePattern(this, argument);
    }
    internal sealed partial class RecursivePatternOperation : BaseRecursivePatternOperation, IRecursivePatternOperation
    {
        internal RecursivePatternOperation(ITypeSymbol matchedType, ISymbol deconstructSymbol, ImmutableArray<IPatternOperation> deconstructionSubpatterns, ImmutableArray<IPropertySubpatternOperation> propertySubpatterns, ISymbol declaredSymbol, ITypeSymbol inputType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(matchedType, deconstructSymbol, declaredSymbol, inputType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            DeconstructionSubpatterns = SetParentOperation(deconstructionSubpatterns, this);
            PropertySubpatterns = SetParentOperation(propertySubpatterns, this);
        }
        public override ImmutableArray<IPatternOperation> DeconstructionSubpatterns { get; }
        public override ImmutableArray<IPropertySubpatternOperation> PropertySubpatterns { get; }
    }
    internal abstract partial class LazyRecursivePatternOperation : BaseRecursivePatternOperation, IRecursivePatternOperation
    {
        private ImmutableArray<IPatternOperation> _lazyDeconstructionSubpatterns;
        private ImmutableArray<IPropertySubpatternOperation> _lazyPropertySubpatterns;
        internal LazyRecursivePatternOperation(ITypeSymbol matchedType, ISymbol deconstructSymbol, ISymbol declaredSymbol, ITypeSymbol inputType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(matchedType, deconstructSymbol, declaredSymbol, inputType, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract ImmutableArray<IPatternOperation> CreateDeconstructionSubpatterns();
        public override ImmutableArray<IPatternOperation> DeconstructionSubpatterns
        {
            get
            {
                if (_lazyDeconstructionSubpatterns.IsDefault)
                {
                    ImmutableArray<IPatternOperation> deconstructionSubpatterns = CreateDeconstructionSubpatterns();
                    SetParentOperation(deconstructionSubpatterns, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyDeconstructionSubpatterns, deconstructionSubpatterns);
                }
                return _lazyDeconstructionSubpatterns;
            }
        }
        protected abstract ImmutableArray<IPropertySubpatternOperation> CreatePropertySubpatterns();
        public override ImmutableArray<IPropertySubpatternOperation> PropertySubpatterns
        {
            get
            {
                if (_lazyPropertySubpatterns.IsDefault)
                {
                    ImmutableArray<IPropertySubpatternOperation> propertySubpatterns = CreatePropertySubpatterns();
                    SetParentOperation(propertySubpatterns, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyPropertySubpatterns, propertySubpatterns);
                }
                return _lazyPropertySubpatterns;
            }
        }
    }
    internal sealed partial class DiscardPatternOperation : BasePatternOperation, IDiscardPatternOperation
    {
        internal DiscardPatternOperation(ITypeSymbol inputType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(inputType, OperationKind.DiscardPattern, semanticModel, syntax, type, constantValue, isImplicit) { }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitDiscardPattern(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitDiscardPattern(this, argument);
    }
    internal abstract partial class BaseSwitchExpressionOperation : Operation, ISwitchExpressionOperation
    {
        internal BaseSwitchExpressionOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.SwitchExpression, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Value { get; }
        public abstract ImmutableArray<ISwitchExpressionArmOperation> Arms { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
                foreach (var child in Arms)
                {
                    if (child is object) yield return child;
                }
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitSwitchExpression(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitSwitchExpression(this, argument);
    }
    internal sealed partial class SwitchExpressionOperation : BaseSwitchExpressionOperation, ISwitchExpressionOperation
    {
        internal SwitchExpressionOperation(IOperation value, ImmutableArray<ISwitchExpressionArmOperation> arms, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Value = SetParentOperation(value, this);
            Arms = SetParentOperation(arms, this);
        }
        public override IOperation Value { get; }
        public override ImmutableArray<ISwitchExpressionArmOperation> Arms { get; }
    }
    internal abstract partial class LazySwitchExpressionOperation : BaseSwitchExpressionOperation, ISwitchExpressionOperation
    {
        private IOperation _lazyValue = s_unset;
        private ImmutableArray<ISwitchExpressionArmOperation> _lazyArms;
        internal LazySwitchExpressionOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
        protected abstract ImmutableArray<ISwitchExpressionArmOperation> CreateArms();
        public override ImmutableArray<ISwitchExpressionArmOperation> Arms
        {
            get
            {
                if (_lazyArms.IsDefault)
                {
                    ImmutableArray<ISwitchExpressionArmOperation> arms = CreateArms();
                    SetParentOperation(arms, this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyArms, arms);
                }
                return _lazyArms;
            }
        }
    }
    internal abstract partial class BaseSwitchExpressionArmOperation : Operation, ISwitchExpressionArmOperation
    {
        internal BaseSwitchExpressionArmOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.SwitchExpressionArm, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Locals = locals;
        }
        public abstract IPatternOperation Pattern { get; }
        public abstract IOperation Guard { get; }
        public abstract IOperation Value { get; }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Pattern is object) yield return Pattern;
                if (Guard is object) yield return Guard;
                if (Value is object) yield return Value;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitSwitchExpressionArm(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitSwitchExpressionArm(this, argument);
    }
    internal sealed partial class SwitchExpressionArmOperation : BaseSwitchExpressionArmOperation, ISwitchExpressionArmOperation
    {
        internal SwitchExpressionArmOperation(IPatternOperation pattern, IOperation guard, IOperation value, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Pattern = SetParentOperation(pattern, this);
            Guard = SetParentOperation(guard, this);
            Value = SetParentOperation(value, this);
        }
        public override IPatternOperation Pattern { get; }
        public override IOperation Guard { get; }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazySwitchExpressionArmOperation : BaseSwitchExpressionArmOperation, ISwitchExpressionArmOperation
    {
        private IPatternOperation _lazyPattern = s_unsetPattern;
        private IOperation _lazyGuard = s_unset;
        private IOperation _lazyValue = s_unset;
        internal LazySwitchExpressionArmOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IPatternOperation CreatePattern();
        public override IPatternOperation Pattern
        {
            get
            {
                if (_lazyPattern == s_unsetPattern)
                {
                    IPatternOperation pattern = CreatePattern();
                    SetParentOperation(pattern, this);
                    Interlocked.CompareExchange(ref _lazyPattern, pattern, s_unsetPattern);
                }
                return _lazyPattern;
            }
        }
        protected abstract IOperation CreateGuard();
        public override IOperation Guard
        {
            get
            {
                if (_lazyGuard == s_unset)
                {
                    IOperation guard = CreateGuard();
                    SetParentOperation(guard, this);
                    Interlocked.CompareExchange(ref _lazyGuard, guard, s_unset);
                }
                return _lazyGuard;
            }
        }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BasePropertySubpatternOperation : Operation, IPropertySubpatternOperation
    {
        internal BasePropertySubpatternOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.PropertySubpattern, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Member { get; }
        public abstract IPatternOperation Pattern { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Member is object) yield return Member;
                if (Pattern is object) yield return Pattern;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitPropertySubpattern(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitPropertySubpattern(this, argument);
    }
    internal sealed partial class PropertySubpatternOperation : BasePropertySubpatternOperation, IPropertySubpatternOperation
    {
        internal PropertySubpatternOperation(IOperation member, IPatternOperation pattern, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Member = SetParentOperation(member, this);
            Pattern = SetParentOperation(pattern, this);
        }
        public override IOperation Member { get; }
        public override IPatternOperation Pattern { get; }
    }
    internal abstract partial class LazyPropertySubpatternOperation : BasePropertySubpatternOperation, IPropertySubpatternOperation
    {
        private IOperation _lazyMember = s_unset;
        private IPatternOperation _lazyPattern = s_unsetPattern;
        internal LazyPropertySubpatternOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateMember();
        public override IOperation Member
        {
            get
            {
                if (_lazyMember == s_unset)
                {
                    IOperation member = CreateMember();
                    SetParentOperation(member, this);
                    Interlocked.CompareExchange(ref _lazyMember, member, s_unset);
                }
                return _lazyMember;
            }
        }
        protected abstract IPatternOperation CreatePattern();
        public override IPatternOperation Pattern
        {
            get
            {
                if (_lazyPattern == s_unsetPattern)
                {
                    IPatternOperation pattern = CreatePattern();
                    SetParentOperation(pattern, this);
                    Interlocked.CompareExchange(ref _lazyPattern, pattern, s_unsetPattern);
                }
                return _lazyPattern;
            }
        }
    }
    internal abstract partial class BaseAggregateQueryOperation : Operation, IAggregateQueryOperation
    {
        internal BaseAggregateQueryOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.None, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Group { get; }
        public abstract IOperation Aggregation { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Group is object) yield return Group;
                if (Aggregation is object) yield return Aggregation;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitAggregateQuery(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitAggregateQuery(this, argument);
    }
    internal sealed partial class AggregateQueryOperation : BaseAggregateQueryOperation, IAggregateQueryOperation
    {
        internal AggregateQueryOperation(IOperation group, IOperation aggregation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Group = SetParentOperation(group, this);
            Aggregation = SetParentOperation(aggregation, this);
        }
        public override IOperation Group { get; }
        public override IOperation Aggregation { get; }
    }
    internal abstract partial class LazyAggregateQueryOperation : BaseAggregateQueryOperation, IAggregateQueryOperation
    {
        private IOperation _lazyGroup = s_unset;
        private IOperation _lazyAggregation = s_unset;
        internal LazyAggregateQueryOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateGroup();
        public override IOperation Group
        {
            get
            {
                if (_lazyGroup == s_unset)
                {
                    IOperation group = CreateGroup();
                    SetParentOperation(group, this);
                    Interlocked.CompareExchange(ref _lazyGroup, group, s_unset);
                }
                return _lazyGroup;
            }
        }
        protected abstract IOperation CreateAggregation();
        public override IOperation Aggregation
        {
            get
            {
                if (_lazyAggregation == s_unset)
                {
                    IOperation aggregation = CreateAggregation();
                    SetParentOperation(aggregation, this);
                    Interlocked.CompareExchange(ref _lazyAggregation, aggregation, s_unset);
                }
                return _lazyAggregation;
            }
        }
    }
    internal abstract partial class BaseFixedOperation : Operation, IFixedOperation
    {
        internal BaseFixedOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.None, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Locals = locals;
        }
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public abstract IVariableDeclarationGroupOperation Variables { get; }
        public abstract IOperation Body { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Variables is object) yield return Variables;
                if (Body is object) yield return Body;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitFixed(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitFixed(this, argument);
    }
    internal sealed partial class FixedOperation : BaseFixedOperation, IFixedOperation
    {
        internal FixedOperation(ImmutableArray<ILocalSymbol> locals, IVariableDeclarationGroupOperation variables, IOperation body, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
            Variables = SetParentOperation(variables, this);
            Body = SetParentOperation(body, this);
        }
        public override IVariableDeclarationGroupOperation Variables { get; }
        public override IOperation Body { get; }
    }
    internal abstract partial class LazyFixedOperation : BaseFixedOperation, IFixedOperation
    {
        private IVariableDeclarationGroupOperation _lazyVariables = s_unsetVariableDeclarationGroup;
        private IOperation _lazyBody = s_unset;
        internal LazyFixedOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(locals, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IVariableDeclarationGroupOperation CreateVariables();
        public override IVariableDeclarationGroupOperation Variables
        {
            get
            {
                if (_lazyVariables == s_unsetVariableDeclarationGroup)
                {
                    IVariableDeclarationGroupOperation variables = CreateVariables();
                    SetParentOperation(variables, this);
                    Interlocked.CompareExchange(ref _lazyVariables, variables, s_unsetVariableDeclarationGroup);
                }
                return _lazyVariables;
            }
        }
        protected abstract IOperation CreateBody();
        public override IOperation Body
        {
            get
            {
                if (_lazyBody == s_unset)
                {
                    IOperation body = CreateBody();
                    SetParentOperation(body, this);
                    Interlocked.CompareExchange(ref _lazyBody, body, s_unset);
                }
                return _lazyBody;
            }
        }
    }
    internal abstract partial class BaseNoPiaObjectCreationOperation : Operation, INoPiaObjectCreationOperation
    {
        internal BaseNoPiaObjectCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.None, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IObjectOrCollectionInitializerOperation Initializer { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Initializer is object) yield return Initializer;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitNoPiaObjectCreation(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitNoPiaObjectCreation(this, argument);
    }
    internal sealed partial class NoPiaObjectCreationOperation : BaseNoPiaObjectCreationOperation, INoPiaObjectCreationOperation
    {
        internal NoPiaObjectCreationOperation(IObjectOrCollectionInitializerOperation initializer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Initializer = SetParentOperation(initializer, this);
        }
        public override IObjectOrCollectionInitializerOperation Initializer { get; }
    }
    internal abstract partial class LazyNoPiaObjectCreationOperation : BaseNoPiaObjectCreationOperation, INoPiaObjectCreationOperation
    {
        private IObjectOrCollectionInitializerOperation _lazyInitializer = s_unsetObjectOrCollectionInitializer;
        internal LazyNoPiaObjectCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IObjectOrCollectionInitializerOperation CreateInitializer();
        public override IObjectOrCollectionInitializerOperation Initializer
        {
            get
            {
                if (_lazyInitializer == s_unsetObjectOrCollectionInitializer)
                {
                    IObjectOrCollectionInitializerOperation initializer = CreateInitializer();
                    SetParentOperation(initializer, this);
                    Interlocked.CompareExchange(ref _lazyInitializer, initializer, s_unsetObjectOrCollectionInitializer);
                }
                return _lazyInitializer;
            }
        }
    }
    internal sealed partial class PlaceholderOperation : Operation, IPlaceholderOperation
    {
        internal PlaceholderOperation(PlaceholderKind placeholderKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.None, semanticModel, syntax, type, constantValue, isImplicit)
        {
            PlaceholderKind = placeholderKind;
        }
        public PlaceholderKind PlaceholderKind { get; }
        public override IEnumerable<IOperation> Children => Array.Empty<IOperation>();
        public override void Accept(OperationVisitor visitor) => visitor.VisitPlaceholder(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitPlaceholder(this, argument);
    }
    internal abstract partial class BasePointerIndirectionReferenceOperation : Operation, IPointerIndirectionReferenceOperation
    {
        internal BasePointerIndirectionReferenceOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.None, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Pointer { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Pointer is object) yield return Pointer;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitPointerIndirectionReference(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitPointerIndirectionReference(this, argument);
    }
    internal sealed partial class PointerIndirectionReferenceOperation : BasePointerIndirectionReferenceOperation, IPointerIndirectionReferenceOperation
    {
        internal PointerIndirectionReferenceOperation(IOperation pointer, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Pointer = SetParentOperation(pointer, this);
        }
        public override IOperation Pointer { get; }
    }
    internal abstract partial class LazyPointerIndirectionReferenceOperation : BasePointerIndirectionReferenceOperation, IPointerIndirectionReferenceOperation
    {
        private IOperation _lazyPointer = s_unset;
        internal LazyPointerIndirectionReferenceOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreatePointer();
        public override IOperation Pointer
        {
            get
            {
                if (_lazyPointer == s_unset)
                {
                    IOperation pointer = CreatePointer();
                    SetParentOperation(pointer, this);
                    Interlocked.CompareExchange(ref _lazyPointer, pointer, s_unset);
                }
                return _lazyPointer;
            }
        }
    }
    internal abstract partial class BaseWithOperation : Operation, IWithOperation
    {
        internal BaseWithOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.None, semanticModel, syntax, type, constantValue, isImplicit) { }
        public abstract IOperation Body { get; }
        public abstract IOperation Value { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (Value is object) yield return Value;
                if (Body is object) yield return Body;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitWith(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitWith(this, argument);
    }
    internal sealed partial class WithOperation : BaseWithOperation, IWithOperation
    {
        internal WithOperation(IOperation body, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            Body = SetParentOperation(body, this);
            Value = SetParentOperation(value, this);
        }
        public override IOperation Body { get; }
        public override IOperation Value { get; }
    }
    internal abstract partial class LazyWithOperation : BaseWithOperation, IWithOperation
    {
        private IOperation _lazyBody = s_unset;
        private IOperation _lazyValue = s_unset;
        internal LazyWithOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IOperation CreateBody();
        public override IOperation Body
        {
            get
            {
                if (_lazyBody == s_unset)
                {
                    IOperation body = CreateBody();
                    SetParentOperation(body, this);
                    Interlocked.CompareExchange(ref _lazyBody, body, s_unset);
                }
                return _lazyBody;
            }
        }
        protected abstract IOperation CreateValue();
        public override IOperation Value
        {
            get
            {
                if (_lazyValue == s_unset)
                {
                    IOperation value = CreateValue();
                    SetParentOperation(value, this);
                    Interlocked.CompareExchange(ref _lazyValue, value, s_unset);
                }
                return _lazyValue;
            }
        }
    }
    internal abstract partial class BaseUsingVariableDeclarationOperation : Operation, IUsingVariableDeclarationOperation
    {
        internal BaseUsingVariableDeclarationOperation(bool isAsynchronous, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(OperationKind.UsingVariableDeclaration, semanticModel, syntax, type, constantValue, isImplicit)
        {
            IsAsynchronous = isAsynchronous;
        }
        public bool IsAsynchronous { get; }
        public abstract IVariableDeclarationGroupOperation DeclarationGroup { get; }
        public override IEnumerable<IOperation> Children
        {
            get
            {
                if (DeclarationGroup is object) yield return DeclarationGroup;
            }
        }
        public override void Accept(OperationVisitor visitor) => visitor.VisitUsingVariableDeclaration(this);
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitUsingVariableDeclaration(this, argument);
    }
    internal sealed partial class UsingVariableDeclarationOperation : BaseUsingVariableDeclarationOperation, IUsingVariableDeclarationOperation
    {
        internal UsingVariableDeclarationOperation(bool isAsynchronous, IVariableDeclarationGroupOperation declarationGroup, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isAsynchronous, semanticModel, syntax, type, constantValue, isImplicit)
        {
            DeclarationGroup = SetParentOperation(declarationGroup, this);
        }
        public override IVariableDeclarationGroupOperation DeclarationGroup { get; }
    }
    internal abstract partial class LazyUsingVariableDeclarationOperation : BaseUsingVariableDeclarationOperation, IUsingVariableDeclarationOperation
    {
        private IVariableDeclarationGroupOperation _lazyDeclarationGroup = s_unsetVariableDeclarationGroup;
        internal LazyUsingVariableDeclarationOperation(bool isAsynchronous, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
            : base(isAsynchronous, semanticModel, syntax, type, constantValue, isImplicit){ }
        protected abstract IVariableDeclarationGroupOperation CreateDeclarationGroup();
        public override IVariableDeclarationGroupOperation DeclarationGroup
        {
            get
            {
                if (_lazyDeclarationGroup == s_unsetVariableDeclarationGroup)
                {
                    IVariableDeclarationGroupOperation declarationGroup = CreateDeclarationGroup();
                    SetParentOperation(declarationGroup, this);
                    Interlocked.CompareExchange(ref _lazyDeclarationGroup, declarationGroup, s_unsetVariableDeclarationGroup);
                }
                return _lazyDeclarationGroup;
            }
        }
    }
    #endregion
    #region Visitors
    public abstract partial class OperationVisitor
    {
        public virtual void Visit(IOperation operation) => operation?.Accept(this);
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
        internal virtual void VisitWith(IWithOperation operation) => DefaultVisit(operation);
        public virtual void VisitUsingVariableDeclaration(IUsingVariableDeclarationOperation operation) => DefaultVisit(operation);
    }
    public abstract partial class OperationVisitor<TArgument, TResult>
    {
        public virtual TResult Visit(IOperation operation, TArgument argument) => operation is null ? default(TResult) : operation.Accept(this, argument);
        public virtual TResult DefaultVisit(IOperation operation, TArgument argument) => default(TResult);
        internal virtual TResult VisitNoneOperation(IOperation operation, TArgument argument) => default(TResult);
        public virtual TResult VisitInvalid(IInvalidOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitBlock(IBlockOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitSwitch(ISwitchOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitForEachLoop(IForEachLoopOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitForLoop(IForLoopOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitForToLoop(IForToLoopOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitWhileLoop(IWhileLoopOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitLabeled(ILabeledOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitBranch(IBranchOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitEmpty(IEmptyOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitReturn(IReturnOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitLock(ILockOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitTry(ITryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitUsing(IUsingOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitExpressionStatement(IExpressionStatementOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitLocalFunction(ILocalFunctionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitStop(IStopOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitEnd(IEndOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitRaiseEvent(IRaiseEventOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitLiteral(ILiteralOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitConversion(IConversionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitInvocation(IInvocationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitArrayElementReference(IArrayElementReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitLocalReference(ILocalReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitParameterReference(IParameterReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitFieldReference(IFieldReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitMethodReference(IMethodReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitPropertyReference(IPropertyReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitEventReference(IEventReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitUnaryOperator(IUnaryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitBinaryOperator(IBinaryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitConditional(IConditionalOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitCoalesce(ICoalesceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitAnonymousFunction(IAnonymousFunctionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitObjectCreation(IObjectCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitArrayCreation(IArrayCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitInstanceReference(IInstanceReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitIsType(IIsTypeOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitAwait(IAwaitOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitSimpleAssignment(ISimpleAssignmentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitCompoundAssignment(ICompoundAssignmentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitParenthesized(IParenthesizedOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitEventAssignment(IEventAssignmentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitConditionalAccess(IConditionalAccessOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitInterpolatedString(IInterpolatedStringOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitMemberInitializer(IMemberInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        [Obsolete("ICollectionElementInitializerOperation has been replaced with " + nameof(IInvocationOperation) + " and " + nameof(IDynamicInvocationOperation), error: true)]
        public virtual TResult VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitNameOf(INameOfOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitTuple(ITupleOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDynamicInvocation(IDynamicInvocationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitTranslatedQuery(ITranslatedQueryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDelegateCreation(IDelegateCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDefaultValue(IDefaultValueOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitTypeOf(ITypeOfOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitSizeOf(ISizeOfOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitAddressOf(IAddressOfOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitIsPattern(IIsPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitThrow(IThrowOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDeclarationExpression(IDeclarationExpressionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitOmittedArgument(IOmittedArgumentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitFieldInitializer(IFieldInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitVariableInitializer(IVariableInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitPropertyInitializer(IPropertyInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitParameterInitializer(IParameterInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitArrayInitializer(IArrayInitializerOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitVariableDeclarator(IVariableDeclaratorOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitVariableDeclaration(IVariableDeclarationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitArgument(IArgumentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitCatchClause(ICatchClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitSwitchCase(ISwitchCaseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDefaultCaseClause(IDefaultCaseClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitPatternCaseClause(IPatternCaseClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitRangeCaseClause(IRangeCaseClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitRelationalCaseClause(IRelationalCaseClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitInterpolation(IInterpolationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitConstantPattern(IConstantPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDeclarationPattern(IDeclarationPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitTupleBinaryOperator(ITupleBinaryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitMethodBodyOperation(IMethodBodyOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitConstructorBodyOperation(IConstructorBodyOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDiscardOperation(IDiscardOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitFlowCapture(IFlowCaptureOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitIsNull(IIsNullOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitCaughtException(ICaughtExceptionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitCoalesceAssignment(ICoalesceAssignmentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitRangeOperation(IRangeOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitReDim(IReDimOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitReDimClause(IReDimClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitRecursivePattern(IRecursivePatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDiscardPattern(IDiscardPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitSwitchExpression(ISwitchExpressionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitSwitchExpressionArm(ISwitchExpressionArmOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitPropertySubpattern(IPropertySubpatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult VisitAggregateQuery(IAggregateQueryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult VisitPlaceholder(IPlaceholderOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult VisitWith(IWithOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitUsingVariableDeclaration(IUsingVariableDeclarationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
    }
    #endregion
}
