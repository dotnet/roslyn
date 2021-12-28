' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ' The binder for a statement that can be exited with Exit and/or Continue.
    Friend Class ExitableStatementBinder
        Inherits BlockBaseBinder

        Private ReadOnly _continueLabel As LabelSymbol  ' the label to jump to for a continue (or Nothing for none)
        Private ReadOnly _continueKind As SyntaxKind    ' the kind of continue to go there
        Private ReadOnly _exitLabel As LabelSymbol      ' the label to jump to for an exit (or Nothing for none)
        Private ReadOnly _exitKind As SyntaxKind        ' the kind of exit to go there

        Public Sub New(enclosing As Binder,
                       continueKind As SyntaxKind,
                       exitKind As SyntaxKind)
            MyBase.New(enclosing)

            Me._continueKind = continueKind
            If continueKind <> SyntaxKind.None Then
                Me._continueLabel = New GeneratedLabelSymbol("continue")
            End If

            Me._exitKind = exitKind
            If exitKind <> SyntaxKind.None Then
                Me._exitLabel = New GeneratedLabelSymbol("exit")
            End If
        End Sub

        Friend Overrides ReadOnly Property Locals As ImmutableArray(Of LocalSymbol)
            Get
                Return ImmutableArray(Of LocalSymbol).Empty
            End Get
        End Property

        Public Overrides Function GetContinueLabel(continueSyntaxKind As SyntaxKind) As LabelSymbol
            If _continueKind = continueSyntaxKind Then
                Return _continueLabel
            Else
                Return ContainingBinder.GetContinueLabel(continueSyntaxKind)
            End If
        End Function

        Public Overrides Function GetExitLabel(exitSyntaxKind As SyntaxKind) As LabelSymbol
            If _exitKind = exitSyntaxKind Then
                Return _exitLabel
            Else
                Return ContainingBinder.GetExitLabel(exitSyntaxKind)
            End If
        End Function

        Public Overrides Function GetReturnLabel() As LabelSymbol
            Select Case _exitKind
                Case SyntaxKind.ExitSubStatement,
                    SyntaxKind.ExitPropertyStatement,
                    SyntaxKind.ExitFunctionStatement,
                    SyntaxKind.EventStatement,
                    SyntaxKind.OperatorStatement
                    ' the last two are not real exit statements, they are used specifically in
                    ' block events and operators to indicate that there is a return label, but
                    ' no ExitXXX statement should be able to bind to it.

                    Return _exitLabel
                Case Else
                    Return ContainingBinder.GetReturnLabel()
            End Select
        End Function
    End Class
End Namespace
