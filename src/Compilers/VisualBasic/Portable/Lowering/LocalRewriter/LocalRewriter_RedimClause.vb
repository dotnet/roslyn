' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitRedimClause(node As BoundRedimClause) As BoundNode
            '  array type must be known if the node is valid
            Debug.Assert(node.ArrayTypeOpt IsNot Nothing)

            '  build expression returning created (and optionally initialized) array
            Dim valueBeingAssigned As BoundExpression = New BoundArrayCreation(node.Syntax,
                                                                               node.Indices, Nothing, node.ArrayTypeOpt)

            Dim temporaries As ArrayBuilder(Of SynthesizedLocal) = Nothing
            Dim assignmentTarget = node.Operand
            Dim useSiteInfo = GetNewCompoundUseSiteInfo()

            Dim copyArrayUtilityMethod As MethodSymbol = Nothing
            If node.Preserve AndAlso TryGetWellknownMember(copyArrayUtilityMethod, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Utils__CopyArray, node.Syntax) Then
                ' build a call to Microsoft.VisualBasic.CompilerServices.Utils.CopyArray

                '  use the operand twice
                temporaries = ArrayBuilder(Of SynthesizedLocal).GetInstance()
                Dim result As UseTwiceRewriter.Result = UseTwiceRewriter.UseTwice(Me._currentMethodOrLambda, assignmentTarget, isForRegularCompoundAssignment:=True, temporaries)

                '  the first to be used as an assignment target
                assignmentTarget = result.First
                '  the second will be used for accessing the array's current value
                Dim arrayValueAccess = result.Second

                '  make an r-value from array value access
                If arrayValueAccess.Kind = BoundKind.PropertyAccess Then
                    arrayValueAccess = DirectCast(arrayValueAccess, BoundPropertyAccess).SetAccessKind(PropertyAccessKind.Get)
                ElseIf arrayValueAccess.IsLateBound() Then
                    arrayValueAccess = arrayValueAccess.SetLateBoundAccessKind(LateBoundAccessKind.Get)
                Else
                    arrayValueAccess = arrayValueAccess.MakeRValue()
                End If

                '  System.Array type
                Dim systemArray = copyArrayUtilityMethod.Parameters(0).Type

                '  add conversion
                arrayValueAccess = New BoundDirectCast(node.Syntax, arrayValueAccess,
                                                       Conversions.ClassifyDirectCastConversion(arrayValueAccess.Type, systemArray, useSiteInfo),
                                                       systemArray, Nothing)

                valueBeingAssigned = New BoundDirectCast(node.Syntax, valueBeingAssigned,
                                                       Conversions.ClassifyDirectCastConversion(valueBeingAssigned.Type, systemArray, useSiteInfo),
                                                       systemArray, Nothing)

                '  bind call to CopyArray
                valueBeingAssigned = New BoundCall(node.Syntax,
                                                   copyArrayUtilityMethod, Nothing, Nothing,
                                                   ImmutableArray.Create(Of BoundExpression)(arrayValueAccess, valueBeingAssigned),
                                                   Nothing, systemArray)
            End If

            '  add conversion if needed
            valueBeingAssigned = New BoundDirectCast(node.Syntax, valueBeingAssigned,
                                                     Conversions.ClassifyDirectCastConversion(valueBeingAssigned.Type, assignmentTarget.Type, useSiteInfo),
                                                     assignmentTarget.Type, Nothing)
            _diagnostics.Add(node, useSiteInfo)

            '  adjust assignment target
            If assignmentTarget.Kind = BoundKind.PropertyAccess Then
                assignmentTarget = DirectCast(assignmentTarget, BoundPropertyAccess).SetAccessKind(PropertyAccessKind.Set)
            ElseIf assignmentTarget.IsLateBound() Then
                assignmentTarget = assignmentTarget.SetLateBoundAccessKind(LateBoundAccessKind.Set)
            End If

            '  create assignment operator
            Dim assignmentOperator As BoundExpression = New BoundAssignmentOperator(node.Syntax, assignmentTarget,
                                                                 valueBeingAssigned, True)

            '  if there are any temporaries, wrap it in 
            If temporaries IsNot Nothing Then
                If temporaries.Count > 0 Then
                    assignmentOperator = New BoundSequence(node.Syntax,
                                                           StaticCast(Of LocalSymbol).From(temporaries.ToImmutableAndFree()),
                                                           ImmutableArray.Create(Of BoundExpression)(assignmentOperator),
                                                           Nothing,
                                                           If(assignmentOperator.Type.IsVoidType(),
                                                              assignmentOperator.Type,
                                                              Compilation.GetSpecialType(SpecialType.System_Void)))
                Else
                    temporaries.Free()
                End If
            End If

            '  create assignment statement
            Return Visit(New BoundExpressionStatement(node.Syntax, assignmentOperator))
        End Function
    End Class
End Namespace
