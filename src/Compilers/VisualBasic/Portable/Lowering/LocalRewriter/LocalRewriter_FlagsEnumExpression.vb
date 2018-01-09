' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter

        Public Overrides Function VisitFlagsEnumOperationExpressionSyntax(node As BoundFlagsEnumOperationExpressionSyntax) As BoundNode
            If _inExpressionLambda Then
                ' just preserve the node to report an error in ExpressionLambdaRewriter
                Return MyBase.VisitFlagsEnumOperationExpressionSyntax(node)
            End If
            'If node.HasErrors Then Return MyBase.VisitFlagsEnumOperationExpressionSyntax(node)
            Return Rewrite_As_IsSet(node)
        End Function

        Private Function Rewrite_As_IsSet(node As BoundFlagsEnumOperationExpressionSyntax) As BoundNode
            Dim _AND_ = New BoundBinaryOperator(node.Syntax, BinaryOperatorKind.And, node.EnumFlags, node.EnumFlag, False, node.EnumFlags.Type)
            Dim pAnd = New BoundParenthesized(node.Syntax, _AND_, _AND_.Type)
            Dim _EQ_ = New BoundBinaryOperator(node.Syntax, BinaryOperatorKind.Equals, pAnd, node.EnumFlag, False, node.Type)
            Dim PEq = New BoundParenthesized(node.Syntax, _EQ_, _EQ_.Type)
            Dim MCG = PEq.MakeCompilerGenerated
            Return MCG.MakeRValue()
        End Function
    End Class
End Namespace
