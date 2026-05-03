' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    ''' <summary>
    ''' Property [|P As New C()|]
    ''' </summary>
    Friend NotInheritable Class PropertyWithNewClauseDeclarationBody
        Inherits FieldOrPropertyDeclarationBody

        Public ReadOnly Property PropertyStatement As PropertyStatementSyntax

        Public Sub New(propertyStatement As PropertyStatementSyntax)
            Me.PropertyStatement = propertyStatement
        End Sub

        Private ReadOnly Property NewExpression As SyntaxNode
            Get
                Return DirectCast(PropertyStatement.AsClause, AsNewClauseSyntax).NewExpression
            End Get
        End Property

        Public Overrides ReadOnly Property InitializerActiveStatement As SyntaxNode
            Get
                Return PropertyStatement
            End Get
        End Property

        Public Overrides ReadOnly Property OtherActiveStatementContainer As SyntaxNode
            Get
                Return NewExpression
            End Get
        End Property

        Public Overrides ReadOnly Property Envelope As TextSpan
            Get
                Return TextSpan.FromBounds(PropertyStatement.Identifier.Span.Start, PropertyStatement.AsClause.Span.End)
            End Get
        End Property

        Public Overrides Function GetActiveTokens(getDescendantTokens As Func(Of SyntaxNode, IEnumerable(Of SyntaxToken))) As IEnumerable(Of SyntaxToken)
            ' Property: Attributes Modifiers [|Identifier AsClause Initializer|] ImplementsClause
            Return SpecializedCollections.SingletonEnumerable(PropertyStatement.Identifier).Concat(getDescendantTokens(PropertyStatement.AsClause))
        End Function

        Public Overrides Function GetUserCodeTokens(getDescendantTokens As Func(Of SyntaxNode, IEnumerable(Of SyntaxToken))) As IEnumerable(Of SyntaxToken)
            Return getDescendantTokens(NewExpression)
        End Function
    End Class
End Namespace
