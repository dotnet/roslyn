// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class IteratorTests : CompilingTestBase
    {
        [Fact]
        public void BasicIterators01()
        {
            var text =
@"using System.Collections.Generic;

class Test
{
    IEnumerable<int> I()
    {
        yield return 1;
        yield break;
    }
}";
            var comp = CreateCompilation(text);

            var i = comp.GetMember<MethodSymbol>("Test.I");
            IMethodSymbol publicI = i.GetPublicSymbol();

            Assert.True(i.IsIterator);
            Assert.True(publicI.IsIterator);
            Assert.Equal("System.Int32", i.IteratorElementTypeWithAnnotations.ToTestDisplayString());

            comp.VerifyDiagnostics();

            Assert.True(i.IsIterator);
            Assert.True(publicI.IsIterator);
            Assert.Equal("System.Int32", i.IteratorElementTypeWithAnnotations.ToTestDisplayString());
        }

        [Fact]
        public void BasicIterators02()
        {
            var text =
@"using System.Collections.Generic;

class Test
{
    IEnumerable<int> I()
    {
        yield return 1;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void BasicIterators_Async()
        {
            var source = """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                
                class Test
                {
                    async IAsyncEnumerable<int> I()
                    {
                        await Task.Yield();
                        yield return 1;
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics();

            var i = comp.GetMember<MethodSymbol>("Test.I");
            Assert.True(i.IsIterator);
            Assert.True(i.GetPublicSymbol().IsIterator);
            Assert.Equal("System.Int32", i.IteratorElementTypeWithAnnotations.ToTestDisplayString());
        }

        [Fact]
        public void BasicIterators_Metadata()
        {
            var source = """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                
                public class Test
                {
                    public IEnumerable<int> I1()
                    {
                        yield return 1;
                    }

                    public async IAsyncEnumerable<int> I2()
                    {
                        await Task.Yield();
                        yield return 1;
                    }
                }
                """;

            var sourceComp = CreateCompilation(source, targetFramework: TargetFramework.Net60);
            sourceComp.VerifyDiagnostics();

            var userComp = CreateCompilation("", references: [sourceComp.EmitToImageReference()]);
            userComp.VerifyEmitDiagnostics();
            var testType = Assert.IsAssignableFrom<PENamedTypeSymbol>(userComp.GetTypeByMetadataName("Test"));

            var i1 = testType.GetMethod("I1");
            Assert.False(i1.IsIterator);
            Assert.False(i1.GetPublicSymbol().IsIterator);

            var i2 = testType.GetMethod("I2");
            Assert.False(i2.IsIterator);
            Assert.False(i2.GetPublicSymbol().IsIterator);
        }

        [Fact]
        public void MethodJustReturnsEnumerable_NotIterator()
        {
            var source = """
                using System.Collections.Generic;

                class Test
                {
                    IEnumerable<int> I1()
                    {
                        return [];
                    }

                    IAsyncEnumerable<int> I2()
                    {
                        return default;
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics();

            var i1 = comp.GetMember<MethodSymbol>("Test.I1");
            Assert.False(i1.IsIterator);
            Assert.False(i1.GetPublicSymbol().IsIterator);

            var i2 = comp.GetMember<MethodSymbol>("Test.I2");
            Assert.False(i2.IsIterator);
            Assert.False(i2.GetPublicSymbol().IsIterator);
        }

        [Fact]
        public void WrongYieldType()
        {
            var text =
@"using System.Collections.Generic;

class Test
{
    IEnumerable<int> I()
    {
        yield return 1.1;
        yield break;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (7,22): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         yield return 1.1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1.1").WithArguments("double", "int").WithLocation(7, 22)
                );
        }

        [Fact]
        public void NoYieldInLambda()
        {
            var text =
@"using System;
using System.Collections.Generic;

class Test
{
    IEnumerable<int> I()
    {
        Func<IEnumerable<int>> i = () => { yield break; };
        yield break;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (8,44): error CS1621: The yield statement cannot be used inside an anonymous method or lambda expression
                //         Func<IEnumerable<int>> i = () => { yield break; };
                Diagnostic(ErrorCode.ERR_YieldInAnonMeth, "yield").WithLocation(8, 44)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72443")]
        public void YieldInLock_Async()
        {
            var source = """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class C
                {
                    public async Task ProcessValueAsync()
                    {
                        await foreach (int item in GetValuesAsync())
                        {
                            await Task.Yield();
                            Console.Write(item);
                        }
                    }

                    private async IAsyncEnumerable<int> GetValuesAsync()
                    {
                        await Task.Yield();
                        lock (this)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                yield return i;

                                if (i == 3)
                                {
                                    yield break;
                                }
                            }
                        }
                    }
                }
                """ + AsyncStreamsTypes;

            var comp = CreateCompilationWithTasksExtensions(source);
            CompileAndVerify(comp).VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72443")]
        public void YieldInLock_Sync()
        {
            var source = """
                using System;
                using System.Collections.Generic;
                using System.Threading;

                object o = new object();
                Console.WriteLine($"Before: {Monitor.IsEntered(o)}");
                using (IEnumerator<int> e = GetValues(o).GetEnumerator())
                {
                    Console.WriteLine($"Inside: {Monitor.IsEntered(o)}");
                    while (e.MoveNext())
                    {
                        Console.WriteLine($"{e.Current}: {Monitor.IsEntered(o)}");
                    }
                    Console.WriteLine($"Done: {Monitor.IsEntered(o)}");
                }
                Console.WriteLine($"After: {Monitor.IsEntered(o)}");

                static IEnumerable<int> GetValues(object obj)
                {
                    lock (obj)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            yield return i;

                            if (i == 1)
                            {
                                yield break;
                            }
                        }
                    }
                }
                """;

            var expectedOutput = """
                Before: False
                Inside: False
                0: True
                1: True
                Done: False
                After: False
                """;

            CompileAndVerify(source, options: TestOptions.ReleaseExe,
                expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72443")]
        public void YieldInLock_Nested()
        {
            var source = """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> M()
                    {
                        yield return 1;
                        lock (this)
                        {
                            yield return 2;

                            local();

                            IEnumerable<int> local()
                            {
                                yield return 3;

                                lock (this)
                                {
                                    yield return 4;

                                    yield break;
                                }
                            }

                            yield break;
                        }
                    }
                }
                """;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(546081, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546081")]
        [Fact]
        public void IteratorBlockWithUnreachableCode()
        {
            var text =
@"using System;
using System.Collections;

public class Stack : IEnumerable
{
    int value;
    public IEnumerator GetEnumerator() { return BottomToTop.GetEnumerator(); }

    public IEnumerable BottomToTop
    {
        get
        {
            throw new Exception();
            yield return value;
        }
    }

}

class Test
{
    static void Main()
    {
    }
}";
            var comp = CreateCompilation(text);

            EmitResult emitResult;
            using (var output = new MemoryStream())
            {
                emitResult = comp.Emit(output, null, null, null);
            }

            Assert.True(emitResult.Success);
        }

        [WorkItem(546364, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546364")]
        [Fact]
        public void IteratorWithEnumeratorMoveNext()
        {
            var text =
@"using System.Collections;
using System.Collections.Generic;
public class Item
{
}
public class Program
{
    private IEnumerable<Item> DeferredFeedItems(IEnumerator elements, bool hasMoved)
    {
        while (hasMoved)
        {
            object o = elements.Current;
            if (o != null)
            {
                Item target = new Item();
                yield return target;
            }
            hasMoved = elements.MoveNext();
        }
    }
    public static void Main()
    {
    }
}";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        [WorkItem(813557, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/813557")]
        [Fact]
        public void IteratorWithDelegateCreationExpression()
        {
            var text =
@"using System.Collections.Generic;

delegate void D();
public class Program
{
    private IEnumerable<int> M1()
    {
        D d = new D(M2);
        yield break;
    }
    void M2(int x) {}
    static void M2() {}

    public static void Main()
    {
    }
}";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        [WorkItem(888254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/888254")]
        [Fact]
        public void IteratorWithTryCatch()
        {
            var text =
@"using System;
using System.Collections.Generic;

namespace RoslynYield
{
    class Program
    {
        static void Main(string[] args)
        {
        }
        private IEnumerable<int> Failure()
        {
            // int x;
            try
            {
                int x;  // int x = 3;
                switch (1)
                {
                    default:
                        x = 4;
                        break;
                }
                Console.Write(x);
            }
            catch (Exception)
            {
            }

            yield break;
        }
    }
}";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        [WorkItem(888254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/888254")]
        [Fact]
        public void IteratorWithTryCatchFinally()
        {
            var text =
@"using System;
using System.Collections.Generic;

namespace RoslynYield
{
    class Program
    {
        static void Main(string[] args)
        {
        }
        private IEnumerable<int> Failure()
        {
            try
            {
                int x;
                switch (1)
                {
                    default:
                        x = 4;
                        break;
                }
                Console.Write(x);
            }
            catch (Exception)
            {
                int x;
                switch (1)
                {
                    default:
                        x = 4;
                        break;
                }
                Console.Write(x);
            }
            finally
            {
                int x;
                switch (1)
                {
                    default:
                        x = 4;
                        break;
                }
                Console.Write(x);
            }

            yield break;
        }
    }
}";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(5390, "https://github.com/dotnet/roslyn/issues/5390")]
        public void TopLevelYieldReturn()
        {
            // The incomplete statement is intended
            var text = "yield return int.";
            var comp = CreateCompilationWithMscorlib461(text, parseOptions: TestOptions.Script);
            comp.VerifyDiagnostics(
                // (1,18): error CS1001: Identifier expected
                // yield return int.
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 18),
                // (1,18): error CS1002: ; expected
                // yield return int.
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 18),
                // (1,1): error CS7020: Cannot use 'yield' in top-level script code
                // yield return int.
                Diagnostic(ErrorCode.ERR_YieldNotAllowedInScript, "yield").WithLocation(1, 1));

            var tree = comp.SyntaxTrees[0];
            var yieldNode = (YieldStatementSyntax)tree.GetRoot().DescendantNodes().Where(n => n is YieldStatementSyntax).SingleOrDefault();

            Assert.NotNull(yieldNode);
            Assert.Equal(SyntaxKind.YieldReturnStatement, yieldNode.Kind());

            var model = comp.GetSemanticModel(tree);
            var typeInfo = model.GetTypeInfo(yieldNode.Expression);

            Assert.Equal(TypeKind.Error, typeInfo.Type.TypeKind);
        }

        [Fact]
        [WorkItem(5390, "https://github.com/dotnet/roslyn/issues/5390")]
        public void TopLevelYieldBreak()
        {
            var text = "yield break;";
            var comp = CreateCompilationWithMscorlib461(text, parseOptions: TestOptions.Script);
            comp.VerifyDiagnostics(
                // (1,1): error CS7020: You cannot use 'yield' in top-level script code
                // yield break;
                Diagnostic(ErrorCode.ERR_YieldNotAllowedInScript, "yield").WithLocation(1, 1));

            var tree = comp.SyntaxTrees[0];
            var yieldNode = (YieldStatementSyntax)tree.GetRoot().DescendantNodes().Where(n => n is YieldStatementSyntax).SingleOrDefault();

            Assert.NotNull(yieldNode);
            Assert.Equal(SyntaxKind.YieldBreakStatement, yieldNode.Kind());
        }

        [Fact]
        [WorkItem(11649, "https://github.com/dotnet/roslyn/issues/11649")]
        public void IteratorRewriterShouldNotRewriteBaseMethodWrapperSymbol()
        {
            var text =
@"using System.Collections.Generic;

class Base
{
    protected virtual IEnumerable<int> M()
    {
        yield break;
    }

    class D : Base
    {
        protected override IEnumerable<int> M()
        {
            base.M(); // the rewriting of D.M() synthesizes a BaseMethodWrapperSymbol for this base call, with return type IEnumerable,
                      // but it should not in turn be lowered by the IteratorRewriter
            yield break;
        }
    }
}";
            var comp = CreateCompilation(text, options: TestOptions.DebugDll);
            comp.VerifyEmitDiagnostics(); // without the fix for bug 11649, the compilation would fail emitting
            CompileAndVerify(comp);
        }

        [Fact]
        [WorkItem(11649, "https://github.com/dotnet/roslyn/issues/11649")]
        public void IteratorRewriterShouldNotRewriteBaseMethodWrapperSymbol2()
        {
            var source =
@"using System.Collections.Generic;

class Base
{
    public static void Main()
    {
        System.Console.WriteLine(string.Join("","", new D().M()));
    }

    protected virtual IEnumerable<int> M()
    {
        yield return 1;
        yield return 2;
        yield break;
    }

    class D : Base
    {
        protected override IEnumerable<int> M()
        {
            yield return 0;
            foreach (var n in base.M())
            {
                yield return n;
            }
            yield return 3;
            yield break;
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "0,1,2,3", options: TestOptions.DebugExe);
            comp.Compilation.VerifyDiagnostics();
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        [WorkItem(261047, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=261047&_a=edit")]
        public void MissingExpression()
        {
            var text =
@"using System.Collections.Generic;

class Test
{
    IEnumerable<int> I()
    {
        yield return;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (7,15): error CS1627: Expression expected after yield return
                //         yield return;
                Diagnostic(ErrorCode.ERR_EmptyYield, "return").WithLocation(7, 15)
                );

            var tree = comp.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<YieldStatementSyntax>().First();

            Assert.Equal("yield return;", node.ToString());

            comp.VerifyOperationTree(node, expectedOperationTree:
@"
IReturnOperation (OperationKind.YieldReturn, Type: null, IsInvalid) (Syntax: 'yield return;')
  ReturnedValue: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'yield return;')
      Children(0)
");
        }

        [Fact]
        [WorkItem(3825, "https://github.com/dotnet/roslyn/issues/3825")]
        public void ObjectCreationExpressionSyntax_01()
        {
            var text = @"
using System.Collections.Generic;

class Test<TKey, TValue>
{
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator(KeyValuePair<TKey, TValue> kvp)
    {
        yield return new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value);
    }
}";
            var comp = CreateCompilationWithMscorlib461(text);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();

            Assert.Equal("new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value)", node.ToString());

            var model = comp.GetSemanticModel(tree);
            var typeInfo = model.GetTypeInfo(node);
            var symbolInfo = model.GetSymbolInfo(node);

            Assert.Null(model.GetDeclaredSymbol(node));
            Assert.Equal("System.Collections.Generic.KeyValuePair<TKey, TValue>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(node).IsIdentity);

            Assert.Equal("System.Collections.Generic.KeyValuePair<TKey, TValue>..ctor(TKey key, TValue value)", symbolInfo.Symbol.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(3825, "https://github.com/dotnet/roslyn/issues/3825")]
        public void ObjectCreationExpressionSyntax_02()
        {
            var text = @"
using System.Collections.Generic;

class Test<TKey, TValue>
{
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator(KeyValuePair<TKey, TValue> kvp)
    {
        yield return new KeyValuePair<TKey, TValue>(kvp, kvp.Value);
    }
}";
            var comp = CreateCompilationWithMscorlib461(text);
            comp.VerifyDiagnostics(
                // (8,53): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.KeyValuePair<TKey, TValue>' to 'TKey'
                //         yield return new KeyValuePair<TKey, TValue>(kvp, kvp.Value);
                Diagnostic(ErrorCode.ERR_BadArgType, "kvp").WithArguments("1", "System.Collections.Generic.KeyValuePair<TKey, TValue>", "TKey").WithLocation(8, 53)
                );

            var tree = comp.SyntaxTrees[0];
            var node = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();

            Assert.Equal("new KeyValuePair<TKey, TValue>(kvp, kvp.Value)", node.ToString());

            var model = comp.GetSemanticModel(tree);
            var typeInfo = model.GetTypeInfo(node);
            var symbolInfo = model.GetSymbolInfo(node);

            Assert.Null(model.GetDeclaredSymbol(node));
            Assert.Equal("System.Collections.Generic.KeyValuePair<TKey, TValue>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(node).IsIdentity);

            Assert.Null(symbolInfo.Symbol);
            Assert.Contains("System.Collections.Generic.KeyValuePair<TKey, TValue>..ctor(TKey key, TValue value)", symbolInfo.CandidateSymbols.Select(c => c.ToTestDisplayString()));
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_01()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve1Attribute : Attribute { }

[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve2Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;

class Test1
{
    IEnumerable<T> M2<[Preserve1][Preserve2]T>(T x)
    {
        yield return x;
    }
}
";
            var comp1 = CreateCompilation([source1, source2, CompilerLoweringPreserveAttributeDefinition]);
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember<NamedTypeSymbol>("Test1.<M2>d__0").TypeParameters.Single().GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_02()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve1Attribute : Attribute { }

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Parameter)]
public class Preserve2Attribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve3Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;

class Test1
{
    IEnumerable<int> M2([Preserve1][Preserve2][Preserve3]int x)
    {
        yield return x;
    }
}
";
            var comp1 = CreateCompilation(
                [source1, source2, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<M2>d__0.x").GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<M2>d__0.<>3__x").GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_03()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve1Attribute : Attribute { }

[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve2Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;

#pragma warning disable CS8321 // Local function is declared but never used

class Test1
{
    static void Test()
    {
        IEnumerable<T> local<[Preserve1][Preserve2]T>(T x)
        {
            yield return x;
        }
    }
}
";
            var comp1 = CreateCompilation([source1, source2, CompilerLoweringPreserveAttributeDefinition], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            string source3 = @"
using System.Collections.Generic;

#pragma warning disable CS8321 // Local function is declared but never used

class Test1
{
    static void Test<[Preserve1][Preserve2]T>()
    {
        IEnumerable<T> local(T x)
        {
            yield return x;
        }
    }
}
";
            comp1 = CreateCompilation([source1, source3, CompilerLoweringPreserveAttributeDefinition], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute", "Preserve2Attribute"],
                    m.GlobalNamespace.GetMember<MethodSymbol>("Test1.<Test>g__local|0_0").TypeParameters.Single().GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember<NamedTypeSymbol>("Test1.<<Test>g__local|0_0>d").TypeParameters.Single().GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_04()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve1Attribute : Attribute { }

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Parameter)]
public class Preserve2Attribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve3Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;

#pragma warning disable CS8321 // Local function is declared but never used

class Test1
{
    static void Test()
    {
        IEnumerable<int> local([Preserve1][Preserve2][Preserve3]int x)
        {
            yield return x;
        }
    }
}
";
            var comp1 = CreateCompilation(
                [source1, source2, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute", "Preserve2Attribute", "Preserve3Attribute"],
                    m.GlobalNamespace.GetMember<MethodSymbol>("Test1.<Test>g__local|0_0").Parameters.Single().GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<<Test>g__local|0_0>d.x").GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<<Test>g__local|0_0>d.<>3__x").GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_05()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve1Attribute : Attribute { }

[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve2Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;

static class Test1
{
    extension<[Preserve1][Preserve2]T>(T x)
    {
        IEnumerable<T> M2()
        {
            yield return x;
        }
    }
}
";
            var comp1 = CreateCompilation([source1, source2, CompilerLoweringPreserveAttributeDefinition]);
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            string source3 = @"
using System.Collections.Generic;

static class Test1
{
    extension(int i)
    {
        IEnumerable<T> M2<[Preserve1][Preserve2]T>(T x)
        {
            yield return x;
        }
    }
}
";
            comp1 = CreateCompilation([source1, source3, CompilerLoweringPreserveAttributeDefinition]);
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember<NamedTypeSymbol>("Test1.<M2>d__1").TypeParameters.Single().GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_06()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve1Attribute : Attribute { }

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Parameter)]
public class Preserve2Attribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve3Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;

static class Test1
{
    extension([Preserve1][Preserve2][Preserve3]int x)
    {
        IEnumerable<int> M2()
        {
            yield return x;
        }
    }
}
";
            var comp1 = CreateCompilation(
                [source1, source2, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            string source3 = @"
using System.Collections.Generic;

static class Test1
{
    extension(int i)
    {
        IEnumerable<int> M2([Preserve1][Preserve2][Preserve3]int x)
        {
            yield return x;
        }
    }
}
";
            comp1 = CreateCompilation(
                [source1, source3, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<M2>d__1.x").GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<M2>d__1.<>3__x").GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_07()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve1Attribute : Attribute { }

[AttributeUsage(AttributeTargets.GenericParameter)]
public class Preserve2Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;

#pragma warning disable CS8321 // Local function is declared but never used

static class Test1
{
    extension<[Preserve1][Preserve2]T>(int i)
    {
        void Test()
        {
            IEnumerable<T> local(T x)
            {
                yield return x;
            }
        }
    }
}
";
            var comp1 = CreateCompilation([source1, source2, CompilerLoweringPreserveAttributeDefinition], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            string source3 = @"
using System.Collections.Generic;

#pragma warning disable CS8321 // Local function is declared but never used

static class Test1
{
    extension(int i)
    {
        static void Test<[Preserve1][Preserve2]T>()
        {
            IEnumerable<T> local(T x)
            {
                yield return x;
            }
        }
    }
}
";
            comp1 = CreateCompilation([source1, source3, CompilerLoweringPreserveAttributeDefinition], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            string source4 = @"
using System.Collections.Generic;

#pragma warning disable CS8321 // Local function is declared but never used

static class Test1
{
    extension(int i)
    {
        static void Test()
        {
            IEnumerable<T> local<[Preserve1][Preserve2]T>(T x)
            {
                yield return x;
            }
        }
    }
}
";
            comp1 = CreateCompilation([source1, source4, CompilerLoweringPreserveAttributeDefinition], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute", "Preserve2Attribute"],
                    m.GlobalNamespace.GetMember<MethodSymbol>("Test1.<Test>g__local|1_0").TypeParameters.Single().GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember<NamedTypeSymbol>("Test1.<<Test>g__local|1_0>d").TypeParameters.Single().GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_08()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve1Attribute : Attribute { }

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Parameter)]
public class Preserve2Attribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve3Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;

#pragma warning disable CS8321 // Local function is declared but never used

static class Test1
{
    extension(int i)
    {
        static void Test()
        {
            IEnumerable<int> local([Preserve1][Preserve2][Preserve3]int x)
            {
                yield return x;
            }
        }
    }
}
";
            var comp1 = CreateCompilation(
                [source1, source2, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.SequenceEqual(
                    ["Preserve1Attribute", "Preserve2Attribute", "Preserve3Attribute"],
                    m.GlobalNamespace.GetMember<MethodSymbol>("Test1.<Test>g__local|1_0").Parameters.Single().GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<<Test>g__local|1_0>d.x").GetAttributes().Select(a => a.ToString()));

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<<Test>g__local|1_0>d.<>3__x").GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_09()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve1Attribute : Attribute { }

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Parameter)]
public class Preserve2Attribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve3Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;

#pragma warning disable CS8321 // Local function is declared but never used

class Test1
{
    static void Test([Preserve1][Preserve2][Preserve3]int x)
    {
        IEnumerable<int> local()
        {
            yield return x;
        }
    }
}
";
            var comp1 = CreateCompilation(
                [source1, source2, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("Test1.<>c__DisplayClass0_0.<Test>g__local|0").Parameters);

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<>c__DisplayClass0_0.x").GetAttributes().Select(a => a.ToString()));
            }
        }

        [Fact]
        public void CompilerLoweringPreserveAttribute_10()
        {
            string source1 = @"
using System;
using System.Runtime.CompilerServices;

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve1Attribute : Attribute { }

[CompilerLoweringPreserve]
[AttributeUsage(AttributeTargets.Parameter)]
public class Preserve2Attribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
public class Preserve3Attribute : Attribute { }
";

            string source2 = @"
using System.Collections.Generic;

#pragma warning disable CS8321 // Local function is declared but never used

static class Test1
{
    extension([Preserve1][Preserve2][Preserve3]int x)
    {
        void Test()
        {
            IEnumerable<int> local()
            {
                yield return x;
            }
        }
    }
}
";
            var comp1 = CreateCompilation(
                [source1, source2, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            string source3 = @"
using System.Collections.Generic;

#pragma warning disable CS8321 // Local function is declared but never used

static class Test1
{
    extension(int i)
    {
        static void Test([Preserve1][Preserve2][Preserve3]int x)
        {
            IEnumerable<int> local()
            {
                yield return x;
            }
        }
    }
}
";
            comp1 = CreateCompilation(
                [source1, source3, CompilerLoweringPreserveAttributeDefinition],
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp1, symbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol m)
            {
                AssertEx.Empty(m.GlobalNamespace.GetMember<MethodSymbol>("Test1.<>c__DisplayClass1_0.<Test>g__local|0").Parameters);

                AssertEx.SequenceEqual(
                    ["Preserve1Attribute"],
                    m.GlobalNamespace.GetMember("Test1.<>c__DisplayClass1_0.x").GetAttributes().Select(a => a.ToString()));
            }
        }
    }
}
