' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    ''' <summary>
    ''' A field that's part of declaration with multiple identifiers and As New clause:
    '''   Dim [|a|], [|b|] As New C(expr)
    ''' </summary>
    Friend NotInheritable Class FieldWithMultipleAsNewClauseDeclarationBody
        Inherits FieldOrPropertyDeclarationBody

        Private ReadOnly _modifedIdentifier As ModifiedIdentifierSyntax

        Public Sub New(modifiedIdentifier As ModifiedIdentifierSyntax)
            _modifedIdentifier = modifiedIdentifier
        End Sub

        Public Overrides ReadOnly Property InitializerActiveStatement As SyntaxNode
            Get
                Return _modifedIdentifier
            End Get
        End Property

        Public Overrides ReadOnly Property OtherActiveStatementContainer As SyntaxNode
            Get
                Return DirectCast(DirectCast(_modifedIdentifier.Parent, VariableDeclaratorSyntax).AsClause, AsNewClauseSyntax).NewExpression
            End Get
        End Property

        Public Overrides ReadOnly Property Envelope As ActiveStatementEnvelope
            Get
                Return New ActiveStatementEnvelope(
                        Span:=TextSpan.FromBounds(_modifedIdentifier.Span.Start, OtherActiveStatementContainer.Span.End),
                        Hole:=TextSpan.FromBounds(_modifedIdentifier.Span.End, OtherActiveStatementContainer.Span.Start))
            End Get
        End Property

        Public Overrides ReadOnly Property EncompassingAncestor As SyntaxNode
            Get
                Return _modifedIdentifier.Parent
            End Get
        End Property

        Public Overrides Function GetActiveTokens() As IEnumerable(Of SyntaxToken)
            Return InitializerActiveStatement.DescendantTokens().Concat(OtherActiveStatementContainer.DescendantTokens())
        End Function
    End Class
End Namespace
