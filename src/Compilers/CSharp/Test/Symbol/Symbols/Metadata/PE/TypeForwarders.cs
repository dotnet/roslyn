// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class TypeForwarders : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.TypeForwarders.TypeForwarder.dll,
                                        TestReferences.SymbolsTests.TypeForwarders.TypeForwarderLib.dll,
                                        TestReferences.SymbolsTests.TypeForwarders.TypeForwarderBase.dll,
                                        TestReferences.NetFx.v4_0_21006.mscorlib
                                    });

            TestTypeForwarderHelper(assemblies);
        }

        private void TestTypeForwarderHelper(AssemblySymbol[] assemblies)
        {
            var module1 = (PEModuleSymbol)assemblies[0].Modules[0];
            var module2 = (PEModuleSymbol)assemblies[1].Modules[0];

            var assembly2 = (MetadataOrSourceAssemblySymbol)assemblies[1];
            var assembly3 = (MetadataOrSourceAssemblySymbol)assemblies[2];

            var derived1 = module1.GlobalNamespace.GetTypeMembers("Derived").Single();
            var base1 = derived1.BaseType();
            BaseTypeResolution.AssertBaseType(base1, "Base");

            var derived4 = module1.GlobalNamespace.GetTypeMembers("GenericDerived").Single();
            var base4 = derived4.BaseType();
            BaseTypeResolution.AssertBaseType(base4, "GenericBase<K>");

            var derived6 = module1.GlobalNamespace.GetTypeMembers("GenericDerived1").Single();
            var base6 = derived6.BaseType();
            BaseTypeResolution.AssertBaseType(base6, "GenericBase<K>.NestedGenericBase<L>");

            Assert.Equal(assembly3, base1.ContainingAssembly);
            Assert.Equal(assembly3, base4.ContainingAssembly);
            Assert.Equal(assembly3, base6.ContainingAssembly);

            Assert.Equal(base1, module1.TypeRefHandleToTypeMap[(TypeReferenceHandle)module1.Module.GetBaseTypeOfTypeOrThrow(((PENamedTypeSymbol)derived1).Handle)]);
            Assert.True(module1.TypeRefHandleToTypeMap.Values.Contains((TypeSymbol)base4.OriginalDefinition));
            Assert.True(module1.TypeRefHandleToTypeMap.Values.Contains((TypeSymbol)base6.OriginalDefinition));

            Assert.Equal(base1, assembly2.CachedTypeByEmittedName(base1.ToTestDisplayString()));
            Assert.Equal(base4.OriginalDefinition, assembly2.CachedTypeByEmittedName("GenericBase`1"));
            Assert.Equal(2, assembly2.EmittedNameToTypeMapCount);

            Assert.Equal(base1, assembly3.CachedTypeByEmittedName(base1.ToTestDisplayString()));
            Assert.Equal(base4.OriginalDefinition, assembly3.CachedTypeByEmittedName("GenericBase`1"));
            Assert.Equal(2, assembly3.EmittedNameToTypeMapCount);

            var derived2 = module2.GlobalNamespace.GetTypeMembers("Derived").Single();
            var base2 = derived2.BaseType();
            BaseTypeResolution.AssertBaseType(base2, "Base");
            Assert.Same(base2, base1);

            var derived3 = module2.GlobalNamespace.GetTypeMembers("GenericDerived").Single();
            var base3 = derived3.BaseType();
            BaseTypeResolution.AssertBaseType(base3, "GenericBase<S>");

            var derived5 = module2.GlobalNamespace.GetTypeMembers("GenericDerived1").Single();
            var base5 = derived5.BaseType();
            BaseTypeResolution.AssertBaseType(base5, "GenericBase<S1>.NestedGenericBase<S2>");
        }

        [Fact]
        public void TypeInNamespace()
        {
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(new SyntaxTree[0]);

            var corlibAssembly = compilation.GetReferencedAssemblySymbol(MscorlibRef);
            Assert.NotNull(corlibAssembly);
            var systemCoreAssembly = compilation.GetReferencedAssemblySymbol(SystemCoreRef);
            Assert.NotNull(systemCoreAssembly);

            const string funcTypeMetadataName = "System.Func`1";

            // mscorlib contains this type, so we should be able to find it without looking in referenced assemblies.
            var funcType = corlibAssembly.GetTypeByMetadataName(funcTypeMetadataName, includeReferences: false, isWellKnownType: false, conflicts: out var _);
            Assert.NotNull(funcType);
            Assert.NotEqual(TypeKind.Error, funcType.TypeKind);
            Assert.Equal(corlibAssembly, funcType.ContainingAssembly);

            // System.Core forwards to mscorlib for System.Func`1.
            Assert.Equal(funcType, systemCoreAssembly.ResolveForwardedType(funcTypeMetadataName));

            // The compilation assembly references both mscorlib and System.Core, but finding
            // System.Func`1 in both isn't ambiguous because one forwards to the other.
            Assert.Equal(funcType, compilation.Assembly.GetTypeByMetadataName(funcTypeMetadataName, includeReferences: true, isWellKnownType: false, conflicts: out var _));
        }

        /// <summary>
        /// pe1 -> pe3; pe2 -> pe3
        /// </summary>
        [Fact]
        public void Diamond()
        {
            var il1 = @"
.assembly extern pe3 { }
.assembly pe1 { }

.class extern forwarder Base
{
  .assembly extern pe3
}
";

            var il2 = @"
.assembly extern pe3 { }
.assembly pe2 { }

.class extern forwarder Base
{
  .assembly extern pe3
}
";

            var il3 = @"
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
";

            var csharp = @"
class Derived : Base
{
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);
            var ref2 = CompileIL(il2, prependDefaultHeader: false);
            var ref3 = CompileIL(il3, prependDefaultHeader: false);

            var compilation = CreateCompilation(csharp, new[] { ref1, ref2, ref3 });

            var ilAssembly1 = compilation.GetReferencedAssemblySymbol(ref1);
            Assert.NotNull(ilAssembly1);
            Assert.Equal("pe1", ilAssembly1.Name);

            var ilAssembly2 = compilation.GetReferencedAssemblySymbol(ref2);
            Assert.NotNull(ilAssembly2);
            Assert.Equal("pe2", ilAssembly2.Name);

            var ilAssembly3 = compilation.GetReferencedAssemblySymbol(ref3);
            Assert.NotNull(ilAssembly3);
            Assert.Equal("pe3", ilAssembly3.Name);

            var baseType = ilAssembly3.GetTypeByMetadataName("Base");
            Assert.NotNull(baseType);
            Assert.False(baseType.IsErrorType());

            Assert.Equal(baseType, ilAssembly1.ResolveForwardedType("Base"));
            Assert.Equal(baseType, ilAssembly2.ResolveForwardedType("Base"));

            var derivedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            Assert.Equal(baseType, derivedType.BaseType());

            // All forwards resolve to the same type, so there's no issue.
            compilation.VerifyDiagnostics();
        }

        /// <summary>
        /// pe1 -> pe2 -> pe1
        /// </summary>
        [Fact]
        public void Cycle1()
        {
            var il1 = @"
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Base
{
  .assembly extern pe2
}
";

            var il2 = @"
.assembly extern pe1 { }
.assembly pe2 { }

.class extern forwarder Base
{
  .assembly extern pe1
}
";

            var csharp = @"
class Derived : Base
{
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);
            var ref2 = CompileIL(il2, prependDefaultHeader: false);

            var compilation = CreateCompilation(csharp, new[] { ref1, ref2 });

            var ilAssembly1 = compilation.GetReferencedAssemblySymbol(ref1);
            Assert.NotNull(ilAssembly1);
            Assert.Equal("pe1", ilAssembly1.Name);

            var ilAssembly2 = compilation.GetReferencedAssemblySymbol(ref2);
            Assert.NotNull(ilAssembly2);
            Assert.Equal("pe2", ilAssembly2.Name);

            Assert.Null(ilAssembly1.GetTypeByMetadataName("Base"));
            Assert.Null(ilAssembly2.GetTypeByMetadataName("Base"));

            // NOTE: the type isn't actually defined in any of the referenced assemblies,
            // so lookup fails.
            compilation.VerifyDiagnostics(
                // (2,17): error CS0731: The type forwarder for type 'Base' in assembly 'pe2' causes a cycle
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_CycleInTypeForwarder, "Base").WithArguments("Base", "pe2"),
                // (2,17): error CS1070: The type name 'Base' could not be found. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider adding a reference to that assembly.
                // class Derived : Base
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFoundFwd, "Base").WithArguments("Base", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        /// <summary>
        /// pe1 -> pe2 -> pe3 -> pe1
        /// </summary>
        [Fact]
        public void Cycle2()
        {
            var il1 = @"
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Base
{
  .assembly extern pe2
}
";

            var il2 = @"
.assembly extern pe3 { }
.assembly pe2 { }

.class extern forwarder Base
{
  .assembly extern pe3
}
";

            var il3 = @"
.assembly extern pe1 { }
.assembly pe3 { }

.class extern forwarder Base
{
  .assembly extern pe1
}
";

            var csharp = @"
class Test
{
    static void Main()
    {
        Base b = new Base();
    }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);
            var ref2 = CompileIL(il2, prependDefaultHeader: false);
            var ref3 = CompileIL(il3, prependDefaultHeader: false);

            var compilation = CreateCompilation(csharp, new[] { ref1, ref2, ref3 });

            var ilAssembly1 = compilation.GetReferencedAssemblySymbol(ref1);
            Assert.NotNull(ilAssembly1);
            Assert.Equal("pe1", ilAssembly1.Name);

            var ilAssembly2 = compilation.GetReferencedAssemblySymbol(ref2);
            Assert.NotNull(ilAssembly2);
            Assert.Equal("pe2", ilAssembly2.Name);

            var ilAssembly3 = compilation.GetReferencedAssemblySymbol(ref3);
            Assert.NotNull(ilAssembly3);
            Assert.Equal("pe3", ilAssembly3.Name);

            Assert.Null(ilAssembly1.GetTypeByMetadataName("Base"));
            Assert.Null(ilAssembly2.GetTypeByMetadataName("Base"));
            Assert.Null(ilAssembly3.GetTypeByMetadataName("Base"));

            // NOTE: the type isn't actually defined in any of the referenced assemblies,
            // so lookup fails.
            compilation.VerifyDiagnostics(
                // (6,9): error CS0731: The type forwarder for type 'Base' in assembly 'pe3' causes a cycle
                //         Base b = new Base();
                Diagnostic(ErrorCode.ERR_CycleInTypeForwarder, "Base").WithArguments("Base", "pe3"),
                // (6,9): error CS1070: The type name 'Base' could not be found. This type has been forwarded to assembly 'pe3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider adding a reference to that assembly.
                //         Base b = new Base();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFoundFwd, "Base").WithArguments("Base", "pe3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (6,22): error CS0731: The type forwarder for type 'Base' in assembly 'pe3' causes a cycle
                //         Base b = new Base();
                Diagnostic(ErrorCode.ERR_CycleInTypeForwarder, "Base").WithArguments("Base", "pe3"),
                // (6,22): error CS1070: The type name 'Base' could not be found. This type has been forwarded to assembly 'pe3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider adding a reference to that assembly.
                //         Base b = new Base();
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFoundFwd, "Base").WithArguments("Base", "pe3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        /// <summary>
        /// pe1 -> pe2 -> pe1; pe3 -> pe4
        /// </summary>
        [Fact]
        public void Cycle3()
        {
            var il1 = @"
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Base
{
  .assembly extern pe2
}
";

            var il2 = @"
.assembly extern pe1 { }
.assembly pe2 { }

.class extern forwarder Base
{
  .assembly extern pe1
}
";

            var il3 = @"
.assembly extern pe4 { }
.assembly pe3 { }

.class extern forwarder Base
{
  .assembly extern pe4
}
";

            var il4 = @"
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
";

            var csharp = @"
class Derived : Base
{
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);
            var ref2 = CompileIL(il2, prependDefaultHeader: false);
            var ref3 = CompileIL(il3, prependDefaultHeader: false);
            var ref4 = CompileIL(il4, prependDefaultHeader: false);

            var compilation = CreateCompilation(csharp, new[] { ref1, ref2, ref3, ref4 });

            var ilAssembly1 = compilation.GetReferencedAssemblySymbol(ref1);
            Assert.NotNull(ilAssembly1);
            Assert.Equal("pe1", ilAssembly1.Name);

            var ilAssembly2 = compilation.GetReferencedAssemblySymbol(ref2);
            Assert.NotNull(ilAssembly2);
            Assert.Equal("pe2", ilAssembly2.Name);

            var ilAssembly3 = compilation.GetReferencedAssemblySymbol(ref3);
            Assert.NotNull(ilAssembly3);
            Assert.Equal("pe3", ilAssembly3.Name);

            var ilAssembly4 = compilation.GetReferencedAssemblySymbol(ref4);
            Assert.NotNull(ilAssembly4);
            Assert.Equal("pe4", ilAssembly4.Name);

            var baseType = ilAssembly4.GetTypeByMetadataName("Base");
            Assert.NotNull(baseType);
            Assert.False(baseType.IsErrorType());

            Assert.Null(ilAssembly1.GetTypeByMetadataName("Base"));
            Assert.True(ilAssembly1.ResolveForwardedType("Base").IsErrorType());
            Assert.Null(ilAssembly2.GetTypeByMetadataName("Base"));
            Assert.True(ilAssembly2.ResolveForwardedType("Base").IsErrorType());

            Assert.Null(ilAssembly3.GetTypeByMetadataName("Base"));
            Assert.Equal(baseType, ilAssembly3.ResolveForwardedType("Base"));

            var derivedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            Assert.Equal(baseType, derivedType.BaseType());

            // Find the type even though there's a cycle.
            compilation.VerifyDiagnostics();
        }

        /// <summary>
        /// pe1 -> pe2 -> pe1; pe3 depends upon the cyclic type.
        /// </summary>
        /// <remarks>
        /// Only produced when the infinitely forwarded type is consumed via a metadata symbol
        /// (i.e. not if it appears in the signature of a source member).
        /// </remarks>
        [Fact]
        public void ERR_CycleInTypeForwarder()
        {
            var il1 = @"
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Cycle
{
  .assembly extern pe2
}
";

            var il2 = @"
.assembly extern pe1 { }
.assembly pe2 { }

.class extern forwarder Cycle
{
  .assembly extern pe1
}
";

            var il3 = @"
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
";

            var csharp = @"
class Test
{
    static void Main()
    {
        UseSite us = new UseSite();
        us.Goo();
    }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);
            var ref2 = CompileIL(il2, prependDefaultHeader: false);
            var ref3 = CompileIL(il3, prependDefaultHeader: false);

            var compilation = CreateCompilation(csharp, new[] { ref1, ref2, ref3 });

            var ilAssembly1 = compilation.GetReferencedAssemblySymbol(ref1);
            Assert.NotNull(ilAssembly1);
            Assert.Equal("pe1", ilAssembly1.Name);

            var ilAssembly2 = compilation.GetReferencedAssemblySymbol(ref2);
            Assert.NotNull(ilAssembly2);
            Assert.Equal("pe2", ilAssembly2.Name);

            var ilAssembly3 = compilation.GetReferencedAssemblySymbol(ref3);
            Assert.NotNull(ilAssembly3);
            Assert.Equal("pe3", ilAssembly3.Name);

            compilation.VerifyDiagnostics(
                // (7,9): error CS0731: The type forwarder for type 'Cycle' in assembly 'pe2' causes a cycle
                //         us.Goo();
                Diagnostic(ErrorCode.ERR_CycleInTypeForwarder, "us.Goo").WithArguments("Cycle", "pe2"));
        }

        /// <summary>
        /// pe1 -> pe2 -> pe1
        /// </summary>
        [Fact]
        public void SpecialTypeCycle()
        {
            var il1 = @"
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder System.String
{
  .assembly extern pe2
}
";

            var il2 = @"
.assembly extern pe1 { }
.assembly pe2 { }

.class extern forwarder System.String
{
  .assembly extern pe1
}
";

            var csharp = @"
class Derived
{
    System.String P { get; set; }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);
            var ref2 = CompileIL(il2, prependDefaultHeader: false);

            var compilation = CreateCompilation(csharp, new[] { ref1, ref2 });

            var ilAssembly1 = compilation.GetReferencedAssemblySymbol(ref1);
            Assert.NotNull(ilAssembly1);
            Assert.Equal("pe1", ilAssembly1.Name);

            var ilAssembly2 = compilation.GetReferencedAssemblySymbol(ref2);
            Assert.NotNull(ilAssembly2);
            Assert.Equal("pe2", ilAssembly2.Name);

            Assert.Null(ilAssembly1.GetTypeByMetadataName("System.String"));
            Assert.Null(ilAssembly2.GetTypeByMetadataName("System.String"));

            // NOTE: We have a reference to the real System.String, so the cycle doesn't cause problems.
            compilation.VerifyDiagnostics();
        }

        /// <summary>
        /// pe1 -> pe2.
        /// </summary>
        [Fact]
        public void Generic()
        {
            var il1 = @"
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Generic`1
{
  .assembly extern pe2
}
";

            var il2 = @"
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
";

            var csharp = @"
class Test
{
    static void Main()
    {
        Generic<int> g = new Generic<int>();
    }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);
            var ref2 = CompileIL(il2, prependDefaultHeader: false);

            CreateCompilation(csharp, new[] { ref1, ref2 }).VerifyDiagnostics();
        }

        /// <summary>
        /// pe1 -> pe2.
        /// </summary>
        [Fact]
        public void Nested()
        {
            var il1 = @"
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
";

            var il2 = @"
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
";

            var csharp = @"
class Test
{
    static void Main()
    {
        Outer outer = new Outer();
        Outer.Inner inner = new Outer.Inner();
    }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);
            var ref2 = CompileIL(il2, prependDefaultHeader: false);

            CreateCompilation(csharp, new[] { ref1, ref2 }).VerifyDiagnostics();
        }

        [Fact]
        public void ForwardToMissingAssembly()
        {
            var il1 = @"
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Base
{
  .assembly extern pe2
}

.class public auto ansi beforefieldinit Derived
       extends [pe2]Base
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [pe2]Base::.ctor()
    ret
  }
} // end of class Derived
";

            var il2 = @"
.assembly extern pe3 { }
.assembly pe2 { }

.class extern forwarder Base
{
  .assembly extern pe3
}
";

            var csharp = @"
class Test : Derived
{
    static void Main()
    {
    }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);
            var ref2 = CompileIL(il2, prependDefaultHeader: false);

            // NOTE: not referring to pe3, even though pe2 forwards there.
            var comp3 = CreateCompilation(csharp, new[] { ref1, ref2 });
            comp3.VerifyDiagnostics(
                // (2,7): error CS0012: The type 'Base' is defined in an assembly that is not referenced. You must add a reference to assembly 'pe3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // class Test : Derived
                Diagnostic(ErrorCode.ERR_NoTypeDef, "Derived").WithArguments("Base", "pe3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void LookupMissingForwardedType()
        {
            var il1 = @"
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
";

            var csharp = @"
class Test
{
    Outer P { get; set; }
    Outer.Inner M() { return null; }
    Outer.Inner<string> F;

    Generic G0 { get; set; }
    Generic<int> G1 { get; set; }
    Generic<int, int> G2 { get; set; }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);

            CreateCompilation(csharp, new[] { ref1 }).VerifyDiagnostics(
                // (5,5): error CS1070: The type name 'Outer' could not be found. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider adding a reference to that assembly.
                //     Outer.Inner M() { return null; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFoundFwd, "Outer").WithArguments("Outer", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(5, 5),
                // (8,5): error CS0246: The type or namespace name 'Generic' could not be found (are you missing a using directive or an assembly reference?)
                //     Generic G0 { get; set; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Generic").WithArguments("Generic").WithLocation(8, 5),
                // (9,5): error CS1070: The type name 'Generic<>' could not be found. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider adding a reference to that assembly.
                //     Generic<int> G1 { get; set; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFoundFwd, "Generic<int>").WithArguments("Generic<>", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(9, 5),
                // (10,5): error CS0246: The type or namespace name 'Generic<,>' could not be found (are you missing a using directive or an assembly reference?)
                //     Generic<int, int> G2 { get; set; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Generic<int, int>").WithArguments("Generic<,>").WithLocation(10, 5),
                // (4,5): error CS1070: The type name 'Outer' could not be found. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider adding a reference to that assembly.
                //     Outer P { get; set; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFoundFwd, "Outer").WithArguments("Outer", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 5),
                // (6,5): error CS1070: The type name 'Outer' could not be found. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider adding a reference to that assembly.
                //     Outer.Inner<string> F;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFoundFwd, "Outer").WithArguments("Outer", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 5),
                // (6,25): warning CS0169: The field 'Test.F' is never used
                //     Outer.Inner<string> F;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("Test.F").WithLocation(6, 25)
                );
        }

        [Fact]
        public void LookupMissingForwardedTypeWrongCase()
        {
            var il1 = @"
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
";

            var csharp = @"
class Test
{
    upper P1 { get; set; }
    uPPeR P2 { get; set; }
    LOWER.mixed P3 { get; set; }
    lOwEr.MIXED P4 { get; set; }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);

            // NOTE: nothing about forwarded types.
            CreateCompilation(csharp, new[] { ref1 }).VerifyDiagnostics(
                // (4,5): error CS0246: The type or namespace name 'upper' could not be found (are you missing a using directive or an assembly reference?)
                //     upper P1 { get; set; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "upper").WithArguments("upper"),
                // (5,5): error CS0246: The type or namespace name 'uPPeR' could not be found (are you missing a using directive or an assembly reference?)
                //     uPPeR P2 { get; set; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "uPPeR").WithArguments("uPPeR"),
                // (6,5): error CS0246: The type or namespace name 'LOWER' could not be found (are you missing a using directive or an assembly reference?)
                //     LOWER.mixed P3 { get; set; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "LOWER").WithArguments("LOWER"),
                // (7,5): error CS0246: The type or namespace name 'lOwEr' could not be found (are you missing a using directive or an assembly reference?)
                //     lOwEr.MIXED P4 { get; set; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "lOwEr").WithArguments("lOwEr"));
        }

        [Fact]
        public void LookupMissingForwardedTypeGlobalAlias()
        {
            var il1 = @"
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Forwarded
{
  .assembly extern pe2
}
";

            var csharp = @"
class Test
{
    static void Main()
    {
        var f = new global::Forwarded();
    }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);

            CreateCompilation(csharp, new[] { ref1 }).VerifyDiagnostics(
                // (6,29): error CS1068: The type name 'Forwarded' could not be found in the global namespace. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //         var f = new global::Forwarded();
                Diagnostic(ErrorCode.ERR_GlobalSingleTypeNameNotFoundFwd, "Forwarded").WithArguments("Forwarded", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void ForwardNullLiteral()
        {
            // From csharp\Source\Conformance\typeforward\attribute\attribute006.cs
            var source = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(null)]
";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,12): error CS0735: Invalid type specified as an argument for TypeForwardedTo attribute
                // [assembly: System.Runtime.CompilerServices.TypeForwardedTo(null)]
                Diagnostic(ErrorCode.ERR_InvalidFwdType, "System.Runtime.CompilerServices.TypeForwardedTo(null)"));
        }

        [WorkItem(529761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529761")]
        [Fact]
        public void LookupMissingForwardedTypeImplicitNamespace()
        {
            var il1 = @"
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Namespace.Forwarded
{
  .assembly extern pe2
}
";

            var csharp = @"
using Namespace;

class Test
{
    static void Main()
    {
        var f = new Forwarded();
    }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);

            CreateCompilation(csharp, new[] { ref1 }).VerifyDiagnostics(
                // (8,21): error CS1069: The type name 'Forwarded' could not be found in the namespace 'Namespace'. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //         var f = new Forwarded();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, "Forwarded").WithArguments("Forwarded", "Namespace", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (2,1): info CS8019: Unnecessary using directive.
                // using Namespace;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Namespace;"));
        }

        [Fact]
        public void LookupMissingForwardedGenericTypeImplicitNamespace()
        {
            var il1 = @"
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Namespace.Forwarded`1
{
  .assembly extern pe2
}
";

            var csharp = @"
using Namespace;

class Test
{
    static void Main()
    {
        var f = new Forwarded<int>();
    }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);

            CreateCompilation(csharp, new[] { ref1 }).VerifyDiagnostics(
                // (8,21): error CS1069: The type name 'Forwarded<>' could not be found in the namespace 'Namespace'. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //         var f = new Forwarded<int>();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, "Forwarded<int>").WithArguments("Forwarded<>", "Namespace", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (2,1): info CS8019: Unnecessary using directive.
                // using Namespace;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Namespace;"));
        }

        [Fact]
        public void NamespacesOnlyMentionedInForwarders()
        {
            var il1 = @"
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

.class extern forwarder T4`1
{
  .assembly extern pe2
}

.class extern forwarder Ns.T5`1
{
  .assembly extern pe2
}

.class extern forwarder Ns.Ms.T6`1
{
  .assembly extern pe2
}
";

            var csharp = @"
class Test
{
    T0 P0 { get; set; }
    Ns.T1 P1 { get; set; }
    Ns.Ms.T2 P2 { get; set; }
    Ns.Ms.Ls.T3 P3 { get; set; }

    T4<int> P4 { get; set; }
    Ns.T5<int> P5 { get; set; }
    Ns.Ms.T6<int> P6 { get; set; }
    Ns.Ms.Ls.T7<int> P7 { get; set; }

    Nope P8 { get; set; }
    Ns.Nope P9 { get; set; }
    Ns.Ms.Nope P10 { get; set; }
    Ns.Ms.Ls.Nope P11 { get; set; }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);

            var compilation = CreateCompilation(csharp, new[] { ref1 });

            compilation.VerifyDiagnostics(
                // (4,5): error CS1070: The type name 'T0' could not be found. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider adding a reference to that assembly.
                //     T0 P0 { get; set; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFoundFwd, "T0").WithArguments("T0", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (5,8): error CS1069: The type name 'T1' could not be found in the namespace 'Ns'. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //     Ns.T1 P1 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, "T1").WithArguments("T1", "Ns", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (6,11): error CS1069: The type name 'T2' could not be found in the namespace 'Ns.Ms'. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //     Ns.Ms.T2 P2 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, "T2").WithArguments("T2", "Ns.Ms", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (7,11): error CS0234: The type or namespace name 'Ls' does not exist in the namespace 'Ns.Ms' (are you missing an assembly reference?)
                //     Ns.Ms.Ls.T3 P3 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Ls").WithArguments("Ls", "Ns.Ms"),
                // (9,5): error CS1070: The type name 'T4<>' could not be found. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider adding a reference to that assembly.
                //     T4<int> P4 { get; set; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFoundFwd, "T4<int>").WithArguments("T4<>", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (10,8): error CS1069: The type name 'T5<>' could not be found in the namespace 'Ns'. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //     Ns.T5<int> P5 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, "T5<int>").WithArguments("T5<>", "Ns", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (11,11): error CS1069: The type name 'T6<>' could not be found in the namespace 'Ns.Ms'. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //     Ns.Ms.T6<int> P6 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, "T6<int>").WithArguments("T6<>", "Ns.Ms", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (12,11): error CS0234: The type or namespace name 'Ls' does not exist in the namespace 'Ns.Ms' (are you missing an assembly reference?)
                //     Ns.Ms.Ls.T7<int> P7 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Ls").WithArguments("Ls", "Ns.Ms"),
                // (14,5): error CS0246: The type or namespace name 'Nope' could not be found (are you missing a using directive or an assembly reference?)
                //     Nope P8 { get; set; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Nope").WithArguments("Nope"),
                // (15,8): error CS0234: The type or namespace name 'Nope' does not exist in the namespace 'Ns' (are you missing an assembly reference?)
                //     Ns.Nope P9 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Nope").WithArguments("Nope", "Ns"),
                // (16,11): error CS0234: The type or namespace name 'Nope' does not exist in the namespace 'Ns.Ms' (are you missing an assembly reference?)
                //     Ns.Ms.Nope P10 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Nope").WithArguments("Nope", "Ns.Ms"),
                // (17,11): error CS0234: The type or namespace name 'Ls' does not exist in the namespace 'Ns.Ms' (are you missing an assembly reference?)
                //     Ns.Ms.Ls.Nope P11 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Ls").WithArguments("Ls", "Ns.Ms"));

            var actualNamespaces = EnumerateNamespaces(compilation).Where(ns =>
                !ns.StartsWith("System", StringComparison.Ordinal) &&
                !ns.StartsWith("Windows", StringComparison.Ordinal) &&
                !ns.StartsWith("FxResources", StringComparison.Ordinal) &&
                !ns.StartsWith("Microsoft", StringComparison.Ordinal));
            var expectedNamespaces = new[] { "Ns", "Ns.Ms" };
            Assert.True(actualNamespaces.SetEquals(expectedNamespaces, EqualityComparer<string>.Default));
        }

        [Fact]
        public void NamespacesMentionedInForwarders()
        {
            var il1 = @"
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
";

            var csharp = @"
namespace N1
{
    class Test
    {
        N2.T t1 { get; set; }
        N2.N3.T t2 { get; set; }
        N1.N2.T t3 { get; set; }
        N1.N2.N3.T t4 { get; set; }
    }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);

            var compilation = CreateCompilation(csharp, new[] { ref1 });

            compilation.VerifyDiagnostics(
                // (7,15): error CS1069: The type name 'T' could not be found in the namespace 'N1.N2.N3'. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //         N2.N3.T t2 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, "T").WithArguments("T", "N1.N2.N3", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (9,18): error CS1069: The type name 'T' could not be found in the namespace 'N1.N2.N3'. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //         N1.N2.N3.T t4 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, "T").WithArguments("T", "N1.N2.N3", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));

            var actualNamespaces = EnumerateNamespaces(compilation).Where(ns =>
                !ns.StartsWith("System", StringComparison.Ordinal) &&
                !ns.StartsWith("Windows", StringComparison.Ordinal) &&
                !ns.StartsWith("FxResources", StringComparison.Ordinal) &&
                !ns.StartsWith("Microsoft", StringComparison.Ordinal));
            var expectedNamespaces = new[] { "N1", "N1.N2", "N1.N2.N3" };
            Assert.True(actualNamespaces.SetEquals(expectedNamespaces, EqualityComparer<string>.Default));
        }

        [Fact]
        public void NamespacesMentionedInForwardersGeneric()
        {
            var il1 = @"
.assembly extern pe2 { }
.assembly extern mscorlib { }
.assembly pe1 { }

.class extern forwarder N1.N2.N3.T`1
{
  .assembly extern pe2
}

.class public auto ansi beforefieldinit N1.N2.T`1<U>
       extends [mscorlib]System.Object
{

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    }

} // end of class N1.N2.T`1
";

            var csharp = @"
namespace N1
{
    class Test
    {
        N2.T<int> t1 { get; set; }
        N2.N3.T<int> t2 { get; set; }
        N1.N2.T<int> t3 { get; set; }
        N1.N2.N3.T<int> t4 { get; set; }
    }
}
";

            var ref1 = CompileIL(il1, prependDefaultHeader: false);

            var compilation = CreateCompilation(csharp, new[] { ref1 });

            compilation.VerifyDiagnostics(
                // (7,15): error CS1069: The type name 'T<>' could not be found in the namespace 'N1.N2.N3'. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //         N2.N3.T<int> t2 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, "T<int>").WithArguments("T<>", "N1.N2.N3", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (9,18): error CS1069: The type name 'T<>' could not be found in the namespace 'N1.N2.N3'. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //         N1.N2.N3.T<int> t4 { get; set; }
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, "T<int>").WithArguments("T<>", "N1.N2.N3", "pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));

            var actualNamespaces = EnumerateNamespaces(compilation).Where(ns =>
                !ns.StartsWith("System", StringComparison.Ordinal) &&
                !ns.StartsWith("Windows", StringComparison.Ordinal) &&
                !ns.StartsWith("FxResources", StringComparison.Ordinal) &&
                !ns.StartsWith("Microsoft", StringComparison.Ordinal));
            var expectedNamespaces = new[] { "N1", "N1.N2", "N1.N2.N3" };
            Assert.True(actualNamespaces.SetEquals(expectedNamespaces, EqualityComparer<string>.Default));
        }

        private static IEnumerable<string> EnumerateNamespaces(CSharpCompilation compilation)
        {
            return EnumerateNamespaces(compilation.GlobalNamespace, "");
        }

        private static IEnumerable<string> EnumerateNamespaces(NamespaceSymbol @namespace, string baseName)
        {
            foreach (var child in @namespace.GetNamespaceMembers())
            {
                var childName = string.IsNullOrEmpty(baseName) ? child.Name : (baseName + "." + child.Name);
                yield return childName;

                foreach (var result in EnumerateNamespaces(child, childName))
                {
                    yield return result;
                }
            }
        }

        [ClrOnlyFact]
        public void EmitForwarder_Simple()
        {
            var source1 = @"
public class Forwarded
{
}
";

            var source2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Forwarded))]
";

            CheckForwarderEmit(source1, source2, "Forwarded");
        }

        [ClrOnlyFact]
        public void EmitForwarder_InNamespace()
        {
            var source1 = @"
namespace NS
{
    public class Forwarded
    {
    }
}
";

            var source2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(NS.Forwarded))]
";

            CheckForwarderEmit(source1, source2, "NS.Forwarded");
        }

        [ClrOnlyFact]
        public void EmitForwarder_OpenGeneric()
        {
            var source1 = @"
public class Forwarded<T>
{
}
";

            var source2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Forwarded<>))]
";

            CheckForwarderEmit(source1, source2, "Forwarded`1");
        }

        [ClrOnlyFact]
        public void EmitForwarder_ConstructedGeneric()
        {
            var source1 = @"
public class Forwarded<T>
{
}
";

            var source2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Forwarded<int>))]
";

            CheckForwarderEmit(source1, source2, "Forwarded`1");
        }

        [ClrOnlyFact]
        public void EmitForwarder_OverlappingGeneric()
        {
            var source1 = @"
public class Forwarded<T>
{
}
";

            var source2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Forwarded<int>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Forwarded<string>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Forwarded<>))]
";

            CheckForwarderEmit(source1, source2, "Forwarded`1");
        }

        [ClrOnlyFact]
        public void EmitForwarder_Nested()
        {
            var source1 = @"
namespace NS
{
    public class Forwarded
    {
        public class Inner
        {
        }
    }
}
";

            var source2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(NS.Forwarded))]
";

            CheckForwarderEmit(source1, source2, "NS.Forwarded", "NS.Forwarded+Inner");
        }

        [ClrOnlyFact]
        public void EmitForwarder_Nested_Private()
        {
            var source1 = @"
namespace NS
{
    public class Forwarded
    {
        private class Private
        {
            public class InnerInner
            {
            }
        }

        internal class Internal
        {
        }

        protected class Protected
        {
        }

        protected internal class ProtectedInternal
        {
        }
    }
}
";

            var source2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(NS.Forwarded))]
";

            // BREAK: dev11 emits Private and Private.InnerInner.
            CheckForwarderEmit(source1, source2, "NS.Forwarded", "NS.Forwarded+Internal", "NS.Forwarded+Protected", "NS.Forwarded+ProtectedInternal");
        }

        [ClrOnlyFact]
        public void EmitForwarder_MultipleNested()
        {
            // Note the order: depth first, children in forward order.
            var source1 = @"
public class Forwarded
{
    public class A
    {
        public class B
        {
        }

        public class C
        {
        }
    }

    public class D
    {
        public class E
        {
        }

        public class F
        {
        }
    }
}
";

            var source2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Forwarded))]
";

            CheckForwarderEmit(source1, source2, "Forwarded", "Forwarded+A", "Forwarded+A+B", "Forwarded+A+C", "Forwarded+D", "Forwarded+D+E", "Forwarded+D+F");
        }

        [ClrOnlyFact]
        public void EmitForwarder_NestedGeneric()
        {
            var source1 = @"
namespace NS
{
    public class Forwarded<T, U>
    {
        public class Inner<V>
        {
        }
    }
}
";

            var source2 = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(NS.Forwarded<,>))]
";

            CheckForwarderEmit(source1, source2, "NS.Forwarded`2", "NS.Forwarded`2+Inner`1");
        }

        /// <summary>
        /// Verify type forwarders in metadata symbols for compiled sources and in ExportedTypes metadata table.
        /// </summary>
        /// <param name="source1">Assembly actually containing types.</param>
        /// <param name="source2">Assembly that forwards types.</param>
        /// <param name="forwardedTypeFullNames">Forwarded type names should be in metadata format (Namespace.Outer`Arity+Inner`Arity).</param>
        private void CheckForwarderEmit(string source1, string source2, params string[] forwardedTypeFullNames)
        {
            var comp1 = CreateCompilation(source1, options: TestOptions.ReleaseDll, assemblyName: "Asm1");
            var verifier1 = CompileAndVerify(comp1);
            var ref1 = MetadataReference.CreateFromImage(verifier1.EmittedAssemblyData);

            var comp2 = CreateCompilation(source2, new[] { ref1 }, options: TestOptions.ReleaseDll, assemblyName: "Asm2");

            Action<ModuleSymbol> metadataValidator = module =>
            {
                var assembly = module.ContainingAssembly;

                // Attributes should not actually be emitted.
                Assert.Equal(0, assembly.GetAttributes(AttributeDescription.TypeForwardedToAttribute).Count());

                var topLevelTypes = new HashSet<string>();

                foreach (var fullName in forwardedTypeFullNames)
                {
                    var plus = fullName.IndexOf('+');

                    if (plus != -1)
                    {
                        topLevelTypes.Add(fullName.Substring(0, plus));
                    }
                    else
                    {
                        topLevelTypes.Add(fullName);
                    }
                }

                foreach (var fullName in topLevelTypes)
                {
                    var type = assembly.ResolveForwardedType(fullName);
                    Assert.NotNull(type);
                    Assert.NotEqual(TypeKind.Error, type.TypeKind);
                    Assert.Equal("Asm1", type.ContainingAssembly.Name);
                }
            };

            var verifier2 = CompileAndVerify(comp2, symbolValidator: metadataValidator);

            using (ModuleMetadata metadata = ModuleMetadata.CreateFromImage(verifier2.EmittedAssemblyData))
            {
                var metadataReader = metadata.Module.GetMetadataReader();

                Assert.Equal(forwardedTypeFullNames.Length, metadataReader.GetTableRowCount(TableIndex.ExportedType));

                int i = 0;
                foreach (var exportedType in metadataReader.ExportedTypes)
                {
                    ValidateExportedTypeRow(exportedType, metadataReader, forwardedTypeFullNames[i]);
                    i++;
                }
            }
        }

        [WorkItem(545911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545911")]
        [ConditionalFact(typeof(DesktopOnly), typeof(ClrOnly))]
        public void EmitForwarder_ModuleInReferencedAssembly()
        {
            string moduleA = @"public class Goo{ public static string A = ""Original""; }";
            var bitsA = CreateCompilation(moduleA, options: TestOptions.ReleaseDll, assemblyName: "asm2").EmitToArray();
            var refA = MetadataReference.CreateFromImage(bitsA);

            string moduleB = @"using System; class Program2222 { static void Main(string[] args) { Console.WriteLine(Goo.A); } }";
            var bitsB = CreateCompilation(moduleB, new[] { refA }, TestOptions.ReleaseExe, assemblyName: "test").EmitToArray();

            string module0 = @"public class Goo{ public static string A = ""Substituted""; }";
            var bits0 = CreateCompilation(module0, options: TestOptions.ReleaseModule, assemblyName: "asm0").EmitToArray();
            var ref0 = ModuleMetadata.CreateFromImage(bits0).GetReference();

            string module1 = "using System;";
            var bits1 = CreateCompilation(module1, new[] { ref0 }, options: TestOptions.ReleaseDll, assemblyName: "asm1").EmitToArray();
            var ref1 = AssemblyMetadata.Create(ModuleMetadata.CreateFromImage(bits1), ModuleMetadata.CreateFromImage(bits0)).GetReference();

            string module2 = @"using System; [assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Goo))]";
            var bits2 = CreateCompilation(module2, new[] { ref1 }, options: TestOptions.ReleaseDll, assemblyName: "asm2").EmitToArray();

            // runtime check:

            var folder = Temp.CreateDirectory();
            var folderA = folder.CreateDirectory("A");
            var folderB = folder.CreateDirectory("B");

            folderA.CreateFile("asm2.dll").WriteAllBytes(bitsA);
            var asmB = folderA.CreateFile("test.exe").WriteAllBytes(bitsB);
            var result = ProcessUtilities.RunAndGetOutput(asmB.Path);
            Assert.Equal("Original", result.Trim());

            folderB.CreateFile("asm0.netmodule").WriteAllBytes(bits0);
            var asm1 = folderB.CreateFile("asm1.dll").WriteAllBytes(bits1);
            var asm2 = folderB.CreateFile("asm2.dll").WriteAllBytes(bits2);
            var asmB2 = folderB.CreateFile("test.exe").WriteAllBytes(bitsB);

            result = ProcessUtilities.RunAndGetOutput(asmB2.Path);
            Assert.Equal("Substituted", result.Trim());
        }

        [WorkItem(545911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545911")]
        [ClrOnlyFact]
        public void EmitForwarder_WithModule()
        {
            var source0 = @"
namespace X 
{
    public class Goo
    {
	    public int getValue()
	    {
		    return -1;
	    }
    }
}";

            var source1 = @"
using System;
";

            var source2 = @"
using System;
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(X.Goo))]
";

            CheckForwarderEmit2(source0, source1, source2, "X.Goo");
        }

        [ConditionalFact(typeof(DesktopOnly), typeof(ClrOnly))]
        [WorkItem(18437, "https://github.com/dotnet/roslyn/issues/18437")]
        public void TypeForwarderInAModule()
        {
            string forwardedTypes =
@"
public class CF1
{}";

            var forwardedTypesCompilation = CreateCompilation(forwardedTypes, options: TestOptions.ReleaseDll, assemblyName: "ForwarderTargetAssembly");

            string mod =
                @"
[assembly: System.Runtime.CompilerServices.TypeForwardedToAttribute(typeof(CF1))]
                ";

            var modCompilation = CreateCompilation(mod, references: new[] { new CSharpCompilationReference(forwardedTypesCompilation) }, options: TestOptions.ReleaseModule);
            var modRef1 = modCompilation.EmitToImageReference();

            string app =
                @"
                public class Test { }
                ";

            var appCompilation = CreateCompilation(app, references: new[] { modRef1, new CSharpCompilationReference(forwardedTypesCompilation) }, options: TestOptions.ReleaseDll);

            var module = (PEModuleSymbol)appCompilation.Assembly.Modules[1];
            var metadata = module.Module;

            var peReader = metadata.GetMetadataReader();
            Assert.Equal(1, peReader.GetTableRowCount(TableIndex.ExportedType));
            ValidateExportedTypeRow(peReader.ExportedTypes.First(), peReader, "CF1");

            EntityHandle token = metadata.GetTypeRef(metadata.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM");
            Assert.True(token.IsNil);   //could the type ref be located? If not then the attribute's not there.

            // Exported types in .NET module cause PEVerify to fail.
            CompileAndVerify(appCompilation, verify: Verification.Fails,
                symbolValidator: m =>
                {
                    var peReader1 = ((PEModuleSymbol)m).Module.GetMetadataReader();
                    Assert.Equal(1, peReader1.GetTableRowCount(TableIndex.ExportedType));
                    ValidateExportedTypeRow(peReader1.ExportedTypes.First(), peReader1, "CF1");

                    // Attributes should not actually be emitted.
                    Assert.Equal(0, m.ContainingAssembly.GetAttributes(AttributeDescription.TypeForwardedToAttribute).Count());
                }).VerifyDiagnostics();

            var ilSource = @"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern Microsoft.VisualBasic
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
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
";

            var modRef2 = GetILModuleReference(ilSource, prependDefaultHeader: false);

            appCompilation = CreateCompilation(app, references: new MetadataReference[] { modRef2, new CSharpCompilationReference(forwardedTypesCompilation) }, options: TestOptions.ReleaseDll);

            module = (PEModuleSymbol)appCompilation.Assembly.Modules[1];
            metadata = module.Module;

            peReader = metadata.GetMetadataReader();
            Assert.Equal(0, peReader.GetTableRowCount(TableIndex.ExportedType));

            token = metadata.GetTypeRef(metadata.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM");
            Assert.False(token.IsNil);   //could the type ref be located? If not then the attribute's not there.
            Assert.Equal(1, peReader.CustomAttributes.Count);

            CompileAndVerify(appCompilation,
                symbolValidator: m =>
                {
                    var peReader1 = ((PEModuleSymbol)m).Module.GetMetadataReader();
                    Assert.Equal(0, peReader1.GetTableRowCount(TableIndex.ExportedType));

                    // Attributes should not actually be emitted.
                    Assert.Equal(0, m.ContainingAssembly.GetAttributes(AttributeDescription.TypeForwardedToAttribute).Count());
                }).VerifyDiagnostics();


            appCompilation = CreateCompilation(app, references: new[] { modRef1, new CSharpCompilationReference(forwardedTypesCompilation) }, options: TestOptions.ReleaseModule);
            var appModule = ModuleMetadata.CreateFromImage(appCompilation.EmitToArray()).Module;

            peReader = appModule.GetMetadataReader();
            Assert.Equal(0, peReader.GetTableRowCount(TableIndex.ExportedType));

            token = appModule.GetTypeRef(appModule.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM");
            Assert.True(token.IsNil);   //could the type ref be located? If not then the attribute's not there.

            appCompilation = CreateCompilation(app, references: new[] { modRef1 }, options: TestOptions.ReleaseDll);

            appCompilation.GetDeclarationDiagnostics().Verify(
                // error CS0012: The type 'CF1' is defined in an assembly that is not referenced. You must add a reference to assembly 'Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_NoTypeDef).WithArguments("CF1", "ForwarderTargetAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        #region Helpers

        private void CheckForwarderEmit2(string source0, string source1, string source2, params string[] forwardedTypeFullNames)
        {
            var folder = Temp.CreateDirectory();
            var comp0 = CreateCompilation(source0, options: TestOptions.ReleaseModule, assemblyName: "asm0");
            var asm0 = ModuleMetadata.CreateFromImage(CompileAndVerify(comp0, verify: Verification.Skipped).EmittedAssemblyData);
            var ref0 = asm0.GetReference();

            var comp1 = CreateCompilation(source1, new[] { ref0 }, options: TestOptions.ReleaseDll, assemblyName: "asm1");
            var asm1 = ModuleMetadata.CreateFromImage(CompileAndVerify(comp1).EmittedAssemblyData);

            var assembly1 = AssemblyMetadata.Create(asm1, asm0);

            var ref1 = assembly1.GetReference();

            var comp2 = CreateCompilation(source2, new[] { ref1 }, options: TestOptions.ReleaseDll, assemblyName: "asm2");

            Action<ModuleSymbol> metadataValidator = module =>
            {
                var assembly = module.ContainingAssembly;

                // Attributes should not actually be emitted.
                Assert.Equal(0, assembly.GetAttributes(AttributeDescription.TypeForwardedToAttribute).Count());

                var topLevelTypes = new HashSet<string>();

                foreach (var fullName in forwardedTypeFullNames)
                {
                    var plus = fullName.IndexOf('+');

                    if (plus != -1)
                    {
                        topLevelTypes.Add(fullName.Substring(0, plus));
                    }
                    else
                    {
                        topLevelTypes.Add(fullName);
                    }
                }

                foreach (var fullName in topLevelTypes)
                {
                    var type = assembly.ResolveForwardedType(fullName);
                    Assert.NotNull(type);
                    Assert.NotEqual(TypeKind.Error, type.TypeKind);
                    Assert.Equal("asm1", type.ContainingAssembly.Name);
                }
            };

            var verifier2 = CompileAndVerify(comp2, symbolValidator: metadataValidator);
            var asm2 = folder.CreateFile("asm2.dll");
            asm2.WriteAllBytes(verifier2.EmittedAssemblyData);

            using (ModuleMetadata metadata = ModuleMetadata.CreateFromImage(verifier2.EmittedAssemblyData))
            {
                var peReader = metadata.Module.GetMetadataReader();

                Assert.Equal(forwardedTypeFullNames.Length, peReader.GetTableRowCount(TableIndex.ExportedType));

                int i = 0;
                foreach (var exportedType in peReader.ExportedTypes)
                {
                    ValidateExportedTypeRow(exportedType, peReader, forwardedTypeFullNames[i]);
                    i++;
                }
            }
        }

        private static void ValidateExportedTypeRow(ExportedTypeHandle exportedTypeHandle, MetadataReader reader, string expectedFullName)
        {
            ExportedType exportedTypeRow = reader.GetExportedType(exportedTypeHandle);
            var split = expectedFullName.Split('.');
            int numParts = split.Length;
            Assert.InRange(numParts, 1, int.MaxValue);
            var expectedType = split[numParts - 1];
            var expectedNamespace = string.Join(".", split, 0, numParts - 1);

            if (expectedFullName.Contains('+'))
            {
                Assert.Equal((TypeAttributes)0, exportedTypeRow.Attributes & TypeAttributesMissing.Forwarder);
                Assert.Equal(0, exportedTypeRow.GetTypeDefinitionId());
                Assert.Equal(expectedType.Split('+').Last(), reader.GetString(exportedTypeRow.Name)); //Only the actual type name.
                Assert.Equal("", reader.GetString(exportedTypeRow.Namespace)); //Empty - presumably there's enough info on the containing type.
                Assert.Equal(HandleKind.ExportedType, exportedTypeRow.Implementation.Kind);
            }
            else
            {
                Assert.Equal(TypeAttributes.NotPublic | TypeAttributesMissing.Forwarder, exportedTypeRow.Attributes);
                Assert.Equal(0, exportedTypeRow.GetTypeDefinitionId());
                Assert.Equal(expectedType, reader.GetString(exportedTypeRow.Name));
                Assert.Equal(expectedNamespace, reader.GetString(exportedTypeRow.Namespace));
                Assert.Equal(HandleKind.AssemblyReference, exportedTypeRow.Implementation.Kind);
            }
        }

        #endregion

        [Fact]
        public void MetadataTypeReferenceResolutionThroughATypeForwardedByCompilation()
        {
            var cA_v1 = CreateCompilation(@"
public class Forwarded<T>
{
}
", options: TestOptions.ReleaseDll, assemblyName: "A");

            var cB = CreateCompilation(@"
public class B : Forwarded<int>
{
}
", new[] { new CSharpCompilationReference(cA_v1) }, options: TestOptions.ReleaseDll, assemblyName: "B");

            var cB_ImageRef = cB.EmitToImageReference();

            var cC_v1 = CreateCompilation(@"
public class Forwarded<T>
{
}
", options: TestOptions.ReleaseDll, assemblyName: "C");

            var cC_v1_ImageRef = cC_v1.EmitToImageReference();

            var cA_v2 = CreateCompilation(@"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Forwarded<byte>))]
", new[] { new CSharpCompilationReference(cC_v1) }, options: TestOptions.ReleaseDll, assemblyName: "A");

            var cA_v2_ImageRef = cA_v2.EmitToImageReference();

            var cD = CreateCompilation(@"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Forwarded<byte>))]
", new[] { new CSharpCompilationReference(cC_v1) }, options: TestOptions.ReleaseModule, assemblyName: "D");

            var cD_ImageRef = cD.EmitToImageReference();

            var cA_v3 = CreateCompilation(@"", new[] { cD_ImageRef, new CSharpCompilationReference(cC_v1) }, options: TestOptions.ReleaseDll, assemblyName: "A");

            var cC_v2 = CreateCompilation(@"
public class Forwarded<T>
{
}
", options: TestOptions.ReleaseDll, assemblyName: "C");

            var ref1 = new MetadataReference[]
            {
                cA_v2_ImageRef,
                new CSharpCompilationReference(cA_v2),
                new CSharpCompilationReference(cA_v3)
            };

            var ref2 = new MetadataReference[]
            {
                new CSharpCompilationReference(cB),
                cB_ImageRef
            };

            var ref3 = new MetadataReference[]
            {
                new CSharpCompilationReference(cC_v1),
                new CSharpCompilationReference(cC_v2),
                cC_v1_ImageRef
            };

            foreach (var r1 in ref1)
            {
                foreach (var r2 in ref2)
                {
                    foreach (var r3 in ref3)
                    {
                        var context = CreateCompilation("", new[] { r1, r2, r3 }, options: TestOptions.ReleaseDll);

                        var forwarded = context.GetTypeByMetadataName("Forwarded`1");
                        var resolved = context.GetTypeByMetadataName("B").BaseType().OriginalDefinition;

                        Assert.NotNull(forwarded);
                        Assert.False(resolved.IsErrorType());
                        Assert.Same(forwarded, resolved);
                    }
                }
            }
        }

        /// <summary>
        /// Aliases to forwarded types are not supported currently.
        /// </summary>
        [WorkItem(27375, "https://github.com/dotnet/roslyn/issues/27375")]
        [Fact]
        public void AliasToTypeForwarder()
        {
            // Library v1: no forwarding.
            const string sourceA1 =
@"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]
namespace MyNamespace
{
    public class MyClass { }
}";
            var compA1 = CreateCompilation(sourceA1, assemblyName: "A");
            var refA1 = compA1.EmitToImageReference(aliases: ImmutableArray.Create("A"));

            const string sourceB1 = sourceA1;
            var compB1 = CreateCompilation(sourceB1, assemblyName: "B");
            var refB1 = compB1.EmitToImageReference(aliases: ImmutableArray.Create("B"));

            const string sourceProgram =
@"extern alias A;
extern alias B;
class Program
{
    static void Main()
    {
        var a = new A::MyNamespace.MyClass();
        var b = new B::MyNamespace.MyClass();
    }
}";
            var comp = CreateCompilation(sourceProgram, references: new[] { refA1, refB1 });
            comp.VerifyDiagnostics();

            // Library v2: forwarding to implementation assembly.
            const string sourceBImpl =
@"namespace MyNamespace
{
    public class MyClass { }
}";
            var compBImpl = CreateCompilation(sourceBImpl, assemblyName: "BImpl");
            var refBImpl = compBImpl.EmitToImageReference();

            const string sourceB2 =
@"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(MyNamespace.MyClass))]";
            var compB2 = CreateCompilation(sourceB2, references: new[] { refBImpl }, assemblyName: "B");

            // Alias to PE assembly.
            comp = CreateCompilation(sourceProgram, references: new[] { refA1, compB2.EmitToImageReference(aliases: ImmutableArray.Create("B")), refBImpl });
            comp.VerifyDiagnostics(
                // (8,36): error CS1069: The type name 'MyClass' could not be found in the namespace 'MyNamespace'. This type has been forwarded to assembly 'BImpl, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //         var b = new B::MyNamespace.MyClass();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, "MyClass").WithArguments("MyClass", "MyNamespace", "BImpl, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 36));

            // Alias to source assembly.
            comp = CreateCompilation(sourceProgram, references: new[] { refA1, new CSharpCompilationReference(compB2, aliases: ImmutableArray.Create("B")), refBImpl });
            comp.VerifyDiagnostics(
                // (8,24): error CS0234: The type or namespace name 'MyNamespace' does not exist in the namespace 'B' (are you missing an assembly reference?)
                //         var b = new B::MyNamespace.MyClass();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "MyNamespace").WithArguments("MyNamespace", "B").WithLocation(8, 24));
        }

        /// <summary>
        /// Aliases to forwarded types are not supported currently.
        /// </summary>
        [WorkItem(27375, "https://github.com/dotnet/roslyn/issues/27375")]
        [Fact]
        public void AliasToGenericTypeForwarder()
        {
            // Library v1: no forwarding.
            const string sourceA1 =
@"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]
namespace MyNamespace
{
    public class MyClass<T> { }
}";
            var compA1 = CreateCompilation(sourceA1, assemblyName: "A");
            var refA1 = compA1.EmitToImageReference(aliases: ImmutableArray.Create("A"));

            const string sourceB1 = sourceA1;
            var compB1 = CreateCompilation(sourceB1, assemblyName: "B");
            var refB1 = compB1.EmitToImageReference(aliases: ImmutableArray.Create("B"));

            const string sourceProgram =
@"extern alias A;
extern alias B;
class Program
{
    static void Main()
    {
        var a = new A::MyNamespace.MyClass<int>();
        var b = new B::MyNamespace.MyClass<int>();
    }
}";
            var comp = CreateCompilation(sourceProgram, references: new[] { refA1, refB1 });
            comp.VerifyDiagnostics();

            // Library v2: forwarding to implementation assembly.
            const string sourceBImpl =
@"namespace MyNamespace
{
    public class MyClass<T> { }
}";
            var compBImpl = CreateCompilation(sourceBImpl, assemblyName: "BImpl");
            var refBImpl = compBImpl.EmitToImageReference();

            const string sourceB2 =
@"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(MyNamespace.MyClass<>))]";
            var compB2 = CreateCompilation(sourceB2, references: new[] { refBImpl }, assemblyName: "B");

            // Alias to PE assembly.
            comp = CreateCompilation(sourceProgram, references: new[] { refA1, compB2.EmitToImageReference(aliases: ImmutableArray.Create("B")), refBImpl });
            comp.VerifyDiagnostics(
                // (8,36): error CS1069: The type name 'MyClass<>' could not be found in the namespace 'MyNamespace'. This type has been forwarded to assembly 'BImpl, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.
                //         var b = new B::MyNamespace.MyClass<int>();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, "MyClass<int>").WithArguments("MyClass<>", "MyNamespace", "BImpl, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 36));

            // Alias to source assembly.
            comp = CreateCompilation(sourceProgram, references: new[] { refA1, new CSharpCompilationReference(compB2, aliases: ImmutableArray.Create("B")), refBImpl });
            comp.VerifyDiagnostics(
                // (8,24): error CS0234: The type or namespace name 'MyNamespace' does not exist in the namespace 'B' (are you missing an assembly reference?)
                //         var b = new B::MyNamespace.MyClass<int>();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "MyNamespace").WithArguments("MyNamespace", "B").WithLocation(8, 24));
        }
    }
}
