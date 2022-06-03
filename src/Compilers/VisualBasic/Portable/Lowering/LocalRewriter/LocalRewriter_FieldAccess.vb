' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitFieldAccess(node As BoundFieldAccess) As BoundNode
            Dim rewrittenReceiver As BoundExpression = If(node.FieldSymbol.IsShared, Nothing, Me.VisitExpressionNode(node.ReceiverOpt))

            If node.FieldSymbol.IsTupleField Then
                Return MakeTupleFieldAccess(node.Syntax, node.FieldSymbol, rewrittenReceiver, node.ConstantValueOpt, node.IsLValue)
            End If

            Return node.Update(rewrittenReceiver, node.FieldSymbol, node.IsLValue, node.SuppressVirtualCalls, constantsInProgressOpt:=Nothing, node.Type)
        End Function

        ''' <summary>
        ''' Converts access to a tuple instance into access into the underlying ValueTuple(s).
        '''
        ''' For instance, tuple.Item8
        ''' produces fieldAccess(field=Item1, receiver=fieldAccess(field=Rest, receiver=ValueTuple for tuple))
        ''' </summary>
        Private Function MakeTupleFieldAccess(
            syntax As SyntaxNode,
            tupleField As FieldSymbol,
            rewrittenReceiver As BoundExpression,
            constantValueOpt As ConstantValue,
            isLValue As Boolean) As BoundExpression

            Dim tupleType = tupleField.ContainingType

            Dim currentLinkType As NamedTypeSymbol = tupleType.TupleUnderlyingType
            Dim underlyingField As FieldSymbol = tupleField.TupleUnderlyingField

            If underlyingField Is Nothing Then
                ' Use-site error must have been reported elsewhere.
                Return MakeBadFieldAccess(syntax, tupleField, rewrittenReceiver)
            End If

            If Not TypeSymbol.Equals(underlyingField.ContainingType, currentLinkType, TypeCompareKind.ConsiderEverything) Then
                Dim wellKnownTupleRest As WellKnownMember = TupleTypeSymbol.GetTupleTypeMember(TupleTypeSymbol.RestPosition, TupleTypeSymbol.RestPosition)
                Dim tupleRestField = DirectCast(TupleTypeSymbol.GetWellKnownMemberInType(currentLinkType.OriginalDefinition, wellKnownTupleRest, _diagnostics, syntax), FieldSymbol)

                If tupleRestField Is Nothing Then
                    ' error tolerance for cases when Rest is missing
                    Return MakeBadFieldAccess(syntax, tupleField, rewrittenReceiver)
                End If

                ' make nested field accesses to Rest
                Do
                    Dim nestedFieldSymbol As FieldSymbol = tupleRestField.AsMember(currentLinkType)
                    rewrittenReceiver = New BoundFieldAccess(syntax, rewrittenReceiver, nestedFieldSymbol, isLValue, nestedFieldSymbol.Type)

                    currentLinkType = currentLinkType.TypeArgumentsNoUseSiteDiagnostics(TupleTypeSymbol.RestPosition - 1).TupleUnderlyingType
                Loop While Not TypeSymbol.Equals(underlyingField.ContainingType, currentLinkType, TypeCompareKind.ConsiderEverything)

            End If

            ' make a field access for the most local access
            Return New BoundFieldAccess(syntax, rewrittenReceiver, underlyingField, isLValue, underlyingField.Type)
        End Function

        Private Shared Function MakeBadFieldAccess(syntax As SyntaxNode, tupleField As FieldSymbol, rewrittenReceiver As BoundExpression) As BoundBadExpression
            Return New BoundBadExpression(
                                    syntax,
                                    LookupResultKind.Empty,
                                    ImmutableArray.Create(Of Symbol)(tupleField),
                                    ImmutableArray.Create(rewrittenReceiver),
                                    tupleField.Type,
                                    hasErrors:=True)
        End Function
    End Class
End Namespace
