// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class SymbolMatcherTests : EditAndContinueTestBase
    {
        private static PEAssemblySymbol CreatePEAssemblySymbol(string source)
        {
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            var reference = compilation.EmitToImageReference();
            return (PEAssemblySymbol)CreateCompilation("", new[] { reference }).GetReferencedAssemblySymbol(reference);
        }

        private static CSharpSymbolMatcher CreateMatcher(CSharpCompilation fromCompilation, CSharpCompilation toCompilation)
            => new CSharpSymbolMatcher(
                fromCompilation.SourceAssembly,
                toCompilation.SourceAssembly,
                SynthesizedTypeMaps.Empty,
                otherSynthesizedMembers: SpecializedCollections.EmptyReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>>(),
                otherDeletedMembers: SpecializedCollections.EmptyReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>>());

        private static CSharpSymbolMatcher CreateMatcher(CSharpCompilation fromCompilation, PEAssemblySymbol peAssemblySymbol)
            => new CSharpSymbolMatcher(
                fromCompilation.SourceAssembly,
                peAssemblySymbol,
                SynthesizedTypeMaps.Empty);

        private static IEnumerable<string> Inspect(ImmutableSegmentedDictionary<AnonymousDelegateWithIndexedNamePartialKey, ImmutableArray<AnonymousTypeValue>> anonymousDelegatesWithIndexedNames)
            => from entry in anonymousDelegatesWithIndexedNames
               from value in entry.Value
               select $"({entry.Key.GenericArity},{entry.Key.ParameterCount}): {value.Type}";

        [Fact]
        public void ConcurrentAccess()
        {
            var source =
@"class A
{
    B F;
    D P { get; set; }
    void M(A a, B b, S s, I i) { }
    delegate void D(S s);
    class B { }
    struct S { }
    interface I { }
}
class B
{
    A M<T, U>() where T : A where U : T, I { return null; }
    event D E;
    delegate void D(S s);
    struct S { }
    interface I { }
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var builder = new List<Symbol>();
            var type = compilation1.GetMember<NamedTypeSymbol>("A");
            builder.Add(type);
            builder.AddRange(type.GetMembers());
            type = compilation1.GetMember<NamedTypeSymbol>("B");
            builder.Add(type);
            builder.AddRange(type.GetMembers());
            var members = builder.ToImmutableArray();
            Assert.True(members.Length > 10);

            for (int i = 0; i < 10; i++)
            {
                var matcher = CreateMatcher(compilation1, compilation0);

                var tasks = new Task[10];
                for (int j = 0; j < tasks.Length; j++)
                {
                    int startAt = i + j + 1;
                    tasks[j] = Task.Run(() =>
                    {
                        MatchAll(matcher, members, startAt);
                        Thread.Sleep(10);
                    });
                }
                Task.WaitAll(tasks);
            }
        }

        private static void MatchAll(CSharpSymbolMatcher matcher, ImmutableArray<Symbol> members, int startAt)
        {
            int n = members.Length;
            for (int i = 0; i < n; i++)
            {
                var member = members[(i + startAt) % n];
                var other = matcher.MapDefinition((Cci.IDefinition)member.GetCciAdapter());
                Assert.NotNull(other);
            }
        }

        [Fact]
        public void TypeArguments()
        {
            const string source =
@"class A<T>
{
    class B<U>
    {
        static A<V> M<V>(A<U>.B<T> x, A<object>.S y)
        {
            return null;
        }
        static A<V> M<V>(A<U>.B<T> x, A<V>.S y)
        {
            return null;
        }
    }
    struct S
    {
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var matcher = CreateMatcher(compilation1, compilation0);
            var members = compilation1.GetMember<NamedTypeSymbol>("A.B").GetMembers("M");
            Assert.Equal(2, members.Length);
            foreach (var member in members)
            {
                var other = matcher.MapDefinition((Cci.IMethodDefinition)member.GetCciAdapter());
                Assert.NotNull(other);
            }
        }

        [Fact]
        public void Constraints()
        {
            const string source =
@"interface I<T> where T : I<T>
{
}
class C
{
    static void M<T>(I<T> o) where T : I<T>
    {
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var matcher = CreateMatcher(compilation1, compilation0);
            var member = compilation1.GetMember<MethodSymbol>("C.M");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            Assert.NotNull(other);
        }

        [Fact]
        public void CustomModifiers()
        {
            var ilSource =
@".class public abstract A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance object modopt(A) [] F(int32 modopt(object) *p) { }
}";
            var metadataRef = CompileIL(ilSource);
            var source =
@"unsafe class B : A
{
    public override object[] F(int* p) { return null; }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, references: new[] { metadataRef });
            var compilation1 = compilation0.WithSource(source);

            var member1 = compilation1.GetMember<MethodSymbol>("B.F");
            Assert.Equal(1, ((PointerTypeSymbol)member1.Parameters[0].Type).PointedAtTypeWithAnnotations.CustomModifiers.Length);
            Assert.Equal(1, ((ArrayTypeSymbol)member1.ReturnType).ElementTypeWithAnnotations.CustomModifiers.Length);

            var matcher = CreateMatcher(compilation1, compilation0);

            var other = (MethodSymbol)matcher.MapDefinition(member1.GetCciAdapter()).GetInternalSymbol();
            Assert.NotNull(other);
            Assert.Equal(1, ((PointerTypeSymbol)other.Parameters[0].Type).PointedAtTypeWithAnnotations.CustomModifiers.Length);
            Assert.Equal(1, ((ArrayTypeSymbol)other.ReturnType).ElementTypeWithAnnotations.CustomModifiers.Length);
        }

        [Fact]
        public void CustomModifiers_InAttribute_Source()
        {
            // The parameter is emitted as
            // int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute)

            var source0 = @"
abstract class C
{  
    // matching
    public abstract void F(in int x);
    public virtual void G(in int x) => throw null;

    // non-matching
    public void H(in int x) => throw null;
}";
            var source1 = @"
abstract class C
{
    // matching
    public abstract void F(in int x);
    public virtual void G(in int x) => throw null;

    // non-matching
    public void H(int x) => throw null;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var g0 = compilation0.GetMember<MethodSymbol>("C.G");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var g1 = compilation1.GetMember<MethodSymbol>("C.G");
            var h1 = compilation1.GetMember<MethodSymbol>("C.H");

            Assert.Same(f0, (MethodSymbol)matcher.MapDefinition(f1.GetCciAdapter()).GetInternalSymbol());
            Assert.Same(g0, (MethodSymbol)matcher.MapDefinition(g1.GetCciAdapter()).GetInternalSymbol());
            Assert.Null(matcher.MapDefinition(h1.GetCciAdapter()));
        }

        [Fact]
        public void CustomModifiers_InAttribute_Metadata()
        {
            // The parameter is emitted as
            // int32& modreq([mscorlib]System.Runtime.InteropServices.InAttribute)

            var source0 = @"
abstract class C
{  
    // matching
    public abstract void F(in int x);
    public virtual void G(in int x) => throw null;

    // non-matching
    public void H(in int x) => throw null;
}";
            var source1 = @"
abstract class C
{
    // matching
    public abstract void F(in int x);
    public virtual void G(in int x) => throw null;

    // non-matching
    public void H(int x) => throw null;
}";

            var peAssemblySymbol = CreatePEAssemblySymbol(source0);

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll).WithSource(source1);

            var matcher = CreateMatcher(compilation1, peAssemblySymbol);

            var f0 = peAssemblySymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember("F");
            var g0 = peAssemblySymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember("G");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var g1 = compilation1.GetMember<MethodSymbol>("C.G");
            var h1 = compilation1.GetMember<MethodSymbol>("C.H");

            Assert.Equal(f0, (MethodSymbol)matcher.MapDefinition(f1.GetCciAdapter()).GetInternalSymbol());
            Assert.Equal(g0, (MethodSymbol)matcher.MapDefinition(g1.GetCciAdapter()).GetInternalSymbol());
            Assert.Null(matcher.MapDefinition(h1.GetCciAdapter()));
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void VaryingCompilationReferences()
        {
            string libSource = @"
public class D { }
";

            string source = @"
public class C
{
    public void F(D a) {}
}
";
            var lib0 = CreateCompilation(libSource, options: TestOptions.DebugDll, assemblyName: "Lib");
            var lib1 = CreateCompilation(libSource, options: TestOptions.DebugDll, assemblyName: "Lib");

            var compilation0 = CreateCompilation(source, new[] { lib0.ToMetadataReference() }, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source).WithReferences(MscorlibRef, lib1.ToMetadataReference());

            var matcher = CreateMatcher(compilation1, compilation0);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var mf1 = matcher.MapDefinition(f1.GetCciAdapter());
            Assert.Equal(f0, mf1.GetInternalSymbol());
        }

        [WorkItem(1533, "https://github.com/dotnet/roslyn/issues/1533")]
        [Fact]
        public void PreviousType_ArrayType()
        {
            var source0 = @"
class C
{  
    static void M()
    {
        int x = 0;
    }
    class D {}
}";
            var source1 = @"
class C
{
    static void M()
    {
        D[] x = null;
    }
    class D {}
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);
            var elementType = compilation1.GetMember<TypeSymbol>("C.D");
            var member = compilation1.CreateArrayTypeSymbol(elementType);
            var other = matcher.MapReference(member.GetCciAdapter());
            Assert.NotNull(other);
        }

        [WorkItem(1533, "https://github.com/dotnet/roslyn/issues/1533")]
        [Fact]
        public void NoPreviousType_ArrayType()
        {
            var source0 = @"
class C
{  
    static void M()
    {
        int x = 0;
    }
}";
            var source1 = @"
class C
{
    static void M()
    {
        D[] x = null;
    }
    class D {}
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);
            var elementType = compilation1.GetMember<TypeSymbol>("C.D");
            var member = compilation1.CreateArrayTypeSymbol(elementType);
            var other = matcher.MapReference(member.GetCciAdapter());
            // For a newly added type, there is no match in the previous generation.
            Assert.Null(other);
        }

        [WorkItem(1533, "https://github.com/dotnet/roslyn/issues/1533")]
        [Fact]
        public void NoPreviousType_PointerType()
        {
            var source0 = @"
class C
{  
    static void M()
    {
        int x = 0;
    }
}";
            var source1 = @"
class C
{
    static unsafe void M()
    {
        D* x = null;
    }
    struct D {}
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);
            var elementType = compilation1.GetMember<TypeSymbol>("C.D");
            var member = compilation1.CreatePointerTypeSymbol(elementType);
            var other = matcher.MapReference(member.GetCciAdapter());
            // For a newly added type, there is no match in the previous generation.
            Assert.Null(other);
        }

        [WorkItem(1533, "https://github.com/dotnet/roslyn/issues/1533")]
        [Fact]
        public void NoPreviousType_GenericType()
        {
            var source0 = @"
using System.Collections.Generic;
class C
{  
    static void M()
    {
        int x = 0;
    }
}";
            var source1 = @"
using System.Collections.Generic;
class C
{
    static void M()
    {
        List<D> x = null;
    }
    class D {}
    List<D> y;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);
            var member = compilation1.GetMember<FieldSymbol>("C.y");
            var other = matcher.MapReference((Cci.ITypeReference)member.Type.GetCciAdapter());
            // For a newly added type, there is no match in the previous generation.
            Assert.Null(other);
        }

        [Fact]
        public void HoistedAnonymousTypes()
        {
            var source0 = @"
using System;

class C
{
    static void F()
    {
        var x1 = new { A = 1 };
        var x2 = new { B = 1 };
        var y = new Func<int>(() => x1.A + x2.B);
    }
}
";
            var source1 = @"
using System;

class C
{
    static void F()
    {
        var x1 = new { A = 1 };
        var x2 = new { b = 1 };
        var y = new Func<int>(() => x1.A + x2.b);
    }
}";

            var peAssemblySymbol0 = CreatePEAssemblySymbol(source0);
            var peModule0 = (PEModuleSymbol)peAssemblySymbol0.Modules[0];

            var reader0 = peModule0.Module.MetadataReader;
            var decoder0 = new MetadataDecoder(peModule0);

            var synthesizedTypes0 = PEDeltaAssemblyBuilder.GetSynthesizedTypesFromMetadata(reader0, decoder0);
            var anonymousTypeMap0 = synthesizedTypes0.AnonymousTypes;
            Assert.Equal("<>f__AnonymousType0", anonymousTypeMap0[new AnonymousTypeKey(ImmutableArray.Create(new AnonymousTypeKeyField("A", isKey: false, ignoreCase: false)))].Name);
            Assert.Equal("<>f__AnonymousType1", anonymousTypeMap0[new AnonymousTypeKey(ImmutableArray.Create(new AnonymousTypeKeyField("B", isKey: false, ignoreCase: false)))].Name);
            Assert.Equal(2, anonymousTypeMap0.Count);

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll);

            var testData = new CompilationTestData();
            compilation1.EmitToArray(testData: testData);
            var peAssemblyBuilder = (PEAssemblyBuilder)testData.Module;

            var c = compilation1.GetMember<NamedTypeSymbol>("C");
            var displayClass = peAssemblyBuilder.GetSynthesizedTypes(c).Single();
            Assert.Equal("<>c__DisplayClass0_0", displayClass.Name);

            var emitContext = new EmitContext(peAssemblyBuilder, null, new DiagnosticBag(), metadataOnly: false, includePrivateMembers: true);

            var fields = displayClass.GetFields(emitContext).ToArray();
            var x1 = fields[0];
            var x2 = fields[1];
            Assert.Equal("x1", x1.Name);
            Assert.Equal("x2", x2.Name);

            var matcher = new CSharpSymbolMatcher(compilation1.SourceAssembly, peAssemblySymbol0, synthesizedTypes0);

            var mappedX1 = (Cci.IFieldDefinition)matcher.MapDefinition(x1);
            var mappedX2 = (Cci.IFieldDefinition)matcher.MapDefinition(x2);

            Assert.Equal("x1", mappedX1.Name);
            Assert.Null(mappedX2);
        }

        [Fact]
        public void HoistedAnonymousTypes_Complex()
        {
            var source0 = @"
using System;

class C
{
    static void F()
    {
        var x1 = new[] { new { A = new { X = 1 } } };
        var x2 = new[] { new { A = new { Y = 1 } } };
        var y = new Func<int>(() => x1[0].A.X + x2[0].A.Y);
    }
}
";
            var source1 = @"
using System;

class C
{
    static void F()
    {
        var x1 = new[] { new { A = new { X = 1 } } };
        var x2 = new[] { new { A = new { Z = 1 } } };
        var y = new Func<int>(() => x1[0].A.X + x2[0].A.Z);
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);

            var peRef0 = compilation0.EmitToImageReference();
            var peAssemblySymbol0 = (PEAssemblySymbol)CreateCompilation("", new[] { peRef0 }).GetReferencedAssemblySymbol(peRef0);
            var peModule0 = (PEModuleSymbol)peAssemblySymbol0.Modules[0];

            var reader0 = peModule0.Module.MetadataReader;
            var decoder0 = new MetadataDecoder(peModule0);

            var synthesizedTypes0 = PEDeltaAssemblyBuilder.GetSynthesizedTypesFromMetadata(reader0, decoder0);
            var anonymousTypeMap0 = synthesizedTypes0.AnonymousTypes;
            Assert.Equal("<>f__AnonymousType0", anonymousTypeMap0[new AnonymousTypeKey(ImmutableArray.Create(new AnonymousTypeKeyField("A", isKey: false, ignoreCase: false)))].Name);
            Assert.Equal("<>f__AnonymousType1", anonymousTypeMap0[new AnonymousTypeKey(ImmutableArray.Create(new AnonymousTypeKeyField("X", isKey: false, ignoreCase: false)))].Name);
            Assert.Equal("<>f__AnonymousType2", anonymousTypeMap0[new AnonymousTypeKey(ImmutableArray.Create(new AnonymousTypeKeyField("Y", isKey: false, ignoreCase: false)))].Name);
            Assert.Equal(3, anonymousTypeMap0.Count);

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll);

            var testData = new CompilationTestData();
            compilation1.EmitToArray(testData: testData);
            var peAssemblyBuilder = (PEAssemblyBuilder)testData.Module;

            var c = compilation1.GetMember<NamedTypeSymbol>("C");
            var displayClass = peAssemblyBuilder.GetSynthesizedTypes(c).Single();
            Assert.Equal("<>c__DisplayClass0_0", displayClass.Name);

            var emitContext = new EmitContext(peAssemblyBuilder, null, new DiagnosticBag(), metadataOnly: false, includePrivateMembers: true);

            var fields = displayClass.GetFields(emitContext).ToArray();
            AssertEx.SetEqual(fields.Select(f => f.Name), new[] { "x1", "x2" });
            var x1 = fields.Where(f => f.Name == "x1").Single();
            var x2 = fields.Where(f => f.Name == "x2").Single();

            var matcher = new CSharpSymbolMatcher(compilation1.SourceAssembly, peAssemblySymbol0, synthesizedTypes0);

            var mappedX1 = (Cci.IFieldDefinition)matcher.MapDefinition(x1);
            var mappedX2 = (Cci.IFieldDefinition)matcher.MapDefinition(x2);

            Assert.Equal("x1", mappedX1.Name);
            Assert.Null(mappedX2);
        }

        [Fact]
        public void TupleField_TypeChange()
        {
            var source0 = @"
class C
{  
    public (int a, int b) x;
}";
            var source1 = @"
class C
{
    public (int a, bool b) x;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<FieldSymbol>("C.x");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            // If a type changes within a tuple, we do not expect types to match.
            Assert.Null(other);
        }

        [Fact]
        public void TupleField_NameChange()
        {
            var source0 = @"
class C
{  
    public (int a, int b) x;
}";
            var source1 = @"
class C
{
    public (int a, int c) x;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<FieldSymbol>("C.x");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            // Types must match because just an element name was changed.
            Assert.NotNull(other);
        }

        [Fact]
        public void TupleMethod_TypeChange()
        {
            var source0 = @"
class C
{  
    public (int a, int b) X() { return null };
}";
            var source1 = @"
class C
{
    public (int a, bool b) X() { return null };
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<MethodSymbol>("C.X");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            // If a type changes within a tuple, we do not expect types to match.
            Assert.Null(other);
        }

        [Fact]
        public void TupleMethod_NameChange()
        {
            var source0 = @"
class C
{  
    public (int a, int b) X() { return null };
}";
            var source1 = @"
class C
{
    public (int a, int c) X() { return null };
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<MethodSymbol>("C.X");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            // Types must match because just an element name was changed.
            Assert.NotNull(other);
        }

        [Fact]
        public void TupleProperty_TypeChange()
        {
            var source0 = @"
class C
{  
    public (int a, int b) X { get { return null; } };
}";
            var source1 = @"
class C
{
    public (int a, bool b) X { get { return null; } };
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<PropertySymbol>("C.X");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            // If a type changes within a tuple, we do not expect types to match.
            Assert.Null(other);
        }

        [Fact]
        public void TupleProperty_NameChange()
        {
            var source0 = @"
class C
{  
    public (int a, int b) X { get { return null; } };
}";
            var source1 = @"
class C
{
    public (int a, int c) X { get { return null; } };
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<PropertySymbol>("C.X");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            // Types must match because just an element name was changed.
            Assert.NotNull(other);
        }

        [Fact]
        public void TupleStructField_TypeChange()
        {
            var source0 = @"
public struct Vector
{
    public (int x, int y) Coordinates;
}";
            var source1 = @"
public struct Vector
{
    public (int x, int y, int z) Coordinates;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<FieldSymbol>("Vector.Coordinates");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            // If a type changes within a tuple, we do not expect types to match.
            Assert.Null(other);
        }

        [Fact]
        public void TupleStructField_NameChange()
        {
            var source0 = @"
public struct Vector
{
    public (int x, int y) Coordinates;
}";
            var source1 = @"
public struct Vector
{
    public (int x, int z) Coordinates;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<FieldSymbol>("Vector.Coordinates");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            // Types must match because just an element name was changed.
            Assert.NotNull(other);
        }

        [Fact]
        public void TupleDelegate_TypeChange()
        {
            var source0 = @"
public class C
{
    public delegate (int, int) F();
}";
            var source1 = @"
public class C
{
    public delegate (int, bool) F();
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<SourceNamedTypeSymbol>("C.F");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            // Tuple delegate defines a type. We should be able to match old and new types by name.
            Assert.NotNull(other);
        }

        [Fact]
        public void TupleDelegate_NameChange()
        {
            var source0 = @"
public class C
{
    public delegate (int, int) F();
}";
            var source1 = @"
public class C
{
    public delegate (int x, int y) F();
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<SourceNamedTypeSymbol>("C.F");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            // Types must match because just an element name was changed.
            Assert.NotNull(other);
        }

        [Fact]
        public void RefReturn_Method()
        {
            var source0 = @"
struct C
{
    // non-matching
    public ref int P() => throw null;
    public ref readonly int Q() => throw null;
    public int R() => throw null;

    // matching
    public ref readonly int S() => throw null;
    public ref int T() => throw null;
}";
            var source1 = @"
struct C
{
    // non-matching
    public ref bool P() => throw null;
    public ref int Q() => throw null;
    public ref int R() => throw null;

    // matching
    public ref readonly int S() => throw null;
    public ref int T() => throw null;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var s0 = compilation0.GetMember<MethodSymbol>("C.S");
            var t0 = compilation0.GetMember<MethodSymbol>("C.T");
            var p1 = compilation1.GetMember<MethodSymbol>("C.P");
            var q1 = compilation1.GetMember<MethodSymbol>("C.Q");
            var r1 = compilation1.GetMember<MethodSymbol>("C.R");
            var s1 = compilation1.GetMember<MethodSymbol>("C.S");
            var t1 = compilation1.GetMember<MethodSymbol>("C.T");

            Assert.Null(matcher.MapDefinition(p1.GetCciAdapter()));
            Assert.Null(matcher.MapDefinition(q1.GetCciAdapter()));
            Assert.Null(matcher.MapDefinition(r1.GetCciAdapter()));

            Assert.Same(s0, matcher.MapDefinition(s1.GetCciAdapter()).GetInternalSymbol());
            Assert.Same(t0, matcher.MapDefinition(t1.GetCciAdapter()).GetInternalSymbol());
        }

        [Fact]
        public void RefReturn_Property()
        {
            var source0 = @"
struct C
{
    // non-matching
    public ref int P => throw null;
    public ref readonly int Q => throw null;
    public int R => throw null;

    // matching
    public ref readonly int S => throw null;
    public ref int T => throw null;
}";
            var source1 = @"
struct C
{
    // non-matching
    public ref bool P => throw null;
    public ref int Q => throw null;
    public ref int R => throw null;

    // matching
    public ref readonly int S => throw null;
    public ref int T => throw null;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var s0 = compilation0.GetMember<PropertySymbol>("C.S");
            var t0 = compilation0.GetMember<PropertySymbol>("C.T");
            var p1 = compilation1.GetMember<PropertySymbol>("C.P");
            var q1 = compilation1.GetMember<PropertySymbol>("C.Q");
            var r1 = compilation1.GetMember<PropertySymbol>("C.R");
            var s1 = compilation1.GetMember<PropertySymbol>("C.S");
            var t1 = compilation1.GetMember<PropertySymbol>("C.T");

            Assert.Null(matcher.MapDefinition(p1.GetCciAdapter()));
            Assert.Null(matcher.MapDefinition(q1.GetCciAdapter()));
            Assert.Null(matcher.MapDefinition(r1.GetCciAdapter()));

            Assert.Same(s0, matcher.MapDefinition(s1.GetCciAdapter()).GetInternalSymbol());
            Assert.Same(t0, matcher.MapDefinition(t1.GetCciAdapter()).GetInternalSymbol());
        }

        [Fact]
        public void Property_CompilationVsPE()
        {
            var source = @"
using System;

interface I<T, S>
{
	int this[int index] { set; }
}

class C : I<int, bool>
{
    int _current;
	int I<int, bool>.this[int anotherIndex] 
	{
		set { _current = anotherIndex + value; }
	}
}";

            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);

            var peRef0 = compilation0.EmitToImageReference();
            var peAssemblySymbol0 = (PEAssemblySymbol)CreateCompilation("", new[] { peRef0 }).GetReferencedAssemblySymbol(peRef0);

            var compilation1 = CreateCompilation(source, options: TestOptions.DebugDll);

            var testData = new CompilationTestData();
            compilation1.EmitToArray(testData: testData);

            var c = compilation1.GetMember<NamedTypeSymbol>("C");
            var property = c.GetMember<PropertySymbol>("I<System.Int32,System.Boolean>.this[]");
            var parameters = property.GetParameters().ToArray();
            Assert.Equal(1, parameters.Length);
            Assert.Equal("anotherIndex", parameters[0].Name);

            var matcher = CreateMatcher(compilation1, peAssemblySymbol0);

            var mappedProperty = (Cci.IPropertyDefinition)matcher.MapDefinition(property.GetCciAdapter());

            Assert.Equal("I<System.Int32,System.Boolean>.Item", ((PropertySymbol)mappedProperty.GetInternalSymbol()).MetadataName);
        }

        [Fact]
        public void Method_ParameterNullableChange()
        {
            var source0 = @"
using System.Collections.Generic;
class C
{
    string c;
    ref string M(string? s, (string a, dynamic? b) tuple, List<string?> list) => ref c;
}";
            var source1 = @"
using System.Collections.Generic;
class C
{
    string c;
    ref string? M(string s, (string? a, dynamic b) tuple, List<string> list) => ref c;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<MethodSymbol>("C.M");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            Assert.NotNull(other);
        }

        [Fact]
        public void Method_ParameterRename()
        {
            var source0 = @"
using System.Collections.Generic;
class C
{
    string M(string s) => s.ToString();
}";
            var source1 = @"
using System.Collections.Generic;
class C
{
     string M(string m) => m.ToString();
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<MethodSymbol>("C.M");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            Assert.NotNull(other);
        }

        [Fact]
        public void Method_ParameterRenameToDiscard()
        {
            var source0 = @"
using System.Collections.Generic;
class C
{
    string M(string s) => s.ToString();
}";
            var source1 = @"
using System.Collections.Generic;
class C
{
     string M(string _) => ""Hello"";
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<MethodSymbol>("C.M");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            Assert.NotNull(other);
        }

        [Fact]
        public void Field_NullableChange()
        {
            var source0 = @"
class C
{
    string S;
}";
            var source1 = @"
class C
{
    string? S;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<FieldSymbol>("C.S");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            Assert.NotNull(other);
        }

        [Fact]
        public void AnonymousTypesWithNullables()
        {
            var source0 = @"
using System;

class C
{
    static T id<T>(T t) => t;
    static T F<T>(Func<T> f) => f();

    static void M(string? x)
    {
        var y1 = new { A = id(x) };
        var y2 = F(() => new { B = id(x) });
        var z = new Func<string>(() => y1.A + y2.B);
    }
}";
            var source1 = @"
using System;

class C
{
    static T id<T>(T t) => t;
    static T F<T>(Func<T> f) => f();

    static void M(string? x)
    {
        if (x is null) throw new Exception();
        var y1 = new { A = id(x) };
        var y2 = F(() => new { B = id(x) });
        var z = new Func<string>(() => y1.A + y2.B);
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);

            var peRef0 = compilation0.EmitToImageReference();
            var peAssemblySymbol0 = (PEAssemblySymbol)CreateCompilation("", new[] { peRef0 }).GetReferencedAssemblySymbol(peRef0);
            var peModule0 = (PEModuleSymbol)peAssemblySymbol0.Modules[0];

            var reader0 = peModule0.Module.MetadataReader;
            var decoder0 = new MetadataDecoder(peModule0);

            var synthesizedTypes0 = PEDeltaAssemblyBuilder.GetSynthesizedTypesFromMetadata(reader0, decoder0);
            var anonymousTypeMap0 = synthesizedTypes0.AnonymousTypes;
            Assert.Equal("<>f__AnonymousType0", anonymousTypeMap0[new AnonymousTypeKey(ImmutableArray.Create(new AnonymousTypeKeyField("A", isKey: false, ignoreCase: false)))].Name);
            Assert.Equal("<>f__AnonymousType1", anonymousTypeMap0[new AnonymousTypeKey(ImmutableArray.Create(new AnonymousTypeKeyField("B", isKey: false, ignoreCase: false)))].Name);
            Assert.Equal(2, anonymousTypeMap0.Count);

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll);

            var testData = new CompilationTestData();
            compilation1.EmitToArray(testData: testData);
            var peAssemblyBuilder = (PEAssemblyBuilder)testData.Module;

            var c = compilation1.GetMember<NamedTypeSymbol>("C");
            var displayClass = peAssemblyBuilder.GetSynthesizedTypes(c).Single();
            Assert.Equal("<>c__DisplayClass2_0", displayClass.Name);

            var emitContext = new EmitContext(peAssemblyBuilder, null, new DiagnosticBag(), metadataOnly: false, includePrivateMembers: true);

            var fields = displayClass.GetFields(emitContext).ToArray();
            AssertEx.SetEqual(fields.Select(f => f.Name), new[] { "x", "y1", "y2" });
            var y1 = fields.Where(f => f.Name == "y1").Single();
            var y2 = fields.Where(f => f.Name == "y2").Single();

            var matcher = new CSharpSymbolMatcher(compilation1.SourceAssembly, peAssemblySymbol0, synthesizedTypes0);

            var mappedY1 = (Cci.IFieldDefinition)matcher.MapDefinition(y1);
            var mappedY2 = (Cci.IFieldDefinition)matcher.MapDefinition(y2);

            Assert.Equal("y1", mappedY1.Name);
            Assert.Equal("y2", mappedY2.Name);
        }

        [Fact]
        public void InterfaceMembers()
        {
            var source = @"
using System;

interface I
{
    static int X = 1;
    static event Action Y;

    static void M() { }
    void N() { }

    static int P { get => 1; set { } }
    int Q { get => 1; set { } }

    static event Action E { add { } remove { } }
    event Action F { add { } remove { } }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var matcher = CreateMatcher(compilation1, compilation0);

            var x0 = compilation0.GetMember<FieldSymbol>("I.X");
            var y0 = compilation0.GetMember<EventSymbol>("I.Y");
            var m0 = compilation0.GetMember<MethodSymbol>("I.M");
            var n0 = compilation0.GetMember<MethodSymbol>("I.N");
            var p0 = compilation0.GetMember<PropertySymbol>("I.P");
            var q0 = compilation0.GetMember<PropertySymbol>("I.Q");
            var e0 = compilation0.GetMember<EventSymbol>("I.E");
            var f0 = compilation0.GetMember<EventSymbol>("I.F");

            var x1 = compilation1.GetMember<FieldSymbol>("I.X");
            var y1 = compilation1.GetMember<EventSymbol>("I.Y");
            var m1 = compilation1.GetMember<MethodSymbol>("I.M");
            var n1 = compilation1.GetMember<MethodSymbol>("I.N");
            var p1 = compilation1.GetMember<PropertySymbol>("I.P");
            var q1 = compilation1.GetMember<PropertySymbol>("I.Q");
            var e1 = compilation1.GetMember<EventSymbol>("I.E");
            var f1 = compilation1.GetMember<EventSymbol>("I.F");

            Assert.Same(x0, matcher.MapDefinition(x1.GetCciAdapter()).GetInternalSymbol());
            Assert.Same(y0, matcher.MapDefinition(y1.GetCciAdapter()).GetInternalSymbol());
            Assert.Same(m0, matcher.MapDefinition(m1.GetCciAdapter()).GetInternalSymbol());
            Assert.Same(n0, matcher.MapDefinition(n1.GetCciAdapter()).GetInternalSymbol());
            Assert.Same(p0, matcher.MapDefinition(p1.GetCciAdapter()).GetInternalSymbol());
            Assert.Same(q0, matcher.MapDefinition(q1.GetCciAdapter()).GetInternalSymbol());
            Assert.Same(e0, matcher.MapDefinition(e1.GetCciAdapter()).GetInternalSymbol());
            Assert.Same(f0, matcher.MapDefinition(f1.GetCciAdapter()).GetInternalSymbol());
        }

        [Fact]
        public void FunctionPointerMembersTranslated()
        {
            var source = @"
unsafe class C
{
    delegate*<void> f1;
    delegate*<C, C, C> f2;
    delegate*<ref C> f3;
    delegate*<ref readonly C> f4;
    delegate*<ref C, void> f5;
    delegate*<in C, void> f6;
    delegate*<out C, void> f7;
}
";

            var compilation0 = CreateCompilation(source, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular9);
            var compilation1 = compilation0.WithSource(source);

            var matcher = CreateMatcher(compilation1, compilation0);

            for (int i = 1; i <= 7; i++)
            {
                var f_0 = compilation0.GetMember<FieldSymbol>($"C.f{i}");
                var f_1 = compilation1.GetMember<FieldSymbol>($"C.f{i}");

                Assert.Same(f_0, matcher.MapDefinition(f_1.GetCciAdapter()).GetInternalSymbol());
            }
        }

        [Theory]
        [InlineData("C", "void")]
        [InlineData("C", "object")]
        [InlineData("C", "ref C")]
        [InlineData("C", "ref readonly C")]
        [InlineData("ref C", "ref readonly C")]
        public void FunctionPointerMembers_ReturnMismatch(string return1, string return2)
        {
            var source1 = $@"
unsafe class C
{{
    delegate*<C, {return1}> f1;
}}";

            var source2 = $@"
unsafe class C
{{
    delegate*<C, {return2}> f1;
}}";

            var compilation0 = CreateCompilation(source1, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular9);
            var compilation1 = compilation0.WithSource(source2);

            var matcher = CreateMatcher(compilation1, compilation0);

            var f_1 = compilation1.GetMember<FieldSymbol>($"C.f1");

            Assert.Null(matcher.MapDefinition(f_1.GetCciAdapter()));
        }

        [Theory]
        [InlineData("C", "object")]
        [InlineData("C", "ref C")]
        [InlineData("C", "out C")]
        [InlineData("C", "in C")]
        [InlineData("ref C", "out C")]
        [InlineData("ref C", "in C")]
        [InlineData("out C", "in C")]
        [InlineData("C, C", "C")]
        public void FunctionPointerMembers_ParamMismatch(string param1, string param2)
        {
            var source1 = $@"
unsafe class C
{{
    delegate*<{param1}, C, void>* f1;
}}";

            var source2 = $@"
unsafe class C
{{
    delegate*<{param2}, C, void>* f1;
}}";

            verify(source1, source2);

            source1 = $@"
unsafe class C
{{
    delegate*<C, {param1}, void> f1;
}}";

            source2 = $@"
unsafe class C
{{
    delegate*<C, {param2}, void> f1;
}}";

            verify(source1, source2);

            static void verify(string source1, string source2)
            {
                var compilation0 = CreateCompilation(source1, options: TestOptions.UnsafeDebugDll, parseOptions: TestOptions.Regular9);
                var compilation1 = compilation0.WithSource(source2);

                var matcher = CreateMatcher(compilation1, compilation0);

                var f_1 = compilation1.GetMember<FieldSymbol>($"C.f1");

                Assert.Null(matcher.MapDefinition(f_1.GetCciAdapter()));
            }
        }

        [Fact]
        public void Record_ImplementSynthesizedMember_ToString()
        {
            var source0 = @"
public record R
{
}";
            var source1 = @"
public record R
{
    public override string ToString() => ""R"";
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member = compilation1.GetMember<SourceOrdinaryMethodSymbol>("R.ToString");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            Assert.NotNull(other);
        }

        [Fact]
        public void Record_ImplementSynthesizedMember_PrintMembers()
        {
            var source0 = @"
public record R
{
}";
            var source1 = @"
public record R
{
    protected virtual bool PrintMembers(System.Text.StringBuilder builder) => true;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member0 = compilation0.GetMember<SynthesizedRecordPrintMembers>("R.PrintMembers");
            var member1 = compilation1.GetMember<SourceOrdinaryMethodSymbol>("R.PrintMembers");

            Assert.Equal(member0, (MethodSymbol)matcher.MapDefinition(member1.GetCciAdapter()).GetInternalSymbol());
        }

        [Fact]
        public void Record_RemoveSynthesizedMember_PrintMembers()
        {
            var source0 = @"
public record R
{
    protected virtual bool PrintMembers(System.Text.StringBuilder builder) => true;
}";
            var source1 = @"
public record R
{
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member0 = compilation0.GetMember<SourceOrdinaryMethodSymbol>("R.PrintMembers");
            var member1 = compilation1.GetMember<SynthesizedRecordPrintMembers>("R.PrintMembers");

            Assert.Equal(member0, (MethodSymbol)matcher.MapDefinition(member1.GetCciAdapter()).GetInternalSymbol());
        }

        [Fact]
        public void Record_ImplementSynthesizedMember_Property()
        {
            var source0 = @"
public record R(int X);";
            var source1 = @"
public record R(int X)
{
    public int X { get; init; } = this.X;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var member0 = compilation0.GetMember<SynthesizedRecordPropertySymbol>("R.X");
            var member1 = compilation1.GetMember<SourcePropertySymbol>("R.X");

            Assert.Equal(member0, (PropertySymbol)matcher.MapDefinition(member1.GetCciAdapter()).GetInternalSymbol());
        }

        [Fact]
        public void Record_ImplementSynthesizedMember_Constructor()
        {
            var source0 = @"
public record R(int X);";
            var source1 = @"
public record R
{
    public R(int X)
    {
        this.X = X;
    }

    public int X { get; init; }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);

            var members = compilation1.GetMembers("R..ctor");
            // There are two, one is the copy constructor
            Assert.Equal(2, members.Length);

            var member = (SourceConstructorSymbol)members.Single(m => m.ToString() == "R.R(int)");
            var other = matcher.MapDefinition(member.GetCciAdapter());
            Assert.NotNull(other);
        }

        [Fact]
        public void SynthesizedDelegates_01()
        {
            var source0 = @"
using System;

class C
{
    static void F()
    {
        var _1 = (int a, ref int b) => a;
        var _2 = (in int a, int b) => { };

        var _3 = (int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8, int _9, int _10, int _11, int _12, int _13, int _14, int _15, int _16, int _17, int _18, int _19, int _20, int _21, int _22, int _23, int _24, int _25, int _26, int _27, int _28, int _29, int _30, int _31, int _32, ref int _33) => { };
    }
}
";
            var source1 = @"
using System;

class C
{
    static void F()
    {
        var _1 = (int c, ref int d) => c;
        var _2 = (in int c, int d) => { d.ToString(); };

        var _3 = (int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8, int _9, int _10, int _11, int _12, int _13, int _14, int _15, int _16, int _17, int _18, int _19, int _20, int _21, int _22, int _23, int _24, int _25, int _26, int _27, int _28, int _29, int _30, int _31, int _32, ref int _33) => { _1.ToString(); };
    }
}";

            var peAssemblySymbol0 = CreatePEAssemblySymbol(source0);
            var peModule0 = (PEModuleSymbol)peAssemblySymbol0.Modules[0];

            var reader0 = peModule0.Module.MetadataReader;
            var decoder0 = new MetadataDecoder(peModule0);

            var synthesizedTypes0 = PEDeltaAssemblyBuilder.GetSynthesizedTypesFromMetadata(reader0, decoder0);
            var synthesizedDelegates0 = synthesizedTypes0.AnonymousDelegates;
            Assert.Contains(new SynthesizedDelegateKey("<>F{00000008}`3"), synthesizedDelegates0.Keys);
            Assert.Contains(new SynthesizedDelegateKey("<>A{00000003}`2"), synthesizedDelegates0.Keys);
            Assert.Contains(new SynthesizedDelegateKey("<>A{00000000,100000000}`33"), synthesizedDelegates0.Keys);
            Assert.Equal(3, synthesizedDelegates0.Count);

            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll);

            var testData = new CompilationTestData();
            compilation1.EmitToArray(testData: testData);
            var peAssemblyBuilder = (PEAssemblyBuilder)testData.Module;

            var c = compilation1.GetMember<NamedTypeSymbol>("C");
            var displayClass = peAssemblyBuilder.GetSynthesizedTypes(c).Single();
            Assert.Equal("<>c", displayClass.Name);

            var emitContext = new EmitContext(peAssemblyBuilder, null, new DiagnosticBag(), metadataOnly: false, includePrivateMembers: true);

            var fields = displayClass.GetFields(emitContext).ToArray();
            var field1 = fields[1];
            var field2 = fields[2];
            var field3 = fields[3];
            Assert.Equal("<>9__0_0", field1.Name);
            Assert.Equal("<>9__0_1", field2.Name);
            Assert.Equal("<>9__0_2", field3.Name);

            var matcher = new CSharpSymbolMatcher(compilation1.SourceAssembly, peAssemblySymbol0, synthesizedTypes0);

            var mappedField1 = (Cci.IFieldDefinition)matcher.MapDefinition(field1);
            var mappedField2 = (Cci.IFieldDefinition)matcher.MapDefinition(field2);
            var mappedField3 = (Cci.IFieldDefinition)matcher.MapDefinition(field3);

            Assert.Equal("<>9__0_0", mappedField1.Name);
            Assert.Equal("<>9__0_1", mappedField2.Name);
            Assert.Equal("<>9__0_2", mappedField3.Name);
        }

        [Fact]
        public void SynthesizedDelegates_02()
        {
            var source0 =
@"unsafe class Program
{
    static void Main()
    {
        var d1 = (int* p) => *p;
    }
}";
            var source1 =
@"unsafe class Program
{
    static void Main()
    {
        var d1 = (int* p) => 1;
    }
}";

            var compilation0 = CreateCompilation(source0, options: TestOptions.UnsafeDebugDll);
            var compilation1 = CreateCompilation(source1, options: TestOptions.UnsafeDebugDll);

            var peRef0 = compilation0.EmitToImageReference();
            var peAssemblySymbol0 = (PEAssemblySymbol)CreateCompilation("", new[] { peRef0 }).GetReferencedAssemblySymbol(peRef0);
            var peModule0 = (PEModuleSymbol)peAssemblySymbol0.Modules[0];

            var reader0 = peModule0.Module.MetadataReader;
            var decoder0 = new MetadataDecoder(peModule0);

            var synthesizedTypes0 = PEDeltaAssemblyBuilder.GetSynthesizedTypesFromMetadata(reader0, decoder0);
            var anonymousDelegates0 = synthesizedTypes0.AnonymousDelegatesWithIndexedNames;
            Assert.Equal("<>f__AnonymousDelegate0", anonymousDelegates0.Single().Value.Single().Name);
            Assert.Equal(1, anonymousDelegates0.Count);

            var testData = new CompilationTestData();
            compilation1.EmitToArray(testData: testData);
            var peAssemblyBuilder = (PEAssemblyBuilder)testData.Module;

            var type = compilation1.GetMember<NamedTypeSymbol>("Program");
            var displayClass = peAssemblyBuilder.GetSynthesizedTypes(type).Single();
            Assert.Equal("<>c", displayClass.Name);

            var emitContext = new EmitContext(peAssemblyBuilder, null, new DiagnosticBag(), metadataOnly: false, includePrivateMembers: true);
            var field0 = displayClass.GetFields(emitContext).Single(f => f.Name == "<>9__0_0");
            Assert.Equal("<>f__AnonymousDelegate0", field0.GetType(emitContext).ToString());

            var matcher = new CSharpSymbolMatcher(compilation1.SourceAssembly, peAssemblySymbol0, synthesizedTypes0);
            var field1 = (Cci.IFieldDefinition)matcher.MapDefinition(field0);
            Assert.Equal("<>9__0_0", field1.Name);
        }

        [Fact]
        public void SynthesizedDelegates_03()
        {
            // This test checks that lambdas between source0 and source1 are not the same,
            // as they differ in default parameter value or `params` modifier.
            var source0 = """
                class C
                {
                    unsafe static void F()
                    {
                        var _1 = (int a, int b = 1) => a;
                        var _2 = (int* x, int[] c) => c;
                        var _3 = (int* x, int b) => b;
                    }
                }
                """;
            var source1 = """
                class C
                {
                    unsafe static void F()
                    {
                        var _1 = (int a, int b = 2) => a;
                        var _2 = (int* x, params int[] c) => c;
                        var _3 = (int* x, int b = 1) => b;
                    }
                }
                """;

            var compilation0 = CreateCompilation(source0, options: TestOptions.UnsafeDebugDll);
            var compilation1 = CreateCompilation(source1, options: TestOptions.UnsafeDebugDll);

            var peRef0 = compilation0.EmitToImageReference();
            var peAssemblySymbol0 = (PEAssemblySymbol)CreateCompilation("", new[] { peRef0 }).GetReferencedAssemblySymbol(peRef0);
            var peModule0 = (PEModuleSymbol)peAssemblySymbol0.Modules[0];

            var reader0 = peModule0.Module.MetadataReader;
            var decoder0 = new MetadataDecoder(peModule0);

            var synthesizedTypes0 = PEDeltaAssemblyBuilder.GetSynthesizedTypesFromMetadata(reader0, decoder0);

            AssertEx.SetEqual(new[]
            {
                "(3,2): <>f__AnonymousDelegate0<T1, T2, TResult>",
                "(0,2): <>f__AnonymousDelegate1",
                "(0,2): <>f__AnonymousDelegate2"
            }, Inspect(synthesizedTypes0.AnonymousDelegatesWithIndexedNames));

            var testData = new CompilationTestData();
            compilation1.EmitToArray(testData: testData);
            var peAssemblyBuilder = (PEAssemblyBuilder)testData.Module;

            var c = compilation1.GetMember<NamedTypeSymbol>("C");
            var displayClass = peAssemblyBuilder.GetSynthesizedTypes(c).Single();
            Assert.Equal("<>c", displayClass.Name);

            var emitContext = new EmitContext(peAssemblyBuilder, null, new DiagnosticBag(), metadataOnly: false, includePrivateMembers: true);

            var fields = displayClass.GetFields(emitContext).ToArray();
            var field1 = fields[1];
            var field2 = fields[2];
            var field3 = fields[3];
            Assert.Equal("<>9__0_0", field1.Name);
            Assert.Equal("<>9__0_1", field2.Name);
            Assert.Equal("<>9__0_2", field3.Name);

            var matcher = new CSharpSymbolMatcher(compilation1.SourceAssembly, peAssemblySymbol0, synthesizedTypes0);

            Assert.Null(matcher.MapDefinition(field1));
            Assert.Null(matcher.MapDefinition(field2));
            Assert.Null(matcher.MapDefinition(field3));
        }

        [Fact]
        public void SynthesizedDelegates_04()
        {
            var source0 = """
                class C
                {
                    unsafe static void F()
                    {
                        var _1 = (int a, int b = 1) => a;
                        var _2 = (int* x, params int[] c) => c;
                        var _3 = (int* x, int b) => b;
                    }
                }
                """;
            var source1 = """
                class C
                {
                    unsafe static void F()
                    {
                        var _1 = (int m, int n = 1) => m;
                        var _2 = (int* y, params int[] p) => p;
                        var _3 = (int* y, int c) => c;
                    }
                }
                """;

            var compilation0 = CreateCompilation(source0, options: TestOptions.UnsafeDebugDll);
            var compilation1 = CreateCompilation(source1, options: TestOptions.UnsafeDebugDll);

            var peRef0 = compilation0.EmitToImageReference();
            var peAssemblySymbol0 = (PEAssemblySymbol)CreateCompilation("", new[] { peRef0 }).GetReferencedAssemblySymbol(peRef0);
            var peModule0 = (PEModuleSymbol)peAssemblySymbol0.Modules[0];

            var reader0 = peModule0.Module.MetadataReader;
            var decoder0 = new MetadataDecoder(peModule0);

            var synthesizedTypes0 = PEDeltaAssemblyBuilder.GetSynthesizedTypesFromMetadata(reader0, decoder0);

            AssertEx.SetEqual(new[]
            {
                "(3,2): <>f__AnonymousDelegate0<T1, T2, TResult>",
                "(0,2): <>f__AnonymousDelegate1",
                "(0,2): <>f__AnonymousDelegate2"
            }, Inspect(synthesizedTypes0.AnonymousDelegatesWithIndexedNames));

            var testData = new CompilationTestData();
            compilation1.EmitToArray(testData: testData);
            var peAssemblyBuilder = (PEAssemblyBuilder)testData.Module;

            var c = compilation1.GetMember<NamedTypeSymbol>("C");
            var displayClass = peAssemblyBuilder.GetSynthesizedTypes(c).Single();
            Assert.Equal("<>c", displayClass.Name);

            var emitContext = new EmitContext(peAssemblyBuilder, null, new DiagnosticBag(), metadataOnly: false, includePrivateMembers: true);

            var fields = displayClass.GetFields(emitContext).ToArray();
            var field1 = fields[1];
            var field2 = fields[2];
            var field3 = fields[3];
            Assert.Equal("<>9__0_0", field1.Name);
            Assert.Equal("<>9__0_1", field2.Name);
            Assert.Equal("<>9__0_2", field3.Name);

            var matcher = new CSharpSymbolMatcher(compilation1.SourceAssembly, peAssemblySymbol0, synthesizedTypes0);

            var mappedField1 = (Cci.IFieldDefinition)matcher.MapDefinition(field1);
            var mappedField2 = (Cci.IFieldDefinition)matcher.MapDefinition(field2);
            var mappedField3 = (Cci.IFieldDefinition)matcher.MapDefinition(field3);

            Assert.Equal("<>9__0_0", mappedField1.Name);
            Assert.Equal("<>9__0_1", mappedField2.Name);
            Assert.Equal("<>9__0_2", mappedField3.Name);
        }

        [Fact]
        public void CheckedUserDefinedOperators_01()
        {
            const string source =
@"
class A 
{
    public static A operator -(A c) => c;
    public static A operator checked -(A c) => c;

    public static A operator +(A c, A d) => c;
    public static A operator checked +(A c, A d) => c;

    public static explicit operator int(A c) => 0;
    public static explicit operator checked int(A c) => 0;
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var matcher = CreateMatcher(compilation1, compilation0);
            var members1 = compilation1.GetMember<NamedTypeSymbol>("A").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind is (MethodKind.Conversion or MethodKind.UserDefinedOperator)).ToArray();
            var members0 = compilation0.GetMember<NamedTypeSymbol>("A").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind is (MethodKind.Conversion or MethodKind.UserDefinedOperator)).ToArray();
            Assert.Equal(6, members1.Length);
            Assert.Equal(members1.Length, members0.Length);

            for (int i = 0; i < members0.Length; i++)
            {
                Assert.Same(members0[i], matcher.MapDefinition(members1[i].GetCciAdapter()).GetInternalSymbol());
            }
        }

        [Fact]
        public void CheckedUserDefinedOperators_02()
        {
            const string source0 =
@"
class A 
{
    public static A operator -(A c) => c;
    public static A operator +(A c, A d) => c;
    public static explicit operator int(A c) => 0;
}
";

            const string source1 =
@"
class A 
{
    public static A operator -(A c) => c;
    public static A operator checked -(A c) => c;

    public static A operator +(A c, A d) => c;
    public static A operator checked +(A c, A d) => c;

    public static explicit operator int(A c) => 0;
    public static explicit operator checked int(A c) => 0;
}
";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);
            var members1 = compilation1.GetMember<NamedTypeSymbol>("A").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind is (MethodKind.Conversion or MethodKind.UserDefinedOperator)).ToArray();
            var members0 = compilation0.GetMember<NamedTypeSymbol>("A").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind is (MethodKind.Conversion or MethodKind.UserDefinedOperator)).ToArray();
            Assert.Equal(6, members1.Length);
            Assert.Equal(3, members0.Length);

            for (int i = 0; i < members0.Length; i++)
            {
                Assert.Same(members0[i], matcher.MapDefinition(members1[i * 2].GetCciAdapter()).GetInternalSymbol());
            }

            for (int i = 0; i < members1.Length; i++)
            {
                if (i % 2 == 0)
                {
                    continue;
                }

                Assert.Null(matcher.MapDefinition(members1[i].GetCciAdapter()));
            }
        }

        [Fact]
        public void CheckedUserDefinedOperators_03()
        {
            const string source0 =
@"
class A 
{
    public static A operator -(A c) => c;
    public static A operator checked -(A c) => c;

    public static A operator +(A c, A d) => c;
    public static A operator checked +(A c, A d) => c;

    public static explicit operator int(A c) => 0;
    public static explicit operator checked int(A c) => 0;
}
";

            const string source1 =
@"
class A 
{
    public static A operator -(A c) => c;
    public static A operator +(A c, A d) => c;
    public static explicit operator int(A c) => 0;
}
";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);
            var members1 = compilation1.GetMember<NamedTypeSymbol>("A").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind is (MethodKind.Conversion or MethodKind.UserDefinedOperator)).ToArray();
            var members0 = compilation0.GetMember<NamedTypeSymbol>("A").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind is (MethodKind.Conversion or MethodKind.UserDefinedOperator)).ToArray();
            Assert.Equal(3, members1.Length);
            Assert.Equal(6, members0.Length);

            for (int i = 0; i < members1.Length; i++)
            {
                Assert.Same(members0[i * 2], matcher.MapDefinition(members1[i].GetCciAdapter()).GetInternalSymbol());
            }
        }

        [Fact]
        public void CheckedUserDefinedOperators_04()
        {
            const string source0 =
@"
class A 
{
    public static A operator -(A c) => default;
    public static A operator checked -(A c) => default;

    public static A operator +(A c, A d) => default;
    public static A operator checked +(A c, A d) => default;

    public static explicit operator int(A c) => default;
    public static explicit operator checked int(A c) => default;
}
";

            const string source1 =
@"
class A 
{
    public static int operator -(A c) => default;
    public static int operator checked -(A c) => default;

    public static int operator +(A c, A d) => default;
    public static int operator checked +(A c, A d) => default;

    public static explicit operator long(A c) => default;
    public static explicit operator checked long(A c) => default;
}
";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);
            var members1 = compilation1.GetMember<NamedTypeSymbol>("A").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind is (MethodKind.Conversion or MethodKind.UserDefinedOperator)).ToArray();
            Assert.Equal(6, members1.Length);

            foreach (var member in members1)
            {
                Assert.Null(matcher.MapDefinition(member.GetCciAdapter()));
            }
        }

        [Fact]
        public void CheckedUserDefinedOperators_05()
        {
            const string source0 =
@"
class A 
{
    public static A operator -(A c) => default;
    public static A operator checked -(A c) => default;

    public static A operator +(A c, A d) => default;
    public static A operator checked +(A c, A d) => default;

    public static explicit operator int(A c) => default;
    public static explicit operator checked int(A c) => default;
}
";

            const string source1 =
@"
class A 
{
    public static A operator -(int c) => default;
    public static A operator checked -(int c) => default;

    public static A operator +(A c, int d) => default;
    public static A operator checked +(A c, int d) => default;

    public static explicit operator int(int c) => default;
    public static explicit operator checked int(int c) => default;
}
";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = CreateMatcher(compilation1, compilation0);
            var members1 = compilation1.GetMember<NamedTypeSymbol>("A").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind is (MethodKind.Conversion or MethodKind.UserDefinedOperator)).ToArray();
            Assert.Equal(6, members1.Length);

            foreach (var member in members1)
            {
                Assert.Null(matcher.MapDefinition(member.GetCciAdapter()));
            }
        }
    }
}
