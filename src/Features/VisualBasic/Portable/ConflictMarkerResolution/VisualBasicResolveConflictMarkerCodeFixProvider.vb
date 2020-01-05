' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.ConflictMarkerResolution

Namespace Microsoft.CodeAnalysis.VisualBasic.ConflictMarkerResolution
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicResolveConflictMarkerCodeFixProvider
        Inherits AbstractResolveConflictMarkerCodeFixProvider

        Private Const BC37284 As String = NameOf(BC37284)

        <ImportingConstructor>
        Public Sub New()
            MyBase.New(BC37284)
        End Sub

        Protected Overrides Function IsConflictMarker(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind() = SyntaxKind.ConflictMarkerTrivia
        End Function

        Protected Overrides Function IsDisabledText(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind() = SyntaxKind.DisabledTextTrivia
        End Function

        Protected Overrides Function IsEndOfLine(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind() = SyntaxKind.EndOfLineTrivia
        End Function
    End Class
End Namespace
