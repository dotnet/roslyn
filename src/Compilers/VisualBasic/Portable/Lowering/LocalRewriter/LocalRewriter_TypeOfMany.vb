' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitTypeOfMany(node As BoundTypeOfMany) As BoundNode
            Return Rewrite_AsMultipleTypeOfs(node)
        End Function

        Function Rewrite_AsMultipleTypeOfs(node As BoundTypeOfMany) As BoundNode
            If node.IsTypeOfIsNotExpression Then
                Return Rewrite_AsMultiple_TypeOfIsNot(node)
            Else
                Return Rewrite_AsMultiple_TypeOfIs(node)
            End If
        End Function

#Region "Rewrite as Multiple (TypeOf expr Is _type_) OrElse ..."
        Private Function Make_OrElse(syntax As SyntaxNode, left As BoundExpression, right As BoundExpression) As BoundExpression
            Dim _OrElse_ = New BoundBinaryOperator(syntax, BinaryOperatorKind.OrElse, left, right, False, GetSpecialType(SpecialType.System_Boolean))
            Return _OrElse_
        End Function

        Private Function Make_TypeOfIs(syntax As SyntaxNode, left As BoundExpression, right As BoundTypeExpression) As BoundExpression
            Dim _Is_ = New BoundBinaryOperator(syntax, BinaryOperatorKind.Is, left, right, False, GetSpecialType(SpecialType.System_Boolean))
            Dim _TypeOf_ As BoundExpression = New BoundTypeOf(syntax, left.Type, _Is_, False, right.Type)
            Return _TypeOf_
        End Function

        Private Function Rewrite_AsMultiple_TypeOfIs(node As BoundTypeOfMany) As BoundNode
            Dim syn = DirectCast(node.Syntax, TypeOfManyExpressionSyntax)
            Dim Current As BoundExpression = Nothing '= New BoundLiteral(syn, ConstantValue.False, GetSpecialType(SpecialType.System_Boolean))
            Dim first = True
            For Each _TypeOf_ In node.TargetTypes
                Dim [Next] As BoundExpression
                If Not first Then
                    [Next] = Make_OrElse(syn, Current, _TypeOf_)
                Else
                    [Next] = _TypeOf_
                    first = False
                End If
                Current = [Next]
            Next
            '       Current.SetWasCompilerGenerated()
            Return Current

        End Function
#End Region

#Region "Rewrite as Multiple (TypeOf expr IsNot _type_) AndAlso ..."
        Private Function Make_AndAlso(syntax As SyntaxNode, left As BoundExpression, right As BoundExpression) As BoundExpression
            Dim _AndAlso_ = New BoundBinaryOperator(syntax, BinaryOperatorKind.AndAlso, left, right, False, GetSpecialType(SpecialType.System_Boolean))
            Return _AndAlso_
        End Function

        Private Function Make_TypeOfIsNot(syntax As SyntaxNode, left As BoundExpression, right As BoundExpression) As BoundExpression
            Dim _IsNot_ = New BoundBinaryOperator(syntax, BinaryOperatorKind.IsNot, left, right, True, GetSpecialType(SpecialType.System_Boolean))
            Dim _TypeOf_ As BoundExpression = New BoundTypeOf(syntax, left.Type, _IsNot_, True, right.Type)
            Return _TypeOf_
        End Function

        Private Function Rewrite_AsMultiple_TypeOfIsNot(node As BoundTypeOfMany) As BoundNode
            Dim syn = DirectCast(node.Syntax, TypeOfManyExpressionSyntax)

            Dim Current As BoundExpression = Nothing ' New BoundLiteral(syn, ConstantValue.False, GetSpecialType(SpecialType.System_Boolean))
            Dim first = True
            For Each _TypeOf_ In node.TargetTypes
                Dim [Next] As BoundExpression
                If Not first Then
                    [Next] = Make_AndAlso(syn, Current, _TypeOf_)
                Else
                    [Next] = _TypeOf_
                    first = False
                End If
                Current = [Next]
            Next
            '   Current.SetWasCompilerGenerated()
            Return Current
        End Function
#End Region
    End Class
End Namespace
