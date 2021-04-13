' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Rename
    Friend Class LabelConflictVisitor
        Inherits VisualBasicSyntaxVisitor

        Private ReadOnly _tracker As ConflictingIdentifierTracker

        Public Sub New(tokenBeingRenamed As SyntaxToken)
            _tracker = New ConflictingIdentifierTracker(tokenBeingRenamed, CaseInsensitiveComparison.Comparer)
        End Sub

        Public Overrides Sub DefaultVisit(node As SyntaxNode)
            For Each child In node.ChildNodes()
                Visit(child)
            Next
        End Sub

        Public Overrides Sub VisitLabelStatement(node As LabelStatementSyntax)
            _tracker.AddIdentifier(node.LabelToken)
        End Sub

        Public Overrides Sub VisitSingleLineLambdaExpression(node As SingleLineLambdaExpressionSyntax)
            ' Don't descend into lambdas, as they have their own label scope
        End Sub

        Public Overrides Sub VisitMultiLineLambdaExpression(node As MultiLineLambdaExpressionSyntax)
            ' Don't descend into lambdas, as they have their own label scope
        End Sub

        Public ReadOnly Property ConflictingTokens As IEnumerable(Of SyntaxToken)
            Get
                Return _tracker.ConflictingTokens
            End Get
        End Property
    End Class
End Namespace
