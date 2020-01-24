' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Enum OperatorPrecedence
        PrecedenceNone = 0
        PrecedenceXor
        PrecedenceOr
        PrecedenceAnd
        PrecedenceNot
        PrecedenceRelational
        PrecedenceShift
        PrecedenceConcatenate
        PrecedenceAdd
        PrecedenceModulus
        PrecedenceIntegerDivide
        PrecedenceMultiply
        PrecedenceNegate
        PrecedenceExponentiate
    End Enum
End Namespace
