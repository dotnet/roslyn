' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    <Flags()>
    Friend Enum UnaryOperatorKind
        Plus = 1
        Minus = 2
        [Not] = 3
        IntrinsicOpMask = &H3

        Lifted = &H4
        UserDefined = &H8

        Implicit = &H10
        Explicit = &H20
        IsTrue = &H30
        IsFalse = &H40

        OpMask = IntrinsicOpMask Or &H70

        [Error] = &H80
    End Enum

    <Flags()>
    Friend Enum BinaryOperatorKind

        Add = 1
        Concatenate = 2
        [Like] = 3
        Equals = 4
        NotEquals = 5
        LessThanOrEqual = 6
        GreaterThanOrEqual = 7
        LessThan = 8
        GreaterThan = 9
        Subtract = 10
        Multiply = 11
        Power = 12
        Divide = 13
        Modulo = 14
        IntegerDivide = 15
        LeftShift = 16
        RightShift = 17
        [Xor] = 18
        [Or] = 19
        [OrElse] = 20
        [And] = 21
        [AndAlso] = 22
        [Is] = 23
        [IsNot] = 24

        OpMask = &H1F

        Lifted = &H20
        CompareText = &H40
        UserDefined = &H80
        [Error] = &H100

        ''' <summary>
        ''' Built-in binary operators bound in "OperandOfConditionalBranch" mode are marked with
        ''' this flag by the Binder, which makes them eligible for some optimizations in
        ''' <see cref="LocalRewriter.VisitNullableIsTrueOperator(BoundNullableIsTrueOperator)"/> 
        ''' </summary>
        IsOperandOfConditionalBranch = &H200

        ''' <summary>
        ''' <see cref="LocalRewriter.AdjustIfOptimizableForConditionalBranch"/> marks built-in binary operators
        ''' with this flag in order to inform <see cref="LocalRewriter.VisitBinaryOperator"/> that the operator
        ''' should be a subject for optimization around use of three-valued Boolean logic.
        ''' The optimization should be applied only when we are absolutely sure that we will "snap" Null to false.
        ''' That is when we actually have the <see cref="BoundNullableIsTrueOperator"/> as the ancestor and no user defined operators
        ''' in between. We simply don't know that information during binding because we haven't bound binary operators
        ''' up the hierarchy yet. So the optimization is triggered by the fact of snapping Null to false
        ''' (getting to the <see cref="LocalRewriter.VisitNullableIsTrueOperator"/> method) and then we are making
        ''' sure we don't have anything unexpected in between.
        ''' </summary>
        OptimizableForConditionalBranch = &H400
    End Enum

End Namespace

