// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineMethod
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsInlineMethod)]
    public class CSharpInlineMethodTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => ((TestWorkspace)workspace).ExportProvider.GetExportedValue<CSharpInlineMethodRefactoringProvider>();

        [Fact]
        public Task TestInlineMethodWithSingleStatement()
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(@"
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
        return r * r * 3.14;
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
        return r * r * 3.14;
    }
}");

        [Fact]
        public Task TestInlineMethodWithMethodExtractionAndRename()
            => TestInRegularAndScript1Async(
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
        return r * r * 3.14;
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
        return r * r * 3.14;
    }
}");

        [Fact]
        public Task TestInlineParamsArrayWithArrayImplicitInitializerExpression()
            => TestInRegularAndScript1Async(@"
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
            => TestInRegularAndScript1Async(@"
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
            => TestInRegularAndScript1Async(@"
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
            => TestInRegularAndScript1Async(@"
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
        public Task TestInlineMethodWithNullCoalescingExpression()
            => TestInRegularAndScript1Async(@"
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
        public Task TestInlineMethodWithGenericsArguments()
            => TestInRegularAndScript1Async(
                @"
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
            => TestInRegularAndScript1Async(
                @"
using System.Threading.Tasks;
public class TestClass
{
    public Task Caller(bool x)
    {
        System.Console.Writeline("");
        return Call[||]ee(10, x ? 1 : 2);
    }

    private async Task Callee(int i, int j)
    {
        return await Task.CompletedTask;
    }
}",
                @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task Caller(bool x)
    {
        System.Console.Writeline("");
        int j = x ? 1 : 2;
        return await Task.CompletedTask;
    }

    private async Task Callee(int i, int j)
    {
        return await Task.CompletedTask;
    }
}");

        [Fact]
        public Task TestInlineWithinDoStatement()
            => TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        do
        {
        } while(Cal[||]lee(SomeInt()) == 1)
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
        } while(i + 1 == 1)
    }

    private int Callee(int i)
    {
        return i + 1;
    }

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineWithinForStatement()
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
                @"
public class TestClass
{
    private void Caller()
    {
        throw new Exception(Call[||]ee(SomeInt()));
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
        throw new Exception(i + 20);
    }

    private int Callee(int i)
    {
        return i + 20;
    }

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineWithinWhileStatement()
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
            @"
public class TestClass
{
    private void Calller()
    {
        try
        {
        }
        catch (Exception e) when (Ca[||]llee(e, SomeInt()))
        {
        }
    }

    private bool Callee(Exception e, int i) => i == 1;

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
        catch (Exception e) when (i == 1)
        {
        }
    }

    private bool Callee(Exception e, int i) => i == 1;

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineMethodWithinUsingStatement()
            => TestInRegularAndScript1Async(@"
public class TestClass2
{
    private class Dispose : IDisposable
    {
        void IDisposable.Dispose()
        {
        }
    }

    private void Calller()
    {
        using (var x = Cal[||]lee(SomeInt()))
        {
        }
    }

    private Dispose Callee(int i) => new Dispose();

    private int SomeInt() => 10;
}",
                @"
public class TestClass2
{
    private class Dispose : IDisposable
    {
        void IDisposable.Dispose()
        {
        }
    }

    private void Calller()
    {
        int i = SomeInt();
        using (var x = new Dispose())
        {
        }
    }

    private Dispose Callee(int i) => new Dispose();

    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineMethodWithinYieldReturnStatement()
            => TestInRegularAndScript1Async(
                @"
public class TestClass2
{
    private IEnumerable<int> Calller()
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
    private IEnumerable<int> Calller()
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public bool Caller(int i, int j)
    {
        return ~Call[||]ee(i, j);
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
        return ~(i == j);
    }

    private bool Callee(int i, int j)
    {
        return i == j;
    }
}");

        [Fact]
        public Task TestCastExpression()
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public void Calller()
    {
        var x = Cal[||]lee() as string
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
        var x = (1 | 2) as string
    }

    private int Callee()
    {
        return 1 | 2;
    }
}");

        [Fact]
        public Task TestCoalesceExpression()
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public int Caller(int? c2)
    {
        return c ?? Cal[||]lee(null);
    }

    private int Callee(int? c2)
    {
        return c2 ?? 1;
    }
}",
                @"
public class TestClass
{
    public int Caller(int? c2)
    {
        return c ?? null ?? 1;
    }

    private int Callee(int? c2)
    {
        return c2 ?? 1;
    }
}");

        [Fact]
        public Task TestSimpleMemberAccessExpression()
            => TestInRegularAndScript1Async(
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

        [Fact(Skip = "Add Support")]
        public Task TestInvocationExpression()
            => Task.CompletedTask;

        [Fact]
        public Task TestElementAccessExpression()
            => TestInRegularAndScript1Async(
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

        [Fact(Skip = "No OperatorPrecedenceService for switch expression")]
        public Task TestSwitchExpression()
            => TestInRegularAndScript1Async(@"
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
        public Task TestConditionalAccessExpression()
            => TestInRegularAndScript1Async(@"
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
            => TestInRegularAndScript1Async(@"
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
            => TestInRegularAndScript1Async(@"
public class TestClass
{
    private object Calller(int x)
    {
        return Ca[||]llee(x) + 1;
    }

    private object Callee(int x) => x = 1;
}",
                @"
public class TestClass
{
    private object Calller(int x)
    {
        return (x = 1) + 1;
    }

    private object Callee(int x) => x = 1;
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
            => TestInRegularAndScript1Async(@"
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
