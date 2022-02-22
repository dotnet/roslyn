' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SpellCheck
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Classification

Namespace Microsoft.CodeAnalysis.VisualBasic.SpellCheck
    <ExportLanguageService(GetType(ISpellCheckSpanService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSpellCheckSpanService
        Inherits AbstractSpellCheckSpanService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetClassificationForIdentifier(token As SyntaxToken) As String
            Return ClassificationHelpers.GetClassificationForIdentifier(token)
        End Function

        Protected Overrides Function GetSpanForComment(trivia As SyntaxTrivia) As TextSpan
            Return TextSpan.FromBounds(trivia.SpanStart + "'".Length, trivia.Span.End)
        End Function

        Protected Overrides Function GetSpanForRawString(token As SyntaxToken) As TextSpan
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Function GetSpanForString(token As SyntaxToken) As TextSpan
            Dim start = token.SpanStart + """".Length
            Dim [end] = Math.Max(start, token.Span.End - If(token.Text.EndsWith(""""), 1, 0))
            Return TextSpan.FromBounds(start, [end])
        End Function
    End Class
End Namespace
