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
        Public Sub TestLoops()
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

        Private Overloads Function CompileAndVerify(source As XElement, expectedOutput As XCData, Optional options As VisualBasicCompilationOptions = Nothing) As CompilationVerifier
            Return MyBase.CompileAndVerify(source, expectedOutput:=expectedOutput, options:=If(options IsNot Nothing, options, TestOptions.ReleaseExe).WithDeterministic(True), emitOptions:=EmitOptions.Default.WithInstrument("Test.Flag"))
        End Function
    End Class
End Namespace
