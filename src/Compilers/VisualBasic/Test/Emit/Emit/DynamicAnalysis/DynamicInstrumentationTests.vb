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
        Private Shared _mvid As System.Guid

        Public Shared Function CreatePayload(mvid As System.Guid, methodIndex As Integer, ByRef payload As Boolean(), payloadLength As Integer) As Boolean()
            If _mvid <> mvid Then
                _payloads = New Boolean(100)() {}
                _mvid = mvid
            End If

            If System.Threading.Interlocked.CompareExchange(payload, new Boolean(payloadLength - 1) {}, Nothing) Is Nothing Then
                _payloads(methodIndex) = payload
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
                    System.Console.WriteLine(i)
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
1
True
True
True
2
True
5
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
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)

            verifier.VerifyIL(
                "Program.TestMain",
            <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  4
  .locals init (Boolean() V_0)
  IL_0000:  ldsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
  IL_0005:  ldtoken    "Sub Program.TestMain()"
  IL_000a:  ldelem.ref
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_002f
  IL_000f:  ldsfld     "System.Guid <PrivateImplementationDetails>.MVID"
  IL_0014:  ldtoken    "Sub Program.TestMain()"
  IL_0019:  ldsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
  IL_001e:  ldtoken    "Sub Program.TestMain()"
  IL_0023:  ldelema    "Boolean()"
  IL_0028:  ldc.i4.1
  IL_0029:  call       "Function Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, Integer, ByRef Boolean(), Integer) As Boolean()"
  IL_002e:  stloc.0
  IL_002f:  ldloc.0
  IL_0030:  ldc.i4.0
  IL_0031:  ldc.i4.1
  IL_0032:  stelem.i1
  IL_0033:  ret
}
                ]]>.Value)

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
8
True
True
True
9
True
12
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
]]>

            ' Explicitly define the "_MyType" pre-processor definition so that the "My" template code is added to
            ' the compilation. The "My" template code returns a special "VisualBasicSyntaxNode" that reports an invalid
            ' path. The "DynamicAnalysisInjector" skips instrumenting such code.
            Dim preprocessorSymbols = ImmutableArray.Create(New KeyValuePair(Of String, Object)("_MyType", "Console"))
            Dim parseOptions = VisualBasicParseOptions.Default.WithPreprocessorSymbols(preprocessorSymbols)

            CompileAndVerify(source, expectedOutput, TestOptions.ReleaseExe.WithParseOptions(parseOptions))
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
1
True
True
2
True
True
True
True
3
True
True
True
4
True
True
True
True
True
7
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
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)

            verifier.VerifyIL(
                "MyBox(Of T).GetValue",
            <![CDATA[
{
  // Code size       95 (0x5f)
  .maxstack  4
  .locals init (T V_0, //GetValue
                Boolean() V_1)
  IL_0000:  ldsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
  IL_0005:  ldtoken    "Function MyBox(Of T).GetValue() As T"
  IL_000a:  ldelem.ref
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  brtrue.s   IL_002f
  IL_000f:  ldsfld     "System.Guid <PrivateImplementationDetails>.MVID"
  IL_0014:  ldtoken    "Function MyBox(Of T).GetValue() As T"
  IL_0019:  ldsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
  IL_001e:  ldtoken    "Function MyBox(Of T).GetValue() As T"
  IL_0023:  ldelema    "Boolean()"
  IL_0028:  ldc.i4.4
  IL_0029:  call       "Function Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, Integer, ByRef Boolean(), Integer) As Boolean()"
  IL_002e:  stloc.1
  IL_002f:  ldloc.1
  IL_0030:  ldc.i4.0
  IL_0031:  ldc.i4.1
  IL_0032:  stelem.i1
  IL_0033:  ldloc.1
  IL_0034:  ldc.i4.2
  IL_0035:  ldc.i4.1
  IL_0036:  stelem.i1
  IL_0037:  ldarg.0
  IL_0038:  ldfld      "MyBox(Of T)._value As T"
  IL_003d:  box        "T"
  IL_0042:  brtrue.s   IL_0052
  IL_0044:  ldloc.1
  IL_0045:  ldc.i4.1
  IL_0046:  ldc.i4.1
  IL_0047:  stelem.i1
  IL_0048:  ldloca.s   V_0
  IL_004a:  initobj    "T"
  IL_0050:  br.s       IL_005d
  IL_0052:  ldloc.1
  IL_0053:  ldc.i4.3
  IL_0054:  ldc.i4.1
  IL_0055:  stelem.i1
  IL_0056:  ldarg.0
  IL_0057:  ldfld      "MyBox(Of T)._value As T"
  IL_005c:  stloc.0
  IL_005d:  ldloc.0
  IL_005e:  ret
}
                ]]>.Value)
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

        y = 75
        If tester(20) > 50 Then
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
1
True
True
True
2
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
5
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

]]>

            CompileAndVerify(source, expectedOutput)
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
    
    Sub TestMain()
        Console.WriteLine(Outer("Goo").Result)
    End Sub

    Async Function Outer(s As String) As Task(Of String)
        Dim s1 As String = Await First(s)
        Dim s2 As String = Await Second(s)

        Return s1 + s2
    End Function

    Async Function First(s As String) As Task(Of String)
        Dim result As String = Await Second(s) + "Glue"
        If result.Length > 2 Then
            Return result
        Else
            Return "Too Short"
        End If
    End Function

    Async Function Second(s As String) As Task(Of String)
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
1
True
True
True
2
True
True
3
True
True
True
True
4
True
True
True
False
True
5
True
True
True
False
True
True
8
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
]]>

            CompileAndVerify(source, expectedOutput)
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
1
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
2
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
3
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
4
True
True
True
True
True
True
7
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
]]>

            CompileAndVerify(source, expectedOutput)
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
1
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
2
True
True
True
5
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
]]>

            CompileAndVerify(source, expectedOutput)
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
1
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
2
True
True
True
5
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
]]>

            CompileAndVerify(source, expectedOutput)
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
1
True
True
True
True
True
False
True
True
2
True
True
True
5
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
]]>

            CompileAndVerify(source, expectedOutput)
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
1
True
True
True
2
True
True
True
5
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
9
True
True
14
True
15
True
True
True
True
True
True
16
True
True
]]>

            CompileAndVerify(source, expectedOutput)
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
1
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
2
True
True
True
5
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
]]>

            CompileAndVerify(source, expectedOutput)
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
1
True
True
True
True
2
True
True
True
3
True
8
True
True
True
False
11
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
]]>

            CompileAndVerify(source, expectedOutput)
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

            Dim diagnostics As ImmutableArray(Of Diagnostic) = CreateCompilation(source).GetEmitDiagnostics(EmitOptions.Default.WithInstrument("Test.Flag"))
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
            Return MyBase.CompileAndVerify(source, expectedOutput:=expectedOutput, additionalRefs:=s_refs, options:=If(options IsNot Nothing, options, TestOptions.ReleaseExe).WithDeterministic(True), emitOptions:=EmitOptions.Default.WithInstrument("Test.Flag"))
        End Function

        Private Shared ReadOnly s_refs As MetadataReference() = New MetadataReference() {MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}
    End Class
End Namespace
