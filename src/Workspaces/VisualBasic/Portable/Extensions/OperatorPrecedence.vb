' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
