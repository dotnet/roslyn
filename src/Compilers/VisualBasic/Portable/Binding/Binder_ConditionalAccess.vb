' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class Binder

        Private Function BindConditionalAccessExpression(node As ConditionalAccessExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim placeholder As BoundRValuePlaceholder = Nothing
            Dim boundExpression As BoundExpression = BindConditionalAccessReceiver(node, diagnostics, placeholder)

            Dim accessBinder As New ConditionalAccessBinder(Me, node, placeholder)

            Dim whenNotNull As BoundExpression = accessBinder.BindExpression(node.WhenNotNull, diagnostics)

            ' Do not know result type yet, it will be determined based on usage.
            Return New BoundConditionalAccess(node, boundExpression, placeholder, whenNotNull, Nothing)
        End Function

        Private Function BindConditionalAccessReceiver(node As ConditionalAccessExpressionSyntax, diagnostics As BindingDiagnosticBag, <Out> ByRef placeholder As BoundRValuePlaceholder) As BoundExpression
            Dim boundExpression As BoundExpression

            If node.Expression Is Nothing Then
                boundExpression = TryBindOmittedLeftForConditionalAccess(node, Me, diagnostics)

                If boundExpression Is Nothing Then
                    ' Didn't find binder that can handle conditional access with omitted left part
                    boundExpression = ReportDiagnosticAndProduceBadExpression(diagnostics, node, ERRID.ERR_BadConditionalWithRef).MakeCompilerGenerated()
                End If
            Else
                ' Bind the expression as a value
                boundExpression = BindValue(node.Expression, diagnostics)

                ' NOTE: If the expression is not an l-value we should make an r-value of it
                If Not boundExpression.IsLValue Then
                    boundExpression = Me.MakeRValue(boundExpression, diagnostics)
                End If
            End If

            Dim type As TypeSymbol = boundExpression.Type

            Dim placeholderType As TypeSymbol = type

            If type.IsValueType Then
                If type.IsNullableType() Then
                    placeholderType = type.GetNullableUnderlyingType()
                Else
                    ' Operator '{0}' is not defined for type '{1}'.
                    ReportDiagnostic(diagnostics, node.QuestionMarkToken, ERRID.ERR_UnaryOperand2, node.QuestionMarkToken.ValueText, type)
                End If
            End If

            ' Create a placeholder
            placeholder = New BoundRValuePlaceholder(node, placeholderType)
            placeholder.SetWasCompilerGenerated()

            Return boundExpression
        End Function

        Protected Overridable Function TryBindOmittedLeftForConditionalAccess(node As ConditionalAccessExpressionSyntax,
                                                                             accessingBinder As Binder,
                                                                             diagnostics As BindingDiagnosticBag) As BoundExpression
            Debug.Assert(Me.ContainingBinder IsNot Nothing)
            Return Me.ContainingBinder.TryBindOmittedLeftForConditionalAccess(node, accessingBinder, diagnostics)
        End Function

        Protected Function GetConditionalAccessReceiver(node As ConditionalAccessExpressionSyntax) As BoundExpression
            Dim result As BoundExpression = TryGetConditionalAccessReceiver(node)

            If result Is Nothing Then
                Dim placeholder As BoundRValuePlaceholder = Nothing
                BindConditionalAccessReceiver(node, BindingDiagnosticBag.Discarded, placeholder)
                Return placeholder
            End If

            Return result
        End Function

        Protected Overridable Function TryGetConditionalAccessReceiver(node As ConditionalAccessExpressionSyntax) As BoundExpression
            Return Me.ContainingBinder.TryGetConditionalAccessReceiver(node)
        End Function
    End Class

    ''' <summary>
    ''' A helper to bind conditional access. 
    ''' </summary>
    Friend NotInheritable Class ConditionalAccessBinder
        Inherits Binder

        Private ReadOnly _conditionalAccess As ConditionalAccessExpressionSyntax
        Private ReadOnly _placeholder As BoundValuePlaceholderBase

        Public Sub New(containingBinder As Binder, conditionalAccess As ConditionalAccessExpressionSyntax, placeholder As BoundValuePlaceholderBase)
            MyBase.New(containingBinder)
            _conditionalAccess = conditionalAccess
            _placeholder = placeholder
        End Sub

        Protected Overrides Function TryGetConditionalAccessReceiver(node As ConditionalAccessExpressionSyntax) As BoundExpression
            If node Is _conditionalAccess Then
                Return _placeholder
            End If

            Return MyBase.TryGetConditionalAccessReceiver(node)
        End Function
    End Class

End Namespace
