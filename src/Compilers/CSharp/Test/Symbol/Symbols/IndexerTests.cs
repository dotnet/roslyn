// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class IndexerTests : CSharpTestBase
    {
        [ClrOnlyFact]
        public void Indexers()
        {
            var source =
@"using System.Runtime.CompilerServices;
class C
{
    [IndexerName(""P"")]
    internal string this[string index]
    {
        get { return null; }
        set { }
    }
}
interface I
{
    object this[int i, params object[] args] { set; }
}
struct S
{
    internal object this[string x]
    {
        get { return null; }
    }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                CheckIndexer(type.Indexers.Single(), true, true, SpecialType.System_String, SpecialType.System_String);

                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I");
                CheckIndexer(type.Indexers.Single(), false, true, SpecialType.System_Object, SpecialType.System_Int32, SpecialType.None);

                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("S");
                CheckIndexer(type.Indexers.Single(), true, false, SpecialType.System_Object, SpecialType.System_String);
            };

            CompileAndVerify(
                source: source,
                sourceSymbolValidator: validator,
                symbolValidator: validator,
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));
        }

        [ClrOnlyFact]
        public void InterfaceImplementations()
        {
            var source =
@"using System.Runtime.CompilerServices;
interface IA
{
    object this[string index] { get; set; }
}
interface IB
{
    object this[string index] { get; }
}
interface IC
{
    [IndexerName(""P"")]
    object this[string index] { get; set; }
}
class A : IA, IB, IC
{
    object IA.this[string index]
    {
        get { return null; }
        set { }
    }
    object IB.this[string index]
    {
        get { return null; }
    }
    object IC.this[string index]
    {
        get { return null; }
        set { }
    }
}
class B : IA, IB, IC
{
    public object this[string index]
    {
        get { return null; }
        set { }
    }
}
class C : IB, IC
{
    [IndexerName(""Q"")]
    public object this[string index]
    {
        get { return null; }
        set { }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyDiagnostics();

            var globalNamespace = (NamespaceSymbol)((CSharpCompilation)compilation.Compilation).GlobalNamespace;

            var type = globalNamespace.GetMember<NamedTypeSymbol>("IA");
            CheckIndexer(type.Indexers.Single(), true, true, SpecialType.System_Object, SpecialType.System_String);

            type = globalNamespace.GetMember<NamedTypeSymbol>("IB");
            CheckIndexer(type.Indexers.Single(), true, false, SpecialType.System_Object, SpecialType.System_String);

            type = globalNamespace.GetMember<NamedTypeSymbol>("IC");
            CheckIndexer(type.Indexers.Single(), true, true, SpecialType.System_Object, SpecialType.System_String);

            type = globalNamespace.GetMember<NamedTypeSymbol>("A");
            var typeAProperties = type.GetMembers().Where(m => m.Kind == SymbolKind.Property).Cast<PropertySymbol>().ToArray();
            Assert.Equal(3, typeAProperties.Length);
            CheckIndexer(typeAProperties[0], true, true, SpecialType.System_Object, SpecialType.System_String);
            CheckIndexer(typeAProperties[1], true, false, SpecialType.System_Object, SpecialType.System_String);
            CheckIndexer(typeAProperties[2], true, true, SpecialType.System_Object, SpecialType.System_String);

            var sourceType = globalNamespace.GetMember<SourceNamedTypeSymbol>("B");
            CheckIndexer(sourceType.Indexers.Single(), true, true, SpecialType.System_Object, SpecialType.System_String);

            var bridgeMethods = sourceType.GetSynthesizedExplicitImplementations(CancellationToken.None).ForwardingMethods;
            Assert.Equal(2, bridgeMethods.Length);
            Assert.True(bridgeMethods.Select(GetPairForSynthesizedExplicitImplementation).SetEquals(new[]
            {
                new KeyValuePair<string, string>("System.Object IC.this[System.String index].get", "System.Object B.this[System.String index].get"),
                new KeyValuePair<string, string>("void IC.this[System.String index].set", "void B.this[System.String index].set"),
            }));

            sourceType = globalNamespace.GetMember<SourceNamedTypeSymbol>("C");
            CheckIndexer(sourceType.Indexers.Single(), true, true, SpecialType.System_Object, SpecialType.System_String);

            bridgeMethods = sourceType.GetSynthesizedExplicitImplementations(CancellationToken.None).ForwardingMethods;
            Assert.Equal(3, bridgeMethods.Length);
            Assert.True(bridgeMethods.Select(GetPairForSynthesizedExplicitImplementation).SetEquals(new[]
            {
                new KeyValuePair<string, string>("System.Object IB.this[System.String index].get", "System.Object C.this[System.String index].get"),
                new KeyValuePair<string, string>("System.Object IC.this[System.String index].get", "System.Object C.this[System.String index].get"),
                new KeyValuePair<string, string>("void IC.this[System.String index].set", "void C.this[System.String index].set"),
            }));
        }

        private static KeyValuePair<string, string> GetPairForSynthesizedExplicitImplementation(SynthesizedExplicitImplementationForwardingMethod bridge)
        {
            return new KeyValuePair<string, string>(bridge.ExplicitInterfaceImplementations.Single().ToTestDisplayString(), bridge.ImplementingMethod.ToTestDisplayString());
        }

        private static void CheckIndexer(PropertySymbol property, bool hasGet, bool hasSet, SpecialType expectedType, params SpecialType[] expectedParameterTypes)
        {
            Assert.NotNull(property);
            Assert.True(property.IsIndexer);

            Assert.Equal(property.Type.SpecialType, expectedType);
            CheckParameters(property.Parameters, expectedParameterTypes);

            var getter = property.GetMethod;
            if (hasGet)
            {
                Assert.NotNull(getter);
                Assert.Equal(getter.ReturnType.SpecialType, expectedType);
                CheckParameters(getter.Parameters, expectedParameterTypes);
            }
            else
            {
                Assert.Null(getter);
            }

            var setter = property.SetMethod;
            if (hasSet)
            {
                Assert.NotNull(setter);
                Assert.True(setter.ReturnsVoid);
                CheckParameters(setter.Parameters, expectedParameterTypes.Concat(new[] { expectedType }).ToArray());
            }
            else
            {
                Assert.Null(setter);
            }

            Assert.Equal(property.GetMethod != null, hasGet);
            Assert.Equal(property.SetMethod != null, hasSet);
        }

        private static void CheckParameters(ImmutableArray<ParameterSymbol> parameters, SpecialType[] expectedTypes)
        {
            Assert.Equal(parameters.Length, expectedTypes.Length);
            for (int i = 0; i < expectedTypes.Length; i++)
            {
                var parameter = parameters[i];
                Assert.Equal(parameter.Ordinal, i);
                Assert.Equal(parameter.Type.SpecialType, expectedTypes[i]);
            }
        }

        [Fact]
        public void OverloadResolution()
        {
            var source =
@"class C
{
    int this[int x, int y]
    {
        get { return 0; }
    }
    int F(C c)
    {
        return this[0] +
            c[0, c] +
            c[1, 2, 3];
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,16): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'C.this[int, int]'
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "this[0]").WithArguments("y", "C.this[int, int]").WithLocation(9, 16),
                // (10,18): error CS1503: Argument 2: cannot convert from 'C' to 'int'
                Diagnostic(ErrorCode.ERR_BadArgType, "c").WithArguments("2", "C", "int").WithLocation(10, 18),
                // (11,13): error CS1501: No overload for method 'this' takes 3 arguments
                Diagnostic(ErrorCode.ERR_BadArgCount, "c[1, 2, 3]").WithArguments("this", "3").WithLocation(11, 13));
        }

        [Fact]
        public void OverridingHiddenIndexer()
        {
            var source =
@"
using System.Runtime.CompilerServices;

public class A
{
    public virtual int this[int x] { get { return 0; } }
}

public class B : A
{
    // Even though the user has specified a name for this indexer that
    // doesn't match the name of the base class accessor, we expect
    // it to hide A's indexer in subclasses (i.e. C).
    [IndexerName(""NotItem"")]
    public int this[int x] { get { return 0; } } //NB: not virtual
}

public class C : B
{
    public override int this[int x] { get { return 0; } }
}";
            var compilation = CreateCompilation(source);

            // NOTE: we could eliminate WRN_NewOrOverrideExpected by putting a "new" modifier on B.this[]
            compilation.VerifyDiagnostics(
                // (15,16): warning CS0114: 'B.this[int]' hides inherited member 'A.this[int]'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "this").WithArguments("B.this[int]", "A.this[int]"),
                // (20,25): error CS0506: 'C.this[int]': cannot override inherited member 'B.this[int]' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "this").WithArguments("C.this[int]", "B.this[int]"));

            var classC = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var indexerC = classC.Indexers.Single();

            Assert.Null(indexerC.OverriddenProperty);
            Assert.Null(indexerC.GetMethod.OverriddenMethod);
        }

        [Fact]
        public void ImplicitlyImplementingIndexersWithDifferentNames_DifferentInterfaces_Source()
        {
            var text = @"
using System.Runtime.CompilerServices;

interface I1
{
    [IndexerName(""A"")]
    int this[int x] { get; }
}

interface I2
{
    [IndexerName(""B"")]
    int this[int x] { get; }
}

class C : I1, I2
{
    public int this[int x] { get { return 0; } }
}
";

            var compilation = CreateCompilation(text);
            compilation.VerifyDiagnostics();

            var interface1 = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("I1");
            var interface1Indexer = interface1.Indexers.Single();

            var interface2 = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("I2");
            var interface2Indexer = interface2.Indexers.Single();

            var @class = compilation.GlobalNamespace.GetMember<SourceNamedTypeSymbol>("C");
            var classIndexer = @class.Indexers.Single();

            // All of the indexers have the same Name
            Assert.Equal(WellKnownMemberNames.Indexer, classIndexer.Name);
            Assert.Equal(WellKnownMemberNames.Indexer, interface1Indexer.Name);
            Assert.Equal(WellKnownMemberNames.Indexer, interface2Indexer.Name);

            // All of the indexers have different MetadataNames
            Assert.NotEqual(interface1Indexer.MetadataName, interface2Indexer.MetadataName);
            Assert.NotEqual(interface1Indexer.MetadataName, classIndexer.MetadataName);
            Assert.NotEqual(interface2Indexer.MetadataName, classIndexer.MetadataName);

            // classIndexer implements both
            Assert.Equal(classIndexer, @class.FindImplementationForInterfaceMember(interface1Indexer));
            Assert.Equal(classIndexer, @class.FindImplementationForInterfaceMember(interface2Indexer));

            var synthesizedExplicitImplementations = @class.GetSynthesizedExplicitImplementations(default(CancellationToken)).ForwardingMethods;
            Assert.Equal(2, synthesizedExplicitImplementations.Length);

            Assert.Equal(classIndexer.GetMethod, synthesizedExplicitImplementations[0].ImplementingMethod);
            Assert.Equal(classIndexer.GetMethod, synthesizedExplicitImplementations[1].ImplementingMethod);

            var interface1Getter = interface1Indexer.GetMethod;
            var interface2Getter = interface2Indexer.GetMethod;
            var interface1GetterImpl = synthesizedExplicitImplementations[0].ExplicitInterfaceImplementations.Single();
            var interface2GetterImpl = synthesizedExplicitImplementations[1].ExplicitInterfaceImplementations.Single();

            Assert.True(interface1Getter == interface1GetterImpl ^ interface1Getter == interface2GetterImpl);
            Assert.True(interface2Getter == interface1GetterImpl ^ interface2Getter == interface2GetterImpl);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ImplicitlyImplementingIndexersWithDifferentNames_DifferentInterfaces_Metadata()
        {
            var il = @"
.class interface public abstract auto ansi I1
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('A')}
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_A(int32 x) cil managed
  {
  } // end of method I1::get_A

  .property instance int32 A(int32)
  {
    .get instance int32 I1::get_A(int32)
  } // end of property I1::A
} // end of class I1

.class interface public abstract auto ansi I2
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('B')}
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_B(int32 x) cil managed
  {
  } // end of method I2::get_B

  .property instance int32 B(int32)
  {
    .get instance int32 I2::get_B(int32)
  } // end of property I2::B
} // end of class I2
";

            var csharp = @"
class C : I1, I2
{
    public int this[int x] { get { return 0; } }
}
";

            CompileWithCustomILSource(csharp, il, compilation =>
            {
                var interface1 = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("I1");
                var interface1Indexer = interface1.Indexers.Single();

                var interface2 = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("I2");
                var interface2Indexer = interface2.Indexers.Single();

                var @class = compilation.GlobalNamespace.GetMember<SourceNamedTypeSymbol>("C");
                var classIndexer = @class.Indexers.Single();

                // All of the indexers have the same Name
                Assert.Equal(WellKnownMemberNames.Indexer, classIndexer.Name);
                Assert.Equal(WellKnownMemberNames.Indexer, interface1Indexer.Name);
                Assert.Equal(WellKnownMemberNames.Indexer, interface2Indexer.Name);

                // All of the indexers have different MetadataNames
                Assert.NotEqual(interface1Indexer.MetadataName, interface2Indexer.MetadataName);
                Assert.NotEqual(interface1Indexer.MetadataName, classIndexer.MetadataName);
                Assert.NotEqual(interface2Indexer.MetadataName, classIndexer.MetadataName);

                // classIndexer implements both
                Assert.Equal(classIndexer, @class.FindImplementationForInterfaceMember(interface1Indexer));
                Assert.Equal(classIndexer, @class.FindImplementationForInterfaceMember(interface2Indexer));

                var synthesizedExplicitImplementations = @class.GetSynthesizedExplicitImplementations(default(CancellationToken)).ForwardingMethods;
                Assert.Equal(2, synthesizedExplicitImplementations.Length);

                Assert.Equal(classIndexer.GetMethod, synthesizedExplicitImplementations[0].ImplementingMethod);
                Assert.Equal(classIndexer.GetMethod, synthesizedExplicitImplementations[1].ImplementingMethod);

                var interface1Getter = interface1Indexer.GetMethod;
                var interface2Getter = interface2Indexer.GetMethod;
                var interface1GetterImpl = synthesizedExplicitImplementations[0].ExplicitInterfaceImplementations.Single();
                var interface2GetterImpl = synthesizedExplicitImplementations[1].ExplicitInterfaceImplementations.Single();

                Assert.True(interface1Getter == interface1GetterImpl ^ interface1Getter == interface2GetterImpl);
                Assert.True(interface2Getter == interface1GetterImpl ^ interface2Getter == interface2GetterImpl);
            });
        }

        /// <summary>
        /// Metadata type has two indexers with the same signature but different names.
        /// Both are implicitly implemented by a single source indexer.
        /// </summary>
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ImplicitlyImplementingIndexersWithDifferentNames_SameInterface()
        {
            var il = @"
.class interface public abstract auto ansi I1
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('getter')}
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  getter(int32 x) cil managed
  {
  } // end of method I1::getter

  .property instance int32 A(int32)
  {
    .get instance int32 I1::getter(int32)
  } // end of property I1::A

  .property instance int32 B(int32)
  {
    .get instance int32 I1::getter(int32)
  } // end of property I1::B
} // end of class I1
";

            var csharp = @"
class C : I1
{
    public int this[int x] { get { return 0; } }
}
";

            CompileWithCustomILSource(csharp, il, compilation =>
            {
                var @interface = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("I1");
                var interfaceIndexers = @interface.Indexers;

                Assert.Equal(2, interfaceIndexers.Length);
                Assert.Equal(interfaceIndexers[0].ToTestDisplayString(), interfaceIndexers[1].ToTestDisplayString());

                var @class = compilation.GlobalNamespace.GetMember<SourceNamedTypeSymbol>("C");
                var classIndexer = @class.Indexers.Single();

                // classIndexer implements both
                Assert.Equal(classIndexer, @class.FindImplementationForInterfaceMember(interfaceIndexers[0]));
                Assert.Equal(classIndexer, @class.FindImplementationForInterfaceMember(interfaceIndexers[1]));

                var synthesizedExplicitImplementation = @class.GetSynthesizedExplicitImplementations(default(CancellationToken)).ForwardingMethods.Single();

                Assert.Equal(classIndexer.GetMethod, synthesizedExplicitImplementation.ImplementingMethod);

                Assert.Equal(interfaceIndexers[0].GetMethod, synthesizedExplicitImplementation.ExplicitInterfaceImplementations.Single());
                Assert.Equal(interfaceIndexers[1].GetMethod, synthesizedExplicitImplementation.ExplicitInterfaceImplementations.Single());
            });
        }

        /// <summary>
        /// Metadata type has two indexers with the same signature but different names.
        /// Both are explicitly implemented by a single source indexer, resulting in an
        /// ambiguity error.
        /// </summary>
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void AmbiguousExplicitIndexerImplementation()
        {
            // NOTE: could be done in C# using IndexerNameAttribute
            var il = @"
.class interface public abstract auto ansi I1
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('get_Item')}
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_Item(int32 x) cil managed
  {
  } // end of method I1::get_Item

  .property instance int32 A(int32)
  {
    .get instance int32 I1::get_Item(int32)
  } // end of property I1::A

  .property instance int32 B(int32)
  {
    .get instance int32 I1::get_Item(int32)
  } // end of property I1::B
} // end of class I1
";

            var csharp1 = @"
class C : I1
{
    int I1.this[int x] { get { return 0; } }
}
";

            var compilation = CreateCompilationWithILAndMscorlib40(csharp1, il).VerifyDiagnostics(
                // (4,12): warning CS0473: Explicit interface implementation 'C.I1.this[int]' matches more than one interface member. Which interface member is actually chosen is implementation-dependent. Consider using a non-explicit implementation instead.
                Diagnostic(ErrorCode.WRN_ExplicitImplCollision, "this").WithArguments("C.I1.this[int]"),
                // (2,7): error CS0535: 'C' does not implement interface member 'I1.this[int]'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("C", "I1.this[int]"));

            var @interface = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("I1");
            var interfaceIndexers = @interface.Indexers;

            Assert.Equal(2, interfaceIndexers.Length);
            Assert.Equal(interfaceIndexers[0].ToTestDisplayString(), interfaceIndexers[1].ToTestDisplayString());

            var @class = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var classIndexer = @class.GetProperty("I1.this[]");

            // One is implemented, the other is not (unspecified which)
            var indexer0Impl = @class.FindImplementationForInterfaceMember(interfaceIndexers[0]);
            var indexer1Impl = @class.FindImplementationForInterfaceMember(interfaceIndexers[1]);
            Assert.True(indexer0Impl == classIndexer ^ indexer1Impl == classIndexer);
            Assert.True(indexer0Impl == null ^ indexer1Impl == null);

            var csharp2 = @"
class C : I1
{
    public int this[int x] { get { return 0; } }
}
";

            compilation = CreateCompilationWithILAndMscorlib40(csharp2, il).VerifyDiagnostics();
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void HidingIndexerWithDifferentName()
        {
            // NOTE: could be done in C# using IndexerNameAttribute
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('A')}
  .method public hidebysig specialname instance int32 
          get_A(int32 x) cil managed
  {
    ldc.i4.0
	ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 A(int32)
  {
    .get instance int32 Base::get_A(int32)
  } // end of property Base::A
} // end of class Base
";

            var csharp = @"
class Derived : Base
{
    public int this[int x] { get { return 0; } }
}
";

            var compilation = CreateCompilationWithILAndMscorlib40(csharp, il);

            compilation.VerifyDiagnostics(
                // (4,16): warning CS0108: 'Derived.this[int]' hides inherited member 'Base.this[int]'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "this").WithArguments("Derived.this[int]", "Base.this[int]"));

            var baseClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseIndexer = baseClass.Indexers.Single();

            var derivedClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var derivedIndexer = derivedClass.Indexers.Single();

            // The indexers have the same Name
            Assert.Equal(WellKnownMemberNames.Indexer, derivedIndexer.Name);
            Assert.Equal(WellKnownMemberNames.Indexer, baseIndexer.Name);

            // The indexers have different MetadataNames
            Assert.NotEqual(baseIndexer.MetadataName, derivedIndexer.MetadataName);

            Assert.Equal(baseIndexer, derivedIndexer.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void OverridingIndexerWithDifferentName()
        {
            // NOTE: could be done in C# using IndexerNameAttribute
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('A')}
  .method public hidebysig newslot specialname virtual 
          instance int32  get_A(int32 x) cil managed
  {
    ldc.i4.0
	ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 A(int32)
  {
    .get instance int32 Base::get_A(int32)
  } // end of property Base::A
} // end of class Base
";

            var csharp = @"
class Derived : Base
{
    public override int this[int x] { get { return 0; } }
}
";

            CompileWithCustomILSource(csharp, il, compilation =>
            {
                var baseClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
                var baseIndexer = baseClass.Indexers.Single();

                var derivedClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
                var derivedIndexer = derivedClass.Indexers.Single();

                // Rhe indexers have the same Name
                Assert.Equal(WellKnownMemberNames.Indexer, derivedIndexer.Name);
                Assert.Equal(WellKnownMemberNames.Indexer, baseIndexer.Name);

                // The indexers have different MetadataNames
                Assert.NotEqual(baseIndexer.MetadataName, derivedIndexer.MetadataName);

                Assert.Equal(baseIndexer, derivedIndexer.OverriddenProperty);
            });
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void HidingMultipleIndexers()
        {
            // NOTE: could be done in C# using IndexerNameAttribute
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('getter')}
  .method public hidebysig specialname instance int32 
          getter(int32 x) cil managed
  {
    ldc.i4.0
	ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 A(int32)
  {
    .get instance int32 Base::getter(int32)
  } // end of property Base::A

  .property instance int32 B(int32)
  {
    .get instance int32 Base::getter(int32)
  } // end of property Base::B
} // end of class Base
";

            var csharp = @"
class Derived : Base
{
    public int this[int x] { get { return 0; } }
}
";

            var compilation = CreateCompilationWithILAndMscorlib40(csharp, il);

            // As in dev10, we report only the first hidden member.
            compilation.VerifyDiagnostics(
                // (4,16): warning CS0108: 'Derived.this[int]' hides inherited member 'Base.this[int]'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "this").WithArguments("Derived.this[int]", "Base.this[int]"));

            var baseClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseIndexers = baseClass.Indexers;

            var derivedClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var derivedIndexer = derivedClass.Indexers.Single();

            // The indexers have the same Name
            Assert.Equal(WellKnownMemberNames.Indexer, derivedIndexer.Name);
            Assert.Equal(WellKnownMemberNames.Indexer, baseIndexers[0].Name);
            Assert.Equal(WellKnownMemberNames.Indexer, baseIndexers[1].Name);

            // The indexers have different MetadataNames
            Assert.NotEqual(baseIndexers[0].MetadataName, baseIndexers[1].MetadataName);
            Assert.NotEqual(baseIndexers[0].MetadataName, derivedIndexer.MetadataName);
            Assert.NotEqual(baseIndexers[1].MetadataName, derivedIndexer.MetadataName);

            // classIndexer implements both
            var hiddenMembers = derivedIndexer.OverriddenOrHiddenMembers.HiddenMembers;
            Assert.Equal(2, hiddenMembers.Length);
            Assert.Contains(baseIndexers[0], hiddenMembers);
            Assert.Contains(baseIndexers[1], hiddenMembers);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void OverridingMultipleIndexers()
        {
            // NOTE: could be done in C# using IndexerNameAttribute
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('getter')}
  .method public hidebysig newslot specialname virtual 
          instance int32  getter(int32 x) cil managed
  {
    ldc.i4.0
	ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 A(int32)
  {
    .get instance int32 Base::getter(int32)
  } // end of property Base::A

  .property instance int32 B(int32)
  {
    .get instance int32 Base::getter(int32)
  } // end of property Base::B
} // end of class Base
";

            var csharp = @"
class Derived : Base
{
    public override int this[int x] { get { return 0; } }
}
";

            var compilation = CreateCompilationWithILAndMscorlib40(csharp, il).VerifyDiagnostics(
                // (4,25): error CS0462: The inherited members 'Base.this[int]' and 'Base.this[int]' have the same signature in type 'Derived', so they cannot be overridden
                Diagnostic(ErrorCode.ERR_AmbigOverride, "this").WithArguments("Base.this[int]", "Base.this[int]", "Derived"));

            var baseClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseIndexers = baseClass.Indexers;

            var derivedClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var derivedIndexer = derivedClass.Indexers.Single();

            // The indexers have the same Name
            Assert.Equal(WellKnownMemberNames.Indexer, derivedIndexer.Name);
            Assert.Equal(WellKnownMemberNames.Indexer, baseIndexers[0].Name);
            Assert.Equal(WellKnownMemberNames.Indexer, baseIndexers[1].Name);

            // The indexers have different MetadataNames
            Assert.NotEqual(baseIndexers[0].MetadataName, baseIndexers[1].MetadataName);
            Assert.NotEqual(baseIndexers[0].MetadataName, derivedIndexer.MetadataName);
            Assert.NotEqual(baseIndexers[1].MetadataName, derivedIndexer.MetadataName);

            // classIndexer implements both
            var overriddenMembers = derivedIndexer.OverriddenOrHiddenMembers.OverriddenMembers;
            Assert.Equal(2, overriddenMembers.Length);
            Assert.Contains(baseIndexers[0], overriddenMembers);
            Assert.Contains(baseIndexers[1], overriddenMembers);
        }

        [Fact]
        public void IndexerAccessErrors()
        {
            var source =
@"class C
{
    public int this[int x, long y] { get { return x; } set { } }

    void M(C c)
    {
        c[0] = c[0, 0, 0]; //wrong number of arguments
        c[true, 1] = c[y: 1, x: long.MaxValue]; //wrong argument types
        c[1, x: 1] = c[x: 1, 2]; //bad mix of named and positional
        this[q: 1, r: 2] = base[0]; //bad parameter names / no indexer
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_1).VerifyDiagnostics(
                // (7,9): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'C.this[int, long]'
                //         c[0] = c[0, 0, 0]; //wrong number of arguments
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "c[0]").WithArguments("y", "C.this[int, long]").WithLocation(7, 9),
                // (7,16): error CS1501: No overload for method 'this' takes 3 arguments
                //         c[0] = c[0, 0, 0]; //wrong number of arguments
                Diagnostic(ErrorCode.ERR_BadArgCount, "c[0, 0, 0]").WithArguments("this", "3").WithLocation(7, 16),
                // (8,11): error CS1503: Argument 1: cannot convert from 'bool' to 'int'
                //         c[true, 1] = c[y: 1, x: long.MaxValue]; //wrong argument types
                Diagnostic(ErrorCode.ERR_BadArgType, "true").WithArguments("1", "bool", "int").WithLocation(8, 11),
                // (8,33): error CS1503: Argument 2: cannot convert from 'long' to 'int'
                //         c[true, 1] = c[y: 1, x: long.MaxValue]; //wrong argument types
                Diagnostic(ErrorCode.ERR_BadArgType, "long.MaxValue").WithArguments("2", "long", "int").WithLocation(8, 33),
                // (9,14): error CS1744: Named argument 'x' specifies a parameter for which a positional argument has already been given
                //         c[1, x: 1] = c[x: 1, 2]; //bad mix of named and positional
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "x").WithArguments("x").WithLocation(9, 14),
                // (9,30): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                //         c[1, x: 1] = c[x: 1, 2]; //bad mix of named and positional
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "2").WithArguments("7.2").WithLocation(9, 30),
                // (10,14): error CS1739: The best overload for 'this' does not have a parameter named 'q'
                //         this[q: 1, r: 2] = base[0]; //bad parameter names / no indexer
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "q").WithArguments("this", "q").WithLocation(10, 14),
                // (10,28): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
                //         this[q: 1, r: 2] = base[0]; //bad parameter names / no indexer
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "base[0]").WithArguments("object").WithLocation(10, 28)
                );
        }

        [Fact]
        public void OverloadResolutionOnIndexersNotAccessors()
        {
            var source =
@"class C
{
    public int this[int x] { set { } }
    public int this[int x, double d = 1] { get { return x; } set { } }

    void M(C c)
    {
        int x = c[0]; //pick the first overload, even though it has no getter and the second would work
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,17): error CS0154: The property or indexer 'C.this[int]' cannot be used in this context because it lacks the get accessor
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "c[0]").WithArguments("C.this[int]"));
        }

        [Fact]
        public void UseExplicitInterfaceImplementationAccessor()
        {
            var source =
@"interface I
{
    int this[int x] { get; }
}


class C : I
{
    int I.this[int x] { get { return x; } }

    void M(C c)
    {
        int x = c[0]; // no indexer found
        int y = ((I)c)[0];
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,17): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[0]").WithArguments("C"));
        }

        [Fact]
        public void UsePropertyAndAccessorsDirectly()
        {
            var source =
@"class C
{
    int this[int x] { get { return x; } set { } }

    void M(C c)
    {
        int x = c.Item[1]; //CS1061 - no such member
        int y = c.get_Item(1); //CS0571 - use the indexer
        c.set_Item(y); //CS0571 - use the indexer
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,19): error CS1061: 'C' does not contain a definition for 'Item' and no extension method 'Item' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Item").WithArguments("C", "Item"),
                // (8,19): error CS0571: 'C.this[int].get': cannot explicitly call operator or accessor
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_Item").WithArguments("C.this[int].get"),
                // (9,11): error CS0571: 'C.this[int].set': cannot explicitly call operator or accessor
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "set_Item").WithArguments("C.this[int].set"));
        }

        [Fact]
        public void NestedIndexerAccesses()
        {
            var source =
@"class C
{
    C this[int x] { get { return this; } set { } }
    int[] this[char x] { get { return null; } set { } }

    void M(C c)
    {
        int x = c[0][1][2][3]['a'][1]; //fine
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void NamedParameters()
        {
            var source =
@"class C
{
    int this[int x, string y, char z] { get { return x; } }

    void M(C c)
    {
        int x;
        x = c[x: 0, y: ""hello"", z:'a'];
        x = c[0, y: ""hello"", z:'a'];
        x = c[0, ""hello"", z:'a'];
        x = c[0, ""hello"", 'a'];

        x = c[z: 'a', x: 0, y: ""hello""]; //all reordered
        x = c[0, z:'a', y: ""hello""]; //some reordered
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void OptionalParameters()
        {
            var source =
@"class C
{
    int this[int x = 1, string y = ""goodbye"", char z = 'b'] { get { return x; } }

    void M(C c)
    {
        int x;
        x = this[]; //CS0443 - can't omit all
        x = c[x: 0];
        x = c[y: ""hello""];
        x = c[z:'a'];
        x = c[x: 0, y: ""hello""];
        x = c[x: 0, z:'a'];
        x = c[y: ""hello"", z:'a'];
        x = c[x: 0, y: ""hello"", z:'a'];
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,18): error CS0443: Syntax error; value expected
                Diagnostic(ErrorCode.ERR_ValueExpected, "]"));
        }

        [Fact]
        public void ParameterArray()
        {
            var source =
@"class C
{
    int this[params int[] args] { get { return 0; } }
    int this[char c, params char[] args] { get { return 0; } }

    void M(C c)
    {
        int x;
        x = this[]; //CS0443 - can't omit all

        x = c[0];
        x = c[0, 1];
        x = c[0, 1, 2];

        x = c[new int[3]];
        x = c[args: new int[3]];

        x = c['a'];
        x = c['a', 'b'];
        x = c['a', 'b', 'c'];

        x = c['a', new char[3]];
        x = c['a', args: new char[3]];
        x = c[args: new char[3], c: 'a'];
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,18): error CS0443: Syntax error; value expected
                Diagnostic(ErrorCode.ERR_ValueExpected, "]"));
        }

        [Fact]
        public void StaticIndexer()
        {
            var source =
@"class C
{
    // Illegal, but we shouldn't blow up
    public static int this[char c] { get { return 0; } } //CS0106 - illegal modifier

    public static void Main()
    {
        int x = C['a']; //CS0119 - can't use a type here
        int y = new C()['a']; //we don't even check for this kind of error because it's always cascading
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,23): error CS0106: The modifier 'static' is not valid for this item
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(4, 23),
                // (8,17): error CS0119: 'C' is a 'type', which is not valid in the given context
                Diagnostic(ErrorCode.ERR_BadSKunknown, "C").WithArguments("C", "type").WithLocation(8, 17));
        }

        [Fact]
        public void OverridingAndHidingWithExplicitIndexerName()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;

public class A
{
    public virtual int this[int x]
    {
        get
        {
            Console.WriteLine(""A"");
            return 0;
        }
    }
}

public class B : A
{
    [IndexerName(""NotItem"")]
    public int this[int x]
    {
        get
        {
            Console.WriteLine(""B"");
            return 0;
        }
    }
}

public class C : B
{
    public override int this[int x]
    {
        get
        {
            Console.WriteLine(""C"");
            return 0;
        }
    }
}";
            // Doesn't matter that B's indexer has an explicit name - the symbols are all called "this[]".
            CreateCompilation(source).VerifyDiagnostics(
                // (19,16): warning CS0114: 'B.this[int]' hides inherited member 'A.this[int]'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "this").WithArguments("B.this[int]", "A.this[int]"),
                // (31,25): error CS0506: 'C.this[int]': cannot override inherited member 'B.this[int]' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "this").WithArguments("C.this[int]", "B.this[int]"));
        }

        [ClrOnlyFact]
        public void CanBeReferencedByName()
        {
            var source = @"
interface I
{
    event System.Action E;
    int P { get; set; }
    int this[int x] { set; }
}

class C : I
{
    event System.Action I.E { add { } remove { } }
    public event System.Action E;

    int I.P { get; set; }
    public int P { get; set; }

    int I.this[int x] { set { } }
    public int this[int x] { set { } }
}
";

            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var globalNamespace = module.GlobalNamespace;
                var compilation = module.DeclaringCompilation;
                Assert.Equal(isFromSource, compilation != null);

                //// Source interface

                var @interface = globalNamespace.GetMember<NamedTypeSymbol>("I");
                if (isFromSource)
                {
                    Assert.True(@interface.IsFromCompilation(compilation));
                }

                var interfaceEvent = @interface.GetMember<EventSymbol>("E");
                var interfaceProperty = @interface.GetMember<PropertySymbol>("P");
                var interfaceIndexer = @interface.Indexers.Single();

                Assert.True(interfaceEvent.CanBeReferencedByName);
                Assert.True(interfaceProperty.CanBeReferencedByName);
                Assert.False(interfaceIndexer.CanBeReferencedByName);

                //// Source class

                var @class = globalNamespace.GetMember<NamedTypeSymbol>("C");
                if (isFromSource)
                {
                    Assert.True(@class.IsFromCompilation(compilation));
                }

                var classEventImpl = @class.GetMembers().Where(m => m.GetExplicitInterfaceImplementations().Contains(interfaceEvent)).Single();
                var classPropertyImpl = @class.GetMembers().Where(m => m.GetExplicitInterfaceImplementations().Contains(interfaceProperty)).Single();
                var classIndexerImpl = @class.GetMembers().Where(m => m.GetExplicitInterfaceImplementations().Contains(interfaceIndexer)).Single();

                Assert.False(classEventImpl.CanBeReferencedByName);
                Assert.False(classPropertyImpl.CanBeReferencedByName);
                Assert.False(classIndexerImpl.CanBeReferencedByName);

                var classEvent = @class.GetMember<EventSymbol>("E");
                var classProperty = @class.GetMember<PropertySymbol>("P");
                var classIndexer = @class.Indexers.Single();

                Assert.True(classEvent.CanBeReferencedByName);
                Assert.True(classProperty.CanBeReferencedByName);
                Assert.False(classIndexer.CanBeReferencedByName);
            };

            CompileAndVerify(source, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
        }

        [Fact]
        public void RegressFinalValidationAssert()
        {
            var source =
@"class C
{
    int this[int x] { get { return x; } }
    void M()
    {
        System.Console.WriteLine(this[0]);
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        /// <summary>
        /// The Name and IsIndexer bits of explicitly implemented interface indexers do not roundtrip.
        /// This is unfortunate, but less so that having something declared with an IndexerDeclarationSyntax
        /// return false for IsIndexer.
        /// </summary>
        [ClrOnlyFact]
        public void ExplicitInterfaceImplementationIndexers()
        {
            var text = @"
public interface I
{
    int this[int x] { set; }
}

public class C : I
{
    int I.this[int x] { set { } }
}
";

            Action<ModuleSymbol> sourceValidator = module =>
            {
                var globalNamespace = module.GlobalNamespace;

                var classC = globalNamespace.GetMember<NamedTypeSymbol>("C");
                Assert.Equal(0, classC.Indexers.Length); //excludes explicit implementations

                var classCIndexer = classC.GetMembers().Where(s => s.Kind == SymbolKind.Property).Single();
                Assert.Equal("I.this[]", classCIndexer.Name); //interface name + WellKnownMemberNames.Indexer
                Assert.True(classCIndexer.IsIndexer()); //since declared with IndexerDeclarationSyntax
            };

            Action<ModuleSymbol> metadataValidator = module =>
            {
                var globalNamespace = module.GlobalNamespace;

                var classC = globalNamespace.GetMember<NamedTypeSymbol>("C");
                Assert.Equal(0, classC.Indexers.Length); //excludes explicit implementations

                var classCIndexer = classC.GetMembers().Where(s => s.Kind == SymbolKind.Property).Single();
                Assert.Equal("I.Item", classCIndexer.Name); //name does not reflect WellKnownMemberNames.Indexer
                Assert.False(classCIndexer.IsIndexer()); //not the default member of C
            };

            CompileAndVerify(text, sourceSymbolValidator: sourceValidator, symbolValidator: metadataValidator);
        }

        [Fact]
        public void NoAutoIndexers()
        {
            var source =
@"class B
{
    public virtual int this[int x] { get; set; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,38): error CS0501: 'B.this[int].get' must declare a body because it is not marked abstract, extern, or partial
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("B.this[int].get"),
                // (3,43): error CS0501: 'B.this[int].set' must declare a body because it is not marked abstract, extern, or partial
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("B.this[int].set"));
        }

        [Fact]
        public void BaseIndexerAccess()
        {
            var source =
@"public class Base
{
    public int this[int x] { get { return x; } }
}

public class Derived : Base
{
    public new int this[int x] { get { return x; } }

    void Method()
    {
        int x = base[1];
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            var indexerAccessSyntax = GetElementAccessExpressions(tree.GetCompilationUnitRoot()).Single();

            var baseClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseIndexer = baseClass.Indexers.Single();

            // Confirm that the base indexer is used (even though the derived indexer signature matches).
            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(indexerAccessSyntax);
            Assert.Equal(baseIndexer.GetPublicSymbol(), symbolInfo.Symbol);
        }

        /// <summary>
        /// Indexers cannot have ref params in source, but they can in metadata.
        /// </summary>
        [Fact]
        public void IndexerWithRefParameter_Access()
        {
            var source = @"
class Test
{
    static void Main()
    {
        RefIndexer r = new RefIndexer();
        int x = 1;
        x = r[ref x];
        r[ref x] = 1;
        r[ref x]++;
        r[ref x] += 2;
    }
}
";
            var compilation = CreateCompilation(source, new[] { TestReferences.SymbolsTests.Indexers });

            compilation.VerifyDiagnostics(
                // (8,13): error CS1545: Property, indexer, or event 'RefIndexer.this[ref int]' is not supported by the language; try directly calling accessor methods 'RefIndexer.get_Item(ref int)' or 'RefIndexer.set_Item(ref int, int)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "r[ref x]").WithArguments("RefIndexer.this[ref int]", "RefIndexer.get_Item(ref int)", "RefIndexer.set_Item(ref int, int)"),
                // (9,9): error CS1545: Property, indexer, or event 'RefIndexer.this[ref int]' is not supported by the language; try directly calling accessor methods 'RefIndexer.get_Item(ref int)' or 'RefIndexer.set_Item(ref int, int)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "r[ref x]").WithArguments("RefIndexer.this[ref int]", "RefIndexer.get_Item(ref int)", "RefIndexer.set_Item(ref int, int)"),
                // (10,9): error CS1545: Property, indexer, or event 'RefIndexer.this[ref int]' is not supported by the language; try directly calling accessor methods 'RefIndexer.get_Item(ref int)' or 'RefIndexer.set_Item(ref int, int)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "r[ref x]").WithArguments("RefIndexer.this[ref int]", "RefIndexer.get_Item(ref int)", "RefIndexer.set_Item(ref int, int)"),
                // (11,9): error CS1545: Property, indexer, or event 'RefIndexer.this[ref int]' is not supported by the language; try directly calling accessor methods 'RefIndexer.get_Item(ref int)' or 'RefIndexer.set_Item(ref int, int)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "r[ref x]").WithArguments("RefIndexer.this[ref int]", "RefIndexer.get_Item(ref int)", "RefIndexer.set_Item(ref int, int)"));
        }

        /// <summary>
        /// Indexers cannot have ref params in source, but they can in metadata.
        /// </summary>
        [Fact]
        public void IndexerWithRefParameter_CallAccessor()
        {
            var source = @"
class Test
{
    static void Main()
    {
        RefIndexer r = new RefIndexer();
        int x = 1;
        x = r.get_Item(ref x);
        r.set_Item(ref x, 1);
    }
}
";
            var compilation = CreateCompilation(source, new[] { TestReferences.SymbolsTests.Indexers });
            compilation.VerifyDiagnostics();
        }

        /// <summary>
        /// Indexers cannot have ref params in source, but they can in metadata.
        /// </summary>
        [Fact]
        public void IndexerWithRefParameter_Override()
        {
            var source = @"
class Test : RefIndexer
{
    public override int this[int x] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilation(source,
                new MetadataReference[] { TestReferences.SymbolsTests.Indexers });
            compilation.VerifyDiagnostics(
                // (4,25): error CS0115: 'Test.this[int]': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("Test.this[int]"));
        }

        /// <summary>
        /// Indexers cannot have ref params in source, but they can in metadata.
        /// </summary>
        [Fact]
        public void IndexerWithRefParameter_ImplicitlyImplement()
        {
            var source = @"
class Test : IRefIndexer
{
    public int this[int x] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilation(source, new[] { TestReferences.SymbolsTests.Indexers });

            // Normally, we wouldn't see errors for the accessors, but here we do because the indexer is bogus.
            compilation.VerifyDiagnostics(
                // (2,7): error CS0535: 'Test' does not implement interface member 'IRefIndexer.get_Item(ref int)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IRefIndexer").WithArguments("Test", "IRefIndexer.get_Item(ref int)"),
                // (2,7): error CS0535: 'Test' does not implement interface member 'IRefIndexer.set_Item(ref int, int)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IRefIndexer").WithArguments("Test", "IRefIndexer.set_Item(ref int, int)"));
        }

        /// <summary>
        /// Indexers cannot have ref params in source, but they can in metadata.
        /// </summary>
        [Fact]
        public void IndexerWithRefParameter_ExplicitlyImplement()
        {
            var source = @"
class Test : IRefIndexer
{
    int IRefIndexer.this[int x] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilation(source,
                new MetadataReference[] { TestReferences.SymbolsTests.Indexers });
            // Normally, we wouldn't see errors for the accessors, but here we do because the indexer is bogus.
            compilation.VerifyDiagnostics(
                // (4,21): error CS0539: 'Test.this[int]' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "this").WithArguments("Test.this[int]"),
                // (2,7): error CS0535: 'Test' does not implement interface member 'IRefIndexer.get_Item(ref int)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IRefIndexer").WithArguments("Test", "IRefIndexer.get_Item(ref int)"),
                // (2,7): error CS0535: 'Test' does not implement interface member 'IRefIndexer.set_Item(ref int, int)'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IRefIndexer").WithArguments("Test", "IRefIndexer.set_Item(ref int, int)"));
        }

        [Fact]
        public void IndexerNameAttribute()
        {
            var source = @"
using System.Runtime.CompilerServices;

class B
{
    [IndexerName(""A"")]
    public virtual int this[int x] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var indexer = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").Indexers.Single();
            Assert.Equal(WellKnownMemberNames.Indexer, indexer.Name);
            Assert.Equal("A", indexer.MetadataName);
            Assert.Equal("get_A", indexer.GetMethod.Name);
            Assert.Equal("get_A", indexer.GetMethod.MetadataName);
            Assert.Equal("set_A", indexer.SetMethod.Name);
            Assert.Equal("set_A", indexer.SetMethod.MetadataName);
        }

        [WorkItem(528830, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528830")]
        [Fact(Skip = "528830")]
        public void EscapedIdentifierInIndexerNameAttribute()
        {
            var source = @"
using System.Runtime.CompilerServices;

interface I
{
    [IndexerName(""@indexer"")]
    int this[int x] { get; set; }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var indexer = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("I").Indexers.Single();
            Assert.Equal("@indexer", indexer.MetadataName);
            Assert.Equal("get_@indexer", indexer.GetMethod.MetadataName);
            Assert.Equal("set_@indexer", indexer.SetMethod.MetadataName);
        }

        [Fact]
        public void NameNotCopiedOnOverride1()
        {
            var source = @"
using System.Runtime.CompilerServices;

class B
{
    [IndexerName(""A"")]
    public virtual int this[int x] { get { return 0; } set { } }
}

class D : B
{
    public override int this[int x] { get { return 0; } set { } }

    [IndexerName(""A"")] //error since name isn't copied down to override
    public int this[int x, int y] { get { return 0; } set { } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,16): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
                Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this"));
        }

        [Fact]
        public void NameNotCopiedOnOverride2()
        {
            var source = @"
using System.Runtime.CompilerServices;

class B
{
    [IndexerName(""A"")]
    public virtual int this[int x] { get { return 0; } set { } }
}

class D : B
{
    [IndexerName(""A"")] //dev10 didn't allow this, but it should eliminate the error
    public override int this[int x] { get { return 0; } set { } }

    [IndexerName(""A"")] //error since name isn't copied down to override
    public int this[int x, int y] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();
            var derivedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("D");
            Assert.True(derivedType.Indexers.All(i => i.MetadataName == "A"));
        }

        [Fact]
        public void NameNotCopiedOnOverride3()
        {
            var source = @"
using System.Runtime.CompilerServices;

class B
{
    [IndexerName(""A"")]
    public virtual int this[int x] { get { return 0; } set { } }
}

class D : B
{
    public override int this[int x] { get { return 0; } set { } }

    // If the name of the overridden indexer was copied, this would be an error.
    public int this[int x, int y] { get { return 0; } set { } }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void IndexerNameLookup1()
        {
            var source = @"
using System.Runtime.CompilerServices;

class A
{
    public const string get_X = ""X"";
}

class B : A
{
    [IndexerName(C.get_X)]
    public int this[int x] { get { return 0; } }
}

class C : B
{
    [IndexerName(get_X)]
    public int this[int x, int y] { get { return 0; } }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var classA = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var classB = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B");
            var classC = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            var get_XA = classA.GetMember<FieldSymbol>("get_X");
            var get_XB = classB.GetMember<MethodSymbol>("get_X");
            var get_XC = classC.GetMember<MethodSymbol>("get_X");

            Assert.Equal("X", get_XB.AssociatedSymbol.MetadataName);
            Assert.Equal("X", get_XC.AssociatedSymbol.MetadataName);
        }

        [Fact]
        public void IndexerNameLookup2()
        {
            var source = @"
using System.Runtime.CompilerServices;

class A
{
    public const string get_X = ""X"";

    [IndexerName(get_X)]
    public int this[int x] { get { return 0; } }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,30): error CS0102: The type 'A' already contains a definition for 'get_X'
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "get").WithArguments("A", "get_X"));

            var classA = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            Assert.Equal("X", classA.Indexers.Single().MetadataName);
        }

        [Fact]
        public void IndexerNameLookup3()
        {
            var source = @"
using System.Runtime.CompilerServices;

public class MyAttribute : System.Attribute
{
    public MyAttribute(object o) { }
}

class A
{
    [IndexerName(get_Item)]
    public int this[int x] { get { return 0; } }

    // Doesn't matter what attribute it is or what member it's on - can't see indexer members.
    [MyAttribute(get_Item)]
    int x;
}
";
            // NOTE: Dev10 reports CS0571 for MyAttribute's use of get_Item
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (11,18): error CS0571: 'A.this[int].get': cannot explicitly call operator or accessor
                //     [IndexerName(get_Item)]
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_Item").WithArguments("A.this[int].get"),
                // (15,18): error CS0571: 'A.this[int].get': cannot explicitly call operator or accessor
                //     [MyAttribute(get_Item)]
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_Item").WithArguments("A.this[int].get"),
                // (16,9): warning CS0169: The field 'A.x' is never used
                //     int x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("A.x"));
        }

        [Fact]
        public void IndexerNameLookup4()
        {
            var source = @"
using System.Runtime.CompilerServices;

class A
{
    [IndexerName(B.get_Item)]
    public int this[int x] { get { return 0; } }
}

class B
{
    [IndexerName(A.get_Item)]
    public int this[int x] { get { return 0; } }
}
";
            // NOTE: Dev10 reports CS0117 in A, but CS0571 in B
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,20): error CS0571: 'B.this[int].get': cannot explicitly call operator or accessor
                //     [IndexerName(B.get_Item)]
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_Item").WithArguments("B.this[int].get"),
                // (12,20): error CS0571: 'A.this[int].get': cannot explicitly call operator or accessor
                //     [IndexerName(A.get_Item)]
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_Item").WithArguments("A.this[int].get"));
        }

        [Fact]
        public void IndexerNameLookup5()
        {
            var source = @"
using System.Runtime.CompilerServices;

class A
{
    public const string get_Item = ""X"";
}

class B : A
{
    public const string C = get_Item;

    [IndexerName(C)]
    public int this[int x] { get { return 0; } }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void IndexerNameLookupClass()
        {
            var source = @"
using System.Runtime.CompilerServices;

class A
{
    public const string Constant1 = B.Constant1;
    public const string Constant2 = B.Constant2;
}

class B
{
    public const string Constant1 = ""X"";
    public const string Constant2 = A.Constant2;

    [IndexerName(A.Constant1)]
    public int this[int x] { get { return 0; } }

    [IndexerName(A.Constant2)]
    public int this[long x] { get { return 0; } }
}
";
            // CONSIDER: this cascading is a bit verbose.
            CreateCompilation(source).VerifyDiagnostics(
                // (18,18): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [IndexerName(A.Constant2)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "A.Constant2"),
                // (7,25): error CS0110: The evaluation of the constant value for 'A.Constant2' involves a circular definition
                //     public const string Constant2 = B.Constant2;
                Diagnostic(ErrorCode.ERR_CircConstValue, "Constant2").WithArguments("A.Constant2"),
                // (19,16): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
                //     public int this[long x] { get { return 0; } }
                Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this"));
        }

        [Fact]
        public void IndexerNameLookupStruct()
        {
            var source = @"
using System.Runtime.CompilerServices;

struct A
{
    public const string Constant1 = B.Constant1;
    public const string Constant2 = B.Constant2;
}

struct B
{
    public const string Constant1 = ""X"";
    public const string Constant2 = A.Constant2;

    [IndexerName(A.Constant1)]
    public int this[int x] { get { return 0; } }

    [IndexerName(A.Constant2)]
    public int this[long x] { get { return 0; } }
}
";
            // CONSIDER: this cascading is a bit verbose.
            CreateCompilation(source).VerifyDiagnostics(
                // (18,18): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [IndexerName(A.Constant2)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "A.Constant2"),
                // (13,25): error CS0110: The evaluation of the constant value for 'A.Constant2' involves a circular definition
                //     public const string Constant2 = A.Constant2;
                Diagnostic(ErrorCode.ERR_CircConstValue, "Constant2").WithArguments("A.Constant2"),
                // (19,16): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
                //     public int this[long x] { get { return 0; } }
                Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this"));
        }

        [Fact]
        public void IndexerNameLookupInterface()
        {
            var source = @"
using System.Runtime.CompilerServices;

interface A
{
    const string Constant1 = B.Constant1;
    const string Constant2 = B.Constant2;
}

interface B
{
    const string Constant1 = ""X"";
    const string Constant2 = A.Constant2;

    [IndexerName(A.Constant1)]
    int this[int x] { get; }

    [IndexerName(A.Constant2)]
    int this[long x] { get; }
}
";
            // CONSIDER: this cascading is a bit verbose.
            CreateCompilation(source, parseOptions: TestOptions.Regular7, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (18,18): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [IndexerName(A.Constant2)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "A.Constant2").WithLocation(18, 18),
                // (7,18): error CS0110: The evaluation of the constant value for 'A.Constant2' involves a circular definition
                //     const string Constant2 = B.Constant2;
                Diagnostic(ErrorCode.ERR_CircConstValue, "Constant2").WithArguments("A.Constant2").WithLocation(7, 18),
                // (19,9): error CS0668: Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type
                //     int this[long x] { get; }
                Diagnostic(ErrorCode.ERR_InconsistentIndexerNames, "this").WithLocation(19, 9),
                // (12,18): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     const string Constant1 = "X";
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Constant1").WithArguments("default interface implementation", "8.0").WithLocation(12, 18),
                // (13,18): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     const string Constant2 = A.Constant2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Constant2").WithArguments("default interface implementation", "8.0").WithLocation(13, 18),
                // (6,18): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     const string Constant1 = B.Constant1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Constant1").WithArguments("default interface implementation", "8.0").WithLocation(6, 18),
                // (7,18): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     const string Constant2 = B.Constant2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Constant2").WithArguments("default interface implementation", "8.0").WithLocation(7, 18)
                );
        }

        [Fact]
        public void IndexerNameLookupGenericClass()
        {
            var source = @"
using System.Runtime.CompilerServices;

class A<T>
{
    public const string Constant1 = B<string>.Constant1;
    public const string Constant2 = B<int>.Constant2;

    [IndexerName(B<byte>.Constant2)]
    public int this[long x] { get { return 0; } }
}

class B<T>
{
    public const string Constant1 = ""X"";
    public const string Constant2 = A<bool>.Constant2;

    [IndexerName(A<char>.Constant1)]
    public int this[int x] { get { return 0; } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,18): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [IndexerName(B<byte>.Constant2)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "B<byte>.Constant2"),
                // (7,25): error CS0110: The evaluation of the constant value for 'A<T>.Constant2' involves a circular definition
                //     public const string Constant2 = B<int>.Constant2;
                Diagnostic(ErrorCode.ERR_CircConstValue, "Constant2").WithArguments("A<T>.Constant2"));
        }

        [Fact]
        public void IndexerNameLookupGenericStruct()
        {
            var source = @"
using System.Runtime.CompilerServices;

struct A<T>
{
    public const string Constant1 = B<string>.Constant1;
    public const string Constant2 = B<int>.Constant2;

    [IndexerName(B<byte>.Constant2)]
    public int this[long x] { get { return 0; } }
}

struct B<T>
{
    public const string Constant1 = ""X"";
    public const string Constant2 = A<bool>.Constant2;

    [IndexerName(A<char>.Constant1)]
    public int this[int x] { get { return 0; } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,18): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [IndexerName(B<byte>.Constant2)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "B<byte>.Constant2"),
                // (7,25): error CS0110: The evaluation of the constant value for 'A<T>.Constant2' involves a circular definition
                //     public const string Constant2 = B<int>.Constant2;
                Diagnostic(ErrorCode.ERR_CircConstValue, "Constant2").WithArguments("A<T>.Constant2"));
        }

        [Fact]
        public void IndexerNameLookupGenericInterface()
        {
            var source = @"
using System.Runtime.CompilerServices;

interface A<T>
{
    const string Constant1 = B<string>.Constant1;
    const string Constant2 = B<int>.Constant2;

    [IndexerName(B<byte>.Constant2)]
    int this[long x] { get; }
}

interface B<T>
{
    const string Constant1 = ""X"";
    const string Constant2 = A<bool>.Constant2;

    [IndexerName(A<char>.Constant1)]
    int this[int x] { get; }
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (9,18): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [IndexerName(B<byte>.Constant2)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "B<byte>.Constant2").WithLocation(9, 18),
                // (7,18): error CS0110: The evaluation of the constant value for 'A<T>.Constant2' involves a circular definition
                //     const string Constant2 = B<int>.Constant2;
                Diagnostic(ErrorCode.ERR_CircConstValue, "Constant2").WithArguments("A<T>.Constant2").WithLocation(7, 18),
                // (15,18): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     const string Constant1 = "X";
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Constant1").WithArguments("default interface implementation", "8.0").WithLocation(15, 18),
                // (16,18): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     const string Constant2 = A<bool>.Constant2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Constant2").WithArguments("default interface implementation", "8.0").WithLocation(16, 18),
                // (6,18): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     const string Constant1 = B<string>.Constant1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Constant1").WithArguments("default interface implementation", "8.0").WithLocation(6, 18),
                // (7,18): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     const string Constant2 = B<int>.Constant2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Constant2").WithArguments("default interface implementation", "8.0").WithLocation(7, 18)
                );
        }

        [Fact]
        public void IndexerNameLookupTypeParameter()
        {
            var source = @"
using System.Runtime.CompilerServices;

class P
{
    public const string Constant1 = Q.Constant1;
    public const string Constant2 = Q.Constant2;
}

class Q
{
    public const string Constant1 = ""X"";
    public const string Constant2 = P.Constant2;
}

class A<T> where T : P
{
    [IndexerName(T.Constant1)]
    public int this[long x] { get { return 0; } }
}

class B<T> where T : Q
{
    [IndexerName(T.Constant2)]
    public int this[long x] { get { return 0; } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,25): error CS0110: The evaluation of the constant value for 'P.Constant2' involves a circular definition
                //     public const string Constant2 = Q.Constant2;
                Diagnostic(ErrorCode.ERR_CircConstValue, "Constant2").WithArguments("P.Constant2"),
                // (18,18): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
                //     [IndexerName(T.Constant1)]
                Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(18, 18),
                // (24,18): error CS0704: Cannot do non-virtual member lookup in 'T' because it is a type parameter
                //     [IndexerName(T.Constant2)]
                Diagnostic(ErrorCode.ERR_LookupInTypeVariable, "T").WithArguments("T").WithLocation(24, 18));
        }

        [Fact]
        public void IndexerNameLookupEnum()
        {
            var source = @"
using System.Runtime.CompilerServices;

enum E
{
    A,
    B,
    C = 6,
    D,
    E = F,
    F = E
}

class A
{
    [IndexerName(E.A)]
    public int this[long x] { get { return 0; } }

    [IndexerName(E.B)]
    public int this[char x] { get { return 0; } }

    [IndexerName(E.C)]
    public int this[bool x] { get { return 0; } }

    [IndexerName(E.D)]
    public int this[uint x] { get { return 0; } }

    [IndexerName(E.E)]
    public int this[byte x] { get { return 0; } }

    [IndexerName(E.F)]
    public int this[ulong x] { get { return 0; } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,5): error CS0110: The evaluation of the constant value for 'E.E' involves a circular definition
                //     E = F,
                Diagnostic(ErrorCode.ERR_CircConstValue, "E").WithArguments("E.E"),
                // (16,18): error CS1503: Argument 1: cannot convert from 'E' to 'string'
                //     [IndexerName(E.A)]
                Diagnostic(ErrorCode.ERR_BadArgType, "E.A").WithArguments("1", "E", "string"),
                // (19,18): error CS1503: Argument 1: cannot convert from 'E' to 'string'
                //     [IndexerName(E.B)]
                Diagnostic(ErrorCode.ERR_BadArgType, "E.B").WithArguments("1", "E", "string"),
                // (22,18): error CS1503: Argument 1: cannot convert from 'E' to 'string'
                //     [IndexerName(E.C)]
                Diagnostic(ErrorCode.ERR_BadArgType, "E.C").WithArguments("1", "E", "string"),
                // (25,18): error CS1503: Argument 1: cannot convert from 'E' to 'string'
                //     [IndexerName(E.D)]
                Diagnostic(ErrorCode.ERR_BadArgType, "E.D").WithArguments("1", "E", "string"),
                // (28,18): error CS1503: Argument 1: cannot convert from 'E' to 'string'
                //     [IndexerName(E.E)]
                Diagnostic(ErrorCode.ERR_BadArgType, "E.E").WithArguments("1", "E", "string"),
                // (31,18): error CS1503: Argument 1: cannot convert from 'E' to 'string'
                //     [IndexerName(E.F)]
                Diagnostic(ErrorCode.ERR_BadArgType, "E.F").WithArguments("1", "E", "string"));
        }

        [Fact]
        public void IndexerNameLookupProperties()
        {
            var source = @"
using System.Runtime.CompilerServices;

class A
{
    internal static string Name { get { return ""A""; } }
    [IndexerName(B.Name)]
    public int this[int x] { get { return 0; } }
}
class B
{
    internal static string Name { get { return ""B""; } }
    [IndexerName(A.Name)]
    public int this[int x] { get { return 0; } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,18): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [IndexerName(A.Name)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "A.Name"),
                // (7,18): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [IndexerName(B.Name)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "B.Name"));
        }

        [Fact]
        public void IndexerNameLookupCalls()
        {
            var source = @"
using System.Runtime.CompilerServices;

class A
{
    internal static string GetName() { return ""A""; }
    [IndexerName(B.GetName())]
    public int this[int x] { get { return 0; } }
}
class B
{
    internal static string GetName() { return ""B""; }
    [IndexerName(A.GetName())]
    public int this[int x] { get { return 0; } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,18): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [IndexerName(B.GetName())]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "B.GetName()"),
                // (13,18): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [IndexerName(A.GetName())]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "A.GetName()"));
        }

        [Fact]
        public void IndexerNameLookupNonExistent()
        {
            var source = @"
using System.Runtime.CompilerServices;

class A
{
    [IndexerName(B.Fake)]
    public int this[int x] { get { return 0; } }
}
class B
{
    [IndexerName(A.Fake)]
    public int this[int x] { get { return 0; } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,20): error CS0117: 'A' does not contain a definition for 'Fake'
                //     [IndexerName(A.Fake)]
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Fake").WithArguments("A", "Fake"),
                // (6,20): error CS0117: 'B' does not contain a definition for 'Fake'
                //     [IndexerName(B.Fake)]
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Fake").WithArguments("B", "Fake"));
        }

        [Fact]
        public void IndexerNameNotEmitted()
        {
            var source = @"
using System.Runtime.CompilerServices;

class Program
{
    [IndexerName(""A"")]
    public int this[int x]
    {
        get { return 0; }
        set { }
    }
}
";
            var compilation = CreateCompilation(source).VerifyDiagnostics();

            var indexer = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Program").Indexers.Single();
            Assert.True(indexer.IsIndexer);
            Assert.Equal("A", indexer.MetadataName);
            Assert.True(indexer.GetAttributes().Single().IsTargetAttribute(AttributeDescription.IndexerNameAttribute));

            CompileAndVerify(compilation, symbolValidator: module =>
            {
                var peIndexer = (PEPropertySymbol)module.GlobalNamespace.GetTypeMember("Program").Indexers.Single();
                Assert.True(peIndexer.IsIndexer);
                Assert.Equal("A", peIndexer.MetadataName);
                Assert.Empty(peIndexer.GetAttributes());
                Assert.Empty(((PEModuleSymbol)module).GetCustomAttributesForToken(peIndexer.Handle));
            });
        }

        [WorkItem(545884, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545884")]
        [Fact]
        public void IndexerNameDeadlock1()
        {
            var source = @"
using System.Runtime.CompilerServices;

class A
{
    public const string Name = ""A"";
    [IndexerName(B.Name)]
    public int this[int x] { get { return 0; } }
}

class B
{
    public const string Name = ""B"";
    [IndexerName(A.Name)]
    public int this[int x] { get { return 0; } }
}
";
            var compilation = CreateCompilation(source);

            var loopResult = Parallel.ForEach(compilation.GlobalNamespace.GetTypeMembers(), type =>
                type.ForceComplete(null, filter: null, default(CancellationToken)));

            Assert.True(loopResult.IsCompleted);

            compilation.VerifyDiagnostics();
        }

        [WorkItem(545884, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545884")]
        [Fact]
        public void IndexerNameDeadlock2()
        {
            var source = @"
using System.Runtime.CompilerServices;

class A
{
    private const string Name = ""A"";
    [IndexerName(B.Name)]
    public int this[int x] { get { return 0; } }
}

class B
{
    private const string Name = ""B"";
    [IndexerName(A.Name)]
    public int this[int x] { get { return 0; } }
}
";
            var compilation = CreateCompilation(source);

            var loopResult = Parallel.ForEach(compilation.GlobalNamespace.GetTypeMembers(), type =>
                type.ForceComplete(null, filter: null, default(CancellationToken)));

            Assert.True(loopResult.IsCompleted);

            compilation.VerifyDiagnostics(
                // (7,20): error CS0122: 'B.Name' is inaccessible due to its protection level
                //     [IndexerName(B.Name)]
                Diagnostic(ErrorCode.ERR_BadAccess, "Name").WithArguments("B.Name"),
                // (14,20): error CS0122: 'A.Name' is inaccessible due to its protection level
                //     [IndexerName(A.Name)]
                Diagnostic(ErrorCode.ERR_BadAccess, "Name").WithArguments("A.Name"));
        }

        [Fact]
        public void OverloadResolutionPrecedence()
        {
            var source =
@"public class C
{
    public int this[int x] { get { return 0; } }
    public int this[int x, int y = 1] { get { return 0; } }
    public int this[params int[] x] { get { return 0; } }

    void Method()
    {
        int x;
        x = this[1];
        x = this[1, 2];
        x = this[1, 2, 3];
        x = this[new int[1]];
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree);

            CheckOverloadResolutionResults(tree, model,
                "System.Int32 C.this[System.Int32 x] { get; }",
                "System.Int32 C.this[System.Int32 x, [System.Int32 y = 1]] { get; }",
                "System.Int32 C.this[params System.Int32[] x] { get; }",
                "System.Int32 C.this[params System.Int32[] x] { get; }");
        }

        [Fact]
        public void OverloadResolutionOverriding()
        {
            var source =
@"public class Base
{
    public virtual int this[int x] { get { return 0; } }
    public virtual int this[int x, int y = 1] { get { return 0; } }
    public virtual int this[params int[] x] { get { return 0; } }
}

public class Derived : Base
{
    public override int this[int x] { get { return 0; } }
    public override int this[int x, int y = 1] { get { return 0; } }
    public override int this[params int[] x] { get { return 0; } }

    void Method()
    {
        int x;
        x = this[1];
        x = this[1, 2];
        x = this[1, 2, 3];
        x = base[1];
        x = base[1, 2];
        x = base[1, 2, 3];
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree);

            CheckOverloadResolutionResults(tree, model,
                // NOTE: we'll actually emit calls to the corresponding base indexers
                "System.Int32 Derived.this[System.Int32 x] { get; }",
                "System.Int32 Derived.this[System.Int32 x, [System.Int32 y = 1]] { get; }",
                "System.Int32 Derived.this[params System.Int32[] x] { get; }",

                "System.Int32 Base.this[System.Int32 x] { get; }",
                "System.Int32 Base.this[System.Int32 x, [System.Int32 y = 1]] { get; }",
                "System.Int32 Base.this[params System.Int32[] x] { get; }");
        }

        [Fact]
        public void OverloadResolutionFallbackInBase()
        {
            var source =
@"public class Base
{
    public int this[params int[] x] { get { return 0; } }
}

public class Derived : Base
{
    public int this[int x] { get { return 0; } }
    public int this[int x, int y = 1] { get { return 0; } }

    void Method()
    {
        int x;
        x = this[1];
        x = this[1, 2];
        x = this[1, 2, 3];
        x = base[1];
        x = base[1, 2];
        x = base[1, 2, 3];
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree);

            CheckOverloadResolutionResults(tree, model,
                "System.Int32 Derived.this[System.Int32 x] { get; }",
                "System.Int32 Derived.this[System.Int32 x, [System.Int32 y = 1]] { get; }",
                "System.Int32 Base.this[params System.Int32[] x] { get; }",
                "System.Int32 Base.this[params System.Int32[] x] { get; }",
                "System.Int32 Base.this[params System.Int32[] x] { get; }",
                "System.Int32 Base.this[params System.Int32[] x] { get; }");
        }

        [Fact]
        public void OverloadResolutionDerivedRemovesParamsModifier()
        {
            var source =
@"abstract class Base
{
    public abstract int this[Derived c1, Derived c2, params Derived[] c3] { get; }
}
class Derived : Base
{
    public override int this[Derived C1, Derived C2, Derived[] C3] { get { return 0; } } //removes 'params'
}
class Test2
{
    public static void Main2()
    {
        Derived d = new Derived();
        Base b = d;
        int x;
        x = b[d, d, d, d, d]; // Fine
        x = d[d, d, d, d, d]; // Fine
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree);

            CheckOverloadResolutionResults(tree, model,
                "System.Int32 Base.this[Derived c1, Derived c2, params Derived[] c3] { get; }",
                "System.Int32 Derived.this[Derived C1, Derived C2, params Derived[] C3] { get; }");
        }

        [Fact]
        public void OverloadResolutionDerivedAddsParamsModifier()
        {
            var source =
@"abstract class Base
{
    public abstract int this[Derived c1, Derived c2, Derived[] c3] { get; }
}
class Derived : Base
{
    public override int this[Derived C1, Derived C2, params Derived[] C3] { get { return 0; } } //adds 'params'
}
class Test2
{
    public static void Main2()
    {
        Derived d = new Derived();
        Base b = d;
        int x;
        x = b[d, d, d, d, d]; // CS1501
        x = d[d, d, d, d, d]; // CS1501
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (16,13): error CS1501: No overload for method 'this' takes 5 arguments
                Diagnostic(ErrorCode.ERR_BadArgCount, "b[d, d, d, d, d]").WithArguments("this", "5"),
                // (17,13): error CS1501: No overload for method 'this' takes 5 arguments
                Diagnostic(ErrorCode.ERR_BadArgCount, "d[d, d, d, d, d]").WithArguments("this", "5"));
        }

        [WorkItem(542747, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542747")]
        [Fact()]
        public void IndexerAccessorParameterIsSynthesized()
        {
            var text = @"
struct Test
{
    public byte this[byte p] { get { return p; } }
}
";
            var comp = CreateCompilation(text);
            NamedTypeSymbol type01 = comp.SourceModule.GlobalNamespace.GetTypeMembers("Test").Single();
            var indexer = type01.GetMembers(WellKnownMemberNames.Indexer).Single() as PropertySymbol;
            Assert.NotNull(indexer.GetMethod);
            Assert.False(indexer.GetMethod.Parameters.IsEmpty);
            // VB is SynthesizedParameterSymbol; C# is SourceComplexParameterSymbol
            foreach (var p in indexer.GetMethod.Parameters)
            {
                Assert.True(p.IsImplicitlyDeclared, "Parameter of Indexer Accessor");
            }
        }

        [WorkItem(542831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542831")]
        [Fact]
        public void ProtectedBaseIndexer()
        {
            var text = @"
public class Base
{
    protected int this[int index] { get { return 0; } }
}
public class Derived : Base
{
    public int M()
    {
        return base[0];
    }
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void SameSignaturesDifferentNames()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit SameSignaturesDifferentNames
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Accessor1')}

  .method public hidebysig specialname instance int32 
          Accessor1(int32 x, int64 y) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname instance void 
          Accessor2(int32 x, int64 y,
                   int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          Accessor3(int32 x, int64 y,
                   int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 Indexer1(int32, int64)
  {
    .get instance int32 SameSignaturesDifferentNames::Accessor1(int32, int64)
    .set instance void SameSignaturesDifferentNames::Accessor2(int32, int64, int32)
  }

  .property instance int32 Indexer2(int32, int64)
  {
    .get instance int32 SameSignaturesDifferentNames::Accessor1(int32, int64)
    .set instance void SameSignaturesDifferentNames::Accessor3(int32, int64, int32)
  }
}";

            var cSharpSource = @"
class Test
{
    static void Main()
    {
        SameSignaturesDifferentNames s = new SameSignaturesDifferentNames();
        System.Console.WriteLine(s[0, 1]);
    }
}
";
            CreateCompilationWithILAndMscorlib40(cSharpSource, ilSource).VerifyDiagnostics(
                // (7,34): error CS0121: The call is ambiguous between the following methods or properties: 'SameSignaturesDifferentNames.this[int, long]' and 'SameSignaturesDifferentNames.this[int, long]'
                Diagnostic(ErrorCode.ERR_AmbigCall, "s[0, 1]").WithArguments("SameSignaturesDifferentNames.this[int, long]", "SameSignaturesDifferentNames.this[int, long]"));
        }

        [WorkItem(543261, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543261")]
        [ClrOnlyFact]
        public void OverrideOneAccessorOnly()
        {
            var source =
@"class A
{
    public virtual object this[object index] { get { return null; } set { } }
}
class B1 : A
{
    public override object this[object index] { get { return base[index]; } }
}
class B2 : A
{
    public override object this[object index] { set { base[index] = value; } }
}
class C
{
    static void M(B1 _1, B2 _2)
    {
        _1[null] = _1[null];
        _2[null] = _2[null];
    }
}";
            CompileAndVerify(source);
        }

        private static void CheckOverloadResolutionResults(SyntaxTree tree, SemanticModel model, params string[] expected)
        {
            var actual = GetElementAccessExpressions(tree.GetCompilationUnitRoot()).Select(syntax => model.GetSymbolInfo(syntax).Symbol.ToTestDisplayString());
            AssertEx.Equal(expected, actual, itemInspector: s => string.Format("\"{0}\"", s));
        }

        private static IEnumerable<ElementAccessExpressionSyntax> GetElementAccessExpressions(SyntaxNode node)
        {
            return node == null ?
                SpecializedCollections.EmptyEnumerable<ElementAccessExpressionSyntax>() :
                node.DescendantNodesAndSelf().Where(s => s.IsKind(SyntaxKind.ElementAccessExpression)).Cast<ElementAccessExpressionSyntax>();
        }

        [Fact]
        public void PartialType()
        {
            var text1 = @"
partial class C
{
    public int this[int x] { get { return 0; } set { } }
}";

            var text2 = @"

partial class C
{
    public void M() {}
}
";
            var compilation = CreateCompilation(new string[] { text1, text2 });
            Assert.True(((TypeSymbol)compilation.GlobalNamespace.GetTypeMembers("C").Single()).GetMembers().Any(x => x.IsIndexer()));

            //test with text inputs reversed in case syntax ordering predicate ever changes.
            compilation = CreateCompilation(new string[] { text2, text1 });
            Assert.True(((TypeSymbol)compilation.GlobalNamespace.GetTypeMembers("C").Single()).GetMembers().Any(x => x.IsIndexer()));
        }

        [WorkItem(543957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543957")]
        [Fact]
        public void SemanticModelIndexerGroupHiding()
        {
            var source =
@"public class Base
{
    public int this[int x] { get { return x; } }
    public virtual int this[int x, int y] { get { return x; } }
    public int this[int x, int y, int z] { get { return x; } }
}

public class Derived : Base
{
    public new int this[int x] { get { return x; } }
    public override int this[int x, int y] { get { return x; } }

    void Method()
    {
        int x;
        x = this[1];
        x = base[1];

        Derived d = new Derived();
        x = d[1];
        
        Base b = new Base();
        x = b[1];

        Wrapper w = new Wrapper();
        x = w.Base[1];
        x = w.Derived[1];

        x = (d ?? w.Derived)[1];
    }
}

public class Wrapper
{
    public Base Base;
    public Derived Derived;
}
";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree);

            var elementAccessSyntaxes = GetElementAccessExpressions(tree.GetCompilationUnitRoot());

            // The access itself doesn't have an indexer group.
            foreach (var syntax in elementAccessSyntaxes)
            {
                Assert.Equal(0, model.GetIndexerGroup(syntax).Length);
            }

            var baseType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var derivedType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");

            var baseIndexers = baseType.Indexers;
            var derivedIndexers = derivedType.Indexers;

            var baseIndexer3 = baseIndexers.Single(indexer => indexer.ParameterCount == 3);

            var baseIndexerGroup = baseIndexers;
            var derivedIndexerGroup = derivedIndexers.Concat(ImmutableArray.Create<PropertySymbol>(baseIndexer3));

            var receiverSyntaxes = elementAccessSyntaxes.Select(access => access.Expression);
            Assert.Equal(7, receiverSyntaxes.Count());

            // The receiver of each access expression has an indexer group.
            foreach (var syntax in receiverSyntaxes)
            {
                var type = model.GetTypeInfo(syntax).Type.GetSymbol();
                Assert.NotNull(type);

                var indexerGroup = model.GetIndexerGroup(syntax);

                if (type.Equals(baseType))
                {
                    Assert.True(indexerGroup.SetEquals(baseIndexerGroup.GetPublicSymbols(), EqualityComparer<IPropertySymbol>.Default));
                }
                else if (type.Equals(derivedType))
                {
                    Assert.True(indexerGroup.SetEquals(derivedIndexerGroup.GetPublicSymbols(), EqualityComparer<IPropertySymbol>.Default));
                }
                else
                {
                    Assert.True(false, "Unexpected type " + type.ToTestDisplayString());
                }
            }
        }

        [WorkItem(543957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543957")]
        [Fact]
        public void SemanticModelIndexerGroupAccessibility()
        {
            var source =
@"class Base
{
    private int this[int x] { get { return 0; } }
    protected int this[string x] { get { return 0; } }
    public int this[bool x] { get { return 0; } }

    void M()
    {
        int x;
        
        x = this[1]; //all
    }
}

class Derived1 : Base
{
    void M()
    {
        int x;

        x = this[""string""]; //public and protected

        Derived2 d = new Derived2();
        x = d[true]; //only public
    }
}

class Derived2 : Base
{
}
";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree);

            var elementAccessSyntaxes = GetElementAccessExpressions(tree.GetCompilationUnitRoot());

            // The access itself doesn't have an indexer group.
            foreach (var syntax in elementAccessSyntaxes)
            {
                Assert.Equal(0, model.GetIndexerGroup(syntax).Length);
            }

            var baseType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var derived1Type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived1");
            var derived2Type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived2");

            var indexers = baseType.Indexers;
            var publicIndexer = indexers.Single(indexer => indexer.DeclaredAccessibility == Accessibility.Public);
            var protectedIndexer = indexers.Single(indexer => indexer.DeclaredAccessibility == Accessibility.Protected);
            var privateIndexer = indexers.Single(indexer => indexer.DeclaredAccessibility == Accessibility.Private);

            var receiverSyntaxes = elementAccessSyntaxes.Select(access => access.Expression).ToArray();
            Assert.Equal(3, receiverSyntaxes.Length);

            // In declaring type, can see everything.
            Assert.True(model.GetIndexerGroup(receiverSyntaxes[0]).SetEquals(
                ImmutableArray.Create<PropertySymbol>(publicIndexer, protectedIndexer, privateIndexer).GetPublicSymbols(),
                EqualityComparer<IPropertySymbol>.Default));

            // In subtype of declaring type, can see non-private.
            Assert.True(model.GetIndexerGroup(receiverSyntaxes[1]).SetEquals(
                ImmutableArray.Create<PropertySymbol>(publicIndexer, protectedIndexer).GetPublicSymbols(),
                EqualityComparer<IPropertySymbol>.Default));

            // In subtype of declaring type, can only see public (or internal) members of other subtypes.
            Assert.True(model.GetIndexerGroup(receiverSyntaxes[2]).SetEquals(
                ImmutableArray.Create<PropertySymbol>(publicIndexer).GetPublicSymbols(),
                EqualityComparer<IPropertySymbol>.Default));
        }

        [WorkItem(545851, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545851")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void DistinctOptionalParameterValues()
        {
            var source1 =
@".class abstract public A
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
}";
            var reference1 = CompileIL(source1);
            var source2 =
@"using System;
class B : A
{
    public override int this[int x, int y = 3]
    {
        get
        {
            Console.WriteLine(""get_P: {0}"", y);
            return 0;
        }
        set
        {
            Console.WriteLine(""set_P: {0}"", y);
        }
    }
}
class C
{
    static void Main()
    {
        B b = new B();
        b[0] = b[0];
        b[1] += 1;
        A a = b;
        a[0] = a[0];
        a[1] += 1; // Dev11 uses get_P default for both
    }
}";
            var compilation2 = CompileAndVerify(source2, references: new[] { reference1 }, expectedOutput:
@"get_P: 3
set_P: 3
get_P: 3
set_P: 3
get_P: 1
set_P: 2
get_P: 1
set_P: 1");
        }

        [Fact, WorkItem(546255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546255")]
        public void RetargetingIndexerMetadataName()
        {
            #region "Source"
            var src1 = @"using System;
    public interface IGoo
    {
        int this[int i] { get; }
    }

    public class Goo : IGoo
    {
        public int this[int i] { get { return i; } }
    }
";

            var src2 = @"using System;
class Test
{
    public void M()
    {
        IGoo igoo = new Goo();
        var local = igoo[100];
    }
}
";
            #endregion

            var comp1 = CreateEmptyCompilation(src1, new[] { Net40.References.mscorlib });
            var comp2 = CreateCompilation(src2, new[] { new CSharpCompilationReference(comp1) });

            var typeSymbol = comp1.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("IGoo");
            var idxSymbol = typeSymbol.GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);
            Assert.NotNull(idxSymbol);
            Assert.Equal("this[]", idxSymbol.Name);
            Assert.Equal("Item", idxSymbol.MetadataName);

            var tree = comp2.SyntaxTrees[0];
            var model = comp2.GetSemanticModel(tree);
            ExpressionSyntax expr = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().FirstOrDefault();
            var idxSymbol2 = model.GetSymbolInfo(expr);
            Assert.NotNull(idxSymbol2.Symbol);
            Assert.Equal(WellKnownMemberNames.Indexer, idxSymbol2.Symbol.Name);
            Assert.Equal("Item", idxSymbol2.Symbol.MetadataName);
        }

        [Fact, WorkItem(546255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546255")]
        public void SubstitutedIndexerMetadataName()
        {
            var source = @"
class C<T>
{
    int this[int x] { get { return 0; } }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var unsubstitutedType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var unsubstitutedIndexer = unsubstitutedType.GetMember<SourcePropertySymbol>(WellKnownMemberNames.Indexer);

            Assert.Equal(WellKnownMemberNames.Indexer, unsubstitutedIndexer.Name);
            Assert.Equal("Item", unsubstitutedIndexer.MetadataName);

            var substitutedType = unsubstitutedType.Construct(comp.GetSpecialType(SpecialType.System_Int32));
            var substitutedIndexer = substitutedType.GetMember<SubstitutedPropertySymbol>(WellKnownMemberNames.Indexer);

            Assert.Equal(WellKnownMemberNames.Indexer, substitutedIndexer.Name);
            Assert.Equal("Item", substitutedIndexer.MetadataName);
        }

        [Fact, WorkItem(806258, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/806258")]
        public void ConflictWithTypeParameter()
        {
            var source = @"
class C<Item, get_Item>
{
    int this[int x] { get { return 0; } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,9): error CS0102: The type 'C<Item, get_Item>' already contains a definition for 'Item'
                //     int this[int x] { get { return 0; } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("C<Item, get_Item>", "Item"),
                // (4,23): error CS0102: The type 'C<Item, get_Item>' already contains a definition for 'get_Item'
                //     int this[int x] { get { return 0; } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "get").WithArguments("C<Item, get_Item>", "get_Item"));
        }

        [Fact, WorkItem(806258, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/806258")]
        public void ConflictWithTypeParameter_IndexerNameAttribute()
        {
            var source = @"
using System.Runtime.CompilerServices;

class C<A, get_A>
{
    [IndexerName(""A"")]
    int this[int x] { get { return 0; } }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): error CS0102: The type 'C<A, get_A>' already contains a definition for 'A'
                //     int this[int x] { get { return 0; } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("C<A, get_A>", "A"),
                // (7,23): error CS0102: The type 'C<A, get_A>' already contains a definition for 'get_A'
                //     int this[int x] { get { return 0; } }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "get").WithArguments("C<A, get_A>", "get_A"));
        }

        [Fact]
        public void IndexerNameNoConstantValue()
        {
            var source =
@"using System.Runtime.CompilerServices;
class C
{
    const string F;
    [IndexerName(F)]
    object this[object o] { get { return null; } }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,18): error CS0145: A const field requires a value to be provided
                //     const string F;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "F").WithLocation(4, 18),
                // (5,18): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [IndexerName(F)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "F").WithLocation(5, 18));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68110")]
        public void DefaultSyntaxValueReentrancy_01()
        {
            var source =
                """
                #nullable enable

                [A(3, X = 6)]
                public struct A
                {
                    public int X;

                    public A(int x, A a = new A()[1]) { }

                    public int this[int i] { get => 0; set { } }
                }
                """;
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);

            var a = compilation.GlobalNamespace.GetTypeMember("A").InstanceConstructors.Where(c => !c.IsDefaultValueTypeConstructor()).Single();

            Assert.Null(a.Parameters[1].ExplicitDefaultValue);
            Assert.True(a.Parameters[1].HasExplicitDefaultValue);

            compilation.VerifyDiagnostics(
                // (3,2): error CS0616: 'A' is not an attribute class
                // [A(3, X = 6)]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "A").WithArguments("A").WithLocation(3, 2),
                // (3,2): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(3, X = 6)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "A(3, X = 6)").WithLocation(3, 2),
                // (8,27): error CS1736: Default parameter value for 'a' must be a compile-time constant
                //     public A(int x, A a = new A()[1]) { }
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new A()[1]").WithArguments("a").WithLocation(8, 27));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43145")]
        public void CompilerShouldNotCrashOnSyntaxErrorInIndexerDeclaration()
        {
            var source =
                """
                struct S
                {
                    public bool This[int t] { get { return false; } }
                }
                """;
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            compilation.GetDiagnostics();
            compilation.VerifyDiagnostics(
                // (3,17): warning CS0649: Field 'S.This' is never assigned to, and will always have its default value false
                //     public bool This[int t] { get { return false; } }
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "This").WithArguments("S.This", "false").WithLocation(3, 17),
                // (3,21): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
                //     public bool This[int t] { get { return false; } }
                Diagnostic(ErrorCode.ERR_CStyleArray, "[int t]").WithLocation(3, 21),
                // (3,22): error CS1525: Invalid expression term 'int'
                //     public bool This[int t] { get { return false; } }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(3, 22),
                // (3,22): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //     public bool This[int t] { get { return false; } }
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "int").WithLocation(3, 22),
                // (3,26): error CS1003: Syntax error, ',' expected
                //     public bool This[int t] { get { return false; } }
                Diagnostic(ErrorCode.ERR_SyntaxError, "t").WithArguments(",").WithLocation(3, 26),
                // (3,26): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //     public bool This[int t] { get { return false; } }
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "t").WithLocation(3, 26),
                // (3,29): error CS1003: Syntax error, ',' expected
                //     public bool This[int t] { get { return false; } }
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(3, 29),
                // (3,31): error CS1002: ; expected
                //     public bool This[int t] { get { return false; } }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "get").WithLocation(3, 31),
                // (3,35): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
                //     public bool This[int t] { get { return false; } }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(3, 35),
                // (3,35): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
                //     public bool This[int t] { get { return false; } }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(3, 35),
                // (3,53): error CS1022: Type or namespace definition, or end-of-file expected
                //     public bool This[int t] { get { return false; } }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(3, 53),
                // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1));
        }
    }
}
