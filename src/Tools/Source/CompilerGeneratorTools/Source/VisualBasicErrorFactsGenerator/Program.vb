' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Text

Friend Module Program
    Public Function Main(args As String()) As Integer
        If args.Length <> 2 Then
            Console.WriteLine(
"Usage: VBErrorFactsGenerator.exe input output
  input     The path to Errors.vb
  output    The path to ErrorFacts.Generated.vb")
            Return -1
        End If

        Dim inputPath = args(0)
        Dim outputPath = args(1)

        Dim outputText = New StringBuilder
        outputText.AppendLine("Namespace Microsoft.CodeAnalysis.VisualBasic")
        outputText.AppendLine("    Friend Partial Module ErrorFacts")

        Dim warningCodeNames, fatalCodeNames, infoCodeNames, hiddenCodeNames As New List(Of String)
        For Each line In From l In File.ReadAllLines(inputPath) Select l.Trim
            If line.StartsWith("WRN_", StringComparison.OrdinalIgnoreCase) Then
                warningCodeNames.Add(line.Substring(0, line.IndexOf(" "c)))
            ElseIf line.StartsWith("FTL_", StringComparison.OrdinalIgnoreCase) Then
                fatalCodeNames.Add(line.Substring(0, line.IndexOf(" "c)))
            ElseIf line.StartsWith("INF_", StringComparison.OrdinalIgnoreCase) Then
                infoCodeNames.Add(line.Substring(0, line.IndexOf(" "c)))
            ElseIf line.StartsWith("HDN_", StringComparison.OrdinalIgnoreCase) Then
                hiddenCodeNames.Add(line.Substring(0, line.IndexOf(" "c)))
            End If
        Next

        GenerateErrorFactsFunction("IsWarning", warningCodeNames, outputText)
        outputText.AppendLine()
        GenerateErrorFactsFunction("IsFatal", fatalCodeNames, outputText)
        outputText.AppendLine()
        GenerateErrorFactsFunction("IsInfo", infoCodeNames, outputText)
        outputText.AppendLine()
        GenerateErrorFactsFunction("IsHidden", hiddenCodeNames, outputText)

        outputText.AppendLine("    End Module")
        outputText.AppendLine("End Namespace")
        File.WriteAllText(outputPath, outputText.ToString())

        Return 0
    End Function

    Private Sub GenerateErrorFactsFunction(functionName As String, codeNames As List(Of String), outputText As StringBuilder)
        outputText.AppendLine(String.Format("        Public Function {0}(code as ERRID) As Boolean", functionName))
        outputText.AppendLine("            Select Case code")
        Dim index = 0
        For Each name In codeNames
            If index = 0 Then
                outputText.Append("                Case ERRID.")
            Else
                outputText.Append("                     ERRID.")
            End If
            outputText.Append(name)
            index += 1
            If index = codeNames.Count Then
                outputText.AppendLine()
                outputText.AppendLine("                    Return True")
            Else
                outputText.AppendLine(",")
            End If
        Next
        outputText.AppendLine("                Case Else")
        outputText.AppendLine("                    Return False")
        outputText.AppendLine("            End Select")
        outputText.AppendLine("        End Function")
    End Sub
End Module
