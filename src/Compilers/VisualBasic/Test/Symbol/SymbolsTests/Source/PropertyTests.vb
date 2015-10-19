' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class PropertyTests
        Inherits BasicTestBase
#Region "Basic Test cases"

        ' Allow assigning to property name in Get accessor.
        <Fact>
        Public Sub AssignToPropertyNameInGet()
            Dim source =
<compilation>
    <file name="c.vb">
Class C
    ReadOnly Property P
        Get
            P = Nothing
        End Get
    End Property
End Class
    </file>
</compilation>
            CompileAndVerify(source)
        End Sub

        ' Properties with setter implemented by getter: not supported.
        <Fact>
        Public Sub GetUsedAsSet()
            Dim customIL = <![CDATA[
.class public A
{
    .method public static bool get_s() { ldnull throw }
    .method public instance int32 get_i() { ldnull throw }
    .property bool P()
    {
        .get bool A::get_s()
        .set bool A::get_s()
    }
    .property int32 Q()
    {
        .get instance int32 A::get_i()
        .set instance int32 A::get_i()
    }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Class B
    Shared Sub M(x As A)
        N(A.P)
        N(x.Q)
    End Sub
    Shared Sub N(o)
    End Sub
End Class
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30643: Property 'A.P' is of an unsupported type.
        N(A.P)
            ~
BC30643: Property 'A.Q' is of an unsupported type.
        N(x.Q)
            ~
</expected>)
        End Sub

        ' Property and accessor signatures have mismatched parameter counts: not
        ' supported. (Note: Native compiler allows these properties but provides
        ' errors when trying to use cases where either the accessor or
        ' property signature has zero parameters and the other has non-zero.)
        <Fact>
        Public Sub PropertyParameterCountMismatch()
            Dim customIL = <![CDATA[
.class public A
{
    .method public instance bool get_0() { ldnull throw }
    .method public instance void set_0(bool val) { ret }
    .method public instance bool get_1(int32 index) { ldnull throw }
    .method public instance void set_1(int32 index, bool val) { ret }
    .method public instance bool get_2(int32 x, int32 y) { ldnull throw }
    .method public instance void set_2(int32 x, int32 y, bool val) { ret }
    .property bool P()
    {
        .get instance bool A::get_1(int32 index)
        .set instance void A::set_1(int32 index, bool val)
    }
    .property bool Q(int32 index)
    {
        .get instance bool A::get_2(int32 x, int32 y)
        .set instance void A::set_2(int32 x, int32 y, bool val)
    }
    .property bool R(int32 x, int32 y)
    {
        .get instance bool A::get_0()
        .set instance void A::set_0(bool val)
    }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Shared Sub M(x As A)
        Dim y As Boolean
        y = x.P
        y = x.Q(0)
        y = x.R(0, 1)
        x.P = y
        x.Q(0) = y
        x.R(0, 1) = y
    End Sub
End Class
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30643: Property 'A.P' is of an unsupported type.
        y = x.P
              ~
BC30643: Property 'A.Q(x As Integer)' is of an unsupported type.
        y = x.Q(0)
              ~
BC30643: Property 'A.R(val As Integer, Param As Integer)' is of an unsupported type.
        y = x.R(0, 1)
              ~
BC30643: Property 'A.P' is of an unsupported type.
        x.P = y
          ~
BC30643: Property 'A.Q(x As Integer)' is of an unsupported type.
        x.Q(0) = y
          ~
BC30643: Property 'A.R(val As Integer, Param As Integer)' is of an unsupported type.
        x.R(0, 1) = y
          ~
</expected>)
        End Sub

        ' Properties with one static and one instance accessor.
        ' Dev11 uses the accessor to determine whether access
        ' is through instance or type name. Roslyn does not
        ' support such properties. Breaking change.
        <WorkItem(528159, "DevDiv")>
        <Fact()>
        Public Sub MismatchedStaticInstanceAccessors()
            Dim customIL = <![CDATA[
.class public A
{
    .method public static int32 get_s() { ldc.i4.0 ret }	
    .method public static void set_s(int32 val) { ret }
    .method public instance int32 get_i() { ldc.i4.0 ret }	
    .method public instance void set_i(int32 val) { ret }
    .property int32 P()
    {
        .get int32 A::get_s()
        .set instance void A::set_i(int32 val)
    }
    .property int32 Q()
    {
        .get instance int32 A::get_i()
        .set void A::set_s(int32 val)
    }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Shared Sub M(x As A)
        x.P = x.Q
        A.Q = A.P
    End Sub
End Class
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL)
            compilation.AssertTheseDiagnostics(
<expected>
BC30643: Property 'A.P' is of an unsupported type.
        x.P = x.Q
          ~
BC30643: Property 'A.Q' is of an unsupported type.
        x.P = x.Q
                ~
BC30643: Property 'A.Q' is of an unsupported type.
        A.Q = A.P
          ~
BC30643: Property 'A.P' is of an unsupported type.
        A.Q = A.P
                ~
</expected>)
        End Sub

        ' Property with type that does not match accessors.
        ' Expression type should be determined from accessor.
        <WorkItem(528160, "DevDiv")>
        <Fact()>
        Public Sub WrongPropertyType()
            Dim customIL = <![CDATA[
.class public A
{
    .method public instance int32 get() { ldc.i4.0 ret }
    .method public instance void set(int32 val) { ret }
    .property string P()
    {
        .get instance int32 A::get()
        .set instance void A::set(int32 val)
    }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Shared Sub M(x As A)
        Dim y As Integer = x.P
        x.P = y
    End Sub
End Class
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        ' Property with void type. (IDE with native compiler crashes.)
        <Fact>
        Public Sub VoidPropertyType()
            Dim customIL = <![CDATA[
.class public A
{
    .method public instance int32 get() { ldc.i4.0 ret }
    .method public instance void set(int32 val) { ret }
    .property void P()
    {
        .get instance int32 A::get()
        .set instance void A::set(int32 val)
    }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Shared Sub M(x As A)
        Dim y As Integer = x.P
        x.P = y
    End Sub
End Class
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL)
            compilation.AssertNoErrors()
        End Sub

        ' Property and getter with void type. Dev10 treats this as a valid
        ' property. Would it be better to treat the property as invalid?
        <Fact>
        Public Sub VoidPropertyAndAccessorType()
            Dim customIL = <![CDATA[
.class public A
{
    .method public static void get_P() { ret }
    .property void P() { .get void A::get_P() }
    .method public instance void get_Q() { ret }
    .property void Q() { .get instance void A::get_Q() }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Class B
    Shared Sub M(x As A)
        N(A.P)
        N(x.Q)
    End Sub
    Shared Sub N(o)
    End Sub
End Class
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
        N(A.P)
          ~~~
BC30491: Expression does not produce a value.
        N(x.Q)
          ~~~
</expected>)
        End Sub

        ' Properties where the property and accessor signatures differ by
        ' modopt only should be supported (as in the native compiler).
        <Fact>
        Public Sub SignaturesDifferByModOptsOnly()
            Dim ilSource = <![CDATA[
.class public A { }
.class public B { }
.class public C
{
	.method public instance int32 get_noopt()
	{
	    ldc.i4.0
		ret
	}
	.method public instance int32 modopt(A) get_returnopt()
	{
	    ldc.i4.0
		ret
	}
    .method public instance void set_noopt(int32 val)
	{
		ret
	}
    .method public instance void set_argopt(int32 modopt(A) val)
	{
		ret
	}
    .method public instance void modopt(A) set_returnopt(int32 val)
	{
		ret
	}
	// Modifier on property but not accessors.
	.property int32 modopt(A) P1()
	{
	    .get instance int32 C::get_noopt()
		.set instance void C::set_noopt(int32)
	}
	// Modifier on accessors but not property.
	.property int32 P2()
	{
	    .get instance int32 modopt(A) C::get_returnopt()
		.set instance void C::set_argopt(int32 modopt(A))
	}
	// Modifier on getter only.
	.property int32 P3()
	{
	    .get instance int32 modopt(A) C::get_returnopt()
		.set instance void C::set_noopt(int32)
	}
	// Modifier on setter only.
	.property int32 P4()
	{
	    .get instance int32 C::get_noopt()
		.set instance void C::set_argopt(int32 modopt(A))
	}
	// Modifier on setter return type.
	.property int32 P5()
	{
	    .get instance int32 C::get_noopt()
		.set instance void modopt(A) C::set_returnopt(int32)
	}
	// Modifier on property and different modifier on accessors.
	.property int32 modopt(B) P6()
	{
	    .get instance int32 modopt(A) C::get_returnopt()
		.set instance void C::set_argopt(int32 modopt(A))
	}
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class D
    Shared Sub M(c As C)
        c.P1 = c.P1
        c.P2 = c.P2
        c.P3 = c.P3
        c.P4 = c.P4
        c.P5 = c.P5
        c.P6 = c.P6
    End Sub
End Class
]]>
                    </file>
                </compilation>

            CompileWithCustomILSource(vbSource, ilSource)
        End Sub

        <Fact>
        Public Sub PropertyGetAndSet()
            Dim source =
<compilation>
    <file name="c.vb">
Module M
    Sub Main()
        Dim x As C = New C()
        x.P = 2
        C.Q = "q"
        System.Console.Write("{0}, {1}", x.P, C.Q)
    End Sub
End Module
Class C
    Private _p
    Property P
        Get
            Return _p
        End Get
        Set(value)
            _p = value
        End Set
    End Property
    Private Shared _q
    Shared Property Q
        Get
            Return _q
        End Get
        Set(value)
            _q = value
        End Set
    End Property
End Class
    </file>
</compilation>

            Dim compilationVerifier = CompileAndVerify(source, expectedOutput:="2, q")
            compilationVerifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  3
  .locals init (C V_0) //x
  IL_0000:  newobj     "Sub C..ctor()"
  IL_0005:  stloc.0   
  IL_0006:  ldloc.0   
  IL_0007:  ldc.i4.2  
  IL_0008:  box        "Integer"
  IL_000d:  callvirt   "Sub C.set_P(Object)"
  IL_0012:  ldstr      "q"
  IL_0017:  call       "Sub C.set_Q(Object)"
  IL_001c:  ldstr      "{0}, {1}"
  IL_0021:  ldloc.0   
  IL_0022:  callvirt   "Function C.get_P() As Object"
  IL_0027:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_002c:  call       "Function C.get_Q() As Object"
  IL_0031:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0036:  call       "Sub System.Console.Write(String, Object, Object)"
  IL_003b:  ret       
}
]]>)
        End Sub

        <Fact>
        Public Sub PropertyAutoGetAndSet()

            Dim source =
<compilation>
    <file name="c.vb">
Module M
    Sub Main()
        Dim x As C = New C()
        x.P = 2
        C.Q = "q"
        System.Console.Write("{0}, {1}", x.P, C.Q)
    End Sub
End Module
Class C
    Property P
    Shared Property Q
End Class
    </file>
</compilation>
            Dim compilationVerifier = CompileAndVerify(source, expectedOutput:="2, q")

            compilationVerifier.VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  3
  .locals init (C V_0) //x
  IL_0000:  newobj     "Sub C..ctor()"
  IL_0005:  stloc.0   
  IL_0006:  ldloc.0   
  IL_0007:  ldc.i4.2  
  IL_0008:  box        "Integer"
  IL_000d:  callvirt   "Sub C.set_P(Object)"
  IL_0012:  ldstr      "q"
  IL_0017:  call       "Sub C.set_Q(Object)"
  IL_001c:  ldstr      "{0}, {1}"
  IL_0021:  ldloc.0   
  IL_0022:  callvirt   "Function C.get_P() As Object"
  IL_0027:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_002c:  call       "Function C.get_Q() As Object"
  IL_0031:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0036:  call       "Sub System.Console.Write(String, Object, Object)"
  IL_003b:  ret       
}
]]>)

            compilationVerifier.VerifyIL("C.get_P",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C._P As Object"
  IL_0006:  ret
}
]]>)

            compilationVerifier.VerifyIL("C.set_P",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0007:  stfld      "C._P As Object"
  IL_000c:  ret       
}
]]>)

            compilationVerifier.VerifyIL("C.get_Q",
            <![CDATA[
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldsfld     "C._Q As Object"
  IL_0005:  ret
}
]]>)

            compilationVerifier.VerifyIL("C.set_Q",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0006:  stsfld     "C._Q As Object"
  IL_000b:  ret       
}
]]>)
        End Sub
#End Region
#Region "Code Gen"
        ' All property overload metadata should have a name that matches the casing the of the first declared overload
        <WorkItem(539893, "DevDiv")>
        <Fact()>
        Public Sub PropertiesILCaseSensitivity()
            Dim source =
            <compilation>
                <file name="a.vb">
Public Class TestClass
    Property P As Integer
    Property p(i As Integer) As Integer
        Get
            Return Nothing
        End Get
        Set(value As Integer)

        End Set
    End Property
    Overridable Property P(i As String) As Integer
        Get
            Return Nothing
        End Get
        Set(value As Integer)

        End Set
    End Property
End Class
    </file>
            </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source, OutputKind.DynamicallyLinkedLibrary)
            Dim referenceBytes = New IO.MemoryStream()
            compilation.Emit(referenceBytes)
            Dim symbols = MetadataTestHelpers.GetSymbolsForReferences({referenceBytes.GetBuffer()}).Single()

            Dim testClassSymbol = symbols.Modules.First().GlobalNamespace.GetMembers("TestClass").OfType(Of NamedTypeSymbol).Single()
            Dim propertySymbols = testClassSymbol.GetMembers("P").OfType(Of PropertySymbol)()
            Dim propertyGettersSymbols = testClassSymbol.GetMembers("get_P").OfType(Of MethodSymbol)()

            Assert.Equal(propertySymbols.Count(Function(psymb) psymb.Name.Equals("P")), 3)
            Assert.Equal(propertyGettersSymbols.Count(Function(msymb) msymb.Name.Equals("get_p")), 1)
            Assert.Equal(propertyGettersSymbols.Count(Function(msymb) msymb.Name.Equals("get_P")), 2)
        End Sub
#End Region
#Region "Properties Parameters"
        ' Set method with no explicit parameter.
        <Fact>
        Public Sub SetParameterImplicit()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation>
        <file name="c.vb">
Class C
    Private _p As Object
    Property P
        Get
            Return _p
        End Get
        Set
            _p = value
        End Set
    End Property
End Class
        </file>
    </compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        ' Set method with parameter name different from default.
        <Fact>
        Public Sub SetParameterNonDefaultName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation>
        <file name="c.vb">
Class C
    Private _p As Object
    Property P
        Get
            Return _p
        End Get
        Set(v)
            _p = v
        End Set
    End Property
End Class
        </file>
    </compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        ' Set method must specify type if property type is not Object.
        <Fact>
        Public Sub SetParameterExplicitTypeForNonObjectProperty()
            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation>
        <file name="c.vb">
Class C
    Private _p As Integer
    Property P As Integer
        Get
            Return _p
        End Get
        Set(value)
            _p = value
        End Set
    End Property
End Class
        </file>
    </compilation>).
VerifyDiagnostics(
    Diagnostic(ERRID.ERR_SetValueNotPropertyType, "value"))
        End Sub

        <Fact>
        Public Sub PropertyMembers()
            Dim sources = <compilation>
                              <file name="c.vb">
Interface I
    Property P
    ReadOnly Property Q
    WriteOnly Property R
End Interface
MustInherit Class A
    MustOverride Property P
    MustOverride ReadOnly Property Q
    MustOverride WriteOnly Property R
End Class
Class B
    Property P
    ReadOnly Property Q
        Get
            Return Nothing
        End Get
    End Property
    WriteOnly Property R
        Set
        End Set
    End Property
End Class
    </file>
                          </compilation>
            Dim validator = Function(isFromSource As Boolean) _
                                Sub([module] As ModuleSymbol)
                                    Dim type = [module].GlobalNamespace.GetTypeMembers("I").Single()
                                    VerifyProperty(type, "P", Accessibility.Public, isFromSource, hasGet:=True, hasSet:=True, hasField:=False)
                                    VerifyProperty(type, "Q", Accessibility.Public, isFromSource, hasGet:=True, hasSet:=False, hasField:=False)
                                    VerifyProperty(type, "R", Accessibility.Public, isFromSource, hasGet:=False, hasSet:=True, hasField:=False)

                                    type = [module].GlobalNamespace.GetTypeMembers("A").Single()
                                    VerifyProperty(type, "P", Accessibility.Public, isFromSource, hasGet:=True, hasSet:=True, hasField:=False)
                                    VerifyProperty(type, "Q", Accessibility.Public, isFromSource, hasGet:=True, hasSet:=False, hasField:=False)
                                    VerifyProperty(type, "R", Accessibility.Public, isFromSource, hasGet:=False, hasSet:=True, hasField:=False)

                                    type = [module].GlobalNamespace.GetTypeMembers("B").Single()
                                    VerifyProperty(type, "P", Accessibility.Public, isFromSource, hasGet:=True, hasSet:=True, hasField:=True)
                                    VerifyProperty(type, "Q", Accessibility.Public, isFromSource, hasGet:=True, hasSet:=False, hasField:=False)
                                    VerifyProperty(type, "R", Accessibility.Public, isFromSource, hasGet:=False, hasSet:=True, hasField:=False)
                                End Sub
            CompileAndVerify(sources, sourceSymbolValidator:=validator(True), symbolValidator:=validator(False))
        End Sub

        Private Sub VerifyProperty(type As NamedTypeSymbol, name As String, declaredAccessibility As Accessibility, isFromSource As Boolean, hasGet As Boolean, hasSet As Boolean, hasField As Boolean)
            Dim [property] = TryCast(type.GetMembers(name).SingleOrDefault(), PropertySymbol)
            Assert.NotNull([property])
            Assert.Equal([property].DeclaredAccessibility, declaredAccessibility)

            Dim accessor = [property].GetMethod
            If hasGet Then
                Assert.NotNull(accessor)
                Assert.Equal(accessor.DeclaredAccessibility, declaredAccessibility)
            Else
                Assert.Null(accessor)
            End If

            accessor = [property].SetMethod
            If hasSet Then
                Assert.NotNull(accessor)
                Assert.Equal(accessor.DeclaredAccessibility, declaredAccessibility)
            Else
                Assert.Null(accessor)
            End If

            Dim field = DirectCast(type.GetMembers("_" + name).SingleOrDefault(), FieldSymbol)
            If isFromSource AndAlso hasField Then
                Assert.NotNull(field)
                Assert.Equal(field.DeclaredAccessibility, Accessibility.Private)
            Else
                Assert.Null(field)
            End If
        End Sub

        <Fact>
        Public Sub PropertyGetAndSetWithParameters()
            Dim sources = <compilation>
                              <file name="c.vb">
Module Module1
    Sub Main()
        Dim x As C = New C()
        x.P(1) = 2
        System.Console.Write("{0}, {1}", x.P(1), x.P(x.P(1)))
    End Sub
End Module
Class C
    Private _p As Object
    Property P(ByVal i As Integer) As Object
        Get
            If (i = 1) Then
                Return _p
            End If
            Return 0
        End Get
        Set(ByVal value As Object)
            If (i = 1) Then
                _p = value
            End If
        End Set
    End Property
End Class
    </file>
                          </compilation>
            Dim validator = Sub([module] As ModuleSymbol)
                                Dim type = [module].GlobalNamespace.GetTypeMembers("C").Single()
                                Dim [property] = type.GetMembers("P").OfType(Of PropertySymbol)().SingleOrDefault()

                                Assert.NotNull([property])
                                VerifyPropertiesParametersCount([property], 1)

                                Assert.Equal(SpecialType.System_Object, [property].Type.SpecialType)
                                Assert.Equal(SpecialType.System_Int32, [property].Parameters(0).Type.SpecialType)

                                Assert.Equal(SpecialType.System_Int32, [property].GetMethod.Parameters(0).Type.SpecialType)

                                Assert.Equal(SpecialType.System_Int32, [property].SetMethod.Parameters(0).Type.SpecialType)
                                Assert.Equal(SpecialType.System_Object, [property].SetMethod.Parameters(1).Type.SpecialType)
                                Assert.Equal([property].SetMethod.Parameters(1).Name, "value")
                            End Sub
            CompileAndVerify(sources, sourceSymbolValidator:=validator, symbolValidator:=validator, expectedOutput:="2, 0")
        End Sub

        <Fact()>
        Public Sub PropertyGetAndSetWithParametersOverridesAndGeneric()
            Dim sources = <compilation>
                              <file name="c.vb">
Public MustInherit Class TestClass(Of T)
End Class
Public Class TestClass2
    Inherits TestClass(Of String)
    Public Overloads Property P1(pr1 As String, Optional pr2 As String = "") As Integer
        Get
            Return Nothing
        End Get
        Set(value As Integer)
        End Set
    End Property
    Public Overloads Property P1(pr1 As Integer, pr2 As String, ParamArray parray() As Double) As Integer
        Get
            Return Nothing
        End Get
        Set(value As Integer)
        End Set
    End Property
    Public Overloads Property P2(pr1 As Integer, pr2 As String) As Integer
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property

    Public Overloads Property P2(pr1 As String, Optional pr2 As String = Nothing) As String
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
End Class
                               </file>
                          </compilation>
            Dim validator = Sub([module] As ModuleSymbol)
                                Dim type = [module].GlobalNamespace.GetTypeMembers("TestClass2").Single()
                                Dim P1s = type.GetMembers("P1").OfType(Of PropertySymbol)().OrderBy(Function(symb) symb.GetMethod.Parameters.Length)
                                Dim P2s = type.GetMembers("P2").OfType(Of PropertySymbol)().OrderBy(Function(symb) symb.GetMethod.ReturnType.Name)

                                Assert.NotNull(P1s)
                                Assert.NotNull(P2s)
                                Assert.NotEmpty(P1s)
                                Assert.NotEmpty(P2s)

                                VerifyPropertiesParametersCount(P1s.ElementAt(0), 2)
                                VerifyPropertiesParametersCount(P1s.ElementAt(1), 3)

                                Assert.True(P1s.ElementAt(0).Parameters(1).IsOptional)
                                Assert.Equal(P1s.ElementAt(0).Parameters(1).ExplicitDefaultValue, String.Empty)
                                Assert.True(P1s.ElementAt(1).Parameters(2).IsParamArray)

                                VerifyPropertiesParametersCount(P2s.ElementAt(0), 2)
                                VerifyPropertiesParametersCount(P2s.ElementAt(1), 2)

                                Assert.Equal(SpecialType.System_String, P2s.ElementAt(1).Type.SpecialType)

                            End Sub
            CompileAndVerify(sources, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        <Fact>
        Public Sub SetWithParametersGetObjectValue()
            Dim compilationVerifier = CompileAndVerify(
                <compilation>
                    <file name="c.vb">
Class C
    Sub Invoke(arg, value)
        P(arg) = value
    End Sub
    Property P(arg As Object) As Object
        Get
            Return Nothing
        End Get
        Set(ByVal value As Object)
        End Set
    End Property
End Class
        </file>
                </compilation>)

            compilationVerifier.VerifyIL("C.Invoke",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  3
  IL_0000:  ldarg.0   
  IL_0001:  ldarg.1   
  IL_0002:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0007:  ldarg.2   
  IL_0008:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000d:  call       "Sub C.set_P(Object, Object)"
  IL_0012:  ret       
}
]]>)
        End Sub

        <Fact>
        Public Sub DictionaryMemberAccess()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Imports System
Imports System.Collections.Generic
Class C
    Private _P As Dictionary(Of String, String)
    Default Property P(key As String) As String
        Get
            Return _P(key)
        End Get
        Set(value As String)
            _P(key) = value
        End Set
    End Property
    Sub New()
        _P = New Dictionary(Of String, String)
    End Sub
    Shared Sub Main()
        Dim x As C = New C()
        x("A") = "value"
        x!B = x!A.ToUpper()
        Console.WriteLine("A={0}, B={1}", x("A"), x("B"))
    End Sub
End Class
    </file>
</compilation>,
            expectedOutput:="A=value, B=VALUE")
        End Sub

        <Fact>
        Public Sub DictionaryMemberAccessPassByRef()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Imports System
Imports System.Collections.Generic
Class A
    Private _P As Dictionary(Of String, String)
    Default Property P(key As String) As String
        Get
            Return _P(key)
        End Get
        Set(value As String)
            _P(key) = value
        End Set
    End Property
    Sub New()
        _P = New Dictionary(Of String, String)
    End Sub
End Class
Class B
    Private _Q As Dictionary(Of String, A)
    Default ReadOnly Property Q(key As String) As A
        Get
            Return _Q(key)
        End Get
    End Property
    Sub New(key As String, value As A)
        _Q = New Dictionary(Of String, A)
        _Q(key) = value
    End Sub
End Class
Class C
    Shared Sub Main()
        Dim value As A = New A()
        value("B") = "value"
        Dim x As B = New B("A", value)
        Console.WriteLine("Before: {0}", x!A!B)
        M(x!A!B)
        Console.WriteLine("After: {0}", x!A!B)
    End Sub
    Shared Sub M(ByRef s As String)
        s = s.ToUpper()
    End Sub
End Class
    </file>
</compilation>,
            expectedOutput:=<![CDATA[
Before: value
After: VALUE
]]>)
        End Sub

        <Fact>
        Public Sub DictionaryMemberAccessWithTypeCharacter()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Option Infer Off
Imports System.Collections.Generic
Module M
    Sub Main()
        M()
    End Sub
    Sub M()
        Dim d As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)()
        d!x% = 1
        d!y = 2
        Dim x = d!x
        Dim y = d!y%
        System.Console.WriteLine("{0}, {1}", x, y)
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:="1, 2")
            compilationVerifier.VerifyIL("M.M",
            <![CDATA[
{
  // Code size       85 (0x55)
  .maxstack  4
  .locals init (Object V_0, //x
  Object V_1) //y
  IL_0000:  newobj     "Sub System.Collections.Generic.Dictionary(Of String, Integer)..ctor()"
  IL_0005:  dup
  IL_0006:  ldstr      "x"
  IL_000b:  ldc.i4.1
  IL_000c:  callvirt   "Sub System.Collections.Generic.Dictionary(Of String, Integer).set_Item(String, Integer)"
  IL_0011:  dup
  IL_0012:  ldstr      "y"
  IL_0017:  ldc.i4.2
  IL_0018:  callvirt   "Sub System.Collections.Generic.Dictionary(Of String, Integer).set_Item(String, Integer)"
  IL_001d:  dup
  IL_001e:  ldstr      "x"
  IL_0023:  callvirt   "Function System.Collections.Generic.Dictionary(Of String, Integer).get_Item(String) As Integer"
  IL_0028:  box        "Integer"
  IL_002d:  stloc.0
  IL_002e:  ldstr      "y"
  IL_0033:  callvirt   "Function System.Collections.Generic.Dictionary(Of String, Integer).get_Item(String) As Integer"
  IL_0038:  box        "Integer"
  IL_003d:  stloc.1
  IL_003e:  ldstr      "{0}, {1}"
  IL_0043:  ldloc.0
  IL_0044:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0049:  ldloc.1
  IL_004a:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_004f:  call       "Sub System.Console.WriteLine(String, Object, Object)"
  IL_0054:  ret
}
]]>)
        End Sub

#End Region
#Region "Auto Properties"

        <Fact>
        Public Sub AutoPropertyInitializer()
            Dim source =
        <compilation>
            <file name="c.vb">
Module M
    Sub Main()
        ' touch something shared from A or derived classes to force the shared constructors always to run.
        ' otherwise beforefieldinit causes different results while running under the debugger.
        Dim ignored = A.Dummy

        M(New ADerived())
        M(New ADerived())
        M(New BDerived())
        M(New BDerived())
    End Sub
    Sub M(ByVal o)
    End Sub
End Module
Class ABase
    Sub New()
        C.Message("ABase..ctor")
    End Sub
End Class
Class A
    Inherits ABase
    Shared Property P = New C("Shared Property A.P")
    Public Shared F = New C("Shared Field A.F")
    Public G = New C("Instance Field A.G")
    Overridable Property Q = New C("Instance Property A.Q")
    Public Shared Dummy As Integer = 23 
End Class
Class ADerived
    Inherits A
    Public Overrides Property Q As Object
        Get
            Return Nothing
        End Get
        Set(ByVal value As Object)
            C.Message("A.Q.set")
        End Set
    End Property
End Class
Class BBase
    Sub New()
        C.Message("BBase..ctor")
    End Sub
End Class
Class B
    Inherits BBase
    Public Shared F = New C("Shared Field B.F")
    Shared Property P = New C("Shared Property B.P")
    Overridable Property Q = New C("Instance Property B.Q")
    Public G = New C("Instance Field B.G")
    Shared Sub New()
        C.Message("B..cctor")
    End Sub
    Sub New()
        C.Message("B..ctor")
    End Sub
End Class
Class BDerived
    Inherits B
    Public Overrides Property Q As Object
        Get
            Return Nothing
        End Get
        Set(ByVal value As Object)
            C.Message("B.Q.set")
        End Set
    End Property
End Class
Class C
    Public Sub New(ByVal s As String)
        Message(s)
    End Sub
    Shared Sub Message(ByVal s As String)
        System.Console.WriteLine("{0}", s)
    End Sub
End Class
    </file>
        </compilation>

            ' if a debugger is attached, the beforefieldinit attribute is ignored and the shared constructors
            ' are executed although no shared fields have been accessed. This causes the test to fail under a debugger.
            ' Therefore I've added an access to a shared field of class A, because accessing a field of type ADerived 
            ' would not trigger the base type initializers.

            Dim compilationVerifier = CompileAndVerify(source,
                                                           expectedOutput:=<![CDATA[
Shared Property A.P
Shared Field A.F
ABase..ctor
Instance Field A.G
Instance Property A.Q
A.Q.set
ABase..ctor
Instance Field A.G
Instance Property A.Q
A.Q.set
Shared Field B.F
Shared Property B.P
B..cctor
BBase..ctor
Instance Property B.Q
B.Q.set
Instance Field B.G
B..ctor
BBase..ctor
Instance Property B.Q
B.Q.set
Instance Field B.G
B..ctor
]]>)

            compilationVerifier.VerifyIL("A..ctor",
            <![CDATA[
{
// Code size       39 (0x27)
.maxstack  2
IL_0000:  ldarg.0
IL_0001:  call       "Sub ABase..ctor()"
IL_0006:  ldarg.0
IL_0007:  ldstr      "Instance Field A.G"
IL_000c:  newobj     "Sub C..ctor(String)"
IL_0011:  stfld      "A.G As Object"
IL_0016:  ldarg.0
IL_0017:  ldstr      "Instance Property A.Q"
IL_001c:  newobj     "Sub C..ctor(String)"
IL_0021:  callvirt   "Sub A.set_Q(Object)"
IL_0026:  ret
}
]]>)

            compilationVerifier.VerifyIL("B..ctor",
            <![CDATA[
{
// Code size       49 (0x31)
.maxstack  2
IL_0000:  ldarg.0
IL_0001:  call       "Sub BBase..ctor()"
IL_0006:  ldarg.0
IL_0007:  ldstr      "Instance Property B.Q"
IL_000c:  newobj     "Sub C..ctor(String)"
IL_0011:  callvirt   "Sub B.set_Q(Object)"
IL_0016:  ldarg.0
IL_0017:  ldstr      "Instance Field B.G"
IL_001c:  newobj     "Sub C..ctor(String)"
IL_0021:  stfld      "B.G As Object"
IL_0026:  ldstr      "B..ctor"
IL_002b:  call       "Sub C.Message(String)"
IL_0030:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub OverrideAutoPropertyInitializer()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Module M1
    Sub Main()
        Dim c As C = New C()
        c.P = 3
    End Sub
End Module
Class A
    Overridable Property P As Integer = 1
End Class
Class B
    Inherits A
    Overrides Property P As Integer = 2
End Class
Class C
    Inherits B
    Overrides Property P As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
            System.Console.Write("{0}, ", value)
        End Set
    End Property
End Class
    </file>
</compilation>, expectedOutput:="1, 2, 3, ")

            compilationVerifier.VerifyIL("B..ctor",
            <![CDATA[
{
   // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub A..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldc.i4.2
  IL_0008:  callvirt   "Sub B.set_P(Integer)"
  IL_000d:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub AutoProperties()
            Dim validator = Function(isFromSource As Boolean) _
                                Sub([module] As ModuleSymbol)
                                    Dim type = [module].GlobalNamespace.GetTypeMembers("C").Single()
                                    VerifyAutoProperty(type, "P", Accessibility.Protected, isFromSource)
                                    VerifyAutoProperty(type, "Q", Accessibility.Friend, isFromSource)
                                End Sub
            CompileAndVerify(
<compilation>
    <file name="c.vb">
Class C
    Protected Property P
    Friend Shared Property Q
End Class
    </file>
</compilation>,
                sourceSymbolValidator:=validator(True),
                symbolValidator:=validator(False),
                options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))
        End Sub

        <Fact>
        Public Sub AutoPropertiesComplexTypeInitializer()
            CompileAndVerify(
 <compilation>
     <file name="c.vb">
Module M1
    Sub Main()
        Dim c As C = New C()
        c.P2.P1 = 2
        System.Console.WriteLine(String.Join(",", c.P2.P1, String.Join(",", c.P3)))
    End Sub
End Module
Class A
    Property P1 As Integer = 1
    Overridable Property P2 As A
    Property P3() As String() = New String() {"A", "B", "C"}
End Class
Class C
    Inherits A
    Overrides Property P2 As New A()
End Class
    </file>
 </compilation>, expectedOutput:="2,A,B,C")
        End Sub

        <Fact>
        Public Sub AutoPropertiesAsNewInitializer()

            Dim source =
<compilation>
    <file name="c.vb">
    imports system

    Class C1
        Public field As Integer

        Public Sub New(p As Integer)
            field = p
        End Sub
    End Class

    Class C2
        Public Property P1 As New C1(23)

        Public Shared Sub Main()
            Dim c as new C2()
            console.writeline(c.P1.field)
        End Sub
    End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
23
]]>)
        End Sub

        <WorkItem(542749, "DevDiv")>
        <Fact>
        Public Sub ValueInAutoAndDefaultProperties()

            Dim source =
<compilation>
    <file name="pp.vb">
Imports system

Class C
    Public Property AP As String

    Structure S
        Default Public Property DefP(p As String) As String
            Get
                Return p
            End Get
            Set
            End Set
        End Property
    End Structure
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim type01 = compilation.SourceModule.GlobalNamespace.GetTypeMembers("C").Single()
            Dim type02 = type01.GetTypeMembers("S").Single()

            Dim autoProp = DirectCast(type01.GetMembers("AP").SingleOrDefault(), PropertySymbol)
            Dim deftProp = DirectCast(type02.GetMembers("DefP").SingleOrDefault(), PropertySymbol)
            ' All accessor's parameters should be Synthesized if it's NOT in source
            Assert.NotNull(autoProp.SetMethod)
            For Each p In autoProp.SetMethod.Parameters
                Assert.True(p.IsImplicitlyDeclared)
                Assert.True(p.IsFromCompilation(compilation))
            Next
            Assert.NotNull(deftProp.SetMethod)
            For Each p In deftProp.SetMethod.Parameters
                Assert.True(p.IsImplicitlyDeclared)
                If p.Name.ToLower() <> "value" Then
                    ' accessor's parameter should point to same location as the parameter's of parent property
                    Assert.False(p.Locations.IsEmpty)
                End If
            Next
        End Sub

        <Fact>
        Public Sub ReadOnlyAutoProperties()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Imports System.Console

Module Program
    Sub Main()
        Dim collection As New MyCollection()
        Write(collection.Capacity)
        Write(collection.Capacity = MyCollection.DefaultCapacity)

        collection = New MyCollection(15)
        Write(collection.Capacity)
        collection.DoubleCapacity()
        Write(collection.Capacity)        
    End Sub
End Module

Class MyCollection
    
    Public Shared ReadOnly Property DefaultCapacity As Integer = 5
    
    Public ReadOnly Property Capacity As Integer = DefaultCapacity

    Public Sub New()
        Write(_Capacity)
    End Sub

    Public Sub New(capacity As Integer)
        Write(_Capacity)
        _Capacity = capacity
    End Sub

    Public Sub DoubleCapacity()
        _Capacity *= 2
    End Sub

End Class
    </file>
</compilation>,
sourceSymbolValidator:=Sub(m As ModuleSymbol)
                           Dim myCollectionType = m.GlobalNamespace.GetTypeMember("MyCollection")
                           Dim defaultCapacityProperty = CType(myCollectionType.GetMember("DefaultCapacity"), PropertySymbol)
                           Assert.True(defaultCapacityProperty.IsReadOnly)
                           Assert.False(defaultCapacityProperty.IsWriteOnly)
                           Assert.True(defaultCapacityProperty.IsShared)
                           Assert.NotNull(defaultCapacityProperty.GetMethod)
                           Assert.Null(defaultCapacityProperty.SetMethod)

                           Dim backingField = CType(myCollectionType.GetMember("_DefaultCapacity"), FieldSymbol)
                           Assert.NotNull(defaultCapacityProperty.AssociatedField)
                           Assert.Same(defaultCapacityProperty.AssociatedField, backingField)
                       End Sub,
expectedOutput:="55True51530")

            verifier.VerifyIL("MyCollection..cctor()",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.5
  IL_0001:  stsfld     "MyCollection._DefaultCapacity As Integer"
  IL_0006:  ret
}
]]>)

            verifier.VerifyIL("MyCollection..ctor()",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  call       "Function MyCollection.get_DefaultCapacity() As Integer"
  IL_000c:  stfld      "MyCollection._Capacity As Integer"
  IL_0011:  ldarg.0
  IL_0012:  ldfld      "MyCollection._Capacity As Integer"
  IL_0017:  call       "Sub System.Console.Write(Integer)"
  IL_001c:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub WriteOnlyAutoProperties()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="c.vb">
Imports System.Console

Module Program
    Sub Main()
        Dim collection As New MyCollection()
        collection.Capacity = 10
        collection.WriteCapacity()

        MyCollection.DefaultCapacity = 15
        MyCollection.WriteDefaultCapacity()
    End Sub
End Module

Class MyCollection
    
    Public Shared WriteOnly Property DefaultCapacity As Integer = 5
    
    Public WriteOnly Property Capacity As Integer = _DefaultCapacity

    Shared Sub New()
        WriteDefaultCapacity()
    End Sub

    Public Sub New()
        WriteCapacity()
    End Sub

    Public Sub WriteCapacity()
        Write(_Capacity)
        Write("_")
    End Sub

    Public Shared Sub WriteDefaultCapacity()
        Write(_DefaultCapacity)
        Write("_")
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(Compilation,
    <expected>
BC37243: Auto-implemented properties cannot be WriteOnly.
    Public Shared WriteOnly Property DefaultCapacity As Integer = 5
                                     ~~~~~~~~~~~~~~~
BC37243: Auto-implemented properties cannot be WriteOnly.
    Public WriteOnly Property Capacity As Integer = _DefaultCapacity
                              ~~~~~~~~
    </expected>)

        End Sub

        <Fact>
        Public Sub ReadOnlyAutoPropertiesAndImplements()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Imports System.Console

Module Program
    Sub Main()
        Dim test As New Test()
        Dim i As I1 = test

        Write(i.P1)
        Write("_")
    End Sub
End Module

Interface I1
    ReadOnly Property P1 As Integer
End Interface

Class Test
    Implements I1

    Sub New()
        _P1 = 5
    End Sub

    Public ReadOnly Property P1 As Integer Implements I1.P1
End Class
    </file>
</compilation>,
expectedOutput:="5_")

        End Sub

        <Fact>
        Public Sub ReadOnlyWriteOnlyAutoPropertiesAndImplementsMismatch()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="c.vb">
Module Program
    Sub Main()
    End Sub
End Module

Interface I1
    ReadOnly Property P1 As Integer
    WriteOnly Property P2 As Integer
    Property P3 As Integer
    Property P4 As Integer
End Interface

Class Test
    Implements I1

    Public WriteOnly Property P1 As Integer Implements I1.P1

    Public ReadOnly Property P2 As Integer Implements I1.P2

    Public WriteOnly Property P3 As Integer Implements I1.P3

    Public ReadOnly Property P4 As Integer Implements I1.P4
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC37243: Auto-implemented properties cannot be WriteOnly.
    Public WriteOnly Property P1 As Integer Implements I1.P1
                              ~~
BC31444: 'ReadOnly Property P1 As Integer' cannot be implemented by a WriteOnly property.
    Public WriteOnly Property P1 As Integer Implements I1.P1
                                                       ~~~~~
BC31444: 'WriteOnly Property P2 As Integer' cannot be implemented by a ReadOnly property.
    Public ReadOnly Property P2 As Integer Implements I1.P2
                                                      ~~~~~
BC37243: Auto-implemented properties cannot be WriteOnly.
    Public WriteOnly Property P3 As Integer Implements I1.P3
                              ~~
BC31444: 'Property P3 As Integer' cannot be implemented by a WriteOnly property.
    Public WriteOnly Property P3 As Integer Implements I1.P3
                                                       ~~~~~
BC31444: 'Property P4 As Integer' cannot be implemented by a ReadOnly property.
    Public ReadOnly Property P4 As Integer Implements I1.P4
                                                      ~~~~~
    </expected>)
        End Sub

        <Fact>
        Public Sub ReadOnlyAutoPropertiesAndOverrides()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Imports System.Console

Module Program
    Sub Main()
        Dim test As New Test()
        Dim i As I1 = test

        Write(i.P1)
        Write("_")
    End Sub
End Module

Class I1
    Overridable ReadOnly Property P1 As Integer
End Class

Class Test
    Inherits I1

    Sub New()
        _P1 = 5
    End Sub

    Public Overrides ReadOnly Property P1 As Integer
End Class
    </file>
</compilation>,
expectedOutput:="5_")

        End Sub

        <Fact>
        Public Sub ReadOnlyWriteOnlyAutoPropertiesAndOverridesMismatch()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="c.vb">
Module Program
    Sub Main()
    End Sub
End Module

Class I1
    Overridable WriteOnly Property P2 As Integer
        Set
        end set
    end property
    Overridable Property P4 As Integer
End Class

Class Test
    Inherits I1

    Public Overrides ReadOnly Property P2 As Integer
    Public Overrides ReadOnly Property P4 As Integer
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30362: 'Public Overrides ReadOnly Property P2 As Integer' cannot override 'Public Overridable WriteOnly Property P2 As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides ReadOnly Property P2 As Integer
                                       ~~
BC30362: 'Public Overrides ReadOnly Property P4 As Integer' cannot override 'Public Overridable Property P4 As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides ReadOnly Property P4 As Integer
                                       ~~
    </expected>)
        End Sub

        <Fact>
        Public Sub ReadOnlyAutoPropertiesAndIterator()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="c.vb">
Module Program
    Sub Main()
    End Sub
End Module

Class Test
    Iterator ReadOnly Property P1 As System.Collections.Generic.IEnumerable(Of Integer)
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30025: Property missing 'End Property'.
    Iterator ReadOnly Property P1 As System.Collections.Generic.IEnumerable(Of Integer)
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30126: 'ReadOnly' property must provide a 'Get'.
    Iterator ReadOnly Property P1 As System.Collections.Generic.IEnumerable(Of Integer)
                               ~~
    </expected>)
        End Sub

        <Fact>
        Public Sub WriteOnlyAutoPropertiesAndIterator()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="c.vb">
Module Program
    Sub Main()
    End Sub
End Module

Class Test
    Iterator WriteOnly Property P2 As System.Collections.Generic.IEnumerable(Of Integer)
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30025: Property missing 'End Property'.
    Iterator WriteOnly Property P2 As System.Collections.Generic.IEnumerable(Of Integer)
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31408: 'Iterator' and 'WriteOnly' cannot be combined.
    Iterator WriteOnly Property P2 As System.Collections.Generic.IEnumerable(Of Integer)
             ~~~~~~~~~
BC30124: Property without a 'ReadOnly' or 'WriteOnly' specifier must provide both a 'Get' and a 'Set'.
    Iterator WriteOnly Property P2 As System.Collections.Generic.IEnumerable(Of Integer)
                                ~~
    </expected>)
        End Sub

#End Region
#Region "Default Properties"

        <Fact>
        Public Sub DefaultProperties()
            Dim source =
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
        Dim d As New DefaultProps
        d(0) = 10
        d("1") = 20
        d.Items("2") = 30
        d.items(3) = 40
        System.Console.WriteLine(String.Join(",", d._items))
    End Sub
End Module
Public Class DefaultProps
    Public _items As Integer()
    Public Sub New()
        _items = New Integer(3) {}
    End Sub
    Default Property Items(index As Integer) As Integer
        Get
            Return _items(index)
        End Get
        Set(value As Integer)
            _items(index) = value
        End Set
    End Property
    Default Property items(index As String) As Integer
        Get
            Return _items(index)
        End Get
        Set(value As Integer)
            _items(index) = value
        End Set
    End Property
End Class
    </file>
</compilation>
            Dim expectedMainILSource = <![CDATA[
{
// Code size       72 (0x48)
.maxstack  3
.locals init (DefaultProps V_0) //d
IL_0000:  newobj     "Sub DefaultProps..ctor()"
IL_0005:  stloc.0
IL_0006:  ldloc.0
IL_0007:  ldc.i4.0
IL_0008:  ldc.i4.s   10
IL_000a:  callvirt   "Sub DefaultProps.set_Items(Integer, Integer)"
IL_000f:  ldloc.0
IL_0010:  ldstr      "1"
IL_0015:  ldc.i4.s   20
IL_0017:  callvirt   "Sub DefaultProps.set_items(String, Integer)"
IL_001c:  ldloc.0
IL_001d:  ldstr      "2"
IL_0022:  ldc.i4.s   30
IL_0024:  callvirt   "Sub DefaultProps.set_items(String, Integer)"
IL_0029:  ldloc.0
IL_002a:  ldc.i4.3
IL_002b:  ldc.i4.s   40
IL_002d:  callvirt   "Sub DefaultProps.set_Items(Integer, Integer)"
IL_0032:  ldstr      ","
IL_0037:  ldloc.0
IL_0038:  ldfld      "DefaultProps._items As Integer()"
IL_003d:  call       "Function String.Join(Of Integer)(String, System.Collections.Generic.IEnumerable(Of Integer)) As String"
IL_0042:  call       "Sub System.Console.WriteLine(String)"
IL_0047:  ret
}
]]>
            Dim validator = Sub([module] As ModuleSymbol)
                                Dim type = [module].GlobalNamespace.GetTypeMembers("DefaultProps").Single()
                                Dim properties = type.GetMembers("Items").OfType(Of PropertySymbol)()
                                Assert.True(properties.All(Function(prop) prop.IsDefault), "Not All default properties had PropertySymbol.IsDefault=true")
                            End Sub

            Dim compilationVerifier = CompileAndVerify(source, sourceSymbolValidator:=validator, symbolValidator:=validator, expectedOutput:="10,20,30,40")

            compilationVerifier.VerifyIL("Program.Main", expectedMainILSource)
        End Sub

        <Fact>
        Public Sub DefaultPropertySameBaseAndDerived()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
' Base and derived properties marked Default.
Class A1
    Default Public ReadOnly Property P(i As Integer)
        Get
            Return Nothing
        End Get
    End Property
End Class
Class B1
    Inherits A1
    Default Public Overloads WriteOnly Property P(x As Integer, y As Integer)
        Set(value)
        End Set
    End Property
End Class
Class C1
    Inherits B1
End Class
' Derived property marked Default, base property not.
Class A2
    Public ReadOnly Property P(i As Integer)
        Get
            Return Nothing
        End Get
    End Property
End Class
Class B2
    Inherits A2
    Default Public Overloads WriteOnly Property P(x As Integer, y As Integer)
        Set(value)
        End Set
    End Property
End Class
Class C2
    Inherits B2
End Class
' Base property marked Default, derived property not.
Class A3
    Default Public ReadOnly Property P(i As Integer)
        Get
            Return Nothing
        End Get
    End Property
End Class
Class B3
    Inherits A3
    Public Overloads WriteOnly Property P(x As Integer, y As Integer)
        Set(value)
        End Set
    End Property
End Class
Class C3
    Inherits B3
End Class
Module M
    Sub M(a As A1, b As B1, c As C1)
        b.P(1, 2) = a.P(3)
        b(1, 2) = a(3)
        c.P(1, 2) = c.P(3)
        c(1, 2) = c(3)
    End Sub
    Sub M(a As A2, b As B2, c As C2)
        b.P(4, 5) = a.P(6)
        b(4, 5) = a(6)
        c.P(4, 5) = c.P(6)
        c(4, 5) = c(6)
    End Sub
    Sub M(a As A3, b As B3, c As C3)
        b.P(7, 8) = a.P(9)
        b(7, 8) = a(9)
        c.P(7, 8) = c.P(9)
        c(7, 8) = c(9)
    End Sub
End Module
        </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30367: Class 'A2' cannot be indexed because it has no default property.
        b(4, 5) = a(6)
                  ~
BC30057: Too many arguments to 'Public ReadOnly Default Property P(i As Integer) As Object'.
        b(7, 8) = a(9)
             ~
BC30057: Too many arguments to 'Public ReadOnly Default Property P(i As Integer) As Object'.
        c(7, 8) = c(9)
             ~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultPropertyDifferentBaseAndDerived()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Imports System
' Default property "P"
Class A
    Default Public ReadOnly Property P(i As Integer)
        Get
            Console.WriteLine("A.P: {0}", i)
            Return Nothing
        End Get
    End Property
    Public ReadOnly Property Q(i As Integer)
        Get
            Console.WriteLine("A.Q: {0}", i)
            Return Nothing
        End Get
    End Property
    Public ReadOnly Property R(x As Integer, y As Integer)
        Get
            Console.WriteLine("A.R: {0}, {1}", x, y)
            Return Nothing
        End Get
    End Property
End Class
' Default property "Q"
Class B
    Inherits A
    Public Overloads ReadOnly Property P(i As Integer)
        Get
            Console.WriteLine("B.P: {0}", i)
            Return Nothing
        End Get
    End Property
    Default Public Overloads ReadOnly Property Q(i As Integer)
        Get
            Console.WriteLine("B.Q: {0}", i)
            Return Nothing
        End Get
    End Property
End Class
' Default property "R"
Class C
    Inherits B
    Default Public Overloads ReadOnly Property R(i As Integer)
        Get
            Console.WriteLine("C.R: {0}", i)
            Return Nothing
        End Get
    End Property
End Class
' No default property
Class D
    Inherits B
    Public Overloads ReadOnly Property P(i As Integer)
        Get
            Console.WriteLine("C.P: {0}", i)
            Return Nothing
        End Get
    End Property
    Public Overloads ReadOnly Property Q(i As Integer)
        Get
            Console.WriteLine("C.Q: {0}", i)
            Return Nothing
        End Get
    End Property
End Class
Module M
    Sub M(x As C, y As D)
        Dim value = x(1)
        value = x(2, 3)
        value = y(4)
        value = DirectCast(y, B)(5)
        value = DirectCast(y, A)(6)
    End Sub
    Sub Main()
        M(New C(), New D())
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:=<![CDATA[
C.R: 1
A.R: 2, 3
B.Q: 4
B.Q: 5
A.P: 6
]]>)
        End Sub

        <Fact>
        Public Sub DefaultPropertyGroupError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Class C
    Default Protected Property P(o As Object)
        Get
            Return Nothing
        End Get
        Set(value)
        End Set
    End Property
End Class
Module M
    Function F(o As C)
        Return o(Nothing)
    End Function
End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30389: 'C.P(o As Object)' is not accessible in this context because it is 'Protected'.
        Return o(Nothing)
               ~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultMembersFromMetadata()
            Dim customIL = <![CDATA[
// DefaultMember names field
.class public DefaultField
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .field public object F
}
// DefaultMember names method
.class public DefaultMethod
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method public hidebysig instance object F(object o) { ldnull ret }
}
// DefaultMember names property
.class public DefaultProperty
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method public hidebysig instance object get_F(object o) { ldnull ret }
    .property object F(object o) { .get instance object DefaultProperty::get_F(object o) }
}
// DefaultMember names static property
.class public DefaultStaticProperty
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method public static hidebysig object get_F(object o) { ldnull ret }
    .property object F(object o) { .get object DefaultStaticProperty::get_F(object o) }
}
// Property but no DefaultMember
.class public PropertyNoDefault
{
    .method public hidebysig instance object get_F(object o) { ldnull ret }
    .property object F(object o) { .get instance object PropertyNoDefault::get_F(object o) }
}
// DefaultMember names property with different case
.class public DifferentCase
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('DefaultName')}
    .method public hidebysig instance object get(object o) { ldnull ret }
    .property object defaultNAME(object o) { .get instance object DifferentCase::get(object o) }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Module M
    Sub M(
        x1 As DefaultField,
        x2 As DefaultMethod,
        x3 As DefaultProperty,
        x4 As DefaultStaticProperty,
        x5 As PropertyNoDefault,
        x6 As DifferentCase)
        Dim value As Object
        value = x1.F
        value = x1()
        value = x2.F(value)
        value = x2(value)
        value = x3.F(value)
        value = x3(value)
        value = x4.F(value)
        value = x4(value)
        value = x5.F(value)
        value = x5(value)
        value = x6.DefaultName(value)
        value = x6(value)
    End Sub
End Module
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL.Value, includeVbRuntime:=True)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30367: Class 'DefaultField' cannot be indexed because it has no default property.
        value = x1()
                ~~
BC30367: Class 'DefaultMethod' cannot be indexed because it has no default property.
        value = x2(value)
                ~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        value = x4.F(value)
                ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        value = x4(value)
                ~~
BC30367: Class 'PropertyNoDefault' cannot be indexed because it has no default property.
        value = x5(value)
                ~~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultAmbiguousMembersFromMetadata()
            Dim customIL = <![CDATA[
// DefaultMember names field and property
.class public DefaultFieldAndProperty
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method public hidebysig instance object get_F(object o) { ldnull ret }
    .property object F(object o) { .get instance object DefaultFieldAndProperty::get_F(object o) }
	.field public object F
}
// DefaultMember names method and property
.class public DefaultMethodAndProperty
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method public hidebysig instance object F(object x, object y) { ldnull ret }
    .method public hidebysig instance object get_F(object o) { ldnull ret }
    .property object F(object o) { .get instance object DefaultMethodAndProperty::get_F(object o) }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Module M
    Sub M(
        a As DefaultFieldAndProperty,
        b As DefaultMethodAndProperty)
        Dim value As Object
        value = a.F
        value = a(value)
        value = b.F(value)
        value = b(value)
    End Sub
End Module
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL.Value, includeVbRuntime:=True)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31429: 'F' is ambiguous because multiple kinds of members with this name exist in class 'DefaultFieldAndProperty'.
        value = a.F
                ~~~
BC31429: 'F' is ambiguous because multiple kinds of members with this name exist in class 'DefaultFieldAndProperty'.
        value = a(value)
                ~
BC31429: 'F' is ambiguous because multiple kinds of members with this name exist in class 'DefaultMethodAndProperty'.
        value = b.F(value)
                ~~~
BC31429: 'F' is ambiguous because multiple kinds of members with this name exist in class 'DefaultMethodAndProperty'.
        value = b(value)
                ~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultPropertiesFromMetadata()
            Dim customIL = <![CDATA[
// DefaultMember names property
.class public DefaultProperty
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method public hidebysig instance object get_F(object o) { ldnull ret }
    .property object F(object o) { .get instance object DefaultProperty::get_F(object o) }
}
// Property but no DefaultMember
.class public PropertyNoDefault
{
    .method public hidebysig instance object get_F(object o) { ldnull ret }
    .property object F(object o) { .get instance object PropertyNoDefault::get_F(object o) }
}
// DefaultMember names property in base
.class public DefaultBaseProperty extends PropertyNoDefault
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
}
// DefaultMember names field in this class and property in base
.class public DefaultFieldAndBaseProperty extends PropertyNoDefault
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .field public object F
}
// DefaultMember names property (with no args) in this class and property (with args) in base
.class public DefaultPropertyAndBaseProperty extends PropertyNoDefault
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method public hidebysig instance object get_F() { ldnull ret }
    .property object F() { .get instance object DefaultPropertyAndBaseProperty::get_F() }
}
// DefaultMember names assembly property in this class and public property in base
.class public DefaultAssemblyPropertyAndBaseProperty extends PropertyNoDefault
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method assembly hidebysig instance object get_F() { ldnull ret }
    .property object F() { .get instance object DefaultAssemblyPropertyAndBaseProperty::get_F() }
}
// DefaultMember names no fields in this class while base has different DefaultMember
.class public DifferentDefaultBaseProperty extends DefaultProperty
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('G')}
}
// DefaultMember names property in this class while base has different DefaultMember
.class public DifferentDefaultPropertyAndBaseProperty extends DefaultProperty
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('G')}
    .method public hidebysig instance object get_G() { ldnull ret }
    .property object G() { .get instance object DifferentDefaultPropertyAndBaseProperty::get_G() }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Option Strict On
Module M
    Sub M(
        x1 As DefaultBaseProperty,
        x2 As DefaultFieldAndBaseProperty,
        x3 As DefaultPropertyAndBaseProperty,
        x4 As DefaultAssemblyPropertyAndBaseProperty,
        x5 As DifferentDefaultBaseProperty,
        x6 As DifferentDefaultPropertyAndBaseProperty)
        Dim value As Object = Nothing
        value = x1.F(value)
        value = x1(value)
        value = x2()
        value = x3.F(value)
        value = x3(value)
        value = x3()
        value = x4.F(value)
        value = x4(value)
        value = x4()
        value = x5.F(value)
        value = x5(value)
        value = x6.F(value)
        value = x6(value)
        value = x6.G()
        value = x6()
    End Sub
End Module
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL.Value, includeVbRuntime:=True)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30367: Class 'DefaultBaseProperty' cannot be indexed because it has no default property.
        value = x1(value)
                ~~
BC30367: Class 'DefaultFieldAndBaseProperty' cannot be indexed because it has no default property.
        value = x2()
                ~~
BC30367: Class 'DefaultAssemblyPropertyAndBaseProperty' cannot be indexed because it has no default property.
        value = x4(value)
                ~~
BC30367: Class 'DefaultAssemblyPropertyAndBaseProperty' cannot be indexed because it has no default property.
        value = x4()
                ~~
BC30574: Option Strict On disallows late binding.
        value = x6(value)
                ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub DefaultPropertiesFromMetadata2()
            Dim customIL = <![CDATA[
.class public auto ansi Class1
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 54 65 73 74 00 00 )                      // ...Test..
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Class1::.ctor

  .method public specialname static int32 
          get_Test(int32 x) cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Class1::get_Test

  .method public specialname static int32 
          get_Test(int32 x,
                   int32 y) cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.2
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Class1::get_Test

  .method public specialname instance int32 
          get_Test(int64 x,
                   int32 y) cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.3
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Class1::get_Test

  .property int32 Test(int32)
  {
    .get int32 Class1::get_Test(int32)
  } // end of property Class1::Test
  .property int32 Test(int32,
                       int32)
  {
    .get int32 Class1::get_Test(int32,
                                int32)
  } // end of property Class1::Test
  .property instance int32 Test(int64,
                                int32)
  {
    .get instance int32 Class1::get_Test(int64,
                                         int32)
  } // end of property Class1::Test
} // end of class Class1
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Module M
    Sub Main()
        Dim x As New Class1()
        System.Console.WriteLine(x(1, 2))
        System.Console.WriteLine(x(2))
        System.Console.WriteLine(x(3L, 2))
    End Sub
End Module
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(x(1, 2))
                                 ~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(x(2))
                                 ~
</expected>)

            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
2
1
3
]]>)

        End Sub

        <Fact>
        Public Sub DefaultPropertiesDerivedTypes()
            Dim customIL = <![CDATA[
// Base class with default property
.class public A
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method public hidebysig instance object get_F(object o) { ldnull ret }
    .property object F(object o) { .get instance object A::get_F(object o) }
}
// Derived class with property overload
.class public B1 extends A
{
    .method public hidebysig instance object get_F(object x, object y) { ldnull ret }
    .property object F(object x, object y) { .get instance object B1::get_F(object x, object y) }
}
// Derived class with different default property
.class public B2 extends A
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('G')}
    .method public hidebysig instance object get_G(object x, object y) { ldnull ret }
    .property object G(object x, object y) { .get instance object B2::get_G(object x, object y) }
}
// Derived class with different internal default property
.class public B3 extends A
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('G')}
    .method assembly hidebysig instance object get_G(object x, object y) { ldnull ret }
    .property object G(object x, object y) { .get instance object B3::get_G(object x, object y) }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Option Strict On
Module M
    Sub M(b1 As B1, b2 As B2, b3 As B3)
        Dim value As Object
        value = b1(1)
        value = b1(2, 3)
        value = b2(1)
        value = b2(2, 3)
        value = b3(1)
        value = b3(2, 3)
    End Sub
End Module
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL.Value, includeVbRuntime:=True)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Overloads ReadOnly Default Property F(o As Object) As Object'.
        value = b1(2, 3)
                      ~
BC30455: Argument not specified for parameter 'y' of 'Public Overloads ReadOnly Default Property G(x As Object, y As Object) As Object'.
        value = b2(1)
                ~~
BC30057: Too many arguments to 'Public Overloads ReadOnly Default Property F(o As Object) As Object'.
        value = b3(2, 3)
                      ~
</expected>)
        End Sub

        <WorkItem(529554, "DevDiv")>
        <Fact()>
        Public Sub DefaultPropertiesWithShadowingMethod()
            Dim customIL = <![CDATA[
// Base class with default property
.class public A
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method public hidebysig instance object get_F(object o) { ldnull ret }
    .property object F(object o) { .get instance object A::get_F(object o) }
}
// Derived class with method with same name
.class public B extends A
{
    .method public hidebysig instance object F(int32 x, int32 y) { ldnull ret }
}
// Derived class with default property overload
.class public C1 extends B
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method public hidebysig instance object get_F(object x, object y) { ldnull ret }
    .property object F(object x, object y) { .get instance object C1::get_F(object x, object y) }
}
// Derived class with internal default property overload
.class public C2 extends B
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method assembly hidebysig instance object get_F(object x, object y) { ldnull ret }
    .property object F(object x, object y) { .get instance object C2::get_F(object x, object y) }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Option Strict On
Module M
    Sub M(b As B, c1 As C1, c2 As C2)
        Dim value As Object
        value = b(1)
        value = b(2, 3)
        value = c1(1)
        value = c1(2, 3)
        value = c2(1)
        value = c2(2, 3)
    End Sub
End Module
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Overloads ReadOnly Default Property F(o As Object) As Object'.
        value = b(2, 3)
                     ~
BC30455: Argument not specified for parameter 'y' of 'Public Overloads ReadOnly Default Property F(x As Object, y As Object) As Object'.
        value = c1(1)
                ~~
BC30057: Too many arguments to 'Public Overloads ReadOnly Default Property F(o As Object) As Object'.
        value = c2(2, 3)
                      ~
</expected>)
        End Sub

        <WorkItem(529553, "DevDiv")>
        <Fact()>
        Public Sub DefaultPropertiesInterfacesWithShadowingMethod()
            Dim customIL = <![CDATA[
// Base class with default property
.class interface public IA
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method public hidebysig newslot abstract virtual instance object get_F(object o) { }
    .property object F(object o) { .get instance object IA::get_F(object o) }
}
// Derived class with method with same name
.class interface public IB implements IA
{
    .method public hidebysig newslot abstract virtual instance object F(int32 x, int32 y) { }
}
// Derived class with default property overload
.class interface public IC implements IB
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('F')}
    .method public hidebysig newslot abstract virtual instance object get_F(object x, object y) { }
    .property object F(object x, object y) { .get instance object IC::get_F(object x, object y) }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Option Strict On
Module M
    Sub M(b As IB, c As IC)
        Dim value As Object
        value = b(1)
        value = b(2, 3)
        value = c(1)
        value = c(2, 3)
    End Sub
End Module
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL.Value, includeVbRuntime:=True)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'ReadOnly Default Property F(o As Object) As Object'.
        value = b(2, 3)
                     ~
</expected>)
        End Sub

        ''' <summary>
        ''' Should only generate DefaultMemberAttribute if not specified explicitly.
        ''' </summary>
        <Fact()>
        Public Sub DefaultMemberAttribute()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Reflection
' No DefaultMemberAttribute.
Interface IA
    Default ReadOnly Property P(o As Object)
End Interface
' Expected DefaultMemberAttribute.
<DefaultMember("P")>
Interface IB
    Default ReadOnly Property P(o As Object)
End Interface
' Different DefaultMemberAttribute.
<DefaultMember("Q")>
Interface IC
    Default ReadOnly Property P(o As Object)
End Interface
' Nothing DefaultMemberAttribute value.
<DefaultMember(Nothing)>
Interface ID
    Default ReadOnly Property P(o As Object)
End Interface
' Empty DefaultMemberAttribute value.
<DefaultMember("")>
Interface IE
    Default ReadOnly Property P(o As Object)
End Interface
' Different case.
<DefaultMember("p")>
Interface [IF]
    Default ReadOnly Property P(o As Object)
End Interface
' Different case.
<DefaultMember("P")>
Interface IG
    Default ReadOnly Property p(o As Object)
End Interface
]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC32304: Conflict between the default property and the 'DefaultMemberAttribute' defined on 'IC'.
Interface IC
          ~~
BC32304: Conflict between the default property and the 'DefaultMemberAttribute' defined on 'ID'.
Interface ID
          ~~
BC32304: Conflict between the default property and the 'DefaultMemberAttribute' defined on 'IE'.
Interface IE
          ~~
]]></errors>)
            Dim globalNamespace = compilation.GlobalNamespace
            Dim type = globalNamespace.GetMember(Of NamedTypeSymbol)("IA")
            CheckDefaultMemberAttribute(compilation, type, "P", synthesized:=True)
            type = globalNamespace.GetMember(Of NamedTypeSymbol)("IB")
            CheckDefaultMemberAttribute(compilation, type, "P", synthesized:=False)
            type = globalNamespace.GetMember(Of NamedTypeSymbol)("IC")
            CheckDefaultMemberAttribute(compilation, type, "Q", synthesized:=False)
            type = globalNamespace.GetMember(Of NamedTypeSymbol)("ID")
            CheckDefaultMemberAttribute(compilation, type, Nothing, synthesized:=False)
            type = globalNamespace.GetMember(Of NamedTypeSymbol)("IE")
            CheckDefaultMemberAttribute(compilation, type, "", synthesized:=False)
            type = globalNamespace.GetMember(Of NamedTypeSymbol)("IF")
            CheckDefaultMemberAttribute(compilation, type, "p", synthesized:=False)
            type = globalNamespace.GetMember(Of NamedTypeSymbol)("IG")
            CheckDefaultMemberAttribute(compilation, type, "P", synthesized:=False)
        End Sub

        Private Sub CheckDefaultMemberAttribute(compilation As VisualBasicCompilation, type As NamedTypeSymbol, name As String, synthesized As Boolean)
            Dim attributes = type.GetAttributes()
            Dim synthesizedAttributes = type.GetSynthesizedAttributes()
            Dim attribute As VisualBasicAttributeData

            If synthesized Then
                Assert.Equal(attributes.Length, 0)
                Assert.Equal(synthesizedAttributes.Length, 1)
                attribute = synthesizedAttributes(0)
            Else
                Assert.Equal(attributes.Length, 1)
                Assert.Equal(synthesizedAttributes.Length, 0)
                attribute = attributes(0)
            End If

            Dim attributeType = attribute.AttributeConstructor.ContainingType
            Dim defaultMemberType = compilation.GetWellKnownType(WellKnownType.System_Reflection_DefaultMemberAttribute)
            Assert.Equal(attributeType, defaultMemberType)
            Assert.Equal(attribute.ConstructorArguments(0).Value, name)
        End Sub

        <Fact>
        Public Sub ParameterNames()
            Dim customIL = <![CDATA[
.class public C
{
    // Property with getter and setter.
    .method public instance object get_P(object g1) { ldnull ret }
    .method public instance void set_P(object s1, object s2) { ret }
    .property object P(object p1)
    {
        .get instance object C::get_P(object p2)
        .set instance void C::set_P(object p3, object p4)
    }
	// Property with getter only.
    .method public instance object get_Q(object g1) { ldnull ret }
    .property object Q(object p1)
    {
        .get instance object C::get_Q(object p2)
    }
	// Property with setter only.
    .method public instance void set_R(object s1, object s2, object s3) { ret }
    .property object R(object p1, object p2)
    {
        .set instance void C::set_R(object p3, object p4, object p5)
    }
	// Bogus property: getter with too many parameters.
    .method public instance object get_S(object g1, object g2) { ldnull ret }
    .method public instance void set_S(object s1, object s2) { ret }
    .property object S(object p1)
    {
        .get instance object C::get_S(object p2, object p3)
        .set instance void C::set_S(object p4, object p5)
    }
	// Bogus property: setter with too many parameters.
    .method public instance void set_T(object s1, object s2, object s3) { ret }
    .property object T(object p1)
    {
        .set instance void C::set_T(object p2, object p3, object p4)
    }
	// Bogus property: getter and setter with too many parameters.
    .method public instance object get_U(object g1, object g2) { ldnull ret }
    .method public instance void set_U(object s1, object s2, object s3) { ret }
    .property object U(object p1)
    {
        .get instance object C::get_U(object p2, object P3)
        .set instance void C::set_U(object p4, object p5, object t6)
    }
	// Bogus property: getter with too few parameters.
    .method public instance object get_V() { ldnull ret }
    .method public instance void set_V(object s1, object s2) { ret }
    .property object V(object p1)
    {
        .get instance object C::get_V()
        .set instance void C::set_V(object p4, object p5)
    }
	// Bogus property: setter with too few parameters.
    .method public instance void set_W(object s1, object s2) { ret }
    .property object W(object p1, object p2)
    {
        .set instance void C::set_W(object p3, object p4)
    }
	// Bogus property: getter and setter with too few parameters.
    .method public instance object get_X(object g1) { ldnull ret }
    .method public instance void set_X(object s1, object s2) { ret }
    .property object X(object p1, object p2)
    {
        .get instance object C::get_X(object P3)
        .set instance void C::set_X(object p4, object p5)
    }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Module M
    Sub M(c As C)
        Dim value = c.P()
        c.P() = value
        value = c.Q()
        c.R() = value
        c.S() = value
        value = c.S(value)
        c.T() = value
        value = c.U()
        c.U() = value
        value = c.V()
        c.V() = value
        c.W() = value
        value = c.X()
        c.X() = value
    End Sub
End Module
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL)
            Dim type = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            CheckParameterNames(type.GetMember(Of PropertySymbol)("P"), "Param")
            CheckParameterNames(type.GetMember(Of PropertySymbol)("Q"), "g1")
            CheckParameterNames(type.GetMember(Of PropertySymbol)("R"), "s1", "s2")
            CheckParameterNames(type.GetMember(Of PropertySymbol)("S"), "Param")
            CheckParameterNames(type.GetMember(Of PropertySymbol)("T"), "s1")
            CheckParameterNames(type.GetMember(Of PropertySymbol)("U"), "Param")
            CheckParameterNames(type.GetMember(Of PropertySymbol)("V"), "s1")
            CheckParameterNames(type.GetMember(Of PropertySymbol)("W"), "s1", "s2")
            CheckParameterNames(type.GetMember(Of PropertySymbol)("X"), "Param", "s2")

            ' TODO: There are two issues (differences from Dev10) with the following:
            ' 1) We're currently using the property parameter name rather than the
            ' accessor parameter name in "Argument not specified for parameter '...'".
            ' 2) Not all the bogus properties are supported.
#If False Then
            CompilationUtils.AssertTheseErrors(compilation,
<expected>
BC30455: Argument not specified for parameter 'g1' of 'Public Property P(g1 As Object) As Object'.
        Dim value = c.P()
                    ~~~~~
BC30455: Argument not specified for parameter 's1' of 'Public Property P(g1 As Object) As Object'.
        c.P() = value
        ~~~~~
BC30455: Argument not specified for parameter 'g1' of 'Public ReadOnly Property Q(g1 As Object) As Object'.
        value = c.Q()
                ~~~~~
BC30455: Argument not specified for parameter 's1' of 'Public WriteOnly Property R(s1 As Object) As Object'.
        c.R() = value
        ~~~~~
BC30455: Argument not specified for parameter 's1' of 'Public Property S(g1 As Object) As Object'.
        c.S() = value
        ~~~~~
BC30455: Argument not specified for parameter 'g2' of 'Public Property S(g1 As Object) As Object'.
        value = c.S(value)
                ~~~~~~~~~~
BC30455: Argument not specified for parameter 's1' of 'Public WriteOnly Property T(s1 As Object) As Object'.
        c.T() = value
        ~~~~~
BC30455: Argument not specified for parameter 's2' of 'Public WriteOnly Property T(s1 As Object) As Object'.
        c.T() = value
        ~~~~~
BC30455: Argument not specified for parameter 'g1' of 'Public Property U(g1 As Object) As Object'.
        value = c.U()
                ~~~~~
BC30455: Argument not specified for parameter 'g2' of 'Public Property U(g1 As Object) As Object'.
        value = c.U()
                ~~~~~
BC30455: Argument not specified for parameter 's1' of 'Public Property U(g1 As Object) As Object'.
        c.U() = value
        ~~~~~
BC30455: Argument not specified for parameter 's2' of 'Public Property U(g1 As Object) As Object'.
        c.U() = value
        ~~~~~
BC30455: Argument not specified for parameter 's1' of 'Public Property V(Param As Object) As Object'.
        c.V() = value
        ~~~~~
BC30455: Argument not specified for parameter 's1' of 'Public WriteOnly Property W(s1 As Object, s2 As Object) As Object'.
        c.W() = value
        ~~~~~
BC30455: Argument not specified for parameter 'g1' of 'Public Property X(g1 As Object, Param As Object) As Object'.
        value = c.X()
                ~~~~~
BC30455: Argument not specified for parameter 's1' of 'Public Property X(g1 As Object, Param As Object) As Object'.
        c.X() = value
        ~~~~~
</expected>)
#End If
        End Sub

        Private Shared Sub CheckParameterNames([property] As PropertySymbol, ParamArray names() As String)
            Dim parameters = [property].Parameters
            Assert.Equal(parameters.Length, names.Length)
            For i = 0 To names.Length - 1
                Assert.Equal(parameters(i).Name, names(i))
            Next
        End Sub

        ' Should be possible to invoke a default property with no
        ' parameters, even though the property declaration is an error.
        <Fact>
        Public Sub DefaultParameterlessProperty()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
Class C
    Default ReadOnly Property P()
        Get
            Return Nothing
        End Get
    End Property
    Shared Sub M(ByVal x As C)
        N(x.P()) ' No error
        N(x()) ' No error
    End Sub
    Shared Sub N(ByVal o)
    End Sub
End Class
    </file>
</compilation>)
            Dim expectedErrors = <errors>
BC31048: Properties with no required parameters cannot be declared 'Default'.
    Default ReadOnly Property P()
                              ~
                 </errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        ''' <summary>
        ''' If the default property is parameterless (supported for
        ''' types from metadata), bind the argument list to the
        ''' default property of the return type instead.
        ''' </summary>
        <WorkItem(531372, "DevDiv")>
        <Fact()>
        Public Sub DefaultPropertyOfParameterlessDefaultPropertyReturnType01()
            Dim source1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Imports System.Reflection
Public Class A
    Default ReadOnly Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class
<DefaultMember("Q")>
Public Class B
    ReadOnly Property Q As A
        Get
            Return Nothing
        End Get
    End Property
End Class
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib(source1)
            compilation1.AssertNoErrors()
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Sub M(o As B)
        Dim value As Object
        value = o()(Nothing)
        value = o(Nothing)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            ' DefaultMember attribute from source should be ignored.
            Dim reference1a = New VisualBasicCompilationReference(compilation1)
            Dim compilation2a = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1a})
            compilation2a.AssertTheseDiagnostics(
<expected>
BC30367: Class 'B' cannot be indexed because it has no default property.
        value = o()(Nothing)
                ~
BC30367: Class 'B' cannot be indexed because it has no default property.
        value = o(Nothing)
                ~
</expected>)
            ' DefaultMember attribute from metadata should be used.
            Dim reference1b = MetadataReference.CreateFromImage(compilation1.EmitToArray())
            Dim compilation2b = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1b})
            compilation2b.AssertNoErrors()
        End Sub

        ''' <summary>
        ''' WriteOnly default parameterless property.
        ''' </summary>
        <WorkItem(531372, "DevDiv")>
        <Fact()>
        Public Sub DefaultPropertyOfParameterlessDefaultPropertyReturnType02()
            Dim source1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Imports System.Reflection
Public Class A
    Default ReadOnly Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class
<DefaultMember("Q")>
Public Class B
    WriteOnly Property Q As A
        Set
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib(source1)
            compilation1.AssertNoErrors()
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Sub M(o As B)
        Dim value As Object
        value = o()(Nothing)
        value = o(Nothing)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim reference1 = MetadataReference.CreateFromImage(compilation1.EmitToArray())
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertTheseDiagnostics(
<expected>
BC30524: Property 'Q' is 'WriteOnly'.
        value = o()(Nothing)
                ~~~
BC30524: Property 'Q' is 'WriteOnly'.
        value = o(Nothing)
                ~
</expected>)
        End Sub

        <WorkItem(531372, "DevDiv")>
        <Fact()>
        Public Sub DefaultPropertyOfParameterlessDefaultPropertyReturnType03()
            Dim source1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Imports System.Reflection
Public Class A
End Class
<DefaultMember("Q")>
Public Class B
    ReadOnly Property Q As A
        Get
            Return Nothing
        End Get
    End Property
End Class
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib(source1)
            compilation1.AssertNoErrors()
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Sub M(o As B)
        Dim value As Object
        value = o()
        value = o(Nothing)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim reference1 = MetadataReference.CreateFromImage(compilation1.EmitToArray())
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertTheseDiagnostics(
<expected>
BC32016: 'Public ReadOnly Default Property Q As A' has no parameters and its return type cannot be indexed.
        value = o(Nothing)
                ~
</expected>)
        End Sub

        <WorkItem(531372, "DevDiv")>
        <Fact()>
        Public Sub DefaultPropertyOfParameterlessDefaultPropertyReturnType04()
            Dim source1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Imports System.Reflection
Public Class A
    Default WriteOnly Property P(o As Object) As Object
        Set(value As Object)
        End Set
    End Property
End Class
' Parameterless default member property.
<DefaultMember("P1")>
Public Class B1
    ReadOnly Property P1 As A
        Get
            Return Nothing
        End Get
    End Property
End Class
' Default member property with ParamArray.
<DefaultMember("P2")>
Public Class B2
    ReadOnly Property P2(ParamArray args As Object()) As A
        Get
            Return Nothing
        End Get
    End Property
End Class
' Default member property with Optional argument.
<DefaultMember("P3")>
Public Class B3
    ReadOnly Property P3(Optional arg As Object = Nothing) As A
        Get
            Return Nothing
        End Get
    End Property
End Class
' Parameterless default member property with overload.
<DefaultMember("P4")>
Public Class B4
    Overloads ReadOnly Property P4 As A
        Get
            Return Nothing
        End Get
    End Property
    Overloads ReadOnly Property P4(arg As Object) As A
        Get
            Return Nothing
        End Get
    End Property
End Class
' Parameterless default member function.
<DefaultMember("F")>
Public Class B5
    Function F() As A
        Return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib(source1)
            compilation1.AssertNoErrors()
            Dim reference1 = MetadataReference.CreateFromImage(compilation1.EmitToArray())
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Sub M(_1 As B1, _2 As B2, _3 As B3, _4 As B4, _5 As B5)
        _1(Nothing) = Nothing
        _2(Nothing) = Nothing
        _3(Nothing) = Nothing
        _4(Nothing) = Nothing
        _5(Nothing) = Nothing
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertTheseDiagnostics(
<expected>
BC30526: Property 'P2' is 'ReadOnly'.
        _2(Nothing) = Nothing
        ~~~~~~~~~~~~~~~~~~~~~
BC30526: Property 'P3' is 'ReadOnly'.
        _3(Nothing) = Nothing
        ~~~~~~~~~~~~~~~~~~~~~
BC30526: Property 'P4' is 'ReadOnly'.
        _4(Nothing) = Nothing
        ~~~~~~~~~~~~~~~~~~~~~
BC30367: Class 'B5' cannot be indexed because it has no default property.
        _5(Nothing) = Nothing
        ~~
</expected>)
            Dim source3 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Sub M(_1 As B1, _2 As B2, _3 As B3, _4 As B4)
        Dim value As Object = Nothing
        _1(Nothing) = value
        value = _2(Nothing)
        value = _3(Nothing)
        value = _4(Nothing)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation3 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source3, {reference1})
            compilation3.AssertNoErrors()
            Dim compilationVerifier = CompileAndVerify(compilation3)
            compilationVerifier.VerifyIL("M.M(B1, B2, B3, B4)",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (Object V_0) //value
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  callvirt   "Function B1.get_P1() As A"
  IL_0008:  ldnull
  IL_0009:  ldloc.0
  IL_000a:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000f:  callvirt   "Sub A.set_P(Object, Object)"
  IL_0014:  ldarg.1
  IL_0015:  ldnull
  IL_0016:  callvirt   "Function B2.get_P2(ParamArray Object()) As A"
  IL_001b:  stloc.0
  IL_001c:  ldarg.2
  IL_001d:  ldnull
  IL_001e:  callvirt   "Function B3.get_P3(Object) As A"
  IL_0023:  stloc.0
  IL_0024:  ldarg.3
  IL_0025:  ldnull
  IL_0026:  callvirt   "Function B4.get_P4(Object) As A"
  IL_002b:  stloc.0
  IL_002c:  ret
}
]]>)
        End Sub

        ''' <summary>
        ''' Default member from ElementAtOrDefault.
        ''' </summary>
        <WorkItem(531372, "DevDiv")>
        <Fact()>
        Public Sub DefaultPropertyOfParameterlessElementAtOrDefault01()
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Delegate Function D(o As Object) As Object
Class A
    Default ReadOnly Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class
' Default member returns type with default member.
Class C1
    Public Function [Select](f As System.Func(Of Object, Object)) As C1
        Return Nothing
    End Function
    Public Function ElementAtOrDefault() As A
        Return Nothing
    End Function
End Class
' Default member returns Array.
Class C2
    Public Function [Select](f As System.Func(Of Object, Object)) As C2
        Return Nothing
    End Function
    Public Function ElementAtOrDefault() As Object()
        Return Nothing
    End Function
End Class
' Default member returns Delegate.
Class C3
    Public Function [Select](f As System.Func(Of Object, Object)) As C3
        Return Nothing
    End Function
    Public Function ElementAtOrDefault() As D
        Return Nothing
    End Function
End Class
Module M
    Sub M(_1 As C1, _2 As C2, _3 As C3)
        Dim value As Object
        value = _1()(Nothing)
        value = _1(Nothing)
        value = _1()()
        value = _2()(Nothing)
        value = _2(Nothing)
        value = _2()()
        value = _3()(Nothing)
        value = _3(Nothing)
        value = _3()()
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.AssertTheseDiagnostics(
<expected>
BC30455: Argument not specified for parameter 'o' of 'Public ReadOnly Default Property P(o As Object) As Object'.
        value = _1()()
                ~~~~
BC30105: Number of indices is less than the number of dimensions of the indexed array.
        value = _2()()
                    ~~
BC30455: Argument not specified for parameter 'o' of 'D'.
        value = _3()()
                ~~~~
</expected>)
        End Sub

        ''' <summary>
        ''' Default member from ElementAtOrDefault.
        ''' </summary>
        <WorkItem(531372, "DevDiv")>
        <Fact()>
        Public Sub DefaultPropertyOfParameterlessElementAtOrDefault02()
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class A
    Default ReadOnly Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class
Class B
    Public Function [Select](f As System.Func(Of Object, Object)) As B
        Return Nothing
    End Function
    Public Function ElementAtOrDefault() As A
        Return Nothing
    End Function
End Class
Class C
    Public Function [Select](f As System.Func(Of Object, Object)) As C
        Return Nothing
    End Function
    Public Function ElementAtOrDefault() As B
        Return Nothing
    End Function
End Class
Class D
    Public Function [Select](f As System.Func(Of Object, Object)) As D
        Return Nothing
    End Function
    Public Function ElementAtOrDefault() As C
        Return Nothing
    End Function
End Class
Module M
    Sub M(o As D)
        Dim value As Object
        value = o()()()(Nothing)
        value = o()()(Nothing)
        value = o()(Nothing)
        value = o(Nothing)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.AssertTheseDiagnostics(
<expected>
BC30057: Too many arguments to 'Public Function ElementAtOrDefault() As A'.
        value = o()(Nothing)
                    ~~~~~~~
BC30057: Too many arguments to 'Public Function ElementAtOrDefault() As B'.
        value = o(Nothing)
                  ~~~~~~~
</expected>)
        End Sub

        ''' <summary>
        ''' ElementAtOrDefault returning System.Array.
        ''' </summary>
        <WorkItem(531372, "DevDiv")>
        <WorkItem(575547, "DevDiv")>
        <Fact()>
        Public Sub DefaultPropertyOfParameterlessElementAtOrDefault03()
            ' Option Strict On
            Dim source1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Class C
    Public Function [Select](f As System.Func(Of Object, Object)) As C
        Return Nothing
    End Function
    Public Function ElementAtOrDefault() As System.Array
        Return Nothing
    End Function
End Class
Module M
    Sub M(o As C)
        Dim value As Object
        value = o()(1)
        value = o(2)
        o()(3) = value
        o(4) = value
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntime(source1)
            compilation1.AssertTheseDiagnostics(
<expected>
BC30574: Option Strict On disallows late binding.
        value = o()(1)
                ~~~
BC30574: Option Strict On disallows late binding.
        value = o(2)
                ~
BC30574: Option Strict On disallows late binding.
        o()(3) = value
        ~~~
BC30574: Option Strict On disallows late binding.
        o(4) = value
        ~
</expected>)
            ' Option Strict Off
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict Off
Class C
    Public Function [Select](f As System.Func(Of Object, Object)) As C
        Return Nothing
    End Function
    Public Function ElementAtOrDefault() As System.Array
        Return Nothing
    End Function
End Class
Module M
    Sub M(o As C)
        Dim value As Object
        value = o()(1)
        value = o(2)
        o()(3) = value
        o(4) = value
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntime(source2)
            compilation2.AssertNoErrors()
        End Sub

        ''' <summary>
        ''' ElementAtOrDefault property. (ElementAtOrDefault field
        ''' not supported - see #576814.)
        ''' </summary>
        <WorkItem(531372, "DevDiv")>
        <Fact()>
        Public Sub DefaultPropertyOfParameterlessElementAtOrDefault04()
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Class A
    Public Function [Select](f As System.Func(Of Object, Object)) As A
        Return Nothing
    End Function
    Public ReadOnly Property ElementAtOrDefault As Integer()
        Get
            Return Nothing
        End Get
    End Property
End Class
Module M
    Sub M(_a As A)
        Dim value As Integer
        value = _a(1)
        _a(2) = value
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.AssertNoErrors()
            Dim compilationVerifier = CompileAndVerify(compilation)
            compilationVerifier.VerifyIL("M.M(A)",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  3
  .locals init (Integer V_0) //value
  IL_0000:  ldarg.0
  IL_0001:  callvirt   "Function A.get_ElementAtOrDefault() As Integer()"
  IL_0006:  ldc.i4.1
  IL_0007:  ldelem.i4
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  callvirt   "Function A.get_ElementAtOrDefault() As Integer()"
  IL_000f:  ldc.i4.2
  IL_0010:  ldloc.0
  IL_0011:  stelem.i4
  IL_0012:  ret
}
]]>)
        End Sub

        ''' <summary>
        ''' Parentheses required for call to delegate.
        ''' </summary>
        <WorkItem(531372, "DevDiv")>
        <Fact()>
        Public Sub DefaultPropertyOfParameterlessDelegate()
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Delegate Function D() As C
Class C
    Default ReadOnly Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class
Module M
    Sub M(o As D)
        Dim value As Object
        value = o()(Nothing)
        value = o(Nothing)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.AssertTheseDiagnostics(
<expected>
BC30057: Too many arguments to 'D'.
        value = o(Nothing)
                  ~~~~~~~
</expected>)
        End Sub

        <WorkItem(578180, "DevDiv")>
        <Fact()>
        Public Sub DefaultPropertyOfInheritedConstrainedTypeParameter()
            Dim source1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Reflection
<DefaultMember("P")>
Public Interface I
    ReadOnly Property P As Object
End Interface
<DefaultMember("P")>
Public Class C
    Public ReadOnly Property P As Object
        Get
            Return Nothing
        End Get
    End Property
End Class
<DefaultMember("P")>
Public Structure S
    Public ReadOnly Property P As Object
        Get
            Return Nothing
        End Get
    End Property
End Structure
<DefaultMember("M")>
Public Enum E
    M
End Enum
Public Delegate Function D() As Object
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib(source1)
            compilation1.AssertNoErrors()
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
MustInherit Class A(Of T)
    MustOverride Function F(Of U As T)(o As U) As Object
End Class
' Interface with default parameterless property.
Class B1
    Inherits A(Of I)
    Public Overrides Function F(Of T As I)(o1 As T) As Object
        Return o1()
    End Function
End Class
' Class with default parameterless property.
Class B2
    Inherits A(Of C)
    Public Overrides Function F(Of T As C)(o2 As T) As Object
        Return o2()
    End Function
End Class
' Structure with default parameterless property.
Class B3
    Inherits A(Of S)
    Public Overrides Function F(Of T As S)(o3 As T) As Object
        Return o3()
    End Function
End Class
' Enum with default member.
Class B4
    Inherits A(Of E)
    Public Overrides Function F(Of T As E)(o4 As T) As Object
        Return o4()
    End Function
End Class
' Delegate.
Class B5
    Inherits A(Of D)
    Public Overrides Function F(Of T As D)(o5 As T) As Object
        Return o5()
    End Function
End Class
' Array.
Class B6
    Inherits A(Of C())
    Public Overrides Function F(Of T As C())(o6 As T) As Object
        Return o6()
    End Function
End Class
' Type parameter.
Class B7(Of T)
    Inherits A(Of T)
    Public Overrides Function F(Of U As T)(o7 As U) As Object
        Return o7()
    End Function
End Class
]]>
                    </file>
                </compilation>
            Dim reference1 = MetadataReference.CreateFromImage(compilation1.EmitToArray())
            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertTheseDiagnostics(
<expected>
BC30547: 'T' cannot be indexed because it has no default property.
        Return o3()
               ~~
BC30547: 'T' cannot be indexed because it has no default property.
        Return o4()
               ~~
BC30547: 'T' cannot be indexed because it has no default property.
        Return o5()
               ~~
BC30547: 'T' cannot be indexed because it has no default property.
        Return o6()
               ~~
BC30547: 'U' cannot be indexed because it has no default property.
        Return o7()
               ~~
</expected>)
        End Sub

        <WorkItem(539951, "DevDiv")>
        <Fact>
        Public Sub ImportedParameterlessDefaultProperties()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit CSDefaultMembers
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 05 49 74 65 6D 73 00 00 )                   // ...Items..
  .field private int32 '<Items>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname instance int32 
          get_Items() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 CSDefaultMembers::'<Items>k__BackingField'
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method CSDefaultMembers::get_Items

  .method public hidebysig specialname instance void 
          set_Items(int32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      int32 CSDefaultMembers::'<Items>k__BackingField'
    IL_0007:  ret
  } // end of method CSDefaultMembers::set_Items

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CSDefaultMembers::.ctor

  .property instance int32 Items()
  {
    .get instance int32 CSDefaultMembers::get_Items()
    .set instance void CSDefaultMembers::set_Items(int32)
  } // end of property CSDefaultMembers::Items
} // end of class CSDefaultMembers]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Module Program
    Sub Main()
        Dim obj As CSDefaultMembers = New CSDefaultMembers()
        obj() = 9
        Dim x = obj()
        System.Console.WriteLine(x)
    End Sub
End Module
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL.Value, TestOptions.ReleaseExe, includeVbRuntime:=True)
            CompilationUtils.AssertNoErrors(compilation)
            CompileAndVerify(compilation, expectedOutput:="9")
        End Sub

        <WorkItem(539957, "DevDiv")>
        <Fact>
        Public Sub DefaultPropertyInFunctionReturn()
            Dim source =
<compilation>
    <file name="a.vb">
Module Program
    Private Obj As VBDefaultMembers
    Sub Main()
        Obj = New VBDefaultMembers()
        Obj(1) = 4
        System.Console.WriteLine(Foo(1))

        Dim dd As DefaultDefaultMember = New DefaultDefaultMember
        dd.Item = Obj
        System.Console.WriteLine(dd.Item(1))

        ' bind-position
    End Sub
    Function Foo() As VBDefaultMembers
        Return Obj
    End Function 
    Function Bar() As Integer()
        Return Nothing
    End Function 
End Module
Public Class DefaultDefaultMember
    Property Item As VBDefaultMembers
End Class
Public Class VBDefaultMembers
    'Property Items As Integer
    Public _items As Integer() = New Integer(4) {}

    Default Public Property Items(index As Integer) As Integer
        Get
            Return _items(index)
        End Get
        Set(value As Integer)
            _items(index) = value
        End Set
    End Property
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)
            Dim position = (source...<file>.Single().Value.IndexOf("' bind-position", StringComparison.Ordinal))

            Dim bindings = compilation.GetSemanticModel(CompilationUtils.GetTree(compilation, "a.vb"))
            Assert.Equal(SpecialType.System_Int32, bindings.GetSpeculativeSemanticInfoSummary(position, SyntaxFactory.ParseExpression("Foo().Items(1)"), SpeculativeBindingOption.BindAsExpression).Type.SpecialType)
            Assert.Equal(SpecialType.System_Int32, bindings.GetSpeculativeSemanticInfoSummary(position, SyntaxFactory.ParseExpression("Foo.Items(1)"), SpeculativeBindingOption.BindAsExpression).Type.SpecialType)
            Assert.Equal(SpecialType.System_Int32, bindings.GetSpeculativeSemanticInfoSummary(position, SyntaxFactory.ParseExpression("Foo()(1)"), SpeculativeBindingOption.BindAsExpression).Type.SpecialType)
            Assert.Equal(SpecialType.System_Int32, bindings.GetSpeculativeSemanticInfoSummary(position, SyntaxFactory.ParseExpression("Foo(1)"), SpeculativeBindingOption.BindAsExpression).Type.SpecialType)
            Assert.Equal(SpecialType.System_Int32, bindings.GetSpeculativeSemanticInfoSummary(position, SyntaxFactory.ParseExpression("dd.Item(1)"), SpeculativeBindingOption.BindAsExpression).Type.SpecialType)
            Assert.Equal(SpecialType.System_Int32, bindings.GetSpeculativeSemanticInfoSummary(position, SyntaxFactory.ParseExpression("Bar(1)"), SpeculativeBindingOption.BindAsExpression).Type.SpecialType)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[
4
4
]]>)
        End Sub

        <WorkItem(539957, "DevDiv")>
        <Fact>
        Public Sub EmptyArgumentListWithNoIndexerOrDefaultProperty()
            Dim source =
<compilation>
    <file name="a.vb">
Class C2
    Private Sub M()
        Dim x = A(6)
        Dim y = B(6)
        Dim z = C(6)
        Call A(6)
        Call B(6)
        Call C(6)
    End Sub

    Private Function A() As String
        Return "Hello"
    End Function

    Private Function B() As Integer()
        Return Nothing
    End Function

    Private Function C() As Integer
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32016: 'Private Function C() As Integer' has no parameters and its return type cannot be indexed.
        Dim z = C(6)
                ~
BC30057: Too many arguments to 'Private Function A() As String'.
        Call A(6)
               ~
BC30057: Too many arguments to 'Private Function B() As Integer()'.
        Call B(6)
               ~
BC30057: Too many arguments to 'Private Function C() As Integer'.
        Call C(6)
               ~
</expected>)
        End Sub

        <WorkItem(539957, "DevDiv")>
        <Fact>
        Public Sub WrongArityWithFunctionsOfZeroParameters()
            Dim source =
<compilation>
    <file name="a.vb">
Class C1
    Public Function Foo() As Integer()
        Return Nothing
    End Function

    Public Sub TST()
        Dim a As Integer = Foo(Of Integer)(1)
    End Sub
End Class

Class C2
    Public Function Foo(Of T)() As Integer()
        Return Nothing
    End Function

    Public Sub TST()
        Dim a As Integer = Foo(1)
        Call Foo(1)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32045: 'Public Function Foo() As Integer()' has no type parameters and so cannot have type arguments.
        Dim a As Integer = Foo(Of Integer)(1)
                              ~~~~~~~~~~~~
BC30057: Too many arguments to 'Public Function Foo(Of T)() As Integer()'.
        Dim a As Integer = Foo(1)
                               ~
BC30057: Too many arguments to 'Public Function Foo(Of T)() As Integer()'.
        Call Foo(1)
                 ~
</expected>)

            '  WARNING!!! Dev10 generates:
            '
            '  BC32045: 'Public Function Foo() As Integer()' has no type parameters and so cannot have type arguments.
            '  BC32050: BC32050: Type parameter 'T' for 'Public Function Foo(Of T)() As Integer()' cannot be inferred.

        End Sub

        <WorkItem(539957, "DevDiv")>
        <Fact>
        Public Sub PropertyReturningDelegate()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C
    Public ReadOnly Property Foo As Func(Of String, Integer)
        Get
            Return AddressOf Impl
        End Get
    End Property

    Private Function Impl(str As String) As Integer
        Return 0
    End Function

    Public Sub TST()
        Dim a As Integer = Foo()("abc")
        Dim b As Integer = Foo("abc")
        Dim c = Foo()
        Dim d = Foo
        Call Foo()("abc")
        Call Foo("abc")
        Call Foo()
        Call Foo
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public ReadOnly Property Foo As Func(Of String, Integer)'.
        Call Foo("abc")
                 ~~~~~
BC30545: Property access must assign to the property or use its value.
        Call Foo()
             ~~~~~
BC30545: Property access must assign to the property or use its value.
        Call Foo
             ~~~
</expected>)
        End Sub

        <WorkItem(539957, "DevDiv")>
        <Fact>
        Public Sub FunctionWithZeroParametersReturningDelegate()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C
    Public Function Foo() As Func(Of String, Integer)
        Return AddressOf Impl
    End Function

    Private Function Impl(str As String) As Integer
        Return 0
    End Function

    Public Sub TST()
        Dim a As Integer = Foo()("abc")
        Dim b As Integer = Foo("abc")
        Dim c = Foo()
        Dim d = Foo()
        Foo()("abc")
        Foo("abc")
        Foo()
        Foo
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Function Foo() As Func(Of String, Integer)'.
        Foo("abc")
            ~~~~~
</expected>)
        End Sub

        <WorkItem(539957, "DevDiv")>
        <Fact>
        Public Sub EmptyArgumentListWithFunctionAndSub()
            Dim source =
<compilation>
    <file name="a.vb">
Module Program
    Public Function Foo() As Integer()
        Dim arr As Integer() = New Integer(4) {}
        arr(2) = 234
        Return arr
    End Function

    Public Sub Foo(i As Integer)
        System.Console.WriteLine(i)
    End Sub

    Public Sub Main()
        Dim a As Integer = Foo(2)
        Call Foo(a)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:="234")
        End Sub

        <WorkItem(539957, "DevDiv")>
        <Fact>
        Public Sub FunctionsWithDifferentArity_0()
            Dim source =
<compilation>
    <file name="a.vb">
Class CLS
    Public Overloads Function Foo(Of T)() As Integer()
        Return Nothing
    End Function
    Public Overloads Function Foo() As Integer()
        Return Nothing
    End Function

    Public Sub TST()
        Dim a As Integer = Foo(Of Integer)(1)
        Dim b As Integer = Foo(1)
        Call Foo(Of Integer)(1)
        Call Foo(1)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Overloads Function Foo(Of Integer)() As Integer()'.
        Call Foo(Of Integer)(1)
                             ~
BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
        Call Foo(1)
             ~~~
</expected>)

            '  WARNING!!! Dev10 generates:
            '
            '  BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
            '  BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
            '  BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
            '  BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.

        End Sub

        <WorkItem(539957, "DevDiv")>
        <Fact>
        Public Sub FunctionsWithDifferentArity_1()
            Dim source =
<compilation>
    <file name="a.vb">
Class CBase
    Public Function Foo(Of T)() As Integer()
        Return Nothing
    End Function
End Class

Class CDerived
    Inherits CBase

    Public Overloads Function Foo() As Integer()
        Return Nothing
    End Function

    Public Sub TST()
        Dim a As Integer = Foo(Of Integer)(1)
        Dim b As Integer = Foo(1)
        Call Foo(Of Integer)(1)
        Call Foo(1)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Function Foo(Of Integer)() As Integer()'.
        Call Foo(Of Integer)(1)
                             ~
BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
        Call Foo(1)
             ~~~
</expected>)

            '  WARNING!!! Dev10 generates:
            '
            '  BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
            '  BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
            '  BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.

        End Sub

        <WorkItem(539957, "DevDiv")>
        <Fact>
        Public Sub FunctionsWithDifferentArity_2()
            Dim source =
<compilation>
    <file name="a.vb">
Class CBase
    Public Function Foo(Of T)() As Integer()
        Return Nothing
    End Function
End Class

Class CDerived
    Inherits CBase

    Public Overloads Function Foo(Of X, Y)() As Integer()
        Return Nothing
    End Function

    Public Sub TST()
        Dim a As Integer = Foo(Of Integer)(1)
        Dim b As Integer = Foo(1)
        Call Foo(Of Integer)(1)
        Call Foo(1)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
        Dim b As Integer = Foo(1)
                           ~~~
BC30057: Too many arguments to 'Public Function Foo(Of Integer)() As Integer()'.
        Call Foo(Of Integer)(1)
                             ~
BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
        Call Foo(1)
             ~~~
</expected>)

            '  WARNING!!! Dev10 generates:
            '
            '  BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
            '  BC32050: Type parameter 'X' for 'Public Overloads Function Foo(Of X, Y)() As Integer()' cannot be inferred.
            '  BC32050: Type parameter 'Y' for 'Public Overloads Function Foo(Of X, Y)() As Integer()' cannot be inferred.
            '  BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
            '  BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.

        End Sub

        <WorkItem(539957, "DevDiv")>
        <Fact>
        Public Sub PropertiesWithInheritanceAndParentheses()
            Dim source =
<compilation>
    <file name="a.vb">
Interface IBase
    Property Foo As Integer()
End Interface

Class CBase
    Public Property Foo As Integer()
End Class

Class CDerived
    Inherits CBase
    Implements IBase

    Public Overloads Property Foo2 As Integer() Implements IBase.Foo

    Public Overloads Property Foo As Integer()

    Public Sub TST()
        Dim a As Integer = Foo()(1)
        Dim b As Integer = Foo(1)
        Dim c As Integer() = Foo()
        Dim d As Integer() = Foo
        Call Foo()(1)
        Call Foo(1)
        Call Foo()
        Call Foo
    End Sub
End Class    
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30454: Expression is not a method.
        Call Foo()(1)
             ~~~~~
BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
        Call Foo(1)
             ~~~
BC30545: Property access must assign to the property or use its value.
        Call Foo()
             ~~~~~
BC30545: Property access must assign to the property or use its value.
        Call Foo
             ~~~
</expected>)

            '  WARNING!!! Dev10 generates:
            '
            '  BC30454: Expression is not a method.
            '  BC30545: Property access must assign to the property or use its value.
            '  BC30545: Property access must assign to the property or use its value.
            '  BC30545: Property access must assign to the property or use its value.

        End Sub

        <WorkItem(539957, "DevDiv")>
        <Fact>
        Public Sub WriteOnlyPropertiesWithInheritanceAndParentheses()
            Dim source =
<compilation>
    <file name="a.vb">
Interface IBase
    WriteOnly Property Foo As Integer()
End Interface

Class CBase
    Public WriteOnly Property Foo As Integer()
        Set(value As Integer())
        End Set
    End Property
End Class

Class CDerived
    Inherits CBase
    Implements IBase

    Public WriteOnly Property Foo2 As Integer() Implements IBase.Foo
        Set(value As Integer())
        End Set
    End Property

    Public Overloads WriteOnly Property Foo As Integer()
        Set(value As Integer())
        End Set
    End Property

    Public Sub TST()
        Dim a As Integer = Foo()(1)
        Dim b As Integer = Foo(1)
        Dim c As Integer() = Foo()
        Dim d As Integer() = Foo
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30524: Property 'Foo' is 'WriteOnly'.
        Dim a As Integer = Foo()(1)
                           ~~~~~
BC30524: Property 'Foo' is 'WriteOnly'.
        Dim b As Integer = Foo(1)
                           ~~~
BC30524: Property 'Foo' is 'WriteOnly'.
        Dim c As Integer() = Foo()
                             ~~~~~
BC30524: Property 'Foo' is 'WriteOnly'.
        Dim d As Integer() = Foo
                             ~~~
</expected>)
        End Sub

        <WorkItem(539903, "DevDiv")>
        <Fact>
        Public Sub DefaultPropertyBangOperator()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Program
    Sub Main()
        Dim bang = New TestClassX()
        bang!Hello = "World"
        System.Console.WriteLine(bang!Hello)
    End Sub
End Module
Class TestClassX
    Public _items() As String = New String(100) {}
    Default Property Items(key As String) As String
        Get
            Return _items(key.Length)
        End Get
        Set(value As String)
            _items(key.Length) = value
        End Set
    End Property
End Class
        ]]>
    </file>
</compilation>
            CompileAndVerify(source, expectedOutput:="World")
        End Sub
#End Region
#Region "Typeless properties"
        <Fact>
        Public Sub TypelessAndImplicitlyTypeProperties()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Class TestClass
    Property Typeless
    Property StringType$
    Property IntegerType%
    Property LongType&
    Property DecimalType@
    Property SingleType!
    Property DoubleType#
End Class
]]>
    </file>
</compilation>
            Dim validator = Sub([module] As ModuleSymbol)
                                Dim testClassType = [module].GlobalNamespace.GetTypeMembers("TestClass").Single()
                                Dim propertiesDictionary = testClassType.GetMembers().OfType(Of PropertySymbol).ToDictionary(Function(prop) prop.Name, Function(prop) prop)
                                Assert.Equal(SpecialType.System_Object, propertiesDictionary!Typeless.Type.SpecialType)
                                Assert.Equal(SpecialType.System_String, propertiesDictionary!StringType.Type.SpecialType)
                                Assert.Equal(SpecialType.System_Int32, propertiesDictionary!IntegerType.Type.SpecialType)
                                Assert.Equal(SpecialType.System_Int64, propertiesDictionary!LongType.Type.SpecialType)
                                Assert.Equal(SpecialType.System_Decimal, propertiesDictionary!DecimalType.Type.SpecialType)
                                Assert.Equal(SpecialType.System_Single, propertiesDictionary!SingleType.Type.SpecialType)
                                Assert.Equal(SpecialType.System_Double, propertiesDictionary!DoubleType.Type.SpecialType)
                            End Sub
            CompileAndVerify(source, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub
#End Region
#Region "Properties calls"
        Private ReadOnly _propertiesCallBaseSource As XElement =
            <compilation>
                <file name="a.vb">
Module Program
    Sub Main()
        Dim obj As TestClass1 = New TestClass1()
        obj.P1 = New TestClass2()
        obj.P2 = New TestClass2()
        obj.P1.id = 1
        obj.P2.id = 2
        <more_code/>
    End Sub
    Sub ByRefSwap(ByRef myObj1 As TestClass2, ByRef myObj2 As TestClass2)
        Dim tempObj As TestClass2 = myObj1
        myObj1 = myObj2
        myObj2 = tempObj
    End Sub
    Sub ByValSwap(myObj1 As TestClass2,  myObj2 As TestClass2)
        Dim tempObj As TestClass2 = myObj1
        myObj1 = myObj2
        myObj2 = tempObj
    End Sub
End Module
Public Class TestClass2
    Property id As Integer
End Class
Public Class TestClass1
    Property P1 As TestClass2
    Property P2 As TestClass2
End Class
                </file>
            </compilation>

        <Fact>
        Public Sub PassPropertyByValue()
            _propertiesCallBaseSource.Element("file").SetElementValue("more_code",
            <![CDATA[
ByValSwap(obj.P1, obj.P2)    'now o.P1.id = 1 and o.P2.id = 2
System.Console.WriteLine(String.Join(",", obj.P1.id, obj.P2.id))]]>.Value)

            CompileAndVerify(_propertiesCallBaseSource, expectedOutput:="1,2")
        End Sub

        <Fact>
        Public Sub PassPropertyByRef()
            _propertiesCallBaseSource.Element("file").SetElementValue("more_code",
            <![CDATA[
ByRefSwap(obj.P1, obj.P2)    'now o.P1.id = 2 and o.P2.id = 1
System.Console.WriteLine(String.Join(",", obj.P1.id, obj.P2.id))]]>.Value)
            CompileAndVerify(_propertiesCallBaseSource, expectedOutput:="2,1")
        End Sub

        <Fact>
        Public Sub PassPropertyByRefWithByValueOverride()
            _propertiesCallBaseSource.Element("file").SetElementValue("more_code",
            <![CDATA[
ByRefSwap((obj.P1), obj.P2)    'now o.P1.id = 1 and o.P2.id = 2
System.Console.WriteLine(String.Join(",", obj.P1.id, obj.P2.id))]]>.Value)
            CompileAndVerify(_propertiesCallBaseSource, expectedOutput:="1,1")
        End Sub
#End Region
#Region "Properties member access"
        <WorkItem(539962, "DevDiv")>
        <Fact>
        Public Sub PropertiesAccess()
            Dim source =
<compilation>
    <file name="a.vb">
Public Class TestClass
    Public Property P1 As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
    Friend ReadOnly Property P2 As Integer
        Get
            Return 0
        End Get
    End Property
    Protected Friend ReadOnly Property P3 As Integer
        Get
            Return 0
        End Get
    End Property
    Protected ReadOnly Property P4 As Integer
        Get
            Return 0
        End Get
    End Property
    Private WriteOnly Property P5 As Integer
        Set
        End Set
    End Property
    ReadOnly Property P6 As Integer
        Get
            Return 0
        End Get
    End Property
    Public Property P7 As Integer
        Private Get
            Return 0
        End Get
        Set
        End Set
    End Property
    Friend Property P8 As Integer
        Get
            Return 0
        End Get

        Private Set
        End Set
    End Property
    Protected Property P9 As Integer
        Get
            Return 0
        End Get
        Private Set
        End Set
    End Property
    Protected Friend Property P10 As Integer
        Protected Get
            Return 0
        End Get
        Set
        End Set
    End Property
    Protected Friend Property P11 As Integer
        Friend Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Class
    </file>
</compilation>

            Dim validator = Function(isFromSource As Boolean) _
                Sub([module] As ModuleSymbol)
                    Dim type = [module].GlobalNamespace.GetTypeMembers("TestClass").Single()
                    Dim members = type.GetMembers()
                    ' Ensure member names are unique.
                    Dim memberNames = members.[Select](Function(member) member.Name).Distinct().ToList()
                    Assert.Equal(memberNames.Count, members.Length)
                    'Dim constructor = members.FirstOrDefault(Function(member) member.Name = ".ctor")
                    'Assert.NotNull(constructor)
                    Dim p1 = type.GetMember(Of PropertySymbol)("P1")
                    Dim p2 = type.GetMember(Of PropertySymbol)("P2")
                    Dim p3 = type.GetMember(Of PropertySymbol)("P3")
                    Dim p4 = type.GetMember(Of PropertySymbol)("P4")
                    Dim p7 = type.GetMember(Of PropertySymbol)("P7")
                    Dim p8 = type.GetMember(Of PropertySymbol)("P8")
                    Dim p9 = type.GetMember(Of PropertySymbol)("P9")
                    Dim p10 = type.GetMember(Of PropertySymbol)("P10")
                    Dim p11 = type.GetMember(Of PropertySymbol)("P11")
                    Dim privateOrNotApplicable = If(isFromSource, Accessibility.Private, Accessibility.NotApplicable)
                    CheckPropertyAccessibility(p1, Accessibility.Public, Accessibility.Public, Accessibility.Public)
                    CheckPropertyAccessibility(p2, Accessibility.Friend, Accessibility.Friend, Accessibility.NotApplicable)
                    CheckPropertyAccessibility(p3, Accessibility.ProtectedOrFriend, Accessibility.ProtectedOrFriend, Accessibility.NotApplicable)
                    CheckPropertyAccessibility(p4, Accessibility.Protected, Accessibility.Protected, Accessibility.NotApplicable)
                    CheckPropertyAccessibility(p10, Accessibility.ProtectedOrFriend, Accessibility.Protected, Accessibility.ProtectedOrFriend)
                    CheckPropertyAccessibility(p11, Accessibility.ProtectedOrFriend, Accessibility.Friend, Accessibility.ProtectedOrFriend)
                    If isFromSource Then
                        Dim p5 = type.GetMember(Of PropertySymbol)("P5")
                        Dim p6 = type.GetMember(Of PropertySymbol)("P6")
                        CheckPropertyAccessibility(p5, Accessibility.Private, Accessibility.NotApplicable, Accessibility.Private)
                        CheckPropertyAccessibility(p6, Accessibility.Public, Accessibility.Public, Accessibility.NotApplicable)
                    End If
                    'This checks a moved to last because they are affected by bug#
                    CheckPropertyAccessibility(p7, Accessibility.Public, privateOrNotApplicable, Accessibility.Public)
                    CheckPropertyAccessibility(p8, Accessibility.Friend, Accessibility.Friend, privateOrNotApplicable)
                    CheckPropertyAccessibility(p9, Accessibility.Protected, Accessibility.Protected, privateOrNotApplicable)
                End Sub

            CompileAndVerify(source, symbolValidator:=validator(False), sourceSymbolValidator:=validator(True), options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))
        End Sub
#End Region
#Region "Ported C# test cases"
#Region "Symbols"
        <Fact>
        Public Sub Simple1()
            Dim text = <compilation><file name="c.vb"><![CDATA[
Class A
    Private MustOverride Property P() As Integer
End Class
]]></file></compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib(text)
            Dim [global] = comp.GlobalNamespace
            Dim a = [global].GetTypeMembers("A", 0).Single()
            Dim p = TryCast(a.GetMembers("P").AsEnumerable().SingleOrDefault(), PropertySymbol)
        End Sub

        <Fact>
        Public Sub EventEscapedIdentifier()
            Dim text = <compilation><file name="c.vb"><![CDATA[
Delegate Sub [out]()
Class C1
    Private Property [in] as out
End Class

]]></file></compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib(text)
            Dim c1 As NamedTypeSymbol = DirectCast(comp.SourceModule.GlobalNamespace.GetMembers("C1").Single(), NamedTypeSymbol)
            Dim ein As PropertySymbol = DirectCast(c1.GetMembers("in").Single(), PropertySymbol)
            Assert.Equal("in", ein.Name)
            Assert.Equal("Private Property [in] As out", ein.ToString())
            Dim dout As NamedTypeSymbol = DirectCast(ein.Type, NamedTypeSymbol)
            Assert.Equal("out", dout.Name)
            Assert.Equal("out", dout.ToString())
        End Sub

        ''' <summary>
        ''' Properties should refer to methods
        ''' in the type members collection.
        ''' </summary>
        <Fact>
        Public Sub MethodsAndAccessorsSame()
            Dim sources = <compilation>
                              <file name="c.vb"><![CDATA[
Class A
    Public Shared Property P
    Public Property Q
    Public Property R(arg)
        Get
            Return Nothing
        End Get
        Set(value)
        End Set
    End Property
End Class
Class B(Of T, U)
    Public Shared Property P
    Public Property Q
    Public Property R(arg As U) As T
        Get
            Return Nothing
        End Get
        Set(value As T)
        End Set
    End Property
End Class
Class C
    Inherits B(Of String, Integer)
End Class
]]></file>
                          </compilation>
            Dim validator = Sub([module] As ModuleSymbol)
                                Dim type As NamedTypeSymbol
                                Dim accessor As MethodSymbol
                                Dim prop As PropertySymbol

                                ' Non-generic type.
                                type = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("A")
                                Assert.Equal(type.TypeParameters.Length, 0)
                                Assert.Same(type.ConstructedFrom, type)
                                accessor = type.GetMember(Of MethodSymbol)("get_P")
                                VerifyMethodAndAccessorSame(type, DirectCast(accessor.AssociatedSymbol, PropertySymbol), accessor)
                                VerifyMethodsAndAccessorsSame(type, type.GetMember(Of PropertySymbol)("P"))
                                VerifyMethodsAndAccessorsSame(type, type.GetMember(Of PropertySymbol)("Q"))
                                prop = type.GetMember(Of PropertySymbol)("R")
                                VerifyMethodsAndAccessorsSame(type, prop)

                                ' Generic type.
                                type = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("B")
                                Assert.Equal(type.TypeParameters.Length, 2)
                                Assert.Same(type.ConstructedFrom, type)
                                accessor = type.GetMember(Of MethodSymbol)("get_P")
                                VerifyMethodAndAccessorSame(type, DirectCast(accessor.AssociatedSymbol, PropertySymbol), accessor)
                                VerifyMethodsAndAccessorsSame(type, type.GetMember(Of PropertySymbol)("P"))
                                VerifyMethodsAndAccessorsSame(type, type.GetMember(Of PropertySymbol)("Q"))
                                prop = type.GetMember(Of PropertySymbol)("R")
                                VerifyMethodsAndAccessorsSame(type, prop)
                                Assert.Equal(type.TypeArguments(0), prop.Type)
                                Assert.Equal(type.TypeArguments(1), prop.Parameters(0).Type)

                                ' Generic type with parameter substitution.
                                type = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").BaseType
                                Assert.Equal(type.TypeParameters.Length, 2)
                                Assert.NotSame(type.ConstructedFrom, type)
                                accessor = type.GetMember(Of MethodSymbol)("get_P")
                                VerifyMethodAndAccessorSame(type, DirectCast(accessor.AssociatedSymbol, PropertySymbol), accessor)
                                VerifyMethodsAndAccessorsSame(type, type.GetMember(Of PropertySymbol)("P"))
                                VerifyMethodsAndAccessorsSame(type, type.GetMember(Of PropertySymbol)("Q"))
                                prop = type.GetMember(Of PropertySymbol)("R")
                                VerifyMethodsAndAccessorsSame(type, prop)
                                Assert.Equal(type.TypeArguments(0), prop.Type)
                                Assert.Equal(type.TypeArguments(1), prop.Parameters(0).Type)
                            End Sub
            CompileAndVerify(sources, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        <Fact>
        Public Sub NoAccessors()
            Dim source =
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
    End Sub
    Sub M(i As NoAccessors)
        i.Instance = NoAccessors.Static
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {TestReferences.SymbolsTests.Properties}, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30456: 'Instance' is not a member of 'NoAccessors'.
        i.Instance = NoAccessors.Static
        ~~~~~~~~~~
BC30456: 'Static' is not a member of 'NoAccessors'.
        i.Instance = NoAccessors.Static
                     ~~~~~~~~~~~~~~~~~~
</expected>)

            Dim type = DirectCast(compilation.GlobalNamespace.GetMembers("NoAccessors").Single(), PENamedTypeSymbol)

            ' Methods are available.
            Assert.NotNull(type.GetMembers("StaticMethod").SingleOrDefault())
            Assert.NotNull(type.GetMembers("InstanceMethod").SingleOrDefault())
            Assert.Equal(2, type.GetMembers().OfType(Of MethodSymbol)().Count())

            ' Properties are not available.
            Assert.Null(type.GetMembers("Static").SingleOrDefault())
            Assert.Null(type.GetMembers("Instance").SingleOrDefault())
            Assert.Equal(0, type.GetMembers().OfType(Of PropertySymbol)().Count())
        End Sub

        <Fact>
        Public Sub FamilyAssembly()
            Dim source =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.Write(Signatures.StaticGet())
    End Sub
End Module
    </file>
</compilation>
            Dim compilation = CompileWithCustomPropertiesAssembly(source, TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))

            Dim type = DirectCast(compilation.GlobalNamespace.GetMembers("FamilyAssembly").Single(), PENamedTypeSymbol)
            VerifyAccessibility(
                DirectCast(type.GetMembers("FamilyGetAssemblySetStatic").Single(), PEPropertySymbol),
                Accessibility.ProtectedOrFriend,
                Accessibility.[Protected],
                Accessibility.[Friend])
            VerifyAccessibility(
                DirectCast(type.GetMembers("FamilyGetFamilyOrAssemblySetStatic").Single(), PEPropertySymbol),
                Accessibility.ProtectedOrFriend,
                Accessibility.[Protected],
                Accessibility.ProtectedOrFriend)
            VerifyAccessibility(
                DirectCast(type.GetMembers("FamilyGetFamilyAndAssemblySetStatic").Single(), PEPropertySymbol),
                Accessibility.[Protected],
                Accessibility.[Protected],
                Accessibility.ProtectedAndFriend)
            VerifyAccessibility(
                DirectCast(type.GetMembers("AssemblyGetFamilyOrAssemblySetStatic").Single(), PEPropertySymbol),
                Accessibility.ProtectedOrFriend,
                Accessibility.[Friend],
                Accessibility.ProtectedOrFriend)
            VerifyAccessibility(
                DirectCast(type.GetMembers("AssemblyGetFamilyAndAssemblySetStatic").Single(), PEPropertySymbol),
                Accessibility.[Friend],
                Accessibility.[Friend],
                Accessibility.ProtectedAndFriend)
            VerifyAccessibility(
                DirectCast(type.GetMembers("FamilyOrAssemblyGetFamilyOrAssemblySetStatic").Single(), PEPropertySymbol),
                Accessibility.ProtectedOrFriend,
                Accessibility.ProtectedOrFriend,
                Accessibility.ProtectedOrFriend)
            VerifyAccessibility(
                DirectCast(type.GetMembers("FamilyOrAssemblyGetFamilyAndAssemblySetStatic").Single(), PEPropertySymbol),
                Accessibility.ProtectedOrFriend,
                Accessibility.ProtectedOrFriend,
                Accessibility.ProtectedAndFriend)
            VerifyAccessibility(
                DirectCast(type.GetMembers("FamilyAndAssemblyGetFamilyAndAssemblySetStatic").Single(), PEPropertySymbol),
                Accessibility.ProtectedAndFriend,
                Accessibility.ProtectedAndFriend,
                Accessibility.ProtectedAndFriend)
            VerifyAccessibility(
                DirectCast(type.GetMembers("FamilyAndAssemblyGetOnlyInstance").Single(), PEPropertySymbol),
                Accessibility.ProtectedAndFriend,
                Accessibility.ProtectedAndFriend,
                Accessibility.NotApplicable)
            VerifyAccessibility(
                DirectCast(type.GetMembers("FamilyOrAssemblySetOnlyInstance").Single(), PEPropertySymbol),
                Accessibility.ProtectedOrFriend,
                Accessibility.NotApplicable,
                Accessibility.ProtectedOrFriend)
            VerifyAccessibility(
                DirectCast(type.GetMembers("FamilyAndAssemblyGetFamilyOrAssemblySetInstance").Single(), PEPropertySymbol),
                Accessibility.ProtectedOrFriend,
                Accessibility.ProtectedAndFriend,
                Accessibility.ProtectedOrFriend)
        End Sub

        <Fact>
        Public Sub PropertyAccessorDoesNotHideMethod()
            Dim vbSource = <compilation><file name="c.vb">
Interface IA
    Function get_Foo() As String
End Interface

Interface IB
    Inherits IA
    ReadOnly Property Foo() As Integer
End Interface

Class Program
    Private Shared Sub Main()
        Dim x As IB = Nothing
        Dim s As String = x.get_Foo().ToLower()
    End Sub
End Class
</file></compilation>

            CompileAndVerify(vbSource).VerifyDiagnostics(
                Diagnostic(ERRID.WRN_SynthMemberShadowsMember5, "Foo").WithArguments("property", "Foo", "get_Foo", "interface", "IA"))
        End Sub

        <Fact>
        Public Sub PropertyAccessorDoesNotConflictWithMethod()
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Interface IA
    Function get_Foo() As String
End Interface

Interface IB
    ReadOnly Property Foo() As Integer
End Interface

Interface IC
    Inherits IA
    Inherits IB
End Interface

Class Program
    Private Shared Sub Main()
        Dim x As IC = Nothing
        Dim s As String = x.get_Foo().ToLower()
    End Sub
End Class
]]></file></compilation>

            CompileAndVerify(vbSource)

        End Sub

        <Fact>
        Public Sub PropertyAccessorCannotBeCalledAsMethod()
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Interface I
    ReadOnly Property Foo() As Integer
End Interface

Class Program
    Private Shared Sub Main()
        Dim x As I = Nothing
        Dim s As String = x.get_Foo()
    End Sub
End Class
]]></file></compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(vbSource)
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_NameNotMember2, "x.get_Foo").WithArguments("get_Foo", "I"))
            Assert.False(compilation.Emit(IO.Stream.Null).Success)
        End Sub

        <Fact>
        Public Sub CanReadInstancePropertyWithStaticGetterAsStatic()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property instance int32 Foo() { .get int32 A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Integer = A.Foo
    End Sub
End Class
]]></file></compilation>
            CompileWithCustomILSource(vbSource, ilSource)
        End Sub

        <WorkItem(528038, "DevDiv")>
        <Fact()>
        Public Sub CanNotReadInstancePropertyWithStaticGetterAsInstance()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property instance int32 Foo() { .get int32 A::get_Foo() }
}
]]>
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Integer = A.Foo
    End Sub
End Class
]]></file></compilation>
            CompileWithCustomILSource(vbSource, ilSource)
        End Sub

        <WorkItem(527658, "DevDiv")>
        <Fact(Skip:="527658")>
        Public Sub PropertyWithPinnedModifierIsBogus()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property instance int32 pinned Foo() { .get int32 A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Object = A.Foo
    End Sub
End Class
]]></file></compilation>
            CreateCompilationWithCustomILSource(vbSource, ilSource).VerifyDiagnostics()
        End Sub

        <WorkItem(538850, "DevDiv")>
        <Fact()>
        Public Sub PropertyWithMismatchedReturnTypeOfGetterIsBogus()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property string Foo() { .get int32 A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Object = A.Foo
    End Sub
End Class
]]></file></compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            compilation.AssertNoErrors()
        End Sub

        <WorkItem(527659, "DevDiv")>
        <Fact()>
        Public Sub PropertyWithCircularReturnTypeIsNotSupported()
            Dim ilSource = <![CDATA[
.class public E extends E { }

.class public A {
  .method public static class E get_Foo() { ldnull throw }
  .property class E Foo() { .get class E A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Object = A.Foo
    End Sub
End Class
]]></file></compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource).VerifyDiagnostics()

            ' Dev10 errors:
            ' error CS0268: Imported type 'E' is invalid. It contains a circular base class dependency.
            ' error CS0570: 'A.Foo' is not supported by the language
        End Sub

        <WorkItem(527664, "DevDiv")>
        <Fact(Skip:="527664")>
        Public Sub PropertyWithOpenGenericTypeAsTypeArgumentOfReturnTypeIsNotSupported()
            Dim ilSource = <![CDATA[
.class public E<T> { }

.class public A {
  .method public static class E<class E> get_Foo() { ldnull throw }
  .property class E<class E> Foo() { .get class E<class E> A::get_Foo() }
}]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Object = A.Foo
    End Sub
End Class
]]></file></compilation>
            CreateCompilationWithCustomILSource(vbSource, ilSource).VerifyDiagnostics()
        End Sub

        <WorkItem(527657, "DevDiv")>
        <Fact(Skip:="527657")>
        Public Sub Dev10IgnoresSentinelInPropertySignature()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property int32 Foo(...) { .get int32 A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Integer = A.Foo
    End Sub
End Class
]]></file></compilation>
            CompileWithCustomILSource(vbSource, ilSource)
        End Sub

        <Fact>
        Public Sub CanReadModOptProperty()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 modopt(int32) get_Foo() { ldnull throw }
  .property int32 modopt(int32) Foo() { .get int32 modopt(int32) A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Integer = A.Foo
    End Sub
End Class
]]></file></compilation>
            CompileWithCustomILSource(vbSource, ilSource)
        End Sub

        <WorkItem(527660, "DevDiv")>
        <Fact(Skip:="527660")>
        Public Sub CanReadPropertyWithModOptInBaseClassOfReturnType()
            Dim ilSource = <![CDATA[
.class public E extends class [mscorlib]System.Collections.Generic.List`1<int32> modopt(int8) { }

.class public A  {
  .method public static class E get_Foo() { ldnull throw }
  .property class E Foo() { .get class E A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Object = A.Foo
    End Sub
End Class
]]>.</file></compilation>
            CompileWithCustomILSource(vbSource, ilSource)
        End Sub

        <Fact>
        Public Sub CanReadPropertyOfArrayTypeWithModOptElement()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 modopt(int32)[] get_Foo() { ldnull throw }
  .property int32 modopt(int32)[] Foo() { .get int32 modopt(int32)[] A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Integer() = A.Foo
    End Sub
End Class
]]></file></compilation>
            CompileWithCustomILSource(vbSource, ilSource)
        End Sub

        <Fact>
        Public Sub CanReadModOptPropertyWithNonModOptGetter()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property int32 modopt(int32) Foo() { .get int32 A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Integer = A.Foo
    End Sub
End Class
]]></file></compilation>
            CompileWithCustomILSource(vbSource, ilSource)
        End Sub

        <WorkItem(527656, "DevDiv")>
        <Fact(Skip:="527656")>
        Public Sub CanReadNonModOptPropertyWithOpenGenericModOptGetter()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 modopt(class [mscorlib]System.IComparable`1) get_Foo() { ldnull throw }
  .property int32 Foo() { .get int32 modopt(class [mscorlib]System.IComparable`1) A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Integer = A.Foo
    End Sub
End Class
]]></file></compilation>
            CompileWithCustomILSource(vbSource, ilSource)
        End Sub

        <Fact>
        Public Sub CanReadNonModOptPropertyWithModOptGetter()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 modopt(int32) get_Foo() { ldnull throw }
  .property int32 Foo() { .get int32 modopt(int32) A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Integer = A.Foo
    End Sub
End Class
]]></file></compilation>
            CompileWithCustomILSource(vbSource, ilSource)
        End Sub

        <Fact>
        Public Sub CanReadModOptPropertyWithDifferentModOptGetter()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 modopt(int32) get_Foo() { ldnull throw }
  .property int32 modopt(string) Foo() { .get int32 modopt(int32) A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Integer = A.Foo
    End Sub
End Class
]]></file></compilation>
            CompileWithCustomILSource(vbSource, ilSource)
        End Sub

        ''' <summary>
        ''' Nested modopt is invalid and results in a use-site error
        ''' in Roslyn. The native compiler ignores modopts completely.
        ''' </summary>
        <WorkItem(538845, "DevDiv")>
        <Fact>
        Public Sub CanReadPropertyWithMultipleAndNestedModOpts()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 modopt(int32) get_Foo() { ldnull throw }
  .property int32 modopt(int8) modopt(native int modopt(uint8)*[] modopt(void)) Foo() { .get int32 modopt(int32) A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Integer = A.Foo
    End Sub
End Class
]]></file></compilation>
            CreateCompilationWithCustomILSource(vbSource, ilSource).AssertTheseDiagnostics(
<expected>
BC30643: Property 'Foo' is of an unsupported type.
        Dim x As Integer = A.Foo
                             ~~~
</expected>)
        End Sub

        ''' <summary>
        ''' Nested modreq within modopt is invalid and results in a use-site error
        ''' in Roslyn. The native compiler ignores modopts completely.
        ''' </summary>
        <Fact()>
        Public Sub CanReadPropertyWithModReqsNestedWithinModOpts()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 modopt(int32) get_Foo() { ldnull throw }
  .property int32 modopt(class [mscorlib]System.IComparable`1<method void*()[]> modreq(bool)) Foo() { .get int32 modopt(int32) A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Integer = A.Foo
    End Sub
End Class
]]></file></compilation>
            CreateCompilationWithCustomILSource(vbSource, ilSource).AssertTheseDiagnostics(
<expected>
BC30643: Property 'Foo' is of an unsupported type.
        Dim x As Integer = A.Foo
                             ~~~
</expected>)
        End Sub

        <WorkItem(538846, "DevDiv")>
        <Fact>
        Public Sub CanNotReadPropertyWithModReq()
            Dim ilSource = <![CDATA[
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property int32 modreq(int8) Foo() { .get int32 A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Object = A.Foo
        x = A.get_Foo()
    End Sub
End Class
]]></file></compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(vbSource, ilSource)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30643: Property 'Foo' is of an unsupported type.
        Dim x As Object = A.Foo
                            ~~~
BC30456: 'get_Foo' is not a member of 'A'.
        x = A.get_Foo()
            ~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(527662, "DevDiv")>
        <Fact(Skip:="527662")>
        Public Sub CanNotReadPropertyWithModReqInBaseClassOfReturnType()
            Dim ilSource = <![CDATA[
.class public E extends class [mscorlib]System.Collections.Generic.List`1<int32 modreq(int8)[]> { }

.class public A {
  .method public static class E get_Foo() { ldnull throw }
  .property class E Foo() { .get class E A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Private Shared Sub Main()
        Dim x As Object = A.Foo
    End Sub
End Class
]]></file></compilation>
            CompileWithCustomILSource(vbSource, ilSource)
        End Sub

        <Fact>
        Public Sub VoidReturningPropertyHidesMembersFromBase()
            Dim ilSource = <![CDATA[
.class public B {
  .method public static int32 get_Foo() { ldnull throw }
  .property int32 Foo() { .get int32 B::get_Foo() }
}

.class public A extends B {
  .method public static void get_Foo() { ldnull throw }
  .property void Foo() { .get void A::get_Foo() }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class B
    Shared Sub Main()
        Dim x As Object = A.Foo
    End Sub
End Class
]]></file></compilation>
            CreateCompilationWithCustomILSource(vbSource, ilSource).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_VoidValue, "A.Foo"))
        End Sub

        <WorkItem(527663, "DevDiv")>
        <Fact>
        Public Sub CanNotReadPropertyFromAmbiguousGenericClass()
            Dim ilSource = <![CDATA[
.class public A`1<T> {
  .method public static int32 get_Foo() { ldnull throw }
  .property int32 Foo() { .get int32 A`1::get_Foo() }
}

.class public A<T> {
  .method public static int32 get_Foo() { ldnull throw }
  .property int32 Foo() { .get int32 A::get_Foo() }
}
]]>
            Dim source = <compilation><file name="c.vb"><![CDATA[
Class B
    Shared Sub Main()
        Dim x As Object = A(Of Integer).Foo
    End Sub
End Class
]]></file></compilation>

            CreateCompilationWithCustomILSource(source, ilSource).
                VerifyDiagnostics(Diagnostic(ERRID.ERR_AmbiguousInUnnamedNamespace1, "A(Of Integer)").WithArguments("A"))
        End Sub

        <Fact>
        Public Sub PropertyWithoutAccessorsIsBogus()
            Dim ilSource = <![CDATA[
.class public B {
  .method public instance void .ctor() {
    ldarg.0
    call instance void class System.Object::.ctor()
    ret
  }

  .property int32 Foo() { }
}
]]>.Value
            Dim vbSource = <compilation><file name="c.vb"><![CDATA[
Class C
    Private Shared Sub Main()
        Dim foo As Object = B.Foo
    End Sub
End Class
]]></file></compilation>
            CreateCompilationWithCustomILSource(vbSource, ilSource).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_NameNotMember2, "B.Foo").WithArguments("Foo", "B"))
        End Sub

        <WorkItem(538946, "DevDiv")>
        <Fact>
        Public Sub FalseAmbiguity()
            Dim text = <compilation><file name="c.vb"><![CDATA[
Interface IA
    ReadOnly Property Foo() As Integer
End Interface

Interface IB(Of T)
    Inherits IA
End Interface

Interface IC
    Inherits IB(Of Integer)
    Inherits IB(Of String)
End Interface

Class C
    Private Shared Sub Main()
        Dim x As IC = Nothing
        Dim y As Integer = x.Foo
    End Sub
End Class
]]></file></compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib(text)
            Dim diagnostics = comp.GetDiagnostics()
            Assert.Empty(diagnostics)
        End Sub

        <WorkItem(539320, "DevDiv")>
        <Fact>
        Public Sub FalseWarningCS0109ForNewModifier()
            Dim text = <compilation><file name="c.vb"><![CDATA[
Class [MyBase]
    Public ReadOnly Property MyProp() As Integer
        Get
            Return 1
        End Get
    End Property
End Class

Class [MyClass]
    Inherits [MyBase]
            Private intI As Integer = 0
    Private Shadows Property MyProp() As Integer
        Get
            Return intI
        End Get
        Set
            intI = value
        End Set
    End Property
End Class
]]></file></compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib(text)
            Dim diagnostics = comp.GetDiagnostics()
            Assert.Empty(diagnostics)
        End Sub

        <Fact>
        Public Sub FalseErrorCS0103ForValueKeywordInExpImpl()
            Dim text = <compilation><file name="c.vb"><![CDATA[
Interface MyInter
    Property MyProp() As Integer
End Interface

Class TestClass
    Implements MyInter
    Shared intI As Integer = 0
    Private Property MyInter_MyProp() As Integer Implements MyInter.MyProp
        Get
            Return intI
        End Get
        Set
            intI = value
        End Set
    End Property
End Class
]]></file></compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib(text)
            Dim diagnostics = comp.GetDiagnostics()
            Assert.Empty(diagnostics)
        End Sub

        <Fact>
        Public Sub ExplicitInterfaceImplementationSimple()
            Dim text = <compilation><file name="c.vb"><![CDATA[
Interface I
    Property P() As Integer
End Interface

Class C
    Implements I
    Private Property I_P() As Integer Implements I.P
        Get
            Return m_I_P
        End Get
        Set
            m_I_P = Value
        End Set
    End Property
    Private m_I_P As Integer
End Class
]]></file></compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib(text)
            CompilationUtils.AssertNoErrors(comp)

            Dim globalNamespace = comp.GlobalNamespace

            Dim [interface] = DirectCast(globalNamespace.GetTypeMembers("I").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Interface], [interface].TypeKind)

            Dim interfaceProperty = DirectCast([interface].GetMembers("P").Single(), PropertySymbol)

            Dim [class] = DirectCast(globalNamespace.GetTypeMembers("C").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Class], [class].TypeKind)
            Assert.True([class].Interfaces.Contains([interface]))

            Dim classProperty = DirectCast([class].GetMembers("I_P").Single(), PropertySymbol)

            CheckPropertyExplicitImplementation([class], classProperty, interfaceProperty)
        End Sub

        <Fact>
        Public Sub ExplicitInterfaceImplementationGeneric()
            Dim text = <compilation><file name="c.vb"><![CDATA[
Namespace N
    Interface I(Of T)
        Property P() As T
    End Interface
End Namespace

Class C
    Implements N.I(Of Integer)
    Private Property N_I_P() As Integer Implements N.I(Of Integer).P
        Get
            Return m_N_I_P
        End Get
        Set
            m_N_I_P = Value
        End Set
    End Property
    Private m_N_I_P As Integer
End Class
]]></file></compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib(text)
            CompilationUtils.AssertNoErrors(comp)

            Dim globalNamespace = comp.GlobalNamespace
            Dim [namespace] = DirectCast(globalNamespace.GetMembers("N").Single(), NamespaceSymbol)

            Dim [interface] = DirectCast([namespace].GetTypeMembers("I").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Interface], [interface].TypeKind)

            Dim interfaceProperty = DirectCast([interface].GetMembers("P").Single(), PropertySymbol)

            Dim [class] = DirectCast(globalNamespace.GetTypeMembers("C").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Class], [class].TypeKind)

            Dim classProperty = DirectCast([class].GetMembers("N_I_P").Single(), PropertySymbol)

            Dim substitutedInterface = [class].Interfaces.Single()
            Assert.Equal([interface], substitutedInterface.ConstructedFrom)

            Dim substitutedInterfaceProperty = DirectCast(substitutedInterface.GetMembers("P").Single(), PropertySymbol)

            CheckPropertyExplicitImplementation([class], classProperty, substitutedInterfaceProperty)
        End Sub
#End Region
#Region "Emit"""
        <Fact>
        Public Sub PropertyNonDefaultAccessorNames()
            Dim source = <compilation><file name="c.vb"><![CDATA[
Class Program
    Private Shared Sub M(i As Valid)
        i.Instance = 0
        System.Console.Write("{0}", i.Instance)
    End Sub
    Shared Sub Main()
        Valid.[Static] = 0
        System.Console.Write("{0}", Valid.[Static])
    End Sub
End Class
]]></file></compilation>

            Dim compilation = CompileAndVerify(source, additionalRefs:={s_propertiesDll}, expectedOutput:="0")
            Dim ilSource = <![CDATA[{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldc.i4.0  
  IL_0001:  call       "Sub Valid.StaticSet(Integer)"
  IL_0006:  ldstr      "{0}"
  IL_000b:  call       "Function Valid.StaticGet() As Integer"
  IL_0010:  box        "Integer"
  IL_0015:  call       "Sub System.Console.Write(String, Object)"
  IL_001a:  ret       
}
]]>
            compilation.VerifyIL("Program.Main", ilSource)
        End Sub

        <WorkItem(528542, "DevDiv")>
        <Fact()>
        Public Sub MismatchedAccessorTypes()
            Dim source = <code><file name="c.vb"><![CDATA[
Class Program
    Private Shared Sub M(i As Mismatched)
        i.Instance = 0
        System.Console.Write("{0}", i.Instance)
    End Sub
    Private Shared Sub N(i As Signatures)
        i.StaticAndInstance = 0
        i.GetUsedAsSet = 0
    End Sub
    Private Shared Sub Main()
        Mismatched.[Static] = 0
        System.Console.Write("{0}", Mismatched.[Static])
    End Sub
End Class
]]></file></code>

            CompilationUtils.CreateCompilationWithMscorlibAndReferences(source, {s_propertiesDll}).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_NameNotMember2, "i.Instance").WithArguments("Instance", "Mismatched"),
                Diagnostic(ERRID.ERR_NameNotMember2, "i.Instance").WithArguments("Instance", "Mismatched"),
                Diagnostic(ERRID.ERR_UnsupportedProperty1, "StaticAndInstance").WithArguments("Signatures.StaticAndInstance"),
                Diagnostic(ERRID.ERR_UnsupportedProperty1, "GetUsedAsSet").WithArguments("Signatures.GetUsedAsSet"),
                Diagnostic(ERRID.ERR_NameNotMember2, "Mismatched.[Static]").WithArguments("Static", "Mismatched"),
                Diagnostic(ERRID.ERR_NameNotMember2, "Mismatched.[Static]").WithArguments("Static", "Mismatched"))
        End Sub

        ''' <summary>
        ''' Calling bogus methods directly should not be allowed.
        ''' </summary>
        <Fact>
        Public Sub CallMethodsDirectly()
            Dim source = <compilation><file name="c.vb"><![CDATA[
Class Program
    Private Shared Sub M(i As Mismatched)
        i.InstanceBoolSet(False)
        System.Console.Write("{0}", i.InstanceInt32Get())
    End Sub
    Private Shared Sub Main()
        Mismatched.StaticBoolSet(False)
        System.Console.Write("{0}", Mismatched.StaticInt32Get())
    End Sub
End Class
]]></file></compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndReferences(source, {s_propertiesDll})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30456: 'InstanceBoolSet' is not a member of 'Mismatched'.
        i.InstanceBoolSet(False)
        ~~~~~~~~~~~~~~~~~
BC30456: 'InstanceInt32Get' is not a member of 'Mismatched'.
        System.Console.Write("{0}", i.InstanceInt32Get())
                                    ~~~~~~~~~~~~~~~~~~
BC30456: 'StaticBoolSet' is not a member of 'Mismatched'.
        Mismatched.StaticBoolSet(False)
        ~~~~~~~~~~~~~~~~~~~~~~~~
BC30456: 'StaticInt32Get' is not a member of 'Mismatched'.
        System.Console.Write("{0}", Mismatched.StaticInt32Get())
                                    ~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub MethodsReferencedInMultipleProperties()
            Dim source = <compilation><file name="c.vb"><![CDATA[
        Class Program
            Private Shared Sub M(i As Signatures)
                i.GoodInstance = 0
                System.Console.Write("{0}", i.GoodInstance)
            End Sub
            Public Shared Sub Main()
                Signatures.GoodStatic = 0
                System.Console.Write("{0}", Signatures.GoodStatic)
            End Sub
        End Class
        ]]></file></compilation>
            Dim result = CompileAndVerify(source, additionalRefs:={s_propertiesDll}, expectedOutput:="0")
            Dim ilSource = <![CDATA[{
// Code size       27 (0x1b)
.maxstack  2
IL_0000:  ldc.i4.0
IL_0001:  call       "Sub Signatures.StaticSet(Integer)"
IL_0006:  ldstr      "{0}"
IL_000b:  call       "Function Signatures.StaticGet() As Integer"
IL_0010:  box        "Integer"
IL_0015:  call       "Sub System.Console.Write(String, Object)"
IL_001a:  ret
}
]]>

            result.VerifyIL("Program.Main", ilSource)

            Dim compilation = CompileWithCustomPropertiesAssembly(source)
            Dim type = DirectCast(compilation.GlobalNamespace.GetMembers("Signatures").Single(), PENamedTypeSymbol)

            ' Valid static property, property with signature that does not match accessors,
            ' and property with accessors that do not match each other.
            Dim goodStatic = DirectCast(type.GetMembers("GoodStatic").Single(), PEPropertySymbol)
            Dim badStatic = DirectCast(type.GetMembers("BadStatic").Single(), PEPropertySymbol)
            Dim mismatchedStatic = DirectCast(type.GetMembers("MismatchedStatic").Single(), PEPropertySymbol)

            Assert.Null(goodStatic.GetUseSiteErrorInfo())
            Assert.Null(badStatic.GetUseSiteErrorInfo()) ' Mismatch based on property type is supported
            Assert.Null(mismatchedStatic.GetUseSiteErrorInfo()) ' Mismatch based on property type is supported

            VerifyAccessor(goodStatic.GetMethod, goodStatic, MethodKind.PropertyGet)
            VerifyAccessor(goodStatic.SetMethod, goodStatic, MethodKind.PropertySet)
            VerifyAccessor(badStatic.GetMethod, goodStatic, MethodKind.PropertyGet)
            VerifyAccessor(badStatic.SetMethod, goodStatic, MethodKind.PropertySet)
            VerifyAccessor(mismatchedStatic.GetMethod, goodStatic, MethodKind.PropertyGet)
            VerifyAccessor(mismatchedStatic.SetMethod, mismatchedStatic, MethodKind.PropertySet)

            ' Valid instance property, property with signature that does not match accessors,
            ' and property with accessors that do not match each other.
            Dim goodInstance = DirectCast(type.GetMembers("GoodInstance").Single(), PEPropertySymbol)
            Dim badInstance = DirectCast(type.GetMembers("BadInstance").Single(), PEPropertySymbol)
            Dim mismatchedInstance = DirectCast(type.GetMembers("MismatchedInstance").Single(), PEPropertySymbol)

            Assert.Null(goodInstance.GetUseSiteErrorInfo())
            Assert.Null(badInstance.GetUseSiteErrorInfo()) ' Mismatch based on property type is supported
            Assert.Null(mismatchedInstance.GetUseSiteErrorInfo()) ' Mismatch based on property type is supported

            VerifyAccessor(goodInstance.GetMethod, goodInstance, MethodKind.PropertyGet)
            VerifyAccessor(goodInstance.SetMethod, goodInstance, MethodKind.PropertySet)
            VerifyAccessor(badInstance.GetMethod, goodInstance, MethodKind.PropertyGet)
            VerifyAccessor(badInstance.SetMethod, goodInstance, MethodKind.PropertySet)
            VerifyAccessor(mismatchedInstance.GetMethod, goodInstance, MethodKind.PropertyGet)
            VerifyAccessor(mismatchedInstance.SetMethod, mismatchedInstance, MethodKind.PropertySet)

            ' Mix of static and instance accessors.
            Dim staticAndInstance = DirectCast(type.GetMembers("StaticAndInstance").Single(), PEPropertySymbol)
            VerifyAccessor(staticAndInstance.GetMethod, goodStatic, MethodKind.PropertyGet)
            VerifyAccessor(staticAndInstance.SetMethod, goodInstance, MethodKind.PropertySet)
            Assert.Equal(ERRID.ERR_UnsupportedProperty1, staticAndInstance.GetUseSiteErrorInfo().Code)

            ' Property with get and set accessors both referring to the same get method.
            Dim getUsedAsSet = DirectCast(type.GetMembers("GetUsedAsSet").Single(), PEPropertySymbol)
            VerifyAccessor(getUsedAsSet.GetMethod, goodInstance, MethodKind.PropertyGet)
            VerifyAccessor(getUsedAsSet.SetMethod, goodInstance, MethodKind.PropertyGet)
            Assert.Equal(ERRID.ERR_UnsupportedProperty1, getUsedAsSet.GetUseSiteErrorInfo().Code)
        End Sub
#End Region
#End Region

        <WorkItem(540343, "DevDiv")>
        <Fact>
        Public Sub PropertiesWithCircularTypeReferences()
            CompileAndVerify(
<compilation>
    <file name="Cobj010mod.vb">
Module Cobj010mod
    Class Class1
        Public Property c2 As Class2
    End Class
    Class Class2
        Public Property c3 As Class3
    End Class
    Class Class3
        Public Property c4 As Class4
    End Class
    Class Class4
        Public Property c5 As Class5
    End Class
    Class Class5
        Public Property c6 As Class6
    End Class
    Class Class6
        Public Property c7 As Class7
    End Class
    Class Class7
        Public Property c8 As Class8
    End Class
    Class Class8
        Public Property c1 As Class1
    End Class

    Sub Main()
        Dim c1 As New Class1()

        if c1 is nothing
            c1.c2.c3.c4.c5.c6.c7.c8.c1.c2.c3.c4.c5.c6.c7.c8.c1.c2.c3.c4.c5.c6.c7.c8.c1.c2.c3.c4.c5.c6.c7.c8 = New Class8()
        end if
    End Sub
End Module
</file>
</compilation>, expectedOutput:="")
        End Sub

        <WorkItem(540342, "DevDiv")>
        <Fact>
        Public Sub NoSequencePointsForAutoPropertyAccessors()
            Dim source =
<compilation>
    <file name="c.vb">
Class C
    Property P
End Class
    </file>
</compilation>
            CompileAndVerify(source, options:=TestOptions.ReleaseDll).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub MultipleOverloadsMetadataName1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="b.vb">
Class Base
    Public ReadOnly Property BANANa(x as string, y as integer) as integer
        Get
            return 1
        End Get
    End Property
End Class

Partial Class Class1
    Inherits Base
    Public ReadOnly Property baNana()
        Get
            return 1
        End Get
    End Property
    Public ReadOnly Property Banana(x as integer)
        Get
            return 1
        End Get
    End Property
End Class
    </file>
    <file name="a.vb">
Partial Class Class1
    Public ReadOnly Property baNANa(xyz as String)
        Get
            return 1
        End Get
    End Property
    Public ReadOnly Property BANANA(x as Long)
        Get
            return 1
        End Get
    End Property
End Class
    </file>
</compilation>)
            ' No "Overloads", so all properties should match first overloads in first source file
            Dim class1 = compilation.GetTypeByMetadataName("Class1")
            Dim allProperties = class1.GetMembers("baNana").OfType(Of PropertySymbol)()

            ' All properties in Class1 should have metadata name "baNana" (first spelling, by source position).
            Dim count = 0
            For Each m In allProperties
                count = count + 1
                Assert.Equal("baNana", m.MetadataName)
                If m.Parameters.Any Then
                    Assert.NotEqual("baNana", m.Name)
                End If
            Next
            Assert.Equal(4, count)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub MultipleOverloadsMetadataName2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="b.vb">
Class Base
    Public ReadOnly Property BANANa(x as string, y as integer)
        Get
            return 1
        End Get
    End Property
End Class

Partial Class Class1
    Inherits Base
    Overloads Public ReadOnly Property baNana()
        Get
            return 1
        End Get
    End Property
    Overloads Public ReadOnly Property Banana(x as integer)
        Get
            return 1
        End Get
    End Property
End Class
    </file>
    <file name="a.vb">
Partial Class Class1
    Overloads Public ReadOnly Property baNANa(xyz as String)
        Get
            return 1
        End Get
    End Property
    Overloads Public ReadOnly Property BANANA(x as Long)
        Get
            return 1
        End Get
    End Property
End Class
    </file>
</compilation>)
            ' "Overloads" specified, so all properties should match method in base
            Dim class1 = compilation.GetTypeByMetadataName("Class1")
            Dim allProperties = class1.GetMembers("baNANa").OfType(Of PropertySymbol)()

            ' All properties in Class1 should have metadata name "baNANa".
            Dim count = 0
            For Each m In allProperties
                count = count + 1
                Assert.Equal("BANANa", m.MetadataName)
            Next
            Assert.Equal(4, count)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub MultipleOverloadsMetadataName3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="b.vb">
Class Base
    Overridable Public ReadOnly Property BANANa(x as string, y as integer)
        Get
            return 1
        End Get
    End Property
End Class

Partial Class Class1
    Inherits Base
    Overloads Public ReadOnly Property baNana()
        Get
            return 1
        End Get
    End Property
    Overrides Public ReadOnly Property baNANa(xyz as String, a as integer)
        Get
            return 1
        End Get
    End Property
    Overloads Public ReadOnly Property Banana(x as integer)
        Get
            return 1
        End Get
    End Property
End Class
    </file>
    <file name="a.vb">
Partial Class Class1
    Overloads Public ReadOnly Property BANANA(x as Long)
        Get
            return 1
        End Get
    End Property
End Class
    </file>
</compilation>)
            ' "Overrides" specified, so all properties should match property in base
            Dim class1 = compilation.GetTypeByMetadataName("Class1")
            Dim allProperties = class1.GetMembers("baNANa").OfType(Of PropertySymbol)()

            ' All properties in Class1 should have metadata name "BANANa".
            Dim count = 0
            For Each m In allProperties
                count = count + 1
                Assert.Equal("BANANa", m.MetadataName)
            Next
            Assert.Equal(4, count)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub MultipleOverloadsMetadataName4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="b.vb">
Interface Base1
    ReadOnly Property BANANa(x as string, y as integer)
End Interface

Interface Base2
    ReadOnly Property BANANa(x as string, y as integer, z as Object)
End Interface

Interface Base3
    Inherits Base2
End Interface

Interface Interface1
    Inherits Base1, Base3
    Overloads ReadOnly Property baNana()
    Overloads ReadOnly Property baNANa(xyz as String, a as integer)
    Overloads ReadOnly Property Banana(x as integer)
End Interface
    </file>

</compilation>)
            ' "Overloads" specified, so all properties should match properties in base
            Dim interface1 = compilation.GetTypeByMetadataName("Interface1")
            Dim allProperties = interface1.GetMembers("baNANa").OfType(Of PropertySymbol)()

            CompilationUtils.AssertNoErrors(compilation)

            ' All methods in Interface1 should have metadata name "BANANa".
            Dim count = 0
            For Each m In allProperties
                count = count + 1
                Assert.Equal("BANANa", m.MetadataName)
            Next
            Assert.Equal(3, count)

        End Sub

        <Fact>
        Public Sub MultipleOverloadsMetadataName5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="b.vb">
Interface Base1
    ReadOnly Property BAnANa(x as string, y as integer)
End Interface

Interface Base2
    ReadOnly Property BANANa(x as string, y as integer, z as Object)
End Interface

Interface Base3
    Inherits Base2
End Interface

Interface Interface1
    Inherits Base1, Base3
    Overloads ReadOnly Property baNana()
    Overloads ReadOnly Property baNANa(xyz as String, a as integer)
    Overloads ReadOnly Property Banana(x as integer)
End Interface
    </file>

</compilation>)
            ' "Overloads" specified, but base properties have multiple casing, so don't use it.
            Dim interface1 = compilation.GetTypeByMetadataName("Interface1")
            Dim allProperties = interface1.GetMembers("baNANa").OfType(Of PropertySymbol)()

            CompilationUtils.AssertNoErrors(compilation)

            ' All methods in Interface1 should have metadata name "baNana".
            Dim count = 0
            For Each m In allProperties
                count = count + 1
                Assert.Equal("baNana", m.MetadataName)
            Next
            Assert.Equal(3, count)

        End Sub

        <Fact()>
        Public Sub AutoImplementedAccessorAreImplicitlyDeclared()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="b.vb">
MustInherit Class A
    Public MustOverride Property P As Integer
    Public Property P2 As Integer
End Class

Interface I
    Property Q As Integer
End Interface
    </file>
</compilation>)

            ' Per design meeting (see bug 11253), in VB, if there's no "Get" or "Set" written,
            ' then IsImplicitlyDeclared should be tru.
            Dim globalNS = comp.GlobalNamespace
            Dim a = globalNS.GetTypeMembers("A", 0).Single()
            Dim i = globalNS.GetTypeMembers("I", 0).Single()
            Dim p = TryCast(a.GetMembers("P").AsEnumerable().SingleOrDefault(), PropertySymbol)
            Assert.True(p.GetMethod.IsImplicitlyDeclared)
            Assert.True(p.SetMethod.IsImplicitlyDeclared)
            p = TryCast(a.GetMembers("P2").SingleOrDefault(), PropertySymbol)
            Assert.True(p.GetMethod.IsImplicitlyDeclared)
            Assert.True(p.SetMethod.IsImplicitlyDeclared)
            Dim q = TryCast(i.GetMembers("Q").AsEnumerable().SingleOrDefault(), PropertySymbol)
            Assert.True(q.GetMethod.IsImplicitlyDeclared)
            Assert.True(q.SetMethod.IsImplicitlyDeclared)
        End Sub

        <Fact(), WorkItem(544315, "DevDiv")>
        Public Sub PropertyAccessorParameterLocation()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="b.vb">
Imports System

Public Class A
    Public Default ReadOnly Property Prop(ByVal p1 As Integer) As String
        Get
            Return "passed"
        End Get
    End Property
End Class
    </file>
</compilation>)

            Dim globalNS = comp.SourceModule.GlobalNamespace
            Dim a = globalNS.GetTypeMembers("A").Single()
            Dim p = TryCast(a.GetMembers("Prop").Single(), PropertySymbol)
            Dim paras = p.Parameters
            Assert.Equal(1, paras.Length)
            Dim p1 = paras(0)
            Assert.Equal("p1", p1.Name)
            Assert.Equal(1, p1.Locations.Length)

            Assert.Equal(1, p.GetMethod.Parameters.Length)
            Dim p11 = p.GetMethod.Parameters(0)
            Assert.False(p11.Locations.IsEmpty, "Parameter Location NotEmpty")
            Assert.True(p11.Locations(0).IsInSource, "Parameter Location(0) IsInSource")
            Assert.Equal(p1.Locations(0), p11.Locations(0))
        End Sub

        ''' <summary>
        ''' Consistent accessor signatures but different
        ''' from property signature.
        ''' </summary>
        <WorkItem(545814, "DevDiv")>
        <Fact()>
        Public Sub DifferentSignatures_AccessorsConsistent()
            Dim source1 = <![CDATA[
.class public A
{
  .method public instance object get_P1(object& i) { ldnull ret }
  .method public instance void set_P1(object& i, object& v) { ret }
  .method public instance object get_P2(object& i) { ldnull ret }
  .method public instance void set_P3(object& i, object& v) { ret }
  .property instance object P1(object)
  {
    .get instance object A::get_P1(object& i)
    .set instance void A::set_P1(object& i, object& v)
  }
  .property instance object P2(object)
  { 
    .get instance object A::get_P2(object& i)
  }
  .property instance object P3(object)
  {
    .set instance void A::set_P3(object& i, object& v)
  }
}
]]>.Value
            Dim reference1 = CompileIL(source1)
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M(o As A, i As Object)
        o.P1(i) = o.P1(i)
        o.P3(i) = o.P2(i)
        F(o.P1(i))
    End Sub
    Sub F(ByRef o As Object)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertNoErrors()
            Dim compilationVerifier = CompileAndVerify(compilation2)
            compilationVerifier.VerifyIL("M.M(A, Object)",
            <![CDATA[
{
  // Code size      131 (0x83)
  .maxstack  4
  .locals init (Object V_0,
  Object V_1,
  Object V_2,
  Object V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldarg.0
  IL_000b:  ldarg.1
  IL_000c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0011:  stloc.2
  IL_0012:  ldloca.s   V_2
  IL_0014:  callvirt   "Function A.get_P1(ByRef Object) As Object"
  IL_0019:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_1
  IL_0021:  callvirt   "Sub A.set_P1(ByRef Object, ByRef Object)"
  IL_0026:  ldarg.0
  IL_0027:  ldarg.1
  IL_0028:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_002d:  stloc.1
  IL_002e:  ldloca.s   V_1
  IL_0030:  ldarg.0
  IL_0031:  ldarg.1
  IL_0032:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0037:  stloc.2
  IL_0038:  ldloca.s   V_2
  IL_003a:  callvirt   "Function A.get_P2(ByRef Object) As Object"
  IL_003f:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0044:  stloc.0
  IL_0045:  ldloca.s   V_0
  IL_0047:  callvirt   "Sub A.set_P3(ByRef Object, ByRef Object)"
  IL_004c:  ldarg.0
  IL_004d:  dup
  IL_004e:  ldarg.1
  IL_004f:  dup
  IL_0050:  stloc.0
  IL_0051:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0056:  stloc.2
  IL_0057:  ldloca.s   V_2
  IL_0059:  callvirt   "Function A.get_P1(ByRef Object) As Object"
  IL_005e:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0063:  stloc.1
  IL_0064:  ldloca.s   V_1
  IL_0066:  call       "Sub M.F(ByRef Object)"
  IL_006b:  ldloc.0
  IL_006c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0071:  stloc.2
  IL_0072:  ldloca.s   V_2
  IL_0074:  ldloc.1
  IL_0075:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_007a:  stloc.3
  IL_007b:  ldloca.s   V_3
  IL_007d:  callvirt   "Sub A.set_P1(ByRef Object, ByRef Object)"
  IL_0082:  ret
}
]]>)
            ' Accessor signature should be used for binding
            ' rather than property signature.
            Dim source3 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M(o As A)
        Dim v As Integer = o.P1(1)
        v = o.P2(2)
        o.P3(3) = v
        F(o.P1(1))
    End Sub
    Sub F(ByRef o As Integer)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source3, {reference1})
            compilation3.AssertTheseDiagnostics(<errors><![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Integer'.
        Dim v As Integer = o.P1(1)
                           ~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Integer'.
        v = o.P2(2)
            ~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Integer'.
        F(o.P1(1))
          ~~~~~~~
]]></errors>)
        End Sub

        ''' <summary>
        ''' Different accessor signatures and different accessor and
        ''' property signatures. (Both are supported by Dev11, but
        ''' Roslyn requires accessors to have consistent signatures.)
        ''' </summary>
        <WorkItem(545814, "DevDiv")>
        <Fact()>
        Public Sub DifferentSignatures_AccessorsDifferent()
            Dim source1 = <![CDATA[
.class public A { }
.class public B { }
.class public C { }
.class public D
{
  .method public instance class B get_P(class A i) { ldnull ret }
  .method public instance void set_P(class B i, class C v) { ret }
  .property instance class A P(class C)
  {
    .get instance class B D::get_P(class A)
    .set instance void D::set_P(class B, class C)
  }
}
]]>.Value
            Dim reference1 = CompileIL(source1)
            ' Accessor method calls.
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M(o As D, x As A, y As B, z As C)
        ' get_P signature.
        y = o.P(x)
        o.P(x) = y
        ' set_P signature.
        z = o.P(y)
        o.P(y) = z
        ' P signature.
        x = o.P(z)
        o.P(z) = x
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC30643: Property 'D.P(i As C)' is of an unsupported type.
        y = o.P(x)
              ~
BC30643: Property 'D.P(i As C)' is of an unsupported type.
        o.P(x) = y
          ~
BC30643: Property 'D.P(i As C)' is of an unsupported type.
        z = o.P(y)
              ~
BC30643: Property 'D.P(i As C)' is of an unsupported type.
        o.P(y) = z
          ~
BC30643: Property 'D.P(i As C)' is of an unsupported type.
        x = o.P(z)
              ~
BC30643: Property 'D.P(i As C)' is of an unsupported type.
        o.P(z) = x
          ~
]]></errors>)
            ' Property references.
            Dim source3 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M(o As D, x As A)
        MBByVal(o.P(x))
        MBByRef(o.P(x))
        MCByVal(o.P(x))
        MCByRef(o.P(x))
    End Sub
    Sub MBByVal(y As B)
    End Sub
    Sub MBByRef(ByRef y As B)
    End Sub
    Sub MCByVal(z As C)
    End Sub
    Sub MCByRef(ByRef z As C)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source3, {reference1})
            compilation3.AssertTheseDiagnostics(<errors><![CDATA[
BC30643: Property 'D.P(i As C)' is of an unsupported type.
        MBByVal(o.P(x))
                  ~
BC30643: Property 'D.P(i As C)' is of an unsupported type.
        MBByRef(o.P(x))
                  ~
BC30643: Property 'D.P(i As C)' is of an unsupported type.
        MCByVal(o.P(x))
                  ~
BC30643: Property 'D.P(i As C)' is of an unsupported type.
        MCByRef(o.P(x))
                  ~
]]></errors>)
        End Sub

        ''' <summary>
        ''' Properties used in object initializers and attributes.
        ''' </summary>
        <WorkItem(545814, "DevDiv")>
        <Fact()>
        Public Sub DifferentSignatures_ObjectInitializersAndAttributes()
            Dim source1 = <![CDATA[
.class public A extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public instance int32 get_P() { ldc.i4.0 ret }
  .method public instance void set_P(int32 v) { ret }
  .property object P()
  {
    .get instance int32 A::get_P()
    .set instance void A::set_P(int32 v)
  }
}
]]>.Value
            Dim reference1 = CompileIL(source1)
            ' Object initializer.
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Private F As New A With {.P = ""}
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Integer'.
    Private F As New A With {.P = ""}
                                  ~~
]]></errors>)
            ' Attribute. Dev11 no errors.
            Dim source3 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
<A(P:="")>
Class C
End Class
]]>
                    </file>
                </compilation>
            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source3, {reference1})
            compilation3.AssertTheseDiagnostics(<errors><![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Integer'.
<A(P:="")>
      ~~
BC30934: Conversion from 'String' to 'Integer' cannot occur in a constant expression used as an argument to an attribute.
<A(P:="")>
      ~~
]]></errors>)
        End Sub

        ''' <summary>
        ''' Overload resolution prefers supported properties over unsupported
        ''' properties. Since we're marking properties with inconsistent signatures
        ''' as unsupported, this can lead to different overload resolution than Dev11.
        ''' </summary>
        ''' <remarks></remarks>
        <WorkItem(545814, "DevDiv")>
        <Fact()>
        Public Sub DifferentSignatures_OverloadResolution()
            Dim source1 = <![CDATA[
.class public A { }
.class public B extends A { }
.class public C
{
  .method public instance int32 get_P(class A o) { ldnull ret }
  .method public instance void set_P(class A o, int32 v) { ret }
  .method public instance int32 get_P(object o) { ldnull ret }
  .method public instance void set_P(object o, int32 v) { ret }
  // Property indexed by A, accessors indexed by A.
  .property instance int32 P(class A)
  {
    .get instance int32 C::get_P(class A o)
    .set instance void C::set_P(class A o, int32 v)
  }
  // Property indexed by B, accessors indexed by object.
  .property instance int32 P(class B)
  {
    .get instance int32 C::get_P(object o)
    .set instance void C::set_P(object o, int32 v)
  }
}
]]>.Value
            Dim reference1 = CompileIL(source1)
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M(_a As A, _b As B, o As C)
        o.P(_a) += 1 ' Dev11: P(A); Roslyn: P(A)
        o.P(_b) += 1 ' Dev11: P(B); Roslyn: P(A)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertNoErrors()
            Dim compilationVerifier = CompileAndVerify(compilation2)
            compilationVerifier.VerifyIL("M.M(A, B, C)",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  4
  .locals init (C V_0,
  A V_1)
  IL_0000:  ldarg.2
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  ldarg.0
  IL_0004:  dup
  IL_0005:  stloc.1
  IL_0006:  ldloc.0
  IL_0007:  ldloc.1
  IL_0008:  callvirt   "Function C.get_P(A) As Integer"
  IL_000d:  ldc.i4.1
  IL_000e:  add.ovf
  IL_000f:  callvirt   "Sub C.set_P(A, Integer)"
  IL_0014:  ldarg.2
  IL_0015:  dup
  IL_0016:  stloc.0
  IL_0017:  ldarg.1
  IL_0018:  dup
  IL_0019:  stloc.1
  IL_001a:  ldloc.0
  IL_001b:  ldloc.1
  IL_001c:  callvirt   "Function C.get_P(A) As Integer"
  IL_0021:  ldc.i4.1
  IL_0022:  add.ovf
  IL_0023:  callvirt   "Sub C.set_P(A, Integer)"
  IL_0028:  ret
}
]]>)
        End Sub

        ''' <summary>
        ''' Accessors with different parameter count than property.
        ''' </summary>
        <WorkItem(545814, "DevDiv")>
        <Fact()>
        Public Sub DifferentSignatures_ParameterCount()
            Dim source1 = <![CDATA[
.class public A
{
  .method public instance int32 get_P(object x, object y) { ldnull ret }
  .method public instance void set_P(object x, object y, int32 v) { ret }
  .method public instance int32 get_Q(object o) { ldnull ret }
  .method public instance void set_Q(object o, int32 v) { ret }
  // Property with fewer arguments than accessors.
  .property instance int32 P(object)
  {
    .get instance int32 A::get_P(object x, object y)
    .set instance void A::set_P(object x, object y, int32 v)
  }
  // Property with more arguments than accessors.
  .property instance int32 Q(object, object)
  {
    .get instance int32 A::get_Q(object o)
    .set instance void A::set_Q(object o, int32 v)
  }
}
]]>.Value
            Dim reference1 = CompileIL(source1)
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M(o As A, x As Object, y As Object)
        o.P(x) = o.P(x)
        o.P(x, y) = o.P(x, y)
        o.Q(x) += 1
        o.Q(x, y) += 1
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC30643: Property 'A.P(x As Object)' is of an unsupported type.
        o.P(x) = o.P(x)
          ~
BC30643: Property 'A.P(x As Object)' is of an unsupported type.
        o.P(x) = o.P(x)
                   ~
BC30643: Property 'A.P(x As Object)' is of an unsupported type.
        o.P(x, y) = o.P(x, y)
          ~
BC30643: Property 'A.P(x As Object)' is of an unsupported type.
        o.P(x, y) = o.P(x, y)
                      ~
BC30643: Property 'A.Q(o As Object, v As Object)' is of an unsupported type.
        o.Q(x) += 1
          ~
BC30643: Property 'A.Q(o As Object, v As Object)' is of an unsupported type.
        o.Q(x, y) += 1
          ~
]]></errors>)
        End Sub

        <WorkItem(545959, "DevDiv")>
        <Fact()>
        Public Sub DifferentAccessorSignatures_NamedArguments_1()
            Dim ilSource = <![CDATA[
.class abstract public A
{
  .method public hidebysig specialname rtspecialname instance void .ctor()
  {
    ldarg.0
	call instance void [mscorlib]System.Object::.ctor()
    ret
  }
  .method public abstract virtual instance void get(object x, object y)
  {
  }
  .method public abstract virtual instance void set(object x, object y)
  {
  }
  .method public instance int32 get_P(object x, object y)
  {
    ldarg.0
    ldarg.1
    ldarg.2
    callvirt instance void A::get(object, object)
    ldnull
    ret
  }
  .method public instance void set_P(object y, object x, int32 v)
  {
    ldarg.0
    ldarg.1
    ldarg.2
    callvirt instance void A::set(object, object)
    ret
  }
  .property instance int32 P(object, object)
  {
    .get instance int32 A::get_P(object, object)
    .set instance void A::set_P(object, object, int32)
  }
}
]]>.Value
            Dim reference = CompileIL(ilSource)
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Class B
    Inherits A
    Public Overrides Sub [get](x As Object, y As Object)
        System.Console.WriteLine("get {0}, {1}", x, y)
    End Sub
    Public Overrides Sub [set](x As Object, y As Object)
        System.Console.WriteLine("set {0}, {1}", x, y)
    End Sub
End Class
Module M
    Sub Main()
        Dim o = New B()
        o.P(1, 2) *= 1
        o.P(x:=3, y:=4) *= 1
        M(o.P(x:=5, y:=6))
    End Sub
    Sub M(ByRef i As Integer)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(vbSource, additionalRefs:={reference})
            compilation.AssertTheseDiagnostics(
<expected>
BC30455: Argument not specified for parameter 'Param' of 'Public Property P(Param As Object, Param As Object) As Integer'.
        o.P(x:=3, y:=4) *= 1
          ~
BC30455: Argument not specified for parameter 'Param' of 'Public Property P(Param As Object, Param As Object) As Integer'.
        o.P(x:=3, y:=4) *= 1
          ~
BC30272: 'x' is not a parameter of 'Public Property P(Param As Object, Param As Object) As Integer'.
        o.P(x:=3, y:=4) *= 1
            ~
BC30272: 'y' is not a parameter of 'Public Property P(Param As Object, Param As Object) As Integer'.
        o.P(x:=3, y:=4) *= 1
                  ~
BC30455: Argument not specified for parameter 'Param' of 'Public Property P(Param As Object, Param As Object) As Integer'.
        M(o.P(x:=5, y:=6))
            ~
BC30455: Argument not specified for parameter 'Param' of 'Public Property P(Param As Object, Param As Object) As Integer'.
        M(o.P(x:=5, y:=6))
            ~
BC30272: 'x' is not a parameter of 'Public Property P(Param As Object, Param As Object) As Integer'.
        M(o.P(x:=5, y:=6))
              ~
BC30272: 'y' is not a parameter of 'Public Property P(Param As Object, Param As Object) As Integer'.
        M(o.P(x:=5, y:=6))
                    ~
</expected>)
        End Sub

        ''' <summary>
        ''' Named arguments that differ by case.
        ''' </summary>
        <Fact()>
        Public Sub DifferentAccessorSignatures_NamedArguments_2()
            Dim ilSource = <![CDATA[
.class public A
{
  .method public instance int32 get_P(object one, object two) { ldnull ret }
  .method public instance void set_P(object ONE, object _two, int32 v) { ret }
  .property instance int32 P(object, object)
  {
    .get instance int32 A::get_P(object one, object two)
    .set instance void A::set_P(object ONE, object _two, int32 v)
  }
}
]]>.Value
            Dim reference = CompileIL(ilSource)
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M(o As A)
        o.P(1, 2) += 1
        o.P(one:=1, two:=2) += 1
        o.P(ONE:=1, _two:=2) += 1
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(vbSource, additionalRefs:={reference})
            compilation.AssertTheseDiagnostics(
<expected>
BC30455: Argument not specified for parameter 'Param' of 'Public Property P(one As Object, Param As Object) As Integer'.
        o.P(one:=1, two:=2) += 1
          ~
BC30272: 'two' is not a parameter of 'Public Property P(one As Object, Param As Object) As Integer'.
        o.P(one:=1, two:=2) += 1
                    ~~~
BC30455: Argument not specified for parameter 'Param' of 'Public Property P(one As Object, Param As Object) As Integer'.
        o.P(ONE:=1, _two:=2) += 1
          ~
BC30272: '_two' is not a parameter of 'Public Property P(one As Object, Param As Object) As Integer'.
        o.P(ONE:=1, _two:=2) += 1
                    ~~~~
</expected>)
        End Sub

        ''' <summary>
        ''' ByRef must be consistent between accessor parameters.
        ''' Note: Dev11 does not require this.
        ''' </summary>
        <Fact()>
        Public Sub DifferentAccessorSignatures_ByRef()
            Dim ilSource = <![CDATA[
.class public A1
{
  .method public instance object get_P(object i) { ldnull ret }
  .method public instance void set_P(object i, object v) { ret }
  .property instance object P(object)
  {
    .get instance object A1::get_P(object)
    .set instance void A1::set_P(object, object v)
  }
}
.class public A2
{
  .method public instance object get_P(object i) { ldnull ret }
  .method public instance void set_P(object& i, object v) { ret }
  .property instance object P(object)
  {
    .get instance object A2::get_P(object)
    .set instance void A2::set_P(object&, object v)
  }
}
.class public A3
{
  .method public instance object get_P(object i) { ldnull ret }
  .method public instance void set_P(object i, object& v) { ret }
  .property instance object P(object)
  {
    .get instance object A3::get_P(object)
    .set instance void A3::set_P(object, object& v)
  }
}
.class public A4
{
  .method public instance object& get_P(object i) { ldnull ret }
  .method public instance void set_P(object i, object v) { ret }
  .property instance object& P(object)
  {
    .get instance object& A4::get_P(object)
    .set instance void A4::set_P(object, object v)
  }
}
.class public A5
{
  .method public instance object& get_P(object i) { ldnull ret }
  .method public instance void set_P(object& i, object v) { ret }
  .property instance object& P(object)
  {
    .get instance object& A5::get_P(object)
    .set instance void A5::set_P(object&, object v)
  }
}
.class public A6
{
  .method public instance object& get_P(object i) { ldnull ret }
  .method public instance void set_P(object i, object& v) { ret }
  .property instance object& P(object)
  {
    .get instance object& A6::get_P(object)
    .set instance void A6::set_P(object, object& v)
  }
}
.class public A7
{
  .method public instance object get_P(object& i) { ldnull ret }
  .method public instance void set_P(object i, object v) { ret }
  .property instance object P(object&)
  {
    .get instance object A7::get_P(object&)
    .set instance void A7::set_P(object, object v)
  }
}
.class public A8
{
  .method public instance object get_P(object& i) { ldnull ret }
  .method public instance void set_P(object& i, object v) { ret }
  .property instance object P(object&)
  {
    .get instance object A8::get_P(object&)
    .set instance void A8::set_P(object&, object v)
  }
}
.class public A9
{
  .method public instance object get_P(object& i) { ldnull ret }
  .method public instance void set_P(object i, object& v) { ret }
  .property instance object P(object&)
  {
    .get instance object A9::get_P(object&)
    .set instance void A9::set_P(object, object& v)
  }
}
]]>.Value
            Dim reference = CompileIL(ilSource)
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M(_1 As A1, _2 As A2, _3 As A3, _4 As A4, _5 As A5, _6 As A6, _7 As A7, _8 As A8, _9 As A9)
        Dim x As Object = Nothing
        Dim y As Object = Nothing
        _1.P(y) = _1.P(x)
        _2.P(y) = _2.P(x)
        _3.P(y) = _3.P(x)
        _4.P(y) = _4.P(x)
        _5.P(y) = _5.P(x)
        _6.P(y) = _6.P(x)
        _7.P(y) = _7.P(x)
        _8.P(y) = _8.P(x)
        _9.P(y) = _9.P(x)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(vbSource, additionalRefs:={reference})
            compilation.AssertTheseDiagnostics(
<expected>
BC30643: Property 'A2.P(ByRef i As Object)' is of an unsupported type.
        _2.P(y) = _2.P(x)
           ~
BC30643: Property 'A2.P(ByRef i As Object)' is of an unsupported type.
        _2.P(y) = _2.P(x)
                     ~
BC30643: Property 'P' is of an unsupported type.
        _4.P(y) = _4.P(x)
           ~
BC30643: Property 'P' is of an unsupported type.
        _4.P(y) = _4.P(x)
                     ~
BC30643: Property 'A5.P(ByRef i As Object)' is of an unsupported type.
        _5.P(y) = _5.P(x)
           ~
BC30643: Property 'A5.P(ByRef i As Object)' is of an unsupported type.
        _5.P(y) = _5.P(x)
                     ~
BC30643: Property 'P' is of an unsupported type.
        _6.P(y) = _6.P(x)
           ~
BC30643: Property 'P' is of an unsupported type.
        _6.P(y) = _6.P(x)
                     ~
BC30643: Property 'A7.P(ByRef i As Object)' is of an unsupported type.
        _7.P(y) = _7.P(x)
           ~
BC30643: Property 'A7.P(ByRef i As Object)' is of an unsupported type.
        _7.P(y) = _7.P(x)
                     ~
BC30643: Property 'A9.P(ByRef i As Object)' is of an unsupported type.
        _9.P(y) = _9.P(x)
           ~
BC30643: Property 'A9.P(ByRef i As Object)' is of an unsupported type.
        _9.P(y) = _9.P(x)
                     ~
</expected>)
        End Sub

        ''' <summary>
        ''' ParamArray must be consistent between accessor parameters.
        ''' Note: Dev11 does not require this.
        ''' </summary>
        <Fact()>
        Public Sub DifferentAccessorSignatures_ParamArray()
            Dim ilSource = <![CDATA[
.class public A
{
  .method public instance object get_NoParamArray(object[] i)
  {
    ldnull
	ret
  }
  .method public instance void set_NoParamArray(object[] i, object v)
  {
    ret
  }
  .method public instance object get_ParamArray(object[] i)
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    ldnull
	ret
  }
  .method public instance void set_ParamArray(object[] i, object v)
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    ret
  }
  // ParamArray on both accessors.
  .property instance object P1(object[])
  {
    .get instance object A::get_ParamArray(object[])
    .set instance void A::set_ParamArray(object[], object)
  }
  // ParamArray on getter only.
  .property instance object P2(object[])
  {
    .get instance object A::get_ParamArray(object[])
    .set instance void A::set_NoParamArray(object[], object)
  }
  // ParamArray on setter only.
  .property instance object P3(object[])
  {
    .get instance object A::get_NoParamArray(object[])
    .set instance void A::set_ParamArray(object[], object)
  }
  // ParamArray on readonly property.
  .property instance object P4(object[])
  {
    .get instance object A::get_ParamArray(object[])
  }
  // ParamArray on writeonly property.
  .property instance object P5(object[])
  {
    .set instance void A::set_ParamArray(object[], object)
  }
}
]]>.Value
            Dim reference = CompileIL(ilSource)
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Sub M(o As A)
        Dim v As Object
        Dim arg1 As Object = Nothing
        Dim arg2 As Object = Nothing
        Dim args = {arg1, arg2}
        v = o.P1(arg1, arg2)
        o.P1(arg1, arg2) = v
        v = o.P2(arg1, arg2)
        o.P2(arg1, arg2) = v
        v = o.P3(arg1, arg2)
        o.P3(arg1, arg2) = v
        v = o.P4(arg1, arg2)
        o.P5(arg1, arg2) = v
        v = o.P1(args)
        o.P1(args) = v
        v = o.P2(args)
        o.P2(args) = v
        v = o.P3(args)
        o.P3(args) = v
        v = o.P4(args)
        o.P5(args) = v
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(vbSource, additionalRefs:={reference})
            compilation.AssertTheseDiagnostics(
<expected>
BC30057: Too many arguments to 'Public Property P2(i As Object()) As Object'.
        v = o.P2(arg1, arg2)
                       ~~~~
BC30057: Too many arguments to 'Public Property P2(i As Object()) As Object'.
        o.P2(arg1, arg2) = v
                   ~~~~
BC30057: Too many arguments to 'Public Property P3(i As Object()) As Object'.
        v = o.P3(arg1, arg2)
                       ~~~~
BC30057: Too many arguments to 'Public Property P3(i As Object()) As Object'.
        o.P3(arg1, arg2) = v
                   ~~~~
</expected>)
            Dim type = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("A")
            Assert.True(type.GetMember(Of PropertySymbol)("P1").Parameters(0).IsParamArray)
            Assert.False(type.GetMember(Of PropertySymbol)("P2").Parameters(0).IsParamArray)
            Assert.False(type.GetMember(Of PropertySymbol)("P3").Parameters(0).IsParamArray)
            Assert.True(type.GetMember(Of PropertySymbol)("P4").Parameters(0).IsParamArray)
            Assert.True(type.GetMember(Of PropertySymbol)("P5").Parameters(0).IsParamArray)
        End Sub

        ''' <summary>
        ''' OptionCompare must be consistent between accessor parameters.
        ''' Note: Dev11 does not require this.
        ''' </summary>
        <Fact()>
        Public Sub DifferentAccessorSignatures_OptionCompare()
            Dim ilSource = <![CDATA[
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.class public A
{
  .method public instance object get_NoOptionCompare(object i)
  {
    ldnull
	ret
  }
  .method public instance void set_NoOptionCompare(object i, object v)
  {
    ret
  }
  .method public instance object get_OptionCompare(object i)
  {
    .param [1]
    .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute::.ctor() = ( 01 00 00 00 ) 
    ldnull
	ret
  }
  .method public instance void set_OptionCompare(object i, object v)
  {
    .param [1]
    .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute::.ctor() = ( 01 00 00 00 ) 
    ret
  }
  // OptionCompare on both accessors.
  .property instance object P1(object)
  {
    .get instance object A::get_OptionCompare(object)
    .set instance void A::set_OptionCompare(object, object)
  }
  // OptionCompare on getter only.
  .property instance object P2(object)
  {
    .get instance object A::get_OptionCompare(object)
    .set instance void A::set_NoOptionCompare(object, object)
  }
  // OptionCompare on setter only.
  .property instance object P3(object)
  {
    .get instance object A::get_NoOptionCompare(object)
    .set instance void A::set_OptionCompare(object, object)
  }
  // OptionCompare on readonly property.
  .property instance object P4(object)
  {
    .get instance object A::get_OptionCompare(object)
  }
  // OptionCompare on writeonly property.
  .property instance object P5(object)
  {
    .set instance void A::set_OptionCompare(object, object)
  }
}
]]>.Value
            Dim reference = CompileIL(ilSource)
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Sub M(o As A)
        Dim v As Object = Nothing
        v = o.P1(v)
        o.P1(v) = v
        v = o.P2(v)
        o.P2(v) = v
        v = o.P3(v)
        o.P3(v) = v
        v = o.P4(v)
        o.P5(v) = v
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(vbSource, additionalRefs:={reference})
            compilation.AssertTheseDiagnostics(
<expected>
BC30643: Property 'A.P2(i As Object)' is of an unsupported type.
        v = o.P2(v)
              ~~
BC30643: Property 'A.P2(i As Object)' is of an unsupported type.
        o.P2(v) = v
          ~~
BC30643: Property 'A.P3(i As Object)' is of an unsupported type.
        v = o.P3(v)
              ~~
BC30643: Property 'A.P3(i As Object)' is of an unsupported type.
        o.P3(v) = v
          ~~
</expected>)
            Dim type = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("A")
            Assert.True(type.GetMember(Of PropertySymbol)("P1").Parameters(0).HasOptionCompare)
            Assert.False(type.GetMember(Of PropertySymbol)("P2").Parameters(0).HasOptionCompare)
            Assert.False(type.GetMember(Of PropertySymbol)("P3").Parameters(0).HasOptionCompare)
            Assert.True(type.GetMember(Of PropertySymbol)("P4").Parameters(0).HasOptionCompare)
            Assert.True(type.GetMember(Of PropertySymbol)("P5").Parameters(0).HasOptionCompare)
        End Sub

        <Fact()>
        Public Sub OptionalParameterValues()
            Dim source1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Imports System
Public Class A
    Public ReadOnly Property P(x As Integer, Optional y As Integer = 1) As Integer
        Get
            Console.WriteLine("get_P: {0}", y)
            Return 0
        End Get
    End Property
    Public WriteOnly Property Q(x As Integer, Optional y As Integer = 2) As Integer
        Set(value As Integer)
            Console.WriteLine("set_Q: {0}", y)
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib(source1)
            Dim reference1 = MetadataReference.CreateFromImage(compilation1.EmitToArray())
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub Main()
        Dim o As A = New A()
        o.Q(2) = o.P(1)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CompileAndVerify(source2, additionalRefs:={reference1}, expectedOutput:=<![CDATA[
get_P: 1
set_Q: 2
]]>)
        End Sub

        <Fact()>
        Public Sub DifferentAccessorSignatures_Optional()
            Dim ilSource = <![CDATA[
.class public A
{
  .method public instance object get_NoOpt(object x, object y)
  {
    .param[2] = int32(1)
    ldnull
    ret
  }
  .method public instance void set_NoOpt(object x, object y, object v)
  {
    ret
  }
  .method public instance object get_Opt(object x, [opt] object y)
  {
    .param[2] = int32(1)
    ldnull
    ret
  }
  .method public instance void set_Opt(object x, [opt] object y, object v)
  {
    .param[2] = int32(1)
    ret
  }
  // Opt on both accessors.
  .property instance object P1(object, object)
  {
    .get instance object A::get_Opt(object, object)
    .set instance void A::set_Opt(object, object, object)
  }
  // Opt on getter only.
  .property instance object P2(object, object)
  {
    .get instance object A::get_Opt(object, object)
    .set instance void A::set_NoOpt(object, object, object)
  }
  // Opt on setter only.
  .property instance object P3(object, object)
  {
    .get instance object A::get_NoOpt(object, object)
    .set instance void A::set_Opt(object, object, object)
  }
  // Opt on readonly property.
  .property instance object P4(object, object)
  {
    .get instance object A::get_Opt(object, object)
  }
  // Opt on writeonly property.
  .property instance object P5(object, object)
  {
    .set instance void A::set_Opt(object, object, object)
  }
}
]]>.Value
            Dim reference = CompileIL(ilSource)
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Sub M(o As A)
        Dim v As Object = Nothing
        v = o.P1(v)
        o.P1(v) = v
        v = o.P2(v)
        o.P2(v) = v
        v = o.P3(v)
        o.P3(v) = v
        v = o.P4(v)
        o.P5(v) = v
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(vbSource, additionalRefs:={reference})
            compilation.AssertTheseDiagnostics(
<expected>
BC30455: Argument not specified for parameter 'y' of 'Public Property P2(x As Object, y As Object) As Object'.
        v = o.P2(v)
              ~~
BC30455: Argument not specified for parameter 'y' of 'Public Property P2(x As Object, y As Object) As Object'.
        o.P2(v) = v
          ~~
BC30455: Argument not specified for parameter 'y' of 'Public Property P3(x As Object, y As Object) As Object'.
        v = o.P3(v)
              ~~
BC30455: Argument not specified for parameter 'y' of 'Public Property P3(x As Object, y As Object) As Object'.
        o.P3(v) = v
          ~~
</expected>)
            Dim defaultValue = ConstantValue.Create(1)
            Dim type = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("A")
            Dim parameter As ParameterSymbol
            parameter = type.GetMember(Of PropertySymbol)("P1").Parameters(1)
            Assert.True(parameter.IsOptional)
            Assert.Equal(parameter.ExplicitDefaultConstantValue, defaultValue)
            parameter = type.GetMember(Of PropertySymbol)("P2").Parameters(1)
            Assert.False(parameter.IsOptional)
            Assert.Null(parameter.ExplicitDefaultConstantValue)
            parameter = type.GetMember(Of PropertySymbol)("P3").Parameters(1)
            Assert.False(parameter.IsOptional)
            Assert.Null(parameter.ExplicitDefaultConstantValue)
            parameter = type.GetMember(Of PropertySymbol)("P4").Parameters(1)
            Assert.True(parameter.IsOptional)
            Assert.Equal(parameter.ExplicitDefaultConstantValue, defaultValue)
            parameter = type.GetMember(Of PropertySymbol)("P5").Parameters(1)
            Assert.True(parameter.IsOptional)
            Assert.Equal(parameter.ExplicitDefaultConstantValue, defaultValue)
        End Sub

        <WorkItem(545959, "DevDiv")>
        <Fact()>
        Public Sub DistinctOptionalParameterValues()
            Dim ilSource = <![CDATA[
.class abstract public A
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('P')}
  .method public hidebysig specialname rtspecialname instance void .ctor()
  {
    ret
  }
  .method public abstract virtual instance int32 get_P(int32 x, [opt] int32 y)
  {
    .param[2] = int32(1)
  }
  .method public abstract virtual instance void set_P(int32 x, [opt] int32 y, int32 v)
  {
    .param[2] = int32(2)
  }
  .property instance int32 P(int32, int32)
  {
    .get instance int32 A::get_P(int32, int32)
    .set instance void A::set_P(int32, int32, int32)
  }
}
]]>.Value
            Dim reference = CompileIL(ilSource)
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Sub M(a As A)
        a(0) = a(0)
        a(1) += 1
        [ByRef](a.P(2))
    End Sub
    Sub [ByRef](ByRef o As Integer)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(vbSource, {reference})
            compilation.AssertTheseDiagnostics(
<expected>
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Default Property P(x As Integer, y As Integer) As Integer'.
        a(0) = a(0)
        ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Default Property P(x As Integer, y As Integer) As Integer'.
        a(0) = a(0)
               ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Default Property P(x As Integer, y As Integer) As Integer'.
        a(1) += 1
        ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Default Property P(x As Integer, y As Integer) As Integer'.
        [ByRef](a.P(2))
                  ~
</expected>)
        End Sub

        <WorkItem(545959, "DevDiv")>
        <Fact()>
        Public Sub DistinctOptionalParameterValues_BadValue()
            Dim ilSource = <![CDATA[
.class abstract public A
{
  .method public abstract virtual instance int32 get_P(int32 x, [opt] int32 y)
  {
    .param[2] = ""
  }
  .method public abstract virtual instance void set_P(int32 x, [opt] int32 y, int32 v)
  {
    .param[2] = int32(1)
  }
  .method public abstract virtual instance int32 get_Q(int32 x, [opt] int32 y)
  {
    .param[2] = int32(2)
  }
  .method public abstract virtual instance void set_Q(int32 x, [opt] int32 y, int32 v)
  {
    .param[2] = ""
  }
  // Bad getter default value.
  .property instance int32 P(int32, int32)
  {
    .get instance int32 A::get_P(int32, int32)
    .set instance void A::set_P(int32, int32, int32)
  }
  // Bad setter default value.
  .property instance int32 Q(int32, int32)
  {
    .get instance int32 A::get_Q(int32, int32)
    .set instance void A::set_Q(int32, int32, int32)
  }
}
]]>.Value
            Dim reference = CompileIL(ilSource)
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M(o As A)
        o.P(1) = o.P(0)
        o.P(2) += 1
        o.P(3, 0) += 1
        o.Q(1) = o.Q(0)
        o.Q(2) += 1
        o.Q(3, 0) += 1
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(vbSource, {reference})
            compilation.AssertTheseDiagnostics(
<expected>
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property P(x As Integer, y As Integer) As Integer'.
        o.P(1) = o.P(0)
          ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property P(x As Integer, y As Integer) As Integer'.
        o.P(1) = o.P(0)
                   ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property P(x As Integer, y As Integer) As Integer'.
        o.P(2) += 1
          ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property Q(x As Integer, y As Integer) As Integer'.
        o.Q(1) = o.Q(0)
          ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property Q(x As Integer, y As Integer) As Integer'.
        o.Q(1) = o.Q(0)
                   ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property Q(x As Integer, y As Integer) As Integer'.
        o.Q(2) += 1
          ~
</expected>)
        End Sub

        <WorkItem(545959, "DevDiv")>
        <Fact()>
        Public Sub DistinctOptionalParameterValues_AdditionalOptional()
            Dim ilSource = <![CDATA[
.class abstract public A
{
  .method public abstract virtual instance int32 get_P(int32 x, [opt] int32 y)
  {
    .param[2] = int32(1)
  }
  .method public abstract virtual instance void set_P(int32 x, int32 y, int32 v)
  {
  }
  .method public abstract virtual instance int32 get_Q(int32 x, int32 y)
  {
  }
  .method public abstract virtual instance void set_Q(int32 x, [opt] int32 y, int32 v)
  {
    .param[2] = int32(2)
  }
  // Optional parameter in getter.
  .property instance int32 P(int32, int32)
  {
    .get instance int32 A::get_P(int32, int32)
    .set instance void A::set_P(int32, int32, int32)
  }
  // Optional parameter in setter.
  .property instance int32 Q(int32, int32)
  {
    .get instance int32 A::get_Q(int32, int32)
    .set instance void A::set_Q(int32, int32, int32)
  }
}
]]>.Value
            Dim reference = CompileIL(ilSource)
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M(o As A)
        o.P(1) = o.P(0)
        o.P(2) += 1
        o.P(3, 0) += 1
        [ByVal](o.P(4))
        [ByRef](o.P(5))
        o.Q(1) = o.Q(0)
        o.Q(2) += 1
        o.Q(3, 0) += 1
        [ByVal](o.Q(4))
        [ByRef](o.Q(5))
    End Sub
    Sub [ByVal](ByVal o As Integer)
    End Sub
    Sub [ByRef](ByRef o As Integer)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(vbSource, {reference})
            compilation.AssertTheseDiagnostics(
<expected>
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property P(x As Integer, y As Integer) As Integer'.
        o.P(1) = o.P(0)
          ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property P(x As Integer, y As Integer) As Integer'.
        o.P(1) = o.P(0)
                   ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property P(x As Integer, y As Integer) As Integer'.
        o.P(2) += 1
          ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property P(x As Integer, y As Integer) As Integer'.
        [ByVal](o.P(4))
                  ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property P(x As Integer, y As Integer) As Integer'.
        [ByRef](o.P(5))
                  ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property Q(x As Integer, y As Integer) As Integer'.
        o.Q(1) = o.Q(0)
          ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property Q(x As Integer, y As Integer) As Integer'.
        o.Q(1) = o.Q(0)
                   ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property Q(x As Integer, y As Integer) As Integer'.
        o.Q(2) += 1
          ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property Q(x As Integer, y As Integer) As Integer'.
        [ByVal](o.Q(4))
                  ~
BC30455: Argument not specified for parameter 'y' of 'Public MustOverride Overrides Property Q(x As Integer, y As Integer) As Integer'.
        [ByRef](o.Q(5))
                  ~
</expected>)
        End Sub

        ''' <summary>
        ''' Signatures where the property value type
        ''' does not match the getter return type.
        ''' </summary>
        <WorkItem(546476, "DevDiv")>
        <Fact()>
        Public Sub DifferentSignatures_PropertyType()
            Dim source1 = <![CDATA[
.class public A { }
.class public B1 extends A { }
.class public B2 extends A { }
.class public C
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public instance class A get_A() { ldnull ret }
  .method public instance class B1 get_B1() { ldnull ret }
  .method public instance class B2 get_B2() { ldnull ret }
  .method public instance void set_A(class A v) { ret }
  .method public instance void set_B1(class B1 v) { ret }
  .property class A P1()
  {
    .get instance class B1 C::get_B1()
    .set instance void C::set_A(class A v)
  }
  .property class B1 P2()
  {
    .get instance class A C::get_A()
    .set instance void C::set_B1(class B1 v)
  }
  .property class B1 P3()
  {
    .get instance class B2 C::get_B2()
    .set instance void C::set_B1(class B1 v)
  }
}
]]>.Value
            Dim reference1 = CompileIL(source1)
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M1(o As C)
        Dim v As B1
        v = o.P1
        v = o.P2
        v = o.P3
        o.P1 = v
        o.P2 = v
        o.P3 = v
        M2(o.P1)
        M2(o.P2)
        M2(o.P3)
        M3(o.P1)
        M3(o.P2)
        M3(o.P3)
    End Sub
    Sub M2(o As B1)
    End Sub
    Sub M3(ByRef o As B1)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'A' to 'B1'.
        v = o.P2
            ~~~~
BC30311: Value of type 'B2' cannot be converted to 'B1'.
        v = o.P3
            ~~~~
BC30512: Option Strict On disallows implicit conversions from 'A' to 'B1'.
        M2(o.P2)
           ~~~~
BC30311: Value of type 'B2' cannot be converted to 'B1'.
        M2(o.P3)
           ~~~~
BC30512: Option Strict On disallows implicit conversions from 'A' to 'B1'.
        M3(o.P2)
           ~~~~
BC30311: Value of type 'B2' cannot be converted to 'B1'.
        M3(o.P3)
           ~~~~
]]></errors>)
        End Sub

        ''' <summary>
        ''' Signatures where the property value type
        ''' does not match the setter value type.
        ''' </summary>
        <WorkItem(546476, "DevDiv")>
        <Fact()>
        Public Sub DifferentSignatures_PropertyType_2()
            Dim source1 = <![CDATA[
.class public A { }
.class public B1 extends A { }
.class public B2 extends A { }
.class public C
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public instance class A get_A() { ldnull ret }
  .method public instance class B1 get_B1() { ldnull ret }
  .method public instance void set_A(class A v) { ret }
  .method public instance void set_B1(class B1 v) { ret }
  .method public instance void set_B2(class B2 v) { ret }
  .property class A P1()
  {
    .get instance class A C::get_A()
    .set instance void C::set_B1(class B1 v)
  }
  .property class B1 P2()
  {
    .get instance class B1 C::get_B1()
    .set instance void C::set_A(class A v)
  }
  .property class B1 P3()
  {
    .get instance class B1 C::get_B1()
    .set instance void C::set_B2(class B2 v)
  }
}
]]>.Value
            Dim reference1 = CompileIL(source1)
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M1(o As C)
        Dim v As B1
        v = o.P1
        v = o.P2
        v = o.P3
        o.P1 = v
        o.P2 = v
        o.P3 = v
        M2(o.P1)
        M2(o.P2)
        M2(o.P3)
        M3(o.P1)
        M3(o.P2)
        M3(o.P3)
    End Sub
    Sub M2(o As B1)
    End Sub
    Sub M3(ByRef o As B1)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'A' to 'B1'.
        v = o.P1
            ~~~~
BC30311: Value of type 'B1' cannot be converted to 'B2'.
        o.P3 = v
               ~
BC30512: Option Strict On disallows implicit conversions from 'A' to 'B1'.
        M2(o.P1)
           ~~~~
BC30512: Option Strict On disallows implicit conversions from 'A' to 'B1'.
        M3(o.P1)
           ~~~~
BC33037: Cannot copy the value of 'ByRef' parameter 'o' back to the matching argument because type 'B1' cannot be converted to type 'B2'.
        M3(o.P3)
           ~~~~
]]></errors>)
        End Sub

        ''' <summary>
        ''' Signatures where the property value type does not
        ''' match the accessors, used in compound assignment.
        ''' </summary>
        <WorkItem(546476, "DevDiv")>
        <Fact()>
        Public Sub DifferentSignatures_PropertyType_3()
            Dim source1 = <![CDATA[
.class public C
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public instance int32 get_P() { ldnull ret }
  .method public instance void set_P(int32 v) { ret }
  .method public instance object get_Q() { ldnull ret }
  .method public instance void set_Q(object v) { ret }
  .property object P()
  {
    .get instance int32 C::get_P()
    .set instance void C::set_P(int32 v)
  }
  .property int32 Q()
  {
    .get instance object C::get_Q()
    .set instance void C::set_Q(object v)
  }
}
]]>.Value
            Dim reference1 = CompileIL(source1)
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M(o As C)
        o.P += 1
        o.Q += 1
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC30038: Option Strict On prohibits operands of type Object for operator '+'.
        o.Q += 1
        ~~~
]]></errors>)
        End Sub

        ''' <summary>
        ''' Getter return type is used for type inference.
        ''' Note: Dev11 uses the property type rather than getter.
        ''' </summary>
        <WorkItem(546476, "DevDiv")>
        <Fact()>
        Public Sub DifferentSignatures_PropertyType_4()
            Dim source1 = <![CDATA[
.class public A { }
.class public B extends A { }
.class public C
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public instance class A get_A() { ldnull ret }
  .method public instance class B get_B() { ldnull ret }
  .method public instance void set_A(class A v) { ret }
  .method public instance void set_B(class B v) { ret }
  .property class A P1()
  {
    .get instance class A C::get_A()
    .set instance void C::set_B(class B v)
  }
  .property class A P2()
  {
    .get instance class B C::get_B()
    .set instance void C::set_A(class A v)
  }
  .property class B P3()
  {
    .get instance class A C::get_A()
    .set instance void C::set_B(class B v)
  }
  .property class B P4()
  {
    .get instance class B C::get_B()
    .set instance void C::set_A(class A v)
  }
}
]]>.Value
            Dim reference1 = CompileIL(source1)
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M(o As C)
        Dim v As B
        v = F1(o.P1)
        v = F1(o.P2)
        v = F1(o.P3)
        v = F1(o.P4)
        v = F2(o.P1)
        v = F2(o.P2)
        v = F2(o.P3)
        v = F2(o.P4)
    End Sub
    Function F1(Of T)(o As T) As T
        Return Nothing
    End Function
    Function F2(Of T)(ByRef o As T) As T
        Return Nothing
    End Function
End Module
]]>
                    </file>
                </compilation>
            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {reference1})
            compilation2.AssertTheseDiagnostics(<errors><![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'A' to 'B'.
        v = F1(o.P1)
            ~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'A' to 'B'.
        v = F1(o.P3)
            ~~~~~~~~
BC32029: Option Strict On disallows narrowing from type 'A' to type 'B' in copying the value of 'ByRef' parameter 'o' back to the matching argument.
        v = F2(o.P1)
               ~~~~
BC32029: Option Strict On disallows narrowing from type 'A' to type 'B' in copying the value of 'ByRef' parameter 'o' back to the matching argument.
        v = F2(o.P3)
               ~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub ByRefType()
            Dim ilSource = <![CDATA[
.class public A
{
  .method public instance object& get_P() { ldnull ret }
  .method public instance void set_P(object& v) { ret }
  .property instance object& P()
  {
    .get instance object& A::get_P()
    .set instance void A::set_P(object&)
  }
}
]]>.Value
            Dim reference = CompileIL(ilSource)
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Sub M(o As A)
        Dim v As Object
        v = o.P
        o.P = v
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(vbSource, additionalRefs:={reference})
            compilation.AssertTheseDiagnostics(
<expected>
BC30643: Property 'P' is of an unsupported type.
        v = o.P
              ~
BC30643: Property 'P' is of an unsupported type.
        o.P = v
          ~    
</expected>)
        End Sub

        <Fact()>
        Public Sub ByRefParameter()
            Dim ilSource = <![CDATA[
.class public A
{
  .method public instance object get_P(object& i) { ldnull ret }
  .method public instance void set_P(object& i, object v) { ret }
  .property instance object P(object&)
  {
    .get instance object A::get_P(object&)
    .set instance void A::set_P(object&, object)
  }
}
]]>.Value
            Dim reference = CompileIL(ilSource)
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Sub M(o As A)
        Dim v As Object
        v = o.P(Nothing)
        o.P(Nothing) = v
        o.P(v) = o.P(v)
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(vbSource, additionalRefs:={reference})
            compilation.AssertNoErrors()
            Dim compilationVerifier = CompileAndVerify(compilation)
            compilationVerifier.VerifyIL("M.M(A)",
            <![CDATA[
{
  // Code size       68 (0x44)
  .maxstack  4
  .locals init (Object V_0, //v
  Object V_1,
  Object V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stloc.1
  IL_0003:  ldloca.s   V_1
  IL_0005:  callvirt   "Function A.get_P(ByRef Object) As Object"
  IL_000a:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000f:  stloc.0
  IL_0010:  ldarg.0
  IL_0011:  ldnull
  IL_0012:  stloc.1
  IL_0013:  ldloca.s   V_1
  IL_0015:  ldloc.0
  IL_0016:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_001b:  callvirt   "Sub A.set_P(ByRef Object, Object)"
  IL_0020:  ldarg.0
  IL_0021:  ldloc.0
  IL_0022:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0027:  stloc.1
  IL_0028:  ldloca.s   V_1
  IL_002a:  ldarg.0
  IL_002b:  ldloc.0
  IL_002c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0031:  stloc.2
  IL_0032:  ldloca.s   V_2
  IL_0034:  callvirt   "Function A.get_P(ByRef Object) As Object"
  IL_0039:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_003e:  callvirt   "Sub A.set_P(ByRef Object, Object)"
  IL_0043:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub MissingSystemTypes_Property()
            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I
    ReadOnly Property P As Object
    WriteOnly Property Q As Object
End Interface
   ]]></file>
</compilation>, references:=Nothing)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'System.Object' is not defined.
    ReadOnly Property P As Object
                           ~~~~~~
BC30002: Type 'System.Void' is not defined.
    WriteOnly Property Q As Object
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.Object' is not defined.
    WriteOnly Property Q As Object
                            ~~~~~~
     ]]></errors>)
        End Sub

        <WorkItem(530418, "DevDiv")>
        <Fact(Skip:="530418")>
        Public Sub MissingSystemTypes_AutoProperty()
            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Property P As Object
End Class
   ]]></file>
</compilation>, references:=Nothing)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue' is not defined.
    Property P As Object
             ~
BC30002: Type 'System.Void' is not defined.
Class C
~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'C.dll' failed.
Class C
      ~
BC30002: Type 'System.Void' is not defined.
    Property P As Object
    ~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.Object' is not defined.
    Property P As Object
                  ~~~~~~
     ]]></errors>)
        End Sub

        <WorkItem(531292, "DevDiv")>
        <Fact()>
        Public Sub Bug17897()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Property Test0()
        Protected Get
            Return Nothing
        End Get
        Set(ByVal Value)

        End Set
    End Property

    Protected Sub Test1()
    End Sub

    Protected Property Test2 As Integer

    Property Test3()
        Get
            Return Nothing
        End Get
        Protected Set(ByVal Value)

        End Set
    End Property

    Protected Test4 As Integer

    Protected Class Test5
    End Class

    Protected Event Test6 As Action

    Protected Delegate Sub Test7()

    Sub Main(args As String())
    End Sub
End Module
   ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(
<errors><![CDATA[
BC30503: Properties in a Module cannot be declared 'Protected'.
        Protected Get
        ~~~~~~~~~
BC30433: Methods in a Module cannot be declared 'Protected'.
    Protected Sub Test1()
    ~~~~~~~~~
BC30503: Properties in a Module cannot be declared 'Protected'.
    Protected Property Test2 As Integer
    ~~~~~~~~~
BC30503: Properties in a Module cannot be declared 'Protected'.
        Protected Set(ByVal Value)
        ~~~~~~~~~
BC30593: Variables in Modules cannot be declared 'Protected'.
    Protected Test4 As Integer
    ~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected'.
    Protected Class Test5
    ~~~~~~~~~
BC30434: Events in a Module cannot be declared 'Protected'.
    Protected Event Test6 As Action
    ~~~~~~~~~
BC30735: Type in a Module cannot be declared 'Protected'.
    Protected Delegate Sub Test7()
    ~~~~~~~~~
]]></errors>)
        End Sub

        ''' <summary>
        ''' When the output type is .winmdobj properties should emit put_Property methods instead
        ''' of set_Property methods.
        ''' </summary>
        <Fact()>
        Public Sub WinRtPropertySet()
            Dim libSrc =
                <compilation>
                    <file name="c.vb"><![CDATA[
Public Class C
    Public Dim Abacking As Integer
    
    Public Property A As Integer
        Get
            Return Abacking
        End Get
        Set
            Abacking = value
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>
            Dim getValidator =
                Function(expectedMembers As String())
                    Return Sub(m As ModuleSymbol)
                               Dim actualMembers = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMembers().ToArray()

                               AssertEx.SetEqual((From s In actualMembers
                                                  Select s.Name), expectedMembers)
                           End Sub
                End Function

            Dim verify =
                Sub(winmd As Boolean, expected As String())
                    Dim validator = getValidator(expected)

                    ' We should see the same members from both source and metadata
                    Dim verifier = CompileAndVerify(
                        libSrc,
                        sourceSymbolValidator:=validator,
                        symbolValidator:=validator,
                        options:=If(winmd, TestOptions.ReleaseWinMD, TestOptions.ReleaseDll))
                    verifier.VerifyDiagnostics()
                End Sub

            ' Test winmd
            verify(True, New String() {
                "Abacking",
                "A",
                "get_A",
                "put_A",
                WellKnownMemberNames.InstanceConstructorName})

            ' Test normal
            verify(False, New String() {
                "Abacking",
                "A",
                "get_A",
                "set_A",
                WellKnownMemberNames.InstanceConstructorName})
        End Sub

        <Fact()>
        Public Sub WinRtAnonymousProperty()
            Dim src = <compilation>
                          <file name="c.vb">
                              <![CDATA[
Imports System

Class C
   Public Property P = New With {.Name = "prop"}
End Class
]]>
                          </file>
                      </compilation>

            Dim srcValidator =
                Sub(m As ModuleSymbol)
                    Dim members = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMembers()

                    AssertEx.SetEqual((From member In members Select member.Name),
                                      {WellKnownMemberNames.InstanceConstructorName,
                                       "_P",
                                       "get_P",
                                       "put_P",
                                       "P"})

                End Sub
            Dim mdValidator =
                Sub(m As ModuleSymbol)
                    Dim members = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("VB$AnonymousType_0").GetMembers()

                    AssertEx.SetEqual((From member In members Select member.Name),
                                      {"get_Name",
                                       "put_Name",
                                       WellKnownMemberNames.InstanceConstructorName,
                                       "ToString",
                                       "Name"})
                End Sub

            Dim verifier = CompileAndVerify(src,
                                            allReferences:={MscorlibRef_v4_0_30316_17626},
                                            options:=TestOptions.ReleaseWinMD,
                                            sourceSymbolValidator:=srcValidator,
                                            symbolValidator:=mdValidator)
        End Sub

        ''' <summary>
        ''' Accessor type names that conflict should cause the appropriate diagnostic
        ''' (i.e., set_ for dll, put_ for winmdobj)
        ''' </summary>
        <Fact()>
        Public Sub WinRtPropertyAccessorNameConflict()
            Dim libSrc =
                <compilation>
                    <file name="c.vb">
                        <![CDATA[
Public Class C
    Public Property A as Integer

    Public Sub put_A(value As Integer)
    End Sub

    Public Sub set_A(value as Integer)
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib(libSrc, OutputKind.DynamicallyLinkedLibrary)
            comp.VerifyDiagnostics(
               Diagnostic(ERRID.ERR_SynthMemberClashesWithMember5, "A").WithArguments("property", "A", "set_A", "class", "C"))

            comp = CreateCompilationWithMscorlib(libSrc, OutputKind.WindowsRuntimeMetadata)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_SynthMemberClashesWithMember5, "A").WithArguments("property", "A", "put_A", "class", "C"))
        End Sub

#Region "Helpers"
        Private Sub VerifyMethodsAndAccessorsSame(type As NamedTypeSymbol, [property] As PropertySymbol)
            VerifyMethodAndAccessorSame(type, [property], [property].GetMethod)
            VerifyMethodAndAccessorSame(type, [property], [property].SetMethod)
        End Sub

        Private Sub VerifyMethodAndAccessorSame(type As NamedTypeSymbol, [property] As PropertySymbol, accessor As MethodSymbol)
            Assert.NotNull(accessor)
            Assert.Same(type, accessor.ContainingType)
            Assert.Same(type, accessor.ContainingSymbol)

            Dim method = type.GetMembers(accessor.Name).Single()
            Assert.NotNull(method)
            Assert.Equal(accessor, method)

            Dim isAccessor = accessor.MethodKind = MethodKind.PropertyGet OrElse accessor.MethodKind = MethodKind.PropertySet
            Assert.True(isAccessor)
            Assert.NotNull(accessor.AssociatedSymbol)
            Assert.Same(accessor.AssociatedSymbol, [property])
        End Sub

        Private Shared Sub CheckPropertyAccessibility([property] As PropertySymbol, propertyAccessibility As Accessibility, getterAccessibility As Accessibility, setterAccessibility As Accessibility)
            Dim type = [property].Type
            Assert.NotEqual(type.PrimitiveTypeCode, Cci.PrimitiveTypeCode.Void)
            Assert.Equal(propertyAccessibility, [property].DeclaredAccessibility)
            CheckPropertyAccessorAccessibility([property], propertyAccessibility, [property].GetMethod, getterAccessibility)
            CheckPropertyAccessorAccessibility([property], propertyAccessibility, [property].SetMethod, setterAccessibility)
        End Sub

        Private Shared Sub CheckPropertyAccessorAccessibility([property] As PropertySymbol, propertyAccessibility As Accessibility, accessor As MethodSymbol, accessorAccessibility As Accessibility)
            If accessor Is Nothing Then
                Assert.Equal(accessorAccessibility, Accessibility.NotApplicable)
            Else
                Dim containingType = [property].ContainingType
                Assert.Same([property], accessor.AssociatedSymbol)
                Assert.Same(containingType, accessor.ContainingType)
                Assert.Same(containingType, accessor.ContainingSymbol)
                Dim method = containingType.GetMembers(accessor.Name).Single()
                Assert.Same(method, accessor)
                Assert.Equal(accessorAccessibility, accessor.DeclaredAccessibility)
            End If
        End Sub

        Private Shared Sub VerifyAutoProperty(type As NamedTypeSymbol, name As String, declaredAccessibility As Accessibility, isFromSource As Boolean)
            Dim [property] = type.GetMembers(name).OfType(Of PropertySymbol)().SingleOrDefault()
            Assert.NotNull([property])
            Assert.Equal([property].DeclaredAccessibility, declaredAccessibility)

            Dim sourceProperty = TryCast([property], SourcePropertySymbol)
            If sourceProperty IsNot Nothing Then
                Assert.True(sourceProperty.IsAutoProperty)

                Dim c = sourceProperty.DeclaringCompilation
                Dim attributes = sourceProperty.AssociatedField.GetSynthesizedAttributes()

                Assert.Equal(
                    c.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor),
                    attributes.Single().AttributeConstructor)
            End If

            Dim field = type.GetMembers("_" + name).OfType(Of FieldSymbol)().SingleOrDefault()
            If isFromSource Then
                Assert.NotNull(field)
                Assert.Equal(field.DeclaredAccessibility, Accessibility.Private)
                Assert.Equal(field.Type, [property].Type)
            Else
                Assert.Null(field)
            End If
        End Sub

        Private Sub VerifyAccessor(accessor As MethodSymbol, associatedProperty As PEPropertySymbol, methodKind As MethodKind)
            Assert.NotNull(accessor)
            Assert.Same(accessor.AssociatedSymbol, associatedProperty)
            Assert.Equal(accessor.MethodKind, methodKind)
            If associatedProperty IsNot Nothing Then
                Dim method = If((methodKind = MethodKind.PropertyGet), associatedProperty.GetMethod, associatedProperty.SetMethod)
                Assert.Same(accessor, method)
            End If
        End Sub

        Private Sub VerifyPropertyParams(propertySymbol As PropertySymbol, expectedParams As String(,))
            For index = 0 To expectedParams.Length
                Assert.Equal(propertySymbol.Parameters(index).Name, expectedParams(index, 0))
                Assert.Equal(propertySymbol.Parameters(index).Type.Name, expectedParams(index, 1))
            Next
        End Sub

        Private Shared Sub CheckPropertyExplicitImplementation([class] As NamedTypeSymbol, classProperty As PropertySymbol, interfaceProperty As PropertySymbol)
            Dim interfacePropertyGetter = interfaceProperty.GetMethod
            Assert.NotNull(interfacePropertyGetter)
            Dim interfacePropertySetter = interfaceProperty.SetMethod
            Assert.NotNull(interfacePropertySetter)

            Assert.Equal(interfaceProperty, classProperty.ExplicitInterfaceImplementations.Single())

            Dim classPropertyGetter = classProperty.GetMethod
            Assert.NotNull(classPropertyGetter)
            Dim classPropertySetter = classProperty.SetMethod
            Assert.NotNull(classPropertySetter)

            Assert.Equal(interfacePropertyGetter, classPropertyGetter.ExplicitInterfaceImplementations.Single())
            Assert.Equal(interfacePropertySetter, classPropertySetter.ExplicitInterfaceImplementations.Single())

            Dim typeDef = DirectCast([class], Cci.ITypeDefinition)
            Dim [module] = New PEAssemblyBuilder(DirectCast([class].ContainingAssembly, SourceAssemblySymbol), EmitOptions.Default, OutputKind.DynamicallyLinkedLibrary, GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable(Of ResourceDescription)())

            Dim context = New EmitContext([module], Nothing, New DiagnosticBag())
            Dim explicitOverrides = typeDef.GetExplicitImplementationOverrides(context)
            Assert.Equal(2, explicitOverrides.Count())
            Assert.True(explicitOverrides.All(Function(override) [class] Is override.ContainingType))

            ' We're not actually asserting that the overrides are in this order - set comparison just seems like overkill for two elements
            Dim getterOverride = explicitOverrides.First()
            Assert.Equal(classPropertyGetter, getterOverride.ImplementingMethod)
            Assert.Equal(interfacePropertyGetter.ContainingType, getterOverride.ImplementedMethod.GetContainingType(context))
            Assert.Equal(interfacePropertyGetter.Name, getterOverride.ImplementedMethod.Name)

            Dim setterOverride = explicitOverrides.Last()
            Assert.Equal(classPropertySetter, setterOverride.ImplementingMethod)
            Assert.Equal(interfacePropertySetter.ContainingType, setterOverride.ImplementedMethod.GetContainingType(context))
            Assert.Equal(interfacePropertySetter.Name, setterOverride.ImplementedMethod.Name)

            context.Diagnostics.Verify()
        End Sub

        Private Shared Sub VerifyAccessibility([property] As PEPropertySymbol, propertyAccessibility As Accessibility, getAccessibility As Accessibility, setAccessibility As Accessibility)
            Assert.Equal([property].DeclaredAccessibility, propertyAccessibility)
            VerifyAccessorAccessibility([property].GetMethod, getAccessibility)
            VerifyAccessorAccessibility([property].SetMethod, setAccessibility)
        End Sub

        Private Shared Sub VerifyAccessorAccessibility(accessor As MethodSymbol, accessorAccessibility As Accessibility)
            If accessorAccessibility = Accessibility.NotApplicable Then
                Assert.Null(accessor)
            Else
                Assert.NotNull(accessor)
                Assert.Equal(accessor.DeclaredAccessibility, accessorAccessibility)
            End If
        End Sub

        Private Function CompileWithCustomPropertiesAssembly(source As XElement, Optional options As VisualBasicCompilationOptions = Nothing) As VisualBasicCompilation
            Return CreateCompilationWithMscorlibAndReferences(source, {s_propertiesDll}, options)
        End Function

        Private Shared ReadOnly s_propertiesDll As MetadataReference = TestReferences.SymbolsTests.Properties

        Private Shared Sub VerifyPropertiesParametersCount([property] As PropertySymbol, expectedCount As Integer)
            Assert.Equal([property].Parameters.Length, expectedCount)
            If [property].GetMethod IsNot Nothing Then
                Assert.Equal([property].GetMethod.Parameters.Length, expectedCount)
            End If
            If [property].SetMethod IsNot Nothing Then
                Assert.Equal([property].SetMethod.Parameters.Length, expectedCount + 1)
            End If
        End Sub

        Private Shared Sub VerifyPropertiesParametersTypes([property] As PropertySymbol, ParamArray expectedTypes() As TypeSymbol)
            Assert.Equal([property].SetMethod.Parameters.Last().Type, [property].Type)
            Assert.True((From param In [property].Parameters Select param.Type).SequenceEqual(expectedTypes))

            If [property].GetMethod IsNot Nothing Then
                Assert.True((From param In [property].GetMethod.Parameters Select param.Type).SequenceEqual(expectedTypes))
            End If
            If [property].SetMethod IsNot Nothing Then
                Assert.True((From param In [property].SetMethod.Parameters Select param.Type).Take([property].Parameters.Length - 1).SequenceEqual(expectedTypes))
            End If
        End Sub

        Private Shared Sub VerifyNoDiagnostics(result As EmitResult)
            Assert.Equal(String.Empty, String.Join(Environment.NewLine, result.Diagnostics))
        End Sub

#End Region
    End Class
End Namespace
