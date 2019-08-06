// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
// < auto-generated />
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Microsoft.CodeAnalysis.Operations
{
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
        /// Locals declared within the switch operation with scope spanning across all <see cref="Cases" />.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
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
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        IOperation Body { get; }
        /// <summary>
        /// Declaration introduced or resource held by the using.
        /// </summary>
        IOperation Resources { get; }
        /// <summary>
        /// Locals declared within the <see cref="Resources" /> with scope spanning across this entire <see cref="IUsingOperation" />.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
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
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol OperatorMethod { get; }
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
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol OperatorMethod { get; }
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
        /// Arguments of the object creation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays.
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        ImmutableArray<IArgumentOperation> Arguments { get; }
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        IObjectOrCollectionInitializerOperation Initializer { get; }
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
        /// Kind of binary operation.
        /// </summary>
        BinaryOperatorKind OperatorKind { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol OperatorMethod { get; }
        /// <summary>
        /// <see langword="true" /> if this assignment contains a 'lifted' binary operation.
        /// </summary>
        bool IsLifted { get; }
        /// <summary>
        /// <see langword="true" /> if overflow checking is performed for the arithmetic operation.
        /// </summary>
        bool IsChecked { get; }
        /// <summary>
        /// Conversion applied to <see cref="IAssignmentOperation.Target" /> before the operation occurs.
        /// </summary>
        CommonConversion InConversion { get; }
        /// <summary>
        /// Conversion applied to the result of the binary operation, before it is assigned back to
        /// <see cref="IAssignmentOperation.Target" />.
        /// </summary>
        CommonConversion OutConversion { get; }
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
        /// Target of the assignment.
        /// </summary>
        IOperation Target { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol OperatorMethod { get; }
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
        /// Body of the exception handler.
        /// </summary>
        IBlockOperation Handler { get; }
        /// <summary>
        /// Locals declared by the <see cref="ExceptionDeclarationOrExpression" /> and/or <see cref="Filter" /> clause.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Type of the exception handled by the catch clause.
        /// </summary>
        ITypeSymbol ExceptionType { get; }
        /// <summary>
        /// Optional source for exception. This could be any of the following operation:
        /// 1. Declaration for the local catch variable bound to the caught exception (C# and VB) OR
        /// 2. Null, indicating no declaration or expression (C# and VB)
        /// 3. Reference to an existing local or parameter (VB) OR
        /// 4. Other expression for error scenarios (VB)
        /// </summary>
        IOperation ExceptionDeclarationOrExpression { get; }
        /// <summary>
        /// Filter operation to be executed to determine whether to handle the exception.
        /// </summary>
        IOperation Filter { get; }
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
        public virtual void VisitFlowCapture(IFlowCaptureOperation operation) => DefaultVisit(operation);
        public virtual void VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation) => DefaultVisit(operation);
        public virtual void VisitIsNull(IIsNullOperation operation) => DefaultVisit(operation);
        public virtual void VisitCaughtException(ICaughtExceptionOperation operation) => DefaultVisit(operation);
        public virtual void VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation) => DefaultVisit(operation);
        public virtual void VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation) => DefaultVisit(operation);
        public virtual void VisitCoalesceAssignment(ICoalesceAssignmentOperation operation) => DefaultVisit(operation);
        public virtual void VisitReDim(IReDimOperation operation) => DefaultVisit(operation);
        public virtual void VisitReDimClause(IReDimClauseOperation operation) => DefaultVisit(operation);
        public virtual void VisitDiscardPattern(IDiscardPatternOperation operation) => DefaultVisit(operation);
        public virtual void VisitSwitchExpression(ISwitchExpressionOperation operation) => DefaultVisit(operation);
        public virtual void VisitSwitchExpressionArm(ISwitchExpressionArmOperation operation) => DefaultVisit(operation);
        internal virtual void VisitAggregateQuery(IAggregateQueryOperation operation) => DefaultVisit(operation);
        internal virtual void VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation) => DefaultVisit(operation);
        internal virtual void VisitPlaceholder(IPlaceholderOperation operation) => DefaultVisit(operation);
        internal virtual void VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation) => DefaultVisit(operation);
        internal virtual void VisitWith(IWithOperation operation) => DefaultVisit(operation);
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
        public virtual TResult VisitFlowCapture(IFlowCaptureOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitIsNull(IIsNullOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitCaughtException(ICaughtExceptionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitCoalesceAssignment(ICoalesceAssignmentOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitReDim(IReDimOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitReDimClause(IReDimClauseOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitDiscardPattern(IDiscardPatternOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitSwitchExpression(ISwitchExpressionOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        public virtual TResult VisitSwitchExpressionArm(ISwitchExpressionArmOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult VisitAggregateQuery(IAggregateQueryOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult VisitPlaceholder(IPlaceholderOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation, TArgument argument) => DefaultVisit(operation, argument);
        internal virtual TResult VisitWith(IWithOperation operation, TArgument argument) => DefaultVisit(operation, argument);
    }
}
