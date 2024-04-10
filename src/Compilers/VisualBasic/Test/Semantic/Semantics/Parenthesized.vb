' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef})

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
BC37259: Tuple must contain at least two elements.
    System.Console.WriteLine(GetType((System.String)))
                                                   ~
BC30456: 'C2' is not a member of 'Type'.
    System.Console.WriteLine(GetType((C1).C2)))
                             ~~~~~~~~~~~~~~~
BC37259: Tuple must contain at least two elements.
    System.Console.WriteLine(GetType((C1).C2)))
                                        ~
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

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
1]]>.Value.Replace(vbLf, Environment.NewLine))

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      353 (0x161)
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
  IL_005a:  ldsfld     "Program._Closure$__.$I2-0 As Program.DT1"
  IL_005f:  brfalse.s  IL_0068
  IL_0061:  ldsfld     "Program._Closure$__.$I2-0 As Program.DT1"
  IL_0066:  br.s       IL_007e
  IL_0068:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_006d:  ldftn      "Function Program._Closure$__._Lambda$__2-0() As Integer"
  IL_0073:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_0078:  dup
  IL_0079:  stsfld     "Program._Closure$__.$I2-0 As Program.DT1"
  IL_007e:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_0083:  pop
  IL_0084:  ldsfld     "Program._Closure$__.$I2-1 As Program.DT1"
  IL_0089:  brfalse.s  IL_0092
  IL_008b:  ldsfld     "Program._Closure$__.$I2-1 As Program.DT1"
  IL_0090:  br.s       IL_00a8
  IL_0092:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0097:  ldftn      "Function Program._Closure$__._Lambda$__2-1() As Integer"
  IL_009d:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_00a2:  dup
  IL_00a3:  stsfld     "Program._Closure$__.$I2-1 As Program.DT1"
  IL_00a8:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_00ad:  pop
  IL_00ae:  ldsfld     "Program._Closure$__.$I2-2 As Program.DT1"
  IL_00b3:  brfalse.s  IL_00bc
  IL_00b5:  ldsfld     "Program._Closure$__.$I2-2 As Program.DT1"
  IL_00ba:  br.s       IL_00d2
  IL_00bc:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_00c1:  ldftn      "Function Program._Closure$__._Lambda$__2-2() As Integer"
  IL_00c7:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_00cc:  dup
  IL_00cd:  stsfld     "Program._Closure$__.$I2-2 As Program.DT1"
  IL_00d2:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_00d7:  pop
  IL_00d8:  ldsfld     "Program._Closure$__.$I2-3 As Program.DT1"
  IL_00dd:  brfalse.s  IL_00e6
  IL_00df:  ldsfld     "Program._Closure$__.$I2-3 As Program.DT1"
  IL_00e4:  br.s       IL_00fc
  IL_00e6:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_00eb:  ldftn      "Function Program._Closure$__._Lambda$__2-3() As Integer"
  IL_00f1:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_00f6:  dup
  IL_00f7:  stsfld     "Program._Closure$__.$I2-3 As Program.DT1"
  IL_00fc:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_0101:  pop
  IL_0102:  ldsfld     "Program._Closure$__.$I2-4 As Program.DT1"
  IL_0107:  brfalse.s  IL_0110
  IL_0109:  ldsfld     "Program._Closure$__.$I2-4 As Program.DT1"
  IL_010e:  br.s       IL_0126
  IL_0110:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0115:  ldftn      "Function Program._Closure$__._Lambda$__2-4() As Integer"
  IL_011b:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_0120:  dup
  IL_0121:  stsfld     "Program._Closure$__.$I2-4 As Program.DT1"
  IL_0126:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_012b:  pop
  IL_012c:  ldsfld     "Program._Closure$__.$I2-5 As <generated method>"
  IL_0131:  brfalse.s  IL_013a
  IL_0133:  ldsfld     "Program._Closure$__.$I2-5 As <generated method>"
  IL_0138:  br.s       IL_0150
  IL_013a:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_013f:  ldftn      "Function Program._Closure$__._Lambda$__2-5() As Integer"
  IL_0145:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_014a:  dup
  IL_014b:  stsfld     "Program._Closure$__.$I2-5 As <generated method>"
  IL_0150:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_0155:  pop
  IL_0156:  call       "Function Program.F1() As Integer"
  IL_015b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0160:  ret
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[F1
L1
F1
L2
F1
L4
L5]]>.Value.Replace(vbLf, Environment.NewLine))

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      341 (0x155)
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
  IL_001b:  brtrue.s   IL_0042
  IL_001d:  pop
  IL_001e:  ldsfld     "Program._Closure$__.$I2-0 As Program.DT1"
  IL_0023:  brfalse.s  IL_002c
  IL_0025:  ldsfld     "Program._Closure$__.$I2-0 As Program.DT1"
  IL_002a:  br.s       IL_0042
  IL_002c:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0031:  ldftn      "Function Program._Closure$__._Lambda$__2-0() As Integer"
  IL_0037:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_003c:  dup
  IL_003d:  stsfld     "Program._Closure$__.$I2-0 As Program.DT1"
  IL_0042:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_0047:  pop
  IL_0048:  ldnull
  IL_0049:  ldftn      "Function Program.F1() As Integer"
  IL_004f:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_0054:  dup
  IL_0055:  brtrue.s   IL_0059
  IL_0057:  pop
  IL_0058:  ldloc.0
  IL_0059:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_005e:  pop
  IL_005f:  ldsfld     "Program._Closure$__.$I2-1 As Program.DT1"
  IL_0064:  brfalse.s  IL_006d
  IL_0066:  ldsfld     "Program._Closure$__.$I2-1 As Program.DT1"
  IL_006b:  br.s       IL_0083
  IL_006d:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0072:  ldftn      "Function Program._Closure$__._Lambda$__2-1() As Integer"
  IL_0078:  newobj     "Sub Program.DT1..ctor(Object, System.IntPtr)"
  IL_007d:  dup
  IL_007e:  stsfld     "Program._Closure$__.$I2-1 As Program.DT1"
  IL_0083:  dup
  IL_0084:  brtrue.s   IL_0088
  IL_0086:  pop
  IL_0087:  ldloc.0
  IL_0088:  callvirt   "Function Program.DT1.Invoke() As Integer"
  IL_008d:  pop
  IL_008e:  ldnull
  IL_008f:  ldftn      "Function Program.F1() As Integer"
  IL_0095:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_009a:  dup
  IL_009b:  brtrue.s   IL_00c2
  IL_009d:  pop
  IL_009e:  ldsfld     "Program._Closure$__.$I2-2 As <generated method>"
  IL_00a3:  brfalse.s  IL_00ac
  IL_00a5:  ldsfld     "Program._Closure$__.$I2-2 As <generated method>"
  IL_00aa:  br.s       IL_00c2
  IL_00ac:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_00b1:  ldftn      "Function Program._Closure$__._Lambda$__2-2() As Integer"
  IL_00b7:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_00bc:  dup
  IL_00bd:  stsfld     "Program._Closure$__.$I2-2 As <generated method>"
  IL_00c2:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_00c7:  pop
  IL_00c8:  ldsfld     "Program._Closure$__.$I2-3 As <generated method>"
  IL_00cd:  brfalse.s  IL_00d6
  IL_00cf:  ldsfld     "Program._Closure$__.$I2-3 As <generated method>"
  IL_00d4:  br.s       IL_00ec
  IL_00d6:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_00db:  ldftn      "Function Program._Closure$__._Lambda$__2-3() As Integer"
  IL_00e1:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_00e6:  dup
  IL_00e7:  stsfld     "Program._Closure$__.$I2-3 As <generated method>"
  IL_00ec:  dup
  IL_00ed:  brtrue.s   IL_00fc
  IL_00ef:  pop
  IL_00f0:  ldnull
  IL_00f1:  ldftn      "Function Program.F1() As Integer"
  IL_00f7:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_00fc:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_0101:  pop
  IL_0102:  ldsfld     "Program._Closure$__.$I2-4 As <generated method>"
  IL_0107:  brfalse.s  IL_0110
  IL_0109:  ldsfld     "Program._Closure$__.$I2-4 As <generated method>"
  IL_010e:  br.s       IL_0126
  IL_0110:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0115:  ldftn      "Function Program._Closure$__._Lambda$__2-4() As Integer"
  IL_011b:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0120:  dup
  IL_0121:  stsfld     "Program._Closure$__.$I2-4 As <generated method>"
  IL_0126:  dup
  IL_0127:  brtrue.s   IL_014e
  IL_0129:  pop
  IL_012a:  ldsfld     "Program._Closure$__.$I2-5 As <generated method>"
  IL_012f:  brfalse.s  IL_0138
  IL_0131:  ldsfld     "Program._Closure$__.$I2-5 As <generated method>"
  IL_0136:  br.s       IL_014e
  IL_0138:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_013d:  ldftn      "Function Program._Closure$__._Lambda$__2-5() As Integer"
  IL_0143:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0148:  dup
  IL_0149:  stsfld     "Program._Closure$__.$I2-5 As <generated method>"
  IL_014e:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_0153:  pop
  IL_0154:  ret
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="L1")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Function Program._Closure$__._Lambda$__0-0() As Integer"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_0029:  pop
  IL_002a:  ret
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
                                            "System.Func`2[System.Int32,System.Double]" & Environment.NewLine &
                                            "System.Func`2[System.Guid,System.Decimal]")
        End Sub

    End Class

End Namespace
