' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class MyBaseMyClassTests
        Inherits BasicTestBase

#Region "Simple method calls using MyClass/MyBase"

        <Fact>
        Public Sub OverloadResolutionWithMyBase1()
            Dim source =
<compilation name="OverloadResolutionWithMyBase1">
    <file name="a.vb">
Imports System

Class Base
    Public Sub S(x As Double)
        Console.WriteLine("Base.S(Double)")
    End Sub
    Public Sub S(x As String)
        Console.WriteLine("Base.S(String)")
    End Sub
End Class
Class Derived
    Inherits Base
    Public Overloads Sub S(x As Integer)
        Console.WriteLine("Derived.S(Integer)")
    End Sub
    Public Sub Test()
        MyBase.S(1)
    End Sub

    Public Shared Sub Main()
        Call New Derived().Test()
    End Sub
End Class
    </file>
</compilation>
            CompileAndVerify(source, expectedOutput:=<![CDATA[Base.S(Double)]]>).VerifyIL("Derived.Main",
            <![CDATA[
{
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  newobj     "Sub Derived..ctor()"
    IL_0005:  call       "Sub Derived.Test()"
    IL_000a:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub OverloadsWithMyClass1()
            Dim source =
<compilation name="OverloadsWithMyClass1">
    <file name="a.vb">
Imports System

Class Base
    Public Overridable Sub S(x As Integer)
        Console.WriteLine("Base.S(Integer)")
    End Sub
End Class

Class Derived
    Inherits Base
    Public Overloads Sub S(x As Double)
        Console.WriteLine("Derived.S(Double)")
    End Sub
    Public Overloads Sub S(x As String)
        Console.WriteLine("Derived.S(String)")
    End Sub

    Public Sub Test()
        MyClass.S(1)
    End Sub

    Public Shared Sub Main()
        Call New Derived2().Test()
    End Sub
End Class

Class Derived2
    Inherits Derived
End Class
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[Base.S(Integer)]]>).VerifyIL("Derived.Test",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  call       "Sub Base.S(Integer)"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub OverloadsWithMyClass2()
            Dim source =
<compilation name="OverloadsWithMyClass2">
    <file name="a.vb">
Imports System

Class Base
    Public Overridable Sub S(x As Integer)
        Console.WriteLine("Base.S(Integer)")
    End Sub
End Class

Class Derived
    Inherits Base
    Public Overrides Sub S(x As Integer)
        Console.WriteLine("Derived.S(Integer)")
    End Sub
    Public Overloads Sub S(x As Double)
        Console.WriteLine("Derived.S(Double)")
    End Sub
    Public Overloads Sub S(x As String)
        Console.WriteLine("Derived.S(String)")
    End Sub

    Public Sub Test()
        MyClass.S(1)
    End Sub

    Public Shared Sub Main()
        Call New Derived2().Test()
    End Sub
End Class

Class Derived2
    Inherits Derived
End Class
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[Derived.S(Integer)]]>).VerifyIL("Derived.Test",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  call       "Sub Derived.S(Integer)"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub OverloadsWithMyClass3()
            Dim source =
<compilation name="OverloadsWithMyClass3">
    <file name="a.vb">
Imports System

Structure STR
    Public Overloads Sub S()
        Console.WriteLine("STR.S()")
    End Sub
    Public Sub Test()
        MyClass.S()
        Me.S()
        Console.WriteLine(Me.ToString())
        Console.WriteLine(MyClass.ToString())
    End Sub

    Public Shared Sub Main()
        Dim a As STR = New STR()
        a.Test()
    End Sub
End Structure

    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
STR.S()
STR.S()
STR
STR
]]>).VerifyIL("STR.Test",
            <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub STR.S()"
  IL_0006:  ldarg.0
  IL_0007:  call       "Sub STR.S()"
  IL_000c:  ldarg.0
  IL_000d:  constrained. "STR"
  IL_0013:  callvirt   "Function System.ValueType.ToString() As String"
  IL_0018:  call       "Sub System.Console.WriteLine(String)"
  IL_001d:  ldarg.0
  IL_001e:  constrained. "STR"
  IL_0024:  callvirt   "Function System.ValueType.ToString() As String"
  IL_0029:  call       "Sub System.Console.WriteLine(String)"
  IL_002e:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CallMethodUsingMyBase2a()
            Dim source =
<compilation name="CallMethodUsingMyBase2a">
    <file name="a.vb">
Imports System

Module M1

    Class B
        Public Overridable Sub M()
            Console.WriteLine("B.M()")
        End Sub
    End Class

    Class D
        Inherits B

        Public Overrides Sub M()
            Console.WriteLine("D.M()")
        End Sub

        Public Sub Test()
            MyBase.M()
            MyClass.M()
            Me.M()
        End Sub
    End Class

    Class DD
        Inherits D

        Public Overrides Sub M()
            Console.WriteLine("DD.M()")
        End Sub
    End Class

    Public Sub Main()
        Call (New DD()).Test()
    End Sub

End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
B.M()
D.M()
DD.M()
]]>).VerifyIL("M1.D.Test",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub M1.B.M()"
  IL_0006:  ldarg.0
  IL_0007:  call       "Sub M1.D.M()"
  IL_000c:  ldarg.0
  IL_000d:  callvirt   "Sub M1.D.M()"
  IL_0012:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CallMethodUsingMyBase3()
            Dim source =
<compilation name="CallMethodUsingMyBase3">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Overridable Sub M()
            Console.WriteLine("B1.M()")
        End Sub
    End Class

    Class B2
        Inherits B1
        Public Overrides Sub M()
            Console.WriteLine("B2.M()")
        End Sub
    End Class

    Class D
        Inherits B2

        Public Overrides Sub M()
            Console.WriteLine("D.M()")
        End Sub

        Public Sub Test()
            MyBase.M()
        End Sub
    End Class

    Public Sub Main()
        Call (New D()).Test()
    End Sub

End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[B2.M()]]>).VerifyIL("M1.D.Test",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub M1.B2.M()"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CallMethodUsingMyBase4()
            Dim source =
<compilation name="CallMethodUsingMyBase4">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Overridable Sub M()
            Console.WriteLine("B1.M()")
        End Sub
    End Class

    Class B2
        Inherits B1
    End Class

    Class D
        Inherits B2

        Public Overrides Sub M()
            Console.WriteLine("D.M()")
        End Sub

        Public Sub Test()
            MyBase.M()
        End Sub
    End Class

    Public Sub Main()
        Call (New D()).Test()
    End Sub

End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[B1.M()]]>).VerifyIL("M1.D.Test",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub M1.B1.M()"
  IL_0006:  ret
}
]]>)
        End Sub

#End Region

#Region "Field and Property Initializers with MyClass/MyBase"

        <Fact>
        Public Sub FieldAndPropertyInitializersWithMyClass()
            Dim source =
<compilation name="FieldInitializersWithMyClass">
    <file name="a.vb">
Imports System
Class Base
    Public Overridable Function Goo(x As Integer) As String
        Return "Base.Goo(Integer)"
    End Function
End Class
Class Derived
    Inherits Base
    Public Overrides Function Goo(x As Integer) As String
        Return "Derived.Goo(Integer)"
    End Function

    Public FLD As String = MyClass.Goo(1)
    Public Property PROP As String = MyClass.Goo(1)

    Public Shared Sub Main()
        Console.WriteLine(New Derived2().FLD)
        Console.WriteLine(New Derived2().PROP)
    End Sub
End Class
Class Derived2
    Inherits Derived
    Public Overloads Function Goo(x As Integer) As String
        Return "Derived2.Goo(Integer)"
    End Function
End Class
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
Derived.Goo(Integer)
Derived.Goo(Integer)
]]>)
        End Sub

        <Fact>
        Public Sub FieldAndPropertyInitializersWithMyBase()
            Dim source =
<compilation name="FieldInitializersWithMyBase">
    <file name="a.vb">
Imports System
Class Base
    Public Overridable Function Goo(x As Integer) As String
        Return "Base.Goo(Integer)"
    End Function
End Class
Class Derived
    Inherits Base
    Public Overrides Function Goo(x As Integer) As String
        Return "Derived.Goo(Integer)"
    End Function

    Public FLD As String = MyBase.Goo(1)
    Public Property PROP As String = MyBase.Goo(1)

    Public Shared Sub Main()
        Console.WriteLine(New Derived().FLD)
        Console.WriteLine(New Derived().PROP)
    End Sub
End Class
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
Base.Goo(Integer)
Base.Goo(Integer)
]]>)
        End Sub

#End Region

#Region "Constructor calls using MyBase/MyClass"

        <Fact>
        Public Sub CallConstructorUsingMyBaseAndMyClass1()
            Dim source =
<compilation name="CallConstructorUsingMyBaseAndMyClass1">
    <file name="a.vb">
Imports System

Module M1

    Class B
        Public Sub New(i As Integer)
            Console.WriteLine("B.New(Integer)")
        End Sub
    End Class

    Class D
        Inherits B

        Public Sub New(i As Integer)
            MyBase.New(i)
            Console.WriteLine("D.New(Integer)")
        End Sub

        Public Sub New()
            Me.New(0)
            Console.WriteLine("D.New()")
        End Sub

        Public Sub New(b As Boolean)
            MyClass.New()
            Console.WriteLine("D.New(Boolean)")
        End Sub
    End Class

    Public Sub Main()
        Call (New D(True)).ToString()
    End Sub

End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
B.New(Integer)
D.New(Integer)
D.New()
D.New(Boolean)
]]>).VerifyIL("M1.D..ctor()",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  call       "Sub M1.D..ctor(Integer)"
  IL_0007:  ldstr      "D.New()"
  IL_000c:  call       "Sub System.Console.WriteLine(String)"
  IL_0011:  ret
}
]]>).VerifyIL("M1.D..ctor(Integer)",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       "Sub M1.B..ctor(Integer)"
  IL_0007:  ldstr      "D.New(Integer)"
  IL_000c:  call       "Sub System.Console.WriteLine(String)"
  IL_0011:  ret
}
]]>).VerifyIL("M1.D..ctor(Boolean)",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub M1.D..ctor()"
  IL_0006:  ldstr      "D.New(Boolean)"
  IL_000b:  call       "Sub System.Console.WriteLine(String)"
  IL_0010:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CallConstructorUsingMyBaseAndMyClass2()
            Dim source =
<compilation name="CallConstructorUsingMyBaseAndMyClass2">
    <file name="a.vb">
Imports System

Module M1

    Class B
        Public FLD As Integer = 123
        Public Sub New(i As Integer)
            Console.WriteLine(FLD)
        End Sub
    End Class

    Class D
        Inherits B

        Public Shadows FLD As Integer = MyBase.FLD + 321

        Public Sub New(i As Integer)
            MyBase.New(i)
            Console.WriteLine(MyBase.FLD)
        End Sub

        Public Sub New(b As Boolean)
            MyClass.New()
            Console.WriteLine(Me.FLD)
        End Sub

        Public Sub New()
            Me.New(0)
            Console.WriteLine(Me.FLD)
        End Sub
    End Class

    Public Sub Main()
        Call (New D(True)).ToString()
    End Sub

End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
123
123
444
444
]]>).VerifyIL("M1.D..ctor()",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  call       "Sub M1.D..ctor(Integer)"
  IL_0007:  ldarg.0
  IL_0008:  ldfld      "M1.D.FLD As Integer"
  IL_000d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0012:  ret
}
]]>).VerifyIL("M1.D..ctor(Integer)",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       "Sub M1.B..ctor(Integer)"
  IL_0007:  ldarg.0
  IL_0008:  ldarg.0
  IL_0009:  ldfld      "M1.B.FLD As Integer"
  IL_000e:  ldc.i4     0x141
  IL_0013:  add.ovf
  IL_0014:  stfld      "M1.D.FLD As Integer"
  IL_0019:  ldarg.0
  IL_001a:  ldfld      "M1.B.FLD As Integer"
  IL_001f:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0024:  ret
}
]]>).VerifyIL("M1.D..ctor(Boolean)",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub M1.D..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.D.FLD As Integer"
  IL_000c:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0011:  ret
}
]]>)
        End Sub

#End Region

#Region "Property access with MyBase/MyClass"

        <Fact>
        Public Sub CallPropertiesWithMyBaseAndMyClass1()
            Dim source =
<compilation name="CallPropertiesWithMyBaseAndMyClass1">
    <file name="a.vb">
Imports System

Module M1
    Class B
        Public Overridable Property PROP As String
            Get
                Console.WriteLine("B.PROP.Get")
                Return ""
            End Get
            Set(value As String)
                Console.WriteLine("B.PROP.Set")
            End Set
        End Property
    End Class

    Class D
        Inherits B

        Public Overrides Property PROP As String
            Get
                Console.WriteLine("D.PROP.Get")
                Return ""
            End Get
            Set(value As String)
                Console.WriteLine("D.PROP.Set")
            End Set
        End Property

        Public Sub Test()
            Me.PROP = MyBase.PROP
            MyBase.PROP = MyClass.PROP
            MyClass.PROP = Me.PROP
        End Sub

    End Class

    Class DD
        Inherits D

        Public Overrides Property PROP As String
            Get
                Console.WriteLine("DD.PROP.Get")
                Return ""
            End Get
            Set(value As String)
                Console.WriteLine("DD.PROP.Set")
            End Set
        End Property

    End Class

    Public Sub Main()
        Call (New DD()).Test()
    End Sub
End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
B.PROP.Get
DD.PROP.Set
D.PROP.Get
B.PROP.Set
DD.PROP.Get
D.PROP.Set	
]]>).VerifyIL("M1.D.Test",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function M1.B.get_PROP() As String"
  IL_0007:  callvirt   "Sub M1.D.set_PROP(String)"
  IL_000c:  ldarg.0
  IL_000d:  ldarg.0
  IL_000e:  call       "Function M1.D.get_PROP() As String"
  IL_0013:  call       "Sub M1.B.set_PROP(String)"
  IL_0018:  ldarg.0
  IL_0019:  ldarg.0
  IL_001a:  callvirt   "Function M1.D.get_PROP() As String"
  IL_001f:  call       "Sub M1.D.set_PROP(String)"
  IL_0024:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CallPropertiesWithMyBaseAndMyClass2()
            Dim source =
<compilation name="CallPropertiesWithMyBaseAndMyClass2">
    <file name="a.vb">
Imports System

Module M1
    Class B
        Protected Overridable Property PROP As String
            Get
                Console.WriteLine("B.PROP.Get")
                Return ""
            End Get
            Set(value As String)
                Console.WriteLine("B.PROP.Set")
            End Set
        End Property
    End Class

    Class D
        Inherits B

        Public Sub Test()
            Me.PROP = MyBase.PROP
            MyBase.PROP = MyClass.PROP
            MyClass.PROP = Me.PROP
        End Sub

    End Class

    Class DD
        Inherits D

        Protected Overrides Property PROP As String
            Get
                Console.WriteLine("DD.PROP.Get")
                Return ""
            End Get
            Set(value As String)
                Console.WriteLine("DD.PROP.Set")
            End Set
        End Property

    End Class

    Public Sub Main()
        Call (New DD()).Test()
    End Sub
End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
B.PROP.Get
DD.PROP.Set
B.PROP.Get
B.PROP.Set
DD.PROP.Get
B.PROP.Set	
]]>).VerifyIL("M1.D.Test",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function M1.B.get_PROP() As String"
  IL_0007:  callvirt   "Sub M1.B.set_PROP(String)"
  IL_000c:  ldarg.0
  IL_000d:  ldarg.0
  IL_000e:  call       "Function M1.B.get_PROP() As String"
  IL_0013:  call       "Sub M1.B.set_PROP(String)"
  IL_0018:  ldarg.0
  IL_0019:  ldarg.0
  IL_001a:  callvirt   "Function M1.B.get_PROP() As String"
  IL_001f:  call       "Sub M1.B.set_PROP(String)"
  IL_0024:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CallPropertiesWithMyBaseAndMyClass3()
            Dim source =
<compilation name="CallPropertiesWithMyBaseAndMyClass3">
    <file name="a.vb">
Imports System

Module M1
    Class B
        Public Overridable Property PROP As String
            Get
                Console.WriteLine("B.PROP.Get")
                Return ""
            End Get
            Set(value As String)
                Console.WriteLine("B.PROP.Set")
            End Set
        End Property
    End Class

    Class D
        Inherits B

        Public Sub Test()
            Dim a as Action = Sub ()
                                Me.PROP = MyBase.PROP
                                MyBase.PROP = MyClass.PROP
                                MyClass.PROP = Me.PROP
                              End Sub
            a()
        End Sub

    End Class

    Class DD
        Inherits D

        Public Overrides Property PROP As String
            Get
                Console.WriteLine("DD.PROP.Get")
                Return ""
            End Get
            Set(value As String)
                Console.WriteLine("DD.PROP.Set")
            End Set
        End Property

    End Class

    Public Sub Main()
        Call (New DD()).Test()
    End Sub
End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
B.PROP.Get
DD.PROP.Set
B.PROP.Get
B.PROP.Set
DD.PROP.Get
B.PROP.Set	
]]>)
            c.VerifyIL("M1.D._Lambda$__1-0",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function M1.B.get_PROP() As String"
  IL_0007:  callvirt   "Sub M1.B.set_PROP(String)"
  IL_000c:  ldarg.0
  IL_000d:  ldarg.0
  IL_000e:  call       "Function M1.B.get_PROP() As String"
  IL_0013:  call       "Sub M1.B.set_PROP(String)"
  IL_0018:  ldarg.0
  IL_0019:  ldarg.0
  IL_001a:  callvirt   "Function M1.B.get_PROP() As String"
  IL_001f:  call       "Sub M1.B.set_PROP(String)"
  IL_0024:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CallPropertiesWithMyBaseAndMyClass4()
            Dim source =
<compilation name="CallPropertiesWithMyBaseAndMyClass4">
    <file name="a.vb">
Imports System

Module M1
    Class B
        Protected Overridable Property PROP As String
            Get
                Console.WriteLine("B.PROP.Get")
                Return ""
            End Get
            Set(value As String)
                Console.WriteLine("B.PROP.Set")
            End Set
        End Property
    End Class

    Class D
        Inherits B

        Protected Overrides Property PROP As String
            Get
                Console.WriteLine("D.PROP.Get")
                Return ""
            End Get
            Set(value As String)
                Console.WriteLine("D.PROP.Set")
            End Set
        End Property

        Public Sub Test()
            Dim x = ""
            Dim a as Action = Sub ()
                                Me.PROP = MyBase.PROP &amp; x
                                MyBase.PROP = MyClass.PROP &amp; x
                                MyClass.PROP = Me.PROP &amp; x
                              End Sub
            a()
        End Sub

    End Class

    Class DD
        Inherits D

        Protected Overrides Property PROP As String
            Get
                Console.WriteLine("DD.PROP.Get")
                Return ""
            End Get
            Set(value As String)
                Console.WriteLine("DD.PROP.Set")
            End Set
        End Property

    End Class

    Public Sub Main()
        Call (New DD()).Test()
    End Sub
End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source, expectedOutput:=<![CDATA[
B.PROP.Get
DD.PROP.Set
D.PROP.Get
B.PROP.Set
DD.PROP.Get
D.PROP.Set	
]]>)
            c.VerifyIL("M1.D._Closure$__4-0._Lambda$__0",
            <![CDATA[
{
  // Code size      100 (0x64)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.D._Closure$__4-0.$VB$Me As M1.D"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.D._Closure$__4-0.$VB$Me As M1.D"
  IL_000c:  call       "Function M1.D.$VB$ClosureStub_get_PROP_MyBase() As String"
  IL_0011:  ldarg.0
  IL_0012:  ldfld      "M1.D._Closure$__4-0.$VB$Local_x As String"
  IL_0017:  call       "Function String.Concat(String, String) As String"
  IL_001c:  callvirt   "Sub M1.D.set_PROP(String)"
  IL_0021:  ldarg.0
  IL_0022:  ldfld      "M1.D._Closure$__4-0.$VB$Me As M1.D"
  IL_0027:  ldarg.0
  IL_0028:  ldfld      "M1.D._Closure$__4-0.$VB$Me As M1.D"
  IL_002d:  call       "Function M1.D.$VB$ClosureStub_get_PROP_MyClass() As String"
  IL_0032:  ldarg.0
  IL_0033:  ldfld      "M1.D._Closure$__4-0.$VB$Local_x As String"
  IL_0038:  call       "Function String.Concat(String, String) As String"
  IL_003d:  call       "Sub M1.D.$VB$ClosureStub_set_PROP_MyBase(String)"
  IL_0042:  ldarg.0
  IL_0043:  ldfld      "M1.D._Closure$__4-0.$VB$Me As M1.D"
  IL_0048:  ldarg.0
  IL_0049:  ldfld      "M1.D._Closure$__4-0.$VB$Me As M1.D"
  IL_004e:  callvirt   "Function M1.D.get_PROP() As String"
  IL_0053:  ldarg.0
  IL_0054:  ldfld      "M1.D._Closure$__4-0.$VB$Local_x As String"
  IL_0059:  call       "Function String.Concat(String, String) As String"
  IL_005e:  call       "Sub M1.D.$VB$ClosureStub_set_PROP_MyClass(String)"
  IL_0063:  ret
}
]]>)
        End Sub

#End Region

#Region "Access fields using MyBase"

        <Fact>
        Public Sub AccessFieldUsingMyBase1()
            Dim source =
<compilation name="AccessFieldUsingMyBase1">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Protected FLD As Integer = 123
    End Class

    Class B2
        Inherits B1
    End Class

    Class D
        Inherits B2

        Protected Shadows FLD As Integer = 321

        Public Sub Test()
            Me.FLD = MyBase.FLD
            MyBase.FLD = Me.FLD
        End Sub
    End Class

    Public Sub Main()
        Call (New D()).Test()
    End Sub

End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[]]>).VerifyIL("M1.D.Test",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      "M1.B1.FLD As Integer"
  IL_0007:  stfld      "M1.D.FLD As Integer"
  IL_000c:  ldarg.0
  IL_000d:  ldarg.0
  IL_000e:  ldfld      "M1.D.FLD As Integer"
  IL_0013:  stfld      "M1.B1.FLD As Integer"
  IL_0018:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub AccessFieldUsingMyBase2()
            Dim source =
<compilation name="AccessFieldUsingMyBase2">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Protected FLD As Integer = 123
    End Class

    Class B2
        Inherits B1
    End Class

    Class D
        Inherits B2

        Protected Shadows FLD As Integer = 321

        Public Sub Test()
            Console.Write(DirectCast(Function() MyBase.FLD, Func(Of Integer))())
            Console.Write("-")
            Console.Write(DirectCast(Function() Me.FLD, Func(Of Integer))())
        End Sub
    End Class

    Public Sub Main()
        Call (New D()).Test()
    End Sub

End Module
    </file>
</compilation>

            Dim c = CompileAndVerify(source, expectedOutput:=<![CDATA[123-321]]>)

            c.VerifyIL("M1.D.Test",
            <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.D._Lambda$__2-0() As Integer"
  IL_0007:  newobj     "Sub System.Func(Of Integer)..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Function System.Func(Of Integer).Invoke() As Integer"
  IL_0011:  call       "Sub System.Console.Write(Integer)"
  IL_0016:  ldstr      "-"
  IL_001b:  call       "Sub System.Console.Write(String)"
  IL_0020:  ldarg.0
  IL_0021:  ldftn      "Function M1.D._Lambda$__2-1() As Integer"
  IL_0027:  newobj     "Sub System.Func(Of Integer)..ctor(Object, System.IntPtr)"
  IL_002c:  callvirt   "Function System.Func(Of Integer).Invoke() As Integer"
  IL_0031:  call       "Sub System.Console.Write(Integer)"
  IL_0036:  ret
}
]]>)
            c.VerifyIL("M1.D._Lambda$__2-0",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B1.FLD As Integer"
  IL_0006:  ret
}
]]>)
            c.VerifyIL("M1.D._Lambda$__2-1",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.D.FLD As Integer"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub AccessFieldUsingMyBase3()
            Dim source =
<compilation name="AccessFieldUsingMyBase3">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Protected FLD As Integer = 123
    End Class

    Class B2
        Inherits B1
    End Class

    Class D
        Inherits B2

        Protected Shadows FLD As Integer = 321

        Public Sub Test()
            Dim _add = 1
            Console.Write(DirectCast(Function() MyBase.FLD + _add, Func(Of Integer))())
            Console.Write("-")
            Console.Write(DirectCast(Function() Me.FLD + _add, Func(Of Integer))())
        End Sub
    End Class

    Public Sub Main()
        Call (New D()).Test()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source, expectedOutput:=<![CDATA[124-322]]>)
            c.VerifyIL("M1.D.Test",
            <![CDATA[
{
  // Code size       73 (0x49)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.D._Closure$__2-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.D._Closure$__2-0.$VB$Me As M1.D"
  IL_000c:  dup
  IL_000d:  ldc.i4.1
  IL_000e:  stfld      "M1.D._Closure$__2-0.$VB$Local__add As Integer"
  IL_0013:  dup
  IL_0014:  ldftn      "Function M1.D._Closure$__2-0._Lambda$__0() As Integer"
  IL_001a:  newobj     "Sub System.Func(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001f:  callvirt   "Function System.Func(Of Integer).Invoke() As Integer"
  IL_0024:  call       "Sub System.Console.Write(Integer)"
  IL_0029:  ldstr      "-"
  IL_002e:  call       "Sub System.Console.Write(String)"
  IL_0033:  ldftn      "Function M1.D._Closure$__2-0._Lambda$__1() As Integer"
  IL_0039:  newobj     "Sub System.Func(Of Integer)..ctor(Object, System.IntPtr)"
  IL_003e:  callvirt   "Function System.Func(Of Integer).Invoke() As Integer"
  IL_0043:  call       "Sub System.Console.Write(Integer)"
  IL_0048:  ret
}
]]>)
            c.VerifyIL("M1.D._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.D._Closure$__2-0.$VB$Me As M1.D"
  IL_0006:  ldfld      "M1.B1.FLD As Integer"
  IL_000b:  ldarg.0
  IL_000c:  ldfld      "M1.D._Closure$__2-0.$VB$Local__add As Integer"
  IL_0011:  add.ovf
  IL_0012:  ret
}
]]>)
            c.VerifyIL("M1.D._Closure$__2-0._Lambda$__1",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.D._Closure$__2-0.$VB$Me As M1.D"
  IL_0006:  ldfld      "M1.D.FLD As Integer"
  IL_000b:  ldarg.0
  IL_000c:  ldfld      "M1.D._Closure$__2-0.$VB$Local__add As Integer"
  IL_0011:  add.ovf
  IL_0012:  ret
}
]]>)
        End Sub

#End Region

#Region "Capturing Me/MyClass/MyBase within closure type"

        <Fact>
        Public Sub CapturingMeReference1()
            Dim source =
<compilation name="CapturingMeReference1">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Overridable Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Public Overrides Function F() As String
            Return "B2::F"
        End Function

        Public Sub TestMe()
            Dim s As String = "     Me: "
            Dim f As Func(Of String) = Function() s &amp; Me.F()
            Console.WriteLine(f())
        End Sub

        Public Sub TestMyClass()
            Dim s As String = "MyClass: "
            Dim f As Func(Of String) = Function() s &amp; MyClass.F()
            Console.WriteLine(f())
        End Sub

        Public Sub TestMyBase()
            Dim s As String = " MyBase: "
            Dim f As Func(Of String) = Function() s &amp; MyBase.F()
            Console.WriteLine(f())
        End Sub
    End Class

    Class D
        Inherits B2

        Public Overrides Function F() As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
     Me: D::F
MyClass: B2::F
 MyBase: B1::F
]]>)
            c.VerifyIL("M1.B2.TestMe",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.B2._Closure$__2-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldstr      "     Me: "
  IL_0012:  stfld      "M1.B2._Closure$__2-0.$VB$Local_s As String"
  IL_0017:  ldftn      "Function M1.B2._Closure$__2-0._Lambda$__0() As String"
  IL_001d:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0022:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0027:  call       "Sub System.Console.WriteLine(String)"
  IL_002c:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__2-0.$VB$Local_s As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_000c:  callvirt   "Function M1.B2.F() As String"
  IL_0011:  call       "Function String.Concat(String, String) As String"
  IL_0016:  ret
}
]]>)
            c.VerifyIL("M1.B2.TestMyClass",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.B2._Closure$__3-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldstr      "MyClass: "
  IL_0012:  stfld      "M1.B2._Closure$__3-0.$VB$Local_s As String"
  IL_0017:  ldftn      "Function M1.B2._Closure$__3-0._Lambda$__0() As String"
  IL_001d:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0022:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0027:  call       "Sub System.Console.WriteLine(String)"
  IL_002c:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__3-0._Lambda$__0",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__3-0.$VB$Local_s As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_000c:  call       "Function M1.B2.$VB$ClosureStub_F_MyClass() As String"
  IL_0011:  call       "Function String.Concat(String, String) As String"
  IL_0016:  ret
}
]]>)
            c.VerifyIL("M1.B2.$VB$ClosureStub_F_MyClass",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function M1.B2.F() As String"
  IL_0006:  ret
}
]]>)
            c.VerifyIL("M1.B2.TestMyBase",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.B2._Closure$__4-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.B2._Closure$__4-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldstr      " MyBase: "
  IL_0012:  stfld      "M1.B2._Closure$__4-0.$VB$Local_s As String"
  IL_0017:  ldftn      "Function M1.B2._Closure$__4-0._Lambda$__0() As String"
  IL_001d:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0022:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0027:  call       "Sub System.Console.WriteLine(String)"
  IL_002c:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__4-0._Lambda$__0",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__4-0.$VB$Local_s As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__4-0.$VB$Me As M1.B2"
  IL_000c:  call       "Function M1.B2.$VB$ClosureStub_F_MyBase() As String"
  IL_0011:  call       "Function String.Concat(String, String) As String"
  IL_0016:  ret
}
]]>)
            c.VerifyIL("M1.B2.$VB$ClosureStub_F_MyBase",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function M1.B1.F() As String"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CapturingMeReference1x()
            Dim source =
<compilation name="CapturingMeReference1x">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Overridable Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Public Sub TestMe()
            Dim s As String = "     Me: "
            Dim f As Func(Of String) = Function() s &amp; Me.F()
            Console.WriteLine(f())
        End Sub

        Public Sub TestMyClass()
            Dim s As String = "MyClass: "
            Dim f As Func(Of String) = Function() s &amp; MyClass.F()
            Console.WriteLine(f())
        End Sub

        Public Sub TestMyBase()
            Dim s As String = " MyBase: "
            Dim f As Func(Of String) = Function() s &amp; MyBase.F()
            Console.WriteLine(f())
        End Sub
    End Class

    Class D
        Inherits B2

        Public Overrides Function F() As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source, expectedOutput:=<![CDATA[
     Me: D::F
MyClass: B1::F
 MyBase: B1::F  
]]>)
            c.VerifyIL("M1.B2.TestMe",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.B2._Closure$__1-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.B2._Closure$__1-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldstr      "     Me: "
  IL_0012:  stfld      "M1.B2._Closure$__1-0.$VB$Local_s As String"
  IL_0017:  ldftn      "Function M1.B2._Closure$__1-0._Lambda$__0() As String"
  IL_001d:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0022:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0027:  call       "Sub System.Console.WriteLine(String)"
  IL_002c:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__1-0._Lambda$__0",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__1-0.$VB$Local_s As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__1-0.$VB$Me As M1.B2"
  IL_000c:  callvirt   "Function M1.B1.F() As String"
  IL_0011:  call       "Function String.Concat(String, String) As String"
  IL_0016:  ret
}
]]>)
            c.VerifyIL("M1.B2.TestMyClass",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.B2._Closure$__2-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldstr      "MyClass: "
  IL_0012:  stfld      "M1.B2._Closure$__2-0.$VB$Local_s As String"
  IL_0017:  ldftn      "Function M1.B2._Closure$__2-0._Lambda$__0() As String"
  IL_001d:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0022:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0027:  call       "Sub System.Console.WriteLine(String)"
  IL_002c:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__2-0.$VB$Local_s As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_000c:  call       "Function M1.B2.$VB$ClosureStub_F_MyBase() As String"
  IL_0011:  call       "Function String.Concat(String, String) As String"
  IL_0016:  ret
}
]]>)
            c.VerifyIL("M1.B2.TestMyBase",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.B2._Closure$__3-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldstr      " MyBase: "
  IL_0012:  stfld      "M1.B2._Closure$__3-0.$VB$Local_s As String"
  IL_0017:  ldftn      "Function M1.B2._Closure$__3-0._Lambda$__0() As String"
  IL_001d:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0022:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0027:  call       "Sub System.Console.WriteLine(String)"
  IL_002c:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__3-0._Lambda$__0",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__3-0.$VB$Local_s As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_000c:  call       "Function M1.B2.$VB$ClosureStub_F_MyBase() As String"
  IL_0011:  call       "Function String.Concat(String, String) As String"
  IL_0016:  ret
}
]]>)
            c.VerifyIL("M1.B2.$VB$ClosureStub_F_MyBase",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function M1.B1.F() As String"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CapturingMeReference1_NotVirtual()
            Dim source =
<compilation name="CapturingMeReference1">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Public Shadows Function F() As String
            Return "B2::F"
        End Function

        Public Sub TestMe()
            Dim s As String = "     Me: "
            Dim f As Func(Of String) = Function() s &amp; Me.F()
            Console.WriteLine(f())
        End Sub

        Public Sub TestMyClass()
            Dim s As String = "MyClass: "
            Dim f As Func(Of String) = Function() s &amp; MyClass.F()
            Console.WriteLine(f())
        End Sub

        Public Sub TestMyBase()
            Dim s As String = " MyBase: "
            Dim f As Func(Of String) = Function() s &amp; MyBase.F()
            Console.WriteLine(f())
        End Sub
    End Class

    Class D
        Inherits B2

        Public Shadows Function F() As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
     Me: B2::F
MyClass: B2::F
 MyBase: B1::F
]]>)
            c.VerifyIL("M1.B2.TestMe",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.B2._Closure$__2-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldstr      "     Me: "
  IL_0012:  stfld      "M1.B2._Closure$__2-0.$VB$Local_s As String"
  IL_0017:  ldftn      "Function M1.B2._Closure$__2-0._Lambda$__0() As String"
  IL_001d:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0022:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0027:  call       "Sub System.Console.WriteLine(String)"
  IL_002c:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__2-0.$VB$Local_s As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_000c:  call       "Function M1.B2.F() As String"
  IL_0011:  call       "Function String.Concat(String, String) As String"
  IL_0016:  ret
}
]]>)
            c.VerifyIL("M1.B2.TestMyClass",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.B2._Closure$__3-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldstr      "MyClass: "
  IL_0012:  stfld      "M1.B2._Closure$__3-0.$VB$Local_s As String"
  IL_0017:  ldftn      "Function M1.B2._Closure$__3-0._Lambda$__0() As String"
  IL_001d:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0022:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0027:  call       "Sub System.Console.WriteLine(String)"
  IL_002c:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__3-0._Lambda$__0",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__3-0.$VB$Local_s As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_000c:  call       "Function M1.B2.F() As String"
  IL_0011:  call       "Function String.Concat(String, String) As String"
  IL_0016:  ret
}
]]>)
            c.VerifyIL("M1.B2.TestMyBase",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.B2._Closure$__4-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.B2._Closure$__4-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldstr      " MyBase: "
  IL_0012:  stfld      "M1.B2._Closure$__4-0.$VB$Local_s As String"
  IL_0017:  ldftn      "Function M1.B2._Closure$__4-0._Lambda$__0() As String"
  IL_001d:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0022:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0027:  call       "Sub System.Console.WriteLine(String)"
  IL_002c:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__4-0._Lambda$__0",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__4-0.$VB$Local_s As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__4-0.$VB$Me As M1.B2"
  IL_000c:  call       "Function M1.B1.F() As String"
  IL_0011:  call       "Function String.Concat(String, String) As String"
  IL_0016:  ret
}
]]>)
        End Sub

#End Region

#Region "Capturing Me/MyClass/MyBase within lambda in the same type"

        <Fact>
        Public Sub CapturingMeReference2()
            Dim source =
<compilation name="CapturingMeReference2">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Overridable Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Public Overrides Function F() As String
            Return "B2::F"
        End Function

        Public Sub TestMe()
            Dim f As Func(Of String) = Function() Me.F()
            Console.WriteLine(f())
        End Sub

        Public Sub TestMyClass()
            Dim f As Func(Of String) = Function() MyClass.F()
            Console.WriteLine(f())
        End Sub

        Public Sub TestMyBase()
            Dim f As Func(Of String) = Function() MyBase.F()
            Console.WriteLine(f())
        End Sub
    End Class

    Class D
        Inherits B2

        Public Overrides Function F() As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
D::F
B2::F
B1::F
]]>)
            c.VerifyIL("M1.B2.TestMe",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B2._Lambda$__2-0() As String"
  IL_0007:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0011:  call       "Sub System.Console.WriteLine(String)"
  IL_0016:  ret
}
]]>)
            c.VerifyIL("M1.B2._Lambda$__2-0",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  callvirt   "Function M1.B2.F() As String"
  IL_0006:  ret
}
]]>)
            c.VerifyIL("M1.B2.TestMyClass",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B2._Lambda$__3-0() As String"
  IL_0007:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0011:  call       "Sub System.Console.WriteLine(String)"
  IL_0016:  ret
}
]]>)
            c.VerifyIL("M1.B2._Lambda$__3-0",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function M1.B2.F() As String"
  IL_0006:  ret
}
]]>)
            c.VerifyIL("M1.B2.TestMyBase",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B2._Lambda$__4-0() As String"
  IL_0007:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0011:  call       "Sub System.Console.WriteLine(String)"
  IL_0016:  ret
}
]]>)
            c.VerifyIL("M1.B2._Lambda$__4-0",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function M1.B1.F() As String"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CapturingMeReference2x()
            Dim source =
<compilation name="CapturingMeReference2x">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Overridable Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Public Sub TestMe()
            Dim f As Func(Of String) = Function() Me.F()
            Console.WriteLine(f())
        End Sub

        Public Sub TestMyClass()
            Dim f As Func(Of String) = Function() MyClass.F()
            Console.WriteLine(f())
        End Sub

        Public Sub TestMyBase()
            Dim f As Func(Of String) = Function() MyBase.F()
            Console.WriteLine(f())
        End Sub
    End Class

    Class D
        Inherits B2

        Public Overrides Function F() As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
D::F
B1::F
B1::F
]]>)
            c.VerifyIL("M1.B2._Lambda$__1-0",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  callvirt   "Function M1.B1.F() As String"
  IL_0006:  ret
}
]]>)
            c.VerifyIL("M1.B2._Lambda$__2-0",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function M1.B1.F() As String"
  IL_0006:  ret
}
]]>)
            c.VerifyIL("M1.B2._Lambda$__3-0",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function M1.B1.F() As String"
  IL_0006:  ret
}
]]>)
        End Sub

#End Region

#Region "AddressOf"

        <Fact>
        Public Sub AddressOfWithMeReference1()
            Dim source =
<compilation name="AddressOfWithMeReference1">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Overridable Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Public Overrides Function F() As String
            Return "B2::F"
        End Function

        Public Sub TestMe()
            Dim f1 As Func(Of String) = AddressOf Me.F
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyClass()
            Dim f1 As Func(Of String) = AddressOf MyClass.F
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyBase()
            Dim f1 As Func(Of String) = AddressOf MyBase.F
            Console.WriteLine(f1())
        End Sub
    End Class

    Class D
        Inherits B2

        Public Overrides Function F() As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
D::F
B2::F
B1::F
]]>).VerifyIL("M1.B2.TestMe",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  "Function M1.B2.F() As String"
  IL_0008:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000d:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0012:  call       "Sub System.Console.WriteLine(String)"
  IL_0017:  ret
}
]]>).VerifyIL("M1.B2.TestMyClass",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B2.F() As String"
  IL_0007:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0011:  call       "Sub System.Console.WriteLine(String)"
  IL_0016:  ret
}
]]>).VerifyIL("M1.B2.TestMyBase",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B1.F() As String"
  IL_0007:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0011:  call       "Sub System.Console.WriteLine(String)"
  IL_0016:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub AddressOfWithMeReference1x()
            Dim source =
<compilation name="AddressOfWithMeReference1x">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Overridable Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Public Sub TestMe()
            Dim f1 As Func(Of String) = AddressOf Me.F
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyClass()
            Dim f1 As Func(Of String) = AddressOf MyClass.F
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyBase()
            Dim f1 As Func(Of String) = AddressOf MyBase.F
            Console.WriteLine(f1())
        End Sub
    End Class

    Class D
        Inherits B2

        Public Overrides Function F() As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
D::F
B1::F
B1::F
]]>).VerifyIL("M1.B2.TestMe",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  "Function M1.B1.F() As String"
  IL_0008:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000d:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0012:  call       "Sub System.Console.WriteLine(String)"
  IL_0017:  ret
}
]]>).VerifyIL("M1.B2.TestMyClass",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B1.F() As String"
  IL_0007:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0011:  call       "Sub System.Console.WriteLine(String)"
  IL_0016:  ret
}
]]>).VerifyIL("M1.B2.TestMyBase",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B1.F() As String"
  IL_0007:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0011:  call       "Sub System.Console.WriteLine(String)"
  IL_0016:  ret
}
]]>)
        End Sub

#End Region

#Region "AddressOf In Lambda, the same class"

        <Fact>
        Public Sub AddressOfWithMeReferenceInLambdas1()
            Dim source =
<compilation name="AddressOfWithMeReferenceInLambdas1">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Protected Overridable Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Protected Overrides Function F() As String
            Return "B2::F"
        End Function

        Public Sub TestMe()
            Dim f1 As Func(Of Func(Of String)) = Function() AddressOf Me.F
            Console.WriteLine(f1()())
        End Sub

        Public Sub TestMyClass()
            Dim f1 As Func(Of Func(Of String)) = Function() AddressOf MyClass.F
            Console.WriteLine(f1()())
        End Sub

        Public Sub TestMyBase()
            Dim f1 As Func(Of Func(Of String)) = Function() AddressOf MyBase.F
            Console.WriteLine(f1()())
        End Sub
    End Class

    Class D
        Inherits B2

        Protected Overrides Function F() As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source, expectedOutput:=<![CDATA[
D::F
B2::F
B1::F
]]>)
            c.VerifyIL("M1.B2.TestMe",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B2._Lambda$__2-0() As System.Func(Of String)"
  IL_0007:  newobj     "Sub System.Func(Of System.Func(Of String))..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Function System.Func(Of System.Func(Of String)).Invoke() As System.Func(Of String)"
  IL_0011:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ret
}
]]>)
            c.VerifyIL("M1.B2._Lambda$__2-0",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  "Function M1.B2.F() As String"
  IL_0008:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000d:  ret
}
]]>)
            c.VerifyIL("M1.B2.TestMyClass",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B2._Lambda$__3-0() As System.Func(Of String)"
  IL_0007:  newobj     "Sub System.Func(Of System.Func(Of String))..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Function System.Func(Of System.Func(Of String)).Invoke() As System.Func(Of String)"
  IL_0011:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ret
}
]]>)
            c.VerifyIL("M1.B2._Lambda$__3-0",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B2.F() As String"
  IL_0007:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000c:  ret
}
]]>)
            c.VerifyIL("M1.B2.TestMyBase",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B2._Lambda$__4-0() As System.Func(Of String)"
  IL_0007:  newobj     "Sub System.Func(Of System.Func(Of String))..ctor(Object, System.IntPtr)"
  IL_000c:  callvirt   "Function System.Func(Of System.Func(Of String)).Invoke() As System.Func(Of String)"
  IL_0011:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ret
}
]]>)
            c.VerifyIL("M1.B2._Lambda$__4-0",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B1.F() As String"
  IL_0007:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000c:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub AddressOfWithMeReferenceInLambdas1x()
            Dim source =
<compilation name="AddressOfWithMeReferenceInLambdas1x">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Overridable Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Public Sub TestMe()
            Dim f1 As Func(Of Func(Of String)) = Function() AddressOf Me.F
            Console.WriteLine(f1()())
        End Sub

        Public Sub TestMyClass()
            Dim f1 As Func(Of Func(Of String)) = Function() AddressOf MyClass.F
            Console.WriteLine(f1()())
        End Sub

        Public Sub TestMyBase()
            Dim f1 As Func(Of Func(Of String)) = Function() AddressOf MyBase.F
            Console.WriteLine(f1()())
        End Sub
    End Class

    Class D
        Inherits B2

        Public Overrides Function F() As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>

            Dim c = CompileAndVerify(source, expectedOutput:=<![CDATA[
D::F
B1::F
B1::F
]]>)
            c.VerifyIL("M1.B2._Lambda$__1-0",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  "Function M1.B1.F() As String"
  IL_0008:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000d:  ret
}
]]>)
            c.VerifyIL("M1.B2._Lambda$__2-0",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B1.F() As String"
  IL_0007:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000c:  ret
}
]]>)
            c.VerifyIL("M1.B2._Lambda$__3-0",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B1.F() As String"
  IL_0007:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_000c:  ret
}
]]>)
        End Sub

#End Region

#Region "AddressOf in lambda in closure class"

        <Fact>
        Public Sub AddressOfWithMeReferenceInLambdas2()
            Dim source =
<compilation name="AddressOfWithMeReferenceInLambdas2">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Protected Overridable Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Protected Overrides Function F() As String
            Return "B2::F"
        End Function

        Public Sub TestMe()
            Dim prefix = "->"
            Dim f1 As Func(Of String) = Function() prefix &amp; DirectCast(AddressOf Me.F, Func(Of String))()
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyClass()
            Dim prefix = "->"
            Dim f1 As Func(Of String) = Function() prefix &amp; DirectCast(AddressOf MyClass.F, Func(Of String))()
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyBase()
            Dim prefix = "->"
            Dim f1 As Func(Of String) = Function() prefix &amp; DirectCast(AddressOf MyBase.F, Func(Of String))()
            Console.WriteLine(f1())
        End Sub
    End Class

    Class D
        Inherits B2

        Protected Overrides Function F() As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>

            Dim c = CompileAndVerify(source, expectedOutput:=<![CDATA[
->D::F
->B2::F
->B1::F
]]>)
            c.VerifyIL("M1.B2._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__2-0.$VB$Local_prefix As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldvirtftn  "Function M1.B2.F() As String"
  IL_0013:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0018:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_001d:  call       "Function String.Concat(String, String) As String"
  IL_0022:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__3-0._Lambda$__0",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__3-0.$VB$Local_prefix As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_000c:  ldftn      "Function M1.B2.$VB$ClosureStub_F_MyClass() As String"
  IL_0012:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0017:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_001c:  call       "Function String.Concat(String, String) As String"
  IL_0021:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__4-0._Lambda$__0",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__4-0.$VB$Local_prefix As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__4-0.$VB$Me As M1.B2"
  IL_000c:  ldftn      "Function M1.B2.$VB$ClosureStub_F_MyBase() As String"
  IL_0012:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0017:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_001c:  call       "Function String.Concat(String, String) As String"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub AddressOfWithMeReferenceInLambdas2x()
            Dim source =
<compilation name="AddressOfWithMeReferenceInLambdas2x">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Public Overridable Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Public Sub TestMe()
            Dim prefix = "->"
            Dim f1 As Func(Of String) = Function() prefix &amp; DirectCast(AddressOf Me.F, Func(Of String))()
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyClass()
            Dim prefix = "->"
            Dim f1 As Func(Of String) = Function() prefix &amp; DirectCast(AddressOf MyClass.F, Func(Of String))()
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyBase()
            Dim prefix = "->"
            Dim f1 As Func(Of String) = Function() prefix &amp; DirectCast(AddressOf MyBase.F, Func(Of String))()
            Console.WriteLine(f1())
        End Sub
    End Class

    Class D
        Inherits B2

        Public Overrides Function F() As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
->D::F
->B1::F
->B1::F
]]>)
            c.VerifyIL("M1.B2._Closure$__1-0._Lambda$__0",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__1-0.$VB$Local_prefix As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__1-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldvirtftn  "Function M1.B1.F() As String"
  IL_0013:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0018:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_001d:  call       "Function String.Concat(String, String) As String"
  IL_0022:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__2-0.$VB$Local_prefix As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_000c:  ldftn      "Function M1.B2.$VB$ClosureStub_F_MyBase() As String"
  IL_0012:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0017:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_001c:  call       "Function String.Concat(String, String) As String"
  IL_0021:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__3-0._Lambda$__0",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__3-0.$VB$Local_prefix As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_000c:  ldftn      "Function M1.B2.$VB$ClosureStub_F_MyBase() As String"
  IL_0012:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0017:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_001c:  call       "Function String.Concat(String, String) As String"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub AddressOfWithMeReferenceInLambdas2_NotVirtual()
            Dim source =
<compilation name="AddressOfWithMeReferenceInLambdas2">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Protected Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Protected Shadows Function F() As String
            Return "B2::F"
        End Function

        Public Sub TestMe()
            Dim prefix = "->"
            Dim f1 As Func(Of String) = Function() prefix &amp; DirectCast(AddressOf Me.F, Func(Of String))()
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyClass()
            Dim prefix = "->"
            Dim f1 As Func(Of String) = Function() prefix &amp; DirectCast(AddressOf MyClass.F, Func(Of String))()
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyBase()
            Dim prefix = "->"
            Dim f1 As Func(Of String) = Function() prefix &amp; DirectCast(AddressOf MyBase.F, Func(Of String))()
            Console.WriteLine(f1())
        End Sub
    End Class

    Class D
        Inherits B2

        Protected Shadows Function F() As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
->B2::F
->B2::F
->B1::F
]]>)
            c.VerifyIL("M1.B2._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__2-0.$VB$Local_prefix As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_000c:  ldftn      "Function M1.B2.F() As String"
  IL_0012:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0017:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_001c:  call       "Function String.Concat(String, String) As String"
  IL_0021:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__3-0._Lambda$__0",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__3-0.$VB$Local_prefix As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_000c:  ldftn      "Function M1.B2.F() As String"
  IL_0012:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0017:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_001c:  call       "Function String.Concat(String, String) As String"
  IL_0021:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__4-0._Lambda$__0",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__4-0.$VB$Local_prefix As String"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__4-0.$VB$Me As M1.B2"
  IL_000c:  ldftn      "Function M1.B1.F() As String"
  IL_0012:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_0017:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_001c:  call       "Function String.Concat(String, String) As String"
  IL_0021:  ret
}
]]>)
        End Sub

#End Region

#Region "Generics with MyBase/MyClass"

        <Fact>
        Public Sub MyBaseMyClassWithGenericMethods1()
            Dim source =
<compilation name="MyBaseMyClassWithGenericMethods1">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Protected Overridable Function F(Of T)(p As T) As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Protected Overrides Function F(Of T)(p As T) As String
            Return "B2::F"
        End Function

        Public Sub TestMe()
            Dim f1 As Func(Of Integer, String) = New Func(Of Integer, String)(AddressOf Me.F(Of Integer))
            Console.WriteLine(f1(1))
        End Sub

        Public Sub TestMyClass()
            Dim f1 As Func(Of Integer, String) = New Func(Of Integer, String)(AddressOf MyClass.F(Of Integer))
            Console.WriteLine(f1(2))
        End Sub

        Public Sub TestMyBase()
            Dim f1 As Func(Of Integer, String) = New Func(Of Integer, String)(AddressOf MyBase.F(Of Integer))
            Console.WriteLine(f1(3))
        End Sub
    End Class

    Class D
        Inherits B2

        Protected Overrides Function F(Of T)(p As T) As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
D::F
B2::F
B1::F
]]>).VerifyIL("M1.B2.TestMe",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  "Function M1.B2.F(Of Integer)(Integer) As String"
  IL_0008:  newobj     "Sub System.Func(Of Integer, String)..ctor(Object, System.IntPtr)"
  IL_000d:  ldc.i4.1
  IL_000e:  callvirt   "Function System.Func(Of Integer, String).Invoke(Integer) As String"
  IL_0013:  call       "Sub System.Console.WriteLine(String)"
  IL_0018:  ret
}
]]>).VerifyIL("M1.B2.TestMyClass",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B2.F(Of Integer)(Integer) As String"
  IL_0007:  newobj     "Sub System.Func(Of Integer, String)..ctor(Object, System.IntPtr)"
  IL_000c:  ldc.i4.2
  IL_000d:  callvirt   "Function System.Func(Of Integer, String).Invoke(Integer) As String"
  IL_0012:  call       "Sub System.Console.WriteLine(String)"
  IL_0017:  ret
}
]]>).VerifyIL("M1.B2.TestMyBase",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B1.F(Of Integer)(Integer) As String"
  IL_0007:  newobj     "Sub System.Func(Of Integer, String)..ctor(Object, System.IntPtr)"
  IL_000c:  ldc.i4.3
  IL_000d:  callvirt   "Function System.Func(Of Integer, String).Invoke(Integer) As String"
  IL_0012:  call       "Sub System.Console.WriteLine(String)"
  IL_0017:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub MyBaseMyClassWithGenericMethods2()
            Dim source =
<compilation name="MyBaseMyClassWithGenericMethods2">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Protected Overridable Function F(Of T)(p As T) As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Protected Overrides Function F(Of T)(p As T) As String
            Return "B2::F"
        End Function

        Public Sub TestMe()
            Dim f1 As Func(Of String) = Function() Me.F(Of Integer)(1)
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyClass()
            Dim f1 As Func(Of String) = Function() MyClass.F(Of Integer)(1)
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyBase()
            Dim f1 As Func(Of String) = Function() MyBase.F(Of Integer)(1)
            Console.WriteLine(f1())
        End Sub
    End Class

    Class D
        Inherits B2

        Protected Overrides Function F(Of T)(p As T) As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
D::F
B2::F
B1::F
]]>)
            c.VerifyIL("M1.B2._Lambda$__2-0",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  callvirt   "Function M1.B2.F(Of Integer)(Integer) As String"
  IL_0007:  ret
}
]]>)
            c.VerifyIL("M1.B2._Lambda$__3-0",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  call       "Function M1.B2.F(Of Integer)(Integer) As String"
  IL_0007:  ret
}
]]>)
            c.VerifyIL("M1.B2._Lambda$__4-0",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  call       "Function M1.B1.F(Of Integer)(Integer) As String"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub MyBaseMyClassWithGenericMethods3()
            Dim source =
<compilation name="MyBaseMyClassWithGenericMethods3">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Protected Overridable Function F(Of T)(p As T) As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Protected Overrides Function F(Of T)(p As T) As String
            Return "B2::F"
        End Function

        Public Sub TestMe()
            Dim p As Integer = 1
            Dim f1 As Func(Of String) = Function() Me.F(Of Integer)(p)
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyClass()
            Dim p As Integer = 1
            Dim f1 As Func(Of String) = Function() MyClass.F(Of Integer)(p)
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyBase()
            Dim p As Integer = 1
            Dim f1 As Func(Of String) = Function() MyBase.F(Of Integer)(p)
            Console.WriteLine(f1())
        End Sub
    End Class

    Class D
        Inherits B2

        Protected Overrides Function F(Of T)(p As T) As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
D::F
B2::F
B1::F
]]>)
            c.VerifyIL("M1.B2.TestMe",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.B2._Closure$__2-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldc.i4.1
  IL_000e:  stfld      "M1.B2._Closure$__2-0.$VB$Local_p As Integer"
  IL_0013:  ldftn      "Function M1.B2._Closure$__2-0._Lambda$__0() As String"
  IL_0019:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_001e:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0023:  call       "Sub System.Console.WriteLine(String)"
  IL_0028:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__2-0.$VB$Local_p As Integer"
  IL_000c:  callvirt   "Function M1.B2.F(Of Integer)(Integer) As String"
  IL_0011:  ret
}
]]>)
            c.VerifyIL("M1.B2.TestMyClass",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.B2._Closure$__3-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldc.i4.1
  IL_000e:  stfld      "M1.B2._Closure$__3-0.$VB$Local_p As Integer"
  IL_0013:  ldftn      "Function M1.B2._Closure$__3-0._Lambda$__0() As String"
  IL_0019:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_001e:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0023:  call       "Sub System.Console.WriteLine(String)"
  IL_0028:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__3-0._Lambda$__0",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__3-0.$VB$Local_p As Integer"
  IL_000c:  call       "Function M1.B2.$VB$ClosureStub_F_MyClass(Of Integer)(Integer) As String"
  IL_0011:  ret
}
]]>)
            c.VerifyIL("M1.B2.$VB$ClosureStub_F_MyClass(Of T)(T)",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       "Function M1.B2.F(Of T)(T) As String"
  IL_0007:  ret
}
]]>)
            c.VerifyIL("M1.B2.TestMyBase",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  3
  IL_0000:  newobj     "Sub M1.B2._Closure$__4-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.B2._Closure$__4-0.$VB$Me As M1.B2"
  IL_000c:  dup
  IL_000d:  ldc.i4.1
  IL_000e:  stfld      "M1.B2._Closure$__4-0.$VB$Local_p As Integer"
  IL_0013:  ldftn      "Function M1.B2._Closure$__4-0._Lambda$__0() As String"
  IL_0019:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_001e:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0023:  call       "Sub System.Console.WriteLine(String)"
  IL_0028:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__4-0._Lambda$__0",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__4-0.$VB$Me As M1.B2"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__4-0.$VB$Local_p As Integer"
  IL_000c:  call       "Function M1.B2.$VB$ClosureStub_F_MyBase(Of Integer)(Integer) As String"
  IL_0011:  ret
}
]]>)
            c.VerifyIL("M1.B2.$VB$ClosureStub_F_MyBase(Of T)(T)",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       "Function M1.B1.F(Of T)(T) As String"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub MyBaseMyClassWithGenericMethods4()
            Dim source =
<compilation name="MyBaseMyClassWithGenericMethods4">
    <file name="a.vb">
Imports System

Module M1

    Class B1
        Protected Overridable Function F(Of T)(p As T) As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Public Sub TestMe()
            Dim p As Integer = 1
            Dim f1 As Func(Of String) = Function() Me.F(Of Integer)(p)
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyClass()
            Dim p As Integer = 1
            Dim f1 As Func(Of String) = Function() MyClass.F(Of Integer)(p)
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyBase()
            Dim p As Integer = 1
            Dim f1 As Func(Of String) = Function() MyBase.F(Of Integer)(p)
            Console.WriteLine(f1())
        End Sub
    End Class

    Class D
        Inherits B2

        Protected Overrides Function F(Of T)(p As T) As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
D::F
B1::F
B1::F
]]>)
            c.VerifyIL("M1.B2._Closure$__1-0._Lambda$__0",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__1-0.$VB$Me As M1.B2"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__1-0.$VB$Local_p As Integer"
  IL_000c:  callvirt   "Function M1.B1.F(Of Integer)(Integer) As String"
  IL_0011:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__2-0.$VB$Local_p As Integer"
  IL_000c:  call       "Function M1.B2.$VB$ClosureStub_F_MyBase(Of Integer)(Integer) As String"
  IL_0011:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__3-0._Lambda$__0",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.B2._Closure$__3-0.$VB$Local_p As Integer"
  IL_000c:  call       "Function M1.B2.$VB$ClosureStub_F_MyBase(Of Integer)(Integer) As String"
  IL_0011:  ret
}
]]>)
            c.VerifyIL("M1.B2.$VB$ClosureStub_F_MyBase(Of T)(T)",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       "Function M1.B1.F(Of T)(T) As String"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub MyBaseMyClassWithGenericTypes1()
            Dim source =
<compilation name="MyBaseMyClassWithGenericTypes1">
    <file name="a.vb">
Imports System

Module M1

    Class B1(Of T)
        Protected Overridable Function F(Of U)(p1 As T, p2 As U) As String
            Return "B1::F"
        End Function
    End Class

    Class B2(Of T)
        Inherits B1(Of T)

        Protected Overrides Function F(Of U)(p1 As T, p2 As U) As String
            Return "B2::F"
        End Function

        Public Sub TestMe()
            Dim f1 As Func(Of T, Integer, String) = New Func(Of T, Integer, String)(AddressOf Me.F(Of Integer))
            Console.WriteLine(f1(Nothing, 1))
        End Sub

        Public Sub TestMyClass()
            Dim f1 As Func(Of T, Integer, String) = New Func(Of T, Integer, String)(AddressOf MyClass.F(Of Integer))
            Console.WriteLine(f1(Nothing, 2))
        End Sub

        Public Sub TestMyBase()
            Dim f1 As Func(Of T, Integer, String) = New Func(Of T, Integer, String)(AddressOf MyBase.F(Of Integer))
            Console.WriteLine(f1(Nothing, 3))
        End Sub
    End Class

    Class D(Of T)
        Inherits B2(Of T)

        Protected Overrides Function F(Of U)(p1 As T, p2 As U) As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D(Of Integer)()).TestMe()
        Call (New D(Of Integer)()).TestMyClass()
        Call (New D(Of Integer)()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
D::F
B2::F
B1::F
]]>).VerifyIL("M1.B2(Of T).TestMe",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  3
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  "Function M1.B2(Of T).F(Of Integer)(T, Integer) As String"
  IL_0008:  newobj     "Sub System.Func(Of T, Integer, String)..ctor(Object, System.IntPtr)"
  IL_000d:  ldloca.s   V_0
  IL_000f:  initobj    "T"
  IL_0015:  ldloc.0
  IL_0016:  ldc.i4.1
  IL_0017:  callvirt   "Function System.Func(Of T, Integer, String).Invoke(T, Integer) As String"
  IL_001c:  call       "Sub System.Console.WriteLine(String)"
  IL_0021:  ret
}
]]>).VerifyIL("M1.B2(Of T).TestMyClass",
            <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B2(Of T).F(Of Integer)(T, Integer) As String"
  IL_0007:  newobj     "Sub System.Func(Of T, Integer, String)..ctor(Object, System.IntPtr)"
  IL_000c:  ldloca.s   V_0
  IL_000e:  initobj    "T"
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.2
  IL_0016:  callvirt   "Function System.Func(Of T, Integer, String).Invoke(T, Integer) As String"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  ret
}
]]>).VerifyIL("M1.B2(Of T).TestMyBase",
            <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B1(Of T).F(Of Integer)(T, Integer) As String"
  IL_0007:  newobj     "Sub System.Func(Of T, Integer, String)..ctor(Object, System.IntPtr)"
  IL_000c:  ldloca.s   V_0
  IL_000e:  initobj    "T"
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.3
  IL_0016:  callvirt   "Function System.Func(Of T, Integer, String).Invoke(T, Integer) As String"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub MyBaseMyClassWithGenericTypes2()
            Dim source =
<compilation name="MyBaseMyClassWithGenericTypes2">
    <file name="a.vb">
Imports System

Module M1

    Class B1(Of T)
        Protected Overridable Function F(Of U)(p1 As T, p2 As U) As String
            Return "B1::F"
        End Function
    End Class

    Class OuterClass(Of V)

        Class B2
            Inherits B1(Of V)

            Protected Overrides Function F(Of U)(p1 As V, p2 As U) As String
                Return "B2::F"
            End Function

            Public Sub TestMe()
                Dim f1 As Func(Of V, V, String) = New Func(Of V, V, String)(AddressOf Me.F(Of V))
                Console.WriteLine(f1(Nothing, Nothing))
            End Sub

            Public Sub TestMyClass()
                Dim f1 As Func(Of V, Integer, String) = New Func(Of V, Integer, String)(AddressOf MyClass.F(Of Integer))
                Console.WriteLine(f1(Nothing, Nothing))
            End Sub

            Public Sub TestMyBase()
                Dim f1 As Func(Of V, String, String) = New Func(Of V, String, String)(AddressOf MyBase.F(Of String))
                Console.WriteLine(f1(Nothing, Nothing))
            End Sub
        End Class
    End Class

    Class D(Of T)
        Inherits OuterClass(Of T).B2

        Protected Overrides Function F(Of U)(p1 As T, p2 As U) As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D(Of Integer)()).TestMe()
        Call (New D(Of Integer)()).TestMyClass()
        Call (New D(Of Integer)()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
D::F
B2::F
B1::F
]]>).VerifyIL("M1.OuterClass(Of V).B2.TestMe",
            <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  3
  .locals init (V V_0)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  "Function M1.OuterClass(Of V).B2.F(Of V)(V, V) As String"
  IL_0008:  newobj     "Sub System.Func(Of V, V, String)..ctor(Object, System.IntPtr)"
  IL_000d:  ldloca.s   V_0
  IL_000f:  initobj    "V"
  IL_0015:  ldloc.0
  IL_0016:  ldloca.s   V_0
  IL_0018:  initobj    "V"
  IL_001e:  ldloc.0
  IL_001f:  callvirt   "Function System.Func(Of V, V, String).Invoke(V, V) As String"
  IL_0024:  call       "Sub System.Console.WriteLine(String)"
  IL_0029:  ret
}
]]>).VerifyIL("M1.OuterClass(Of V).B2.TestMyClass",
            <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (V V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.OuterClass(Of V).B2.F(Of Integer)(V, Integer) As String"
  IL_0007:  newobj     "Sub System.Func(Of V, Integer, String)..ctor(Object, System.IntPtr)"
  IL_000c:  ldloca.s   V_0
  IL_000e:  initobj    "V"
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.0
  IL_0016:  callvirt   "Function System.Func(Of V, Integer, String).Invoke(V, Integer) As String"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  ret
}
]]>).VerifyIL("M1.OuterClass(Of V).B2.TestMyBase",
            <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (V V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Function M1.B1(Of V).F(Of String)(V, String) As String"
  IL_0007:  newobj     "Sub System.Func(Of V, String, String)..ctor(Object, System.IntPtr)"
  IL_000c:  ldloca.s   V_0
  IL_000e:  initobj    "V"
  IL_0014:  ldloc.0
  IL_0015:  ldnull
  IL_0016:  callvirt   "Function System.Func(Of V, String, String).Invoke(V, String) As String"
  IL_001b:  call       "Sub System.Console.WriteLine(String)"
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub MyBaseMyClassWithGenericTypes3()
            Dim source =
<compilation name="MyBaseMyClassWithGenericTypes3">
    <file name="a.vb">
Imports System

Module M1

    Class B1(Of T)
        Protected Overridable Function F(Of U)(p1 As T, p2 As U) As String
            Return "B1::F"
        End Function
    End Class

    Class OuterClass(Of V)

        Class B2
            Inherits B1(Of V)

            Protected Overrides Function F(Of U)(p1 As V, p2 As U) As String
                Return "B2::F"
            End Function

            Public Sub TestMe()
                Dim p As V = Nothing
                Dim f1 As Func(Of String) = Function() (New Func(Of V, V, String)(AddressOf Me.F(Of V)))(p, p)
                Console.WriteLine(f1())
            End Sub

            Public Sub TestMyClass()
                Dim p As V = Nothing
                Dim f1 As Func(Of String) = Function() (New Func(Of V, V, String)(AddressOf MyClass.F(Of V)))(p, p)
                Console.WriteLine(f1())
            End Sub

            Public Sub TestMyBase()
                Dim p As V = Nothing
                Dim f1 As Func(Of String) = Function() (New Func(Of V, V, String)(AddressOf MyBase.F(Of V)))(p, p)
                Console.WriteLine(f1())
            End Sub
        End Class
    End Class

    Class D(Of T)
        Inherits OuterClass(Of T).B2

        Protected Overrides Function F(Of U)(p1 As T, p2 As U) As String
            Return "D::F"
        End Function
    End Class

    Public Sub Main()
        Call (New D(Of Integer)()).TestMe()
        Call (New D(Of Integer)()).TestMyClass()
        Call (New D(Of Integer)()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
D::F
B2::F
B1::F
]]>)
            c.VerifyIL("M1.OuterClass(Of V).B2._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.OuterClass(Of V).B2._Closure$__2-0.$VB$Me As M1.OuterClass(Of V).B2"
  IL_0006:  dup
  IL_0007:  ldvirtftn  "Function M1.OuterClass(Of V).B2.F(Of V)(V, V) As String"
  IL_000d:  newobj     "Sub System.Func(Of V, V, String)..ctor(Object, System.IntPtr)"
  IL_0012:  ldarg.0
  IL_0013:  ldfld      "M1.OuterClass(Of V).B2._Closure$__2-0.$VB$Local_p As V"
  IL_0018:  ldarg.0
  IL_0019:  ldfld      "M1.OuterClass(Of V).B2._Closure$__2-0.$VB$Local_p As V"
  IL_001e:  callvirt   "Function System.Func(Of V, V, String).Invoke(V, V) As String"
  IL_0023:  ret
}
]]>)
            c.VerifyIL("M1.OuterClass(Of V).B2._Closure$__3-0._Lambda$__0",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.OuterClass(Of V).B2._Closure$__3-0.$VB$Me As M1.OuterClass(Of V).B2"
  IL_0006:  ldftn      "Function M1.OuterClass(Of V).B2.$VB$ClosureStub_F_MyClass(Of V)(V, V) As String"
  IL_000c:  newobj     "Sub System.Func(Of V, V, String)..ctor(Object, System.IntPtr)"
  IL_0011:  ldarg.0
  IL_0012:  ldfld      "M1.OuterClass(Of V).B2._Closure$__3-0.$VB$Local_p As V"
  IL_0017:  ldarg.0
  IL_0018:  ldfld      "M1.OuterClass(Of V).B2._Closure$__3-0.$VB$Local_p As V"
  IL_001d:  callvirt   "Function System.Func(Of V, V, String).Invoke(V, V) As String"
  IL_0022:  ret
}
]]>)
            c.VerifyIL("M1.OuterClass(Of V).B2.$VB$ClosureStub_F_MyClass(Of U)",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  ldarg.2   
  IL_0003:  call       "Function M1.OuterClass(Of V).B2.F(Of U)(V, U) As String"
  IL_0008:  ret       
}
]]>)
            c.VerifyIL("M1.OuterClass(Of V).B2._Closure$__4-0._Lambda$__0",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.OuterClass(Of V).B2._Closure$__4-0.$VB$Me As M1.OuterClass(Of V).B2"
  IL_0006:  ldftn      "Function M1.OuterClass(Of V).B2.$VB$ClosureStub_F_MyBase(Of V)(V, V) As String"
  IL_000c:  newobj     "Sub System.Func(Of V, V, String)..ctor(Object, System.IntPtr)"
  IL_0011:  ldarg.0
  IL_0012:  ldfld      "M1.OuterClass(Of V).B2._Closure$__4-0.$VB$Local_p As V"
  IL_0017:  ldarg.0
  IL_0018:  ldfld      "M1.OuterClass(Of V).B2._Closure$__4-0.$VB$Local_p As V"
  IL_001d:  callvirt   "Function System.Func(Of V, V, String).Invoke(V, V) As String"
  IL_0022:  ret
}
]]>)
            c.VerifyIL("M1.OuterClass(Of V).B2.$VB$ClosureStub_F_MyBase(Of U)",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  ldarg.2   
  IL_0003:  call       "Function M1.B1(Of V).F(Of U)(V, U) As String"
  IL_0008:  ret       
}
]]>)
        End Sub

        <Fact>
        Public Sub MyBaseMyClassWithGenericTypes4()
            Dim source =
<compilation name="MyBaseMyClassWithGenericTypes4">
    <file name="a.vb">
Imports System

Module M1

    Class OuterClass(Of T)
        Class B1
            Protected Overridable Function F1(p1 As T) As String
                Return "B1::F1"
            End Function
            Protected Overridable Function F2(p1 As T) As String
                Return "B1::F2"
            End Function
        End Class
    End Class

    Class B2
        Inherits OuterClass(Of Integer).B1

        Protected Overrides Function F1(p1 As Integer) As String
            Return "B2::F1"
        End Function

        Public Sub TestMe()
            Dim p As Integer = 1
            Dim f1 As Func(Of String) = Function() (New Func(Of Integer, String)(AddressOf Me.F1))(p) &amp; " - " &amp; (New Func(Of Integer, String)(AddressOf Me.F2))(p)
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyClass()
            Dim p As Integer = 1
            Dim f1 As Func(Of String) = Function() (New Func(Of Integer, String)(AddressOf MyClass.F1))(p) &amp; " - " &amp; (New Func(Of Integer, String)(AddressOf MyClass.F2))(p)
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyBase()
            Dim p As Integer = 1
            Dim f1 As Func(Of String) = Function() (New Func(Of Integer, String)(AddressOf MyBase.F1))(p) &amp; " - " &amp; (New Func(Of Integer, String)(AddressOf MyBase.F2))(p)
            Console.WriteLine(f1())
        End Sub
    End Class

    Class D
        Inherits B2

        Protected Overrides Function F1(p1 As Integer) As String
            Return "D::F1"
        End Function
        Protected Overrides Function F2(p1 As Integer) As String
            Return "D::F2"
        End Function
    End Class

    Public Sub Main()
        Call (New D()).TestMe()
        Call (New D()).TestMyClass()
        Call (New D()).TestMyBase()
    End Sub

End Module
    </file>
</compilation>
            Dim c = CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
D::F1 - D::F2
B2::F1 - B1::F2
B1::F1 - B1::F2
]]>)
            c.VerifyIL("M1.B2._Closure$__2-0._Lambda$__0",
            <![CDATA[
{
  // Code size       69 (0x45)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_0006:  dup
  IL_0007:  ldvirtftn  "Function M1.B2.F1(Integer) As String"
  IL_000d:  newobj     "Sub System.Func(Of Integer, String)..ctor(Object, System.IntPtr)"
  IL_0012:  ldarg.0
  IL_0013:  ldfld      "M1.B2._Closure$__2-0.$VB$Local_p As Integer"
  IL_0018:  callvirt   "Function System.Func(Of Integer, String).Invoke(Integer) As String"
  IL_001d:  ldstr      " - "
  IL_0022:  ldarg.0
  IL_0023:  ldfld      "M1.B2._Closure$__2-0.$VB$Me As M1.B2"
  IL_0028:  dup
  IL_0029:  ldvirtftn  "Function M1.OuterClass(Of Integer).B1.F2(Integer) As String"
  IL_002f:  newobj     "Sub System.Func(Of Integer, String)..ctor(Object, System.IntPtr)"
  IL_0034:  ldarg.0
  IL_0035:  ldfld      "M1.B2._Closure$__2-0.$VB$Local_p As Integer"
  IL_003a:  callvirt   "Function System.Func(Of Integer, String).Invoke(Integer) As String"
  IL_003f:  call       "Function String.Concat(String, String, String) As String"
  IL_0044:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__3-0._Lambda$__0",
            <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_0006:  ldftn      "Function M1.B2.$VB$ClosureStub_F1_MyClass(Integer) As String"
  IL_000c:  newobj     "Sub System.Func(Of Integer, String)..ctor(Object, System.IntPtr)"
  IL_0011:  ldarg.0
  IL_0012:  ldfld      "M1.B2._Closure$__3-0.$VB$Local_p As Integer"
  IL_0017:  callvirt   "Function System.Func(Of Integer, String).Invoke(Integer) As String"
  IL_001c:  ldstr      " - "
  IL_0021:  ldarg.0
  IL_0022:  ldfld      "M1.B2._Closure$__3-0.$VB$Me As M1.B2"
  IL_0027:  ldftn      "Function M1.B2.$VB$ClosureStub_F2_MyBase(Integer) As String"
  IL_002d:  newobj     "Sub System.Func(Of Integer, String)..ctor(Object, System.IntPtr)"
  IL_0032:  ldarg.0
  IL_0033:  ldfld      "M1.B2._Closure$__3-0.$VB$Local_p As Integer"
  IL_0038:  callvirt   "Function System.Func(Of Integer, String).Invoke(Integer) As String"
  IL_003d:  call       "Function String.Concat(String, String, String) As String"
  IL_0042:  ret
}
]]>)
            c.VerifyIL("M1.B2.$VB$ClosureStub_F1_MyClass",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       "Function M1.B2.F1(Integer) As String"
  IL_0007:  ret
}
]]>)
            c.VerifyIL("M1.B2._Closure$__4-0._Lambda$__0",
            <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.B2._Closure$__4-0.$VB$Me As M1.B2"
  IL_0006:  ldftn      "Function M1.B2.$VB$ClosureStub_F1_MyBase(Integer) As String"
  IL_000c:  newobj     "Sub System.Func(Of Integer, String)..ctor(Object, System.IntPtr)"
  IL_0011:  ldarg.0
  IL_0012:  ldfld      "M1.B2._Closure$__4-0.$VB$Local_p As Integer"
  IL_0017:  callvirt   "Function System.Func(Of Integer, String).Invoke(Integer) As String"
  IL_001c:  ldstr      " - "
  IL_0021:  ldarg.0
  IL_0022:  ldfld      "M1.B2._Closure$__4-0.$VB$Me As M1.B2"
  IL_0027:  ldftn      "Function M1.B2.$VB$ClosureStub_F2_MyBase(Integer) As String"
  IL_002d:  newobj     "Sub System.Func(Of Integer, String)..ctor(Object, System.IntPtr)"
  IL_0032:  ldarg.0
  IL_0033:  ldfld      "M1.B2._Closure$__4-0.$VB$Local_p As Integer"
  IL_0038:  callvirt   "Function System.Func(Of Integer, String).Invoke(Integer) As String"
  IL_003d:  call       "Function String.Concat(String, String, String) As String"
  IL_0042:  ret
}
]]>)
            c.VerifyIL("M1.B2.$VB$ClosureStub_F1_MyBase",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       "Function M1.OuterClass(Of Integer).B1.F1(Integer) As String"
  IL_0007:  ret
}
]]>)
            c.VerifyIL("M1.B2.$VB$ClosureStub_F2_MyBase",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       "Function M1.OuterClass(Of Integer).B1.F2(Integer) As String"
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub MyBaseMyClassWithGenericTypesAndConstraints()
            Dim ilSource = <![CDATA[
.class public auto ansi B1`1<T>
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method B1`1::.ctor

  .method family newslot strict virtual instance string 
          F<valuetype U,.ctor X,class Y,(!!X) Z>(!T p1,
                                                 !!U p2,
                                                 !!X p3,
                                                 !!Y p4,
                                                 !!Z p5) cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init ([0] string F)
    IL_0000:  ldstr      "B1::F"
    IL_0005:  ret
  } // end of method B1`1::F

} // end of class MyBaseTest.B1`1
]]>

            Dim source =
<compilation name="MyBaseMyClassWithGenericTypesAndConstraints">
    <file name="a.vb">
Imports System

Class OuterClass(Of V)

    Class B2
        Inherits B1(Of V)

        Protected Overrides Function F(Of U As Structure, X As New, Y As Class, Z As X)(p1 As V, p2 As U, p3 As X, p4 As Y, p5 As Z) As String
            Return "B2::F"
        End Function

        Public Sub TestMyClass()
            Dim p As Integer = 123
            Console.Write(DirectCast(Function() DirectCast(AddressOf MyClass.F(Of Integer, Object, String, String), 
                            Func(Of V, Integer, Object, String, String, String))(Nothing, p, Nothing, Nothing, Nothing), Func(Of String))())
        End Sub

        Public Sub TestMyBase()
            Dim p As Integer = 123
            Console.Write(DirectCast(Function() DirectCast(AddressOf MyBase.F(Of Integer, Object, String, String), 
                            Func(Of V, Integer, Object, String, String, String))(Nothing, p, Nothing, Nothing, Nothing), Func(Of String))())
        End Sub
    End Class
End Class

Module M1
    Public Sub Main(args As String())
        Call (New OuterClass(Of Integer).B2()).TestMyClass()
        Console.Write("-")
        Call (New OuterClass(Of Integer).B2()).TestMyBase()
    End Sub
End Module
    </file>
</compilation>
            CompileWithCustomILSource(source, ilSource.Value, TestOptions.ReleaseExe, expectedOutput:="B2::F-B1::F").
            VerifyIL("OuterClass(Of V).B2.$VB$ClosureStub_F_MyBase",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  6
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  ldarg.3
  IL_0004:  ldarg.s    V_4
  IL_0006:  ldarg.s    V_5
  IL_0008:  call       "Function B1(Of V).F(Of U, X, Y, Z)(V, U, X, Y, Z) As String"
  IL_000d:  ret
}
]]>).
VerifyIL("OuterClass(Of V).B2.$VB$ClosureStub_F_MyClass",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  6
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  ldarg.3
  IL_0004:  ldarg.s    V_4
  IL_0006:  ldarg.s    V_5
  IL_0008:  call       "Function OuterClass(Of V).B2.F(Of U, X, Y, Z)(V, U, X, Y, Z) As String"
  IL_000d:  ret
}
]]>)
        End Sub

#End Region

#Region "MyClass/MyBase method wrappers symbol tests"

        <Fact>
        Public Sub MyClassMyBaseMethodWrappersSymbolTests()
            Dim compilationDef =
    <compilation name="MyClassMyBaseMethodWrappersSymbolTests">
        <file name="a.vb">
Imports System

Module M1

    Class B1
        Protected Overridable Function F(Of T)(p As T) As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Protected Overrides Function F(Of T)(p As T) As String
            Return "B2::F"
        End Function

        Public Sub Test()
            Dim p As Integer = 1
            'POSITION
            Dim f1 = DirectCast(Function() (MyClass.F(Of Integer)(p) &amp; MyBase.F(Of Integer)(p)), Func(Of String))()
        End Sub
    End Class

    Public Sub Main()
    End Sub

End Module
        </file>
    </compilation>

            Dim position = compilationDef.<file>.Value.IndexOf("'POSITION", StringComparison.Ordinal)

            Dim verifier = CompileAndVerify(compilationDef,
                             options:=TestOptions.DebugDll,
                             references:={SystemCoreRef})

            Dim _assembly = Assembly.Load(verifier.EmittedAssemblyData.ToArray())
            Assert.NotNull(_assembly)

            Dim _M1 = _assembly.GetType("M1")
            Assert.NotNull(_M1)

            Dim _B2 = _M1.GetNestedType("B2")
            Assert.NotNull(_B2)

            Dim _wrapperMyBase = _B2.GetMethod("$VB$ClosureStub_F_MyBase", BindingFlags.Instance Or BindingFlags.NonPublic)
            Assert.NotNull(_wrapperMyBase)
            Assert.True(_wrapperMyBase.IsDefined(GetType(DebuggerHiddenAttribute), False))
            Assert.True(_wrapperMyBase.IsDefined(GetType(CompilerGeneratedAttribute), False))

            Dim _wrapperMyClass = _B2.GetMethod("$VB$ClosureStub_F_MyClass", BindingFlags.Instance Or BindingFlags.NonPublic)
            Assert.NotNull(_wrapperMyClass)
            Assert.True(_wrapperMyClass.IsDefined(GetType(DebuggerHiddenAttribute), False))
            Assert.True(_wrapperMyBase.IsDefined(GetType(CompilerGeneratedAttribute), False))
        End Sub

        Private Shared Function GetNamedTypeSymbol(m As ModuleSymbol, namedTypeName As String) As NamedTypeSymbol
            Dim nameParts = namedTypeName.Split("."c)

            Dim peAssembly = DirectCast(m.ContainingAssembly, PEAssemblySymbol)
            Dim nsSymbol As NamespaceSymbol = Nothing
            For Each ns In nameParts.Take(nameParts.Length - 1)
                nsSymbol = DirectCast(If(nsSymbol Is Nothing,
                                         m.ContainingAssembly.CorLibrary.GlobalNamespace.GetMember(ns),
                                         nsSymbol.GetMember(ns)), NamespaceSymbol)
            Next
            Return DirectCast(nsSymbol.GetTypeMember(nameParts(nameParts.Length - 1)), NamedTypeSymbol)
        End Function

#End Region

    End Class

End Namespace

