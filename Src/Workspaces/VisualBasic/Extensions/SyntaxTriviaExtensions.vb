' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module SyntaxTriviaExtensions
        <Extension()>
        Public Function IsKind(trivia As SyntaxTrivia, kind1 As SyntaxKind, kind2 As SyntaxKind) As Boolean
            Return trivia.VisualBasicKind = kind1 OrElse
                   trivia.VisualBasicKind = kind2
        End Function

        <Extension()>
        Public Function IsKind(trivia As SyntaxTrivia, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind) As Boolean
            Return trivia.VisualBasicKind = kind1 OrElse
                   trivia.VisualBasicKind = kind2 OrElse
                   trivia.VisualBasicKind = kind3
        End Function

        <Extension()>
        Public Function IsWhitespace(trivia As SyntaxTrivia) As Boolean
            Return trivia.VisualBasicKind = SyntaxKind.WhitespaceTrivia OrElse
                   trivia.VisualBasicKind = SyntaxKind.EndOfLineTrivia
        End Function
    End Module
End Namespace