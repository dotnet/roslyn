' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Reflection.PortableExecutable
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.DynamicAnalysis.UnitTests

    Public Class DynamicInstrumentationTests
        Inherits BasicTestBase

        ReadOnly InstrumentationHelperSource As XElement = <file name="c.vb">
                                                               <![CDATA[
Namespace Microsoft.CodeAnalysis.Runtime

    Public Class Instrumentation
    
        Private Shared _payloads As Boolean()()
        Private Shared _fileIndices As Integer()
        Private Shared _mvid As System.Guid

        Public Shared Function CreatePayload(mvid As System.Guid, methodIndex As Integer, fileIndex As Integer, ByRef payload As Boolean(), payloadLength As Integer) As Boolean()
            If _mvid <> mvid Then
                _payloads = New Boolean(100)() {}
                _fileIndices = New Integer(100) {}
                _mvid = mvid
            End If

            If System.Threading.Interlocked.CompareExchange(payload, new Boolean(payloadLength - 1) {}, Nothing) Is Nothing Then
                _payloads(methodIndex) = payload
                _fileIndices(methodIndex) = fileIndex
                Return payload
            End If

            Return _payloads(methodIndex)
        End Function

        Public Shared Sub FlushPayload()
            System.Console.WriteLine("Flushing")
            If _payloads Is Nothing Then
                Return
            End If
            For i As Integer = 0 To _payloads.Length - 1
                Dim payload As Boolean() = _payloads(i)
                if payload IsNot Nothing
                    System.Console.WriteLine("Method " & i.ToString())
                    System.Console.WriteLine("File " & _fileIndices(i).ToString())
                    For j As Integer = 0 To payload.Length - 1
                        System.Console.WriteLine(payload(j))
                        payload(j) = False
                    Next
                End If
            Next
        End Sub
    End Class
End Namespace
]]>
                                                           </file>

        <Fact>
        Public Sub SimpleCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Public Sub Main(args As String())                                   ' Method 1
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub TestMain()                                                      ' Method 2
    End Sub
End Module
]]>
                                         </file>

            Dim source As XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
Method 5
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)

            verifier.VerifyIL(
                "Program.TestMain",
            <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  5
  .locals init (Boolean() V_0)
  IL_0000:  ldsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
  IL_0005:  ldtoken    "Sub Program.TestMain()"
  IL_000a:  ldelem.ref
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_0034
  IL_000f:  ldsfld     "System.Guid <PrivateImplementationDetails>.MVID"
  IL_0014:  ldtoken    "Sub Program.TestMain()"
  IL_0019:  ldtoken    Source Document 0
  IL_001e:  ldsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
  IL_0023:  ldtoken    "Sub Program.TestMain()"
  IL_0028:  ldelema    "Boolean()"
  IL_002d:  ldc.i4.1
  IL_002e:  call       "Function Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, Integer, Integer, ByRef Boolean(), Integer) As Boolean()"
  IL_0033:  stloc.0
  IL_0034:  ldloc.0
  IL_0035:  ldc.i4.0
  IL_0036:  ldc.i4.1
  IL_0037:  stelem.i1
  IL_0038:  ret
}
                ]]>.Value)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub MyTemplateNotCovered()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Public Sub Main(args As String())                                   ' Method 1
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub TestMain()                                                      ' Method 2
    End Sub
End Module
]]>
                                         </file>

            Dim source As XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 8
File 1
True
True
True
Method 9
File 1
True
Method 12
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            ' Explicitly define the "_MyType" pre-processor definition so that the "My" template code is added to
            ' the compilation. The "My" template code returns a special "VisualBasicSyntaxNode" that reports an invalid
            ' path. The "DynamicAnalysisInjector" skips instrumenting such code.
            Dim preprocessorSymbols = ImmutableArray.Create(New KeyValuePair(Of String, Object)("_MyType", "Console"))
            Dim parseOptions = VisualBasicParseOptions.Default.WithPreprocessorSymbols(preprocessorSymbols)

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput, TestOptions.ReleaseExe.WithParseOptions(parseOptions))
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub MultipleFilesCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Public Sub Main(args As String())                                   ' Method 1
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub TestMain()                                                      ' Method 2
        Called()
    End Sub
End Module
]]>
                                         </file>

            Dim testSource1 As XElement = <file name="d.vb">
                                              <![CDATA[
Module More
    Sub Called()                                                       ' Method 3
        Another()
        Another()
    End Sub
End Module
]]>
                                          </file>

            Dim testSource2 As XElement = <file name="e.vb">
                                              <![CDATA[
Module EvenMore
    Sub Another()                                                       ' Method 4
    End Sub
End Module
]]>
                                          </file>

            Dim source As XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(testSource1)
            source.Add(testSource2)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
Method 3
File 2
True
True
True
Method 4
File 3
True
Method 7
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub MethodsOfGenericTypesCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[                                             
Class MyBox(Of T As Class)
    ReadOnly _value As T

    Public Sub New(value As T)
        _value = value
    End Sub

    Public Function GetValue() As T
        If _value Is Nothing Then
            Return Nothing
        End If

        Return _value
    End Function
End Class

Module Program
    Public Sub Main(args As String())                                   ' Method 1
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub TestMain()                                                      ' Method 2
        Dim x As MyBox(Of Object) = New MyBox(Of Object)(Nothing)
        System.Console.WriteLine(If(x.GetValue() Is Nothing, "null", x.GetValue().ToString()))
        Dim s As MyBox(Of String) = New MyBox(Of String)("Hello")
        System.Console.WriteLine(If(s.GetValue() Is Nothing, "null", s.GetValue()))
    End Sub
End Module
]]>
                                         </file>

            Dim source As XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[null
Hello
Flushing
Method 1
File 1
True
True
Method 2
File 1
True
True
True
True
Method 3
File 1
True
True
True
Method 4
File 1
True
True
True
True
True
Method 7
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)

            verifier.VerifyIL(
                "MyBox(Of T).GetValue",
            <![CDATA[
{
  // Code size      100 (0x64)
  .maxstack  5
  .locals init (T V_0, //GetValue
                Boolean() V_1)
  IL_0000:  ldsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
  IL_0005:  ldtoken    "Function MyBox(Of T).GetValue() As T"
  IL_000a:  ldelem.ref
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  brtrue.s   IL_0034
  IL_000f:  ldsfld     "System.Guid <PrivateImplementationDetails>.MVID"
  IL_0014:  ldtoken    "Function MyBox(Of T).GetValue() As T"
  IL_0019:  ldtoken    Source Document 0
  IL_001e:  ldsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
  IL_0023:  ldtoken    "Function MyBox(Of T).GetValue() As T"
  IL_0028:  ldelema    "Boolean()"
  IL_002d:  ldc.i4.4
  IL_002e:  call       "Function Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, Integer, Integer, ByRef Boolean(), Integer) As Boolean()"
  IL_0033:  stloc.1
  IL_0034:  ldloc.1
  IL_0035:  ldc.i4.0
  IL_0036:  ldc.i4.1
  IL_0037:  stelem.i1
  IL_0038:  ldloc.1
  IL_0039:  ldc.i4.2
  IL_003a:  ldc.i4.1
  IL_003b:  stelem.i1
  IL_003c:  ldarg.0
  IL_003d:  ldfld      "MyBox(Of T)._value As T"
  IL_0042:  box        "T"
  IL_0047:  brtrue.s   IL_0057
  IL_0049:  ldloc.1
  IL_004a:  ldc.i4.1
  IL_004b:  ldc.i4.1
  IL_004c:  stelem.i1
  IL_004d:  ldloca.s   V_0
  IL_004f:  initobj    "T"
  IL_0055:  br.s       IL_0062
  IL_0057:  ldloc.1
  IL_0058:  ldc.i4.3
  IL_0059:  ldc.i4.1
  IL_005a:  stelem.i1
  IL_005b:  ldarg.0
  IL_005c:  ldfld      "MyBox(Of T)._value As T"
  IL_0061:  stloc.0
  IL_0062:  ldloc.0
  IL_0063:  ret
}
                ]]>.Value)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub LambdaCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Public Sub Main(args As String())                                   ' Method 1
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub TestMain()
        Dim y As Integer = 5
        Dim tester As System.Func(Of Integer, Integer) = Function(x)
                                                             While x > 10
                                                                 Return y
                                                             End While

                                                             Return x
                                                         End Function
        Dim identity As System.Func(Of Integer, Integer) = Function(x) x
        y = 75
        If tester(20) > 50 AndAlso identity(20) = 20 Then
            System.Console.WriteLine("OK")
        Else
            System.Console.WriteLine("Bad")
        End If
    End sub
End Module
]]>
                                         </file>

            Dim source As XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[OK
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
True
False
True
True
True
True
True
False
True
Method 5
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AsyncCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Program
    Public Sub Main(args As String())                                   ' Method 1
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub
    
    Sub TestMain()                                                      ' Method 2
        Console.WriteLine(Outer("Goo").Result)
    End Sub

    Async Function Outer(s As String) As Task(Of String)                ' Method 3
        Dim s1 As String = Await First(s)
        Dim s2 As String = Await Second(s)

        Return s1 + s2
    End Function

    Async Function First(s As String) As Task(Of String)                ' Method 4
        Dim result As String = Await Second(s) + "Glue"
        If result.Length > 2 Then
            Return result
        Else
            Return "Too Short"
        End If
    End Function

    Async Function Second(s As String) As Task(Of String)               ' Method 5
        Dim doubled As String = ""
        If s.Length > 2 Then
            doubled = s + s
        Else
            doubled = "HuhHuh"
        End If
        Return Await Task.Factory.StartNew(Function() doubled)
    End Function
End Module
]]>
                                         </file>

            Dim source As XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[GooGooGlueGooGoo
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
Method 3
File 1
True
True
True
True
Method 4
File 1
True
True
True
False
True
Method 5
File 1
True
True
True
False
True
True
True
Method 8
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub LoopsCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Function TestIf(a As Boolean, b As Boolean) As Integer              ' Method 1
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
    
    Function TestDoLoops() As Integer                                   ' Method 2
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

    Sub TestForLoops()                                                  ' Method 3
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

            Dim source As XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 1
File 1
True
True
True
True
True
True
False
True
True
True
False
True
False
True
True
Method 2
File 1
True
True
True
True
False
True
True
True
False
True
True
True
True
True
True
Method 3
File 1
True
True
True
True
True
True
True
True
True
True
Method 4
File 1
True
True
True
True
True
True
Method 7
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TryAndSelectCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Sub TryAndSelect()                                                      ' Method 1
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

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 1
File 1
True
True
True
True
True
True
False
False
True
True
Method 2
File 1
True
True
True
Method 5
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub BranchesCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Sub Branches()                                                          ' Method 1
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

    Public Sub Main(args As String())                                       ' Method 2
        Branches()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub
End Module
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 1
File 1
True
True
True
False
True
True
False
True
False
True
True
False
True
True
False
Method 2
File 1
True
True
True
Method 5
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub StaticLocalsCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Sub TestMain()                                                          ' Method 1
        Dim x As Integer = 1
        Static y As Integer = 2
        If x + y = 3 Then
            Dim a As Integer = 10
            Static b As Integer = 20
            If a + b = 31 Then
                Return
            End If
        End If
    End Sub

    Public Sub Main(args As String())                                       ' Method 2
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub
End Module
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 1
File 1
True
True
True
True
True
False
True
True
Method 2
File 1
True
True
True
Method 5
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub OddCornersCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Sub TestMain()                                                              ' Method 1
        Dim h As New HasEvents()
        h.Stuff()
    End Sub

    Class HasEvents
        WithEvents f As HasEvents
        Sub New()                                                               ' Method 9
            AddHandler Mumble, AddressOf Handler
        End Sub

        Event Mumble()
        Event Stumble()
        Sub Handler() Handles Me.Stumble                                        ' Method 14
        End Sub

        Sub Stuff()                                                             ' Method 15
            f = New HasEvents()
            RaiseEvent Mumble()
            RaiseEvent Stumble()
            RemoveHandler Mumble, AddressOf Handler
            Dim meme As HasEvents = Me + Me
        End Sub

        Shared Operator +(x As HasEvents, y As HasEvents) As HasEvents          ' Method 16
            Return x
        End Operator
    End Class

    Public Sub Main(args As String())                                           ' Method 2
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub
End Module
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
Method 5
File 1
True
True
False
True
True
True
True
True
True
True
True
True
Method 9
File 1
True
True
Method 14
File 1
True
Method 15
File 1
True
True
True
True
True
True
Method 16
File 1
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub DoubleDeclarationsCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Sub TestMain()                                                              ' Method 1
        Dim x, y As Integer, z As String
        Dim a As Integer = 10, b As Integer = 20, c as Integer = 30
        If a = 11 Then
            Dim aa, bb As Integer
            Dim cc As Integer, dd As Integer
            Return
        End If
        If a + b + c = 61 Then
            x = 10
            z = "Howdy"
        End If
        Dim o1 As Object, o2 As New Object(), o3 as New Object(), o4 As Object
    End Sub

    Public Sub Main(args As String())                                           ' Method 2
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub
End Module
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 1
File 1
True
True
True
True
False
True
False
False
True
True
True
Method 2
File 1
True
True
True
Method 5
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UnusedLocal, "y").WithArguments("y").WithLocation(3, 16),
                Diagnostic(ERRID.WRN_UnusedLocal, "o1").WithArguments("o1").WithLocation(14, 13),
                Diagnostic(ERRID.WRN_UnusedLocal, "aa").WithArguments("aa").WithLocation(6, 17),
                Diagnostic(ERRID.WRN_UnusedLocal, "o4").WithArguments("o4").WithLocation(14, 67),
                Diagnostic(ERRID.WRN_UnusedLocal, "bb").WithArguments("bb").WithLocation(6, 21),
                Diagnostic(ERRID.WRN_UnusedLocal, "cc").WithArguments("cc").WithLocation(7, 17),
                Diagnostic(ERRID.WRN_UnusedLocal, "dd").WithArguments("dd").WithLocation(7, 32))
        End Sub

        <Fact>
        Public Sub PropertiesCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Sub TestMain()                                                              ' Method 1
       xxx = 12
       yyy = 11
       yyy = zzz
    End Sub

    Public Sub Main(args As String())                                           ' Method 2
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Property xxx As Integer
        Set                                                                     ' Method 3
        End Set
        Get
            Return 12
        End Get
    End Property

    Property yyy

    Property zzz As Integer
        Set
        End Set
        Get                                                                     ' Method 8
            If yyy > 10 Then
                Return 40
            End If
            Return 50
        End Get
    End Property
End Module
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 1
File 1
True
True
True
True
Method 2
File 1
True
True
True
Method 3
File 1
True
Method 8
File 1
True
True
True
False
Method 11
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestFieldInitializersCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Private x As Integer

    Public Sub Main()                           ' Method 1
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub TestMain()                              ' Method 2
        Dim local As New C() : local = New C(1, 2)
    End Sub
End Module

Class C
    Shared Function Init() As Integer           ' Method 3
        Return 33
    End Function

    Sub New()                                   ' Method 4
        _z = 12
    End Sub

    Shared Sub New()                            ' Method 5
        s_z = 123
    End Sub

    Private _x As Integer = Init()
    Private _y As Integer = Init() + 12
    Private _z As Integer
    Private Shared s_x As Integer = Init()
    Private Shared s_y As Integer = Init() + 153
    Private Shared s_z As Integer

    Sub New(x As Integer)                       ' Method 6
        _z = x
    End Sub

    Sub New(a As Integer, b As Integer)         ' Method 7
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

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
Method 3
File 1
True
True
Method 4
File 1
True
True
True
True
True
Method 5
File 1
True
True
True
True
True
Method 7
File 1
True
True
True
True
True
Method 14
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestImplicitConstructorConverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Private x As Integer

    Public Sub Main()                           ' Method 1
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub TestMain()                              ' Method 2
        Dim local As New C()
        Dim x As Integer = local._x + C.s_x
    End Sub
End Module

Class C

    ' Method 3 is the implicit shared constructor.
    ' Method 4 is the implicit instance constructor.

    Shared Function Init() As Integer           ' Method 5
        Return 33
    End Function
    
    Public _x As Integer = Init()
    Public _y As Integer = Init() + 12
    Public Shared s_x As Integer = Init()
    Public Shared s_y As Integer = Init() + 153
    Public Shared s_z As Integer = 144
    
    Property A As Integer = 1234
    Shared Property B As Integer = 5678
End Class
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
Method 3
File 1
True
True
True
True
Method 4
File 1
True
True
True
Method 5
File 1
True
True
Method 12
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestImplicitConstructorsWithLambdasCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Private x As Integer

    Public Sub Main()                                                   ' Method 1
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub TestMain()                                                      ' Method 2
        Dim y As Integer = C.s_c._function()
        Dim dd As New D()
        Dim z As Integer = dd._c._function()
        Dim zz As Integer = D.s_c._function()
        Dim zzz As Integer = dd._c1._function()
        Dim zzzz As Integer = F.s_c._function()
    End Sub
End Module

Class C
    Public Sub New(f As System.Func(Of Integer))                        ' Method 4
        _function = f
    End Sub

    Shared Public s_c As New C(Function () 15)
    Public _function as System.Func(Of Integer)
End Class

Partial Class D
End Class

Partial Class D
    Public _c As C = New C(Function() 120)
    Public Shared s_c As C = New C(Function() 144)
    Public _c1 As New C(Function()
                            Return 130
                        End Function)
    Public Shared s_c1 As New C(Function()
                                    Return 156
                                End Function)
End Class

Partial Class D
End Class

Structure E
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

' Method 3 is the synthesized shared constructor for C.
' Method 5 is the synthesized shared constructor for D.
' Method 6 is the synthesized instance constructor for D.
' Method 7 (which is not called, and so does not appear in the output) is the synthesized shared constructor for E.
' Method 8 is the synthesized shared constructor for F.
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[
Flushing
Method 1
File 1
True
True
True
Method 2
File 1
True
True
True
True
True
True
True
Method 3
File 1
True
True
Method 4
File 1
True
True
Method 5
File 1
True
True
False
True
Method 6
File 1
True
True
True
True
Method 8
File 1
True
True
Method 11
File 1
True
True
False
True
True
True
True
True
True
True
True
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub MissingMethodNeededForAnaysis()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Namespace System
    Public Class [Object] : End Class
    Public Structure Int32 : End Structure
    Public Structure [Boolean] : End Structure
    Public Class [String] : End Class
    Public Class Exception : End Class
    Public Class ValueType : End Class
    Public Class [Enum] : End Class
    Public Structure Void : End Structure
    Public Class Guid : End Class
End Namespace

Namespace System
    Public Class Console
        Public Shared Sub WriteLine(s As String)
        End Sub
        Public Shared Sub WriteLine(i As Integer)
        End Sub
        Public Shared Sub WriteLine(b As Boolean)
        End Sub
    End Class
End Namespace

Class Program
    Public Shared Sub Main(args As String())
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub
    
    Shared Sub TestMain()
    End Sub
End Class
]]>
                                         </file>
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim diagnostics As ImmutableArray(Of Diagnostic) = CreateCompilation(source).GetEmitDiagnostics(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))
            For Each Diagnostic As Diagnostic In diagnostics
                If Diagnostic.Code = ERRID.ERR_MissingRuntimeHelper AndAlso Diagnostic.Arguments(0).Equals("System.Guid.Parse") Then
                    Return
                End If
            Next

            Assert.True(False)
        End Sub

        Private Function CreateCompilation(source As XElement) As Compilation
            Return CompilationUtils.CreateCompilationWithReferences(source, references:=New MetadataReference() {}, options:=TestOptions.ReleaseExe.WithDeterministic(True))
        End Function

        Private Overloads Function CompileAndVerify(source As XElement, expectedOutput As XCData, Optional options As VisualBasicCompilationOptions = Nothing) As CompilationVerifier
            Return MyBase.CompileAndVerify(source, expectedOutput:=expectedOutput, additionalRefs:=s_refs, options:=If(options IsNot Nothing, options, TestOptions.ReleaseExe).WithDeterministic(True), emitOptions:=EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))
        End Function

        Private Shared ReadOnly s_refs As MetadataReference() = New MetadataReference() {MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}
    End Class
End Namespace
