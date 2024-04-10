' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection.PortableExecutable
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities

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

        Public Shared Function CreatePayload(mvid As System.Guid, methodToken As Integer, fileIndices As Integer(), ByRef payload As Boolean(), payloadLength As Integer) As Boolean()
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

            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents,
                            "'c.vb' 1A-10-00-3A-D1-71-8C-BD-53-CC-9D-9C-5D-53-5D-7F-8C-70-89-0F (SHA1)",
                            "'a.vb' C2-D0-74-6D-08-69-59-85-2E-64-93-75-AE-DD-55-73-C3-C1-48-3A (SHA1)")

            Assert.Equal(12, reader.Methods.Length)

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

            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents,
                            "'c.vb' 55-FA-D1-46-BA-6F-EC-77-0E-02-1A-A6-BC-62-21-F7-E3-31-4F-2C (SHA1)",
                            "'a.vb' C2-D0-74-6D-08-69-59-85-2E-64-93-75-AE-DD-55-73-C3-C1-48-3A (SHA1)")

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

            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents,
                            "'c.vb' 36-B0-C2-29-F1-DC-B1-63-93-45-31-11-58-6C-5A-46-89-A1-42-34 (SHA1)",
                            "'a.vb' C2-D0-74-6D-08-69-59-85-2E-64-93-75-AE-DD-55-73-C3-C1-48-3A (SHA1)")

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

            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents,
                            "'c.vb' 1B-06-B9-C5-D5-D3-AD-EE-8A-D3-31-8F-48-EC-20-BE-AF-7D-2C-27 (SHA1)",
                            "'a.vb' C2-D0-74-6D-08-69-59-85-2E-64-93-75-AE-DD-55-73-C3-C1-48-3A (SHA1)")

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

            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents,
                            "'c.vb' 49-14-6E-73-25-48-FF-97-B3-56-26-54-65-D2-2B-00-B2-1A-FA-F5 (SHA1)",
                            "'a.vb' C2-D0-74-6D-08-69-59-85-2E-64-93-75-AE-DD-55-73-C3-C1-48-3A (SHA1)")

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
        Public Sub TestFieldInitializerSpans()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Private x As Integer

    Public Sub Main()                           ' Method 0
        TestMain()
    End Sub

    Sub TestMain()                              ' Method 1
        Dim local As New C() : local = New C(1, 2)
    End Sub
End Module

Class C
    Shared Function Init() As Integer           ' Method 2
        Return 33
    End Function

    Sub New()                                   ' Method 3
        _z = 12
    End Sub

    Shared Sub New()                            ' Method 4
        s_z = 123
    End Sub

    Private _x As Integer = Init()
    Private _y As Integer = Init() + 12
    Private _z As Integer
    Private Shared s_x As Integer = Init()
    Private Shared s_y As Integer = Init() + 153
    Private Shared s_z As Integer

    Sub New(x As Integer)                       ' Method 5
        _z = x
    End Sub

    Sub New(a As Integer, b As Integer)         ' Method 6
        _z = a + b
    End Sub

    Property A As Integer = 1234
    Shared Property B As Integer = 5678
End Class
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents,
                            "'c.vb' 13-52-6F-4D-3B-A7-8B-F7-A3-50-EE-1C-3B-0A-57-AB-B7-E5-33-0C (SHA1)",
                            "'a.vb' C2-D0-74-6D-08-69-59-85-2E-64-93-75-AE-DD-55-73-C3-C1-48-3A (SHA1)")

            Dim sourceLines As String() = testSource.ToString().Split(vbLf(0))

            VerifySpans(reader, reader.Methods(0), sourceLines,
                        New SpanResult(3, 4, 5, 11, "Public Sub Main()"),
                        New SpanResult(4, 8, 4, 18, "TestMain()"))

            VerifySpans(reader, reader.Methods(1), sourceLines,
                        New SpanResult(7, 4, 9, 11, "Sub TestMain()"),
                        New SpanResult(8, 21, 8, 28, "New C()"),
                        New SpanResult(8, 31, 8, 50, "local = New C(1, 2)"))

            VerifySpans(reader, reader.Methods(2), sourceLines,
                        New SpanResult(13, 4, 15, 16, "Shared Function Init() As Integer"),
                        New SpanResult(14, 8, 14, 17, "Return 33"))

            VerifySpans(reader, reader.Methods(3), sourceLines,
                        New SpanResult(17, 4, 19, 11, "Sub New()"),
                        New SpanResult(25, 28, 25, 34, "Init()"),
                        New SpanResult(26, 28, 26, 39, "Init() + 12"),
                        New SpanResult(40, 28, 40, 32, "1234"),
                        New SpanResult(18, 8, 18, 15, "_z = 12"))

            VerifySpans(reader, reader.Methods(4), sourceLines,
                        New SpanResult(21, 4, 23, 11, "Shared Sub New()"),
                        New SpanResult(28, 36, 28, 42, "Init()"),
                        New SpanResult(29, 36, 29, 48, "Init() + 153"),
                        New SpanResult(41, 35, 41, 39, "5678"),
                        New SpanResult(22, 8, 22, 17, "s_z = 123"))

            VerifySpans(reader, reader.Methods(5), sourceLines,
                        New SpanResult(32, 4, 34, 11, "Sub New(x As Integer)"),
                        New SpanResult(25, 28, 25, 34, "Init()"),
                        New SpanResult(26, 28, 26, 39, "Init() + 12"),
                        New SpanResult(40, 28, 40, 32, "1234"),
                        New SpanResult(33, 8, 33, 14, "_z = x"))

            VerifySpans(reader, reader.Methods(6), sourceLines,
                        New SpanResult(36, 4, 38, 11, "Sub New(a As Integer, b As Integer)"),
                        New SpanResult(25, 28, 25, 34, "Init()"),
                        New SpanResult(26, 28, 26, 39, "Init() + 12"),
                        New SpanResult(40, 28, 40, 32, "1234"),
                        New SpanResult(37, 8, 37, 18, "_z = a + b"))
        End Sub

        <Fact>
        Public Sub TestImplicitConstructorSpans()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Private x As Integer

    Public Sub Main()                           ' Method 0
        TestMain()
    End Sub

    Sub TestMain()                              ' Method 1
        Dim local As New C()
    End Sub
End Module

Class C
    Shared Function Init() As Integer           ' Method 4
        Return 33
    End Function

    Private _x As Integer = Init()
    Private _y As Integer = Init() + 12
    Private Shared s_x As Integer = Init()
    Private Shared s_y As Integer = Init() + 153
    Private Shared s_z As Integer = 144

    Property A As Integer = 1234
    Shared Property B As Integer = 5678
End Class
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents,
                            "'c.vb' 7D-27-1F-B0-ED-E6-00-8D-6C-FF-13-69-26-40-2D-4B-AB-06-0D-2E (SHA1)",
                            "'a.vb' C2-D0-74-6D-08-69-59-85-2E-64-93-75-AE-DD-55-73-C3-C1-48-3A (SHA1)")

            Dim sourceLines As String() = testSource.ToString().Split(vbLf(0))

            VerifySpans(reader, reader.Methods(0), sourceLines,
                        New SpanResult(3, 4, 5, 11, "Public Sub Main()"),
                        New SpanResult(4, 8, 4, 18, "TestMain()"))

            VerifySpans(reader, reader.Methods(1), sourceLines,
                        New SpanResult(7, 4, 9, 11, "Sub TestMain()"),
                        New SpanResult(8, 21, 8, 28, "New C()"))

            VerifySpans(reader, reader.Methods(4), sourceLines,
                        New SpanResult(13, 4, 15, 16, "Shared Function Init() As Integer"),
                        New SpanResult(14, 8, 14, 17, "Return 33"))

            VerifySpans(reader, reader.Methods(2), sourceLines,                     ' implicit shared constructor
                        New SpanResult(19, 36, 19, 42, "Init()"),
                        New SpanResult(20, 36, 20, 48, "Init() + 153"),
                        New SpanResult(21, 36, 21, 39, "144"),
                        New SpanResult(24, 35, 24, 39, "5678"))

            VerifySpans(reader, reader.Methods(3), sourceLines,                     ' implicit instance constructor
                        New SpanResult(17, 28, 17, 34, "Init()"),
                        New SpanResult(18, 28, 18, 39, "Init() + 12"),
                        New SpanResult(23, 28, 23, 32, "1234"))
        End Sub

        <Fact>
        Public Sub TestImplicitConstructorsWithLambdasSpans()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Private x As Integer

    Public Sub Main()                                   ' Method 0
        TestMain()
    End Sub

    Sub TestMain()                                      ' Method 1
        Dim y As Integer = C.s_c._function()
        Dim dd As New D()
        Dim z As Integer = dd._c._function()
        Dim zz As Integer = D.s_c._function()
        Dim zzz As Integer = dd._c1._function()
    End Sub
End Module

Class C
    Public Sub New(f As System.Func(Of Integer))        ' Method 3
        _function = f
    End Sub

    Shared Public s_c As New C(Function () 15)
    Public _function as System.Func(Of Integer)
End Class

Class D
    Public _c As C = New C(Function() 120)
    Public Shared s_c As C = New C(Function() 144)
    Public _c1 As New C(Function()
                            Return 130
                        End Function)
    Public Shared s_c1 As New C(Function()
                                    Return 156
                                End Function)
End Class

Partial Structure E
End Structure

Partial Structure E
    Public Shared s_c As C = New C(Function() 1444)
    Public Shared s_c1 As New C(Function()
                                    Return 1567
                                End Function)
End Structure

Module F
    Public s_c As New C(Function()
                            Return 333
                        End Function)
End Module
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents,
                            "'c.vb' 71-DD-56-A9-0E-56-57-E0-2B-53-EC-DA-0E-60-47-5E-CD-D1-D9-16 (SHA1)",
                            "'a.vb' C2-D0-74-6D-08-69-59-85-2E-64-93-75-AE-DD-55-73-C3-C1-48-3A (SHA1)")

            Dim sourceLines As String() = testSource.ToString().Split(vbLf(0))

            VerifySpans(reader, reader.Methods(0), sourceLines,
                        New SpanResult(3, 4, 5, 11, "Public Sub Main()"),
                        New SpanResult(4, 8, 4, 18, "TestMain()"))

            VerifySpans(reader, reader.Methods(1), sourceLines,
                        New SpanResult(7, 4, 13, 11, "Sub TestMain()"),
                        New SpanResult(8, 27, 8, 44, "C.s_c._function()"),
                        New SpanResult(9, 18, 9, 25, "New D()"),
                        New SpanResult(10, 27, 10, 44, "dd._c._function()"),
                        New SpanResult(11, 28, 11, 45, "D.s_c._function()"),
                        New SpanResult(12, 29, 12, 47, "dd._c1._function()"))

            VerifySpans(reader, reader.Methods(2), sourceLines,                                         ' Synthesized shared constructor for C
                        New SpanResult(21, 43, 21, 45, "15"),
                        New SpanResult(21, 25, 21, 46, "New C(Function () 15)"))

            VerifySpans(reader, reader.Methods(3), sourceLines,
                        New SpanResult(17, 4, 19, 11, "Public Sub New(f As System.Func(Of Integer))"),
                        New SpanResult(18, 8, 18, 21, "_function = f"))

            VerifySpans(reader, reader.Methods(4), sourceLines,                                         ' Synthesized shared constructor for D
                        New SpanResult(27, 46, 27, 49, "144"),
                        New SpanResult(27, 29, 27, 50, "New C(Function() 144)"),
                        New SpanResult(32, 36, 32, 46, "Return 156"),
                        New SpanResult(31, 26, 33, 45, "New C(Function()"))

            VerifySpans(reader, reader.Methods(5), sourceLines,                                         ' Synthesized instance constructor for D
                        New SpanResult(26, 38, 26, 41, "120"),
                        New SpanResult(26, 21, 26, 42, "New C(Function() 120)"),
                        New SpanResult(29, 28, 29, 38, "Return 130"),
                        New SpanResult(28, 18, 30, 37, "New C(Function()"))

            VerifySpans(reader, reader.Methods(6), sourceLines,                                         ' Synthesized shared constructor for E
                        New SpanResult(40, 46, 40, 50, "1444"),
                        New SpanResult(40, 29, 40, 51, "New C(Function() 1444)"),
                        New SpanResult(42, 36, 42, 47, "Return 1567"),
                        New SpanResult(41, 26, 43, 45, "New C(Function()"))

            VerifySpans(reader, reader.Methods(7), sourceLines,                                         ' Synthesized shared constructor for F
                        New SpanResult(48, 28, 48, 38, "Return 333"),
                        New SpanResult(47, 18, 49, 37, "New C(Function()"))
        End Sub

        <Fact>
        Public Sub TestDynamicAnalysisResourceMissingWhenInstrumentationFlagIsDisabled()
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(ExampleSource)
            source.Add(InstrumentationHelperSource)

            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
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
