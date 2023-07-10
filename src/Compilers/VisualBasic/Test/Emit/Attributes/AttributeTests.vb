' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class AttributeTests
        Inherits BasicTestBase

#Region "Function Tests"

        <WorkItem(530310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530310")>
        <Fact>
        Public Sub PEParameterSymbolParamArrayAttribute()
            Dim source1 = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly '<<GeneratedFileName>>'
{
}
.class public A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public static void M(int32 x, int32[] y)
  {
    .param [2]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }
}]]>.Value

            Dim reference1 = CompileIL(source1, prependDefaultHeader:=False)
            Dim source2 =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Class C
    Shared Sub Main(args As String())
        A.M(1, 2, 3)
        A.M(1, 2, 3, 4)
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40AndReferences(source2, {reference1})

            Dim method = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("A").GetMember(Of PEMethodSymbol)("M")
            Dim yParam = method.Parameters.Item(1)
            Assert.Equal(0, yParam.GetAttributes().Length)
            Assert.True(yParam.IsParamArray)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        <WorkItem(20741, "https://github.com/dotnet/roslyn/issues/20741")>
        Public Sub TestNamedArgumentOnStringParamsArgument()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

Class MarkAttribute
    Inherits Attribute

    Public Sub New(ByVal otherArg As Boolean, ParamArray args As Object())
    End Sub
End Class

<Mark(args:=New String() {"Hello", "World"}, otherArg:=True)>
Module Program

    Private Sub Test(ByVal otherArg As Boolean, ParamArray args As Object())
    End Sub

    Sub Main()
        Console.WriteLine("Method call")
        Test(args:=New String() {"Hello", "World"}, otherArg:=True)
    End Sub
End Module
]]>
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30455: Argument not specified for parameter 'otherArg' of 'Public Sub New(otherArg As Boolean, ParamArray args As Object())'.
<Mark(args:=New String() {"Hello", "World"}, otherArg:=True)>
 ~~~~
BC30661: Field or property 'args' is not found.
<Mark(args:=New String() {"Hello", "World"}, otherArg:=True)>
      ~~~~
BC30661: Field or property 'otherArg' is not found.
<Mark(args:=New String() {"Hello", "World"}, otherArg:=True)>
                                             ~~~~~~~~
BC30587: Named argument cannot match a ParamArray parameter.
        Test(args:=New String() {"Hello", "World"}, otherArg:=True)
             ~~~~
                                        ]]></errors>)
        End Sub

        ''' <summary>
        ''' This function is the same as PEParameterSymbolParamArray
        ''' except that we check attributes first (to check for race
        ''' conditions).
        ''' </summary>
        <WorkItem(530310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530310")>
        <Fact>
        Public Sub PEParameterSymbolParamArrayAttribute2()
            Dim source1 = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly '<<GeneratedFileName>>'
{
}
.class public A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public static void M(int32 x, int32[] y)
  {
    .param [2]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }
}]]>.Value

            Dim reference1 = CompileIL(source1, prependDefaultHeader:=False)
            Dim source2 =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Class C
    Shared Sub Main(args As String())
        A.M(1, 2, 3)
        A.M(1, 2, 3, 4)
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40AndReferences(source2, {reference1})

            Dim method = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("A").GetMember(Of PEMethodSymbol)("M")
            Dim yParam = method.Parameters.Item(1)
            Assert.True(yParam.IsParamArray)
            Assert.Equal(0, yParam.GetAttributes().Length)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub BindingScope_Parameters()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class A
    Inherits System.Attribute

    Public Sub New(value As Integer)
    End Sub
End Class

Class C
    Const Value As Integer = 0

    Sub method1(<A(Value)> x As Integer)
    End Sub

End Class
]]>
    </file>
</compilation>
            CompileAndVerify(source)
        End Sub

        <Fact>
        Public Sub TestAssemblyAttributes()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System     
Imports System.Runtime.CompilerServices

<assembly: InternalsVisibleTo("Roslyn.Compilers.UnitTests")>
<assembly: InternalsVisibleTo("Roslyn.Compilers.CSharp")>
<assembly: InternalsVisibleTo("Roslyn.Compilers.CSharp.UnitTests")>
<assembly: InternalsVisibleTo("Roslyn.Compilers.CSharp.Test.Utilities")>
<assembly: InternalsVisibleTo("Roslyn.Compilers.VisualBasic")>
]]>
    </file>
</compilation>

            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim assembly = m.ContainingSymbol
                                         Dim compilerServicesNS = GetSystemRuntimeCompilerServicesNamespace(m)
                                         Dim internalsVisibleToAttr As NamedTypeSymbol = compilerServicesNS.GetTypeMembers("InternalsVisibleToAttribute").First()

                                         Dim attrs = assembly.GetAttributes(internalsVisibleToAttr)
                                         Assert.Equal(5, attrs.Count)
                                         attrs(0).VerifyValue(0, TypedConstantKind.Primitive, "Roslyn.Compilers.UnitTests")
                                         attrs(1).VerifyValue(0, TypedConstantKind.Primitive, "Roslyn.Compilers.CSharp")
                                         attrs(2).VerifyValue(0, TypedConstantKind.Primitive, "Roslyn.Compilers.CSharp.UnitTests")
                                         attrs(3).VerifyValue(0, TypedConstantKind.Primitive, "Roslyn.Compilers.CSharp.Test.Utilities")
                                         attrs(4).VerifyValue(0, TypedConstantKind.Primitive, "Roslyn.Compilers.VisualBasic")
                                     End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        Private Function GetSystemRuntimeCompilerServicesNamespace(m As ModuleSymbol) As NamespaceSymbol
            Dim compilation = m.DeclaringCompilation
            Dim globalNS = If(compilation Is Nothing, m.ContainingAssembly.CorLibrary.GlobalNamespace, compilation.GlobalNamespace)

            Return globalNS.
                GetMember(Of NamespaceSymbol)("System").
                GetMember(Of NamespaceSymbol)("Runtime").
                GetMember(Of NamespaceSymbol)("CompilerServices")
        End Function

        <Fact>
        Public Sub TestAssemblyAttributesReflection()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation>
                    <file name="attr.vb"><![CDATA[
                    Imports System.Reflection
                    Imports System.Runtime.CompilerServices
                    Imports System.Runtime.InteropServices

                    ' These are not pseudo attributes, but encoded as bits in metadata
                    <assembly: AssemblyAlgorithmId(System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5)>
                    <assembly: AssemblyCultureAttribute("")>
                    <assembly: AssemblyDelaySign(true)>
                    <assembly: AssemblyFlags(AssemblyNameFlags.Retargetable)>
                    <assembly: AssemblyKeyFile("MyKey.snk")>
                    <assembly: AssemblyKeyName("Key Name")>
                    <assembly: AssemblyVersion("1.2.*")>

                    <assembly: AssemblyFileVersionAttribute("4.3.2.100")>
                ]]>
                    </file>
                </compilation>)

            Dim attrs = compilation.Assembly.GetAttributes()
            Assert.Equal(8, attrs.Length)

            For Each a In attrs
                Select Case a.AttributeClass.Name
                    Case "AssemblyAlgorithmIdAttribute"
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(0, a.CommonNamedArguments.Length)
                        Assert.Equal(TypedConstantKind.Enum, a.CommonConstructorArguments(0).Kind)
                        Assert.Equal("System.Configuration.Assemblies.AssemblyHashAlgorithm", a.CommonConstructorArguments(0).Type.ToDisplayString)
                        Assert.Equal(Configuration.Assemblies.AssemblyHashAlgorithm.MD5, CType(a.CommonConstructorArguments(0).Value, Configuration.Assemblies.AssemblyHashAlgorithm))
                    Case "AssemblyCultureAttribute"
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(TypedConstantKind.Primitive, a.CommonConstructorArguments(0).Kind)
                        Assert.Equal("String", a.CommonConstructorArguments(0).Type.ToDisplayString)
                        Assert.Equal(0, a.CommonNamedArguments.Length)
                    Case "AssemblyDelaySignAttribute"
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(TypedConstantKind.Primitive, a.CommonConstructorArguments(0).Kind)
                        Assert.Equal("Boolean", a.CommonConstructorArguments(0).Type.ToDisplayString)
                        Assert.Equal(True, a.CommonConstructorArguments(0).Value)
                    Case "AssemblyFlagsAttribute"
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(TypedConstantKind.Enum, a.CommonConstructorArguments(0).Kind)
                        Assert.Equal("System.Reflection.AssemblyNameFlags", a.CommonConstructorArguments(0).Type.ToDisplayString)
                        Assert.Equal(AssemblyNameFlags.Retargetable, CType(a.CommonConstructorArguments(0).Value, AssemblyNameFlags))
                    Case "AssemblyKeyFileAttribute"
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(TypedConstantKind.Primitive, a.CommonConstructorArguments(0).Kind)
                        Assert.Equal("String", a.CommonConstructorArguments(0).Type.ToDisplayString)
                        Assert.Equal("MyKey.snk", a.CommonConstructorArguments(0).Value)
                    Case "AssemblyKeyNameAttribute"
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(TypedConstantKind.Primitive, a.CommonConstructorArguments(0).Kind)
                        Assert.Equal("String", a.CommonConstructorArguments(0).Type.ToDisplayString)
                        Assert.Equal("Key Name", a.CommonConstructorArguments(0).Value)
                    Case "AssemblyVersionAttribute"
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(TypedConstantKind.Primitive, a.CommonConstructorArguments(0).Kind)
                        Assert.Equal("String", a.CommonConstructorArguments(0).Type.ToDisplayString)
                        Assert.Equal("1.2.*", a.CommonConstructorArguments(0).Value)
                    Case "AssemblyFileVersionAttribute"
                        Assert.Equal(1, a.CommonConstructorArguments.Length)
                        Assert.Equal(TypedConstantKind.Primitive, a.CommonConstructorArguments(0).Kind)
                        Assert.Equal("String", a.CommonConstructorArguments(0).Type.ToDisplayString)
                        Assert.Equal("4.3.2.100", a.CommonConstructorArguments(0).Value)
                    Case Else
                        Assert.Equal("Unexpected Attr", a.AttributeClass.Name)
                End Select
            Next
        End Sub

        ' Verify that resolving an attribute defined within a class on a class does not cause infinite recursion
        <Fact>
        Public Sub TestAttributesOnClassDefinedInClass()

            Dim compilation = CreateCompilationWithMscorlib40(
                <compilation>
                    <file name="a.vb"><![CDATA[
                        Imports System     
                        Imports System.Runtime.CompilerServices

                        <A.X()>
                        Public Class A

                            <AttributeUsage(AttributeTargets.All, allowMultiple:=true)>
                            Public Class XAttribute
                                Inherits Attribute
                            End Class

                        End Class]]>
                    </file>
                </compilation>)

            Dim attrs = compilation.SourceModule.GlobalNamespace.GetMember("A").GetAttributes()
            Assert.Equal(1, attrs.Length)
            Assert.Equal("A.XAttribute", attrs(0).AttributeClass.ToDisplayString)
        End Sub

        <WorkItem(540506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540506")>
        <Fact>
        Public Sub TestAttributesOnClassWithConstantDefinedInClass()

            Dim compilation = CreateCompilationWithMscorlib40(
                <compilation>
                    <file name="a.vb"><![CDATA[
                        <Attr(Goo.p)>
                        Class Goo
                            Friend Const p As Object = 2 + 2
                        End Class
                        Friend Class AttrAttribute
                            Inherits Attribute
                        End Class
                        ]]>
                    </file>
                </compilation>)

            Dim attrs = compilation.SourceModule.GlobalNamespace.GetMember("Goo").GetAttributes()
            Assert.Equal(1, attrs.Length)
            attrs(0).VerifyValue(0, TypedConstantKind.Primitive, 4)
        End Sub

        <WorkItem(540407, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540407")>
        <Fact>
        Public Sub TestAttributesOnProperty()

            Dim source =
                <compilation>
                    <file name="a.vb"><![CDATA[
Imports System

Public Class A
    Inherits Attribute
End Class

Public Interface I
    <A>
    Property AP(<A()>a As Integer) As <A>Integer
End Interface

Public Class C
    <A>
    Public Property AP As <A>Integer

    <A>
    Public Property P(<A> a As Integer) As <A>Integer
       <A>
       Get
           Return 0
       End Get

       <A>
       Set(<A>value As Integer)
       End Set
    End Property
End Class
                   ]]></file>
                </compilation>

            Dim attributeValidator =
                Function(isFromSource As Boolean) _
                    Sub(m As ModuleSymbol)
                        Dim i = DirectCast(m.GlobalNamespace.GetMember("I"), NamedTypeSymbol)
                        Dim c = DirectCast(m.GlobalNamespace.GetMember("C"), NamedTypeSymbol)

                        ' auto-property in interface
                        Dim ap = i.GetMember("AP")
                        Assert.Equal(1, ap.GetAttributes().Length)

                        Dim get_AP = DirectCast(i.GetMember("get_AP"), MethodSymbol)
                        Assert.Equal(0, get_AP.GetAttributes().Length)
                        Assert.Equal(1, get_AP.GetReturnTypeAttributes().Length)
                        Assert.Equal(1, get_AP.Parameters(0).GetAttributes().Length)

                        Dim set_AP = DirectCast(i.GetMember("set_AP"), MethodSymbol)
                        Assert.Equal(0, set_AP.GetAttributes().Length)
                        Assert.Equal(0, set_AP.GetReturnTypeAttributes().Length)
                        Assert.Equal(1, set_AP.Parameters(0).GetAttributes().Length)
                        Assert.Equal(0, set_AP.Parameters(1).GetAttributes().Length)

                        ' auto-property on class
                        ap = c.GetMember("AP")
                        Assert.Equal(1, ap.GetAttributes().Length)

                        get_AP = DirectCast(c.GetMember("get_AP"), MethodSymbol)
                        If isFromSource Then
                            Assert.Equal(0, get_AP.GetAttributes().Length)
                        Else
                            AssertEx.Equal({"CompilerGeneratedAttribute"}, GetAttributeNames(get_AP.GetAttributes()))
                        End If
                        Assert.Equal(1, get_AP.GetReturnTypeAttributes().Length)

                        set_AP = DirectCast(c.GetMember("set_AP"), MethodSymbol)
                        If isFromSource Then
                            Assert.Equal(0, get_AP.GetAttributes().Length)
                        Else
                            AssertEx.Equal({"CompilerGeneratedAttribute"}, GetAttributeNames(set_AP.GetAttributes()))
                        End If
                        Assert.Equal(0, set_AP.GetReturnTypeAttributes().Length)
                        Assert.Equal(0, set_AP.Parameters(0).GetAttributes().Length)

                        ' property 
                        Dim p = c.GetMember("P")
                        Assert.Equal(1, p.GetAttributes().Length)

                        Dim get_P = DirectCast(c.GetMember("get_P"), MethodSymbol)
                        Assert.Equal(1, get_P.GetAttributes().Length)
                        Assert.Equal(1, get_P.GetReturnTypeAttributes().Length)
                        Assert.Equal(1, get_P.Parameters(0).GetAttributes().Length)

                        Dim set_P = DirectCast(c.GetMember("set_P"), MethodSymbol)
                        Assert.Equal(1, set_P.GetAttributes().Length)
                        Assert.Equal(0, set_P.GetReturnTypeAttributes().Length)
                        Assert.Equal(1, set_P.Parameters(0).GetAttributes().Length)
                        Assert.Equal(1, set_P.Parameters(1).GetAttributes().Length)
                    End Sub

            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator(True), symbolValidator:=attributeValidator(False))
        End Sub

        <WorkItem(540407, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540407")>
        <Fact>
        Public Sub TestAttributesOnPropertyReturnType()

            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Imports System.Runtime.InteropServices

Public Class A
    Inherits System.Attribute
End Class

Public Class B
    Inherits System.Attribute
End Class

Public Interface I
    Property Auto As <A> Integer
    ReadOnly Property AutoRO As <A> Integer
    WriteOnly Property AutoWO As <A> Integer   ' warning
End Interface

Public Class C
    Property Auto As <A> Integer

    ReadOnly Property ROA As <A> Integer
        Get
            Return 0
        End Get
    End Property

    WriteOnly Property WOA As <A> Integer      ' warning
        Set(value As Integer)
        End Set
    End Property

    Property A As <A> Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Property AB As <A> Integer
        Get
            Return 0
        End Get
        Set(<B()> value As Integer)
        End Set
    End Property
End Class
]]></file>
</compilation>

            Dim attributeValidator =
                Sub(m As ModuleSymbol)
                    Dim i = DirectCast(m.GlobalNamespace.GetMember("I"), NamedTypeSymbol)
                    Dim c = DirectCast(m.GlobalNamespace.GetMember("C"), NamedTypeSymbol)

                    ' auto-property in interface
                    Dim auto = DirectCast(i.GetMember("Auto"), PropertySymbol)
                    Assert.Equal(1, auto.GetMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, auto.SetMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, auto.SetMethod.Parameters(0).GetAttributes().Length)

                    Dim autoRO = DirectCast(i.GetMember("AutoRO"), PropertySymbol)
                    Assert.Equal(1, autoRO.GetMethod.GetReturnTypeAttributes().Length)
                    Assert.Null(autoRO.SetMethod)

                    Dim autoWO = DirectCast(i.GetMember("AutoWO"), PropertySymbol)
                    Assert.Null(autoWO.GetMethod)
                    Assert.Equal(0, autoWO.SetMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, auto.SetMethod.Parameters(0).GetAttributes().Length)

                    ' auto-property in class
                    auto = DirectCast(c.GetMember("Auto"), PropertySymbol)
                    Assert.Equal(1, auto.GetMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, auto.SetMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, auto.SetMethod.Parameters(0).GetAttributes().Length)

                    ' custom property in class
                    Dim roa = DirectCast(c.GetMember("ROA"), PropertySymbol)
                    Assert.Equal(1, roa.GetMethod.GetReturnTypeAttributes().Length)
                    Assert.Null(roa.SetMethod)

                    Dim woa = DirectCast(c.GetMember("WOA"), PropertySymbol)
                    Assert.Null(woa.GetMethod)
                    Assert.Equal(0, woa.SetMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, woa.SetMethod.Parameters(0).GetAttributes().Length)

                    Dim a = DirectCast(c.GetMember("A"), PropertySymbol)
                    Assert.Equal(1, a.GetMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, a.SetMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, a.SetMethod.Parameters(0).GetAttributes().Length)

                    Dim ab = DirectCast(c.GetMember("AB"), PropertySymbol)
                    Assert.Equal(1, ab.GetMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal("A", ab.GetMethod.GetReturnTypeAttributes()(0).AttributeClass.Name)
                    Assert.Equal(0, ab.SetMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal(1, ab.SetMethod.Parameters(0).GetAttributes().Length)
                    Assert.Equal("B", ab.SetMethod.Parameters(0).GetAttributes()(0).AttributeClass.Name)
                End Sub

            Dim verifier = CompileAndVerify(source, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<errors><![CDATA[
BC42364: Attributes applied on a return type of a WriteOnly Property have no effect.
    WriteOnly Property AutoWO As <A> Integer   ' warning
                                  ~
BC42364: Attributes applied on a return type of a WriteOnly Property have no effect.
    WriteOnly Property WOA As <A> Integer      ' warning
                               ~
]]></errors>)
        End Sub

        <WorkItem(546779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546779")>
        <Fact>
        Public Sub TestAttributesOnPropertyReturnType_MarshalAs()

            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Interface I
    Property Auto As <MarshalAs(UnmanagedType.I4)> Integer
End Interface
]]></file>
</compilation>

            Dim attributeValidator =
                Sub(m As ModuleSymbol)
                    Dim i = DirectCast(m.GlobalNamespace.GetMember("I"), NamedTypeSymbol)

                    ' auto-property in interface
                    Dim auto = DirectCast(i.GetMember("Auto"), PropertySymbol)
                    Assert.Equal(UnmanagedType.I4, auto.GetMethod.ReturnTypeMarshallingInformation.UnmanagedType)
                    Assert.Equal(1, auto.GetMethod.GetReturnTypeAttributes().Length)
                    Assert.Null(auto.SetMethod.ReturnTypeMarshallingInformation)
                    Assert.Equal(UnmanagedType.I4, auto.SetMethod.Parameters(0).MarshallingInformation.UnmanagedType)
                    Assert.Equal(0, auto.SetMethod.Parameters(0).GetAttributes().Length)
                End Sub

            ' TODO (tomat): implement reading from PE: symbolValidator:=attributeValidator
            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator)
        End Sub

        <WorkItem(540433, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540433")>
        <Fact>
        Public Sub TestAttributesOnPropertyAndGetSet()

            Dim source =
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System

<AObject(GetType(Object), O:=A.obj)>
Public Class A
    Public Const obj As Object = Nothing
    Public ReadOnly Property RProp As String
        <AObject(New Object() {GetType(String)})>
        Get
            Return Nothing
        End Get
    End Property

    <AObject(New Object() {1, "two", GetType(String), 3.1415926})>
    Public WriteOnly Property WProp
        <AObject(New Object() {New Object() {GetType(String)}})>
        Set(value)

        End Set
    End Property

End Class
                   ]]></file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(
                source,
                {MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib.AsImmutableOrNull())},
                TestOptions.ReleaseDll)

            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim type = DirectCast(m.GlobalNamespace.GetMember("A"), NamedTypeSymbol)
                                         Dim attrs = type.GetAttributes()
                                         Assert.Equal("AObjectAttribute", attrs(0).AttributeClass.ToDisplayString)
                                         attrs(0).VerifyValue(Of Object)(0, TypedConstantKind.Type, GetType(Object))
                                         attrs(0).VerifyValue(Of Object)(0, "O", TypedConstantKind.Primitive, Nothing)

                                         Dim prop = type.GetMember(Of PropertySymbol)("RProp")
                                         attrs = prop.GetMethod.GetAttributes()
                                         attrs(0).VerifyValue(0, TypedConstantKind.Array, {GetType(String)})

                                         prop = type.GetMember(Of PropertySymbol)("WProp")
                                         attrs = prop.GetAttributes()
                                         attrs(0).VerifyValue(Of Object())(0, TypedConstantKind.Array, {1, "two", GetType(String), 3.1415926})
                                         attrs = prop.SetMethod.GetAttributes()
                                         attrs(0).VerifyValue(0, TypedConstantKind.Array, {New Object() {GetType(String)}})
                                     End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <Fact>
        Public Sub TestAttributesOnPropertyParameters()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class A
    Inherits Attribute
End Class

Public Class X
    Public Property P(<A>a As Integer) As <A>Integer
       Get
           Return 0
       End Get
       Set(<A>value As Integer)
       End Set
    End Property
End Class
]]>
    </file>
</compilation>

            Dim attributeValidator =
                Sub(m As ModuleSymbol)
                    Dim type = DirectCast(m.GlobalNamespace.GetMember("X"), NamedTypeSymbol)
                    Dim getter = DirectCast(type.GetMember("get_P"), MethodSymbol)
                    Dim setter = DirectCast(type.GetMember("set_P"), MethodSymbol)

                    ' getter
                    Assert.Equal(1, getter.Parameters.Length)
                    Assert.Equal(1, getter.Parameters(0).GetAttributes().Length)
                    Assert.Equal(1, getter.GetReturnTypeAttributes().Length)

                    ' setter
                    Assert.Equal(2, setter.Parameters.Length)
                    Assert.Equal(1, setter.Parameters(0).GetAttributes().Length)
                    Assert.Equal(1, setter.Parameters(1).GetAttributes().Length)

                End Sub

            CompileAndVerify(source, symbolValidator:=attributeValidator, sourceSymbolValidator:=attributeValidator)
        End Sub

        <Fact>
        Public Sub TestAttributesOnEnumField()

            Dim source =
    <compilation>
        <file name="attr.vb"><![CDATA[
Option Strict On

Imports System
Imports system.Collections.Generic
Imports System.Reflection
Imports CustomAttribute
Imports AN = CustomAttribute.AttrName

' Use AttrName without Attribute suffix
<Assembly: AN(UShortField:=4321)> 
<Assembly: AN(UShortField:=1234)> 
<Module: AttrName(TypeField:=GetType(System.IO.FileStream))> 

Namespace AttributeTest

    Public Interface IGoo

        Class NestedClass
            ' enum as object
            <AllInheritMultiple(System.IO.FileMode.Open, BindingFlags.DeclaredOnly Or BindingFlags.Public, UIntField:=123 * Field)>
            Public Const Field As UInteger = 10
        End Class

        <AllInheritMultiple(New Char() {"q"c, "c"c}, "")>
        <AllInheritMultiple()>
        Enum NestedEnum
            zero
            one = 1
            <AllInheritMultiple(Nothing, 256, 0.0F, -1, AryField:=New ULong() {0, 1, 12345657})>
            <AllInheritMultipleAttribute(GetType(Dictionary(Of String, Integer)), 255 + NestedClass.Field, -0.0001F, 3 - CShort(NestedEnum.oneagain))>
            three = 3
            oneagain = one
        End Enum

    End Interface
End Namespace
                   ]]></file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(
                source,
                {MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01)},
                TestOptions.ReleaseDll)

            Dim attributeValidator =
                Function(isFromSource As Boolean) _
                    Sub(m As ModuleSymbol)
                        Dim attrs = m.GetAttributes()
                        Assert.Equal(1, attrs.Length)
                        Assert.Equal("CustomAttribute.AttrNameAttribute", attrs(0).AttributeClass.ToDisplayString)
                        attrs(0).VerifyValue(0, "TypeField", TypedConstantKind.Type, GetType(FileStream))

                        Dim assembly = m.ContainingSymbol
                        attrs = assembly.GetAttributes()
                        If isFromSource Then
                            Assert.Equal(2, attrs.Length)
                            Assert.Equal("CustomAttribute.AttrName", attrs(0).AttributeClass.ToDisplayString)
                            attrs(1).VerifyValue(Of UShort)(0, "UShortField", TypedConstantKind.Primitive, 1234)
                        Else
                            Assert.Equal(5, attrs.Length) ' 3 synthesized assembly attributes
                            Assert.Equal("CustomAttribute.AttrName", attrs(3).AttributeClass.ToDisplayString)
                            attrs(4).VerifyValue(Of UShort)(0, "UShortField", TypedConstantKind.Primitive, 1234)
                        End If

                        Dim ns = DirectCast(m.GlobalNamespace.GetMember("AttributeTest"), NamespaceSymbol)
                        Dim top = DirectCast(ns.GetMember("IGoo"), NamedTypeSymbol)
                        Dim type = top.GetMember(Of NamedTypeSymbol)("NestedClass")

                        Dim field = type.GetMember(Of FieldSymbol)("Field")
                        attrs = field.GetAttributes()
                        Assert.Equal("CustomAttribute.AllInheritMultipleAttribute", attrs(0).AttributeClass.ToDisplayString)
                        attrs(0).VerifyValue(0, TypedConstantKind.Enum, CInt(FileMode.Open))
                        attrs(0).VerifyValue(1, TypedConstantKind.Enum, CInt(BindingFlags.DeclaredOnly Or BindingFlags.Public))
                        attrs(0).VerifyValue(0, "UIntField", TypedConstantKind.Primitive, 1230)

                        Dim nenum = top.GetMember(Of TypeSymbol)("NestedEnum")
                        attrs = nenum.GetAttributes()
                        Assert.Equal(2, attrs.Length)
                        attrs(0).VerifyValue(0, TypedConstantKind.Array, {"q"c, "c"c})

                        attrs = nenum.GetMember("three").GetAttributes()
                        Assert.Equal(2, attrs.Length)
                        attrs(0).VerifyValue(Of Object)(0, TypedConstantKind.Primitive, Nothing)
                        attrs(0).VerifyValue(Of Long)(1, TypedConstantKind.Primitive, 256)
                        attrs(0).VerifyValue(Of Single)(2, TypedConstantKind.Primitive, 0)
                        attrs(0).VerifyValue(Of Short)(3, TypedConstantKind.Primitive, -1)
                        attrs(0).VerifyValue(Of ULong())(0, "AryField", TypedConstantKind.Array, New ULong() {0, 1, 12345657})

                        attrs(1).VerifyValue(Of Object)(0, TypedConstantKind.Type, GetType(Dictionary(Of String, Integer)))
                        attrs(1).VerifyValue(Of Long)(1, TypedConstantKind.Primitive, 265)
                        attrs(1).VerifyValue(Of Single)(2, TypedConstantKind.Primitive, -0.0001F)
                        attrs(1).VerifyValue(Of Short)(3, TypedConstantKind.Primitive, 2)

                    End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator(True), symbolValidator:=attributeValidator(False))
        End Sub

        <Fact>
        Public Sub TestAttributesOnDelegate()

            Dim source =
    <compilation>
        <file name="TestAttributesOnDelegate.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports CustomAttribute

Namespace AttributeTest

    Public Interface IGoo

        <AllInheritMultiple(New Object() {0, "", Nothing}, 255, -127 - 1, AryProp:=New Object() {New Object() {"", GetType(IList(Of String))}})>
        Delegate Sub NestedSubDele(<AllInheritMultiple()> <Derived(GetType(String(,,)))> p As String)

    End Interface
End Namespace
                   ]]></file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(
                source,
                {MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01)},
                TestOptions.ReleaseDll)

            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim ns = DirectCast(m.GlobalNamespace.GetMember("AttributeTest"), NamespaceSymbol)
                                         Dim type = DirectCast(ns.GetMember("IGoo"), NamedTypeSymbol)

                                         Dim dele = DirectCast(type.GetTypeMember("NestedSubDele"), NamedTypeSymbol)
                                         Dim attrs = dele.GetAttributes()
                                         attrs(0).VerifyValue(Of Object)(0, TypedConstantKind.Array, New Object() {0, "", Nothing})
                                         attrs(0).VerifyValue(Of Byte)(1, TypedConstantKind.Primitive, 255)
                                         attrs(0).VerifyValue(Of SByte)(2, TypedConstantKind.Primitive, -128)
                                         attrs(0).VerifyValue(Of Object())(0, "AryProp", TypedConstantKind.Array, New Object() {New Object() {"", GetType(IList(Of String))}})

                                         Dim mem = dele.GetMember(Of MethodSymbol)("Invoke")

                                         ' no attributes on the method:
                                         Assert.Equal(0, mem.GetAttributes().Length)

                                         ' attributes on parameters:
                                         attrs = mem.Parameters(0).GetAttributes()
                                         Assert.Equal(2, attrs.Length)
                                         attrs(1).VerifyValue(Of Object)(0, TypedConstantKind.Type, GetType(String(,,)))

                                     End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <WorkItem(540600, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540600")>
        <Fact>
        Public Sub TestAttributesUseBaseAttributeField()

            Dim source =
    <compilation>
        <file name="TestAttributesUseBaseAttributeField.vb"><![CDATA[
Imports System

Namespace AttributeTest

    Public Interface IGoo

        <CustomAttribute.Derived(New Object() {1, Nothing, "Hi"}, ObjectField:=2)>
        Function F(p As Integer) As Integer

    End Interface
End Namespace
                   ]]></file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(
                source,
                {MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01)},
                TestOptions.ReleaseDll)

            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim ns = DirectCast(m.GlobalNamespace.GetMember("AttributeTest"), NamespaceSymbol)
                                         Dim type = DirectCast(ns.GetMember("IGoo"), NamedTypeSymbol)
                                         Dim attrs = type.GetMember(Of MethodSymbol)("F").GetAttributes()

                                         Assert.Equal("CustomAttribute.DerivedAttribute", attrs(0).AttributeClass.ToDisplayString)
                                         attrs(0).VerifyValue(Of Object)(0, TypedConstantKind.Array, New Object() {1, Nothing, "Hi"})
                                         attrs(0).VerifyValue(Of Object)(0, "ObjectField", TypedConstantKind.Primitive, 2)
                                     End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <Fact(), WorkItem(529421, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529421")>
        Public Sub TestAttributesWithParamArrayInCtor()

            Dim source =
    <compilation>
        <file name="TestAttributesWithParamArrayInCtor.vb"><![CDATA[
Imports System
Imports CustomAttribute

Namespace AttributeTest

    <AllInheritMultiple(New Char() {" "c, Nothing}, "")>
    Public Interface IGoo
    End Interface
End Namespace
                   ]]></file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(
                source,
                {MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01)},
                TestOptions.ReleaseDll)

            Dim sourceAttributeValidator = Sub(m As ModuleSymbol)
                                               Dim ns = DirectCast(m.GlobalNamespace.GetMember("AttributeTest"), NamespaceSymbol)
                                               Dim type = DirectCast(ns.GetMember("IGoo"), NamedTypeSymbol)
                                               Dim attrs = type.GetAttributes()
                                               attrs(0).VerifyValue(Of Char())(0, TypedConstantKind.Array, New Char() {" "c, Nothing})
                                               attrs(0).VerifyValue(Of String())(1, TypedConstantKind.Array, New String() {""})

                                               Dim attrCtor = attrs(0).AttributeConstructor
                                               Debug.Assert(attrCtor.Parameters.Last.IsParamArray)
                                           End Sub

            Dim mdAttributeValidator = Sub(m As ModuleSymbol)
                                           Dim ns = DirectCast(m.GlobalNamespace.GetMember("AttributeTest"), NamespaceSymbol)
                                           Dim type = DirectCast(ns.GetMember("IGoo"), NamedTypeSymbol)
                                           Dim attrs = type.GetAttributes()
                                           attrs(0).VerifyValue(Of Char())(0, TypedConstantKind.Array, New Char() {" "c, Nothing})
                                           attrs(0).VerifyValue(Of String())(1, TypedConstantKind.Array, New String() {""})
                                       End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator:=sourceAttributeValidator, symbolValidator:=mdAttributeValidator)
        End Sub

        <WorkItem(540605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540605")>
        <Fact>
        Public Sub TestAttributesOnReturnType()

            Dim source =
    <compilation>
        <file name="TestAttributesOnReturnType.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports CustomAttribute

Namespace AttributeTest

    Class XAttribute
        inherits Attribute
            Sub New(s as string)
            End Sub
    End Class

    Public Interface IGoo
        Function F1(i as integer) as <X("f1 return type")> string

        Property P1 as <X("p1 return type")> string
    End Interface

    Class C1
        Property P1 as <X("p1 return type")> string
        ReadOnly Property P2 as <X("p2 return type")> string
            Get
                return nothing
            End Get
        End Property

        Function F2(i as integer) As <X("f2 returns an integer")> integer
           return 0
        end function
    End Class

End Namespace
                   ]]></file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(
                source,
                {MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01.AsImmutableOrNull())},
                TestOptions.ReleaseDll)

            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim ns = DirectCast(m.GlobalNamespace.GetMember("AttributeTest"), NamespaceSymbol)
                                         Dim iGoo = DirectCast(ns.GetMember("IGoo"), NamedTypeSymbol)

                                         Dim f1 = DirectCast(iGoo.GetMember("F1"), MethodSymbol)
                                         Dim attrs = f1.GetReturnTypeAttributes()
                                         attrs(0).VerifyValue(Of Object)(0, TypedConstantKind.Primitive, "f1 return type")

                                         Dim p1 = DirectCast(iGoo.GetMember("P1"), PropertySymbol)
                                         attrs = p1.GetMethod.GetReturnTypeAttributes()
                                         attrs(0).VerifyValue(Of Object)(0, TypedConstantKind.Primitive, "p1 return type")

                                         Dim c1 = DirectCast(ns.GetMember("C1"), NamedTypeSymbol)
                                         Dim p2 = DirectCast(c1.GetMember("P2"), PropertySymbol)
                                         attrs = p2.GetMethod.GetReturnTypeAttributes()
                                         attrs(0).VerifyValue(Of Object)(0, TypedConstantKind.Primitive, "p2 return type")

                                         Dim f2 = DirectCast(c1.GetMember("F2"), MethodSymbol)
                                         attrs = f2.GetReturnTypeAttributes()
                                         attrs(0).VerifyValue(Of Object)(0, TypedConstantKind.Primitive, "f2 returns an integer")
                                     End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <Fact>
        Public Sub TestAttributesUsageCasing()

            Dim source =
    <compilation>
        <file name="TestAttributesOnReturnType.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports CustomAttribute

Namespace AttributeTest

    <ATTributeUSageATTribute(AttributeTargets.ReturnValue, ALLowMulTiplE:=True, InHeRIteD:=false)>
    Class XAttribute
        Inherits Attribute

        Sub New()
        End Sub

    End Class

End Namespace
                   ]]></file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(
                source,
                {MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01)},
                TestOptions.ReleaseDll)

            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim ns = DirectCast(m.GlobalNamespace.GetMember("AttributeTest"), NamespaceSymbol)
                                         Dim xAttributeClass = DirectCast(ns.GetMember("XAttribute"), NamedTypeSymbol)

                                         Dim attributeUsage = xAttributeClass.GetAttributeUsageInfo()
                                         Assert.Equal(AttributeTargets.ReturnValue, attributeUsage.ValidTargets)
                                         Assert.Equal(True, attributeUsage.AllowMultiple)
                                         Assert.Equal(False, attributeUsage.Inherited)
                                     End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <WorkItem(540940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540940")>
        <Fact>
        Public Sub TestAttributeWithParamArray()

            Dim source =
    <compilation>
        <file name="TestAttributeWithParamArray.vb"><![CDATA[
Imports System

Class A
    Inherits Attribute
    Public Sub New(ParamArray x As Integer())
    End Sub
End Class

<A>
Module M
    Sub Main()
    End Sub
End Module
                   ]]></file>
    </compilation>

            CompileAndVerify(source)
        End Sub

        <WorkItem(528469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528469")>
        <Fact()>
        Public Sub TestAttributeWithAttributeTargets()

            Dim source =
    <compilation>
        <file name="TestAttributeWithParamArray.vb"><![CDATA[
Imports System
<System.AttributeUsage(AttributeTargets.All)> _
Class ZAttribute
    Inherits Attribute
End Class
<Z()> _
Class scen1
End Class
Module M1
    Sub goo()
        <Z()> _
        Static x1 As Object
    End Sub
End Module
                   ]]></file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompileAndVerify(compilation)
        End Sub

        <WorkItem(541277, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541277")>
        <Fact>
        Public Sub TestAttributeEmitObjectValue()

            Dim source =
    <compilation>
        <file name="TestAttributeEmitObjectValue.vb"><![CDATA[
Imports System

<AttributeUsageAttribute(AttributeTargets.All, AllowMultiple:=True)>
Class A
    Inherits Attribute
    Public Property X As Object

    Public Sub New()
    End Sub

    Public Sub New(o As Object)
    End Sub
End Class

<A(1)>
<A(New String() {"a", "b"})>
<A(X:=1), A(X:=New String() {"a", "b"})>
Class B
    Shared Sub Main()
        Dim b As New B()
        Dim a As A = b.GetType().GetCustomAttributes(False)(0)
        Console.WriteLine(a.X)
    End Sub
End Class

                   ]]></file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source)
            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim bClass = DirectCast(m.GlobalNamespace.GetMember("B"), NamedTypeSymbol)
                                         Dim attrs = bClass.GetAttributes()
                                         attrs(0).VerifyValue(Of Object)(0, TypedConstantKind.Primitive, 1)
                                         attrs(1).VerifyValue(Of Object)(0, TypedConstantKind.Array, New String() {"a", "b"})
                                         attrs(2).VerifyValue(Of Object)(0, "X", TypedConstantKind.Primitive, 1)
                                         attrs(3).VerifyValue(Of Object)(0, "X", TypedConstantKind.Array, New String() {"a", "b"})
                                     End Sub
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <WorkItem(541278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541278")>
        <Fact>
        Public Sub TestAttributeEmitGenericEnumValue()

            Dim source =
    <compilation>
        <file name="TestAttributeEmitObjectValue.vb"><![CDATA[
        Imports System
        Class A
            Inherits Attribute
            Public Sub New(x As Object)
                Y = x
            End Sub

            Public Property Y As Object
        End Class

        Class B(Of T)
            Class D
            End Class
            Enum E
                A = &HABCDEF
            End Enum
        End Class

        <A(B(Of Integer).E.A)>
        Class C
        End Class

        Module m1
            Sub Main()
            End Sub
        End Module

]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim cClass = DirectCast(m.GlobalNamespace.GetMember("C"), NamedTypeSymbol)
                                         Dim attrs = cClass.GetAttributes()
                                         Dim tc = attrs(0).CommonConstructorArguments(0)
                                         Assert.Equal(tc.Kind, TypedConstantKind.Enum)
                                         Assert.Equal(CType(tc.Value, Int32), &HABCDEF)
                                         Assert.Equal(tc.Type.ToDisplayString, "B(Of Integer).E")
                                     End Sub
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <WorkItem(546380, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546380")>
        <Fact()>
        Public Sub TestAttributeEmitOpenGeneric()

            Dim source =
    <compilation>
        <file name="TestAttributeEmitObjectValue.vb"><![CDATA[
        Imports System
        Class A
            Inherits Attribute
            Public Sub New(x As Object)
                Y = x
            End Sub

            Public Property Y As Object
        End Class

        <A(GetType(B(Of)))>
        Class B(Of T)
        End Class

        Module m1
            Sub Main()
            End Sub
        End Module

]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim bClass = DirectCast(m.GlobalNamespace.GetMember("B"), NamedTypeSymbol)
                                         Dim attrs = bClass.GetAttributes()
                                         attrs(0).VerifyValue(0, TypedConstantKind.Type, bClass.ConstructUnboundGenericType())
                                     End Sub
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <WorkItem(541278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541278")>
        <Fact>
        Public Sub TestAttributeToString()

            Dim source =
    <compilation>
        <file name="TestAttributeToString.vb"><![CDATA[
        Imports System

<AttributeUsageAttribute(AttributeTargets.Class Or AttributeTargets.Struct, AllowMultiple:=True)>
Class A
    Inherits Attribute
    Public Property X As Object
    Public Property T As Type

    Public Sub New()
    End Sub

    Public Sub New(ByVal o As Object)
    End Sub

    Public Sub New(ByVal y As Y)
    End Sub
End Class

Public Enum Y
    One = 1
    Two = 2
    Three = 3
End Enum

<A()>
Class B1
    Shared Sub Main()
    End Sub
End Class

<A(1)>
Class B2
End Class

<A(New String() {"a", "b"})>
Class B3
End Class

<A(X:=1)>
Class B4
End Class

<A(T:=GetType(String))>
Class B5
End Class

<A(1, X:=1, T:=GetType(String))>
Class B6
End Class

<A(Y.Three)>
Class B7
End Class

<A(DirectCast(5, Y))>
Class B8
End Class
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source)
            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim aClass = DirectCast(m.GlobalNamespace.GetMember("A"), NamedTypeSymbol)
                                         Dim attrs = aClass.GetAttributes()
                                         Assert.Equal("System.AttributeUsageAttribute(System.AttributeTargets.Class Or System.AttributeTargets.Struct, AllowMultiple:=True)",
                                                      attrs(0).ToString())

                                         Dim bClass = DirectCast(m.GlobalNamespace.GetMember("B1"), NamedTypeSymbol)
                                         attrs = bClass.GetAttributes()
                                         Assert.Equal("A", attrs(0).ToString())

                                         bClass = DirectCast(m.GlobalNamespace.GetMember("B2"), NamedTypeSymbol)
                                         attrs = bClass.GetAttributes()
                                         Assert.Equal("A(1)", attrs(0).ToString())

                                         bClass = DirectCast(m.GlobalNamespace.GetMember("B3"), NamedTypeSymbol)
                                         attrs = bClass.GetAttributes()
                                         Assert.Equal("A({""a"", ""b""})", attrs(0).ToString())

                                         bClass = DirectCast(m.GlobalNamespace.GetMember("B4"), NamedTypeSymbol)
                                         attrs = bClass.GetAttributes()
                                         Assert.Equal("A(X:=1)", attrs(0).ToString())

                                         bClass = DirectCast(m.GlobalNamespace.GetMember("B5"), NamedTypeSymbol)
                                         attrs = bClass.GetAttributes()
                                         Assert.Equal("A(T:=GetType(String))", attrs(0).ToString())

                                         bClass = DirectCast(m.GlobalNamespace.GetMember("B6"), NamedTypeSymbol)
                                         attrs = bClass.GetAttributes()
                                         Assert.Equal("A(1, X:=1, T:=GetType(String))", attrs(0).ToString())

                                         bClass = DirectCast(m.GlobalNamespace.GetMember("B7"), NamedTypeSymbol)
                                         attrs = bClass.GetAttributes()
                                         Assert.Equal("A(Y.Three)", attrs(0).ToString())

                                         bClass = DirectCast(m.GlobalNamespace.GetMember("B8"), NamedTypeSymbol)
                                         attrs = bClass.GetAttributes()
                                         Assert.Equal("A(5)", attrs(0).ToString())
                                     End Sub
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <WorkItem(541687, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541687")>
        <Fact>
        Public Sub Bug_8524_NullAttributeArrayArgument()

            Dim source =
    <compilation>
        <file><![CDATA[
Imports System

Class A
    Inherits Attribute
    Public Property X As Object
End Class

<A(X:=CStr(Nothing))>
Class B
    Shared Sub Main()
        Dim b As New B()
        Dim a As A = b.GetType().GetCustomAttributes(False)(0)
        Console.Write(a.X Is Nothing)
    End Sub
End Class
]]>
        </file>
    </compilation>

            CompileAndVerify(source, expectedOutput:="True")
        End Sub

        <WorkItem(541964, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541964")>
        <Fact>
        Public Sub TestApplyNamedArgumentTwice()
            Dim source =
    <compilation>
        <file name="TestApplyNamedArgumentTwice.vb"><![CDATA[
Imports System
<AttributeUsage(AttributeTargets.All, AllowMultiple:=False, AllowMultiple:=True), A, A>
Class A
    Inherits Attribute

End Class

Module Module1
    Sub Main()
    End Sub
End Module
]]>
        </file>
    </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim aClass = DirectCast(m.GlobalNamespace.GetMember("A"), NamedTypeSymbol)
                                         Dim attrs = aClass.GetAttributes()
                                         Assert.Equal("System.AttributeUsageAttribute(System.AttributeTargets.All, AllowMultiple:=False, AllowMultiple:=True)",
                                                      attrs(0).ToString())
                                     End Sub
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <Fact>
        Public Sub TestApplyNamedArgumentCasing()
            Dim source =
    <compilation>
        <file name="TestApplyNamedArgumentTwice.vb"><![CDATA[
Imports System
<AttributeUsage(AttributeTargets.All, ALLOWMULTIPLE:=True), A, A>
Class A
    Inherits Attribute

End Class

Module Module1
    Sub Main()
    End Sub
End Module
]]>
        </file>
    </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim aClass = DirectCast(m.GlobalNamespace.GetMember("A"), NamedTypeSymbol)
                                         Dim attrs = aClass.GetAttributes()
                                         Assert.Equal("System.AttributeUsageAttribute(System.AttributeTargets.All, AllowMultiple:=True)",
                                                      attrs(0).ToString())
                                     End Sub
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <WorkItem(542123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542123")>
        <Fact>
        Public Sub TestApplyNestedDerivedAttribute()
            Dim source =
    <compilation>
        <file name="TestApplyNestedDerivedAttribute.vb"><![CDATA[
Imports System

<First.Second.Third()>
Class First : Inherits Attribute
    Class Second : Inherits First
    End Class
    Class Third : Inherits Second
    End Class
End Class

Module Module1
    Sub Main()
    End Sub
End Module
]]>
        </file>
    </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.VerifyDiagnostics()
        End Sub

        <WorkItem(542269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542269")>
        <Fact>
        Public Sub TestApplyNestedDerivedAttributeOnTypeAndItsMember()
            Dim source =
    <compilation>
        <file name="TestApplyNestedDerivedAttributeOnTypeAndItsMember.vb"><![CDATA[
Imports System
Public Class First : Inherits Attribute
    <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Method)>
    Friend Class Second
        Inherits First
    End Class
End Class

<First.Second()>
Module Module1
    <First.Second()>
    Function ToString(x As Integer, y As Integer) As String
        Return Nothing
    End Function
    Public Sub Main()
    End Sub
End Module
]]>
        </file>
    </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeTargets_Method()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.Method)>
Class Attr
    Inherits Attribute
End Class

Public Class C
    <Attr()>
    Custom Event EvntWithAccessors As Action(Of Integer)
        <Attr()>
        AddHandler(value As Action(Of Integer))
        End AddHandler

        <Attr()>
        RemoveHandler(value As Action(Of Integer))
        End RemoveHandler

        <Attr()>
        RaiseEvent(obj As Integer)
        End RaiseEvent
    End Event

    <Attr()>
    Property PropertyWithAccessors As Integer
        <Attr()>
        Get
            Return 0
        End Get

        <Attr()>
        Set(value As Integer)

        End Set
    End Property

    <Attr()>
    Shared Sub Sub1()
    End Sub

    <Attr()>
    Function Ftn2() As Integer
        Return 1
    End Function

    <Attr()>
    Declare Function DeclareFtn Lib "bar" () As Integer

    <Attr()>
    Declare Sub DeclareSub Lib "bar" ()

    <Attr()>
    Shared Operator -(a As C, b As C) As Integer
        Return 0
    End Operator

    <Attr()>
    Shared Narrowing Operator CType(a As C) As Integer
        Return 0
    End Operator

    <Attr()>
    Public Event Evnt As Action(Of Integer)

    <Attr()>
    Public Field As Integer

    <Attr()>
    Public WithEvents WE As NestedType

    <Attr()>
    Class NestedType
    End Class

    <Attr()>
    Interface Iface
    End Interface
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "EvntWithAccessors"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "PropertyWithAccessors"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "Evnt"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "Field"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "WE"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "NestedType"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "Iface"))
        End Sub

        <Fact>
        Public Sub AttributeTargets_Field()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.Field)>
Class Attr
    Inherits Attribute
End Class

Public Class C
    <Attr()>
    Custom Event EvntWithAccessors As Action(Of Integer)
        <Attr()>
        AddHandler(value As Action(Of Integer))
        End AddHandler

        <Attr()>
        RemoveHandler(value As Action(Of Integer))
        End RemoveHandler

        <Attr()>
        RaiseEvent(obj As Integer)
        End RaiseEvent
    End Event

    <Attr()>
    Property PropertyWithAccessors As Integer
        <Attr()>
        Get
            Return 0
        End Get

        <Attr()>
        Set(value As Integer)

        End Set
    End Property

    <Attr()>
    Shared Sub Sub1()
    End Sub

    <Attr()>
    Function Ftn2() As Integer
        Return 1
    End Function

    <Attr()>
    Declare Function DeclareFtn Lib "bar" () As Integer

    <Attr()>
    Declare Sub DeclareSub Lib "bar" ()

    <Attr()>
    Shared Operator -(a As C, b As C) As Integer
        Return 0
    End Operator

    <Attr()>
    Shared Narrowing Operator CType(a As C) As Integer
        Return 0
    End Operator

    <Attr()>
    Public Event Evnt As Action(Of Integer)

    <Attr()>
    Public Field As Integer

    <Attr()>
    Public WithEvents WE As NestedType

    <Attr()>
    Class NestedType
    End Class

    <Attr()>
    Interface Iface
    End Interface
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "EvntWithAccessors"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsageOnAccessor, "Attr").WithArguments("Attr", "AddHandler", "EvntWithAccessors"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsageOnAccessor, "Attr").WithArguments("Attr", "RemoveHandler", "EvntWithAccessors"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsageOnAccessor, "Attr").WithArguments("Attr", "RaiseEvent", "EvntWithAccessors"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "PropertyWithAccessors"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsageOnAccessor, "Attr").WithArguments("Attr", "Get", "PropertyWithAccessors"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsageOnAccessor, "Attr").WithArguments("Attr", "Set", "PropertyWithAccessors"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "Sub1"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "Ftn2"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "DeclareFtn"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "DeclareSub"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "-"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "CType"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "Evnt"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "NestedType"),
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "Attr").WithArguments("Attr", "Iface"))
        End Sub

        <Fact>
        Public Sub AttributeTargets_WithEvents()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class A 
    Inherits Attribute
End Class

Class D
  <A>
  Public WithEvents myButton As Button
End Class

Class Button
    Public Event OnClick()
End Class
]]>
    </file>
</compilation>

            Dim c = CreateCompilationWithMscorlib40(source)

            Dim d = DirectCast(c.GlobalNamespace.GetMembers("D").Single(), NamedTypeSymbol)
            Dim myButton = DirectCast(d.GetMembers("myButton").Single(), PropertySymbol)

            Assert.Equal(0, myButton.GetAttributes().Length)
            Dim attr = myButton.GetFieldAttributes().Single()
            Assert.Equal("A", attr.AttributeClass.Name)
        End Sub

        <Fact>
        Public Sub AttributeTargets_WithEventsGenericInstantiation()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class A 
    Inherits Attribute
End Class

Class D(Of T As Button)
  <A>
  Public WithEvents myButton As T
End Class

Class Button
    Public Event OnClick()
End Class
]]>
    </file>
</compilation>

            Dim c = CreateCompilationWithMscorlib40(source)

            Dim d = DirectCast(c.GlobalNamespace.GetMembers("D").Single(), NamedTypeSymbol)
            Dim button = DirectCast(c.GlobalNamespace.GetMembers("Button").Single(), NamedTypeSymbol)
            Dim dOfButton = d.Construct(button)
            Dim myButton = DirectCast(dOfButton.GetMembers("myButton").Single(), PropertySymbol)

            Assert.Equal(0, myButton.GetAttributes().Length)
            Dim attr = myButton.GetFieldAttributes().Single()
            Assert.Equal("A", attr.AttributeClass.Name)
        End Sub

        <Fact>
        Public Sub AttributeTargets_Events()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class A 
    Inherits Attribute
End Class

<Serializable>
Class D
  <A, NonSerialized>
  Public Event OnClick As Action
End Class
]]>
    </file>
</compilation>

            Dim c = CreateCompilationWithMscorlib40(source)
            c.VerifyDiagnostics()

            Dim d = DirectCast(c.GlobalNamespace.GetMembers("D").Single(), NamedTypeSymbol)
            Dim onClick = DirectCast(d.GetMembers("OnClick").Single(), EventSymbol)

            ' TODO (tomat): move NonSerialized attribute onto the associated field
            Assert.Equal(2, onClick.GetAttributes().Length)       ' should be 1 
            Assert.Equal(0, onClick.GetFieldAttributes().Length)  ' should be 1 
        End Sub

        <Fact, WorkItem(546769, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546769"), WorkItem(546770, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546770")>
        Public Sub DiagnosticsOnEventParameters()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class C
    Event E(<MarshalAs()> x As Integer)
End Class
]]>
    </file>
</compilation>

            Dim c = CreateCompilationWithMscorlib40(source)

            c.AssertTheseDiagnostics(<![CDATA[
BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.
    Event E(<MarshalAs()> x As Integer)
             ~~~~~~~~~
]]>)
        End Sub

        <Fact, WorkItem(528748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528748")>
        Public Sub TestNonPublicConstructor()
            Dim source =
<compilation>
    <file name="TestNonPublicConstructor.vb"><![CDATA[
<Fred(1)>
Class Class1
End Class

Class FredAttribute : Inherits System.Attribute
    Public Sub New(x As Integer, Optional y As Integer = 1)

    End Sub
    Friend Sub New(x As Integer)

    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_BadAttributeNonPublicConstructor, "Fred"))
        End Sub

        <WorkItem(542223, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542223")>
        <Fact>
        Public Sub AttributeArgumentAsEnumFromMetadata()
            Dim metadata1 = VisualBasicCompilation.Create("bar.dll",
                                               references:={MscorlibRef},
                                               syntaxTrees:={Parse("Public Enum Bar : Baz : End Enum")}).EmitToArray(New EmitOptions(metadataOnly:=True))

            Dim ref1 = MetadataReference.CreateFromImage(metadata1)

            Dim metadata2 = VisualBasicCompilation.Create(
                                "goo.dll",
                                references:={MscorlibRef, ref1},
                                syntaxTrees:={
                                    VisualBasicSyntaxTree.ParseText(<![CDATA[
                                        Public Class Ca : Inherits System.Attribute
                                            Public Sub New(o As Object)
                                            End Sub
                                        End Class
                                        <Ca(Bar.Baz)>
                                        Public Class Goo
                                        End Class]]>.Value)}).EmitToArray(options:=New EmitOptions(metadataOnly:=True))

            Dim ref2 = MetadataReference.CreateFromImage(metadata2)

            Dim comp = VisualBasicCompilation.Create("moo.dll", references:={MscorlibRef, ref1, ref2})

            Dim goo = comp.GetTypeByMetadataName("Goo")
            Dim ca = goo.GetAttributes().First().CommonConstructorArguments.First()

            Assert.Equal("Bar", ca.Type.Name)
        End Sub

        <Fact>
        Public Sub TestAttributeWithNestedUnboundGeneric()
            Dim library =
    <file name="Library.vb"><![CDATA[
Namespace ClassLibrary1
Public Class C1(Of T1)
    Public Class C2(Of T2, T3)
    End Class
End Class
End Namespace
]]>
    </file>

            Dim compilation1 = VisualBasicCompilation.Create("library.dll",
                                                             {VisualBasicSyntaxTree.ParseText(library.Value)},
                                                             {MscorlibRef},
                                                             TestOptions.ReleaseDll)

            Dim classLibrary = MetadataReference.CreateFromImage(compilation1.EmitToArray())

            Dim source =
        <compilation>
            <file name="TestAttributeWithNestedUnboundGeneric.vb"><![CDATA[
Imports System

Class A
    Inherits Attribute

    Public Sub New(o As Object)
    End Sub
End Class

<A(GetType(ClassLibrary1.C1(Of )))>
Module Module1
    Sub Main()
    End Sub
End Module
]]>
            </file>
        </compilation>
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(source, {SystemRef, MsvbRef, classLibrary})
            compilation2.VerifyDiagnostics()

            Dim a = compilation2.GetTypeByMetadataName("Module1")
            Dim gt = a.GetAttributes().First().CommonConstructorArguments.First()
            Assert.False(DirectCast(gt.Value, TypeSymbol).IsErrorType)
            Dim arg = DirectCast(gt.Value, UnboundGenericType)
            Assert.Equal("ClassLibrary1.C1(Of )", arg.ToDisplayString)
            Assert.False(DirectCast(arg, INamedTypeSymbol).IsSerializable)
        End Sub

        <Fact>
        Public Sub TestAttributeWithAliasToUnboundGeneric()
            Dim library =
    <file name="Library.vb"><![CDATA[
Namespace ClassLibrary1
Public Class C1(Of T1)
    Public Class C2(Of T2, T3)
    End Class
End Class
End Namespace
]]>
    </file>

            Dim compilation1 = VisualBasicCompilation.Create("library.dll",
                                                             {VisualBasicSyntaxTree.ParseText(library.Value)},
                                                             {MscorlibRef},
                                                             TestOptions.ReleaseDll)

            Dim classLibrary = MetadataReference.CreateFromImage(compilation1.EmitToArray())

            Dim source =
        <compilation>
            <file name="TestAttributeWithAliasToUnboundGeneric.vb"><![CDATA[
Imports System
Imports x = ClassLibrary1

Class A
    Inherits Attribute

    Public Sub New(o As Object)
    End Sub
End Class

<A(GetType(x.C1(Of )))>
Module Module1
    Sub Main()
    End Sub
End Module
]]>
            </file>
        </compilation>
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(source, {SystemRef, MsvbRef, classLibrary})
            compilation2.VerifyDiagnostics()

            Dim a = compilation2.GetTypeByMetadataName("Module1")
            Dim gt = a.GetAttributes().First().CommonConstructorArguments.First()
            Assert.False(DirectCast(gt.Value, TypeSymbol).IsErrorType)
            Dim arg = DirectCast(gt.Value, UnboundGenericType)
            Assert.Equal("ClassLibrary1.C1(Of )", arg.ToDisplayString)
        End Sub

        <Fact>
        Public Sub TestAttributeWithArrayOfUnboundGeneric()

            Dim source =
        <compilation>
            <file name="TestAttributeWithArrayOfUnboundGeneric.vb"><![CDATA[
Imports System
Class C1(of T)
End Class

Class A
    Inherits Attribute

    Public Sub New(o As Object)
    End Sub
End Class

<A(GetType(C1(Of )()))>
Module Module1
    Sub Main()
    End Sub
End Module
]]>
            </file>
        </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndReferences(source, {SystemRef, MsvbRef})
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_ArrayOfRawGenericInvalid, "()"))

            Dim a = compilation.GetTypeByMetadataName("Module1")
            Dim gt = a.GetAttributes().First().CommonConstructorArguments.First()
            Assert.False(DirectCast(gt.Value, TypeSymbol).IsErrorType)
            Dim arg = DirectCast(gt.Value, ArrayTypeSymbol)
            Assert.Equal("C1(Of ?)()", arg.ToDisplayString)
        End Sub

        <Fact>
        Public Sub TestAttributeWithNullableUnboundGeneric()

            Dim source =
        <compilation>
            <file name="TestAttributeWithNullableUnboundGeneric.vb"><![CDATA[
Imports System
Class C1(of T)
End Class

Class A
    Inherits Attribute

    Public Sub New(o As Object)
    End Sub
End Class

<A(GetType(C1(Of )?))>
Module Module1
    Sub Main()
    End Sub
End Module
]]>
            </file>
        </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndReferences(source, {SystemRef, MsvbRef})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors><![CDATA[
BC33101: Type 'C1(Of ?)' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
<A(GetType(C1(Of )?))>
           ~~~~~~~
BC30182: Type expected.
<A(GetType(C1(Of )?))>
                 ~
]]>
</errors>)
            Dim a = compilation.GetTypeByMetadataName("Module1")
            Dim gt = a.GetAttributes().First().CommonConstructorArguments.First()
            Assert.False(DirectCast(gt.Value, TypeSymbol).IsErrorType)
            Dim arg = DirectCast(gt.Value, SubstitutedNamedType)
            Assert.Equal("C1(Of ?)?", arg.ToDisplayString)
        End Sub

        <Fact()>
        Public Sub TestConstantValueInsideAttributes()
            Dim tree = VisualBasicSyntaxTree.ParseText(<![CDATA[
Class c1
    const A as integer = 1;
    const B as integer = 2;

    class MyAttribute : inherits Attribute
        Sub New(i as integer)
        End Sub
    end class

    <MyAttribute(A + B + 3)>
    Sub Goo()
    End Sub
End Class"]]>.Value)
            Dim expr = tree.GetRoot().DescendantNodes().OfType(Of BinaryExpressionSyntax).First()
            Dim comp = CreateCompilationWithMscorlib40({tree})
            Dim constantValue = comp.GetSemanticModel(tree).GetConstantValue(expr)
            Assert.True(constantValue.HasValue)
            Assert.Equal(constantValue.Value, 6)
        End Sub

        <Fact>
        Public Sub TestArrayTypeInAttributeArgument()
            Dim source =
    <compilation>
        <file name="TestArrayTypeInAttributeArgument.vb"><![CDATA[
Imports System

Public Class W
End Class

Public Class Y(Of T)
    Public Class F
    End Class

    Public Class Z(Of U)
    End Class
End Class

Public Class XAttribute
    Inherits Attribute
    Public Sub New(y As Object)
    End Sub
End Class

<X(GetType(W()))> _
Public Class C1
End Class

<X(GetType(W(,)))> _
Public Class C2
End Class

<X(GetType(W(,)()))> _
Public Class C3
End Class

<X(GetType(Y(Of W)()(,)))> _
Public Class C4
End Class

<X(GetType(Y(Of Integer).F(,)()(,,)))> _
Public Class C5
End Class

<X(GetType(Y(Of Integer).Z(Of W)(,)()))> _
Public Class C6
End Class
]]>
        </file>
    </compilation>

            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim classW As NamedTypeSymbol = m.GlobalNamespace.GetTypeMember("W")
                                         Dim classY As NamedTypeSymbol = m.GlobalNamespace.GetTypeMember("Y")
                                         Dim classF As NamedTypeSymbol = classY.GetTypeMember("F")
                                         Dim classZ As NamedTypeSymbol = classY.GetTypeMember("Z")
                                         Dim classX As NamedTypeSymbol = m.GlobalNamespace.GetTypeMember("XAttribute")

                                         Dim classC1 As NamedTypeSymbol = m.GlobalNamespace.GetTypeMember("C1")
                                         Dim classC2 As NamedTypeSymbol = m.GlobalNamespace.GetTypeMember("C2")
                                         Dim classC3 As NamedTypeSymbol = m.GlobalNamespace.GetTypeMember("C3")
                                         Dim classC4 As NamedTypeSymbol = m.GlobalNamespace.GetTypeMember("C4")
                                         Dim classC5 As NamedTypeSymbol = m.GlobalNamespace.GetTypeMember("C5")
                                         Dim classC6 As NamedTypeSymbol = m.GlobalNamespace.GetTypeMember("C6")

                                         Dim attrs = classC1.GetAttributes()
                                         Assert.Equal(1, attrs.Length)
                                         Dim typeArg = ArrayTypeSymbol.CreateVBArray(classW, Nothing, 1, m.ContainingAssembly)
                                         attrs.First().VerifyValue(Of Object)(0, TypedConstantKind.Type, typeArg)

                                         attrs = classC2.GetAttributes()
                                         Assert.Equal(1, attrs.Length)
                                         typeArg = ArrayTypeSymbol.CreateVBArray(classW, CType(Nothing, ImmutableArray(Of CustomModifier)), rank:=2, declaringAssembly:=m.ContainingAssembly)
                                         attrs.First().VerifyValue(Of Object)(0, TypedConstantKind.Type, typeArg)

                                         attrs = classC3.GetAttributes()
                                         Assert.Equal(1, attrs.Length)
                                         typeArg = ArrayTypeSymbol.CreateVBArray(classW, Nothing, 1, m.ContainingAssembly)
                                         typeArg = ArrayTypeSymbol.CreateVBArray(typeArg, CType(Nothing, ImmutableArray(Of CustomModifier)), rank:=2, declaringAssembly:=m.ContainingAssembly)
                                         attrs.First().VerifyValue(Of Object)(0, TypedConstantKind.Type, typeArg)

                                         attrs = classC4.GetAttributes()
                                         Assert.Equal(1, attrs.Length)
                                         Dim classYOfW As NamedTypeSymbol = classY.Construct(ImmutableArray.Create(Of TypeSymbol)(classW))
                                         typeArg = ArrayTypeSymbol.CreateVBArray(classYOfW, CType(Nothing, ImmutableArray(Of CustomModifier)), rank:=2, declaringAssembly:=m.ContainingAssembly)
                                         typeArg = ArrayTypeSymbol.CreateVBArray(typeArg, Nothing, 1, m.ContainingAssembly)
                                         attrs.First().VerifyValue(Of Object)(0, TypedConstantKind.Type, typeArg)

                                         attrs = classC5.GetAttributes()
                                         Assert.Equal(1, attrs.Length)
                                         Dim classYOfInt As NamedTypeSymbol = classY.Construct(ImmutableArray.Create(Of TypeSymbol)(m.ContainingAssembly.GetSpecialType(SpecialType.System_Int32)))
                                         Dim substNestedF As NamedTypeSymbol = classYOfInt.GetTypeMember("F")
                                         typeArg = ArrayTypeSymbol.CreateVBArray(substNestedF, CType(Nothing, ImmutableArray(Of CustomModifier)), rank:=3, declaringAssembly:=m.ContainingAssembly)
                                         typeArg = ArrayTypeSymbol.CreateVBArray(typeArg, Nothing, 1, m.ContainingAssembly)
                                         typeArg = ArrayTypeSymbol.CreateVBArray(typeArg, CType(Nothing, ImmutableArray(Of CustomModifier)), rank:=2, declaringAssembly:=m.ContainingAssembly)
                                         attrs.First().VerifyValue(Of Object)(0, TypedConstantKind.Type, typeArg)

                                         attrs = classC6.GetAttributes()
                                         Assert.Equal(1, attrs.Length)
                                         Dim substNestedZ As NamedTypeSymbol = classYOfInt.GetTypeMember("Z").Construct(ImmutableArray.Create(Of TypeSymbol)(classW))
                                         typeArg = ArrayTypeSymbol.CreateVBArray(substNestedZ, Nothing, 1, m.ContainingAssembly)
                                         typeArg = ArrayTypeSymbol.CreateVBArray(typeArg, CType(Nothing, ImmutableArray(Of CustomModifier)), rank:=2, declaringAssembly:=m.ContainingAssembly)
                                         attrs.First().VerifyValue(Of Object)(0, TypedConstantKind.Type, typeArg)
                                     End Sub

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompileAndVerify(compilation, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <Fact>
        Public Sub TestAttributeCallerInfoSemanticModel()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices

Class Attr
    Inherits Attribute

    Public Sub New(<CallerMemberName> Optional s As String = Nothing)
    End Sub
End Class

Class C
    <Attr()>'BIND:""Attr""
    Sub M0()
    End Sub
End Class
"
            Dim comp = CreateCompilation(source)
            comp.VerifyDiagnostics()
            Dim tree = comp.SyntaxTrees(0)
            Dim root = tree.GetRoot()
            Dim attrSyntax = root.DescendantNodes().OfType(Of AttributeSyntax)().Last()
            Dim semanticModel = comp.GetSemanticModel(tree)
            Dim m0 = semanticModel.GetDeclaredSymbol(root.DescendantNodes().OfType(Of MethodStatementSyntax)().Last())
            Dim attrs = m0.GetAttributes()
            Assert.Equal("M0", attrs.Single().ConstructorArguments.Single().Value)
            Dim expectedTree As String = "
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'Attr()')
  IObjectCreationOperation (Constructor: Sub Attr..ctor([s As System.String = Nothing])) (OperationKind.ObjectCreation, Type: Attr, IsImplicit) (Syntax: 'Attr()')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Attr')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""M0"", IsImplicit) (Syntax: 'Attr')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
"
            VerifyOperationTreeForTest(Of AttributeSyntax)(comp, fileName:="", expectedTree)

            Dim operation = semanticModel.GetOperation(attrSyntax)
            Dim operationTreeFromSemanticModel = OperationTreeVerifier.GetOperationTree(comp, operation)
            OperationTreeVerifier.Verify(expectedTree, operationTreeFromSemanticModel)
        End Sub

        <Fact>
        Public Sub TestAttributeCallerInfoSemanticModel_Method_Speculative()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices

Class Attr 
    Inherits Attribute

    Public Sub New(<CallerMemberName> Optional s As String = Nothing)
    End Sub
End Class

Class C
    <Attr(""a"")>
    Sub M0()
    End Sub
End Class
"
            Dim comp = CreateCompilation(source)
            comp.VerifyDiagnostics()
            Dim tree = comp.SyntaxTrees(0)
            Dim root = tree.GetRoot()
            Dim attrSyntax = root.DescendantNodes().OfType(Of AttributeSyntax)().Last()
            Dim semanticModel = comp.GetSemanticModel(tree)
            Dim newRoot = root.ReplaceNode(attrSyntax, attrSyntax.WithArgumentList(SyntaxFactory.ParseArgumentList("()")))
            Dim newAttrSyntax = newRoot.DescendantNodes().OfType(Of AttributeSyntax)().Last()
            Dim speculativeModel As SemanticModel = Nothing
            Assert.True(semanticModel.TryGetSpeculativeSemanticModel(attrSyntax.ArgumentList.Position, newAttrSyntax, speculativeModel))
            Dim speculativeOperation = speculativeModel.GetOperation(newAttrSyntax)
            Dim expectedTree As String = "
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'Attr()')
  IObjectCreationOperation (Constructor: Sub Attr..ctor([s As System.String = Nothing])) (OperationKind.ObjectCreation, Type: Attr, IsImplicit) (Syntax: 'Attr()')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Attr')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""M0"", IsImplicit) (Syntax: 'Attr')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
"
            Dim speculativeOperationTree = OperationTreeVerifier.GetOperationTree(comp, speculativeOperation)
            OperationTreeVerifier.Verify(expectedTree, speculativeOperationTree)
        End Sub

        <Fact>
        Public Sub TestAttributeCallerInfoSemanticModel_Parameter_Speculative()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices

Class Attr
    Inherits Attribute

    Public Sub New(<CallerMemberName> Optional s As String = Nothing)
    End Sub
End Class

Class C
    Sub M0(<Attr(""a"")> x As Integer)
    End Sub
End Class
"
            Dim comp = CreateCompilation(source)
            comp.VerifyDiagnostics()
            Dim tree = comp.SyntaxTrees(0)
            Dim root = tree.GetRoot()
            Dim attrSyntax = root.DescendantNodes().OfType(Of AttributeSyntax)().Last()
            Dim semanticModel = comp.GetSemanticModel(tree)
            Dim newRoot = root.ReplaceNode(attrSyntax, attrSyntax.WithArgumentList(SyntaxFactory.ParseArgumentList("()")))
            Dim newAttrSyntax = newRoot.DescendantNodes().OfType(Of AttributeSyntax)().Last()
            Dim speculativeModel As SemanticModel = Nothing
            Assert.True(semanticModel.TryGetSpeculativeSemanticModel(attrSyntax.ArgumentList.Position, newAttrSyntax, speculativeModel))
            Dim speculativeOperation = speculativeModel.GetOperation(newAttrSyntax)
            Dim expectedTree As String = "
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'Attr()')
  IObjectCreationOperation (Constructor: Sub Attr..ctor([s As System.String = Nothing])) (OperationKind.ObjectCreation, Type: Attr, IsImplicit) (Syntax: 'Attr()')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Attr')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""M0"", IsImplicit) (Syntax: 'Attr')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
"
            Dim speculativeOperationTree = OperationTreeVerifier.GetOperationTree(comp, speculativeOperation)
            OperationTreeVerifier.Verify(expectedTree, speculativeOperationTree)
        End Sub

        <Fact>
        Public Sub TestAttributeCallerInfoSemanticModel_Class_Speculative()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices

Class Attr
    Inherits Attribute

    Public Sub New(<CallerMemberName> Optional s As String = Nothing)
    End Sub
End Class

<Attr(""a"")>
Class C
End Class
"
            Dim comp = CreateCompilation(source)
            comp.VerifyDiagnostics()
            Dim tree = comp.SyntaxTrees(0)
            Dim root = tree.GetRoot()
            Dim attrSyntax = root.DescendantNodes().OfType(Of AttributeSyntax)().Last()
            Dim semanticModel = comp.GetSemanticModel(tree)
            Dim newRoot = root.ReplaceNode(attrSyntax, attrSyntax.WithArgumentList(SyntaxFactory.ParseArgumentList("()")))
            Dim newAttrSyntax = newRoot.DescendantNodes().OfType(Of AttributeSyntax)().Last()
            Dim speculativeModel As SemanticModel = Nothing
            Assert.True(semanticModel.TryGetSpeculativeSemanticModel(attrSyntax.ArgumentList.Position, newAttrSyntax, speculativeModel))
            Dim speculativeOperation = speculativeModel.GetOperation(newAttrSyntax)
            Dim expectedTree As String = "
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'Attr()')
  IObjectCreationOperation (Constructor: Sub Attr..ctor([s As System.String = Nothing])) (OperationKind.ObjectCreation, Type: Attr, IsImplicit) (Syntax: 'Attr()')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Attr')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'Attr')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsImplicit) (Syntax: 'Attr')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
"
            Dim speculativeOperationTree = OperationTreeVerifier.GetOperationTree(comp, speculativeOperation)
            OperationTreeVerifier.Verify(expectedTree, speculativeOperationTree)
        End Sub

        <Fact>
        Public Sub TestAttributeCallerInfoSemanticModel_Speculative_AssemblyTarget()
            Dim source = "
Imports System
Imports System.Runtime.CompilerServices

<Assembly: Attr(""a"")>

Class Attr
    Inherits Attribute

    Public Sub New(<CallerMemberName> Optional s As String = ""default_value"")
    End Sub
End Class
"
            Dim comp = CreateCompilation(source)
            comp.VerifyDiagnostics()
            Dim tree = comp.SyntaxTrees(0)
            Dim root = tree.GetRoot()
            Dim attrSyntax = root.DescendantNodes().OfType(Of AttributeSyntax)().First()
            Dim semanticModel = comp.GetSemanticModel(tree)
            Dim newRoot = root.ReplaceNode(attrSyntax, attrSyntax.WithArgumentList(SyntaxFactory.ParseArgumentList("()")))
            Dim newAttrSyntax = newRoot.DescendantNodes().OfType(Of AttributeSyntax)().First()
            Dim speculativeModel As SemanticModel = Nothing
            Assert.True(semanticModel.TryGetSpeculativeSemanticModel(attrSyntax.Position, newAttrSyntax, speculativeModel))
            Dim speculativeOperation = speculativeModel.GetOperation(newAttrSyntax)
            Dim expectedTree As String = "
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'Assembly: Attr()')
  IObjectCreationOperation (Constructor: Sub Attr..ctor([s As System.String = ""default_value""])) (OperationKind.ObjectCreation, Type: Attr, IsImplicit) (Syntax: 'Assembly: Attr()')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Attr')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""default_value"", IsImplicit) (Syntax: 'Attr')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
"
            Dim speculativeOperationTree = OperationTreeVerifier.GetOperationTree(comp, speculativeOperation)
            OperationTreeVerifier.Verify(expectedTree, speculativeOperationTree)
        End Sub

#End Region

#Region "Error Tests"

        <Fact>
        Public Sub AttributeConstructorErrors1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
        <compilation>
            <file name="AttributeConstructorErrors1.vb">
                <![CDATA[ 
            Imports System   

            Module m
                Function NotAConstant() as integer
                    return 9
                End Function
            End Module

            Enum e1
                a
            End Enum

            <AttributeUsage(AttributeTargets.All, allowMultiple:=True)>     
            Class XAttribute
                Inherits Attribute

                Sub New()
                End Sub

                Sub New(ByVal d As Decimal)
                End Sub

                Sub New(ByRef i As Integer)
                End Sub

                Public Sub New(ByVal e As e1)
                End Sub
            End Class   

            <XDoesNotExist>
            <X(1D)>
            <X(1)>
            <X(e1.a)>
            <X(NotAConstant() + 2)>
            Class A
            End Class  
            ]]>
            </file>
        </compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30002: Type 'XDoesNotExist' is not defined.
            <XDoesNotExist>
             ~~~~~~~~~~~~~
BC30045: Attribute constructor has a parameter of type 'Decimal', which is not an integral, floating-point or Enum type or one of Object, Char, String, Boolean, System.Type or 1-dimensional array of these types.
            <X(1D)>
             ~
BC36006: Attribute constructor has a 'ByRef' parameter of type 'Integer'; cannot use constructors with byref parameters to apply the attribute.
            <X(1)>
             ~
BC31516: Type 'e1' cannot be used in an attribute because it is not declared 'Public'.
            <X(e1.a)>
             ~
BC36006: Attribute constructor has a 'ByRef' parameter of type 'Integer'; cannot use constructors with byref parameters to apply the attribute.
            <X(NotAConstant() + 2)>
             ~
BC30059: Constant expression is required.
            <X(NotAConstant() + 2)>
               ~~~~~~~~~~~~~~~~~~
                               ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub AttributeConversionsErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
        <compilation>
            <file name="AttributeConversionsErrors.vb">
                <![CDATA[ 
            Imports System   

            <AttributeUsage(AttributeTargets.All, allowMultiple:=True)>     
            Class XAttribute
                Inherits Attribute

                Sub New()
                End Sub

                Sub New(i As Integer)
                End Sub

            End Class   

            <X("a")>
            Class A
            End Class  
            ]]>
            </file>
        </compilation>)

            Dim expectedErrors = <errors><![CDATA[
BC30934: Conversion from 'String' to 'Integer' cannot occur in a constant expression used as an argument to an attribute.
            <X("a")>
               ~~~
]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub AttributeNamedArgumentErrors1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="AttributeNamedArgumentErrors1.vb">
            <![CDATA[ 
        Imports System     

        <AttributeUsage(AttributeTargets.All, allowMultiple:=True)>     
        Class XAttribute
            Inherits Attribute

            Sub F1(i as integer)
            End Sub

            private PrivateField as integer

            shared Property SharedProperty as integer

            ReadOnly Property ReadOnlyProperty As Integer
                Get
                    Return Nothing
                End Get
            End Property

            Property BadDecimalType as decimal

            Property BadDateType as date

            Property BadArrayType As Attribute()

        End Class   

        <X(NotFound := nothing)>
        <X(F1 := nothing)>
        <X(PrivateField := nothing)>
        <X(SharedProperty := nothing)>
        <X(ReadOnlyProperty := nothing)>
        <X(BadDecimalType := nothing)>
        <X(BadDateType := nothing)>
        <X(BadArrayType:=Nothing)>
        Class A
        End Class  
        ]]>
        </file>
    </compilation>)

            Dim expectedErrors = <errors>
                                     <![CDATA[
BC30661: Field or property 'NotFound' is not found.
        <X(NotFound := nothing)>
           ~~~~~~~~
BC32010: 'F1' cannot be named as a parameter in an attribute specifier because it is not a field or property.
        <X(F1 := nothing)>
           ~~
BC30389: 'XAttribute.PrivateField' is not accessible in this context because it is 'Private'.
        <X(PrivateField := nothing)>
           ~~~~~~~~~~~~
BC31500: 'Shared' attribute property 'SharedProperty' cannot be the target of an assignment.
        <X(SharedProperty := nothing)>
           ~~~~~~~~~~~~~~
BC31501: 'ReadOnly' attribute property 'ReadOnlyProperty' cannot be the target of an assignment.
        <X(ReadOnlyProperty := nothing)>
           ~~~~~~~~~~~~~~~~
BC30659: Property or field 'BadDecimalType' does not have a valid attribute type.
        <X(BadDecimalType := nothing)>
           ~~~~~~~~~~~~~~
BC30659: Property or field 'BadDateType' does not have a valid attribute type.
        <X(BadDateType := nothing)>
           ~~~~~~~~~~~
BC30659: Property or field 'BadArrayType' does not have a valid attribute type.
        <X(BadArrayType:=Nothing)>
           ~~~~~~~~~~~~
                               ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <WorkItem(540939, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540939")>
        <Fact>
        Public Sub AttributeProtectedConstructorError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
            <![CDATA[ 
Imports System
<A()>
Class A
  Inherits Attribute
  Protected Sub New()
  End Sub
End Class

<B("goo")>
Class B
  Inherits Attribute
  Protected Sub New()
  End Sub
End Class

<C("goo")>
Class C
  Inherits Attribute
  Protected Sub New()
  End Sub
  Protected Sub New(b as string)
  End Sub
End Class

<D(1S)>
Class D
  Inherits Attribute
  Protected Sub New()
  End Sub
  protected Sub New(b as Integer)
  End Sub
  protected Sub New(b as Long)
  End Sub
End Class


        ]]>
        </file>
    </compilation>)

            Dim expectedErrors = <errors>
                                     <![CDATA[
BC30517: Overload resolution failed because no 'New' is accessible.
<A()>
 ~~~
BC30517: Overload resolution failed because no 'New' is accessible.
<B("goo")>
 ~~~~~~~~
BC30517: Overload resolution failed because no 'New' is accessible.
<C("goo")>
 ~~~~~~~~
BC30517: Overload resolution failed because no 'New' is accessible.
<D(1S)>
 ~~~~~
                               ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)

            ' TODO: after fixing LookupResults and changing the error handling for inaccessible attribute constructors,
            ' the error messages from above are expected to change to something like:
            ' Error BC30390 'A.Protected Sub New()' is not accessible in this context because it is 'Protected'.
        End Sub

        <WorkItem(540624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540624")>
        <Fact>
        Public Sub AttributeNoMultipleAndInvalidTarget()
            Dim source =
    <compilation>
        <file name="AttributeNoMultipleAndInvalidTarget.vb"><![CDATA[
Imports System
Imports CustomAttribute

<Base(1)>
<Base("SOS")>
Module AttributeMod

    <Derived("Q"c)>
    <Derived("C"c)>
    Public Class Goo

    End Class

End Module
                   ]]></file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source,
                {MetadataReference.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01)})

            ' BC30663, BC30662
            Dim expectedErrors =
<errors>
    <![CDATA[
BC30663: Attribute 'BaseAttribute' cannot be applied multiple times.
<Base("SOS")>
 ~~~~~~~~~~~
BC30662: Attribute 'DerivedAttribute' cannot be applied to 'Goo' because the attribute is not valid on this declaration type.
    <Derived("Q"c)>
     ~~~~~~~
BC30663: Attribute 'DerivedAttribute' cannot be applied multiple times.
    <Derived("C"c)>
     ~~~~~~~~~~~~~
    ]]>
</errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub AttributeNameScoping()
            Dim source =
    <compilation>
        <file name="AttributeNameScoping.vb"><![CDATA[
Imports System

    ' X1 should not be visible without qualification
    <clscompliant(x1)>
    Module m1

    Const X1 as boolean = true

    ' C1 should not be visible without qualification
    <clscompliant(C1)>
    Public Class CGoo
        Public Const C1 as Boolean = true

        <clscompliant(c1)>
        public Sub s()
        end sub
    End Class

    ' C1 should not be visible without qualification
    <clscompliant(C1)>
    Public Structure SGoo
        Public Const C1 as Boolean = true

        <clscompliant(c1)>
        public Sub s()
        end sub
    End Structure

    ' s should not be visible without qualification
    <clscompliant(s.GetType() isnot nothing)>
    Public Interface IGoo
        Sub s()
    End Interface


    ' C1 should not be visible without qualification
    <clscompliant(a = 1)>
    Public Enum EGoo
        A = 1
    End Enum

End Module
                   ]]></file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InaccessibleSymbol2, "x1").WithArguments("m1.X1", "Private"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "C1").WithArguments("C1"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "C1").WithArguments("C1"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "s").WithArguments("s"),
                Diagnostic(ERRID.ERR_RequiredConstExpr, "s.GetType() isnot nothing"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "a").WithArguments("a"),
                Diagnostic(ERRID.ERR_RequiredConstExpr, "a = 1"))
        End Sub

        <WorkItem(541279, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541279")>
        <Fact>
        Public Sub AttributeArrayMissingInitializer()
            Dim source =
    <compilation>
        <file name="AttributeArrayMissingInitializer.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All, AllowMultiple:=true)>
Class A
    Inherits Attribute
    Public Sub New(x As Object())
    End Sub
End Class

<A(New Object(5) {})>
<A(New Object(5) {1})>
Class B
    Shared Sub Main()
    End Sub
End Class

                   ]]></file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source)
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_MissingValuesForArraysInApplAttrs, "{}"),
                Diagnostic(ERRID.ERR_InitializerTooFewElements1, "{1}").WithArguments("5")
                )
        End Sub

        <Fact>
        Public Sub Bug8642()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
            <![CDATA[ 
Imports System

<goo(Type.EmptyTypes)>
Module Program
    Sub Main(args As String())

    End Sub
End Module
        ]]>
        </file>
    </compilation>)

            Dim expectedErrors = <errors>
                                     <![CDATA[
BC30002: Type 'goo' is not defined.
<goo(Type.EmptyTypes)>
 ~~~
BC30059: Constant expression is required.
<goo(Type.EmptyTypes)>
     ~~~~~~~~~~~~~~~
                               ]]></errors>

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ErrorsInMultipleSyntaxTrees()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
                            <compilation>
                                <file name="a.vb">
                                    <![CDATA[ 
Imports System

<Module: A>
<AttributeUsage(AttributeTargets.Class)>
Class A
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.Method)>
Class B
    Inherits Attribute
End Class
        ]]>
                                </file>
                                <file name="b.vb">
                                    <![CDATA[ 
<Module: B>
                                    ]]>
                                </file>
                            </compilation>)

            Dim expectedErrors = <errors>
                                     <![CDATA[
BC30549: Attribute 'A' cannot be applied to a module.
<Module: A>
         ~
BC30549: Attribute 'B' cannot be applied to a module.
<Module: B>
         ~
                               ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub ErrorsInMultiplePartialDeclarations()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
                            <compilation>
                                <file name="a.vb">
                                    <![CDATA[ 
Imports System

<AttributeUsage(AttributeTargets.Class)>
Class A1
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.Method)>
Class A2
    Inherits Attribute
End Class

<A1>
Class B
End Class
        ]]>
                                </file>
                                <file name="b.vb">
                                    <![CDATA[ 
<A1, A2>
Partial Class B
End Class
                                    ]]>
                                </file>
                            </compilation>)

            Dim expectedErrors = <errors>
                                     <![CDATA[
BC30663: Attribute 'A1' cannot be applied multiple times.
<A1, A2>
 ~~
BC30662: Attribute 'A2' cannot be applied to 'B' because the attribute is not valid on this declaration type.
<A1, A2>
     ~~
                               ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub PartialMethods()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[ 
Imports System

Public Class A 
    Inherits Attribute
End Class

Public Class B
    Inherits Attribute
End Class

Partial Class C
    <A>
    Private Partial Sub Goo()
    End Sub

    <B>
    Private Sub Goo()
    End Sub
End Class
]]>
    </file>
</compilation>)

            CompileAndVerify(compilation, sourceSymbolValidator:=
                Sub(moduleSymbol)
                    Dim c = DirectCast(moduleSymbol.GlobalNamespace.GetMembers("C").Single(), NamedTypeSymbol)
                    Dim goo = DirectCast(c.GetMembers("Goo").Single(), MethodSymbol)

                    Dim attrs = goo.GetAttributes()
                    Assert.Equal(2, attrs.Length)
                    Assert.Equal("A", attrs(0).AttributeClass.Name)
                    Assert.Equal("B", attrs(1).AttributeClass.Name)
                End Sub)
        End Sub

        <WorkItem(542020, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542020")>
        <Fact>
        Public Sub ErrorsAttributeNameResolutionWithNamespace()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
                            <compilation>
                                <file name="a.vb">
                                    <![CDATA[ 
Imports System
Class CAttribute
    Inherits Attribute
End Class

Namespace Y.CAttribute
End Namespace

Namespace Y
    Namespace X
        <C>
        Class C
            Inherits Attribute
        End Class
    End Namespace
End Namespace
        ]]>
                                </file>
                            </compilation>)

            Dim expectedErrors = <errors>
                                     <![CDATA[
BC30182: Type expected.
        <C>
         ~
                               ]]></errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <WorkItem(542170, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542170")>
        <Fact>
        Public Sub GenericTypeParameterUsedAsAttribute()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
            <![CDATA[ 
Imports System

Module M
    <T>
    Sub Goo(Of T)
    End Sub

    Class T : Inherits Attribute
    End Class
End Module

        ]]>
        </file>
    </compilation>)

            'BC32067: Type parameters, generic types or types contained in generic types cannot be used as attributes.
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_AttrCannotBeGenerics, "T").WithArguments("T"))
        End Sub

        <WorkItem(542273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542273")>
        <Fact()>
        Public Sub AnonymousTypeFieldAsAttributeNamedArgValue()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
            <![CDATA[ 
Imports System

<AttributeUsage(AttributeTargets.Class, AllowMultiple:=New With {.anonymousField = False}.anonymousField)>
Class ExtensionAttribute
    Inherits Attribute
End Class
        ]]>
        </file>
    </compilation>)

            'BC30059: Constant expression is required.
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_RequiredConstExpr, "New With {.anonymousField = False}.anonymousField"))
        End Sub

        <WorkItem(545073, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545073")>
        <Fact>
        Public Sub AttributeOnDelegateReturnTypeError()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class ReturnTypeAttribute
    Inherits System.Attribute   
End Class

Class C
    Public Delegate Function D() As <ReturnTypeAttribute(0)> Integer
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_TooManyArgs1, "0").WithArguments("Public Sub New()"))
        End Sub

        <Fact>
        Public Sub AttributeOnDelegateParameterError()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class ReturnTypeAttribute
    Inherits System.Attribute   
End Class

Class C
    Public Delegate Function D(<ReturnTypeAttribute(0)>ByRef a As Integer) As Integer
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_TooManyArgs1, "0").WithArguments("Public Sub New()"))
        End Sub

        <WorkItem(545073, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545073")>
        <Fact>
        Public Sub AttributesOnDelegate()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Public Class A
    Inherits System.Attribute   
End Class

Public Class B
    Inherits System.Attribute   
End Class

Public Class C
    Inherits System.Attribute   
End Class

<A>
Public Delegate Function D(<C>a As Integer, <C>ByRef b As Integer) As <B> Integer
]]>
                             </file>
                         </compilation>

            Dim attributeValidator =
                Sub(m As ModuleSymbol)
                    Dim d = m.GlobalNamespace.GetTypeMember("D")

                    Dim invoke = DirectCast(d.GetMember("Invoke"), MethodSymbol)
                    Dim beginInvoke = DirectCast(d.GetMember("BeginInvoke"), MethodSymbol)
                    Dim endInvoke = DirectCast(d.GetMember("EndInvoke"), MethodSymbol)
                    Dim ctor = DirectCast(d.Constructors.Single(), MethodSymbol)

                    Dim p As ParameterSymbol

                    ' no attributes on methods:
                    Assert.Equal(0, invoke.GetAttributes().Length)
                    Assert.Equal(0, beginInvoke.GetAttributes().Length)
                    Assert.Equal(0, endInvoke.GetAttributes().Length)
                    Assert.Equal(0, ctor.GetAttributes().Length)

                    ' attributes on return types:
                    Assert.Equal(0, beginInvoke.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, endInvoke.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, ctor.GetReturnTypeAttributes().Length)

                    Dim attrs = invoke.GetReturnTypeAttributes()
                    Assert.Equal(1, attrs.Length)
                    Assert.Equal(attrs(0).AttributeClass.Name, "B")

                    ' ctor parameters:
                    Assert.Equal(2, ctor.Parameters.Length)
                    Assert.Equal(0, ctor.Parameters(0).GetAttributes().Length)
                    Assert.Equal(0, ctor.Parameters(0).GetAttributes().Length)

                    ' Invoke parameters:
                    Assert.Equal(2, invoke.Parameters.Length)

                    attrs = invoke.Parameters(0).GetAttributes()
                    Assert.Equal(1, attrs.Length)
                    Assert.Equal(attrs(0).AttributeClass.Name, "C")

                    attrs = invoke.Parameters(1).GetAttributes()
                    Assert.Equal(1, attrs.Length)
                    Assert.Equal(attrs(0).AttributeClass.Name, "C")

                    ' BeginInvoke parameters:
                    Assert.Equal(4, beginInvoke.Parameters.Length)

                    p = beginInvoke.Parameters(0)
                    Assert.Equal("a", p.Name)
                    attrs = p.GetAttributes()
                    Assert.Equal(1, attrs.Length)
                    Assert.Equal(attrs(0).AttributeClass.Name, "C")
                    Assert.Equal(1, p.GetAttributes(attrs(0).AttributeClass).Count)
                    Assert.False(p.IsExplicitByRef)
                    Assert.False(p.IsByRef)

                    p = beginInvoke.Parameters(1)
                    Assert.Equal("b", p.Name)
                    attrs = p.GetAttributes()
                    Assert.Equal(1, attrs.Length)
                    Assert.Equal(attrs(0).AttributeClass.Name, "C")
                    Assert.Equal(1, p.GetAttributes(attrs(0).AttributeClass).Count)
                    Assert.True(p.IsExplicitByRef)
                    Assert.True(p.IsByRef)

                    Assert.Equal(0, beginInvoke.Parameters(2).GetAttributes().Length)
                    Assert.Equal(0, beginInvoke.Parameters(3).GetAttributes().Length)

                    ' EndInvoke parameters:
                    Assert.Equal(2, endInvoke.Parameters.Length)

                    p = endInvoke.Parameters(0)
                    Assert.Equal("b", p.Name)
                    attrs = p.GetAttributes()
                    Assert.Equal(1, attrs.Length)
                    Assert.Equal(attrs(0).AttributeClass.Name, "C")
                    Assert.Equal(1, p.GetAttributes(attrs(0).AttributeClass).Count)
                    Assert.True(p.IsExplicitByRef)
                    Assert.True(p.IsByRef)

                    p = endInvoke.Parameters(1)
                    attrs = p.GetAttributes()
                    Assert.Equal(0, attrs.Length)
                End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

#End Region

        ''' <summary>
        ''' Verify that inaccessible friend AAttribute is preferred over accessible A
        ''' </summary>
        <Fact()>
        Public Sub TestAttributeLookupInaccessibleFriend()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
    Class A
        Inherits System.Attribute
    End Class

    <A()>
    Class C
    End Class

    Module Module1

            Sub Main()
            End Sub
    End Module]]>
                             </file>
                         </compilation>

            Dim sourceWithAAttribute As XElement =
<compilation>
    <file name="library.vb">
        <![CDATA[
         Class AAttribute
         End Class

            ]]></file>
</compilation>

            Dim compWithAAttribute = VisualBasicCompilation.Create(
                "library.dll",
                {VisualBasicSyntaxTree.ParseText(sourceWithAAttribute.Value)},
                {MsvbRef, MscorlibRef, SystemCoreRef},
                TestOptions.ReleaseDll)

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {compWithAAttribute.ToMetadataReference()})
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_InaccessibleSymbol2, "A").WithArguments("AAttribute", "Friend"))
        End Sub

        ''' <summary>
        ''' Verify that inaccessible inherited private is preferred
        ''' </summary>
        <Fact()>
        Public Sub TestAttributeLookupInaccessibleInheritedPrivate()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System

Class C
    Private Class NSAttribute
        Inherits Attribute
    End Class

End Class

Class d
    Inherits C

    <NS()>
    Sub d()
    End Sub

End Class

Module Module1
    Sub Main()
    End Sub
End Module]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_InaccessibleSymbol2, "NS").WithArguments("C.NSAttribute", "Private"))
        End Sub

        ''' <summary>
        ''' Verify that ambiguous error is reported when A binds to two Attributes.
        ''' </summary>
        ''' <remarks>If this is run in the IDE make sure global namespace is empty or add
        ''' global namespace prefix to N1 and N2 or run test at command line.</remarks>
        <Fact()>
        Public Sub TestAttributeLookupAmbiguousAttributesWithPrefix()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System
Imports N1
Imports N2

Namespace N1
    Class AAttribute
    End Class
End Namespace

Namespace N2
    Class AAttribute
    End Class
End Namespace

Class A
    Inherits Attribute
End Class

<A()>
Class C
End Class]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_AmbiguousInImports2, "A").WithArguments("AAttribute", "N1, N2"))
        End Sub

        ''' <summary>
        ''' Verify that source attribute takes precedence over metadata attribute with the same name
        ''' </summary>
        <Fact()>
        Public Sub TestAttributeLookupSourceOverridesMetadata()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)>
    Public Class extensionattribute : Inherits Attribute
    End Class
End Namespace

Module m
    <System.Runtime.CompilerServices.extension()>
    Sub Test1(x As Integer)
        System.Console.WriteLine(x)
    End Sub

    Sub Main()
        Dim x = New System.Runtime.CompilerServices.extensionattribute()
    End Sub
End Module]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source)
            comp.VerifyDiagnostics()
        End Sub

        <WorkItem(543855, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543855")>
        <Fact()>
        Public Sub VariantArrayConversionInAttribute()
            Dim vbCompilation = CreateVisualBasicCompilation("VariantArrayConversion",
            <![CDATA[
    Imports System
    <Assembly: AObject(new Type() {GetType(string)})>

    <AttributeUsage(AttributeTargets.All)>
    Public Class AObjectAttribute
        Inherits Attribute

        Sub New(b As Object)
        End Sub

        Sub New(b As Object())
        End Sub
    End Class
    ]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            CompileAndVerify(vbCompilation).VerifyDiagnostics()
        End Sub

        <Fact(), WorkItem(544199, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544199")>
        Public Sub EnumsAllowedToViolateAttributeUsage()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Runtime.InteropServices

&lt;ComVisible(True)&gt; _
&lt;Guid("6f4eb02b-3469-424c-bbcc-2672f653e646")&gt; _
&lt;BestFitMapping(False)&gt; _
&lt;StructLayout(LayoutKind.Auto)&gt; _
&lt;TypeLibType(TypeLibTypeFlags.FRestricted)&gt; _
&lt;Flags()&gt; _
Public Enum EnumHasAllSupportedAttributes
    ID1
    ID2
End Enum
    </file>
</compilation>).VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(544367, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544367")>
        Public Sub AttributeOnPropertyParameter()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Dim classAType As Type = GetType(A)
        Dim itemGetter = classAType.GetMethod("get_Item")
        ShowAttributes(itemGetter.GetParameters()(0))
        Dim itemSetter = classAType.GetMethod("set_Item")
        ShowAttributes(itemSetter.GetParameters()(0))
    End Sub

    Sub ShowAttributes(p As Reflection.ParameterInfo)
        Dim attrs = p.GetCustomAttributes(False)
        Console.WriteLine("param {1} in {0} has {2} attributes", p.Member, p.Name, attrs.Length)
        For Each a In attrs
            Console.WriteLine("  attribute of type {0}", a.GetType())
        Next
    End Sub
End Module

Class A
    Default Public Property Item( &lt;MyAttr(A.C)&gt; index As Integer) As String
        Get
            Return ""
        End Get
        Set(value As String)
        End Set
    End Property

    Public Const C as Integer = 4
End Class

Class MyAttr
    Inherits Attribute
    Public Sub New(x As Integer)
    End Sub
End Class    
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(comp, <![CDATA[
param index in System.String get_Item(Int32) has 1 attributes
  attribute of type MyAttr
param index in Void set_Item(Int32, System.String) has 1 attributes
  attribute of type MyAttr
]]>)
        End Sub

        <Fact, WorkItem(544367, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544367")>
        Public Sub AttributeOnPropertyParameterWithError()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Module Module1
    Sub Main()
    End Sub
End Module

Class A
    Default Public Property Item(<MyAttr> index As Integer) As String
        Get
            Return ""
        End Get
        Set(value As String)
        End Set
    End Property
End Class

Class MyAttr
    ' Does not inherit attribute
End Class    
]]>
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(comp, <expected>
BC31504: 'MyAttr' cannot be used as an attribute because it does not inherit from 'System.Attribute'.
    Default Public Property Item(&lt;MyAttr&gt; index As Integer) As String
                                  ~~~~~~
                                                     </expected>)
        End Sub

        <Fact, WorkItem(543810, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543810")>
        Public Sub AttributeNamedArgumentWithEvent()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System

Class Base
    Event myEvent
End Class

Class Program
    Inherits Base
    <MyAttribute(t:=myEvent)>
    Event MyEvent2

    Sub Main(args As String())
    End Sub
End Class

Class MyAttribute 
   Inherits Attribute
    Public t As Object
    Sub New(t As Integer, Optional x As Integer = 1.0D, Optional y As String = Nothing)
    End Sub
End Class

Module m
Sub Main()
End Sub
End Module
]]></file>
</compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(comp, <expected><![CDATA[
BC30455: Argument not specified for parameter 't' of 'Public Sub New(t As Integer, [x As Integer = 1], [y As String = Nothing])'.
    <MyAttribute(t:=myEvent)>
     ~~~~~~~~~~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
    <MyAttribute(t:=myEvent)>
                    ~~~~~~~
]]></expected>)
        End Sub

        <Fact, WorkItem(543955, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543955")>
        Public Sub StringParametersInDeclareMethods_1()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection

Module Module1

    Declare Ansi Function GetWindowsDirectory1 Lib "kernel32" Alias "GetWindowsDirectoryW" (buffer As String, ByVal buffer As Integer) As Integer
    Declare Unicode Function GetWindowsDirectory2 Lib "kernel32" Alias "GetWindowsDirectoryW" (buffer As String, ByVal buffer As Integer) As Integer
    Declare Auto Function GetWindowsDirectory3 Lib "kernel32" Alias "GetWindowsDirectoryW" (buffer As String, ByVal buffer As Integer) As Integer
    Declare Ansi Function GetWindowsDirectory4 Lib "kernel32" Alias "GetWindowsDirectoryW" (ByRef buffer As String, ByVal buffer As Integer) As Integer
    Declare Unicode Function GetWindowsDirectory5 Lib "kernel32" Alias "GetWindowsDirectoryW" (ByRef buffer As String, ByVal buffer As Integer) As Integer
    Declare Auto Function GetWindowsDirectory6 Lib "kernel32" Alias "GetWindowsDirectoryW" (ByRef buffer As String, ByVal buffer As Integer) As Integer

    Delegate Function D(ByRef buffer As String, ByVal buffer As Integer) As Integer

    Sub Main()
        Dim t = GetType(Module1)

        Print(t.GetMethod("GetWindowsDirectory1"))
        System.Console.WriteLine()
        Print(t.GetMethod("GetWindowsDirectory2"))
        System.Console.WriteLine()
        Print(t.GetMethod("GetWindowsDirectory3"))
        System.Console.WriteLine()
        Print(t.GetMethod("GetWindowsDirectory4"))
        System.Console.WriteLine()
        Print(t.GetMethod("GetWindowsDirectory5"))
        System.Console.WriteLine()
        Print(t.GetMethod("GetWindowsDirectory6"))
        System.Console.WriteLine()
    End Sub

    Private Sub Print(m As MethodInfo)
        System.Console.WriteLine(m.Name)
        For Each p In m.GetParameters()
            System.Console.WriteLine("{0} As {1}", p.Name, p.ParameterType)
            For Each marshal In p.GetCustomAttributes(GetType(Runtime.InteropServices.MarshalAsAttribute), False)
                System.Console.WriteLine(DirectCast(marshal, Runtime.InteropServices.MarshalAsAttribute).Value.ToString())
            Next
        Next
    End Sub

    Sub Test1(ByRef x As String)
        GetWindowsDirectory1(x, 0)
    End Sub
    Sub Test2(ByRef x As String)
        GetWindowsDirectory2(x, 0)
    End Sub
    Sub Test3(ByRef x As String)
        GetWindowsDirectory3(x, 0)
    End Sub
    Sub Test4(ByRef x As String)
        GetWindowsDirectory4(x, 0)
    End Sub
    Sub Test5(ByRef x As String)
        GetWindowsDirectory5(x, 0)
    End Sub
    Sub Test6(ByRef x As String)
        GetWindowsDirectory6(x, 0)
    End Sub

    Function Test11() As D
        Return AddressOf GetWindowsDirectory1
    End Function
    Function Test12() As D
        Return AddressOf GetWindowsDirectory2
    End Function
    Function Test13() As D
        Return AddressOf GetWindowsDirectory3
    End Function
    Function Test14() As D
        Return AddressOf GetWindowsDirectory4
    End Function
    Function Test15() As D
        Return AddressOf GetWindowsDirectory5
    End Function
    Function Test16() As D
        Return AddressOf GetWindowsDirectory6
    End Function
End Module
]]></file>
</compilation>, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
GetWindowsDirectory1
buffer As System.String&
VBByRefStr
buffer As System.Int32

GetWindowsDirectory2
buffer As System.String&
VBByRefStr
buffer As System.Int32

GetWindowsDirectory3
buffer As System.String&
VBByRefStr
buffer As System.Int32

GetWindowsDirectory4
buffer As System.String&
AnsiBStr
buffer As System.Int32

GetWindowsDirectory5
buffer As System.String&
BStr
buffer As System.Int32

GetWindowsDirectory6
buffer As System.String&
TBStr
buffer As System.Int32
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  call       "Declare Ansi Function Module1.GetWindowsDirectory1 Lib "kernel32" Alias "GetWindowsDirectoryW" (String, Integer) As Integer"
  IL_0007:  pop
  IL_0008:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  call       "Declare Unicode Function Module1.GetWindowsDirectory2 Lib "kernel32" Alias "GetWindowsDirectoryW" (String, Integer) As Integer"
  IL_0007:  pop
  IL_0008:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  call       "Declare Auto Function Module1.GetWindowsDirectory3 Lib "kernel32" Alias "GetWindowsDirectoryW" (String, Integer) As Integer"
  IL_0007:  pop
  IL_0008:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  call       "Declare Ansi Function Module1.GetWindowsDirectory4 Lib "kernel32" Alias "GetWindowsDirectoryW" (ByRef String, Integer) As Integer"
  IL_0007:  pop
  IL_0008:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test5",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  call       "Declare Unicode Function Module1.GetWindowsDirectory5 Lib "kernel32" Alias "GetWindowsDirectoryW" (ByRef String, Integer) As Integer"
  IL_0007:  pop
  IL_0008:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test6",
            <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  call       "Declare Auto Function Module1.GetWindowsDirectory6 Lib "kernel32" Alias "GetWindowsDirectoryW" (ByRef String, Integer) As Integer"
  IL_0007:  pop
  IL_0008:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(543955, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543955")>
        Public Sub StringParametersInDeclareMethods_3()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Reflection

Module Module1

    Declare Ansi Function GetWindowsDirectory1 Lib "kernel32" Alias "GetWindowsDirectoryW" (ByVal buffer As String, ByVal buffer As Integer) As Integer
    Declare Ansi Function GetWindowsDirectory4 Lib "kernel32" Alias "GetWindowsDirectoryW" (ByRef buffer As String, ByVal buffer As Integer) As Integer

    Delegate Function D3(buffer As String, ByVal buffer As Integer) As Integer
    Delegate Function D4(buffer As Integer, ByVal buffer As Integer) As Integer

    Function Test19() As D3
        Return AddressOf GetWindowsDirectory1 ' 1
    End Function
    Function Test20() As D3
        Return AddressOf GetWindowsDirectory4 ' 2
    End Function
    Function Test21() As D4
        Return AddressOf GetWindowsDirectory1 ' 3
    End Function
    Function Test22() As D4
        Return AddressOf GetWindowsDirectory4 ' 4
    End Function

    Sub Main()
    End Sub
End Module
]]></file>
</compilation>, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(comp,
<expected>
BC31143: Method 'Public Declare Ansi Function GetWindowsDirectory1 Lib "kernel32" Alias "GetWindowsDirectoryW" (buffer As String, buffer As Integer) As Integer' does not have a signature compatible with delegate 'Delegate Function Module1.D3(buffer As String, buffer As Integer) As Integer'.
        Return AddressOf GetWindowsDirectory1 ' 1
                         ~~~~~~~~~~~~~~~~~~~~
BC31143: Method 'Public Declare Ansi Function GetWindowsDirectory4 Lib "kernel32" Alias "GetWindowsDirectoryW" (ByRef buffer As String, buffer As Integer) As Integer' does not have a signature compatible with delegate 'Delegate Function Module1.D3(buffer As String, buffer As Integer) As Integer'.
        Return AddressOf GetWindowsDirectory4 ' 2
                         ~~~~~~~~~~~~~~~~~~~~
BC31143: Method 'Public Declare Ansi Function GetWindowsDirectory1 Lib "kernel32" Alias "GetWindowsDirectoryW" (buffer As String, buffer As Integer) As Integer' does not have a signature compatible with delegate 'Delegate Function Module1.D4(buffer As Integer, buffer As Integer) As Integer'.
        Return AddressOf GetWindowsDirectory1 ' 3
                         ~~~~~~~~~~~~~~~~~~~~
BC31143: Method 'Public Declare Ansi Function GetWindowsDirectory4 Lib "kernel32" Alias "GetWindowsDirectoryW" (ByRef buffer As String, buffer As Integer) As Integer' does not have a signature compatible with delegate 'Delegate Function Module1.D4(buffer As Integer, buffer As Integer) As Integer'.
        Return AddressOf GetWindowsDirectory4 ' 4
                         ~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <WorkItem(529620, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529620")>
        <Fact()>
        Public Sub TestFriendEnumInAttribute()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
        ' Friend Enum in an array in an attribute should be an error.
        Imports System

        Friend Enum e2
            r
            g
            b
        End Enum

        Namespace Test2
            <MyAttr1(New e2() {e2.g})>
            Class C
            End Class

            Class MyAttr1
                Inherits Attribute
                Sub New(ByVal B As e2())
                End Sub
            End Class
        End Namespace
]]>
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_BadAttributeNonPublicType1, "MyAttr1").WithArguments("e2()"))

        End Sub

        <WorkItem(545558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545558")>
        <Fact()>
        Public Sub TestUndefinedEnumInAttribute()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System.ComponentModel

Module Program
    <EditorBrowsable(EditorBrowsableState.n)>
    Sub Main(args As String())
    End Sub
End Module
]]>
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_NameNotMember2, "EditorBrowsableState.n").WithArguments("n", "System.ComponentModel.EditorBrowsableState"))

        End Sub

        <WorkItem(545697, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545697")>
        <Fact>
        Public Sub TestUnboundLambdaInNamedAttributeArgument()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System
 
Module Program
    <A(F:=Sub()
              Dim x = Mid("1", 1)
          End Sub)>
    Sub Main(args As String())
        Dim a As Action = Sub()
                              Dim x = Mid("1", 1)
                          End Sub
    End Sub
 
End Module
]]>
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, references:={SystemCoreRef})
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_UndefinedType1, "A").WithArguments("A"),
                                   Diagnostic(ERRID.ERR_NameNotDeclared1, "Mid").WithArguments("Mid"),
                                   Diagnostic(ERRID.ERR_PropertyOrFieldNotDefined1, "F").WithArguments("F"),
                                   Diagnostic(ERRID.ERR_NameNotDeclared1, "Mid").WithArguments("Mid"))

        End Sub

        <Fact>
        Public Sub SpecialNameAttributeFromSource()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System.Runtime.CompilerServices

<SpecialName()>
Public Structure S
    <SpecialName()>
    Friend Event E As Action(Of String)
    <SpecialName()>
    Default Property P(b As Byte) As Byte
        Get
            Return b
        End Get
        Set(value As Byte)

        End Set
    End Property
End Structure
]]>
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim globalNS = comp.SourceAssembly.GlobalNamespace
            Dim typesym = DirectCast(globalNS.GetMember("S"), NamedTypeSymbol)
            Assert.NotNull(typesym)
            Assert.True(typesym.HasSpecialName)

            Dim e = DirectCast(typesym.GetMember("E"), EventSymbol)
            Assert.NotNull(e)
            Assert.True(e.HasSpecialName)

            Dim p = DirectCast(typesym.GetMember("P"), PropertySymbol)
            Assert.NotNull(p)
            Assert.True(p.HasSpecialName)

            Assert.True(e.HasSpecialName)
            Assert.Equal("Private EEvent As Action", e.AssociatedField.ToString)
            Assert.True(e.HasAssociatedField)
            Assert.Equal(ImmutableArray.Create(Of VisualBasicAttributeData)(), e.GetFieldAttributes)
            Assert.Null(e.OverriddenEvent)

            Assert.NotNull(p)
            Assert.True(p.HasSpecialName)
            Assert.Equal(ImmutableArray.Create(Of VisualBasicAttributeData)(), p.GetFieldAttributes)
        End Sub

        ''' <summary>
        ''' Verify that attributeusage from base class is used by derived class
        ''' </summary>
        <Fact()>
        Public Sub TestAttributeUsageInheritedBaseAttribute()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
    Imports System

    Module Module1
        <DerivedAllowsMultiple()>
        <DerivedAllowsMultiple()> ' Should allow multiple
        Sub Main()
        End Sub
    End Module]]>
                             </file>
                         </compilation>

            Dim sourceWithAttribute As XElement =
<compilation>
    <file name="library.vb">
        <![CDATA[
        Imports System

        Public Class DerivedAllowsMultiple
            Inherits Base
        End Class

        <AttributeUsage(AttributeTargets.All, AllowMultiple:=True, Inherited:=true)>
        Public Class Base
            Inherits Attribute
        End Class
            ]]></file>
</compilation>

            Dim compWithAttribute = VisualBasicCompilation.Create(
                "library.dll",
                {VisualBasicSyntaxTree.ParseText(sourceWithAttribute.Value)},
                {MsvbRef, MscorlibRef, SystemCoreRef},
                TestOptions.ReleaseDll)

            Dim sourceLibRef = compWithAttribute.ToMetadataReference()

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {sourceLibRef})
            comp.AssertNoDiagnostics()

            Dim metadataLibRef As MetadataReference = compWithAttribute.ToMetadataReference()

            comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {metadataLibRef})
            comp.AssertNoDiagnostics()

            Dim attributesOnMain = comp.GlobalNamespace.GetModuleMembers("Module1").Single().GetMembers("Main").Single().GetAttributes()
            Assert.Equal(2, attributesOnMain.Length())

            Assert.NotEqual(attributesOnMain(0).ApplicationSyntaxReference, attributesOnMain(1).ApplicationSyntaxReference)
            Assert.NotNull(attributesOnMain(0).ApplicationSyntaxReference)
        End Sub

        <Fact(), WorkItem(546490, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546490")>
        Public Sub Bug15984()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern FSharp.Core {}
.assembly '<<GeneratedFileName>>'
{
}

.class public abstract auto ansi sealed Library1.Goo
       extends [mscorlib]System.Object
{
  .custom instance void [FSharp.Core]Microsoft.FSharp.Core.CompilationMappingAttribute::.ctor(valuetype [FSharp.Core]Microsoft.FSharp.Core.SourceConstructFlags) = ( 01 00 07 00 00 00 00 00 ) 
  .method public static int32  inc(int32 x) cil managed
  {
    // Code size       5 (0x5)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  add
    IL_0004:  ret
  } // end of method Goo::inc

} // end of class Library1.Goo
]]>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation>
    <file name="a.vb">
    </file>
</compilation>, customIL.Value, appendDefaultHeader:=False)

            Dim type = compilation.GetTypeByMetadataName("Library1.Goo")
            Assert.Equal(0, type.GetAttributes()(0).ConstructorArguments.Count)

        End Sub

        <Fact>
        <WorkItem(569089, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569089")>
        Public Sub NullArrays()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Public Class A
    Inherits Attribute
   
    Public Sub New(a As Object(), b As Integer())
    End Sub

    Public Property P As Object()

    Public F As Integer()
End Class

<A(Nothing, Nothing, P:=Nothing, F:=Nothing)>
Class C

End Class
    ]]></file>
</compilation>

            CompileAndVerify(source, symbolValidator:=
                Sub(m)
                    Dim c = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                    Dim attr = c.GetAttributes().Single()

                    Dim args = attr.ConstructorArguments.ToArray()

                    Assert.True(args(0).IsNull)
                    Assert.Equal("Object()", args(0).Type.ToDisplayString())
                    Assert.Throws(Of InvalidOperationException)(Function() args(0).Value)

                    Assert.True(args(1).IsNull)
                    Assert.Equal("Integer()", args(1).Type.ToDisplayString())
                    Assert.Throws(Of InvalidOperationException)(Function() args(1).Value)

                    Dim named = attr.NamedArguments.ToDictionary(Function(e) e.Key, Function(e) e.Value)

                    Assert.True(named("P").IsNull)
                    Assert.Equal("Object()", named("P").Type.ToDisplayString())
                    Assert.Throws(Of InvalidOperationException)(Function() named("P").Value)

                    Assert.True(named("F").IsNull)
                    Assert.Equal("Integer()", named("F").Type.ToDisplayString())
                    Assert.Throws(Of InvalidOperationException)(Function() named("F").Value)
                End Sub)
        End Sub

        <Fact>
        <WorkItem(688268, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/688268")>
        Public Sub Bug688268()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Imports System.Security

Public Interface I
    Sub _VtblGap1_30()
    Sub _VtblGaX1_30()
End Interface
    ]]></file>
</compilation>

            Dim metadataValidator As System.Action(Of ModuleSymbol) =
                Sub([module] As ModuleSymbol)
                    Dim metadata = DirectCast([module], PEModuleSymbol).Module

                    Dim typeI = DirectCast([module].GlobalNamespace.GetTypeMembers("I").Single(), PENamedTypeSymbol)

                    Dim methods = metadata.GetMethodsOfTypeOrThrow(typeI.Handle)
                    Assert.Equal(2, methods.Count)

                    Dim e = methods.GetEnumerator()
                    e.MoveNext()
                    Dim flags = metadata.GetMethodDefFlagsOrThrow(e.Current)
                    Assert.Equal(
                        MethodAttributes.PrivateScope Or
                        MethodAttributes.Public Or
                        MethodAttributes.Virtual Or
                        MethodAttributes.VtableLayoutMask Or
                        MethodAttributes.CheckAccessOnOverride Or
                        MethodAttributes.Abstract Or
                        MethodAttributes.SpecialName Or
                        MethodAttributes.RTSpecialName,
                        flags)

                    e.MoveNext()
                    flags = metadata.GetMethodDefFlagsOrThrow(e.Current)
                    Assert.Equal(
                        MethodAttributes.PrivateScope Or
                        MethodAttributes.Public Or
                        MethodAttributes.Virtual Or
                        MethodAttributes.VtableLayoutMask Or
                        MethodAttributes.CheckAccessOnOverride Or
                        MethodAttributes.Abstract,
                        flags)
                End Sub

            CompileAndVerify(source, symbolValidator:=metadataValidator)
        End Sub

        <Fact>
        Public Sub NullTypeAndString()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Public Class A
    Inherits Attribute 

    Public Sub New(t As Type, s As String)
    End Sub
End Class

<A(Nothing, Nothing)>
Class C
End Class
    ]]></file>
</compilation>

            CompileAndVerify(source, symbolValidator:=
                Sub(m)
                    Dim c = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                    Dim attr = c.GetAttributes().Single()
                    Dim args = attr.ConstructorArguments.ToArray()

                    Assert.Null(args(0).Value)
                    Assert.Equal("Type", args(0).Type.Name)
                    Assert.Throws(Of InvalidOperationException)(Function() args(0).Values)

                    Assert.Null(args(1).Value)
                    Assert.Equal("String", args(1).Type.Name)
                    Assert.Throws(Of InvalidOperationException)(Function() args(1).Values)
                End Sub)
        End Sub

        <Fact>
        <WorkItem(728865, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/728865")>
        Public Sub Repro728865()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Reflection
Imports Microsoft.Yeti

Namespace PFxIntegration
    Public Class ProducerConsumerScenario
        Shared Sub Main()
            Dim program = GetType(ProducerConsumerScenario)
            Dim methodInfo = program.GetMethod("ProducerConsumer")
            Dim myAttributes = methodInfo.GetCustomAttributes(False)
            If myAttributes.Length > 0 Then
                Console.WriteLine()
                Console.WriteLine("The attributes for the method - {0} - are: ", methodInfo)
                Console.WriteLine()

                For j = 0 To myAttributes.Length - 1
                    Console.WriteLine("The type of the attribute is {0}", myAttributes(j))
                Next
            End If
        End Sub

        Public Enum CollectionType
            [Default]
            Queue
            Stack
            Bag
        End Enum

        Public Sub New()
        End Sub

        <CartesianRowData({5, 100, 100000}, {CollectionType.Default, CollectionType.Queue, CollectionType.Stack, CollectionType.Bag})>
        Public Sub ProducerConsumer()
            Console.WriteLine("Hello")
        End Sub
    End Class
End Namespace

Namespace Microsoft.Yeti
    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=True)>
    Public Class CartesianRowDataAttribute
        Inherits Attribute

        Public Sub New()
        End Sub

        Public Sub New(ParamArray data As Object())
            Dim asEnum As IEnumerable(Of Object)() = New IEnumerable(Of Object)(data.Length) {}

            For i = 0 To data.Length - 1
                WrapEnum(DirectCast(data(i), IEnumerable))
            Next
        End Sub

        Shared Sub WrapEnum(x As IEnumerable)
            For Each a In x
                Console.WriteLine(" - {0} -", a)
            Next
        End Sub
    End Class
End Namespace
    ]]></file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
 - 5 -
 - 100 -
 - 100000 -
 - Default -
 - Queue -
 - Stack -
 - Bag -

The attributes for the method - Void ProducerConsumer() - are: 

The type of the attribute is Microsoft.Yeti.CartesianRowDataAttribute]]>)
        End Sub

        <Fact>
        <WorkItem(728865, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/728865")>
        Public Sub ParamArrayAttributeConstructor()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Public Class MyAttribute
    Inherits Attribute

    Public Sub New(ParamArray array As Object())

    End Sub
End Class

Public Class Test
    <My({1, 2, 3})>
    Sub M1()
    End Sub

    <My(1, 2, 3)>
    Sub M2()
    End Sub

    <My({"A", "B", "C"})>
    Sub M3()
    End Sub

    <My("A", "B", "C")>
    Sub M4()
    End Sub

    <My({{1, 2, 3}, {"A", "B", "C"}})>
    Sub M5()
    End Sub

    <My({1, 2, 3}, {"A", "B", "C"})>
    Sub M6()
    End Sub
End Class

    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Dim methods = Enumerable.Range(1, 6).Select(Function(i) type.GetMember(Of MethodSymbol)("M" & i)).ToArray()

            methods(0).GetAttributes().Single().VerifyValue(0, TypedConstantKind.Array, New Integer() {1, 2, 3})
            methods(1).GetAttributes().Single().VerifyValue(0, TypedConstantKind.Array, New Object() {1, 2, 3})
            methods(2).GetAttributes().Single().VerifyValue(0, TypedConstantKind.Array, New String() {"A", "B", "C"})
            methods(3).GetAttributes().Single().VerifyValue(0, TypedConstantKind.Array, New Object() {"A", "B", "C"})
            methods(4).GetAttributes().Single().VerifyValue(0, TypedConstantKind.Array, New Object() {}) ' Value was invalid.
            methods(5).GetAttributes().Single().VerifyValue(0, TypedConstantKind.Array, New Object() {DirectCast({1, 2, 3}, Object), DirectCast({"A", "B", "C"}, Object)})

            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30934: Conversion from 'Object(*,*)' to 'Object' cannot occur in a constant expression used as an argument to an attribute.
    <My({{1, 2, 3}, {"A", "B", "C"}})>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        <WorkItem(737021, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/737021")>
        Public Sub NothingVersusEmptyArray()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Public Class ArrayAttribute
    Inherits Attribute

    Public field As Integer()

    Public Sub New(array As Integer())
    End Sub
End Class

Public Class Test
    <Array(Nothing)>
    Sub M0()
    End Sub

    <Array({})>
    Sub M1()
    End Sub

    <Array(Nothing, field:=Nothing)>
    Sub M2()
    End Sub

    <Array({}, field:=Nothing)>
    Sub M3()
    End Sub

    <Array(Nothing, field:={})>
    Sub M4()
    End Sub

    <Array({}, field:={})>
    Sub M5()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Dim methods = Enumerable.Range(0, 6).Select(Function(i) type.GetMember(Of MethodSymbol)("M" & i))
            Dim attrs = methods.Select(Function(m) m.GetAttributes().Single()).ToArray()

            Const fieldName = "field"

            Dim nullArray As Integer() = Nothing
            Dim emptyArray As Integer() = {}

            Assert.NotEqual(nullArray, emptyArray)

            attrs(0).VerifyValue(0, TypedConstantKind.Array, nullArray)

            attrs(1).VerifyValue(0, TypedConstantKind.Array, emptyArray)

            attrs(2).VerifyValue(0, TypedConstantKind.Array, nullArray)
            attrs(2).VerifyNamedArgumentValue(0, fieldName, TypedConstantKind.Array, nullArray)

            attrs(3).VerifyValue(0, TypedConstantKind.Array, emptyArray)
            attrs(3).VerifyNamedArgumentValue(0, fieldName, TypedConstantKind.Array, nullArray)

            attrs(4).VerifyValue(0, TypedConstantKind.Array, nullArray)
            attrs(4).VerifyNamedArgumentValue(0, fieldName, TypedConstantKind.Array, emptyArray)

            attrs(5).VerifyValue(0, TypedConstantKind.Array, emptyArray)
            attrs(5).VerifyNamedArgumentValue(0, fieldName, TypedConstantKind.Array, emptyArray)
        End Sub

        <Fact>
        <WorkItem(530266, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530266")>
        Public Sub UnboundGenericTypeInTypedConstant()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Public Class TestAttribute
    Inherits Attribute

    Sub New(x as Type)
    End Sub
End Class

<TestAttribute(GetType(Target(Of )))>
Class Target(Of T)
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll)
            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Target")

            Dim typeInAttribute = DirectCast(type.GetAttributes()(0).ConstructorArguments(0).Value, NamedTypeSymbol)
            Assert.True(typeInAttribute.IsUnboundGenericType)
            Assert.True(DirectCast(typeInAttribute, INamedTypeSymbol).IsUnboundGenericType)
            Assert.Equal("Target(Of )", typeInAttribute.ToTestDisplayString())

            Dim comp2 = CreateCompilationWithMscorlib40AndReferences(<compilation><file></file></compilation>, {comp.EmitToImageReference()})
            type = comp2.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Target")

            Assert.IsAssignableFrom(Of PENamedTypeSymbol)(type)

            typeInAttribute = DirectCast(type.GetAttributes()(0).ConstructorArguments(0).Value, NamedTypeSymbol)
            Assert.True(typeInAttribute.IsUnboundGenericType)
            Assert.True(DirectCast(typeInAttribute, INamedTypeSymbol).IsUnboundGenericType)
            Assert.Equal("Target(Of )", typeInAttribute.ToTestDisplayString())
        End Sub

        <Fact, WorkItem(879792, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/879792")>
        Public Sub Bug879792()
            Dim source2 =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System
 
<Z>
Module Program
    Sub Main()
    End Sub
End Module
 
Interface ZatTribute(Of T)
End Interface
 
Class Z
    Inherits Attribute
End Class
]]>
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source2)
            CompilationUtils.AssertNoDiagnostics(comp)

            Dim program = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Program")
            Assert.Equal("Z", program.GetAttributes()(0).AttributeClass.ToTestDisplayString())
        End Sub

        <Fact, WorkItem(1020038, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020038")>
        Public Sub Bug1020038()
            Dim source1 =
<compilation name="Bug1020038">
    <file name="a.vb"><![CDATA[
Public Class CTest
End Class
]]>
    </file>
</compilation>

            Dim validator = Sub(m As ModuleSymbol)
                                Assert.Equal(2, m.ReferencedAssemblies.Length)
                                Assert.Equal("Bug1020038", m.ReferencedAssemblies(1).Name)
                            End Sub

            Dim compilation1 = CreateCompilationWithMscorlib40(source1)

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class CAttr 
    Inherits System.Attribute

    Sub New(x as System.Type)
    End Sub
End Class

<CAttr(GetType(CTest))>
Class Test
End Class
]]>
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(source2, {New VisualBasicCompilationReference(compilation1)})
            CompileAndVerify(compilation2, symbolValidator:=validator)

            Dim source3 =
<compilation>
    <file name="a.vb"><![CDATA[
Class CAttr 
    Inherits System.Attribute

    Sub New(x as System.Type)
    End Sub
End Class

<CAttr(GetType(System.Func(Of System.Action(Of CTest))))>
Class Test
End Class
]]>
    </file>
</compilation>

            Dim compilation3 = CreateCompilationWithMscorlib40AndReferences(source3, {New VisualBasicCompilationReference(compilation1)})
            CompileAndVerify(compilation3, symbolValidator:=validator)
        End Sub

        <Fact, WorkItem(1144603, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1144603")>
        Public Sub EmitMetadataOnlyInPresenceOfErrors()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class DiagnosticAnalyzerAttribute
    Inherits System.Attribute
    Public Sub New(firstLanguage As String, ParamArray additionalLanguages As String())
    End Sub
End Class

Public Class LanguageNames
    Public Const CSharp As xyz = "C#"
End Class
]]>
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithMscorlib40(source1, options:=TestOptions.DebugDll)

            AssertTheseDiagnostics(compilation1, <![CDATA[
BC30002: Type 'xyz' is not defined.
    Public Const CSharp As xyz = "C#"
                           ~~~
]]>)

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
<DiagnosticAnalyzer(LanguageNames.CSharp)>
Class CSharpCompilerDiagnosticAnalyzer
End Class
]]>
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(source2, {New VisualBasicCompilationReference(compilation1)}, options:=TestOptions.DebugDll.WithModuleName("Test.dll"))
            Assert.Same(compilation1.Assembly, compilation2.SourceModule.ReferencedAssemblySymbols(1))
            AssertTheseDiagnostics(compilation2)

            Dim emitResult2 = compilation2.Emit(peStream:=New MemoryStream(), options:=New EmitOptions(metadataOnly:=True))
            Assert.False(emitResult2.Success)
            AssertTheseDiagnostics(emitResult2.Diagnostics, <![CDATA[
BC36970: Failed to emit module 'Test.dll': Module has invalid attributes.
]]>)

            ' Use different mscorlib to test retargeting scenario
            Dim compilation3 = CreateCompilationWithMscorlib45AndVBRuntime(source2, {New VisualBasicCompilationReference(compilation1)}, options:=TestOptions.DebugDll)
            Assert.NotSame(compilation1.Assembly, compilation3.SourceModule.ReferencedAssemblySymbols(1))
            AssertTheseDiagnostics(compilation3, <![CDATA[
BC30002: Type 'xyz' is not defined.
<DiagnosticAnalyzer(LanguageNames.CSharp)>
                    ~~~~~~~~~~~~~~~~~~~~
]]>)

            Dim emitResult3 = compilation3.Emit(peStream:=New MemoryStream(), options:=New EmitOptions(metadataOnly:=True))
            Assert.False(emitResult3.Success)
            AssertTheseDiagnostics(emitResult3.Diagnostics, <![CDATA[
BC30002: Type 'xyz' is not defined.
<DiagnosticAnalyzer(LanguageNames.CSharp)>
                    ~~~~~~~~~~~~~~~~~~~~
]]>)
        End Sub

        <Fact>
        Public Sub ReferencingEmbeddedAttributesFromADifferentAssemblyFails_Internal()

            Dim reference =
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute("Source")>
Namespace Microsoft.CodeAnalysis
    Friend Class EmbeddedAttribute
        Inherits System.Attribute
    End Class
End Namespace
Namespace TestReference
    <Microsoft.CodeAnalysis.Embedded>
    Friend Class TestType1
    End Class
    <Microsoft.CodeAnalysis.EmbeddedAttribute>
    Friend Class TestType2
    End Class
    Friend Class TestType3
    End Class
End Namespace
]]>
    </file>
</compilation>

            Dim referenceCompilation = CreateCompilationWithMscorlib40(reference).ToMetadataReference()

            Dim code = "
Public Class Program
    Public Shared Sub Main()
        Dim obj1 = New TestReference.TestType1()
        Dim obj2 = New TestReference.TestType2()
        Dim obj3 = New TestReference.TestType3() ' This should be fine
    End Sub
End Class"

            Dim compilation = CreateCompilationWithMscorlib40(code, references:={referenceCompilation}, assemblyName:="Source")

            AssertTheseDiagnostics(compilation, <![CDATA[
BC30002: Type 'TestReference.TestType1' is not defined.
        Dim obj1 = New TestReference.TestType1()
                       ~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'TestReference.TestType2' is not defined.
        Dim obj2 = New TestReference.TestType2()
                       ~~~~~~~~~~~~~~~~~~~~~~~
]]>)
        End Sub

        <Fact>
        Public Sub ReferencingEmbeddedAttributesFromADifferentAssemblyFails_Public()

            Dim reference =
<compilation>
    <file name="a.vb"><![CDATA[
Namespace Microsoft.CodeAnalysis
    Friend Class EmbeddedAttribute
        Inherits System.Attribute
    End Class
End Namespace
Namespace TestReference
    <Microsoft.CodeAnalysis.Embedded>
    Public Class TestType1
    End Class
    <Microsoft.CodeAnalysis.EmbeddedAttribute>
    Public Class TestType2
    End Class
    Public Class TestType3
    End Class
End Namespace
]]>
    </file>
</compilation>

            Dim referenceCompilation = CreateCompilationWithMscorlib40(reference).ToMetadataReference()

            Dim code = "
Public Class Program
    Public Shared Sub Main()
        Dim obj1 = New TestReference.TestType1()
        Dim obj2 = New TestReference.TestType2()
        Dim obj3 = New TestReference.TestType3() ' This should be fine
    End Sub
End Class"

            Dim compilation = CreateCompilationWithMscorlib40(code, references:={referenceCompilation})

            AssertTheseDiagnostics(compilation, <![CDATA[
BC30002: Type 'TestReference.TestType1' is not defined.
        Dim obj1 = New TestReference.TestType1()
                       ~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'TestReference.TestType2' is not defined.
        Dim obj2 = New TestReference.TestType2()
                       ~~~~~~~~~~~~~~~~~~~~~~~
]]>)
        End Sub

        <Fact>
        Public Sub ReferencingEmbeddedAttributesFromADifferentAssemblyFails_Module()

            Dim moduleCode = CreateCompilationWithMscorlib40(options:=TestOptions.ReleaseModule, source:="
Namespace Microsoft.CodeAnalysis
    Friend Class EmbeddedAttribute
        Inherits System.Attribute
    End Class
End Namespace
Namespace TestReference
    <Microsoft.CodeAnalysis.Embedded>
    Public Class TestType1
    End Class
    <Microsoft.CodeAnalysis.EmbeddedAttribute>
    Public Class TestType2
    End Class
    Public Class TestType3
    End Class
End Namespace")

            Dim reference = ModuleMetadata.CreateFromImage(moduleCode.EmitToArray()).GetReference()

            Dim code = "
Public Class Program
    Public Shared Sub Main()
        Dim obj1 = New TestReference.TestType1()
        Dim obj2 = New TestReference.TestType2()
        Dim obj3 = New TestReference.TestType3() ' This should be fine
    End Sub
End Class"

            Dim compilation = CreateCompilationWithMscorlib40(code, references:={reference})

            AssertTheseDiagnostics(compilation, <![CDATA[
BC30002: Type 'TestReference.TestType1' is not defined.
        Dim obj1 = New TestReference.TestType1()
                       ~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'TestReference.TestType2' is not defined.
        Dim obj2 = New TestReference.TestType2()
                       ~~~~~~~~~~~~~~~~~~~~~~~
]]>)
        End Sub

        <Fact>
        Public Sub ReferencingEmbeddedAttributesFromTheSameAssemblySucceeds()

            Dim compilation = CreateCompilationWithMscorlib40(source:="
Namespace Microsoft.CodeAnalysis
    Friend Class EmbeddedAttribute
        Inherits System.Attribute
    End Class
End Namespace
Namespace TestReference
    <Microsoft.CodeAnalysis.Embedded>
    Public Class TestType1
    End Class
    <Microsoft.CodeAnalysis.EmbeddedAttribute>
    Public Class TestType2
    End Class
    Public Class TestType3
    End Class
End Namespace
Public Class Program
    Public Shared Sub Main()
        Dim obj1 = New TestReference.TestType1()
        Dim obj2 = New TestReference.TestType2()
        Dim obj3 = New TestReference.TestType3()
    End Sub
End Class")

            AssertTheseEmitDiagnostics(compilation)

        End Sub

        <Fact>
        Public Sub EmbeddedAttributeInSourceIsAllowedIfCompilerDoesNotNeedToGenerateOne()

            Dim compilation = CreateCompilationWithMscorlib40(options:=TestOptions.ReleaseExe, source:=
<compilation>
    <file name="a.vb"><![CDATA[
Namespace Microsoft.CodeAnalysis
    Friend Class EmbeddedAttribute
        Inherits System.Attribute
    End Class
End Namespace
Namespace OtherNamespace
    <Microsoft.CodeAnalysis.Embedded>
    Public Class TestReference
        Public Shared Function GetValue() As Integer
            Return 3
        End Function
    End Class
End Namespace
Public Class Program
    Public Shared Sub Main()
        ' This should be fine, as the compiler doesn't need to use an embedded attribute for this compilation
        System.Console.Write(OtherNamespace.TestReference.GetValue())
    End Sub
End Class
]]>
    </file>
</compilation>)

            CompileAndVerify(compilation, expectedOutput:="3")
        End Sub

        <Fact>
        Public Sub EmbeddedTypesInAnAssemblyAreNotExposedExternally()

            Dim compilation1 = CreateCompilationWithMscorlib40(options:=TestOptions.ReleaseDll, source:=
<compilation>
    <file name="a.vb"><![CDATA[
Namespace Microsoft.CodeAnalysis
    Friend Class EmbeddedAttribute
        Inherits System.Attribute
    End Class
End Namespace
<Microsoft.CodeAnalysis.Embedded>
Public Class TestReference1
End Class
Public Class TestReference2
End Class
]]>
    </file>
</compilation>)

            Assert.NotNull(compilation1.GetTypeByMetadataName("TestReference1"))
            Assert.NotNull(compilation1.GetTypeByMetadataName("TestReference2"))

            Dim compilation2 = CreateCompilationWithMscorlib40("", references:={compilation1.EmitToImageReference()})

            Assert.Null(compilation2.GetTypeByMetadataName("TestReference1"))
            Assert.NotNull(compilation2.GetTypeByMetadataName("TestReference2"))
        End Sub

        <Fact>
        Public Sub AttributeWithTaskDelegateParameter()
            Dim code = "
Imports System
Imports System.Threading.Tasks

Namespace a
    Public Class Class1
        <AttributeUsage(AttributeTargets.Class, AllowMultiple:=True)>
        Public Class CommandAttribute
            Inherits Attribute

            Public Delegate Function FxCommand() As Task

            Public Sub New(Fx As FxCommand)
                Me.Fx = Fx
            End Sub

            Public Property Fx As FxCommand
        End Class

        <Command(AddressOf UserInfo)>
        Public Shared Async Function UserInfo() As Task
            Await New Task(
                Sub()
                End Sub)
        End Function
    End Class
End Namespace
"
            CreateCompilationWithMscorlib45(code).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_BadAttributeConstructor1, "Command").WithArguments("a.Class1.CommandAttribute.FxCommand").WithLocation(20, 10),
                Diagnostic(ERRID.ERR_RequiredConstExpr, "AddressOf UserInfo").WithLocation(20, 18))
        End Sub

        <Fact>
        Public Sub AttributeWithOptionalNullableParameter_NullIsPassed()
            Dim code = "
Imports System

Class MyAttribute
    Inherits Attribute
    Public Sub New(Optional x As Integer? = 0)
    End Sub
End Class

<My(Nothing)>
Class C
End Class
"
            CreateCompilation(code).AssertTheseDiagnostics(
<expected><![CDATA[
BC30045: Attribute constructor has a parameter of type 'Integer?', which is not an integral, floating-point or Enum type or one of Object, Char, String, Boolean, System.Type or 1-dimensional array of these types.
<My(Nothing)>
 ~~
]]></expected>)
        End Sub

        Private Shared ReadOnly experimentalAttributeCSharpSrc As String = "
#nullable enable

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }

        public string? UrlFormat { get; set; }
    }
}
"

        <Fact>
        Public Sub ExperimentalWithDiagnosticsId()
            Dim attrComp = CreateCSharpCompilation(experimentalAttributeCSharpSrc)

            Dim src = <compilation>
                          <file name="a.vb">
                              <![CDATA[
<System.Diagnostics.CodeAnalysis.Experimental("DiagID1")>
Class C
End Class

Class D
    Sub M(c As C)
    End Sub
End Class
]]>
                          </file>
                      </compilation>

            Dim comp = CreateCompilation(src, references:={attrComp.EmitToImageReference()})

            comp.AssertTheseDiagnostics(
<expected><![CDATA[
DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
    Sub M(c As C)
               ~
]]></expected>)

            Dim diag = comp.GetDiagnostics().Single()
            Assert.Equal("DiagID1", diag.Id)
            Assert.Equal(ERRID.WRN_Experimental, diag.Code)
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC42380)", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact>
        Public Sub ExperimentalWithDiagnosticsId_FullyQualified()
            Dim attrComp = CreateCSharpCompilation(experimentalAttributeCSharpSrc)

            Dim src = <compilation>
                          <file name="a.vb">
                              <![CDATA[
Namespace N
    <System.Diagnostics.CodeAnalysis.Experimental("DiagID1")>
    Class C
    End Class
End Namespace

Class D
    Sub M(c As N.C)
    End Sub
End Class
]]>
                          </file>
                      </compilation>

            Dim comp = CreateCompilation(src, references:={attrComp.EmitToImageReference()})

            comp.AssertTheseDiagnostics(
<expected><![CDATA[
DiagID1: 'N.C' is for evaluation purposes only and is subject to change or removal in future updates.
    Sub M(c As N.C)
               ~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ExperimentalWithDiagnosticsId_WithObsolete()
            Dim attrComp = CreateCSharpCompilation(experimentalAttributeCSharpSrc)

            Dim src = <compilation>
                          <file name="a.vb">
                              <![CDATA[
<System.Obsolete("error", True)>
<System.Diagnostics.CodeAnalysis.Experimental("DiagID1")>
Class C
End Class

Class D
    Sub M(c As C)
    End Sub
End Class
]]>
                          </file>
                      </compilation>

            Dim comp = CreateCompilation(src, references:={attrComp.EmitToImageReference()})

            comp.AssertTheseDiagnostics(
<expected><![CDATA[
DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
    Sub M(c As C)
               ~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ExperimentalWithDiagnosticsId_WithObsolete_Metadata()
            Dim attrReference = CreateCSharpCompilation(experimentalAttributeCSharpSrc).EmitToImageReference()

            Dim libSrc = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
<System.Obsolete("error", True)>
<System.Diagnostics.CodeAnalysis.Experimental("DiagID1")>
Public Class C
End Class
]]>
                             </file>
                         </compilation>

            Dim libComp = CreateCompilation(libSrc, references:={attrReference})

            Dim src = "
Class D
    Sub M(c As C)
    End Sub
End Class
"

            Dim comp = CreateCompilation(src, references:={attrReference, libComp.EmitToImageReference()})

            comp.AssertTheseDiagnostics(
<expected><![CDATA[
BC30668: 'C' is obsolete: 'error'.
    Sub M(c As C)
               ~
]]></expected>)

        End Sub

        <Fact>
        Public Sub ExperimentalWithDiagnosticsIdAndUrlFormat()
            Dim attrComp = CreateCSharpCompilation(experimentalAttributeCSharpSrc)

            Dim src = <compilation>
                          <file name="a.vb">
                              <![CDATA[
<System.Diagnostics.CodeAnalysis.Experimental("DiagID1", UrlFormat:="https://example.org/{0}")>
Class C
End Class

Class D
    Sub M(c As C)
    End Sub
End Class
]]>
                          </file>
                      </compilation>

            Dim comp = CreateCompilation(src, references:={attrComp.EmitToImageReference()})

            comp.AssertTheseDiagnostics(
<expected><![CDATA[
DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
    Sub M(c As C)
               ~
]]></expected>)

            Dim diag = Comp.GetDiagnostics().Single()
            Assert.Equal("DiagID1", diag.Id)
            Assert.Equal(ERRID.WRN_Experimental, diag.Code)
            Assert.Equal("https://example.org/DiagID1", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact>
        Public Sub ExperimentalWithDiagnosticsIdAndUrlFormat_InMetadata()
            Dim attrReference = CreateCSharpCompilation(experimentalAttributeCSharpSrc).EmitToImageReference()

            Dim libSrc = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
<System.Diagnostics.CodeAnalysis.Experimental("DiagID1", UrlFormat:="https://example.org/{0}")>
Public Class C
End Class
]]>
                             </file>
                         </compilation>

            Dim libComp = CreateCompilation(libSrc, references:={attrReference})

            Dim src = "
Class D
    Sub M(c As C)
    End Sub
End Class
"

            Dim comp = CreateCompilation(src, references:={attrReference, libComp.EmitToImageReference()})

            comp.AssertTheseDiagnostics(
<expected><![CDATA[
DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
    Sub M(c As C)
               ~
]]></expected>)

            Dim diag = comp.GetDiagnostics().Single()
            Assert.Equal("DiagID1", diag.Id)
            Assert.Equal(ERRID.WRN_Experimental, diag.Code)
            Assert.Equal("https://example.org/DiagID1", diag.Descriptor.HelpLinkUri)
        End Sub

    End Class
End Namespace
