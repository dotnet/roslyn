' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class BoundAssignmentOperator
        Inherits BoundExpression

        Public Sub New(syntax As VisualBasicSyntaxNode, left As BoundExpression, right As BoundExpression, suppressObjectClone As Boolean, type As TypeSymbol, Optional hasErrors As Boolean = False)
            Me.New(syntax, left, leftOnTheRightOpt:=Nothing, right:=right, suppressObjectClone:=suppressObjectClone, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Sub New(syntax As VisualBasicSyntaxNode, left As BoundExpression, right As BoundExpression, suppressObjectClone As Boolean, Optional hasErrors As Boolean = False)
            Me.New(syntax, left, leftOnTheRightOpt:=Nothing, right:=right, suppressObjectClone:=suppressObjectClone, hasErrors:=hasErrors)
        End Sub

        Public Sub New(
            syntax As VisualBasicSyntaxNode,
            left As BoundExpression,
            leftOnTheRightOpt As BoundCompoundAssignmentTargetPlaceholder,
            right As BoundExpression,
            suppressObjectClone As Boolean,
            Optional hasErrors As Boolean = False
        )
            'NOTE: even though in general assignment returns the value of the Right,
            '      the type of the operator is the type of the Left as that is the type of the location 
            '      into which the Right is stored.
            '
            '       Dim x as Long
            '       Dim y as byte = 1
            '       Dim z := x := y       ' if this was legal assignment of an assignment, z would have type Long
            '
            '      properties and latebound assignments are special cased since they are semantically statements
            '
            'TODO: it could make sense to have BoundAssignmentStatement just for the purpose of 
            '      binding assignments. It would make invariants for both BoundAssignmentExpression and BoundAssignmentStatement simpler.
            Me.New(syntax, left, leftOnTheRightOpt, right:=right, suppressObjectClone:=suppressObjectClone,
                   type:=If(left.IsPropertyOrXmlPropertyAccess(),
                            left.GetPropertyOrXmlProperty().ContainingAssembly.GetSpecialType(SpecialType.System_Void),
                            If(left.IsLateBound,
                               left.Type.ContainingAssembly.GetSpecialType(SpecialType.System_Void),
                               left.Type)),
                   hasErrors:=hasErrors)
        End Sub

#If DEBUG Then
        Private Sub Validate()
            If Not HasErrors Then
                Debug.Assert(Left.IsLValue OrElse Left.IsPropertyOrXmlPropertyAccess() OrElse Left.IsLateBound)

                If Left.IsPropertyOrXmlPropertyAccess() Then
                    Debug.Assert(Left.GetAccessKind() = If(LeftOnTheRightOpt Is Nothing, PropertyAccessKind.Set, PropertyAccessKind.Set Or PropertyAccessKind.Get))
                    Debug.Assert(Type.IsVoidType())
                Else
                    Select Case Left.Kind
                        Case BoundKind.LateMemberAccess
                            Debug.Assert(Left.GetLateBoundAccessKind() = If(LeftOnTheRightOpt Is Nothing, LateBoundAccessKind.Set, LateBoundAccessKind.Set Or LateBoundAccessKind.Get))

                        Case BoundKind.LateInvocation
                            Dim invocation = DirectCast(Left, BoundLateInvocation)
                            Debug.Assert(invocation.AccessKind = If(LeftOnTheRightOpt Is Nothing, LateBoundAccessKind.Set, LateBoundAccessKind.Set Or LateBoundAccessKind.Get))

                            If Not invocation.ArgumentsOpt.IsDefault Then
                                For Each arg In invocation.ArgumentsOpt
                                    Debug.Assert(Not arg.IsSupportingAssignment())
                                Next
                            End If

                        Case Else
                            Debug.Assert(Not Left.IsLateBound)
                    End Select
                End If

                Debug.Assert(Left.Type.IsSameTypeIgnoringCustomModifiers(Right.Type))
            End If

            Right.AssertRValue()
            Debug.Assert(Left.IsPropertyOrXmlPropertyAccess() OrElse
                         Left.IsLateBound OrElse
                         Left.Type.IsSameTypeIgnoringCustomModifiers(Type) OrElse
                         (Type.IsVoidType() AndAlso Syntax.Kind = SyntaxKind.MidAssignmentStatement) OrElse
                         (Left.Kind = BoundKind.FieldAccess AndAlso
                                DirectCast(Left, BoundFieldAccess).FieldSymbol.AssociatedSymbol.Kind = SymbolKind.Property AndAlso
                                Type.IsVoidType()))

        End Sub
#End If

    End Class

End Namespace
