// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class MetadataMemberTests : CSharpTestBase
    {
        private const string VTableGapClassIL = @"
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
";
        private const string VTableGapInterfaceIL = @"
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
";

        [WorkItem(537346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537346")]
        [Fact]
        public void MetadataMethodSymbolCtor01()
        {
            var text = "public class A {}";
            var compilation = CreateCompilation(text);

            var mscorlib = compilation.ExternalReferences[0];
            var mscorNS = compilation.GetReferencedAssemblySymbol(mscorlib);
            Assert.Equal(RuntimeCorLibName.Name, mscorNS.Name);
            Assert.Equal(SymbolKind.Assembly, mscorNS.Kind);
            var ns1 = mscorNS.GlobalNamespace.GetMembers("System").Single() as NamespaceSymbol;
            var type1 = ns1.GetTypeMembers("StringComparer").Single() as NamedTypeSymbol;
            var ctor = type1.InstanceConstructors.Single();

            Assert.Equal(type1, ctor.ContainingSymbol);
            Assert.Equal(WellKnownMemberNames.InstanceConstructorName, ctor.Name);
            Assert.Equal(SymbolKind.Method, ctor.Kind);
            Assert.Equal(MethodKind.Constructor, ctor.MethodKind);
            Assert.Equal(Accessibility.Protected, ctor.DeclaredAccessibility);
            Assert.True(ctor.IsDefinition);
            Assert.False(ctor.IsStatic);
            Assert.False(ctor.IsSealed);
            Assert.False(ctor.IsOverride);
            Assert.False(ctor.IsExtensionMethod);
            Assert.True(ctor.ReturnsVoid);
            Assert.False(ctor.IsVararg);
            // Bug - 2067
            Assert.Equal("System.StringComparer." + WellKnownMemberNames.InstanceConstructorName + "()", ctor.ToTestDisplayString());
            Assert.Equal(0, ctor.TypeParameters.Length);
            Assert.Equal("Void", ctor.ReturnType.Name);

            Assert.Empty(compilation.GetDeclarationDiagnostics());
        }

        [WorkItem(537345, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537345")]
        [Fact]
        public void MetadataMethodSymbol01()
        {
            var text = "public class A {}";
            var compilation = CreateEmptyCompilation(text, new[] { MscorlibRef });

            var mscorlib = compilation.ExternalReferences[0];
            var mscorNS = compilation.GetReferencedAssemblySymbol(mscorlib);
            Assert.Equal("mscorlib", mscorNS.Name);
            Assert.Equal(SymbolKind.Assembly, mscorNS.Kind);
            var ns1 = mscorNS.GlobalNamespace.GetMembers("Microsoft").Single() as NamespaceSymbol;
            var ns2 = ns1.GetMembers("Runtime").Single() as NamespaceSymbol;
            var ns3 = ns2.GetMembers("Hosting").Single() as NamespaceSymbol;

            var class1 = ns3.GetTypeMembers("StrongNameHelpers").First() as NamedTypeSymbol;
            var members = class1.GetMembers("StrongNameSignatureGeneration");
            // 4 overloads
            Assert.Equal(4, members.Length);
            var member1 = members.Last() as MethodSymbol;

            Assert.Equal(mscorNS, member1.ContainingAssembly);
            Assert.Equal(class1, member1.ContainingSymbol);
            Assert.Equal(SymbolKind.Method, member1.Kind);
            Assert.Equal(MethodKind.Ordinary, member1.MethodKind);
            Assert.Equal(Accessibility.Public, member1.DeclaredAccessibility);
            Assert.True(member1.IsDefinition);

            Assert.True(member1.IsStatic);
            Assert.False(member1.IsAbstract);
            Assert.False(member1.IsSealed);
            Assert.False(member1.IsVirtual);
            Assert.False(member1.IsOverride);
            // Bug -
            // Assert.True(member1.IsOverloads);
            Assert.False(member1.IsGenericMethod);
            // Not Impl
            // Assert.False(member1.IsExtensionMethod);
            Assert.False(member1.ReturnsVoid);
            Assert.False(member1.IsVararg);

            var fullName = "System.Boolean Microsoft.Runtime.Hosting.StrongNameHelpers.StrongNameSignatureGeneration(System.String pwzFilePath, System.String pwzKeyContainer, System.Byte[] bKeyBlob, System.Int32 cbKeyBlob, ref System.IntPtr ppbSignatureBlob, out System.Int32 pcbSignatureBlob)";
            Assert.Equal(fullName, member1.ToTestDisplayString());
            Assert.Equal(0, member1.TypeArgumentsWithAnnotations.Length);
            Assert.Equal(0, member1.TypeParameters.Length);
            Assert.Equal(6, member1.Parameters.Length);
            Assert.Equal("Boolean", member1.ReturnType.Name);

            Assert.Empty(compilation.GetDeclarationDiagnostics());
        }

        [WorkItem(527150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527150")]
        [WorkItem(527151, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527151")]
        [Fact]
        public void MetadataParameterSymbol01()
        {
            var text = "public class A {}";
            var compilation = CreateEmptyCompilation(text, new[] { MscorlibRef });

            var mscorlib = compilation.ExternalReferences[0];
            var mscorNS = compilation.GetReferencedAssemblySymbol(mscorlib);
            Assert.Equal("mscorlib", mscorNS.Name);
            Assert.Equal(SymbolKind.Assembly, mscorNS.Kind);
            var ns1 = mscorNS.GlobalNamespace.GetMembers("Microsoft").Single() as NamespaceSymbol;
            var ns2 = (ns1.GetMembers("Runtime").Single() as NamespaceSymbol).GetMembers("Hosting").Single() as NamespaceSymbol;

            var class1 = ns2.GetTypeMembers("StrongNameHelpers").First() as NamedTypeSymbol;
            var members = class1.GetMembers("StrongNameSignatureGeneration");
            var member1 = members.Last() as MethodSymbol;

            Assert.Equal(6, member1.Parameters.Length);
            var p1 = member1.Parameters[0] as ParameterSymbol;
            var p2 = member1.Parameters[1] as ParameterSymbol;
            var p3 = member1.Parameters[2] as ParameterSymbol;
            var p4 = member1.Parameters[3] as ParameterSymbol;
            var p5 = member1.Parameters[4] as ParameterSymbol;
            var p6 = member1.Parameters[5] as ParameterSymbol;

            Assert.Equal(mscorNS, p1.ContainingAssembly);
            Assert.Equal(class1, p1.ContainingType);
            Assert.Equal(member1, p1.ContainingSymbol);
            Assert.Equal(SymbolKind.Parameter, p1.Kind);
            Assert.Equal(Accessibility.NotApplicable, p1.DeclaredAccessibility);
            Assert.Equal("pwzFilePath", p1.Name);
            Assert.Equal("System.String pwzKeyContainer", p2.ToTestDisplayString());
            Assert.Equal("String", p2.Type.Name);
            Assert.True(p2.IsDefinition);
            Assert.Equal("System.Byte[] bKeyBlob", p3.ToTestDisplayString());
            Assert.Equal("System.Byte[]", p3.Type.ToTestDisplayString()); //array types do not have names - use ToTestDisplayString

            Assert.False(p1.IsStatic);
            Assert.False(p1.IsAbstract);
            Assert.False(p2.IsSealed);
            Assert.False(p2.IsVirtual);
            Assert.False(p3.IsOverride);
            Assert.False(p3.IsParams);
            Assert.False(p4.IsOptional);
            Assert.False(p4.HasExplicitDefaultValue);
            // Not Impl - out of scope
            // Assert.Null(p4.DefaultValue);

            Assert.Equal("ppbSignatureBlob", p5.Name);
            Assert.Equal("IntPtr", p5.Type.Name);
            Assert.Equal(RefKind.Ref, p5.RefKind);

            Assert.Equal("out System.Int32 pcbSignatureBlob", p6.ToTestDisplayString());
            Assert.Equal(RefKind.Out, p6.RefKind);

            Assert.Empty(compilation.GetDeclarationDiagnostics());
        }

        [Fact]
        public void MetadataMethodSymbolGen02()
        {
            var text = "public class A {}";
            var compilation = CreateCompilation(text);

            var mscorlib = compilation.ExternalReferences[0];
            var mscorNS = compilation.GetReferencedAssemblySymbol(mscorlib);
            var ns1 = (mscorNS.GlobalNamespace.GetMembers("System").Single() as NamespaceSymbol).GetMembers("Collections").Single() as NamespaceSymbol;
            var ns2 = ns1.GetMembers("Generic").Single() as NamespaceSymbol;

            var type1 = ns2.GetTypeMembers("IDictionary").First() as NamedTypeSymbol;
            var member1 = type1.GetMembers("Add").Single() as MethodSymbol;
            var member2 = type1.GetMembers("TryGetValue").Single() as MethodSymbol;

            Assert.Equal(mscorNS, member1.ContainingAssembly);
            Assert.Equal(type1, member1.ContainingSymbol);
            Assert.Equal(SymbolKind.Method, member1.Kind);
            // Not Impl
            //Assert.Equal(MethodKind.Ordinary, member2.MethodKind);
            Assert.Equal(Accessibility.Public, member2.DeclaredAccessibility);
            Assert.True(member2.IsDefinition);

            Assert.False(member1.IsStatic);
            Assert.True(member1.IsAbstract);
            Assert.False(member2.IsSealed);
            Assert.False(member2.IsVirtual);
            Assert.False(member2.IsOverride);
            //Assert.True(member1.IsOverloads); 
            //Assert.True(member2.IsOverloads); 
            Assert.False(member1.IsGenericMethod);
            // Not Impl
            //Assert.False(member1.IsExtensionMethod);
            Assert.True(member1.ReturnsVoid);
            Assert.False(member2.IsVararg);

            Assert.Equal(0, member1.TypeArgumentsWithAnnotations.Length);
            Assert.Equal(0, member2.TypeParameters.Length);
            Assert.Equal(2, member1.Parameters.Length);
            Assert.Equal("Boolean", member2.ReturnType.Name);
            Assert.Equal("System.Boolean System.Collections.Generic.IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)", member2.ToTestDisplayString());

            Assert.Empty(compilation.GetDeclarationDiagnostics());
        }

        [Fact]
        public void MetadataParameterSymbolGen02()
        {
            var text = "public class A {}";
            var compilation = CreateCompilation(text);

            var mscorlib = compilation.ExternalReferences[0];
            var mscorNS = compilation.GetReferencedAssemblySymbol(mscorlib);
            var ns1 = (mscorNS.GlobalNamespace.GetMembers("System").Single() as NamespaceSymbol).GetMembers("Collections").Single() as NamespaceSymbol;
            var ns2 = ns1.GetMembers("Generic").Single() as NamespaceSymbol;

            var type1 = ns2.GetTypeMembers("IDictionary").First() as NamedTypeSymbol;
            var member1 = type1.GetMembers("TryGetValue").Single() as MethodSymbol;
            Assert.Equal(2, member1.Parameters.Length);
            var p1 = member1.Parameters[0] as ParameterSymbol;
            var p2 = member1.Parameters[1] as ParameterSymbol;

            Assert.Equal(mscorNS, p1.ContainingAssembly);
            Assert.Equal(type1, p2.ContainingType);
            Assert.Equal(member1, p1.ContainingSymbol);
            Assert.Equal(SymbolKind.Parameter, p2.Kind);
            Assert.Equal(Accessibility.NotApplicable, p1.DeclaredAccessibility);
            Assert.Equal("value", p2.Name);
            Assert.Equal("TKey key", p1.ToTestDisplayString());
            // Empty
            // Assert.Equal("TValue", p2.Type.Name);
            Assert.True(p2.IsDefinition);

            Assert.False(p1.IsStatic);
            Assert.False(p1.IsAbstract);
            Assert.False(p2.IsSealed);
            Assert.False(p2.IsVirtual);
            Assert.False(p1.IsOverride);
            Assert.False(p1.IsExtern);
            Assert.False(p1.IsParams);
            Assert.False(p2.IsOptional);
            Assert.False(p2.HasExplicitDefaultValue);
            // Not Impl - Not in M2 scope
            // Assert.Null(p2.DefaultValue);

            Assert.Empty(compilation.GetDeclarationDiagnostics());
        }

        [WorkItem(537424, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537424")]
        [Fact]
        public void MetadataMethodStaticAndInstanceCtor()
        {
            var text = @"
class C
{
    C() { }
    static C() { }
}";
            var compilation = CreateCompilation(text);
            Assert.False(compilation.GetDiagnostics().Any());
            var classC = compilation.SourceModule.GlobalNamespace.GetTypeMembers("C").Single();
            // NamedTypeSymbol.Constructors only contains instance constructors
            Assert.Equal(1, classC.InstanceConstructors.Length);
            Assert.Equal(1, classC.GetMembers(WellKnownMemberNames.StaticConstructorName).Length);
        }

        [ClrOnlyFact]
        public void ImportDecimalConstantAttribute()
        {
            const string ilSource = @"
.class public C extends [mscorlib]System.Object
{
  .field public static initonly valuetype [mscorlib]System.Decimal MyDecimalTen
  .custom instance void [mscorlib]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8,
                                                                                                  uint8,
                                                                                                  uint32,
                                                                                                  uint32,
                                                                                                  uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 0A 00 00 00
                                                                                                              00 00 ) 
} // end of class C";
            const string cSharpSource = @"
class B {
  static void Main() {
    var x = C.MyDecimalTen;
    System.Console.Write(x);    
  }
}
";
            CompileWithCustomILSource(cSharpSource, ilSource, expectedOutput: "10");
        }

        [Fact]
        public void TypeAndNamespaceWithSameNameButDifferentArities()
        {
            var il = @"
.class interface public abstract auto ansi A.B.C
{
}

.class interface public abstract auto ansi A.B`1<T>
{
}
";
            var csharp = @"";

            var compilation = CreateCompilationWithILAndMscorlib40(csharp, il);

            var namespaceA = compilation.GlobalNamespace.GetMember<NamespaceSymbol>("A");

            var members = namespaceA.GetMembers("B");
            Assert.Equal(2, members.Length);
            Assert.NotNull(members[0]);
            Assert.NotNull(members[1]);
        }

        [Fact]
        public void TypeAndNamespaceWithSameNameAndArity()
        {
            var il = @"
.class interface public abstract auto ansi A.B.C
{
}

.class interface public abstract auto ansi A.B
{
}
";
            var csharp = @"";

            var compilation = CreateCompilationWithILAndMscorlib40(csharp, il);

            var namespaceA = compilation.GlobalNamespace.GetMember<NamespaceSymbol>("A");

            var members = namespaceA.GetMembers("B");
            Assert.Equal(2, members.Length);
            Assert.NotNull(members[0]);
            Assert.NotNull(members[1]);
        }

        // TODO: Update this test if we decide to include gaps in the symbol table for NoPIA (DevDiv #17472).
        [WorkItem(546951, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546951")]
        [Fact]
        public void VTableGapsNotInSymbolTable()
        {
            var csharp = @"";

            var comp = CreateCompilationWithILAndMscorlib40(csharp, VTableGapClassIL);
            comp.VerifyDiagnostics();

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Class");
            AssertEx.None(type.GetMembersUnordered(), symbol => symbol.Name.StartsWith("_VtblGap", StringComparison.Ordinal));

            // Dropped entirely.
            Assert.Equal(0, type.GetMembers("_VtblGap1_1").Length);

            // Dropped entirely, since both accessors are dropped.
            Assert.Equal(0, type.GetMembers("BothAccessorsAreGaps").Length);

            // Getter is silently dropped, property appears valid and write-only.
            var propWithoutGetter = type.GetMember<PropertySymbol>("GetterIsGap");
            Assert.Null(propWithoutGetter.GetMethod);
            Assert.NotNull(propWithoutGetter.SetMethod);
            Assert.False(propWithoutGetter.MustCallMethodsDirectly);

            // Setter is silently dropped, property appears valid and read-only.
            var propWithoutSetter = type.GetMember<PropertySymbol>("SetterIsGap");
            Assert.NotNull(propWithoutSetter.GetMethod);
            Assert.Null(propWithoutSetter.SetMethod);
            Assert.False(propWithoutSetter.MustCallMethodsDirectly);
        }

        [WorkItem(546951, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546951")]
        [Fact]
        public void CallVTableGap()
        {
            var csharp = @"
class Test
{
    static void Main()
    {
        Class c = new Class();

        c._VtblGap1_1(); // CS1061

        int x;

        x = c.BothAccessorsAreGaps; // CS1061
        c.BothAccessorsAreGaps = x; // CS1061

        x = c.GetterIsGap; // CS0154
        c.GetterIsGap = x;

        x = c.SetterIsGap;
        c.SetterIsGap = x; // CS0200
    }
}
";
            var comp = CreateCompilationWithILAndMscorlib40(csharp, VTableGapClassIL);
            comp.VerifyDiagnostics(
                // (8,11): error CS1061: 'Class' does not contain a definition for '_VtblGap1_1' and no extension method '_VtblGap1_1' accepting a first argument of type 'Class' could be found (are you missing a using directive or an assembly reference?)
                //         c._VtblGap1_1();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "_VtblGap1_1").WithArguments("Class", "_VtblGap1_1"),
                // (12,15): error CS1061: 'Class' does not contain a definition for 'BothAccessorsAreGaps' and no extension method 'BothAccessorsAreGaps' accepting a first argument of type 'Class' could be found (are you missing a using directive or an assembly reference?)
                //         x = c.BothAccessorsAreGaps;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "BothAccessorsAreGaps").WithArguments("Class", "BothAccessorsAreGaps"),
                // (13,11): error CS1061: 'Class' does not contain a definition for 'BothAccessorsAreGaps' and no extension method 'BothAccessorsAreGaps' accepting a first argument of type 'Class' could be found (are you missing a using directive or an assembly reference?)
                //         c.BothAccessorsAreGaps = x;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "BothAccessorsAreGaps").WithArguments("Class", "BothAccessorsAreGaps"),
                // (15,13): error CS0154: The property or indexer 'Class.GetterIsGap' cannot be used in this context because it lacks the get accessor
                //         x = c.GetterIsGap;
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "c.GetterIsGap").WithArguments("Class.GetterIsGap"),
                // (19,9): error CS0200: Property or indexer 'Class.SetterIsGap' cannot be assigned to -- it is read only
                //         c.SetterIsGap = x;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "c.SetterIsGap").WithArguments("Class.SetterIsGap"));
        }

        [WorkItem(546951, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546951")]
        [Fact]
        public void ImplementVTableGap()
        {
            var csharp = @"
class Empty : Interface
{
}

class Implicit : Interface
{
    public void _VtblGap1_1() { }
    public int GetterIsGap { get; set; }
    public int SetterIsGap { get; set; }
    public int BothAccessorsAreGaps { get; set; }
}

class Explicit : Interface
{
    void Interface._VtblGap1_1() { }
    int Interface.GetterIsGap { get; set; }
    int Interface.SetterIsGap { get; set; }
    int Interface.BothAccessorsAreGaps { get; set; }
}
";

            var comp = CreateCompilationWithILAndMscorlib40(csharp, VTableGapInterfaceIL);
            comp.VerifyDiagnostics(
                // (2,7): error CS0535: 'Empty' does not implement interface member 'Interface.SetterIsGap'
                // class Empty : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Empty", "Interface.SetterIsGap"),
                // (2,7): error CS0535: 'Empty' does not implement interface member 'Interface.GetterIsGap'
                // class Empty : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("Empty", "Interface.GetterIsGap"),
                // (17,33): error CS0550: 'Explicit.Interface.GetterIsGap.get' adds an accessor not found in interface member 'Interface.GetterIsGap'
                //     int Interface.GetterIsGap { get; set; }
                Diagnostic(ErrorCode.ERR_ExplicitPropertyAddingAccessor, "get").WithArguments("Explicit.Interface.GetterIsGap.get", "Interface.GetterIsGap"),
                // (18,38): error CS0550: 'Explicit.Interface.SetterIsGap.set' adds an accessor not found in interface member 'Interface.SetterIsGap'
                //     int Interface.SetterIsGap { get; set; }
                Diagnostic(ErrorCode.ERR_ExplicitPropertyAddingAccessor, "set").WithArguments("Explicit.Interface.SetterIsGap.set", "Interface.SetterIsGap"),
                // (19,19): error CS0539: 'Explicit.BothAccessorsAreGaps' in explicit interface declaration is not a member of interface
                //     int Interface.BothAccessorsAreGaps { get; set; }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "BothAccessorsAreGaps").WithArguments("Explicit.BothAccessorsAreGaps"),
                // (16,20): error CS0539: 'Explicit._VtblGap1_1()' in explicit interface declaration is not a member of interface
                //     void Interface._VtblGap1_1() { }
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "_VtblGap1_1").WithArguments("Explicit._VtblGap1_1()"));
        }

        [Fact, WorkItem(1094411, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094411")]
        public void Bug1094411_01()
        {
            var source1 =
@"
class Test
{
    public int F;
    public int P {get; set;}
    public event System.Action E
    {
        add { }
        remove { }
    }
    public void M() {}
}
";
            var members = new[] { "F", "P", "E", "M" };

            var comp1 = CreateCompilation(source1, options: TestOptions.ReleaseDll);

            var test1 = comp1.GetTypeByMetadataName("Test");
            var memberNames1 = new HashSet<string>(test1.MemberNames);

            foreach (var m in members)
            {
                Assert.True(memberNames1.Contains(m), m);
            }

            var comp2 = CreateCompilation("", new[] { comp1.EmitToImageReference() });

            var test2 = comp2.GetTypeByMetadataName("Test");
            var memberNames2 = new HashSet<string>(test2.MemberNames);

            foreach (var m in members)
            {
                Assert.True(memberNames2.Contains(m), m);
            }
        }

        [Fact, WorkItem(1094411, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094411")]
        public void Bug1094411_02()
        {
            var source1 =
@"
class Test
{
    public int F;
    public int P {get; set;}
    public event System.Action E
    {
        add { }
        remove { }
    }
    public void M() {}
}
";
            var members = new[] { "F", "P", "E", "M" };

            var comp1 = CreateCompilation(source1, options: TestOptions.ReleaseDll);

            var test1 = comp1.GetTypeByMetadataName("Test");
            test1.GetMembers();
            var memberNames1 = new HashSet<string>(test1.MemberNames);

            foreach (var m in members)
            {
                Assert.True(memberNames1.Contains(m), m);
            }

            var comp2 = CreateCompilation("", new[] { comp1.EmitToImageReference() });

            var test2 = comp2.GetTypeByMetadataName("Test");
            test2.GetMembers();
            var memberNames2 = new HashSet<string>(test2.MemberNames);

            foreach (var m in members)
            {
                Assert.True(memberNames2.Contains(m), m);
            }
        }

        [Fact]
        public void TestMetadataImportOptions_01()
        {
            var expectedDiagnostics = new[]
            {
                // error CS7088: Invalid 'MetadataImportOptions' value: '255'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("MetadataImportOptions", "255").WithLocation(1, 1)
            };

            var options = TestOptions.DebugDll;

            Assert.Equal(MetadataImportOptions.Public, options.MetadataImportOptions);
            options.VerifyErrors();
            options = options.WithMetadataImportOptions(MetadataImportOptions.Internal);
            Assert.Equal(MetadataImportOptions.Internal, options.MetadataImportOptions);
            options.VerifyErrors();
            options = options.WithMetadataImportOptions(MetadataImportOptions.All);
            Assert.Equal(MetadataImportOptions.All, options.MetadataImportOptions);
            options.VerifyErrors();
            options = options.WithMetadataImportOptions(MetadataImportOptions.Public);
            Assert.Equal(MetadataImportOptions.Public, options.MetadataImportOptions);
            options.VerifyErrors();
            options = options.WithMetadataImportOptions((MetadataImportOptions)byte.MaxValue);
            Assert.Equal((MetadataImportOptions)byte.MaxValue, options.MetadataImportOptions);
            options.VerifyErrors(expectedDiagnostics);

            var commonOptions = (CompilationOptions)options;

            commonOptions = commonOptions.WithMetadataImportOptions(MetadataImportOptions.Internal);
            Assert.Equal(MetadataImportOptions.Internal, ((CSharpCompilationOptions)commonOptions).MetadataImportOptions);
            ((CSharpCompilationOptions)commonOptions).VerifyErrors();
            commonOptions = commonOptions.WithMetadataImportOptions(MetadataImportOptions.All);
            Assert.Equal(MetadataImportOptions.All, ((CSharpCompilationOptions)commonOptions).MetadataImportOptions);
            ((CSharpCompilationOptions)commonOptions).VerifyErrors();
            commonOptions = commonOptions.WithMetadataImportOptions(MetadataImportOptions.Public);
            Assert.Equal(MetadataImportOptions.Public, ((CSharpCompilationOptions)commonOptions).MetadataImportOptions);
            ((CSharpCompilationOptions)commonOptions).VerifyErrors();
            commonOptions = commonOptions.WithMetadataImportOptions((MetadataImportOptions)byte.MaxValue);
            Assert.Equal((MetadataImportOptions)byte.MaxValue, ((CSharpCompilationOptions)commonOptions).MetadataImportOptions);
            ((CSharpCompilationOptions)commonOptions).VerifyErrors(expectedDiagnostics);

            var source = @"
public class C
{
    public int P1 {get; set;}
    internal int P2 {get; set;}
    private int P3 {get; set;}
}
";
            var compilation0 = CreateCompilation(source);

            options = TestOptions.DebugDll;
            var compilation = CreateCompilation("", options: options, references: new[] { compilation0.EmitToImageReference() });
            var c = compilation.GetTypeByMetadataName("C");
            Assert.NotEmpty(c.GetMembers("P1"));
            Assert.Empty(c.GetMembers("P2"));
            Assert.Empty(c.GetMembers("P3"));
            CompileAndVerify(compilation);

            compilation = compilation.WithOptions(options.WithMetadataImportOptions(MetadataImportOptions.Internal));
            c = compilation.GetTypeByMetadataName("C");
            Assert.NotEmpty(c.GetMembers("P1"));
            Assert.NotEmpty(c.GetMembers("P2"));
            Assert.Empty(c.GetMembers("P3"));
            CompileAndVerify(compilation);

            compilation = compilation.WithOptions(options.WithMetadataImportOptions(MetadataImportOptions.All));
            c = compilation.GetTypeByMetadataName("C");
            Assert.NotEmpty(c.GetMembers("P1"));
            Assert.NotEmpty(c.GetMembers("P2"));
            Assert.NotEmpty(c.GetMembers("P3"));
            CompileAndVerify(compilation);

            compilation = compilation.WithOptions(options.WithMetadataImportOptions((MetadataImportOptions)byte.MaxValue));
            c = compilation.GetTypeByMetadataName("C");
            Assert.NotEmpty(c.GetMembers("P1"));
            Assert.NotEmpty(c.GetMembers("P2"));
            Assert.Empty(c.GetMembers("P3"));
            compilation.VerifyEmitDiagnostics(expectedDiagnostics);
            compilation.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void TestMetadataImportOptions_02()
        {
            var expectedDiagnostics = new[]
            {
                // error CS7088: Invalid 'MetadataImportOptions' value: '255'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("MetadataImportOptions", "255").WithLocation(1, 1)
            };

            var options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.Internal);
            Assert.Equal(MetadataImportOptions.Internal, options.MetadataImportOptions);
            options.VerifyErrors();
            options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All);
            Assert.Equal(MetadataImportOptions.All, options.MetadataImportOptions);
            options.VerifyErrors();
            options = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.Public);
            Assert.Equal(MetadataImportOptions.Public, options.MetadataImportOptions);
            options.VerifyErrors();
            options = TestOptions.DebugDll.WithMetadataImportOptions((MetadataImportOptions)byte.MaxValue);
            Assert.Equal((MetadataImportOptions)byte.MaxValue, options.MetadataImportOptions);
            options.VerifyErrors(expectedDiagnostics);

            var source = @"
public class C
{
    public int P1 {get; set;}
    internal int P2 {get; set;}
    private int P3 {get; set;}
}
";
            var compilation0 = CreateCompilation(source);

            var compilation = CreateCompilation("", options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.Internal), references: new[] { compilation0.EmitToImageReference() });
            var c = compilation.GetTypeByMetadataName("C");
            Assert.NotEmpty(c.GetMembers("P1"));
            Assert.NotEmpty(c.GetMembers("P2"));
            Assert.Empty(c.GetMembers("P3"));
            CompileAndVerify(compilation);

            compilation = compilation.WithOptions(TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            c = compilation.GetTypeByMetadataName("C");
            Assert.NotEmpty(c.GetMembers("P1"));
            Assert.NotEmpty(c.GetMembers("P2"));
            Assert.NotEmpty(c.GetMembers("P3"));
            CompileAndVerify(compilation);

            compilation = compilation.WithOptions(TestOptions.DebugDll.WithMetadataImportOptions((MetadataImportOptions)byte.MaxValue));
            c = compilation.GetTypeByMetadataName("C");
            Assert.NotEmpty(c.GetMembers("P1"));
            Assert.NotEmpty(c.GetMembers("P2"));
            Assert.Empty(c.GetMembers("P3"));
            compilation.VerifyEmitDiagnostics(expectedDiagnostics);
            compilation.VerifyDiagnostics(expectedDiagnostics);
        }
    }
}
