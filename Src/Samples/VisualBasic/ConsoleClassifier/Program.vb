' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text

Module Program

    Sub Main(args As String())
        TestFormatterAndClassifierAsync().Wait()
    End Sub

    Async Function TestFormatterAndClassifierAsync() As Task
        Dim workspace = New CustomWorkspace()
        Dim solution = workspace.CurrentSolution
        Dim project = solution.AddProject("projectName", "assemblyName", LanguageNames.VisualBasic)
        Dim document = project.AddDocument("name.vb",
"Module M
Sub Main()
WriteLine(""Hello, World!"")
End Sub
End Module")
        document = Await Formatter.FormatAsync(document)
        Dim text As SourceText = Await document.GetTextAsync()

        Dim classifiedSpans As IEnumerable(Of ClassifiedSpan) = Await Classifier.GetClassifiedSpansAsync(document, TextSpan.FromBounds(0, text.Length))
        Console.BackgroundColor = ConsoleColor.Black

        Dim ranges = From span As ClassifiedSpan In classifiedSpans
                     Select New Range(span, text.GetSubText(span.TextSpan).ToString())

        ' Whitespace isn't classified so fill in ranges for whitespace.
        ranges = FillGaps(text, ranges)

        For Each range As Range In ranges
            Select Case range.ClassificationType
                Case "keyword"
                    Console.ForegroundColor = ConsoleColor.DarkCyan
                Case "class name", "module name"
                    Console.ForegroundColor = ConsoleColor.Cyan
                Case "string"
                    Console.ForegroundColor = ConsoleColor.DarkYellow
                Case Else
                    Console.ForegroundColor = ConsoleColor.White
            End Select

            Console.Write(range.Text)
        Next

        Console.ResetColor()
        Console.WriteLine()
    End Function

    Iterator Function FillGaps(text As SourceText, ranges As IEnumerable(Of Range)) As IEnumerable(Of Range)
        Const whitespaceClassification As String = Nothing

        Dim current As Integer = 0
        Dim previous As Range = Nothing

        For Each range As Range In ranges
            Dim start As Integer = range.TextSpan.Start
            If start > current Then
                Yield New Range(whitespaceClassification, TextSpan.FromBounds(current, start), text)
            End If

            If previous Is Nothing OrElse range.TextSpan <> previous.TextSpan Then
                Yield range
            End If

            previous = range
            current = range.TextSpan.End
        Next

        If current < text.Length Then
            Yield New Range(whitespaceClassification, TextSpan.FromBounds(current, text.Length), text)
        End If
    End Function
End Module