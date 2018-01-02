Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RegularExpressions
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.RegularExpressions
    <ExportLanguageService(GetType(IVirtualCharService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicVirtualCharService
        Inherits AbstractVirtualCharService

        Public Shared ReadOnly Instance As IVirtualCharService = New VisualBasicVirtualCharService()

        Protected Overrides Function TryConvertToVirtualCharsWorker(token As SyntaxToken) As ImmutableArray(Of VirtualChar)
            If token.Kind() <> SyntaxKind.StringLiteralToken Then
                Return Nothing
            End If

            Const StartDelimeter = """"
            Const EndDelimeter = """"

            Dim tokenText = token.Text
            If Not tokenText.StartsWith(StartDelimeter) OrElse
                Not tokenText.EndsWith(EndDelimeter) Then

                Debug.Assert(False, "This should not be reachable as long as the compiler added no diagnostics.")
                Return Nothing
            End If

            Dim startIndexInclusive = StartDelimeter.Length
            Dim endIndexExclusive = tokenText.Length - EndDelimeter.Length

            Dim result = ArrayBuilder(Of VirtualChar).GetInstance()

            Dim offset = token.SpanStart
            Dim index = startIndexInclusive
            While index < endIndexExclusive
                If tokenText(index) = """"c AndAlso
                    tokenText(index + 1) = """"c Then

                    result.Add(New VirtualChar(""""c, New TextSpan(offset + index, 2)))
                    index = index + 2

                Else
                    result.Add(New VirtualChar(tokenText(index), New TextSpan(offset + index, 1)))
                    index = index + 1
                End If
            End While

            Return result.ToImmutableAndFree()
        End Function
    End Class
End Namespace
