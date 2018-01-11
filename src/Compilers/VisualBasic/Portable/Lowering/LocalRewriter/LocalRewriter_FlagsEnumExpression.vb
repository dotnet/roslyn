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
            ' IsSet <== (Flags And Flag) = Flag
            Dim flagPart = node.EnumFlag.MakeRValue
            Dim _AND_ = MakeBinaryExpression(node.Syntax, BinaryOperatorKind.And, node.EnumFlags.MakeRValue, flagPart, False, node.EnumFlags.Type).MakeCompilerGenerated
            Dim _EQ_ = MakeBinaryExpression(node.Syntax, BinaryOperatorKind.Equals, _AND_.MakeRValue, flagPart, False, GetSpecialType(SpecialType.System_Boolean)).MakeCompilerGenerated
            Return _EQ_.MakeRValue
        End Function

        Private Function Rewrite_As_FlagSet(node As BoundFlagsEnumOperationExpressionSyntax) As BoundNode
            ' WithSetFlag <== Flags Or Flag
            Dim flagPart = node.EnumFlag.MakeRValue
            Dim _Or_ = MakeBinaryExpression(node.Syntax, BinaryOperatorKind.Or, node.EnumFlags.MakeRValue, flagPart, False, node.EnumFlags.Type).MakeCompilerGenerated
            Return _Or_.MakeRValue
        End Function

        Private Function Rewrite_As_FlagClr(node As BoundFlagsEnumOperationExpressionSyntax) As BoundNode
            ' WithClearedFlag <== Flags And (Not Flag)
            Dim flagPart = node.EnumFlag.MakeRValue
            Dim _NOT_ = New BoundUnaryOperator(node.Syntax, UnaryOperatorKind.Not, flagPart, False, node.EnumFlags.Type)
            Dim _AND_ = MakeBinaryExpression(node.Syntax, BinaryOperatorKind.And, node.EnumFlags.MakeRValue, _NOT_, False, node.EnumFlags.Type).MakeCompilerGenerated
            Return _AND_.MakeRValue
        End Function

    End Class
End Namespace
