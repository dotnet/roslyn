' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class CommentTriviaStructureProvider
        Inherits AbstractSyntaxTriviaStructureProvider

        Public Overrides Sub CollectBlockSpans(
                                              document As Document,
                                              trivia As SyntaxTrivia,
                                              spans As ArrayBuilder(Of BlockSpan),
                                              cancellationToken As CancellationToken)
            If trivia.Kind = SyntaxKind.CommentTrivia Then
                VisualBasicOutliningHelpers.CollectCommentsRegions(trivia.Token.LeadingTrivia, spans)
                VisualBasicOutliningHelpers.CollectCommentsRegions(trivia.Token.TrailingTrivia, spans)
            End If
        End Sub

    End Class
End Namespace
