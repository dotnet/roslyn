// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineMethod
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsInlineMethod)]
    public class CSharpInlineMethodTests
    {
        private class TestVerifier : CSharpCodeRefactoringVerifier<CSharpInlineMethodRefactoringProvider>.Test
        {
            public static async Task TestInRegularAndScript1Async(
                string initialMarkUp,
                string expectedMarkUp,
                int index = 0)
            {
                var test = new TestVerifier { CodeActionIndex = index };
                test.TestState.Sources.Add(initialMarkUp);
                test.FixedState.Sources.Add(expectedMarkUp);
                await test.RunAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public Task TestInlineMethodWithSingleStatement()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller(int i, int j)
    {
        Ca[||]llee(i, j);
    }

    private void Callee(int i, int j)
    {
        System.Console.WriteLine(i + j);
    }
}",
                @"
public class TestClass
{
    private void Caller(int i, int j)
    {
        System.Console.WriteLine(i + j);
    }

    private void Callee(int i, int j)
    {
        System.Console.WriteLine(i + j);
    }
}");

        [Fact]
        public Task TestExtractArrowExpressionBody()
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass
{
    private void Caller(int i, int j)
    {
        Ca[||]llee(i, j);
    }

    private void Callee(int i, int j)
        => System.Console.WriteLine(i + j);
}",
                @"
public class TestClass
{
    private void Caller(int i, int j)
    {
        System.Console.WriteLine(i + j);
    }

    private void Callee(int i, int j)
        => System.Console.WriteLine(i + j);
}");

        [Fact]
        public Task TestExtractExpressionBody()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller(int i, int j)
    {
        var x = Ca[||]llee(i, j);
    }

    private int Callee(int i, int j)
        => i + j;
}",
                @"
public class TestClass
{
    private void Caller(int i, int j)
    {
        var x = i + j;
    }

    private int Callee(int i, int j)
        => i + j;
}");

        [Fact]
        public Task TestDefaultValueReplacementForExpressionStatement()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        Cal[||]lee();
    }

    private void Callee(int i = 1, string c = null, bool y = false)
    {
        System.Console.WriteLine(y ? i : (c ?? ""Hello"").Length);
    }
}",
                @"
public class TestClass
{
    private void Caller()
    {
        System.Console.WriteLine(false ? 1 : (null ?? ""Hello"").Length);
    }

    private void Callee(int i = 1, string c = null, bool y = false)
    {
        System.Console.WriteLine(y ? i : (c ?? ""Hello"").Length);
    }
}");

        [Fact]
        public Task TestDefaultValueReplacementForArrowExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        Cal[||]lee();
    }

    private void Callee(int i = default, string c = default, bool y = false) =>
        System.Console.WriteLine(y ? i : (c ?? ""Hello"").Length);
}",
                @"
public class TestClass
{
    private void Caller()
    {
        System.Console.WriteLine(false ? 0 : (null ?? ""Hello"").Length);
    }

    private void Callee(int i = default, string c = default, bool y = false) =>
        System.Console.WriteLine(y ? i : (c ?? ""Hello"").Length);
}");

        [Fact]
        public Task TestInlineMethodWithLiteralValue()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        Cal[||]lee(1, 'y', true, ""Hello"");
    }

    private void Callee(int i, char c, bool x, string y) =>
        System.Console.WriteLine(i + (int)c + (x ? 1 : y.Length));
}",
                @"
public class TestClass
{
    private void Caller()
    {
        System.Console.WriteLine(1 + (int)'y' + (true ? 1 : ""Hello"".Length));
    }

    private void Callee(int i, char c, bool x, string y) =>
        System.Console.WriteLine(i + (int)c + (x ? 1 : y.Length));
}");

        [Fact]
        public Task TestInlineMethodWithIdentifierReplacement()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller(int m)
    {
        Cal[||]lee(10, m, k: ""Hello"");
    }

    private void Callee(int i, int j = 100, string k = null)
    {
        System.Console.WriteLine(i + j + (k ?? """"));
    }
}",
                @"
public class TestClass
{
    private void Caller(int m)
    {
        System.Console.WriteLine(10 + m + (""Hello"" ?? """"));
    }

    private void Callee(int i, int j = 100, string k = null)
    {
        System.Console.WriteLine(i + j + (k ?? """"));
    }
}");

        [Fact]
        public Task TestInlineMethodWithMethodExtraction()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller(float r1, float r2)
    {
        Cal[||]lee(SomeCaculation(r1), SomeCaculation(r2));
    }

    private void Callee(float s1, float s2)
    {
        System.Console.WriteLine(""This is s1"" + s1 + ""This is S2"" + s2);
    }

    public float SomeCaculation(float r)
    {
        return r * r * 3.14f;
    }
}",
                @"
public class TestClass
{
    private void Caller(float r1, float r2)
    {
        float s1 = SomeCaculation(r1);
        float s2 = SomeCaculation(r2);
        System.Console.WriteLine(""This is s1"" + s1 + ""This is S2"" + s2);
    }

    private void Callee(float s1, float s2)
    {
        System.Console.WriteLine(""This is s1"" + s1 + ""This is S2"" + s2);
    }

    public float SomeCaculation(float r)
    {
        return r * r * 3.14f;
    }
}");

        [Fact]
        public Task TestInlineMethodWithMethodExtractionAndRename()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller(float s1, float s2)
    {
        Ca[||]llee(SomeCaculation(s1), SomeCaculation(s2));
    }

    private void Callee(float s1, float s2)
    {
        System.Console.WriteLine(""This is s1"" + s1 + ""This is s2"" + s2);
    }

    public float SomeCaculation(float r)
    {
        return r * r * 3.14f;
    }
}",
                @"
public class TestClass
{
    private void Caller(float s1, float s2)
    {
        float s3 = SomeCaculation(s1);
        float s4 = SomeCaculation(s2);
        System.Console.WriteLine(""This is s1"" + s3 + ""This is s2"" + s4);
    }

    private void Callee(float s1, float s2)
    {
        System.Console.WriteLine(""This is s1"" + s1 + ""This is s2"" + s2);
    }

    public float SomeCaculation(float r)
    {
        return r * r * 3.14f;
    }
}");

        [Fact]
        public Task TestInlineParamsArrayWithArrayImplicitInitializerExpression()
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass
{
    private void Caller()
    {
        Cal[||]lee(new int[] {1, 2, 3, 4, 5, 6});
    }

    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}"
,
                @"
public class TestClass
{
    private void Caller()
    {
        int[] x = new int[] {1, 2, 3, 4, 5, 6};
        System.Console.WriteLine(x.Length);
    }

    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}");

        [Fact]
        public Task TestInlineParamsArrayWithArrayInitializerExpression()
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass
{
    private void Caller()
    {
        Cal[||]lee(new int[6] {1, 2, 3, 4, 5, 6});
    }

    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}"
,
                @"
public class TestClass
{
    private void Caller()
    {
        int[] x = new int[6] {1, 2, 3, 4, 5, 6};
        System.Console.WriteLine(x.Length);
    }

    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}");

        [Fact]
        public Task TestInlineParamsArrayWithOneElement()
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass
{
    private void Caller()
    {
        Cal[||]lee(1);
    }

    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}"
,
                @"
public class TestClass
{
    private void Caller()
    {
        int[] x = {
            1
        };
        System.Console.WriteLine(x.Length);
    }

    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}");

        [Fact]
        public Task TestInlineParamsArrayMethodWithIdentifier()
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass
{
    private void Caller()
    {
        var i = new int[] {1, 2, 3, 4, 5};
        Cal[||]lee(i);
    }

    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}",
                @"
public class TestClass
{
    private void Caller()
    {
        var i = new int[] {1, 2, 3, 4, 5};
        System.Console.WriteLine(i.Length);
    }

    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}");
        [Fact]
        public Task TestInlineMethodWithNoElementInParamsArray()
            => TestVerifier.TestInRegularAndScript1Async(
                    @"
public class TestClass
{
    private void Caller()
    {
        Cal[||]lee();
    }
    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}",
                    @"
public class TestClass
{
    private void Caller()
    {
        int[] x = {
        };
        System.Console.WriteLine(x.Length);
    }
    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}");

        [Fact]
        public Task TestInlineMethodWithParamsArray()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        Ca[||]llee(1, 2, 3, 4, 5, 6);
    }

    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}",
                // Is this is the intentional array format?
                @"
public class TestClass
{
    private void Caller()
    {
        int[] x = {
            1,
            2,
            3,
            4,
            5,
            6
        };
        System.Console.WriteLine(x.Length);
    }

    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}");

        [Fact]
        public Task TestInlineMethodWithEmptyContent()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        Call[||]ee();
    }

    private void Callee()
    {
        return;
    }
}",
                @"
public class TestClass
{
    private void Caller()
    {
    }

    private void Callee()
    {
        return;
    }
}");

        [Fact]
        public Task TestInlineMethodWithVariableDeclaration()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        Cal[||]lee(out var x);
    }

    private void Callee(out int z)
    {
        z = 10;
    }
}",
                @"
public class TestClass
{
    private void Caller()
    {
        int x;
        x = 10;
    }

    private void Callee(out int z)
    {
        z = 10;
    }
}");

        [Fact]
        public Task TestInlineMethodAsArgument()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        var x = Callee1(Cal[||]lee1(Callee1(Callee1(10))));
    }

    private int Callee1(int j)
    {
        return 1 + 2 + j;
    }
}",
                @"
public class TestClass
{
    private void Caller()
    {
        int j = Callee1(Callee1(10));
        var x = Callee1(1 + 2 + j);
    }

    private int Callee1(int j)
    {
        return 1 + 2 + j;
    }
}");
        [Fact]
        public Task TestInlineMethodWithConditionalExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public void Caller(bool x)
    {
        int t = C[||]allee(x ? Callee(1) : Callee(2));
    }
    
    private int Callee(int i)
    {
        return i + 1;
    }
}",
                @"
public class TestClass
{
    public void Caller(bool x)
    {
        int i = x ? Callee(1) : Callee(2);
        int t = i + 1;
    }
    
    private int Callee(int i)
    {
        return i + 1;
    }
}");

        [Fact]
        public Task TestInlineExpressionWithoutAssignedToVariable()
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass
{
    public void Caller(int j)
    {
        Cal[||]lee(j);
    }

    private int Callee(int i)
    {
        return i + 1;
    }
}", @"
public class TestClass
{
    public void Caller(int j)
    {
        int tmp = j + 1;
    }

    private int Callee(int i)
    {
        return i + 1;
    }
}");

        [Fact]
        public Task TestInlineMethodWithNullCoalescingExpression()
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass
{
    public void Caller(int? i)
    {
        var t = Cal[||]lee(i ?? 1);
    }

    private int Callee(int i)
    {
        return i + 1;
    }
}", @"
public class TestClass
{
    public void Caller(int? i)
    {
        int i1 = i ?? 1;
        var t = i1 + 1;
    }

    private int Callee(int i)
    {
        return i + 1;
    }
}");

        [Fact]
        public Task TestInlineSimpleLambdaExpression()
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass
{
    public System.Func<int, int, int> Caller()
    {
        return Ca[||]llee();
    }

    private System.Func<int, int, int> Callee() => (i, j) => i + j;
}", @"
public class TestClass
{
    public System.Func<int, int, int> Caller()
    {
        return (i, j) => i + j;
    }

    private System.Func<int, int, int> Callee() => (i, j) => i + j;
}");

        [Fact]
        public Task TestInlineMethodWithGenericsArguments()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
using System;
public class TestClass
{
    private void Caller<U>()
    {
        Ca[||]llee<int, U>(1, 2, 3);
    }
    private void Callee<T, U>(params T[] i)
    {
        System.Console.WriteLine(typeof(T).Name.Length + i.Length + typeof(U).Name.Length);
    }
}",
                @"
using System;
public class TestClass
{
    private void Caller<U>()
    {
        int[] i = {
            1,
            2,
            3
        };
        System.Console.WriteLine(typeof(int).Name.Length + i.Length + typeof(U).Name.Length);
    }
    private void Callee<T, U>(params T[] i)
    {
        System.Console.WriteLine(typeof(T).Name.Length + i.Length + typeof(U).Name.Length);
    }
}");

        [Fact]
        public Task TestAwaitExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
using System.Threading.Tasks;
public class TestClass
{
    public Task<int> Caller(bool x)
    {
        System.Console.WriteLine("""");
        return Call[||]ee(10, x ? 1 : 2);
    }

    private async Task<int> Callee(int i, int j)
    {
        return await Task.FromResult(i + j);
    }
}",
                @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task<int> Caller(bool x)
    {
        System.Console.WriteLine("""");
        int j = x ? 1 : 2;
        return await Task.FromResult(10 + j);
    }

    private async Task<int> Callee(int i, int j)
    {
        return await Task.FromResult(i + j);
    }
}");

        [Fact]
        public Task TestInlineWithinDoStatement()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        do
        {
        } while(Cal[||]lee(SomeInt()) == 1);
    }

    private int Callee(int i)
    {
        return i + 1;
    }

    private int SomeInt() => 10;
}",
                @"
public class TestClass
{
    private void Caller()
    {
        int i = SomeInt();
        do
        {
        } while(i + 1 == 1);
    }

    private int Callee(int i)
    {
        return i + 1;
    }

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineWithinForStatement()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        for (int i = Ca[||]llee(SomeInt()); i < 10; i++)
        {
        }
    }

    private int Callee(int i)
    {
        return i + 1;
    }

    private int SomeInt() => 10;
}",
                @"
public class TestClass
{
    private void Caller()
    {
        int i1 = SomeInt();
        for (int i = i1 + 1; i < 10; i++)
        {
        }
    }

    private int Callee(int i)
    {
        return i + 1;
    }

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineWithinIfStatement()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        if (Ca[||]llee(SomeInt()) == 1)
        {
        }
    }

    private int Callee(int i)
    {
        return i + 1;
    }

    private int SomeInt() => 10;
}",
                @"
public class TestClass
{
    private void Caller()
    {
        int i = SomeInt();
        if (i + 1 == 1)
        {
        }
    }

    private int Callee(int i)
    {
        return i + 1;
    }

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineWithinLockStatement()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        lock (Ca[||]llee(SomeInt()))
        {
        }
    }

    private string Callee(int i)
    {
        return ""Hello"" + i;
    }

    private int SomeInt() => 10;
}",
                @"
public class TestClass
{
    private void Caller()
    {
        int i = SomeInt();
        lock (""Hello"" + i)
        {
        }
    }

    private string Callee(int i)
    {
        return ""Hello"" + i;
    }

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineWithinReturnStatement()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private string Caller()
    {
        return Call[||]ee(SomeInt());
    }

    private string Callee(int i)
    {
        return ""Hello"" + i;
    }

    private int SomeInt() => 10;
}",
                @"
public class TestClass
{
    private string Caller()
    {
        int i = SomeInt();
        return ""Hello"" + i;
    }

    private string Callee(int i)
    {
        return ""Hello"" + i;
    }

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineMethodWithinThrowStatement()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        throw new System.Exception(Call[||]ee(SomeInt()) + """");
    }

    private int Callee(int i)
    {
        return i + 20;
    }

    private int SomeInt() => 10;
}",
                @"
public class TestClass
{
    private void Caller()
    {
        int i = SomeInt();
        throw new System.Exception(i + 20 + """");
    }

    private int Callee(int i)
    {
        return i + 20;
    }

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineWithinWhileStatement()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        while (Cal[||]lee(SomeInt()) == 1)
        {}
    }

    private int Callee(int i)
    {
        return i + 1;
    }

    private int SomeInt() => 10;
}",
                @"
public class TestClass
{
    private void Caller()
    {
        int i = SomeInt();
        while (i + 1 == 1)
        {}
    }

    private int Callee(int i)
    {
        return i + 1;
    }

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineMethodWithinTryStatement()
            => TestVerifier.TestInRegularAndScript1Async(
            @"
public class TestClass
{
    private void Calller()
    {
        try
        {
        }
        catch (System.Exception e) when (Ca[||]llee(e, SomeInt()))
        {
        }
    }

    private bool Callee(System.Exception e, int i) => i == 1;

    private int SomeInt() => 10;
}",
                @"
public class TestClass
{
    private void Calller()
    {
        int i = SomeInt();
        try
        {
        }
        catch (System.Exception e) when (i == 1)
        {
        }
    }

    private bool Callee(System.Exception e, int i) => i == 1;

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineMethodWithinUsingStatement()
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass2
{
    private class DisposeClass : System.IDisposable
    {
        public void Dispose()
        {
        }
    }

    private void Calller()
    {
        using (var x = Cal[||]lee(SomeInt()))
        {
        }
    }

    private System.IDisposable Callee(int i) => new DisposeClass();

    private int SomeInt() => 10;
}",
                @"
public class TestClass2
{
    private class DisposeClass : System.IDisposable
    {
        public void Dispose()
        {
        }
    }

    private void Calller()
    {
        int i = SomeInt();
        using (var x = new DisposeClass())
        {
        }
    }

    private System.IDisposable Callee(int i) => new DisposeClass();

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineMethodWithinYieldReturnStatement()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass2
{
    private System.Collections.Generic.IEnumerable<int> Calller()
    {
        yield return 1;
        yield return Cal[||]lee(SomeInt());
        yield return Callee(SomeInt());
    }

    private int Callee(int i) => i + 10;

    private int SomeInt() => 10;
}",
                @"
public class TestClass2
{
    private System.Collections.Generic.IEnumerable<int> Calller()
    {
        yield return 1;
        int i = SomeInt();
        yield return i + 10;
        yield return Callee(SomeInt());
    }

    private int Callee(int i) => i + 10;

    private int SomeInt() => 10;
}");

        #region parenthesisTest

        [Fact]
        public Task TestInlineExpressionAsLeftValueInLeftAssociativeExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public int Caller
    {
        get
        {
            return Ca[||]llee(1, 2) - 1;
        }
    }

    private int Callee(int i, int j)
    {
        return i + j;
    }
}",
                @"
public class TestClass
{
    public int Caller
    {
        get
        {
            return 1 + 2 - 1;
        }
    }

    private int Callee(int i, int j)
    {
        return i + j;
    }
}");

        [Fact]
        public Task TestInlineExpressionAsRightValueInRightAssociativeExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public int Caller
    {
        get
        {
            return 1 - Ca[||]llee(1, 2);
        }
    }

    private int Callee(int i, int j)
    {
        return i + j;
    }
}",
                @"
public class TestClass
{
    public int Caller
    {
        get
        {
            return 1 - (1 + 2);
        }
    }

    private int Callee(int i, int j)
    {
        return i + j;
    }
}");

        [Fact]
        public Task TestAddExpressionWithMultiply()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public int Caller
    {
        get
        {
            return Ca[||]llee(1, 2) * 2;
        }
    }

    private int Callee(int i, int j)
    {
        return i + j;
    }
}",
                @"
public class TestClass
{
    public int Caller
    {
        get
        {
            return (1 + 2) * 2;
        }
    }

    private int Callee(int i, int j)
    {
        return i + j;
    }
}");

        [Fact]
        public Task TestIsExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public bool Caller(int i, int j)
    {
        return Ca[||]llee(i, j) is int;
    }

    private bool Callee(int i, int j)
    {
        return i == j;
    }
}",
                @"
public class TestClass
{
    public bool Caller(int i, int j)
    {
        return (i == j) is int;
    }

    private bool Callee(int i, int j)
    {
        return i == j;
    }
}");

        [Fact]
        public Task TestUnaryPlusOperator()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public int Caller()
    {
        return +Call[||]ee();
    }

    private int Callee()
    {
        return 1 + 2;
    }
}",
                @"
public class TestClass
{
    public int Caller()
    {
        return +(1 + 2);
    }

    private int Callee()
    {
        return 1 + 2;
    }
}");

        [Fact]
        public Task TestLogicalNotExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public bool Caller(int i, int j)
    {
        return !Ca[||]llee(i, j);
    }

    private bool Callee(int i, int j)
    {
        return i == j;
    }
}",
                @"
public class TestClass
{
    public bool Caller(int i, int j)
    {
        return !(i == j);
    }

    private bool Callee(int i, int j)
    {
        return i == j;
    }
}");

        [Fact]
        public Task TestBitWiseNotExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public int Caller(int i, int j)
    {
        return ~Call[||]ee(i, j);
    }

    private int Callee(int i, int j)
    {
        return i | j;
    }
}",
                @"
public class TestClass
{
    public int Caller(int i, int j)
    {
        return ~(i | j);
    }

    private int Callee(int i, int j)
    {
        return i | j;
    }
}");

        [Fact]
        public Task TestCastExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public char Caller(int i, int j)
    {
        return (char)Call[||]ee(i, j);
    }

    private int Callee(int i, int j)
    {
        return i + j;
    }
}",
                @"
public class TestClass
{
    public char Caller(int i, int j)
    {
        return (char)(i + j);
    }

    private int Callee(int i, int j)
    {
        return i + j;
    }
}");

        [Fact]
        public Task TestIsPatternExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public void Calller()
    {
        if (Ca[||]llee() is int i)
        {
        }
    }

    private int Callee()
    {
        return 1 | 2;
    }
}",
                @"
public class TestClass
{
    public void Calller()
    {
        if ((1 | 2) is int i)
        {
        }
    }

    private int Callee()
    {
        return 1 | 2;
    }
}");

        [Fact]
        public Task TestAsExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public void Calller(bool f)
    {
        var x = Cal[||]lee(f) as string;
    }

    private object Callee(bool f)
    {
        return f ? ""Hello"" : ""World"";
    }
}",
                @"
public class TestClass
{
    public void Calller(bool f)
    {
        var x = (f ? ""Hello"" : ""World"") as string;
    }

    private object Callee(bool f)
    {
        return f ? ""Hello"" : ""World"";
    }
}");

        [Fact]
        public Task TestCoalesceExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public int Caller(int? c)
    {
        return 1 + Cal[||]lee(c);
    }

    private int Callee(int? c)
    {
        return c ?? 1;
    }
}",
                @"
public class TestClass
{
    public int Caller(int? c)
    {
        return 1 + (c ?? 1);
    }

    private int Callee(int? c)
    {
        return c ?? 1;
    }
}");

        [Fact]
        public Task TestCoalesceExpressionAsRightValue()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public string Caller(string c)
    {
        return c ?? Cal[||]lee(null);
    }

    private string Callee(string c2)
    {
        return c2 ?? ""Hello"";
    }
}",
                @"
public class TestClass
{
    public string Caller(string c)
    {
        return c ?? null ?? ""Hello"";
    }

    private string Callee(string c2)
    {
        return c2 ?? ""Hello"";
    }
}");

        [Fact]
        public Task TestSimpleMemberAccessExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public void Caller()
    {
        var x = C[||]allee().Length;
    }

    private string Callee() => ""H"" + ""L"";
}",
                @"
public class TestClass
{
    public void Caller()
    {
        var x = (""H"" + ""L"").Length;
    }

    private string Callee() => ""H"" + ""L"";
}");

        [Fact]
        public Task TestElementAccessExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public void Caller()
    {
        var x = C[||]allee()[0];
    }

    private string Callee() => ""H"" + ""L"";
}",
                @"
public class TestClass
{
    public void Caller()
    {
        var x = (""H"" + ""L"")[0];
    }

    private string Callee() => ""H"" + ""L"";
}");

        [Fact(Skip = "No Precedence support")]
        public Task TestSwitchExpression()
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass
{
    private void Calller()
    {
        var x = C[||]allee() switch
        {
            1 => 1,
            2 => 2,
            _ => 3
        };
    }

    private int Callee() => 1 + 2;
}",
                @"
public class TestClass
{
    private void Calller()
    {
        var x = (1 + 2) switch
        {
            1 => 1,
            2 => 2,
            _ => 3
        };
    }

    private int Callee() => 1 + 2;
}");

        [Fact]
        public Task TestConditionalExpressionSyntax()
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass
{
    private void Calller(int x)
    {
        var z = true;
        var z1 = false;
        var y = z ? Callee(x) : z1 ? 1 : Ca[||]llee(x);
    }

    private int Callee(int x) => x = 1;
}",
                @"
public class TestClass
{
    private void Calller(int x)
    {
        var z = true;
        var z1 = false;
        var y = z ? Callee(x) : z1 ? 1 : (x = 1);
    }

    private int Callee(int x) => x = 1;
}");

        [Fact(Skip = "No Precedence support")]
        public Task TestSuppressNullableWarningExpression()
            => TestVerifier.TestInRegularAndScript1Async(@"
#nullable enable
public class TestClass
{
    private object Calller(int x)
    {
        return Ca[||]llee(x)!;
    }

    private object Callee(int x) => x = 1;
}",
                @"
#nullable enable
public class TestClass
{
    private object Calller(int x)
    {
        return (x = 1)!;
    }

    private object Callee(int x) => x = 1;
}");

        [Fact]
        public Task TestSimpleAssignmentExpression()
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass
{
    private int Calller(int x)
    {
        return Ca[||]llee(x) + 1;
    }

    private int Callee(int x) => x = 1;
}",
                @"
public class TestClass
{
    private int Calller(int x)
    {
        return (x = 1) + 1;
    }

    private int Callee(int x) => x = 1;
}");

        [Fact]
        public Task TestConditionalAccessExpression()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public void Caller()
    {
        var x = Cal[||]lee()?.Length;
    }

    private string Callee() => ""Hello"" + ""World"";
}",
                @"
public class TestClass
{
    public void Caller()
    {
        var x = (""Hello"" + ""World"")?.Length;
    }

    private string Callee() => ""Hello"" + ""World"";
}");

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        [InlineData("%")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        [InlineData(">>")]
        [InlineData("<<")]
        public Task TestAssignmentExpression(string op)
            => TestVerifier.TestInRegularAndScript1Async(@"
public class TestClass
{
    private int Calller(int x)
    {
        return 10 - Ca[||]llee(x);
    }

    private int Callee(int x) => x (op)= 1;
}".Replace("(op)", op),
                @"
public class TestClass
{
    private int Calller(int x)
    {
        return 10 - (x (op)= 1);
    }

    private int Callee(int x) => x (op)= 1;
}".Replace("(op)", op));

        #endregion
    }
}
