' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text

Friend Module Program

    Public Sub Main(args As String())
        TestFormatterAndClassifierAsync().Wait()
    End Sub

    Public Async Function TestFormatterAndClassifierAsync() As Task
        Dim workspace = New AdhocWorkspace()
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

    Public Iterator Function FillGaps(text As SourceText, ranges As IEnumerable(Of Range)) As IEnumerable(Of Range)
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
