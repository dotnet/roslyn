// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Threading;
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

            CompileAndVerify(source, symbolValidator: module =>
            {
                var baseLine = System.Xml.Linq.XElement.Load(new StringReader(Resources.EmitSimpleBaseLine1));
                System.Xml.Linq.XElement dumpXML = DumpTypeInfo(module);

                Assert.Equal(baseLine.ToString(), dumpXML.ToString());
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

            CompileAndVerify(source, new[] { metadataTestLib1, metadataTestLib2 }, assemblyValidator: (assembly) =>
            {
                var refs = assembly.Modules[0].ReferencedAssemblies.OrderBy(r => r.Name).ToArray();
                Assert.Equal(2, refs.Length);
                Assert.Equal(refs[0].Name, "MDTestLib1", StringComparer.OrdinalIgnoreCase);
                Assert.Equal(refs[1].Name, "mscorlib", StringComparer.OrdinalIgnoreCase);
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
            CompileAndVerify(sources, new[] { TestReferences.SymbolsTests.MultiModule.Assembly }, assemblyValidator: (assembly) =>
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
                verify: false,
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
            CompileAndVerify(source, new[] { netModule1, netModule2 }, assemblyValidator: (assembly) =>
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

                Assert.Equal(5, reader.GetTableRowCount(TableIndex.ExportedType));
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

                Assert.Same(i1, classA.Interfaces.Single());

                var interfaces = classB.Interfaces;
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
                Assert.True(i1.Interfaces.SequenceEqual(ImmutableArray.Create<NamedTypeSymbol>(i2, i3, i4, i5, i6, i7)));
                Assert.True(i2.Interfaces.SequenceEqual(ImmutableArray.Create<NamedTypeSymbol>(i3, i4)));
                Assert.False(i3.Interfaces.Any());
                Assert.False(i4.Interfaces.Any());
                Assert.True(i5.Interfaces.SequenceEqual(ImmutableArray.Create<NamedTypeSymbol>(i6, i7)));
                Assert.False(i6.Interfaces.Any());
                Assert.False(i7.Interfaces.Any());

                Assert.True(c.Interfaces.SequenceEqual(ImmutableArray.Create<NamedTypeSymbol>(i1, i2, i3, i4, i5, i6, i7)));
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

                Assert.Equal(6, ((PEModuleSymbol)module).Module.GetMetadataReader().TypeReferences.Count);
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
            CompileAndVerify(source, options: TestOptions.ReleaseDll, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
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
                Assert.Equal(0, f1.CustomModifiers.Length);

                Assert.True(f2.IsVolatile);
                Assert.Equal(1, f2.CustomModifiers.Length);

                CustomModifier mod = f2.CustomModifiers[0];

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
                var type = module.GlobalNamespace.GetNamespaceMembers().Single().GetTypeMembers("C").Single();
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
                Assert.Equal("Void", ctor.ReturnType.Name);

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
                    Assert.Equal(0, cctor.TypeArguments.Length);
                    Assert.Equal(0, cctor.Parameters.Length);
                    Assert.Equal("Void", cctor.ReturnType.Name);
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
                var nmspace = module.GlobalNamespace.GetNamespaceMembers().Single();
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
                Assert.Equal(derivedType.Arity, 2);

                var baseType = derivedType.BaseType;
                Assert.Equal(baseType.Name, "Base");
                Assert.Equal(baseType.Arity, 2);

                Assert.Equal(derivedType.BaseType, baseType);
                Assert.Same(baseType.TypeArguments[0], derivedType.TypeParameters[0]);
                Assert.Same(baseType.TypeArguments[1], derivedType.TypeParameters[1]);
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
            var comp = CreateCompilationWithMscorlib45(@"using System;
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
            var comp = CreateCompilationWithMscorlib(@"using System;
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
}", parseOptions: TestOptions.ExperimentalParseOptions,
    options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.Internal));
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

                var p = type.GetMember<SourcePropertySymbol>("P");
                var pBack = p.BackingField;
                Assert.False(pBack.IsReadOnly);
                Assert.False(pBack.IsStatic);
                Assert.Equal(pBack.Type.SpecialType, SpecialType.System_Int32);

                var q = type.GetMember<SourcePropertySymbol>("Q");
                var qBack = q.BackingField;
                Assert.False(qBack.IsReadOnly);
                Assert.False(qBack.IsStatic);
                Assert.Equal(qBack.Type.SpecialType, SpecialType.System_String);

                var r = type.GetMember<SourcePropertySymbol>("R");
                var rBack = r.BackingField;
                Assert.True(rBack.IsReadOnly);
                Assert.False(rBack.IsStatic);
                Assert.Equal(rBack.Type.SpecialType, SpecialType.System_Decimal);

                var s = type.GetMember<SourcePropertySymbol>("S");
                var sBack = s.BackingField;
                Assert.True(sBack.IsReadOnly);
                Assert.True(sBack.IsStatic);
                Assert.Equal(sBack.Type.SpecialType, SpecialType.System_Char);
            };

            CompileAndVerify(
                comp,
                sourceSymbolValidator: validator,
                expectedOutput: "1test300S");
        }

        [Fact]
        public void AutoPropInitializersStruct()
        {
            var comp = CreateCompilationWithMscorlib(@"
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
}", parseOptions: TestOptions.ExperimentalParseOptions,
    options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.Internal));

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("S");

                var p = type.GetMember<SourceMemberFieldSymbol>("P");
                Assert.False(p.HasInitializer);
                Assert.True(p.IsReadOnly);
                Assert.False(p.IsStatic);
                Assert.Equal(p.Type.SpecialType, SpecialType.System_Int32);

                var q = type.GetMember<SourcePropertySymbol>("Q");
                var qBack = q.BackingField;
                Assert.True(qBack.IsReadOnly);
                Assert.False(qBack.IsStatic);
                Assert.Equal(qBack.Type.SpecialType, SpecialType.System_String);

                var r = type.GetMember<SourcePropertySymbol>("R");
                var rBack = r.BackingField;
                Assert.True(rBack.IsReadOnly);
                Assert.False(rBack.IsStatic);
                Assert.Equal(rBack.Type.SpecialType, SpecialType.System_Decimal);

                var s = type.GetMember<SourcePropertySymbol>("T");
                var sBack = s.BackingField;
                Assert.True(sBack.IsReadOnly);
                Assert.True(sBack.IsStatic);
                Assert.Equal(sBack.Type.SpecialType, SpecialType.System_Char);
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
            var type = property.Type;
            Assert.NotEqual(type.PrimitiveTypeCode, Microsoft.Cci.PrimitiveTypeCode.Void);
            Assert.Equal(propertyAccessibility, property.DeclaredAccessibility);
            CheckPropertyAccessorAccessibility(property, propertyAccessibility, property.GetMethod, getterAccessibility);
            CheckPropertyAccessorAccessibility(property, propertyAccessibility, property.SetMethod, setterAccessibility);
        }

        private static void CheckPropertyAccessorAccessibility(PropertySymbol property, Accessibility propertyAccessibility, MethodSymbol accessor, Accessibility accessorAccessibility)
        {
            if (accessor == null)
            {
                Assert.Equal(accessorAccessibility, Accessibility.NotApplicable);
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
                var classA = module.GlobalNamespace.GetTypeMembers("A").Single();
                var p = classA.GetMembers("P").OfType<PropertySymbol>().Single();
                VerifyAutoProperty(p, isFromSource);
                var q = classA.GetMembers("Q").OfType<PropertySymbol>().Single();
                VerifyAutoProperty(q, isFromSource);

                var classC = module.GlobalNamespace.GetTypeMembers("C").Single();
                p = classC.BaseType.GetMembers("P").OfType<PropertySymbol>().Single();
                VerifyAutoProperty(p, isFromSource);
                Assert.Equal(p.Type.SpecialType, SpecialType.System_String);
                Assert.Equal(p.GetMethod.AssociatedSymbol, p);
            };

            CompileAndVerify(source, sourceSymbolValidator: validator(true), symbolValidator: validator(false), options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));
        }

        private static void VerifyAutoProperty(PropertySymbol property, bool isFromSource)
        {
            var sourceProperty = property as SourcePropertySymbol;
            if (sourceProperty != null)
            {
                Assert.True(sourceProperty.IsAutoProperty);
                Assert.Equal(((SourceAssemblySymbol)sourceProperty.ContainingAssembly).DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor),
                    sourceProperty.BackingField.GetSynthesizedAttributes().Single().AttributeConstructor);
            }

            VerifyAutoPropertyAccessor(property, property.GetMethod, isFromSource);
            VerifyAutoPropertyAccessor(property, property.SetMethod, isFromSource);
        }

        private static void VerifyAutoPropertyAccessor(PropertySymbol property, MethodSymbol accessor, bool isFromSource)
        {
            if (accessor != null)
            {
                var method = property.ContainingType.GetMembers(accessor.Name).Single();
                Assert.Equal(method, accessor);
                Assert.Equal(accessor.AssociatedSymbol, property);
                if (isFromSource)
                {
                    Assert.False(accessor.IsImplicitlyDeclared, "MethodSymbol.IsImplicitlyDeclared should be false for auto property accessors");
                }
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
            if (sourceType != null)
            {
                var fieldDefinition = (Microsoft.Cci.IFieldDefinition)field;
                Assert.False(fieldDefinition.IsSpecialName);
                Assert.False(fieldDefinition.IsRuntimeSpecial);
            }
        }

        private void CheckEnumType(NamedTypeSymbol type, Accessibility declaredAccessibility, SpecialType underlyingType)
        {
            Assert.Equal(type.BaseType.SpecialType, SpecialType.System_Enum);
            Assert.Equal(type.EnumUnderlyingType.SpecialType, underlyingType);
            Assert.Equal(type.DeclaredAccessibility, declaredAccessibility);
            Assert.True(type.IsSealed);

            // value__ field should not be exposed from type, even though it is public,
            // since we want to prevent source from accessing the field directly.
            var field = type.GetMembers(WellKnownMemberNames.EnumBackingFieldName).SingleOrDefault() as FieldSymbol;
            Assert.Null(field);

            var sourceType = type as SourceNamedTypeSymbol;
            if (sourceType != null)
            {
                field = sourceType.EnumValueField;
                Assert.NotNull(field);
                Assert.Equal(field.Name, WellKnownMemberNames.EnumBackingFieldName);
                Assert.False(field.IsStatic);
                Assert.False(field.IsConst);
                Assert.False(field.IsReadOnly);
                Assert.Equal(field.DeclaredAccessibility, Accessibility.Public); // Dev10: value__ is public
                Assert.Equal(field.Type, type.EnumUnderlyingType);

                var module = new PEAssemblyBuilder((SourceAssemblySymbol)sourceType.ContainingAssembly, EmitOptions.Default, OutputKind.DynamicallyLinkedLibrary,
                    GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable<ResourceDescription>());

                var context = new EmitContext(module, null, new DiagnosticBag());

                var typeDefinition = (Microsoft.Cci.ITypeDefinition)type;
                var fieldDefinition = typeDefinition.GetFields(context).First();
                Assert.Same(fieldDefinition, field); // Dev10: value__ field is the first field.
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
    void foo() 
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
        System.Console.WriteLine(f);
        System.Console.WriteLine(d);
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
                        Assert.Equal(invoke.Parameters[i].Type, endInvoke.Parameters[k].Type);
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

    private static void Foo()
    {
        System.Console.WriteLine(msg);
    }

    public static void Main()
    {
        Foo();
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
            var comp = CreateCompilationWithMscorlib("class Test { static void Main() { } }");

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
            var options = EmitOptions.Default.WithFileAlignment(8192);
            var syntax = SyntaxFactory.ParseSyntaxTree(@"class C {}", TestOptions.Regular);

            var peStream = CreateCompilationWithMscorlib(
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
            Assert.Equal(-609170495, coffHeader.TimeDateStamp);

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

            var syntax = SyntaxFactory.ParseSyntaxTree(@"class C { static void Main() { } }", TestOptions.Regular);

            var peStream = CreateCompilationWithMscorlib(
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
            Assert.Equal(-862605524, coffHeader.TimeDateStamp);

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
    }
}
