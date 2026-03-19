' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CodeGenStaticInitializer
        Inherits BasicTestBase

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CodeGen_SimpleStaticLocal()
            'TODO: get the correct IL
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System

Module Module1
        Sub Main()
            StaticLocalInSub()
            StaticLocalInSub()
        End Sub

        Sub StaticLocalInSub()
            <Obsolete>
            Static SLItem1 = 1
            Console.WriteLine("StaticLocalInSub")
            Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
            Console.WriteLine(SLItem1.ToString) 'Value
            SLItem1 += 1
        End Sub
End Module
        ]]>
        </file>
    </compilation>).
                VerifyIL("Module1.StaticLocalInSub",
            <![CDATA[
{
  // Code size      187 (0xbb)
  .maxstack  3
  .locals init (Boolean V_0)
  IL_0000:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldsflda    "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_000c:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor()"
  IL_0011:  ldnull
  IL_0012:  call       "Function System.Threading.Interlocked.CompareExchange(Of Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag)(ByRef Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag) As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0017:  pop
  IL_0018:  ldc.i4.0
  IL_0019:  stloc.0
  .try
{
  IL_001a:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0026:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_002b:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0030:  brtrue.s   IL_004a
  IL_0032:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0037:  ldc.i4.2
  IL_0038:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_003d:  ldc.i4.1
  IL_003e:  box        "Integer"
  IL_0043:  stsfld     "Module1.SLItem1 As Object"
  IL_0048:  leave.s    IL_0078
  IL_004a:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_004f:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0054:  ldc.i4.2
  IL_0055:  bne.un.s   IL_005d
  IL_0057:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor()"
  IL_005c:  throw
  IL_005d:  leave.s    IL_0078
}
  finally
{
  IL_005f:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0064:  ldc.i4.1
  IL_0065:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_006a:  ldloc.0
  IL_006b:  brfalse.s  IL_0077
  IL_006d:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0072:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0077:  endfinally
}
  IL_0078:  ldstr      "StaticLocalInSub"
  IL_007d:  call       "Sub System.Console.WriteLine(String)"
  IL_0082:  ldsfld     "Module1.SLItem1 As Object"
  IL_0087:  callvirt   "Function Object.GetType() As System.Type"
  IL_008c:  callvirt   "Function System.Type.ToString() As String"
  IL_0091:  call       "Sub System.Console.WriteLine(String)"
  IL_0096:  ldsfld     "Module1.SLItem1 As Object"
  IL_009b:  callvirt   "Function Object.ToString() As String"
  IL_00a0:  call       "Sub System.Console.WriteLine(String)"
  IL_00a5:  ldsfld     "Module1.SLItem1 As Object"
  IL_00aa:  ldc.i4.1
  IL_00ab:  box        "Integer"
  IL_00b0:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_00b5:  stsfld     "Module1.SLItem1 As Object"
  IL_00ba:  ret
}
]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CodeGen_DoubleStaticLocalWithSameNameDifferentScopes()
            'TODO: get the correct IL

            CompileAndVerify(
    <compilation>
        <file name="a.vb">
        Imports System

        Module Module1
        Sub Main()
            Goo()
            Bar()
            Goo()
            Bar()
        End Sub

        Sub Goo()
            Static SLItem1 = 1
            Console.WriteLine("StaticLocalInSub")
            Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
            Console.WriteLine(SLItem1.ToString) 'Value
            SLItem1 += 1
        End Sub

        Sub Bar()
            Static SLItem1 = 1
            Console.WriteLine("StaticLocalInSub")
            Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
            Console.WriteLine(SLItem1.ToString) 'Value
            SLItem1 += 1
        End Sub
End Module

    </file>
    </compilation>).
                VerifyIL("Module1.Goo", <![CDATA[
{
  // Code size      187 (0xbb)
  .maxstack  3
  .locals init (Boolean V_0)
  IL_0000:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldsflda    "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_000c:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor()"
  IL_0011:  ldnull
  IL_0012:  call       "Function System.Threading.Interlocked.CompareExchange(Of Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag)(ByRef Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag) As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0017:  pop
  IL_0018:  ldc.i4.0
  IL_0019:  stloc.0
  .try
{
  IL_001a:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0026:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_002b:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0030:  brtrue.s   IL_004a
  IL_0032:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0037:  ldc.i4.2
  IL_0038:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_003d:  ldc.i4.1
  IL_003e:  box        "Integer"
  IL_0043:  stsfld     "Module1.SLItem1 As Object"
  IL_0048:  leave.s    IL_0078
  IL_004a:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_004f:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0054:  ldc.i4.2
  IL_0055:  bne.un.s   IL_005d
  IL_0057:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor()"
  IL_005c:  throw
  IL_005d:  leave.s    IL_0078
}
  finally
{
  IL_005f:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0064:  ldc.i4.1
  IL_0065:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_006a:  ldloc.0
  IL_006b:  brfalse.s  IL_0077
  IL_006d:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0072:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0077:  endfinally
}
  IL_0078:  ldstr      "StaticLocalInSub"
  IL_007d:  call       "Sub System.Console.WriteLine(String)"
  IL_0082:  ldsfld     "Module1.SLItem1 As Object"
  IL_0087:  callvirt   "Function Object.GetType() As System.Type"
  IL_008c:  callvirt   "Function System.Type.ToString() As String"
  IL_0091:  call       "Sub System.Console.WriteLine(String)"
  IL_0096:  ldsfld     "Module1.SLItem1 As Object"
  IL_009b:  callvirt   "Function Object.ToString() As String"
  IL_00a0:  call       "Sub System.Console.WriteLine(String)"
  IL_00a5:  ldsfld     "Module1.SLItem1 As Object"
  IL_00aa:  ldc.i4.1
  IL_00ab:  box        "Integer"
  IL_00b0:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_00b5:  stsfld     "Module1.SLItem1 As Object"
  IL_00ba:  ret
}
]]>)

            CompileAndVerify(
    <compilation>
        <file name="a.vb">
        Imports System

        Module Module1
        Sub Main()
            Goo()
            Bar()
            Goo()
            Bar()
        End Sub

        Sub Goo()
            Static SLItem1 = 1
            Console.WriteLine("StaticLocalInSub")
            Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
            Console.WriteLine(SLItem1.ToString) 'Value
            SLItem1 += 1
        End Sub

        Sub Bar()
            Static SLItem1 = 1
            Console.WriteLine("StaticLocalInSub")
            Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
            Console.WriteLine(SLItem1.ToString) 'Value
            SLItem1 += 1
        End Sub
End Module

    </file>
    </compilation>).
    VerifyIL("Module1.Bar", <![CDATA[
{
  // Code size      187 (0xbb)
  .maxstack  3
  .locals init (Boolean V_0)
  IL_0000:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldsflda    "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_000c:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor()"
  IL_0011:  ldnull
  IL_0012:  call       "Function System.Threading.Interlocked.CompareExchange(Of Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag)(ByRef Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag) As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0017:  pop
  IL_0018:  ldc.i4.0
  IL_0019:  stloc.0
  .try
{
  IL_001a:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0026:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_002b:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0030:  brtrue.s   IL_004a
  IL_0032:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0037:  ldc.i4.2
  IL_0038:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_003d:  ldc.i4.1
  IL_003e:  box        "Integer"
  IL_0043:  stsfld     "Module1.SLItem1 As Object"
  IL_0048:  leave.s    IL_0078
  IL_004a:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_004f:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0054:  ldc.i4.2
  IL_0055:  bne.un.s   IL_005d
  IL_0057:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor()"
  IL_005c:  throw
  IL_005d:  leave.s    IL_0078
}
  finally
{
  IL_005f:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0064:  ldc.i4.1
  IL_0065:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_006a:  ldloc.0
  IL_006b:  brfalse.s  IL_0077
  IL_006d:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0072:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0077:  endfinally
}
  IL_0078:  ldstr      "StaticLocalInSub"
  IL_007d:  call       "Sub System.Console.WriteLine(String)"
  IL_0082:  ldsfld     "Module1.SLItem1 As Object"
  IL_0087:  callvirt   "Function Object.GetType() As System.Type"
  IL_008c:  callvirt   "Function System.Type.ToString() As String"
  IL_0091:  call       "Sub System.Console.WriteLine(String)"
  IL_0096:  ldsfld     "Module1.SLItem1 As Object"
  IL_009b:  callvirt   "Function Object.ToString() As String"
  IL_00a0:  call       "Sub System.Console.WriteLine(String)"
  IL_00a5:  ldsfld     "Module1.SLItem1 As Object"
  IL_00aa:  ldc.i4.1
  IL_00ab:  box        "Integer"
  IL_00b0:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_00b5:  stsfld     "Module1.SLItem1 As Object"
  IL_00ba:  ret
}
]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CodeGen_DoubleStaticLocalWithSameNameDifferentOverloads()
            'TODO: get the correct IL

            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System

        Module Module1
        Sub Main()
            Goo()
            Goo(1)
            Goo()
            Goo(2)
        End Sub

        Sub Goo()
            Static SLItem1 = 1
            Console.WriteLine("StaticLocalInSub")
            Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
            Console.WriteLine(SLItem1.ToString) 'Value
            SLItem1 += 1
        End Sub

        Sub goo(x as Integer)
            Static SLItem1 = 1
            Console.WriteLine("StaticLocalInSub")
            Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
            Console.WriteLine(SLItem1.ToString) 'Value
            SLItem1 += 1
        End Sub
End Module
    </file>
    </compilation>).
                VerifyIL("Module1.Goo", <![CDATA[
{
  // Code size      187 (0xbb)
  .maxstack  3
  .locals init (Boolean V_0)
  IL_0000:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldsflda    "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_000c:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor()"
  IL_0011:  ldnull
  IL_0012:  call       "Function System.Threading.Interlocked.CompareExchange(Of Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag)(ByRef Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag) As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0017:  pop
  IL_0018:  ldc.i4.0
  IL_0019:  stloc.0
  .try
{
  IL_001a:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0026:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_002b:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0030:  brtrue.s   IL_004a
  IL_0032:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0037:  ldc.i4.2
  IL_0038:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_003d:  ldc.i4.1
  IL_003e:  box        "Integer"
  IL_0043:  stsfld     "Module1.SLItem1 As Object"
  IL_0048:  leave.s    IL_0078
  IL_004a:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_004f:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0054:  ldc.i4.2
  IL_0055:  bne.un.s   IL_005d
  IL_0057:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor()"
  IL_005c:  throw
  IL_005d:  leave.s    IL_0078
}
  finally
{
  IL_005f:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0064:  ldc.i4.1
  IL_0065:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_006a:  ldloc.0
  IL_006b:  brfalse.s  IL_0077
  IL_006d:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0072:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0077:  endfinally
}
  IL_0078:  ldstr      "StaticLocalInSub"
  IL_007d:  call       "Sub System.Console.WriteLine(String)"
  IL_0082:  ldsfld     "Module1.SLItem1 As Object"
  IL_0087:  callvirt   "Function Object.GetType() As System.Type"
  IL_008c:  callvirt   "Function System.Type.ToString() As String"
  IL_0091:  call       "Sub System.Console.WriteLine(String)"
  IL_0096:  ldsfld     "Module1.SLItem1 As Object"
  IL_009b:  callvirt   "Function Object.ToString() As String"
  IL_00a0:  call       "Sub System.Console.WriteLine(String)"
  IL_00a5:  ldsfld     "Module1.SLItem1 As Object"
  IL_00aa:  ldc.i4.1
  IL_00ab:  box        "Integer"
  IL_00b0:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_00b5:  stsfld     "Module1.SLItem1 As Object"
  IL_00ba:  ret
}
]]>)

            CompileAndVerify(
    <compilation>
        <file name="a.vb">
        Imports System

        Module Module1
        Sub Main()
            Goo()
            Goo(1)
            Goo()
            Goo(2)
        End Sub

        Sub goo(x as Integer)
            Static SLItem1 = 1
            Console.WriteLine("StaticLocalInSub")
            Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
            Console.WriteLine(SLItem1.ToString) 'Value
            SLItem1 += 1
        End Sub

        Sub Goo()
            Static SLItem1 = 1
            Console.WriteLine("StaticLocalInSub")
            Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
            Console.WriteLine(SLItem1.ToString) 'Value
            SLItem1 += 1
        End Sub
End Module
    </file>
    </compilation>).
    VerifyIL("Module1.goo", <![CDATA[
{
  // Code size      187 (0xbb)
  .maxstack  3
  .locals init (Boolean V_0)
  IL_0000:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldsflda    "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_000c:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag..ctor()"
  IL_0011:  ldnull
  IL_0012:  call       "Function System.Threading.Interlocked.CompareExchange(Of Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag)(ByRef Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag, Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag) As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0017:  pop
  IL_0018:  ldc.i4.0
  IL_0019:  stloc.0
  .try
{
  IL_001a:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
  IL_0026:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_002b:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0030:  brtrue.s   IL_004a
  IL_0032:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0037:  ldc.i4.2
  IL_0038:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_003d:  ldc.i4.1
  IL_003e:  box        "Integer"
  IL_0043:  stsfld     "Module1.SLItem1 As Object"
  IL_0048:  leave.s    IL_0078
  IL_004a:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_004f:  ldfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_0054:  ldc.i4.2
  IL_0055:  bne.un.s   IL_005d
  IL_0057:  newobj     "Sub Microsoft.VisualBasic.CompilerServices.IncompleteInitialization..ctor()"
  IL_005c:  throw
  IL_005d:  leave.s    IL_0078
}
  finally
{
  IL_005f:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0064:  ldc.i4.1
  IL_0065:  stfld      "Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag.State As Short"
  IL_006a:  ldloc.0
  IL_006b:  brfalse.s  IL_0077
  IL_006d:  ldsfld     "Module1.SLItem1$Init As Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"
  IL_0072:  call       "Sub System.Threading.Monitor.Exit(Object)"
  IL_0077:  endfinally
}
  IL_0078:  ldstr      "StaticLocalInSub"
  IL_007d:  call       "Sub System.Console.WriteLine(String)"
  IL_0082:  ldsfld     "Module1.SLItem1 As Object"
  IL_0087:  callvirt   "Function Object.GetType() As System.Type"
  IL_008c:  callvirt   "Function System.Type.ToString() As String"
  IL_0091:  call       "Sub System.Console.WriteLine(String)"
  IL_0096:  ldsfld     "Module1.SLItem1 As Object"
  IL_009b:  callvirt   "Function Object.ToString() As String"
  IL_00a0:  call       "Sub System.Console.WriteLine(String)"
  IL_00a5:  ldsfld     "Module1.SLItem1 As Object"
  IL_00aa:  ldc.i4.1
  IL_00ab:  box        "Integer"
  IL_00b0:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_00b5:  stsfld     "Module1.SLItem1 As Object"
  IL_00ba:  ret
}
]]>)
        End Sub
    End Class

End Namespace
