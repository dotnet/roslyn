' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
#If Not Retargeting Then
    Public Class RetargetingTests
        Inherits BasicTestBase

        <Fact>
        Public Sub RetargetMembers()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation>
                    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

' Explicit members.
Class A
    Shared Sub New()
    End Sub

    Sub New()
    End Sub

    Public B

    Sub C(value As A)
    End Sub

    Function D(Of T)() As T
    End Function

    Property E

    Class F
    End Class

    Structure G
    End Structure

    Enum H
        Value
    End Enum

    Interface I
    End Interface

    <MarshalAs(UnmanagedType.SafeArray, SafeArraySubType:=VarEnum.VT_DISPATCH, SafeArrayUserDefinedSubType:=GetType(B))>
    Public MF As Integer

    Friend Function M(<MarshalAs(UnmanagedType.SafeArray, SafeArraySubType:=VarEnum.VT_DISPATCH, SafeArrayUserDefinedSubType:=GetType(B))> arg As Integer) _
        As <MarshalAs(UnmanagedType.SafeArray, SafeArraySubType:=VarEnum.VT_DISPATCH, SafeArrayUserDefinedSubType:=GetType(B))> Integer
        Return 1
    End Function
End Class
' Implicit constructors.
Class B
    Shared F = 1
    Private G = 2
End Class
' Generic type.
Class C(Of T)
    Sub M(Of U)(a As T, b As U)
    End Sub
    Function F() As C(Of A)
    End Function
End Class
]]>
                    </file>
                </compilation>)

            Dim sourceModule = compilation.SourceModule
            Dim sourceAssembly = DirectCast(sourceModule.ContainingAssembly, SourceAssemblySymbol)
            Dim sourceNamespace = sourceModule.GlobalNamespace

            Dim retargetingAssembly = New RetargetingAssemblySymbol(sourceAssembly, isLinked:=False)
            Dim retargetingModule = retargetingAssembly.Modules(0)
            Dim retargetingNamespace = retargetingModule.GlobalNamespace

            Dim sourceType As NamedTypeSymbol
            Dim retargetingType As NamedTypeSymbol

            Dim sourceMethod As MethodSymbol
            Dim retargetingMethod As MethodSymbol

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("A")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("A")
            CheckTypes(sourceType, retargetingType)

            CheckMethods(sourceType.GetMethod(".cctor"), retargetingType.GetMethod(".cctor"))
            CheckMethods(sourceType.GetMethod(".ctor"), retargetingType.GetMethod(".ctor"))
            CheckFields(sourceType.GetMember("B"), retargetingType.GetMember("B"))
            CheckFields(sourceType.GetMember("MF"), retargetingType.GetMember("MF"))
            CheckMethods(sourceType.GetMember("M"), retargetingType.GetMember("M"))

            sourceMethod = sourceType.GetMember(Of MethodSymbol)("C")
            retargetingMethod = retargetingType.GetMember(Of MethodSymbol)("C")
            CheckMethods(sourceMethod, retargetingMethod)
            CheckParameters(sourceMethod.Parameters(0), retargetingMethod.Parameters(0))

            sourceMethod = sourceType.GetMember(Of MethodSymbol)("D")
            retargetingMethod = retargetingType.GetMember(Of MethodSymbol)("D")
            CheckMethods(sourceMethod, retargetingMethod)
            CheckTypeParameters(sourceMethod.TypeParameters(0), retargetingMethod.TypeParameters(0))
            CheckTypeParameters(sourceMethod.ReturnType, retargetingMethod.ReturnType)

            CheckProperties(sourceType.GetMember("E"), retargetingType.GetMember("E"))

            CheckTypes(sourceType.GetMember("F"), retargetingType.GetMember("F"))
            CheckTypes(sourceType.GetMember("G"), retargetingType.GetMember("G"))
            CheckTypes(sourceType.GetMember("H"), retargetingType.GetMember("H"))
            CheckTypes(sourceType.GetMember("I"), retargetingType.GetMember("I"))

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("B")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("B")
            CheckTypes(sourceType, retargetingType)

            CheckMethods(sourceType.GetMethod(".cctor"), retargetingType.GetMethod(".cctor"))
            CheckMethods(sourceType.GetMethod(".ctor"), retargetingType.GetMethod(".ctor"))

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("C")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("C")
            CheckTypes(sourceType, retargetingType)
            CheckTypeParameters(sourceType.TypeParameters(0), retargetingType.TypeParameters(0))

            sourceMethod = sourceType.GetMember(Of MethodSymbol)("M")
            retargetingMethod = retargetingType.GetMember(Of MethodSymbol)("M")
            CheckMethods(sourceMethod, retargetingMethod)
            CheckTypeParameters(sourceMethod.TypeParameters(0), retargetingMethod.TypeParameters(0))

            sourceMethod = sourceType.GetMember(Of MethodSymbol)("F")
            retargetingMethod = retargetingType.GetMember(Of MethodSymbol)("F")
            CheckMethods(sourceMethod, retargetingMethod)

            sourceType = DirectCast(sourceMethod.ReturnType, NamedTypeSymbol)
            retargetingType = DirectCast(retargetingMethod.ReturnType, NamedTypeSymbol)
            CheckTypes(sourceType.TypeArguments(0), retargetingType.TypeArguments(0))
        End Sub

        Private Sub CheckTypes(source As Symbol, retargeting As Symbol)
            CheckUnderlyingMember(source, DirectCast(retargeting, RetargetingNamedTypeSymbol).UnderlyingNamedType)
        End Sub

        Private Sub CheckFields(source As Symbol, retargeting As Symbol)
            Dim a = DirectCast(source, FieldSymbol)
            Dim b = DirectCast(retargeting, RetargetingFieldSymbol)

            Dim aAssociated As Symbol = a.AssociatedSymbol
            Dim bAssociated As Symbol = b.AssociatedSymbol
            If aAssociated Is Nothing Then
                Assert.Null(bAssociated)
            ElseIf aAssociated.Kind = SymbolKind.Property Then
                CheckProperties(aAssociated, bAssociated)
            Else
                CheckEvent(aAssociated, bAssociated)
            End If

            Assert.Equal(a.Name, b.Name)
            CheckUnderlyingMember(a, b.UnderlyingField)
            CheckMarshallingInformation(a.MarshallingInformation, b.MarshallingInformation)
        End Sub

        Private Sub CheckMarshallingInformation(a As MarshalPseudoCustomAttributeData, b As MarshalPseudoCustomAttributeData)
            Assert.Equal(a Is Nothing, b Is Nothing)
            If a IsNot Nothing Then
                CheckTypes(DirectCast(a.TryGetSafeArrayElementUserDefinedSubtype(), TypeSymbol), DirectCast(b.TryGetSafeArrayElementUserDefinedSubtype(), TypeSymbol))
            End If
        End Sub

        Private Sub CheckMethods(source As Symbol, retargeting As Symbol)
            Dim a = DirectCast(source, MethodSymbol)
            Dim b = DirectCast(retargeting, RetargetingMethodSymbol)

            CheckUnderlyingMember(a, b.UnderlyingMethod)
            CheckMarshallingInformation(a.ReturnTypeMarshallingInformation, b.ReturnTypeMarshallingInformation)
        End Sub

        Private Sub CheckProperties(source As Symbol, retargeting As Symbol)
            CheckUnderlyingMember(source, DirectCast(retargeting, RetargetingPropertySymbol).UnderlyingProperty)
        End Sub

        Private Sub CheckTypeParameters(source As Symbol, retargeting As Symbol)
            CheckUnderlyingMember(source, DirectCast(retargeting, RetargetingTypeParameterSymbol).UnderlyingTypeParameter)
        End Sub

        Private Sub CheckParameters(source As Symbol, retargeting As Symbol)
            Dim a = DirectCast(source, ParameterSymbol)
            Dim b = DirectCast(retargeting, RetargetingParameterSymbol)

            CheckUnderlyingMember(a, b.UnderlyingParameter)
            CheckMarshallingInformation(a.MarshallingInformation, b.MarshallingInformation)
        End Sub

        Private Sub CheckEvent(source As Symbol, retargeting As Symbol)
            CheckUnderlyingMember(source, DirectCast(retargeting, RetargetingEventSymbol).UnderlyingEvent)
        End Sub

        Private Sub CheckUnderlyingMember(source As Symbol, underlying As Symbol)
            Assert.NotNull(source)
            Assert.NotNull(underlying)
            Assert.Same(source, underlying)
            Assert.Equal(source.IsImplicitlyDeclared, underlying.IsImplicitlyDeclared)
        End Sub

        <WorkItem(542571, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542571")>
        <Fact>
        Public Sub RetargetExplicitImplementationDifferentModule()

            Dim source1 =
<compilation name="assembly1">
    <file name="source1.vb">
Public Interface I(Of T)
    Sub M(Of U)(o As I(Of U))
    Sub N(o As I(Of T))
    ReadOnly Property P As I(Of T)
End Interface

Public Class A
End Class
    </file>
</compilation>

            Dim compilation1_v1 = CreateCompilationWithMscorlib40(source1)
            Dim compilation1_v2 = CreateCompilationWithMscorlib40(source1)

            Dim source2 =
<compilation name="assembly2">
    <file name="source2.vb">
Class B
    Implements I(Of A)

    Public Sub M(Of U)(o As I(Of U)) Implements I(Of A).M
    End Sub

    Public Sub N(o As I(Of A)) Implements I(Of A).N
    End Sub

    Public ReadOnly Property P As I(Of A) Implements I(Of A).P
        Get
            Return Nothing
        End Get
    End Property
End Class

Class C(Of CT)
    Implements I(Of CT)

    Public Sub M(Of U)(o As I(Of U)) Implements I(Of CT).M
    End Sub

    Public Sub N(o As I(Of CT)) Implements I(Of CT).N
    End Sub

    Public ReadOnly Property P As I(Of CT) Implements I(Of CT).P
        Get
            Return Nothing
        End Get
    End Property
End Class
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(source2, {New VisualBasicCompilationReference(compilation1_v1)})

            Dim compilation2Ref = New VisualBasicCompilationReference(compilation2)

            Dim compilation3 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="assembly3">
    <file name="source3.vb">
Public Interface I(Of T)
    </file>
</compilation>, {compilation2Ref, New VisualBasicCompilationReference(compilation1_v2)})

            Dim assembly2 = compilation3.GetReferencedAssemblySymbol(compilation2Ref)
            Dim implemented_m As MethodSymbol
            Dim implemented_n As MethodSymbol
            Dim implemented_p As PropertySymbol

            Dim b = assembly2.GetTypeByMetadataName("B")
            Dim m = b.GetMember(Of MethodSymbol)("M")
            implemented_m = m.ExplicitInterfaceImplementations(0)

            Assert.Equal("Sub I(Of A).M(Of U)(o As I(Of U))", implemented_m.ToTestDisplayString())

            Dim a_v2 = compilation1_v2.GetTypeByMetadataName("A")
            Dim i_a_v2 = compilation1_v2.GetTypeByMetadataName("I`1").Construct(ImmutableArray.Create(Of TypeSymbol)(a_v2))
            Dim i_a_m_v2 = i_a_v2.GetMember(Of MethodSymbol)("M")
            Assert.Equal(i_a_m_v2, implemented_m)

            Dim n = b.GetMember(Of MethodSymbol)("N")
            implemented_n = n.ExplicitInterfaceImplementations(0)

            Assert.Equal("Sub I(Of A).N(o As I(Of A))", implemented_n.ToTestDisplayString())

            Dim i_a_n_v2 = i_a_v2.GetMember(Of MethodSymbol)("N")
            Assert.Equal(i_a_n_v2, implemented_n)

            Dim p = b.GetMember(Of PropertySymbol)("P")
            implemented_p = p.ExplicitInterfaceImplementations(0)

            Assert.Equal("ReadOnly Property I(Of A).P As I(Of A)", implemented_p.ToTestDisplayString())

            Dim i_a_p_v2 = i_a_v2.GetMember(Of PropertySymbol)("P")
            Assert.Equal(i_a_p_v2, implemented_p)

            Dim c = assembly2.GetTypeByMetadataName("C`1")
            Dim i_ct_v2 = compilation1_v2.GetTypeByMetadataName("I`1").Construct(ImmutableArray.Create(Of TypeSymbol)(c.TypeParameters(0)))

            implemented_m = c.GetMember(Of MethodSymbol)("M").ExplicitInterfaceImplementations(0)

            Assert.Equal("Sub I(Of CT).M(Of U)(o As I(Of U))", implemented_m.ToTestDisplayString())

            Dim i_ct_m_v2 = i_ct_v2.GetMember(Of MethodSymbol)("M")
            Assert.Equal(i_ct_m_v2, implemented_m)

            implemented_n = c.GetMember(Of MethodSymbol)("N").ExplicitInterfaceImplementations(0)

            Assert.Equal("Sub I(Of CT).N(o As I(Of CT))", implemented_n.ToTestDisplayString())

            Dim i_ct_n_v2 = i_ct_v2.GetMember(Of MethodSymbol)("N")
            Assert.Equal(i_ct_n_v2, implemented_n)

            implemented_p = c.GetMember(Of PropertySymbol)("P").ExplicitInterfaceImplementations(0)

            Assert.Equal("ReadOnly Property I(Of CT).P As I(Of CT)", implemented_p.ToTestDisplayString())

            Dim i_ct_p_v2 = i_ct_v2.GetMember(Of PropertySymbol)("P")
            Assert.Equal(i_ct_p_v2, implemented_p)
        End Sub

        <Fact>
        <WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")>
        Public Sub RetargetInvalidEnumUnderlyingType_Implicit()
            Dim source =
<compilation name="test">
    <file name="a.vb">
Public Enum E
    A
End Enum
    </file>
</compilation>

            Dim comp = CreateEmptyCompilation(source)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UndefinedType1, "E").WithArguments("System.Int32"),
                Diagnostic(ERRID.ERR_UndefinedType1, <![CDATA[Public Enum E
    A
End Enum]]>.Value.Replace(vbLf, vbCrLf)).WithArguments("System.Void"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Enum", "test.dll"))

            Dim sourceAssembly = DirectCast(comp.Assembly, SourceAssemblySymbol)
            Dim sourceType = sourceAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("E")
            Assert.Equal(0, sourceType.Interfaces.Length)
            Assert.Equal(TypeKind.Error, sourceType.BaseType.TypeKind)
            Assert.Equal(SpecialType.System_Enum, sourceType.BaseType.SpecialType)
            Assert.Equal(TypeKind.Error, sourceType.EnumUnderlyingType.TypeKind)
            Assert.Equal(SpecialType.System_Int32, sourceType.EnumUnderlyingType.SpecialType)

            Dim retargetingAssembly = New RetargetingAssemblySymbol(sourceAssembly, isLinked:=False)
            retargetingAssembly.SetCorLibrary(sourceAssembly.CorLibrary) ' Need to do this explicitly since our retargeting assembly wasn't constructed using the real mechanism.
            Dim retargetingType = retargetingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("E")
            Assert.Equal(0, retargetingType.Interfaces.Length)
            Assert.Equal(TypeKind.Error, retargetingType.BaseType.TypeKind)
            Assert.Equal(SpecialType.System_Enum, retargetingType.BaseType.SpecialType)
            Assert.Equal(TypeKind.Error, retargetingType.EnumUnderlyingType.TypeKind)
            Assert.Equal(SpecialType.System_Int32, retargetingType.EnumUnderlyingType.SpecialType)
        End Sub

        <Fact>
        <WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")>
        Public Sub RetargetInvalidEnumUnderlyingType_Explicit()
            Dim source =
<compilation name="test">
    <file name="a.vb">
Public Enum E As Short
    A
End Enum
    </file>
</compilation>

            Dim comp = CreateEmptyCompilation(source)
            comp.VerifyDiagnostics(
    Diagnostic(ERRID.ERR_UndefinedType1, "Short").WithArguments("System.Int16"),
    Diagnostic(ERRID.ERR_UndefinedType1, <![CDATA[Public Enum E As Short
    A
End Enum]]>.Value.Replace(vbLf, vbCrLf)).WithArguments("System.Void"),
    Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Enum", "test.dll"))

            Dim sourceAssembly = DirectCast(comp.Assembly, SourceAssemblySymbol)
            Dim sourceType = sourceAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("E")
            Assert.Equal(0, sourceType.Interfaces.Length)
            Assert.Equal(TypeKind.Error, sourceType.BaseType.TypeKind)
            Assert.Equal(SpecialType.System_Enum, sourceType.BaseType.SpecialType)
            Assert.Equal(TypeKind.Error, sourceType.EnumUnderlyingType.TypeKind)
            Assert.Equal(SpecialType.System_Int16, sourceType.EnumUnderlyingType.SpecialType)

            Dim retargetingAssembly = New RetargetingAssemblySymbol(sourceAssembly, isLinked:=False)
            retargetingAssembly.SetCorLibrary(sourceAssembly.CorLibrary) ' Need to do this explicitly since our retargeting assembly wasn't constructed using the real mechanism.
            Dim retargetingType = retargetingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("E")
            Assert.Equal(0, retargetingType.Interfaces.Length)
            Assert.Equal(TypeKind.Error, retargetingType.BaseType.TypeKind)
            Assert.Equal(SpecialType.System_Enum, retargetingType.BaseType.SpecialType)
            Assert.Equal(TypeKind.Error, retargetingType.EnumUnderlyingType.TypeKind)
            Assert.Equal(SpecialType.System_Int16, retargetingType.EnumUnderlyingType.SpecialType)
        End Sub

        <Fact>
        <WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")>
        Public Sub RetargetInvalidInterfaceType_Class()
            Dim source =
<compilation>
    <file name="a.vb">
Public Class Test
	Implements Short
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_BadImplementsType, "Short"))

            Dim sourceAssembly = DirectCast(comp.Assembly, SourceAssemblySymbol)
            Dim sourceType = sourceAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.Equal(0, sourceType.Interfaces.Length)
            Assert.Equal(SpecialType.System_Object, sourceType.BaseType.SpecialType)

            Dim retargetingAssembly = New RetargetingAssemblySymbol(sourceAssembly, isLinked:=False)
            Dim retargetingType = retargetingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.Equal(0, retargetingType.Interfaces.Length)
            Assert.Equal(SpecialType.System_Object, retargetingType.BaseType.SpecialType)
        End Sub

        <Fact>
        <WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")>
        Public Sub RetargetMissingInterfaceType_Class()
            Dim source =
<compilation name="test">
    <file name="a.vb">
Public Class Test
    Implements Short
End Class
    </file>
</compilation>

            Dim comp = CreateEmptyCompilation(source)

            AssertTheseDiagnostics(comp,
<expected>
BC30002: Type 'System.Void' is not defined.
Public Class Test
~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'test.dll' failed.
Public Class Test
             ~~~~
BC30002: Type 'System.Int16' is not defined.
    Implements Short
               ~~~~~
</expected>)

            ' NOTE: slightly different from C# result, because Short can't be re-interpreted as a base type.

            Dim sourceAssembly = DirectCast(comp.Assembly, SourceAssemblySymbol)
            Dim sourceType = sourceAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.Equal(1, sourceType.Interfaces.Length)
            Assert.Equal(SpecialType.System_Object, sourceType.BaseType.SpecialType)

            Dim retargetingAssembly = New RetargetingAssemblySymbol(sourceAssembly, isLinked:=False)
            Dim retargetingType = retargetingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.Equal(1, retargetingType.Interfaces.Length)
            Assert.Equal(SpecialType.System_Object, retargetingType.BaseType.SpecialType)
        End Sub

        <Fact>
        <WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")>
        Public Sub RetargetInvalidInterfaceType_Struct()
            Dim source =
    <compilation>
        <file name="a.vb">
Public Structure Test
	Implements Short
End Structure
    </file>
    </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_BadImplementsType, "Short"))

            Dim sourceAssembly = DirectCast(comp.Assembly, SourceAssemblySymbol)
            Dim sourceType = sourceAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.Equal(0, sourceType.Interfaces.Length)
            Assert.Equal(SpecialType.System_ValueType, sourceType.BaseType.SpecialType)

            Dim retargetingAssembly = New RetargetingAssemblySymbol(sourceAssembly, isLinked:=False)
            Dim retargetingType = retargetingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.Equal(0, retargetingType.Interfaces.Length)
            Assert.Equal(SpecialType.System_ValueType, retargetingType.BaseType.SpecialType)
        End Sub

        <Fact>
        <WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")>
        <WorkItem(609515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609515")>
        Public Sub RetargetMissingInterfaceType_Struct()
            Dim source =
    <compilation name="test">
        <file name="a.vb">
Public Structure Test
    Implements Short
End Structure
    </file>
    </compilation>

            Dim comp = CreateEmptyCompilation(source)

            AssertTheseDiagnostics(comp,
<expected>
BC30002: Type 'System.Void' is not defined.
Public Structure Test
~~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'ValueType' from assembly or module 'test.dll' failed.
Public Structure Test
                 ~~~~
BC30002: Type 'System.Int16' is not defined.
    Implements Short
               ~~~~~
</expected>)

            Dim sourceAssembly = DirectCast(comp.Assembly, SourceAssemblySymbol)
            Dim sourceType = sourceAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.Equal(1, sourceType.Interfaces.Length)
            Assert.Equal(TypeKind.Error, sourceType.BaseType.TypeKind)
            Assert.Equal(SpecialType.System_ValueType, sourceType.BaseType.SpecialType)

            Dim retargetingAssembly = New RetargetingAssemblySymbol(sourceAssembly, isLinked:=False)
            Dim retargetingType = retargetingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.Equal(1, retargetingType.Interfaces.Length)
            Assert.Equal(TypeKind.Error, retargetingType.BaseType.TypeKind)
            Assert.Equal(SpecialType.System_ValueType, retargetingType.BaseType.SpecialType)
        End Sub

        <Fact>
        <WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")>
        Public Sub RetargetInvalidInterfaceType_Interface()
            Dim source =
    <compilation>
        <file name="a.vb">
Public Interface Test
    Inherits Short
End Interface
    </file>
    </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            AssertTheseDiagnostics(comp,
<expected>
BC30354: Interface can inherit only from another interface.
    Inherits Short
             ~~~~~
</expected>)

            Dim sourceAssembly = DirectCast(comp.Assembly, SourceAssemblySymbol)
            Dim sourceType = sourceAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.Equal(0, sourceType.Interfaces.Length)
            Assert.Null(sourceType.BaseType)

            Dim retargetingAssembly = New RetargetingAssemblySymbol(sourceAssembly, isLinked:=False)
            Dim retargetingType = retargetingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.Equal(0, retargetingType.Interfaces.Length)
            Assert.Null(retargetingType.BaseType)
        End Sub

        <Fact>
        <WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")>
        <WorkItem(609515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609515")>
        Public Sub RetargetMissingInterfaceType_Interface()
            Dim source =
    <compilation>
        <file name="a.vb">
Public Interface Test
	Inherits Short
End Interface
    </file>
    </compilation>

            Dim comp = CreateEmptyCompilation(source)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UndefinedType1, "Short").WithArguments("System.Int16"))

            Dim sourceAssembly = DirectCast(comp.Assembly, SourceAssemblySymbol)
            Dim sourceType = sourceAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.Equal(1, sourceType.Interfaces.Length)
            Assert.Null(sourceType.BaseType)

            Dim retargetingAssembly = New RetargetingAssemblySymbol(sourceAssembly, isLinked:=False)
            Dim retargetingType = retargetingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.Equal(1, retargetingType.Interfaces.Length)
            Assert.Null(retargetingType.BaseType)
        End Sub

        <Fact>
        <WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")>
        <WorkItem(609519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609519")>
        Public Sub RetargetInvalidConstraint()
            Dim source =
    <compilation>
        <file name="a.vb">
Public Class C(Of T As Short)
End Class
    </file>
    </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            comp.AssertTheseDiagnostics(<expected>
BC32048: Type constraint 'Short' must be either a class, interface or type parameter.
Public Class C(Of T As Short)
                       ~~~~~
</expected>)

            Dim sourceAssembly = DirectCast(comp.Assembly, SourceAssemblySymbol)
            Dim sourceType = sourceAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Dim sourceTypeParameter = sourceType.TypeParameters.Single()
            Dim sourceTypeParameterConstraint = sourceTypeParameter.ConstraintTypes.Single()
            Assert.Equal(SpecialType.System_Int16, sourceTypeParameterConstraint.SpecialType)

            Dim retargetingAssembly = New RetargetingAssemblySymbol(sourceAssembly, isLinked:=False)
            Dim retargetingType = retargetingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Dim retargetingTypeParameter = retargetingType.TypeParameters.Single()
            Dim retargetingTypeParameterConstraint = retargetingTypeParameter.ConstraintTypes.Single()
            Assert.Equal(SpecialType.System_Int16, retargetingTypeParameterConstraint.SpecialType)
        End Sub

        <Fact>
        <WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")>
        <WorkItem(609519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609519")>
        Public Sub RetargetMissingConstraint()
            Dim source =
    <compilation name="test">
        <file name="a.vb">
Public Class C(Of T As Short)
End Class
    </file>
    </compilation>

            Dim comp = CreateEmptyCompilation(source)
            comp.AssertTheseDiagnostics(<expected>
BC30002: Type 'System.Void' is not defined.
Public Class C(Of T As Short)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'test.dll' failed.
Public Class C(Of T As Short)
             ~
BC30002: Type 'System.Int16' is not defined.
Public Class C(Of T As Short)
                       ~~~~~
BC31091: Import of type 'Short' from assembly or module 'test.dll' failed.
Public Class C(Of T As Short)
                       ~~~~~
</expected>)

            Dim sourceAssembly = DirectCast(comp.Assembly, SourceAssemblySymbol)
            Dim sourceType = sourceAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Dim sourceTypeParameter = sourceType.TypeParameters.Single()
            Dim sourceTypeParameterConstraint = sourceTypeParameter.ConstraintTypes.Single()
            Assert.Equal(TypeKind.Error, sourceTypeParameterConstraint.TypeKind)
            Assert.Equal(SpecialType.System_Int16, sourceTypeParameterConstraint.SpecialType)

            Dim retargetingAssembly = New RetargetingAssemblySymbol(sourceAssembly, isLinked:=False)
            Dim retargetingType = retargetingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Dim retargetingTypeParameter = retargetingType.TypeParameters.Single()
            Dim retargetingTypeParameterConstraint = retargetingTypeParameter.ConstraintTypes.Single()
            Assert.Equal(TypeKind.Error, retargetingTypeParameterConstraint.TypeKind)
            Assert.Equal(SpecialType.System_Int16, retargetingTypeParameterConstraint.SpecialType)
        End Sub

        <Fact>
        Public Sub Retarget_Non_GenericTypes()
            'The test involves compilation with/without retargeting and ensuring same behavior at runtime
            'same diagnostics (or lack off) as compile time
            'and verification of the retargeted types and the underlying source type and pertinent items

            Dim source =
               <compilation name="App1">
                   <file name="a.vb"><![CDATA[
Imports System
Imports ClassLibrary1
Imports ClassLibrary1.TestNS

Module Module1
    Sub Main()
        Dim Usage_Class As New NewClass
        Usage_Class.TestProperty = 101
        With Usage_Class
            ._Field = 102
            If .ReadOnlyProperty <> 102 Then
                Console.WriteLine("Problem")
            End If

            .WriteOnlyProperty = 103

            If .ReadOnlyProperty <> 103 Then
                Console.WriteLine("Problem")
            End If
        End With
        Console.WriteLine("Success")
    End Sub
End Module

'We need to use each of the items in the Class Library

Class NewClass
    Inherits ClassLibrary1.TestClass
End Class

<Test>
Class NewClass_Using_Attribute

End Class


Class NewClass_ImplementingInterface
    Implements ClassLibrary1.TestInterface

    Public Sub Sub_In_Interface(y As String) Implements ClassLibrary1.TestInterface.Sub_In_Interface
        Console.Writeline("Success")
    End Sub

    Public Function Function_In_Interface(x As Integer) As Boolean Implements ClassLibrary1.TestInterface.Function_In_Interface
        Return True
    End Function
End Class


Class New_InheritedAttribute
    Inherits ClassLibrary1.TestAttribute
End Class

Module Module_Usage
    Sub TestMethod()
        Dim x As New ClassLibrary1.Class_With_Attribute

        'Structure
        Dim y As New ClassLibrary1.TestStructure
        y._int_In_Structure = 102

        If y._int_In_Structure <> 102 Then
            Console.WriteLine("Problem")
        End If

        y.String_Property = "Success"
        Console.WriteLine("Property:" & y.String_Property)
    End Sub

    Sub test_Constraints()

        Dim t1 As New ClassLibrary1.TestClass
        Dim t2 As New ClassLibrary1.TestStructure
        Dim t3 As New ClassLibrary1.Test_Enum
        Dim t4 As New ClassLibrary1.TestNS.Class_In_NS
        Dim t5 As New ClassLibrary1.NestedTypeBase
        Dim t5_1 As New ClassLibrary1.NestedTypeDerived
        Dim t6 As New ClassLibrary1.Structure_With_Attribute

        Dim c1 = ClassLibrary1.Test_Module.Test_Constant
        Dim d1 As ClassLibrary1.Test_Module.DelSub
        Dim d2 As ClassLibrary1.Test_Module.DelFunction

        d1 = AddressOf goo
        d2 = AddressOf bar

    End Sub

    Sub Usage_Attribute()
        Dim x As Structure_With_Attribute
        x._int_In_Structure = 1
    End Sub

    Sub TestEnum()
        Dim EnumValue = Test_Enum.Item1
        EnumValue = Test_Enum.Item3

        If ClassLibrary1.Test_Module.Test_Constant <> 101 Then
            Console.WriteLine("Problem")
        End If

        Dim DelSub As ClassLibrary1.Test_Module.DelSub = AddressOf DelTestMethod
        DelSub.Invoke(1)

        Dim DelFunc As ClassLibrary1.Test_Module.DelFunction = AddressOf DelTestMethod
        Dim xresult = DelFunc("Test")
    End Sub

    Sub DelTestMethod(x As Integer)
        Console.WriteLine("Success")
    End Sub

    Function DelTestMethod(x As String) As Boolean
        If x = "Test" Then
            Return True
        Else
            Return False
        End If
    End Function


    Sub NestedTypes()
        Dim xb As New ClassLibrary1.NestedTypeBase
        Dim xd As New ClassLibrary1.NestedTypeDerived

        If TypeOf (xd) Is ClassLibrary1.NestedTypeDerived Then
            Console.WriteLine("Success")
        End If
    End Sub

    Sub TestNs()
        Dim x As New Class_In_NS
        If x.SuccessProperty = True Then
            Console.WriteLine("Success")
        End If
    End Sub

    Private Sub goo()
        Console.WriteLine("goo")
    End Sub

    Private Function bar(x As String) As Boolean
        Return True
    End Function

End Module

Class ABC
End Class
]]>
                   </file>
               </compilation>

            Dim CL1_source =
               <compilation name="C1">
                   <file name="Cl.vb"><![CDATA[
Imports System

Namespace ClassLibrary1

Public Class TestClass
    'This Class has Properties and Fields of various types etc which 
    'should be accessible from outside
    Public Property TestProperty As Integer

    Public ReadOnly Property ReadOnlyProperty As String
        Get
            Return _Field
        End Get
    End Property
    Public WriteOnly Property WriteOnlyProperty As String
        Set(value As String)
            _Field = value
        End Set
    End Property

    Public _Field As String = ""
End Class

Public Structure TestStructure
    Public _int_In_Structure As Integer
    Public Property String_Property As String
End Structure

Public Interface TestInterface
    Sub Sub_In_Interface(y As String)
    Function Function_In_Interface(x As Integer) As Boolean
End Interface

Public Class TestAttribute
    Inherits Attribute
End Class

<Test>
Public Class Class_With_Attribute

End Class


<Test>
Public Structure Structure_With_Attribute    
    Public _int_In_Structure As Integer
    Public Property String_Property As String
End Structure

'Test Enum
Public Enum Test_Enum
    Item1
    Item3 = 3
End Enum


Public Module Test_Module
    Public Const Test_Constant As Integer = 101

    Public Delegate Sub DelSub(x As Integer)
    Public Delegate Function DelFunction(x As String) As Boolean
End Module

Public Class NestedTypeBase
End Class

Public Class NestedTypeDerived
    Inherits NestedTypeBase
End Class

Namespace TestNS
    Public Class Class_In_NS
        Public Property SuccessProperty As Boolean = True
    End Class
End Namespace

End Namespace
]]>
                   </file>
               </compilation>

            'Check Expected Behavior - Expect no diagnostic errors without retargeting            
            Dim referenceLibrary_Compilation = DirectCast(CompileAndVerify(CL1_source, options:=TestOptions.ReleaseDll).Compilation, VisualBasicCompilation)

            Dim referenceLibrary_Metadata = referenceLibrary_Compilation.ToMetadataReference
            Dim main_NoRetarget = CompileAndVerify(source, references:={referenceLibrary_Metadata},
                                                   expectedOutput:=<![CDATA[Success
]]>)
            main_NoRetarget.VerifyDiagnostics()

            'Retargetted - should result in No additional Errors also and same runtime behavior
            Dim RetargetReference = RetargetCompilationToV2MsCorlib(referenceLibrary_Compilation)
            ' ILVerify: Multiple modules named 'mscorlib' were found
            Dim Main_Retarget = CompileAndVerify(source, references:={RetargetReference}, options:=TestOptions.ReleaseExe, verify:=Verification.FailsILVerify,
                                                 expectedOutput:=<![CDATA[Success
]]>)
            Main_Retarget.VerifyDiagnostics()

            'Check the retargeting symbol information
            Dim sourceAssembly = DirectCast(RetargetReference.Compilation.Assembly, SourceAssemblySymbol)
            Dim SourceModuleReference = sourceAssembly.Modules(0)
            Dim sourceNamespace As SourceNamespaceSymbol = CType(SourceModuleReference.GlobalNamespace.GetNamespace("ClassLibrary1"), SourceNamespaceSymbol)

            Dim retargetingAssembly = Main_Retarget.Compilation.GetReferencedAssemblySymbol(RetargetReference)
            Dim retargetingModule = retargetingAssembly.Modules(0)
            Dim retargetingNamespace As RetargetingNamespaceSymbol = CType(retargetingModule.GlobalNamespace.GetNamespace("ClassLibrary1"), RetargetingNamespaceSymbol)

            'Comparison of Types
            Dim sourceType As NamedTypeSymbol = Nothing
            Dim retargetingType As NamedTypeSymbol = Nothing

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestClass")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestClass")
            CheckTypes(sourceType, retargetingType)

            ' CheckMethods(sourceType.GetMethod(".cctor"), retargetingType.GetMethod(".cctor"))
            CheckMethods(sourceType.GetMethod(".ctor"), retargetingType.GetMethod(".ctor"))
            CheckFields(sourceType.GetMember("_Field"), retargetingType.GetMember("_Field"))
            CheckProperties(sourceType.GetMember("TestProperty"), retargetingType.GetMember("TestProperty"))
            CheckProperties(sourceType.GetMember("ReadOnlyProperty"), retargetingType.GetMember("ReadOnlyProperty"))
            CheckProperties(sourceType.GetMember("WriteOnlyProperty"), retargetingType.GetMember("WriteOnlyProperty"))

            'Structure
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestStructure")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestStructure")
            CheckTypes(sourceType, retargetingType)
            CheckFields(sourceType.GetMember("_int_In_Structure"), retargetingType.GetMember("_int_In_Structure"))
            CheckProperties(sourceType.GetMember("String_Property"), retargetingType.GetMember("String_Property"))
            Assert.Equal(sourceType.GetAttributes.Length, retargetingType.GetAttributes.Length)
            Assert.Equal(0, sourceType.GetAttributes.Length)

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Structure_With_Attribute")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Structure_With_Attribute")
            CheckTypes(sourceType, retargetingType)
            CheckFields(sourceType.GetMember("_int_In_Structure"), retargetingType.GetMember("_int_In_Structure"))
            CheckProperties(sourceType.GetMember("String_Property"), retargetingType.GetMember("String_Property"))

            'Same Structure Content but different with/without attribute
            Assert.NotSame(sourceNamespace.GetMember(Of NamedTypeSymbol)("Structure_With_Attribute"), sourceNamespace.GetMember(Of NamedTypeSymbol)("TestStructure"))
            Assert.Equal(1, sourceType.GetAttributes.Length)

            'Attribute
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestAttribute")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestAttribute")
            CheckTypes(sourceType, retargetingType)
            Assert.Equal(sourceType.GetAttributeTarget, retargetingType.GetAttributeTarget)

            'Enums
            'Public Enum Test_Enum
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Test_Enum")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Test_Enum")
            CheckTypes(sourceType, retargetingType)
            Assert.Equal(4, sourceType.GetMembers.Length)
            Assert.Equal(sourceType.GetMembers.Length, retargetingType.GetMembers.Length)

            'Modules - for VB
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Test_Module")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Test_Module")
            CheckTypes(sourceType, retargetingType)
            Assert.Equal(sourceType.GetMembers.Length, retargetingType.GetMembers.Length)
            Assert.Equal(3, sourceType.GetMembers.Length)

            'Delegates
            Assert.Equal(TypeKind.Delegate, CType(sourceType.GetMembers.Item(1), SourceNamedTypeSymbol).TypeKind)
            Assert.Equal(TypeKind.Delegate, CType(sourceType.GetMembers.Item(2), SourceNamedTypeSymbol).TypeKind)

            CheckFields(sourceType.GetMembers.Item(0), retargetingType.GetMembers.Item(0)) 'Const
            CheckTypes(sourceType.GetMembers.Item(1), retargetingType.GetMembers.Item(1)) 'Delegate
            CheckTypes(sourceType.GetMembers.Item(2), retargetingType.GetMembers.Item(2)) 'Delegate
        End Sub

        <Fact>
        Public Sub Retarget_GenericTypes()
            'The test involves compilation with/without retargeting and ensuring same behavior at runtime
            'same diagnostics (or lack off) as compile time
            'and verification of the retargeted types and the underlying source type and pertinent items

            Dim source =
               <compilation name="App1">
                   <file name="a.vb"><![CDATA[
Imports System
Imports ClassLibrary1
Imports ClassLibrary1.TestNS

Module Module1
    Sub Main()
            Dim Usage_Class As New NewClass
            Usage_Class.TestProperty = 101
            With Usage_Class
                ._Field = 102
                If .ReadOnlyProperty <> 102 Then
                    Console.WriteLine("Problem")
                End If

                .WriteOnlyProperty = 103

                If .ReadOnlyProperty <> 103 Then
                    Console.WriteLine("Problem")
                End If
            End With
            Console.WriteLine("Success")
    End Sub
End Module

            'We need to use each of the items in the Class Library

Class NewClass
    Inherits ClassLibrary1.TestClass
End Class

<ClassLibrary1.Test>
Class NewClass_Using_Attribute

        End Class


        Class NewClass_ImplementingInterface
            Implements ClassLibrary1.TestInterface

            Public Sub Sub_In_Interface(y As String) Implements ClassLibrary1.TestInterface.Sub_In_Interface
            Console.WriteLine("Success")
            End Sub

            Public Function Function_In_Interface(x As Integer) As Boolean Implements ClassLibrary1.TestInterface.Function_In_Interface
            Return True
            End Function
        End Class


        Class New_InheritedAttribute
            Inherits ClassLibrary1.TestAttribute
        End Class

Module Module_Usage
            Sub TestMethod()
            Dim x As New ClassLibrary1.Class_With_Attribute

            'Structure
            Dim y As New ClassLibrary1.TestStructure
            y._int_In_Structure = 102

            If y._int_In_Structure <> 102 Then
                Console.WriteLine("Problem")
            End If

            y.String_Property = "Success"
            Console.WriteLine("Property:" & y.String_Property)
            End Sub

            Sub test_Constraints()
            Dim t1 As New ClassLibrary1.Generic_Test_Class(Of NewClass)
            Dim t2 As New ClassLibrary1.Generic_Test_Class_Constrained_Interface(Of ClassLibrary1.TestInterface)
            Dim t3 As New ClassLibrary1.Generic_Test_Class_Constrained_New(Of NewClass)
            Dim t3_1 As New ClassLibrary1.Generic_Test_Class_Constrained_New(Of ClassLibrary1.Generic_Test_Class(Of Integer))
            Dim t4 As New ClassLibrary1.Generic_Test_Class_Constrained_Specific(Of ClassLibrary1.TestClass)
            Dim t4_1 As New ClassLibrary1.Generic_Test_Class_Constrained_Specific(Of NewClass)
            Dim t5 As New ClassLibrary1.Generic_Test_Class_Constrained_Multiple(Of ClassLibrary1.TestClass)

#If CompErrorTest Then
        Dim f_t2 As New ClassLibrary1.Generic_Test_Class_Constrained_Interface(Of NewClass)
        Dim f_t3 As New ClassLibrary1.Generic_Test_Class_Constrained_New(Of ClassLibrary1.TestInterface)
        Dim f_t4 As New ClassLibrary1.Generic_Test_Class_Constrained_Specific(Of Integer)
        Dim f_t5 As New ClassLibrary1.Generic_Test_Class_Constrained_Multiple(Of ABC)
#End If
            End Sub

            Sub Usage_Attribute()
            Dim x As ClassLibrary1.Structure_With_Attribute
            x._int_In_Structure = 1
            End Sub

            Sub TestEnum()
            Dim EnumValue = ClassLibrary1.Test_Enum.Item1
            EnumValue = ClassLibrary1.Test_Enum.Item3

            If ClassLibrary1.Test_Module.Test_Constant <> 101 Then
                Console.WriteLine("Problem")
            End If

            Dim DelSub As ClassLibrary1.Test_Module.DelSub = AddressOf DelTestMethod
            DelSub.Invoke(1)

            Dim DelFunc As ClassLibrary1.Test_Module.DelFunction = AddressOf DelTestMethod
            Dim xresult = DelFunc("Test")
            End Sub

            Sub DelTestMethod(x As Integer)
            Console.WriteLine("Success")
            End Sub

            Function DelTestMethod(x As String) As Boolean
            If x = "Test" Then
                Return True
            Else
                Return False
            End If
            End Function


            Sub NestedTypes()
            Dim xb As New ClassLibrary1.NestedTypeBase
            Dim xd As New ClassLibrary1.NestedTypeDerived

            If TypeOf (xd) Is ClassLibrary1.NestedTypeDerived Then
                Console.WriteLine("Success")
            End If
            End Sub

            Sub TestNs()
            Dim x As New Class_In_NS
            If x.SuccessProperty = True Then
                Console.WriteLine("Success")
            End If
            End Sub
        End Module
        Class ABC
        End Class
]]>
                   </file>
               </compilation>

            Dim CL1_source =
               <compilation name="C1">
                   <file name="Cl.vb"><![CDATA[
Imports System

Namespace ClassLibrary1

Public Class TestClass
    'This Class has Properties and Fields of various types etc which 
    'should be accessible from outside
    Public Property TestProperty As Integer

    Public ReadOnly Property ReadOnlyProperty As String
        Get
            Return _Field
        End Get
    End Property
    Public WriteOnly Property WriteOnlyProperty As String
        Set(value As String)
            _Field = value
        End Set
    End Property

    Public _Field As String = ""
End Class

Public Structure TestStructure
    Public _int_In_Structure As Integer
    Public Property String_Property As String
End Structure

Public Interface TestInterface
    Sub Sub_In_Interface(y As String)
    Function Function_In_Interface(x As Integer) As Boolean
End Interface

Public Class TestAttribute
    Inherits Attribute
End Class

<Test>
Public Class Class_With_Attribute

End Class


<Test>
Public Structure Structure_With_Attribute    
    Public _int_In_Structure As Integer
    Public Property String_Property As String
End Structure

'Test Enum
Public Enum Test_Enum
    Item1
    Item3 = 3
End Enum


Public Module Test_Module
    Public Const Test_Constant As Integer = 101

    Public Delegate Sub DelSub(x As Integer)
    Public Delegate Function DelFunction(x As String) As Boolean
End Module



'Generic Types
Public Class Generic_Test_Class(Of t)
    Public Function xyz() As Integer
        Return True
    End Function
End Class

'Constrained
Public Class Generic_Test_Class_Constrained_Specific(Of t As TestClass)
    Public Function xyz() As Integer
        Return True
    End Function
End Class

Public Class Generic_Test_Class_Constrained_Interface(Of t As TestInterface)
    Public Function xyz() As Integer
        Return True
    End Function
End Class

Public Class Generic_Test_Class_Constrained_New(Of t As New)
    Public Function xyz() As Integer
        Return True
    End Function
End Class

Public Class Generic_Test_Class_Constrained_Multiple(Of t As {New, TestClass})
    Public Function xyz() As Integer
        Return True
    End Function
End Class


Public Class NestedTypeBase

End Class

Public Class NestedTypeDerived
    Inherits NestedTypeBase
End Class

Namespace TestNS
    Public Class Class_In_NS
        Public Property SuccessProperty As Boolean = True
    End Class
End Namespace

End Namespace
]]>
                   </file>
               </compilation>

            'Check Expected Behavior - Expect no diagnostic errors with retargeting
            ' All on same FX - should result in No Errors
            Dim referenceLibrary_Compilation = DirectCast(CompileAndVerify(CL1_source, options:=TestOptions.ReleaseDll).Compilation, VisualBasicCompilation)

            Dim referenceLibrary_Metadata = referenceLibrary_Compilation.ToMetadataReference
            Dim main_NoRetarget = CompileAndVerify(source, references:={referenceLibrary_Metadata},
                                                   expectedOutput:=<![CDATA[Success
]]>)
            main_NoRetarget.VerifyDiagnostics()

            '//Retargetted - should result in No Errors also and same runtime behavior
            Dim RetargetReference = RetargetCompilationToV2MsCorlib(referenceLibrary_Compilation)
            ' ILVerify: Multiple modules named 'mscorlib' were found
            Dim Main_Retarget = CompileAndVerify(source, references:={RetargetReference}, options:=TestOptions.ReleaseExe, verify:=Verification.FailsILVerify,
                                                 expectedOutput:=<![CDATA[Success
]]>)
            Main_Retarget.VerifyDiagnostics()

            'Check the retargeting symbol information
            Dim sourceAssembly = DirectCast(RetargetReference.Compilation.Assembly, SourceAssemblySymbol)
            Dim SourceModuleReference = sourceAssembly.Modules(0)
            Dim sourceNamespace As SourceNamespaceSymbol = CType(SourceModuleReference.GlobalNamespace.GetNamespace("ClassLibrary1"), SourceNamespaceSymbol)

            Dim retargetingAssembly = Main_Retarget.Compilation.GetReferencedAssemblySymbol(RetargetReference)
            Dim retargetingModule = retargetingAssembly.Modules(0)
            Dim retargetingNamespace As RetargetingNamespaceSymbol = CType(retargetingModule.GlobalNamespace.GetNamespace("ClassLibrary1"), RetargetingNamespaceSymbol)

            'Comparison of Types
            Dim sourceType As NamedTypeSymbol = Nothing
            Dim retargetingType As NamedTypeSymbol = Nothing

            'Generic Types
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Generic_Test_Class")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Generic_Test_Class")
            CheckTypes(sourceType, retargetingType)
            'Check the Type Parameters
            Assert.Equal(sourceType.TypeParameters.Length, retargetingType.TypeParameters.Length)
            For i = 0 To sourceType.TypeParameters.Length - 1
                CheckTypeParameters(sourceType.TypeParameters(i), retargetingType.TypeParameters(i))
            Next

            'Single Constraints
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Generic_Test_Class_Constrained_Interface")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Generic_Test_Class_Constrained_Interface")
            CheckTypes(sourceType, retargetingType)
            'Check the Type Parameters
            Assert.Equal(sourceType.TypeParameters.Length, retargetingType.TypeParameters.Length)
            Assert.Equal(1, sourceType.TypeParameters(0).ConstraintTypes.Length)
            Assert.Equal(sourceType.TypeParameters(0).ConstraintTypes.Length, retargetingType.TypeParameters(0).ConstraintTypes.Length)
            Assert.False(sourceType.TypeParameters(0).HasConstructorConstraint)
            Assert.Equal(sourceType.TypeParameters(0).HasConstructorConstraint, retargetingType.TypeParameters(0).HasConstructorConstraint)
            For i = 0 To sourceType.TypeParameters(0).ConstraintTypes.Length - 1
                CheckTypes(sourceType.TypeParameters(0).ConstraintTypes(i), retargetingType.TypeParameters(0).ConstraintTypes(i))
            Next
            Assert.Equal("TestInterface", CType(sourceType.TypeParameters(0).ConstraintTypes(0), SourceNamedTypeSymbol).Name)

            'Multiple Constraints 
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Generic_Test_Class_Constrained_Multiple")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Generic_Test_Class_Constrained_Multiple")
            CheckTypes(sourceType, retargetingType)
            'Check the Type Parameters
            Assert.Equal(sourceType.TypeParameters.Length, retargetingType.TypeParameters.Length)
            Assert.Equal(1, sourceType.TypeParameters(0).ConstraintTypes.Length)
            Assert.Equal(sourceType.TypeParameters(0).ConstraintTypes.Length, retargetingType.TypeParameters(0).ConstraintTypes.Length)
            Assert.True(sourceType.TypeParameters(0).HasConstructorConstraint)
            Assert.Equal(sourceType.TypeParameters(0).HasConstructorConstraint, retargetingType.TypeParameters(0).HasConstructorConstraint)
            For i = 0 To sourceType.TypeParameters.Length - 1
                CheckTypeParameters(sourceType.TypeParameters(i), retargetingType.TypeParameters(i))
                CheckTypes(sourceType.TypeParameters(0).ConstraintTypes(i), retargetingType.TypeParameters(0).ConstraintTypes(i))
            Next
            Assert.Equal("TestClass", CType(sourceType.TypeParameters(0).ConstraintTypes(0), SourceNamedTypeSymbol).Name)
        End Sub

        <Fact>
        Public Sub Retarget_Scoping()
            'Retargeting symbols occurs for types even if inaccessible 
            'Diagnostics are checked to verify semantic behavior

            Dim source =
               <compilation name="App1">
                   <file name="a.vb"><![CDATA[
        Imports System

        Module Module1

            Sub Main()
                Dim Usage_Class As TestClass = nothing
                Usage_Class.TestProperty = 101
                With Usage_Class
                    ._Field = 102
                    If .ReadOnlyProperty <> 102 Then
                        Console.WriteLine("Problem")
                    End If

                    .WriteOnlyProperty = 103

                    If .ReadOnlyProperty <> 103 Then
                        Console.WriteLine("Problem")
                    End If
                End With


            Dim Usage_Struct As TestStructure = nothing
            Usage_Struct._int_In_Structure  = 1
            Usage_Struct.PrivateField  = False 'This is error 

            End Sub

            Dim x as TestInterface

        End Module

        Class C1
            Implements TestInterface

            Public Function Function_In_Interface(x As Integer) As Boolean Implements TestInterface.Function_In_Interface
                Return True
            End Function

            Public Sub Sub_In_Interface(y As String) Implements TestInterface.Sub_In_Interface
                Console.WriteLine("Success" & y.tostring())
            End Sub
        End Class

        Class C1_Private
            Implements TestInterface

            Public Function Function_In_Interface(x As Integer) As Boolean Implements TestInterface.Function_In_Interface
                Return True
            End Function

            Public Sub Sub_In_Interface(y As String) Implements TestInterface.Sub_In_Interface
                Console.WriteLine("Success" & y.tostring())
            End Sub
        End Class
        ]]>
                   </file>
               </compilation>

            Dim CL1_source =
               <compilation name="C1">
                   <file name="Cl.vb"><![CDATA[
                Public Class TestClass
            'This Class has Properties and Fields of various types etc which 
            'should be accessible from outside
            Public Property TestProperty As Integer

            Public ReadOnly Property ReadOnlyProperty As String
                Get
                    Return _Field
                End Get
            End Property
            Public WriteOnly Property WriteOnlyProperty As String
                Set(value As String)
                    _Field = value
                End Set
            End Property

            Public _Field As String = ""
        End Class

        Friend Structure TestStructure
            Public _int_In_Structure As Integer
            Public Property String_Property As String

            Private PrivateField As Boolean
            Friend Sub FriendMethod()
            End Sub
        End Structure

        Friend Interface TestInterface
            Sub Sub_In_Interface(y As String)
            Function Function_In_Interface(x As Integer) As Boolean
        End Interface

        Class ContainingClass
            Private Class PrivateTestClass
                Public _int_In_Structure As Integer
                Public Property String_Property As String

                Private PrivateField As Boolean
                Friend Sub FriendMethod()
                End Sub
            End Class
        End Class
        ]]>
                   </file>
               </compilation>

            'Check Expected Behavior             
            Dim referenceLibrary_Compilation = DirectCast(CompileAndVerify(CL1_source, options:=TestOptions.ReleaseDll).Compilation, VisualBasicCompilation)

            Dim referenceLibraryMetaData = referenceLibrary_Compilation.ToMetadataReference
            Dim main_NoRetarget = CreateCompilationWithMscorlib40AndVBRuntime(source, additionalRefs:={referenceLibraryMetaData})

            '//Retargetted 
            Dim RetargetReference = RetargetCompilationToV2MsCorlib(referenceLibrary_Compilation)
            Dim Main_Retarget = CreateCompilationWithMscorlib40AndVBRuntime(source, additionalRefs:={RetargetReference}, options:=TestOptions.ReleaseExe)

            'Check the retargeting symbol information
            Dim sourceAssembly = DirectCast(RetargetReference.Compilation.Assembly, SourceAssemblySymbol)
            Dim SourceModuleReference = sourceAssembly.Modules(0)
            Dim sourceNamespace As SourceNamespaceSymbol = CType(SourceModuleReference.GlobalNamespace, SourceNamespaceSymbol)

            Dim retargetingAssembly = Main_Retarget.GetReferencedAssemblySymbol(RetargetReference)
            Dim retargetingModule = retargetingAssembly.Modules(0)
            Dim retargetingNamespace As RetargetingNamespaceSymbol = CType(retargetingModule.GlobalNamespace, RetargetingNamespaceSymbol)

            'Comparison of Types
            Dim sourceType As NamedTypeSymbol = Nothing
            Dim retargetingType As NamedTypeSymbol = Nothing

            Dim sourceMethod As MethodSymbol = Nothing
            Dim retargetingMethod As MethodSymbol = Nothing

            'Public With Different Accessible Member
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestClass")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestClass")
            CheckTypes(sourceType, retargetingType)
            CheckMethods(sourceType.GetMethod(".ctor"), retargetingType.GetMethod(".ctor"))
            CheckFields(sourceType.GetMember("_Field"), retargetingType.GetMember("_Field"))
            CheckProperties(sourceType.GetMember("TestProperty"), retargetingType.GetMember("TestProperty"))
            CheckProperties(sourceType.GetMember("ReadOnlyProperty"), retargetingType.GetMember("ReadOnlyProperty"))
            CheckProperties(sourceType.GetMember("WriteOnlyProperty"), retargetingType.GetMember("WriteOnlyProperty"))

            'Friend Type with different accessible Members
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestStructure")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestStructure")
            CheckTypes(sourceType, retargetingType)
            CheckMethods(sourceType.GetMethod(".ctor"), retargetingType.GetMethod(".ctor"))
            CheckProperties(sourceType.GetMember("String_Property"), retargetingType.GetMember("String_Property"))
            CheckFields(sourceType.GetMember("_int_In_Structure"), retargetingType.GetMember("_int_In_Structure")) 'Public member in Private Type
            CheckFields(sourceType.GetMember("PrivateField"), retargetingType.GetMember("PrivateField")) 'Private member in Private Type
            CheckMethods(sourceType.GetMember("FriendMethod"), retargetingType.GetMember("FriendMethod")) 'Friend member in Private Type

            'Private Type with different accessible Members- Private Type need to be in a containing class to avoid compile error
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("ContainingClass").GetMember(Of NamedTypeSymbol)("PrivateTestClass")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("ContainingClass").GetMember(Of NamedTypeSymbol)("PrivateTestClass")
            CheckTypes(sourceType, retargetingType)
            CheckMethods(sourceType.GetMethod(".ctor"), retargetingType.GetMethod(".ctor"))
            CheckProperties(sourceType.GetMember("String_Property"), retargetingType.GetMember("String_Property"))
            CheckFields(sourceType.GetMember("_int_In_Structure"), retargetingType.GetMember("_int_In_Structure")) 'Public member in Private Type
            CheckFields(sourceType.GetMember("PrivateField"), retargetingType.GetMember("PrivateField")) 'Private member in Private Type
            CheckMethods(sourceType.GetMember("FriendMethod"), retargetingType.GetMember("FriendMethod")) 'Friend member in Private Type

            'Friend Interface Type with no accessors
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestInterface")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestInterface")
            CheckTypes(sourceType, retargetingType)
            CheckMethods(sourceType.GetMember("Sub_In_Interface"), retargetingType.GetMember("Sub_In_Interface"))
            CheckMethods(sourceType.GetMember("Function_In_Interface"), retargetingType.GetMember("Function_In_Interface"))

            'Diagnostics should be the same in Retargeting or Non-Retargeting scenarios as they are these are handled in
            'semantic behavior and symbols need to be available for this to occur irrespective of scope.
            'Expect same diagnostic errors with retargeting
            main_NoRetarget.VerifyDiagnostics(Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestStructure").WithArguments("TestStructure", "Friend"))

            '//Retargetted - should result in No Errors also and same runtime behavior
            Main_Retarget.VerifyDiagnostics(Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestInterface").WithArguments("TestInterface", "Friend"),
    Diagnostic(ERRID.ERR_InaccessibleSymbol2, "TestStructure").WithArguments("TestStructure", "Friend"))

        End Sub

        <Fact>
        Public Sub Retarget_Array_Types()
            'The test involves compilation with/without retargeting and ensuring same behavior at runtime
            'same diagnostics (or lack off) as compile time
            'and verification of the retargeted types and the underlying source type and pertinent items

            Dim source =
               <compilation name="App1">
                   <file name="a.vb"><![CDATA[
Imports System

Module Module1a
    Sub Main()
        ReDim Module1.Array_Test_Class(10)
        ReDim Module1.Array_Test_Structure(11)
        ReDim Module1.Array_Test_Class_Jagged(12)
        ReDim Module1.Array_Test_Structure_Jagged(13)
        ReDim Module1.Array_Test_Class_Multi(10, 20)
        ReDim Module1.Array_Test_Structure_Multi(11, 21, 31)

        'This will be using types which are array types
        Dim Usage_Local_1 = Module1.Array_Test_Class
        Dim Usage_Local_2 = Module1.Array_Test_Structure
        Dim Usage_Local_3 = Module1.Array_Test_Class_Jagged
        Dim Usage_Local_4 = Module1.Array_Test_Structure_Jagged
        Dim Usage_Local_5 = Module1.Array_Test_Class_Multi
        Dim Usage_Local_6 = Module1.Array_Test_Structure_Multi

        'Use the Local types
        Console.WriteLine(Usage_Local_1.GetLength(0))
        Console.WriteLine(Usage_Local_2.GetLength(0))
        Console.WriteLine(Usage_Local_3.GetLength(0))
        Console.WriteLine(Usage_Local_4.GetLength(0))
        Console.WriteLine(Usage_Local_5.GetLength(0))
        Console.WriteLine(Usage_Local_5.GetLength(1))
        Console.WriteLine(Usage_Local_6.GetLength(0))
        Console.WriteLine(Usage_Local_6.GetLength(1))
        Console.WriteLine(Usage_Local_6.GetLength(2))

    End Sub
End Module

]]>
                   </file>
               </compilation>

            Dim CL1_source =
               <compilation name="C1">
                   <file name="Cl.vb"><![CDATA[

Imports System

    Public Class TestClass
        'This Class has Properties and Fields of various types etc which 
        'should be accessible from outside
        Public Property TestProperty As Integer

        Public ReadOnly Property ReadOnlyProperty As String
            Get
                Return _Field
            End Get
        End Property
        Public WriteOnly Property WriteOnlyProperty As String
            Set(value As String)
                _Field = value
            End Set
        End Property

        Public _Field As String = ""
    End Class

    Public Structure TestStructure
        Public _int_In_Structure As Integer
        Public Property String_Property As String
    End Structure

    Public Class TestAttribute
        Inherits Attribute
    End Class


    Public Module Module1
        'These are array types
        Public Array_Test_Class() As TestClass
        Public Array_Test_Structure As TestStructure()


        Public Array_Test_Class_Jagged()() As TestClass
        Public Array_Test_Structure_Jagged()() As TestStructure

        Public Array_Test_Class_Multi(,) As TestClass
        Public Array_Test_Structure_Multi(,,) As TestStructure
    End Module
]]>
                   </file>
               </compilation>

            'Check Expected Behavior - Expect no diagnostic errors with retargeting
            ' All on same FX - should result in No Errors
            Dim referenceLibrary_Compilation = DirectCast(CompileAndVerify(CL1_source, options:=TestOptions.ReleaseDll).Compilation, VisualBasicCompilation)

            Dim referenceLibrary_Metadata = referenceLibrary_Compilation.ToMetadataReference
            Dim main_NoRetarget = CompileAndVerify(source, references:={referenceLibrary_Metadata},
                                                   expectedOutput:=<![CDATA[11
12
13
14
11
21
12
22
32]]>)
            main_NoRetarget.VerifyDiagnostics()

            '//Retargetted - should result in No Errors also and same runtime behavior
            Dim RetargetReference = RetargetCompilationToV2MsCorlib(referenceLibrary_Compilation)
            ' ILVerify: Multiple modules named 'mscorlib' were found
            Dim Main_Retarget = CompileAndVerify(source, references:={RetargetReference}, options:=TestOptions.ReleaseExe, verify:=Verification.FailsILVerify,
                                                 expectedOutput:=<![CDATA[11
12
13
14
11
21
12
22
32]]>)
            Main_Retarget.VerifyDiagnostics()

            'Check the retargeting symbol information
            Dim sourceAssembly = DirectCast(RetargetReference.Compilation.Assembly, SourceAssemblySymbol)
            Dim SourceModuleReference = sourceAssembly.Modules(0)
            Dim sourceNamespace = SourceModuleReference.GlobalNamespace

            Dim retargetingAssembly = Main_Retarget.Compilation.GetReferencedAssemblySymbol(RetargetReference)
            Dim retargetingModule = retargetingAssembly.Modules(0)
            Dim retargetingNamespace = retargetingModule.GlobalNamespace

            'Comparison of Types
            Dim sourceType As NamedTypeSymbol = Nothing
            Dim retargetingType As NamedTypeSymbol = Nothing

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestClass")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestClass")
            CheckTypes(sourceType, retargetingType)

            CheckMethods(sourceType.GetMethod(".ctor"), retargetingType.GetMethod(".ctor"))
            CheckFields(sourceType.GetMember("_Field"), retargetingType.GetMember("_Field"))
            CheckProperties(sourceType.GetMember("TestProperty"), retargetingType.GetMember("TestProperty"))
            CheckProperties(sourceType.GetMember("ReadOnlyProperty"), retargetingType.GetMember("ReadOnlyProperty"))
            CheckProperties(sourceType.GetMember("WriteOnlyProperty"), retargetingType.GetMember("WriteOnlyProperty"))

            'Structure
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestStructure")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestStructure")
            CheckTypes(sourceType, retargetingType)
            CheckFields(sourceType.GetMember("_int_In_Structure"), retargetingType.GetMember("_int_In_Structure"))
            CheckProperties(sourceType.GetMember("String_Property"), retargetingType.GetMember("String_Property"))

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Module1")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Module1")
            CheckTypes(sourceType, retargetingType)

            'Fields of various types Array types
            'Verify the retargeting fields and that they are array types
            'Single, Jagged and Multi Dimensional
            CheckFields(sourceType.GetMember("Array_Test_Class"), retargetingType.GetMember("Array_Test_Class"))
            Assert.True(IsArrayType(CType(sourceType.GetMember("Array_Test_Class"), SourceMemberFieldSymbol).Type))
            Assert.True(IsArrayType(CType(retargetingType.GetMember("Array_Test_Class"), RetargetingFieldSymbol).UnderlyingField.Type))
            CheckFields(sourceType.GetMember("Array_Test_Structure"), retargetingType.GetMember("Array_Test_Structure"))
            Assert.True(IsArrayType(CType(sourceType.GetMember("Array_Test_Structure"), SourceMemberFieldSymbol).Type))
            Assert.True(IsArrayType(CType(retargetingType.GetMember("Array_Test_Class"), RetargetingFieldSymbol).UnderlyingField.Type))
            CheckFields(sourceType.GetMember("Array_Test_Class_Jagged"), retargetingType.GetMember("Array_Test_Class_Jagged"))
            Assert.True(IsArrayType(CType(sourceType.GetMember("Array_Test_Class_Jagged"), SourceMemberFieldSymbol).Type))
            Assert.True(IsArrayType(CType(retargetingType.GetMember("Array_Test_Class_Jagged"), RetargetingFieldSymbol).UnderlyingField.Type))
            CheckFields(sourceType.GetMember("Array_Test_Structure_Jagged"), retargetingType.GetMember("Array_Test_Structure_Jagged"))
            Assert.True(IsArrayType(CType(sourceType.GetMember("Array_Test_Structure_Jagged"), SourceMemberFieldSymbol).Type))
            Assert.True(IsArrayType(CType(retargetingType.GetMember("Array_Test_Structure_Jagged"), RetargetingFieldSymbol).UnderlyingField.Type))
            CheckFields(sourceType.GetMember("Array_Test_Class_Multi"), retargetingType.GetMember("Array_Test_Class_Multi"))
            Assert.True(IsArrayType(CType(sourceType.GetMember("Array_Test_Class_Multi"), SourceMemberFieldSymbol).Type))
            Assert.True(IsArrayType(CType(retargetingType.GetMember("Array_Test_Class_Multi"), RetargetingFieldSymbol).UnderlyingField.Type))
            CheckFields(sourceType.GetMember("Array_Test_Structure_Multi"), retargetingType.GetMember("Array_Test_Structure_Multi"))
            Assert.True(IsArrayType(CType(sourceType.GetMember("Array_Test_Structure_Multi"), SourceMemberFieldSymbol).Type))
            Assert.True(IsArrayType(CType(retargetingType.GetMember("Array_Test_Structure_Multi"), RetargetingFieldSymbol).UnderlyingField.Type))
        End Sub

        Friend Function IsArrayType(t As TypeSymbol) As Boolean
            Dim RetValue As Boolean = False
            Try
                If t.IsArrayType Then RetValue = True
            Catch
            End Try
            Return RetValue
        End Function

        <Fact>
        Public Sub Retarget_Overloads_overrides_Shadows()
            'The test involves compilation with/without retargeting and ensuring same behavior at runtime
            'same diagnostics (or lack off) as compile time
            'and verification of the retargeted types and the underlying source type and pertinent items

            Dim source =
               <compilation name="App1">
                   <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()

        Dim Usage_1 As New Usage_C1
        Dim Usage2 As New TestClass_NotInheriTABLE

        'Shared
        Dim Usage3 As New TestClass_Shared
        TestClass_Shared._Field = "Success"
        TestClass_Shared.TestProperty = 1
        TestClass_Shared.SharedMethods()


        Dim Usage4 As New TestClass_Other
        Usage4.TestProperty = 2
        Usage4.Method()
        Usage4.MethodOverload(1)

        Dim Usage4_Base As New TestClass_Base
        Usage4_Base.TestProperty = 3
        Usage4_Base.Method()
        Usage4_Base.MethodOverload()

    End Sub

    Class Usage_C1
        Inherits TestClass_MustInherit
    End Class

#If CompileError Then
    Class Usage_C1_error
        Inherits TestClass_NotInheriTABLE
    End Class
#End If
End Module

]]>
                   </file>
               </compilation>

            Dim CL1_source =
               <compilation name="C1">
                   <file name="Cl.vb"><![CDATA[
Imports System

Public MustInherit Class TestClass_MustInherit
    Public Property TestProperty As Integer
    Public _Field As String = ""
End Class


Public NotInheritable Class TestClass_NotInheriTABLE
    Public Property TestProperty As Integer
    Public _Field As String = ""
End Class


Public Class TestClass_Shared
    Public Shared Property TestProperty As Integer
    Public Shared _Field As String = ""
    Public Shared Sub SharedMethods()
        Console.WriteLine("Sharedmethod")
    End Sub
End Class


Public Class TestClass_Base
    Public Property TestProperty As Integer

    Public Overridable Sub Method()
        Console.WriteLine("Method(Base)")
    End Sub
    Public Sub MethodOverload()
        Console.WriteLine("MethodOverload(Base)")
    End Sub
End Class

Public Class TestClass_Other
    Inherits TestClass_Base
    Public Shadows Property TestProperty As Integer

    Public Overrides Sub method()
        Console.WriteLine("method(Other)")
    End Sub

    Public Overloads Sub MethodOverload(x As Integer)
        Console.WriteLine("MethodOverload(Other)")
    End Sub
End Class
]]>
                   </file>
               </compilation>

            'Check Expected Behavior - Expect no diagnostic errors with retargeting
            ' All on same FX - should result in No Errors
            Dim referenceLibrary_Compilation = DirectCast(CompileAndVerify(CL1_source, options:=TestOptions.ReleaseDll).Compilation, VisualBasicCompilation)

            Dim referenceLibrary_Metadata = referenceLibrary_Compilation.ToMetadataReference
            Dim main_NoRetarget = CompileAndVerify(source, references:={referenceLibrary_Metadata},
                                                   expectedOutput:=<![CDATA[Sharedmethod
method(Other)
MethodOverload(Other)
Method(Base)
MethodOverload(Base)
]]>)
            main_NoRetarget.VerifyDiagnostics()

            '//Retargetted - should result in No Errors also same runtime behavior
            Dim RetargetReference = RetargetCompilationToV2MsCorlib(referenceLibrary_Compilation)
            ' ILVerify: Multiple modules named 'mscorlib' were found
            Dim Main_Retarget = CompileAndVerify(source, references:={RetargetReference}, options:=TestOptions.ReleaseExe, verify:=Verification.FailsILVerify,
                                                 expectedOutput:=<![CDATA[Sharedmethod
method(Other)
MethodOverload(Other)
Method(Base)
MethodOverload(Base)
]]>)
            Main_Retarget.VerifyDiagnostics()

            'Check Retargeting Symbols
            Dim sourceAssembly = DirectCast(RetargetReference.Compilation.Assembly, SourceAssemblySymbol)
            Dim SourceModuleReference = sourceAssembly.Modules(0)
            Dim sourceNamespace = SourceModuleReference.GlobalNamespace

            Dim retargetingAssembly = Main_Retarget.Compilation.GetReferencedAssemblySymbol(RetargetReference)
            Dim retargetingModule = retargetingAssembly.Modules(0)
            Dim retargetingNamespace = retargetingModule.GlobalNamespace

            Dim sourceType As NamedTypeSymbol
            Dim retargetingType As NamedTypeSymbol

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestClass_MustInherit")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestClass_MustInherit")
            CheckTypes(sourceType, retargetingType)
            CheckFields(sourceType.GetMember("_Field"), retargetingType.GetMember("_Field"))
            CheckProperties(sourceType.GetMember("TestProperty"), retargetingType.GetMember("TestProperty"))
            Assert.True(sourceType.IsMustInherit)
            Assert.Equal(sourceType.IsMustInherit, retargetingType.IsMustInherit)

            'NotInheritable
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestClass_NotInheriTABLE")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestClass_NotInheriTABLE")
            CheckTypes(sourceType, retargetingType)
            Assert.True(sourceType.IsNotInheritable)
            Assert.Equal(sourceType.IsNotInheritable, retargetingType.IsNotInheritable)

            'Shared
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestClass_Shared")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestClass_Shared")
            CheckTypes(sourceType, retargetingType)
            CheckMethods(sourceType.GetMember("SharedMethods"), retargetingType.GetMember("SharedMethods"))
            Assert.True(sourceType.GetMember("SharedMethods").IsShared)
            Assert.Equal(sourceType.GetMember("SharedMethods").IsShared, retargetingType.GetMember("SharedMethods").IsShared)
            CheckFields(sourceType.GetMember("_Field"), retargetingType.GetMember("_Field"))
            Assert.True(sourceType.GetMember("_Field").IsShared)
            Assert.Equal(sourceType.GetMember("_Field").IsShared, retargetingType.GetMember("_Field").IsShared)
            CheckProperties(sourceType.GetMember("TestProperty"), retargetingType.GetMember("TestProperty"))
            Assert.True(sourceType.GetMember("TestProperty").IsShared)
            Assert.Equal(sourceType.GetMember("TestProperty").IsShared, retargetingType.GetMember("TestProperty").IsShared)

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestClass_Base")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestClass_Base")
            CheckTypes(sourceType, retargetingType)
            Assert.True(sourceType.GetMember("Method").IsOverridable)
            Assert.Equal(sourceType.GetMember("Method").IsOverridable, retargetingType.GetMember("Method").IsOverridable)
            CheckMethods(sourceType.GetMember("Method"), retargetingType.GetMember("Method"))
            Assert.True(sourceType.GetMember("MethodOverload").IsOverloadable)
            Assert.Equal(sourceType.GetMember("MethodOverload").IsOverloadable, retargetingType.GetMember("MethodOverload").IsOverloadable)
            CheckMethods(sourceType.GetMember("MethodOverload"), retargetingType.GetMember("MethodOverload"))

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("TestClass_Other")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("TestClass_Other")
            CheckTypes(sourceType, retargetingType)
            Assert.False(sourceType.GetMember("Method").IsOverridable)
            Assert.Equal(sourceType.GetMember("Method").IsOverridable, retargetingType.GetMember("Method").IsOverridable)
            Assert.Equal(sourceType.GetMember("Method").IsOverrides, retargetingType.GetMember("Method").IsOverrides)
            CheckMethods(sourceType.GetMember("Method"), retargetingType.GetMember("Method"))

            Assert.False(sourceType.GetMember("MethodOverload").IsOverridable)
            Assert.Equal(sourceType.GetMember("MethodOverload").IsOverridable, retargetingType.GetMember("MethodOverload").IsOverridable)
            Assert.True(sourceType.GetMember("MethodOverload").IsOverloadable)
            Assert.Equal(sourceType.GetMember("MethodOverload").IsOverloadable, retargetingType.GetMember("MethodOverload").IsOverloadable)

            Assert.Equal(sourceType.GetMember("MethodOverload").IsOverloads, retargetingType.GetMember("MethodOverload").IsOverloads)
            CheckMethods(sourceType.GetMember("MethodOverload"), retargetingType.GetMember("MethodOverload"))
            CheckProperties(sourceType.GetMember("TestProperty"), retargetingType.GetMember("TestProperty"))
            Assert.True(sourceType.GetMember("TestProperty").IsShadows)

            'Check
            Assert.True(sourceType.GetMember("TestProperty").ShadowsExplicitly)
            Assert.False(retargetingType.GetMember("TestProperty").ShadowsExplicitly)
            Assert.True(CType(retargetingType.GetMember("TestProperty"), RetargetingPropertySymbol).UnderlyingProperty.ShadowsExplicitly)

        End Sub

        <Fact>
        Public Sub Retarget_Events()
            'The test involves compilation with/without retargeting and ensuring same behavior at runtime
            'same diagnostics (or lack off) as compile time
            'and verification of the retargeted types and the underlying source type and pertinent items

            Dim source =
               <compilation name="App1">
                   <file name="a.vb"><![CDATA[
Imports System

Public Module Module1
    WithEvents x As New EventClass
    Dim noevents As New EventClass

    Dim StrResult = ""

    Sub Main()
        StrResult = ""
        Module_TestEventsWithHandlers.Caller_RaiseTestEvents()        
        If Module_TestEventsWithHandlers.EventCalled = "CalledCalled" Then
            Console.WriteLine("Success")
        Else
            Console.WriteLine("Fail")
        End If

        StrResult = ""
        Module_TestEventsWithAddHandler.Caller_RaiseTestEvents()
        If Module_TestEventsWithAddHandler.EventCalled = "Called" Then
            Console.WriteLine("Success")
        Else
            Console.WriteLine("Fail")
        End If

        'Using Withevents - events may be raised by no handlers hooked up       
        StrResult = ""
        Module_TestEventsWithAddHandler.EventCalled = ""
        If Module_TestEventsWithHandlers.EventCalled = "CalledCalled" Then
            Console.WriteLine("Success")
        Else
            Console.WriteLine("Fail")
        End If


        x.RaiseEvents()
        If StrResult = "Called c" Then
            Console.WriteLine("Success")
        Else
            Console.WriteLine("Fail")
        End If


        AddHandler x.XEvent, AddressOf a
        AddHandler x.YEvent, AddressOf b
        x.RaiseEvents()
        If StrResult = "Called cCalled cCalled aCalled b" Then
            Console.WriteLine("Success")
        Else
            Console.WriteLine("Fail")
        End If


        'Using Class 
        StrResult = ""
        Dim U1 As New EventClass
        AddHandler U1.XEvent, AddressOf a
        RemoveHandler U1.XEvent, AddressOf a
        AddHandler U1.YEvent, AddressOf a
        U1.RaiseEvents() ' This will call method which will raise events
        If StrResult = "Called a" Then
            Console.WriteLine("Success")
        Else
            Console.WriteLine("Fail")
        End If

        StrResult = ""
        x.RaiseEvents()

        StrResult = ""
        noevents.RaiseEvents()
        If StrResult = "" Then
            Console.WriteLine("Success")
        Else
            Console.WriteLine("Fail")
        End If

        StrResult = ""
        AddHandler noevents.XEvent, AddressOf b
        RemoveHandler noevents.XEvent, AddressOf b
        AddHandler noevents.YEvent, AddressOf b
        noevents.RaiseEvents()
        If StrResult = "Called b" Then
            Console.WriteLine("Success")
        Else
            Console.WriteLine("Fail")
        End If
    End Sub

    Private Sub a()
        StrResult &= "Called a"
    End Sub

    Private Sub b()
        StrResult &= "Called b"
    End Sub


    Private Sub c() Handles x.XEvent
        StrResult &= "Called c"
    End Sub
End Module
]]>
                   </file>
               </compilation>

            Dim CL1_source =
               <compilation name="C1">
                   <file name="Cl.vb"><![CDATA[
' Declare a WithEvents variable.
Public Module Module_TestEventsWithHandlers
    Public Property EventCalled As String = ""

    Dim WithEvents EClass As New EventClass
    ' Call the method that raises the object's events.
    Sub Caller_RaiseTestEvents()
        EventCalled = ""
        EClass.RaiseEvents()
    End Sub

    ' Declare an event handler that handles multiple events.
    Sub EClass_EventHandler() Handles EClass.XEvent, EClass.YEvent
        EventCalled &= "Called"
    End Sub
End Module


' Declare a WithEvents variable.
Public Module Module_TestEventsWithAddHandler
    Public Property EventCalled As String = ""
    Dim WithEvents EClass As New EventClass

    ' Call the method that raises the object's events.
    Sub Caller_RaiseTestEvents()
        AddHandler EClass.XEvent, AddressOf EClass_EventHandler
        AddHandler EClass.YEvent, AddressOf EClass_EventHandler
        RemoveHandler EClass.YEvent, AddressOf EClass_EventHandler
        EClass.RaiseEvents()
    End Sub

    ' Declare an event handler that handles multiple events.
    Sub EClass_EventHandler()
        EventCalled &= "Called"
    End Sub
End Module


Public Class EventClass
    Public Event XEvent()
    Public Event YEvent()

    ' RaiseEvents raises both events.
    Public Sub RaiseEvents()
        RaiseEvent XEvent()
        RaiseEvent YEvent()
    End Sub
End Class
]]>
                   </file>
               </compilation>

            'Check Expected Behavior - Expect no diagnostic errors with retargeting
            ' All on same FX - should result in No Errors
            Dim referenceLibrary_Compilation = DirectCast(CompileAndVerify(CL1_source, options:=TestOptions.ReleaseDll).Compilation, VisualBasicCompilation)

            Dim referenceLibrary_Metadata = referenceLibrary_Compilation.ToMetadataReference
            Dim main_NoRetarget = CompileAndVerify(source, references:={referenceLibrary_Metadata},
                                                   expectedOutput:=<![CDATA[Success
Success
Success
Success
Success
Success
Success
Success]]>)
            main_NoRetarget.VerifyDiagnostics()

            '//Retargetted - should result in No Errors also and same runtime behavior
            Dim RetargetReference = RetargetCompilationToV2MsCorlib(referenceLibrary_Compilation)
            ' ILVerify: Multiple modules named 'mscorlib' were found
            Dim Main_Retarget = CompileAndVerify(source, references:={RetargetReference}, options:=TestOptions.ReleaseExe, verify:=Verification.FailsILVerify,
                                                 expectedOutput:=<![CDATA[Success
Success
Success
Success
Success
Success
Success
Success]]>)
            Main_Retarget.VerifyDiagnostics()

            'Check the retargeting symbol information
            Dim sourceAssembly As SourceAssemblySymbol = DirectCast(RetargetReference.Compilation.Assembly, SourceAssemblySymbol)
            Dim SourceModuleReference As ModuleSymbol = sourceAssembly.Modules(0)
            Dim sourceNamespace = SourceModuleReference.GlobalNamespace

            Dim retargetingAssembly = Main_Retarget.Compilation.GetReferencedAssemblySymbol(RetargetReference)
            Dim retargetingModule = retargetingAssembly.Modules(0)
            Dim retargetingNamespace = retargetingModule.GlobalNamespace

            'Comparison of Types
            Dim sourceType As NamedTypeSymbol = Nothing
            Dim retargetingType As NamedTypeSymbol = Nothing

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("EventClass")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("EventClass")
            CheckTypes(sourceType, retargetingType)

            CheckTypes(CType(sourceType.GetMember("xEvent"), SourceEventSymbol).Type, CType(retargetingType.GetMember("xEvent"), RetargetingEventSymbol).Type)
            CheckEvent(sourceType.GetMember("xEvent"), retargetingType.GetMember("xEvent"))
            CheckEvent(sourceType.GetMember("yEvent"), retargetingType.GetMember("yEvent"))

            Dim SourceEventItem = CType(retargetingType.GetMember("xEvent"), EventSymbol)
            Dim RetargetEventItem = CType(retargetingType.GetMember("xEvent"), RetargetingEventSymbol).UnderlyingEvent
            Assert.NotNull(RetargetEventItem.AddMethod)
            Assert.NotNull(RetargetEventItem.RemoveMethod)
            Assert.Null(RetargetEventItem.RaiseMethod)
            Assert.Null(SourceEventItem.RaiseMethod)
            Assert.Equal(SymbolKind.Event, RetargetEventItem.Kind)
            Assert.Equal(SourceEventItem.AddMethod.Name, RetargetEventItem.AddMethod.Name)
            Assert.Equal(SourceEventItem.RemoveMethod.Name, RetargetEventItem.RemoveMethod.Name)

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Module_TestEventsWithAddHandler")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Module_TestEventsWithAddHandler")
            CheckTypes(sourceType, retargetingType)
            CheckProperties(sourceType.GetMember("EClass"), retargetingType.GetMember("EClass"))
            CheckFields(sourceType.GetMember("_EClass"), retargetingType.GetMember("_EClass"))
            Assert.True(sourceType.GetMember("EClass").IsWithEventsProperty)
            Assert.Equal(sourceType.GetMember("EClass").IsWithEventsProperty, retargetingType.GetMember("EClass").IsWithEventsProperty)
        End Sub

        <Fact>
        Public Sub Retarget_Attributes()
            'The test involves compilation with/without retargeting and ensuring same behavior at runtime
            'same diagnostics (or lack off) as compile time
            'and verification of the retargeted types and the underlying source type and pertinent items

            Dim source =
               <compilation name="App1">
                   <file name="a.vb"><![CDATA[
Imports System

Module Module1   
    Sub Main()
        Dim x0 As New UsageClass
        Dim x1 As New UsageClass_1
        Dim x2 As New UsageClass_2

        Usage_All.Prop1 = 1
        Usage_All._Field = "test"
        Usage_All.Method1()

        Usage_All.Prop1 = Test.Item
    End Sub
End Module

'Attribute Usage Class or Structure
<ClassOrStructureAttribute()>
<Attribute_Class>
Class UsageClass
End Class

<ClassOrStructureAttribute()>
Structure UsageStructure
End Structure

'Multiple Attributes
<Attribute_All, Attribute_Class>
Class UsageClass_1

End Class

<Attribute_All, Attribute_Class> <ClassOrStructure>
Class UsageClass_2
End Class

<Attribute_All>
Module Usage_All

    <Attribute_All>
    Property Prop1 As Integer

    <Attribute_All>
    Public _Field As String = ""

    <Attribute_All>
    Sub Method1()
        Console.WriteLine(Prop1.ToString)
        Console.WriteLine(_Field)
    End Sub
End Module

<Attribute_InheritedFalse>
Class InheritedFalse

End Class

<Attribute_Enum>
Public Enum Test
    Item
    Item2
End Enum
]]>
                   </file>
               </compilation>

            Dim CL1_source =
               <compilation name="C1">
                   <file name="Cl.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.Class, AllowMultiple:=False)> _
Public Class Attribute_Class
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.Method, Inherited:=True, AllowMultiple:=False)> _
Public Class Attribute_method
    Inherits Attribute
End Class


<AttributeUsage(AttributeTargets.All, Inherited:=True, AllowMultiple:=False)> _
Public Class Attribute_All
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.Enum, Inherited:=True, AllowMultiple:=False)> _
Public Class Attribute_Enum
    Inherits Attribute

End Class

<AttributeUsage(AttributeTargets.All, Inherited:=False, AllowMultiple:=True)> _
Public Class Attribute_InheritedFalse
    Inherits Attribute

End Class


<AttributeUsage(AttributeTargets.Class Or AttributeTargets.Struct)>
Public Class ClassOrStructureAttribute
    Inherits Attribute

End Class

<AttributeUsage(AttributeTargets.Class)><Attribute_Class>
Public Class Class_Multiple
    Inherits Attribute

End Class
]]>
                   </file>
               </compilation>

            'Check Expected Behavior - Expect no diagnostic errors with retargeting
            ' All on same FX - should result in No Errors
            Dim referenceLibrary_Compilation = DirectCast(CompileAndVerify(CL1_source, options:=TestOptions.ReleaseDll).Compilation, VisualBasicCompilation)

            Dim referenceLibrary_Metadata = referenceLibrary_Compilation.ToMetadataReference
            Dim main_NoRetarget = CompileAndVerify(source, references:={referenceLibrary_Metadata},
                                                   expectedOutput:=<![CDATA[1
test
]]>)
            main_NoRetarget.VerifyDiagnostics()

            '//Retargetted - should result in No Errors also
            Dim RetargetReference = RetargetCompilationToV2MsCorlib(referenceLibrary_Compilation)
            ' ILVerify: Multiple modules named 'mscorlib' were found
            Dim Main_Retarget = CompileAndVerify(source, references:={RetargetReference}, options:=TestOptions.ReleaseExe, verify:=Verification.FailsILVerify,
                                                 expectedOutput:=<![CDATA[1
test
]]>)
            Main_Retarget.VerifyDiagnostics()

            Dim sourceAssembly = DirectCast(RetargetReference.Compilation.Assembly, SourceAssemblySymbol)
            Dim SourceModuleReference = sourceAssembly.Modules(0)
            Dim sourceNamespace = SourceModuleReference.GlobalNamespace

            Dim retargetingAssembly = Main_Retarget.Compilation.GetReferencedAssemblySymbol(RetargetReference)
            Dim retargetingModule = retargetingAssembly.Modules(0)
            Dim retargetingNamespace = retargetingModule.GlobalNamespace

            'Comparison of Types
            Dim sourceType As NamedTypeSymbol = Nothing
            Dim retargetingType As NamedTypeSymbol = Nothing

            ''Check Attributes
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Attribute_Class")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Attribute_Class")
            CheckTypes(sourceType, retargetingType)

            Assert.Equal(1, retargetingType.GetAttributes.Length)
            Assert.Equal(sourceType.GetAttributes.Length, retargetingType.GetAttributes.Length)

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Attribute_method")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Attribute_method")
            Assert.Equal(1, retargetingType.GetAttributes.Length)
            Assert.Equal(sourceType.GetAttributes.Length, retargetingType.GetAttributes.Length)
            CheckTypes(sourceType, retargetingType)

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("ClassOrStructureAttribute")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("ClassOrStructureAttribute")
            Assert.Equal(1, retargetingType.GetAttributes.Length)
            Assert.Equal(sourceType.GetAttributes.Length, retargetingType.GetAttributes.Length)
            CheckTypes(sourceType, retargetingType)

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Class_Multiple")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Class_Multiple")
            Assert.Equal(2, retargetingType.GetAttributes.Length)
            Assert.Equal(sourceType.GetAttributes.Length, retargetingType.GetAttributes.Length)
            CheckTypes(sourceType, retargetingType)

        End Sub

        <Fact>
        Public Sub RetargetTest_NoChangeInDiagnostics_CleanCompile()
            'This test should also result in clean compilation after retargeting
            Dim sourceLibV1 =
<compilation name="Lib">
    <file><![CDATA[
Imports System

Namespace ClassLibrary1
Public Class TestClass
    'This Class has Properties and Fields of various types etc which 
    'should be accessible from outside
    Public Property TestProperty As Integer

    Public ReadOnly Property ReadOnlyProperty As String
        Get
            Return _Field
        End Get
    End Property
    Public WriteOnly Property WriteOnlyProperty As String
        Set(value As String)
            _Field = value
        End Set
    End Property

    Public _Field As String = ""
End Class

Public Structure TestStructure
    Dim _int_In_Structure As Integer

    Public Property String_Property As String
End Structure

Public Interface TestInterface
    Sub Sub_In_Interface(y As String)
    Function Function_In_Interface(x As Integer) As Boolean
End Interface

Public Class TestAttribute
    Inherits Attribute
End Class

<Test>
Public Class Class_With_Attribute

End Class

<Test>
Public Structure Structure_With_Attribute
    Dim int_In_Structure As Integer
End Structure

'Test Enum
Public Enum Test_Enum
    Item1
    Item3 = 3
End Enum


Public Module Test_Module
    Public Const Test_Constant As Integer = 101

    Public Delegate Sub DelSub(x As Integer)
    Public Delegate Function DelFunction(x As String) As Boolean
End Module



'Generic Types
Public Class Generic_Test_Class(Of t)
    Public Function xyz() As Integer
        Return True
    End Function
End Class

'Constrained
Public Class Generic_Test_Class_Constrained_Specific(Of t As TestClass)
    Public Function xyz() As Integer
        Return True
    End Function
End Class

Public Class Generic_Test_Class_Constrained_Interface(Of t As TestInterface)
    Public Function xyz() As Integer
        Return True
    End Function
End Class

Public Class Generic_Test_Class_Constrained_New(Of t As New)
    Public Function xyz() As Integer
        Return True
    End Function
End Class

Public Class Generic_Test_Class_Constrained_Multiple(Of t As {New, TestClass})
    Public Function xyz() As Integer
        Return True
    End Function
End Class


Public Class NestedTypeBase

End Class

Public Class NestedTypeDerived
    Inherits NestedTypeBase
End Class

Namespace TestNS
    Public Class Class_In_NS
        Public Property SuccessProperty As Boolean = True
    End Class
End Namespace
End Namespace
]]>
    </file>
</compilation>

            Dim sourceMain =
<compilation name="Main">
    <file><![CDATA[
Imports System
Imports ClassLibrary1.TestNS


Module Module1

    Sub Main()
        Dim Usage_Class As New NewClass
        Usage_Class.TestProperty = 101
        With Usage_Class
            ._Field = 102
            If .ReadOnlyProperty <> 102 Then
                Console.WriteLine("Problem")
            End If

            .WriteOnlyProperty = 103

            If .ReadOnlyProperty <> 103 Then
                Console.WriteLine("Problem")
            End If
        End With
    End Sub
End Module

'We need to use each of the items in the Class Library

Class NewClass
    Inherits ClassLibrary1.TestClass
End Class

<ClassLibrary1.Test>
Class NewClass_Using_Attribute

End Class


Class NewClass_ImplementingInterface
    Implements ClassLibrary1.TestInterface

    Public Sub Sub_In_Interface(y As String) Implements ClassLibrary1.TestInterface.Sub_In_Interface

    End Sub

    Public Function Function_In_Interface(x As Integer) As Boolean Implements ClassLibrary1.TestInterface.Function_In_Interface
        Return True
    End Function
End Class


Class New_InheritedAttribute
    Inherits ClassLibrary1.TestAttribute
End Class

Module Module_Usage
    Sub TestMethod()
        Dim x As New ClassLibrary1.Class_With_Attribute

        'Structure
        Dim y As New ClassLibrary1.TestStructure
        y._int_In_Structure = 102

        If y._int_In_Structure <> 102 Then
            Console.WriteLine("Problem")
        End If

        y.String_Property = "Success"
        Console.WriteLine("Property:" & y.String_Property)
    End Sub

    Sub test_Constraints()
        Dim t1 As New ClassLibrary1.Generic_Test_Class(Of NewClass)
        Dim t2 As New ClassLibrary1.Generic_Test_Class_Constrained_Interface(Of ClassLibrary1.TestInterface)
        Dim t3 As New ClassLibrary1.Generic_Test_Class_Constrained_New(Of NewClass)
        Dim t3_1 As New ClassLibrary1.Generic_Test_Class_Constrained_New(Of ClassLibrary1.Generic_Test_Class(Of Integer))
        Dim t4 As New ClassLibrary1.Generic_Test_Class_Constrained_Specific(Of ClassLibrary1.TestClass)
        Dim t4_1 As New ClassLibrary1.Generic_Test_Class_Constrained_Specific(Of NewClass)
        Dim t5 As New ClassLibrary1.Generic_Test_Class_Constrained_Multiple(Of ClassLibrary1.TestClass)

#If CompErrorTest Then
        Dim f_t2 As New ClassLibrary1.Generic_Test_Class_Constrained_Interface(Of NewClass)
        Dim f_t3 As New ClassLibrary1.Generic_Test_Class_Constrained_New(Of ClassLibrary1.TestInterface)
        Dim f_t4 As New ClassLibrary1.Generic_Test_Class_Constrained_Specific(Of Integer)
        Dim f_t5 As New ClassLibrary1.Generic_Test_Class_Constrained_Multiple(Of ABC)
#End If
    End Sub

    Sub Usage_Attribute()
        Dim x As ClassLibrary1.Structure_With_Attribute
        x.int_In_Structure = 1
    End Sub

    Sub TestEnum()
        Dim EnumValue = ClassLibrary1.Test_Enum.Item1
        EnumValue = ClassLibrary1.Test_Enum.Item3

        If ClassLibrary1.Test_Module.Test_Constant <> 101 Then
            Console.WriteLine("Problem")
        End If

        Dim DelSub As ClassLibrary1.Test_Module.DelSub = AddressOf DelTestMethod
        DelSub.Invoke(1)

        Dim DelFunc As ClassLibrary1.Test_Module.DelFunction = AddressOf DelTestMethod
        Dim xresult = DelFunc("Test")
    End Sub

    Sub DelTestMethod(x As Integer)
        Console.WriteLine("Success")
    End Sub

    Function DelTestMethod(x As String) As Boolean
        If x = "Test" Then
            Return True
        Else
            Return False
        End If
    End Function


    Sub NestedTypes()
        Dim xb As New ClassLibrary1.NestedTypeBase
        Dim xd As New ClassLibrary1.NestedTypeDerived

        If TypeOf (xd) Is ClassLibrary1.NestedTypeDerived Then
            Console.WriteLine("Success")
        End If
    End Sub

    Sub TestNs()
        Dim x As New Class_In_NS
        If x.SuccessProperty = True Then
            Console.WriteLine("Success")
        End If
    End Sub
End Module

Class ABC

End Class
]]>
    </file>
</compilation>
            ''//All on same FX - should result in No Errors
            Dim referenceLibrary_Compilation = DirectCast(CompileAndVerify(sourceLibV1, options:=TestOptions.ReleaseDll).Compilation, VisualBasicCompilation)
            Dim main_NoRetarget = CompileAndVerify(sourceMain, references:={referenceLibrary_Compilation.ToMetadataReference})
            main_NoRetarget.VerifyDiagnostics()

            ''//Retargetted - should result in No Errors also
            Dim RetargetReference = RetargetCompilationToV2MsCorlib(referenceLibrary_Compilation)
            ' ILVerify: Multiple modules named 'mscorlib' were found
            Dim Main_Retarget = CompileAndVerify(sourceMain, references:={RetargetReference}, options:=TestOptions.ReleaseExe, verify:=Verification.FailsILVerify)
            main_NoRetarget.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub RetargetTest_NoChangeInDiagnostics_Errors()
            ' Ensure that same errors occur in retargeting for constraint compilation errors
            Dim sourceLibV1 =
<compilation name="Lib">
    <file><![CDATA[
Imports System

Namespace ClassLibrary1
Public Class TestClass
    'This Class has Properties and Fields of various types etc which 
    'should be accessible from outside
    Public Property TestProperty As Integer

    Public ReadOnly Property ReadOnlyProperty As String
        Get
            Return _Field
        End Get
    End Property
    Public WriteOnly Property WriteOnlyProperty As String
        Set(value As String)
            _Field = value
        End Set
    End Property

    Public _Field As String = ""
End Class

Public Structure TestStructure
    Dim _int_In_Structure As Integer

    Public Property String_Property As String
End Structure

Public Interface TestInterface
    Sub Sub_In_Interface(y As String)
    Function Function_In_Interface(x As Integer) As Boolean
End Interface

Public Class TestAttribute
    Inherits Attribute
End Class

<Test>
Public Class Class_With_Attribute

End Class

<Test>
Public Structure Structure_With_Attribute
    Dim int_In_Structure As Integer
End Structure

'Test Enum
Public Enum Test_Enum
    Item1
    Item3 = 3
End Enum


Public Module Test_Module
    Public Const Test_Constant As Integer = 101

    Public Delegate Sub DelSub(x As Integer)
    Public Delegate Function DelFunction(x As String) As Boolean
End Module



'Generic Types
Public Class Generic_Test_Class(Of t)
    Public Function xyz() As Integer
        Return True
    End Function
End Class

'Constrained
Public Class Generic_Test_Class_Constrained_Specific(Of t As TestClass)
    Public Function xyz() As Integer
        Return True
    End Function
End Class

Public Class Generic_Test_Class_Constrained_Interface(Of t As TestInterface)
    Public Function xyz() As Integer
        Return True
    End Function
End Class

Public Class Generic_Test_Class_Constrained_New(Of t As New)
    Public Function xyz() As Integer
        Return True
    End Function
End Class

Public Class Generic_Test_Class_Constrained_Multiple(Of t As {New, TestClass})
    Public Function xyz() As Integer
        Return True
    End Function
End Class


Public Class NestedTypeBase

End Class

Public Class NestedTypeDerived
    Inherits NestedTypeBase
End Class

Namespace TestNS
    Public Class Class_In_NS
        Public Property SuccessProperty As Boolean = True
    End Class
End Namespace
End Namespace
]]>
    </file>
</compilation>

            Dim sourceMain =
<compilation name="Main">
    <file><![CDATA[
Imports System
Imports ClassLibrary1
Imports ClassLibrary1.TestNS

#Const CompErrorTest = True

Module Module1

    Sub Main()
        Dim Usage_Class As New NewClass
        Usage_Class.TestProperty = 101
        With Usage_Class
            ._Field = 102
            If .ReadOnlyProperty <> 102 Then
                Console.WriteLine("Problem")
            End If

            .WriteOnlyProperty = 103

            If .ReadOnlyProperty <> 103 Then
                Console.WriteLine("Problem")
            End If
        End With
    End Sub
End Module

'We need to use each of the items in the Class Library

Class NewClass
    Inherits ClassLibrary1.TestClass
End Class

<ClassLibrary1.Test>
Class NewClass_Using_Attribute

End Class


Class NewClass_ImplementingInterface
    Implements ClassLibrary1.TestInterface

    Public Sub Sub_In_Interface(y As String) Implements ClassLibrary1.TestInterface.Sub_In_Interface

    End Sub

    Public Function Function_In_Interface(x As Integer) As Boolean Implements ClassLibrary1.TestInterface.Function_In_Interface
        Return True
    End Function
End Class


Class New_InheritedAttribute
    Inherits ClassLibrary1.TestAttribute
End Class

Module Module_Usage
    Sub TestMethod()
        Dim x As New ClassLibrary1.Class_With_Attribute

        'Structure
        Dim y As New ClassLibrary1.TestStructure
        y._int_In_Structure = 102

        If y._int_In_Structure <> 102 Then
            Console.WriteLine("Problem")
        End If

        y.String_Property = "Success"
        Console.WriteLine("Property:" & y.String_Property)
    End Sub

    Sub test_Constraints()
        Dim t1 As New ClassLibrary1.Generic_Test_Class(Of NewClass)
        Dim t2 As New ClassLibrary1.Generic_Test_Class_Constrained_Interface(Of ClassLibrary1.TestInterface)
        Dim t3 As New ClassLibrary1.Generic_Test_Class_Constrained_New(Of NewClass)
        Dim t3_1 As New ClassLibrary1.Generic_Test_Class_Constrained_New(Of ClassLibrary1.Generic_Test_Class(Of Integer))
        Dim t4 As New ClassLibrary1.Generic_Test_Class_Constrained_Specific(Of ClassLibrary1.TestClass)
        Dim t4_1 As New ClassLibrary1.Generic_Test_Class_Constrained_Specific(Of NewClass)
        Dim t5 As New ClassLibrary1.Generic_Test_Class_Constrained_Multiple(Of ClassLibrary1.TestClass)

#If CompErrorTest Then
        Dim f_t2 As New ClassLibrary1.Generic_Test_Class_Constrained_Interface(Of NewClass)
        Dim f_t3 As New ClassLibrary1.Generic_Test_Class_Constrained_New(Of ClassLibrary1.TestInterface)
        Dim f_t4 As New ClassLibrary1.Generic_Test_Class_Constrained_Specific(Of Integer)
        Dim f_t5 As New ClassLibrary1.Generic_Test_Class_Constrained_Multiple(Of ABC)
#End If
    End Sub

    Sub Usage_Attribute()
        Dim x As ClassLibrary1.Structure_With_Attribute
        x.int_In_Structure = 1
    End Sub

    Sub TestEnum()
        Dim EnumValue = ClassLibrary1.Test_Enum.Item1
        EnumValue = ClassLibrary1.Test_Enum.Item3

        If ClassLibrary1.Test_Module.Test_Constant <> 101 Then
            Console.WriteLine("Problem")
        End If

        Dim DelSub As ClassLibrary1.Test_Module.DelSub = AddressOf DelTestMethod
        DelSub.Invoke(1)

        Dim DelFunc As ClassLibrary1.Test_Module.DelFunction = AddressOf DelTestMethod
        Dim xresult = DelFunc("Test")
    End Sub

    Sub DelTestMethod(x As Integer)
        Console.WriteLine("Success")
    End Sub

    Function DelTestMethod(x As String) As Boolean
        If x = "Test" Then
            Return True
        Else
            Return False
        End If
    End Function


    Sub NestedTypes()
        Dim xb As New ClassLibrary1.NestedTypeBase
        Dim xd As New ClassLibrary1.NestedTypeDerived

        If TypeOf (xd) Is ClassLibrary1.NestedTypeDerived Then
            Console.WriteLine("Success")
        End If
    End Sub

    Sub TestNs()
        Dim x As New Class_In_NS
        If x.SuccessProperty = True Then
            Console.WriteLine("Success")
        End If
    End Sub
End Module

Class ABC

End Class
]]>
    </file>
</compilation>
            ''//All on same FX - should result in No Errors
            Dim referenceLibrary_Compilation = DirectCast(CompileAndVerify(sourceLibV1, options:=TestOptions.ReleaseDll).Compilation, VisualBasicCompilation)
            Dim main_NoRetarget = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceMain, references:={referenceLibrary_Compilation.ToMetadataReference})
            main_NoRetarget.VerifyDiagnostics(Diagnostic(ERRID.ERR_GenericConstraintNotSatisfied2, "NewClass").WithArguments("NewClass", "ClassLibrary1.TestInterface"),
                                              Diagnostic(ERRID.ERR_NoSuitableNewForNewConstraint2, "ClassLibrary1.TestInterface").WithArguments("ClassLibrary1.TestInterface", "t"),
                                              Diagnostic(ERRID.ERR_GenericConstraintNotSatisfied2, "Integer").WithArguments("Integer", "ClassLibrary1.TestClass"),
                                              Diagnostic(ERRID.ERR_GenericConstraintNotSatisfied2, "ABC").WithArguments("ABC", "ClassLibrary1.TestClass"))

            ''//Retargetted - should result in Same Errors 
            Dim RetargetReference = RetargetCompilationToV2MsCorlib(referenceLibrary_Compilation)
            Dim Main_Retarget = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceMain, references:={RetargetReference}, options:=TestOptions.ReleaseExe)
            main_NoRetarget.VerifyDiagnostics(Diagnostic(ERRID.ERR_GenericConstraintNotSatisfied2, "NewClass").WithArguments("NewClass", "ClassLibrary1.TestInterface"),
                                              Diagnostic(ERRID.ERR_NoSuitableNewForNewConstraint2, "ClassLibrary1.TestInterface").WithArguments("ClassLibrary1.TestInterface", "t"),
                                              Diagnostic(ERRID.ERR_GenericConstraintNotSatisfied2, "Integer").WithArguments("Integer", "ClassLibrary1.TestClass"),
                                              Diagnostic(ERRID.ERR_GenericConstraintNotSatisfied2, "ABC").WithArguments("ABC", "ClassLibrary1.TestClass"))
        End Sub

        <Fact>
        Public Sub Retarget_Delegate()
            'The test involves compilation with/without retargeting and ensuring same behavior at runtime
            'same diagnostics (or lack off) as compile time
            'and verification of the retargeted types and the underlying source type and pertinent items

            Dim source =
               <compilation name="App1">
                   <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim D1 As ClassLibrary1.Del_NG_Sub = AddressOf s_d1
        Dim D2 As ClassLibrary1.Del_NG_Function = AddressOf s_d2

        Dim D3 As ClassLibrary1.Del_G_Sub(Of Integer) = AddressOf s_d3
        Dim D4 As ClassLibrary1.Del_G_Function(Of Boolean) = AddressOf s_d4

        Dim D5 As ClassLibrary1.Del_G_Sub_2(Of Integer) = AddressOf s_d3
        Dim D6 As ClassLibrary1.Del_G_Function_2(Of Boolean) = AddressOf s_d4

        'Dim d7 As ClassLibrary1.Del_G_Sub_Constraint(Of Integer)
        Dim d7_1 As ClassLibrary1.Del_G_Sub_Constraint(Of ClassLibrary1.Ifoo) = Nothing

        Console.WriteLine("Success")
    End Sub

    Private Sub s_d1()
        Console.WriteLine("s_d1")
    End Sub

    Private Function s_d2() As Integer
        Console.WriteLine("s_d2")
        Return 1
    End Function

    Private Sub s_d3()
        Console.WriteLine("s_d3")
    End Sub

    Private Function s_d4()
        Console.WriteLine("s_d4")
        Return 1
    End Function

    Private Sub s_d5(x As Integer)
        Console.WriteLine("s_d5")
    End Sub

    Private Function s_d6(y As Integer) As Integer
        Console.WriteLine("s_d6")
        Return 1
    End Function

    Class Goo

    End Class
End Module

]]>
                   </file>
               </compilation>

            Dim CL1_source =
               <compilation name="C1">
                   <file name="Cl.vb"><![CDATA[
Namespace ClassLibrary1
    Public Module Module1
        Public Delegate Sub Del_NG_Sub()
        Public Delegate Function Del_NG_Function() As Integer

        Public Delegate Sub Del_G_Sub(Of t)()
        Public Delegate Function Del_G_Function(Of t)() As t

        Public Delegate Sub Del_G_Sub_2(Of t)(x As t)
        Public Delegate Function Del_G_Function_2(Of t)(x As t) As t


        Public Delegate Sub Del_G_Sub_Constraint(Of t As Ifoo)(x As t)
    End Module

    Public Interface Ifoo

    End Interface
End Namespace
]]>
                   </file>
               </compilation>

            'Check Expected Behavior - Expect no diagnostic errors with retargeting
            ' All on same FX - should result in No Errors
            Dim referenceLibrary_Compilation = DirectCast(CompileAndVerify(CL1_source, options:=TestOptions.ReleaseDll).Compilation, VisualBasicCompilation)

            Dim referenceLibrary_Metadata = referenceLibrary_Compilation.ToMetadataReference
            Dim main_NoRetarget = CompileAndVerify(source, references:={referenceLibrary_Metadata},
                                                   expectedOutput:=<![CDATA[Success
]]>)
            main_NoRetarget.VerifyDiagnostics()

            '//Retargetted - should result in No Errors also and same runtime behavior
            Dim RetargetReference = RetargetCompilationToV2MsCorlib(referenceLibrary_Compilation)
            ' ILVerify: Multiple modules named 'mscorlib' were found
            Dim Main_Retarget = CompileAndVerify(source, references:={RetargetReference}, options:=TestOptions.ReleaseExe, verify:=Verification.FailsILVerify,
                                                 expectedOutput:=<![CDATA[Success
]]>)
            Main_Retarget.VerifyDiagnostics()

            'Check the retargeting symbol information
            Dim sourceAssembly = DirectCast(RetargetReference.Compilation.Assembly, SourceAssemblySymbol)
            Dim SourceModuleReference = sourceAssembly.Modules(0)
            Dim sourceNamespace As SourceNamespaceSymbol = CType(SourceModuleReference.GlobalNamespace.GetNamespace("ClassLibrary1"), SourceNamespaceSymbol)

            Dim retargetingAssembly = Main_Retarget.Compilation.GetReferencedAssemblySymbol(RetargetReference)
            Dim retargetingModule = retargetingAssembly.Modules(0)
            Dim retargetingNamespace As RetargetingNamespaceSymbol = CType(retargetingModule.GlobalNamespace.GetNamespace("ClassLibrary1"), RetargetingNamespaceSymbol)

            'Comparison of Types
            Dim sourceType As NamedTypeSymbol = Nothing
            Dim retargetingType As NamedTypeSymbol = Nothing

            'Lets Look at the Delegate Items
            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Module1")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Module1")
            CheckTypes(sourceType, retargetingType)

            sourceType = sourceNamespace.GetMember(Of NamedTypeSymbol)("Module1")
            retargetingType = retargetingNamespace.GetMember(Of NamedTypeSymbol)("Module1")
            CheckTypes(sourceType, retargetingType)

            CheckTypes(sourceType.GetMember("Del_NG_Sub"), retargetingType.GetMember("Del_NG_Sub"))
            CheckTypes(sourceType.GetMember("Del_NG_Function"), retargetingType.GetMember("Del_NG_Function"))
            CheckTypes(sourceType.GetMember("Del_G_Sub"), retargetingType.GetMember("Del_G_Sub"))
            Assert.Equal(1, CType(sourceType.GetMember("Del_G_Sub"), SourceNamedTypeSymbol).TypeParameters.Length)
            CheckTypeParameters(CType(sourceType.GetMember("Del_G_Sub"), SourceNamedTypeSymbol).TypeParameters(0), CType(retargetingType.GetMember("Del_G_Sub"), RetargetingNamedTypeSymbol).TypeParameters(0))

            CheckTypes(sourceType.GetMember("Del_G_Function"), retargetingType.GetMember("Del_G_Function"))
            Assert.Equal(1, CType(sourceType.GetMember("Del_G_Function"), SourceNamedTypeSymbol).TypeParameters.Length)
            CheckTypeParameters(CType(sourceType.GetMember("Del_G_Function"), SourceNamedTypeSymbol).TypeParameters(0), CType(retargetingType.GetMember("Del_G_Function"), RetargetingNamedTypeSymbol).TypeParameters(0))

            CheckTypes(sourceType.GetMember("Del_G_Sub_2"), retargetingType.GetMember("Del_G_Sub_2"))
            Assert.Equal(1, CType(sourceType.GetMember("Del_G_Sub_2"), SourceNamedTypeSymbol).TypeParameters.Length)
            CheckTypeParameters(CType(sourceType.GetMember("Del_G_Sub_2"), SourceNamedTypeSymbol).TypeParameters(0), CType(retargetingType.GetMember("Del_G_Sub_2"), RetargetingNamedTypeSymbol).TypeParameters(0))
            Assert.Equal(0, CType(sourceType.GetMember("Del_G_Sub_2"), SourceNamedTypeSymbol).TypeParameters(0).ConstraintTypes.Length)

            CheckTypes(sourceType.GetMember("Del_G_Function_2"), retargetingType.GetMember("Del_G_Function_2"))
            Assert.Equal(1, CType(sourceType.GetMember("Del_G_Function_2"), SourceNamedTypeSymbol).TypeParameters.Length)
            CheckTypeParameters(CType(sourceType.GetMember("Del_G_Function_2"), SourceNamedTypeSymbol).TypeParameters(0), CType(retargetingType.GetMember("Del_G_Function_2"), RetargetingNamedTypeSymbol).TypeParameters(0))

            CheckTypes(sourceType.GetMember("Del_G_Sub_Constraint"), retargetingType.GetMember("Del_G_Sub_Constraint"))
            Assert.Equal(1, CType(sourceType.GetMember("Del_G_Sub_Constraint"), SourceNamedTypeSymbol).TypeParameters.Length)
            CheckTypeParameters(CType(sourceType.GetMember("Del_G_Sub_Constraint"), SourceNamedTypeSymbol).TypeParameters(0), CType(retargetingType.GetMember("Del_G_Sub_Constraint"), RetargetingNamedTypeSymbol).TypeParameters(0))
            Assert.Equal(1, CType(sourceType.GetMember("Del_G_Sub_Constraint"), SourceNamedTypeSymbol).TypeParameters(0).ConstraintTypes.Length)
            Assert.Equal(CType(sourceType.GetMember("Del_G_Sub_Constraint"), SourceNamedTypeSymbol).TypeParameters(0).ConstraintTypes(0), CType(retargetingType.GetMember("Del_G_Sub_Constraint"), RetargetingNamedTypeSymbol).UnderlyingNamedType.TypeParameters(0).ConstraintTypes(0))
        End Sub

        Friend Function RetargetCompilationToV2MsCorlib(C As VisualBasicCompilation) As VisualBasicCompilationReference
            Dim NewCompilation As VisualBasicCompilation = Nothing

            Dim OldReference As MetadataReference = Nothing
            Dim OldVBReference As MetadataReference = Nothing

            'For Retargeting - I want to ensure that if mscorlib v4 is detected then I will retarget to V2.
            'This should apply to mscorlib, microsoft.visualbasic, system

            'Exist V2 reference
            Dim bAbleToRetargetToV2 As Boolean = False
            For Each r In C.References
                Dim Item As String = r.Display
                If r.Display.ToLower.Contains("mscorlib") And r.Display.ToLower.Contains("net4") Then
                    bAbleToRetargetToV2 = True
                End If
            Next

            'Verify is mscorlib reference is present that is v4       
            If bAbleToRetargetToV2 Then
                Dim AssembliesToRetarget As Integer = 0
                For Each r In C.References
                    Dim Item As String = r.Display
                    If r.Display.ToLower.Contains("mscorlib") And r.Display.ToLower.Contains("net4") Then
                        OldReference = r
                        AssembliesToRetarget = AssembliesToRetarget + 1
                    ElseIf r.Display.ToLower.Contains("microsoft.visualbasic") And r.Display.ToLower.Contains("net4") Then
                        OldVBReference = r
                        AssembliesToRetarget = AssembliesToRetarget + 2
                        'ElseIf r.Display.Contains("System") And r.Display.Contains("v4") Then
                        'bfound = bfound + 4
                    End If
                Next

                If AssembliesToRetarget = 0 Then
                    NewCompilation = C
                Else
                    'Retarget to use v2.0 assemblies
                    If AssembliesToRetarget = 1 Then
                        NewCompilation = C.ReplaceReference(oldReference:=OldReference, newReference:=Net20.References.mscorlib)
                    ElseIf AssembliesToRetarget = 2 Then
                        NewCompilation = C.ReplaceReference(oldReference:=OldVBReference, newReference:=Net20.References.MicrosoftVisualBasic)
                    ElseIf AssembliesToRetarget = 3 Then
                        NewCompilation = C.ReplaceReference(oldReference:=OldReference, newReference:=Net20.References.mscorlib).
                            ReplaceReference(oldReference:=OldVBReference, newReference:=Net20.References.MicrosoftVisualBasic)
                    End If
                End If
            Else
                NewCompilation = C
            End If

            Return New VisualBasicCompilationReference(NewCompilation)
        End Function

        <Fact> <WorkItem(703433, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/703433")>
        Public Sub Bug703433()
            Dim source1 =
<compilation>
    <file name="a.vb">
Public Class C1(Of T)
End Class
    </file>
</compilation>

            Dim comp1 = CreateEmptyCompilationWithReferences(source1, {MscorlibRef_v20}, TestOptions.ReleaseDll)
            comp1.VerifyDiagnostics()

            Dim c1 As NamedTypeSymbol = comp1.Assembly.GlobalNamespace.GetTypeMembers("C1").Single

            Dim source2 =
<compilation>
    <file name="a.vb">
    </file>
</compilation>

            Dim comp2 = CreateEmptyCompilationWithReferences(source2, {MscorlibRef_v4_0_30316_17626, New VisualBasicCompilationReference(comp1)}, TestOptions.ReleaseDll)

            Dim c1r As NamedTypeSymbol = comp2.GlobalNamespace.GetTypeMembers("C1").Single

            Assert.IsType(Of RetargetingNamedTypeSymbol)(c1r)
            Assert.Equal(c1.Name, c1r.Name)
            Assert.Equal(c1.Arity, c1r.Arity)
            Assert.Equal(c1.MangleName, c1r.MangleName)
            Assert.Equal(c1.MetadataName, c1r.MetadataName)
        End Sub

        <Fact>
        <WorkItem(3898, "https://github.com/dotnet/roslyn/issues/3898")>
        Public Sub Regargeting_IsSerializable()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C(Of T)
End Class
<System.Serializable>
Public Class CS(Of T)
End Class
    ]]></file>
</compilation>

            Dim comp1 = CreateEmptyCompilation(ParseSourceXml(source1, Nothing).ToArray(), references:={MscorlibRef_v20}, options:=TestOptions.ReleaseDll)
            comp1.VerifyDiagnostics()

            Dim source2 =
<compilation>
    <file name="a.vb">
    </file>
</compilation>

            Dim comp2 = CreateEmptyCompilation(ParseSourceXml(source2, Nothing).ToArray(), references:={MscorlibRef_v4_0_30316_17626, New VisualBasicCompilationReference(comp1)}, options:=TestOptions.ReleaseDll)

            Dim c As NamedTypeSymbol = comp2.GlobalNamespace.GetTypeMembers("C").Single
            Assert.IsType(Of RetargetingNamedTypeSymbol)(c)
            Assert.False(DirectCast(c, INamedTypeSymbol).IsSerializable)

            Dim cs As NamedTypeSymbol = comp2.GlobalNamespace.GetTypeMembers("CS").Single
            Assert.IsType(Of RetargetingNamedTypeSymbol)(cs)
            Assert.True(DirectCast(cs, INamedTypeSymbol).IsSerializable)
        End Sub

        <Fact>
        Public Sub ExplicitInterfaceImplementationRetargetingGenericType()
            Dim source1 = "
Public Class C1(Of T)
    Public Interface I1
        Sub M(x As T)
    End Interface
End Class
"
            Dim ref1 = CreateEmptyCompilation("").ToMetadataReference()
            Dim compilation1 = CreateCompilation(source1, references:={ref1})

            Dim source2 = "
Public Class C2(Of U) 
    Implements C1(Of U).I1

    Sub M(x As U) Implements C1(Of U).I1.M
    End Sub
End Class
"
            Dim compilation2 = CreateCompilation(source2, references:={compilation1.ToMetadataReference(), ref1, CreateEmptyCompilation("").ToMetadataReference()})

            Dim compilation3 = CreateCompilation("", references:={compilation1.ToMetadataReference(), compilation2.ToMetadataReference()})

            Assert.NotSame(compilation2.GetTypeByMetadataName("C1`1"), compilation3.GetTypeByMetadataName("C1`1"))

            Dim c2 = compilation3.GetTypeByMetadataName("C2`1")
            Assert.IsType(Of RetargetingNamedTypeSymbol)(c2)

            Dim m = c2.GetMethod("M")

            Assert.Equal(c2.Interfaces().Single().GetMethod("M"), m.ExplicitInterfaceImplementations.Single())
        End Sub

    End Class
#End If

End Namespace

