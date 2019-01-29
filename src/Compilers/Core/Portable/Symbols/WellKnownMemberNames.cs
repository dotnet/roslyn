﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the member names known to the compiler (such as <c>.ctor</c> or <c>op_Explicit</c>).
    /// </summary>
    public static class WellKnownMemberNames
    {
        /// <summary>
        /// Name of the enum backing field.
        /// </summary>
        public const string EnumBackingFieldName = "value__";

        /// <summary>
        /// The name assigned to an instance constructor.
        /// </summary>
        public const string InstanceConstructorName = ".ctor";

        /// <summary>
        /// The name assigned to the static constructor.
        /// </summary>
        public const string StaticConstructorName = ".cctor";

        /// <summary>
        /// The symbol name assigned to all indexers, other than explicit interface implementations.
        /// </summary>
        /// <remarks>
        /// Will not correspond to the name that appears in metadata.
        /// </remarks>
        public const string Indexer = "this[]";

        /// <summary>
        /// The name assigned to the destructor.
        /// </summary>
        public const string DestructorName = "Finalize";

        /// <summary>
        /// The name assigned to the delegate <c>Invoke</c> method.
        /// </summary>
        public const string DelegateInvokeName = "Invoke";

        /// <summary>
        /// The name assigned to the delegate <c>BeginInvoke</c> method.
        /// </summary>
        public const string DelegateBeginInvokeName = "BeginInvoke";

        /// <summary>
        /// The name assigned to the delegate <c>EndInvoke</c> method.
        /// </summary>
        public const string DelegateEndInvokeName = "EndInvoke";

        /// <summary>
        /// The name of an entry point method.
        /// </summary>
        public const string EntryPointMethodName = "Main";

        /// <summary>
        /// The default fully qualified name of a <c>Script</c> class.
        /// </summary>
        public const string DefaultScriptClassName = "Script";

        /// <summary>
        /// The name assigned to <c>Object.ToString</c> method.
        /// </summary>
        public const string ObjectToString = "ToString";

        /// <summary>
        /// The name assigned to <c>Object.Equals</c> method.
        /// </summary>
        public const string ObjectEquals = "Equals";

        /// <summary>
        /// The name assigned to <c>Object.GetHashCode</c> method.
        /// </summary>
        public const string ObjectGetHashCode = "GetHashCode";

        /// <summary>
        /// The name assigned to an implicit (widening) conversion.
        /// </summary>
        public const string ImplicitConversionName = "op_Implicit";

        /// <summary>
        /// The name assigned to an explicit (narrowing) conversion.
        /// </summary>
        public const string ExplicitConversionName = "op_Explicit";

        /// <summary>
        /// The name assigned to the Addition operator.
        /// </summary>
        public const string AdditionOperatorName = "op_Addition";

        /// <summary>
        /// The name assigned to the BitwiseAnd operator.
        /// </summary>
        public const string BitwiseAndOperatorName = "op_BitwiseAnd";

        /// <summary>
        /// The name assigned to the BitwiseOr operator.
        /// </summary>
        public const string BitwiseOrOperatorName = "op_BitwiseOr";

        /// <summary>
        /// The name assigned to the Decrement operator.
        /// </summary>
        public const string DecrementOperatorName = "op_Decrement";

        /// <summary>
        /// The name assigned to the Division operator.
        /// </summary>
        public const string DivisionOperatorName = "op_Division";

        /// <summary>
        /// The name assigned to the Equality operator.
        /// </summary>
        public const string EqualityOperatorName = "op_Equality";

        /// <summary>
        /// The name assigned to the ExclusiveOr operator.
        /// </summary>
        public const string ExclusiveOrOperatorName = "op_ExclusiveOr";

        /// <summary>
        /// The name assigned to the False operator.
        /// </summary>
        public const string FalseOperatorName = "op_False";

        /// <summary>
        /// The name assigned to the GreaterThan operator.
        /// </summary>
        public const string GreaterThanOperatorName = "op_GreaterThan";

        /// <summary>
        /// The name assigned to the GreaterThanOrEqual operator.
        /// </summary>
        public const string GreaterThanOrEqualOperatorName = "op_GreaterThanOrEqual";

        /// <summary>
        /// The name assigned to the Increment operator.
        /// </summary>
        public const string IncrementOperatorName = "op_Increment";

        /// <summary>
        /// The name assigned to the Inequality operator.
        /// </summary>
        public const string InequalityOperatorName = "op_Inequality";

        /// <summary>
        /// The name assigned to the LeftShift operator.
        /// </summary>
        public const string LeftShiftOperatorName = "op_LeftShift";

        /// <summary>
        /// The name assigned to the UnsignedLeftShift operator.
        /// </summary>
        public const string UnsignedLeftShiftOperatorName = "op_UnsignedLeftShift";

        /// <summary>
        /// The name assigned to the LessThan operator.
        /// </summary>
        public const string LessThanOperatorName = "op_LessThan";

        /// <summary>
        /// The name assigned to the LessThanOrEqual operator.
        /// </summary>
        public const string LessThanOrEqualOperatorName = "op_LessThanOrEqual";

        /// <summary>
        /// The name assigned to the LogicalNot operator.
        /// </summary>
        public const string LogicalNotOperatorName = "op_LogicalNot";

        /// <summary>
        /// The name assigned to the LogicalOr operator.
        /// </summary>
        public const string LogicalOrOperatorName = "op_LogicalOr";

        /// <summary>
        /// The name assigned to the LogicalAnd operator.
        /// </summary>
        public const string LogicalAndOperatorName = "op_LogicalAnd";

        /// <summary>
        /// The name assigned to the Modulus operator.
        /// </summary>
        public const string ModulusOperatorName = "op_Modulus";

        /// <summary>
        /// The name assigned to the Multiply operator.
        /// </summary>
        public const string MultiplyOperatorName = "op_Multiply";

        /// <summary>
        /// The name assigned to the OnesComplement operator.
        /// </summary>
        public const string OnesComplementOperatorName = "op_OnesComplement";

        /// <summary>
        /// The name assigned to the RightShift operator.
        /// </summary>
        public const string RightShiftOperatorName = "op_RightShift";

        /// <summary>
        /// The name assigned to the UnsignedRightShift operator.
        /// </summary>
        public const string UnsignedRightShiftOperatorName = "op_UnsignedRightShift";

        /// <summary>
        /// The name assigned to the Subtraction operator.
        /// </summary>
        public const string SubtractionOperatorName = "op_Subtraction";

        /// <summary>
        /// The name assigned to the True operator.
        /// </summary>
        public const string TrueOperatorName = "op_True";

        /// <summary>
        /// The name assigned to the UnaryNegation operator.
        /// </summary>
        public const string UnaryNegationOperatorName = "op_UnaryNegation";

        /// <summary>
        /// The name assigned to the UnaryPlus operator.
        /// </summary>
        public const string UnaryPlusOperatorName = "op_UnaryPlus";

        /// <summary>
        /// The name assigned to the Concatenate operator.
        /// </summary>
        public const string ConcatenateOperatorName = "op_Concatenate";

        /// <summary>
        /// The name assigned to the Exponent operator.
        /// </summary>
        public const string ExponentOperatorName = "op_Exponent";

        /// <summary>
        /// The name assigned to the IntegerDivision operator.
        /// </summary>
        public const string IntegerDivisionOperatorName = "op_IntegerDivision";

        /// <summary>
        /// The name assigned to the <c>Like</c> operator.
        /// </summary>
        public const string LikeOperatorName = "op_Like";

        /// <summary>
        /// The required name for the <c>GetEnumerator</c> method used in a ForEach statement.
        /// </summary>
        public const string GetEnumeratorMethodName = "GetEnumerator";

        /// <summary>
        /// The required name for the <c>GetAsyncEnumerator</c> method used in a ForEach statement.
        /// </summary>
        public const string GetAsyncEnumeratorMethodName = "GetAsyncEnumerator";

        /// <summary>
        /// The required name for the <c>MoveNextAsync</c> method used in a ForEach-await statement.
        /// </summary>
        public const string MoveNextAsyncMethodName = "MoveNextAsync";

        /// <summary>
        /// The required name for the <c>Deconstruct</c> method used in a deconstruction.
        /// </summary>
        public const string DeconstructMethodName = "Deconstruct";

        /// <summary>
        /// The required name for the <c>MoveNext</c> method used in a ForEach statement.
        /// </summary>
        public const string MoveNextMethodName = "MoveNext";

        /// <summary>
        /// The required name for the <c>Current</c> property used in a ForEach statement.
        /// </summary>
        public const string CurrentPropertyName = "Current";

        /// <summary>
        /// The required name for the <see cref="Nullable{T}.Value"/> property used in
        /// a ForEach statement when the collection is a nullable struct.
        /// </summary>
        public const string ValuePropertyName = "Value";

        /// <summary>
        /// The name for the <c>Add</c> method to be invoked for each element in a collection initializer expression
        /// (see C# Specification, §7.6.10.3 Collection initializers).
        /// </summary>
        public const string CollectionInitializerAddMethodName = "Add";

        /// <summary>
        /// The required name for the <c>GetAwaiter</c> method used to obtain an awaiter for a task
        /// (see C# Specification, §7.7.7.1 Awaitable expressions).
        /// </summary>
        public const string GetAwaiter = nameof(GetAwaiter);

        /// <summary>
        /// The required name for the <c>IsCompleted</c> property used to determine if a task is already complete
        /// (see C# Specification, §7.7.7.1 Awaitable expressions).
        /// </summary>
        public const string IsCompleted = nameof(IsCompleted);

        /// <summary>
        /// The required name for the <c>GetResult</c> method used to obtain the outcome of a task once it is complete
        /// (see C# Specification, §7.7.7.1 Awaitable expressions).
        /// </summary>
        public const string GetResult = nameof(GetResult);

        /// <summary>
        /// The name of the <see cref="INotifyCompletion.OnCompleted"/> method used to register a resumption delegate
        /// (see C# Specification, §7.7.7.1 Awaitable expressions).
        /// </summary>
        public const string OnCompleted = nameof(OnCompleted);

        /// <summary>
        /// The required name for the <c>Dispose</c> method used in a Using statement.
        /// </summary>
        public const string DisposeMethodName = "Dispose";

        /// <summary>
        /// The required name for the <c>DisposeAsync</c> method used in an await using statement.
        /// </summary>
        public const string DisposeAsyncMethodName = "DisposeAsync";
    }
}
