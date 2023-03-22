' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenForeach
        Inherits BasicTestBase

        ' The loop object must be an array or an object collection
        <Fact>
        Public Sub SimpleForeachTest()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
        Dim arr As String() = New String(1) {}
        arr(0) = "one"
        arr(1) = "two"
        For Each s As String In arr
            Console.WriteLine(s)
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
one
two
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  4
  .locals init (String() V_0,
  Integer V_1)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     "String"
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      "one"
  IL_000d:  stelem.ref
  IL_000e:  dup
  IL_000f:  ldc.i4.1
  IL_0010:  ldstr      "two"
  IL_0015:  stelem.ref
  IL_0016:  stloc.0
  IL_0017:  ldc.i4.0
  IL_0018:  stloc.1
  IL_0019:  br.s       IL_0027
  IL_001b:  ldloc.0
  IL_001c:  ldloc.1
  IL_001d:  ldelem.ref
  IL_001e:  call       "Sub System.Console.WriteLine(String)"
  IL_0023:  ldloc.1
  IL_0024:  ldc.i4.1
  IL_0025:  add.ovf
  IL_0026:  stloc.1
  IL_0027:  ldloc.1
  IL_0028:  ldloc.0
  IL_0029:  ldlen
  IL_002a:  conv.i4
  IL_002b:  blt.s      IL_001b
  IL_002d:  ret
}
]]>).Compilation

        End Sub

        ' Type is not required in a foreach statement
        <Fact>
        Public Sub TypeIsNotRequiredTest()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On        
Imports System
Class C
    Shared Sub Main()
        Dim myarray As Integer() = New Integer(2) {1, 2, 3}
        For Each item In myarray
        Next
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithModuleName("MODULE")).VerifyIL("C.Main", <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (Integer() V_0,
                Integer V_1)
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     "Integer"
  IL_0006:  dup
  IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D"
  IL_000c:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0011:  stloc.0
  IL_0012:  ldc.i4.0
  IL_0013:  stloc.1
  IL_0014:  br.s       IL_001e
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldelem.i4
  IL_0019:  pop
  IL_001a:  ldloc.1
  IL_001b:  ldc.i4.1
  IL_001c:  add.ovf
  IL_001d:  stloc.1
  IL_001e:  ldloc.1
  IL_001f:  ldloc.0
  IL_0020:  ldlen
  IL_0021:  conv.i4
  IL_0022:  blt.s      IL_0016
  IL_0024:  ret
}
]]>)
        End Sub

        ' Narrowing conversions from the elements in group to element are evaluated and performed at run time
        <Fact>
        Public Sub NarrowConversions()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Imports System
Class C
    Shared Sub Main()
        For Each number As Integer In New Long() {45, 3}
            Console.WriteLine(number)
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
45
3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  4
  .locals init (Long() V_0,
  Integer V_1)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     "Long"
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   45
  IL_000a:  conv.i8
  IL_000b:  stelem.i8
  IL_000c:  dup
  IL_000d:  ldc.i4.1
  IL_000e:  ldc.i4.3
  IL_000f:  conv.i8
  IL_0010:  stelem.i8
  IL_0011:  stloc.0
  IL_0012:  ldc.i4.0
  IL_0013:  stloc.1
  IL_0014:  br.s       IL_0023
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldelem.i8
  IL_0019:  conv.ovf.i4
  IL_001a:  call       "Sub System.Console.WriteLine(Integer)"
  IL_001f:  ldloc.1
  IL_0020:  ldc.i4.1
  IL_0021:  add.ovf
  IL_0022:  stloc.1
  IL_0023:  ldloc.1
  IL_0024:  ldloc.0
  IL_0025:  ldlen
  IL_0026:  conv.i4
  IL_0027:  blt.s      IL_0016
  IL_0029:  ret
}
]]>)
        End Sub

        ' Narrowing conversions from the elements in group to element are evaluated and performed at run time
        <Fact>
        Public Sub NarrowConversions_2()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Imports System
Class C
    Shared Sub Main()
        For Each number As Integer In New Long() {9876543210}
            Console.WriteLine(number)
        Next
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("C.Main", <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  4
  .locals init (Long() V_0,
  Integer V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     "Long"
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i8     0x24cb016ea
  IL_0011:  stelem.i8
  IL_0012:  stloc.0
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.1
  IL_0015:  br.s       IL_0024
  IL_0017:  ldloc.0
  IL_0018:  ldloc.1
  IL_0019:  ldelem.i8
  IL_001a:  conv.ovf.i4
  IL_001b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0020:  ldloc.1
  IL_0021:  ldc.i4.1
  IL_0022:  add.ovf
  IL_0023:  stloc.1
  IL_0024:  ldloc.1
  IL_0025:  ldloc.0
  IL_0026:  ldlen
  IL_0027:  conv.i4
  IL_0028:  blt.s      IL_0017
  IL_002a:  ret
}
]]>)
        End Sub

        ' Multiline
        <Fact>
        Public Sub Multiline()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Imports System
Class C
    Shared Public Sub Main()
        Dim a() As Integer = New Integer() {7}
        For Each x As Integer In a : System.Console.WriteLine(x) : Next
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("C.Main", <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  4
  .locals init (Integer() V_0,
  Integer V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     "Integer"
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.7
  IL_0009:  stelem.i4
  IL_000a:  stloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  stloc.1
  IL_000d:  br.s       IL_001b
  IL_000f:  ldloc.0
  IL_0010:  ldloc.1
  IL_0011:  ldelem.i4
  IL_0012:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0017:  ldloc.1
  IL_0018:  ldc.i4.1
  IL_0019:  add.ovf
  IL_001a:  stloc.1
  IL_001b:  ldloc.1
  IL_001c:  ldloc.0
  IL_001d:  ldlen
  IL_001e:  conv.i4
  IL_001f:  blt.s      IL_000f
  IL_0021:  ret
}
]]>)
        End Sub

        ' Line continuations
        <Fact>
        Public Sub LineContinuations()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Imports System
Class C
    Public Shared Sub Main()
        Dim a() As Integer = New Integer() {7}
For _
Each _
x _
As _
Integer _
In _
a _
 _
: Next
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("C.Main", <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  4
  .locals init (Integer() V_0,
  Integer V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     "Integer"
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.7
  IL_0009:  stelem.i4
  IL_000a:  stloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  stloc.1
  IL_000d:  br.s       IL_0017
  IL_000f:  ldloc.0
  IL_0010:  ldloc.1
  IL_0011:  ldelem.i4
  IL_0012:  pop
  IL_0013:  ldloc.1
  IL_0014:  ldc.i4.1
  IL_0015:  add.ovf
  IL_0016:  stloc.1
  IL_0017:  ldloc.1
  IL_0018:  ldloc.0
  IL_0019:  ldlen
  IL_001a:  conv.i4
  IL_001b:  blt.s      IL_000f
  IL_001d:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(9151, "DevDiv_Projects/Roslyn"), WorkItem(546096, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546096")>
        Public Sub IterationVarInConditionalExpression()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Class C
    Shared Sub Main()
        For Each x As S In If(True, x, 1)
        Next
    End Sub
End Class
Public Structure S
End Structure
    </file>
</compilation>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       77 (0x4d)
  .maxstack  2
  .locals init (S V_0,
  System.Collections.IEnumerator V_1,
  S V_2)
  .try
{
  IL_0000:  ldloc.0
  IL_0001:  box        "S"
  IL_0006:  castclass  "System.Collections.IEnumerable"
  IL_000b:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_0010:  stloc.1
  IL_0011:  br.s       IL_002e
  IL_0013:  ldloc.1
  IL_0014:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_0019:  dup
  IL_001a:  brtrue.s   IL_0028
  IL_001c:  pop
  IL_001d:  ldloca.s   V_2
  IL_001f:  initobj    "S"
  IL_0025:  ldloc.2
  IL_0026:  br.s       IL_002d
  IL_0028:  unbox.any  "S"
  IL_002d:  pop
  IL_002e:  ldloc.1
  IL_002f:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_0034:  brtrue.s   IL_0013
  IL_0036:  leave.s    IL_004c
}
  finally
{
  IL_0038:  ldloc.1
  IL_0039:  isinst     "System.IDisposable"
  IL_003e:  brfalse.s  IL_004b
  IL_0040:  ldloc.1
  IL_0041:  isinst     "System.IDisposable"
  IL_0046:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_004b:  endfinally
}
  IL_004c:  ret
}
]]>)
        End Sub

        ' Use the declared variable to initialize collection
        <Fact>
        Public Sub IterationVarInCollectionExpression_1()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        For Each x As Integer In New Integer() {x + 5, x + 6, x + 7}
            System.Console.WriteLine(x)
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
5
6
7
]]>)
        End Sub

        <Fact(), WorkItem(9151, "DevDiv_Projects/Roslyn"), WorkItem(546096, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546096")>
        Public Sub TraversingNothing()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Class C
    Shared Sub Main()
        For Each item In Nothing
        Next
    End Sub
End Class
    </file>
</compilation>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0)
  .try
{
  IL_0000:  ldnull
  IL_0001:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_0015
  IL_0009:  ldloc.0
  IL_000a:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_000f:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0014:  pop
  IL_0015:  ldloc.0
  IL_0016:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_001b:  brtrue.s   IL_0009
  IL_001d:  leave.s    IL_0033
}
  finally
{
  IL_001f:  ldloc.0
  IL_0020:  isinst     "System.IDisposable"
  IL_0025:  brfalse.s  IL_0032
  IL_0027:  ldloc.0
  IL_0028:  isinst     "System.IDisposable"
  IL_002d:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0032:  endfinally
}
  IL_0033:  ret
}
]]>)
        End Sub

        ' Nested ForEach can use a var declared in the outer ForEach
        <Fact>
        Public Sub NestedForeach()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim c(3)() As Integer
        For Each x As Integer() In c
            ReDim x(3)
            For i As Integer = 0 To 3
                x(i) = i
            Next
            For Each y As Integer In x
                System .Console .WriteLine (y)
            Next
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
0
1
2
3
0
1
2
3
0
1
2
3
0
1
2
3
]]>)
        End Sub

        ' Inner foreach loop referencing the outer foreach loop iteration variable
        <Fact>
        Public Sub NestedForeach_1()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S
            For Each y As Char In x
                System.Console.WriteLine(y)
            Next
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
A
B
C
X
Y
Z
]]>)
        End Sub

        ' Foreach value can't be modified in a loop
        <Fact>
        Public Sub ModifyIterationValueInLoop()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Collections

Class C
    Shared Sub Main()
        Dim list As New ArrayList()
        list.Add("One")
        list.Add("Two")
        For Each s As String In list
            s = "a"
        Next
        For Each s As String In list
            System.Console.WriteLine(s)
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
One
Two
]]>)
        End Sub

        ' Pass fields as a ref argument for 'foreach iteration variable'
        <Fact()>
        Public Sub PassFieldsAsRefArgument()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim sa As S() = New S(2) {New S With {.I = 1}, New S With {.I = 2}, New S With {.I = 3}}
        For Each s As S In sa
            f(s.i)
        Next
        For Each s As S In sa
            System.Console.WriteLine(s.i)
        Next
    End Sub
    Private Shared Sub f(ByRef iref As Integer)
        iref = 1
    End Sub
End Class
Structure S
    Public I As integer
End Structure
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
]]>)
        End Sub

        ' With multidimensional arrays, you can use one loop to iterate through the elements
        <Fact>
        Public Sub TraversingMultidimensionalArray()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim numbers2D(,) As Integer = New Integer (2,1) {}
        numbers2D(0,0) = 9
        numbers2D(0,1) = 99
        numbers2D(1,0) = 3
        numbers2D(1,1) = 33
        numbers2D(2,0) = 5
        numbers2D(2,1) = 55

        For Each i As Integer In numbers2D
            System.Console.WriteLine(i)
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
9
99
3
33
5
55
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       98 (0x62)
  .maxstack  5
  .locals init (System.Collections.IEnumerator V_0)
  IL_0000:  ldc.i4.3
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.s   9
  IL_000c:  call       "Integer(*,*).Set"
  IL_0011:  dup
  IL_0012:  ldc.i4.0
  IL_0013:  ldc.i4.1
  IL_0014:  ldc.i4.s   99
  IL_0016:  call       "Integer(*,*).Set"
  IL_001b:  dup
  IL_001c:  ldc.i4.1
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.3
  IL_001f:  call       "Integer(*,*).Set"
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.1
  IL_0027:  ldc.i4.s   33
  IL_0029:  call       "Integer(*,*).Set"
  IL_002e:  dup
  IL_002f:  ldc.i4.2
  IL_0030:  ldc.i4.0
  IL_0031:  ldc.i4.5
  IL_0032:  call       "Integer(*,*).Set"
  IL_0037:  dup
  IL_0038:  ldc.i4.2
  IL_0039:  ldc.i4.1
  IL_003a:  ldc.i4.s   55
  IL_003c:  call       "Integer(*,*).Set"
  IL_0041:  callvirt   "Function System.Array.GetEnumerator() As System.Collections.IEnumerator"
  IL_0046:  stloc.0
  IL_0047:  br.s       IL_0059
  IL_0049:  ldloc.0
  IL_004a:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_004f:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_0054:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0059:  ldloc.0
  IL_005a:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_005f:  brtrue.s   IL_0049
  IL_0061:  ret
}
]]>)

        End Sub

        ' Traversing jagged arrays
        <Fact>
        Public Sub TraversingJaggedArray()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim numbers2D As Integer()() = New Integer()() {New Integer() {1, 2}, New Integer() {4, 5, 6}}
        For Each x As Integer() In numbers2D
            For Each y As Integer In x
                System.Console.WriteLine(y)
            Next
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
4
5
6
]]>)
        End Sub

        ' Optimization to foreach (char c in String) by treating String as a char array
        <Fact()>
        Public Sub TraversingString()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On        
Class C
    Public Shared Sub Main()
        Dim str As String = "ABC"
        For Each x In str
            System.Console.WriteLine(x)
        Next
        For Each var In "goo"
            If Not var.[GetType]().Equals(GetType(Char)) Then
                System.Console.WriteLine("False")
            End If
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
A
B
C
]]>)
        End Sub

        ' Traversing items in Dictionary
        <Fact>
        Public Sub TraversingDictionary()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">      
Option Infer On

Imports System.Collections.Generic

Class C
    Public Shared Sub Main()
        Dim s As New Dictionary(Of Integer, Integer)()
        s.Add(1, 2)
        s.Add(2, 3)
        s.Add(3, 4)
        For Each pair In s
            System .Console .WriteLine (pair.Key)
        Next
        For Each pair As KeyValuePair(Of Integer, Integer) In s
            System .Console .WriteLine (pair.Value )
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
2
3
4
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size      141 (0x8d)
  .maxstack  3
  .locals init (System.Collections.Generic.Dictionary(Of Integer, Integer) V_0, //s
  System.Collections.Generic.Dictionary(Of Integer, Integer).Enumerator V_1,
  System.Collections.Generic.KeyValuePair(Of Integer, Integer) V_2, //pair
  System.Collections.Generic.Dictionary(Of Integer, Integer).Enumerator V_3,
  System.Collections.Generic.KeyValuePair(Of Integer, Integer) V_4) //pair
  IL_0000:  newobj     "Sub System.Collections.Generic.Dictionary(Of Integer, Integer)..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ldc.i4.2
  IL_0009:  callvirt   "Sub System.Collections.Generic.Dictionary(Of Integer, Integer).Add(Integer, Integer)"
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.2
  IL_0010:  ldc.i4.3
  IL_0011:  callvirt   "Sub System.Collections.Generic.Dictionary(Of Integer, Integer).Add(Integer, Integer)"
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.3
  IL_0018:  ldc.i4.4
  IL_0019:  callvirt   "Sub System.Collections.Generic.Dictionary(Of Integer, Integer).Add(Integer, Integer)"
  .try
{
  IL_001e:  ldloc.0
  IL_001f:  callvirt   "Function System.Collections.Generic.Dictionary(Of Integer, Integer).GetEnumerator() As System.Collections.Generic.Dictionary(Of Integer, Integer).Enumerator"
  IL_0024:  stloc.1
  IL_0025:  br.s       IL_003b
  IL_0027:  ldloca.s   V_1
  IL_0029:  call       "Function System.Collections.Generic.Dictionary(Of Integer, Integer).Enumerator.get_Current() As System.Collections.Generic.KeyValuePair(Of Integer, Integer)"
  IL_002e:  stloc.2
  IL_002f:  ldloca.s   V_2
  IL_0031:  call       "Function System.Collections.Generic.KeyValuePair(Of Integer, Integer).get_Key() As Integer"
  IL_0036:  call       "Sub System.Console.WriteLine(Integer)"
  IL_003b:  ldloca.s   V_1
  IL_003d:  call       "Function System.Collections.Generic.Dictionary(Of Integer, Integer).Enumerator.MoveNext() As Boolean"
  IL_0042:  brtrue.s   IL_0027
  IL_0044:  leave.s    IL_0054
}
  finally
{
  IL_0046:  ldloca.s   V_1
  IL_0048:  constrained. "System.Collections.Generic.Dictionary(Of Integer, Integer).Enumerator"
  IL_004e:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0053:  endfinally
}
  IL_0054:  nop
  .try
{
  IL_0055:  ldloc.0
  IL_0056:  callvirt   "Function System.Collections.Generic.Dictionary(Of Integer, Integer).GetEnumerator() As System.Collections.Generic.Dictionary(Of Integer, Integer).Enumerator"
  IL_005b:  stloc.3
  IL_005c:  br.s       IL_0073
  IL_005e:  ldloca.s   V_3
  IL_0060:  call       "Function System.Collections.Generic.Dictionary(Of Integer, Integer).Enumerator.get_Current() As System.Collections.Generic.KeyValuePair(Of Integer, Integer)"
  IL_0065:  stloc.s    V_4
  IL_0067:  ldloca.s   V_4
  IL_0069:  call       "Function System.Collections.Generic.KeyValuePair(Of Integer, Integer).get_Value() As Integer"
  IL_006e:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0073:  ldloca.s   V_3
  IL_0075:  call       "Function System.Collections.Generic.Dictionary(Of Integer, Integer).Enumerator.MoveNext() As Boolean"
  IL_007a:  brtrue.s   IL_005e
  IL_007c:  leave.s    IL_008c
}
  finally
{
  IL_007e:  ldloca.s   V_3
  IL_0080:  constrained. "System.Collections.Generic.Dictionary(Of Integer, Integer).Enumerator"
  IL_0086:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_008b:  endfinally
}
  IL_008c:  ret
}
]]>)
        End Sub

        ' Breaking from nested Loops
        <Fact>
        Public Sub BreakFromForeach()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S
            For Each y As Char In x
                If y = "B"c Then
                    Exit For
                Else
                    System.Console.WriteLine(y)
                End If
            Next
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
A
X
Y
Z
]]>)
        End Sub

        ' Continuing for nested Loops
        <Fact>
        Public Sub ContinueInForeach()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S
            For Each y As Char In x
                If y = "B"c Then
                    Continue For
                End If
                System.Console.WriteLine(y)
        Next y, x
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
A
C
X
Y
Z
]]>)
        End Sub

        ' Query expression works in foreach
        <Fact()>
        Public Sub QueryExpressionInForeach()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq

Class C
    Public Shared Sub Main()
        For Each x In From w In New Integer() {1, 2, 3} Select z = w.ToString()
            System.Console.WriteLine(x.ToLower())
        Next
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithModuleName("MODULE"), references:={LinqAssemblyRef}, expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size      103 (0x67)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerator(Of String) V_0)
  .try
  {
    IL_0000:  ldc.i4.3
    IL_0001:  newarr     "Integer"
    IL_0006:  dup
    IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D"
    IL_000c:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
    IL_0011:  ldsfld     "C._Closure$__.$I1-0 As System.Func(Of Integer, String)"
    IL_0016:  brfalse.s  IL_001f
    IL_0018:  ldsfld     "C._Closure$__.$I1-0 As System.Func(Of Integer, String)"
    IL_001d:  br.s       IL_0035
    IL_001f:  ldsfld     "C._Closure$__.$I As C._Closure$__"
    IL_0024:  ldftn      "Function C._Closure$__._Lambda$__1-0(Integer) As String"
    IL_002a:  newobj     "Sub System.Func(Of Integer, String)..ctor(Object, System.IntPtr)"
    IL_002f:  dup
    IL_0030:  stsfld     "C._Closure$__.$I1-0 As System.Func(Of Integer, String)"
    IL_0035:  call       "Function System.Linq.Enumerable.Select(Of Integer, String)(System.Collections.Generic.IEnumerable(Of Integer), System.Func(Of Integer, String)) As System.Collections.Generic.IEnumerable(Of String)"
    IL_003a:  callvirt   "Function System.Collections.Generic.IEnumerable(Of String).GetEnumerator() As System.Collections.Generic.IEnumerator(Of String)"
    IL_003f:  stloc.0
    IL_0040:  br.s       IL_0052
    IL_0042:  ldloc.0
    IL_0043:  callvirt   "Function System.Collections.Generic.IEnumerator(Of String).get_Current() As String"
    IL_0048:  callvirt   "Function String.ToLower() As String"
    IL_004d:  call       "Sub System.Console.WriteLine(String)"
    IL_0052:  ldloc.0
    IL_0053:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
    IL_0058:  brtrue.s   IL_0042
    IL_005a:  leave.s    IL_0066
  }
  finally
  {
    IL_005c:  ldloc.0
    IL_005d:  brfalse.s  IL_0065
    IL_005f:  ldloc.0
    IL_0060:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_0065:  endfinally
  }
  IL_0066:  ret
}
]]>)
        End Sub

        ' No confusion in a foreach statement when from is a value type
        <Fact>
        Public Sub ReDimFrom()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On
Class C
    Public Shared Sub Main()
        Dim src = New System.Collections.ArrayList()
        src.Add(new from(1))
        For Each x As from In src
            System.Console.WriteLine(x.X)
        Next
    End Sub
End Class
Public Structure from
    Dim X As Integer
    Public Sub New(p as integer)
        x = p
    End Sub
End Structure
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
]]>)
        End Sub

        ' Foreach on generic type that implements the Collection Pattern
        <Fact>
        Public Sub GenericTypeImplementsCollection()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On

Imports System.Collections.Generic

Class C
    Public Shared Sub Main()
        For Each j In New Gen(Of Integer)()
        Next
    End Sub
End Class

Public Class Gen(Of T As New)
    Public Function GetEnumerator() As IEnumerator(Of T)
        Return Nothing
    End Function
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("C.Main", <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  1
  .locals init (System.Collections.Generic.IEnumerator(Of Integer) V_0)
  .try
{
  IL_0000:  newobj     "Sub Gen(Of Integer)..ctor()"
  IL_0005:  call       "Function Gen(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0014
  IL_000d:  ldloc.0
  IL_000e:  callvirt   "Function System.Collections.Generic.IEnumerator(Of Integer).get_Current() As Integer"
  IL_0013:  pop
  IL_0014:  ldloc.0
  IL_0015:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_001a:  brtrue.s   IL_000d
  IL_001c:  leave.s    IL_0028
}
  finally
{
  IL_001e:  ldloc.0
  IL_001f:  brfalse.s  IL_0027
  IL_0021:  ldloc.0
  IL_0022:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0027:  endfinally
}
  IL_0028:  ret
}
]]>)
        End Sub

        ' Customer defined type of collection to support 'foreach'
        <Fact>
        Public Sub CustomerDefinedCollections()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class C
    Public Shared Sub Main()
        Dim col As New MyCollection()
        For Each i As Integer In col
            Console.WriteLine(i)
        Next
    End Sub
End Class
Public Class MyCollection
    Private items As Integer()
    Public Sub New()
        items = New Integer(4) {1, 4, 3, 2, 5}
    End Sub
    Public Function GetEnumerator() As MyEnumerator
        Return New MyEnumerator(Me)
    End Function
    Public Class MyEnumerator
        Private nIndex As Integer
        Private collection As MyCollection
        Public Sub New(coll As MyCollection)
            collection = coll
            nIndex = nIndex - 1
        End Sub
        Public Function MoveNext() As Boolean
            nIndex = nIndex + 1
            Return (nIndex &lt; collection.items.GetLength(0))
        End Function
        Public ReadOnly Property Current() As Integer
            Get
                Return (collection.items(nIndex))
            End Get
        End Property
    End Class
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
4
3
2
5
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (MyCollection.MyEnumerator V_0)
  IL_0000:  newobj     "Sub MyCollection..ctor()"
  IL_0005:  callvirt   "Function MyCollection.GetEnumerator() As MyCollection.MyEnumerator"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0018
  IL_000d:  ldloc.0
  IL_000e:  callvirt   "Function MyCollection.MyEnumerator.get_Current() As Integer"
  IL_0013:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0018:  ldloc.0
  IL_0019:  callvirt   "Function MyCollection.MyEnumerator.MoveNext() As Boolean"
  IL_001e:  brtrue.s   IL_000d
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestForEachPattern()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On
Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Class

Class Enumerator
    Private x As Integer = 0
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     "Sub Enumerable..ctor()"
  IL_0005:  call       "Function Enumerable.GetEnumerator() As Enumerator"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0018
  IL_000d:  ldloc.0
  IL_000e:  callvirt   "Function Enumerator.get_Current() As Integer"
  IL_0013:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0018:  ldloc.0
  IL_0019:  callvirt   "Function Enumerator.MoveNext() As Boolean"
  IL_001e:  brtrue.s   IL_000d
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestForEachInterface()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Implements System.Collections.IEnumerable
    ' Explicit implementation won't match pattern.
    Private Function System_Collections_IEnumerable_GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Dim list As New System.Collections.Generic.List(Of Integer)()
        list.Add(3)
        list.Add(2)
        list.Add(1)
        Return list.GetEnumerator()
    End Function
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
3
2
1
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       65 (0x41)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0)
  .try
{
  IL_0000:  newobj     "Sub Enumerable..ctor()"
  IL_0005:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0022
  IL_000d:  ldloc.0
  IL_000e:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_0013:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0018:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_001d:  call       "Sub System.Console.WriteLine(Object)"
  IL_0022:  ldloc.0
  IL_0023:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_0028:  brtrue.s   IL_000d
  IL_002a:  leave.s    IL_0040
}
  finally
{
  IL_002c:  ldloc.0
  IL_002d:  isinst     "System.IDisposable"
  IL_0032:  brfalse.s  IL_003f
  IL_0034:  ldloc.0
  IL_0035:  isinst     "System.IDisposable"
  IL_003a:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_003f:  endfinally
}
  IL_0040:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestForEachExplicitlyDisposableStruct()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On 

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Class

Structure Enumerator
    Implements System.IDisposable
    Private x As Integer
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
    Private Sub System_IDisposable_Dispose() Implements System.IDisposable.Dispose
    End Sub
End Structure
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (Enumerator V_0)
  .try
{
  IL_0000:  newobj     "Sub Enumerable..ctor()"
  IL_0005:  call       "Function Enumerable.GetEnumerator() As Enumerator"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0019
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Function Enumerator.get_Current() As Integer"
  IL_0014:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       "Function Enumerator.MoveNext() As Boolean"
  IL_0020:  brtrue.s   IL_000d
  IL_0022:  leave.s    IL_0032
}
  finally
{
  IL_0024:  ldloca.s   V_0
  IL_0026:  constrained. "Enumerator"
  IL_002c:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0031:  endfinally
}
  IL_0032:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestForEachDisposeStruct()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Class
Structure Enumerator
    Private x As Integer
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
    Public Sub Dispose()
    End Sub
End Structure
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     "Sub Enumerable..ctor()"
  IL_0005:  call       "Function Enumerable.GetEnumerator() As Enumerator"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0019
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Function Enumerator.get_Current() As Integer"
  IL_0014:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       "Function Enumerator.MoveNext() As Boolean"
  IL_0020:  brtrue.s   IL_000d
  IL_0022:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestForEachNonDisposableStruct()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Class

Structure Enumerator
    Private x As Integer
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
End Structure
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     "Sub Enumerable..ctor()"
  IL_0005:  call       "Function Enumerable.GetEnumerator() As Enumerator"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0019
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Function Enumerator.get_Current() As Integer"
  IL_0014:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       "Function Enumerator.MoveNext() As Boolean"
  IL_0020:  brtrue.s   IL_000d
  IL_0022:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestForEachExplicitlyGetEnumeratorStruct()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On

Imports System.Collections

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Structure Enumerable
    Implements IEnumerable
    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return New Integer() {1, 2, 3}.GetEnumerator()
    End Function
End Structure
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       79 (0x4f)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0,
  Enumerable V_1)
  .try
{
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    "Enumerable"
  IL_0008:  ldloc.1
  IL_0009:  box        "Enumerable"
  IL_000e:  castclass  "System.Collections.IEnumerable"
  IL_0013:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_0018:  stloc.0
  IL_0019:  br.s       IL_0030
  IL_001b:  ldloc.0
  IL_001c:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_0021:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0026:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_002b:  call       "Sub System.Console.WriteLine(Object)"
  IL_0030:  ldloc.0
  IL_0031:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_0036:  brtrue.s   IL_001b
  IL_0038:  leave.s    IL_004e
}
  finally
{
  IL_003a:  ldloc.0
  IL_003b:  isinst     "System.IDisposable"
  IL_0040:  brfalse.s  IL_004d
  IL_0042:  ldloc.0
  IL_0043:  isinst     "System.IDisposable"
  IL_0048:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_004d:  endfinally
}
  IL_004e:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestForEachGetEnumeratorStruct()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On

Imports System.Collections

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Structure Enumerable
    Public Function GetEnumerator() As IEnumerator
        Return New Integer() {1, 2, 3}.GetEnumerator()
    End Function
End Structure
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       72 (0x48)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0,
  Enumerable V_1)
  .try
{
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    "Enumerable"
  IL_0008:  ldloc.1
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       "Function Enumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_0011:  stloc.0
  IL_0012:  br.s       IL_0029
  IL_0014:  ldloc.0
  IL_0015:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_001a:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_001f:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0024:  call       "Sub System.Console.WriteLine(Object)"
  IL_0029:  ldloc.0
  IL_002a:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_002f:  brtrue.s   IL_0014
  IL_0031:  leave.s    IL_0047
}
  finally
{
  IL_0033:  ldloc.0
  IL_0034:  isinst     "System.IDisposable"
  IL_0039:  brfalse.s  IL_0046
  IL_003b:  ldloc.0
  IL_003c:  isinst     "System.IDisposable"
  IL_0041:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0046:  endfinally
}
  IL_0047:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestForEachDisposableSealed()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On

Class Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Class

NotInheritable Class Enumerator
    Implements System.IDisposable
    Private x As Integer
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
    Private Sub System_IDisposable_Dispose() Implements System.IDisposable.Dispose
    End Sub
End Class

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class


    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (Enumerator V_0)
  .try
{
  IL_0000:  newobj     "Sub Enumerable..ctor()"
  IL_0005:  call       "Function Enumerable.GetEnumerator() As Enumerator"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0018
  IL_000d:  ldloc.0
  IL_000e:  callvirt   "Function Enumerator.get_Current() As Integer"
  IL_0013:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0018:  ldloc.0
  IL_0019:  callvirt   "Function Enumerator.MoveNext() As Boolean"
  IL_001e:  brtrue.s   IL_000d
  IL_0020:  leave.s    IL_002c
}
  finally
{
  IL_0022:  ldloc.0
  IL_0023:  brfalse.s  IL_002b
  IL_0025:  ldloc.0
  IL_0026:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_002b:  endfinally
}
  IL_002c:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestForEachNonDisposableSealed()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Class

NotInheritable Class Enumerator
    Private x As Integer
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (Enumerator V_0)
  IL_0000:  newobj     "Sub Enumerable..ctor()"
  IL_0005:  call       "Function Enumerable.GetEnumerator() As Enumerator"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0018
  IL_000d:  ldloc.0
  IL_000e:  callvirt   "Function Enumerator.get_Current() As Integer"
  IL_0013:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0018:  ldloc.0
  IL_0019:  callvirt   "Function Enumerator.MoveNext() As Boolean"
  IL_001e:  brtrue.s   IL_000d
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestForEachNonDisposableAbstractClass()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable1()
            System.Console.WriteLine(x)
        Next

        For Each x In New Enumerable2()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable1
    Public Function GetEnumerator() As AbstractEnumerator
        Return New DisposableEnumerator()
    End Function
End Class

Class Enumerable2
    Public Function GetEnumerator() As AbstractEnumerator
        Return New NonDisposableEnumerator()
    End Function
End Class

MustInherit Class AbstractEnumerator
    Public MustOverride ReadOnly Property Current() As Integer
    Public MustOverride Function MoveNext() As Boolean
End Class

Class DisposableEnumerator
    Inherits AbstractEnumerator
    Implements System.IDisposable
    Private x As Integer
    Public Overrides ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Overrides Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
    Private Sub System_IDisposable_Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Done with DisposableEnumerator")
    End Sub
End Class

Class NonDisposableEnumerator
    Inherits AbstractEnumerator
    Private x As Integer
    Public Overrides ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Overrides Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Decrement(x) &gt; -4
    End Function
End Class

    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
-1
-2
-3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       65 (0x41)
  .maxstack  1
  .locals init (AbstractEnumerator V_0,
  AbstractEnumerator V_1)
  IL_0000:  newobj     "Sub Enumerable1..ctor()"
  IL_0005:  call       "Function Enumerable1.GetEnumerator() As AbstractEnumerator"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0018
  IL_000d:  ldloc.0
  IL_000e:  callvirt   "Function AbstractEnumerator.get_Current() As Integer"
  IL_0013:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0018:  ldloc.0
  IL_0019:  callvirt   "Function AbstractEnumerator.MoveNext() As Boolean"
  IL_001e:  brtrue.s   IL_000d
  IL_0020:  newobj     "Sub Enumerable2..ctor()"
  IL_0025:  call       "Function Enumerable2.GetEnumerator() As AbstractEnumerator"
  IL_002a:  stloc.1
  IL_002b:  br.s       IL_0038
  IL_002d:  ldloc.1
  IL_002e:  callvirt   "Function AbstractEnumerator.get_Current() As Integer"
  IL_0033:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0038:  ldloc.1
  IL_0039:  callvirt   "Function AbstractEnumerator.MoveNext() As Boolean"
  IL_003e:  brtrue.s   IL_002d
  IL_0040:  ret
}
]]>)
        End Sub

        <WorkItem(528679, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528679")>
        <Fact>
        Public Sub TestForEachNested()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer on        
Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            For Each y In New Enumerable()
                System.Console.WriteLine("({0}, {1})", x, y)
            Next
        Next
    End Sub
End Class

Class Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Class

Class Enumerator
    Private x As Integer = 0
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
(1, 1)
(1, 2)
(1, 3)
(2, 1)
(2, 2)
(2, 3)
(3, 1)
(3, 2)
(3, 3)
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       79 (0x4f)
  .maxstack  3
  .locals init (Enumerator V_0,
  Integer V_1, //x
  Enumerator V_2,
  Integer V_3) //y
  IL_0000:  newobj     "Sub Enumerable..ctor()"
  IL_0005:  call       "Function Enumerable.GetEnumerator() As Enumerator"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_0046
  IL_000d:  ldloc.0
  IL_000e:  callvirt   "Function Enumerator.get_Current() As Integer"
  IL_0013:  stloc.1
  IL_0014:  newobj     "Sub Enumerable..ctor()"
  IL_0019:  call       "Function Enumerable.GetEnumerator() As Enumerator"
  IL_001e:  stloc.2
  IL_001f:  br.s       IL_003e
  IL_0021:  ldloc.2
  IL_0022:  callvirt   "Function Enumerator.get_Current() As Integer"
  IL_0027:  stloc.3
  IL_0028:  ldstr      "({0}, {1})"
  IL_002d:  ldloc.1
  IL_002e:  box        "Integer"
  IL_0033:  ldloc.3
  IL_0034:  box        "Integer"
  IL_0039:  call       "Sub System.Console.WriteLine(String, Object, Object)"
  IL_003e:  ldloc.2
  IL_003f:  callvirt   "Function Enumerator.MoveNext() As Boolean"
  IL_0044:  brtrue.s   IL_0021
  IL_0046:  ldloc.0
  IL_0047:  callvirt   "Function Enumerator.MoveNext() As Boolean"
  IL_004c:  brtrue.s   IL_000d
  IL_004e:  ret
}
]]>)
        End Sub

        <WorkItem(542075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542075")>
        <Fact>
        Public Sub TestGetEnumeratorWithParams()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On
Imports System.Collections.Generic
Imports System

Class C
    public Shared Sub Main()
        For Each x In New B()
           Console.WriteLine(x.ToLower())
        Next
    End Sub
End Class

Class A
    Public Function GetEnumerator() As List(Of String).Enumerator
        Dim s = New List(Of String)()
        s.Add("A")
        s.Add("B")
        s.Add("C")
        Return s.GetEnumerator()
    End Function
End Class

Class B
    Inherits A
    Public Overloads Function GetEnumerator(ParamArray x As Integer()) As List(Of Integer).Enumerator
        Return nothing
    End Function
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
a
b
c
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  1
  .locals init (System.Collections.Generic.List(Of String).Enumerator V_0)
  .try
{
  IL_0000:  newobj     "Sub B..ctor()"
  IL_0005:  call       "Function A.GetEnumerator() As System.Collections.Generic.List(Of String).Enumerator"
  IL_000a:  stloc.0
  IL_000b:  br.s       IL_001e
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Function System.Collections.Generic.List(Of String).Enumerator.get_Current() As String"
  IL_0014:  callvirt   "Function String.ToLower() As String"
  IL_0019:  call       "Sub System.Console.WriteLine(String)"
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       "Function System.Collections.Generic.List(Of String).Enumerator.MoveNext() As Boolean"
  IL_0025:  brtrue.s   IL_000d
  IL_0027:  leave.s    IL_0037
}
  finally
{
  IL_0029:  ldloca.s   V_0
  IL_002b:  constrained. "System.Collections.Generic.List(Of String).Enumerator"
  IL_0031:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0036:  endfinally
}
  IL_0037:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestMoveNextWithNonBoolDeclaredReturnType()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On
Imports System.Collections
Class Program
    Public  Shared Sub Main()
        Goo(sub(x)
                For Each y In x
                Next
            end sub )
    End Sub

    Public Shared Sub Goo(a As System.Action(Of IEnumerable))
        System.Console.WriteLine(1)
    End Sub

End Class

Class A
    Public Function GetEnumerator() As E(Of Boolean)
        Return New E(Of Boolean)()
    End Function
End Class

Class E(Of T)
    Public Function MoveNext() As T
        Return Nothing
    End Function

    Public Property Current() As Integer
        Get
            Return m_Current
        End Get
        Set(value As Integer)
            m_Current = Value
        End Set
    End Property
    Private m_Current As Integer
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
]]>)
        End Sub

        <Fact()>
        Public Sub TestNonConstantNullInForeach()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On
Class Program
    Public Shared Sub Main()
        Try
            Const s As String = Nothing
            For Each y In TryCast(s, String)
            Next
        Catch generatedExceptionName As System.NullReferenceException
            System.Console.WriteLine(1)
        End Try
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
]]>)
        End Sub

        <WorkItem(542079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542079")>
        <Fact>
        Public Sub TestForEachStructEnumerable()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On
Imports System.Collections
Class C
    public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class
Structure Enumerable
    Implements IEnumerable
    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return New Integer() {1, 2, 3}.GetEnumerator()
    End Function
End Structure
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       79 (0x4f)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0,
  Enumerable V_1)
  .try
{
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    "Enumerable"
  IL_0008:  ldloc.1
  IL_0009:  box        "Enumerable"
  IL_000e:  castclass  "System.Collections.IEnumerable"
  IL_0013:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_0018:  stloc.0
  IL_0019:  br.s       IL_0030
  IL_001b:  ldloc.0
  IL_001c:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_0021:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0026:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_002b:  call       "Sub System.Console.WriteLine(Object)"
  IL_0030:  ldloc.0
  IL_0031:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_0036:  brtrue.s   IL_001b
  IL_0038:  leave.s    IL_004e
}
  finally
{
  IL_003a:  ldloc.0
  IL_003b:  isinst     "System.IDisposable"
  IL_0040:  brfalse.s  IL_004d
  IL_0042:  ldloc.0
  IL_0043:  isinst     "System.IDisposable"
  IL_0048:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_004d:  endfinally
}
  IL_004e:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestForEachMutableStructEnumerablePattern()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On
Class C
    Public Shared Sub Main()
        Dim e As New Enumerable()
        System.Console.WriteLine(e.i)
        For Each x In e
        Next
        System.Console.WriteLine(e.i)
    End Sub
End Class

Structure Enumerable
    Public i As Integer
    Public Function GetEnumerator() As Enumerator
        i = i + 1
        Return New Enumerator()
    End Function
End Structure

Structure Enumerator
    Private x As Integer
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
End Structure
    </file>
</compilation>, expectedOutput:=<![CDATA[
0
1
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  1
  .locals init (Enumerable V_0, //e
  Enumerator V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Enumerable"
  IL_0008:  ldloc.0
  IL_0009:  ldfld      "Enumerable.i As Integer"
  IL_000e:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       "Function Enumerable.GetEnumerator() As Enumerator"
  IL_001a:  stloc.1
  IL_001b:  br.s       IL_0025
  IL_001d:  ldloca.s   V_1
  IL_001f:  call       "Function Enumerator.get_Current() As Integer"
  IL_0024:  pop
  IL_0025:  ldloca.s   V_1
  IL_0027:  call       "Function Enumerator.MoveNext() As Boolean"
  IL_002c:  brtrue.s   IL_001d
  IL_002e:  ldloc.0
  IL_002f:  ldfld      "Enumerable.i As Integer"
  IL_0034:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0039:  ret
}
]]>)
        End Sub

        <WorkItem(542079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542079")>
        <Fact>
        Public Sub TestForEachMutableStructEnumerableInterface()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On
Imports System.Collections

Class C
    Public  Shared Sub Main()
        Dim e As New Enumerable()
        System.Console.WriteLine(e.i)
        For Each x In e
        Next
        System.Console.WriteLine(e.i)
    End Sub
End Class

Structure Enumerable
    Implements IEnumerable
    Public i As Integer
    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        i = i + 1
        Return New Enumerator()
    End Function
End Structure

Structure Enumerator
    Implements IEnumerator
    Private x As Integer
    Public ReadOnly Property Current() As Object Implements IEnumerator.Current
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
    Public Sub Reset() Implements IEnumerator.Reset
        x = 0
    End Sub
End Structure
    </file>
</compilation>, expectedOutput:=<![CDATA[
0
0
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       92 (0x5c)
  .maxstack  1
  .locals init (Enumerable V_0, //e
  System.Collections.IEnumerator V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Enumerable"
  IL_0008:  ldloc.0
  IL_0009:  ldfld      "Enumerable.i As Integer"
  IL_000e:  call       "Sub System.Console.WriteLine(Integer)"
  .try
{
  IL_0013:  ldloc.0
  IL_0014:  box        "Enumerable"
  IL_0019:  castclass  "System.Collections.IEnumerable"
  IL_001e:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_0023:  stloc.1
  IL_0024:  br.s       IL_0032
  IL_0026:  ldloc.1
  IL_0027:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_002c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0031:  pop
  IL_0032:  ldloc.1
  IL_0033:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_0038:  brtrue.s   IL_0026
  IL_003a:  leave.s    IL_0050
}
  finally
{
  IL_003c:  ldloc.1
  IL_003d:  isinst     "System.IDisposable"
  IL_0042:  brfalse.s  IL_004f
  IL_0044:  ldloc.1
  IL_0045:  isinst     "System.IDisposable"
  IL_004a:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_004f:  endfinally
}
  IL_0050:  ldloc.0
  IL_0051:  ldfld      "Enumerable.i As Integer"
  IL_0056:  call       "Sub System.Console.WriteLine(Integer)"
  IL_005b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TypeParameterAsEnumeratorTypeCanBeReferenceType()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">      
Option Strict On

Imports System
Imports System.Collections

Public Class Custom(Of S As {IEnumerator, IDisposable})
    Public Function GetEnumerator() As S
        Return Nothing
    End Function
End Class

Class C1(Of S As {IEnumerator, IDisposable})
    Public Sub DoStuff()
        Dim myCustomCollection As Custom(Of S) = nothing

        For Each element In myCustomCollection
            Console.WriteLine("goo")
        Next
    End Sub
End Class 

Class C2
    Public Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("C1(Of S).DoStuff", <![CDATA[
{
  // Code size       80 (0x50)
  .maxstack  1
  .locals init (Custom(Of S) V_0, //myCustomCollection
  S V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  .try
{
  IL_0002:  ldloc.0
  IL_0003:  callvirt   "Function Custom(Of S).GetEnumerator() As S"
  IL_0008:  stloc.1
  IL_0009:  br.s       IL_0028
  IL_000b:  ldloca.s   V_1
  IL_000d:  constrained. "S"
  IL_0013:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_0018:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_001d:  pop
  IL_001e:  ldstr      "goo"
  IL_0023:  call       "Sub System.Console.WriteLine(String)"
  IL_0028:  ldloca.s   V_1
  IL_002a:  constrained. "S"
  IL_0030:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_0035:  brtrue.s   IL_000b
  IL_0037:  leave.s    IL_004f
}
  finally
{
  IL_0039:  ldloc.1
  IL_003a:  box        "S"
  IL_003f:  brfalse.s  IL_004e
  IL_0041:  ldloca.s   V_1
  IL_0043:  constrained. "S"
  IL_0049:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_004e:  endfinally
}
  IL_004f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TypeParameterAsEnumeratorTypeHasValueConstraint()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">      
Option Strict On

Imports System
Imports System.Collections

Public Class Custom(Of S As {IEnumerator, IDisposable, Structure})
    Public Function GetEnumerator() As S
        Return Nothing
    End Function
End Class

Class C1(Of S As {IEnumerator, IDisposable, Structure})
    Public Sub DoStuff()
        Dim myCustomCollection As Custom(Of S) = nothing

        For Each element In myCustomCollection
            Console.WriteLine("goo")
        Next
    End Sub
End Class 

Class C2
    Public Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("C1(Of S).DoStuff", <![CDATA[
{
  // Code size       72 (0x48)
  .maxstack  1
  .locals init (Custom(Of S) V_0, //myCustomCollection
  S V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  .try
{
  IL_0002:  ldloc.0
  IL_0003:  callvirt   "Function Custom(Of S).GetEnumerator() As S"
  IL_0008:  stloc.1
  IL_0009:  br.s       IL_0028
  IL_000b:  ldloca.s   V_1
  IL_000d:  constrained. "S"
  IL_0013:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_0018:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_001d:  pop
  IL_001e:  ldstr      "goo"
  IL_0023:  call       "Sub System.Console.WriteLine(String)"
  IL_0028:  ldloca.s   V_1
  IL_002a:  constrained. "S"
  IL_0030:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_0035:  brtrue.s   IL_000b
  IL_0037:  leave.s    IL_0047
}
  finally
{
  IL_0039:  ldloca.s   V_1
  IL_003b:  constrained. "S"
  IL_0041:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0046:  endfinally
}
  IL_0047:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub NoObjectCopyForGetCurrent()
            ' ILVerify: Unexpected type on the stack. { Offset = 25, Found = readonly address of '[...]C2+S1', Expected = address of '[...]C2+S1' }
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">      
Option Infer On

Imports System
Imports System.Collections.Generic

Class C2

    Public Structure S1
      Public Field as Integer

      Public Sub New(x as integer)
        Field = x
      End Sub
    End Structure

    Public Shared Sub Main()
        dim coll = New List(Of S1)
        coll.add(new S1(23))
        coll.add(new S1(42))

        DoStuff(coll)
    End Sub

    Public Shared Sub DoStuff(coll as System.Collections.IEnumerable)
        for each x as Object in coll
          Console.WriteLine(Directcast(x,S1).Field)
        next
    End Sub
End Class  
</file>
</compilation>, expectedOutput:=<![CDATA[
23
42
]]>, verify:=Verification.FailsILVerify).VerifyIL("C2.DoStuff", <![CDATA[
{
  // Code size       66 (0x42)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0)
  .try
{
  IL_0000:  ldarg.0
  IL_0001:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_0023
  IL_0009:  ldloc.0
  IL_000a:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_000f:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0014:  unbox      "C2.S1"
  IL_0019:  ldfld      "C2.S1.Field As Integer"
  IL_001e:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0023:  ldloc.0
  IL_0024:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_0029:  brtrue.s   IL_0009
  IL_002b:  leave.s    IL_0041
}
  finally
{
  IL_002d:  ldloc.0
  IL_002e:  isinst     "System.IDisposable"
  IL_0033:  brfalse.s  IL_0040
  IL_0035:  ldloc.0
  IL_0036:  isinst     "System.IDisposable"
  IL_003b:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0040:  endfinally
}
  IL_0041:  ret
}
]]>)
            ' there should be a check for null before calling Dispose
        End Sub

        <WorkItem(542185, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542185")>
        <Fact>
        Public Sub CustomDefinedType()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">      
Option Infer On
Imports System.Collections.Generic

Class C
    Public Shared Sub Main()
        Dim x = New B()
    End Sub
End Class
Class A
    Public Function GetEnumerator() As List(Of String).Enumerator
        Dim s = New List(Of String)()
        s.Add("A")
        s.Add("B")
        s.Add("C")
        Return s.GetEnumerator()
    End Function
End Class
Class B
    Inherits A
    Public Overloads Function GetEnumerator(ParamArray x As Integer()) As List(Of Integer).Enumerator
        Return New List(Of Integer).Enumerator()
    End Function
End Class
</file>
</compilation>)
        End Sub

        <Fact>
        Public Sub ForEachQuery()
            CompileAndVerify(
<compilation>
    <file name="a.vb">      
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim ii As Integer() = New Integer() {1, 2, 3}
        For Each iii In ii.Where(Function(jj) jj >= ii(0)).Select(Function(jj) jj)
            System.Console.Write(iii)
        Next
    End Sub
End Module
</file>
</compilation>,
            references:={LinqAssemblyRef},
            expectedOutput:="123")

        End Sub

        <WorkItem(544311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544311")>
        <Fact()>
        Public Sub ForEachWithMultipleDimArray()
            CompileAndVerify(
<compilation>
    <file name="a.vb">      
Option Strict On
Imports System

Module Program
    Sub Main()

        Dim k(,) = {{1}, {1}}
        For Each [Custom] In k
            Console.Write(VerifyStaticType([Custom], GetType(Integer)))
            Console.Write(VerifyStaticType([Custom], GetType(Object)))
            Exit For
        Next
    End Sub

    Function VerifyStaticType(Of T)(ByVal x As T, ByVal y As System.Type) As Boolean
        Return GetType(T) Is y
    End Function
End Module
</file>
</compilation>, expectedOutput:="TrueFalse")

        End Sub

        <WorkItem(545519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545519")>
        <Fact()>
        Public Sub NewForEachScopeDev11()
            Dim source =
<compilation>
    <file name="a.vb">
imports system
imports system.collections.generic

Module m1
    Sub Main()
        Dim actions = New List(Of Action)()
        Dim values = New List(Of Integer) From {1, 2, 3}

        ' test lifting of control variable in loop body when collection is array
        For Each i As Integer In values
            actions.Add(Sub() Console.WriteLine(i))
        Next

        For Each a In actions
            a()
        Next
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("m1.Main", <![CDATA[
{
  // Code size      154 (0x9a)
  .maxstack  3
  .locals init (System.Collections.Generic.List(Of System.Action) V_0, //actions
               System.Collections.Generic.List(Of Integer) V_1, //values
               System.Collections.Generic.List(Of Integer).Enumerator V_2,
                m1._Closure$__0-0 V_3, //$VB$Closure_0
                System.Collections.Generic.List(Of System.Action).Enumerator V_4)
  IL_0000:  newobj     "Sub System.Collections.Generic.List(Of System.Action)..ctor()"
  IL_0005:  stloc.0
  IL_0006:  newobj     "Sub System.Collections.Generic.List(Of Integer)..ctor()"
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  callvirt   "Sub System.Collections.Generic.List(Of Integer).Add(Integer)"
  IL_0012:  dup
  IL_0013:  ldc.i4.2
  IL_0014:  callvirt   "Sub System.Collections.Generic.List(Of Integer).Add(Integer)"
  IL_0019:  dup
  IL_001a:  ldc.i4.3
  IL_001b:  callvirt   "Sub System.Collections.Generic.List(Of Integer).Add(Integer)"
  IL_0020:  stloc.1
  .try
  {
    IL_0021:  ldloc.1
    IL_0022:  callvirt   "Function System.Collections.Generic.List(Of Integer).GetEnumerator() As System.Collections.Generic.List(Of Integer).Enumerator"
    IL_0027:  stloc.2
    IL_0028:  br.s       IL_0050
    IL_002a:  ldloc.3
    IL_002b:  newobj     "Sub m1._Closure$__0-0..ctor(m1._Closure$__0-0)"
    IL_0030:  stloc.3
    IL_0031:  ldloc.3
    IL_0032:  ldloca.s   V_2
    IL_0034:  call       "Function System.Collections.Generic.List(Of Integer).Enumerator.get_Current() As Integer"
    IL_0039:  stfld      "m1._Closure$__0-0.$VB$Local_i As Integer"
    IL_003e:  ldloc.0
    IL_003f:  ldloc.3
    IL_0040:  ldftn      "Sub m1._Closure$__0-0._Lambda$__0()"
    IL_0046:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
    IL_004b:  callvirt   "Sub System.Collections.Generic.List(Of System.Action).Add(System.Action)"
    IL_0050:  ldloca.s   V_2
    IL_0052:  call       "Function System.Collections.Generic.List(Of Integer).Enumerator.MoveNext() As Boolean"
    IL_0057:  brtrue.s   IL_002a
    IL_0059:  leave.s    IL_0069
  }
  finally
  {
    IL_005b:  ldloca.s   V_2
    IL_005d:  constrained. "System.Collections.Generic.List(Of Integer).Enumerator"
    IL_0063:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_0068:  endfinally
  }
  IL_0069:  nop
  .try
  {
    IL_006a:  ldloc.0
    IL_006b:  callvirt   "Function System.Collections.Generic.List(Of System.Action).GetEnumerator() As System.Collections.Generic.List(Of System.Action).Enumerator"
    IL_0070:  stloc.s    V_4
    IL_0072:  br.s       IL_0080
    IL_0074:  ldloca.s   V_4
    IL_0076:  call       "Function System.Collections.Generic.List(Of System.Action).Enumerator.get_Current() As System.Action"
    IL_007b:  callvirt   "Sub System.Action.Invoke()"
    IL_0080:  ldloca.s   V_4
    IL_0082:  call       "Function System.Collections.Generic.List(Of System.Action).Enumerator.MoveNext() As Boolean"
    IL_0087:  brtrue.s   IL_0074
    IL_0089:  leave.s    IL_0099
  }
  finally
  {
    IL_008b:  ldloca.s   V_4
    IL_008d:  constrained. "System.Collections.Generic.List(Of System.Action).Enumerator"
    IL_0093:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_0098:  endfinally
  }
  IL_0099:  ret
}
]]>)
        End Sub

        <WorkItem(545519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545519")>
        <Fact()>
        Public Sub NewForEachScopeDev11_2()
            Dim source =
<compilation>
    <file name="a.vb">
imports system
imports system.collections.generic

Module m1
    Sub Main()
        ' Test Array
        Dim x(10) as action
        ' test lifting of control variable in loop body and lifting of control variable when used 
        ' in the collection expression itself.
        For Each i As Integer In (function() {i + 1, i + 2, i + 3})()
            x(i) = Sub() console.writeline(i.toString)
        Next
        for i = 1 to 3 
            x(i).invoke()
        next
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("m1.Main", <![CDATA[
{
  // Code size       93 (0x5d)
  .maxstack  4
  .locals init (System.Action() V_0, //x
                Integer() V_1,
                Integer V_2,
                m1._Closure$__0-1 V_3, //$VB$Closure_0
                Integer V_4) //i
  IL_0000:  ldc.i4.s   11
  IL_0002:  newarr     "System.Action"
  IL_0007:  stloc.0
  IL_0008:  newobj     "Sub m1._Closure$__0-0..ctor()"
  IL_000d:  callvirt   "Function m1._Closure$__0-0._Lambda$__0() As Integer()"
  IL_0012:  stloc.1
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.2
  IL_0015:  br.s       IL_003f
  IL_0017:  ldloc.3
  IL_0018:  newobj     "Sub m1._Closure$__0-1..ctor(m1._Closure$__0-1)"
  IL_001d:  stloc.3
  IL_001e:  ldloc.3
  IL_001f:  ldloc.1
  IL_0020:  ldloc.2
  IL_0021:  ldelem.i4
  IL_0022:  stfld      "m1._Closure$__0-1.$VB$Local_i As Integer"
  IL_0027:  ldloc.0
  IL_0028:  ldloc.3
  IL_0029:  ldfld      "m1._Closure$__0-1.$VB$Local_i As Integer"
  IL_002e:  ldloc.3
  IL_002f:  ldftn      "Sub m1._Closure$__0-1._Lambda$__1()"
  IL_0035:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_003a:  stelem.ref
  IL_003b:  ldloc.2
  IL_003c:  ldc.i4.1
  IL_003d:  add.ovf
  IL_003e:  stloc.2
  IL_003f:  ldloc.2
  IL_0040:  ldloc.1
  IL_0041:  ldlen
  IL_0042:  conv.i4
  IL_0043:  blt.s      IL_0017
  IL_0045:  ldc.i4.1
  IL_0046:  stloc.s    V_4
  IL_0048:  ldloc.0
  IL_0049:  ldloc.s    V_4
  IL_004b:  ldelem.ref
  IL_004c:  callvirt   "Sub System.Action.Invoke()"
  IL_0051:  ldloc.s    V_4
  IL_0053:  ldc.i4.1
  IL_0054:  add.ovf
  IL_0055:  stloc.s    V_4
  IL_0057:  ldloc.s    V_4
  IL_0059:  ldc.i4.3
  IL_005a:  ble.s      IL_0048
  IL_005c:  ret
}
]]>)
        End Sub

        <WorkItem(545519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545519")>
        <Fact()>
        Public Sub NewForEachScopeDev11_3()
            Dim source =
<compilation>
    <file name="a.vb">
imports system
imports system.collections.generic

Module m1
    Sub Main()
        ' Test Array
        Dim x(10) as action

        for j = 0 to 2
            ' test lifting of control variable in loop body and lifting of control variable when used 
            ' in the collection expression itself.
            for each i as integer in (function(a) goo())(i)
                x(i) = sub() console.write(i.toString &amp; " ")
            next

            for i = 1 to 3 
                x(i).invoke()
            next

            Console.Writeline()
        next j
    End Sub

    function goo() as IEnumerable(of Integer)
        return new list(of integer) from {1, 2, 3}
    end function
End Module
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[1 2 3 
1 2 3 
1 2 3 

]]>).VerifyIL("m1.Main", <![CDATA[
{
  // Code size      170 (0xaa)
  .maxstack  4
  .locals init (System.Action() V_0, //x
                Integer V_1, //j
                Integer V_2,
                System.Collections.Generic.IEnumerator(Of Integer) V_3,
                m1._Closure$__0-0 V_4, //$VB$Closure_0
                Integer V_5) //i
  IL_0000:  ldc.i4.s   11
  IL_0002:  newarr     "System.Action"
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.1
  IL_000a:  nop
  .try
  {
    IL_000b:  ldsfld     "m1._Closure$__.$I0-0 As <generated method>"
    IL_0010:  brfalse.s  IL_0019
    IL_0012:  ldsfld     "m1._Closure$__.$I0-0 As <generated method>"
    IL_0017:  br.s       IL_002f
    IL_0019:  ldsfld     "m1._Closure$__.$I As m1._Closure$__"
    IL_001e:  ldftn      "Function m1._Closure$__._Lambda$__0-0(Object) As System.Collections.Generic.IEnumerable(Of Integer)"
    IL_0024:  newobj     "Sub VB$AnonymousDelegate_0(Of Object, System.Collections.Generic.IEnumerable(Of Integer))..ctor(Object, System.IntPtr)"
    IL_0029:  dup
    IL_002a:  stsfld     "m1._Closure$__.$I0-0 As <generated method>"
    IL_002f:  ldloc.2
    IL_0030:  box        "Integer"
    IL_0035:  callvirt   "Function VB$AnonymousDelegate_0(Of Object, System.Collections.Generic.IEnumerable(Of Integer)).Invoke(Object) As System.Collections.Generic.IEnumerable(Of Integer)"
    IL_003a:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_003f:  stloc.3
    IL_0040:  br.s       IL_006e
    IL_0042:  ldloc.s    V_4
    IL_0044:  newobj     "Sub m1._Closure$__0-0..ctor(m1._Closure$__0-0)"
    IL_0049:  stloc.s    V_4
    IL_004b:  ldloc.s    V_4
    IL_004d:  ldloc.3
    IL_004e:  callvirt   "Function System.Collections.Generic.IEnumerator(Of Integer).get_Current() As Integer"
    IL_0053:  stfld      "m1._Closure$__0-0.$VB$Local_i As Integer"
    IL_0058:  ldloc.0
    IL_0059:  ldloc.s    V_4
    IL_005b:  ldfld      "m1._Closure$__0-0.$VB$Local_i As Integer"
    IL_0060:  ldloc.s    V_4
    IL_0062:  ldftn      "Sub m1._Closure$__0-0._Lambda$__1()"
    IL_0068:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
    IL_006d:  stelem.ref
    IL_006e:  ldloc.3
    IL_006f:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
    IL_0074:  brtrue.s   IL_0042
    IL_0076:  leave.s    IL_0082
  }
  finally
  {
    IL_0078:  ldloc.3
    IL_0079:  brfalse.s  IL_0081
    IL_007b:  ldloc.3
    IL_007c:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_0081:  endfinally
  }
  IL_0082:  ldc.i4.1
  IL_0083:  stloc.s    V_5
  IL_0085:  ldloc.0
  IL_0086:  ldloc.s    V_5
  IL_0088:  ldelem.ref
  IL_0089:  callvirt   "Sub System.Action.Invoke()"
  IL_008e:  ldloc.s    V_5
  IL_0090:  ldc.i4.1
  IL_0091:  add.ovf
  IL_0092:  stloc.s    V_5
  IL_0094:  ldloc.s    V_5
  IL_0096:  ldc.i4.3
  IL_0097:  ble.s      IL_0085
  IL_0099:  call       "Sub System.Console.WriteLine()"
  IL_009e:  ldloc.1
  IL_009f:  ldc.i4.1
  IL_00a0:  add.ovf
  IL_00a1:  stloc.1
  IL_00a2:  ldloc.1
  IL_00a3:  ldc.i4.2
  IL_00a4:  ble        IL_000a
  IL_00a9:  ret
}
]]>)
        End Sub

        <WorkItem(545519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545519")>
        <Fact()>
        Public Sub NewForEachScopeDev11_4()
            Dim source =
<compilation>
    <file name="a.vb">
imports system
imports system.collections.generic

Module m1
    Sub Main()
        ' Test Array
        Dim lambdas As New List(Of Action)

        'Expected 0,1,2, 0,1,2, 0,1,2
        lambdas.clear    
        For y = 1 To 3
            ' test lifting of control variable in loop body and lifting of control variable when used 
            ' in the collection expression itself. The for each itself is nested in a for loop.
            For Each x as integer In (function(a)
                                         x = x + 1 
                                        return {a, x, 2}
                                      end function)(x)
                lambdas.add( Sub() Console.Write(x.ToString + "," ) )
            Next
            lambdas.add(sub() Console.WriteLine())
        Next
        For Each lambda In lambdas
           lambda()
        Next
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
0,1,2,
0,1,2,
0,1,2,
]]>).VerifyIL("m1.Main", <![CDATA[
{
  // Code size      195 (0xc3)
  .maxstack  3
  .locals init (System.Collections.Generic.List(Of System.Action) V_0, //lambdas
                Integer V_1, //y
                Object() V_2,
                Integer V_3,
                m1._Closure$__0-1 V_4, //$VB$Closure_0
                System.Collections.Generic.List(Of System.Action).Enumerator V_5)
  IL_0000:  newobj     "Sub System.Collections.Generic.List(Of System.Action)..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  callvirt   "Sub System.Collections.Generic.List(Of System.Action).Clear()"
  IL_000c:  ldc.i4.1
  IL_000d:  stloc.1
  IL_000e:  newobj     "Sub m1._Closure$__0-0..ctor()"
  IL_0013:  dup
  IL_0014:  ldfld      "m1._Closure$__0-0.$VB$NonLocal_2 As Integer"
  IL_0019:  box        "Integer"
  IL_001e:  callvirt   "Function m1._Closure$__0-0._Lambda$__0(Object) As Object()"
  IL_0023:  stloc.2
  IL_0024:  ldc.i4.0
  IL_0025:  stloc.3
  IL_0026:  br.s       IL_0057
  IL_0028:  ldloc.s    V_4
  IL_002a:  newobj     "Sub m1._Closure$__0-1..ctor(m1._Closure$__0-1)"
  IL_002f:  stloc.s    V_4
  IL_0031:  ldloc.s    V_4
  IL_0033:  ldloc.2
  IL_0034:  ldloc.3
  IL_0035:  ldelem.ref
  IL_0036:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_003b:  stfld      "m1._Closure$__0-1.$VB$Local_x As Integer"
  IL_0040:  ldloc.0
  IL_0041:  ldloc.s    V_4
  IL_0043:  ldftn      "Sub m1._Closure$__0-1._Lambda$__1()"
  IL_0049:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_004e:  callvirt   "Sub System.Collections.Generic.List(Of System.Action).Add(System.Action)"
  IL_0053:  ldloc.3
  IL_0054:  ldc.i4.1
  IL_0055:  add.ovf
  IL_0056:  stloc.3
  IL_0057:  ldloc.3
  IL_0058:  ldloc.2
  IL_0059:  ldlen
  IL_005a:  conv.i4
  IL_005b:  blt.s      IL_0028
  IL_005d:  ldloc.0
  IL_005e:  ldsfld     "m1._Closure$__.$I0-2 As System.Action"
  IL_0063:  brfalse.s  IL_006c
  IL_0065:  ldsfld     "m1._Closure$__.$I0-2 As System.Action"
  IL_006a:  br.s       IL_0082
  IL_006c:  ldsfld     "m1._Closure$__.$I As m1._Closure$__"
  IL_0071:  ldftn      "Sub m1._Closure$__._Lambda$__0-2()"
  IL_0077:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_007c:  dup
  IL_007d:  stsfld     "m1._Closure$__.$I0-2 As System.Action"
  IL_0082:  callvirt   "Sub System.Collections.Generic.List(Of System.Action).Add(System.Action)"
  IL_0087:  ldloc.1
  IL_0088:  ldc.i4.1
  IL_0089:  add.ovf
  IL_008a:  stloc.1
  IL_008b:  ldloc.1
  IL_008c:  ldc.i4.3
  IL_008d:  ble        IL_000e
  IL_0092:  nop
  .try
  {
    IL_0093:  ldloc.0
    IL_0094:  callvirt   "Function System.Collections.Generic.List(Of System.Action).GetEnumerator() As System.Collections.Generic.List(Of System.Action).Enumerator"
    IL_0099:  stloc.s    V_5
    IL_009b:  br.s       IL_00a9
    IL_009d:  ldloca.s   V_5
    IL_009f:  call       "Function System.Collections.Generic.List(Of System.Action).Enumerator.get_Current() As System.Action"
    IL_00a4:  callvirt   "Sub System.Action.Invoke()"
    IL_00a9:  ldloca.s   V_5
    IL_00ab:  call       "Function System.Collections.Generic.List(Of System.Action).Enumerator.MoveNext() As Boolean"
    IL_00b0:  brtrue.s   IL_009d
    IL_00b2:  leave.s    IL_00c2
  }
  finally
  {
    IL_00b4:  ldloca.s   V_5
    IL_00b6:  constrained. "System.Collections.Generic.List(Of System.Action).Enumerator"
    IL_00bc:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_00c1:  endfinally
  }
  IL_00c2:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ForEachLateBinding()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
imports system
Class C
    Shared Sub Main()
        Dim o As Object = {1, 2, 3}
        For Each x In o
            console.writeline(x)
        Next
    End Sub
End Class        
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithModuleName("MODULE"), expectedOutput:=<![CDATA[
1
2
3
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       84 (0x54)
  .maxstack  3
  .locals init (Object V_0, //o
                System.Collections.IEnumerator V_1)
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     "Integer"
  IL_0006:  dup
  IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.4636993D3E1DA4E9D6B8F87B79E8F7C6D018580D52661950EABC3845C5897A4D"
  IL_000c:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0011:  stloc.0
  .try
  {
    IL_0012:  ldloc.0
    IL_0013:  castclass  "System.Collections.IEnumerable"
    IL_0018:  callvirt   "Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator"
    IL_001d:  stloc.1
    IL_001e:  br.s       IL_0035
    IL_0020:  ldloc.1
    IL_0021:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
    IL_0026:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
    IL_002b:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
    IL_0030:  call       "Sub System.Console.WriteLine(Object)"
    IL_0035:  ldloc.1
    IL_0036:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
    IL_003b:  brtrue.s   IL_0020
    IL_003d:  leave.s    IL_0053
  }
  finally
  {
    IL_003f:  ldloc.1
    IL_0040:  isinst     "System.IDisposable"
    IL_0045:  brfalse.s  IL_0052
    IL_0047:  ldloc.1
    IL_0048:  isinst     "System.IDisposable"
    IL_004d:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_0052:  endfinally
  }
  IL_0053:  ret
}
]]>).Compilation

        End Sub

    End Class
End Namespace
