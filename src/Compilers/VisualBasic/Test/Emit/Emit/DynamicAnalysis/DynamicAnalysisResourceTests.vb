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

            Dim sourceLines As String() = ExampleSource.ToString().Split(vbLf(0))

            VerifySpans(reader, reader.Methods(1), sourceLines,                                 ' Main
                New SpanResult(3, 4, 6, 11, "Public Shared Sub Main()"),
                New SpanResult(4, 8, 4, 30, "Console.WriteLine(123)"),
                New SpanResult(5, 8, 5, 30, "Console.WriteLine(123)"))

            VerifySpans(reader, reader.Methods(2), sourceLines,                                 ' Fred get
                New SpanResult(8, 4, 10, 16, "Public Shared Function Fred As Integer"),
                New SpanResult(9, 8, 9, 16, "Return 3"))

            VerifySpans(reader, reader.Methods(3), sourceLines,                                 ' Barney
                New SpanResult(12, 4, 14, 16, "Public Shared Function Barney(x As Integer)"),
                New SpanResult(13, 8, 13, 16, "Return x"))

            VerifySpans(reader, reader.Methods(4), sourceLines,                                 ' Wilma get
                New SpanResult(17, 8, 19, 15, "Get"),
                New SpanResult(18, 12, 18, 21, "Return 12"))

            VerifySpans(reader, reader.Methods(5), sourceLines,                                 ' Wilma set
                New SpanResult(20, 8, 21, 15, "Set"))

            VerifySpans(reader, reader.Methods(6))                                              ' Betty get -- VB does not supply a valid syntax node for the body.

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

            Dim sourceLines As String() = testSource.ToString().Split(vbLf(0))

            VerifySpans(reader, reader.Methods(0), sourceLines,                                             ' TestIf
                New SpanResult(1, 4, 18, 16, "Function TestIf(a As Boolean, b As Boolean) As Integer"),
                New SpanResult(2, 27, 2, 28, "0"),
                New SpanResult(3, 18, 3, 24, "x += 1"),
                New SpanResult(3, 30, 3, 37, "x += 10"),
                New SpanResult(3, 11, 3, 12, "a"),
                New SpanResult(5, 12, 5, 18, "x += 1"),
                New SpanResult(7, 12, 7, 19, "x += 10"),
                New SpanResult(9, 12, 9, 20, "x += 100"),
                New SpanResult(6, 15, 6, 26, "a AndAlso b"),
                New SpanResult(4, 11, 4, 12, "a"),
                New SpanResult(12, 12, 12, 18, "x += 1"),
                New SpanResult(11, 11, 11, 12, "b"),
                New SpanResult(15, 12, 15, 19, "x += 10"),
                New SpanResult(14, 11, 14, 22, "a AndAlso b"),
                New SpanResult(17, 8, 17, 16, "Return x"))

            VerifySpans(reader, reader.Methods(1), sourceLines,                                             ' TestDoLoops
                New SpanResult(20, 4, 43, 16, "Function TestDoLoops() As Integer"),
                New SpanResult(21, 27, 21, 30, "100"),
                New SpanResult(23, 12, 23, 18, "x += 1"),
                New SpanResult(22, 14, 22, 21, "x < 150"),
                New SpanResult(26, 12, 26, 18, "x += 1"),
                New SpanResult(25, 14, 25, 21, "x < 150"),
                New SpanResult(29, 12, 29, 18, "x += 1"),
                New SpanResult(28, 17, 28, 24, "x < 200"),
                New SpanResult(32, 12, 32, 18, "x += 1"),
                New SpanResult(31, 17, 31, 24, "x = 200"),
                New SpanResult(35, 12, 35, 18, "x += 1"),
                New SpanResult(36, 19, 36, 26, "x < 200"),
                New SpanResult(38, 12, 38, 18, "x += 1"),
                New SpanResult(39, 19, 39, 26, "x = 202"),
                New SpanResult(41, 12, 41, 20, "Return x"))

            VerifySpans(reader, reader.Methods(2), sourceLines,                                             ' TestForLoops
                New SpanResult(45, 4, 58, 11, "Sub TestForLoops()"),
                New SpanResult(46, 27, 46, 28, "0"),
                New SpanResult(47, 27, 47, 29, "10"),
                New SpanResult(48, 27, 48, 28, "3"),
                New SpanResult(49, 27, 49, 28, "x"),
                New SpanResult(50, 12, 50, 18, "z += 1"),
                New SpanResult(52, 27, 52, 28, "1"),
                New SpanResult(53, 12, 53, 18, "z += 1"),
                New SpanResult(55, 33, 55, 42, "{x, y, z}"),
                New SpanResult(56, 12, 56, 18, "z += 1"))

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

            Dim sourceLines As String() = testSource.ToString().Split(vbLf(0))

            VerifySpans(reader, reader.Methods(0), sourceLines,                                     ' TryAndSelect
                New SpanResult(1, 4, 23, 11, "Sub TryAndSelect()"),
                New SpanResult(2, 27, 2, 28, "0"),
                New SpanResult(5, 35, 5, 36, "0"),
                New SpanResult(6, 32, 6, 33, "x"),
                New SpanResult(8, 28, 8, 34, "y += 1"),
                New SpanResult(10, 28, 10, 56, "Throw New System.Exception()"),
                New SpanResult(12, 28, 12, 34, "y += 1"),
                New SpanResult(14, 28, 14, 34, "y += 1"),
                New SpanResult(18, 16, 18, 22, "y += 1"),
                New SpanResult(21, 12, 21, 18, "y += 1"))
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

            Dim sourceLines As String() = testSource.ToString().Split(vbLf(0))

            VerifySpans(reader, reader.Methods(0), sourceLines,                                     ' Branches
                New SpanResult(1, 4, 26, 11, "Sub Branches()"),
                New SpanResult(2, 27, 2, 28, "0"),
                New SpanResult(5, 12, 5, 19, "Exit Do"),
                New SpanResult(6, 12, 6, 18, "y += 1"),
                New SpanResult(8, 27, 8, 28, "1"),
                New SpanResult(9, 12, 9, 20, "Exit For"),
                New SpanResult(10, 12, 10, 18, "y += 1"),
                New SpanResult(13, 12, 13, 20, "Exit Try"),
                New SpanResult(14, 12, 14, 18, "y += 1"),
                New SpanResult(17, 20, 17, 21, "y"),
                New SpanResult(19, 16, 19, 27, "Exit Select"),
                New SpanResult(20, 16, 20, 22, "y += 0"),
                New SpanResult(23, 12, 23, 20, "Exit Sub"),
                New SpanResult(22, 11, 22, 16, "y = 0"),
                New SpanResult(25, 8, 25, 20, "GoTo MyLabel"))
        End Sub

        <Fact>
        Public Sub TestMethodSpansWithAttributes()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Private x As Integer

    Public Sub Main()                       ' Method 0
        Fred()
    End Sub

    <System.Obsolete()>
    Sub Fred()                              ' Method 1
    End Sub

    Sub New()                               ' Method 2
        x = 12
    End Sub
End Module

Class c
    <System.Security.SecurityCritical>
    Public Sub New(x As Integer)                            ' Method 3
    End Sub

    <System.Security.SecurityCritical>
    Sub New()                                               ' Method 4
    End Sub

    <System.Obsolete>
    Public Sub Fred()                                       ' Method 5
        Return
    End Sub

    <System.Obsolete>
    Function Barney() As Integer                            ' Method 6
        Return 12
    End Function

    <System.Obsolete>
    Shared Sub New()                                        ' Method 7
    End Sub

    <System.Obsolete>
    Public Shared Operator +(a As c, b As c) As c           ' Method 8
        Return a
    End Operator

    Property P1 As Integer
        <System.Security.SecurityCritical>
        Get                                                 ' Method 9
            Return 10
        End Get
        <System.Security.SecurityCritical>
        Set(value As Integer)                               ' Method 10
        End Set
    End Property
End Class
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

            Dim sourceLines As String() = testSource.ToString().Split(vbLf(0))

            VerifySpans(reader, reader.Methods(0), sourceLines,
                        New SpanResult(3, 4, 5, 11, "Public Sub Main()"),
                        New SpanResult(4, 8, 4, 14, "Fred()"))

            VerifySpans(reader, reader.Methods(1), sourceLines,
                        New SpanResult(8, 4, 9, 11, "Sub Fred()"))

            VerifySpans(reader, reader.Methods(2), sourceLines,
                        New SpanResult(11, 4, 13, 11, "Sub New()"),
                        New SpanResult(12, 8, 12, 14, "x = 12"))

            VerifySpans(reader, reader.Methods(3), sourceLines,
                        New SpanResult(18, 4, 19, 11, "Public Sub New(x As Integer) "))

            VerifySpans(reader, reader.Methods(4), sourceLines,
                        New SpanResult(22, 4, 23, 11, "Sub New()"))

            VerifySpans(reader, reader.Methods(5), sourceLines,
                        New SpanResult(26, 4, 28, 11, "Public Sub Fred()"),
                        New SpanResult(27, 8, 27, 14, "Return"))

            VerifySpans(reader, reader.Methods(6), sourceLines,
                        New SpanResult(31, 4, 33, 16, "Function Barney() As Integer"),
                        New SpanResult(32, 8, 32, 17, "Return 12"))

            VerifySpans(reader, reader.Methods(7), sourceLines,
                        New SpanResult(36, 4, 37, 11, "Shared Sub New()"))

            VerifySpans(reader, reader.Methods(8), sourceLines,
                        New SpanResult(40, 4, 42, 16, "Public Shared Operator +(a As c, b As c) As c"),
                        New SpanResult(41, 8, 41, 16, "Return a"))

            VerifySpans(reader, reader.Methods(9), sourceLines,
                        New SpanResult(46, 8, 48, 15, "Get"),
                        New SpanResult(47, 12, 47, 21, "Return 10"))

            VerifySpans(reader, reader.Methods(10), sourceLines,
                        New SpanResult(50, 8, 51, 15, "Set(value As Integer)"))
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

        Private Class SpanResult
            Public ReadOnly Property StartLine As Integer
            Public ReadOnly Property StartColumn As Integer
            Public ReadOnly Property EndLine As Integer
            Public ReadOnly Property EndColumn As Integer
            Public ReadOnly Property TextStart As String
            Public Sub New(startLine As Integer, startColumn As Integer, endLine As Integer, endColumn As Integer, textStart As String)
                Me.StartLine = startLine
                Me.StartColumn = startColumn
                Me.EndLine = endLine
                Me.EndColumn = endColumn
                Me.TextStart = textStart
            End Sub
        End Class

        Private Shared Sub VerifySpans(reader As DynamicAnalysisDataReader, methodData As DynamicAnalysisMethod, sourceLines As String(), ParamArray expected As SpanResult())
            Dim expectedSpanSpellings As ArrayBuilder(Of String) = ArrayBuilder(Of String).GetInstance(expected.Length)
            For Each expectedSpanResult As SpanResult In expected
                Assert.True(sourceLines(expectedSpanResult.StartLine + 1).Substring(expectedSpanResult.StartColumn).StartsWith(expectedSpanResult.TextStart))
                expectedSpanSpellings.Add(String.Format("({0},{1})-({2},{3})", expectedSpanResult.StartLine, expectedSpanResult.StartColumn, expectedSpanResult.EndLine, expectedSpanResult.EndColumn))
            Next

            VerifySpans(reader, methodData, expectedSpanSpellings.ToArrayAndFree())
        End sub

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
