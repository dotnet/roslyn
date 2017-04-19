﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Linq;

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
            var comp = CreateStandardCompilation(text);
            comp.VerifyDiagnostics();
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
            var comp = CreateStandardCompilation(text);
            comp.VerifyDiagnostics();
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
            var comp = CreateStandardCompilation(text);
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
            var comp = CreateStandardCompilation(text);
            comp.VerifyDiagnostics(
                // (8,44): error CS1621: The yield statement cannot be used inside an anonymous method or lambda expression
                //         Func<IEnumerable<int>> i = () => { yield break; };
                Diagnostic(ErrorCode.ERR_YieldInAnonMeth, "yield").WithLocation(8, 44)
                );
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
            var comp = CreateStandardCompilation(text);

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
            // The imcomplete statement is intended
            var text = "yield return int.";
            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Script);
            comp.VerifyDiagnostics(
                // (1,18): error CS1001: Identifier expected
                // yield return int.
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 18),
                // (1,18): error CS1002: ; expected
                // yield return int.
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 18),
                // (1,18): error CS0117: 'int' does not contain a definition for ''
                // yield return int.
                Diagnostic(ErrorCode.ERR_NoSuchMember, "").WithArguments("int", "").WithLocation(1, 18),
                // (1,1): error CS7020: You cannot use 'yield' in top-level script code
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
            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Script);
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
            var comp = CreateStandardCompilation(text, options: TestOptions.DebugDll);
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
            var comp = CreateStandardCompilation(text);
            comp.VerifyDiagnostics(
                // (7,15): error CS1627: Expression expected after yield return
                //         yield return;
                Diagnostic(ErrorCode.ERR_EmptyYield, "return").WithLocation(7, 15)
                );
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
            var comp = CreateCompilationWithMscorlib45(text);
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
            var comp = CreateCompilationWithMscorlib45(text);
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
    }
}
