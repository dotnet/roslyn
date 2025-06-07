' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenUsingStatement
        Inherits BasicTestBase

        ' Take the ByRef parameter as object
        <Fact()>
        Public Sub ParameterAsResource()
            Dim source =
<compilation>
    <file name="a.vb">
Class C1
    Shared Sub main()
        Dim x As MyManagedClass = New MyManagedClass()
        foo(x)
    End Sub
    Private Shared Sub foo(ByRef x1 As MyManagedClass)
        Using x1
            x1 = New MyManagedClass()
        End Using
    End Sub
End Class
Class MyManagedClass
        Implements System.IDisposable
        Sub Dispose() Implements System.IDisposable.Dispose
            System.Console.WriteLine("Dispose")
        End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:=<![CDATA[
Dispose
]]>).VerifyIL("C1.foo", <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (MyManagedClass V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldind.ref
  IL_0002:  stloc.0
  .try
{
  IL_0003:  ldarg.0
  IL_0004:  newobj     "Sub MyManagedClass..ctor()"
  IL_0009:  stind.ref
  IL_000a:  leave.s    IL_0016
}
  finally
{
  IL_000c:  ldloc.0
  IL_000d:  brfalse.s  IL_0015
  IL_000f:  ldloc.0
  IL_0010:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0015:  endfinally
}
  IL_0016:  ret
}
]]>)
        End Sub

        ' Take the function call as object
        <Fact()>
        Public Sub FunctionCallAsResource()
            Dim source =
<compilation>
    <file name="a.vb">
Class MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
Class C1
    Sub foo()
        Using N()
        End Using
    End Sub
    Shared Sub main()
    End Sub
    Function N() As MyManagedClass
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("C1.foo", <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (MyManagedClass V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function C1.N() As MyManagedClass"
  IL_0006:  stloc.0
  .try
{
  IL_0007:  leave.s    IL_0013
}
  finally
{
  IL_0009:  ldloc.0
  IL_000a:  brfalse.s  IL_0012
  IL_000c:  ldloc.0
  IL_000d:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0012:  endfinally
}
  IL_0013:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(10570, "DevDiv_Projects/Roslyn")>
        Public Sub UsingNothing()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Option Strict Off
Class C1
    Sub foo()
        Using Nothing
        End Using
    End Sub
    Shared Sub main()
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("C1.foo", <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (Object V_0)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  .try
{
  IL_0002:  leave.s    IL_0013
}
  finally
{
  IL_0004:  ldloc.0
  IL_0005:  brfalse.s  IL_0012
  IL_0007:  ldloc.0
  IL_0008:  castclass  "System.IDisposable"
  IL_000d:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0012:  endfinally
}
  IL_0013:  ret
}]]>)
        End Sub

        ' Implement Dispose with another Name
        <Fact()>
        Public Sub ImplementDisposeWithAnotherName()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Option Strict Off
Imports System
Class C1
    Shared Sub Main()
        Dim mnObj As New MyManagedClass
        Using mnObj
        End Using
    End Sub
End Class
Class MyManagedClass
    Implements IDisposable
    Sub Dispose()
        Console.Write("Dispose")
    End Sub
    Public Sub RealDispose() Implements System.IDisposable.Dispose
        Console.Write("RealDispose")
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, "RealDispose").VerifyIL("C1.Main", <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (MyManagedClass V_0)
  IL_0000:  newobj     "Sub MyManagedClass..ctor()"
  IL_0005:  stloc.0
  .try
{
  IL_0006:  leave.s    IL_0012
}
  finally
{
  IL_0008:  ldloc.0
  IL_0009:  brfalse.s  IL_0011
  IL_000b:  ldloc.0
  IL_000c:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0011:  endfinally
}
  IL_0012:  ret
}
]]>)
        End Sub

        ' Using different kinds of values, function returns, property values, statics etc
        <Fact()>
        Public Sub DifferentKindOfValuesAsResource()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Class cls1
    Inherits Exception
    Implements IDisposable
    Public Sub Dispose() Implements System.IDisposable.Dispose
        Console.WriteLine("Dispose")
    End Sub
    Sub New()
    End Sub
    Sub New(ByRef x As cls1)
        x = Me
    End Sub
    Sub foo()
        Using Me
        End Using

        Dim m_x As New cls_member
        Using m_x.m
        End Using

        Dim p_x As New cls_prop
        Using p_x.P1
        End Using

        Dim f_x As New cls_fun
        Using f_x.F1
        End Using

        Dim c As cls1
        Using New cls1(c)
        End Using
    End Sub

    Function goo() As cls1
        goo = New cls1
        Using goo
        End Using
    End Function

    Shared Sub Main()
        Dim x = New cls1()
        x.foo()
    End Sub
End Class
Class cls_member
    Public m As New cls1
End Class
Class cls_prop
    Private o As New cls1
    Public Property P1() As cls1
        Get
            Return o
        End Get
        Set(ByVal Value As cls1)
        End Set
    End Property
End Class
Class cls_fun
    Private o As New cls1
    Public Function F1() As cls1
        Return o
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Dispose
Dispose
Dispose
Dispose
Dispose]]>)

        End Sub

        ' Nested using
        <Fact()>
        Public Sub NestedUsing()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Option Strict Off
Imports System
Class cls1
    Implements IDisposable
    Public disposed As Boolean = False
    Public Sub Dispose() Implements System.IDisposable.Dispose
        disposed = True
    End Sub
End Class
Class C1
    Shared Sub Main()
        Dim o1 As New cls1
        Using o1
            Using o1
            End Using
        End Using
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (cls1 V_0, //o1
  cls1 V_1,
  cls1 V_2)
  IL_0000:  newobj     "Sub cls1..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  stloc.1
  .try
{
  IL_0008:  ldloc.0
  IL_0009:  stloc.2
  .try
{
  IL_000a:  leave.s    IL_0020
}
  finally
{
  IL_000c:  ldloc.2
  IL_000d:  brfalse.s  IL_0015
  IL_000f:  ldloc.2
  IL_0010:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0015:  endfinally
}
}
  finally
{
  IL_0016:  ldloc.1
  IL_0017:  brfalse.s  IL_001f
  IL_0019:  ldloc.1
  IL_001a:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_001f:  endfinally
}
  IL_0020:  ret
}]]>)

        End Sub

        ' Goto in using
        <Fact()>
        Public Sub JumpInUsing()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim obj = New s1()
        Using obj
            GoTo label2
label2:
            GoTo label2
        End Using
    End Sub
End Module
Structure s1
    Implements IDisposable
    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (s1 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "s1"
  IL_0008:  ldloc.0
  IL_0009:  stloc.0
  .try
  {
    IL_000a:  br.s       IL_000a
  }
  finally
  {
    IL_000c:  ldloca.s   V_0
    IL_000e:  constrained. "s1"
    IL_0014:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_0019:  endfinally
  }
}
]]>)

        End Sub

        ' Dispose() not called if the object(Reference type) not created 
        <Fact()>
        Public Sub DisplayNotCalledIfObjectNotCreated_Class_Outside()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Class Program
    Shared mnObj As MyManagedClass
    Shared Sub Main(args As String())
        Dim x = New Program()
        Dim obj1, obj2 As MyManagedClass
        x.foo(obj1, obj2)
    End Sub
    Sub foo(ByVal x As MyManagedClass, ByRef y As MyManagedClass)
        Dim mnObj1 As MyManagedClass
        Using mnObj
            mnObj = New MyManagedClass()
        End Using
        mnObj.Display()
        Using mnObj1
            mnObj1 = New MyManagedClass()
        End Using
        mnObj1.Display()
        Using x
            x = Nothing
        End Using
        Using y
            y = Nothing
        End Using
    End Sub
End Class
Class MyManagedClass
    Implements IDisposable
    Dim flag = 1
    Sub Dispose() Implements IDisposable.Dispose
        flag = 2
        Console.WriteLine("Dispose")
    End Sub
    Sub Display()
        Console.WriteLine(flag)
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[1
1]]>)

        End Sub

        <Fact()>
        Public Sub DisplayNotCalledIfObjectNotCreated_Class_Inside()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Class Program
    Shared mnObj As MyManagedClass
    Shared Sub Main(args As String())
        Dim x = New Program()
        x.foo()
    End Sub
    Sub foo()
        Using mnObj As MyManagedClass = Nothing
        End Using
    End Sub
End Class
Class MyManagedClass
    Implements IDisposable
    Sub Dispose() Implements IDisposable.Dispose
        Console.WriteLine("Dispose")
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.foo", <![CDATA[
{
// Code size       15 (0xf)
  .maxstack  1
  .locals init (MyManagedClass V_0) //mnObj
  IL_0000:  ldnull
  IL_0001:  stloc.0
  .try
{
  IL_0002:  leave.s    IL_000e
}
  finally
{
  IL_0004:  ldloc.0
  IL_0005:  brfalse.s  IL_000d
  IL_0007:  ldloc.0
  IL_0008:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_000d:  endfinally
}
  IL_000e:  ret
}]]>)

        End Sub

        ' Dispose() not called if the object(Reference type) not created 
        <Fact()>
        Public Sub DisplayNotCalledIfObjectNotCreated_Structure_Outside()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Class Program
    Shared mnObj As MyManagedClass
    Shared Sub Main(args As String())
        Dim x = New Program()
        Dim obj1, obj2 As MyManagedClass
        x.foo(obj1, obj2)
    End Sub
    Sub foo(ByVal x As MyManagedClass, ByRef y As MyManagedClass)
        Dim mnObj1 As MyManagedClass
        Using mnObj
            mnObj = New MyManagedClass()
        End Using
        Using mnObj1
            mnObj1 = New MyManagedClass()
        End Using
        Using x
            x = Nothing
        End Using
        Using y
            y = Nothing
        End Using
    End Sub
End Class
Structure MyManagedClass
    Implements IDisposable
    Sub Dispose() Implements IDisposable.Dispose
        Console.WriteLine("Dispose")
    End Sub
End Structure
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Dispose
Dispose
Dispose
Dispose]]>)

        End Sub

        <Fact()>
        Public Sub DisplayNotCalledIfObjectNotCreated_Structure_Inside()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer on        
Imports System
Class Program
    Shared mnObj As MyManagedClass
    Shared Sub Main(args As String())
        Dim x = New Program()
        x.foo()
    End Sub
    Sub foo()
        Using mnObj As MyManagedClass = Nothing
        End Using
    End Sub
End Class
Structure MyManagedClass
    Implements IDisposable
    Sub Dispose() Implements IDisposable.Dispose
        Console.WriteLine("Dispose")
    End Sub
End Structure
    </file>
</compilation>

            CompileAndVerify(source, "Dispose").VerifyIL("Program.foo", <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (MyManagedClass V_0) //mnObj
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "MyManagedClass"
  .try
{
  IL_0008:  leave.s    IL_0018
}
  finally
{
  IL_000a:  ldloca.s   V_0
  IL_000c:  constrained. "MyManagedClass"
  IL_0012:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0017:  endfinally
}
  IL_0018:  ret
}
]]>)

        End Sub

        ' The used variable is nulled-out in the using block
        <Fact()>
        Public Sub VarNulledInUsing_Class()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared mnObj1 As MyManagedClass = New MyManagedClass()
    Shared Sub Main(args As String())
        Dim x = New Program()
        Dim obj1 = New MyManagedClass(), obj2 = New MyManagedClass()
        x.foo(obj1, obj2)
    End Sub
    Sub foo(ByVal x As MyManagedClass, ByRef y As MyManagedClass)
        Dim mnObj2 As MyManagedClass = New MyManagedClass
        Using mnObj1
            mnObj1 = Nothing
        End Using
        Using mnObj2
            mnObj2 = Nothing
        End Using
        Using x
            x = Nothing
        End Using
        Using y
            y = Nothing
        End Using
    End Sub
End Class
Class MyManagedClass
    Implements IDisposable
    Sub Dispose() Implements IDisposable.Dispose
        Console.WriteLine("Dispose")
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Dispose
Dispose
Dispose
Dispose]]>)

        End Sub

        ' The used variable is nulled-out in the using block
        <Fact()>
        Public Sub VarNulledInUsing_Structure()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared mnObj1 As MyManagedClass = New MyManagedClass()
    Shared Sub Main(args As String())
        Dim x = New Program()
        Dim obj1 = New MyManagedClass(), obj2 = New MyManagedClass()
        x.foo(obj1, obj2)
    End Sub
    Sub foo(ByVal x As MyManagedClass, ByRef y As MyManagedClass)
        Dim mnObj2 As MyManagedClass = New MyManagedClass
        Using mnObj1
            mnObj1 = Nothing
        End Using
        Using mnObj2
            mnObj2 = Nothing
        End Using
        Using x
            x = Nothing
        End Using
        Using y
            y = Nothing
        End Using
    End Sub
End Class
Structure MyManagedClass
    Implements IDisposable
    Sub Dispose() Implements IDisposable.Dispose
        Console.WriteLine("Dispose")
    End Sub
End Structure
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Dispose
Dispose
Dispose
Dispose]]>)

        End Sub

        ' Dispose() called before leave the block
        <Fact()>
        Public Sub JumpOutFromUsing()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main(args As String())
        Using x = New MyManagedClass()
            GoTo label1
        End Using
label1:
    End Sub
End Class
Class MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Dispose]]>).VerifyIL("Program.Main", <![CDATA[
{
// Code size       19 (0x13)
  .maxstack  1
  .locals init (MyManagedClass V_0) //x
  IL_0000:  newobj     "Sub MyManagedClass..ctor()"
  IL_0005:  stloc.0
  .try
{
  IL_0006:  leave.s    IL_0012
}
  finally
{
  IL_0008:  ldloc.0
  IL_0009:  brfalse.s  IL_0011
  IL_000b:  ldloc.0
  IL_000c:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0011:  endfinally
}
  IL_0012:  ret
}]]>)

        End Sub

        <Fact()>
        Public Sub JumpOutFromUsing_1()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main(args As String())
        Dim Obj1 = New MyManagedClass()
        For x As Integer = 1 To 10
            Using Obj1
                Exit For
            End Using
        Next
        Try
            Using Obj1
                Dim i As Integer = i \ i
            End Using
        Catch ex As Exception
        End Try
        goo(Obj1)
        factorial(4)
    End Sub
    Shared Function factorial(ByVal x As Integer) As Integer
        Dim o As New MyManagedClass
        Using o
            If x = 0 Then
                factorial = 1
            Else
                factorial = x * factorial(x - 1) * x
            End If
        End Using
    End Function
    Shared Sub goo(ByVal o As MyManagedClass)
        Using o
            Exit Sub
        End Using
    End Sub
End Class
Class MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Dispose
Dispose
Dispose
Dispose
Dispose
Dispose
Dispose
Dispose]]>)

        End Sub

        ' Disposing the var inside the Using
        <Fact()>
        Public Sub CallDisposeInsideUsing()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main(args As String())
        Dim obj As New MyManagedClass
        Using obj
            CType(obj, IDisposable).Dispose()
        End Using
    End Sub
End Class
Structure MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Structure
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Dispose
Dispose]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  1
  .locals init (MyManagedClass V_0, //obj
  MyManagedClass V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "MyManagedClass"
  IL_0008:  ldloc.0
  IL_0009:  stloc.1
  .try
{
  IL_000a:  ldloc.0
  IL_000b:  box        "MyManagedClass"
  IL_0010:  castclass  "System.IDisposable"
  IL_0015:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_001a:  leave.s    IL_002a
}
  finally
{
  IL_001c:  ldloca.s   V_1
  IL_001e:  constrained. "MyManagedClass"
  IL_0024:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0029:  endfinally
}
  IL_002a:  ret
}
]]>)

        End Sub

        ' Multiple object declared inside using with different type
        <Fact()>
        Public Sub MultipleResourceWithDifferentType()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Using x = New MyManagedClass(), y = New MyManagedClass1()
        End Using
    End Sub
End Class

Class MyManagedClass
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
Structure MyManagedClass1
    Implements System.IDisposable
    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose1")
    End Sub
End Structure
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Dispose1
Dispose]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  1
  .locals init (MyManagedClass V_0, //x
  MyManagedClass1 V_1) //y
  IL_0000:  newobj     "Sub MyManagedClass..ctor()"
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldloca.s   V_1
  IL_0008:  initobj    "MyManagedClass1"
  .try
{
  IL_000e:  leave.s    IL_0028
}
  finally
{
  IL_0010:  ldloca.s   V_1
  IL_0012:  constrained. "MyManagedClass1"
  IL_0018:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_001d:  endfinally
}
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

        <Fact()>
        Public Sub MutableStructCallAndUsing1()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()

        Using y = New MyManagedStruct(42)

            y.mutate(123)
        End Using

    End Sub
End Class

Structure MyManagedStruct
    Implements System.IDisposable

    Private num As Integer

    Sub mutate(x As Integer)
        num = x
    End Sub

    Sub New(x As Integer)
        num = x
    End Sub

    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose_" &amp; num)
    End Sub
End Structure
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Dispose_42]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (MyManagedStruct V_0, //y
  MyManagedStruct V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   42
  IL_0004:  call       "Sub MyManagedStruct..ctor(Integer)"
  .try
{
  IL_0009:  ldloc.0
  IL_000a:  stloc.1
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldc.i4.s   123
  IL_000f:  call       "Sub MyManagedStruct.mutate(Integer)"
  IL_0014:  leave.s    IL_0024
}
  finally
{
  IL_0016:  ldloca.s   V_0
  IL_0018:  constrained. "MyManagedStruct"
  IL_001e:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0023:  endfinally
}
  IL_0024:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub MutableStructCallAndUsing2()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()

        Using y = New MyManagedStruct(42)
            y.mutate(123)
            y.n.n.mutate(456)
        End Using

    End Sub
End Class

Structure MyManagedStruct
    Implements System.IDisposable

    Structure Nested
        Public n As Nested1

        Structure Nested1
            Public num As Integer

            Sub mutate(x As Integer)
                num = x
            End Sub
        End Structure
    End Structure

    Public n As Nested

    Sub mutate(x As Integer)
        n.n.num = x
    End Sub

    Sub New(x As Integer)
        n.n.num = x
    End Sub

    Sub Dispose() Implements System.IDisposable.Dispose
        System.Console.WriteLine("Dispose_" &amp; n.n.num)
    End Sub
End Structure
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Dispose_42]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       61 (0x3d)
  .maxstack  2
  .locals init (MyManagedStruct V_0, //y
  MyManagedStruct V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   42
  IL_0004:  call       "Sub MyManagedStruct..ctor(Integer)"
  .try
{
  IL_0009:  ldloc.0
  IL_000a:  stloc.1
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldc.i4.s   123
  IL_000f:  call       "Sub MyManagedStruct.mutate(Integer)"
  IL_0014:  ldloc.0
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_1
  IL_0018:  ldflda     "MyManagedStruct.n As MyManagedStruct.Nested"
  IL_001d:  ldflda     "MyManagedStruct.Nested.n As MyManagedStruct.Nested.Nested1"
  IL_0022:  ldc.i4     0x1c8
  IL_0027:  call       "Sub MyManagedStruct.Nested.Nested1.mutate(Integer)"
  IL_002c:  leave.s    IL_003c
}
  finally
{
  IL_002e:  ldloca.s   V_0
  IL_0030:  constrained. "MyManagedStruct"
  IL_0036:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_003b:  endfinally
}
  IL_003c:  ret
}
]]>)

        End Sub

        <Fact()>
        Public Sub Assignment_2()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Option Strict On
Imports System
Class Program
    Shared Sub Main()
        Dim x As MyManagedClass = Nothing
        Using x
            x = MyManagedClass.Acquire()
        End Using
    End Sub
End Class

Public Class MyManagedClass
    Implements System.IDisposable
    Public Sub Dispose() Implements System.IDisposable.Dispose
        Console.Write("Dispose")
    End Sub
    Public Shared Function Acquire() As MyManagedClass
        Return New MyManagedClass()
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (MyManagedClass V_0)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  .try
{
  IL_0002:  call       "Function MyManagedClass.Acquire() As MyManagedClass"
  IL_0007:  pop
  IL_0008:  leave.s    IL_0014
}
  finally
{
  IL_000a:  ldloc.0
  IL_000b:  brfalse.s  IL_0013
  IL_000d:  ldloc.0
  IL_000e:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0013:  endfinally
}
  IL_0014:  ret
}
]]>)

        End Sub

        'Put the using around the try
        <Fact()>
        Public Sub UsingAroundTry()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System        
Class Program
    Public Shared Sub Main()
        Dim x As New MyManagedClass()
        Using x
            Try
                System.Console.WriteLine("Try")
            Finally
                x = Nothing
                System.Console.WriteLine("Catch")
            End Try
        End Using
    End Sub
End Class
Public Class MyManagedClass
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Try
Catch
Dispose]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       40 (0x28)
  .maxstack  1
  .locals init (MyManagedClass V_0)
  IL_0000:  newobj     "Sub MyManagedClass..ctor()"
  IL_0005:  stloc.0
  .try
{
  .try
{
  IL_0006:  ldstr      "Try"
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  leave.s    IL_0027
}
  finally
{
  IL_0012:  ldstr      "Catch"
  IL_0017:  call       "Sub System.Console.WriteLine(String)"
  IL_001c:  endfinally
}
}
  finally
{
  IL_001d:  ldloc.0
  IL_001e:  brfalse.s  IL_0026
  IL_0020:  ldloc.0
  IL_0021:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0026:  endfinally
}
  IL_0027:  ret
}]]>)

        End Sub

        'Put the using in the try
        <Fact()>
        Public Sub UsingInTry()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System        
Class Program
    Public Shared Sub Main()
        Dim x As New MyManagedClass()
        Try
            System.Console.WriteLine("Try")
            Using x
                Throw New Exception()
            End Using
        Catch
            System.Console.WriteLine("Catch")
            x = Nothing
        End Try
    End Sub
End Class
Public Class MyManagedClass
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
        System.Console.WriteLine("Dispose")
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Try
Dispose
Catch]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  1
  .locals init (MyManagedClass V_0, //x
  MyManagedClass V_1)
  IL_0000:  newobj     "Sub MyManagedClass..ctor()"
  IL_0005:  stloc.0
  .try
{
  IL_0006:  ldstr      "Try"
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  ldloc.0
  IL_0011:  stloc.1
  .try
{
  IL_0012:  newobj     "Sub System.Exception..ctor()"
  IL_0017:  throw
}
  finally
{
  IL_0018:  ldloc.1
  IL_0019:  brfalse.s  IL_0021
  IL_001b:  ldloc.1
  IL_001c:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0021:  endfinally
}
}
  catch System.Exception
{
  IL_0022:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_0027:  ldstr      "Catch"
  IL_002c:  call       "Sub System.Console.WriteLine(String)"
  IL_0031:  ldnull
  IL_0032:  stloc.0
  IL_0033:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0038:  leave.s    IL_003a
}
  IL_003a:  ret
}]]>)

        End Sub

        ' Using inside SyncLock
        <Fact()>
        Public Sub UsingInsideSyncLock()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Class cls1
    Implements IDisposable
    Public disposed As Boolean = False
    Public Sub Dispose() Implements System.IDisposable.Dispose
        disposed = True
        Console.WriteLine("Dispose")
    End Sub
End Class
Class Program
    Shared Sub main()
        Dim o1 As New cls1
        SyncLock New Object
            Using o1
            End Using
        End SyncLock
        Dim o2 As New cls1
        SyncLock New Object
            Using o2
            End Using
        End SyncLock
        Dim o3 As New cls1
        Try
            SyncLock New Object
                Using o3
                    Throw New Exception
                End Using
            End SyncLock
        Catch
        End Try
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[Dispose
Dispose
Dispose]]>)

        End Sub

        <Fact()>
        Public Sub Regress574470()
            Dim source =
<compilation>
    <file name="a.vb">
Option Infer On
Imports System

Structure strd
    Implements IDisposable
    Public disposed As Integer
    Public Shared disposed_s As Integer
    Public Sub Dispose() Implements System.IDisposable.Dispose
        disposed += 1
        disposed_s += 1
    End Sub
End Structure
Friend Module Module1
    Sub Main()
        strd.disposed_s = 0
        Dim o2 As Object = New strd
        Using o2
            Using o2
            End Using
        End Using
        Console.Write(o2.disposed)
        Console.Write(o2.disposed_s)
    End Sub
End Module

    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[22]]>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size      120 (0x78)
  .maxstack  7
  .locals init (Object V_0, //o2
  strd V_1,
  Object V_2,
  Object V_3)
  IL_0000:  ldc.i4.0
  IL_0001:  stsfld     "strd.disposed_s As Integer"
  IL_0006:  ldloca.s   V_1
  IL_0008:  initobj    "strd"
  IL_000e:  ldloc.1
  IL_000f:  box        "strd"
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  stloc.2
  .try
{
  IL_0017:  ldloc.0
  IL_0018:  stloc.3
  .try
{
  IL_0019:  leave.s    IL_0039
}
  finally
{
  IL_001b:  ldloc.3
  IL_001c:  brfalse.s  IL_0029
  IL_001e:  ldloc.3
  IL_001f:  castclass  "System.IDisposable"
  IL_0024:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0029:  endfinally
}
}
  finally
{
  IL_002a:  ldloc.2
  IL_002b:  brfalse.s  IL_0038
  IL_002d:  ldloc.2
  IL_002e:  castclass  "System.IDisposable"
  IL_0033:  callvirt   "Sub System.IDisposable.Dispose()"
  IL_0038:  endfinally
}
  IL_0039:  ldloc.0
  IL_003a:  ldnull
  IL_003b:  ldstr      "disposed"
  IL_0040:  ldc.i4.0
  IL_0041:  newarr     "Object"
  IL_0046:  ldnull
  IL_0047:  ldnull
  IL_0048:  ldnull
  IL_0049:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_004e:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0053:  call       "Sub System.Console.Write(Object)"
  IL_0058:  ldloc.0
  IL_0059:  ldnull
  IL_005a:  ldstr      "disposed_s"
  IL_005f:  ldc.i4.0
  IL_0060:  newarr     "Object"
  IL_0065:  ldnull
  IL_0066:  ldnull
  IL_0067:  ldnull
  IL_0068:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_006d:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0072:  call       "Sub System.Console.Write(Object)"
  IL_0077:  ret
}
]]>)

        End Sub

    End Class
End Namespace
