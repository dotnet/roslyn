﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class SymbolKeyTests : TestBase
    {
        [Fact]
        public void TestMemberDeclarations()
        {
            var source = @"

public class C
{
    public class B { };
    public delegate int D(int v);

    public int F;
    public B F2;
    public int P { get; set;}
    public B P2 { get; set; }
    public void M() { };
    public void M(int a) { };
    public void M(int a, string b) { };
    public void M(string a, int b) { };
    public void M(B b) { };
    public int M2() { return 0; }
    public int M2(int a) { return 0; }
    public int M2(int a, string b) { return 0; }
    public int M2(string a, int b) { return 0; }
    public B M3() { return default(B); }
    public int this[int index] { get { return 0; } }
    public int this[int a, int b] { get { return 0; } }
    public B this[B b] { get { return b; } }
    public event D E;
    public event D E2 { add; remove; }
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            TestRoundTrip(GetDeclaredSymbols(compilation), compilation);
        }

        [Fact]
        [WorkItem(14364, "https://github.com/dotnet/roslyn/issues/14364")]
        public void TestVBParameterizedEvent()
        {
            var source = @"
Module M
    Event E(x As Object)
End Module
";
            var compilation = GetCompilation(source, LanguageNames.VisualBasic);
            TestRoundTrip(GetAllSymbols(compilation.GetSemanticModel(compilation.SyntaxTrees.Single())), compilation);
        }

        [Fact]
        public void TestNamespaceDeclarations()
        {
            var source = @"
namespace N { }
namespace A.B { }
namespace A { namespace B.C { } }
namespace A { namespace B { namespace C { } } }
namespace A { namespace N { } }
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var symbols = GetDeclaredSymbols(compilation);
            Assert.Equal(5, symbols.Count);
            TestRoundTrip(symbols, compilation);
        }

        [Fact]
        public void TestConstructedTypeReferences()
        {
            var source = @"
using System.Collections.Generic;

public class C
{
    public List<int> G1;
    public List<List<int>> G2;
    public Dictionary<string, int> G3;
    public int[] A1;
    public int[,] A2;
    public int[,,] A3;
    public List<int>[] A4;
    public int* P1;
    public int** p2;
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            TestRoundTrip(GetDeclaredSymbols(compilation).OfType<IFieldSymbol>().Select(fs => fs.Type), compilation);
        }

        [Fact]
        public void TestErrorTypeReferences()
        {
            var source = @"
using System.Collections.Generic;

public class C
{
    public T E1;
    public List<T> E2;
    public T<int> E3;
    public T<A> E4;
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            TestRoundTrip(GetDeclaredSymbols(compilation).OfType<IFieldSymbol>().Select(fs => fs.Type), compilation, s => s.ToDisplayString());
        }

        [Fact]
        public void TestParameterDeclarations()
        {
            var source = @"
using System.Collections.Generic;

public class C
{
    public void M(int p) { }
    public void M(int p1, int p2) { }
    public void M<T>(T p) { }
    public void M<T>(T[] p) { }
    public void M<T>(List<T> p) { }
    public void M<T>(T* p) { }
    public void M(ref int p)  { }
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            TestRoundTrip(GetDeclaredSymbols(compilation).OfType<IMethodSymbol>().SelectMany(ms => ms.Parameters), compilation);
        }

        [Fact]
        public void TestTypeParameters()
        {
            var source = @"
public class C
{
    public void M() { }
    public void M<A>(A a) { }
    public void M<A>(int i) { }
    public void M<A, B>(A a, B b) { }
    public void M<A, B>(A a, int i) { }
    public void M<A, B>(int i, B b) { }
    public void M<A, B>(B b, A a) { }
    public void M<A, B>(B b, int i) { }
    public void M<A, B>(int i, A a) { }
    public void M<A, B>(int i, int j) { }
    public void M(C c) { }
    public int GetInt() { return 0 ; }
    public A GetA<A>(A a) { return a; }
    public A GetA<A, B>(A a, B b) { return a; }
    public B GetB<A, B>(A a, B b) { return b; }
    publi C GetC() { return default(C); }
}

public class C<T>
{
    public void M() { }
    public void M(T t) { }
    public void M<A>(A a) { }
    public void M<A>(T t, A a) { }
    public void M<A, B>(A a, B b) { }
    public void M<A, B>(B b, A a) { }
    public void M(C<T> c) { }
    public void M(C<int> c) { }
    public T GetT() { return default(T); }
    public C<T> GetCT() { return default(C<T>); }
    public C<int> GetCInt() { return default(C<int>); }
    public C<A> GetCA<A>() { return default(C<A>); }
}

public class C<S, T>
{
    public void M() { }
    public void M(T t, S s) { }
    public void M<A>(A a) { }
    public void M<A>(T t, S s, A a) { }
    public void M<A>(A a, T t, S s) { }
    public void M<A, B>(A a, B b) { }
    public void M<A, B>(T t, S s, A a, B b) { }
    public void M<A, B>(A a, B b, T t, S s) { }
    public T GetT() { return default(T); } 
    public S GetS() { return default(S); }
    public C<S, T> GetCST() { return default(C<S,T>); }
    public C<T, S> GetCTS() { return default(C<T, S>); }
    public C<T, A> GetCTA<A>() { return default(C<T, A>); }
}
";

            var compilation = GetCompilation(source, LanguageNames.CSharp);
            TestRoundTrip(GetDeclaredSymbols(compilation), compilation);
        }

        [Fact]
        public void TestLocals()
        {
            var source = @"
using System.Collections.Generic;

public class C
{
    public void M() {
        int a, b;
        if (a > b) {
           int c = a + b;
        }

        {
            string d = "";
        }

        {
            double d = 0.0;
        }

        {
            bool d = false;
        }

        var q = new { };
    }
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var symbols = GetDeclaredSymbols(compilation).OfType<IMethodSymbol>().SelectMany(ms => GetInteriorSymbols(ms, compilation).OfType<ILocalSymbol>()).ToList();
            Assert.Equal(7, symbols.Count);
            TestRoundTrip(symbols, compilation);
        }

        [Fact]
        public void TestLabels()
        {
            var source = @"
using System.Collections.Generic;

public class C
{
    public void M() {
        start: goto end;
        end: goto start;
        end: ; // duplicate label
        }
    }
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var symbols = GetDeclaredSymbols(compilation).OfType<IMethodSymbol>().SelectMany(ms => GetInteriorSymbols(ms, compilation).OfType<ILabelSymbol>()).ToList();
            Assert.Equal(3, symbols.Count);
            TestRoundTrip(symbols, compilation);
        }

        [Fact]
        public void TestRangeVariables()
        {
            var source = @"
using System.Collections.Generic;

public class C
{
    public void M() {
        int[] xs = new int[] { 1, 2, 3, 4 };
        
        {
            var q = from x in xs where x > 2 select x;
        }

        {
            var q2 = from x in xs where x < 4 select x;
        }
    }
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var symbols = GetDeclaredSymbols(compilation).OfType<IMethodSymbol>().SelectMany(ms => GetInteriorSymbols(ms, compilation).OfType<IRangeVariableSymbol>()).ToList();
            Assert.Equal(2, symbols.Count);
            TestRoundTrip(symbols, compilation);
        }

        [Fact]
        public void TestMethodReferences()
        {
            var source = @"
public class C
{
    public void M() { }
    public void M(int x) { }
    public void M2<T>() { }
    public void M2<T>(T t) { }
    public T M3<T>(T t) { return default(T); }
   
    public void Test() {
        M():
        M(0);
        M2<string>();
        M2(0);
        var tmp = M3(0);
    }
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var symbols = tree.GetRoot().DescendantNodes().OfType<CSharp.Syntax.InvocationExpressionSyntax>().Select(s => model.GetSymbolInfo(s).Symbol).ToList();
            Assert.True(symbols.Count > 0);
            TestRoundTrip(symbols, compilation);
        }
        [Fact]
        public void TestExtensionMethodReferences()
        {
            var source = @"
using System;
using System.Collections.Generic;

public static class E 
{
    public static void Z(this C c) { }
    public static void Z(this C c, int x) { }
    public static void Z<T>(this T t, string y) { }
    public static void Z<T>(this T t, T t2) { }
    public static void Y<T, S>(this T t, S other) { }
    public static TResult Select<TSource, TResult>(this IEnumerable<TSource> collection, Func<TSource, TResult> selector) { return null;}
}

public class C
{
    public void M() {
        this.Z();
        this.Z(1);
        this.Z(""test"");
        this.Z(this);
        this.Y(1.0);
        new[] { 1, 2, 3 }.Select(
    }
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var symbols = tree.GetRoot().DescendantNodes().OfType<CSharp.Syntax.InvocationExpressionSyntax>().Select(s => model.GetSymbolInfo(s).GetAnySymbol()).ToList();
            Assert.True(symbols.Count > 0);
            Assert.True(symbols.All(s => s.IsReducedExtension()));
            TestRoundTrip(symbols, compilation);
        }

        [Fact]
        public void TestAliasSymbols()
        {
            var source = @"
using G=System.Collections.Generic;
using GL=System.Collections.Generic.List<int>;

public class C
{
    public G.List<int> F;
    public GL F2;
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var symbols = tree.GetRoot().DescendantNodes().OfType<CSharp.Syntax.UsingDirectiveSyntax>().Select(s => model.GetDeclaredSymbol(s)).ToList();
            Assert.Equal(2, symbols.Count);
            Assert.NotNull(symbols[0]);
            Assert.True(symbols[0] is IAliasSymbol);
            TestRoundTrip(symbols, compilation);

            var refSymbols = GetDeclaredSymbols(compilation).OfType<IFieldSymbol>().Select(f => f.Type).ToList();
            Assert.Equal(2, refSymbols.Count);
            TestRoundTrip(refSymbols, compilation);
        }

        [Fact]
        public void TestDynamicSymbols()
        {
            var source = @"
public class C
{
    public dynamic F;
    public dynamic[] F2;
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var symbols = GetDeclaredSymbols(compilation).OfType<IFieldSymbol>().Select(f => f.Type).ToList();
            Assert.Equal(2, symbols.Count);
            TestRoundTrip(symbols, compilation);
        }

        [Fact]
        public void TestSelfReferentialGenericMethod()
        {
            var source = @"
public class C
{
    public void M<S, T>() { }
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var method = GetDeclaredSymbols(compilation).OfType<IMethodSymbol>().First();
            var constructed = method.Construct(compilation.GetSpecialType(SpecialType.System_Int32), method.TypeParameters[1]);

            TestRoundTrip(constructed, compilation);
        }

        [Fact]
        public void TestSelfReferentialGenericType()
        {
            var source = @"
public class C<S, T>
{
}
";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var type = GetDeclaredSymbols(compilation).OfType<INamedTypeSymbol>().First();
            var constructed = type.Construct(compilation.GetSpecialType(SpecialType.System_Int32), type.TypeParameters[1]);

            TestRoundTrip(constructed, compilation);
        }

        [Fact, WorkItem(235912, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=235912&_a=edit")]
        public void TestNestedGenericType()
        {
            var source = @"
public class A<TOuter>
{
    public class B<TInner>
    {
    }
}";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var outer = GetDeclaredSymbols(compilation).OfType<INamedTypeSymbol>().First(s => s.Name == "A");
            var constructed = outer.Construct(compilation.GetSpecialType(SpecialType.System_String));
            var inner = constructed.GetTypeMembers().Single();
            TestRoundTrip(inner, compilation);
        }

        [Fact, WorkItem(235912, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=235912&_a=edit")]
        public void TestNestedGenericType1()
        {
            var source = @"
using System.Collections.Generic;

public class A<T1>
{
    public class B<T2>
    {
        void M<T3>(T1 t1, T2, T3 t3, List<int> l1, List<T3> l2) { }
    }
}";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var a = GetDeclaredSymbols(compilation).OfType<INamedTypeSymbol>().Single(s => s.Name == "A");
            var a_b = a.GetTypeMembers().Single();
            var a_b_m = a_b.GetMembers().Single(s => s.Name == "M");

            TestRoundTrip(a, compilation);
            TestRoundTrip(a_b, compilation);
            TestRoundTrip(a_b_m, compilation);

            var a_string = a.Construct(compilation.GetSpecialType(SpecialType.System_String));
            var a_string_b = a_string.GetTypeMembers().Single();
            var a_string_b_m = a_string_b.GetMembers().Single(s => s.Name == "M");
            TestRoundTrip(a_string, compilation);
            TestRoundTrip(a_string_b, compilation);
            TestRoundTrip(a_string_b_m, compilation);

            var a_string_b_int = a_string_b.Construct(compilation.GetSpecialType(SpecialType.System_Int32));
            var a_string_b_int_m = a_string_b_int.GetMembers().Single(s => s.Name == "M");
            TestRoundTrip(a_string_b_int, compilation);
            TestRoundTrip(a_string_b_int_m, compilation);

            var a_string_b_int_m_datetime = ((IMethodSymbol)a_string_b_int_m).Construct(compilation.GetSpecialType(SpecialType.System_DateTime));
            TestRoundTrip(a_string_b_int_m_datetime, compilation);

            var a_b_int = a_b.Construct(compilation.GetSpecialType(SpecialType.System_Int32));
            var a_b_int_m = a_b_int.GetMembers().Single(s => s.Name == "M");
            var a_b_int_m_datetime = ((IMethodSymbol)a_b_int_m).Construct(compilation.GetSpecialType(SpecialType.System_DateTime));
            TestRoundTrip(a_b_int, compilation);
            TestRoundTrip(a_b_int_m, compilation);
            TestRoundTrip(a_b_int_m_datetime, compilation);

            var a_b_m_datetime = ((IMethodSymbol)a_b_m).Construct(compilation.GetSpecialType(SpecialType.System_DateTime));
            TestRoundTrip(a_b_m_datetime, compilation);
        }

        [Fact, WorkItem(235912, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=235912&_a=edit")]
        public void TestGenericTypeTypeParameter()
        {
            var source = @"class C<T> { }";

            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var typeParameter = GetDeclaredSymbols(compilation).OfType<INamedTypeSymbol>().Single().TypeParameters.Single();

            TestRoundTrip(typeParameter, compilation);
        }

        [Fact, WorkItem(235912, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=235912&_a=edit")]
        public void TestGenericMethodTypeParameter()
        {
            var source = @"class C { void M<T>() { } }";

            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var typeParameter = GetDeclaredSymbols(compilation).OfType<INamedTypeSymbol>().Single().GetMembers("M").OfType<IMethodSymbol>().Single().TypeParameters.Single();

            TestRoundTrip(typeParameter, compilation);
        }

        [Fact, WorkItem(11193, "https://github.com/dotnet/roslyn/issues/11193")]
        public async Task TestGetInteriorSymbolsDoesNotCrashOnSpeculativeSemanticModel()
        {
            var markup = @"
class C
{
    void goo()
    {
        System.Func<int> lambda = () => 
        {
        int x;
        $$
        }
    }
}";
            MarkupTestFile.GetPosition(markup, out var text, out int position);

            var sourceText = SourceText.From(text);
            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject("Test", LanguageNames.CSharp);
            var document = workspace.AddDocument(project.Id, "testdocument", sourceText);

            var firstModel = await document.GetSemanticModelAsync();
            var tree1 = await document.GetSyntaxTreeAsync();
            var basemethod1 = tree1.FindTokenOnLeftOfPosition(position, CancellationToken.None).GetAncestor<CSharp.Syntax.BaseMethodDeclarationSyntax>();

            // Modify the document so we can use the old semantic model as a base.
            var updated = sourceText.WithChanges(new TextChange(new TextSpan(position, 0), "insertion"));
            workspace.TryApplyChanges(document.WithText(updated).Project.Solution);

            document = workspace.CurrentSolution.GetDocument(document.Id);
            var tree2 = await document.GetSyntaxTreeAsync();
            var basemethod2 = tree2.FindTokenOnLeftOfPosition(position, CancellationToken.None).GetAncestor<CSharp.Syntax.BaseMethodDeclarationSyntax>();

            var service = CSharp.CSharpSemanticFactsService.Instance;
            var m = service.TryGetSpeculativeSemanticModel(firstModel, basemethod1, basemethod2, out var testModel);

            var xSymbol = testModel.LookupSymbols(position).First(s => s.Name == "x");

            // This should not throw an exception.
            Assert.NotNull(SymbolKey.Create(xSymbol));
        }

        [Fact]
        public void TestGenericMethodTypeParameterMissing1()
        {
            var source1 = @"
public class C
{
    void M<T>(T t) { }
}
";

            var source2 = @"
public class C
{
}
";

            var compilation1 = GetCompilation(source1, LanguageNames.CSharp);
            var compilation2 = GetCompilation(source2, LanguageNames.CSharp);

            var methods = GetDeclaredSymbols(compilation1).OfType<IMethodSymbol>();
            foreach (var method in methods)
            {
                var key = SymbolKey.Create(method);
                key.Resolve(compilation2);
            }
        }

        [Fact, WorkItem(377839, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=377839")]
        public void TestConstructedMethodInsideLocalFunctionWithTypeParameters()
        {
            var source = @"
using System.Linq;

class C
{
    void Method()
    {
        object LocalFunction<T>()
        {
            return Enumerable.Empty<T>();
        }
    }
}";
            var compilation = GetCompilation(source, LanguageNames.CSharp);
            var symbols = GetAllSymbols(
                compilation.GetSemanticModel(compilation.SyntaxTrees.Single()),
                n => n is CSharp.Syntax.MemberAccessExpressionSyntax || n is CSharp.Syntax.InvocationExpressionSyntax);

            var tested = false;
            foreach (var symbol in symbols)
            {
                // Ensure we don't crash getting these symbol keys.
                var id = SymbolKey.ToString(symbol);
                Assert.NotNull(id);
                var found = SymbolKey.Resolve(id, compilation: compilation).GetAnySymbol();
                Assert.NotNull(found);

                // note: we don't check that the symbols are equal.  That's because the compiler
                // doesn't guarantee that the TypeParameters will be hte same across successive
                // invocations. 
                Assert.Equal(symbol.OriginalDefinition, found.OriginalDefinition);

                tested = true;
            }

            Assert.True(tested);
        }

        [Fact, WorkItem(17702, "https://github.com/dotnet/roslyn/issues/17702")]
        public void TestTupleWithLocalTypeReferences1()
        {
            var source = @"
using System.Linq;

class C
{
    void Method((C, int) t)
    {
    }
}";
            // Tuples store locations along with them.  But we can only recover those locations
            // if we're re-resolving into a compilation with the same files.
            var compilation1 = GetCompilation(source, LanguageNames.CSharp, "File1.cs");
            var compilation2 = GetCompilation(source, LanguageNames.CSharp, "File2.cs");

            var symbol = GetAllSymbols(
                compilation1.GetSemanticModel(compilation1.SyntaxTrees.Single()),
                n => n is CSharp.Syntax.MethodDeclarationSyntax).Single();

            // Ensure we don't crash getting these symbol keys.
            var id = SymbolKey.ToString(symbol);
            Assert.NotNull(id);

            // Validate that if the client does ask to resolve locations that we
            // do not crash if those locations cannot be found.
            var found = SymbolKey.Resolve(id, compilation2, resolveLocations: true).GetAnySymbol();
            Assert.NotNull(found);

            Assert.Equal(symbol.Name, found.Name);
            Assert.Equal(symbol.Kind, found.Kind);

            var method = found as IMethodSymbol;
            Assert.True(method.Parameters[0].Type.IsTupleType);
        }

        [Fact, WorkItem(17702, "https://github.com/dotnet/roslyn/issues/17702")]
        public void TestTupleWithLocalTypeReferences2()
        {
            var source = @"
using System.Linq;

class C
{
    void Method((C a, int b) t)
    {
    }
}";
            // Tuples store locations along with them.  But we can only recover those locations
            // if we're re-resolving into a compilation with the same files.
            var compilation1 = GetCompilation(source, LanguageNames.CSharp, "File1.cs");
            var compilation2 = GetCompilation(source, LanguageNames.CSharp, "File2.cs");

            var symbol = GetAllSymbols(
                compilation1.GetSemanticModel(compilation1.SyntaxTrees.Single()),
                n => n is CSharp.Syntax.MethodDeclarationSyntax).Single();

            // Ensure we don't crash getting these symbol keys.
            var id = SymbolKey.ToString(symbol);
            Assert.NotNull(id);

            // Validate that if the client does ask to resolve locations that we
            // do not crash if those locations cannot be found.
            var found = SymbolKey.Resolve(id, compilation2, resolveLocations: true).GetAnySymbol();
            Assert.NotNull(found);

            Assert.Equal(symbol.Name, found.Name);
            Assert.Equal(symbol.Kind, found.Kind);

            var method = found as IMethodSymbol;
            Assert.True(method.Parameters[0].Type.IsTupleType);
        }

        private void TestRoundTrip(IEnumerable<ISymbol> symbols, Compilation compilation, Func<ISymbol, object> fnId = null)
        {
            foreach (var symbol in symbols)
            {
                TestRoundTrip(symbol, compilation, fnId: fnId);
            }
        }

        private void TestRoundTrip(ISymbol symbol, Compilation compilation, Func<ISymbol, object> fnId = null)
        {
            var id = SymbolKey.ToString(symbol);
            Assert.NotNull(id);
            var found = SymbolKey.Resolve(id, compilation).GetAnySymbol();
            Assert.NotNull(found);

            if (fnId != null)
            {
                var expected = fnId(symbol);
                var actual = fnId(found);
                Assert.Equal(expected, actual);
            }
            else
            {
                Assert.Equal(symbol, found);
            }
        }

        private Compilation GetCompilation(string source, string language, string path = "")
        {
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };

            if (language == LanguageNames.CSharp)
            {
                var tree = CSharp.SyntaxFactory.ParseSyntaxTree(source, path: path);
                return CSharp.CSharpCompilation.Create("Test", syntaxTrees: new[] { tree }, references: references);
            }
            else if (language == LanguageNames.VisualBasic)
            {
                var tree = VisualBasic.SyntaxFactory.ParseSyntaxTree(source, path: path);
                return VisualBasic.VisualBasicCompilation.Create("Test", syntaxTrees: new[] { tree }, references: references);
            }

            throw new NotSupportedException();
        }

        private List<ISymbol> GetAllSymbols(
            SemanticModel model, Func<SyntaxNode, bool> predicate = null)
        {
            var list = new List<ISymbol>();
            GetAllSymbols(model, model.SyntaxTree.GetRoot(), list, predicate);
            return list;
        }

        private void GetAllSymbols(
            SemanticModel model, SyntaxNode node,
            List<ISymbol> list, Func<SyntaxNode, bool> predicate)
        {
            if (predicate == null || predicate(node))
            {
                var symbol = model.GetDeclaredSymbol(node);
                if (symbol != null)
                {
                    list.Add(symbol);
                }

                symbol = model.GetSymbolInfo(node).GetAnySymbol();
                if (symbol != null)
                {
                    list.Add(symbol);
                }
            }

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    GetAllSymbols(model, child.AsNode(), list, predicate);
                }
            }
        }

        private List<ISymbol> GetDeclaredSymbols(Compilation compilation)
        {
            var list = new List<ISymbol>();
            GetDeclaredSymbols(compilation.Assembly.GlobalNamespace, list);
            return list;
        }

        private void GetDeclaredSymbols(INamespaceOrTypeSymbol container, List<ISymbol> symbols)
        {
            foreach (var member in container.GetMembers())
            {
                symbols.Add(member);

                if (member is INamespaceOrTypeSymbol nsOrType)
                {
                    GetDeclaredSymbols(nsOrType, symbols);
                }
            }
        }

        private static IEnumerable<ISymbol> GetInteriorSymbols(ISymbol containingSymbol, Compilation compilation)
        {
            var results = new List<ISymbol>();

            foreach (var declaringLocation in containingSymbol.DeclaringSyntaxReferences)
            {
                var node = declaringLocation.GetSyntax();
                if (node.Language == LanguageNames.VisualBasic)
                {
                    node = node.Parent;
                }

                var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

                GetInteriorSymbols(semanticModel, node, results);
            }

            return results;
        }

        private static void GetInteriorSymbols(SemanticModel model, SyntaxNode root, List<ISymbol> symbols)
        {
            foreach (var token in root.DescendantNodes())
            {
                var symbol = model.GetDeclaredSymbol(token);
                if (symbol != null)
                {
                    symbols.Add(symbol);
                }
            }
        }
    }
}
