' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenStringConcat
        Inherits BasicTestBase

        <Fact()>
        Public Sub Concat001()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System        
Module Module1

    Sub Main()
        Dim a = "qqqq"

        Dim b = a & a & a & a

        Console.WriteLine(b)
    End Sub

End Module
]]>
    </file>
</compilation>,
expectedOutput:="qqqqqqqqqqqqqqqq").
            VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  4
  IL_0000:  ldstr      "qqqq"
  IL_0005:  dup
  IL_0006:  dup
  IL_0007:  dup
  IL_0008:  call       "Function String.Concat(String, String, String, String) As String"
  IL_000d:  call       "Sub System.Console.WriteLine(String)"
  IL_0012:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ConcatMerge()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System        
Module Module1

    Sub Main()
        Dim a = "qqqq"

        Dim b = a & "A" & "B" & a

        Console.WriteLine(b)
    End Sub

End Module
]]>
    </file>
</compilation>,
expectedOutput:="qqqqABqqqq").
            VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (String V_0) //a
  IL_0000:  ldstr      "qqqq"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      "AB"
  IL_000c:  ldloc.0
  IL_000d:  call       "Function String.Concat(String, String, String) As String"
  IL_0012:  call       "Sub System.Console.WriteLine(String)"
  IL_0017:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ConcatMergeParams()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System        
Module Module1

    Sub Main()
        Dim a = "qqqq"

        Dim b = (a & a & a & a & a &"A") & ("B" & a)

        Console.WriteLine(b)
    End Sub

End Module
]]>
    </file>
</compilation>,
expectedOutput:="qqqqqqqqqqqqqqqqqqqqABqqqq").
            VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  4
  .locals init (String V_0) //a
  IL_0000:  ldstr      "qqqq"
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.7
  IL_0007:  newarr     "String"
  IL_000c:  dup
  IL_000d:  ldc.i4.0
  IL_000e:  ldloc.0
  IL_000f:  stelem.ref
  IL_0010:  dup
  IL_0011:  ldc.i4.1
  IL_0012:  ldloc.0
  IL_0013:  stelem.ref
  IL_0014:  dup
  IL_0015:  ldc.i4.2
  IL_0016:  ldloc.0
  IL_0017:  stelem.ref
  IL_0018:  dup
  IL_0019:  ldc.i4.3
  IL_001a:  ldloc.0
  IL_001b:  stelem.ref
  IL_001c:  dup
  IL_001d:  ldc.i4.4
  IL_001e:  ldloc.0
  IL_001f:  stelem.ref
  IL_0020:  dup
  IL_0021:  ldc.i4.5
  IL_0022:  ldstr      "AB"
  IL_0027:  stelem.ref
  IL_0028:  dup
  IL_0029:  ldc.i4.6
  IL_002a:  ldloc.0
  IL_002b:  stelem.ref
  IL_002c:  call       "Function String.Concat(ParamArray String()) As String"
  IL_0031:  call       "Sub System.Console.WriteLine(String)"
  IL_0036:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ConcatWithOtherOptimizations()
            Dim result = CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System        
Module Module1

    Sub Main()
        Dim expr1 = "hi"
        Dim expr2 = "bye"

        ' expr1 is optimized away 
        ' only expr2 should be lifted!!
        Dim f As Func(Of String) = Function() If("abc" & "def" & Nothing, expr1 & "moo" & "baz") & expr2

        System.Console.WriteLine(f())
    End Sub

End Module
]]>
    </file>
</compilation>,
expectedOutput:="abcdefbye")

            ' IMPORTANT!!  only  $VB$Local_expr2  should be initialized,
            '              there should not be such thing as $VB$Local_expr1
            result.VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  newobj     "Sub Module1._Closure$__0-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldstr      "bye"
  IL_000b:  stfld      "Module1._Closure$__0-0.$VB$Local_expr2 As String"
  IL_0010:  ldftn      "Function Module1._Closure$__0-0._Lambda$__0() As String"
  IL_0016:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_001b:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0020:  call       "Sub System.Console.WriteLine(String)"
  IL_0025:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(679120, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/679120")>
        Public Sub ConcatEmptyArray()
            Dim result = CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System

Module Program
    Sub Main()
        Console.WriteLine("Start")
        Console.WriteLine(String.Concat({}))
        Console.WriteLine(String.Concat({}) + String.Concat({}))
        Console.WriteLine("A" + String.Concat({}))
        Console.WriteLine(String.Concat({}) + "B")
        Console.WriteLine("End")
    End Sub
End Module
]]>
    </file>
</compilation>,
expectedOutput:=<![CDATA[Start


A
B
End
]]>.Value.Replace(vbLf, vbCrLf))

            result.VerifyIL("Program.Main", <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  1
  IL_0000:  ldstr      "Start"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  ldc.i4.0
  IL_000b:  newarr     "String"
  IL_0010:  call       "Function String.Concat(ParamArray String()) As String"
  IL_0015:  call       "Sub System.Console.WriteLine(String)"
  IL_001a:  ldstr      ""
  IL_001f:  call       "Sub System.Console.WriteLine(String)"
  IL_0024:  ldstr      "A"
  IL_0029:  call       "Sub System.Console.WriteLine(String)"
  IL_002e:  ldstr      "B"
  IL_0033:  call       "Sub System.Console.WriteLine(String)"
  IL_0038:  ldstr      "End"
  IL_003d:  call       "Sub System.Console.WriteLine(String)"
  IL_0042:  ret
}
]]>)
        End Sub


    End Class
End Namespace

