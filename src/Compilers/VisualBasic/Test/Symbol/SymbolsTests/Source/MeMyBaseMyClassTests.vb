' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class MeMyBaseMyClassTests
        Inherits BasicTestBase

#Region "Me test"
        ' 'Me' is key word and can't use as in identify
        <Fact>
        Public Sub MeIsKeyWord()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MeIsKeyWord">
    <file name="a.vb">
Module Program
    Dim Me As String
End Module
Enum Me
    Blue
End Enum
Structure S1
    Sub Me()
    End Sub
End Structure
Class C1
    Function Me.goo() As String
        Return "Hello"
    End Function
End Class
    </file>
</compilation>)
            VerifyDiagnostics(comp,
                        Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "Me"),
                        Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "Me"),
                        Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "Me"),
                        Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "Me"))

        End Sub

        ' Me can be used differentiate between a method parameter and an instance member.
        <Fact>
        Public Sub MeUsedToDiffParameterAndInstanceMember()
            CompileAndVerify(
<compilation name="MeUsedToDiffParameterAndInstanceMember">
    <file name="a.vb">
Imports System
Structure S1
    Dim value As Int16
    Public Sub SetValue(ByVal value As String)
        value = Me.value
    End Sub
End Structure
    </file>
</compilation>).VerifyIL("S1.SetValue", <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "S1.value As Short"
  IL_0006:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String"
  IL_000b:  starg.s    V_1
  IL_000d:  ret
}
]]>)

        End Sub

        ' 'Me' is not valid within a Module.
        <Fact>
        Public Sub MeIsInvalidInModule()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MeIsInvalidInModule">
    <file name="a.vb">
Imports System
Module Program
    Dim value As Int16
    Sub Main(args As String())
        Me.Value = 1
    End Sub
End Module
    </file>
</compilation>)
            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_UseOfKeywordFromModule1, "Me").WithArguments("Me"))

        End Sub

        <Fact()>
        Public Sub GetTypeForMe()
            CompileAndVerify(
<compilation name="GetTypeForMe">
    <file name="a.vb">
Imports System
Class MeClass
    Public Sub test()
        Console.WriteLine(TypeOf Me Is MeClass)
    End Sub
    Public Shared Sub Main()
        Dim x = New MeClass()
        x.test()
    End Sub
End Class
    </file>
</compilation>, <![CDATA[True]]>).
    VerifyIL("MeClass.test",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  isinst     "MeClass"
  IL_0006:  ldnull
  IL_0007:  cgt.un
  IL_0009:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_000e:  ret
}
]]>)
        End Sub

        ' Use Me in an Is comparison
        <Fact>
        Public Sub CompareMe()
            CompileAndVerify(
<compilation name="CompareMe">
    <file name="a.vb">
Imports System
Class MeClass
    Public Sub test()
        Console.WriteLine(Me Is Me)
    End Sub
    Public Shared Sub Main()
        Dim x = New MeClass()
        x.test()
    End Sub
End Class
    </file>
</compilation>, <![CDATA[True]]>).VerifyIL("MeClass.test", <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ceq
  IL_0004:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0009:  ret
}
]]>)

        End Sub

        ' Use Me in field initialize
        <Fact>
        Public Sub UseMeInFieldInit()
            CompileAndVerify(
<compilation name="UseMeInFieldInit">
    <file name="a.vb">
Class c1
    Dim x As c1 = Me
End Class

Class base
End Class
Structure s1
    Class c1
        Inherits base
        Dim x As c1 = Me
        Dim y As base = Me
    End Class
End Structure
    </file>
</compilation>).VerifyIL("c1..ctor", <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      "c1.x As c1"
  IL_000d:  ret
}
]]>).VerifyIL("s1.c1..ctor", <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub base..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      "s1.c1.x As s1.c1"
  IL_000d:  ldarg.0
  IL_000e:  ldarg.0
  IL_000f:  stfld      "s1.c1.y As base"
  IL_0014:  ret
}
]]>)

        End Sub

        ' Call Me.[Me]
        <Fact>
        Public Sub CallMe()
            CompileAndVerify(
<compilation name="CallMe">
    <file name="a.vb">
Imports System
Class MeClass
    Function [Me]() As String
        [Me] = "Hello"
        Console.WriteLine(Me.Me)
    End Function
    Public Shared Sub Main()
        Dim x = New MeClass
        x.Me()
    End Sub
End Class
    </file>
</compilation>).VerifyIL("MeClass.Me", <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (String V_0) //Me
  IL_0000:  ldstr      "Hello"
  IL_0005:  stloc.0
  IL_0006:  ldarg.0
  IL_0007:  call       "Function MeClass.Me() As String"
  IL_000c:  call       "Sub System.Console.WriteLine(String)"
  IL_0011:  ldloc.0
  IL_0012:  ret
}
]]>)
        End Sub

        ' Call Me on a Public method defined only in the base class.
        <Fact>
        Public Sub CallFunctionInBaseClassByMe()
            CompileAndVerify(
<compilation name="CallFunctionInBaseClassByMe">
    <file name="a.vb">
Imports System
Class BaseClass
    Function Method() As String
        Return "BaseClass"
    End Function
End Class
Class DerivedClass
    Inherits BaseClass
    Sub Test()
        Console.WriteLine(Me.Method)
    End Sub
End Class
    </file>
</compilation>).VerifyIL("DerivedClass.Test", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function BaseClass.Method() As String"
  IL_0006:  call       "Sub System.Console.WriteLine(String)"
  IL_000b:  ret
}
]]>)
        End Sub

        ' Call Me on a private method defined only in the base class.
        <Fact>
        Public Sub CallFunctionInBaseClassByMe_2()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CallFunctionInBaseClassByMe">
    <file name="a.vb">
Class BaseClass
    Private Sub goo()
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
    Sub Test()
        Me.goo()
    End Sub
End Class
    </file>
</compilation>)
            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_InaccessibleMember3, "Me.goo").WithArguments("BaseClass", "Private Sub goo()", "Private"))

        End Sub

        ' Call Me on a Public Shared Method.
        <Fact>
        Public Sub CallSharedFunctionInBaseClassByMe()
            CompileAndVerify(
<compilation name="CallSharedFunctionInBaseClassByMe">
    <file name="a.vb">
Imports System
Class BaseClass
    Function Method() As String
        Return "BaseClass"
    End Function
End Class
Class DerivedClass
    Inherits BaseClass
    Sub Test()
        Console.WriteLine(Me.Method)
    End Sub
End Class
    </file>
</compilation>).VerifyIL("DerivedClass.Test", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function BaseClass.Method() As String"
  IL_0006:  call       "Sub System.Console.WriteLine(String)"
  IL_000b:  ret
}
]]>)
        End Sub

        ' Assign Me to a variable.
        <Fact>
        Public Sub AssignMeToVar()
            CompileAndVerify(
<compilation name="AssignMeToVar">
    <file name="a.vb">
Imports System
Class DerivedClass
    Dim vme = Me
End Class
    </file>
</compilation>).VerifyIL("DerivedClass..ctor", <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      "DerivedClass.vme As Object"
  IL_000d:  ret
}
]]>)
        End Sub

        ' Pass Me as argument
        <Fact>
        Public Sub MeAsArgument()
            CompileAndVerify(
<compilation name="MeAsArgument">
    <file name="a.vb">
Imports System
Class Class1
    Sub Goo()
        Goo(Me)
    End Sub
    Sub Goo(ByVal x As Class1)
        x = Nothing
    End Sub
End Class

Class MeClass
    Sub test()
        Dim varMe As MeClass
        varMe = Me.PassMe(Me)
    End Sub
    Function PassMe(ByRef varme As MeClass) As MeClass
        PassMe = varme
    End Function
End Class
    </file>
</compilation>).VerifyIL("Class1.Goo", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Sub Class1.Goo(Class1)"
  IL_0007:  ret
}
]]>).VerifyIL("MeClass.test", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (MeClass V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       "Function MeClass.PassMe(ByRef MeClass) As MeClass"
  IL_000a:  pop
  IL_000b:  ret
}
]]>)
        End Sub

        ' Call data member by Me
        <Fact>
        Public Sub CallDataMemberByMe()
            CompileAndVerify(
<compilation name="CallDataMemberByMe">
    <file name="a.vb">
Class MeClass
    Dim age As Integer
    Property age1 As Integer
    Sub test()
        Me.age = 18
        Dim x = Me.age
    End Sub
    Sub test1()
        Me.age1 = 18
        Dim x = Me.age1
    End Sub
End Class
    </file>
</compilation>).VerifyIL("MeClass.test", <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   18
  IL_0003:  stfld      "MeClass.age As Integer"
  IL_0008:  ldarg.0
  IL_0009:  ldfld      "MeClass.age As Integer"
  IL_000e:  pop
  IL_000f:  ret
}
]]>).VerifyIL("MeClass.test1", <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   18
  IL_0003:  call       "Sub MeClass.set_age1(Integer)"
  IL_0008:  ldarg.0
  IL_0009:  call       "Function MeClass.get_age1() As Integer"
  IL_000e:  pop
  IL_000f:  ret
}
]]>)
        End Sub

        ' Call data member by Me
        <Fact>
        Public Sub CallSharedDataMemberByMe()
            CompileAndVerify(
<compilation name="CallSharedDataMemberByMe">
    <file name="a.vb">
Class MeClass
    Shared age As Integer
    Sub test()
        Me.age = 18
        Dim x = Me.age
    End Sub
End Class
    </file>
</compilation>).VerifyIL("MeClass.test", <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldc.i4.s   18
  IL_0002:  stsfld     "MeClass.age As Integer"
  IL_0007:  ldsfld     "MeClass.age As Integer"
  IL_000c:  pop
  IL_000d:  ret
}
]]>)
        End Sub

        ' Use Me in structure
        <Fact>
        Public Sub UseMeInStructure()
            CompileAndVerify(
<compilation name="UseMeInStructure">
    <file name="a.vb">
Structure s1
    Dim x As Integer
    Sub goo()
        Me.x = 1
        Dim y = Me.x
        System.Console.WriteLine(Me.x)
    End Sub
End Structure
    </file>
</compilation>).VerifyIL("s1.goo", <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      "s1.x As Integer"
  IL_0007:  ldarg.0
  IL_0008:  ldfld      "s1.x As Integer"
  IL_000d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0012:  ret
}
]]>)
        End Sub

        ' Use Me in constructor
        <Fact>
        Public Sub UseMeInConstructor()
            CompileAndVerify(
<compilation name="UseMeInConstructor">
    <file name="a.vb">
Structure Student
    Property Name As String
    Sub New(value As String)
        Me.Name = value
    End Sub
End Structure
    </file>
</compilation>).VerifyIL("Student..ctor", <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    "Student"
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  call       "Sub Student.set_Name(String)"
  IL_000e:  ret
}
]]>)
        End Sub

        ' Use Me in constructor
        <Fact>
        Public Sub UseMeInConstructor_2()
            CompileAndVerify(
<compilation name="UseMeInConstructor">
    <file name="a.vb">
Class C1
    Public Sub New(ByVal accountKey As Integer)
        Me.new(accountKey, Nothing)
    End Sub
    Public Sub New(ByVal accountKey As Integer, ByVal accountName As String)
        Me.New(accountKey, accountName, Nothing)
    End Sub
    Public Sub New(ByVal accountKey As Integer, ByVal accountName As String, ByVal accountNumber As String)
    End Sub
End Class
    </file>
</compilation>).VerifyIL("C1..ctor(Integer)", <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldnull
  IL_0003:  call       "Sub C1..ctor(Integer, String)"
  IL_0008:  ret
}
]]>).VerifyIL("C1..ctor(Integer, String)", <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  ldnull
  IL_0004:  call       "Sub C1..ctor(Integer, String, String)"
  IL_0009:  ret
}
]]>)
        End Sub

        ' Use Me in Lambda
        <Fact()>
        Public Sub UseMeInLambda()
            CompileAndVerify(
<compilation name="UseMeInLambda">
    <file name="a.vb">
Option Infer On
Module Module1
    Class Class1
        Function Bar() As Integer
            return 0
        End Function
    End Class
    Class Class2 : Inherits Class1
        Sub TEST()
            Dim TEMP = Function(X) Me.Bar()
            TEMP(Nothing)
        End Sub
    End Class
End Module
    </file>
</compilation>).VerifyIL("Module1.Class2.TEST", <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function Module1.Class2._Lambda$__1-0(Object) As Integer"
  IL_0007:  newobj     "Sub VB$AnonymousDelegate_0(Of Object, Integer)..ctor(Object, System.IntPtr)"
  IL_000c:  ldnull
  IL_000d:  callvirt   "Function VB$AnonymousDelegate_0(Of Object, Integer).Invoke(Object) As Integer"
  IL_0012:  pop
  IL_0013:  ret
}
]]>)
        End Sub

        ' Use Me in Query
        <Fact>
        Public Sub UseMeInQuery()
            CompileAndVerify(
<compilation name="UseMeInQuery">
    <file name="a.vb">
Imports System.Linq
Module Module1
    Class Class1
        Function Bar1() As String
            Bar1 = "hello"
        End Function
    End Class
    Class Class2 : Inherits Class1
        Function TEST()
            TEST = From x In Me.Bar1 Select Me
        End Function
    End Class
End Module
    </file>
</compilation>, references:={SystemCoreRef}).VerifyIL("Module1.Class2.TEST", <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  3
  .locals init (Object V_0) //TEST
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.Class1.Bar1() As String"
  IL_0006:  ldarg.0
  IL_0007:  ldftn      "Function Module1.Class2._Lambda$__1-0(Char) As Module1.Class2"
  IL_000d:  newobj     "Sub System.Func(Of Char, Module1.Class2)..ctor(Object, System.IntPtr)"
  IL_0012:  call       "Function System.Linq.Enumerable.Select(Of Char, Module1.Class2)(System.Collections.Generic.IEnumerable(Of Char), System.Func(Of Char, Module1.Class2)) As System.Collections.Generic.IEnumerable(Of Module1.Class2)"
  IL_0017:  stloc.0
  IL_0018:  ldloc.0
  IL_0019:  ret
}
]]>)
        End Sub

        ' Invalid use of  Me
        <Fact>
        Public Sub InvalidUseOfMe()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="InvalidUseOfMe">
    <file name="a.vb">
Class Class1
    Sub Bar1()
        Me
        Me()
        Me.Me()
        MyBase.me()
        Me.mybase()
        Me.myclass()
        MyClass.me()
    End Sub
End Class
    </file>
</compilation>)
            VerifyDiagnostics(comp,
                    Diagnostic(ERRID.ERR_ExpectedProcedure, "Me"),
                    Diagnostic(ERRID.ERR_ExpectedProcedure, "Me"),
                    Diagnostic(ERRID.ERR_NameNotMember2, "Me.Me").WithArguments("Me", "Class1"),
                    Diagnostic(ERRID.ERR_NameNotMember2, "MyBase.me").WithArguments("me", "Object"),
                    Diagnostic(ERRID.ERR_NameNotMember2, "Me.mybase").WithArguments("mybase", "Class1"),
                    Diagnostic(ERRID.ERR_NameNotMember2, "Me.myclass").WithArguments("myclass", "Class1"),
                   Diagnostic(ERRID.ERR_NameNotMember2, "MyClass.me").WithArguments("me", "Class1"))

        End Sub
#End Region

#Region "MyBase test"

        ' 'MyBase' is key word and can't use as in identify
        <Fact>
        Public Sub MyBaseIsKeyWord()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MyBaseIsKeyWord">
    <file name="a.vb">
Class MyBase
    Property MyBase As String
End Class
    </file>
</compilation>)
            VerifyDiagnostics(comp,
                    Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "MyBase"),
                    Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "MyBase"))

        End Sub

        <Fact>
        Public Sub MyBaseIsKeyWord_2()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MyBaseIsKeyWord">
    <file name="a.vb">
Class C1
    Property Age As MyBase
End Class
    </file>
</compilation>)
            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_UnrecognizedTypeKeyword, ""))
        End Sub

        <Fact>
        Public Sub MyBaseIsKeyWord_3()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MyBaseIsKeyWord">
    <file name="a.vb">
Class C1
    Sub MyBase()
    End Sub
End Class
Enum mybase
    Blue
End Enum
    </file>
</compilation>)
            VerifyDiagnostics(comp,
                    Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "MyBase"),
                    Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "mybase"))

        End Sub

        ' MyBase refers to the immediate base class and its inherited members. It cannot be used to access Private members in the class.
        <Fact>
        Public Sub AccessPrivateMethodInBase()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AccessPrivateMethodInBase">
    <file name="a.vb">
Class BaseClass
    Private Sub goo()
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
    Sub Test()
        MyBase.goo()
    End Sub
End Class
    </file>
</compilation>)
            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_InaccessibleMember3, "MyBase.goo").WithArguments("BaseClass", "Private Sub goo()", "Private"))

        End Sub

        ' The method that MyBase qualifies does not need to be defined in the immediate base class; it may instead be defined in an indirectly inherited base class. 
        <Fact>
        Public Sub AccessIndirectMethodInBase()
            CompileAndVerify(
<compilation name="AccessIndirectMethodInBase">
    <file name="a.vb">
Class BaseClass
    Public Sub goo()
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
End Class
Class C1
    Inherits DerivedClass
    Sub Test()
        MyBase.goo()
    End Sub
End Class
    </file>
</compilation>).VerifyIL("C1.Test", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub BaseClass.goo()"
  IL_0006:  ret
}
]]>)
        End Sub

        ' MyBase is a keyword, not a real object. MyBase cannot be assigned to a variable, passed to procedures, or used in an Is comparison.
        <Fact()>
        Public Sub MyBaseIsKeyWord_4()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MyBaseIsKeyWord">
    <file name="a.vb">
Class BaseClass
    Private Sub goo()
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
    Sub Test()
        Dim obj = MyBase
        Dim base = New BaseClass()
        MyBase = base
        System.Console.WriteLine(base Is MyBase)
        System.Console.WriteLine(TypeOf (MyBase))
        goo(MyBase)
    End Sub
    Sub goo(base As BaseClass)
    End Sub
End Class
    </file>
</compilation>)

            ' ROSLYN extra errors - ERR_MissingIsInTypeOf,ERR_UnrecognizedType,ERR_ExpectedRparen,ERR_LValueRequired
            VerifyDiagnostics(comp,
                       Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
    Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
    Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
    Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
    Diagnostic(ERRID.ERR_MissingIsInTypeOf, ""),
    Diagnostic(ERRID.ERR_UnrecognizedType, ""),
    Diagnostic(ERRID.ERR_ExpectedRparen, ""),
    Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
    Diagnostic(ERRID.ERR_LValueRequired, "MyBase"))

        End Sub

        ' MyBase cannot be used to qualify itself. 
        <Fact>
        Public Sub InvalidUseOfMyBase()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="InvalidUseOfMyBase">
    <file name="a.vb">
MustInherit Class BaseClass
    Sub MyMethod()
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
    Overloads Sub MyMethod()
    End Sub
End Class
Class DerivedClass2
    Inherits DerivedClass
    Sub test()
        MyBase.mybase.Mymethod()
    End Sub
End Class
    </file>
</compilation>)

            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_NameNotMember2, "MyBase.mybase").WithArguments("mybase", "DerivedClass"))

        End Sub

        ' MyBase cannot be used in modules.
        <Fact>
        Public Sub MyBaseCannotUsedInModule()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MyBaseCannotUsedInModule">
    <file name="a.vb">
Module M1
    Sub GOO()
        MyBase.ToString()
    End Sub
End Module
    </file>
</compilation>)
            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_UseOfKeywordFromModule1, "MyBase").WithArguments("MyBase"))

        End Sub

        ' MyBase cannot be used to access base class members that are marked as Friend if the base class is in a different assembly.
        <Fact>
        Public Sub InvalidUseOfMyBase_2()
            Dim comp1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="InvalidUseOfMyBase_2">
    <file name="a.vb">
Public Class Class1
    Friend Sub goo()
    End Sub
End Class
    </file>
</compilation>)
            Dim compRef = New VisualBasicCompilationReference(comp1)
            Dim comp2 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="InvalidUseOfMyBase_2">
    <file name="a.vb">
Class DerivedClass
    Inherits Class1
    Sub test()
        MyBase.goo()
    End Sub
End Class
    </file>
</compilation>, references:={compRef})

            VerifyDiagnostics(comp2, Diagnostic(ERRID.ERR_InaccessibleMember3, "MyBase.goo").WithArguments("Class1", "Friend Sub goo()", "Friend"))

        End Sub

        ' Calls MyBase.method which is only defined in the derived class 
        <Fact>
        Public Sub MethodOnlyDefinedInDerived()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MethodOnlyDefinedInDerived">
    <file name="a.vb">
MustInherit Class BaseClass
End Class
Class DerivedClass
    Inherits BaseClass
    Friend Sub goo()
    End Sub
    Sub test()
        MyBase.goo()
    End Sub
End Class
    </file>
</compilation>)
            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_NameNotMember2, "MyBase.goo").WithArguments("goo", "BaseClass"))

        End Sub

        ' MyBase used to qualify a function overriding.
        <Fact>
        Public Sub InvalidUseOfMyBase_3()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="InvalidUseOfMyBase">
    <file name="a.vb">
MustInherit Class BaseClass
End Class
Class DerivedClass
    Inherits BaseClass
    Friend Sub goo()
    End Sub
    Sub test()
        MyBase.goo()
    End Sub
End Class
    </file>
</compilation>)
            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_NameNotMember2, "MyBase.goo").WithArguments("goo", "BaseClass"))

        End Sub

        ' Call MyBase by itself in a class that has a default property 
        <Fact>
        Public Sub CallMyBaseWithDefaultProperty()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CallMyBaseWithDefaultProperty">
    <file name="a.vb">
Class BaseClass
    Default Property myProperty(ByVal index As Integer) As String
        Get
            Return index.ToString()
        End Get
        Set(value As String)
        End Set
    End Property
    Overridable Sub Fun()
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
    Overrides Sub Fun()
        MyBase
        MyBase(10)
    End Sub
End Class    </file>
</compilation>)
            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
                                    Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
                                    Diagnostic(ERRID.ERR_ExpectedProcedure, "MyBase"),
                                    Diagnostic(ERRID.ERR_ExpectedProcedure, "MyBase"))

        End Sub

        ' Call data member by MyBase
        <Fact>
        Public Sub CallDataMemberByMyBase()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CallDataMemberByMyBase">
    <file name="a.vb">
Class BaseClass
    Property age As Integer
    Public name As String
    Private ID As String
End Class
Class DerivedClass
    Inherits BaseClass
    Sub test()
        Dim X = MyBase.age
        Dim Y = MyBase.name
        Dim Z = MyBase.id
    End Sub
End Class
    </file>
</compilation>)

            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_InaccessibleSymbol2, "MyBase.id").WithArguments("BaseClass.ID", "Private"))

        End Sub

        ' Call MyBase in constructor
        <Fact>
        Public Sub CallMyBaseInConstructor()
            CompileAndVerify(
<compilation name="CallMyBaseInConstructor">
    <file name="a.vb">
Imports System.Linq
Class BaseClass
    Property Name As String
    Sub New(value As String)
        Me.Name = value
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
    Sub New(value As String)
        MyBase.New("J")
    End Sub
End Class
    </file>
</compilation>).VerifyIL("DerivedClass..ctor(String)", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      "J"
  IL_0006:  call       "Sub BaseClass..ctor(String)"
  IL_000b:  ret
}
]]>)
        End Sub

        ' Call MyBase in field initialize
        <Fact>
        Public Sub CallMyBaseInFieldInit()
            CompileAndVerify(
<compilation name="CallMyBaseInFieldInit">
    <file name="a.vb">
Imports System.Linq
Class c1
    Dim x = MyBase.ToString()
End Class
Class base
End Class
Structure s1
    Class c1
        Inherits base
        Dim x = MyBase.ToString()
    End Class
End Structure
    </file>
</compilation>).VerifyIL("c1..ctor()", <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  call       "Function Object.ToString() As String"
  IL_000d:  stfld      "c1.x As Object"
  IL_0012:  ret
}
]]>).VerifyIL("s1.c1..ctor()", <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub base..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  call       "Function Object.ToString() As String"
  IL_000d:  stfld      "s1.c1.x As Object"
  IL_0012:  ret
}
]]>)
        End Sub

        ' Call MyBase in lambda
        <Fact()>
        Public Sub CallMyBaseInLambda()
            CompileAndVerify(
<compilation name="CallMyBaseInLambda">
    <file name="a.vb">
Imports System
Module Module1
    Class Class1
        Function Bar(n As Integer) As Integer
            Return n + 1
        End Function
    End Class
    Class Class2 : Inherits Class1
        Sub TEST()
            Dim TEMP = Function(X) MyBase.Bar(x)
            TEMP(1)
        End Sub
    End Class
End Module
    </file>
</compilation>).VerifyIL("Module1.Class2.TEST", <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function Module1.Class2._Lambda$__1-0(Object) As Integer"
  IL_0007:  newobj     "Sub VB$AnonymousDelegate_0(Of Object, Integer)..ctor(Object, System.IntPtr)"
  IL_000c:  ldc.i4.1
  IL_000d:  box        "Integer"
  IL_0012:  callvirt   "Function VB$AnonymousDelegate_0(Of Object, Integer).Invoke(Object) As Integer"
  IL_0017:  pop
  IL_0018:  ret
}
]]>)
        End Sub

        ' Call MyBase in Query
        <WorkItem(543465, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543465")>
        <Fact()>
        Public Sub UseMyBaseInQuery()

            CompileAndVerify(
<compilation name="UseMyBaseInQuery">
    <file name="a.vb">
Imports System.Linq
Module Module1
    Class Class1
        Function Bar() As String
            Bar = "hello"
        End Function
    End Class
    Class Class2 : Inherits Class1
        Function TEST()
            TEST = From x In MyBase.Bar Select x
        End Function
    End Class
End Module
    </file>
</compilation>, references:={SystemCoreRef}).VerifyIL("Module1.Class2.TEST", <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  3
  .locals init (Object V_0) //TEST
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.Class1.Bar() As String"
  IL_0006:  ldsfld     "Module1.Class2._Closure$__.$I1-0 As System.Func(Of Char, Char)"
  IL_000b:  brfalse.s  IL_0014
  IL_000d:  ldsfld     "Module1.Class2._Closure$__.$I1-0 As System.Func(Of Char, Char)"
  IL_0012:  br.s       IL_002a
  IL_0014:  ldsfld     "Module1.Class2._Closure$__.$I As Module1.Class2._Closure$__"
  IL_0019:  ldftn      "Function Module1.Class2._Closure$__._Lambda$__1-0(Char) As Char"
  IL_001f:  newobj     "Sub System.Func(Of Char, Char)..ctor(Object, System.IntPtr)"
  IL_0024:  dup
  IL_0025:  stsfld     "Module1.Class2._Closure$__.$I1-0 As System.Func(Of Char, Char)"
  IL_002a:  call       "Function System.Linq.Enumerable.Select(Of Char, Char)(System.Collections.Generic.IEnumerable(Of Char), System.Func(Of Char, Char)) As System.Collections.Generic.IEnumerable(Of Char)"
  IL_002f:  stloc.0
  IL_0030:  ldloc.0
  IL_0031:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub UseMyBaseInAddressOf()
            CompileAndVerify(
<compilation name="UseMyBaseInAddressOf">
    <file name="a.vb">
Imports System.Linq
Module Module1
    Class Class1
        Overridable Function Bar() As Integer
            Return 1
        End Function
    End Class
    Class Class2 : Inherits Class1
        Overrides Function Bar() As Integer
            Return 2
        End Function
        Delegate Function Func(Of T)() As T
        Function Test() As Integer
            Dim x = 5
            Dim f = Function() (New Func(Of Integer)(AddressOf MyBase.Bar))() + x
            return f()
        End Function
    End Class
    Sub Main()
        System.Console.WriteLine((New Class2).Test)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="6")
        End Sub

        ' Invoke MyBase.AutoProperty
        <Fact()>
        Public Sub InvokeMyBaseAutoProperty()
            CompileAndVerify(
<compilation name="InvokeMyBaseAutoProperty">
    <file name="a.vb">
Imports System
Class GenBase
    Public Property Propabc As Integer = 1
    Public abc As Integer = 1
End Class
Class GenParent(Of t)
    Inherits GenBase
    Dim xyz = 1
    Public Property PropXyz = 1
    Sub goo()
        Dim x = Sub()
                    xyz = 2
                    MyBase.abc = 1
                    PropXyz = 3
                    MyBase.Propabc = 4
                End Sub
        x.Invoke()
    End Sub
End Class
    </file>
</compilation>).VerifyIL("GenParent(Of t).goo", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub GenParent(Of t)._Lambda$__6-0()"
  IL_0007:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0011:  ret
}
]]>)
        End Sub

        ' Invoke MyBase in the class that implement multi-interface
        <Fact>
        Public Sub InvokeMyBaseImplementMultInterface()
            CompileAndVerify(
<compilation name="InvokeMyBaseImplementMultInterface">
    <file name="a.vb">
Imports System.Collections.Generic
Imports System
Class C1
    Implements IComparer(Of String)
    Implements System.Collections.Generic.IComparer(Of Integer)
    Public Function Compare1(ByVal x As String, ByVal y As String) As Integer Implements IComparer(Of String).Compare
        Return 0
    End Function
    Public Function Compare1(ByVal x As Integer, ByVal y As Integer) As Integer Implements System.Collections.Generic.IComparer(Of Integer).Compare
        Return 0
    End Function
    Sub GOO()
        Console.WriteLine(MyBase.ToString())
    End Sub
End Class
    </file>
</compilation>).VerifyIL("C1.GOO", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Object.ToString() As String"
  IL_0006:  call       "Sub System.Console.WriteLine(String)"
  IL_000b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub InvalidUseOfMyBase_4()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="InvalidUseOfMyBase">
    <file name="a.vb">
Class C1
    Sub GOO()
        MyBase
        MyBase()
        MyBase!FirstName = "DoDad"
        Dim Str = MyBase!FirstName
        With MyBase
        End With
    End Sub
End Class
    </file>
</compilation>).
            VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
                Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
                Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
                Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
                Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
                Diagnostic(ERRID.ERR_ExpectedProcedure, "MyBase"),
                Diagnostic(ERRID.ERR_ExpectedProcedure, "MyBase"))
        End Sub

        ' Invalid use of  MyBase 
        <Fact>
        Public Sub InvalidUseOfMyBase_5()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="InvalidUseOfMyBase">
    <file name="a.vb">
Class C1
    Sub GOO()
        MyBase!()
        MyBase.MyBase.Whatever()
        MyBase.MyClass.Whatever()
        MyClass.MyBase.Whatever()
        Me.MyBase.Whatever()
        MyBase.Me.Whatever()
        MyBase.Whatever()
    End Sub
End Class
    </file>
</compilation>)
            VerifyDiagnostics(comp,
                    Diagnostic(ERRID.ERR_NameNotDeclared1, "MyBase!").WithArguments("MyBase"),
                    Diagnostic(ERRID.ERR_NameNotMember2, "MyBase.MyBase").WithArguments("MyBase", "Object"),
                    Diagnostic(ERRID.ERR_NameNotMember2, "MyBase.MyClass").WithArguments("MyClass", "Object"),
                    Diagnostic(ERRID.ERR_NameNotMember2, "MyClass.MyBase").WithArguments("MyBase", "C1"),
                    Diagnostic(ERRID.ERR_NameNotMember2, "Me.MyBase").WithArguments("MyBase", "C1"),
                    Diagnostic(ERRID.ERR_NameNotMember2, "MyBase.Me").WithArguments("Me", "Object"),
                    Diagnostic(ERRID.ERR_NameNotMember2, "MyBase.Whatever").WithArguments("Whatever", "Object"))

        End Sub

#End Region

#Region "MyClass test"

        ' MyClass is a keyword, not a real object. MyClass cannot be assigned to a variable, passed to procedures, or used in an Is comparison. 
        <Fact()>
        Public Sub MyClassIsKeyWord()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MyClassIsKeyWord">
    <file name="a.vb">
Imports System
Class BaseClass
    Private Sub goo()
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
    Sub Test()
        Dim obj = MyClass
        Dim base = New BaseClass()
        MyClass = base
        Console.WriteLine(base Is MyClass)
        Console.WriteLine(TypeOf (MyClass))
        goo(MyClass)
    End Sub
    Sub goo(base As DerivedClass)
    End Sub
End Class
    </file>
</compilation>)

            ' Extra 4 errors in ROSLYN: ERR_MissingIsInTypeOf, ERR_UnrecognizedType, ERR_ExpectedRparen & ERR_LValueRequired
            VerifyDiagnostics(comp,
                        Diagnostic(ERRID.ERR_ExpectedDotAfterMyClass, "MyClass"),
    Diagnostic(ERRID.ERR_ExpectedDotAfterMyClass, "MyClass"),
    Diagnostic(ERRID.ERR_ExpectedDotAfterMyClass, "MyClass"),
    Diagnostic(ERRID.ERR_ExpectedDotAfterMyClass, "MyClass"),
    Diagnostic(ERRID.ERR_MissingIsInTypeOf, ""),
    Diagnostic(ERRID.ERR_UnrecognizedType, ""),
    Diagnostic(ERRID.ERR_ExpectedRparen, ""),
    Diagnostic(ERRID.ERR_ExpectedDotAfterMyClass, "MyClass"),
    Diagnostic(ERRID.ERR_LValueRequired, "MyClass"))

        End Sub

        ' MyClass refers to the containing class and its inherited members.
        <Fact>
        Public Sub MyClassRefsDerivedMethod()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MyClassRefsDerivedMethod">
    <file name="a.vb">
Class BaseClass
    Sub Test()
        Dim x = MyClass.goo()
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
    Public Function goo()
        goo = "STRING"
    End Function
End Class
    </file>
</compilation>)

            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_NameNotMember2, "MyClass.goo").WithArguments("goo", "BaseClass"))

        End Sub

        ' MyClass can be used as a qualifier for Shared members.
        <Fact>
        Public Sub MyClassUsedToQualifierSharedMember()
            CompileAndVerify(
<compilation name="MyClassUsedToQualifierSharedMember">
    <file name="a.vb">
Class BaseClass
    Private Sub goo()
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
    Shared age As Integer
    Sub Test()
        Dim x = MyClass.age
        x = MyClass.goo()
    End Sub
    Shared Function goo()
        goo = "hello"
    End Function
End Class
    </file>
</compilation>).VerifyIL("DerivedClass.Test", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldsfld     "DerivedClass.age As Integer"
  IL_0005:  pop
  IL_0006:  call       "Function DerivedClass.goo() As Object"
  IL_000b:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_0010:  pop
  IL_0011:  ret
}
]]>)
        End Sub

        ' Invoke a extension method from Myclass
        <Fact>
        Public Sub InvokeExtensionMethodFromMyClass()
            CompileAndVerify(
<compilation name="InvokeExtensionMethodFromMyClass">
    <file name="a.vb">
Imports System.Runtime.CompilerServices
Imports System
Class C1
    Sub Goo()
        Console.WriteLine(MyClass.Sum())
    End Sub
End Class
&lt;Extension()&gt;
Module MyExtensionModule
    &lt;Extension()&gt;
    Function Sum([Me] As C1) As Integer
        Sum = 1
    End Function
End Module
    </file>
</compilation>, references:={TestMetadata.Net40.SystemCore}).VerifyIL("C1.Goo", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function MyExtensionModule.Sum(C1) As Integer"
  IL_0006:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000b:  ret
}
]]>)
        End Sub

        ' MyClass used in structure
        <Fact>
        Public Sub MyClassUsedInStructure()
            CompileAndVerify(
<compilation name="MyClassUsedInStructure">
    <file name="a.vb">
Imports System
Structure s1
    Sub goo()
        Console.WriteLine(MyClass.ToString())
    End Sub
End Structure
    </file>
</compilation>).VerifyIL("s1.goo", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  constrained. "s1"
  IL_0007:  callvirt   "Function System.ValueType.ToString() As String"
  IL_000c:  call       "Sub System.Console.WriteLine(String)"
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub MyClassUsedInStructureWithMutation()
            CompileAndVerify(
<compilation name="MyClassUsedInStructure">
    <file name="a.vb">
Imports System
Structure S1
    Dim i As Integer

    Public Shared Sub Main()
        Dim s = New S1()
        s.Goo()
        s.Goo()
    End Sub

    Sub Goo()
        Console.Write(MyClass.M())
    End Sub

    Function M() As String
        i = i + 1
        Return i.ToString()
    End Function
End Structure
    </file>
</compilation>, expectedOutput:="12").VerifyIL("S1.Goo", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function S1.M() As String"
  IL_0006:  call       "Sub System.Console.Write(String)"
  IL_000b:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub MyClassUsedInStructureWithMutationInOverride()
            CompileAndVerify(
<compilation name="MyClassUsedInStructure">
    <file name="a.vb">
Imports System
Structure S1
    Dim i As Integer

    Public Shared Sub Main()
        Dim s = New S1()
        s.Goo()
        s.Goo()
    End Sub

    Sub Goo()
        Console.Write(MyClass.ToString())
    End Sub

    Public Overrides Function ToString() As String
        i = i + 1
        Return i.ToString()
    End Function
End Structure
    </file>
</compilation>, expectedOutput:="12").VerifyIL("S1.Goo", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  constrained. "S1"
  IL_0007:  callvirt   "Function Object.ToString() As String"
  IL_000c:  call       "Sub System.Console.Write(String)"
  IL_0011:  ret
}
]]>)
        End Sub

        ' 'MyClass' is valid only within an instance method.
        <Fact>
        Public Sub MyClassOnlyValidInInstanceMethod()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MyClassOnlyValidInInstanceMethod">
    <file name="a.vb">
Structure s1
    Shared Function goo()
        Dim x = MyClass.ToString()
    End Function
End Structure
    </file>
</compilation>)
            VerifyDiagnostics(comp, Diagnostic(ERRID.ERR_UseOfKeywordNotInInstanceMethod1, "MyClass").WithArguments("MyClass"),
                                    Diagnostic(ERRID.WRN_DefAsgNoRetValFuncRef1, "End Function").WithArguments("goo"))

        End Sub

        ' MyClass used in field initialize
        <Fact>
        Public Sub MyClassUsedInFieldInit()
            CompileAndVerify(
<compilation name="MyClassUsedInFieldInit">
    <file name="a.vb">
Class c1
    Dim x = MyClass.ToString()
End Class

Class base
End Class
Structure s1
    Class c1
        Inherits base
        Dim x = MyClass.ToString()
    End Class
End Structure
    </file>
</compilation>).VerifyIL("c1..ctor", <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  call       "Function Object.ToString() As String"
  IL_000d:  stfld      "c1.x As Object"
  IL_0012:  ret
}
]]>).VerifyIL("s1.c1..ctor", <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub base..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  call       "Function Object.ToString() As String"
  IL_000d:  stfld      "s1.c1.x As Object"
  IL_0012:  ret
}
]]>)
        End Sub

        ' MyClass can be used to qualify a method that is defined in a base class and that has no implementation of the method provided in that class. 
        <Fact>
        Public Sub MyClassUsedToRefMethodDefinedInBaseClass()
            CompileAndVerify(
<compilation name="MyClassUsedToRefMethodDefinedInBaseClass">
    <file name="a.vb">
Class BaseClass
    Public Function goo()
        goo = "STRING"
    End Function
End Class
Class DerivedClass
    Inherits BaseClass
    Sub Test()
        Dim x = MyClass.goo()
    End Sub
End Class
    </file>
</compilation>).VerifyIL("DerivedClass.Test", <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function BaseClass.goo() As Object"
  IL_0006:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000b:  pop
  IL_000c:  ret
}
]]>)
        End Sub

        ' Assigning to the Me/MyBase/MyClass variable in a Class/Module/Structure (should never work)
        <Fact>
        Public Sub AssignValueToMeMyBaseMyClass()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AssignValueToMeMyBaseMyClass">
    <file name="a.vb">
Class Base
End Class
Class Class1
    Inherits Base
    Sub Goo()
        Dim c1 As New Class1()
        Me = c1
        MyClass = c1
        MyBase = New Base()
    End Sub
End Class

Module M1
    Sub Goo()
        Me = Nothing
        MyClass = Nothing
        MyBase = Nothing
    End Sub
End Module

Structure S1
    Sub Goo()
        Me = Nothing
        MyClass = Nothing
        MyBase = Nothing
    End Sub
End Structure
    </file>
</compilation>)

            VerifyDiagnostics(comp,
                    Diagnostic(ERRID.ERR_ExpectedDotAfterMyClass, "MyClass"),
                    Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
                    Diagnostic(ERRID.ERR_ExpectedDotAfterMyClass, "MyClass"),
                    Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
                    Diagnostic(ERRID.ERR_ExpectedDotAfterMyClass, "MyClass"),
                    Diagnostic(ERRID.ERR_ExpectedDotAfterMyBase, "MyBase"),
                    Diagnostic(ERRID.ERR_LValueRequired, "Me"),
                    Diagnostic(ERRID.ERR_LValueRequired, "MyClass"),
                    Diagnostic(ERRID.ERR_LValueRequired, "MyBase"),
                    Diagnostic(ERRID.ERR_UseOfKeywordFromModule1, "Me").WithArguments("Me"),
                    Diagnostic(ERRID.ERR_MyClassNotInClass, "MyClass").WithArguments("MyClass"),
                    Diagnostic(ERRID.ERR_UseOfKeywordFromModule1, "MyBase").WithArguments("MyBase"),
                    Diagnostic(ERRID.ERR_LValueRequired, "Me"),
                    Diagnostic(ERRID.ERR_LValueRequired, "MyClass"),
                    Diagnostic(ERRID.ERR_UseOfKeywordFromStructure1, "MyBase").WithArguments("MyBase"))

        End Sub
#End Region

    End Class
End Namespace
