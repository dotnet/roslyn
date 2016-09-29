' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata

    Public Class MetadataMemberTests
        Inherits BasicTestBase

        Private ReadOnly _VTableGapClassIL As String = <![CDATA[
.class public auto ansi beforefieldinit Class
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  _VtblGap1_1() cil managed
  {
    ret
  }

  .method public hidebysig specialname instance int32 
          _VtblGap2_1() cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          set_GetterIsGap(int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname instance int32 
          get_SetterIsGap() cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          _VtblGap3_1(int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname instance int32 
          _VtblGap4_1() cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          _VtblGap5_1(int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ret
  }

  .property instance int32 GetterIsGap()
  {
    .get instance int32 Class::_VtblGap2_1()
    .set instance void Class::set_GetterIsGap(int32)
  } // end of property Class::GetterIsGap
  .property instance int32 SetterIsGap()
  {
    .get instance int32 Class::get_SetterIsGap()
    .set instance void Class::_VtblGap3_1(int32)
  } // end of property Class::SetterIsGap
  .property instance int32 BothAccessorsAreGaps()
  {
    .get instance int32 Class::_VtblGap4_1()
    .set instance void Class::_VtblGap5_1(int32)
  } // end of property Class::BothAccessorsAreGaps
} // end of class Class
]]>.Value

        Private ReadOnly _VTableGapInterfaceIL As String = <![CDATA[
.class interface public abstract auto ansi Interface
{
  .method public hidebysig newslot specialname rtspecialname abstract virtual 
          instance void  _VtblGap1_1() cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  _VtblGap2_1() cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_GetterIsGap(int32 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_SetterIsGap() cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  _VtblGap3_1(int32 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  _VtblGap4_1() cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  _VtblGap5_1(int32 'value') cil managed
  {
  }

  .property instance int32 GetterIsGap()
  {
    .get instance int32 Interface::_VtblGap2_1()
    .set instance void Interface::set_GetterIsGap(int32)
  } // end of property Interface::GetterIsGap
  .property instance int32 SetterIsGap()
  {
    .get instance int32 Interface::get_SetterIsGap()
    .set instance void Interface::_VtblGap3_1(int32)
  } // end of property Interface::SetterIsGap
  .property instance int32 BothAccessorsAreGaps()
  {
    .get instance int32 Interface::_VtblGap4_1()
    .set instance void Interface::_VtblGap5_1(int32)
  } // end of property Interface::BothAccessorsAreGaps
} // end of class Interface
]]>.Value

        <WorkItem(527152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527152")>
        <Fact>
        Public Sub MetadataMethodSymbolCtor01()
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
            Dim type1 = DirectCast(ns1.GetTypeMembers("StringComparer").Single(), NamedTypeSymbol)
            Dim ctors = type1.InstanceConstructors
            ' instance only
            Assert.Equal(1, ctors.Length())
            Dim ctor = DirectCast(ctors(0), MethodSymbol)

            Assert.Equal(type1, ctor.ContainingSymbol)
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, ctor.Name)
            Assert.Equal(SymbolKind.Method, ctor.Kind)
            Assert.Equal(MethodKind.Constructor, ctor.MethodKind)
            Assert.Equal(Accessibility.Protected, ctor.DeclaredAccessibility)

            Assert.True(ctor.IsDefinition)

            Assert.False(ctor.IsShared)
            Assert.False(ctor.IsNotOverridable)
            Assert.False(ctor.IsOverrides)
            Assert.True(ctor.IsOverloads)
            Assert.True(ctor.IsSub)

            Assert.Equal("Sub System.StringComparer." + WellKnownMemberNames.InstanceConstructorName + "()", ctor.ToTestDisplayString())
            Assert.Equal(0, ctor.TypeParameters.Length)
            Assert.Equal("Void", ctor.ReturnType.Name)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <WorkItem(537334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537334")>
        <Fact>
        Public Sub MetadataMethodSymbol01()
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
            Dim members = class1.GetMembers("StrongNameSignatureGeneration")
            ' 4 overloads
            Assert.Equal(4, members.Length())
            Dim member1 = DirectCast(members(3), MethodSymbol)

            Assert.Equal(mscorNS, member1.ContainingAssembly)
            Assert.Equal(class1, member1.ContainingSymbol)
            Assert.Equal(SymbolKind.Method, member1.Kind)
            ' Not Impl
            Assert.Equal(MethodKind.Ordinary, member1.MethodKind)
            Assert.Equal(Accessibility.Public, member1.DeclaredAccessibility)
            Assert.True(member1.IsDefinition)

            Assert.True(member1.IsShared)
            Assert.False(member1.IsMustOverride)
            Assert.False(member1.IsNotOverridable)
            Assert.False(member1.IsOverridable)
            Assert.False(member1.IsOverrides)
            Assert.True(member1.IsOverloads)
            Assert.False(member1.IsGenericMethod)
            Assert.False(member1.IsExternalMethod)
            ' Not Impl
            Assert.False(member1.IsExtensionMethod)
            Assert.False(member1.IsSub)
            Assert.False(member1.IsVararg)

            Dim fullName = "Function Microsoft.Runtime.Hosting.StrongNameHelpers.StrongNameSignatureGeneration(pwzFilePath As System.String, pwzKeyContainer As System.String, bKeyBlob As System.Byte(), cbKeyBlob As System.Int32, ByRef ppbSignatureBlob As System.IntPtr, ByRef pcbSignatureBlob As System.Int32) As System.Boolean"
            Assert.Equal(fullName, member1.ToTestDisplayString())
            Assert.Equal(0, member1.TypeArguments.Length)
            Assert.Equal(0, member1.TypeParameters.Length)
            Assert.Equal(6, member1.Parameters.Length)
            Assert.Equal("Boolean", member1.ReturnType.Name)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <WorkItem(527150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527150")>
        <WorkItem(537337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537337")>
        <Fact>
        Public Sub MetadataParameterSymbol01()
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
            Dim ns2 = DirectCast(DirectCast(ns1.GetMembers("Runtime").Single(), NamespaceSymbol).GetMembers("Hosting").Single(), NamespaceSymbol)

            Dim class1 = DirectCast(ns2.GetTypeMembers("StrongNameHelpers").First(), NamedTypeSymbol)
            Dim members = class1.GetMembers("StrongNameSignatureGeneration")
            Dim member1 = DirectCast(members(3), MethodSymbol)
            Assert.Equal(6, member1.Parameters.Length)
            Dim p1 = DirectCast(member1.Parameters(0), ParameterSymbol)
            Dim p2 = DirectCast(member1.Parameters(1), ParameterSymbol)
            Dim p3 = DirectCast(member1.Parameters(2), ParameterSymbol)
            Dim p4 = DirectCast(member1.Parameters(3), ParameterSymbol)
            Dim p5 = DirectCast(member1.Parameters(4), ParameterSymbol)
            Dim p6 = DirectCast(member1.Parameters(5), ParameterSymbol)

            Assert.Equal(mscorNS, p1.ContainingAssembly)
            Assert.Equal(class1, p1.ContainingType)
            Assert.Equal(member1, p1.ContainingSymbol)
            Assert.Equal(SymbolKind.Parameter, p1.Kind)
            Assert.Equal(Accessibility.NotApplicable, p1.DeclaredAccessibility) ' chk C#
            Assert.Equal("pwzFilePath", p1.Name)
            Dim fullName = " bKeyBlob As System.Byte(), cbKeyBlob As System.Int32, ppbSignatureBlob As ByRef System.IntPtr, pcbSignatureBlob As ByRef System.Int32) As System.Boolean"
            Assert.Equal("pwzKeyContainer As System.String", p2.ToTestDisplayString())
            Assert.Equal("String", p2.Type.Name)
            Assert.True(p2.IsDefinition)
            Assert.Equal("bKeyBlob As System.Byte()", p3.ToTestDisplayString())
            ' Bug - 2056
            Assert.Equal("System.Byte()", p3.Type.ToTestDisplayString())

            Assert.False(p1.IsShared)
            Assert.False(p1.IsMustOverride)
            Assert.False(p2.IsNotOverridable)
            Assert.False(p2.IsOverridable)
            Assert.False(p3.IsOverrides)
            ' Not Impl
            'Assert.False(p3.IsParamArray)
            Assert.False(p4.IsOptional)
            Assert.False(p4.HasExplicitDefaultValue)
            ' Not Impl
            'Assert.Null(p4.DefaultValue)

            Assert.Equal("ppbSignatureBlob", p5.Name)
            Assert.Equal("IntPtr", p5.Type.Name)
            Assert.True(p5.IsByRef)

            Assert.Equal("ByRef pcbSignatureBlob As System.Int32", p6.ToTestDisplayString())
            Assert.True(p6.IsByRef)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub MetadataMethodSymbolGen02()
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

            Dim type1 = DirectCast(ns2.GetTypeMembers("IDictionary").First(), NamedTypeSymbol)
            Dim member1 = DirectCast(type1.GetMembers("Add").Single(), MethodSymbol)
            Dim member2 = DirectCast(type1.GetMembers("TryGetValue").Single(), MethodSymbol)

            Assert.Equal(mscorNS, member1.ContainingAssembly)
            Assert.Equal(type1, member1.ContainingSymbol)
            Assert.Equal(SymbolKind.Method, member1.Kind)
            ' Not Impl
            'Assert.Equal(MethodKind.Ordinary, member2.MethodKind)
            Assert.Equal(Accessibility.Public, member2.DeclaredAccessibility)
            Assert.True(member2.IsDefinition)

            Assert.False(member1.IsShared)
            Assert.True(member1.IsMustOverride)
            Assert.False(member2.IsNotOverridable)
            Assert.False(member2.IsOverridable)
            Assert.False(member2.IsOverrides)
            ' Bug
            ' Assert.False(member1.IsOverloads)
            ' Assert.False(member2.IsOverloads)
            Assert.False(member1.IsGenericMethod)
            Assert.False(member1.IsExternalMethod)
            ' Not Impl
            'Assert.False(member1.IsExtensionMethod)
            Assert.True(member1.IsSub)
            Assert.False(member2.IsVararg)

            Assert.Equal(0, member1.TypeArguments.Length)
            Assert.Equal(0, member2.TypeParameters.Length)
            Assert.Equal(2, member1.Parameters.Length)
            Assert.Equal("Boolean", member2.ReturnType.Name)
            Assert.Equal("Function System.Collections.Generic.IDictionary(Of TKey, TValue).TryGetValue(key As TKey, ByRef value As TValue) As System.Boolean", member2.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <WorkItem(537335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537335")>
        <Fact>
        Public Sub MetadataParameterSymbolGen02()
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

            Dim type1 = DirectCast(ns2.GetTypeMembers("IDictionary").First(), NamedTypeSymbol)
            Dim member1 = DirectCast(type1.GetMembers("TryGetValue").Single(), MethodSymbol)
            Assert.Equal(2, member1.Parameters.Length)
            Dim p1 = DirectCast(member1.Parameters(0), ParameterSymbol)
            Dim p2 = DirectCast(member1.Parameters(1), ParameterSymbol)

            Assert.Equal(mscorNS, p1.ContainingAssembly)
            Assert.Equal(type1, p2.ContainingType)
            Assert.Equal(member1, p1.ContainingSymbol)
            Assert.Equal(SymbolKind.Parameter, p2.Kind)
            Assert.Equal(Accessibility.NotApplicable, p1.DeclaredAccessibility)
            Assert.Equal("value", p2.Name)
            Assert.Equal("key As TKey", p1.ToTestDisplayString())
            Assert.Equal("TValue", p2.Type.Name)
            Assert.True(p2.IsDefinition)

            Assert.False(p1.IsShared)
            Assert.False(p1.IsMustOverride)
            Assert.False(p2.IsNotOverridable)
            Assert.False(p2.IsOverridable)
            Assert.False(p1.IsOverrides)
            ' 2054
            Assert.False(p1.IsParamArray)
            Assert.False(p2.IsOptional)
            Assert.False(p2.HasExplicitDefaultValue)
            ' Not Impl - not in M2 scope
            'Assert.Null(p2.DefaultValue)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact()>
        Public Sub ImportConstantsWithIllegalConstantValues()

            Dim ilsource = <![CDATA[
// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit C2
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
  } 
}

  .class sequential ansi sealed public beforefieldinit C3
         extends [mscorlib]System.ValueType
  {
    .field public static int32 bar
    .method private specialname rtspecialname static 
            void  .cctor() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldc.i4.s   23
      IL_0002:  stsfld     int32 C3::bar
      IL_0007:  ret
    } // end of method foo::.cctor

  } // end of class foo

.class public auto ansi beforefieldinit C1
       extends [mscorlib]System.Object
{
  .field public static literal bool MyConstBoolean = "a string 01"
  .field public static literal char MyConstChar = "a string 02"
  .field public static literal int8 MyConstSByte = "a string 03"
  .field public static literal int16 MyConstInt16 = "a string 04"
  .field public static literal int32 MyConstInt32 = "a string 05"
  .field public static literal int64 MyConstInt64 = "a string 06"
  .field public static literal uint8 MyConstByte = "a string 07"
  .field public static literal uint16 MyConstUInt16 = "a string 08"
  .field public static literal uint32 MyConstUInt32 = "a string 09"
  .field public static literal uint64 MyConstUInt64 = "a string 10"
  .field public static literal float32 MyConstSingle = "a string 11"
  .field public static literal float64 MyConstDouble = "a string 12"
  .field public static initonly valuetype [mscorlib]System.Decimal MyConstDecimal
  .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 00 C0 28 6A 27 0C CB 08 00 00 )             // ....(j'.....  
  .field public static initonly valuetype [mscorlib]System.DateTime MyConstDateTime
  .custom instance void [mscorlib]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8,
                                                                                                  uint8,
                                                                                                  uint32,
                                                                                                  uint32,
                                                                                                  uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 E4 AC E5 00 
                                                                                                              00 00 ) 
  .field public static initonly valuetype [mscorlib]System.DateTime MyConstDateTime2
  .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 00 C0 28 6A 27 0C CB 08 00 00 )             // ....(j'.....  

  .field public static literal valuetype [mscorlib]System.Decimal MyConstDecimal2 = "booo!"
  .field public static literal valuetype [mscorlib]System.Decimal MyConstDecimal3 = nullref
  .field public static literal valuetype [mscorlib]System.DateTime MyConstDateTime3 = nullref
  .field public static literal string MyConstString2 = nullref
  .field public static literal char MyConstChar2 = nullref
  .field public static literal bool MyConstBoolean2 = nullref
  .field public static literal class C2 MyConstC2 = nullref
  .field public static literal valuetype C3 MyConstC3 = nullref
  .field public static literal string MyConstString = uint32(34)

  .method private specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       36 (0x24)
    .maxstack  8
    IL_0000:  ldc.i4     0xe5ace4
    IL_0005:  conv.i8
    IL_0006:  newobj     instance void [mscorlib]System.Decimal::.ctor(int64)
    IL_000b:  stsfld     valuetype [mscorlib]System.Decimal C1::MyConstDecimal
    IL_0010:  ldc.i8     0x8cb0c276a28c000
    IL_0019:  newobj     instance void [mscorlib]System.DateTime::.ctor(int64)
    IL_001e:  stsfld     valuetype [mscorlib]System.DateTime C1::MyConstDateTime
    IL_0020:  ldc.i8     0x8cb0c276a28c000
    IL_0021:  newobj     instance void [mscorlib]System.DateTime::.ctor(int64)
    IL_0022:  stsfld     valuetype [mscorlib]System.DateTime C1::MyConstDateTime2
    IL_0027:  ret
  } // end of method C1::.cctor

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C1::.ctor

} // end of class C1
                        ]]>.Value

            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Public Class C2
  Public Shared Sub DoStuff()
    Console.WriteLine(C1.MyConstBoolean)    
    Console.WriteLine(C1.MyConstChar)
    Console.WriteLine(C1.MyConstSByte)
    Console.WriteLine(C1.MyConstInt16)
    Console.WriteLine(C1.MyConstInt32)
    Console.WriteLine(C1.MyConstInt64)
    Console.WriteLine(C1.MyConstByte)
    Console.WriteLine(C1.MyConstUInt16)
    Console.WriteLine(C1.MyConstUInt32)
    Console.WriteLine(C1.MyConstUInt64)
    Console.WriteLine(C1.MyConstSingle)
    Console.WriteLine(C1.MyConstDouble)
    Console.WriteLine(C1.MyConstString)
    Console.WriteLine(C1.MyConstDecimal2) 
    Console.WriteLine(C1.MyConstC3)
  End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source,
                                                                  includeVbRuntime:=True,
                                                                  ilSource:=ilsource)

            AssertTheseDiagnostics(compilation,
<errors>
BC30799: Field 'C1.MyConstBoolean' has an invalid constant value.
    Console.WriteLine(C1.MyConstBoolean)    
                      ~~~~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstChar' has an invalid constant value.
    Console.WriteLine(C1.MyConstChar)
                      ~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstSByte' has an invalid constant value.
    Console.WriteLine(C1.MyConstSByte)
                      ~~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstInt16' has an invalid constant value.
    Console.WriteLine(C1.MyConstInt16)
                      ~~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstInt32' has an invalid constant value.
    Console.WriteLine(C1.MyConstInt32)
                      ~~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstInt64' has an invalid constant value.
    Console.WriteLine(C1.MyConstInt64)
                      ~~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstByte' has an invalid constant value.
    Console.WriteLine(C1.MyConstByte)
                      ~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstUInt16' has an invalid constant value.
    Console.WriteLine(C1.MyConstUInt16)
                      ~~~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstUInt32' has an invalid constant value.
    Console.WriteLine(C1.MyConstUInt32)
                      ~~~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstUInt64' has an invalid constant value.
    Console.WriteLine(C1.MyConstUInt64)
                      ~~~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstSingle' has an invalid constant value.
    Console.WriteLine(C1.MyConstSingle)
                      ~~~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstDouble' has an invalid constant value.
    Console.WriteLine(C1.MyConstDouble)
                      ~~~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstString' has an invalid constant value.
    Console.WriteLine(C1.MyConstString)
                      ~~~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstDecimal2' has an invalid constant value.
    Console.WriteLine(C1.MyConstDecimal2) 
                      ~~~~~~~~~~~~~~~~~~
BC30799: Field 'C1.MyConstC3' has an invalid constant value.
    Console.WriteLine(C1.MyConstC3)
                      ~~~~~~~~~~~~
</errors>)

            source =
<compilation>
    <file name="b.vb">
Imports System

Module Program
  Dim cul = System.Globalization.CultureInfo.InvariantCulture
  Public Sub Main()
    Console.WriteLine(if(C1.MyConstString2, "MyConstString2=nullref"))     
    Console.WriteLine(if(C1.MyConstChar2 = Char.MinValue,"\0",C1.MyConstChar2))
    Console.WriteLine(C1.MyConstBoolean2)
    Console.WriteLine(if(C1.MyConstC2, "MyConstC2=nullref"))     

    Console.WriteLine(C1.MyConstDateTime.ToString("M/d/yyyy h:mm:ss tt", cul)) 'BIND:"MyConstDateTime"
    Console.WriteLine(C1.MyConstDecimal)  'BIND1:"MyConstDecimal"
    Console.WriteLine(C1.MyConstDateTime2.ToString("M/d/yyyy h:mm:ss tt", cul)) 'BIND2:"MyConstDateTime2"
    Console.WriteLine(C1.MyConstDecimal3) 'BIND3:"MyConstDecimal3"
    Console.WriteLine(C1.MyConstDateTime3.ToString("M/d/yyyy h:mm:ss tt", cul)) 'BIND4:"MyConstDateTime3"
  End Sub
End Module
    </file>
</compilation>

            compilation = CreateCompilationWithCustomILSource(source,
                                                              includeVbRuntime:=True,
                                                              options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                                                              ilSource:=ilsource)

            CompileAndVerify(compilation, expectedOutput:=<![CDATA[
MyConstString2=nullref
\0
False
MyConstC2=nullref
11/4/2008 12:00:00 AM
15052004
11/4/2008 12:00:00 AM
0
1/1/0001 12:00:00 AM
]]>)

            Dim model = GetSemanticModel(compilation, "b.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "b.vb", 0)
            Assert.Equal(SyntaxKind.IdentifierName, expressionSyntax.Kind)
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "b.vb", 0)
            Dim symbol = DirectCast(semanticInfo.Symbol, FieldSymbol)
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Field, symbol.Kind)
            Assert.Equal("MyConstDateTime", symbol.Name)
            Assert.False(symbol.IsConst)

            expressionSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "b.vb", 1)
            Assert.Equal(SyntaxKind.IdentifierName, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "b.vb", 1)
            symbol = DirectCast(semanticInfo.Symbol, FieldSymbol)
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Field, symbol.Kind)
            Assert.Equal("MyConstDecimal", symbol.Name)
            Assert.False(symbol.IsConst)

            expressionSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "b.vb", 2)
            Assert.Equal(SyntaxKind.IdentifierName, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "b.vb", 2)
            symbol = DirectCast(semanticInfo.Symbol, FieldSymbol)
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Field, symbol.Kind)
            Assert.Equal("MyConstDateTime2", symbol.Name)
            Assert.True(symbol.IsConst)

            expressionSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "b.vb", 3)
            Assert.Equal(SyntaxKind.IdentifierName, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "b.vb", 3)
            symbol = DirectCast(semanticInfo.Symbol, FieldSymbol)
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Field, symbol.Kind)
            Assert.Equal("MyConstDecimal3", symbol.Name)
            Assert.True(symbol.IsConst)

            expressionSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "b.vb", 4)
            Assert.Equal(SyntaxKind.IdentifierName, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "b.vb", 4)
            symbol = DirectCast(semanticInfo.Symbol, FieldSymbol)
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Field, symbol.Kind)
            Assert.Equal("MyConstDateTime3", symbol.Name)
            Assert.True(symbol.IsConst)
        End Sub

        <Fact>
        Public Sub AccessorWithImportedGenericType()
            Dim comp0 = CreateCompilationWithMscorlib(
<compilation name="GT">
    <file name="a.vb">
Public Class MC(Of T)
End Class
public delegate Sub MD(Of T)(T t)
    </file>
</compilation>)

            Dim comp1 = CreateCompilationWithMscorlibAndReferences(
    <compilation name="MT">
        <file name="b.vb">
Public Class G(Of T)

    Public Property Prop As MC(Of T)
        Set(value As MC(Of T))

        End Set
        Get

        End Get
    End Property

    Public Custom Event E As MD(Of T)
        AddHandler(value As MD(Of T))
        End AddHandler

        RemoveHandler(value As MD(Of T))
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
    End Event
End Class
    </file>
    </compilation>, references:={New VisualBasicCompilationReference(comp0)})

            Dim mtdata = DirectCast(comp1, Compilation).EmitToArray(options:=New EmitOptions(metadataOnly:=True))
            Dim mtref = MetadataReference.CreateFromImage(mtdata)
            Dim comp2 = CreateCompilationWithMscorlibAndReferences(
    <compilation name="App">
        <file name="c.vb">
        </file>
    </compilation>, references:={mtref})

            Dim tsym = comp2.GetReferencedAssemblySymbol(mtref).GlobalNamespace.GetMember(Of NamedTypeSymbol)("G")
            Assert.NotNull(tsym)
            Dim mm = tsym.GetMembers()
            Dim mems = tsym.GetMembers().Where(Function(s) s.Kind = SymbolKind.Method)
            '
            Assert.Equal(5, mems.Count())
            For Each m As MethodSymbol In mems

                If m.MethodKind = MethodKind.Constructor Then
                    Continue For
                End If

                Assert.NotNull(m.AssociatedSymbol)
                Assert.NotEqual(MethodKind.Ordinary, m.MethodKind)
            Next

        End Sub

        ' TODO: Update this test if we decide to include gaps in the symbol table for NoPIA (DevDiv #17472).
        <Fact, WorkItem(546951, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546951")>
        Public Sub VTableGapsNotInSymbolTable()
            Dim vb = <compilation name="MT">
                         <file name="a.vb">
                         </file>
                     </compilation>
            Dim comp = CreateCompilationWithCustomILSource(vb, _VTableGapClassIL, includeVbRuntime:=True)
            comp.VerifyDiagnostics()

            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Class")
            AssertEx.None(type.GetMembersUnordered().AsEnumerable(), Function(symbol) symbol.Name.StartsWith("_VtblGap", StringComparison.Ordinal))

            ' Dropped entirely.
            Assert.Equal(0, type.GetMembers("_VtblGap1_1").Length)

            ' Dropped entirely, since both accessors are dropped.
            Assert.Equal(0, type.GetMembers("BothAccessorsAreGaps").Length)

            ' Getter is silently dropped, property appears valid and write-only.
            Dim propWithoutGetter = type.GetMember(Of PropertySymbol)("GetterIsGap")
            Assert.Null(propWithoutGetter.GetMethod)
            Assert.NotNull(propWithoutGetter.SetMethod)

            ' Setter is silently dropped, property appears valid and read-only.
            Dim propWithoutSetter = type.GetMember(Of PropertySymbol)("SetterIsGap")
            Assert.NotNull(propWithoutSetter.GetMethod)
            Assert.Null(propWithoutSetter.SetMethod)
        End Sub

        <Fact, WorkItem(546951, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546951")>
        Public Sub CallVTableGap()
            Dim vb = <compilation name="MT">
                         <file name="a.vb">
Module Test
    Sub Main()
        dim c as new [Class]()

        c._VtblGap1_1() ' CS1061

        dim x as Integer

        x = c.BothAccessorsAreGaps ' CS1061
        c.BothAccessorsAreGaps = x ' CS1061

        x = c.GetterIsGap ' CS0154
        c.GetterIsGap = x

        x = c.SetterIsGap
        c.SetterIsGap = x ' CS0200
    End Sub
End Module
    </file>
                     </compilation>

            ' BREAK: Dev11 produces no diagnostics, but the emitted code does not peverify.
            Dim comp = CreateCompilationWithCustomILSource(vb, _VTableGapClassIL, includeVbRuntime:=True)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_NameNotMember2, "c._VtblGap1_1").WithArguments("_VtblGap1_1", "[Class]"),
                Diagnostic(ERRID.ERR_NameNotMember2, "c.BothAccessorsAreGaps").WithArguments("BothAccessorsAreGaps", "[Class]"),
                Diagnostic(ERRID.ERR_NameNotMember2, "c.BothAccessorsAreGaps").WithArguments("BothAccessorsAreGaps", "[Class]"),
                Diagnostic(ERRID.ERR_NoGetProperty1, "c.GetterIsGap").WithArguments("GetterIsGap"),
                Diagnostic(ERRID.ERR_NoSetProperty1, "c.SetterIsGap = x").WithArguments("SetterIsGap"))
        End Sub

        <Fact, WorkItem(546951, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546951")>
        Public Sub ImplementVTableGap()
            Dim vb = <compilation name="MT">
                         <file name="a.vb">

Class Empty
    Implements [Interface]
End Class

Class Full
    Implements [Interface]

    Public Sub _VtblGap1_1() Implements [Interface]._VtblGap1_1
    End Sub

    Public Property GetterIsGap As Integer Implements [Interface].GetterIsGap
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Property SetterIsGap As Integer Implements [Interface].SetterIsGap
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Property BothAccessorsAreGaps As Integer Implements [Interface].BothAccessorsAreGaps
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
    </file>
                     </compilation>

            ' BREAK: Dev11 produces errors for the vtable gaps that Empty does not implement and not for the 
            ' vtable gaps that Full does implement.
            Dim comp = CreateCompilationWithCustomILSource(vb, _VTableGapInterfaceIL, includeVbRuntime:=True)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UnimplementedMember3, "[Interface]").WithArguments("Class", "Empty", "WriteOnly Property GetterIsGap As Integer", "[Interface]"),
                Diagnostic(ERRID.ERR_UnimplementedMember3, "[Interface]").WithArguments("Class", "Empty", "ReadOnly Property SetterIsGap As Integer", "[Interface]"),
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "[Interface]._VtblGap1_1").WithArguments("_VtblGap1_1", "_VtblGap1_1", "sub", "[Interface]"),
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "[Interface].BothAccessorsAreGaps").WithArguments("BothAccessorsAreGaps", "BothAccessorsAreGaps", "property", "[Interface]"))
        End Sub

        <Fact, WorkItem(1094411, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094411")>
        Public Sub Bug1094411_01()
            Dim source1 =
<compilation>
    <file name="a.vb">
Class Test
    Public F As Integer
    Property P As Integer
    Event E As System.Action
    Sub M()
    End Sub
End Class
    </file>
</compilation>

            Dim members = {"F", "P", "E", "M"}

            Dim comp1 = CreateCompilationWithMscorlib(source1, options:=TestOptions.ReleaseDll)

            Dim test1 = comp1.GetTypeByMetadataName("Test")
            Dim memberNames1 = New HashSet(Of String)(test1.MemberNames)

            For Each m In members
                Assert.True(memberNames1.Contains(m), m)
            Next

            Dim comp2 = CreateCompilationWithMscorlibAndReferences(<compilation><file name="a.vb"/></compilation>, {comp1.EmitToImageReference()})

            Dim test2 = comp2.GetTypeByMetadataName("Test")
            Dim memberNames2 = New HashSet(Of String)(test2.MemberNames)

            For Each m In members
                Assert.True(memberNames2.Contains(m), m)
            Next
        End Sub

        <Fact, WorkItem(1094411, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094411")>
        Public Sub Bug1094411_02()
            Dim source1 =
<compilation>
    <file name="a.vb">
Class Test
    Public F As Integer
    Property P As Integer
    Event E As System.Action
    Sub M()
    End Sub
End Class
    </file>
</compilation>

            Dim members = {"F", "P", "E", "M"}

            Dim comp1 = CreateCompilationWithMscorlib(source1, options:=TestOptions.ReleaseDll)

            Dim test1 = comp1.GetTypeByMetadataName("Test")
            test1.GetMembers()
            Dim memberNames1 = New HashSet(Of String)(test1.MemberNames)

            For Each m In members
                Assert.True(memberNames1.Contains(m), m)
            Next

            Dim comp2 = CreateCompilationWithMscorlibAndReferences(<compilation><file name="a.vb"/></compilation>, {comp1.EmitToImageReference()})

            Dim test2 = comp2.GetTypeByMetadataName("Test")
            test2.GetMembers()
            Dim memberNames2 = New HashSet(Of String)(test2.MemberNames)

            For Each m In members
                Assert.True(memberNames2.Contains(m), m)
            Next
        End Sub

    End Class

End Namespace
