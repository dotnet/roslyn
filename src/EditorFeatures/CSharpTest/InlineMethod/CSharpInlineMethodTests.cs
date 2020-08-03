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
        Cal[||]lee(10, m, k: ""Hello"")
    }

    private void Callee(int i, int j = 100, string k = null)
    {
        System.Console.WriteLine(i + j + (k ?? ""));
    }
}",
                @"
public class TestClass
{
    private void Caller(int m)
    {
        System.Console.WriteLine(10 + m + (""Hello"" ?? ""));
    }

    private void Callee(int i, int j = 100, string k = null)
    {
        System.Console.WriteLine(i + j + (k ?? ""));
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
    }
}
