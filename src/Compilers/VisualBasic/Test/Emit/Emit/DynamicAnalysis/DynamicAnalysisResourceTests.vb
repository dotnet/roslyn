' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Reflection.PortableExecutable
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.DynamicAnalysis.UnitTests

    Public Class DynamicAnalysisResourceTests
        Inherits BasicTestBase

        ReadOnly InstrumentationHelperSource As XElement = <file name="a.vb">
                                                               <![CDATA[
Namespace Microsoft.CodeAnalysis.Runtime
    Public Class Instrumentation
        Public Shared Function CreatePayload(mvid As System.Guid, methodToken As Integer, fileIndex As Integer, ByRef payload As Boolean(), payloadLength As Integer) As Boolean()
            Return payload
        End Function

        Public Shared Sub FlushPayload()
        End Sub
    End Class
End Namespace
]]>
                                                           </file>

        ReadOnly ExampleSource As XElement = <file name="c.vb">
                                                 <![CDATA[
Imports System

Public Class C
    Public Shared Sub Main()
        Console.WriteLine(123)
        Console.WriteLine(123)
    End Sub

    Public Shared Function Fred As Integer
        Return 3
    End Function

    Public Shared Function Barney(x As Integer)
        Return x
    End Function

    Public Shared Property Wilma As Integer
        Get
            Return 12
        End Get
        Set
        End Set
    End Property

    Public Shared ReadOnly Property Betty As Integer
End Class
]]>
                                             </file>

        <Fact>
        Public Sub TestSpansPresentInResource()
            Dim source As XElement = <compilation></compilation>
            source.Add(ExampleSource)
            source.Add(InstrumentationHelperSource)

            Dim c = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrument("Test.Flag"))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents, "'c.vb'", "'a.vb'")

            Assert.Equal(11, reader.Methods.Length)

            VerifySpans(reader, reader.Methods(1),                                      ' Main
                "(3,4)-(6,11)",
                "(4,8)-(4,30)",
                "(5,8)-(5,30)")

            VerifySpans(reader, reader.Methods(2),                                      ' Fred get
                "(8,4)-(10,16)",
                "(9,8)-(9,16)")

            VerifySpans(reader, reader.Methods(3),                                      ' Barney
                "(12,4)-(14,16)",
                "(13,8)-(13,16)")

            VerifySpans(reader, reader.Methods(4),                                      ' Wilma get
                "(17,8)-(19,15)",
                "(18,12)-(18,21)")

            VerifySpans(reader, reader.Methods(5),                                      ' Wilma set
                "(20,8)-(21,15)")

            VerifySpans(reader, reader.Methods(6))                                      ' Betty get -- VB does not supply a valid syntax node for the body.

            VerifySpans(reader, reader.Methods(7))
        End Sub

        <Fact>
        Public Sub TestLoopSpans()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Function TestIf(a As Boolean, b As Boolean) As Integer
        Dim x As Integer = 0
        If a Then x += 1 Else x += 10
        If a Then
            x += 1
        ElseIf a AndAlso b Then
            x += 10
        Else
            x += 100
        End If
        If b Then
            x += 1
        End If
        If a AndAlso b Then
            x += 10
        End If
        Return x
    End Function

    Function TestDoLoops() As Integer
        Dim x As Integer = 100
        While x < 150
            x += 1
        End While
        While x < 150
            x += 1
        End While
        Do While x < 200
            x += 1
        Loop
        Do Until x = 200
            x += 1
        Loop
        Do
            x += 1
        Loop While x < 200
        Do
            x += 1
        Loop Until x = 202
        Do
            Return x
        Loop
    End Function

    Sub TestForLoops()
        Dim x As Integer = 0
        Dim y As Integer = 10
        Dim z As Integer = 3
        For a As Integer = x To y Step z
            z += 1
        Next
        For b As Integer = 1 To 10
            z += 1
        Next
        For Each c As Integer In {x, y, z}
            z += 1
        Next
    End Sub

    Public Sub Main(args As String())
        TestIf(False, False)
        TestIf(True, False)
        TestDoLoops()
        TestForLoops()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub
End Module
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim c = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrument("Test.Flag"))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents, "'c.vb'", "'a.vb'")

            VerifySpans(reader, reader.Methods(0),                                      ' TestIf
                "(1,4)-(18,16)",
                "(2,27)-(2,28)",
                "(3,18)-(3,24)",
                "(3,30)-(3,37)",
                "(3,11)-(3,12)",
                "(5,12)-(5,18)",
                "(7,12)-(7,19)",
                "(9,12)-(9,20)",
                "(6,15)-(6,26)",
                "(4,11)-(4,12)",
                "(12,12)-(12,18)",
                "(11,11)-(11,12)",
                "(15,12)-(15,19)",
                "(14,11)-(14,22)",
                "(17,8)-(17,16)")

            VerifySpans(reader, reader.Methods(1),                                      ' TestDoLoops
                "(20,4)-(43,16)",
                "(21,27)-(21,30)",
                "(23,12)-(23,18)",
                "(22,14)-(22,21)",
                "(26,12)-(26,18)",
                "(25,14)-(25,21)",
                "(29,12)-(29,18)",
                "(28,17)-(28,24)",
                "(32,12)-(32,18)",
                "(31,17)-(31,24)",
                "(35,12)-(35,18)",
                "(36,19)-(36,26)",
                "(38,12)-(38,18)",
                "(39,19)-(39,26)",
                "(41,12)-(41,20)")

            VerifySpans(reader, reader.Methods(2),                                      ' TestForLoops
                "(45,4)-(58,11)",
                "(46,27)-(46,28)",
                "(47,27)-(47,29)",
                "(48,27)-(48,28)",
                "(49,27)-(49,28)",
                "(50,12)-(50,18)",
                "(52,27)-(52,28)",
                "(53,12)-(53,18)",
                "(55,33)-(55,42)",
                "(56,12)-(56,18)")

        End Sub

        <Fact>
        Public Sub TestTryAndSelect()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Sub TryAndSelect()
        Dim y As Integer = 0
        Try
            Try
                For x As Integer = 0 To 10
                    Select Case x
                        Case 0
                            y += 1
                        Case 1
                            Throw New System.Exception()
                        Case >= 2
                            y += 1
                        Case Else
                            y += 1
                    End Select
                Next
            Catch e As System.Exception
                y += 1
            End Try
        Finally
            y += 1
        End Try
    End Sub

    Public Sub Main(args As String())
        TryAndSelect()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub
End Module
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim c = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrument("Test.Flag"))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents, "'c.vb'", "'a.vb'")

            VerifySpans(reader, reader.Methods(0),                                      ' TryAndSelect
                "(1,4)-(23,11)",
                "(2,27)-(2,28)",
                "(5,35)-(5,36)",
                "(6,32)-(6,33)",
                "(8,28)-(8,34)",
                "(10,28)-(10,56)",
                "(12,28)-(12,34)",
                "(14,28)-(14,34)",
                "(18,16)-(18,22)",
                "(21,12)-(21,18)")
        End Sub

        <Fact>
        Public Sub TestBranches()
            Dim testSource As XElement = <file name="c.vb">
                <![CDATA[
Module Program
    Sub Branches()
        Dim y As Integer = 0
MyLabel:
        Do
            Exit Do
            y += 1
        Loop
        For x As Integer = 1 To 10
            Exit For
            y += 1
        Next
        Try
            Exit Try
            y += 1
        Catch ex As System.Exception
        End Try
        Select Case y
            Case 0
                Exit Select
                y += 0
        End Select
        If y = 0 Then
            Exit Sub
        End If
        GoTo MyLabel
    End Sub

    Public Sub Main(args As String())
        Branches()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub
End Module
]]>
            </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim c = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrument("Test.Flag"))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents, "'c.vb'", "'a.vb'")

            VerifySpans(reader, reader.Methods(0),                                      ' Branches
                "(1,4)-(26,11)",
                "(2,27)-(2,28)",
                "(5,12)-(5,19)",
                "(6,12)-(6,18)",
                "(8,27)-(8,28)",
                "(9,12)-(9,20)",
                "(10,12)-(10,18)",
                "(13,12)-(13,20)",
                "(14,12)-(14,18)",
                "(17,20)-(17,21)",
                "(19,16)-(19,27)",
                "(20,16)-(20,22)",
                "(23,12)-(23,20)",
                "(22,11)-(22,16)",
                "(25,8)-(25,20)")
        End Sub

        <Fact>
        Public Sub TestDynamicAnalysisResourceMissingWhenInstrumentationFlagIsDisabled()
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(ExampleSource)
            source.Add(InstrumentationHelperSource)

            Dim c = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default)

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            Assert.Null(reader)
        End Sub

        Private Shared Sub VerifySpans(reader As DynamicAnalysisDataReader, methodData As DynamicAnalysisMethod, ParamArray expected As String())
            AssertEx.Equal(expected, reader.GetSpans(methodData.Blob).Select(Function(s) $"({s.StartLine},{s.StartColumn})-({s.EndLine},{s.EndColumn})"))
        End Sub

        Private Sub VerifyDocuments(reader As DynamicAnalysisDataReader, documents As ImmutableArray(Of DynamicAnalysisDocument), ParamArray expected As String())
            Dim sha1 = New Guid("ff1816ec-aa5e-4d10-87F7-6F4963833460")

            Dim actual = From d In documents
                         Let name = reader.GetDocumentName(d.Name)
                         Let hash = If(d.Hash.IsNil, "", " " + BitConverter.ToString(reader.GetBytes(d.Hash)))
                         Let hashAlgGuid = reader.GetGuid(d.HashAlgorithm)
                         Let hashAlg = If(hashAlgGuid = sha1, " (SHA1)", If(hashAlgGuid = New Guid, "", " " + hashAlgGuid.ToString()))
                         Select $"'{name}'{hash}{hashAlg}"

            AssertEx.Equal(expected, actual)
        End Sub
    End Class
End Namespace
