' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Test.Utilities.VBInstrumentationChecker
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.DynamicAnalysis.UnitTests

    Public Class DynamicInstrumentationTests
        Inherits BasicTestBase

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

            Dim checker = New VBInstrumentationChecker()
            checker.Method(1, 1, "Public Sub Main").
                True("TestMain()").
                True("Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()")
            checker.Method(2, 1, "Sub TestMain()")
            checker.Method(5, 1).
                True().
                False().
                True().
                True().
                True().
                True().
                True().
                True().
                True().
                True().
                True().
                True()

            Dim verifier As CompilationVerifier = CompileAndVerify(source, checker.ExpectedOutput)
            checker.CompleteCheck(verifier.Compilation, testSource)

            verifier.VerifyIL(
                "Program.TestMain",
            <![CDATA[{
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

            verifier.VerifyIL(
                ".cctor",
            <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldtoken    Max Method Token Index
  IL_0005:  newarr     "Boolean()"
  IL_000a:  stsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
  IL_000f:  ldstr      ##MVID##
  IL_0014:  newobj     "Sub System.Guid..ctor(String)"
  IL_0019:  stsfld     "System.Guid <PrivateImplementationDetails>.MVID"
  IL_001e:  ret
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
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub IteratorCoverage()
            Dim testSource As XElement = <file name="c.vb">
                                             <![CDATA[
Module Program
    Public Sub Main(args As String())                                   ' Method 1
        TestMain()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub TestMain()                                                      ' Method 2
        For Each number In Goo()
            System.Console.WriteLine(number)
        Next                                                     
        For Each number In Goo()
            System.Console.WriteLine(number)
        Next
    End Sub

    Public Iterator Function Goo() As System.Collections.Generic.IEnumerable(Of Integer)      ' Method 3
        For counter = 1 To 5
            Yield counter
        Next
    End Function
End Module
]]>
                                         </file>

            Dim source As XElement = <compilation></compilation>
            source.Add(testSource)
            source.Add(InstrumentationHelperSource)

            Dim expectedOutput As XCData = <![CDATA[1
2
3
4
5
1
2
3
4
5
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
Method 3
File 1
True
True
Method 6
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
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)

            verifier.VerifyIL(
                "Program.VB$StateMachine_2_Goo.MoveNext()",
            <![CDATA[
{
  // Code size      149 (0x95)
  .maxstack  5
  .locals init (Integer V_0,
                Boolean() V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Goo.$State As Integer"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0073
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  dup
  IL_0013:  stloc.0
  IL_0014:  stfld      "Program.VB$StateMachine_2_Goo.$State As Integer"
  IL_0019:  ldsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
  IL_001e:  ldtoken    "Function Program.Goo() As System.Collections.Generic.IEnumerable(Of Integer)"
  IL_0023:  ldelem.ref
  IL_0024:  stloc.1
  IL_0025:  ldloc.1
  IL_0026:  brtrue.s   IL_004d
  IL_0028:  ldsfld     "System.Guid <PrivateImplementationDetails>.MVID"
  IL_002d:  ldtoken    "Function Program.Goo() As System.Collections.Generic.IEnumerable(Of Integer)"
  IL_0032:  ldtoken    Source Document 0
  IL_0037:  ldsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
  IL_003c:  ldtoken    "Function Program.Goo() As System.Collections.Generic.IEnumerable(Of Integer)"
  IL_0041:  ldelema    "Boolean()"
  IL_0046:  ldc.i4.2
  IL_0047:  call       "Function Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, Integer, Integer, ByRef Boolean(), Integer) As Boolean()"
  IL_004c:  stloc.1
  IL_004d:  ldloc.1
  IL_004e:  ldc.i4.0
  IL_004f:  ldc.i4.1
  IL_0050:  stelem.i1
  IL_0051:  ldloc.1
  IL_0052:  ldc.i4.1
  IL_0053:  ldc.i4.1
  IL_0054:  stelem.i1
  IL_0055:  ldarg.0
  IL_0056:  ldc.i4.1
  IL_0057:  stfld      "Program.VB$StateMachine_2_Goo.$VB$ResumableLocal_counter$0 As Integer"
  IL_005c:  ldarg.0
  IL_005d:  ldarg.0
  IL_005e:  ldfld      "Program.VB$StateMachine_2_Goo.$VB$ResumableLocal_counter$0 As Integer"
  IL_0063:  stfld      "Program.VB$StateMachine_2_Goo.$Current As Integer"
  IL_0068:  ldarg.0
  IL_0069:  ldc.i4.1
  IL_006a:  dup
  IL_006b:  stloc.0
  IL_006c:  stfld      "Program.VB$StateMachine_2_Goo.$State As Integer"
  IL_0071:  ldc.i4.1
  IL_0072:  ret
  IL_0073:  ldarg.0
  IL_0074:  ldc.i4.m1
  IL_0075:  dup
  IL_0076:  stloc.0
  IL_0077:  stfld      "Program.VB$StateMachine_2_Goo.$State As Integer"
  IL_007c:  ldarg.0
  IL_007d:  ldarg.0
  IL_007e:  ldfld      "Program.VB$StateMachine_2_Goo.$VB$ResumableLocal_counter$0 As Integer"
  IL_0083:  ldc.i4.1
  IL_0084:  add.ovf
  IL_0085:  stfld      "Program.VB$StateMachine_2_Goo.$VB$ResumableLocal_counter$0 As Integer"
  IL_008a:  ldarg.0
  IL_008b:  ldfld      "Program.VB$StateMachine_2_Goo.$VB$ResumableLocal_counter$0 As Integer"
  IL_0090:  ldc.i4.5
  IL_0091:  ble.s      IL_005c
  IL_0093:  ldc.i4.0
  IL_0094:  ret
}
                ]]>.Value)
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
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)

            verifier.VerifyIL(
                "Program.VB$StateMachine_4_Second.MoveNext()",
            <![CDATA[
{
  // Code size      375 (0x177)
  .maxstack  6
  .locals init (String V_0,
                Integer V_1,
                Program._Closure$__4-0 V_2, //$VB$Closure_0
                System.Runtime.CompilerServices.TaskAwaiter(Of String) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_4_Second.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse    IL_010e
    IL_000d:  newobj     "Sub Program._Closure$__4-0..ctor()"
    IL_0012:  stloc.2
    IL_0013:  ldloc.2
    IL_0014:  ldsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
    IL_0019:  ldtoken    "Function Program.Second(String) As System.Threading.Tasks.Task(Of String)"
    IL_001e:  ldelem.ref
    IL_001f:  stfld      "Program._Closure$__4-0.$VB$NonLocal_2 As Boolean()"
    IL_0024:  ldloc.2
    IL_0025:  ldfld      "Program._Closure$__4-0.$VB$NonLocal_2 As Boolean()"
    IL_002a:  brtrue.s   IL_0056
    IL_002c:  ldloc.2
    IL_002d:  ldsfld     "System.Guid <PrivateImplementationDetails>.MVID"
    IL_0032:  ldtoken    "Function Program.Second(String) As System.Threading.Tasks.Task(Of String)"
    IL_0037:  ldtoken    Source Document 0
    IL_003c:  ldsfld     "Boolean()() <PrivateImplementationDetails>.PayloadRoot0"
    IL_0041:  ldtoken    "Function Program.Second(String) As System.Threading.Tasks.Task(Of String)"
    IL_0046:  ldelema    "Boolean()"
    IL_004b:  ldc.i4.7
    IL_004c:  call       "Function Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, Integer, Integer, ByRef Boolean(), Integer) As Boolean()"
    IL_0051:  stfld      "Program._Closure$__4-0.$VB$NonLocal_2 As Boolean()"
    IL_0056:  ldloc.2
    IL_0057:  ldfld      "Program._Closure$__4-0.$VB$NonLocal_2 As Boolean()"
    IL_005c:  ldc.i4.0
    IL_005d:  ldc.i4.1
    IL_005e:  stelem.i1
    IL_005f:  ldloc.2
    IL_0060:  ldfld      "Program._Closure$__4-0.$VB$NonLocal_2 As Boolean()"
    IL_0065:  ldc.i4.1
    IL_0066:  ldc.i4.1
    IL_0067:  stelem.i1
    IL_0068:  ldloc.2
    IL_0069:  ldstr      ""
    IL_006e:  stfld      "Program._Closure$__4-0.$VB$Local_doubled As String"
    IL_0073:  ldloc.2
    IL_0074:  ldfld      "Program._Closure$__4-0.$VB$NonLocal_2 As Boolean()"
    IL_0079:  ldc.i4.4
    IL_007a:  ldc.i4.1
    IL_007b:  stelem.i1
    IL_007c:  ldarg.0
    IL_007d:  ldfld      "Program.VB$StateMachine_4_Second.$VB$Local_s As String"
    IL_0082:  callvirt   "Function String.get_Length() As Integer"
    IL_0087:  ldc.i4.2
    IL_0088:  ble.s      IL_00ac
    IL_008a:  ldloc.2
    IL_008b:  ldfld      "Program._Closure$__4-0.$VB$NonLocal_2 As Boolean()"
    IL_0090:  ldc.i4.2
    IL_0091:  ldc.i4.1
    IL_0092:  stelem.i1
    IL_0093:  ldloc.2
    IL_0094:  ldarg.0
    IL_0095:  ldfld      "Program.VB$StateMachine_4_Second.$VB$Local_s As String"
    IL_009a:  ldarg.0
    IL_009b:  ldfld      "Program.VB$StateMachine_4_Second.$VB$Local_s As String"
    IL_00a0:  call       "Function String.Concat(String, String) As String"
    IL_00a5:  stfld      "Program._Closure$__4-0.$VB$Local_doubled As String"
    IL_00aa:  br.s       IL_00c0
    IL_00ac:  ldloc.2
    IL_00ad:  ldfld      "Program._Closure$__4-0.$VB$NonLocal_2 As Boolean()"
    IL_00b2:  ldc.i4.3
    IL_00b3:  ldc.i4.1
    IL_00b4:  stelem.i1
    IL_00b5:  ldloc.2
    IL_00b6:  ldstr      "HuhHuh"
    IL_00bb:  stfld      "Program._Closure$__4-0.$VB$Local_doubled As String"
    IL_00c0:  ldloc.2
    IL_00c1:  ldfld      "Program._Closure$__4-0.$VB$NonLocal_2 As Boolean()"
    IL_00c6:  ldc.i4.6
    IL_00c7:  ldc.i4.1
    IL_00c8:  stelem.i1
    IL_00c9:  call       "Function System.Threading.Tasks.Task.get_Factory() As System.Threading.Tasks.TaskFactory"
    IL_00ce:  ldloc.2
    IL_00cf:  ldftn      "Function Program._Closure$__4-0._Lambda$__0() As String"
    IL_00d5:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
    IL_00da:  callvirt   "Function System.Threading.Tasks.TaskFactory.StartNew(Of String)(System.Func(Of String)) As System.Threading.Tasks.Task(Of String)"
    IL_00df:  callvirt   "Function System.Threading.Tasks.Task(Of String).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of String)"
    IL_00e4:  stloc.3
    IL_00e5:  ldloca.s   V_3
    IL_00e7:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of String).get_IsCompleted() As Boolean"
    IL_00ec:  brtrue.s   IL_012a
    IL_00ee:  ldarg.0
    IL_00ef:  ldc.i4.0
    IL_00f0:  dup
    IL_00f1:  stloc.1
    IL_00f2:  stfld      "Program.VB$StateMachine_4_Second.$State As Integer"
    IL_00f7:  ldarg.0
    IL_00f8:  ldloc.3
    IL_00f9:  stfld      "Program.VB$StateMachine_4_Second.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of String)"
    IL_00fe:  ldarg.0
    IL_00ff:  ldflda     "Program.VB$StateMachine_4_Second.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String)"
    IL_0104:  ldloca.s   V_3
    IL_0106:  ldarg.0
    IL_0107:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of String), Program.VB$StateMachine_4_Second)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of String), ByRef Program.VB$StateMachine_4_Second)"
    IL_010c:  leave.s    IL_0176
    IL_010e:  ldarg.0
    IL_010f:  ldc.i4.m1
    IL_0110:  dup
    IL_0111:  stloc.1
    IL_0112:  stfld      "Program.VB$StateMachine_4_Second.$State As Integer"
    IL_0117:  ldarg.0
    IL_0118:  ldfld      "Program.VB$StateMachine_4_Second.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of String)"
    IL_011d:  stloc.3
    IL_011e:  ldarg.0
    IL_011f:  ldflda     "Program.VB$StateMachine_4_Second.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of String)"
    IL_0124:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of String)"
    IL_012a:  ldloca.s   V_3
    IL_012c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of String).GetResult() As String"
    IL_0131:  ldloca.s   V_3
    IL_0133:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of String)"
    IL_0139:  stloc.0
    IL_013a:  leave.s    IL_0160
  }
  catch System.Exception
  {
    IL_013c:  dup
    IL_013d:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0142:  stloc.s    V_4
    IL_0144:  ldarg.0
    IL_0145:  ldc.i4.s   -2
    IL_0147:  stfld      "Program.VB$StateMachine_4_Second.$State As Integer"
    IL_014c:  ldarg.0
    IL_014d:  ldflda     "Program.VB$StateMachine_4_Second.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String)"
    IL_0152:  ldloc.s    V_4
    IL_0154:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String).SetException(System.Exception)"
    IL_0159:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_015e:  leave.s    IL_0176
  }
  IL_0160:  ldarg.0
  IL_0161:  ldc.i4.s   -2
  IL_0163:  dup
  IL_0164:  stloc.1
  IL_0165:  stfld      "Program.VB$StateMachine_4_Second.$State As Integer"
  IL_016a:  ldarg.0
  IL_016b:  ldflda     "Program.VB$StateMachine_4_Second.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String)"
  IL_0170:  ldloc.0
  IL_0171:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String).SetResult(String)"
  IL_0176:  ret
}
                ]]>.Value)
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
True
Method 10
File 1
True
True
Method 15
File 1
True
Method 16
File 1
True
True
True
True
True
True
Method 17
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
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestImplicitConstructorCoverage()
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
True
]]>

            Dim verifier As CompilationVerifier = CompileAndVerify(source, expectedOutput)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub MissingMethodNeededForAnalysis()
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
                If Diagnostic.Code = ERRID.ERR_MissingRuntimeHelper AndAlso Diagnostic.Arguments(0).Equals("System.Guid..ctor") Then
                    Return
                End If
            Next

            Assert.True(False)
        End Sub

        <Fact>
        Public Sub ExcludeFromCodeCoverageAttribute_Method()
            Dim source = "
Imports System
Imports System.Diagnostics.CodeAnalysis

Class C
    <ExcludeFromCodeCoverage>
    Sub M1()
        Console.WriteLine(1)
    End Sub

    Sub M2()
        Console.WriteLine(1)
    End Sub
End Class
"
            Dim verifier = CompileAndVerify(source & InstrumentationHelperSourceStr, options:=TestOptions.ReleaseDll)
            AssertNotInstrumented(verifier, "C.M1")
            AssertInstrumented(verifier, "C.M2")
        End Sub

        <Fact>
        Public Sub ExcludeFromCodeCoverageAttribute_Ctor()
            Dim source = "
Imports System
Imports System.Diagnostics.CodeAnalysis

Class C
    Dim a As Integer = 1

    <ExcludeFromCodeCoverage>
    Public Sub New()
        Console.WriteLine(3)
    End Sub
End Class
"
            Dim verifier = CompileAndVerify(source & InstrumentationHelperSourceStr, options:=TestOptions.ReleaseDll)
            AssertNotInstrumented(verifier, "C..ctor")
        End Sub

        <Fact>
        Public Sub ExcludeFromCodeCoverageAttribute_Cctor()
            Dim source = "
Imports System
Imports System.Diagnostics.CodeAnalysis

Class C
    Shared a As Integer = 1

    <ExcludeFromCodeCoverage>
    Shared Sub New()
        Console.WriteLine(3)
    End Sub
End Class
"
            Dim verifier = CompileAndVerify(source & InstrumentationHelperSourceStr, options:=TestOptions.ReleaseDll)
            AssertNotInstrumented(verifier, "C..cctor")
        End Sub

        <Fact>
        Public Sub ExcludeFromCodeCoverageAttribute_Lambdas_InMethod()
            Dim source = "
Imports System
Imports System.Diagnostics.CodeAnalysis

Class C
    <ExcludeFromCodeCoverage>
    Shared Sub M1()
        Dim s = New Action(Sub() Console.WriteLine(1))
        s.Invoke()
    End Sub

    Shared Sub M2()
        Dim s = New Action(Sub() Console.WriteLine(2))
        s.Invoke()
    End Sub
End Class
"
            Dim verifier = CompileAndVerify(source & InstrumentationHelperSourceStr, options:=TestOptions.ReleaseDll)

            AssertNotInstrumented(verifier, "C.M1")
            AssertNotInstrumented(verifier, "C._Closure$__._Lambda$__1-0")

            AssertInstrumented(verifier, "C.M2")
            AssertInstrumented(verifier, "C._Closure$__2-0._Lambda$__0")
        End Sub

        <Fact>
        Public Sub ExcludeFromCodeCoverageAttribute_Lambdas_InInitializers()
            Dim source = "
Imports System
Imports System.Diagnostics.CodeAnalysis

Class C
    Dim [IF] As Action = Sub() Console.WriteLine(1)

    ReadOnly Property IP As Action = Sub() Console.WriteLine(2)

    Shared SF As Action = Sub() Console.WriteLine(3)

    Shared ReadOnly Property SP As Action = Sub() Console.WriteLine(4)

    <ExcludeFromCodeCoverage>
    Sub New()
    End Sub

    Shared Sub New()
    End Sub
End Class
"
            Dim verifier = CompileAndVerify(source & InstrumentationHelperSourceStr, options:=TestOptions.ReleaseDll)
            verifier.VerifyDiagnostics()

            AssertNotInstrumented(verifier, "C..ctor")
            AssertNotInstrumented(verifier, "C._Closure$__._Lambda$__8-0")
            AssertNotInstrumented(verifier, "C._Closure$__._Lambda$__8-1")

            AssertInstrumented(verifier, "C..cctor")
            AssertInstrumented(verifier, "C._Closure$__9-0._Lambda$__0")
            AssertInstrumented(verifier, "C._Closure$__9-0._Lambda$__1")
        End Sub

        <Fact>
        Public Sub ExcludeFromCodeCoverageAttribute_Lambdas_InAccessors()
            Dim source = "
Imports System
Imports System.Diagnostics.CodeAnalysis

Class C

    <ExcludeFromCodeCoverage>
    Property P1 As Integer
        Get
            Dim s = Sub() Console.WriteLine(1)
            s()
            Return 1
        End Get

        Set
            Dim s = Sub() Console.WriteLine(2)
            s()
        End Set
    End Property

    Property P2 As Integer
        Get
            Dim s = Sub() Console.WriteLine(3)
            s()
            Return 3
        End Get

        Set
            Dim s = Sub() Console.WriteLine(4)
            s()
        End Set
    End Property
End Class
"
            Dim verifier = CompileAndVerify(source & InstrumentationHelperSourceStr, options:=TestOptions.ReleaseDll)

            AssertNotInstrumented(verifier, "C.get_P1")
            AssertNotInstrumented(verifier, "C.set_P1")
            AssertNotInstrumented(verifier, "C._Closure$__._Lambda$__2-0")
            AssertNotInstrumented(verifier, "C._Closure$__._Lambda$__3-0")

            AssertInstrumented(verifier, "C.get_P2")
            AssertInstrumented(verifier, "C.set_P2")
            AssertInstrumented(verifier, "C._Closure$__6-0._Lambda$__0")
            AssertInstrumented(verifier, "C._Closure$__5-0._Lambda$__0")
        End Sub

        <Fact>
        Public Sub ExcludeFromCodeCoverageAttribute_Type()
            Dim source = "
Imports System
Imports System.Diagnostics.CodeAnalysis

<ExcludeFromCodeCoverage>
Class C
    Dim x As Integer = 1

    Shared Sub New()
    End Sub

    Sub M1()
        Console.WriteLine(1)
    End Sub

    Property P As Integer
        Get
            Return 1
        End Get
        Set
        End Set
    End Property

    Custom Event E As Action
        AddHandler(v As Action)
        End AddHandler

        RemoveHandler(v As Action)
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Class D
    Dim x As Integer = 1

    Shared Sub New()
    End Sub

    Sub M1()
        Console.WriteLine(1)
    End Sub

    Property P As Integer
        Get
            Return 1
        End Get
        Set
        End Set
    End Property

    Custom Event E As Action
        AddHandler(v As Action)
        End AddHandler

        RemoveHandler(v As Action)
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
    End Event
End Class
"
            Dim verifier = CompileAndVerify(source & InstrumentationHelperSourceStr, options:=TestOptions.ReleaseDll)

            AssertNotInstrumented(verifier, "C..ctor")
            AssertNotInstrumented(verifier, "C..cctor")
            AssertNotInstrumented(verifier, "C.M1")
            AssertNotInstrumented(verifier, "C.get_P")
            AssertNotInstrumented(verifier, "C.set_P")
            AssertNotInstrumented(verifier, "C.add_E")
            AssertNotInstrumented(verifier, "C.remove_E")
            AssertNotInstrumented(verifier, "C.raise_E")

            AssertInstrumented(verifier, "D..ctor")
            AssertInstrumented(verifier, "D..cctor")
            AssertInstrumented(verifier, "D.M1")
            AssertInstrumented(verifier, "D.get_P")
            AssertInstrumented(verifier, "D.set_P")
            AssertInstrumented(verifier, "D.add_E")
            AssertInstrumented(verifier, "D.remove_E")
            AssertInstrumented(verifier, "D.raise_E")
        End Sub

        <Fact>
        Public Sub ExcludeFromCodeCoverageAttribute_NestedType()
            Dim source = "
Imports System
Imports System.Diagnostics.CodeAnalysis

Class A
    Class B1
        <ExcludeFromCodeCoverage>
        Class C
            Sub M1()
                Console.WriteLine(1)
            End Sub
        End Class

        Sub M2()
            Console.WriteLine(2)
        End Sub
    End Class

    <ExcludeFromCodeCoverage>
    Partial Class B2
        Partial Class C1
            Sub M3()
                Console.WriteLine(3)
            End Sub
        End Class

        Class C2
            Sub M4()
                Console.WriteLine(4)
            End Sub
        End Class

        Sub M5()
            Console.WriteLine(5)
        End Sub
    End Class

    Partial Class B2
        <ExcludeFromCodeCoverage>
        Partial Class C1
            Sub M6()
                Console.WriteLine(6)
            End Sub
        End Class

        Sub M7()
            Console.WriteLine(7)
        End Sub
    End Class

    Sub M8()
        Console.WriteLine(8)
    End Sub
End Class
"
            Dim verifier = CompileAndVerify(source & InstrumentationHelperSourceStr, options:=TestOptions.ReleaseDll)
            AssertNotInstrumented(verifier, "A.B1.C.M1")
            AssertInstrumented(verifier, "A.B1.M2")
            AssertNotInstrumented(verifier, "A.B2.C1.M3")
            AssertNotInstrumented(verifier, "A.B2.C2.M4")
            AssertNotInstrumented(verifier, "A.B2.C1.M6")
            AssertNotInstrumented(verifier, "A.B2.M7")
            AssertInstrumented(verifier, "A.M8")
        End Sub

        <Fact>
        Public Sub ExcludeFromCodeCoverageAttribute_Accessors()
            Dim source = "
Imports System
Imports System.Diagnostics.CodeAnalysis

Class C
    <ExcludeFromCodeCoverage>
    Property P1 As Integer
        Get
            Return 1
        End Get
        Set
        End Set
    End Property
          
    <ExcludeFromCodeCoverage>
    Custom Event E1 As Action
        AddHandler(v As Action)
        End AddHandler

        RemoveHandler(v As Action)
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
    End Event
                                            
    Property P2 As Integer
        Get
            Return 2
        End Get
        Set
        End Set
    End Property

    Custom Event E2 As Action
        AddHandler(v As Action)
        End AddHandler

        RemoveHandler(v As Action)
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
    End Event
End Class
"
            Dim verifier = CompileAndVerify(source & InstrumentationHelperSourceStr, options:=TestOptions.ReleaseDll)

            AssertNotInstrumented(verifier, "C.get_P1")
            AssertNotInstrumented(verifier, "C.set_P1")
            AssertNotInstrumented(verifier, "C.add_E1")
            AssertNotInstrumented(verifier, "C.remove_E1")
            AssertNotInstrumented(verifier, "C.raise_E1")

            AssertInstrumented(verifier, "C.get_P2")
            AssertInstrumented(verifier, "C.set_P2")
            AssertInstrumented(verifier, "C.add_E2")
            AssertInstrumented(verifier, "C.remove_E2")
            AssertInstrumented(verifier, "C.raise_E2")
        End Sub

        <Fact>
        Public Sub ExcludeFromCodeCoverageAttribute_CustomDefinition_Good()
            Dim source = "
Imports System.Diagnostics.CodeAnalysis

Namespace System.Diagnostics.CodeAnalysis

    <AttributeUsage(AttributeTargets.Class)>
    Public Class ExcludeFromCodeCoverageAttribute
        Inherits Attribute

        Public Sub New()
        End Sub
    End Class
End Namespace

<ExcludeFromCodeCoverage>
Class C
    Sub M()
    End Sub
End Class

Class D
    Sub M()
    End Sub
End Class
"
            Dim c = CreateCompilationWithMscorlib40(source & InstrumentationHelperSourceStr, options:=TestOptions.ReleaseDll)
            c.VerifyDiagnostics()

            Dim verifier = CompileAndVerify(c, emitOptions:=EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))
            c.VerifyEmitDiagnostics()

            AssertNotInstrumented(verifier, "C.M")
            AssertInstrumented(verifier, "D.M")
        End Sub

        <Fact>
        Public Sub ExcludeFromCodeCoverageAttribute_CustomDefinition_Bad()
            Dim source = "
Imports System.Diagnostics.CodeAnalysis

Namespace System.Diagnostics.CodeAnalysis

    <AttributeUsage(AttributeTargets.Class)>
    Public Class ExcludeFromCodeCoverageAttribute
        Inherits Attribute

        Public Sub New(x As Integer)
        End Sub
    End Class
End Namespace

<ExcludeFromCodeCoverage(1)>
Class C
    Sub M()
    End Sub
End Class

Class D
    Sub M()
    End Sub
End Class
"
            Dim c = CreateCompilationWithMscorlib40(source & InstrumentationHelperSourceStr, options:=TestOptions.ReleaseDll)
            c.VerifyDiagnostics()

            Dim verifier = CompileAndVerify(c, emitOptions:=EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))
            c.VerifyEmitDiagnostics()

            AssertInstrumented(verifier, "C.M")
            AssertInstrumented(verifier, "D.M")
        End Sub

        <Fact>
        Public Sub TestPartialMethodsWithImplementation()
            Dim testSource = <file name="c.vb">
                                 <![CDATA[
Imports System

Partial Class Class1
    Private Partial Sub Method1(x as Integer)
    End Sub
    Public Sub Method2(x as Integer)
        Console.WriteLine("Method2: x = {0}", x)
        Method1(x)
    End Sub
End Class

Partial Class Class1
    Private Sub Method1(x as Integer)
        Console.WriteLine("Method1: x = {0}", x)
        If x > 0
            Console.WriteLine("Method1: x > 0")
            Method1(0)
        ElseIf x < 0
            Console.WriteLine("Method1: x < 0")
        End If
    End Sub
End Class

Module Program
    Public Sub Main()
        Test()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub Test()
        Console.WriteLine("Test")
        Dim c = new Class1()
        c.Method2(1)
    End Sub
End Module
]]>
                             </file>

            Dim source = <compilation>
                             <%= testSource %>
                             <%= InstrumentationHelperSource %>
                         </compilation>

            Dim checker = New VBInstrumentationChecker()
            checker.Method(1, 1, "New", expectBodySpan:=False)
            checker.Method(2, 1, "Private Sub Method1(x as Integer)").
                True("Console.WriteLine(""Method1: x = {0}"", x)").
                True("Console.WriteLine(""Method1: x > 0"")").
                True("Method1(0)").
                False("Console.WriteLine(""Method1: x < 0"")").
                True("x < 0").
                True("x > 0")
            checker.Method(3, 1, "Public Sub Method2(x as Integer)").
                True("Console.WriteLine(""Method2: x = {0}"", x)").
                True("Method1(x)")
            checker.Method(4, 1, "Public Sub Main()").
                True("Test()").
                True("Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()")
            checker.Method(5, 1, "Sub Test()").
                True("Console.WriteLine(""Test"")").
                True("new Class1()").
                True("c.Method2(1)")
            checker.Method(8, 1).
                True().
                False().
                True().
                True().
                True().
                True().
                True().
                True().
                True().
                True().
                True().
                True()

            Dim expectedOutput = "Test
Method2: x = 1
Method1: x = 1
Method1: x > 0
Method1: x = 0
" + XCDataToString(checker.ExpectedOutput)

            Dim verifier = CompileAndVerify(source, expectedOutput, options:=TestOptions.ReleaseExe)
            checker.CompleteCheck(verifier.Compilation, testSource)
            verifier.VerifyDiagnostics()

            verifier = CompileAndVerify(source, expectedOutput, options:=TestOptions.DebugExe)
            checker.CompleteCheck(verifier.Compilation, testSource)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestPartialMethodsWithoutImplementation()
            Dim testSource = <file name="c.vb">
                                 <![CDATA[
Imports System

Partial Class Class1
    Private Partial Sub Method1(x as Integer)
    End Sub
    Public Sub Method2(x as Integer)
        Console.WriteLine("Method2: x = {0}", x)
        Method1(x)
    End Sub
End Class

Module Program
    Public Sub Main()
        Test()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub Test()
        Console.WriteLine("Test")
        Dim c = new Class1()
        c.Method2(1)
    End Sub
End Module
]]>
                             </file>

            Dim source = <compilation>
                             <%= testSource %>
                             <%= InstrumentationHelperSource %>
                         </compilation>

            Dim checker = New VBInstrumentationChecker()
            checker.Method(1, 1, "New", expectBodySpan:=False)
            checker.Method(2, 1, "Public Sub Method2(x as Integer)").
                True("Console.WriteLine(""Method2: x = {0}"", x)")
            checker.Method(3, 1, "Public Sub Main()").
                True("Test()").
                True("Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()")
            checker.Method(4, 1, "Sub Test()").
                True("Console.WriteLine(""Test"")").
                True("new Class1()").
                True("c.Method2(1)")
            checker.Method(7, 1).
                True().
                False().
                True().
                True().
                True().
                True().
                True().
                True().
                True().
                True().
                True().
                True()

            Dim expectedOutput = "Test
Method2: x = 1
" + XCDataToString(checker.ExpectedOutput)

            Dim verifier = CompileAndVerify(source, expectedOutput, options:=TestOptions.ReleaseExe)
            checker.CompleteCheck(verifier.Compilation, testSource)
            verifier.VerifyDiagnostics()

            verifier = CompileAndVerify(source, expectedOutput, options:=TestOptions.DebugExe)
            checker.CompleteCheck(verifier.Compilation, testSource)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestSynthesizedConstructorWithSpansInMultipleFilesCoverage()
            Dim source1 = <file name="aa.vb">
                              <![CDATA[
Imports System

Partial Class Class1
    Dim a As Action(Of Integer) =
            Sub(i As Integer)
                Console.WriteLine(i)
            End Sub
End Class

Module Program
    Public Sub Main()
        Test()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub Test()
        Console.WriteLine("Test")
        Dim c = new Class1()
        c.Method1(1)
    End Sub
End Module
]]>
                          </file>

            Dim source2 = <file name="bb.vb">
                              <![CDATA[
Imports System

Partial Class Class1
    Dim x As Integer = 1

    Sub Method1(i As Integer)
        a(i)
        Console.WriteLine(x)
        Console.WriteLine(y)
        Console.WriteLine(z)
    End Sub
End Class
]]>
                          </file>

            Dim source3 = <file name="cc.vb">
                              <![CDATA[
Partial Class Class1
    Dim y As Integer = 2
    Dim z As Integer = 3
End Class
]]>
                          </file>

            Dim source = <compilation>
                             <%= source1 %>
                             <%= source2 %>
                             <%= source3 %>
                             <%= InstrumentationHelperSource %>
                         </compilation>

            Dim expectedOutput = <![CDATA[Test
1
1
2
3
Flushing
Method 1
File 1
File 2
File 3
True
True
True
True
True
Method 2
File 2
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
Method 4
File 1
True
True
True
True
Method 7
File 4
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
True
]]>

            Dim verifier = CompileAndVerify(source, expectedOutput, options:=TestOptions.ReleaseExe)
            verifier.VerifyDiagnostics()

            verifier = CompileAndVerify(source, expectedOutput, options:=TestOptions.DebugExe)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestSynthesizedStaticConstructorWithSpansInMultipleFilesCoverage()
            Dim source1 = <file name="aa.vb">
                              <![CDATA[
Imports System

Partial Class Class1
    Shared Dim a As Action(Of Integer) =
            Sub(i As Integer)
                Console.WriteLine(i)
            End Sub
End Class

Module Program
    Public Sub Main()
        Test()
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()
    End Sub

    Sub Test()
        Console.WriteLine("Test")
        Dim c = new Class1()
        Class1.Method1(1)
    End Sub
End Module
]]>
                          </file>

            Dim source2 = <file name="bb.vb">
                              <![CDATA[
Imports System

Partial Class Class1
    Shared Dim x As Integer = 1

    Shared Sub Method1(i As Integer)
        a(i)
        Console.WriteLine(x)
        Console.WriteLine(y)
        Console.WriteLine(z)
    End Sub
End Class
]]>
                          </file>

            Dim source3 = <file name="cc.vb">
                              <![CDATA[
Partial Class Class1
    Shared Dim y As Integer = 2
    Shared Dim z As Integer = 3
End Class
]]>
                          </file>

            Dim source = <compilation>
                             <%= source1 %>
                             <%= source2 %>
                             <%= source3 %>
                             <%= InstrumentationHelperSource %>
                         </compilation>

            Dim expectedOutput = <![CDATA[Test
1
1
2
3
Flushing
Method 1
File 1
File 2
File 3
True
True
True
True
True
Method 2
File 1
Method 3
File 2
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
Method 5
File 1
True
True
True
True
Method 8
File 4
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
True
]]>

            Dim verifier = CompileAndVerify(source, expectedOutput, options:=TestOptions.ReleaseExe)
            verifier.VerifyDiagnostics()

            verifier = CompileAndVerify(source, expectedOutput, options:=TestOptions.DebugExe)
            verifier.VerifyDiagnostics()
        End Sub

        Private Shared Sub AssertNotInstrumented(verifier As CompilationVerifier, qualifiedMethodName As String)
            AssertInstrumented(verifier, qualifiedMethodName, expected:=False)
        End Sub

        Private Shared Sub AssertInstrumented(verifier As CompilationVerifier, qualifiedMethodName As String, Optional expected As Boolean = True)
            Dim il = verifier.VisualizeIL(qualifiedMethodName)

            ' Tests using this helper are constructed such that instrumented methods contain a call to CreatePayload, 
            ' lambdas a reference to payload Boolean array.
            Dim instrumented = il.Contains("CreatePayload") OrElse il.Contains("As Boolean()")

            Assert.True(expected = instrumented, $"Method '{qualifiedMethodName}' should {If(expected, "be", "not be")} instrumented. Actual IL:{Environment.NewLine}{il}")
        End Sub

        Private Function CreateCompilation(source As XElement) As Compilation
            Return CreateEmptyCompilationWithReferences(source, references:=New MetadataReference() {}, options:=TestOptions.ReleaseExe.WithDeterministic(True))
        End Function

        Private Overloads Function CompileAndVerify(source As XElement, Optional expectedOutput As XCData = Nothing, Optional options As VisualBasicCompilationOptions = Nothing) As CompilationVerifier
            Return CompileAndVerify(source,
                                    LatestVbReferences,
                                    XCDataToString(expectedOutput),
                                    options:=If(options, TestOptions.ReleaseExe).WithDeterministic(True),
                                    emitOptions:=EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))
        End Function

        Private Overloads Function CompileAndVerify(source As XElement, Optional expectedOutput As String = Nothing, Optional options As VisualBasicCompilationOptions = Nothing) As CompilationVerifier
            Return CompileAndVerify(source,
                                    LatestVbReferences,
                                    expectedOutput,
                                    options:=If(options, TestOptions.ReleaseExe).WithDeterministic(True),
                                    emitOptions:=EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)))
        End Function

        Private Overloads Function CompileAndVerify(source As String, Optional expectedOutput As String = Nothing, Optional options As VisualBasicCompilationOptions = Nothing) As CompilationVerifier
            Return CompileAndVerifyEx(source,
                                    LatestVbReferences,
                                    expectedOutput,
                                    options:=If(options, TestOptions.ReleaseExe).WithDeterministic(True),
                                    emitOptions:=EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)),
                                    targetFramework:=TargetFramework.Empty)
        End Function
    End Class
End Namespace
