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
            'If node.HasErrors Then Return MyBase.VisitFlagsEnumOperationExpressionSyntax(node
            Dim EnumFlags As BoundExpression = VisitExpression(node.EnumFlags).MakeRValue
            Dim flagPart As BoundExpression = VisitExpression(node.EnumFlag).MakeRValue
            Select Case node.Op
                Case FlagsEnumOperatorKind.IsAny : Return Rewrite_As_IsAny(node, EnumFlags, flagPart)
                Case FlagsEnumOperatorKind.IsSet : Return Rewrite_As_IsSet(node, EnumFlags, flagPart)
                Case FlagsEnumOperatorKind.Clear : Return Rewrite_As_FlagClr(node, EnumFlags, flagPart)
                Case FlagsEnumOperatorKind.[Set] : Return Rewrite_As_FlagSet(node, EnumFlags, flagPart)
            End Select
            Return MyBase.VisitFlagsEnumOperationExpressionSyntax(node)
        End Function

        Private Function Rewrite_As_IsAny(node As BoundFlagsEnumOperationExpressionSyntax, EnumFlags As BoundExpression, flagPart As BoundExpression) As BoundNode
            ' IsAny <== (Flags And Flag) <> 0
            Dim _AND_ = MakeBinaryExpression(node.Syntax, BinaryOperatorKind.And, EnumFlags, flagPart, False, EnumFlags.Type).MakeCompilerGenerated
            Dim Zero = New BoundLiteral(node.Syntax, ConstantValue.Create(0), EnumFlags.Type).MakeCompilerGenerated
            Dim _EQ_ = MakeBinaryExpression(node.Syntax, BinaryOperatorKind.NotEquals, _AND_.MakeRValue, Zero, False, GetSpecialType(SpecialType.System_Boolean)).MakeCompilerGenerated
            Return _EQ_.MakeRValue
        End Function

        Private Function Rewrite_As_IsSet(node As BoundFlagsEnumOperationExpressionSyntax, EnumFlags As BoundExpression, flagPart As BoundExpression) As BoundNode
            ' IsSet <== (Flags And Flag) = Flag
            Dim _AND_ = MakeBinaryExpression(node.Syntax, BinaryOperatorKind.And, EnumFlags, flagPart, False, EnumFlags.Type).MakeCompilerGenerated
            Dim _EQ_ = MakeBinaryExpression(node.Syntax, BinaryOperatorKind.Equals, _AND_.MakeRValue, flagPart, False, GetSpecialType(SpecialType.System_Boolean)).MakeCompilerGenerated
            Return _EQ_.MakeRValue
        End Function

        Private Function Rewrite_As_FlagSet(node As BoundFlagsEnumOperationExpressionSyntax, EnumFlags As BoundExpression, flagPart As BoundExpression) As BoundNode
            ' WithSetFlag <== Flags Or Flag
            Dim _Or_ = MakeBinaryExpression(node.Syntax, BinaryOperatorKind.Or, EnumFlags, flagPart, False, EnumFlags.Type).MakeCompilerGenerated
            Return _Or_.MakeRValue
        End Function

        Private Function Rewrite_As_FlagClr(node As BoundFlagsEnumOperationExpressionSyntax, EnumFlags As BoundExpression, flagPart As BoundExpression) As BoundNode
            ' WithClearedFlag <== Flags And (Not Flag)
            Dim _NOT_ = New BoundUnaryOperator(node.Syntax, UnaryOperatorKind.Not, flagPart, False, EnumFlags.Type)
            Dim _AND_ = MakeBinaryExpression(node.Syntax, BinaryOperatorKind.And, EnumFlags.MakeRValue, _NOT_, False, EnumFlags.Type).MakeCompilerGenerated
            Return _AND_.MakeRValue
        End Function

    End Class
End Namespace
