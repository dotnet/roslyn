' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundNamespaceExpression
        Public Sub New(syntax As SyntaxNode, unevaluatedReceiverOpt As BoundExpression, namespaceSymbol As NamespaceSymbol, hasErrors As Boolean)
            MyClass.New(syntax, unevaluatedReceiverOpt, Nothing, namespaceSymbol, hasErrors)
        End Sub

        Public Sub New(syntax As SyntaxNode, unevaluatedReceiverOpt As BoundExpression, namespaceSymbol As NamespaceSymbol)
            MyClass.New(syntax, unevaluatedReceiverOpt, Nothing, namespaceSymbol)
        End Sub

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return If(DirectCast(Me.AliasOpt, Symbol), Me.NamespaceSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property ResultKind As LookupResultKind
            Get
                If NamespaceSymbol.NamespaceKind = NamespaceKindNamespaceGroup Then
                    Return LookupResult.WorseResultKind(LookupResultKind.Ambiguous, MyBase.ResultKind)
                End If

                Return MyBase.ResultKind
            End Get
        End Property

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(UnevaluatedReceiverOpt Is Nothing OrElse UnevaluatedReceiverOpt.Kind = BoundKind.NamespaceExpression)
            Debug.Assert(AliasOpt Is Nothing OrElse NamespaceSymbol.NamespaceKind <> NamespaceKindNamespaceGroup)
        End Sub
#End If
    End Class
End Namespace
