' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class ExpansionTests : Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub Enums()
            Dim source = "
Imports System
Enum E
    A
    B
End Enum
Enum F As Byte
    A = 42
End Enum
<Flags> Enum [if]
    [else] = 1
    fi
End Enum
Class C
    Dim e As E = E.B
    Dim f As F = Nothing
    Dim g As [if] = [if].else Or [if].fi
    Dim h As [if] = 5
End Class"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim rootExpr = "New C()"
            Dim value = CreateDkmClrValue(Activator.CreateInstance(type))
            Dim result = FormatResult(rootExpr, value)
            Verify(result,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable))
            Dim children = GetChildren(result)
            Verify(children,
                EvalResult("e", "B {1}", "E", "(New C()).e", DkmEvaluationResultFlags.CanFavorite, editableValue:="E.B"),
                EvalResult("f", "0", "F", "(New C()).f", DkmEvaluationResultFlags.CanFavorite, editableValue:="0"),
                EvalResult("g", "else Or fi {3}", "if", "(New C()).g", DkmEvaluationResultFlags.CanFavorite, editableValue:="[if].else Or [if].fi"),
                EvalResult("h", "5", "if", "(New C()).h", DkmEvaluationResultFlags.CanFavorite, editableValue:="5"))
        End Sub

        <Fact>
        Public Sub Nullable()
            Dim source = "
Enum E
    A
End Enum
Structure S
    Friend Sub New(f as Integer)
        Me.F = f
    End Sub
    Dim F As Object
End Structure
Class C
    Dim e1 As E? = E.A
    Dim e2 As E? = Nothing
    Dim s1 As S? = New S(1)
    Dim s2 As S? = Nothing
    Dim o1 As Object = New System.Nullable(Of S)(Nothing)
    Dim o2 As Object = New System.Nullable(Of S)()
End Class"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim value = CreateDkmClrValue(Activator.CreateInstance(type))
            Dim rootExpr = "New C()"
            Dim result = FormatResult(rootExpr, value)
            Verify(result,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable))
            Dim children = GetChildren(result)
            Verify(children,
                EvalResult("e1", "A {0}", "E?", "(New C()).e1", DkmEvaluationResultFlags.CanFavorite, editableValue:="E.A"),
                EvalResult("e2", "Nothing", "E?", "(New C()).e2", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("o1", "{S}", "Object {S}", "(New C()).o1", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("o2", "Nothing", "Object", "(New C()).o2", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("s1", "{S}", "S?", "(New C()).s1", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("s2", "Nothing", "S?", "(New C()).s2", DkmEvaluationResultFlags.CanFavorite))
            ' Dim o1 As Object = New System.Nullable(Of S)(Nothing)
            Verify(GetChildren(children(2)),
                EvalResult("F", "Nothing", "Object", "DirectCast((New C()).o1, S).F", DkmEvaluationResultFlags.CanFavorite))
            ' Dim s1 As S? = New S(1)
            Verify(GetChildren(children(4)),
                EvalResult("F", "1", "Object {Integer}", "(New C()).s1.F", DkmEvaluationResultFlags.CanFavorite))
        End Sub

        <Fact>
        Public Sub Pointers()
            Dim source =
".class private auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .field private int32* p
  .field private int32* q
  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor(native int p) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  call       void* [mscorlib]System.IntPtr::op_Explicit(native int)
    IL_000f:  stfld      int32* C::p
    IL_0014:  ret
  }
}"
            Dim assembly = GetAssemblyFromIL(source)
            Dim type = assembly.GetType("C")
            Dim p = GCHandle.Alloc(4, GCHandleType.Pinned).AddrOfPinnedObject()
            Dim rootExpr = String.Format("new C({0})", p)
            Dim value = CreateDkmClrValue(ReflectionUtilities.Instantiate(type, p))
            Dim result = FormatResult(rootExpr, value)
            Verify(result,
                    EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable))
            Dim children = GetChildren(result)
            Verify(children,
                    EvalResult("p", PointerToString(p), "Integer*", String.Format("({0}).p", rootExpr), DkmEvaluationResultFlags.Expandable),
                    EvalResult("q", PointerToString(IntPtr.Zero), "Integer*", String.Format("({0}).q", rootExpr)))
            Dim fullName = String.Format("*({0}).p", rootExpr)
            Verify(GetChildren(children(0)),
                    EvalResult(fullName, "4", "Integer", fullName, DkmEvaluationResultFlags.None))
        End Sub

        <Fact>
        Public Sub StaticMembers()
            Dim source =
"Class A
    Const F As Integer = 1
    Shared ReadOnly G As Integer = 2
End Class
Class B : Inherits A
End Class
Structure S
    Const F As Object = Nothing
    Shared ReadOnly Property P As Object
        Get
            Return 3
        End Get
    End Property
End Structure
Enum E
    A
    B
End Enum
Class C
    Dim a As A = Nothing
    Dim b As B = Nothing
    Dim s As New S()
    Dim sn? As S = Nothing
    Dim e As E = E.B
End Class"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim rootExpr = "New C()"
            Dim value = CreateDkmClrValue(Activator.CreateInstance(type))
            Dim result = FormatResult(rootExpr, value)
            Verify(result,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable))
            Dim children = GetChildren(result)
            Verify(children,
                EvalResult("a", "Nothing", "A", "(New C()).a", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("b", "Nothing", "B", "(New C()).b", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("e", "B {1}", "E", "(New C()).e", DkmEvaluationResultFlags.CanFavorite, editableValue:="E.B"),
                EvalResult("s", "{S}", "S", "(New C()).s", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("sn", "Nothing", "S?", "(New C()).sn", DkmEvaluationResultFlags.CanFavorite))

            ' Dim a As A = Nothing
            Dim more = GetChildren(children(0))
            Verify(more,
                EvalResult("Shared members", Nothing, "", "A", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            more = GetChildren(more(0))
            Verify(more,
                EvalResult("F", "1", "Integer", "A.F", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("G", "2", "Integer", "A.G", DkmEvaluationResultFlags.ReadOnly))

            ' Dim s As New S()
            more = GetChildren(children(3))
            Verify(more,
                EvalResult("Shared members", Nothing, "", "S", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            more = GetChildren(more(0))
            Verify(more,
                EvalResult("F", "Nothing", "Object", "S.F", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P", "3", "Object {Integer}", "S.P", DkmEvaluationResultFlags.ReadOnly))
        End Sub

        <Fact>
        Public Sub DeclaredTypeObject_Array()
            Dim source = "
Interface I
    Property Q As Integer
End Interface
Class A
    Friend F As Object
End Class
Class B : Inherits A : Implements I
    Friend Sub New(f As Object)
        Me.F = f
    End Sub
    Friend ReadOnly Property P As Object
        Get
            Return Me.F
        End Get
    End Property
    Property Q As Integer Implements I.Q
        Get
        End Get
        Set
        End Set
    End Property
End Class
Class C
    Dim a() As A = { New B(1) }
    Dim b() As B = { New B(2) }
    Dim i() As I = { New B(3) }
    Dim o() As Object = { New B(4) }
End Class
"
            Dim assembly = GetAssembly(source)
            Dim typeC = assembly.GetType("C")

            Dim children = GetChildren(FormatResult("c", CreateDkmClrValue(Activator.CreateInstance(typeC))))
            Verify(children,
                EvalResult("a", "{Length=1}", "A()", "c.a", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("b", "{Length=1}", "B()", "c.b", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("i", "{Length=1}", "I()", "c.i", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("o", "{Length=1}", "Object()", "c.o", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite))

            Verify(GetChildren(GetChildren(children(0)).Single()), ' as A()
                EvalResult("F", "1", "Object {Integer}", "c.a(0).F", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("P", "1", "Object {Integer}", "DirectCast(c.a(0), B).P", DkmEvaluationResultFlags.ReadOnly Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("Q", "0", "Integer", "DirectCast(c.a(0), B).Q", DkmEvaluationResultFlags.CanFavorite))

            Verify(GetChildren(GetChildren(children(1)).Single()), ' as B()
                EvalResult("F", "2", "Object {Integer}", "c.b(0).F", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("P", "2", "Object {Integer}", "c.b(0).P", DkmEvaluationResultFlags.ReadOnly Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("Q", "0", "Integer", "c.b(0).Q", DkmEvaluationResultFlags.CanFavorite))

            Verify(GetChildren(GetChildren(children(2)).Single()), ' as I()
                EvalResult("F", "3", "Object {Integer}", "DirectCast(c.i(0), A).F", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("P", "3", "Object {Integer}", "DirectCast(c.i(0), B).P", DkmEvaluationResultFlags.ReadOnly Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("Q", "0", "Integer", "DirectCast(c.i(0), B).Q", DkmEvaluationResultFlags.CanFavorite))

            Verify(GetChildren(GetChildren(children(3)).Single()), ' as Object()
                EvalResult("F", "4", "Object {Integer}", "DirectCast(c.o(0), A).F", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("P", "4", "Object {Integer}", "DirectCast(c.o(0), B).P", DkmEvaluationResultFlags.ReadOnly Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("Q", "0", "Integer", "DirectCast(c.o(0), B).Q", DkmEvaluationResultFlags.CanFavorite))
        End Sub

        <Fact>
        Public Sub MultilineString()
            Dim str = vbCrLf & "line1" & vbCrLf & "line2"
            Dim quotedStr = "vbCrLf & ""line1"" & vbCrLf & ""line2"""
            Dim value = CreateDkmClrValue(str, evalFlags:=DkmEvaluationResultFlags.RawString)
            Dim result = FormatResult("str", value)
            Verify(result,
                EvalResult("str", quotedStr, "String", "str", DkmEvaluationResultFlags.RawString, editableValue:=quotedStr))
        End Sub

        <Fact>
        Public Sub UnicodeChar()
            ' This Char is printable, so we expect the EditableValue to just be the Char.
            Dim c = ChrW(&H1234)
            Dim quotedChar = """" & c & """c"
            Dim value = CreateDkmClrValue(c)
            Dim result = FormatResult("c", value)
            Verify(result,
                EvalResult("c", quotedChar, "Char", "c", editableValue:=quotedChar))

            ' This Char is not printable, so we expect the EditableValue to be the "ChrW" representation.
            quotedChar = "ChrW(&H7)"
            value = CreateDkmClrValue(ChrW(&H0007))
            result = FormatResult("c", value, inspectionContext:=CreateDkmInspectionContext(radix:=16))
            Verify(result,
                EvalResult("c", quotedChar, "Char", "c", editableValue:=quotedChar))
        End Sub

        <Fact>
        Public Sub UnicodeString()
            Const quotedString = """" & ChrW(&H1234) & """ & ChrW(7)"
            Dim value = CreateDkmClrValue(New String({ChrW(&H1234), ChrW(&H0007)}))
            Dim result = FormatResult("s", value)
            Verify(result,
                EvalResult("s", quotedString, "String", "s", editableValue:=quotedString, flags:=DkmEvaluationResultFlags.RawString))
        End Sub

        <Fact, WorkItem(1002381, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1002381")>
        Public Sub BaseTypeEditableValue()
            Dim source = "
Imports System
Imports System.Collections.Generic
<Flags> Enum E
    A = 1
    B = 2
End Enum
Class C
    Dim s1 As IEnumerable(Of Char) = String.Empty
    Dim d1 As Object = 1D
    Dim e1 As ValueType = E.A Or E.B
End Class"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim value = CreateDkmClrValue(Activator.CreateInstance(type))
            Dim result = FormatResult("o", value)
            Verify(result,
                EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable))
            Dim children = GetChildren(result)
            Verify(children,
                EvalResult("d1", "1", "Object {Decimal}", "o.d1", DkmEvaluationResultFlags.CanFavorite, editableValue:="1D"),
                EvalResult("e1", "A Or B {3}", "System.ValueType {E}", "o.e1", DkmEvaluationResultFlags.CanFavorite, editableValue:="E.A Or E.B"),
                EvalResult("s1", """""", "System.Collections.Generic.IEnumerable(Of Char) {String}", "o.s1", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.CanFavorite, editableValue:=""""""))
        End Sub

        ''' <summary>
        ''' Hide members that have compiler-generated names.
        ''' </summary>
        ''' <remarks>
        ''' As in dev11, the FullName expressions don't parse.
        ''' </remarks> 
        <Fact, WorkItem(1010498, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1010498")>
        Public Sub HiddenMembers()
            Dim source =
".class public A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .field public object '@'
  .field public object '<'
  .field public static object '>'
  .field public static object '><'
  .field public object '<>'
  .field public object '1<>'
  .field public object '<2'
  .field public object '<>__'
  .field public object '<>k'
  .field public static object '<3>k'
  .field public static object '<<>>k'
  .field public static object '<>>k'
  .field public static object '<<>k'
  .field public static object '< >k'
  .field public object 'CS$'
  .field public object 'CS$<>0_'
  .field public object 'CS$<>7__8'
  .field public object 'CS$$<>7__8'
  .field public object 'CS<>7__8'
  .field public static object '$<>7__8'
  .field public static object 'CS$<M>7'
}
.class public B
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public instance object '<>k__get'() { ldnull ret }
  .method public static object '<M>7__get'() { ldnull ret }
  .property instance object '@'() { .get instance object B::'<>k__get'() }
  .property instance object '<'() { .get instance object B::'<>k__get'() }
  .property object '>'() { .get object B::'<M>7__get'() }
  .property object '><'() { .get object B::'<M>7__get'() }
  .property instance object '<>'() { .get instance object B::'<>k__get'() }
  .property instance object '1<>'() { .get instance object B::'<>k__get'() }
  .property instance object '<2'() { .get instance object B::'<>k__get'() }
  .property instance object '<>__'() { .get instance object B::'<>k__get'() }
  .property instance object '<>k'() { .get instance object B::'<>k__get'() }
  .property object '<3>k'() { .get object B::'<M>7__get'() }
  .property object '<<>>k'() { .get object B::'<M>7__get'() }
  .property object '<>>k'() { .get object B::'<M>7__get'() }
  .property object '<<>k'() { .get object B::'<M>7__get'() }
  .property object '< >k'() { .get object B::'<M>7__get'() }
  .property instance object 'VB$'() { .get instance object B::'<>k__get'() }
  .property instance object 'VB$<>0_'() { .get instance object B::'<>k__get'() }
  .property instance object 'VB$Me<>7__8'() { .get instance object B::'<>k__get'() }
  .property instance object 'VB$$<>7__8'() { .get instance object B::'<>k__get'() }
  .property instance object 'VB<>7__8'() { .get instance object B::'<>k__get'() }
  .property object '$<>7__8'() { .get object B::'<M>7__get'() }
  .property object 'CS$<M>7'() { .get object B::'<M>7__get'() }
}"
            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            CommonTestBase.EmitILToArray(source, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly = ReflectionUtilities.Load(assemblyBytes)

            Dim type = assembly.GetType("A")
            Dim rootExpr = "New A()"
            Dim value = CreateDkmClrValue(Activator.CreateInstance(type))
            Dim result = FormatResult(rootExpr, value)
            Verify(result,
                EvalResult(rootExpr, "{A}", "A", rootExpr, DkmEvaluationResultFlags.Expandable))
            Dim children = GetChildren(result)
            Verify(children,
                EvalResult("1<>", "Nothing", "Object", fullName:=Nothing, DkmEvaluationResultFlags.CanFavorite),
                EvalResult("@", "Nothing", "Object", fullName:=Nothing, DkmEvaluationResultFlags.CanFavorite),
                EvalResult("CS<>7__8", "Nothing", "Object", fullName:=Nothing, DkmEvaluationResultFlags.CanFavorite),
                EvalResult("Shared members", Nothing, "", "A", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            children = GetChildren(children(children.Length - 1))
            Verify(children,
                EvalResult(">", "Nothing", "Object", fullName:=Nothing),
                EvalResult("><", "Nothing", "Object", fullName:=Nothing))

            type = assembly.GetType("B")
            rootExpr = "New B()"
            value = CreateDkmClrValue(Activator.CreateInstance(type))
            result = FormatResult(rootExpr, value)
            Verify(result,
                EvalResult(rootExpr, "{B}", "B", rootExpr, DkmEvaluationResultFlags.Expandable))
            children = GetChildren(result)
            Verify(children,
                EvalResult("1<>", "Nothing", "Object", fullName:=Nothing, flags:=DkmEvaluationResultFlags.ReadOnly Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("@", "Nothing", "Object", fullName:=Nothing, flags:=DkmEvaluationResultFlags.ReadOnly Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("VB<>7__8", "Nothing", "Object", fullName:=Nothing, flags:=DkmEvaluationResultFlags.ReadOnly Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("Shared members", Nothing, "", "B", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            children = GetChildren(children(children.Length - 1))
            Verify(children,
                EvalResult(">", "Nothing", "Object", fullName:=Nothing, flags:=DkmEvaluationResultFlags.ReadOnly),
                EvalResult("><", "Nothing", "Object", fullName:=Nothing, flags:=DkmEvaluationResultFlags.ReadOnly))
        End Sub

        <Fact, WorkItem(965892, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/965892")>
        Public Sub DeclaredTypeAndRuntimeTypeDifferent()
            Dim source = "
Class A
End Class
Class B : Inherits A
End Class"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("B")
            Dim declaredType = assembly.GetType("A")
            Dim value = CreateDkmClrValue(Activator.CreateInstance(type), type)
            Dim result = FormatResult("a", value, New DkmClrType(CType(declaredType, TypeImpl)))
            Verify(result,
                EvalResult("a", "{B}", "A {B}", "a", DkmEvaluationResultFlags.None))
            Dim children = GetChildren(result)
            Verify(children)
        End Sub

        <Fact>
        Public Sub NameConflictsWithFieldOnBase()
            Dim source = "
Class A
    Private f As Integer
End Class
Class B : Inherits A
    Friend f As Double
End Class"
            Dim assembly = GetAssembly(source)
            Dim typeB = assembly.GetType("B")
            Dim instanceB = Activator.CreateInstance(typeB)
            Dim value = CreateDkmClrValue(instanceB, typeB)
            Dim result = FormatResult("b", value)
            Verify(GetChildren(result),
                EvalResult("f (A)", "0", "Integer", "DirectCast(b, A).f"),
                EvalResult("f", "0", "Double", "b.f", DkmEvaluationResultFlags.CanFavorite))

            Dim typeA = assembly.GetType("A")
            value = CreateDkmClrValue(instanceB, typeB)
            result = FormatResult("a", value, New DkmClrType(CType(typeA, TypeImpl)))
            Verify(GetChildren(result),
                EvalResult("f (A)", "0", "Integer", "a.f"),
                EvalResult("f", "0", "Double", "DirectCast(a, B).f", DkmEvaluationResultFlags.CanFavorite))
        End Sub

        <Fact>
        Public Sub NameConflictsWithFieldsOnMultipleBase()
            Dim source = "
Class A
    Private f As Integer
End Class
Class B : Inherits A
    Friend f As Double
End Class
Class C : Inherits B
End Class"
            Dim assembly = GetAssembly(source)
            Dim typeC = assembly.GetType("C")
            Dim instanceC = Activator.CreateInstance(typeC)
            Dim value = CreateDkmClrValue(instanceC, typeC)
            Dim result = FormatResult("c", value)
            Verify(GetChildren(result),
                EvalResult("f (A)", "0", "Integer", "DirectCast(c, A).f"),
                EvalResult("f", "0", "Double", "c.f", DkmEvaluationResultFlags.CanFavorite))

            Dim typeB = assembly.GetType("B")
            value = CreateDkmClrValue(instanceC, typeC)
            result = FormatResult("b", value, New DkmClrType(CType(typeB, TypeImpl)))
            Verify(GetChildren(result),
                EvalResult("f (A)", "0", "Integer", "DirectCast(b, A).f"),
                EvalResult("f", "0", "Double", "b.f", DkmEvaluationResultFlags.CanFavorite))
        End Sub

        <Fact>
        Public Sub NameConflictsWithPropertyOnNestedBase()
            Dim source = "
Class A
    Private Property P As Integer

    Class B : Inherits A
        Friend Property P As Double
    End Class
End Class
Class C : Inherits A.B
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim instanceC = Activator.CreateInstance(type)
            Dim value = CreateDkmClrValue(instanceC, type)
            Dim result = FormatResult("c", value)
            Verify(GetChildren(result),
                EvalResult("P (A)", "0", "Integer", "DirectCast(c, A).P"),
                EvalResult("P", "0", "Double", "c.P", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("_P (A)", "0", "Integer", "DirectCast(c, A)._P"),
                EvalResult("_P", "0", "Double", "c._P", DkmEvaluationResultFlags.CanFavorite))
        End Sub

        <Fact>
        Public Sub NameConflictsWithPropertyOnGenericBase()
            Dim source = "
Class A(Of T)
    Public Property P As T
End Class
Class B : Inherits A(Of Integer)
    Private Property P As Double
End Class
Class C : Inherits B
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim instanceC = Activator.CreateInstance(type)
            Dim value = CreateDkmClrValue(instanceC, type)
            Dim result = FormatResult("c", value)
            Verify(GetChildren(result),
                EvalResult("P (A(Of Integer))", "0", "Integer", "DirectCast(c, A(Of Integer)).P"),
                EvalResult("P", "0", "Double", "c.P", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("_P (A(Of Integer))", "0", "Integer", "DirectCast(c, A(Of Integer))._P"),
                EvalResult("_P", "0", "Double", "c._P", DkmEvaluationResultFlags.CanFavorite))
        End Sub

        <Fact>
        Public Sub PropertyNameConflictsWithFieldOnBase()
            Dim source = "
Class A
    Public F As String
End Class
Class B : Inherits A
    Private Property F As Double
End Class
Class C : Inherits B
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim instanceC = Activator.CreateInstance(type)
            Dim value = CreateDkmClrValue(instanceC, type)
            Dim result = FormatResult("c", value)
            Verify(GetChildren(result),
                EvalResult("F (A)", "Nothing", "String", "DirectCast(c, A).F"),
                EvalResult("F", "0", "Double", "c.F", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("_F", "0", "Double", "c._F", DkmEvaluationResultFlags.CanFavorite))
        End Sub

        <Fact>
        Public Sub NameConflictsWithIndexerOnBase()
            Dim source = "
Class A
    Public ReadOnly Property P(x As String) As String
        Get
            Return ""DeriveMe""
        End Get
    End Property
End Class
Class B : Inherits A
    Public Property P As String = ""Derived""
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("B")
            Dim instanceB = Activator.CreateInstance(type)
            Dim value = CreateDkmClrValue(instanceB, type)
            Dim result = FormatResult("b", value)
            Verify(GetChildren(result),
                EvalResult("P", """Derived""", "String", "b.P", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.CanFavorite, editableValue:="""Derived"""),
                EvalResult("_P", """Derived""", "String", "b._P", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.CanFavorite, editableValue:="""Derived"""))
        End Sub

        <Fact>
        Public Sub NameConflictsWithPropertyHiddenByNameOnBase()
            Dim source = "
Class A
    Shared S As Integer = 42
    Friend Overridable Property p As Integer = 43
End Class
Class B : Inherits A
    Friend Overrides Property P As Integer = 45
End Class
Class C : Inherits B
    Shadows Property P As Double = 4.4
End Class
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim instanceC = Activator.CreateInstance(type)
            Dim value = CreateDkmClrValue(instanceC, type)
            Dim result = FormatResult("c", value)
            Dim children = GetChildren(result)
            ' TODO:  Name hiding across overrides should be case-insensitive in VB,
            ' so "p" in this case should be hidden by "P" (and we need to qualify "p"
            ' with the type name "B" To disambiguate).  However, we also need to
            ' support multiple members on the same type that differ only by case
            ' (properties in C# that have the same name as their backing field, etc).
            Verify(children,
                EvalResult("P", "4.4", "Double", "c.P", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("_P (B)", "45", "Integer", "DirectCast(c, B)._P"),
                EvalResult("_P", "4.4", "Double", "c._P", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("_p", "0", "Integer", "c._p", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("p", "45", "Integer", "c.p", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("Shared members", Nothing, "", "C", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))
            Verify(GetChildren(children(5)),
                EvalResult("S", "42", "Integer", "A.S"))
        End Sub

        <Fact, WorkItem(1074435, "DevDiv")>
        Public Sub NameConflictsWithInterfaceReimplementation()
            Dim source = "
Interface I
    ReadOnly Property P As Integer
End Interface

Class A : Implements I
    Public ReadOnly Property P As Integer Implements I.P
        Get
            Return 1
        End Get
    End Property
End Class

Class B : Inherits A : Implements I
    Public ReadOnly Property P As Integer Implements I.P
        Get
            Return 2
        End Get
    End Property
End Class

Class C : Inherits B : Implements I
    Public ReadOnly Property P As Integer Implements I.P
        Get
            Return 3
        End Get
    End Property
End Class
"
            Dim assembly = GetAssembly(source)
            Dim typeB = assembly.GetType("B")
            Dim typeC = assembly.GetType("C")
            Dim instanceC = Activator.CreateInstance(typeC)
            Dim value = CreateDkmClrValue(instanceC, typeC)
            Dim result = FormatResult("b", value, New DkmClrType(CType(typeB, TypeImpl)))
            Dim children = GetChildren(result)
            Verify(children,
                EvalResult("P (A)", "1", "Integer", "DirectCast(b, A).P", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P (B)", "2", "Integer", "b.P", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P", "3", "Integer", "DirectCast(b, C).P", DkmEvaluationResultFlags.ReadOnly Or DkmEvaluationResultFlags.CanFavorite))
        End Sub

        <Fact>
        Public Sub NameConflictsWithVirtualPropertiesAcrossDeclaredType()
            Dim source = "
Class A 
    Public Overridable Property P As Integer = 1
End Class
Class B : Inherits A
    Public Overrides Property P As Integer = 2
End Class
Class C : Inherits B
End Class
Class D : Inherits C
    Public Overrides Property p As Integer = 3
End Class"
            Dim assembly = GetAssembly(source)
            Dim typeC = assembly.GetType("C")
            Dim typeD = assembly.GetType("D")
            Dim instanceD = Activator.CreateInstance(typeD)
            Dim value = CreateDkmClrValue(instanceD, typeD)
            Dim result = FormatResult("c", value, New DkmClrType(CType(typeC, TypeImpl)))
            Dim children = GetChildren(result)
            ' Ideally, we would only emit "c.P" for the full name of properties, but
            ' the added complexity of figuring that out (instead always just calling
            ' most derived) doesn't seem worth it.
            Verify(children,
                EvalResult("P", "3", "Integer", "DirectCast(c, D).P", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("_P (A)", "0", "Integer", "DirectCast(c, A)._P"),
                EvalResult("_P", "0", "Integer", "c._P", DkmEvaluationResultFlags.CanFavorite),
                EvalResult("_p", "3", "Integer", "DirectCast(c, D)._p", DkmEvaluationResultFlags.CanFavorite))
        End Sub

        <WorkItem(1016895, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1016895")>
        <Fact>
        Public Sub RootVersusInternal()
            Const source = "
Imports System.Diagnostics

<DebuggerDisplay(""Value"", Name:=""Name"")>
Class A
End Class

Class B
    Public A As A
    
    Public Sub New(a As A)
        Me.A = a
    End Sub
End Class
"
            Dim assembly = GetAssembly(source)
            Dim typeA = assembly.GetType("A")
            Dim typeB = assembly.GetType("B")
            Dim instanceA = typeA.Instantiate()
            Dim instanceB = typeB.Instantiate(instanceA)
            Dim result = FormatResult("a", CreateDkmClrValue(instanceA))
            Verify(result,
                EvalResult("a", "Value", "A", "a", DkmEvaluationResultFlags.None))

            result = FormatResult("b", CreateDkmClrValue(instanceB))
            Verify(GetChildren(result),
                EvalResult("Name", "Value", "A", "b.A", DkmEvaluationResultFlags.None))
        End Sub

    End Class

End Namespace
