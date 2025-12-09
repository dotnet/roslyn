// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class EmitMetadata : EmitMetadataTestBase
    {
        [Fact]
        public void InstantiatedGenerics()
        {
            string source = @"
public class A<T>
{
    public class B : A<T>
    {
        internal class C : B
        {}

        protected B y1;
        protected A<D>.B y2;
    }

    public class H<S>
    {
        public class I : A<T>.H<S>
        {}
    }

    internal A<T> x1;
    internal A<D> x2;
}

public class D
{
    public class K<T>
    {
        public class L : K<T>
        {}
    }
}

namespace NS1
{
    class E : D
    {}
}

class F : A<D>
{}

class G : A<NS1.E>.B
{}

class J : A<D>.H<D>
{}

public class M
{}

public class N : D.K<M>
{}
";

            CompileAndVerify(source, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), symbolValidator: module =>
            {
                var dump = DumpTypeInfo(module).ToString();

                AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
<Global>
  <type name=""&lt;Module&gt;"" />
  <type name=""A"" Of=""T"" base=""System.Object"">
    <field name=""x1"" type=""A&lt;T&gt;"" />
    <field name=""x2"" type=""A&lt;D&gt;"" />
    <type name=""B"" base=""A&lt;T&gt;"">
      <field name=""y1"" type=""A&lt;T&gt;.B"" />
      <field name=""y2"" type=""A&lt;D&gt;.B"" />
      <type name=""C"" base=""A&lt;T&gt;.B"" />
    </type>
    <type name=""H"" Of=""S"" base=""System.Object"">
      <type name=""I"" base=""A&lt;T&gt;.H&lt;S&gt;"" />
    </type>
  </type>
  <type name=""D"" base=""System.Object"">
    <type name=""K"" Of=""T"" base=""System.Object"">
      <type name=""L"" base=""D.K&lt;T&gt;"" />
    </type>
  </type>
  <type name=""F"" base=""A&lt;D&gt;"" />
  <type name=""G"" base=""A&lt;NS1.E&gt;.B"" />
  <type name=""J"" base=""A&lt;D&gt;.H&lt;D&gt;"" />
  <type name=""M"" base=""System.Object"" />
  <type name=""N"" base=""D.K&lt;M&gt;"" />
  <NS1>
    <type name=""E"" base=""D"" />
  </NS1>
</Global>
", dump);
            }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));
        }

        [Fact]
        public void StringArrays()
        {
            string source = @"

public class D
{
    public D()
    {}

    public static void Main()
    {
        System.Console.WriteLine(65536);

        arrayField = new string[] {""string1"", ""string2""};
        System.Console.WriteLine(arrayField[1]);
        System.Console.WriteLine(arrayField[0]);
    }

    static string[] arrayField;
}
";

            CompileAndVerify(source, expectedOutput: @"
65536
string2
string1
"
            );
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void FieldRVA()
        {
            string source = @"

public class D
{
    public D()
    {}

    public static void Main()
    {
        byte[] a = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        System.Console.WriteLine(a[0]);
        System.Console.WriteLine(a[8]);
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
1
9
"
            );
        }

        [Fact]
        public void AssemblyRefs1()
        {
            var metadataTestLib1 = TestReferences.SymbolsTests.MDTestLib1;
            var metadataTestLib2 = TestReferences.SymbolsTests.MDTestLib2;

            string source = @"
public class Test : C107
{
}
";

            CompileAndVerifyWithMscorlib40(source, new[] { metadataTestLib1, metadataTestLib2 }, assemblyValidator: (assembly) =>
            {
                var refs = assembly.Modules[0].ReferencedAssemblies.OrderBy(r => r.Name).ToArray();
                Assert.Equal(2, refs.Length);
                Assert.Equal("MDTestLib1", refs[0].Name, StringComparer.OrdinalIgnoreCase);
                Assert.Equal("mscorlib", refs[1].Name, StringComparer.OrdinalIgnoreCase);
            });
        }

        [Fact]
        public void AssemblyRefs2()
        {
            string sources = @"
public class Test : Class2
{
}
";
            CompileAndVerifyWithMscorlib40(sources, new[] { TestReferences.SymbolsTests.MultiModule.Assembly }, verify: Verification.FailsILVerify, assemblyValidator: (assembly) =>
            {
                var refs2 = assembly.Modules[0].ReferencedAssemblies.Select(r => r.Name);
                Assert.Equal(2, refs2.Count());
                Assert.Contains("MultiModule", refs2, StringComparer.OrdinalIgnoreCase);
                Assert.Contains("mscorlib", refs2, StringComparer.OrdinalIgnoreCase);

                var peFileReader = assembly.GetMetadataReader();

                Assert.Equal(0, peFileReader.GetTableRowCount(TableIndex.File));
                Assert.Equal(0, peFileReader.GetTableRowCount(TableIndex.ModuleRef));
            });
        }

        [WorkItem(687434, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/687434")]
        [Fact()]
        public void Bug687434()
        {
            CompileAndVerify(
                "public class C { }",
                verify: Verification.Fails,
                options: TestOptions.DebugDll.WithOutputKind(OutputKind.NetModule));
        }

        [Fact, WorkItem(529006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529006")]
        public void AddModule()
        {
            var netModule1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule1).GetReference(filePath: Path.GetFullPath("netModule1.netmodule"));
            var netModule2 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule2).GetReference(filePath: Path.GetFullPath("netModule2.netmodule"));

            string source = @"
public class Test : Class1
{
}
";
            // modules not supported in ref emit
            // ILVerify: Assembly or module not found: netModule1
            CompileAndVerify(source, new[] { netModule1, netModule2 }, verify: Verification.FailsILVerify, assemblyValidator: (assembly) =>
            {
                Assert.Equal(3, assembly.Modules.Length);

                var reader = assembly.GetMetadataReader();

                Assert.Equal(2, reader.GetTableRowCount(TableIndex.File));

                var file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1));
                var file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2));
                Assert.Equal("netModule1.netmodule", reader.GetString(file1.Name));
                Assert.Equal("netModule2.netmodule", reader.GetString(file2.Name));

                Assert.False(file1.HashValue.IsNil);
                Assert.False(file2.HashValue.IsNil);

                Assert.Equal(1, reader.GetTableRowCount(TableIndex.ModuleRef));
                var moduleRefName = reader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(1)).Name;
                Assert.Equal("netModule1.netmodule", reader.GetString(moduleRefName));

                var actual = from h in reader.ExportedTypes
                             let et = reader.GetExportedType(h)
                             select $"{reader.GetString(et.NamespaceDefinition)}.{reader.GetString(et.Name)} 0x{MetadataTokens.GetToken(et.Implementation):X8} ({et.Implementation.Kind}) 0x{(int)et.Attributes:X4}";

                AssertEx.Equal(new[]
                {
                    ".Class1 0x26000001 (AssemblyFile) 0x0001",
                    ".Class3 0x27000001 (ExportedType) 0x0002",
                    "NS1.Class4 0x26000001 (AssemblyFile) 0x0001",
                    ".Class7 0x27000003 (ExportedType) 0x0002",
                    ".Class2 0x26000002 (AssemblyFile) 0x0001"
                }, actual);
            });
        }

        [Fact]
        public void ImplementingAnInterface()
        {
            string source = @"
public interface I1
{}

public class A : I1
{
}

public interface I2
{
    void M2();
}

public interface I3
{
    void M3();
}

abstract public class B : I2, I3
{
    public abstract void M2();
    public abstract void M3();
}
";

            CompileAndVerify(source, symbolValidator: module =>
            {
                var classA = module.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
                var classB = module.GlobalNamespace.GetMember<NamedTypeSymbol>("B");
                var i1 = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I1");
                var i2 = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I2");
                var i3 = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I3");

                Assert.Equal(TypeKind.Interface, i1.TypeKind);
                Assert.Equal(TypeKind.Interface, i2.TypeKind);
                Assert.Equal(TypeKind.Interface, i3.TypeKind);
                Assert.Equal(TypeKind.Class, classA.TypeKind);
                Assert.Equal(TypeKind.Class, classB.TypeKind);

                Assert.Same(i1, classA.Interfaces().Single());

                var interfaces = classB.Interfaces();
                Assert.Same(i2, interfaces[0]);
                Assert.Same(i3, interfaces[1]);

                Assert.Equal(1, i2.GetMembers("M2").Length);
                Assert.Equal(1, i3.GetMembers("M3").Length);
            });
        }

        [Fact]
        public void InterfaceOrder()
        {
            string source = @"
interface I1 : I2, I5 { }
interface I2 : I3, I4 { }
interface I3 { }
interface I4 { }
interface I5 : I6, I7 { }
interface I6 { }
interface I7 { }

class C : I1 { }
";

            CompileAndVerify(source, symbolValidator: module =>
            {
                var i1 = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I1");
                var i2 = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I2");
                var i3 = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I3");
                var i4 = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I4");
                var i5 = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I5");
                var i6 = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I6");
                var i7 = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I7");
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

                // Order is important - should be pre-order depth-first with declaration order at each level
                Assert.True(i1.Interfaces().SequenceEqual(ImmutableArray.Create<NamedTypeSymbol>(i2, i3, i4, i5, i6, i7)));
                Assert.True(i2.Interfaces().SequenceEqual(ImmutableArray.Create<NamedTypeSymbol>(i3, i4)));
                Assert.False(i3.Interfaces().Any());
                Assert.False(i4.Interfaces().Any());
                Assert.True(i5.Interfaces().SequenceEqual(ImmutableArray.Create<NamedTypeSymbol>(i6, i7)));
                Assert.False(i6.Interfaces().Any());
                Assert.False(i7.Interfaces().Any());

                Assert.True(c.Interfaces().SequenceEqual(ImmutableArray.Create<NamedTypeSymbol>(i1, i2, i3, i4, i5, i6, i7)));
            });
        }

        [Fact]
        public void ExplicitGenericInterfaceImplementation()
        {
            CompileAndVerify(@"
class S
{
    class C<T>
    {
        public interface I
        {
            void m(T x);
        }
    }
    abstract public class D : C<int>.I
    {
        void C<int>.I.m(int x)
        {
        }
    }
}
");
        }

        [Fact]
        public void TypeWithAbstractMethod()
        {
            string source = @"
abstract public class A
{
    public abstract A[] M1(ref System.Array p1);
    public abstract A[,] M2(System.Boolean p2);
    public abstract A[,,] M3(System.Char p3);
    public abstract void M4(System.SByte p4,
        System.Single p5,
        System.Double p6,
        System.Int16 p7,
        System.Int32 p8,
        System.Int64 p9,
        System.IntPtr p10,
        System.String p11,
        System.Byte p12,
        System.UInt16 p13,
        System.UInt32 p14,
        System.UInt64 p15,
        System.UIntPtr p16);
    public abstract void M5<T, S>(T p17, S p18);
}";

            CompileAndVerify(source, options: TestOptions.ReleaseDll, symbolValidator: module =>
            {
                var classA = module.GlobalNamespace.GetTypeMembers("A").Single();

                var m1 = classA.GetMembers("M1").OfType<MethodSymbol>().Single();
                var m2 = classA.GetMembers("M2").OfType<MethodSymbol>().Single();
                var m3 = classA.GetMembers("M3").OfType<MethodSymbol>().Single();
                var m4 = classA.GetMembers("M4").OfType<MethodSymbol>().Single();
                var m5 = classA.GetMembers("M5").OfType<MethodSymbol>().Single();

                var method1Ret = (ArrayTypeSymbol)m1.ReturnType;
                var method2Ret = (ArrayTypeSymbol)m2.ReturnType;
                var method3Ret = (ArrayTypeSymbol)m3.ReturnType;

                Assert.True(method1Ret.IsSZArray);
                Assert.Same(classA, method1Ret.ElementType);
                Assert.Equal(2, method2Ret.Rank);
                Assert.Same(classA, method2Ret.ElementType);
                Assert.Equal(3, method3Ret.Rank);
                Assert.Same(classA, method3Ret.ElementType);

                Assert.True(classA.IsAbstract);
                Assert.Equal(Accessibility.Public, classA.DeclaredAccessibility);

                var parameter1 = m1.Parameters.Single();
                var parameter1Type = parameter1.Type;

                Assert.Equal(RefKind.Ref, parameter1.RefKind);
                Assert.Same(module.GetCorLibType(SpecialType.System_Array), parameter1Type);
                Assert.Same(module.GetCorLibType(SpecialType.System_Boolean), m2.Parameters.Single().Type);
                Assert.Same(module.GetCorLibType(SpecialType.System_Char), m3.Parameters.Single().Type);

                var method4ParamTypes = m4.Parameters.Select(p => p.Type).ToArray();

                Assert.Same(module.GetCorLibType(SpecialType.System_Void), m4.ReturnType);
                Assert.Same(module.GetCorLibType(SpecialType.System_SByte), method4ParamTypes[0]);
                Assert.Same(module.GetCorLibType(SpecialType.System_Single), method4ParamTypes[1]);
                Assert.Same(module.GetCorLibType(SpecialType.System_Double), method4ParamTypes[2]);
                Assert.Same(module.GetCorLibType(SpecialType.System_Int16), method4ParamTypes[3]);
                Assert.Same(module.GetCorLibType(SpecialType.System_Int32), method4ParamTypes[4]);
                Assert.Same(module.GetCorLibType(SpecialType.System_Int64), method4ParamTypes[5]);
                Assert.Same(module.GetCorLibType(SpecialType.System_IntPtr), method4ParamTypes[6]);
                Assert.Same(module.GetCorLibType(SpecialType.System_String), method4ParamTypes[7]);
                Assert.Same(module.GetCorLibType(SpecialType.System_Byte), method4ParamTypes[8]);
                Assert.Same(module.GetCorLibType(SpecialType.System_UInt16), method4ParamTypes[9]);
                Assert.Same(module.GetCorLibType(SpecialType.System_UInt32), method4ParamTypes[10]);
                Assert.Same(module.GetCorLibType(SpecialType.System_UInt64), method4ParamTypes[11]);
                Assert.Same(module.GetCorLibType(SpecialType.System_UIntPtr), method4ParamTypes[12]);

                Assert.True(m5.IsGenericMethod);
                Assert.Same(m5.TypeParameters[0], m5.Parameters[0].Type);
                Assert.Same(m5.TypeParameters[1], m5.Parameters[1].Type);

                Assert.Equal(10, ((PEModuleSymbol)module).Module.GetMetadataReader().TypeReferences.Count);
            });
        }

        [Fact]
        public void Types()
        {
            string source = @"
sealed internal class B
{}

static class C
{
    public class D{}
    internal class E{}
    protected class F{}
    private class G{}
    protected internal class H{}
    class K{}
}
";
            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var classB = module.GlobalNamespace.GetTypeMembers("B").Single();
                Assert.True(classB.IsSealed);
                Assert.Equal(Accessibility.Internal, classB.DeclaredAccessibility);

                var classC = module.GlobalNamespace.GetTypeMembers("C").Single();
                Assert.True(classC.IsStatic);
                Assert.Equal(Accessibility.Internal, classC.DeclaredAccessibility);

                var classD = classC.GetTypeMembers("D").Single();
                var classE = classC.GetTypeMembers("E").Single();
                var classF = classC.GetTypeMembers("F").Single();
                var classH = classC.GetTypeMembers("H").Single();

                Assert.Equal(Accessibility.Public, classD.DeclaredAccessibility);
                Assert.Equal(Accessibility.Internal, classE.DeclaredAccessibility);
                Assert.Equal(Accessibility.Protected, classF.DeclaredAccessibility);
                Assert.Equal(Accessibility.ProtectedOrInternal, classH.DeclaredAccessibility);

                if (isFromSource)
                {
                    var classG = classC.GetTypeMembers("G").Single();
                    var classK = classC.GetTypeMembers("K").Single();
                    Assert.Equal(Accessibility.Private, classG.DeclaredAccessibility);
                    Assert.Equal(Accessibility.Private, classK.DeclaredAccessibility);
                }

                var peModuleSymbol = module as PEModuleSymbol;
                if (peModuleSymbol != null)
                {
                    Assert.Equal(5, peModuleSymbol.Module.GetMetadataReader().TypeReferences.Count);
                }
            };
            CompileAndVerify(source, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.ReleaseDll, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
        }

        [Fact]
        public void Fields()
        {
            string source = @"
public class A
{
    public int F1;
    internal volatile int F2;
    protected internal string F3;
    protected float F4;
    private double F5;
    char F6;
}";
            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var classA = module.GlobalNamespace.GetTypeMembers("A").Single();

                var f1 = classA.GetMembers("F1").OfType<FieldSymbol>().Single();
                var f2 = classA.GetMembers("F2").OfType<FieldSymbol>().Single();
                var f3 = classA.GetMembers("F3").OfType<FieldSymbol>().Single();
                var f4 = classA.GetMembers("F4").OfType<FieldSymbol>().Single();

                Assert.False(f1.IsVolatile);
                Assert.Equal(0, f1.TypeWithAnnotations.CustomModifiers.Length);

                Assert.True(f2.IsVolatile);
                Assert.Equal(1, f2.TypeWithAnnotations.CustomModifiers.Length);

                CustomModifier mod = f2.TypeWithAnnotations.CustomModifiers[0];

                Assert.Equal(Accessibility.Public, f1.DeclaredAccessibility);
                Assert.Equal(Accessibility.Internal, f2.DeclaredAccessibility);
                Assert.Equal(Accessibility.ProtectedOrInternal, f3.DeclaredAccessibility);
                Assert.Equal(Accessibility.Protected, f4.DeclaredAccessibility);

                if (isFromSource)
                {
                    var f5 = classA.GetMembers("F5").OfType<FieldSymbol>().Single();
                    var f6 = classA.GetMembers("F6").OfType<FieldSymbol>().Single();
                    Assert.Equal(Accessibility.Private, f5.DeclaredAccessibility);
                    Assert.Equal(Accessibility.Private, f6.DeclaredAccessibility);
                }

                Assert.False(mod.IsOptional);
                Assert.Equal("System.Runtime.CompilerServices.IsVolatile", mod.Modifier.ToTestDisplayString());
            };

            CompileAndVerify(source, sourceSymbolValidator: validator(true), symbolValidator: validator(false), options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));
        }

        [Fact]
        public void Constructors()
        {
            string source =
@"namespace N
{
    abstract class C
    {
        static C() {}
        protected C() {}
    }
}";
            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("N.C");
                var ctor = (MethodSymbol)type.GetMembers(".ctor").SingleOrDefault();
                var cctor = (MethodSymbol)type.GetMembers(".cctor").SingleOrDefault();

                Assert.NotNull(ctor);
                Assert.Equal(WellKnownMemberNames.InstanceConstructorName, ctor.Name);
                Assert.Equal(MethodKind.Constructor, ctor.MethodKind);
                Assert.Equal(Accessibility.Protected, ctor.DeclaredAccessibility);
                Assert.True(ctor.IsDefinition);
                Assert.False(ctor.IsStatic);
                Assert.False(ctor.IsAbstract);
                Assert.False(ctor.IsSealed);
                Assert.False(ctor.IsVirtual);
                Assert.False(ctor.IsOverride);
                Assert.False(ctor.IsGenericMethod);
                Assert.False(ctor.IsExtensionMethod);
                Assert.True(ctor.ReturnsVoid);
                Assert.False(ctor.IsVararg);
                // Bug - 2067
                Assert.Equal("N.C." + WellKnownMemberNames.InstanceConstructorName + "()", ctor.ToTestDisplayString());
                Assert.Equal(0, ctor.TypeParameters.Length);
                Assert.Equal("Void", ctor.ReturnTypeWithAnnotations.Type.Name);

                if (isFromSource)
                {
                    Assert.NotNull(cctor);
                    Assert.Equal(WellKnownMemberNames.StaticConstructorName, cctor.Name);
                    Assert.Equal(MethodKind.StaticConstructor, cctor.MethodKind);
                    Assert.Equal(Accessibility.Private, cctor.DeclaredAccessibility);
                    Assert.True(cctor.IsDefinition);
                    Assert.True(cctor.IsStatic);
                    Assert.False(cctor.IsAbstract);
                    Assert.False(cctor.IsSealed);
                    Assert.False(cctor.IsVirtual);
                    Assert.False(cctor.IsOverride);
                    Assert.False(cctor.IsGenericMethod);
                    Assert.False(cctor.IsExtensionMethod);
                    Assert.True(cctor.ReturnsVoid);
                    Assert.False(cctor.IsVararg);
                    // Bug - 2067
                    Assert.Equal("N.C." + WellKnownMemberNames.StaticConstructorName + "()", cctor.ToTestDisplayString());
                    Assert.Equal(0, cctor.TypeArgumentsWithAnnotations.Length);
                    Assert.Equal(0, cctor.Parameters.Length);
                    Assert.Equal("Void", cctor.ReturnTypeWithAnnotations.Type.Name);
                }
                else
                {
                    Assert.Null(cctor);
                }
            };
            CompileAndVerify(source, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
        }

        [Fact]
        public void ConstantFields()
        {
            string source =
@"class C
{
    private const int I = -1;
    internal const int J = I;
    protected internal const object O = null;
    public const string S = ""string"";
}
";
            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var type = module.GlobalNamespace.GetTypeMembers("C").Single();
                if (isFromSource)
                {
                    CheckConstantField(type, "I", Accessibility.Private, SpecialType.System_Int32, -1);
                }
                CheckConstantField(type, "J", Accessibility.Internal, SpecialType.System_Int32, -1);
                CheckConstantField(type, "O", Accessibility.ProtectedOrInternal, SpecialType.System_Object, null);
                CheckConstantField(type, "S", Accessibility.Public, SpecialType.System_String, "string");
            };

            CompileAndVerify(source: source, sourceSymbolValidator: validator(true), symbolValidator: validator(false), options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));
        }

        private void CheckConstantField(NamedTypeSymbol type, string name, Accessibility declaredAccessibility, SpecialType fieldType, object value)
        {
            var field = type.GetMembers(name).SingleOrDefault() as FieldSymbol;
            Assert.NotNull(field);
            Assert.True(field.IsStatic);
            Assert.True(field.IsConst);
            Assert.Equal(field.DeclaredAccessibility, declaredAccessibility);
            Assert.Equal(field.Type.SpecialType, fieldType);
            Assert.Equal(field.ConstantValue, value);
        }

        //the test for not importing internal members is elsewhere
        [Fact]
        public void DoNotImportPrivateMembers()
        {
            string source =
@"namespace Namespace
{
    public class Public { }
    internal class Internal { }
}
class Types
{
    public class Public { }
    internal class Internal { }
    protected class Protected { }
    protected internal class ProtectedInternal { }
    private class Private { }
}
class Fields
{
    public object Public = null;
    internal object Internal = null;
    protected object Protected = null;
    protected internal object ProtectedInternal = null;
    private object Private = null;
}
class Methods
{
    public void Public() { }
    internal void Internal() { }
    protected void Protected() { }
    protected internal void ProtectedInternal() { }
    private void Private() { }
}
class Properties
{
    public object Public { get; set; }
    internal object Internal { get; set; }
    protected object Protected { get; set; }
    protected internal object ProtectedInternal { get; set; }
    private object Private { get; set; }
}";
            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var nmspace = module.GlobalNamespace.GetMember<NamespaceSymbol>("Namespace");
                Assert.NotNull(nmspace.GetTypeMembers("Public").SingleOrDefault());
                Assert.NotNull(nmspace.GetTypeMembers("Internal").SingleOrDefault());

                CheckPrivateMembers(module.GlobalNamespace.GetTypeMembers("Types").Single(), isFromSource, true);
                CheckPrivateMembers(module.GlobalNamespace.GetTypeMembers("Fields").Single(), isFromSource, false);
                CheckPrivateMembers(module.GlobalNamespace.GetTypeMembers("Methods").Single(), isFromSource, false);
                CheckPrivateMembers(module.GlobalNamespace.GetTypeMembers("Properties").Single(), isFromSource, false);
            };

            CompileAndVerify(source: source, sourceSymbolValidator: validator(true), symbolValidator: validator(false), options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));
        }

        private void CheckPrivateMembers(NamedTypeSymbol type, bool isFromSource, bool importPrivates)
        {
            Symbol member;
            member = type.GetMembers("Public").SingleOrDefault();
            Assert.NotNull(member);
            member = type.GetMembers("Internal").SingleOrDefault();
            Assert.NotNull(member);
            member = type.GetMembers("Protected").SingleOrDefault();
            Assert.NotNull(member);
            member = type.GetMembers("ProtectedInternal").SingleOrDefault();
            Assert.NotNull(member);
            member = type.GetMembers("Private").SingleOrDefault();
            if (isFromSource || importPrivates)
            {
                Assert.NotNull(member);
            }
            else
            {
                Assert.Null(member);
            }
        }

        [Fact]
        public void GenericBaseTypeResolution()
        {
            string source =
@"class Base<T, U>
{
}
class Derived<T, U> : Base<T, U>
{
}";
            Action<ModuleSymbol> validator = module =>
            {
                var derivedType = module.GlobalNamespace.GetTypeMembers("Derived").Single();
                Assert.Equal(2, derivedType.Arity);

                var baseType = derivedType.BaseType();
                Assert.Equal("Base", baseType.Name);
                Assert.Equal(2, baseType.Arity);

                Assert.Equal(derivedType.BaseType(), baseType);
                Assert.Same(baseType.TypeArguments()[0], derivedType.TypeParameters[0]);
                Assert.Same(baseType.TypeArguments()[1], derivedType.TypeParameters[1]);
            };
            CompileAndVerify(source: source, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void ImportExplicitImplementations()
        {
            string source =
@"interface I
{
    void Method();
    object Property { get; set; }
}
class C : I
{
    void I.Method() { }
    object I.Property { get; set; }
}";
            Action<ModuleSymbol> validator = module =>
            {
                // Interface
                var type = module.GlobalNamespace.GetTypeMembers("I").Single();
                var method = (MethodSymbol)type.GetMembers("Method").Single();
                Assert.NotNull(method);
                var property = (PropertySymbol)type.GetMembers("Property").Single();
                Assert.NotNull(property.GetMethod);
                Assert.NotNull(property.SetMethod);

                // Implementation
                type = module.GlobalNamespace.GetTypeMembers("C").Single();
                method = (MethodSymbol)type.GetMembers("I.Method").Single();
                Assert.NotNull(method);
                property = (PropertySymbol)type.GetMembers("I.Property").Single();
                Assert.NotNull(property.GetMethod);
                Assert.NotNull(property.SetMethod);
            };
            CompileAndVerify(source: source, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void Properties()
        {
            string source =
@"public class C
{
    public int P1 { get { return 0; } set { } }
    internal int P2 { get { return 0; } }
    protected internal int P3 { get { return 0; } }
    protected int P4 { get { return 0; } }
    private int P5 { set { } }
    int P6 { get { return 0; } }
    public int P7 { private get { return 0; } set { } }
    internal int P8 { get { return 0; } private set { } }
    protected int P9 { get { return 0; } private set { } }
    protected internal int P10 { protected get { return 0; } set { } }
    protected internal int P11 { internal get { return 0; } set { } }
}";
            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var type = module.GlobalNamespace.GetTypeMembers("C").Single();
                var members = type.GetMembers();

                // Ensure member names are unique.
                var memberNames = members.Select(member => member.Name).Distinct().ToList();
                Assert.Equal(memberNames.Count, members.Length);

                var c = members.First(member => member.Name == ".ctor");
                Assert.NotNull(c);

                var p1 = (PropertySymbol)members.First(member => member.Name == "P1");
                var p2 = (PropertySymbol)members.First(member => member.Name == "P2");
                var p3 = (PropertySymbol)members.First(member => member.Name == "P3");
                var p4 = (PropertySymbol)members.First(member => member.Name == "P4");
                var p7 = (PropertySymbol)members.First(member => member.Name == "P7");
                var p8 = (PropertySymbol)members.First(member => member.Name == "P8");
                var p9 = (PropertySymbol)members.First(member => member.Name == "P9");
                var p10 = (PropertySymbol)members.First(member => member.Name == "P10");
                var p11 = (PropertySymbol)members.First(member => member.Name == "P11");

                var privateOrNotApplicable = isFromSource ? Accessibility.Private : Accessibility.NotApplicable;

                CheckPropertyAccessibility(p1, Accessibility.Public, Accessibility.Public, Accessibility.Public);
                CheckPropertyAccessibility(p2, Accessibility.Internal, Accessibility.Internal, Accessibility.NotApplicable);
                CheckPropertyAccessibility(p3, Accessibility.ProtectedOrInternal, Accessibility.ProtectedOrInternal, Accessibility.NotApplicable);
                CheckPropertyAccessibility(p4, Accessibility.Protected, Accessibility.Protected, Accessibility.NotApplicable);
                CheckPropertyAccessibility(p7, Accessibility.Public, privateOrNotApplicable, Accessibility.Public);
                CheckPropertyAccessibility(p8, Accessibility.Internal, Accessibility.Internal, privateOrNotApplicable);
                CheckPropertyAccessibility(p9, Accessibility.Protected, Accessibility.Protected, privateOrNotApplicable);
                CheckPropertyAccessibility(p10, Accessibility.ProtectedOrInternal, Accessibility.Protected, Accessibility.ProtectedOrInternal);
                CheckPropertyAccessibility(p11, Accessibility.ProtectedOrInternal, Accessibility.Internal, Accessibility.ProtectedOrInternal);

                if (isFromSource)
                {
                    var p5 = (PropertySymbol)members.First(member => member.Name == "P5");
                    var p6 = (PropertySymbol)members.First(member => member.Name == "P6");
                    CheckPropertyAccessibility(p5, Accessibility.Private, Accessibility.NotApplicable, Accessibility.Private);
                    CheckPropertyAccessibility(p6, Accessibility.Private, Accessibility.Private, Accessibility.NotApplicable);
                }
            };

            CompileAndVerify(source: source, sourceSymbolValidator: validator(true), symbolValidator: validator(false), options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));
        }

        [Fact]
        public void SetGetOnlyAutopropsInConstructors()
        {
            var comp = CreateCompilationWithMscorlib461(@"using System;
class C
{
    public int P1 { get; }
    public static int P2 { get; }

    public C()
    {
        P1 = 10;
    }

    static C()
    {
        P2 = 11;
    }
    
    static void Main()
    {
        Console.Write(C.P2);
        var c = new C();
        Console.Write(c.P1);
    }
}", options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: "1110");
        }

        [Fact]
        public void AutoPropInitializersClass()
        {
            var comp = CreateCompilation(@"using System;
class C
{
    public int P { get; set; } = 1;
    public string Q { get; set; } = ""test"";
    public decimal R { get; } = 300;
    public static char S { get; } = 'S';

    static void Main()
    {
        var c = new C();
        Console.Write(c.P);
        Console.Write(c.Q);
        Console.Write(c.R);
        Console.Write(C.S);
    }
}", parseOptions: TestOptions.Regular,
    options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.Internal));
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

                var p = type.GetMember<SourcePropertySymbol>("P");
                var pBack = p.BackingField;
                Assert.False(pBack.IsReadOnly);
                Assert.False(pBack.IsStatic);
                Assert.Equal(SpecialType.System_Int32, pBack.Type.SpecialType);

                var q = type.GetMember<SourcePropertySymbol>("Q");
                var qBack = q.BackingField;
                Assert.False(qBack.IsReadOnly);
                Assert.False(qBack.IsStatic);
                Assert.Equal(SpecialType.System_String, qBack.Type.SpecialType);

                var r = type.GetMember<SourcePropertySymbol>("R");
                var rBack = r.BackingField;
                Assert.True(rBack.IsReadOnly);
                Assert.False(rBack.IsStatic);
                Assert.Equal(SpecialType.System_Decimal, rBack.Type.SpecialType);

                var s = type.GetMember<SourcePropertySymbol>("S");
                var sBack = s.BackingField;
                Assert.True(sBack.IsReadOnly);
                Assert.True(sBack.IsStatic);
                Assert.Equal(SpecialType.System_Char, sBack.Type.SpecialType);
            };

            CompileAndVerify(
                comp,
                sourceSymbolValidator: validator,
                expectedOutput: "1test300S");
        }

        [Fact]
        public void AutoPropInitializersStruct()
        {
            var comp = CreateCompilation(@"
using System;
struct S
{
    public readonly int P;
    public string Q { get; }
    public decimal R { get; }
    public static char T { get; } = 'T';

    public S(int p)
    {
        P = p;
        Q = ""test"";
        R = 300;
    }

    static void Main()
    {
        var s = new S(1);
        Console.Write(s.P);
        Console.Write(s.Q);
        Console.Write(s.R);
        Console.Write(S.T);

        s = new S();
        Console.Write(s.P);
        Console.Write(s.Q ?? ""null"");
        Console.Write(s.R);
        Console.Write(S.T);
    }
}", parseOptions: TestOptions.Regular,
    options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.Internal));

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("S");

                var p = type.GetMember<SourceMemberFieldSymbol>("P");
                Assert.False(p.HasInitializer);
                Assert.True(p.IsReadOnly);
                Assert.False(p.IsStatic);
                Assert.Equal(SpecialType.System_Int32, p.Type.SpecialType);

                var q = type.GetMember<SourcePropertySymbol>("Q");
                var qBack = q.BackingField;
                Assert.True(qBack.IsReadOnly);
                Assert.False(qBack.IsStatic);
                Assert.Equal(SpecialType.System_String, qBack.Type.SpecialType);

                var r = type.GetMember<SourcePropertySymbol>("R");
                var rBack = r.BackingField;
                Assert.True(rBack.IsReadOnly);
                Assert.False(rBack.IsStatic);
                Assert.Equal(SpecialType.System_Decimal, rBack.Type.SpecialType);

                var s = type.GetMember<SourcePropertySymbol>("T");
                var sBack = s.BackingField;
                Assert.True(sBack.IsReadOnly);
                Assert.True(sBack.IsStatic);
                Assert.Equal(SpecialType.System_Char, sBack.Type.SpecialType);
            };

            CompileAndVerify(
                comp,
                sourceSymbolValidator: validator,
                expectedOutput: "1test300T0null0T");
        }

        /// <summary>
        /// Private accessors of a virtual property should not be virtual.
        /// </summary>
        [Fact]
        public void PrivatePropertyAccessorNotVirtual()
        {
            string source = @"
class C
{
    public virtual int P { get; private set; }
    public virtual int Q { get; internal set; }
}
class D : C
{
    public override int Q { internal set { } }
}
class E : D
{
    public override int Q { get { return 0; } }
}
class F : E
{
    public override int P { get { return 0; } }
    public override int Q { internal set { } }
}
class Program
{
    static void Main()
    {
    }
}
";
            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var type = module.GlobalNamespace.GetTypeMembers("C").Single();
                bool checkValidProperties = (type is PENamedTypeSymbol);

                var propertyP = (PropertySymbol)type.GetMembers("P").Single();
                if (isFromSource)
                {
                    CheckPropertyAccessibility(propertyP, Accessibility.Public, Accessibility.Public, Accessibility.Private);
                    Assert.False(propertyP.SetMethod.IsVirtual);
                    Assert.False(propertyP.SetMethod.IsOverride);
                }
                else
                {
                    CheckPropertyAccessibility(propertyP, Accessibility.Public, Accessibility.Public, Accessibility.NotApplicable);
                    Assert.Null(propertyP.SetMethod);
                }
                Assert.True(propertyP.GetMethod.IsVirtual);
                Assert.False(propertyP.GetMethod.IsOverride);
                var propertyQ = (PropertySymbol)type.GetMembers("Q").Single();
                CheckPropertyAccessibility(propertyQ, Accessibility.Public, Accessibility.Public, Accessibility.Internal);
                Assert.True(propertyQ.GetMethod.IsVirtual);
                Assert.False(propertyQ.GetMethod.IsOverride);
                Assert.True(propertyQ.SetMethod.IsVirtual);
                Assert.False(propertyQ.SetMethod.IsOverride);
                Assert.False(propertyQ.IsReadOnly);
                Assert.False(propertyQ.IsWriteOnly);
                if (checkValidProperties)
                {
                    Assert.False(propertyP.MustCallMethodsDirectly);
                    Assert.False(propertyQ.MustCallMethodsDirectly);
                }

                type = module.GlobalNamespace.GetTypeMembers("F").Single();
                propertyP = (PropertySymbol)type.GetMembers("P").Single();
                CheckPropertyAccessibility(propertyP, Accessibility.Public, Accessibility.Public, Accessibility.NotApplicable);
                Assert.False(propertyP.GetMethod.IsVirtual);
                Assert.True(propertyP.GetMethod.IsOverride);
                propertyQ = (PropertySymbol)type.GetMembers("Q").Single();
                // Derived property should be public even though the only
                // declared accessor on the derived property is internal.
                CheckPropertyAccessibility(propertyQ, Accessibility.Public, Accessibility.NotApplicable, Accessibility.Internal);
                Assert.False(propertyQ.SetMethod.IsVirtual);
                Assert.True(propertyQ.SetMethod.IsOverride);
                Assert.False(propertyQ.IsReadOnly);
                Assert.False(propertyQ.IsWriteOnly);
                if (checkValidProperties)
                {
                    Assert.False(propertyP.MustCallMethodsDirectly);
                    Assert.False(propertyQ.MustCallMethodsDirectly);
                }
                // Overridden property should be E but overridden
                // accessor should be D.set_Q.
                var overriddenProperty = module.GlobalNamespace.GetTypeMembers("E").Single().GetMembers("Q").Single();
                Assert.NotNull(overriddenProperty);
                Assert.Same(overriddenProperty, propertyQ.OverriddenProperty);
                var overriddenAccessor = module.GlobalNamespace.GetTypeMembers("D").Single().GetMembers("set_Q").Single();
                Assert.NotNull(overriddenProperty);
                Assert.Same(overriddenAccessor, propertyQ.SetMethod.OverriddenMethod);
            };
            CompileAndVerify(source: source, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
        }

        [Fact]
        public void InterfaceProperties()
        {
            string source = @"
interface I
{
    int P { get; set; }
}
public class C : I
{
    int I.P { get { return 0; } set { } }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetTypeMembers("C").Single();
                var members = type.GetMembers();
                var ip = (PropertySymbol)members.First(member => member.Name == "I.P");
                CheckPropertyAccessibility(ip, Accessibility.Private, Accessibility.Private, Accessibility.Private);
            };
            CompileAndVerify(source: source, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        private static void CheckPropertyAccessibility(PropertySymbol property, Accessibility propertyAccessibility, Accessibility getterAccessibility, Accessibility setterAccessibility)
        {
            var type = property.TypeWithAnnotations;
            Assert.NotEqual(Microsoft.Cci.PrimitiveTypeCode.Void, type.PrimitiveTypeCode);
            Assert.Equal(propertyAccessibility, property.DeclaredAccessibility);
            CheckPropertyAccessorAccessibility(property, propertyAccessibility, property.GetMethod, getterAccessibility);
            CheckPropertyAccessorAccessibility(property, propertyAccessibility, property.SetMethod, setterAccessibility);
        }

        private static void CheckPropertyAccessorAccessibility(PropertySymbol property, Accessibility propertyAccessibility, MethodSymbol accessor, Accessibility accessorAccessibility)
        {
            if (accessor == null)
            {
                Assert.Equal(Accessibility.NotApplicable, accessorAccessibility);
            }
            else
            {
                var containingType = property.ContainingType;
                Assert.Equal(property, accessor.AssociatedSymbol);
                Assert.Equal(containingType, accessor.ContainingType);
                Assert.Equal(containingType, accessor.ContainingSymbol);
                var method = containingType.GetMembers(accessor.Name).Single();
                Assert.Equal(method, accessor);
                Assert.Equal(accessorAccessibility, accessor.DeclaredAccessibility);
            }
        }

        // Property/method override should succeed (and should reference
        // the correct base method, even if there is a method/property
        // with the same name in an intermediate class.
        [WorkItem(538720, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538720")]
        [Fact]
        public void TestPropertyOverrideGet()
        {
            PropertyOverrideGet(@"
class A
{
    public virtual int P { get { return 0; } }
}
class B : A
{
    public virtual int get_P() { return 0; }
}
class C : B
{
    public override int P { get { return 0; } }
}
");
            PropertyOverrideGet(@"
class A
{
    public virtual int get_P() { return 0; }
}
class B : A
{
    public virtual int P { get { return 0; } }
}
class C : B
{
    public override int get_P() { return 0; }
}
");
        }

        private void PropertyOverrideGet(string source)
        {
            Action<ModuleSymbol> validator = module =>
            {
                var typeA = module.GlobalNamespace.GetTypeMembers("A").Single();
                Assert.NotNull(typeA);
                var getMethodA = (MethodSymbol)typeA.GetMembers("get_P").Single();
                Assert.NotNull(getMethodA);
                Assert.True(getMethodA.IsVirtual);
                Assert.False(getMethodA.IsOverride);

                var typeC = module.GlobalNamespace.GetTypeMembers("C").Single();
                Assert.NotNull(typeC);
                var getMethodC = (MethodSymbol)typeC.GetMembers("get_P").Single();
                Assert.NotNull(getMethodC);
                Assert.False(getMethodC.IsVirtual);
                Assert.True(getMethodC.IsOverride);

                Assert.Same(getMethodC.OverriddenMethod, getMethodA);
            };
            CompileAndVerify(source: source, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void AutoProperties()
        {
            string source = @"
class A
{
    public int P { get; private set; }
    internal int Q { get; set; }
}
class B<T>
{
    protected internal T P { get; set; }
}
class C : B<string>
{
}
";
            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var classA = module.GlobalNamespace.GetTypeMember("A");
                var p = classA.GetProperty("P");
                VerifyAutoProperty(p, isFromSource);
                var q = classA.GetProperty("Q");
                VerifyAutoProperty(q, isFromSource);

                var classC = module.GlobalNamespace.GetTypeMembers("C").Single();
                p = classC.BaseType().GetProperty("P");
                VerifyAutoProperty(p, isFromSource);
                Assert.Equal(SpecialType.System_String, p.Type.SpecialType);
                Assert.Equal(p.GetMethod.AssociatedSymbol, p);
            };

            CompileAndVerify(
                source,
                sourceSymbolValidator: validator(true),
                symbolValidator: validator(false),
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
        }

        private static void VerifyAutoProperty(PropertySymbol property, bool isFromSource)
        {
            if (isFromSource)
            {
                if (property is SourcePropertySymbol sourceProperty)
                {
                    Assert.True(sourceProperty.IsAutoProperty);
                }
            }
            else
            {
                var backingField = property.ContainingType.GetField(GeneratedNames.MakeBackingFieldName(property.Name));
                var attribute = backingField.GetAttributes().Single();

                Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attribute.AttributeClass.ToTestDisplayString());
                Assert.Empty(attribute.AttributeConstructor.Parameters);
            }

            VerifyAutoPropertyAccessor(property, property.GetMethod);
            VerifyAutoPropertyAccessor(property, property.SetMethod);
        }

        private static void VerifyAutoPropertyAccessor(PropertySymbol property, MethodSymbol accessor)
        {
            if (accessor != null)
            {
                var method = property.ContainingType.GetMembers(accessor.Name).Single();
                Assert.Equal(method, accessor);
                Assert.Equal(accessor.AssociatedSymbol, property);
                Assert.False(accessor.IsImplicitlyDeclared, "MethodSymbol.IsImplicitlyDeclared should be false for auto property accessors");
            }
        }

        [Fact]
        public void EmptyEnum()
        {
            string source = "enum E {}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetTypeMembers("E").Single();
                CheckEnumType(type, Accessibility.Internal, SpecialType.System_Int32);
                Assert.Equal(1, type.GetMembers().Length);
            };
            CompileAndVerify(source: source, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void NonEmptyEnum()
        {
            string source =
@"enum E : short
{
    A,
    B = 0x02,
    C,
    D,
    E = B | D,
    F = C,
    G,
}
";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetTypeMembers("E").Single();
                CheckEnumType(type, Accessibility.Internal, SpecialType.System_Int16);

                Assert.Equal(8, type.GetMembers().Length);
                CheckEnumConstant(type, "A", (short)0);
                CheckEnumConstant(type, "B", (short)2);
                CheckEnumConstant(type, "C", (short)3);
                CheckEnumConstant(type, "D", (short)4);
                CheckEnumConstant(type, "E", (short)6);
                CheckEnumConstant(type, "F", (short)3);
                CheckEnumConstant(type, "G", (short)4);
            };
            CompileAndVerify(source: source, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        private void CheckEnumConstant(NamedTypeSymbol type, string name, object value)
        {
            var field = type.GetMembers(name).SingleOrDefault() as FieldSymbol;
            Assert.NotNull(field);
            Assert.True(field.IsStatic);
            Assert.True(field.IsConst);
            // TODO: DeclaredAccessibility should be NotApplicable.
            //Assert.Equal(field.DeclaredAccessibility, Accessibility.NotApplicable);
            Assert.Equal(field.Type, type);
            Assert.Equal(field.ConstantValue, value);

            var sourceType = type as SourceNamedTypeSymbol;
            if ((object)sourceType != null)
            {
                var fieldDefinition = (Microsoft.Cci.IFieldDefinition)field.GetCciAdapter();
                Assert.False(fieldDefinition.IsSpecialName);
                Assert.False(fieldDefinition.IsRuntimeSpecial);
            }
        }

        private void CheckEnumType(NamedTypeSymbol type, Accessibility declaredAccessibility, SpecialType underlyingType)
        {
            Assert.Equal(SpecialType.System_Enum, type.BaseType().SpecialType);
            Assert.Equal(type.EnumUnderlyingType.SpecialType, underlyingType);
            Assert.Equal(type.DeclaredAccessibility, declaredAccessibility);
            Assert.True(type.IsSealed);

            // value__ field should not be exposed from type, even though it is public,
            // since we want to prevent source from accessing the field directly.
            var field = type.GetMembers(WellKnownMemberNames.EnumBackingFieldName).SingleOrDefault() as FieldSymbol;
            Assert.Null(field);

            var sourceType = type as SourceNamedTypeSymbol;
            if ((object)sourceType != null)
            {
                field = sourceType.EnumValueField;
                Assert.NotNull(field);
                Assert.Equal(WellKnownMemberNames.EnumBackingFieldName, field.Name);
                Assert.False(field.IsStatic);
                Assert.False(field.IsConst);
                Assert.False(field.IsReadOnly);
                Assert.Equal(Accessibility.Public, field.DeclaredAccessibility); // Dev10: value__ is public
                Assert.Equal(field.Type, type.EnumUnderlyingType);

                var module = new PEAssemblyBuilder((SourceAssemblySymbol)sourceType.ContainingAssembly, EmitOptions.Default, OutputKind.DynamicallyLinkedLibrary,
                    GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable<ResourceDescription>());

                var context = new EmitContext(module, null, new DiagnosticBag(), metadataOnly: false, includePrivateMembers: true);

                var typeDefinition = (Microsoft.Cci.ITypeDefinition)type.GetCciAdapter();
                var fieldDefinition = typeDefinition.GetFields(context).First();
                Assert.Same(fieldDefinition.GetInternalSymbol(), field); // Dev10: value__ field is the first field.
                Assert.True(fieldDefinition.IsSpecialName);
                Assert.True(fieldDefinition.IsRuntimeSpecial);
                context.Diagnostics.Verify();
            }
        }

        [Fact]
        public void GenericMethods()
        {
            string source = @"
public class A
{
    public static void Main()
    {
        System.Console.WriteLine(""GenericMethods"");
        //B.Test<int>();
        //C<int>.Test<int>();
    }
}

public class B
{
    public static void Test<T>()
    {
        System.Console.WriteLine(""Test<T>"");
    }
}

public class C<T>
{
    public static void Test<S>()
    {
        System.Console.WriteLine(""C<T>.Test<S>"");
    }
}
";

            CompileAndVerify(source, expectedOutput: "GenericMethods\r\n");
        }

        [Fact]
        public void GenericMethods2()
        {
            string source = @"
class A
{
    public static void Main()
    {
        TC1 x = new TC1();
        System.Console.WriteLine(x.GetType());
        TC2<byte> y = new TC2<byte>();
        System.Console.WriteLine(y.GetType());
        TC3<byte>.TC4 z = new TC3<byte>.TC4();
        System.Console.WriteLine(z.GetType());
    }
}

class TC1
{
    void TM1<T1>()
    {
        TM1<T1>();
    }

    void TM2<T2>()
    {
        TM2<int>();
    }
}

class TC2<T3>
{
    void TM3<T4>()
    {
        TM3<T4>();
        TM3<T4>();
    }

    void TM4<T5>()
    {
        TM4<int>();
        TM4<int>();
    }

    static void TM5<T6>(T6 x)
    {
        TC2<int>.TM5(x);
    }

    static void TM6<T7>(T7 x)
    {
        TC2<int>.TM6(1);
    }

    void TM9()
    {
        TM9();
        TM9();
    }

}

class TC3<T8>
{
    public class TC4
    {
        void TM7<T9>()
        {
            TM7<T9>();
            TM7<int>();
        }

        static void TM8<T10>(T10 x)
        {
            TC3<int>.TC4.TM8(x);
            TC3<int>.TC4.TM8(1);
        }
    }
}

";

            var verifier = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput:
@"TC1
TC2`1[System.Byte]
TC3`1+TC4[System.Byte]
");

            verifier.VerifyIL("TC1.TM1<T1>",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void TC1.TM1<T1>()""
  IL_0006:  ret
}
");

            verifier.VerifyIL("TC1.TM2<T2>",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void TC1.TM2<int>()""
  IL_0006:  ret
}
");

            verifier.VerifyIL("TC2<T3>.TM3<T4>",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void TC2<T3>.TM3<T4>()""
  IL_0006:  ldarg.0
  IL_0007:  call       ""void TC2<T3>.TM3<T4>()""
  IL_000c:  ret
}
");

            verifier.VerifyIL("TC2<T3>.TM4<T5>",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void TC2<T3>.TM4<int>()""
  IL_0006:  ldarg.0
  IL_0007:  call       ""void TC2<T3>.TM4<int>()""
  IL_000c:  ret
}
");

            verifier.VerifyIL("TC2<T3>.TM5<T6>",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""void TC2<int>.TM5<T6>(T6)""
  IL_0006:  ret
}
");

            verifier.VerifyIL("TC2<T3>.TM6<T7>",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""void TC2<int>.TM6<int>(int)""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void Generics3()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        C1<Byte, Byte> x1 = new C1<Byte, Byte>();
        C1<Byte, Byte>.C2<Byte, Byte> x2 = new C1<Byte, Byte>.C2<Byte, Byte>();
        C1<Byte, Byte>.C2<Byte, Byte>.C3<Byte, Byte> x3 = new C1<Byte, Byte>.C2<Byte, Byte>.C3<Byte, Byte>();
        C1<Byte, Byte>.C2<Byte, Byte>.C3<Byte, Byte>.C4<Byte> x4 = new C1<Byte, Byte>.C2<Byte, Byte>.C3<Byte, Byte>.C4<Byte>();
        C1<Byte, Byte>.C5 x5 = new C1<Byte, Byte>.C5();
    }
}


class C1<C1T1, C1T2>
{
    public class C2<C2T1, C2T2>
    {
        public class C3<C3T1, C3T2> where C3T2 : C1T1
        {
            public class C4<C4T1>
            {
            }
        }

        public C1<int, C2T2>.C5 V1;
        public C1<C2T1, C2T2>.C5 V2;
        public C1<int, int>.C5 V3;

        public C2<Byte, Byte> V4;

        public C1<C1T2, C1T1>.C2<C2T1, C2T2> V5;
        public C1<C1T2, C1T1>.C2<C2T2, C2T1> V6;
        public C1<C1T2, C1T1>.C2<Byte, int> V7;
        public C2<C2T1, C2T2> V8;
        public C2<Byte, C2T2> V9;

        void Test12(C2<int, int> x)
        {
            C1<C1T1, C1T2>.C2<Byte, int> y = x.V9;
        }

        void Test11(C1<int, int>.C2<Byte, Byte> x)
        {
            C1<int, int>.C2<Byte, Byte> y = x.V8;
        }

        void Test6(C1<C1T2, C1T1>.C2<C2T1, C2T2> x)
        {
            C1<C1T1, C1T2>.C2<C2T1, C2T2> y = x.V5;
        }

        void Test7(C1<C1T2, C1T1>.C2<C2T2, C2T1> x)
        {
            C1<C1T1, C1T2>.C2<C2T1, C2T2> y = x.V6;
        }

        void Test8(C1<C1T2, C1T1>.C2<C2T2, C2T1> x)
        {
            C1<C1T1, C1T2>.C2<Byte, int> y = x.V7;
        }

        void Test9(C1<int, Byte>.C2<C2T2, C2T1> x)
        {
            C1<Byte, int>.C2<Byte, int> y = x.V7;
        }

        void Test10(C1<C1T1, C1T2>.C2<C2T2, C2T1> x)
        {
            C1<C1T2, C1T1>.C2<Byte, int> y = x.V7;
        }
    }

    public class C5
    {
    }

    void Test1(C2<C1T1, int> x)
    {
        C1<int, int>.C5 y = x.V1;
    }

    void Test2(C2<C1T1, C1T2> x)
    {
        C5 y = x.V2;
    }

    void Test3(C2<C1T2, C1T1> x)
    {
        C1<int, int>.C5 y = x.V3;
    }

    void Test4(C1<int, int>.C2<C1T1, C1T2> x)
    {
        C1<int, int>.C2<Byte, Byte> y = x.V4;
    }
}

";
            CompileAndVerify(source);
        }

        [Fact]
        public void RefEmit_UnsupportedOrdering1()
        {
            CompileAndVerify(@"
public class E
{
  public struct N2
  { 
    public N3 n1;
  }
  public struct N3
  { 
  }
  N2 n2; 
}
");
        }

        [Fact]
        public void RefEmit_UnsupportedOrdering1_EP()
        {
            string source = @"
public class E
{
  public struct N2
  { 
    public N3 n1;
  }
  public struct N3
  { 
  }
  N2 n2; 

  public static void Main()
  {
    System.Console.Write(1234);
  }
}";

            CompileAndVerify(source, expectedOutput: @"1234");
        }

        [Fact]
        public void RefEmit_UnsupportedOrdering2()
        {
            CompileAndVerify(@"
class B<T> where T : A {}
class A : B<A> {}
");
        }

        [Fact]
        public void RefEmit_MembersOfOpenGenericType()
        {
            CompileAndVerify(@"
class C<T> 
{
    void goo() 
    {
        System.Collections.Generic.Dictionary<int, T> d = new System.Collections.Generic.Dictionary<int, T>();
    }
}
");
        }

        [Fact]
        public void RefEmit_ListOfValueTypes()
        {
            string source = @"
using System.Collections.Generic;

class A
{
    struct S { }

    List<S> f;
}";

            CompileAndVerify(source);
        }

        [Fact]
        public void RefEmit_SpecializedNestedSelfReference()
        {
            string source = @"
class A<T>
{
    class B {
    }

    A<int>.B x;
}";

            CompileAndVerify(source);
        }

        [Fact]
        public void RefEmit_SpecializedNestedGenericSelfReference()
        {
            string source = @"
class A<T>
{
    public class B<S> {
        public class C<U,V> {
        }
    }

    A<int>.B<double>.C<string, bool> x;
}";

            CompileAndVerify(source);
        }

        [Fact]
        public void RefEmit_Cycle()
        {
            string source = @"
public class B : I<C> { }
public class C : I<B> { }
public interface I<T> { }
";
            CompileAndVerify(source);
        }

        [Fact]
        public void RefEmit_SpecializedMemberReference()
        {
            string source = @"
class A<T>
{
    public A() 
    {
        A<int>.method();
        int a = A<string>.field;
        new A<double>();
    }

    public static void method() 
    {
    }

    public static int field;
}";

            CompileAndVerify(source);
        }

        [Fact]
        public void RefEmit_NestedGenericTypeReferences()
        {
            string source = @"
class A<T>
{
    public class H<S>
    {
        A<T>.H<S> x;      
    }
}";

            CompileAndVerify(source);
        }

        [Fact]
        public void RefEmit_Ordering2()
        {
            // order: 
            // E <(value type field) E.C.N2 <(value type field) N3
            string source = @"
public class E
{
   public class C {
     public struct N2
     { 
        public N3 n1;
     }
   }
   C.N2 n2; 
}
public struct N3
{ 
   E f;
   int g;
}";

            CompileAndVerify(source);
        }

        [Fact]
        public void RefEmit_Ordering3()
        {
            string source = @"
using System.Collections.Generic;

public class E
{
  public struct N2
  { 
    public List<N3> n1;        // E.N2 doesn't depend on E.N3 since List<> isn't a value type
  }
  public struct N3
  { 
  }
  N2 n2;                           
}";

            CompileAndVerify(source);
        }

        [Fact]
        public void RefEmit_IL1()
        {
            CompileAndVerify(@"
using System.Globalization;
class C 
{ 
    public static void Main() 
    { 
        int i = 0, j, k = 2147483647;
        long l = 0, m = 9200000000000000000L;
        int b = -10;
        byte c = 200;
        float f = 3.14159F;
        double d = 2.71828;
        string s = ""abcdef"";
        bool x = true;

        System.Console.WriteLine(i);
        System.Console.WriteLine(k);
        System.Console.WriteLine(b);
        System.Console.WriteLine(c);
        System.Console.WriteLine(f.ToString(CultureInfo.InvariantCulture));
        System.Console.WriteLine(d.ToString(CultureInfo.InvariantCulture));
        System.Console.WriteLine(s);
        System.Console.WriteLine(x);
    }
}
", expectedOutput: @"
0
2147483647
-10
200
3.14159
2.71828
abcdef
True
");
        }

        [WorkItem(540581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540581")]
        [Fact]
        public void RefEmit_DependencyGraphAndCachedTypeReferences()
        {
            var source = @"
using System;

interface I1<T>
{
    void Method(T x);
}

interface I2<U>
{
    void Method(U x);
}

interface I3<W> : I1<W>, I2<W>
{
    void Method(W x);
}

class Implicit2 : I3<string>                            // Implicit2 depends on I3<string> 
{
    public void Method(string x) {  }
}

class Test
{
    public static void Main()
    {
        I3<string> i = new Implicit2();                 
    }
}
";
            // If I3<string> in Main body is resolved first and stored in a cache,
            // the fact that Implicit2 depends on I3<string> isn't recorded if we pull 
            // I3<string> from cache at the beginning of ResolveType method.
            CompileAndVerify(source);
        }

        [Fact]
        public void CheckRef()
        {
            string source = @"
public abstract class C
{
    public abstract int M(int x, ref int y, out int z);
}
";

            CompileAndVerify(source, symbolValidator: module =>
            {
                var global = module.GlobalNamespace;

                var c = global.GetTypeMembers("C", 0).Single() as NamedTypeSymbol;
                var m = c.GetMembers("M").Single() as MethodSymbol;
                Assert.Equal(RefKind.None, m.Parameters[0].RefKind);
                Assert.Equal(RefKind.Ref, m.Parameters[1].RefKind);
                Assert.Equal(RefKind.Out, m.Parameters[2].RefKind);
            });
        }

        [Fact]
        public void OutArgument()
        {
            string source = @"
class C 
{
    static void Main() { double d; double.TryParse(null, out d); } 
}
";
            CompileAndVerify(source);
        }

        [Fact]
        public void CreateInstance()
        {
            string source = @"
class C 
{
    static void Main() { System.Activator.CreateInstance<int>(); } 
}
";
            CompileAndVerify(source);
        }

        [Fact]
        public void DelegateRoundTrip()
        {
            string source = @"delegate int MyDel(
                int x,
                // ref int y, // commented out until 4264 is fixed.
                // out int z, // commented out until 4264 is fixed.
                int w);";

            CompileAndVerify(source, symbolValidator: module =>
            {
                var global = module.GlobalNamespace;

                var myDel = global.GetTypeMembers("MyDel", 0).Single() as NamedTypeSymbol;

                var invoke = myDel.DelegateInvokeMethod;

                var beginInvoke = myDel.GetMembers("BeginInvoke").Single() as MethodSymbol;
                Assert.Equal(invoke.Parameters.Length + 2, beginInvoke.Parameters.Length);
                Assert.Equal(TypeKind.Interface, beginInvoke.ReturnType.TypeKind);
                Assert.Equal("System.IAsyncResult", beginInvoke.ReturnType.ToTestDisplayString());
                for (int i = 0; i < invoke.Parameters.Length; i++)
                {
                    Assert.Equal(invoke.Parameters[i].Type, beginInvoke.Parameters[i].Type);
                    Assert.Equal(invoke.Parameters[i].RefKind, beginInvoke.Parameters[i].RefKind);
                }
                Assert.Equal("System.AsyncCallback", beginInvoke.Parameters[invoke.Parameters.Length].Type.ToTestDisplayString());
                Assert.Equal("System.Object", beginInvoke.Parameters[invoke.Parameters.Length + 1].Type.ToTestDisplayString());

                var invokeReturn = invoke.ReturnType;
                var endInvoke = myDel.GetMembers("EndInvoke").Single() as MethodSymbol;
                var endInvokeReturn = endInvoke.ReturnType;
                Assert.Equal(invokeReturn, endInvokeReturn);
                int k = 0;
                for (int i = 0; i < invoke.Parameters.Length; i++)
                {
                    if (invoke.Parameters[i].RefKind != RefKind.None)
                    {
                        Assert.Equal(invoke.Parameters[i].TypeWithAnnotations, endInvoke.Parameters[k].TypeWithAnnotations);
                        Assert.Equal(invoke.Parameters[i].RefKind, endInvoke.Parameters[k++].RefKind);
                    }
                }
                Assert.Equal("System.IAsyncResult", endInvoke.Parameters[k++].Type.ToTestDisplayString());
                Assert.Equal(k, endInvoke.Parameters.Length);
            });
        }

        [Fact]
        public void StaticClassRoundTrip()
        {
            string source = @"
public static class C
{
    private static string msg = ""Hello"";

    private static void Goo()
    {
        System.Console.WriteLine(msg);
    }

    public static void Main()
    {
        Goo();
    }
}
";

            CompileAndVerify(source,
                symbolValidator: module =>
            {
                var global = module.GlobalNamespace;
                var classC = global.GetMember<NamedTypeSymbol>("C");
                Assert.True(classC.IsStatic, "Expected C to be static");
                Assert.False(classC.IsAbstract, "Expected C to be non-abstract"); //even though it is abstract in metadata
                Assert.False(classC.IsSealed, "Expected C to be non-sealed"); //even though it is sealed in metadata
                Assert.Equal(0, classC.GetMembers(WellKnownMemberNames.InstanceConstructorName).Length); //since C is static
                Assert.Equal(0, classC.GetMembers(WellKnownMemberNames.StaticConstructorName).Length); //since we don't import private members
            });
        }

        [Fact]
        public void DoNotImportInternalMembers()
        {
            string sources =
@"public class Fields
{
    public int Public;
    internal int Internal;
}
public class Methods
{
    public void Public() {}
    internal void Internal() {}
}";

            Func<bool, Action<ModuleSymbol>> validator = isFromSource => (ModuleSymbol m) =>
            {
                CheckInternalMembers(m.GlobalNamespace.GetTypeMembers("Fields").Single(), isFromSource);
                CheckInternalMembers(m.GlobalNamespace.GetTypeMembers("Methods").Single(), isFromSource);
            };

            CompileAndVerify(sources, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
        }

        [Fact]
        public void Issue4695()
        {
            string source = @"
using System;

class Program
{
    sealed class Cache
    {
        abstract class BucketwiseBase<TArg> where TArg : class
        {
            internal abstract void Default(TArg arg);
        }

        class BucketwiseBase<TAccumulator, TArg> : BucketwiseBase<TArg> where TArg : class
        {
            internal override void Default(TArg arg = null) { }
        }

        public string GetAll()
        {
            new BucketwiseBase<object, object>().Default(); // Bad image format thrown here on legacy compiler 
            return ""OK"";
        }
    }

    static void Main(string[] args)
    {
        Console.WriteLine(new Cache().GetAll());
    }
}
";
            CompileAndVerify(source, expectedOutput: "OK");
        }

        private void CheckInternalMembers(NamedTypeSymbol type, bool isFromSource)
        {
            Assert.NotNull(type.GetMembers("Public").SingleOrDefault());
            var member = type.GetMembers("Internal").SingleOrDefault();
            if (isFromSource)
                Assert.NotNull(member);
            else
                Assert.Null(member);
        }

        [WorkItem(90, "https://github.com/dotnet/roslyn/issues/90")]
        [Fact]
        public void EmitWithNoResourcesAllPlatforms()
        {
            var comp = CreateCompilation("class Test { static void Main() { } }");

            VerifyEmitWithNoResources(comp, Platform.AnyCpu);
            VerifyEmitWithNoResources(comp, Platform.AnyCpu32BitPreferred);
            VerifyEmitWithNoResources(comp, Platform.Arm);     // broken before fix
            VerifyEmitWithNoResources(comp, Platform.Itanium); // broken before fix
            VerifyEmitWithNoResources(comp, Platform.X64);     // broken before fix
            VerifyEmitWithNoResources(comp, Platform.X86);
        }

        private void VerifyEmitWithNoResources(CSharpCompilation comp, Platform platform)
        {
            var options = TestOptions.ReleaseExe.WithPlatform(platform);
            CompileAndVerify(comp.WithAssemblyName("EmitWithNoResourcesAllPlatforms_" + platform.ToString()).WithOptions(options));
        }

        [Fact]
        public unsafe void PEHeaders1()
        {
            var options = EmitOptions.Default.WithFileAlignment(0x2000);
            var syntax = SyntaxFactory.ParseSyntaxTree(@"class C {}", TestOptions.Regular.WithNoRefSafetyRulesAttribute());

            var peStream = CreateCompilationWithMscorlib40(
                syntax,
                options: TestOptions.ReleaseDll.WithDeterministic(true),
                assemblyName: "46B9C2B2-B7A0-45C5-9EF9-28DDF739FD9E").EmitToStream(options);

            peStream.Position = 0;
            var peReader = new PEReader(peStream);

            var peHeaders = peReader.PEHeaders;
            var peHeader = peHeaders.PEHeader;
            var coffHeader = peHeaders.CoffHeader;
            var corHeader = peHeaders.CorHeader;

            Assert.Equal(PEMagic.PE32, peHeader.Magic);
            Assert.Equal(0x0000237E, peHeader.AddressOfEntryPoint);
            Assert.Equal(0x00002000, peHeader.BaseOfCode);
            Assert.Equal(0x00004000, peHeader.BaseOfData);
            Assert.Equal(0x00002000, peHeader.SizeOfHeaders);
            Assert.Equal(0x00002000, peHeader.SizeOfCode);
            Assert.Equal(0x00001000u, peHeader.SizeOfHeapCommit);
            Assert.Equal(0x00100000u, peHeader.SizeOfHeapReserve);
            Assert.Equal(0x00006000, peHeader.SizeOfImage);
            Assert.Equal(0x00002000, peHeader.SizeOfInitializedData);
            Assert.Equal(0x00001000u, peHeader.SizeOfStackCommit);
            Assert.Equal(0x00100000u, peHeader.SizeOfStackReserve);
            Assert.Equal(0, peHeader.SizeOfUninitializedData);
            Assert.Equal(Subsystem.WindowsCui, peHeader.Subsystem);
            Assert.Equal(DllCharacteristics.DynamicBase | DllCharacteristics.NxCompatible | DllCharacteristics.NoSeh | DllCharacteristics.TerminalServerAware, peHeader.DllCharacteristics);
            Assert.Equal(0u, peHeader.CheckSum);
            Assert.Equal(0x2000, peHeader.FileAlignment);
            Assert.Equal(0x10000000u, peHeader.ImageBase);
            Assert.Equal(0x2000, peHeader.SectionAlignment);
            Assert.Equal(0, peHeader.MajorImageVersion);
            Assert.Equal(0, peHeader.MinorImageVersion);
            Assert.Equal(0x30, peHeader.MajorLinkerVersion);
            Assert.Equal(0, peHeader.MinorLinkerVersion);
            Assert.Equal(4, peHeader.MajorOperatingSystemVersion);
            Assert.Equal(0, peHeader.MinorOperatingSystemVersion);
            Assert.Equal(4, peHeader.MajorSubsystemVersion);
            Assert.Equal(0, peHeader.MinorSubsystemVersion);
            Assert.Equal(16, peHeader.NumberOfRvaAndSizes);
            Assert.Equal(0x2000, peHeader.SizeOfHeaders);

            Assert.Equal(0x4000, peHeader.BaseRelocationTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0xc, peHeader.BaseRelocationTableDirectory.Size);
            Assert.Equal(0, peHeader.BoundImportTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.BoundImportTableDirectory.Size);
            Assert.Equal(0, peHeader.CertificateTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.CertificateTableDirectory.Size);
            Assert.Equal(0, peHeader.CopyrightTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.CopyrightTableDirectory.Size);
            Assert.Equal(0x2008, peHeader.CorHeaderTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0x48, peHeader.CorHeaderTableDirectory.Size);
            Assert.Equal(0x2310, peHeader.DebugTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0x1C, peHeader.DebugTableDirectory.Size);
            Assert.Equal(0, peHeader.ExceptionTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.ExceptionTableDirectory.Size);
            Assert.Equal(0, peHeader.ExportTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.ExportTableDirectory.Size);
            Assert.Equal(0x2000, peHeader.ImportAddressTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0x8, peHeader.ImportAddressTableDirectory.Size);
            Assert.Equal(0x232C, peHeader.ImportTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0x4f, peHeader.ImportTableDirectory.Size);
            Assert.Equal(0, peHeader.LoadConfigTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.LoadConfigTableDirectory.Size);
            Assert.Equal(0, peHeader.ResourceTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.ResourceTableDirectory.Size);
            Assert.Equal(0, peHeader.ThreadLocalStorageTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.ThreadLocalStorageTableDirectory.Size);

            int importAddressTableDirectoryOffset;
            Assert.True(peHeaders.TryGetDirectoryOffset(peHeader.ImportAddressTableDirectory, out importAddressTableDirectoryOffset));
            Assert.Equal(0x2000, importAddressTableDirectoryOffset);

            var importAddressTableDirectoryBytes = new byte[peHeader.ImportAddressTableDirectory.Size];
            peStream.Position = importAddressTableDirectoryOffset;
            peStream.Read(importAddressTableDirectoryBytes, 0, importAddressTableDirectoryBytes.Length);
            AssertEx.Equal(new byte[]
            {
                0x60, 0x23, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            }, importAddressTableDirectoryBytes);

            int importTableDirectoryOffset;
            Assert.True(peHeaders.TryGetDirectoryOffset(peHeader.ImportTableDirectory, out importTableDirectoryOffset));
            Assert.Equal(0x232C, importTableDirectoryOffset);

            var importTableDirectoryBytes = new byte[peHeader.ImportTableDirectory.Size];
            peStream.Position = importTableDirectoryOffset;
            peStream.Read(importTableDirectoryBytes, 0, importTableDirectoryBytes.Length);
            AssertEx.Equal(new byte[]
            {
                0x54, 0x23, 0x00, 0x00, // RVA
                0x00, 0x00, 0x00, 0x00, // 0
                0x00, 0x00, 0x00, 0x00, // 0
                0x6E, 0x23, 0x00, 0x00, // name RVA
                0x00, 0x20, 0x00, 0x00, // ImportAddressTableDirectory RVA
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x60, 0x23, 0x00, 0x00, // hint RVA
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00,             // hint
                (byte)'_', (byte)'C', (byte)'o', (byte)'r', (byte)'D', (byte)'l', (byte)'l', (byte)'M', (byte)'a', (byte)'i', (byte)'n', 0x00,
                (byte)'m', (byte)'s', (byte)'c', (byte)'o', (byte)'r', (byte)'e', (byte)'e', (byte)'.', (byte)'d', (byte)'l', (byte)'l', 0x00,
                0x00
            }, importTableDirectoryBytes);

            var entryPointSectionIndex = peHeaders.GetContainingSectionIndex(peHeader.AddressOfEntryPoint);
            Assert.Equal(0, entryPointSectionIndex);

            peStream.Position = peHeaders.SectionHeaders[0].PointerToRawData + peHeader.AddressOfEntryPoint - peHeaders.SectionHeaders[0].VirtualAddress;
            byte[] startupStub = new byte[8];
            peStream.Read(startupStub, 0, startupStub.Length);
            AssertEx.Equal(new byte[] { 0xFF, 0x25, 0x00, 0x20, 0x00, 0x10, 0x00, 0x00 }, startupStub);

            Assert.Equal(Characteristics.Dll | Characteristics.LargeAddressAware | Characteristics.ExecutableImage, coffHeader.Characteristics);
            Assert.Equal(Machine.I386, coffHeader.Machine);
            Assert.Equal(2, coffHeader.NumberOfSections);
            Assert.Equal(0, coffHeader.NumberOfSymbols);
            Assert.Equal(0, coffHeader.PointerToSymbolTable);
            Assert.Equal(0xe0, coffHeader.SizeOfOptionalHeader);
            Assert.Equal(-609278808, coffHeader.TimeDateStamp);

            Assert.Equal(0, corHeader.EntryPointTokenOrRelativeVirtualAddress);
            Assert.Equal(CorFlags.ILOnly, corHeader.Flags);
            Assert.Equal(2, corHeader.MajorRuntimeVersion);
            Assert.Equal(5, corHeader.MinorRuntimeVersion);

            Assert.Equal(0, corHeader.CodeManagerTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, corHeader.CodeManagerTableDirectory.Size);
            Assert.Equal(0, corHeader.ExportAddressTableJumpsDirectory.RelativeVirtualAddress);
            Assert.Equal(0, corHeader.ExportAddressTableJumpsDirectory.Size);
            Assert.Equal(0, corHeader.ManagedNativeHeaderDirectory.RelativeVirtualAddress);
            Assert.Equal(0, corHeader.ManagedNativeHeaderDirectory.Size);
            Assert.Equal(0x2058, corHeader.MetadataDirectory.RelativeVirtualAddress);
            Assert.Equal(0x02b8, corHeader.MetadataDirectory.Size);
            Assert.Equal(0, corHeader.ResourcesDirectory.RelativeVirtualAddress);
            Assert.Equal(0, corHeader.ResourcesDirectory.Size);
            Assert.Equal(0, corHeader.StrongNameSignatureDirectory.RelativeVirtualAddress);
            Assert.Equal(0, corHeader.StrongNameSignatureDirectory.Size);
            Assert.Equal(0, corHeader.VtableFixupsDirectory.RelativeVirtualAddress);
            Assert.Equal(0, corHeader.VtableFixupsDirectory.Size);

            var sections = peHeaders.SectionHeaders;
            Assert.Equal(2, sections.Length);

            Assert.Equal(".text", sections[0].Name);
            Assert.Equal(0, sections[0].NumberOfLineNumbers);
            Assert.Equal(0, sections[0].NumberOfRelocations);
            Assert.Equal(0, sections[0].PointerToLineNumbers);
            Assert.Equal(0x2000, sections[0].PointerToRawData);
            Assert.Equal(0, sections[0].PointerToRelocations);
            Assert.Equal(SectionCharacteristics.ContainsCode | SectionCharacteristics.MemExecute | SectionCharacteristics.MemRead, sections[0].SectionCharacteristics);
            Assert.Equal(0x2000, sections[0].SizeOfRawData);
            Assert.Equal(0x2000, sections[0].VirtualAddress);
            Assert.Equal(900, sections[0].VirtualSize);

            Assert.Equal(".reloc", sections[1].Name);
            Assert.Equal(0, sections[1].NumberOfLineNumbers);
            Assert.Equal(0, sections[1].NumberOfRelocations);
            Assert.Equal(0, sections[1].PointerToLineNumbers);
            Assert.Equal(0x4000, sections[1].PointerToRawData);
            Assert.Equal(0, sections[1].PointerToRelocations);
            Assert.Equal(SectionCharacteristics.ContainsInitializedData | SectionCharacteristics.MemDiscardable | SectionCharacteristics.MemRead, sections[1].SectionCharacteristics);
            Assert.Equal(0x2000, sections[1].SizeOfRawData);
            Assert.Equal(0x4000, sections[1].VirtualAddress);
            Assert.Equal(12, sections[1].VirtualSize);

            var relocBlock = peReader.GetSectionData(sections[1].VirtualAddress);
            var relocBytes = new byte[sections[1].VirtualSize];
            Marshal.Copy((IntPtr)relocBlock.Pointer, relocBytes, 0, relocBytes.Length);
            AssertEx.Equal(new byte[] { 0, 0x20, 0, 0, 0x0c, 0, 0, 0, 0x80, 0x33, 0, 0 }, relocBytes);
        }

        [Fact]
        public void PEHeaders2()
        {
            var options = EmitOptions.Default.
                WithFileAlignment(512).
                WithBaseAddress(0x123456789ABCDEF).
                WithHighEntropyVirtualAddressSpace(true).
                WithSubsystemVersion(SubsystemVersion.WindowsXP);

            var syntax = SyntaxFactory.ParseSyntaxTree(@"class C { static void Main() { } }", TestOptions.Regular.WithNoRefSafetyRulesAttribute());

            var peStream = CreateCompilationWithMscorlib40(
                syntax,
                options: TestOptions.DebugExe.WithPlatform(Platform.X64).WithDeterministic(true),
                assemblyName: "B37A4FCD-ED76-4924-A2AD-298836056E00").EmitToStream(options);

            peStream.Position = 0;
            var peHeaders = new PEHeaders(peStream);

            var peHeader = peHeaders.PEHeader;
            var coffHeader = peHeaders.CoffHeader;
            var corHeader = peHeaders.CorHeader;

            Assert.Equal(PEMagic.PE32Plus, peHeader.Magic);
            Assert.Equal(0x00000000, peHeader.AddressOfEntryPoint);
            Assert.Equal(0x00002000, peHeader.BaseOfCode);
            Assert.Equal(0x00000000, peHeader.BaseOfData);
            Assert.Equal(0x00000200, peHeader.SizeOfHeaders);
            Assert.Equal(0x00000400, peHeader.SizeOfCode);
            Assert.Equal(0x00002000u, peHeader.SizeOfHeapCommit);
            Assert.Equal(0x00100000u, peHeader.SizeOfHeapReserve);
            Assert.Equal(0x00004000, peHeader.SizeOfImage);
            Assert.Equal(0x00000000, peHeader.SizeOfInitializedData);
            Assert.Equal(0x00004000u, peHeader.SizeOfStackCommit);
            Assert.Equal(0x0400000u, peHeader.SizeOfStackReserve);
            Assert.Equal(0, peHeader.SizeOfUninitializedData);
            Assert.Equal(Subsystem.WindowsCui, peHeader.Subsystem);
            Assert.Equal(0u, peHeader.CheckSum);
            Assert.Equal(0x200, peHeader.FileAlignment);
            Assert.Equal(0x0123456789ac0000u, peHeader.ImageBase);
            Assert.Equal(0x2000, peHeader.SectionAlignment);
            Assert.Equal(0, peHeader.MajorImageVersion);
            Assert.Equal(0, peHeader.MinorImageVersion);
            Assert.Equal(0x30, peHeader.MajorLinkerVersion);
            Assert.Equal(0, peHeader.MinorLinkerVersion);
            Assert.Equal(4, peHeader.MajorOperatingSystemVersion);
            Assert.Equal(0, peHeader.MinorOperatingSystemVersion);
            Assert.Equal(5, peHeader.MajorSubsystemVersion);
            Assert.Equal(1, peHeader.MinorSubsystemVersion);
            Assert.Equal(16, peHeader.NumberOfRvaAndSizes);
            Assert.Equal(0x200, peHeader.SizeOfHeaders);

            Assert.Equal(0, peHeader.BaseRelocationTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.BaseRelocationTableDirectory.Size);
            Assert.Equal(0, peHeader.BoundImportTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.BoundImportTableDirectory.Size);
            Assert.Equal(0, peHeader.CertificateTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.CertificateTableDirectory.Size);
            Assert.Equal(0, peHeader.CopyrightTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.CopyrightTableDirectory.Size);
            Assert.Equal(0x2000, peHeader.CorHeaderTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0x48, peHeader.CorHeaderTableDirectory.Size);
            Assert.Equal(0x2324, peHeader.DebugTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0x1C, peHeader.DebugTableDirectory.Size);
            Assert.Equal(0, peHeader.ExceptionTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.ExceptionTableDirectory.Size);
            Assert.Equal(0, peHeader.ExportTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.ExportTableDirectory.Size);
            Assert.Equal(0, peHeader.ImportAddressTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.ImportAddressTableDirectory.Size);
            Assert.Equal(0, peHeader.ImportTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.ImportTableDirectory.Size);
            Assert.Equal(0, peHeader.LoadConfigTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.LoadConfigTableDirectory.Size);
            Assert.Equal(0, peHeader.ResourceTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.ResourceTableDirectory.Size);
            Assert.Equal(0, peHeader.ThreadLocalStorageTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeader.ThreadLocalStorageTableDirectory.Size);

            Assert.Equal(Characteristics.LargeAddressAware | Characteristics.ExecutableImage, coffHeader.Characteristics);
            Assert.Equal(Machine.Amd64, coffHeader.Machine);
            Assert.Equal(1, coffHeader.NumberOfSections);
            Assert.Equal(0, coffHeader.NumberOfSymbols);
            Assert.Equal(0, coffHeader.PointerToSymbolTable);
            Assert.Equal(240, coffHeader.SizeOfOptionalHeader);
            Assert.Equal(-1823671907, coffHeader.TimeDateStamp);

            Assert.Equal(0x06000001, corHeader.EntryPointTokenOrRelativeVirtualAddress);
            Assert.Equal(CorFlags.ILOnly, corHeader.Flags);
            Assert.Equal(2, corHeader.MajorRuntimeVersion);
            Assert.Equal(5, corHeader.MinorRuntimeVersion);

            Assert.Equal(0, corHeader.CodeManagerTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, corHeader.CodeManagerTableDirectory.Size);
            Assert.Equal(0, corHeader.ExportAddressTableJumpsDirectory.RelativeVirtualAddress);
            Assert.Equal(0, corHeader.ExportAddressTableJumpsDirectory.Size);
            Assert.Equal(0, corHeader.ManagedNativeHeaderDirectory.RelativeVirtualAddress);
            Assert.Equal(0, corHeader.ManagedNativeHeaderDirectory.Size);
            Assert.Equal(0x2054, corHeader.MetadataDirectory.RelativeVirtualAddress);
            Assert.Equal(0x02d0, corHeader.MetadataDirectory.Size);
            Assert.Equal(0, corHeader.ResourcesDirectory.RelativeVirtualAddress);
            Assert.Equal(0, corHeader.ResourcesDirectory.Size);
            Assert.Equal(0, corHeader.StrongNameSignatureDirectory.RelativeVirtualAddress);
            Assert.Equal(0, corHeader.StrongNameSignatureDirectory.Size);
            Assert.Equal(0, corHeader.VtableFixupsDirectory.RelativeVirtualAddress);
            Assert.Equal(0, corHeader.VtableFixupsDirectory.Size);

            var sections = peHeaders.SectionHeaders;
            Assert.Equal(1, sections.Length);

            Assert.Equal(".text", sections[0].Name);
            Assert.Equal(0, sections[0].NumberOfLineNumbers);
            Assert.Equal(0, sections[0].NumberOfRelocations);
            Assert.Equal(0, sections[0].PointerToLineNumbers);
            Assert.Equal(0x200, sections[0].PointerToRawData);
            Assert.Equal(0, sections[0].PointerToRelocations);
            Assert.Equal(SectionCharacteristics.ContainsCode | SectionCharacteristics.MemExecute | SectionCharacteristics.MemRead, sections[0].SectionCharacteristics);
            Assert.Equal(0x400, sections[0].SizeOfRawData);
            Assert.Equal(0x2000, sections[0].VirtualAddress);
            Assert.Equal(832, sections[0].VirtualSize);
        }

        [Fact]
        public void InParametersShouldHaveMetadataIn_TypeMethods()
        {
            var text = @"
using System.Runtime.InteropServices;
class T
{
    public void M(in int a, [In]in int b, [In]int c, int d) {}
}";

            Action<ModuleSymbol> verifier = module =>
            {
                var parameters = module.GlobalNamespace.GetTypeMember("T").GetMethod("M").GetParameters();
                Assert.Equal(4, parameters.Length);

                Assert.True(parameters[0].IsMetadataIn);
                Assert.True(parameters[1].IsMetadataIn);
                Assert.True(parameters[2].IsMetadataIn);
                Assert.False(parameters[3].IsMetadataIn);
            };

            CompileAndVerify(text, sourceSymbolValidator: verifier, symbolValidator: verifier);
        }

        [Fact]
        public void InParametersShouldHaveMetadataIn_IndexerMethods()
        {
            var text = @"
using System.Runtime.InteropServices;
class T
{
    public int this[in int a, [In]in int b, [In]int c, int d] => 0;
}";

            Action<ModuleSymbol> verifier = module =>
            {
                var parameters = module.GlobalNamespace.GetTypeMember("T").GetMethod("get_Item").GetParameters();
                Assert.Equal(4, parameters.Length);

                Assert.True(parameters[0].IsMetadataIn);
                Assert.True(parameters[1].IsMetadataIn);
                Assert.True(parameters[2].IsMetadataIn);
                Assert.False(parameters[3].IsMetadataIn);
            };

            CompileAndVerify(text, sourceSymbolValidator: verifier, symbolValidator: verifier);
        }

        [Fact]
        public void InParametersShouldHaveMetadataIn_Delegates()
        {
            var text = @"
using System.Runtime.InteropServices;
public delegate void D(in int a, [In]in int b, [In]int c, int d);
public class C
{
    public void M()
    {
        N((in int a, in int b, int c, int d) => {});
    }
    public void N(D lambda) { }
}
";

            CompileAndVerify(text,
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
                sourceSymbolValidator: module =>
                {
                    var parameters = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod.Parameters;
                    Assert.Equal(4, parameters.Length);

                    Assert.True(parameters[0].IsMetadataIn);
                    Assert.True(parameters[1].IsMetadataIn);
                    Assert.True(parameters[2].IsMetadataIn);
                    Assert.False(parameters[3].IsMetadataIn);
                },
                symbolValidator: module =>
                {
                    var delegateParameters = module.ContainingAssembly.GetTypeByMetadataName("D").DelegateInvokeMethod.Parameters;
                    Assert.Equal(4, delegateParameters.Length);

                    Assert.True(delegateParameters[0].IsMetadataIn);
                    Assert.True(delegateParameters[1].IsMetadataIn);
                    Assert.True(delegateParameters[2].IsMetadataIn);
                    Assert.False(delegateParameters[3].IsMetadataIn);

                    var lambdaParameters = module.GlobalNamespace.GetTypeMember("C").GetTypeMember("<>c").GetMethod("<M>b__0_0").Parameters;
                    Assert.Equal(4, lambdaParameters.Length);

                    Assert.True(lambdaParameters[0].IsMetadataIn);
                    Assert.True(lambdaParameters[1].IsMetadataIn);
                    Assert.False(lambdaParameters[2].IsMetadataIn);
                    Assert.False(lambdaParameters[3].IsMetadataIn);
                });
        }

        [Fact]
        public void InParametersShouldHaveMetadataIn_LocalFunctions()
        {
            var text = @"
using System.Runtime.InteropServices;
public class C
{
    public void M()
    {
        void local(in int a, int c) { }
    }
}
";

            CompileAndVerify(text, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                var parameters = module.GlobalNamespace.GetTypeMember("C").GetMember("<M>g__local|0_0").GetParameters();
                Assert.Equal(2, parameters.Length);

                Assert.True(parameters[0].IsMetadataIn);
                Assert.False(parameters[1].IsMetadataIn);
            });
        }

        [Fact]
        public void InParametersShouldHaveMetadataIn_ExternMethods()
        {
            var text = @"
using System.Runtime.InteropServices;
class T
{
    [DllImport(""Other.dll"")]
    public static extern void M(in int a, [In]in int b, [In]int c, int d);
}";

            Action<ModuleSymbol> verifier = module =>
            {
                var parameters = module.GlobalNamespace.GetTypeMember("T").GetMethod("M").GetParameters();
                Assert.Equal(4, parameters.Length);

                Assert.True(parameters[0].IsMetadataIn);
                Assert.True(parameters[1].IsMetadataIn);
                Assert.True(parameters[2].IsMetadataIn);
                Assert.False(parameters[3].IsMetadataIn);
            };

            CompileAndVerify(text, sourceSymbolValidator: verifier, symbolValidator: verifier);
        }

        [Fact]
        public void InParametersShouldHaveMetadataIn_NoPIA()
        {
            var comAssembly = CreateCompilationWithMscorlib40(@"
using System;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""test.dll"")]
[assembly: Guid(""6681dcd6-9c3e-4c3a-b04a-aef3ee85c2cf"")]
[ComImport()]
[Guid(""6681dcd6-9c3e-4c3a-b04a-aef3ee85c2cf"")]
public interface T
{
    void M(in int a, [In]in int b, [In]int c, int d);
}");

            CompileAndVerify(comAssembly, symbolValidator: module =>
            {
                var parameters = module.GlobalNamespace.GetTypeMember("T").GetMethod("M").GetParameters();
                Assert.Equal(4, parameters.Length);

                Assert.True(parameters[0].IsMetadataIn);
                Assert.True(parameters[1].IsMetadataIn);
                Assert.True(parameters[2].IsMetadataIn);
                Assert.False(parameters[3].IsMetadataIn);
            });

            var code = @"
class User
{
    public void M(T obj)
    {
        obj.M(1, 2, 3, 4);
    }
}";

            CompileAndVerify(
                source: code,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                references: new[] { comAssembly.EmitToImageReference(embedInteropTypes: true) },
                symbolValidator: module =>
                {
                    var parameters = module.GlobalNamespace.GetTypeMember("T").GetMethod("M").GetParameters();
                    Assert.Equal(4, parameters.Length);

                    Assert.True(parameters[0].IsMetadataIn);
                    Assert.True(parameters[1].IsMetadataIn);
                    Assert.True(parameters[2].IsMetadataIn);
                    Assert.False(parameters[3].IsMetadataIn);
                });
        }

        [Fact]
        public void ExtendingInParametersFromParentWithoutInAttributeWorksWithoutErrors()
        {
            var reference = CompileIL(@"
.class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (01 00 00 00)
    .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (01 00 00 00)

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsReadOnlyAttribute extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (01 00 00 00)
    .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (01 00 00 00)

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}

.class public auto ansi beforefieldinit Parent extends [mscorlib]System.Object
{
    .method public hidebysig newslot virtual instance void M (
            int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute)  a,
            int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute)  b,
            int32 c,
            int32 d) cil managed 
    {
        .param [1] .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .param [2] .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
        .maxstack 8
        IL_0000: nop
        IL_0001: ldstr ""Parent called""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void[mscorlib] System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}");

            var comp = CreateCompilation(@"
using System;
using System.Runtime.InteropServices;

public class Child : Parent
{
    public override void M(in int a, [In]in int b, [In]int c, int d)
    {
        base.M(a, b, c, d);

        Console.WriteLine(""Child called"");
    }
}
public static class Program
{
    public static void Main()
    {
        var obj = new Child();
        obj.M(1, 2, 3, 4);
    }
}", new[] { reference }, TestOptions.ReleaseExe);

            var parentParameters = comp.GetTypeByMetadataName("Parent").GetMethod("M").GetParameters();
            Assert.Equal(4, parentParameters.Length);

            Assert.False(parentParameters[0].IsMetadataIn);
            Assert.False(parentParameters[1].IsMetadataIn);
            Assert.False(parentParameters[2].IsMetadataIn);
            Assert.False(parentParameters[3].IsMetadataIn);

            var expectedOutput =
@"Parent called
Child called";

            CompileAndVerify(comp, expectedOutput: expectedOutput, symbolValidator: module =>
            {
                var childParameters = module.ContainingAssembly.GetTypeByMetadataName("Child").GetMethod("M").GetParameters();
                Assert.Equal(4, childParameters.Length);

                Assert.True(childParameters[0].IsMetadataIn);
                Assert.True(childParameters[1].IsMetadataIn);
                Assert.True(childParameters[2].IsMetadataIn);
                Assert.False(childParameters[3].IsMetadataIn);
            });
        }

        [Fact]
        public void GeneratingProxyForVirtualMethodInParentCopiesMetadataBitsCorrectly_OutAttribute()
        {
            var reference = CreateCompilation(@"
using System.Runtime.InteropServices;

public class Parent
{
    public void M(out int a, [Out] int b) => throw null;
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var sourceParentParameters = module.GlobalNamespace.GetTypeMember("Parent").GetMethod("M").GetParameters();
                Assert.Equal(2, sourceParentParameters.Length);

                Assert.True(sourceParentParameters[0].IsMetadataOut);
                Assert.True(sourceParentParameters[1].IsMetadataOut);
            });

            var source = @"
using System.Runtime.InteropServices;

public interface IParent
{
    void M(out int a, [Out] int b);
}

public class Child : Parent, IParent
{
}";

            CompileAndVerify(
                source: source,
                references: new[] { reference.EmitToImageReference() },
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var interfaceParameters = module.GlobalNamespace.GetTypeMember("IParent").GetMethod("M").GetParameters();
                    Assert.Equal(2, interfaceParameters.Length);

                    Assert.True(interfaceParameters[0].IsMetadataOut);
                    Assert.True(interfaceParameters[1].IsMetadataOut);

                    var proxyChildParameters = module.GlobalNamespace.GetTypeMember("Child").GetMethod("IParent.M").GetParameters();
                    Assert.Equal(2, proxyChildParameters.Length);

                    Assert.True(proxyChildParameters[0].IsMetadataOut);
                    Assert.False(proxyChildParameters[1].IsMetadataOut); // User placed attributes are not copied.
                });
        }

        [Fact]
        public void GeneratingProxyForVirtualMethodInParentCopiesMetadataBitsCorrectly_InAttribute()
        {
            var reference = CreateCompilation(@"
using System.Runtime.InteropServices;

public class Parent
{
    public void M(in int a, [In] int b) => throw null;
}");

            CompileAndVerify(reference, symbolValidator: module =>
            {
                var sourceParentParameters = module.GlobalNamespace.GetTypeMember("Parent").GetMethod("M").GetParameters();
                Assert.Equal(2, sourceParentParameters.Length);

                Assert.True(sourceParentParameters[0].IsMetadataIn);
                Assert.True(sourceParentParameters[1].IsMetadataIn);
            });

            var source = @"
using System.Runtime.InteropServices;

public interface IParent
{
    void M(in int a, [In] int b);
}

public class Child : Parent, IParent
{
}";

            CompileAndVerify(
                source: source,
                references: new[] { reference.EmitToImageReference() },
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var interfaceParameters = module.GlobalNamespace.GetTypeMember("IParent").GetMethod("M").GetParameters();
                    Assert.Equal(2, interfaceParameters.Length);

                    Assert.True(interfaceParameters[0].IsMetadataIn);
                    Assert.True(interfaceParameters[1].IsMetadataIn);

                    var proxyChildParameters = module.GlobalNamespace.GetTypeMember("Child").GetMethod("IParent.M").GetParameters();
                    Assert.Equal(2, proxyChildParameters.Length);

                    Assert.True(proxyChildParameters[0].IsMetadataIn);
                    Assert.False(proxyChildParameters[1].IsMetadataIn); // User placed attributes are not copied.
                });
        }

        [Fact]
        public void DataSectionStringLiterals_MissingMembers()
        {
            var source = """
                System.Console.Write("a");
                System.Console.Write("bb");
                System.Console.Write("ccc");
                """;

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"));
            comp.MakeMemberMissing(WellKnownMember.System_Text_Encoding__get_UTF8);
            comp.VerifyDiagnostics(
                // error CS0656: Missing compiler required member 'System.Text.Encoding.get_UTF8'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Text.Encoding", "get_UTF8").WithLocation(1, 1));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"));
            comp.MakeMemberMissing(WellKnownMember.System_Text_Encoding__GetString);
            comp.VerifyDiagnostics(
                // error CS0656: Missing compiler required member 'System.Text.Encoding.GetString'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Text.Encoding", "GetString").WithLocation(1, 1));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"));
            comp.MakeMemberMissing(WellKnownMember.System_Text_Encoding__get_UTF8);
            comp.MakeMemberMissing(WellKnownMember.System_Text_Encoding__GetString);
            comp.VerifyDiagnostics(
                // error CS0656: Missing compiler required member 'System.Text.Encoding.get_UTF8'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Text.Encoding", "get_UTF8").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.Text.Encoding.GetString'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Text.Encoding", "GetString").WithLocation(1, 1));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "1"));
            comp.MakeMemberMissing(WellKnownMember.System_Text_Encoding__get_UTF8);
            comp.MakeMemberMissing(WellKnownMember.System_Text_Encoding__GetString);
            comp.VerifyDiagnostics(
                // error CS0656: Missing compiler required member 'System.Text.Encoding.get_UTF8'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Text.Encoding", "get_UTF8").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.Text.Encoding.GetString'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Text.Encoding", "GetString").WithLocation(1, 1));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "3"));
            comp.MakeMemberMissing(WellKnownMember.System_Text_Encoding__get_UTF8);
            comp.MakeMemberMissing(WellKnownMember.System_Text_Encoding__GetString);
            comp.VerifyDiagnostics(
                // error CS0656: Missing compiler required member 'System.Text.Encoding.get_UTF8'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Text.Encoding", "get_UTF8").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.Text.Encoding.GetString'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Text.Encoding", "GetString").WithLocation(1, 1));
        }

        [Fact]
        public void DataSectionStringLiterals_Threshold()
        {
            var source = """
                System.Console.Write("a");
                System.Console.Write("bb");
                System.Console.Write("ccc");
                """;

            var expectedOutput = "abbccc";

            var expectedIl = """
                {
                  // Code size       31 (0x1f)
                  .maxstack  1
                  IL_0000:  ldstr      "a"
                  IL_0005:  call       "void System.Console.Write(string)"
                  IL_000a:  ldstr      "bb"
                  IL_000f:  call       "void System.Console.Write(string)"
                  IL_0014:  ldstr      "ccc"
                  IL_0019:  call       "void System.Console.Write(string)"
                  IL_001e:  ret
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: expectedOutput)
                .VerifyDiagnostics()
                .VerifyIL("<top-level-statements-entry-point>", expectedIl);
            Assert.Null(verifier.Compilation.DataSectionStringLiteralThreshold);

            foreach (var feature in new[] { "off", null })
            {
                verifier = CompileAndVerify(source,
                    parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, null),
                    expectedOutput: expectedOutput)
                    .VerifyDiagnostics()
                    .VerifyIL("<top-level-statements-entry-point>", expectedIl);
                Assert.Null(verifier.Compilation.DataSectionStringLiteralThreshold);
            }

            // unrecognized input => default value 100
            foreach (var feature in new[] { "true", "false", "", "-1", long.MaxValue.ToString() })
            {
                verifier = CompileAndVerify(source,
                    parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, feature),
                    expectedOutput: expectedOutput)
                    .VerifyDiagnostics()
                    .VerifyIL("<top-level-statements-entry-point>", expectedIl);
                Assert.Equal(100, verifier.Compilation.DataSectionStringLiteralThreshold);
            }

            verifier = CompileAndVerify(source,
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "1000"),
                expectedOutput: expectedOutput)
                .VerifyDiagnostics()
                .VerifyIL("<top-level-statements-entry-point>", expectedIl);
            Assert.Equal(1000, verifier.Compilation.DataSectionStringLiteralThreshold);

            verifier = CompileAndVerify(source,
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "3"),
                expectedOutput: expectedOutput)
                .VerifyDiagnostics()
                .VerifyIL("<top-level-statements-entry-point>", expectedIl);
            Assert.Equal(3, verifier.Compilation.DataSectionStringLiteralThreshold);

            verifier = CompileAndVerify(source,
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "2"),
                verify: Verification.Fails,
                expectedOutput: expectedOutput)
                .VerifyDiagnostics()
                .VerifyIL("<top-level-statements-entry-point>", """
                {
                  // Code size       31 (0x1f)
                  .maxstack  1
                  IL_0000:  ldstr      "a"
                  IL_0005:  call       "void System.Console.Write(string)"
                  IL_000a:  ldstr      "bb"
                  IL_000f:  call       "void System.Console.Write(string)"
                  IL_0014:  ldsfld     "string <PrivateImplementationDetails>.<S>BE20CA004CC2993A396345E0D52DF013.s"
                  IL_0019:  call       "void System.Console.Write(string)"
                  IL_001e:  ret
                }
                """);
            Assert.Equal(2, verifier.Compilation.DataSectionStringLiteralThreshold);

            verifier = CompileAndVerify(source,
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "1"),
                verify: Verification.Fails,
                expectedOutput: expectedOutput)
                .VerifyDiagnostics()
                .VerifyIL("<top-level-statements-entry-point>", """
                {
                  // Code size       31 (0x1f)
                  .maxstack  1
                  IL_0000:  ldstr      "a"
                  IL_0005:  call       "void System.Console.Write(string)"
                  IL_000a:  ldsfld     "string <PrivateImplementationDetails>.<S>DB1DE4B3DA6C7871B776D5CB968AA5A4.s"
                  IL_000f:  call       "void System.Console.Write(string)"
                  IL_0014:  ldsfld     "string <PrivateImplementationDetails>.<S>BE20CA004CC2993A396345E0D52DF013.s"
                  IL_0019:  call       "void System.Console.Write(string)"
                  IL_001e:  ret
                }
                """);
            Assert.Equal(1, verifier.Compilation.DataSectionStringLiteralThreshold);

            verifier = CompileAndVerify(source,
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"),
                verify: Verification.Fails,
                expectedOutput: expectedOutput)
                .VerifyDiagnostics()
                .VerifyIL("<top-level-statements-entry-point>", """
                {
                  // Code size       31 (0x1f)
                  .maxstack  1
                  IL_0000:  ldsfld     "string <PrivateImplementationDetails>.<S>A96FAF705AF16834E6C632B61E964E1F.s"
                  IL_0005:  call       "void System.Console.Write(string)"
                  IL_000a:  ldsfld     "string <PrivateImplementationDetails>.<S>DB1DE4B3DA6C7871B776D5CB968AA5A4.s"
                  IL_000f:  call       "void System.Console.Write(string)"
                  IL_0014:  ldsfld     "string <PrivateImplementationDetails>.<S>BE20CA004CC2993A396345E0D52DF013.s"
                  IL_0019:  call       "void System.Console.Write(string)"
                  IL_001e:  ret
                }
                """);
            Assert.Equal(0, verifier.Compilation.DataSectionStringLiteralThreshold);
        }

        [Fact]
        public void DataSectionStringLiterals_Switch()
        {
            var source = """
                System.Console.Write(args[0] switch 
                {
                    "a" => 1,
                    "bb" => 2,
                    "ccc" => 3,
                    _ => 4
                });
                """;

            var verifier = CompileAndVerify(
                source,
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"),
                verify: Verification.Skipped);

            verifier.VerifyIL("<top-level-statements-entry-point>", """
                {
                  // Code size       66 (0x42)
                  .maxstack  2
                  .locals init (int V_0,
                                string V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  ldc.i4.0
                  IL_0002:  ldelem.ref
                  IL_0003:  stloc.1
                  IL_0004:  ldloc.1
                  IL_0005:  ldsfld     "string <PrivateImplementationDetails>.<S>A96FAF705AF16834E6C632B61E964E1F.s"
                  IL_000a:  call       "bool string.op_Equality(string, string)"
                  IL_000f:  brtrue.s   IL_002d
                  IL_0011:  ldloc.1
                  IL_0012:  ldsfld     "string <PrivateImplementationDetails>.<S>DB1DE4B3DA6C7871B776D5CB968AA5A4.s"
                  IL_0017:  call       "bool string.op_Equality(string, string)"
                  IL_001c:  brtrue.s   IL_0031
                  IL_001e:  ldloc.1
                  IL_001f:  ldsfld     "string <PrivateImplementationDetails>.<S>BE20CA004CC2993A396345E0D52DF013.s"
                  IL_0024:  call       "bool string.op_Equality(string, string)"
                  IL_0029:  brtrue.s   IL_0035
                  IL_002b:  br.s       IL_0039
                  IL_002d:  ldc.i4.1
                  IL_002e:  stloc.0
                  IL_002f:  br.s       IL_003b
                  IL_0031:  ldc.i4.2
                  IL_0032:  stloc.0
                  IL_0033:  br.s       IL_003b
                  IL_0035:  ldc.i4.3
                  IL_0036:  stloc.0
                  IL_0037:  br.s       IL_003b
                  IL_0039:  ldc.i4.4
                  IL_003a:  stloc.0
                  IL_003b:  ldloc.0
                  IL_003c:  call       "void System.Console.Write(int)"
                  IL_0041:  ret
                }
                """);
        }

        [Theory]
        [InlineData("""public static string M() => "abc";""")]
        [InlineData("""public static void M(string s) { switch (s) { case "abc": break; } }""")]
        public void DataSectionStringLiterals_UsedAssemblyReferences(string code)
        {
            var source1 = """
                namespace System
                {
                    public class Object;
                    public class String
                    {
                        public static bool op_Equality(string a, string b) => false;
                    }
                    public class ValueType;
                    public struct Void;
                    public struct Byte;
                    public struct Int16;
                    public struct Int32;
                    public struct Int64;
                    public struct Boolean;
                    public class Attribute;
                    public class Enum;
                    public enum AttributeTargets;
                    public class AttributeUsageAttribute
                    {
                        public AttributeUsageAttribute(AttributeTargets validOn) { }
                        public bool AllowMultiple { get; set; }
                        public bool Inherited { get; set; }
                    }
                }
                """;
            var ref1 = CreateEmptyCompilation(source1, assemblyName: "MinimalCoreLib").VerifyDiagnostics().EmitToImageReference();

            var source2 = """
                namespace System.Text
                {
                    public class Encoding
                    {
                        public static Encoding UTF8 => null;
                        public unsafe string GetString(byte* bytes, int byteCount) => null;
                    }
                }
                """;
            var ref2 = CreateEmptyCompilation(source2, [ref1], options: TestOptions.UnsafeDebugDll, assemblyName: "Encoding")
                .VerifyDiagnostics().EmitToImageReference();

            var source3 = $$"""
                public static class C
                {
                    {{code}}
                }
                """;
            var comp = CreateEmptyCompilation(source3, [ref1, ref2], assemblyName: "Lib1");
            AssertEx.SetEqual([ref1], comp.GetUsedAssemblyReferences());
            comp.VerifyEmitDiagnostics();

            comp = CreateEmptyCompilation(source3, [ref1, ref2], assemblyName: "Lib2",
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"));
            AssertEx.SetEqual([ref1, ref2], comp.GetUsedAssemblyReferences());
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void DataSectionStringLiterals_InvalidUtf8()
        {
            var source = """
                System.Console.WriteLine("Hello \uD801\uD802");
                """;
            CompileAndVerify(source,
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"),
                expectedOutput: "Hello \uD801\uD802",
                symbolValidator: static (ModuleSymbol module) =>
                {
                    // No <S> types expected.
                    AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                        <Module>
                        EmbeddedAttribute
                        RefSafetyRulesAttribute
                        Program
                        """, module.TypeNames.Join("\n"));
                })
                .VerifyDiagnostics()
                .VerifyIL("<top-level-statements-entry-point>", $$"""
                    {
                      // Code size       11 (0xb)
                      .maxstack  1
                      IL_0000:  ldstr      "Hello {{"\uD801\uD802"}}"
                      IL_0005:  call       "void System.Console.WriteLine(string)"
                      IL_000a:  ret
                    }
                    """);
        }

        [Fact]
        public void DataSectionStringLiterals_HashCollision()
        {
            var emitOptions = new EmitOptions
            {
                // Take only the first byte of each string as its hash to simulate collisions.
                TestOnly_DataToHexViaXxHash128 = static (data) => data[0].ToString(),
            };
            var source = """
                System.Console.Write("a");
                System.Console.Write("b");
                System.Console.Write("aa");
                """;
            CreateCompilation(source,
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"))
                .VerifyEmitDiagnostics(emitOptions,
                    // (3,22): error CS9274: Cannot emit this string literal into the data section because it has XXHash128 collision with another string literal: a
                    // System.Console.Write("aa");
                    Diagnostic(ErrorCode.ERR_DataSectionStringLiteralHashCollision, @"""aa""").WithArguments("a").WithLocation(3, 22));
        }

        [Fact]
        public void DataSectionStringLiterals_SynthesizedTypes()
        {
            var source = """
                System.Console.WriteLine("Hello");
                """;
            var verifier = CompileAndVerify(source,
                targetFramework: TargetFramework.Mscorlib46,
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"),
                verify: Verification.Fails,
                expectedOutput: "Hello",
                symbolValidator: static (ModuleSymbol module) =>
                {
                    AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                        <Module>
                        EmbeddedAttribute
                        RefSafetyRulesAttribute
                        Program
                        <PrivateImplementationDetails>
                        __StaticArrayInitTypeSize=5
                        <S>1BFD09D1A433FB78117B4C7B1583D16D
                        """, module.TypeNames.Join("\n"));
                });
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("<top-level-statements-entry-point>", """
                {
                  // Code size       11 (0xb)
                  .maxstack  1
                  IL_0000:  ldsfld     "string <PrivateImplementationDetails>.<S>1BFD09D1A433FB78117B4C7B1583D16D.s"
                  IL_0005:  call       "void System.Console.WriteLine(string)"
                  IL_000a:  ret
                }
                """);

            var offset = ExecutionConditionUtil.IsUnix ? "00002890" : "00002850";
            verifier.VerifyTypeIL("<PrivateImplementationDetails>", $$"""
                .class private auto ansi sealed '<PrivateImplementationDetails>'
                	extends [mscorlib]System.Object
                {
                	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                		01 00 00 00
                	)
                	// Nested Types
                	.class nested assembly explicit ansi sealed '__StaticArrayInitTypeSize=5'
                		extends [mscorlib]System.ValueType
                	{
                		.pack 1
                		.size 5
                	} // end of class __StaticArrayInitTypeSize=5
                	.class nested assembly auto ansi sealed beforefieldinit '<S>1BFD09D1A433FB78117B4C7B1583D16D'
                		extends [mscorlib]System.Object
                	{
                		// Fields
                		.field assembly static initonly string s
                		// Methods
                		.method private hidebysig specialname rtspecialname static 
                			void .cctor () cil managed 
                		{
                			// Method begins at RVA 0x2089
                			// Code size 17 (0x11)
                			.maxstack 8
                			IL_0000: ldsflda valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=5' '<PrivateImplementationDetails>'::'185F8DB32271FE25F561A6FC938B2E264306EC304EDA518007D1764826381969'
                			IL_0005: ldc.i4.5
                			IL_0006: call string '<PrivateImplementationDetails>'::BytesToString(uint8*, int32)
                			IL_000b: stsfld string '<PrivateImplementationDetails>'/'<S>1BFD09D1A433FB78117B4C7B1583D16D'::s
                			IL_0010: ret
                		} // end of method '<S>1BFD09D1A433FB78117B4C7B1583D16D'::.cctor
                	} // end of class <S>1BFD09D1A433FB78117B4C7B1583D16D
                	// Fields
                	.field assembly static initonly valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=5' '185F8DB32271FE25F561A6FC938B2E264306EC304EDA518007D1764826381969' at I_{{offset}}
                    .data cil I_{{offset}} = bytearray (
                		48 65 6c 6c 6f
                	)
                	// Methods
                	.method private hidebysig static 
                		string BytesToString (
                			uint8* bytes,
                			int32 length
                		) cil managed 
                	{
                		// Method begins at RVA 0x207b
                		// Code size 13 (0xd)
                		.maxstack 8
                		IL_0000: call class [mscorlib]System.Text.Encoding [mscorlib]System.Text.Encoding::get_UTF8()
                		IL_0005: ldarg.0
                		IL_0006: ldarg.1
                		IL_0007: callvirt instance string [mscorlib]System.Text.Encoding::GetString(uint8*, int32)
                		IL_000c: ret
                	} // end of method '<PrivateImplementationDetails>'::BytesToString
                } // end of class <PrivateImplementationDetails>
                """);
        }

        [Theory, CombinatorialData]
        public void DataSectionStringLiterals_MetadataOnly(
            [CombinatorialValues("0", "off")] string feature)
        {
            var source = """
                class C
                {
                    void M()
                    {
                        System.Console.WriteLine("Hello");
                    }
                }
                """;
            CompileAndVerify(source,
                emitOptions: EmitOptions.Default.WithEmitMetadataOnly(true),
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, feature),
                symbolValidator: static (ModuleSymbol module) =>
                {
                    AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                        <Module>
                        EmbeddedAttribute
                        RefSafetyRulesAttribute
                        C
                        """, module.TypeNames.Join("\n"));
                })
                .VerifyDiagnostics();
        }

        [Fact]
        public void DataSectionStringLiterals_SharedType()
        {
            var source = """
                using static System.Console;

                Write("a");
                Write("b");
                Write("ccc");
                Write("ddd");
                """;
            CompileAndVerify(source,
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"),
                options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All),
                verify: Verification.Fails,
                expectedOutput: "abcccddd",
                symbolValidator: static (ModuleSymbol module) =>
                {
                    var privateImplDetails = module.GlobalNamespace.GetTypeMember("<PrivateImplementationDetails>");

                    // Data fields
                    AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                        <PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.64DAA44AD493FF28A96EFFAB6E77F1732A3D97D83241581B37DBD70A7A4900FE
                        <PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.730F75DAFD73E047B86ACB2DBD74E75DCB93272FA084A9082848F2341AA1ABB6
                        System.Byte <PrivateImplementationDetails>.3E23E8160039594A33894F6564E1B1348BBD7A0088D42C4ACB73EEAED59C009D
                        System.Byte <PrivateImplementationDetails>.CA978112CA1BBDCAFAC231B39A23DC4DA786EFF8147C4E72B9807785AFEE48BB
                        """,
                        privateImplDetails.GetMembers().OfType<FieldSymbol>().Select(f => f.ToTestDisplayString()).Order().Join("\n"));

                    // Nested types
                    AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                        __StaticArrayInitTypeSize=3
                        <S>A96FAF705AF16834E6C632B61E964E1F
                        <S>4B2212E31AC97FD4575A0B1C44D8843F
                        <S>BE20CA004CC2993A396345E0D52DF013
                        <S>1F6CEF082E150274999DD6657C23A29E
                        """,
                        privateImplDetails.GetTypeMembers().Select(t => t.Name).Join("\n"));
                })
                .VerifyDiagnostics();
        }

        [Fact]
        public void DataSectionStringLiterals_SharedValue()
        {
            var source = """
                using static System.Console;

                Write("a");
                Write("a");
                Write("bbb");
                Write("bbb");
                """;
            CompileAndVerify(source,
                parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"),
                options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All),
                verify: Verification.Fails,
                expectedOutput: "aabbbbbb",
                symbolValidator: static (ModuleSymbol module) =>
                {
                    var privateImplDetails = module.GlobalNamespace.GetTypeMember("<PrivateImplementationDetails>");

                    // Data fields
                    AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                        <PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.3E744B9DC39389BAF0C5A0660589B8402F3DBB49B89B3E75F2C9355852A3C677
                        System.Byte <PrivateImplementationDetails>.CA978112CA1BBDCAFAC231B39A23DC4DA786EFF8147C4E72B9807785AFEE48BB
                        """,
                        privateImplDetails.GetMembers().OfType<FieldSymbol>().Select(f => f.ToTestDisplayString()).Order().Join("\n"));

                    // Nested types
                    AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                        __StaticArrayInitTypeSize=3
                        <S>A96FAF705AF16834E6C632B61E964E1F
                        <S>A6DC9C19EFE4ABBCBE168A8B4D34D73A
                        """,
                        privateImplDetails.GetTypeMembers().Select(t => t.Name).Join("\n"));
                })
                .VerifyDiagnostics();
        }

        [Fact]
        public void DataSectionStringLiterals_SharedType_ArrayInitializer()
        {
            var source = """
                using System;
                using static System.Console;

                Write("abc");
                M(new byte[] { 1, 2, 3 });

                static void M(ReadOnlySpan<byte> x)
                {
                    foreach (var b in x)
                    {
                        Write(b);
                    }
                }
                """;
            CompileAndVerify(
                CreateCompilationWithSpan(source,
                    parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"),
                    options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All)),
                verify: Verification.Fails,
                expectedOutput: "abc123",
                symbolValidator: static (ModuleSymbol module) =>
                {
                    // Data fields
                    AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                        <PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81
                        <PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD
                        """,
                        module.GlobalNamespace.GetTypeMember("<PrivateImplementationDetails>").GetMembers()
                            .OfType<FieldSymbol>().Select(f => f.ToTestDisplayString()).Order().Join("\n"));
                })
                .VerifyDiagnostics();
        }

        [Fact]
        public void DataSectionStringLiterals_SharedValue_ArrayInitializer()
        {
            var source = """
                using System;
                using static System.Console;

                Write("abc");
                M(new byte[] { 97, 98, 99 });

                static void M(ReadOnlySpan<byte> x)
                {
                    foreach (var b in x)
                    {
                        Write((char)b);
                    }
                }
                """;
            CompileAndVerify(
                CreateCompilationWithSpan(source,
                    parseOptions: TestOptions.Regular.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"),
                    options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All)),
                verify: Verification.Fails,
                expectedOutput: "abcabc",
                symbolValidator: static (ModuleSymbol module) =>
                {
                    // Data fields
                    AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                        <PrivateImplementationDetails>.__StaticArrayInitTypeSize=3 <PrivateImplementationDetails>.BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD
                        """,
                        module.GlobalNamespace.GetTypeMember("<PrivateImplementationDetails>").GetMembers()
                            .OfType<FieldSymbol>().Select(f => f.ToTestDisplayString()).Order().Join("\n"));
                })
                .VerifyDiagnostics();
        }

        /// <summary>
        /// Tests a scenario that utilizes a private implementation detail class,
        /// but doesn't use the string type, and the string type is not defined.
        /// </summary>
        [Fact]
        public void PrivateImplDetailsWithoutString()
        {
            var source = """
                #pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type

                class C
                {
                    bool M(int i) => i switch { 1 => true };
                }

                namespace System
                {
                    public class Object;
                    public class ValueType;
                    public struct Void;
                    public struct Boolean;
                    public struct Byte;
                    public struct Int16;
                    public struct Int32;
                    public struct Int64;
                    public class InvalidOperationException();
                }
                """;

            var parseOptions = TestOptions.RegularPreview
                .WithNoRefSafetyRulesAttribute();

            CompileAndVerify(CreateEmptyCompilation(source, parseOptions: parseOptions),
                verify: Verification.Skipped,
                symbolValidator: static (ModuleSymbol module) =>
                {
                    // PrivateImplementationDetails should be in the list.
                    AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                        <Module>
                        C
                        Object
                        ValueType
                        Void
                        Boolean
                        Byte
                        Int16
                        Int32
                        Int64
                        InvalidOperationException
                        <PrivateImplementationDetails>
                        """, module.TypeNames.Join("\n"));
                })
                .VerifyDiagnostics(
                    // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                    Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1));

            // NOTE: If the feature is enabled by default in the future, it should not fail in case of missing Encoding members
            //       (it should be automatically disabled instead and could warn) to avoid regressing the scenario above.
            CreateEmptyCompilation(source,
                parseOptions: parseOptions.WithFeature(FeatureFlag.ExperimentalDataSectionStringLiterals, "0"))
                .VerifyDiagnostics(
                // error CS0656: Missing compiler required member 'System.Text.Encoding.get_UTF8'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Text.Encoding", "get_UTF8").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.Text.Encoding.GetString'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Text.Encoding", "GetString").WithLocation(1, 1));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76707")]
        public void EmitMetadataOnly_Exe()
        {
            CompileAndVerify("""
                System.Console.WriteLine("a");
                """,
                options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All),
                emitOptions: EmitOptions.Default.WithEmitMetadataOnly(true),
                symbolValidator: static (ModuleSymbol module) =>
                {
                    Assert.NotEqual(0, module.GetMetadata().Module.PEReaderOpt.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress);
                    var main = module.GlobalNamespace.GetMember<MethodSymbol>("Program.<Main>$");
                    Assert.Equal(Accessibility.Private, main.DeclaredAccessibility);
                })
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76707")]
        public void EmitMetadataOnly_Exe_AsyncMain()
        {
            CompileAndVerify("""
                using System.Threading.Tasks;
                static class Program
                {
                    static async Task Main()
                    {
                        await Task.Yield();
                        System.Console.WriteLine("a");
                    }
                }
                """,
                options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All),
                emitOptions: EmitOptions.Default.WithEmitMetadataOnly(true),
                symbolValidator: static (ModuleSymbol module) =>
                {
                    Assert.NotEqual(0, module.GetMetadata().Module.PEReaderOpt.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress);
                    var main = module.GlobalNamespace.GetMember<MethodSymbol>("Program.<Main>");
                    Assert.Equal(Accessibility.Private, main.DeclaredAccessibility);
                })
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76707")]
        public void EmitMetadataOnly_Exe_NoMain()
        {
            var emitResult = CreateCompilation("""
                class Program;
                """,
                options: TestOptions.ReleaseExe)
                .Emit(new MemoryStream(), options: EmitOptions.Default.WithEmitMetadataOnly(true));
            Assert.False(emitResult.Success);
            emitResult.Diagnostics.Verify(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76707")]
        public void EmitMetadataOnly_Exe_PrivateMain_ExcludePrivateMembers()
        {
            CompileAndVerify("""
                static class Program
                {
                    private static void Main() { }
                }
                """,
                options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All),
                emitOptions: EmitOptions.Default
                    .WithEmitMetadataOnly(true)
                    .WithIncludePrivateMembers(false),
                symbolValidator: static (ModuleSymbol module) =>
                {
                    Assert.NotEqual(0, module.GetMetadata().Module.PEReaderOpt.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress);
                    var main = module.GlobalNamespace.GetMember<MethodSymbol>("Program.Main");
                    Assert.Equal(Accessibility.Private, main.DeclaredAccessibility);
                })
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76707")]
        public void EmitMetadataOnly_Exe_PrivateMain_ExcludePrivateMembers_AsyncMain()
        {
            CompileAndVerify("""
                using System.Threading.Tasks;
                static class Program
                {
                    private static async Task Main()
                    {
                        await Task.Yield();
                    }
                }
                """,
                options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All),
                emitOptions: EmitOptions.Default
                    .WithEmitMetadataOnly(true)
                    .WithIncludePrivateMembers(false),
                symbolValidator: static (ModuleSymbol module) =>
                {
                    Assert.NotEqual(0, module.GetMetadata().Module.PEReaderOpt.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress);
                    var main = module.GlobalNamespace.GetMember<MethodSymbol>("Program.<Main>");
                    Assert.Equal(Accessibility.Private, main.DeclaredAccessibility);
                })
                .VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76707")]
        public void ExcludePrivateMembers_PrivateMain()
        {
            using var peStream = new MemoryStream();
            using var metadataStream = new MemoryStream();
            var comp = CreateCompilation("""
                static class Program
                {
                    private static void Main() { }
                }
                """,
                options: TestOptions.ReleaseExe);
            var emitResult = comp.Emit(
                peStream: peStream,
                metadataPEStream: metadataStream,
                options: EmitOptions.Default.WithIncludePrivateMembers(false));
            Assert.True(emitResult.Success);
            emitResult.Diagnostics.Verify();

            verify(peStream);
            verify(metadataStream);

            CompileAndVerify(comp).VerifyDiagnostics();

            static void verify(Stream stream)
            {
                stream.Position = 0;
                Assert.NotEqual(0, new PEHeaders(stream).CorHeader.EntryPointTokenOrRelativeVirtualAddress);

                stream.Position = 0;
                var reference = AssemblyMetadata.CreateFromStream(stream).GetReference();
                var comp = CreateCompilation("", references: [reference],
                    options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
                var main = comp.GetMember<MethodSymbol>("Program.Main");
                Assert.Equal(Accessibility.Private, main.DeclaredAccessibility);
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76707")]
        public void ExcludePrivateMembers_PrivateMain_AsyncMain()
        {
            using var peStream = new MemoryStream();
            using var metadataStream = new MemoryStream();
            var comp = CreateCompilation("""
                using System.Threading.Tasks;
                static class Program
                {
                    private static async Task Main()
                    {
                        await Task.Yield();
                    }
                }
                """,
                options: TestOptions.ReleaseExe);
            var emitResult = comp.Emit(
                peStream: peStream,
                metadataPEStream: metadataStream,
                options: EmitOptions.Default.WithIncludePrivateMembers(false));
            Assert.True(emitResult.Success);
            emitResult.Diagnostics.Verify();

            verify(peStream);
            verify(metadataStream);

            CompileAndVerify(comp).VerifyDiagnostics();

            static void verify(Stream stream)
            {
                stream.Position = 0;
                Assert.NotEqual(0, new PEHeaders(stream).CorHeader.EntryPointTokenOrRelativeVirtualAddress);

                stream.Position = 0;
                var reference = AssemblyMetadata.CreateFromStream(stream).GetReference();
                var comp = CreateCompilation("", references: [reference],
                    options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
                var main = comp.GetMember<MethodSymbol>("Program.<Main>");
                Assert.Equal(Accessibility.Private, main.DeclaredAccessibility);
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76707")]
        public void ExcludePrivateMembers_DebugEntryPoint()
        {
            using var peStream = new MemoryStream();
            using var metadataStream = new MemoryStream();

            {
                var comp = CreateCompilation("""
                    static class Program
                    {
                        static void M1() { }
                        static void M2() { }
                    }
                    """).VerifyDiagnostics();
                var emitResult = comp.Emit(
                    peStream: peStream,
                    metadataPEStream: metadataStream,
                    debugEntryPoint: comp.GetMember<MethodSymbol>("Program.M1").GetPublicSymbol(),
                    options: EmitOptions.Default.WithIncludePrivateMembers(false));
                Assert.True(emitResult.Success);
                emitResult.Diagnostics.Verify();
            }

            {
                // M1 should be emitted (it's the debug entry-point), M2 shouldn't (private members are excluded).
                metadataStream.Position = 0;
                var reference = AssemblyMetadata.CreateFromStream(metadataStream).GetReference();
                var comp = CreateCompilation("", references: [reference],
                    options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
                var m1 = comp.GetMember<MethodSymbol>("Program.M1");
                Assert.Equal(Accessibility.Private, m1.DeclaredAccessibility);
                Assert.Null(comp.GetMember<MethodSymbol>("Program.M2"));
            }
        }
    }
}
