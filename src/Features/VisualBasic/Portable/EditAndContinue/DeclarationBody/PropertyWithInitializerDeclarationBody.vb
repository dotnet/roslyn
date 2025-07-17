' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    ''' <summary>
    ''' Property [|P As Integer = initializer|]
    ''' </summary>
    Friend NotInheritable Class PropertyWithInitializerDeclarationBody
        Inherits FieldOrPropertyDeclarationBody

        Public ReadOnly Property PropertyStatement As PropertyStatementSyntax

        Public Sub New(propertyStatement As PropertyStatementSyntax)
            Me.PropertyStatement = propertyStatement
        End Sub

        Public Overrides ReadOnly Property InitializerActiveStatement As SyntaxNode
            Get
                Return PropertyStatement
            End Get
        End Property

        Public Overrides ReadOnly Property OtherActiveStatementContainer As SyntaxNode
            Get
                Return PropertyStatement.Initializer.Value
            End Get
        End Property

        Public Overrides ReadOnly Property Envelope As TextSpan
            Get
                Return TextSpan.FromBounds(PropertyStatement.Identifier.Span.Start, PropertyStatement.Initializer.Span.End)
            End Get
        End Property

        Public Overrides Function GetActiveTokens(getDescendantTokens As Func(Of SyntaxNode, IEnumerable(Of SyntaxToken))) As IEnumerable(Of SyntaxToken)
            ' Property: Attributes Modifiers [|Identifier$ Initializer|] ImplementsClause
            Return SpecializedCollections.SingletonEnumerable(PropertyStatement.Identifier).Concat(
                    If(PropertyStatement.AsClause IsNot Nothing, getDescendantTokens(PropertyStatement.AsClause), Array.Empty(Of SyntaxToken))).
                        Concat(getDescendantTokens(PropertyStatement.Initializer))
        End Function

        Public Overrides Function GetUserCodeTokens(getDescendantTokens As Func(Of SyntaxNode, IEnumerable(Of SyntaxToken))) As IEnumerable(Of SyntaxToken)
            Return getDescendantTokens(PropertyStatement.Initializer.Value)
        End Function
    End Class
End Namespace
