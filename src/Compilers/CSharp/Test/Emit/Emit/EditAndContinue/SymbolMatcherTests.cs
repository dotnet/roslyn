// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class SymbolMatcherTests : EditAndContinueTestBase
    {
        private static void MatchAll(CSharpSymbolMatcher matcher, ImmutableArray<Symbol> members, int startAt)
        {
            int n = members.Length;
            for (int i = 0; i < n; i++)
            {
                var member = members[(i + startAt) % n];
                var other = matcher.MapDefinition((Cci.IDefinition)member);
                Assert.NotNull(other);
            }
        }

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

            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
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
                var matcher = new CSharpSymbolMatcher(
                    null,
                    compilation1.SourceAssembly,
                    default(EmitContext),
                    compilation0.SourceAssembly,
                    default(EmitContext),
                    null);

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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var matcher = new CSharpSymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(EmitContext),
                compilation0.SourceAssembly,
                default(EmitContext),
                null);
            var members = compilation1.GetMember<NamedTypeSymbol>("A.B").GetMembers("M");
            Assert.Equal(members.Length, 2);
            foreach (var member in members)
            {
                var other = matcher.MapDefinition((Cci.IMethodDefinition)member);
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
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var matcher = new CSharpSymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(EmitContext),
                compilation0.SourceAssembly,
                default(EmitContext),
                null);
            var member = compilation1.GetMember<MethodSymbol>("C.M");
            var other = matcher.MapDefinition((Cci.IMethodDefinition)member);
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
            const string source =
@"unsafe class B : A
{
    public override object[] F(int* p) { return null; }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, references: new[] { metadataRef });
            var compilation1 = compilation0.WithSource(source);

            var member1 = compilation1.GetMember<MethodSymbol>("B.F");
            Assert.Equal(((PointerTypeSymbol)member1.Parameters[0].Type).CustomModifiers.Length, 1);
            Assert.Equal(((ArrayTypeSymbol)member1.ReturnType).CustomModifiers.Length, 1);

            var matcher = new CSharpSymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(EmitContext),
                compilation0.SourceAssembly,
                default(EmitContext),
                null);
            var other = (MethodSymbol)matcher.MapDefinition((Cci.IMethodDefinition)member1);
            Assert.NotNull(other);
            Assert.Equal(((PointerTypeSymbol)other.Parameters[0].Type).CustomModifiers.Length, 1);
            Assert.Equal(((ArrayTypeSymbol)other.ReturnType).CustomModifiers.Length, 1);
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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = new CSharpSymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(EmitContext),
                compilation0.SourceAssembly,
                default(EmitContext),
                null);
            var elementType = compilation1.GetMember<TypeSymbol>("C.D");
            var member = compilation1.CreateArrayTypeSymbol(elementType);
            var other = matcher.MapReference((Cci.ITypeReference)member);
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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = new CSharpSymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(EmitContext),
                compilation0.SourceAssembly,
                default(EmitContext),
                null);
            var elementType = compilation1.GetMember<TypeSymbol>("C.D");
            var member = compilation1.CreateArrayTypeSymbol(elementType);
            var other = matcher.MapReference((Cci.ITypeReference)member);
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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = new CSharpSymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(EmitContext),
                compilation0.SourceAssembly,
                default(EmitContext),
                null);
            var elementType = compilation1.GetMember<TypeSymbol>("C.D");
            var member = compilation1.CreatePointerTypeSymbol(elementType);
            var other = matcher.MapReference((Cci.ITypeReference)member);
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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var matcher = new CSharpSymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(EmitContext),
                compilation0.SourceAssembly,
                default(EmitContext),
                null);
            var member = compilation1.GetMember<FieldSymbol>("C.y");
            var other = matcher.MapReference((Cci.ITypeReference)member.Type);
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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);

            var peRef0 = compilation0.EmitToImageReference();
            var peAssemblySymbol0 = (PEAssemblySymbol)CreateCompilationWithMscorlib("", new[] { peRef0 }).GetReferencedAssemblySymbol(peRef0);
            var peModule0 = (PEModuleSymbol)peAssemblySymbol0.Modules[0];

            var reader0 = peModule0.Module.MetadataReader;
            var decoder0 = new MetadataDecoder(peModule0);

            var anonymousTypeMap0 = PEDeltaAssemblyBuilder.GetAnonymousTypeMapFromMetadata(reader0, decoder0);
            Assert.Equal("<>f__AnonymousType0", anonymousTypeMap0[new AnonymousTypeKey(ImmutableArray.Create(new AnonymousTypeKeyField("A", isKey: false, ignoreCase: false)))].Name);
            Assert.Equal("<>f__AnonymousType1", anonymousTypeMap0[new AnonymousTypeKey(ImmutableArray.Create(new AnonymousTypeKeyField("B", isKey: false, ignoreCase: false)))].Name);
            Assert.Equal(2, anonymousTypeMap0.Count);

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);

            var testData = new CompilationTestData();
            compilation1.EmitToArray(testData: testData);
            var peAssemblyBuilder = (PEAssemblyBuilder)testData.Module;

            var c = compilation1.GetMember<NamedTypeSymbol>("C");
            var displayClass = peAssemblyBuilder.GetSynthesizedTypes(c).Single();
            Assert.Equal("<>c__DisplayClass0_0", displayClass.Name);

            var emitContext = new EmitContext(peAssemblyBuilder, null, new DiagnosticBag());

            var fields = displayClass.GetFields(emitContext).ToArray();
            var x1 = fields[0];
            var x2 = fields[1];
            Assert.Equal("x1", x1.Name);
            Assert.Equal("x2", x2.Name);

            var matcher = new CSharpSymbolMatcher(anonymousTypeMap0, compilation1.SourceAssembly, emitContext, peAssemblySymbol0);

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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);

            var peRef0 = compilation0.EmitToImageReference();
            var peAssemblySymbol0 = (PEAssemblySymbol)CreateCompilationWithMscorlib("", new[] { peRef0 }).GetReferencedAssemblySymbol(peRef0);
            var peModule0 = (PEModuleSymbol)peAssemblySymbol0.Modules[0];

            var reader0 = peModule0.Module.MetadataReader;
            var decoder0 = new MetadataDecoder(peModule0);

            var anonymousTypeMap0 = PEDeltaAssemblyBuilder.GetAnonymousTypeMapFromMetadata(reader0, decoder0);
            Assert.Equal("<>f__AnonymousType0", anonymousTypeMap0[new AnonymousTypeKey(ImmutableArray.Create(new AnonymousTypeKeyField("A", isKey: false, ignoreCase: false)))].Name);
            Assert.Equal("<>f__AnonymousType1", anonymousTypeMap0[new AnonymousTypeKey(ImmutableArray.Create(new AnonymousTypeKeyField("X", isKey: false, ignoreCase: false)))].Name);
            Assert.Equal("<>f__AnonymousType2", anonymousTypeMap0[new AnonymousTypeKey(ImmutableArray.Create(new AnonymousTypeKeyField("Y", isKey: false, ignoreCase: false)))].Name);
            Assert.Equal(3, anonymousTypeMap0.Count);

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);

            var testData = new CompilationTestData();
            compilation1.EmitToArray(testData: testData);
            var peAssemblyBuilder = (PEAssemblyBuilder)testData.Module;

            var c = compilation1.GetMember<NamedTypeSymbol>("C");
            var displayClass = peAssemblyBuilder.GetSynthesizedTypes(c).Single();
            Assert.Equal("<>c__DisplayClass0_0", displayClass.Name);

            var emitContext = new EmitContext(peAssemblyBuilder, null, new DiagnosticBag());

            var fields = displayClass.GetFields(emitContext).ToArray();
            var x1 = fields[0];
            var x2 = fields[1];
            Assert.Equal("x1", x1.Name);
            Assert.Equal("x2", x2.Name);

            var matcher = new CSharpSymbolMatcher(anonymousTypeMap0, compilation1.SourceAssembly, emitContext, peAssemblySymbol0);

            var mappedX1 = (Cci.IFieldDefinition)matcher.MapDefinition(x1);
            var mappedX2 = (Cci.IFieldDefinition)matcher.MapDefinition(x2);

            Assert.Equal("x1", mappedX1.Name);
            Assert.Null(mappedX2);
        }
    }
}
