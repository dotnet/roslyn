﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Collections.Generic
Imports System.Console
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography

''' <summary>
''' Contains the startup code, command line argument processing, and driving the execution of the tool.
''' </summary>
Friend Module Program

    Public Function Main(args As String()) As Integer

        Const exitWithErrors = 1,
              exitWithoutErrors = 0

        Try
            Dim outputKind As String = Nothing
            Dim paths As New List(Of String)()

            For Each arg In args
                Dim c = arg.ToLowerInvariant()
                If c = "/test" OrElse c = "/source" OrElse c = "/gettext" Then
                    If outputKind IsNot Nothing Then
                        PrintUsage()
                        Return exitWithErrors
                    End If
                    outputKind = c
                ElseIf c = "/?" Then
                    PrintUsage()
                    Return exitWithErrors
                Else
                    paths.Add(arg)
                End If
            Next

            If paths.Count <> 2 Then
                PrintUsage()
                Return exitWithErrors
            End If

            Dim inputFile = paths(0)
            Dim outputFile = paths(1)

            If Not File.Exists(inputFile) Then
                Console.Error.WriteLine("Input file not found - ""{0}""", inputFile)

                Return exitWithErrors
            End If

            Dim definition As ParseTree = Nothing
            If Not TryReadDefinition(inputFile, definition) Then
                Return exitWithErrors
            End If

            Dim checksum = GetChecksum(inputFile)
            WriteOutput(inputFile, outputFile, definition, outputKind, checksum)

            Return exitWithoutErrors

        Catch ex As Exception
            Console.Error.WriteLine("FATAL ERROR: {0}", ex.Message)
            Console.Error.WriteLine(ex.StackTrace)

            Return exitWithErrors
        End Try

    End Function

    Private Function GetChecksum(filePath As String) As String
        Dim fileBytes = File.ReadAllBytes(filePath)
        Dim func = SHA256.Create()
        Dim hashBytes = func.ComputeHash(fileBytes)
        Dim data = BitConverter.ToString(hashBytes)
        Return data.Replace("-", "")
    End Function

    Private Sub PrintUsage()
        WriteLine("VBSyntaxGenerator.exe input output [/source] [/test]")
        WriteLine("  /source        Generates syntax model source code.")
        WriteLine("  /test          Generates syntax model unit tests.")
        WriteLine("  /gettext       Generates GetText method only.")
    End Sub

    Public Function TryReadDefinition(inputFile As String, <Out> ByRef definition As ParseTree) As Boolean
        If Not TryReadTheTree(inputFile, definition) Then
            Return False
        End If

        ValidateTree(definition)

        Return True
    End Function

    Public Sub WriteOutput(inputFile As String, outputFile As String, definition As ParseTree, outputKind As String, checksum As String)
        Select Case outputKind
            Case "/test"
                Using output As New StreamWriter(New FileStream(outputFile, FileMode.Create, FileAccess.Write))

                    WriteHeader(output, checksum)
                    output.WriteLine()
                    output.WriteLine("Imports System.Collections.Generic")
                    output.WriteLine("Imports System.Collections.Immutable")
                    output.WriteLine("Imports System.Runtime.CompilerServices")
                    output.WriteLine("Imports Microsoft.CodeAnalysis.VisualBasic.Syntax")
                    output.WriteLine("Imports Roslyn.Utilities")
                    output.WriteLine("Imports Xunit")

                    Dim testWriter As New TestWriter(definition, checksum)
                    testWriter.WriteTestCode(output)
                End Using

            Case "/gettext"
                Using output As New StreamWriter(New FileStream(outputFile, FileMode.Create, FileAccess.Write))
                    WriteHeader(output, checksum)
                    Dim syntaxFactsWriter As New SyntaxFactsWriter(definition)
                    syntaxFactsWriter.GenerateGetText(output)
                End Using

            Case Else
                WriteSyntax(inputFile, outputFile, definition, checksum)
        End Select
    End Sub

    Public Sub WriteSyntax(inputFile As String, outputFile As String, definition As ParseTree, checksum As String)
        Dim outputPath = outputFile.Trim(""""c)
        Dim prefix = Path.GetFileName(inputFile)
        Dim mainFile = Path.Combine(outputPath, $"{prefix}.Main.Generated.vb")
        Dim syntaxFile = Path.Combine(outputPath, $"{prefix}.Syntax.Generated.vb")
        Dim internalFile = Path.Combine(outputPath, $"{prefix}.Internal.Generated.vb")
        Dim redNodeWriter As New RedNodeWriter(definition)

        Using output As New StreamWriter(New FileStream(mainFile, FileMode.Create, FileAccess.Write))
            WriteSyntaxHeader(output, checksum)
            redNodeWriter.WriteMainTreeAsCode(output)

            Dim redFactoryWriter As New RedNodeFactoryWriter(definition)
            redFactoryWriter.WriteFactories(output)

            Dim syntaxFactsWriter As New SyntaxFactsWriter(definition)
            syntaxFactsWriter.GenerateFile(output)
        End Using

        Using output As New StreamWriter(New FileStream(syntaxFile, FileMode.Create, FileAccess.Write))
            WriteSyntaxHeader(output, checksum)

            redNodeWriter.WriteSyntaxTreeAsCode(output)
        End Using

        Using output As New StreamWriter(New FileStream(internalFile, FileMode.Create, FileAccess.Write))
            WriteSyntaxHeader(output, checksum)

            Dim greenNodeWriter As New GreenNodeWriter(definition)
            greenNodeWriter.WriteTreeAsCode(output)

            Dim greenFactoryWriter As New GreenNodeFactoryWriter(definition)
            greenFactoryWriter.WriteFactories(output)
        End Using

    End Sub

    Private Sub WriteHeader(output As StreamWriter, checksum As String)
        output.WriteLine("' Definition of syntax model.")
        output.WriteLine("' Generated by a tool from SHA256 content {0}", checksum)
        output.WriteLine("' DO NOT HAND EDIT")
    End Sub


    Private Sub WriteSyntaxHeader(output As StreamWriter, checksum As String)
        WriteHeader(output, checksum)

        output.WriteLine()
        output.WriteLine("Imports System.Collections.Generic")
        output.WriteLine("Imports System.Collections.Immutable")
        output.WriteLine("Imports System.Runtime.CompilerServices")
        output.WriteLine("Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax")
        output.WriteLine("Imports Microsoft.CodeAnalysis.VisualBasic.Syntax")
        output.WriteLine("Imports Roslyn.Utilities")
    End Sub
End Module