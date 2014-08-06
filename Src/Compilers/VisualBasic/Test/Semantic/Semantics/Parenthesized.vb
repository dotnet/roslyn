' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class Parenthesized
        Inherits BasicTestBase

        <Fact>
        Public Sub Bug4353()

            Dim compilationDef =
<compilation name="Bug4353">
    <file name="a.vb">
Module Program
  Sub Main()
    Dim y = (Nothing)
    System.Console.WriteLine("[{0}]",y)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="[]")
        End Sub

        <Fact>
        Public Sub Bug4252()

            Dim compilationDef =
<compilation name="Bug4252">
    <file name="a.vb">
Option Strict On
 
Imports System
 
Module M
  Sub Main()
    Dim x As DayOfWeek = (0)
    System.Console.WriteLine(x.GetType())
    System.Console.WriteLine(x)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
System.DayOfWeek
0
]]>)
        End Sub

        <Fact>
        Public Sub Bug4262_1()

            Dim compilationDef =
<compilation name="Bug4353">
    <file name="a.vb">
Class C1
    Class C2
    End Class
End Class

Module Program
  Sub Main()
    System.Console.WriteLine((System.String).Equals("", ""))
    System.Console.WriteLine(((((System.String)))).Equals("", "1"))
    System.Console.WriteLine((C1).C2.Equals("", "1"))
    System.Console.WriteLine(((C1).C2).Equals("", ""))
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
True
False
False
True
]]>)
        End Sub

        <Fact>
        Public Sub Bug4262_2()

            Dim compilationDef =
<compilation name="Bug4353">
    <file name="a.vb">
Class C1
    Class C2
    End Class
End Class

Module Program
  Sub Main()
    System.Console.WriteLine((System).String.Equals("", ""))
    System.Console.WriteLine(System.(String).Equals("", "1"))
    System.Console.WriteLine((C1).(C2).Equals("", "1"))
    System.Console.WriteLine(C1.(C2).Equals("", ""))
    System.Console.WriteLine(GetType((System.String)))
    System.Console.WriteLine(GetType((C1).C2)))
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30112: 'System' is a namespace and cannot be used as an expression.
    System.Console.WriteLine((System).String.Equals("", ""))
                              ~~~~~~
BC30203: Identifier expected.
    System.Console.WriteLine(System.(String).Equals("", "1"))
                                    ~
BC32093: 'Of' required when specifying type arguments for a generic type or method.
    System.Console.WriteLine(System.(String).Equals("", "1"))
                                     ~
BC30203: Identifier expected.
    System.Console.WriteLine((C1).(C2).Equals("", "1"))
                                  ~
BC30451: 'C2' is not declared. It may be inaccessible due to its protection level.
    System.Console.WriteLine((C1).(C2).Equals("", "1"))
                                   ~~
BC30203: Identifier expected.
    System.Console.WriteLine(C1.(C2).Equals("", ""))
                                ~
BC30451: 'C2' is not declared. It may be inaccessible due to its protection level.
    System.Console.WriteLine(C1.(C2).Equals("", ""))
                                 ~~
BC30182: Type expected.
    System.Console.WriteLine(GetType((System.String)))
                                     ~
BC30638: Array bounds cannot appear in type specifiers.
    System.Console.WriteLine(GetType((System.String)))
                                      ~~~~~~~~~~~~~
BC30456: 'C2' is not a member of 'System.Type'.
    System.Console.WriteLine(GetType((C1).C2)))
                             ~~~~~~~~~~~~~~~
BC30182: Type expected.
    System.Console.WriteLine(GetType((C1).C2)))
                                     ~
BC30638: Array bounds cannot appear in type specifiers.
    System.Console.WriteLine(GetType((C1).C2)))
                                      ~~
BC30198: ')' expected.
    System.Console.WriteLine(GetType((C1).C2)))
                                         ~
</expected>)
        End Sub

        <Fact()>
        Public Sub Reclassification1()

            Dim compilationDef =
<compilation name="Reclassification">
    <file name="a.vb">
Module Program

    Delegate Function DT1() As Integer

    Function F1() As Integer
        System.Console.WriteLine("F1")
        Return 1
    End Function

    Sub Main()
        Dim d1 As DT1
        d1 = (AddressOf F1)
        d1()

        d1 = (((AddressOf F1)))
        d1()

        d1 = CType((((AddressOf F1))), DT1)
        d1()

        d1 = DirectCast((((AddressOf F1))), DT1)
        d1()

        d1 = TryCast((((AddressOf F1))), DT1)
        d1()

        'd1 = New DT1((((AddressOf F1))))

        d1 = (Function()
                  System.Console.WriteLine("L1")
                  Return 1
              End Function)
        d1()

        d1 = (((Function() 'BIND1:"Function()"
                    System.Console.WriteLine("L2")
                    Return 1
                End Function)))
        d1()

        d1 = CType((((Function()
                          System.Console.WriteLine("L3")
                          Return 1
                      End Function))), DT1)
        d1()

        d1 = DirectCast((((Function()
                               System.Console.WriteLine("L4")
                               Return 1
                           End Function))), DT1)
        d1()

        d1 = DirectCast((((Function()
                               System.Console.WriteLine("L5")
                               Return 1
                           End Function))), DT1)
        d1()


        'd1 = New DT1((((Function()
        '                    System.Console.WriteLine("L6")
        '                    Return 1
        '                End Function))))

        Call (((Function()
                    System.Console.WriteLine("L7")
                    Return 1
                End Function)))()

        System.Console.WriteLine((((F1))))
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            If True Then
                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("Program.DT1", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Widening, Lambda", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[F1
F1
F1
F1
F1
L1
L2
L3
L4
L5
L7
F1
1]]>.Value.Replace(vbLf, vbCrLf))

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)


            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      329 (0x149)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      "Function Program.F1() As Integer"
  IL_0007:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_0011:  pop
  IL_0012:  ldnull
  IL_0013:  ldftn      "Function Program.F1() As Integer"
  IL_0019:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_001e:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_0023:  pop
  IL_0024:  ldnull
  IL_0025:  ldftn      "Function Program.F1() As Integer"
  IL_002b:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_0030:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_0035:  pop
  IL_0036:  ldnull
  IL_0037:  ldftn      "Function Program.F1() As Integer"
  IL_003d:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_0042:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_0047:  pop
  IL_0048:  ldnull
  IL_0049:  ldftn      "Function Program.F1() As Integer"
  IL_004f:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_0054:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_0059:  pop
  IL_005a:  ldsfld     "Program._ClosureCache$__2 As Program.DT1"
  IL_005f:  brfalse.s  IL_0068
  IL_0061:  ldsfld     "Program._ClosureCache$__2 As Program.DT1"
  IL_0066:  br.s       IL_007a
  IL_0068:  ldnull
  IL_0069:  ldftn      "Function Program._Lambda$__1(Object) As Integer"
  IL_006f:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_0074:  dup
  IL_0075:  stsfld     "Program._ClosureCache$__2 As Program.DT1"
  IL_007a:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_007f:  pop
  IL_0080:  ldsfld     "Program._ClosureCache$__4 As Program.DT1"
  IL_0085:  brfalse.s  IL_008e
  IL_0087:  ldsfld     "Program._ClosureCache$__4 As Program.DT1"
  IL_008c:  br.s       IL_00a0
  IL_008e:  ldnull
  IL_008f:  ldftn      "Function Program._Lambda$__3(Object) As Integer"
  IL_0095:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_009a:  dup
  IL_009b:  stsfld     "Program._ClosureCache$__4 As Program.DT1"
  IL_00a0:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_00a5:  pop
  IL_00a6:  ldsfld     "Program._ClosureCache$__6 As Program.DT1"
  IL_00ab:  brfalse.s  IL_00b4
  IL_00ad:  ldsfld     "Program._ClosureCache$__6 As Program.DT1"
  IL_00b2:  br.s       IL_00c6
  IL_00b4:  ldnull
  IL_00b5:  ldftn      "Function Program._Lambda$__5(Object) As Integer"
  IL_00bb:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_00c0:  dup
  IL_00c1:  stsfld     "Program._ClosureCache$__6 As Program.DT1"
  IL_00c6:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_00cb:  pop
  IL_00cc:  ldsfld     "Program._ClosureCache$__8 As Program.DT1"
  IL_00d1:  brfalse.s  IL_00da
  IL_00d3:  ldsfld     "Program._ClosureCache$__8 As Program.DT1"
  IL_00d8:  br.s       IL_00ec
  IL_00da:  ldnull
  IL_00db:  ldftn      "Function Program._Lambda$__7(Object) As Integer"
  IL_00e1:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_00e6:  dup
  IL_00e7:  stsfld     "Program._ClosureCache$__8 As Program.DT1"
  IL_00ec:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_00f1:  pop
  IL_00f2:  ldsfld     "Program._ClosureCache$__10 As Program.DT1"
  IL_00f7:  brfalse.s  IL_0100
  IL_00f9:  ldsfld     "Program._ClosureCache$__10 As Program.DT1"
  IL_00fe:  br.s       IL_0112
  IL_0100:  ldnull
  IL_0101:  ldftn      "Function Program._Lambda$__9(Object) As Integer"
  IL_0107:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_010c:  dup
  IL_010d:  stsfld     "Program._ClosureCache$__10 As Program.DT1"
  IL_0112:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_0117:  pop
  IL_0118:  ldsfld     "Program._ClosureCache$__12 As <generated method>"
  IL_011d:  brfalse.s  IL_0126
  IL_011f:  ldsfld     "Program._ClosureCache$__12 As <generated method>"
  IL_0124:  br.s       IL_0138
  IL_0126:  ldnull
  IL_0127:  ldftn      "Function Program._Lambda$__11(Object) As Integer"
  IL_012d:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0132:  dup
  IL_0133:  stsfld     "Program._ClosureCache$__12 As <generated method>"
  IL_0138:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_013d:  pop
  IL_013e:  call       "Function Program.F1() As Integer"
  IL_0143:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0148:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Reclassification2()

            Dim compilationDef =
<compilation name="Reclassification">
    <file name="a.vb">
Module Program

    Delegate Function DT1() As Integer

    Function F1() As Integer
        System.Console.WriteLine("F1")
        Return 1
    End Function

    Sub Main()
        Dim d1 As DT1

        d1 = New DT1((((AddressOf F1))))

        d1 = New DT1((((Function()
                            System.Console.WriteLine("L6")
                            Return 1
                        End Function))))
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32008: Delegate 'Program.DT1' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
        d1 = New DT1((((AddressOf F1))))
                    ~~~~~~~~~~~~~~~~~~~~
BC32008: Delegate 'Program.DT1' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
        d1 = New DT1((((Function()
                    ~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub ReclassificationAndDominantTypeInference()

            Dim compilationDef =
<compilation name="Reclassification">
    <file name="a.vb">
Module Program

    Delegate Function DT1() As Integer

    Function F1() As Integer
        System.Console.WriteLine("F1")
        Return 1
    End Function

    Sub Main()
        Dim d As DT1 = Nothing

        Dim x = If(d, ((AddressOf F1)))
        x()

        Dim y = If(d, (((Function()
                             System.Console.WriteLine("L1")
                             Return 1
                         End Function))))
        y()

        Dim x1 = If(((AddressOf F1)), d)
        x1()

        Dim y1 = If((((Function()
                           System.Console.WriteLine("L2")
                           Return 1
                       End Function))), d)
        y1()

        Dim x2 = If(((AddressOf F1)), (((Function()
                                             System.Console.WriteLine("L3")
                                             Return 1
                                         End Function))))
        x2()

        Dim y2 = If((((Function()
                           System.Console.WriteLine("L4")
                           Return 1
                       End Function))), ((AddressOf F1)))
        y2()

        Dim x3 = If((((Function()
                           System.Console.WriteLine("L5")
                           Return 1
                       End Function))), (((Function()
                                               System.Console.WriteLine("L6")
                                               Return 1
                                           End Function))))
        x3()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[F1
L1
F1
L2
F1
L4
L5]]>.Value.Replace(vbLf, vbCrLf))

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      317 (0x13d)
  .maxstack  2
  .locals init (Program.DT1 V_0) //d
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  dup
  IL_0004:  brtrue.s   IL_0013
  IL_0006:  pop
  IL_0007:  ldnull
  IL_0008:  ldftn      "Function Program.F1() As Integer"
  IL_000e:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_0013:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_0018:  pop
  IL_0019:  ldloc.0
  IL_001a:  dup
  IL_001b:  brtrue.s   IL_003e
  IL_001d:  pop
  IL_001e:  ldsfld     "Program._ClosureCache$__2 As Program.DT1"
  IL_0023:  brfalse.s  IL_002c
  IL_0025:  ldsfld     "Program._ClosureCache$__2 As Program.DT1"
  IL_002a:  br.s       IL_003e
  IL_002c:  ldnull
  IL_002d:  ldftn      "Function Program._Lambda$__1(Object) As Integer"
  IL_0033:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_0038:  dup
  IL_0039:  stsfld     "Program._ClosureCache$__2 As Program.DT1"
  IL_003e:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_0043:  pop
  IL_0044:  ldnull
  IL_0045:  ldftn      "Function Program.F1() As Integer"
  IL_004b:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_0050:  dup
  IL_0051:  brtrue.s   IL_0055
  IL_0053:  pop
  IL_0054:  ldloc.0
  IL_0055:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_005a:  pop
  IL_005b:  ldsfld     "Program._ClosureCache$__4 As Program.DT1"
  IL_0060:  brfalse.s  IL_0069
  IL_0062:  ldsfld     "Program._ClosureCache$__4 As Program.DT1"
  IL_0067:  br.s       IL_007b
  IL_0069:  ldnull
  IL_006a:  ldftn      "Function Program._Lambda$__3(Object) As Integer"
  IL_0070:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_0075:  dup
  IL_0076:  stsfld     "Program._ClosureCache$__4 As Program.DT1"
  IL_007b:  dup
  IL_007c:  brtrue.s   IL_0080
  IL_007e:  pop
  IL_007f:  ldloc.0
  IL_0080:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_0085:  pop
  IL_0086:  ldnull
  IL_0087:  ldftn      "Function Program.F1() As Integer"
  IL_008d:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0092:  dup
  IL_0093:  brtrue.s   IL_00b6
  IL_0095:  pop
  IL_0096:  ldsfld     "Program._ClosureCache$__6 As <generated method>"
  IL_009b:  brfalse.s  IL_00a4
  IL_009d:  ldsfld     "Program._ClosureCache$__6 As <generated method>"
  IL_00a2:  br.s       IL_00b6
  IL_00a4:  ldnull
  IL_00a5:  ldftn      "Function Program._Lambda$__5(Object) As Integer"
  IL_00ab:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_00b0:  dup
  IL_00b1:  stsfld     "Program._ClosureCache$__6 As <generated method>"
  IL_00b6:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_00bb:  pop
  IL_00bc:  ldsfld     "Program._ClosureCache$__8 As <generated method>"
  IL_00c1:  brfalse.s  IL_00ca
  IL_00c3:  ldsfld     "Program._ClosureCache$__8 As <generated method>"
  IL_00c8:  br.s       IL_00dc
  IL_00ca:  ldnull
  IL_00cb:  ldftn      "Function Program._Lambda$__7(Object) As Integer"
  IL_00d1:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_00d6:  dup
  IL_00d7:  stsfld     "Program._ClosureCache$__8 As <generated method>"
  IL_00dc:  dup
  IL_00dd:  brtrue.s   IL_00ec
  IL_00df:  pop
  IL_00e0:  ldnull
  IL_00e1:  ldftn      "Function Program.F1() As Integer"
  IL_00e7:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_00ec:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_00f1:  pop
  IL_00f2:  ldsfld     "Program._ClosureCache$__10 As <generated method>"
  IL_00f7:  brfalse.s  IL_0100
  IL_00f9:  ldsfld     "Program._ClosureCache$__10 As <generated method>"
  IL_00fe:  br.s       IL_0112
  IL_0100:  ldnull
  IL_0101:  ldftn      "Function Program._Lambda$__9(Object) As Integer"
  IL_0107:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_010c:  dup
  IL_010d:  stsfld     "Program._ClosureCache$__10 As <generated method>"
  IL_0112:  dup
  IL_0113:  brtrue.s   IL_0136
  IL_0115:  pop
  IL_0116:  ldsfld     "Program._ClosureCache$__12 As <generated method>"
  IL_011b:  brfalse.s  IL_0124
  IL_011d:  ldsfld     "Program._ClosureCache$__12 As <generated method>"
  IL_0122:  br.s       IL_0136
  IL_0124:  ldnull
  IL_0125:  ldftn      "Function Program._Lambda$__11(Object) As Integer"
  IL_012b:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0130:  dup
  IL_0131:  stsfld     "Program._ClosureCache$__12 As <generated method>"
  IL_0136:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_013b:  pop
  IL_013c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LocalTypeInference()

            Dim compilationDef =
<compilation name="Reclassification">
    <file name="a.vb">
Module Program

    Sub Main()
        Dim y = (((Function()
                       System.Console.WriteLine("L1")
                       Return 1
                   End Function)))
        y()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="L1")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  2
  IL_0000:  ldsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Function Program._Lambda$__1(Object) As Integer"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_0020:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_0025:  pop
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub RemoveHandler1()

            Dim compilationDef =
<compilation name="Reclassification">
    <file name="a.vb">
Module Program

    Event E1()

    Sub Main()
        RemoveHandler E1, (((Sub()
                                 System.Console.WriteLine("L1")
                             End Sub)))
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42326: Lambda expression will not be removed from this event handler. Assign the lambda expression to a variable and use the variable to add and remove the event.
        RemoveHandler E1, (((Sub()
                          ~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TypeArgumentInference()

            Dim compilationDef =
<compilation name="Reclassification">
    <file name="a.vb">
Module Program

    Sub Test1(Of T, S)(x As System.Func(Of T, S))
        System.Console.WriteLine(x)
    End Sub

    Sub Test2(Of T, S)(x As System.Func(Of T, S), y As T)
        System.Console.WriteLine(x)
    End Sub

    Function Test3(Of T)(x As T) As Decimal
        Return 0
    End Function

    Sub Main()
        Test1(((Function(x As Integer) CDbl(x))))
        Test2(((AddressOf Test3)), New System.Guid())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
                                            "System.Func`2[System.Int32,System.Double]" & vbCrLf &
                                            "System.Func`2[System.Guid,System.Decimal]")
        End Sub

    End Class

End Namespace
