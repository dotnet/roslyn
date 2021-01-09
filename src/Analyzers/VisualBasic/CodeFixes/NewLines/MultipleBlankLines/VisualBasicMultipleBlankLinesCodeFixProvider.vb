' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.NewLines.MultipleBlankLines

Namespace Microsoft.CodeAnalysis.VisualBasic.NewLines.MultipleBlankLines
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicMultipleBlankLinesCodeFixProvider
        Inherits AbstractMultipleBlankLinesCodeFixProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function IsEndOfLine(trivia As SyntaxTrivia) As Boolean
            Return trivia.IsKind(SyntaxKind.EndOfLineTrivia)
        End Function
    End Class
End Namespace
