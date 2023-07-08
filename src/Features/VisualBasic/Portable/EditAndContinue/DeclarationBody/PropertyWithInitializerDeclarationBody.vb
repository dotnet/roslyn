' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.EditAndContinue
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

        Public Overrides ReadOnly Property Envelope As ActiveStatementEnvelope
            Get
                Return TextSpan.FromBounds(PropertyStatement.Identifier.Span.Start, PropertyStatement.Initializer.Span.End)
            End Get
        End Property

        Public Overrides Function GetActiveTokens() As IEnumerable(Of SyntaxToken)
            ' Property: Attributes Modifiers [|Identifier$ Initializer|] ImplementsClause
            Return SpecializedCollections.SingletonEnumerable(PropertyStatement.Identifier).Concat(
                    If(PropertyStatement.AsClause?.DescendantTokens(), Array.Empty(Of SyntaxToken))).Concat(PropertyStatement.Initializer.DescendantTokens())
        End Function
    End Class
End Namespace
