' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class TypeForwarders : Inherits BasicTestBase

        <Fact>
        Public Sub Test1()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                    {TestResources.SymbolsTests.TypeForwarders.TypeForwarder,
                                     TestResources.SymbolsTests.TypeForwarders.TypeForwarderLib,
                                     TestResources.SymbolsTests.TypeForwarders.TypeForwarderBase,
                                     TestMetadata.ResourcesNet40.mscorlib})

            TestTypeForwarderHelper(assemblies)
        End Sub

        Private Sub TestTypeForwarderHelper(assemblies() As AssemblySymbol)

            Dim module1 = DirectCast(assemblies(0).Modules(0), PEModuleSymbol)
            Dim module2 = DirectCast(assemblies(1).Modules(0), PEModuleSymbol)

            Dim assembly2 = DirectCast(assemblies(1), MetadataOrSourceAssemblySymbol)
            Dim assembly3 = DirectCast(assemblies(2), MetadataOrSourceAssemblySymbol)

            Dim derived1 = DirectCast(module1.GlobalNamespace.GetMembers("Derived").Single(), NamedTypeSymbol)
            Dim base1 = derived1.BaseType
            BaseTypeResolution.AssertBaseType(base1, "Base")

            Dim derived4 = DirectCast(module1.GlobalNamespace.GetMembers("GenericDerived").Single(), NamedTypeSymbol)
            Dim base4 = derived4.BaseType
            BaseTypeResolution.AssertBaseType(base4, "GenericBase(Of K)")

            Dim derived6 = DirectCast(module1.GlobalNamespace.GetMembers("GenericDerived1").Single(), NamedTypeSymbol)
            Dim base6 = derived6.BaseType
            BaseTypeResolution.AssertBaseType(base6, "GenericBase(Of K).NestedGenericBase(Of L)")


            Assert.Equal(assembly3, base1.ContainingAssembly)
            Assert.Equal(assembly3, base4.ContainingAssembly)
            Assert.Equal(assembly3, base6.ContainingAssembly)

            Assert.Equal(base1, module1.TypeRefHandleToTypeMap(CType(module1.Module.GetBaseTypeOfTypeOrThrow(DirectCast(derived1, PENamedTypeSymbol).Handle), TypeReferenceHandle)))
            Assert.True(module1.TypeRefHandleToTypeMap.Values.Contains(DirectCast(base4.OriginalDefinition, TypeSymbol)))
            Assert.True(module1.TypeRefHandleToTypeMap.Values.Contains(DirectCast(base6.OriginalDefinition, TypeSymbol)))

            Assert.Equal(base1, assembly2.CachedTypeByEmittedName(base1.ToTestDisplayString()))
            Assert.Equal(base4.OriginalDefinition, assembly2.CachedTypeByEmittedName("GenericBase`1"))
            Assert.Equal(2, assembly2.EmittedNameToTypeMapCount)

            Assert.Equal(base1, assembly3.CachedTypeByEmittedName(base1.ToTestDisplayString()))
            Assert.Equal(base4.OriginalDefinition, assembly3.CachedTypeByEmittedName("GenericBase`1"))
            Assert.Equal(2, assembly3.EmittedNameToTypeMapCount)


            Dim derived2 = DirectCast(module2.GlobalNamespace.GetMembers("Derived").Single(), NamedTypeSymbol)
            Dim base2 = derived2.BaseType
            BaseTypeResolution.AssertBaseType(base2, "Base")
            Assert.Same(base1, base2)

            Dim derived3 = DirectCast(module2.GlobalNamespace.GetMembers("GenericDerived").Single(), NamedTypeSymbol)
            Dim base3 = derived3.BaseType
            BaseTypeResolution.AssertBaseType(base3, "GenericBase(Of S)")

            Dim derived5 = DirectCast(module2.GlobalNamespace.GetMembers("GenericDerived1").Single(), NamedTypeSymbol)
            Dim base5 = derived5.BaseType
            BaseTypeResolution.AssertBaseType(base5, "GenericBase(Of S1).NestedGenericBase(Of S2)")

        End Sub

        <Fact>
        Public Sub TypeInNamespace()
            Dim comp = VisualBasicCompilation.Create("Dummy", references:={MscorlibRef, SystemCoreRef})

            Dim corlibAssembly = comp.GetReferencedAssemblySymbol(MscorlibRef)
            Assert.NotNull(corlibAssembly)
            Dim systemCoreAssembly = comp.GetReferencedAssemblySymbol(SystemCoreRef)
            Assert.NotNull(systemCoreAssembly)

            Const funcTypeMetadataName As String = "System.Func`1"

            ' mscorlib contains this type, so we should be able to find it without looking in referenced assemblies.
            Dim funcType = corlibAssembly.GetTypeByMetadataName(funcTypeMetadataName, includeReferences:=False, isWellKnownType:=True, conflicts:=Nothing)
            Assert.NotNull(funcType)
            Assert.NotEqual(TypeKind.Error, funcType.TypeKind)
            Assert.Equal(corlibAssembly, funcType.ContainingAssembly)

            ' System.Core forwards to mscorlib for System.Func`1.
            Assert.Equal(funcType, systemCoreAssembly.ResolveForwardedType(funcTypeMetadataName))

            ' The compilation assembly references both mscorlib and System.Core, but finding
            ' System.Func`1 in both isn't ambiguous because one forwards to the other.
            Assert.Equal(funcType, comp.Assembly.GetTypeByMetadataName(funcTypeMetadataName, includeReferences:=True, isWellKnownType:=True, conflicts:=Nothing))
        End Sub

        ''' <summary>
        ''' pe1 -> pe3; pe2 -> pe3
        ''' </summary>
        <Fact>
        Public Sub Diamond()
            Dim il1 = <![CDATA[
.assembly extern pe3 { }
.assembly pe1 { }

.class extern forwarder Base
{
  .assembly extern pe3
}
]]>
            Dim il2 = <![CDATA[
.assembly extern pe3 { }
.assembly pe2 { }

.class extern forwarder Base
{
  .assembly extern pe3
}
]]>
            Dim il3 = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly pe3 { }

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
]]>
            Dim vb =
<compilation name="TypeForwarders">
    <file name="a.vb">
Option Strict On

Class Derived 
    Inherits Base
End Class
        </file>
</compilation>

            Dim ref1 = CompileIL(il1.Value, prependDefaultHeader:=False)
            Dim ref2 = CompileIL(il2.Value, prependDefaultHeader:=False)
            Dim ref3 = CompileIL(il3.Value, prependDefaultHeader:=False)

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(vb, {ref1, ref2, ref3})

            Dim ilAssembly1 = compilation.GetReferencedAssemblySymbol(ref1)
            Assert.NotNull(ilAssembly1)
            Assert.Equal("pe1", ilAssembly1.Name)

            Dim ilAssembly2 = compilation.GetReferencedAssemblySymbol(ref2)
            Assert.NotNull(ilAssembly2)
            Assert.Equal("pe2", ilAssembly2.Name)

            Dim ilAssembly3 = compilation.GetReferencedAssemblySymbol(ref3)
            Assert.NotNull(ilAssembly3)
            Assert.Equal("pe3", ilAssembly3.Name)

            Dim baseType = ilAssembly3.GetTypeByMetadataName("Base")
            Assert.NotNull(baseType)
            Assert.False(baseType.IsErrorType())

            Assert.Equal(baseType, ilAssembly1.ResolveForwardedType("Base"))
            Assert.Equal(baseType, ilAssembly2.ResolveForwardedType("Base"))

            Dim derivedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Derived")
            Assert.Equal(baseType, derivedType.BaseType)

            ' All forwards resolve to the same type, so there's no issue.
            compilation.VerifyDiagnostics()
        End Sub

        ''' <summary>
        ''' pe1 -> pe2 -> pe1
        ''' </summary>
        <Fact>
        Public Sub Cycle1()
            Dim il1 = <![CDATA[
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Base
{
  .assembly extern pe2
}
]]>
            Dim il2 = <![CDATA[
.assembly extern pe1 { }
.assembly pe2 { }

.class extern forwarder Base
{
  .assembly extern pe1
}
]]>
            Dim vb =
<compilation name="TypeForwarders">
    <file name="a.vb">
Option Strict On

Class Derived 
    Inherits Base
End Class
        </file>
</compilation>

            Dim ref1 = CompileIL(il1.Value, prependDefaultHeader:=False)
            Dim ref2 = CompileIL(il2.Value, prependDefaultHeader:=False)

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(vb, {ref1, ref2})

            Dim ilAssembly1 = compilation.GetReferencedAssemblySymbol(ref1)
            Assert.NotNull(ilAssembly1)
            Assert.Equal("pe1", ilAssembly1.Name)

            Dim ilAssembly2 = compilation.GetReferencedAssemblySymbol(ref2)
            Assert.NotNull(ilAssembly2)
            Assert.Equal("pe2", ilAssembly2.Name)

            Assert.Null(ilAssembly1.GetTypeByMetadataName("Base"))
            Assert.Null(ilAssembly2.GetTypeByMetadataName("Base"))

            ' NOTE: the type isn't actually defined in any of the referenced assemblies,
            ' so lookup fails.
            ' CONSIDER: Dev10 actually reports ERR_AmbiguousInUnnamedNamespace1 instead of ERR_ForwardedTypeUnavailable3,
            ' but that seems less accurate.
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "Base").WithArguments("Base", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                Diagnostic(ERRID.ERR_TypeFwdCycle2, "Base").WithArguments("Base", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
        End Sub

        ''' <summary>
        ''' pe1 -> pe2 -> pe3 -> pe1
        ''' </summary>
        <Fact>
        Public Sub Cycle2()
            Dim il1 = <![CDATA[
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Base
{
  .assembly extern pe2
}
]]>
            Dim il2 = <![CDATA[
.assembly extern pe3 { }
.assembly pe2 { }

.class extern forwarder Base
{
  .assembly extern pe3
}
]]>
            Dim il3 = <![CDATA[
.assembly extern pe1 { }
.assembly pe3 { }

.class extern forwarder Base
{
  .assembly extern pe1
}
]]>
            Dim vb =
<compilation name="TypeForwarders">
    <file name="a.vb">
Option Strict On

Class Derived 
    Shared Sub Main()
        Dim b = new Base()
    End Sub
End Class
        </file>
</compilation>

            Dim ref1 = CompileIL(il1.Value, prependDefaultHeader:=False)
            Dim ref2 = CompileIL(il2.Value, prependDefaultHeader:=False)
            Dim ref3 = CompileIL(il3.Value, prependDefaultHeader:=False)

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(vb, {ref1, ref2, ref3})

            Dim ilAssembly1 = compilation.GetReferencedAssemblySymbol(ref1)
            Assert.NotNull(ilAssembly1)
            Assert.Equal("pe1", ilAssembly1.Name)

            Dim ilAssembly2 = compilation.GetReferencedAssemblySymbol(ref2)
            Assert.NotNull(ilAssembly2)
            Assert.Equal("pe2", ilAssembly2.Name)

            Dim ilAssembly3 = compilation.GetReferencedAssemblySymbol(ref3)
            Assert.NotNull(ilAssembly3)
            Assert.Equal("pe3", ilAssembly3.Name)

            Assert.Null(ilAssembly1.GetTypeByMetadataName("Base"))
            Assert.Null(ilAssembly2.GetTypeByMetadataName("Base"))
            Assert.Null(ilAssembly3.GetTypeByMetadataName("Base"))

            ' NOTE: the type isn't actually defined in any of the referenced assemblies,
            ' so lookup fails.
            ' CONSIDER: Dev10 actually reports ERR_AmbiguousInUnnamedNamespace1 instead of ERR_ForwardedTypeUnavailable3,
            ' but that seems less accurate.
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "Base").WithArguments("Base", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                Diagnostic(ERRID.ERR_TypeFwdCycle2, "Base").WithArguments("Base", "pe3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
        End Sub

        ''' <summary>
        ''' pe1 -> pe2 -> pe1; pe3 -> pe4
        ''' </summary>
        <Fact>
        Public Sub Cycle3()
            Dim il1 = <![CDATA[
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Base
{
  .assembly extern pe2
}
]]>
            Dim il2 = <![CDATA[
.assembly extern pe1 { }
.assembly pe2 { }

.class extern forwarder Base
{
  .assembly extern pe1
}
]]>
            Dim il3 = <![CDATA[
.assembly extern pe4 { }
.assembly pe3 { }

.class extern forwarder Base
{
  .assembly extern pe4
}
]]>
            Dim il4 = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly pe4 { }

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
]]>
            Dim vb =
<compilation name="TypeForwarders">
    <file name="a.vb">
Option Strict On

Class Derived 
    Inherits Base
End Class
        </file>
</compilation>

            Dim ref1 = CompileIL(il1.Value, prependDefaultHeader:=False)
            Dim ref2 = CompileIL(il2.Value, prependDefaultHeader:=False)
            Dim ref3 = CompileIL(il3.Value, prependDefaultHeader:=False)
            Dim ref4 = CompileIL(il4.Value, prependDefaultHeader:=False)

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(vb, {ref1, ref2, ref3, ref4})

            Dim ilAssembly1 = compilation.GetReferencedAssemblySymbol(ref1)
            Assert.NotNull(ilAssembly1)
            Assert.Equal("pe1", ilAssembly1.Name)

            Dim ilAssembly2 = compilation.GetReferencedAssemblySymbol(ref2)
            Assert.NotNull(ilAssembly2)
            Assert.Equal("pe2", ilAssembly2.Name)

            Dim ilAssembly3 = compilation.GetReferencedAssemblySymbol(ref3)
            Assert.NotNull(ilAssembly3)
            Assert.Equal("pe3", ilAssembly3.Name)

            Dim ilAssembly4 = compilation.GetReferencedAssemblySymbol(ref4)
            Assert.NotNull(ilAssembly4)
            Assert.Equal("pe4", ilAssembly4.Name)

            Dim baseType = ilAssembly4.GetTypeByMetadataName("Base")
            Assert.NotNull(baseType)
            Assert.False(baseType.IsErrorType())

            Assert.Null(ilAssembly1.GetTypeByMetadataName("Base"))
            Assert.True(ilAssembly1.ResolveForwardedType("Base").IsErrorType())
            Assert.Null(ilAssembly2.GetTypeByMetadataName("Base"))
            Assert.True(ilAssembly2.ResolveForwardedType("Base").IsErrorType())

            Assert.Equal(baseType, ilAssembly3.ResolveForwardedType("Base"))

            Dim derivedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Derived")
            Assert.Equal(baseType, derivedType.BaseType)

            ' Find the type even though there's a cycle.
            compilation.VerifyDiagnostics()
        End Sub

        ''' <summary>
        ''' pe1 -> pe2 -> pe1; pe3 depends upon the cyclic type.
        ''' </summary>
        ''' <remarks>
        ''' Only produced when the infinitely forwarded type is consumed via a metadata symbol
        ''' </remarks>
        <Fact>
        Public Sub ERR_TypeFwdCycle2()
            Dim il1 = <![CDATA[
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Cycle
{
  .assembly extern pe2
}
]]>
            Dim il2 = <![CDATA[
.assembly extern pe1 { }
.assembly pe2 { }

.class extern forwarder Cycle
{
  .assembly extern pe1
}
]]>
            Dim il3 = <![CDATA[
.assembly extern pe1 { }
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly pe3 { }

.class public auto ansi beforefieldinit UseSite
       extends [mscorlib]System.Object
{
  .method public hidebysig instance class [pe1]Cycle 
          Goo() cil managed
  {
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class Test
]]>
            Dim vb =
<compilation name="TypeForwarders">
    <file name="a.vb">
Option Strict On

Class Derived 
    Shared Sub Main()
        Dim us as New UseSite()
        us.Goo()
    End Sub
End Class
        </file>
</compilation>

            Dim ref1 = CompileIL(il1.Value, prependDefaultHeader:=False)
            Dim ref2 = CompileIL(il2.Value, prependDefaultHeader:=False)
            Dim ref3 = CompileIL(il3.Value, prependDefaultHeader:=False)

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(vb, {ref1, ref2, ref3})

            Dim ilAssembly1 = compilation.GetReferencedAssemblySymbol(ref1)
            Assert.NotNull(ilAssembly1)
            Assert.Equal("pe1", ilAssembly1.Name)

            Dim ilAssembly2 = compilation.GetReferencedAssemblySymbol(ref2)
            Assert.NotNull(ilAssembly2)
            Assert.Equal("pe2", ilAssembly2.Name)

            Dim ilAssembly3 = compilation.GetReferencedAssemblySymbol(ref3)
            Assert.NotNull(ilAssembly3)
            Assert.Equal("pe3", ilAssembly3.Name)

            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_TypeFwdCycle2, "us.Goo()").WithArguments("Cycle", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
        End Sub

        ''' <summary>
        ''' pe1 -> pe2 -> pe1
        ''' </summary>
        <Fact>
        Public Sub SpecialTypeCycle()
            Dim il1 = <![CDATA[
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder System.String
{
  .assembly extern pe2
}
]]>
            Dim il2 = <![CDATA[
.assembly extern pe1 { }
.assembly pe2 { }

.class extern forwarder System.String
{
  .assembly extern pe1
}
]]>
            Dim vb =
<compilation name="TypeForwarders">
    <file name="a.vb">
Option Strict On

Class Derived 
    Property P As System.String
        Get
            Return Nothing
        End Get
        Set(ByVal value As System.String)
        End Set
    End Property
End Class
        </file>
</compilation>

            Dim ref1 = CompileIL(il1.Value, prependDefaultHeader:=False)
            Dim ref2 = CompileIL(il2.Value, prependDefaultHeader:=False)

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(vb, {ref1, ref2})

            Dim ilAssembly1 = compilation.GetReferencedAssemblySymbol(ref1)
            Assert.NotNull(ilAssembly1)
            Assert.Equal("pe1", ilAssembly1.Name)

            Dim ilAssembly2 = compilation.GetReferencedAssemblySymbol(ref2)
            Assert.NotNull(ilAssembly2)
            Assert.Equal("pe2", ilAssembly2.Name)

            Assert.Null(ilAssembly1.GetTypeByMetadataName("System.String"))
            Assert.Null(ilAssembly2.GetTypeByMetadataName("System.String"))

            ' NOTE: We have a reference to the real System.String, so the cycle doesn't cause problems.
            compilation.VerifyDiagnostics()
        End Sub

        ''' <summary>
        ''' pe1 -> pe2.
        ''' </summary>
        <Fact>
        Public Sub Generic()
            Dim il1 = <![CDATA[
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Generic`1
{
  .assembly extern pe2
}
]]>
            Dim il2 = <![CDATA[
.assembly extern mscorlib { }
.assembly pe2 { }

.class public auto ansi beforefieldinit Generic`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class Generic`1
]]>
            Dim vb =
<compilation name="TypeForwarders">
    <file name="a.vb">
Option Strict On

Class Derived 
    Shared Sub Main()
        Dim g as new Generic(Of Integer)()
    End Sub
End Class
        </file>
</compilation>

            Dim ref1 = CompileIL(il1.Value, prependDefaultHeader:=False)
            Dim ref2 = CompileIL(il2.Value, prependDefaultHeader:=False)

            CreateCompilationWithMscorlib40AndReferences(vb, {ref1, ref2}).VerifyDiagnostics()
        End Sub

        ''' <summary>
        ''' pe1 -> pe2.
        ''' </summary>
        <Fact>
        Public Sub Nested()
            Dim il1 = <![CDATA[
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Outer
{
  .assembly extern pe2
}

.class extern Inner
{
  .class extern Outer
}
]]>
            Dim il2 = <![CDATA[
.assembly extern mscorlib { }
.assembly pe2 { }

.class public auto ansi beforefieldinit Outer
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Inner
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    }

  } // end of class Inner

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    }

} // end of class Outer
]]>
            Dim vb =
<compilation name="TypeForwarders">
    <file name="a.vb">
Option Strict On

Class Derived 
    Shared Sub Main()
        Dim o as new Outer()
        Dim i as new Outer.Inner()
    End Sub
End Class
        </file>
</compilation>

            Dim ref1 = CompileIL(il1.Value, prependDefaultHeader:=False)
            Dim ref2 = CompileIL(il2.Value, prependDefaultHeader:=False)

            CreateCompilationWithMscorlib40AndReferences(vb, {ref1, ref2}).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub LookupMissingForwardedType()
            Dim il1 = <![CDATA[
.assembly extern pe2 { }
.assembly extern mscorlib { }
.assembly pe1 { }

.class extern forwarder Outer
{
  .assembly extern pe2
}

.class extern Inner
{
  .class extern Outer
}

.class extern forwarder Generic`1
{
  .assembly extern pe2
}
]]>
            Dim vb =
<compilation name="TypeForwarders">
    <file name="a.vb">
Option Strict On

Class Test 
    ReadOnly Property P As Outer
        Get
            Return Nothing
        End Get
    End Property

    Function M() As Outer.Inner
        Return Nothing
    End Function

    Private F As Outer.Inner(Of String)

    Private G0 As Generic
    Private G1 As Generic(Of Integer)
    Private G2 As Generic(Of Integer, Integer)
End Class
        </file>
</compilation>

            Dim ref1 = CompileIL(il1.Value, prependDefaultHeader:=False)

            CreateCompilationWithMscorlib40AndReferences(vb, {ref1}).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "Outer").WithArguments("Outer", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "Outer").WithArguments("Outer", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "Outer").WithArguments("Outer", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                Diagnostic(ERRID.ERR_UndefinedType1, "Generic").WithArguments("Generic"),
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "Generic(Of Integer)").WithArguments("Generic", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                Diagnostic(ERRID.ERR_UndefinedType1, "Generic(Of Integer, Integer)").WithArguments("Generic"))
        End Sub

        <Fact>
        Public Sub LookupMissingForwardedTypeIgnoringCase()
            Dim il1 = <![CDATA[
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder UPPER
{
  .assembly extern pe2
}

.class extern forwarder lower.mIxEd
{
  .assembly extern pe2
}
]]>
            Dim vb =
<compilation name="TypeForwarders">
    <file name="a.vb">
Option Strict On

Class Test 
    Private F1 As upper
    Private F2 As uPPeR
    Private F3 As LOWER.mixed
    Private F4 As lOwEr.MIXED
End Class
        </file>
</compilation>

            Dim ref1 = CompileIL(il1.Value, prependDefaultHeader:=False)

            CreateCompilationWithMscorlib40AndReferences(vb, {ref1}).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "upper").WithArguments("upper", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "uPPeR").WithArguments("uPPeR", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "LOWER.mixed").WithArguments("LOWER.mixed", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "lOwEr.MIXED").WithArguments("lOwEr.MIXED", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
        End Sub

        <Fact>
        Public Sub NamespacesOnlyMentionedInForwarders()
            Dim il1 = <![CDATA[
.assembly extern pe2 { }
.assembly extern mscorlib { }
.assembly pe1 { }

.class extern forwarder T0
{
  .assembly extern pe2
}

.class extern forwarder Ns.T1
{
  .assembly extern pe2
}

.class extern forwarder Ns.Ms.T2
{
  .assembly extern pe2
}
]]>
            ' NOTE: case doesn't match
            Dim vb =
<compilation name="TypeForwarders">
    <file name="a.vb">
Option Strict On

Class Test 
    Private F0 As t0
    Private F1 As ns.t1
    Private F2 As ns.ms.t2
    Private F3 As ns.ms.ls.t3
            
    Private F4 As nope
    Private F5 As ns.nope
    Private F6 As ns.ms.nope
    Private F7 As ns.ms.ls.nope
End Class
        </file>
</compilation>

            Dim ref1 = CompileIL(il1.Value, prependDefaultHeader:=False)

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(vb, {ref1})

            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "t0").WithArguments("t0", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "ns.t1").WithArguments("ns.t1", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "ns.ms.t2").WithArguments("ns.ms.t2", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                Diagnostic(ERRID.ERR_UndefinedType1, "ns.ms.ls.t3").WithArguments("ns.ms.ls.t3"),
                Diagnostic(ERRID.ERR_UndefinedType1, "nope").WithArguments("nope"),
                Diagnostic(ERRID.ERR_UndefinedType1, "ns.nope").WithArguments("ns.nope"),
                Diagnostic(ERRID.ERR_UndefinedType1, "ns.ms.nope").WithArguments("ns.ms.nope"),
                Diagnostic(ERRID.ERR_UndefinedType1, "ns.ms.ls.nope").WithArguments("ns.ms.ls.nope"))

            Dim actualNamespaces = EnumerateNamespaces(compilation).Where(Function(ns) Not ns.StartsWith("System", StringComparison.Ordinal) AndAlso Not ns.StartsWith("Microsoft", StringComparison.Ordinal))
            Dim expectedNamespaces = {"Ns", "Ns.Ms"}
            Assert.True(actualNamespaces.SetEquals(expectedNamespaces, EqualityComparer(Of String).Default))
        End Sub

        <Fact>
        Public Sub NamespacesMentionedInForwarders()
            Dim il1 = <![CDATA[
.assembly extern pe2 { }
.assembly extern mscorlib { }
.assembly pe1 { }

.class extern forwarder N1.N2.N3.T
{
  .assembly extern pe2
}

.class public auto ansi beforefieldinit N1.N2.T
       extends [mscorlib]System.Object
{

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    }

} // end of class N1.N2.T
]]>
            ' NOTE: case doesn't match
            Dim vb =
<compilation name="TypeForwarders">
    <file name="a.vb">
Option Strict On

Namespace N1
    Class Test 
        Private T0 As n2.t
        Private T1 As n2.n3.t
        Private T2 As n1.n2.t
        Private T3 As n1.n2.n3.t
    End Class
End Namespace
        </file>
</compilation>

            Dim ref1 = CompileIL(il1.Value, prependDefaultHeader:=False)

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(vb, {ref1})

            ' TODO: it would be nice if we could report ERR_ForwardedTypeUnavailable3 in the first
            ' case as well (see DevDiv #14280).
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UndefinedType1, "n2.n3.t").WithArguments("n2.n3.t"),
                Diagnostic(ERRID.ERR_ForwardedTypeUnavailable3, "n1.n2.n3.t").WithArguments("n1.n2.n3.t", "TypeForwarders, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))

            Dim actualNamespaces = EnumerateNamespaces(compilation).Where(Function(ns) Not ns.StartsWith("System", StringComparison.Ordinal) AndAlso Not ns.StartsWith("Microsoft", StringComparison.Ordinal))
            Dim expectedNamespaces = {"N1", "N1.N2", "N1.N2.N3"}
            Assert.True(actualNamespaces.SetEquals(expectedNamespaces, EqualityComparer(Of String).Default))
        End Sub

        Private Shared Function EnumerateNamespaces(comp As VisualBasicCompilation) As IEnumerable(Of String)
            Return EnumerateNamespaces(comp.GlobalNamespace, "")
        End Function

        Private Shared Function EnumerateNamespaces([namespace] As NamespaceSymbol, baseName As String) As IEnumerable(Of String)
            Dim result As New List(Of String)
            For Each child In [namespace].GetNamespaceMembers()
                Dim childName = If(String.IsNullOrEmpty(baseName), child.Name, baseName + "." + child.Name)
                result.Add(childName)

                Dim seq = EnumerateNamespaces(child, childName)
                result.AddRange(seq)
            Next

            Return result
        End Function

        <Fact>
        Public Sub TypeForwardedToAttribute()
            Dim source =
<compilation name="TypeForwarders">
    <file name="a.vb">
&lt;Assembly: System.Runtime.CompilerServices.TypeForwardedTo(GetType(Forwarded))&gt;

Class Forwarded
End Class
        </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll)

            ' Attribute is erased. This is an intentional change in behavior by comparison to Dev12.
            Dim verifier = CompileAndVerify(compilation,
                                            sourceSymbolValidator:=Sub(moduleSymbol)
                                                                       Assert.Equal(1, moduleSymbol.ContainingAssembly.GetAttributes(AttributeDescription.TypeForwardedToAttribute).Count)
                                                                   End Sub,
                                            symbolValidator:=Sub(moduleSymbol)
                                                                 Assert.Equal(0, moduleSymbol.ContainingAssembly.GetAttributes(AttributeDescription.TypeForwardedToAttribute).Count)
                                                             End Sub)

            Using metadata = ModuleMetadata.CreateFromImage(verifier.EmittedAssemblyData)
                ' No entries in the ExportedType table.
                Dim peReader = metadata.Module.GetMetadataReader()
                Assert.Equal(0, peReader.GetTableRowCount(TableIndex.ExportedType))
            End Using
        End Sub

        <Fact>
        Public Sub TypeForwarderInAModule()

            Dim forwardedTypes =
<compilation name="ForwarderTargetAssembly">
    <file name="a.vb">
Public Class CF1
End Class
        </file>
</compilation>

            Dim forwardedTypesCompilation = CreateCompilationWithMscorlib40(forwardedTypes, options:=TestOptions.ReleaseDll)

            Dim netmod =
<compilation>
    <file name="a.vb"><![CDATA[
<assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(GetType(CF1))>
    ]]></file>
</compilation>

            Dim modCompilation = CreateCompilationWithMscorlib40AndReferences(netmod, {New VisualBasicCompilationReference(forwardedTypesCompilation)}, TestOptions.ReleaseModule)
            Dim modRef = modCompilation.EmitToImageReference()

            Dim app =
<compilation>
    <file name="a.vb"><![CDATA[
public class Test
End class
    ]]></file>
</compilation>

            Dim appCompilation = CreateCompilationWithMscorlib40AndReferences(app, {modRef}, TestOptions.ReleaseDll)

            Dim peModule = DirectCast(appCompilation.Assembly.Modules(1), PEModuleSymbol)
            Dim metadata = peModule.Module

            Dim metadataReader = metadata.GetMetadataReader()
            Assert.Equal(0, metadataReader.GetTableRowCount(TableIndex.ExportedType))

            Dim token As EntityHandle = metadata.GetTypeRef(metadata.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM")
            Assert.True(token.IsNil)

            CompileAndVerify(appCompilation,
                symbolValidator:=Sub(m)
                                     Dim peReader1 = DirectCast(m, PEModuleSymbol).Module.GetMetadataReader()
                                     Assert.Equal(0, peReader1.GetTableRowCount(TableIndex.ExportedType))
                                     Assert.Equal(0, m.ContainingAssembly.GetAttributes(AttributeDescription.TypeForwardedToAttribute).Count)
                                 End Sub
            ).VerifyDiagnostics()


            Dim ilSource1 =
            <![CDATA[
.assembly extern ForwarderTargetAssembly
{
  .ver 0:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.class extern forwarder CF1
{
  .assembly extern ForwarderTargetAssembly
}
.module mod.netmodule
// MVID: {C39224E6-1B32-4C27-98ED-8F2FE3CC5358}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00840000
]]>

            Dim ilBytes As ImmutableArray(Of Byte) = Nothing
            Using reference = IlasmUtilities.CreateTempAssembly(ilSource1.Value, prependDefaultHeader:=False)
                ilBytes = ReadFromFile(reference.Path)
            End Using

            Dim modRef1 = ModuleMetadata.CreateFromImage(ilBytes).GetReference()

            appCompilation = CreateCompilationWithMscorlib40AndReferences(app, {modRef1, New VisualBasicCompilationReference(forwardedTypesCompilation)}, TestOptions.ReleaseDll)

            Assert.Equal({"CF1"}, GetNamesOfForwardedTypes(appCompilation))

            peModule = DirectCast(appCompilation.Assembly.Modules(1), PEModuleSymbol)
            metadata = peModule.Module

            metadataReader = metadata.GetMetadataReader()
            Assert.Equal(1, metadataReader.GetTableRowCount(TableIndex.ExportedType))
            ValidateExportedTypeRow(metadataReader.ExportedTypes.First(), metadataReader, "CF1")

            token = metadata.GetTypeRef(metadata.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM")
            Assert.True(token.IsNil)   'could the type ref be located? If not then the attribute's not there.

            ' Exported types in .NET module cause PEVerify to fail.
            CompileAndVerify(appCompilation, verify:=Verification.FailsPEVerify,
                symbolValidator:=Sub(m)
                                     Dim metadataReader1 = DirectCast(m, PEModuleSymbol).Module.GetMetadataReader()
                                     Assert.Equal(1, metadataReader1.GetTableRowCount(TableIndex.ExportedType))
                                     ValidateExportedTypeRow(metadataReader1.ExportedTypes.First(), metadataReader1, "CF1")
                                     Assert.Equal({"CF1"}, GetNamesOfForwardedTypes(m.ContainingAssembly))

                                     ' Attributes should not actually be emitted.
                                     Assert.Equal(0, m.ContainingAssembly.GetAttributes(AttributeDescription.TypeForwardedToAttribute).Count())
                                 End Sub
            ).VerifyDiagnostics()

            Dim ilSource2 =
            <![CDATA[
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly extern Microsoft.VisualBasic
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 10:0:0:0
}
.assembly extern ForwarderTargetAssembly
{
  .ver 0:0:0:0
}
.module mod.netmodule
// MVID: {EFC6E215-2156-4ACE-A787-67C58990AEB5}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0002       // WINDOWS_GUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00980000

.custom ([mscorlib]System.Runtime.CompilerServices.AssemblyAttributesGoHereM) instance void [mscorlib]System.Runtime.CompilerServices.TypeForwardedToAttribute::.ctor(class [mscorlib]System.Type)
         = {type(class 'CF1, ForwarderTargetAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null')}
]]>

            Using reference = IlasmUtilities.CreateTempAssembly(ilSource2.Value, prependDefaultHeader:=False)
                ilBytes = ReadFromFile(reference.Path)
            End Using

            Dim modRef2 = ModuleMetadata.CreateFromImage(ilBytes).GetReference()

            appCompilation = CreateCompilationWithMscorlib40AndReferences(app, {modRef2, New VisualBasicCompilationReference(forwardedTypesCompilation)}, TestOptions.ReleaseDll)

            peModule = DirectCast(appCompilation.Assembly.Modules(1), PEModuleSymbol)
            metadata = peModule.Module

            metadataReader = metadata.GetMetadataReader()
            Assert.Equal(0, metadataReader.GetTableRowCount(TableIndex.ExportedType))

            token = metadata.GetTypeRef(metadata.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM")
            Assert.False(token.IsNil)   'could the type ref be located? If not then the attribute's not there.
            Assert.Equal(1, metadataReader.CustomAttributes.Count)

            CompileAndVerify(appCompilation,
                symbolValidator:=Sub(m)
                                     Dim metadataReader1 = DirectCast(m, PEModuleSymbol).Module.GetMetadataReader()
                                     Assert.Equal(0, metadataReader1.GetTableRowCount(TableIndex.ExportedType))

                                     ' Attributes should not actually be emitted.
                                     Assert.Equal(0, m.ContainingAssembly.GetAttributes(AttributeDescription.TypeForwardedToAttribute).Count())
                                 End Sub
            ).VerifyDiagnostics()

            appCompilation = CreateCompilationWithMscorlib40AndReferences(app, {modRef1, New VisualBasicCompilationReference(forwardedTypesCompilation)}, TestOptions.ReleaseModule)

            Dim appModule = ModuleMetadata.CreateFromImage(appCompilation.EmitToArray()).Module
            metadataReader = appModule.GetMetadataReader()
            Assert.Equal(0, metadataReader.GetTableRowCount(TableIndex.ExportedType))

            token = appModule.GetTypeRef(appModule.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM")
            Assert.True(token.IsNil)   'could the type ref be located? If not then the attribute's not there.

            appCompilation = CreateCompilationWithMscorlib40AndReferences(app, {modRef1}, TestOptions.ReleaseDll)

            AssertTheseDeclarationDiagnostics(appCompilation,
<expected>
BC30652: Reference required to assembly 'ForwarderTargetAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CF1'. Add one to your project.
</expected>)
        End Sub

        Private Shared Function GetNamesOfForwardedTypes(appCompilation As VisualBasicCompilation) As IEnumerable(Of String)
            Return GetNamesOfForwardedTypes(appCompilation.Assembly)
        End Function

        Private Shared Function GetNamesOfForwardedTypes(assembly As AssemblySymbol) As IEnumerable(Of String)
            Return DirectCast(assembly, IAssemblySymbol).GetForwardedTypes().Select(Function(t) t.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat))
        End Function

        Private Shared Sub ValidateExportedTypeRow(exportedTypeHandle As ExportedTypeHandle, reader As MetadataReader, expectedFullName As String)
            Dim exportedTypeRow As ExportedType = reader.GetExportedType(exportedTypeHandle)
            Dim split = expectedFullName.Split("."c)
            Dim numParts As Integer = split.Length
            Assert.InRange(numParts, 1, Integer.MaxValue)
            Dim expectedType = split(numParts - 1)
            Dim expectedNamespace = String.Join(".", split, 0, numParts - 1)

            If expectedFullName.Contains("+"c) Then
                Assert.Equal(0, exportedTypeRow.Attributes And TypeAttributesMissing.Forwarder)
                Assert.Equal(0, exportedTypeRow.GetTypeDefinitionId())
                Assert.Equal(expectedType.Split("+"c).Last(), reader.GetString(exportedTypeRow.Name)) 'Only the actual type name.
                Assert.Equal("", reader.GetString(exportedTypeRow.Namespace)) 'Empty - presumably there's enough info on the containing type.
                Assert.Equal(HandleKind.ExportedType, exportedTypeRow.Implementation.Kind)
            Else
                Assert.Equal(System.Reflection.TypeAttributes.NotPublic Or TypeAttributesMissing.Forwarder, exportedTypeRow.Attributes)
                Assert.Equal(0, exportedTypeRow.GetTypeDefinitionId())
                Assert.Equal(expectedType, reader.GetString(exportedTypeRow.Name))
                Assert.Equal(expectedNamespace, reader.GetString(exportedTypeRow.Namespace))
                Assert.Equal(HandleKind.AssemblyReference, exportedTypeRow.Implementation.Kind)
            End If
        End Sub

        <Fact>
        Public Sub TypeForwarderInAModule2()

            Dim forwardedTypes =
<compilation name="ForwarderTargetAssembly">
    <file name="a.vb">
Public Class CF1
    Public Class CF2
    End Class
    Private Class CF3
    End Class
End Class
        </file>
</compilation>

            Dim forwardedTypesCompilation = CreateCompilationWithMscorlib40(forwardedTypes, options:=TestOptions.ReleaseDll)

            Dim ilSource =
            <![CDATA[
.assembly extern ForwarderTargetAssembly
{
  .ver 0:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.class extern forwarder CF1
{
  .assembly extern ForwarderTargetAssembly
}
.module mod.netmodule
// MVID: {C39224E6-1B32-4C27-98ED-8F2FE3CC5358}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00840000
]]>

            Dim ilBytes As ImmutableArray(Of Byte) = Nothing
            Using reference = IlasmUtilities.CreateTempAssembly(ilSource.Value, prependDefaultHeader:=False)
                ilBytes = ReadFromFile(reference.Path)
            End Using

            Dim modRef = ModuleMetadata.CreateFromImage(ilBytes).GetReference()

            Dim app =
<compilation>
    <file name="a.vb"><![CDATA[
public class Test
End class
    ]]></file>
</compilation>

            Dim appCompilation = CreateCompilationWithMscorlib40AndReferences(app, {modRef, New VisualBasicCompilationReference(forwardedTypesCompilation)}, TestOptions.ReleaseDll)
            Assert.Equal({"CF1"}, GetNamesOfForwardedTypes(appCompilation))

            ' Exported types in .NET module cause PEVerify to fail.
            CompileAndVerify(appCompilation, verify:=Verification.FailsPEVerify,
                symbolValidator:=Sub(m)
                                     Dim peReader1 = DirectCast(m, PEModuleSymbol).Module.GetMetadataReader()
                                     Assert.Equal({"CF1"}, GetNamesOfForwardedTypes(m.ContainingAssembly))
                                     Assert.Equal(2, peReader1.GetTableRowCount(TableIndex.ExportedType))
                                     ValidateExportedTypeRow(peReader1.ExportedTypes.First(), peReader1, "CF1")
                                     ValidateExportedTypeRow(peReader1.ExportedTypes(1), peReader1, "CF1+CF2")
                                 End Sub
            ).VerifyDiagnostics()

        End Sub

        <Fact>
        Public Sub MetadataTypeReferenceResolutionThroughATypeForwardedByCompilation()
            Dim cA_v1 = CreateCompilationWithMscorlib40(
<compilation name="A">
    <file name="a.vb"><![CDATA[
public class Forwarded(Of T)
End class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll)

            Dim cB = CreateCompilationWithMscorlib40AndReferences(
<compilation name="B">
    <file name="a.vb"><![CDATA[
Public Class B 
    Inherits Forwarded(Of Integer)
End class
    ]]></file>
</compilation>, {New VisualBasicCompilationReference(cA_v1)}, TestOptions.ReleaseDll)

            Dim cB_ImageRef = cB.EmitToImageReference()

            Dim cC_v1 = CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb"><![CDATA[
public class Forwarded(Of T)
End class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll)

            Dim cC_v1_ImageRef = cC_v1.EmitToImageReference()

            Dim cA_v3 = CreateCompilationWithMscorlib40AndReferences(
<compilation name="A">
    <file name="a.vb"><![CDATA[
    ]]></file>
</compilation>, {ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.TypeForwarders.Forwarded).GetReference(),
                 New VisualBasicCompilationReference(cC_v1)},
                TestOptions.ReleaseDll)


            Dim cC_v2 = CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb"><![CDATA[
public class Forwarded(Of T)
End class
    ]]></file>
</compilation>, options:=TestOptions.ReleaseDll)

            Dim ref1 = New MetadataReference() {
                New VisualBasicCompilationReference(cA_v3)
            }

            Dim ref2 = New MetadataReference() {
                New VisualBasicCompilationReference(cB),
                cB_ImageRef
            }

            Dim ref3 = New MetadataReference() {
                New VisualBasicCompilationReference(cC_v1),
                New VisualBasicCompilationReference(cC_v2),
                cC_v1_ImageRef
            }

            For Each r1 In ref1
                For Each r2 In ref2
                    For Each r3 In ref3
                        Dim context = CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
    ]]></file>
</compilation>, {r1, r2, r3}, TestOptions.ReleaseDll)

                        Dim forwarded = context.GetTypeByMetadataName("Forwarded`1")
                        Dim resolved = context.GetTypeByMetadataName("B").BaseType.OriginalDefinition

                        Assert.NotNull(forwarded)
                        Assert.False(resolved.IsErrorType())
                        Assert.Same(forwarded, resolved)
                    Next
                Next
            Next
        End Sub

    End Class

End Namespace
