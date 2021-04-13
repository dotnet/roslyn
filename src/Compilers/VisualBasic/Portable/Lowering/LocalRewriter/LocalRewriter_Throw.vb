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
        Public Overrides Function VisitThrowStatement(node As BoundThrowStatement) As BoundNode

            Dim expressionOpt As BoundExpression = node.ExpressionOpt

            If expressionOpt IsNot Nothing Then
                expressionOpt = VisitExpressionNode(expressionOpt)

                If expressionOpt.Type.SpecialType = SpecialType.System_Int32 Then
                    Debug.Assert(node.Syntax.Kind = SyntaxKind.ErrorStatement, "Must be an Error statement.")
                    Dim nodeFactory As New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, node.Syntax, _compilationState, _diagnostics)

                    Dim createProjectError As MethodSymbol = nodeFactory.WellKnownMember(Of MethodSymbol)(WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__CreateProjectError)

                    If createProjectError IsNot Nothing Then
                        expressionOpt = New BoundCall(node.Syntax, createProjectError, Nothing, Nothing,
                                                      ImmutableArray.Create(Of BoundExpression)(expressionOpt),
                                                      Nothing, createProjectError.ReturnType)
                    End If
                End If
            End If

            Dim rewritten As BoundStatement = node.Update(expressionOpt)

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                rewritten = RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, rewritten, canThrow:=True)
            End If

            If Instrument(node, rewritten) Then
                rewritten = _instrumenterOpt.InstrumentThrowStatement(node, rewritten)
            End If

            Return rewritten
        End Function
    End Class
End Namespace
