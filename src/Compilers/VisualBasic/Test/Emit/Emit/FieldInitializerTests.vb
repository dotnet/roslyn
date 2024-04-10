' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

    Public Class FieldInitializerTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TestFieldInitializer()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system

class c2
  public field as integer

  public sub new(p as integer)
    field = p
  end sub
end class

Class C1
    Public field1 as integer = 42
    Public Shared field2 as integer = 23
    Public field3 as new C2(12)
    Public field4, field5 as new C2(42)
    Public field6 as integer = C1.Goo()
    Public field7 as integer = Goo
    Public field8 as C2 = New C2(20120421)

    Public Shared Function Goo() as Integer
        return 2
    End Function

    Public sub DumpFields()
        Console.WriteLine(field1)
        Console.WriteLine(field2)
        Console.Writeline(field3.field)
        Console.Writeline(field4.field)
        Console.Writeline(field5.field)
        Console.WriteLine(field6)
        Console.WriteLine(field7)

        if field4 isnot field5 then
            console.writeline("Using AsNew Initialization with multiple names creates different instances.")
        end if

        Console.WriteLine(field8.field)
    End Sub

    Public shared Sub Main(args() as string)
        Dim c as C1 =  new C1()
        c.DumpFields()
    End sub
End Class
    </file>
</compilation>

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
42
23
12
42
42
2
2
Using AsNew Initialization with multiple names creates different instances.
20120421
]]>)
        End Sub

        <Fact>
        Public Sub TestFieldsOfDelegates()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system

Class C1
    Public Delegate Sub SubDel(p as integer)
    
    Public Shared Sub goo(p as Integer)
        Console.WriteLine("DelegateField works :) " + p.ToString())
    End Sub

    Public delfield1 as SubDel = AddressOf C1.goo

    Public sub DumpFields()
        delfield1(23)
    End Sub

    Public shared Sub Main(args() as string)
        Dim c as new C1
        c.DumpFields()
    End sub
End Class
    </file>
</compilation>

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
DelegateField works :) 23
]]>)
        End Sub

        <Fact>
        Public Sub TestFieldsConst()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system

Class C1
    private const f1 as integer = 22
    private f2 as integer = Me.f1 + 1

    Public sub DumpFields()
        Console.WriteLine(f2)
    End Sub

    Public shared Sub Main(args() as string)
        Dim c as new C1
        c.DumpFields()
    End sub
End Class
    </file>
</compilation>

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
23
]]>)
        End Sub

        <Fact>
        Public Sub TestFieldsUsingMe()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system

class c2
    public sub New(p as integer)
        console.writeLine("new c2 with " &amp; p)
    end sub
end class

Class C1
    private f1 as integer = 21
    private f2 as integer = Me.f1 + 1
    private f3, f4 as new C2(Me.f1)
    private f5, f6 as new C2(goo)
    private f7, f8 as new C2(prop)

    Public ReadOnly Property prop As Integer
        Get
            f1 = f1 + 1
            return f1
        End Get
    End Property


    public function goo() as Integer
        return 12
    end function

    public Sub New()
    me.f2 = me.f2 + 1
    end sub

    public Sub New(starter as integer)
    me.f2 = me.f2 + starter
    end sub

    Public sub DumpFields()
        Console.WriteLine(f2)
    End Sub

    Public shared Sub Main(args() as string)
        Dim c as new C1
        c.DumpFields()

        Dim c2 as new C1(20)
        c2.DumpFields()

    End sub
End Class
    </file>
</compilation>

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
new c2 with 21
new c2 with 21
new c2 with 12
new c2 with 12
new c2 with 22
new c2 with 23
23
new c2 with 21
new c2 with 21
new c2 with 12
new c2 with 12
new c2 with 22
new c2 with 23
42
]]>)
        End Sub

        <Fact>
        Public Sub TestConstFields_Misc()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
Option Infer On
imports system

Class C1
    private const c1s = "goo"
    Private const c2 as object = 1
    Private const c3 = c2 
    private shared c4 as integer = 1 + cint(c2) + cint(c3)
    private const c5 as object = nothing

    Public Delegate Sub SubDel(p as integer)
    
    Public Shared Sub goo(p as Integer)
        Console.writeline(c1s)
        Console.WriteLine("DelegateField works :) " + c1s + p.ToString() + " " + c4.ToString())
    End Sub

    Public delfield1 as SubDel = AddressOf C1.goo

    Public sub DumpFields()
        delfield1(23)
    End Sub

    Public shared Sub Main(args() as string)
        Dim c as new C1
        c.DumpFields()
    End sub
End Class
    </file>
</compilation>

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
goo
DelegateField works :) goo23 3
]]>)
        End Sub

        <Fact>
        Public Sub TestFieldInitializersFromDifferentSyntaxTreesWithInitialBindErrors()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system

partial Class C1
    public x55555 as string = System

    Public sub DumpFields()
        Console.WriteLine(x1)
    End Sub

    Public shared Sub Main(args() as string)
        Dim c as new C1
        c.DumpFields()
    End sub
End Class
    </file>
    <file name="b.vb">
Option strict on
imports system

partial Class C1

    public x1 as string = System

End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40(source)
            AssertTheseDiagnostics(c1,
<expected>
BC30112: 'System' is a namespace and cannot be used as an expression.
    public x55555 as string = System
                              ~~~~~~
BC30112: 'System' is a namespace and cannot be used as an expression.
    public x1 as string = System
                          ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestFieldInitializersFromDifferentSyntaxTrees()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system

partial Class C1


    Public sub DumpFields()
        Console.WriteLine(x1)
    End Sub

    Public shared Sub Main(args() as string)
        Dim c as new C1
        c.DumpFields()
    End sub
End Class
    </file>
    <file name="b.vb">
Option strict on

partial Class C1

    public x1 as string = 33 &amp; 2.34 'No inference here

End Class
    </file>
</compilation>

            ' not referencing vb runtime to get an error in the synthesized assignments
            ' coming from the field initializers
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
                source,
                options:=TestOptions.ReleaseExe.WithOverflowChecks(True))

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Conversions.ToString' is not defined.
    public x1 as string = 33 &amp; 2.34 'No inference here
                          ~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Conversions.ToString' is not defined.
    public x1 as string = 33 &amp; 2.34 'No inference here
                               ~~~~    
</expected>)
        End Sub

        ' Me.New
        <Fact>
        Public Sub TestFieldInitializersNotInChainedConstructor()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system

public Class C1

    Public a As Integer = "123".Length

    Public sub New(p as Integer)
    End Sub

    Public sub New(p as String)
        Me.New(23)
    End Sub

    Public sub New(p as Date)
        MyClass.New(23)
    End Sub
End Class
    </file>
</compilation>

            ' todo update IL to check if there are no synthesized assignments in New(p as String)
            CompileAndVerify(source).
                VerifyIL("C1..ctor(Integer)",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldstr      "123"
  IL_000c:  call       "Function String.get_Length() As Integer"
  IL_0011:  stfld      "C1.a As Integer"
  IL_0016:  ret
}
]]>).
                VerifyIL("C1..ctor(String)",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   23
  IL_0003:  call       "Sub C1..ctor(Integer)"
  IL_0008:  ret
}
]]>).
                VerifyIL("C1..ctor(Date)",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   23
  IL_0003:  call       "Sub C1..ctor(Integer)"
  IL_0008:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TestFieldsConst2()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
Option Infer On

imports system
imports system.reflection

Class C1
    private const f1 = 23
    private const f2 = #11/04/2008#
    private const f3 = -42.00000000000000000000000@
    private const f4 = (#11/04/2008#)
    private const f5 = ((-42.00000000000000000000000@))


    Public shared sub DumpFields()
        Console.WriteLine(f1)
        Console.WriteLine(f2.ToString("M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture))
        Console.WriteLine(f3.ToString(System.Globalization.CultureInfo.InvariantCulture))
    End Sub

    Public Shared Sub DumpFieldsWithReflection()
        DumpFieldWithReflection("f1")
        DumpFieldWithReflection("f2")
        DumpFieldWithReflection("f3")
    End Sub
Shared cul As System.Globalization.CultureInfo = System.Globalization.CultureInfo.InvariantCulture
    Public Shared Sub DumpFieldWithReflection(name As String)
        Dim instance As C1 = New C1()
        Dim ty As Type = instance.GetType()
        Dim field As FieldInfo = ty.GetField(name, BindingFlags.Static Or BindingFlags.NonPublic)
        Dim val = field.GetValue(instance)
        Dim vType = val.GetType()
        If vType Is GetType(DateTime) Then
            Console.WriteLine(DirectCast(val, DateTime).ToString("M/d/yyyy h:mm:ss tt", cul))
        ElseIf vType Is GetType(Single) Then
            Console.WriteLine(DirectCast(val, Single).ToString(cul))
        ElseIf vType Is GetType(Double) Then
            Console.WriteLine(DirectCast(val, Double).ToString(cul))
        ElseIf vType Is GetType(Decimal) Then
            Console.WriteLine(DirectCast(val, Decimal).ToString(cul))
        Else
            Console.WriteLine(val)
        End If
    End Sub

    Public shared Sub Main(args() as string)
        C1.DumpFields()
        C1.DumpFieldsWithReflection()
    End sub
End Class
    </file>
</compilation>

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
23
11/4/2008 12:00:00 AM
-42.00000000000000000000000
23
11/4/2008 12:00:00 AM
-42.00000000000000000000000
]]>)
        End Sub

        <Fact>
        Public Sub OptionInferConstTypeInference_1()

            Dim alloptions = {({"On", "On"}), ({"Off", "On"}), ({"Off", "Off"})}
            For Each options In alloptions
                Dim strict = options(0)
                Dim infer = options(1)

                Dim source =
        <compilation>
            <file name="a.vb">
        Option strict <%= strict %>
option infer <%= infer %>

imports system

        Class C1
            Public Const f1 = "goo"
            Public Const f2 As Object = "goo"
            Public Const f3 = 23
            Public Const f4 As Object = 42
            public const f5 as integer = nothing


            Public Shared Sub Main(args() As String)
                console.writeline(f1)
                console.writeline(f2)
                console.writeline(f3)
                console.writeline(f4)
                console.writeline(f5)
            End Sub
        End Class
                </file>
        </compilation>

                CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
goo
goo
23
42
0
        ]]>)

            Next
        End Sub

        <Fact>
        Public Sub ConstFieldNotAsNew()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
Imports system

Public Class C1
    Public Const goo as New C1()
    
    ' make sure you only get error message about wrong type, NOT about wrong parameters
    Public Const goo2 as New C1(23,23)

    Public shared Sub Main(args() as string)
    End sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            AssertTheseDiagnostics(c1,
<expected>
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    Public Const goo as New C1()
                            ~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    Public Const goo2 as New C1(23,23)
                             ~~
</expected>)
        End Sub

#If NET472 Then
        <Fact>
        Public Sub ChrChrWAscAscWAreConst()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system
imports microsoft.visualbasic.strings
Imports System.Text

Class C1
    ' Asc for Chars in 0 to 127 are const
    private const f01 as Integer = Asc("A"c)
    private const f02 as Integer = Asc("A")
    private const f03 as Integer = Asc("ABC")
    private const f04 as Integer = Asc(Chr(0))
    private const f05 as Integer = Asc(Chr(127))

    ' AscW for all chars are constant
    private const f06 as Integer = AscW("A"c)
    private const f07 as Integer = AscW("A")
    private const f08 as Integer = AscW("ABC")
    private const f09 as Integer = AscW(ChrW(0))
    private const f10 as Integer = AscW(ChrW(127))
    private const f11 as Integer = AscW(ChrW(255))
    private const f12 as Integer = AscW(ChrW(-32768)) ' same as 32768 (!)
    private const f13 as Integer = AscW(ChrW(32767))
    private const f14 as Integer = AscW(ChrW(65535))
    private const f15 as Integer = AscW("\uffff")
    private const f16 as Integer = Ascw(nothing) ' defaults to Char=0

    private const f17 as char = Chr(0)
    private const f18 as char = Chr(127)

    private const f19 as Char = ChrW(0)
    private const f20 as Char = ChrW(127)
    private const f21 as Char = ChrW(255)
    private const f22 as Char = ChrW(-32768) ' same as 32768 (!)
    private const f23 as Char = ChrW(-1) ' same as 1 (!)
    private const f24 as Char = ChrW(32767)    
    private const f25 as Char = ChrW(32768)
    private const f26 as Char = ChrW(65535)

    ' some value ranges can not be made const ...
    private shared f27 as Integer = Asc(ChrW(255))
    private shared f28 as Char = Chr(128)
    private shared f29 as Char = Chr(255)

    Public shared Sub Main(args() as string)
        console.writeline("Asc:")
        console.writeline(f01)
        console.writeline(f02)
        console.writeline(f03)
        console.writeline(f04)
        console.writeline(f05)

        console.writeline("AscW:")
        console.writeline(f06)
        console.writeline(f07)
        console.writeline(f08)
        console.writeline(f09)
        console.writeline(f10)
        console.writeline(f10)
        console.writeline(f11)
        console.writeline(f12)
        console.writeline(f13)
        console.writeline(f14)
        console.writeline(f15)
        console.writeline(f16)

        console.writeline("Chr:")
        console.writeline(Asc(f17))
        console.writeline(Asc(f18))

        console.writeline("ChrW:")
        console.writeline(AscW(f19))
        console.writeline(AscW(f20)) 
        console.writeline(AscW(f21))
        console.writeline(AscW(f22))
        console.writeline(AscW(f23))
        console.writeline(AscW(f24))
        console.writeline(AscW(f25))
        console.writeline(AscW(f26)) 

        console.writeline("Asc:")
        console.writeline(f27)

        console.writeline("Chr:")
        console.writeline(Asc(f28))
        console.writeline(Asc(f29))

    End sub
End Class
    </file>
</compilation>

            Dim expectedOutput = <![CDATA[
Asc:
65
65
65
0
127
AscW:
65
65
65
0
127
127
255
32768
32767
65535
92
0
Chr:
0
127
ChrW:
0
127
255
32768
65535
32767
32768
65535
Asc:
{0}
Chr:
{1}
{2}
]]>
            Dim localeDependentValues = Encoding.Default.GetBytes({ChrW(255), Chr(128), Chr(255)})
            expectedOutput.Value = String.Format(expectedOutput.Value, localeDependentValues(0), localeDependentValues(1), localeDependentValues(2))
            CompileAndVerify(source, expectedOutput).VerifyIL("C1..cctor", <![CDATA[
{
    // Code size       46 (0x2e)
    .maxstack  1
    IL_0000:  ldc.i4     0xff
    IL_0005:  call       "Function Microsoft.VisualBasic.Strings.Asc(Char) As Integer"
    IL_000a:  stsfld     "C1.f27 As Integer"
    IL_000f:  ldc.i4     0x80
    IL_0014:  call       "Function Microsoft.VisualBasic.Strings.Chr(Integer) As Char"
    IL_0019:  stsfld     "C1.f28 As Char"
    IL_001e:  ldc.i4     0xff
    IL_0023:  call       "Function Microsoft.VisualBasic.Strings.Chr(Integer) As Char"
    IL_0028:  stsfld     "C1.f29 As Char"
    IL_002d:  ret
}    ]]>)
        End Sub
#End If

        <Fact>
        Public Sub TestFieldsConstInStructures()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On

imports system
imports system.reflection

Structure S1
    private const f1 = 23
    private shared f2 as Integer = 42

    Public shared sub DumpFields()
        Console.WriteLine(f1)
        Console.WriteLine(f2)
    End Sub

    Public shared Sub Main(args() as string)
        s1.DumpFields()
        s2.DumpFields()
    End sub
End Structure

Structure S2
    private const f1 = #06/07/2010#
    private const f2 as Decimal = ((-42.00000000000000000000000@))

    Public shared sub DumpFields()
        Console.WriteLine(f1.ToString("M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture))
        Console.WriteLine(f2.ToString(System.Globalization.CultureInfo.InvariantCulture))
    End Sub
End Structure

    </file>
</compilation>

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
23
42
6/7/2010 12:00:00 AM
-42.00000000000000000000000
]]>)
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source)
            Dim globalNS = compilation.SourceModule.GlobalNamespace
            ' test that shared fields add a shared constructor to members list
            Dim type = DirectCast(globalNS.GetTypeMembers("S1").Single(), NamedTypeSymbol)
            ' 2 fields + Dump + Main + cctor + implied ctor
            Assert.Equal(6, type.GetMembers().Length)

            ' const field of type date or decimal don't add shared constructor
            type = DirectCast(globalNS.GetTypeMembers("S2").Single(), NamedTypeSymbol)
            ' 2 fields + Dump + implied ctor (no cctor)
            Assert.Equal(4, type.GetMembers().Length)
        End Sub

        <Fact>
        Public Sub AsNewAnonymousTypes()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
Class C1
    Public f1 As New With { .Name = "John Smith", .Age = 34 }
    Public Property goo As New With { .Name2 = "John Smith", .Age2 = 34 }

    public shared sub main()
    end sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            AssertTheseDiagnostics(c1,
<expected>
BC30180: Keyword does not name a type.
    Public f1 As New With { .Name = "John Smith", .Age = 34 }
                     ~~~~
BC30180: Keyword does not name a type.
    Public Property goo As New With { .Name2 = "John Smith", .Age2 = 34 }
                               ~~~~
</expected>)
        End Sub

        <WorkItem(541266, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541266")>
        <Fact>
        Public Sub AsNewMissingType()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
Class C1
    Public f1 As New 
    Public Property goo As New 

    public shared sub main()
    end sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            AssertTheseDiagnostics(c1,
<expected>
BC30182: Type expected.
    Public f1 As New 
                     ~
BC30182: Type expected.
    Public Property goo As New 
                               ~    
</expected>)
        End Sub

        ''' Bug 7629
        <Fact>
        Public Sub Bug_7629()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class Base
End Class

Class Derived
End Class

Module Program
    Sub Main(args As String())
        Console.WriteLine(f1)
        Console.WriteLine(f2)
        Console.WriteLine(f3)
        Console.WriteLine(f4)
        Console.WriteLine(f5)
        'Console.WriteLine(f6)

        Console.WriteLine(p1)
        Console.WriteLine(p2)
        Console.WriteLine(p3)

    End Sub

    Dim derived = New Derived()
    Dim base = New Base()

    Dim f1 As Integer = PassByRef(f2)
    Dim f2 As Byte
    Dim f3 As Integer = PassByRef(41)
    Dim f4 As Integer = if(f1 &lt; f2, 11, 4)
    Dim f5 As Base = if(base, derived)
    
    'Dim f6 As Integer = (Function() 2010).Invoke

    Property p1 As Byte = 2
    Property p2 As Integer = PassByRef(p1)
    Property p3 As Integer = PassByRef(22)


    Function PassByRef(ByRef x As Integer) As Integer
        Return x + 1
    End Function
End Module
    </file>
</compilation>

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
1
0
42
4
Base
2
3
23
]]>)
        End Sub

        <Fact>
        Public Sub ArrayFieldWithInitializer()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim x As C = New C()
        Console.Write("{0}, {1}", C.F(1), x.G(2))
    End Sub
End Module
Class C
    Public Shared F() As Integer = New Integer(2) {1, 2, 3}
    Public G() As Integer = New Integer(2) {1, 2, 3}
End Class
    </file>
</compilation>,
                expectedOutput:=<![CDATA[2, 3]]>)
        End Sub

        <WorkItem(541398, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541398")>
        <Fact>
        Public Sub ArrayFieldWithoutInitializer()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim x As C = New C()
        Console.WriteLine("{0}, {1}", C.F3(1), x.G3(2))
        Console.WriteLine("{0}, {1}, {2}", C.F1 Is Nothing, C.F2 Is Nothing, C.F3 Is Nothing)
        Console.WriteLine("{0}, {1}, {2}", x.G1 Is Nothing, x.G2 Is Nothing, x.G3 Is Nothing)
        Console.WriteLine("{0}, {1}, {2}", x.H1 Is Nothing, x.H3 Is Nothing, x.H4 Is Nothing)
    End Sub
End Module
Class C
    Friend Shared F1 As Integer()
    Friend Shared F2() As Integer
    Friend Shared F3(2) As Integer
    Friend G1 As Integer()
    Friend G2() As Integer
    Friend G3(2) As Integer
    Friend H1(), H2 As Integer
    Friend H3(1), H4(2)
    Shared Sub New()
        F3(1) = 2
    End Sub
    Public Sub New()
        G3(2) = 3
    End Sub
End Class
    </file>
</compilation>,
                expectedOutput:=<![CDATA[2, 3
True, True, False
True, True, False
True, False, False]]>)
            compilation.VerifyIL("C..cctor",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  3
  IL_0000:  ldc.i4.3
  IL_0001:  newarr     "Integer"
  IL_0006:  stsfld     "C.F3 As Integer()"
  IL_000b:  ldsfld     "C.F3 As Integer()"
  IL_0010:  ldc.i4.1
  IL_0011:  ldc.i4.2
  IL_0012:  stelem.i4
  IL_0013:  ret
}
]]>)
            compilation.VerifyIL("C..ctor",
            <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.3
  IL_0008:  newarr     "Integer"
  IL_000d:  stfld      "C.G3 As Integer()"
  IL_0012:  ldarg.0
  IL_0013:  ldc.i4.2
  IL_0014:  newarr     "Object"
  IL_0019:  stfld      "C.H3 As Object()"
  IL_001e:  ldarg.0
  IL_001f:  ldc.i4.3
  IL_0020:  newarr     "Object"
  IL_0025:  stfld      "C.H4 As Object()"
  IL_002a:  ldarg.0
  IL_002b:  ldfld      "C.G3 As Integer()"
  IL_0030:  ldc.i4.2
  IL_0031:  ldc.i4.3
  IL_0032:  stelem.i4
  IL_0033:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ConstArrayField()
            Dim source =
<compilation>
    <file name="a.vb">
Class C
    Public Const F1(2) As Integer
    Public Const F2() As Integer = New Integer(2) {1, 2, 3}
    Friend Const F3 As Integer()
    Friend Const F4() As Integer
    Public Const F5()
    Public Const F6(2)
End Class
    </file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    Public Const F1(2) As Integer
                 ~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    Public Const F2() As Integer = New Integer(2) {1, 2, 3}
                 ~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    Friend Const F3 As Integer()
                 ~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    Friend Const F4() As Integer
                 ~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    Public Const F5()
                 ~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    Public Const F6(2)
                 ~~
</expected>)
        End Sub

        <Fact>
        Public Sub ErrorArrayField()
            Dim source =
<compilation>
    <file name="a.vb">
Class C
    Private F(2) As A
    Private G() As B = New B(0) {Nothing}
End Class
    </file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            AssertTheseDiagnostics(compilation,
<expected>
BC30002: Type 'A' is not defined.
    Private F(2) As A
                    ~
BC30002: Type 'B' is not defined.
    Private G() As B = New B(0) {Nothing}
                   ~
BC30002: Type 'B' is not defined.
    Private G() As B = New B(0) {Nothing}
                           ~
</expected>)
        End Sub
    End Class
End Namespace
