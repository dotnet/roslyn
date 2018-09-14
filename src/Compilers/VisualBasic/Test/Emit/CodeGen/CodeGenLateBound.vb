' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenLateBound
        Inherits BasicTestBase

        <Fact()>
        Public Sub LateAccess()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program
    Sub Main()
        Dim obj As Object = New cls1
        obj.P1 = 42                         ' assignment    (Set)
        obj.P1()                            ' side-effect   (Call)
        Console.WriteLine(obj.P1)           ' value         (Get)
    End Sub

    Class cls1
        Private _p1 As Integer
        Public Property p1 As Integer
            Get
                Console.Write("Get")
                Return _p1
            End Get
            Set(value As Integer)
                Console.Write("Set")
                _p1 = value
            End Set
        End Property
    End Class
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[SetGetGet42]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       91 (0x5b)
  .maxstack  8
  .locals init (Object V_0) //obj
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldnull
  IL_0008:  ldstr      "P1"
  IL_000d:  ldc.i4.1
  IL_000e:  newarr     "Object"
  IL_0013:  dup
  IL_0014:  ldc.i4.0
  IL_0015:  ldc.i4.s   42
  IL_0017:  box        "Integer"
  IL_001c:  stelem.ref
  IL_001d:  ldnull
  IL_001e:  ldnull
  IL_001f:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateSet(Object, System.Type, String, Object(), String(), System.Type())"
  IL_0024:  ldloc.0
  IL_0025:  ldnull
  IL_0026:  ldstr      "P1"
  IL_002b:  ldc.i4.0
  IL_002c:  newarr     "Object"
  IL_0031:  ldnull
  IL_0032:  ldnull
  IL_0033:  ldnull
  IL_0034:  ldc.i4.1
  IL_0035:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_003a:  pop
  IL_003b:  ldloc.0
  IL_003c:  ldnull
  IL_003d:  ldstr      "P1"
  IL_0042:  ldc.i4.0
  IL_0043:  newarr     "Object"
  IL_0048:  ldnull
  IL_0049:  ldnull
  IL_004a:  ldnull
  IL_004b:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_0050:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0055:  call       "Sub System.Console.WriteLine(Object)"
  IL_005a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LateAccessByref()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program
    Sub Main()
        Dim obj As Object = New cls1
        goo(obj.p1)                     'LateSetComplex
        Console.WriteLine(obj.P1)
    End Sub

    Sub goo(ByRef x As Object)
        x = 42
    End Sub

    Class cls1
        Private _p1 As Integer
        Public Property p1 As Integer
            Get
                Console.Write("Get")
                Return _p1
            End Get
            Set(value As Integer)
                Console.Write("Set")
                _p1 = value
            End Set
        End Property
    End Class
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[GetSetGet42]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       98 (0x62)
  .maxstack  8
  .locals init (Object V_0, //obj
  Object V_1)
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  dup
  IL_0008:  ldnull
  IL_0009:  ldstr      "p1"
  IL_000e:  ldc.i4.0
  IL_000f:  newarr     "Object"
  IL_0014:  ldnull
  IL_0015:  ldnull
  IL_0016:  ldnull
  IL_0017:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_001c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0021:  stloc.1
  IL_0022:  ldloca.s   V_1
  IL_0024:  call       "Sub Program.goo(ByRef Object)"
  IL_0029:  ldnull
  IL_002a:  ldstr      "p1"
  IL_002f:  ldc.i4.1
  IL_0030:  newarr     "Object"
  IL_0035:  dup
  IL_0036:  ldc.i4.0
  IL_0037:  ldloc.1
  IL_0038:  stelem.ref
  IL_0039:  ldnull
  IL_003a:  ldnull
  IL_003b:  ldc.i4.1
  IL_003c:  ldc.i4.0
  IL_003d:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateSetComplex(Object, System.Type, String, Object(), String(), System.Type(), Boolean, Boolean)"
  IL_0042:  ldloc.0
  IL_0043:  ldnull
  IL_0044:  ldstr      "P1"
  IL_0049:  ldc.i4.0
  IL_004a:  newarr     "Object"
  IL_004f:  ldnull
  IL_0050:  ldnull
  IL_0051:  ldnull
  IL_0052:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_0057:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_005c:  call       "Sub System.Console.WriteLine(Object)"
  IL_0061:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LateCall()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Module Program
    Sub Main()
        Dim obj As Object = "hi"
        cls1.goo$(obj)
    End Sub

    Class cls1
        Shared Function goo$(x As Integer)
            System.Console.WriteLine("int")
            Return Nothing
        End Function
        Shared Function goo$(x As String)
            System.Console.WriteLine("str")
            Return Nothing
        End Function
    End Class
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[str]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       70 (0x46)
  .maxstack  10
  .locals init (Object V_0, //obj
  Object() V_1,
  Boolean() V_2)
  IL_0000:  ldstr      "hi"
  IL_0005:  stloc.0
  IL_0006:  ldnull
  IL_0007:  ldtoken    "Program.cls1"
  IL_000c:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0011:  ldstr      "goo"
  IL_0016:  ldc.i4.1
  IL_0017:  newarr     "Object"
  IL_001c:  dup
  IL_001d:  ldc.i4.0
  IL_001e:  ldloc.0
  IL_001f:  stelem.ref
  IL_0020:  dup
  IL_0021:  stloc.1
  IL_0022:  ldnull
  IL_0023:  ldnull
  IL_0024:  ldc.i4.1
  IL_0025:  newarr     "Boolean"
  IL_002a:  dup
  IL_002b:  ldc.i4.0
  IL_002c:  ldc.i4.1
  IL_002d:  stelem.i1
  IL_002e:  dup
  IL_002f:  stloc.2
  IL_0030:  ldc.i4.1
  IL_0031:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_0036:  pop
  IL_0037:  ldloc.2
  IL_0038:  ldc.i4.0
  IL_0039:  ldelem.u1
  IL_003a:  brfalse.s  IL_0045
  IL_003c:  ldloc.1
  IL_003d:  ldc.i4.0
  IL_003e:  ldelem.ref
  IL_003f:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0044:  stloc.0
  IL_0045:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub GenericCall()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Option Strict Off

Imports System

Module Program
    Sub Main()
        Dim obj As Object = New cls1
        obj.Goo(Of String)()
        Console.WriteLine(obj.Goo(Of Integer)())
    End Sub

    Class cls1
        Public Function goo(Of T)()
            Console.WriteLine(GetType(T))
            Return 42
        End Function
    End Class
End Module
    </file>
    </compilation>,
    expectedOutput:=<![CDATA[System.String
System.Int32
42]]>)
        End Sub

        <Fact()>
        Public Sub LateIndex()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Option Strict Off

Imports System

Module Program
    Sub Main()
        Dim obj As Object = New cls1
        obj(1) = 41                         ' assignment    (IndexSet)
        Console.WriteLine(obj(1))           ' value         (IndexGet)
    End Sub

    Class cls1
        Private _p1 As Integer
        Default Public Property p1(x As Integer) As Integer
            Get
                Console.Write("Get")
                Return _p1
            End Get
            Set(value As Integer)
                Console.Write("Set")
                _p1 = value + x
            End Set
        End Property
    End Class
End Module
    </file>
    </compilation>,
    expectedOutput:=<![CDATA[SetGet42]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       71 (0x47)
  .maxstack  5
  .locals init (Object V_0) //obj
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.2
  IL_0008:  newarr     "Object"
  IL_000d:  dup
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.1
  IL_0010:  box        "Integer"
  IL_0015:  stelem.ref
  IL_0016:  dup
  IL_0017:  ldc.i4.1
  IL_0018:  ldc.i4.s   41
  IL_001a:  box        "Integer"
  IL_001f:  stelem.ref
  IL_0020:  ldnull
  IL_0021:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexSet(Object, Object(), String())"
  IL_0026:  ldloc.0
  IL_0027:  ldc.i4.1
  IL_0028:  newarr     "Object"
  IL_002d:  dup
  IL_002e:  ldc.i4.0
  IL_002f:  ldc.i4.1
  IL_0030:  box        "Integer"
  IL_0035:  stelem.ref
  IL_0036:  ldnull
  IL_0037:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexGet(Object, Object(), String()) As Object"
  IL_003c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0041:  call       "Sub System.Console.WriteLine(Object)"
  IL_0046:  ret
}
    ]]>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub LateIndexRValue()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Option Strict Off

Imports System

Module Program
    Sub Main()
        Dim obj As Object = New cls1
        Dim c As New cls1
        obj(c(1)) = c(40)                   ' assignment    (IndexSet)
        Console.WriteLine(obj(c(1)))           ' value         (IndexGet)

        Dim saveCulture = System.Threading.Thread.CurrentThread.CurrentCulture
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            obj(Sub()

                End Sub) = 7                   ' InvalidCast 

        Catch ex As InvalidCastException
            Console.WriteLine(ex.Message)
        Finally
            System.Threading.Thread.CurrentThread.CurrentCulture = saveCulture
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Sub

    Class cls1
        Private _p1 As Integer
        Default Public Property p1(x As Integer) As Integer
            Get
                Console.Write("Get")
                Return _p1 + x
            End Get
            Set(value As Integer)
                Console.Write("Set")
                _p1 = value + x
            End Set
        End Property
    End Class
End Module
    </file>
    </compilation>,
    expectedOutput:=<![CDATA[GetGetSetGetGet42
Method invocation failed because 'Public Property p1(x As Integer) As Integer' cannot be called with these arguments:
    Argument matching parameter 'x' cannot convert from 'VB$AnonymousDelegate_0' to 'Integer'.]]>)
        End Sub

        <Fact()>
        Public Sub LateIndexByref()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program
    Dim obj As Object = New cls1

    Sub Main()
        goo(EvalObj(x:=EvalArg))                     'LateIndexSetComplex
        Console.WriteLine(obj(1))
    End Sub

    Private Function EvalArg() As Integer
        Console.Write("EvalArg")
        Return 1
    End Function

    Private Function EvalObj() As Object
        Console.Write("EvalObj")
        Return obj
    End Function

    Sub goo(ByRef x As Object)
        x = 40
    End Sub

    Class cls1
        Private _p1 As Integer
        Default Public Property p1(x As Integer) As Integer
            Get
                Console.Write("Get")
                Return _p1 + x
            End Get
            Set(value As Integer)
                Console.Write("Set")
                _p1 = value + x
            End Set
        End Property
    End Class
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[EvalObjEvalArgGetSetGet42]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      133 (0x85)
  .maxstack  6
  .locals init (Object V_0,
  Object V_1,
  Object V_2)
  IL_0000:  call       "Function Program.EvalObj() As Object"
  IL_0005:  dup
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  newarr     "Object"
  IL_000d:  dup
  IL_000e:  ldc.i4.0
  IL_000f:  call       "Function Program.EvalArg() As Integer"
  IL_0014:  box        "Integer"
  IL_0019:  dup
  IL_001a:  stloc.1
  IL_001b:  stelem.ref
  IL_001c:  ldc.i4.1
  IL_001d:  newarr     "String"
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldstr      "x"
  IL_0029:  stelem.ref
  IL_002a:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexGet(Object, Object(), String()) As Object"
  IL_002f:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0034:  stloc.2
  IL_0035:  ldloca.s   V_2
  IL_0037:  call       "Sub Program.goo(ByRef Object)"
  IL_003c:  ldloc.0
  IL_003d:  ldc.i4.2
  IL_003e:  newarr     "Object"
  IL_0043:  dup
  IL_0044:  ldc.i4.0
  IL_0045:  ldloc.1
  IL_0046:  stelem.ref
  IL_0047:  dup
  IL_0048:  ldc.i4.1
  IL_0049:  ldloc.2
  IL_004a:  stelem.ref
  IL_004b:  ldc.i4.1
  IL_004c:  newarr     "String"
  IL_0051:  dup
  IL_0052:  ldc.i4.0
  IL_0053:  ldstr      "x"
  IL_0058:  stelem.ref
  IL_0059:  ldc.i4.1
  IL_005a:  ldc.i4.1
  IL_005b:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexSetComplex(Object, Object(), String(), Boolean, Boolean)"
  IL_0060:  ldsfld     "Program.obj As Object"
  IL_0065:  ldc.i4.1
  IL_0066:  newarr     "Object"
  IL_006b:  dup
  IL_006c:  ldc.i4.0
  IL_006d:  ldc.i4.1
  IL_006e:  box        "Integer"
  IL_0073:  stelem.ref
  IL_0074:  ldnull
  IL_0075:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexGet(Object, Object(), String()) As Object"
  IL_007a:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_007f:  call       "Sub System.Console.WriteLine(Object)"
  IL_0084:  ret
}
]]>)
        End Sub

        <WorkItem(543749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543749")>
        <Fact()>
        Public Sub MethodCallWithoutArgParensEnclosedInParens()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Option Strict Off
Imports System

Module Program
    Sub Main()
        Try
            Dim a = ("a".Clone)()
        Catch ex As MissingMemberException
        End Try
    End Sub
End Module
    </file>
    </compilation>,
    expectedOutput:=<![CDATA[]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (System.MissingMemberException V_0) //ex
  .try
{
  IL_0000:  ldstr      "a"
  IL_0005:  call       "Function String.Clone() As Object"
  IL_000a:  ldc.i4.0
  IL_000b:  newarr     "Object"
  IL_0010:  ldnull
  IL_0011:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexGet(Object, Object(), String()) As Object"
  IL_0016:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_001b:  pop
  IL_001c:  leave.s    IL_002c
}
  catch System.MissingMemberException
{
  IL_001e:  dup
  IL_001f:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_0024:  stloc.0
  IL_0025:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_002a:  leave.s    IL_002c
}
  IL_002c:  ret
}
    ]]>)
        End Sub

        <Fact()>
        Public Sub LateAccessCompound()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        x.p += 1
        Console.WriteLine(x.p)
    End Sub

    Class cls1
        Private _p1 As Integer
        Public Property p() As Integer
            Get
                Console.WriteLine("Get")
                Return _p1
            End Get
            Set(value As Integer)
                Console.WriteLine("Set")
                _p1 = value
            End Set
        End Property
    End Class

End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[Get
Set
Get
1]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       95 (0x5f)
  .maxstack  13
  .locals init (Object V_0, //x
  Object V_1)
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  ldnull
  IL_000a:  ldstr      "p"
  IL_000f:  ldc.i4.1
  IL_0010:  newarr     "Object"
  IL_0015:  dup
  IL_0016:  ldc.i4.0
  IL_0017:  ldloc.1
  IL_0018:  ldnull
  IL_0019:  ldstr      "p"
  IL_001e:  ldc.i4.0
  IL_001f:  newarr     "Object"
  IL_0024:  ldnull
  IL_0025:  ldnull
  IL_0026:  ldnull
  IL_0027:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_002c:  ldc.i4.1
  IL_002d:  box        "Integer"
  IL_0032:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_0037:  stelem.ref
  IL_0038:  ldnull
  IL_0039:  ldnull
  IL_003a:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateSet(Object, System.Type, String, Object(), String(), System.Type())"
  IL_003f:  ldloc.0
  IL_0040:  ldnull
  IL_0041:  ldstr      "p"
  IL_0046:  ldc.i4.0
  IL_0047:  newarr     "Object"
  IL_004c:  ldnull
  IL_004d:  ldnull
  IL_004e:  ldnull
  IL_004f:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_0054:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0059:  call       "Sub System.Console.WriteLine(Object)"
  IL_005e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LateIndexCompound()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As object = New cls1
        x(Eval) += 1
        Console.WriteLine(x(1))
    End Sub

    Private Function Eval() As Integer
        Console.WriteLine("Eval")
        Return 1
    End Function

    Structure cls1
        Private _p1 As Integer
        Default Public Property p(x As Integer) As Integer
            Get
                Console.WriteLine("Get")
                Return _p1 + x
            End Get
            Set(value As Integer)
                Console.WriteLine("Set")
                _p1 = value + x
            End Set
        End Property
    End Structure

End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[Eval
Get
Set
Get
4]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      109 (0x6d)
  .maxstack  9
  .locals init (Object V_0, //x
  Program.cls1 V_1,
  Object V_2,
  Object V_3)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    "Program.cls1"
  IL_0008:  ldloc.1
  IL_0009:  box        "Program.cls1"
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  stloc.2
  IL_0011:  ldloc.2
  IL_0012:  ldc.i4.2
  IL_0013:  newarr     "Object"
  IL_0018:  dup
  IL_0019:  ldc.i4.0
  IL_001a:  call       "Function Program.Eval() As Integer"
  IL_001f:  box        "Integer"
  IL_0024:  dup
  IL_0025:  stloc.3
  IL_0026:  stelem.ref
  IL_0027:  dup
  IL_0028:  ldc.i4.1
  IL_0029:  ldloc.2
  IL_002a:  ldc.i4.1
  IL_002b:  newarr     "Object"
  IL_0030:  dup
  IL_0031:  ldc.i4.0
  IL_0032:  ldloc.3
  IL_0033:  stelem.ref
  IL_0034:  ldnull
  IL_0035:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexGet(Object, Object(), String()) As Object"
  IL_003a:  ldc.i4.1
  IL_003b:  box        "Integer"
  IL_0040:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_0045:  stelem.ref
  IL_0046:  ldnull
  IL_0047:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexSet(Object, Object(), String())"
  IL_004c:  ldloc.0
  IL_004d:  ldc.i4.1
  IL_004e:  newarr     "Object"
  IL_0053:  dup
  IL_0054:  ldc.i4.0
  IL_0055:  ldc.i4.1
  IL_0056:  box        "Integer"
  IL_005b:  stelem.ref
  IL_005c:  ldnull
  IL_005d:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexGet(Object, Object(), String()) As Object"
  IL_0062:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0067:  call       "Sub System.Console.WriteLine(Object)"
  IL_006c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LateMemberCompound()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        x.p(Eval) += 1
        Console.WriteLine(x.p(1))
    End Sub

    Private Function Eval() As Integer
        Console.WriteLine("Eval")
        Return 1
    End Function

    Class cls1
        Private _p1 As Integer
        Default Public Property p(x As Integer) As Integer
            Get
                Console.WriteLine("Get")
                Return _p1 + x
            End Get
            Set(value As Integer)
                Console.WriteLine("Set")
                _p1 = value + x
            End Set
        End Property
    End Class

End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[Eval
Get
Set
Get
4]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      123 (0x7b)
  .maxstack  13
  .locals init (Object V_0, //x
  Object V_1,
  Object V_2)
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  ldnull
  IL_000a:  ldstr      "p"
  IL_000f:  ldc.i4.2
  IL_0010:  newarr     "Object"
  IL_0015:  dup
  IL_0016:  ldc.i4.0
  IL_0017:  call       "Function Program.Eval() As Integer"
  IL_001c:  box        "Integer"
  IL_0021:  dup
  IL_0022:  stloc.2
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  ldc.i4.1
  IL_0026:  ldloc.1
  IL_0027:  ldnull
  IL_0028:  ldstr      "p"
  IL_002d:  ldc.i4.1
  IL_002e:  newarr     "Object"
  IL_0033:  dup
  IL_0034:  ldc.i4.0
  IL_0035:  ldloc.2
  IL_0036:  stelem.ref
  IL_0037:  ldnull
  IL_0038:  ldnull
  IL_0039:  ldnull
  IL_003a:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_003f:  ldc.i4.1
  IL_0040:  box        "Integer"
  IL_0045:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_004a:  stelem.ref
  IL_004b:  ldnull
  IL_004c:  ldnull
  IL_004d:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateSet(Object, System.Type, String, Object(), String(), System.Type())"
  IL_0052:  ldloc.0
  IL_0053:  ldnull
  IL_0054:  ldstr      "p"
  IL_0059:  ldc.i4.1
  IL_005a:  newarr     "Object"
  IL_005f:  dup
  IL_0060:  ldc.i4.0
  IL_0061:  ldc.i4.1
  IL_0062:  box        "Integer"
  IL_0067:  stelem.ref
  IL_0068:  ldnull
  IL_0069:  ldnull
  IL_006a:  ldnull
  IL_006b:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_0070:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0075:  call       "Sub System.Console.WriteLine(Object)"
  IL_007a:  ret
}
]]>)
        End Sub


        <Fact()>
        Public Sub LateMemberInvoke()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        x.p(Eval)
    End Sub

    Private Function Eval() As Integer
        Console.WriteLine("Eval")
        Return 1
    End Function

    Class cls1
        Private _p1 As Integer
        Default Public Property p(x As Integer) As Integer
            Get
                Console.WriteLine("Get")
                Return _p1 + x
            End Get
            Set(value As Integer)
                Console.WriteLine("Set")
                _p1 = value + x
            End Set
        End Property
    End Class

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Eval
Get]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  8
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  ldnull
  IL_0006:  ldstr      "p"
  IL_000b:  ldc.i4.1
  IL_000c:  newarr     "Object"
  IL_0011:  dup
  IL_0012:  ldc.i4.0
  IL_0013:  call       "Function Program.Eval() As Integer"
  IL_0018:  box        "Integer"
  IL_001d:  stelem.ref
  IL_001e:  ldnull
  IL_001f:  ldnull
  IL_0020:  ldnull
  IL_0021:  ldc.i4.1
  IL_0022:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_0027:  pop
  IL_0028:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LateMemberInvoke1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        Dim c As New cls1
        x.goo(c.p)

        Console.WriteLine(c.p)
    End Sub

    Class cls1
        Public Sub goo(ByRef x As Integer)
            x += 1
        End Sub

        Private _p1 As Integer
        Public Property p() As Integer
            Get
                Console.WriteLine("Get")
                Return _p1
            End Get
            Set(value As Integer)
                Console.WriteLine("Set")
                _p1 = value
            End Set
        End Property
    End Class

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Get
Set
Get
1]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      113 (0x71)
  .maxstack  10
  .locals init (Program.cls1 V_0, //c
  Program.cls1 V_1,
  Object() V_2,
  Boolean() V_3)
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  newobj     "Sub Program.cls1..ctor()"
  IL_000a:  stloc.0
  IL_000b:  ldnull
  IL_000c:  ldstr      "goo"
  IL_0011:  ldc.i4.1
  IL_0012:  newarr     "Object"
  IL_0017:  dup
  IL_0018:  ldc.i4.0
  IL_0019:  ldloc.0
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  callvirt   "Function Program.cls1.get_p() As Integer"
  IL_0021:  box        "Integer"
  IL_0026:  stelem.ref
  IL_0027:  dup
  IL_0028:  stloc.2
  IL_0029:  ldnull
  IL_002a:  ldnull
  IL_002b:  ldc.i4.1
  IL_002c:  newarr     "Boolean"
  IL_0031:  dup
  IL_0032:  ldc.i4.0
  IL_0033:  ldc.i4.1
  IL_0034:  stelem.i1
  IL_0035:  dup
  IL_0036:  stloc.3
  IL_0037:  ldc.i4.1
  IL_0038:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_003d:  pop
  IL_003e:  ldloc.3
  IL_003f:  ldc.i4.0
  IL_0040:  ldelem.u1
  IL_0041:  brfalse.s  IL_0065
  IL_0043:  ldloc.1
  IL_0044:  ldloc.2
  IL_0045:  ldc.i4.0
  IL_0046:  ldelem.ref
  IL_0047:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_004c:  ldtoken    "Integer"
  IL_0051:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0056:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ChangeType(Object, System.Type) As Object"
  IL_005b:  unbox.any  "Integer"
  IL_0060:  callvirt   "Sub Program.cls1.set_p(Integer)"
  IL_0065:  ldloc.0
  IL_0066:  callvirt   "Function Program.cls1.get_p() As Integer"
  IL_006b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0070:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LateMemberInvoke1Readonly()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        Dim c As New cls1
        x.goo(c.p)

        Console.WriteLine(c.p)
    End Sub

    Class cls1
        Public Sub goo(ByRef x As Integer)
            x += 1
        End Sub

        Private _p1 As Integer
        Public ReadOnly Property p() As Integer
            Get
                Console.WriteLine("Get")
                Return _p1
            End Get
        End Property
    End Class

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Get
Get
0]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  8
  .locals init (Program.cls1 V_0) //c
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  newobj     "Sub Program.cls1..ctor()"
  IL_000a:  stloc.0
  IL_000b:  ldnull
  IL_000c:  ldstr      "goo"
  IL_0011:  ldc.i4.1
  IL_0012:  newarr     "Object"
  IL_0017:  dup
  IL_0018:  ldc.i4.0
  IL_0019:  ldloc.0
  IL_001a:  callvirt   "Function Program.cls1.get_p() As Integer"
  IL_001f:  box        "Integer"
  IL_0024:  stelem.ref
  IL_0025:  ldnull
  IL_0026:  ldnull
  IL_0027:  ldnull
  IL_0028:  ldc.i4.1
  IL_0029:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_002e:  pop
  IL_002f:  ldloc.0
  IL_0030:  callvirt   "Function Program.cls1.get_p() As Integer"
  IL_0035:  call       "Sub System.Console.WriteLine(Integer)"
  IL_003a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LateMemberInvokeLateBound()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        Dim c As Object = New cls1
        x.goo(c.p)

        Console.WriteLine(c.p)
    End Sub


    Class cls1
        Public Sub goo(ByRef x As Integer)
            x += 1
        End Sub

        Private _p1 As Integer
        Public Property p() As Integer
            Get
                Console.WriteLine("Get")
                Return _p1
            End Get
            Set(value As Integer)
                Console.WriteLine("Set")
                _p1 = value
            End Set
        End Property
    End Class

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Get
Set
Get
1]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      137 (0x89)
  .maxstack  13
  .locals init (Object V_0, //c
  Object V_1,
  Object() V_2,
  Boolean() V_3)
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  newobj     "Sub Program.cls1..ctor()"
  IL_000a:  stloc.0
  IL_000b:  ldnull
  IL_000c:  ldstr      "goo"
  IL_0011:  ldc.i4.1
  IL_0012:  newarr     "Object"
  IL_0017:  dup
  IL_0018:  ldc.i4.0
  IL_0019:  ldloc.0
  IL_001a:  stloc.1
  IL_001b:  ldloc.1
  IL_001c:  ldnull
  IL_001d:  ldstr      "p"
  IL_0022:  ldc.i4.0
  IL_0023:  newarr     "Object"
  IL_0028:  ldnull
  IL_0029:  ldnull
  IL_002a:  ldnull
  IL_002b:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_0030:  stelem.ref
  IL_0031:  dup
  IL_0032:  stloc.2
  IL_0033:  ldnull
  IL_0034:  ldnull
  IL_0035:  ldc.i4.1
  IL_0036:  newarr     "Boolean"
  IL_003b:  dup
  IL_003c:  ldc.i4.0
  IL_003d:  ldc.i4.1
  IL_003e:  stelem.i1
  IL_003f:  dup
  IL_0040:  stloc.3
  IL_0041:  ldc.i4.1
  IL_0042:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_0047:  pop
  IL_0048:  ldloc.3
  IL_0049:  ldc.i4.0
  IL_004a:  ldelem.u1
  IL_004b:  brfalse.s  IL_0069
  IL_004d:  ldloc.1
  IL_004e:  ldnull
  IL_004f:  ldstr      "p"
  IL_0054:  ldc.i4.1
  IL_0055:  newarr     "Object"
  IL_005a:  dup
  IL_005b:  ldc.i4.0
  IL_005c:  ldloc.2
  IL_005d:  ldc.i4.0
  IL_005e:  ldelem.ref
  IL_005f:  stelem.ref
  IL_0060:  ldnull
  IL_0061:  ldnull
  IL_0062:  ldc.i4.1
  IL_0063:  ldc.i4.0
  IL_0064:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateSetComplex(Object, System.Type, String, Object(), String(), System.Type(), Boolean, Boolean)"
  IL_0069:  ldloc.0
  IL_006a:  ldnull
  IL_006b:  ldstr      "p"
  IL_0070:  ldc.i4.0
  IL_0071:  newarr     "Object"
  IL_0076:  ldnull
  IL_0077:  ldnull
  IL_0078:  ldnull
  IL_0079:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_007e:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0083:  call       "Sub System.Console.WriteLine(Object)"
  IL_0088:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LateMemberInvokeLateBoundReadonly()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        Dim c As Object = New cls1
        x.goo(c.p)

        Console.WriteLine(c.p)
    End Sub


    Class cls1
        Public Sub goo(ByRef x As Integer)
            x += 1
        End Sub

        Private _p1 As Integer
        Public ReadOnly Property p() As Integer
            Get
                Console.WriteLine("Get")
                Return _p1
            End Get
        End Property
    End Class

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Get
Get
0]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      137 (0x89)
  .maxstack  13
  .locals init (Object V_0, //c
  Object V_1,
  Object() V_2,
  Boolean() V_3)
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  newobj     "Sub Program.cls1..ctor()"
  IL_000a:  stloc.0
  IL_000b:  ldnull
  IL_000c:  ldstr      "goo"
  IL_0011:  ldc.i4.1
  IL_0012:  newarr     "Object"
  IL_0017:  dup
  IL_0018:  ldc.i4.0
  IL_0019:  ldloc.0
  IL_001a:  stloc.1
  IL_001b:  ldloc.1
  IL_001c:  ldnull
  IL_001d:  ldstr      "p"
  IL_0022:  ldc.i4.0
  IL_0023:  newarr     "Object"
  IL_0028:  ldnull
  IL_0029:  ldnull
  IL_002a:  ldnull
  IL_002b:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_0030:  stelem.ref
  IL_0031:  dup
  IL_0032:  stloc.2
  IL_0033:  ldnull
  IL_0034:  ldnull
  IL_0035:  ldc.i4.1
  IL_0036:  newarr     "Boolean"
  IL_003b:  dup
  IL_003c:  ldc.i4.0
  IL_003d:  ldc.i4.1
  IL_003e:  stelem.i1
  IL_003f:  dup
  IL_0040:  stloc.3
  IL_0041:  ldc.i4.1
  IL_0042:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_0047:  pop
  IL_0048:  ldloc.3
  IL_0049:  ldc.i4.0
  IL_004a:  ldelem.u1
  IL_004b:  brfalse.s  IL_0069
  IL_004d:  ldloc.1
  IL_004e:  ldnull
  IL_004f:  ldstr      "p"
  IL_0054:  ldc.i4.1
  IL_0055:  newarr     "Object"
  IL_005a:  dup
  IL_005b:  ldc.i4.0
  IL_005c:  ldloc.2
  IL_005d:  ldc.i4.0
  IL_005e:  ldelem.ref
  IL_005f:  stelem.ref
  IL_0060:  ldnull
  IL_0061:  ldnull
  IL_0062:  ldc.i4.1
  IL_0063:  ldc.i4.0
  IL_0064:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateSetComplex(Object, System.Type, String, Object(), String(), System.Type(), Boolean, Boolean)"
  IL_0069:  ldloc.0
  IL_006a:  ldnull
  IL_006b:  ldstr      "p"
  IL_0070:  ldc.i4.0
  IL_0071:  newarr     "Object"
  IL_0076:  ldnull
  IL_0077:  ldnull
  IL_0078:  ldnull
  IL_0079:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_007e:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0083:  call       "Sub System.Console.WriteLine(Object)"
  IL_0088:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LateRedim()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New Integer(5) {}
        ReDim x(10)

        Console.WriteLine(x.Length)
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[11]]>)
        End Sub

        <Fact()>
        Public Sub LateMemberLateBound2levels()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        Dim c As Object = New cls1
        Dim v As Object = 1
        x.goo(c.p(v))

        Console.WriteLine(c.p(1))
        Console.WriteLine(v)
    End Sub


    Class cls1
        Public Sub goo(ByRef x As Integer)
            x += 1
        End Sub

        Private _p1 As Integer
        Public Property p(x As Integer) As Integer
            Get
                Console.WriteLine("Get")
                Return _p1 + x
            End Get
            Set(value As Integer)
                Console.WriteLine("Set")
                _p1 = value + x
            End Set
        End Property
    End Class

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Get
Set
Get
4
1]]>)
        End Sub

        <Fact()>
        Public Sub LateMemberLateBound2levelsByVal()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        Dim c As Object = New cls1
        Dim v As Object = 1
        x.goo((c.p(v)))

        Console.WriteLine(c.p(1))
        Console.WriteLine(v)
    End Sub


    Class cls1
        Public Sub goo(ByRef x As Integer)
            x += 1
        End Sub

        Private _p1 As Integer
        Public Property p(x As Integer) As Integer
            Get
                Console.WriteLine("Get")
                Return _p1 + x
            End Get
            Set(value As Integer)
                Console.WriteLine("Set")
                _p1 = value + x
            End Set
        End Property
    End Class

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Get
Get
1
1]]>)
        End Sub

        <Fact()>
        Public Sub LateMemberLateBound2levelsCompound()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        Dim c As Object = New cls1
        Dim v As Object = 1
        x.p(c.p(v)) += 1

        Console.WriteLine(x.p(1))
        Console.WriteLine(c.p(1))
        Console.WriteLine(v)
    End Sub


    Class cls1
        Public Sub goo(ByRef x As Integer)
            x += 1
        End Sub

        Private _p1 As Integer
        Public Property p(x As Integer) As Integer
            Get
                Console.WriteLine("Get")
                Return _p1 + x
            End Get
            Set(value As Integer)
                Console.WriteLine("Set")
                _p1 = value + x
            End Set
        End Property
    End Class

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Get
Get
Set
Get
4
Get
1
1]]>)
        End Sub

        <Fact()>
        Public Sub LateMemberLateBound2levelsCompoundProp()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        Dim c As New cls1
        Dim v As Object = 1
        x.p(c.p(v)) += 1

        Console.WriteLine(x.p(1))
        Console.WriteLine(c.p(1))
        Console.WriteLine(v)
    End Sub


    Class cls1
        Public Sub goo(ByRef x As Integer)
            x += 1
        End Sub

        Private _p1 As Integer
        Public Property p(x As Integer) As Integer
            Get
                Console.WriteLine("Get")
                Return _p1 + x
            End Get
            Set(value As Integer)
                Console.WriteLine("Set")
                _p1 = value + x
            End Set
        End Property
    End Class

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Get
Get
Set
Get
4
Get
1
1]]>)
        End Sub

        <Fact()>
        Public Sub LateMemberLateBound2levels1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        Dim c As Object = New cls1
        Dim v As Object = 1
        x.goo(c.goo(v))

        Console.WriteLine(c.p(1))
        Console.WriteLine(v)
    End Sub


    Class cls1
        Public Sub goo(ByRef x As Integer)
            Console.WriteLine("goo")
            x += 1
        End Sub

        Private _p1 As Integer
        Public Property p(x As Integer) As Integer
            Get
                Console.WriteLine("Get")
                Return _p1 + x
            End Get
            Set(value As Integer)
                Console.WriteLine("Set")
                _p1 = value + x
            End Set
        End Property
    End Class

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[goo
goo
Get
1
2]]>)
        End Sub

        <Fact()>
        Public Sub LateMemberLateBound2levels2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program

    Sub Main()
        Dim x As Object = New cls1
        Dim c As Object = New cls1
        Dim v As Object = 1
        Dim v1 As Object = 5

        x.goo(c.p(v), c.p(v))
        x.goo(v1, v1)

        Console.WriteLine(c.p(1))
        Console.WriteLine(v)
        Console.WriteLine(v1)
    End Sub


    Class cls1
        Public Sub goo(ByRef x As Integer, ByRef y As Integer)
            x += 1
            y += 1
        End Sub

        Private _p1 As Integer
        Public Property p(x As Integer) As Integer
            Get
                Console.WriteLine("Get")
                Return _p1 + x
            End Get
            Set(value As Integer)
                Console.WriteLine("Set")
                _p1 = value + x
            End Set
        End Property
    End Class

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Get
Get
Set
Set
Get
4
1
6]]>)
        End Sub


        <Fact()>
        Public Sub LateMemberArgConvert()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

imports system

Module Program
    Sub Main()
        Dim obj As Object = New cls1
        goo(obj.moo)
    End Sub

    Sub goo(byref x As Integer)
        Console.WriteLine(x)
    End Sub

    Public Class cls1
        Public Function moo() As Integer
            Return 42
        End Function
    End Class
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[42]]>)
        End Sub

        <Fact()>
        Public Sub Bug257437Legacy()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Class Bug257437
    Shared Result As Integer

    Shared Sub goo(ByVal i As Integer, ByVal b As Byte)
        Result = 1
    End Sub

    Shared Sub goo(ByVal i As Integer, ByVal b As Int16)
        Result = 2
    End Sub

    Shared Sub goo(ByVal i As Integer, ByVal b As Int32)
        Result = 3
    End Sub

    Shared Sub goo(ByVal i As Integer, ByVal b As String, Optional ByVal x As Integer = 1)
        Result = 4
    End Sub

    Shared Sub Main()
        Console.WriteLine("*** Bug 257437")
        Try
            Dim fnum

            Console.Write("   1) ")
            goo(fnum, CByte(255))
            PassFail(Result = 1)

            Console.Write("   2) ")
            goo(fnum, -1S)
            PassFail(Result = 2)

            Console.Write("   3) ")
            goo(fnum, -1I)
            PassFail(Result = 3)

            Console.Write("   4) ")
            goo(fnum, "abc")
            PassFail(Result = 4)
        Catch ex As Exception
            Failed(ex)
        End Try
    End Sub

End Class

Module TestHarness
    Sub Failed(ByVal ex As Exception)
        If ex Is Nothing Then
            Console.WriteLine("NULL System.Exception")
        Else
            Console.WriteLine(ex.GetType().FullName)
        End If
        Console.WriteLine(ex.Message)
        Console.WriteLine(ex.StackTrace)
        Console.WriteLine("FAILED !!!")
    End Sub

    Sub Failed()
        Console.WriteLine("FAILED !!!")
    End Sub

    Sub Passed()
        Console.WriteLine("passed")
    End Sub

    Sub PassFail(ByVal bPassed As Boolean)
        If bPassed Then
            Console.WriteLine("passed")
        Else
            Console.WriteLine("FAILED !!!")
        End If
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[*** Bug 257437
   1) passed
   2) passed
   3) passed
   4) passed]]>)
        End Sub

        <Fact()>
        Public Sub Bug168135Legacy()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Bug168135
    'This tests setting/getting of array fields
    Sub Main()
        Dim bFailed As Boolean

        Console.WriteLine("Regression test Bug168135")
        Try
            Dim o As Object = New Class1()
            If o.Ary(0) &lt;&gt; 6 Then
                Console.WriteLine("Bug168135: FAILED step 1a")
                bFailed = True
            End If
            o.Ary(4) = 11  'this is causing an unexpected MissingMethodException
            If o.Ary(4) &lt;&gt; 11 Then
                Console.WriteLine("Bug168135: FAILED step 1b")
                bFailed = True
            End If
        Catch ex As Exception
            Console.WriteLine(ex.GetType().Name &amp; ": " &amp; ex.Message)
            Console.WriteLine("Bug168135: FAILED step 1c")
            bFailed = True
        End Try

        Try
            Dim o As Object = New Class1()
            If o.ObjectValue(0) &lt;&gt; 1 Then
                Console.WriteLine("Bug168135: FAILED step 2a")
                bFailed = True
            End If
            o.ObjectValue(4) = 6  'this is causing an unexpected MissingMethodException
            If o.ObjectValue(4) &lt;&gt; 6 Then
                Console.WriteLine("Bug168135: FAILED step 2b")
                bFailed = True
            End If
        Catch ex As Exception
            Failed(ex)
            Console.WriteLine("Bug168135: FAILED step 2c")
            bFailed = True
        End Try

        Try
            Dim o As Object = New Class1()
            If o(0) &lt;&gt; "A" Then
                Console.WriteLine("Bug168135: FAILED step 3a")
                bFailed = True
            End If
            o(4) = "X"  'this is causing an unexpected MissingMethodException
            If o(4) &lt;&gt; "X" Then
                Console.WriteLine("Bug168135: FAILED step 3b")
                bFailed = True
            End If
        Catch ex As Exception
            Failed(ex)
            Console.WriteLine("Bug168135: FAILED step 3c")
            bFailed = True
        End Try

        If Not bFailed Then
            Console.WriteLine("Bug168135: PASSED")
        End If
    End Sub
End Module

Public Class Class1
    Private m_default() As String

    Private Shared PrivateSharedValue As Integer = 12
    Protected Shared ProtectedSharedValue As Integer = 23
    Public Shared PublicSharedValue As Integer = 12345

    Sub New()
        ObjectValue = New Integer() {1, 2, 3, 4, 5}
        m_default = New String() {"A", "B", "C", "D", "E"}
        Ary = New Integer() {6, 7, 8, 9, 10}
    End Sub

    Public Ary(4) As Integer
    Public ObjectValue As Object
    Public ShortValue As Short
    Public IntegerValue As Integer
    Public LongValue As Long
    Public SingleValue As Single
    Public DoubleValue As Double
    Public DateValue As Date
    Public DecimalValue As Decimal
    Public StringValue As String

    Private PrivateValue As String

    Protected Property ProtectedProp() As Integer
        Get
            Return 12345
        End Get
        Set(ByVal Value As Integer)
            If Value &lt;&gt; 12345 Then
            Throw New ArgumentException("Argument was not correct")
            End If
        End Set
    End Property

        Default Public Property DefaultProp(ByVal Index As Integer) As String
            Get
                Return m_default(Index)
            End Get
            Set(ByVal Value As String)
                m_default(Index) = Value
            End Set
        End Property

        Friend Declare Ansi Function AnsiStrFunction Lib "DeclExtNightly001.DLL" Alias "StrFunction" (ByVal Arg As String, ByVal Arg1 As Integer, ByVal Arg2 As Integer, ByVal Arg3 As Boolean) As Boolean
        Public Declare Sub MsgBeep Lib "user32.DLL" Alias "MessageBeep" (Optional ByVal x As Integer = 0)


    End Class


    Module TestHarness
        Sub Failed(ByVal ex As Exception)
            If ex Is Nothing Then
                Console.WriteLine("NULL System.Exception")
            Else
                Console.WriteLine(ex.GetType().FullName)
            End If
            Console.WriteLine(ex.Message)
            Console.WriteLine(ex.StackTrace)
            Console.WriteLine("FAILED !!!")
        End Sub

        Sub Failed()
            Console.WriteLine("FAILED !!!")
        End Sub

        Sub Passed()
            Console.WriteLine("passed")
        End Sub

        Sub PassFail(ByVal bPassed As Boolean)
            If bPassed Then
                Console.WriteLine("passed")
            Else
                Console.WriteLine("FAILED !!!")
            End If
        End Sub
    End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Regression test Bug168135
Bug168135: PASSED]]>)
        End Sub

        <Fact()>
        Public Sub Bug302246Legacy()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Bug302246
    Private m_i As Integer
    Private m_ArgInteger As Integer
    Private m_Arg2 As Integer
    Private m_ArgString As Integer

    Public Class Class1
        Public Sub goo(ByVal Arg As Integer, ByVal Arg2 As Integer) ', Optional ByVal Arg2 As Long = 40)
            m_i = 1
            m_ArgInteger = Arg
            m_Arg2 = Arg2
        End Sub

        Public Sub Goo(ByVal Arg2 As Integer, ByVal Arg As String)
            m_i = 2
            m_ArgString = Arg
            m_Arg2 = Arg2
        End Sub
    End Class


    Sub Main()

        Console.Write("Bug 302246: ")

        Try
            Dim iEarly As Integer
            Dim c As New Class1()
            Dim o As Object = c

            m_i = -1
            c.goo(40, Arg:=50)
            iEarly = m_i

            m_i = -1
            o.goo(40, Arg:=50)  'this late bound case throws an unexpected exception - BUG
            PassFail(m_i = iEarly AndAlso m_ArgString = "50" AndAlso m_Arg2 = 40)

        Catch ex As Exception
            Failed(ex)
        End Try

    End Sub
End Module

Module TestHarness
    Sub Failed(ByVal ex As Exception)
        If ex Is Nothing Then
            Console.WriteLine("NULL System.Exception")
        Else
            Console.WriteLine(ex.GetType().FullName)
        End If
        Console.WriteLine(ex.Message)
        Console.WriteLine(ex.StackTrace)
        Console.WriteLine("FAILED !!!")
    End Sub

    Sub Failed()
        Console.WriteLine("FAILED !!!")
    End Sub

    Sub Passed()
        Console.WriteLine("passed")
    End Sub

    Sub PassFail(ByVal bPassed As Boolean)
        If bPassed Then
            Console.WriteLine("passed")
        Else
            Console.WriteLine("FAILED !!!")
        End If
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Bug 302246: passed]]>)
        End Sub

        <Fact()>
        Public Sub Bug231364Legacy()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Bug231364

    Delegate Sub goo(ByRef x As Short, ByRef y As Long)
    Sub goo1(ByRef x As Short, Optional ByRef y As Long = 0)
        y = 8 / 2
    End Sub

    Sub Main()
        Console.Write("Bug 231364: ")
        Try
            Dim var As Object

            var = New goo(AddressOf goo1)

            var2 = 8
            var3 = 10

            var.Invoke(var3, y:=var2)
            PassFail(var2 = 4)

        Catch ex As Exception
            Failed(ex)
        End Try
    End Sub

    Private _value2 As Long
    Private Property var2 As Long
        Get
            Console.WriteLine("GetVar2")
            Return _value2
        End Get
        Set(value As Long)
            Console.WriteLine("SetVar2")
            _value2 = value
        End Set
    End Property

    Private _value3 As Long
    Private Property var3 As Long
        Get
            Console.WriteLine("GetVar3")
            Return _value3
        End Get
        Set(value As Long)
            Console.WriteLine("SetVar3")
            _value3 = value
        End Set
    End Property

End Module

Module TestHarness
    Sub Failed(ByVal ex As Exception)
        If ex Is Nothing Then
            Console.WriteLine("NULL System.Exception")
        Else
            Console.WriteLine(ex.GetType().FullName)
        End If
        Console.WriteLine(ex.Message)
        Console.WriteLine(ex.StackTrace)
        Console.WriteLine("FAILED !!!")
    End Sub

    Sub Failed()
        Console.WriteLine("FAILED !!!")
    End Sub

    Sub Passed()
        Console.WriteLine("passed")
    End Sub

    Sub PassFail(ByVal bPassed As Boolean)
        If bPassed Then
            Console.WriteLine("passed")
        Else
            Console.WriteLine("FAILED !!!")
        End If
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Bug 231364: SetVar2
SetVar3
GetVar3
GetVar2
SetVar3
SetVar2
GetVar2
passed]]>)
        End Sub

        <Fact()>
        Public Sub OverflowInCopyback()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Program1

    Delegate Sub goo(ByRef x As Short, ByRef y As Long)
    Sub goo1(ByRef x As Short, Optional ByRef y As Long = 0)
        y = 8 / 2
        x = -1
    End Sub

    Sub Main()
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Dim var As Object
            var = New goo(AddressOf goo1)

            Dim var2 = 8
            Dim var3 As ULong = 10

            var.Invoke(var3, y:=var2)
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        Finally
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[Arithmetic operation resulted in an overflow.]]>)
        End Sub

        <Fact()>
        Public Sub LateCallMissing()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System

Module Program
    Sub Main()
        Dim obj As Object = New cls1
        obj.goo(, y:=obj.goo(, ))
    End Sub

    Class cls1
        Shared Sub goo(Optional x As Integer = 1, Optional y As Integer = 2)
            Console.WriteLine(x + y)
        End Sub
    End Class
End Module


    </file>
</compilation>,
expectedOutput:=<![CDATA[3
1]]>).
            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      161 (0xa1)
  .maxstack  13
  .locals init (Object V_0, //obj
  Object V_1,
  Object V_2,
  Object V_3,
  Object() V_4,
  Boolean() V_5,
  Object() V_6)
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldnull
  IL_0008:  ldstr      "goo"
  IL_000d:  ldc.i4.2
  IL_000e:  newarr     "Object"
  IL_0013:  stloc.s    V_6
  IL_0015:  ldloc.s    V_6
  IL_0017:  ldc.i4.1
  IL_0018:  ldsfld     "System.Reflection.Missing.Value As System.Reflection.Missing"
  IL_001d:  stelem.ref
  IL_001e:  ldloc.s    V_6
  IL_0020:  ldc.i4.0
  IL_0021:  ldloc.0
  IL_0022:  stloc.1
  IL_0023:  ldloc.1
  IL_0024:  ldnull
  IL_0025:  ldstr      "goo"
  IL_002a:  ldc.i4.2
  IL_002b:  newarr     "Object"
  IL_0030:  dup
  IL_0031:  ldc.i4.0
  IL_0032:  ldsfld     "System.Reflection.Missing.Value As System.Reflection.Missing"
  IL_0037:  dup
  IL_0038:  stloc.2
  IL_0039:  stelem.ref
  IL_003a:  dup
  IL_003b:  ldc.i4.1
  IL_003c:  ldsfld     "System.Reflection.Missing.Value As System.Reflection.Missing"
  IL_0041:  dup
  IL_0042:  stloc.3
  IL_0043:  stelem.ref
  IL_0044:  ldnull
  IL_0045:  ldnull
  IL_0046:  ldnull
  IL_0047:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_004c:  stelem.ref
  IL_004d:  ldloc.s    V_6
  IL_004f:  dup
  IL_0050:  stloc.s    V_4
  IL_0052:  ldc.i4.1
  IL_0053:  newarr     "String"
  IL_0058:  dup
  IL_0059:  ldc.i4.0
  IL_005a:  ldstr      "y"
  IL_005f:  stelem.ref
  IL_0060:  ldnull
  IL_0061:  ldc.i4.2
  IL_0062:  newarr     "Boolean"
  IL_0067:  dup
  IL_0068:  ldc.i4.0
  IL_0069:  ldc.i4.1
  IL_006a:  stelem.i1
  IL_006b:  dup
  IL_006c:  stloc.s    V_5
  IL_006e:  ldc.i4.1
  IL_006f:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_0074:  pop
  IL_0075:  ldloc.s    V_5
  IL_0077:  ldc.i4.0
  IL_0078:  ldelem.u1
  IL_0079:  brfalse.s  IL_00a0
  IL_007b:  ldloc.1
  IL_007c:  ldnull
  IL_007d:  ldstr      "goo"
  IL_0082:  ldc.i4.3
  IL_0083:  newarr     "Object"
  IL_0088:  dup
  IL_0089:  ldc.i4.0
  IL_008a:  ldloc.2
  IL_008b:  stelem.ref
  IL_008c:  dup
  IL_008d:  ldc.i4.1
  IL_008e:  ldloc.3
  IL_008f:  stelem.ref
  IL_0090:  dup
  IL_0091:  ldc.i4.2
  IL_0092:  ldloc.s    V_4
  IL_0094:  ldc.i4.0
  IL_0095:  ldelem.ref
  IL_0096:  stelem.ref
  IL_0097:  ldnull
  IL_0098:  ldnull
  IL_0099:  ldc.i4.1
  IL_009a:  ldc.i4.0
  IL_009b:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateSetComplex(Object, System.Type, String, Object(), String(), System.Type(), Boolean, Boolean)"
  IL_00a0:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub LateAddressOfTrueClosure()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System
    
Class C
    Dim _id As Integer

    Sub New(id As Integer)
        _id = id
    End Sub

    Function G(x As C) As C
        Console.WriteLine(x._id)
        Return x
    End Function

    Sub goo(x As Integer, y As Integer)
    End Sub
End Class

Module Program
    Sub Main()
        Dim obj0 As Object = New C(1)
        Dim obj1 As Object = New C(2)

        Dim o As Action(Of Byte, Integer) = AddressOf obj0.G(obj1).goo

        obj1 = New C(5)
        o(1, 2)
    End Sub
End Module
    </file>
</compilation>

            Dim c = CompileAndVerify(source, expectedOutput:="5", options:=TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator:=
                Sub(m)
                    Dim closure = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Program._Closure$__0-0")

                    AssertEx.Equal(
                    {
                        "Public $VB$Local_obj0 As Object",
                        "Public $VB$Local_obj1 As Object",
                        "Public Sub New()",
                        "Friend Sub _Lambda$__0(a0 As Byte, a1 As Integer)"
                    }, closure.GetMembers().Select(Function(x) x.ToString()))
                End Sub)
        End Sub

        <Fact()>
        Public Sub LateAddressOf()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System

Module Program
    Sub Main()
        Dim obj As Object = New cls1

        Dim o As Action(Of Integer, Integer) = AddressOf obj.goo

        o(1, 2)
    End Sub

    Class cls1
        Shared Sub goo(x As Integer, y As Integer)
            Console.WriteLine(x + y)
        End Sub
    End Class
End Module

    </file>
</compilation>,
expectedOutput:="3")

            c.VerifyIL("Program._Closure$__0-0._Lambda$__0",
            <![CDATA[
{
  // Code size      134 (0x86)
  .maxstack  10
  .locals init (Object() V_0,
  Boolean() V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program._Closure$__0-0.$VB$Local_obj As Object"
  IL_0006:  ldnull
  IL_0007:  ldstr      "goo"
  IL_000c:  ldc.i4.2
  IL_000d:  newarr     "Object"
  IL_0012:  dup
  IL_0013:  ldc.i4.0
  IL_0014:  ldarg.1
  IL_0015:  box        "Integer"
  IL_001a:  stelem.ref
  IL_001b:  dup
  IL_001c:  ldc.i4.1
  IL_001d:  ldarg.2
  IL_001e:  box        "Integer"
  IL_0023:  stelem.ref
  IL_0024:  dup
  IL_0025:  stloc.0
  IL_0026:  ldnull
  IL_0027:  ldnull
  IL_0028:  ldc.i4.2
  IL_0029:  newarr     "Boolean"
  IL_002e:  dup
  IL_002f:  ldc.i4.0
  IL_0030:  ldc.i4.1
  IL_0031:  stelem.i1
  IL_0032:  dup
  IL_0033:  ldc.i4.1
  IL_0034:  ldc.i4.1
  IL_0035:  stelem.i1
  IL_0036:  dup
  IL_0037:  stloc.1
  IL_0038:  ldc.i4.1
  IL_0039:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_003e:  pop
  IL_003f:  ldloc.1
  IL_0040:  ldc.i4.0
  IL_0041:  ldelem.u1
  IL_0042:  brfalse.s  IL_0062
  IL_0044:  ldloc.0
  IL_0045:  ldc.i4.0
  IL_0046:  ldelem.ref
  IL_0047:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_004c:  ldtoken    "Integer"
  IL_0051:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0056:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ChangeType(Object, System.Type) As Object"
  IL_005b:  unbox.any  "Integer"
  IL_0060:  starg.s    V_1
  IL_0062:  ldloc.1
  IL_0063:  ldc.i4.1
  IL_0064:  ldelem.u1
  IL_0065:  brfalse.s  IL_0085
  IL_0067:  ldloc.0
  IL_0068:  ldc.i4.1
  IL_0069:  ldelem.ref
  IL_006a:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_006f:  ldtoken    "Integer"
  IL_0074:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0079:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ChangeType(Object, System.Type) As Object"
  IL_007e:  unbox.any  "Integer"
  IL_0083:  starg.s    V_2
  IL_0085:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LateAddressOfRef()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Imports System

Module Program

    Delegate Sub d1(ByRef x As Integer, y As Integer)

    Sub Main()
        Dim obj As Object = New cls1

        Dim o As d1 = AddressOf obj.goo

        Dim l As Integer = 0
        o(l, 2)

        Console.WriteLine(l)
    End Sub

    Class cls1
        Shared Sub goo(ByRef x As Integer, y As Integer)
            x = 42
            Console.WriteLine(x + y)
        End Sub
    End Class
End Module


    </file>
</compilation>,
expectedOutput:=<![CDATA[44
42]]>)
        End Sub

        <Fact()>
        Public Sub Regress14733()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
        Dim obj As Object = New C3()
        Try
            obj.x()
        Catch e As MissingMemberException
        End Try
    End Sub
End Module

Class C1
    Overridable Sub x()
        Console.WriteLine("True")
    End Sub
End Class

Class C2
    Inherits C1
    Overridable Shadows Sub x(ByVal i As Integer)
    End Sub
End Class

Class C3
    Inherits C2
    Overrides Sub x(ByVal i As Integer)
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[True]]>)
        End Sub


        <Fact()>
        Public Sub Regress14991()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Class C1
        Shared Widening Operator CType(ByVal arg As Integer) As C1
            Return New C1
        End Operator
        Shared Widening Operator CType(ByVal arg As C1) As Integer
            Return 0
        End Operator
    End Class
    Dim c1Obj As New C1
    Class C2
        Sub goo(Of T)(ByRef x As T)
            x = Nothing
        End Sub
    End Class
 
    Sub Main()
        Dim c2Obj As New C2
        Dim obj As Object = c2Obj
        obj.goo(Of Integer)(c1Obj)
    End Sub
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[]]>).
            VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size      105 (0x69)
  .maxstack  10
  .locals init (Object() V_0,
  Boolean() V_1)
  IL_0000:  newobj     "Sub Module1.C2..ctor()"
  IL_0005:  ldnull
  IL_0006:  ldstr      "goo"
  IL_000b:  ldc.i4.1
  IL_000c:  newarr     "Object"
  IL_0011:  dup
  IL_0012:  ldc.i4.0
  IL_0013:  ldsfld     "Module1.c1Obj As Module1.C1"
  IL_0018:  stelem.ref
  IL_0019:  dup
  IL_001a:  stloc.0
  IL_001b:  ldnull
  IL_001c:  ldc.i4.1
  IL_001d:  newarr     "System.Type"
  IL_0022:  dup
  IL_0023:  ldc.i4.0
  IL_0024:  ldtoken    "Integer"
  IL_0029:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_002e:  stelem.ref
  IL_002f:  ldc.i4.1
  IL_0030:  newarr     "Boolean"
  IL_0035:  dup
  IL_0036:  ldc.i4.0
  IL_0037:  ldc.i4.1
  IL_0038:  stelem.i1
  IL_0039:  dup
  IL_003a:  stloc.1
  IL_003b:  ldc.i4.1
  IL_003c:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_0041:  pop
  IL_0042:  ldloc.1
  IL_0043:  ldc.i4.0
  IL_0044:  ldelem.u1
  IL_0045:  brfalse.s  IL_0068
  IL_0047:  ldloc.0
  IL_0048:  ldc.i4.0
  IL_0049:  ldelem.ref
  IL_004a:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_004f:  ldtoken    "Module1.C1"
  IL_0054:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0059:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ChangeType(Object, System.Type) As Object"
  IL_005e:  castclass  "Module1.C1"
  IL_0063:  stsfld     "Module1.c1Obj As Module1.C1"
  IL_0068:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Regress15196()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
imports system
Module Test
    Sub Main()
        Dim a = New class1
        Dim O As Object = 5S
        a.Bb(O)
    End Sub

    Friend Class class1
        Public Overridable Sub Bb(ByRef y As String)
            Console.WriteLine("string")
        End Sub

        Public Overridable Sub BB(ByRef y As Short)
            Console.WriteLine("short")
        End Sub
    End Class
End Module

    </file>
</compilation>,
expectedOutput:=<![CDATA[short]]>)
        End Sub

        <WorkItem(546467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546467")>
        <Fact()>
        Public Sub Bug15939_1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        I0.GoHome()
        I1.GoHome()
        I2.GoHome()
        I3.GoHome()
    End Sub

    &lt;InterfaceType(CType(3, ComInterfaceType))&gt;
    Interface II0
    End Interface
    Property I0 As II0

    &lt;InterfaceType(ComInterfaceType.InterfaceIsDual)&gt;
    Interface II1
    End Interface
    Property I1 As II1

    &lt;InterfaceType(ComInterfaceType.InterfaceIsIUnknown)&gt;
    Interface II2
    End Interface
    Property I2 As II2

    &lt;InterfaceType(ComInterfaceType.InterfaceIsIDispatch)&gt;
    Interface II3
    End Interface
    Property I3 As II3
End Module
    </file>
</compilation>).VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_NameNotMember2, "I1.GoHome").WithArguments("GoHome", "Module1.II1"),
                    Diagnostic(ERRID.ERR_NameNotMember2, "I2.GoHome").WithArguments("GoHome", "Module1.II2"))
        End Sub

        <WorkItem(546467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546467")>
        <Fact()>
        Public Sub Bug15939_2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        I0.GoHome()
        I1.GoHome()
        I2.GoHome()
        I3.GoHome()
        I4.GoHome()
    End Sub

    &lt;TypeLibType(100000)&gt;
    Interface II0
    End Interface
    Property I0 As II0

    &lt;TypeLibType(1000000)&gt;
    Interface II1
    End Interface
    Property I1 As II1

    &lt;TypeLibType(0)&gt;
    Interface II2
    End Interface
    Property I2 As II2

    &lt;TypeLibType(TypeLibTypeFlags.FCanCreate)&gt;
    Interface II3
    End Interface
    Property I3 As II3

    &lt;TypeLibType(TypeLibTypeFlags.FCanCreate Or TypeLibTypeFlags.FNonExtensible)&gt;
    Interface II4
    End Interface
    Property I4 As II4
End Module
    </file>
</compilation>).VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_NameNotMember2, "I0.GoHome").WithArguments("GoHome", "Module1.II0"),
                    Diagnostic(ERRID.ERR_NameNotMember2, "I4.GoHome").WithArguments("GoHome", "Module1.II4"))
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub Regress14722()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

imports system
imports Microsoft.VisualBasic

Friend Module Module1
    Sub Main()
        Dim Res
        Dim clt As Collection = New Collection()
        clt.Add("Roslyn", "RsKey")

        Dim embclt As Collection = New Collection()
        embclt.Add(clt, "MyKey")
        'Try a Get
        Res = embclt!MyKey!RsKey
        Console.WriteLine(Res)
    End Sub
End Module


    </file>
</compilation>,
expectedOutput:=<![CDATA[Roslyn]]>)
        End Sub

        <Fact()>
        Public Sub Regress17205()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()
        Dim x = New C
        Dim x1 = New C(123)
    End Sub

End Module

Class C
    Sub New()
        Me.New(Goo(CObj(42)))
    End Sub

    Sub New(a As Integer)
        Me.New(Sub(x) Goo(x))
    End Sub

    Sub New(x As Action(Of Object))
        x(777)
    End Sub

    Sub New(x As Object)
    End Sub

    Sub Goo(x As String)
    End Sub

    Shared Sub Goo(x As Integer)
        Console.WriteLine(x)
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:=<![CDATA[42
777]]>)
        End Sub

        <Fact(), WorkItem(531546, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531546")>
        Public Sub Bug18273()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class VBIDO
    Public Property Item2(Optional ByVal p1 As String = "", Optional ByVal p2 As Integer = 0) As Long
        Get
            Return 0
        End Get
        Set(ByVal value As Long)
            System.Console.WriteLine(p1)
            System.Console.WriteLine(p2)
            System.Console.WriteLine(value)
        End Set
    End Property
End Class

Module Program
    Sub Main()
        Dim o As Object = New VBIDO
        o.Item2("hello", p2:="1") = 2
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=
            <![CDATA[
hello
1
2
]]>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       63 (0x3f)
  .maxstack  8
  IL_0000:  newobj     "Sub VBIDO..ctor()"
  IL_0005:  ldnull
  IL_0006:  ldstr      "Item2"
  IL_000b:  ldc.i4.3
  IL_000c:  newarr     "Object"
  IL_0011:  dup
  IL_0012:  ldc.i4.1
  IL_0013:  ldstr      "hello"
  IL_0018:  stelem.ref
  IL_0019:  dup
  IL_001a:  ldc.i4.0
  IL_001b:  ldstr      "1"
  IL_0020:  stelem.ref
  IL_0021:  dup
  IL_0022:  ldc.i4.2
  IL_0023:  ldc.i4.2
  IL_0024:  box        "Integer"
  IL_0029:  stelem.ref
  IL_002a:  ldc.i4.1
  IL_002b:  newarr     "String"
  IL_0030:  dup
  IL_0031:  ldc.i4.0
  IL_0032:  ldstr      "p2"
  IL_0037:  stelem.ref
  IL_0038:  ldnull
  IL_0039:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateSet(Object, System.Type, String, Object(), String(), System.Type())"
  IL_003e:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(531546, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531546")>
        Public Sub Bug18273_2()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class VBIDO
    Public Sub Item2(Optional ByVal p1 As String = "", Optional ByVal p2 As Integer = 0)
        System.Console.WriteLine(p1)
        System.Console.WriteLine(p2)
    End Sub
End Class

Module Program
    Sub Main()
        Dim o As Object = New VBIDO
        o.Item2("hello", p2:="1")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=
            <![CDATA[
hello
1
]]>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  8
  IL_0000:  newobj     "Sub VBIDO..ctor()"
  IL_0005:  ldnull
  IL_0006:  ldstr      "Item2"
  IL_000b:  ldc.i4.2
  IL_000c:  newarr     "Object"
  IL_0011:  dup
  IL_0012:  ldc.i4.1
  IL_0013:  ldstr      "hello"
  IL_0018:  stelem.ref
  IL_0019:  dup
  IL_001a:  ldc.i4.0
  IL_001b:  ldstr      "1"
  IL_0020:  stelem.ref
  IL_0021:  ldc.i4.1
  IL_0022:  newarr     "String"
  IL_0027:  dup
  IL_0028:  ldc.i4.0
  IL_0029:  ldstr      "p2"
  IL_002e:  stelem.ref
  IL_002f:  ldnull
  IL_0030:  ldnull
  IL_0031:  ldc.i4.1
  IL_0032:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
  IL_0037:  pop
  IL_0038:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(531546, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531546")>
        Public Sub Bug18273_3()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class VBIDO
    Public Function Item2(Optional ByVal p1 As String = "", Optional ByVal p2 As Integer = 0) As Integer
        System.Console.WriteLine(p1)
        System.Console.WriteLine(p2)
        Return 2
    End Function
End Class

Module Program
    Sub Main()
        Dim o As Object = New VBIDO
        System.Console.WriteLine(o.Item2("hello", p2:="1"))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=
            <![CDATA[
hello
1
2
]]>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       65 (0x41)
  .maxstack  8
  IL_0000:  newobj     "Sub VBIDO..ctor()"
  IL_0005:  ldnull
  IL_0006:  ldstr      "Item2"
  IL_000b:  ldc.i4.2
  IL_000c:  newarr     "Object"
  IL_0011:  dup
  IL_0012:  ldc.i4.1
  IL_0013:  ldstr      "hello"
  IL_0018:  stelem.ref
  IL_0019:  dup
  IL_001a:  ldc.i4.0
  IL_001b:  ldstr      "1"
  IL_0020:  stelem.ref
  IL_0021:  ldc.i4.1
  IL_0022:  newarr     "String"
  IL_0027:  dup
  IL_0028:  ldc.i4.0
  IL_0029:  ldstr      "p2"
  IL_002e:  stelem.ref
  IL_002f:  ldnull
  IL_0030:  ldnull
  IL_0031:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_0036:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_003b:  call       "Sub System.Console.WriteLine(Object)"
  IL_0040:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(531546, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531546")>
        Public Sub Bug18273_4()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class VBIDO
    Public Default Property Item2(ByVal p1 As String, ByVal p2 As Integer) As Long
        Get
            Return 0
        End Get
        Set(ByVal value As Long)
            System.Console.WriteLine(p1)
            System.Console.WriteLine(p2)
            System.Console.WriteLine(value)
        End Set
    End Property
End Class

Module Program
    Sub Main()
        Dim o As Object = New VBIDO
        o("hello", p2:="1") = 2
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=
            <![CDATA[
hello
1
2
]]>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  6
  IL_0000:  newobj     "Sub VBIDO..ctor()"
  IL_0005:  ldc.i4.3
  IL_0006:  newarr     "Object"
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldstr      "hello"
  IL_0012:  stelem.ref
  IL_0013:  dup
  IL_0014:  ldc.i4.0
  IL_0015:  ldstr      "1"
  IL_001a:  stelem.ref
  IL_001b:  dup
  IL_001c:  ldc.i4.2
  IL_001d:  ldc.i4.2
  IL_001e:  box        "Integer"
  IL_0023:  stelem.ref
  IL_0024:  ldc.i4.1
  IL_0025:  newarr     "String"
  IL_002a:  dup
  IL_002b:  ldc.i4.0
  IL_002c:  ldstr      "p2"
  IL_0031:  stelem.ref
  IL_0032:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexSet(Object, Object(), String())"
  IL_0037:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(531546, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531546")>
        Public Sub Bug18273_5()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Public Class VBIDO
    Public Default Property Item2(ByVal p1 As String, ByVal p2 As Integer) As Long
        Get
            System.Console.WriteLine(p1)
            System.Console.WriteLine(p2)
            Return 2
        End Get
        Set(ByVal value As Long)
        End Set
    End Property
End Class

Module Program
    Sub Main()
        Dim o As Object = New VBIDO
        System.Console.WriteLine(o("hello", p2:="1"))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=
            <![CDATA[
hello
1
2
]]>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  6
  IL_0000:  newobj     "Sub VBIDO..ctor()"
  IL_0005:  ldc.i4.2
  IL_0006:  newarr     "Object"
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldstr      "hello"
  IL_0012:  stelem.ref
  IL_0013:  dup
  IL_0014:  ldc.i4.0
  IL_0015:  ldstr      "1"
  IL_001a:  stelem.ref
  IL_001b:  ldc.i4.1
  IL_001c:  newarr     "String"
  IL_0021:  dup
  IL_0022:  ldc.i4.0
  IL_0023:  ldstr      "p2"
  IL_0028:  stelem.ref
  IL_0029:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexGet(Object, Object(), String()) As Object"
  IL_002e:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0033:  call       "Sub System.Console.WriteLine(Object)"
  IL_0038:  ret
}
]]>)
        End Sub

        <WorkItem(531547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531547")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub Bug18274()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Module Program
    Sub Main()
        Dim o As Object = Nothing
        Dim x = New With {.a = ""}
        o.Func(o.Func(x.a))
    End Sub
End Module
    </file>
</compilation>)
        End Sub

        <WorkItem(531547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531547")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28046")>
        Public Sub Bug18274_1()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Module Module1
    Sub Main()
        System.Console.WriteLine("-----1")
        Case1()
        System.Console.WriteLine("-----2")
        Case2()
        System.Console.WriteLine("-----3")
        Case3()
        System.Console.WriteLine("-----4")
        Case4()
        System.Console.WriteLine("-----5")
        Case5()
        System.Console.WriteLine("-----6")
        Case6()
        System.Console.WriteLine("-----7")
        Case7()
        System.Console.WriteLine("-----8")
        Case8()
        System.Console.WriteLine("-----9")
        Case9()
        System.Console.WriteLine("-----10")
        Case10()
        System.Console.WriteLine("-----11")
        Case11()
        System.Console.WriteLine("-----12")
        Case12()
        System.Console.WriteLine("-----13")
        Case13()
        System.Console.WriteLine("-----14")
        Case14()
        System.Console.WriteLine("-----15")
        Case15()
        System.Console.WriteLine("-----16")
        Case16()
        System.Console.WriteLine("-----17")
        Case17()
        System.Console.WriteLine("-----18")
        Case18()
        System.Console.WriteLine("-----19")
        Case19()
        System.Console.WriteLine("-----20")
        Case20()
        System.Console.WriteLine("-----21")
        Case21()
        System.Console.WriteLine("-----22")
        Case22()

        System.Console.WriteLine("-----23")
        Case23()
        System.Console.WriteLine("-----24")
        Case24()
        System.Console.WriteLine("-----25")
        Case25()
        System.Console.WriteLine("-----26")
        Case26()
        System.Console.WriteLine("-----27")
        Case27()
        System.Console.WriteLine("-----28")
        Case28()

        'System.Console.WriteLine()
        'System.Console.WriteLine()
        'System.Console.WriteLine()
        'System.Console.WriteLine()

        'Dim caseN As Integer = 7
        'Dim members = {"M1", "P1"}

        'For i As Integer = 0 To 1
        '    For j As Integer = 0 To 1
        '        For k As Integer = 0 To 1
        '            For l As Integer = 0 To 1
        '                'System.Console.WriteLine("        System.Console.WriteLine(""-----{0}"")", caseN)
        '                'System.Console.WriteLine("        Case{0}()", caseN)

        '                System.Console.WriteLine("    Sub Case{0}()", caseN)
        '                System.Console.WriteLine("#If EarlyBound")
        '                System.Console.WriteLine("        Dim t1 As New Test1()")
        '                System.Console.WriteLine("#Else")
        '                System.Console.WriteLine("        Dim t1 As Object = New Test1()")
        '                System.Console.WriteLine("#EndIf")
        '                System.Console.WriteLine("        Dim x = t1.{0}(t1.{1}(t1.{2}(t1.{3}(0))))", members(i), members(j), members(k), members(l))
        '                System.Console.WriteLine("    End Sub")
        '                System.Console.WriteLine()

        '                caseN += 1
        '            Next
        '        Next
        '    Next
        'Next

    End Sub

    Sub Case0()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.P1(t1.M1(0))
    End Sub

    Sub Case1()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.P1(0))
    End Sub

    Sub Case2()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1((t1.P1(0)))
    End Sub

    Sub Case3()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.M1(t1.P1(0)))
    End Sub

    Sub Case4()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.P1(t1.P1(0)))
    End Sub

    Sub Case5()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.M1(t1.M1(0)))
    End Sub

    Sub Case6()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.P1(t1.M1(t1.M1(0)))
    End Sub

    Sub Case7()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.M1(t1.M1(t1.M1(0))))
    End Sub

    Sub Case8()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.M1(t1.M1(t1.P1(0))))
    End Sub

    Sub Case9()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.M1(t1.P1(t1.M1(0))))
    End Sub

    Sub Case10()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.M1(t1.P1(t1.P1(0))))
    End Sub

    Sub Case11()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.P1(t1.M1(t1.M1(0))))
    End Sub

    Sub Case12()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.P1(t1.M1(t1.P1(0))))
    End Sub

    Sub Case13()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.P1(t1.P1(t1.M1(0))))
    End Sub

    Sub Case14()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.P1(t1.P1(t1.P1(0))))
    End Sub

    Sub Case15()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.P1(t1.M1(t1.M1(t1.M1(0))))
    End Sub

    Sub Case16()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.P1(t1.M1(t1.M1(t1.P1(0))))
    End Sub

    Sub Case17()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.P1(t1.M1(t1.P1(t1.M1(0))))
    End Sub

    Sub Case18()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.P1(t1.M1(t1.P1(t1.P1(0))))
    End Sub

    Sub Case19()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.P1(t1.P1(t1.M1(t1.M1(0))))
    End Sub

    Sub Case20()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.P1(t1.P1(t1.M1(t1.P1(0))))
    End Sub

    Sub Case21()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.P1(t1.P1(t1.P1(t1.M1(0))))
    End Sub

    Sub Case22()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.P1(t1.P1(t1.P1(t1.P1(0))))
    End Sub

    Sub Case23()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.M1(t1.M1((t1.P1(0)))))
    End Sub

    Sub Case24()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1(t1.M1((t1.P1(t1.M1(0)))))
    End Sub

    Sub Case25()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If
        Dim x = t1.M1((t1.P1(t1.M1(t1.M1(0)))))
    End Sub

    Sub Case26()
        Dim t1 As New Test1()
        Dim t2 As Object = New Test1()

        Dim x = t1.M1(t2.P1(0))
    End Sub

    Sub Case27()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If

        ReDim t1.P2(t1.P1(0))(2)
    End Sub

    Sub Case28()
#If EarlyBound Then
        Dim t1 As New Test1()
#Else
        Dim t1 As Object = New Test1()
#End If

        ReDim Preserve t1.P2(t1.P1(1))(3)
    End Sub

End Module



Class Test1

    Private _m1 As Integer = 200

    Public Function M1(ByRef x As Integer) As Integer
        _m1 += 1
        System.Console.WriteLine("M1 {0} ({1})", _m1, x)
        Return _m1
    End Function

    Private _p1 As Integer = 300

    Public Property P1(x As Integer) As Integer
        Get
            _p1 += 1
            System.Console.WriteLine("get_P1 {0} ({1})", _p1, x)
            Return _p1
        End Get
        Set(value As Integer)
            _p1 += 1
            System.Console.WriteLine("set_P1 {0} ({1}) = {2}", _p1, x, value)
        End Set
    End Property

    Private _p2 As Integer = 400

    Public Property P2(x As Integer) As Object()
        Get
            _p2 += 1
            System.Console.WriteLine("get_P2 {0} ({1})", _p2, x)
            Return {_p2}
        End Get
        Set(value As Object())
            _p2 += 1
            System.Console.WriteLine("set_P2 {0} ({1}) = {2} {3}", _p2, x, value.Length, If(value(0),"null"))
        End Set
    End Property
End Class
    </file>
</compilation>, expectedOutput:=
            <![CDATA[
-----1
get_P1 301 (0)
M1 201 (301)
set_P1 302 (0) = 301
-----2
get_P1 301 (0)
M1 201 (301)
-----3
get_P1 301 (0)
M1 201 (301)
set_P1 302 (0) = 301
M1 202 (201)
-----4
get_P1 301 (0)
get_P1 302 (301)
M1 201 (302)
set_P1 303 (301) = 302
-----5
M1 201 (0)
M1 202 (201)
M1 203 (202)
-----6
M1 201 (0)
M1 202 (201)
get_P1 301 (202)
-----7
M1 201 (0)
M1 202 (201)
M1 203 (202)
M1 204 (203)
-----8
get_P1 301 (0)
M1 201 (301)
set_P1 302 (0) = 301
M1 202 (201)
M1 203 (202)
-----9
M1 201 (0)
get_P1 301 (201)
M1 202 (301)
set_P1 302 (201) = 301
M1 203 (202)
-----10
get_P1 301 (0)
get_P1 302 (301)
M1 201 (302)
set_P1 303 (301) = 302
M1 202 (201)
-----11
M1 201 (0)
M1 202 (201)
get_P1 301 (202)
M1 203 (301)
set_P1 302 (202) = 301
-----12
get_P1 301 (0)
M1 201 (301)
set_P1 302 (0) = 301
get_P1 303 (201)
M1 202 (303)
set_P1 304 (201) = 303
-----13
M1 201 (0)
get_P1 301 (201)
get_P1 302 (301)
M1 202 (302)
set_P1 303 (301) = 302
-----14
get_P1 301 (0)
get_P1 302 (301)
get_P1 303 (302)
M1 201 (303)
set_P1 304 (302) = 303
-----15
M1 201 (0)
M1 202 (201)
M1 203 (202)
get_P1 301 (203)
-----16
get_P1 301 (0)
M1 201 (301)
set_P1 302 (0) = 301
M1 202 (201)
get_P1 303 (202)
-----17
M1 201 (0)
get_P1 301 (201)
M1 202 (301)
set_P1 302 (201) = 301
get_P1 303 (202)
-----18
get_P1 301 (0)
get_P1 302 (301)
M1 201 (302)
set_P1 303 (301) = 302
get_P1 304 (201)
-----19
M1 201 (0)
M1 202 (201)
get_P1 301 (202)
get_P1 302 (301)
-----20
get_P1 301 (0)
M1 201 (301)
set_P1 302 (0) = 301
get_P1 303 (201)
get_P1 304 (303)
-----21
M1 201 (0)
get_P1 301 (201)
get_P1 302 (301)
get_P1 303 (302)
-----22
get_P1 301 (0)
get_P1 302 (301)
get_P1 303 (302)
get_P1 304 (303)
-----23
get_P1 301 (0)
M1 201 (301)
M1 202 (201)
M1 203 (202)
-----24
M1 201 (0)
get_P1 301 (201)
M1 202 (301)
M1 203 (202)
-----25
M1 201 (0)
M1 202 (201)
get_P1 301 (202)
M1 203 (301)
-----26
get_P1 301 (0)
M1 201 (301)
set_P1 302 (0) = 301
-----27
get_P1 301 (0)
set_P2 401 (301) = 3 null
-----28
get_P1 301 (1)
get_P2 401 (301)
set_P2 402 (301) = 4 401
]]>)
        End Sub

        <Fact>
        Public Sub LateBoundArgumentForByRefParameterInEarlyBoundCall_Diagnostic()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Class Test2

    Shared Sub Test()
        Dim t2 As Object = Nothing
        M1(t2.p1(0))
    End Sub

    Public Shared Sub M1(ByRef x As Integer)
    End Sub
End Class    </file>
</compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))

            CompileAndVerify(compilation)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42017: Late bound resolution; runtime errors could occur.
        M1(t2.p1(0))
           ~~~~~
BC42016: Implicit conversion from 'Object' to 'Integer'.
        M1(t2.p1(0))
           ~~~~~~~~
</expected>)

        End Sub


        <Fact(), WorkItem(531153, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531153")>
        Public Sub Bug531153()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module Program
    Sub Sub1(ByRef x)
        Console.WriteLine(x)
    End Sub
    Sub Main(args As String())
        Dim VI As Object = 1
        Sub1(Math.Abs(VI))
    End Sub
End Module

    </file>
</compilation>, expectedOutput:="1")

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       82 (0x52)
  .maxstack  10
  .locals init (Object V_0, //VI
  Object V_1,
  Object() V_2,
  Boolean() V_3)
  IL_0000:  ldc.i4.1
  IL_0001:  box        "Integer"
  IL_0006:  stloc.0
  IL_0007:  ldnull
  IL_0008:  ldtoken    "System.Math"
  IL_000d:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0012:  ldstr      "Abs"
  IL_0017:  ldc.i4.1
  IL_0018:  newarr     "Object"
  IL_001d:  dup
  IL_001e:  ldc.i4.0
  IL_001f:  ldloc.0
  IL_0020:  stelem.ref
  IL_0021:  dup
  IL_0022:  stloc.2
  IL_0023:  ldnull
  IL_0024:  ldnull
  IL_0025:  ldc.i4.1
  IL_0026:  newarr     "Boolean"
  IL_002b:  dup
  IL_002c:  ldc.i4.0
  IL_002d:  ldc.i4.1
  IL_002e:  stelem.i1
  IL_002f:  dup
  IL_0030:  stloc.3
  IL_0031:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_0036:  ldloc.3
  IL_0037:  ldc.i4.0
  IL_0038:  ldelem.u1
  IL_0039:  brfalse.s  IL_0044
  IL_003b:  ldloc.2
  IL_003c:  ldc.i4.0
  IL_003d:  ldelem.ref
  IL_003e:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0043:  stloc.0
  IL_0044:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0049:  stloc.1
  IL_004a:  ldloca.s   V_1
  IL_004c:  call       "Sub Program.Sub1(ByRef Object)"
  IL_0051:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(531153, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531153")>
        Public Sub Bug531153_1()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System                        

Module Program
    Sub Sub1(ByRef x)
        Console.WriteLine(x)
    End Sub
    Sub Main(args As String())
        Dim VI As Object = 1
        Sub1(P1(VI))
    End Sub

    Public ReadOnly Property P1(x As Integer) As Integer
        Get
            Return 1
        End Get
    End Property

    Public ReadOnly Property P1(x As string) As Integer
        Get
            Return 2
        End Get
    End Property

End Module


    </file>
</compilation>, expectedOutput:="1")

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       96 (0x60)
  .maxstack  8
  .locals init (Object V_0, //VI
  Object V_1,
  Object V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  box        "Integer"
  IL_0006:  stloc.0
  IL_0007:  ldnull
  IL_0008:  ldtoken    "Program"
  IL_000d:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0012:  ldstr      "P1"
  IL_0017:  ldc.i4.1
  IL_0018:  newarr     "Object"
  IL_001d:  dup
  IL_001e:  ldc.i4.0
  IL_001f:  ldloc.0
  IL_0020:  dup
  IL_0021:  stloc.1
  IL_0022:  stelem.ref
  IL_0023:  ldnull
  IL_0024:  ldnull
  IL_0025:  ldnull
  IL_0026:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
  IL_002b:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0030:  stloc.2
  IL_0031:  ldloca.s   V_2
  IL_0033:  call       "Sub Program.Sub1(ByRef Object)"
  IL_0038:  ldnull
  IL_0039:  ldtoken    "Program"
  IL_003e:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0043:  ldstr      "P1"
  IL_0048:  ldc.i4.2
  IL_0049:  newarr     "Object"
  IL_004e:  dup
  IL_004f:  ldc.i4.0
  IL_0050:  ldloc.1
  IL_0051:  stelem.ref
  IL_0052:  dup
  IL_0053:  ldc.i4.1
  IL_0054:  ldloc.2
  IL_0055:  stelem.ref
  IL_0056:  ldnull
  IL_0057:  ldnull
  IL_0058:  ldc.i4.1
  IL_0059:  ldc.i4.0
  IL_005a:  call       "Sub Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateSetComplex(Object, System.Type, String, Object(), String(), System.Type(), Boolean, Boolean)"
  IL_005f:  ret
}
]]>)
        End Sub

        <WorkItem(575833, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/575833")>
        <Fact()>
        Public Sub OverloadedMethodUsingNamespaceDotMethodSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off
Option Strict Off
Namespace N
        Module M1

        Sub main
        End Sub

        Friend Sub F(o As String)
        End Sub
        Friend Sub F(o As Integer)
        End Sub
    End Module
    Module M2
        Sub M(o)
            N.F(o)
        End Sub
    End Module
End Namespace
   ]]></file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        <WorkItem(531569, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531569")>
        <Fact()>
        Public Sub ObjectToXmlLiteral()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
        <![CDATA[
Option Explicit Off
Option Strict Off

Imports System
Imports System.Xml.Linq

Friend Module Program
    Sub Main()
        Dim o2 As Object = "E"
        o2 = XName.Get("HELLO")
        Dim y2 = <<%= o2 %>></>

        Console.WriteLine(y2.Name)
    End Sub
End Module

   ]]>
    </file>
</compilation>, expectedOutput:="HELLO", references:=XmlReferences)

        End Sub

        <WorkItem(531569, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531569")>
        <Fact()>
        Public Sub ObjectToXmlLiteral_Err()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off
Option Strict On

Imports System
Imports System.Xml.Linq

Friend Module Program
    Sub Main()
        Dim o2 As Object = "E"
        o2 = XName.Get("HELLO")
        Dim y2 = <<%= o2 %>></>

        Console.WriteLine(y2.Name)
    End Sub
End Module

   ]]></file>
</compilation>, references:=XmlReferences)

            compilation.AssertTheseDiagnostics(
                <expected><![CDATA[
                    BC30518: Overload resolution failed because no accessible 'New' can be called with these arguments:
    'Public Overloads Sub New(name As XName)': Option Strict On disallows implicit conversions from 'Object' to 'XName'.
    'Public Overloads Sub New(other As XElement)': Option Strict On disallows implicit conversions from 'Object' to 'XElement'.
    'Public Overloads Sub New(other As XStreamingElement)': Option Strict On disallows implicit conversions from 'Object' to 'XStreamingElement'.
        Dim y2 = <<%= o2 %>></>
                  ~~~~~~~~~
]]>
                </expected>)

        End Sub

        <WorkItem(632206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632206")>
        <Fact()>
        Public Sub LateBang()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[

Imports System
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())
        Dim test As Object = Nothing

        ' error
        test.dictionaryField!second

        ' error
        test.dictionaryField!second()

        ' not an error
        Dim o = test.dictionaryField!second

        ' not an error
        o = test.dictionaryField!second()

        ' not an error
        moo(test.dictionaryField!second)

    End Sub

    Sub moo(ByRef o As Object)

    End Sub
End Module

   ]]>
    </file>
</compilation>)

            compilation.AssertTheseDiagnostics(
                <expected><![CDATA[
BC30454: Expression is not a method.
        test.dictionaryField!second
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30454: Expression is not a method.
        test.dictionaryField!second()
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
                </expected>)


        End Sub

    End Class
End Namespace
