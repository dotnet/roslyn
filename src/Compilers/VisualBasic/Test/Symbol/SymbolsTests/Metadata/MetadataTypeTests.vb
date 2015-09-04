' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata

    Public Class MetadataTypeTests
        Inherits BasicTestBase

        <Fact>
        Public Sub MetadataNamespaceSymbol01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="MT">
    <file name="a.vb">
Public Class A
End Class
    </file>
</compilation>)

            Dim mscorNS = compilation.GetReferencedAssemblySymbol(compilation.References(0))
            Assert.Equal("mscorlib", mscorNS.Name)
            Assert.Equal(SymbolKind.Assembly, mscorNS.Kind)
            Dim ns1 = DirectCast(mscorNS.GlobalNamespace.GetMembers("System").Single(), NamespaceSymbol)
            Dim ns2 = DirectCast(ns1.GetMembers("Runtime").Single(), NamespaceSymbol)
            Dim ns = DirectCast(ns2.GetMembers("Serialization").Single(), NamespaceSymbol)

            Assert.Equal(mscorNS, ns.ContainingAssembly)
            Assert.Equal(ns2, ns.ContainingSymbol)
            Assert.Equal(ns2, ns.ContainingNamespace)
            Assert.True(ns.IsDefinition) ' ?
            Assert.True(ns.IsNamespace)
            Assert.False(ns.IsType)

            Assert.Equal(SymbolKind.Namespace, ns.Kind)
            Assert.Equal(Accessibility.Public, ns.DeclaredAccessibility)
            Assert.True(ns.IsShared)
            Assert.False(ns.IsMustOverride)
            Assert.False(ns.IsNotOverridable)
            Assert.False(ns.IsOverridable)
            Assert.False(ns.IsOverrides)

            ' 47 types, 1 namespace (Formatters)
            Assert.Equal(48, ns.GetMembers().Length)
            Assert.Equal(47, ns.GetTypeMembers().Length())

            Dim fullName = "System.Runtime.Serialization"
            ' Friend Assert.Equal(fullName, class1.GetFullNameWithoutGenericArgs())
            Assert.Equal(fullName, ns.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <WorkItem(530123, "DevDiv")>
        <Fact>
        Public Sub MetadataTypeSymbolModule01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Public Module A
End Module
    </file>
</compilation>, TestOptions.ReleaseDll)

            CompileAndVerify(compilation,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim a = m.GlobalNamespace.GetTypeMember("A")
                                                  Assert.Equal(TypeKind.Module, a.TypeKind)
                                                  Assert.Equal(0, a.GetAttributes().Length) ' Should not have StandardModule attribute
                                                  Assert.Equal("Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute", a.GetCustomAttributesToEmit(New ModuleCompilationState).Single().ToString())
                                              End Sub)

            CompileAndVerify(compilation,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim a = m.GlobalNamespace.GetTypeMember("A")
                                                  Assert.Equal(0, a.GetAttributes().Length) ' Should not have StandardModule attribute
                                                  Assert.Equal(TypeKind.Module, a.TypeKind)
                                                  Assert.Equal("Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute", a.GetCustomAttributesToEmit(New ModuleCompilationState).Single().ToString())
                                              End Sub)
        End Sub

        <WorkItem(537324, "DevDiv")>
        <Fact>
        Public Sub MetadataTypeSymbolClass01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="MT">
    <file name="a.vb">
Public Class A
End Class
    </file>
</compilation>)

            Dim mscorNS = compilation.GetReferencedAssemblySymbol(compilation.References(0))
            Assert.Equal("mscorlib", mscorNS.Name)
            Assert.Equal(SymbolKind.Assembly, mscorNS.Kind)
            Dim ns1 = DirectCast(mscorNS.GlobalNamespace.GetMembers("Microsoft").Single(), NamespaceSymbol)
            Dim ns2 = DirectCast(ns1.GetMembers("Runtime").Single(), NamespaceSymbol)
            Dim ns3 = DirectCast(ns2.GetMembers("Hosting").Single(), NamespaceSymbol)

            Dim class1 = DirectCast(ns3.GetTypeMembers("StrongNameHelpers").First(), NamedTypeSymbol)
            ' internal static class
            Assert.Equal(0, class1.Arity)
            Assert.Equal(mscorNS, class1.ContainingAssembly)
            Assert.Equal(ns3, class1.ContainingSymbol)
            Assert.Equal(ns3, class1.ContainingNamespace)
            Assert.True(class1.IsDefinition)
            Assert.False(class1.IsNamespace)
            Assert.True(class1.IsType)
            Assert.True(class1.IsReferenceType)
            Assert.False(class1.IsValueType)

            Assert.Equal(SymbolKind.NamedType, class1.Kind)
            Assert.Equal(TypeKind.Class, class1.TypeKind)
            Assert.Equal(Accessibility.Friend, class1.DeclaredAccessibility)
            Assert.False(class1.IsShared)
            Assert.False(class1.IsMustInherit)
            Assert.False(class1.IsMustOverride)
            Assert.True(class1.IsNotInheritable)
            Assert.False(class1.IsNotOverridable)
            Assert.False(class1.IsOverridable)
            Assert.False(class1.IsOverrides)

            ' 18 members
            Assert.Equal(18, class1.GetMembers().Length)
            Assert.Equal(0, class1.GetTypeMembers().Length())
            Assert.Equal(0, class1.Interfaces.Length())

            Dim fullName = "Microsoft.Runtime.Hosting.StrongNameHelpers"
            ' Friend Assert.Equal(fullName, class1.GetFullNameWithoutGenericArgs())
            Assert.Equal(fullName, class1.ToTestDisplayString())
            Assert.Equal(0, class1.TypeArguments.Length)
            Assert.Equal(0, class1.TypeParameters.Length)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub MetadataTypeSymbolGenClass02()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation name="MT">
    <file name="a.vb">
Public Class A
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))

            Dim mscorNS = compilation.GetReferencedAssemblySymbol(compilation.References(0))
            Assert.Equal("mscorlib", mscorNS.Name)
            Assert.Equal(SymbolKind.Assembly, mscorNS.Kind)
            Dim ns1 = DirectCast(DirectCast(mscorNS.GlobalNamespace.GetMembers("System").Single(), NamespaceSymbol).GetMembers("Collections").Single(), NamespaceSymbol)
            Dim ns2 = DirectCast(ns1.GetMembers("Generic").Single(), NamespaceSymbol)

            Dim type1 = DirectCast(ns2.GetTypeMembers("Dictionary").First(), NamedTypeSymbol)
            ' public generic class
            Assert.Equal(2, type1.Arity)
            Assert.Equal(mscorNS, type1.ContainingAssembly)
            Assert.Equal(ns2, type1.ContainingSymbol)
            Assert.Equal(ns2, type1.ContainingNamespace)
            Assert.True(type1.IsDefinition)
            Assert.False(type1.IsNamespace)
            Assert.True(type1.IsType)
            Assert.True(type1.IsReferenceType)
            Assert.False(type1.IsValueType)

            Assert.Equal(SymbolKind.NamedType, type1.Kind)
            Assert.Equal(TypeKind.Class, type1.TypeKind)
            Assert.Equal(Accessibility.Public, type1.DeclaredAccessibility)
            Assert.False(type1.IsShared)
            Assert.False(type1.IsMustInherit)
            Assert.False(type1.IsMustOverride)
            Assert.False(type1.IsNotInheritable)
            Assert.False(type1.IsNotOverridable)
            Assert.False(type1.IsOverridable)
            Assert.False(type1.IsOverrides)

            ' 4 nested types, 64 members overall
            Assert.Equal(64, type1.GetMembers().Length)
            Assert.Equal(4, type1.GetTypeMembers().Length())
            ' IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, 
            ' IDictionary, ICollection, IEnumerable, ISerializable, IDeserializationCallback
            Assert.Equal(8, type1.Interfaces.Length())

            Dim fullName = "System.Collections.Generic.Dictionary(Of TKey, TValue)"
            ' Friend Assert.Equal(fullName, class1.GetFullNameWithoutGenericArgs())
            Assert.Equal(fullName, type1.ToTestDisplayString())
            Assert.Equal(2, type1.TypeArguments.Length)
            Assert.Equal(2, type1.TypeParameters.Length)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub MetadataTypeSymbolGenInterface01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="MT">
    <file name="a.vb">
Public Class A
End Class
    </file>
</compilation>)

            Dim mscorNS = compilation.GetReferencedAssemblySymbol(compilation.References(0))
            Assert.Equal("mscorlib", mscorNS.Name)
            Assert.Equal(SymbolKind.Assembly, mscorNS.Kind)
            Dim ns1 = DirectCast(DirectCast(mscorNS.GlobalNamespace.GetMembers("System").Single(), NamespaceSymbol).GetMembers("Collections").Single(), NamespaceSymbol)
            Dim ns2 = DirectCast(ns1.GetMembers("Generic").Single(), NamespaceSymbol)

            Dim type1 = DirectCast(ns2.GetTypeMembers("IList").First(), NamedTypeSymbol)
            ' public generic interface
            Assert.Equal(1, type1.Arity)
            Assert.Equal(mscorNS, type1.ContainingAssembly)
            Assert.Equal(ns2, type1.ContainingSymbol)
            Assert.Equal(ns2, type1.ContainingNamespace)
            Assert.True(type1.IsDefinition)
            Assert.False(type1.IsNamespace)
            Assert.True(type1.IsType)
            Assert.True(type1.IsReferenceType)
            Assert.False(type1.IsValueType)

            Assert.Equal(SymbolKind.NamedType, type1.Kind)
            Assert.Equal(TypeKind.Interface, type1.TypeKind)
            Assert.Equal(Accessibility.Public, type1.DeclaredAccessibility)
            Assert.False(type1.IsShared)
            Assert.True(type1.IsMustInherit)
            Assert.False(type1.IsMustOverride) ' member
            Assert.False(type1.IsNotInheritable)
            Assert.False(type1.IsNotOverridable) ' member
            Assert.False(type1.IsOverridable)
            Assert.False(type1.IsOverrides)

            ' 3 method, 2 get|set_<Prop> method, 1 Properties
            Assert.Equal(6, type1.GetMembers().Length)
            Assert.Equal(0, type1.GetTypeMembers().Length())
            ' ICollection<T>, IEnumerable<T>, IEnumerable
            Assert.Equal(3, type1.Interfaces.Length())

            Dim fullName = "System.Collections.Generic.IList(Of T)"
            ' Friend Assert.Equal(fullName, class1.GetFullNameWithoutGenericArgs())
            Assert.Equal(fullName, type1.ToTestDisplayString())
            Assert.Equal(1, type1.TypeArguments.Length)
            Assert.Equal(1, type1.TypeParameters.Length)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub MetadataTypeSymbolStruct01()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation name="MT">
    <file name="a.vb">
Public Class A
End Class
    </file>
</compilation>, TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))

            Dim mscorNS = compilation.GetReferencedAssemblySymbol(compilation.References(0))
            Assert.Equal("mscorlib", mscorNS.Name)
            Assert.Equal(SymbolKind.Assembly, mscorNS.Kind)
            Dim ns1 = DirectCast(mscorNS.GlobalNamespace.GetMembers("System").Single(), NamespaceSymbol)
            Dim ns2 = DirectCast(ns1.GetMembers("Runtime").Single(), NamespaceSymbol)
            Dim ns3 = DirectCast(ns2.GetMembers("Serialization").Single(), NamespaceSymbol)
            Dim type1 = DirectCast(ns3.GetTypeMembers("StreamingContext").First(), NamedTypeSymbol)

            Assert.Equal(mscorNS, type1.ContainingAssembly)
            Assert.Equal(ns3, type1.ContainingSymbol)
            Assert.Equal(ns3, type1.ContainingNamespace)
            Assert.True(type1.IsDefinition)
            Assert.False(type1.IsNamespace)
            Assert.True(type1.IsType)
            Assert.False(type1.IsReferenceType)
            Assert.True(type1.IsValueType)

            Assert.Equal(SymbolKind.NamedType, type1.Kind)
            Assert.Equal(TypeKind.Structure, type1.TypeKind)
            Assert.Equal(Accessibility.Public, type1.DeclaredAccessibility)
            Assert.False(type1.IsShared)
            Assert.False(type1.IsMustInherit)
            Assert.False(type1.IsMustOverride)
            Assert.True(type1.IsNotInheritable)
            Assert.False(type1.IsNotOverridable) ' only applied to member
            Assert.False(type1.IsOverridable)
            Assert.False(type1.IsOverrides)

            ' 4 methods + 1 implicit parameterless constructor, 2 get_<Prop> methods, 2 Properties, 2 fields
            Assert.Equal(11, type1.GetMembers().Length)
            Assert.Equal(0, type1.GetTypeMembers().Length())
            Assert.Equal(0, type1.Interfaces.Length())

            Dim fullName = "System.Runtime.Serialization.StreamingContext"
            ' Friend Assert.Equal(fullName, class1.GetFullNameWithoutGenericArgs())
            Assert.Equal(fullName, type1.ToTestDisplayString())
            Assert.Equal(0, type1.TypeArguments.Length)
            Assert.Equal(0, type1.TypeParameters.Length)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub MetadataArrayTypeSymbol01()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation name="MT">
    <file name="a.vb">
Public Class A
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))

            Dim mscorNS = compilation.GetReferencedAssemblySymbol(compilation.References(0))
            Assert.Equal("mscorlib", mscorNS.Name)
            Assert.Equal(SymbolKind.Assembly, mscorNS.Kind)
            Dim ns1 = DirectCast(mscorNS.GlobalNamespace.GetMembers("System").Single(), NamespaceSymbol)
            Dim ns2 = DirectCast(ns1.GetMembers("Diagnostics").Single(), NamespaceSymbol)
            Dim ns3 = DirectCast(ns2.GetMembers("Eventing").Single(), NamespaceSymbol)

            Dim type1 = DirectCast(ns3.GetTypeMembers("EventProviderBase").Single(), NamedTypeSymbol)
            ' EventData[]
            Dim type2 = DirectCast(DirectCast(type1.GetMembers("m_EventData").Single(), FieldSymbol).Type, ArrayTypeSymbol)
            Dim member2 = DirectCast(type1.GetMembers("WriteTransferEventHelper").Single(), MethodSymbol)
            Assert.Equal(3, member2.Parameters.Length)
            ' params object[]
            Dim type3 = DirectCast(DirectCast(member2.Parameters(2), ParameterSymbol).Type, ArrayTypeSymbol)

            Assert.Equal(SymbolKind.ArrayType, type2.Kind)
            Assert.Equal(SymbolKind.ArrayType, type3.Kind)
            Assert.Equal(Accessibility.NotApplicable, type2.DeclaredAccessibility)
            Assert.Equal(Accessibility.NotApplicable, type3.DeclaredAccessibility)

            Assert.Equal(1, type2.Rank)
            Assert.True(type2.IsSZArray)
            Assert.Equal(1, type3.Rank)
            Assert.Equal(TypeKind.Array, type2.TypeKind)
            Assert.Equal(TypeKind.Array, type3.TypeKind)

            Assert.Equal("EventData", type2.ElementType.Name)
            Assert.Equal("Array", type2.BaseType.Name)
            Assert.Equal("Object", type3.ElementType.Name)
            Assert.Equal("System.Diagnostics.Eventing.EventProviderBase.EventData()", type2.ToTestDisplayString())
            Assert.Equal("System.Object()", type3.ToTestDisplayString())

            Assert.Equal(1, type2.Interfaces.Length)
            Assert.Equal(1, type3.Interfaces.Length)
            ' bug - 2041
            'Assert.False(type2.IsDefinition)
            Assert.False(type2.IsNamespace)
            Assert.True(type3.IsType)

            Assert.True(type2.IsReferenceType)
            Assert.True(type2.ElementType.IsValueType)
            Assert.True(type3.IsReferenceType)
            Assert.False(type3.IsValueType)

            Assert.False(type2.IsShared)
            Assert.False(type2.IsMustOverride)
            Assert.False(type2.IsNotOverridable)
            Assert.False(type3.IsOverridable)
            Assert.False(type3.IsOverrides)

            Assert.Equal(0, type2.GetMembers().Length)
            Assert.Equal(0, type3.GetMembers(String.Empty).Length)

            Assert.Equal(0, type3.GetTypeMembers().Length)
            Assert.Equal(0, type2.GetTypeMembers(String.Empty).Length)
            Assert.Equal(0, type3.GetTypeMembers(String.Empty, 0).Length)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <WorkItem(542755, "DevDiv")>
        <Fact>
        Public Sub SpellingOfGenericClassNameIsPreserved()
            Dim ilSource = <![CDATA[
.class interface public abstract I2<T> { }

.class interface public abstract I
{
    .method public hidebysig newslot abstract virtual instance void M(class I2<string> x) { }
}
        ]]>

            Dim vbSource =
<compilation name="SpellingOfGenericClassNameIsPreserved">
    <file name="a.vb">
Class C
    Sub Test(y As I)
        y.M(Nothing)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)

            AssertNoErrors(compilation)

            Dim i2 As NamedTypeSymbol = compilation.GetTypeByMetadataName("I2")
            Assert.False(i2.IsErrorType())
            Assert.Equal(1, i2.Arity)
            Assert.Equal("I2", i2.Name)
            Assert.False(i2.MangleName)
            Assert.Equal("I2(Of T)", i2.ToTestDisplayString())
            Assert.Equal("I2", i2.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat))

            CompileAndVerify(compilation)
        End Sub

        <Fact>
        Public Sub SpellingOfGenericClassNameIsPreserved2()
            Dim ilSource = <![CDATA[
.class interface public abstract I2`2<T> { }

.class interface public abstract I
{
    .method public hidebysig newslot abstract virtual instance void M(class I2`2<string> x) { }
}
        ]]>

            Dim vbSource =
<compilation name="SpellingOfGenericClassNameIsPreserved">
    <file name="a.vb">
Class C
    Sub Test(y As I)
        y.M(Nothing)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)

            AssertNoErrors(compilation)

            Dim i2 As NamedTypeSymbol = compilation.GetTypeByMetadataName("I2`2")
            Assert.False(i2.IsErrorType())
            Assert.Equal(1, i2.Arity)
            Assert.Equal("I2`2", i2.Name)
            Assert.False(i2.MangleName)
            Assert.Equal("I2`2(Of T)", i2.ToTestDisplayString())
            Assert.Equal("I2`2", i2.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat))

            CompileAndVerify(compilation)
        End Sub

        <Fact>
        Public Sub SpellingOfGenericClassNameIsPreserved3()
            Dim ilSource = <![CDATA[
.class interface public abstract I2`1<T> { }

.class interface public abstract I
{
    .method public hidebysig newslot abstract virtual instance void M(class I2`1<string> x) { }
}
        ]]>

            Dim vbSource =
<compilation name="SpellingOfGenericClassNameIsPreserved">
    <file name="a.vb">
Class C
    Sub Test(y As I)
        y.M(Nothing)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)

            AssertNoErrors(compilation)

            Dim i2 As NamedTypeSymbol = compilation.GetTypeByMetadataName("I2`1")
            Assert.False(i2.IsErrorType())
            Assert.Equal(1, i2.Arity)
            Assert.Equal("I2", i2.Name)
            Assert.True(i2.MangleName)
            Assert.Equal("I2(Of T)", i2.ToTestDisplayString())
            Assert.Equal("I2`1", i2.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat))

            CompileAndVerify(compilation)
        End Sub

        <Fact>
        Public Sub SpellingOfGenericClassNameIsPreserved4()
            Dim ilSource = <![CDATA[
.class interface public abstract I2`01<T> { }

.class interface public abstract I
{
    .method public hidebysig newslot abstract virtual instance void M(class I2`01<string> x) { }
}
        ]]>

            Dim vbSource =
<compilation name="SpellingOfGenericClassNameIsPreserved">
    <file name="a.vb">
Class C
    Sub Test(y As I)
        y.M(Nothing)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)

            AssertNoErrors(compilation)

            Dim i2 As NamedTypeSymbol = compilation.GetTypeByMetadataName("I2`01")
            Assert.False(i2.IsErrorType())
            Assert.Equal(1, i2.Arity)
            Assert.Equal("I2`01", i2.Name)
            Assert.False(i2.MangleName)
            Assert.Equal("I2`01(Of T)", i2.ToTestDisplayString())
            Assert.Equal("I2`01", i2.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat))

            CompileAndVerify(compilation)
        End Sub

        <Fact>
        Public Sub SpellingOfGenericClassNameIsPreserved5()
            Dim ilSource = <![CDATA[
.class interface public abstract I2`1 { }

.class interface public abstract I
{
    .method public hidebysig newslot abstract virtual instance void M(class I2`1 x) { }

    .class interface nested public abstract I2`1 { }
    .class interface nested public abstract I { }
    .class interface nested public abstract I3`1<T> { }
    .class interface nested public abstract I4`2<T> { }
}

.class interface public System.IEquatable`1 { }

.class interface public System.Linq.IQueryable`1 { }

.class interface public System.Linq.IQueryable<T> { }

.class interface public abstract I3`1<T> { }

.class interface public abstract I4`2<T> { }
]]>

            Dim vbSource =
<compilation name="SpellingOfGenericClassNameIsPreserved">
    <file name="a.vb">
Class C
    Sub Test(y As I)
        y.M(Nothing)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)

            AssertNoErrors(compilation)

            Dim i2 As NamedTypeSymbol = compilation.GetTypeByMetadataName("I2`1")
            Assert.False(i2.IsErrorType())
            Assert.Equal(0, i2.Arity)
            Assert.Equal("I2`1", i2.Name)
            Assert.False(i2.MangleName)
            Assert.Equal("I2`1", i2.ToTestDisplayString())
            Assert.Equal("I2`1", i2.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat))

            Dim iEquatable As NamedTypeSymbol = compilation.GetWellKnownType(WellKnownType.System_IEquatable_T)
            Assert.False(iEquatable.IsErrorType())
            Assert.Equal(1, iEquatable.Arity)
            Assert.Null(compilation.GetTypeByMetadataName("System.IEquatable`1"))

            Dim iQueryable_T As NamedTypeSymbol = compilation.GetWellKnownType(WellKnownType.System_Linq_IQueryable_T)
            Assert.True(iQueryable_T.IsErrorType())
            Assert.Equal(1, iQueryable_T.Arity)

            Dim iQueryable As NamedTypeSymbol = compilation.GetWellKnownType(WellKnownType.System_Linq_IQueryable)
            Assert.True(iQueryable.IsErrorType())
            Assert.Equal(0, iQueryable.Arity)

            Dim mdName As MetadataTypeName
            Dim t As NamedTypeSymbol

            Dim asm As AssemblySymbol = i2.ContainingAssembly

            mdName = MetadataTypeName.FromFullName("I3`1", False, -1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.False(t.IsErrorType())
            Assert.Equal("I3", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I3`1", False, 0)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.True(t.IsErrorType())
            Assert.Equal("I3`1", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I3`1", False, 1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.False(t.IsErrorType())
            Assert.Equal("I3", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I3`1", False, 2)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.True(t.IsErrorType())
            Assert.Equal("I3`1", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(2, t.Arity)

            mdName = MetadataTypeName.FromFullName("I3`1", True, -1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.False(t.IsErrorType())
            Assert.Equal("I3", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I3`1", True, 0)
            't = asm.LookupTopLevelMetadataType(mdName, True)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I3`1", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I3`1", True, 1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.False(t.IsErrorType())
            Assert.Equal("I3", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I3`1", True, 2)
            't = asm.LookupTopLevelMetadataType(mdName, True)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I3`1", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(2, t.Arity)


            mdName = MetadataTypeName.FromFullName("I", False, -1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.False(t.IsErrorType())
            Assert.Equal("I", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I", False, 0)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.False(t.IsErrorType())
            Assert.Equal("I", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I", False, 1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.True(t.IsErrorType())
            Assert.Equal("I", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I", True, -1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.False(t.IsErrorType())
            Assert.Equal("I", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I", True, 0)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.False(t.IsErrorType())
            Assert.Equal("I", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I", True, 1)
            't = asm.LookupTopLevelMetadataType(mdName, True)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(1, t.Arity)


            mdName = MetadataTypeName.FromFullName("I2`1", False, -1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.False(t.IsErrorType())
            Assert.Equal("I2`1", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I2`1", False, 0)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.False(t.IsErrorType())
            Assert.Equal("I2`1", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I2`1", False, 1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.True(t.IsErrorType())
            Assert.Equal("I2", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I2`1", False, 2)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.True(t.IsErrorType())
            Assert.Equal("I2`1", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(2, t.Arity)

            mdName = MetadataTypeName.FromFullName("I2`1", True, -1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.True(t.IsErrorType())
            Assert.Equal("I2", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I2`1", True, 0)
            't = asm.LookupTopLevelMetadataType(mdName, True)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I2`1", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I2`1", True, 1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.True(t.IsErrorType())
            Assert.Equal("I2", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I2`1", True, 2)
            't = asm.LookupTopLevelMetadataType(mdName, True)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I2`1", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(2, t.Arity)


            mdName = MetadataTypeName.FromFullName("I4`2", False, -1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.False(t.IsErrorType())
            Assert.Equal("I4`2", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I4`2", False, 0)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.True(t.IsErrorType())
            Assert.Equal("I4`2", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I4`2", False, 1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.False(t.IsErrorType())
            Assert.Equal("I4`2", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I4`2", False, 2)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.True(t.IsErrorType())
            Assert.Equal("I4", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(2, t.Arity)

            mdName = MetadataTypeName.FromFullName("I4`2", True, -1)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.True(t.IsErrorType())
            Assert.Equal("I4", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(2, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I4`2", True, 0)
            't = asm.LookupTopLevelMetadataType(mdName, True)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I4`2", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(0, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I4`2", True, 1)
            't = asm.LookupTopLevelMetadataType(mdName, True)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I4`2", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I4`2", True, 2)
            t = asm.LookupTopLevelMetadataType(mdName, True)
            Assert.True(t.IsErrorType())
            Assert.Equal("I4", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(2, t.Arity)

            Dim containingType As NamedTypeSymbol = compilation.GetTypeByMetadataName("I")

            mdName = MetadataTypeName.FromFullName("I3`1", False, -1)
            t = containingType.LookupMetadataType(mdName)
            Assert.False(t.IsErrorType())
            Assert.Equal("I3", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I3`1", False, 0)
            t = containingType.LookupMetadataType(mdName)
            Assert.True(t.IsErrorType())
            Assert.Equal("I3`1", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I3`1", False, 1)
            t = containingType.LookupMetadataType(mdName)
            Assert.False(t.IsErrorType())
            Assert.Equal("I3", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I3`1", False, 2)
            t = containingType.LookupMetadataType(mdName)
            Assert.True(t.IsErrorType())
            Assert.Equal("I3`1", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(2, t.Arity)

            mdName = MetadataTypeName.FromFullName("I3`1", True, -1)
            t = containingType.LookupMetadataType(mdName)
            Assert.False(t.IsErrorType())
            Assert.Equal("I3", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I3`1", True, 0)
            't = containingType.LookupMetadataType(mdName)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I3`1", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I3`1", True, 1)
            t = containingType.LookupMetadataType(mdName)
            Assert.False(t.IsErrorType())
            Assert.Equal("I3", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I3`1", True, 2)
            't = containingType.LookupMetadataType(mdName)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I3`1", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(2, t.Arity)


            mdName = MetadataTypeName.FromFullName("I", False, -1)
            t = containingType.LookupMetadataType(mdName)
            Assert.False(t.IsErrorType())
            Assert.Equal("I", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I", False, 0)
            t = containingType.LookupMetadataType(mdName)
            Assert.False(t.IsErrorType())
            Assert.Equal("I", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I", False, 1)
            t = containingType.LookupMetadataType(mdName)
            Assert.True(t.IsErrorType())
            Assert.Equal("I", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I", True, -1)
            t = containingType.LookupMetadataType(mdName)
            Assert.False(t.IsErrorType())
            Assert.Equal("I", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I", True, 0)
            t = containingType.LookupMetadataType(mdName)
            Assert.False(t.IsErrorType())
            Assert.Equal("I", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I", True, 1)
            't = containingType.LookupMetadataType(mdName)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(1, t.Arity)


            mdName = MetadataTypeName.FromFullName("I2`1", False, -1)
            t = containingType.LookupMetadataType(mdName)
            Assert.False(t.IsErrorType())
            Assert.Equal("I2`1", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I2`1", False, 0)
            t = containingType.LookupMetadataType(mdName)
            Assert.False(t.IsErrorType())
            Assert.Equal("I2`1", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I2`1", False, 1)
            t = containingType.LookupMetadataType(mdName)
            Assert.True(t.IsErrorType())
            Assert.Equal("I2", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I2`1", False, 2)
            t = containingType.LookupMetadataType(mdName)
            Assert.True(t.IsErrorType())
            Assert.Equal("I2`1", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(2, t.Arity)

            mdName = MetadataTypeName.FromFullName("I2`1", True, -1)
            t = containingType.LookupMetadataType(mdName)
            Assert.True(t.IsErrorType())
            Assert.Equal("I2", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I2`1", True, 0)
            't = containingType.LookupMetadataType(mdName)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I2`1", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I2`1", True, 1)
            t = containingType.LookupMetadataType(mdName)
            Assert.True(t.IsErrorType())
            Assert.Equal("I2", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(1, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I2`1", True, 2)
            't = containingType.LookupMetadataType(mdName)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I2`1", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(2, t.Arity)


            mdName = MetadataTypeName.FromFullName("I4`2", False, -1)
            t = containingType.LookupMetadataType(mdName)
            Assert.False(t.IsErrorType())
            Assert.Equal("I4`2", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I4`2", False, 0)
            t = containingType.LookupMetadataType(mdName)
            Assert.True(t.IsErrorType())
            Assert.Equal("I4`2", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(0, t.Arity)

            mdName = MetadataTypeName.FromFullName("I4`2", False, 1)
            t = containingType.LookupMetadataType(mdName)
            Assert.False(t.IsErrorType())
            Assert.Equal("I4`2", t.Name)
            Assert.False(t.MangleName)
            Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I4`2", False, 2)
            t = containingType.LookupMetadataType(mdName)
            Assert.True(t.IsErrorType())
            Assert.Equal("I4", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(2, t.Arity)

            mdName = MetadataTypeName.FromFullName("I4`2", True, -1)
            t = containingType.LookupMetadataType(mdName)
            Assert.True(t.IsErrorType())
            Assert.Equal("I4", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(2, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I4`2", True, 0)
            't = containingType.LookupMetadataType(mdName)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I4`2", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(0, t.Arity)

            'mdName = MetadataTypeName.FromFullName("I4`2", True, 1)
            't = containingType.LookupMetadataType(mdName)
            'Assert.True(t.IsErrorType())
            'Assert.Equal("I4`2", t.Name)
            'Assert.False(t.MangleName)
            'Assert.Equal(1, t.Arity)

            mdName = MetadataTypeName.FromFullName("I4`2", True, 2)
            t = containingType.LookupMetadataType(mdName)
            Assert.True(t.IsErrorType())
            Assert.Equal("I4", t.Name)
            Assert.True(t.MangleName)
            Assert.Equal(2, t.Arity)

            CompileAndVerify(compilation)
        End Sub

        <WorkItem(1066489)>
        <Fact>
        Public Sub InstanceIterator_ExplicitInterfaceImplementation_OldCSharpName()
            Dim ilSource = "
.class interface public abstract auto ansi I`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerable 
          F() cil managed
  {
  } // end of method I`1::F

} // end of class I`1

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
       implements class I`1<int32>
{
  .class auto ansi sealed nested private beforefieldinit '<I<System.Int32>'.'F>d__0'
         extends [mscorlib]System.Object
         implements class [mscorlib]System.Collections.Generic.IEnumerable`1<object>,
                    [mscorlib]System.Collections.IEnumerable,
                    class [mscorlib]System.Collections.Generic.IEnumerator`1<object>,
                    [mscorlib]System.Collections.IEnumerator,
                    [mscorlib]System.IDisposable
  {
    .field private object '<>2__current'
    .field private int32 '<>1__state'
    .field private int32 '<>l__initialThreadId'
    .field public class C '<>4__this'

    .method private hidebysig newslot virtual final 
            instance class [mscorlib]System.Collections.Generic.IEnumerator`1<object> 
            'System.Collections.Generic.IEnumerable<System.Object>.GetEnumerator'() cil managed
    {
      ldnull
      throw
    }

    .method private hidebysig newslot virtual final 
            instance class [mscorlib]System.Collections.IEnumerator 
            System.Collections.IEnumerable.GetEnumerator() cil managed
    {
      ldnull
      throw
    }

    .method private hidebysig newslot virtual final 
            instance bool  MoveNext() cil managed
    {
      ldnull
      throw
    }

    .method private hidebysig newslot specialname virtual final 
            instance object  'System.Collections.Generic.IEnumerator<System.Object>.get_Current'() cil managed
    {
      ldnull
      throw
    }

    .method private hidebysig newslot virtual final 
            instance void  System.Collections.IEnumerator.Reset() cil managed
    {
      ldnull
      throw
    }

    .method private hidebysig newslot virtual final 
            instance void  System.IDisposable.Dispose() cil managed
    {
      ldnull
      throw
    }

    .method private hidebysig newslot specialname virtual final 
            instance object  System.Collections.IEnumerator.get_Current() cil managed
    {
      ldnull
      throw
    }

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int32 '<>1__state') cil managed
    {
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    }

    .property instance object 'System.Collections.Generic.IEnumerator<System.Object>.Current'()
    {
      .get instance object C/'<I<System.Int32>'.'F>d__0'::'System.Collections.Generic.IEnumerator<System.Object>.get_Current'()
    }
    .property instance object System.Collections.IEnumerator.Current()
    {
      .get instance object C/'<I<System.Int32>'.'F>d__0'::System.Collections.IEnumerator.get_Current()
    }
  } // end of class '<I<System.Int32>'.'F>d__0'

  .method private hidebysig newslot virtual final 
          instance class [mscorlib]System.Collections.IEnumerable 
          'I<System.Int32>.F'() cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class C
"

            Dim source = "
Class D : Inherits C
End Class
"

            Dim comp = CreateCompilationWithCustomILSource(<compilation/>, ilSource)

            Dim stateMachineClass = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMembers().OfType(Of NamedTypeSymbol)().Single()
            Assert.Equal("<I<System.Int32>.F>d__0", stateMachineClass.Name) ' The name has been reconstructed correctly.
            Assert.Equal("C.<I<System.Int32>.F>d__0", stateMachineClass.ToTestDisplayString()) ' SymbolDisplay works.
            Assert.Equal(stateMachineClass, comp.GetTypeByMetadataName("C+<I<System.Int32>.F>d__0")) ' GetTypeByMetadataName works.
        End Sub

    End Class

End Namespace
