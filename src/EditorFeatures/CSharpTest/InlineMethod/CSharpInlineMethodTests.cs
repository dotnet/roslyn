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
            private const string Marker = "##";
            public static async Task TestInRegularAndScript1Async(
                string initialMarkUp,
                string expectedMarkUp,
                bool keepInlinedMethod = true)
            {
                var test = new TestVerifier
                {
                    CodeActionIndex = keepInlinedMethod ? 0 : 1,
                    TestState =
                    {
                        Sources = { initialMarkUp }
                    },
                    FixedState =
                    {
                        Sources = { expectedMarkUp }
                    }
                };

                await test.RunAsync().ConfigureAwait(false);
            }

            public static async Task TestBothKeepAndRemoveInlinedMethodAsync(
                string initialMarkUp,
                string expectedMarkUp)
            {
                var firstMarkerIndex = expectedMarkUp.IndexOf(Marker);
                var secondMarkerIndex = expectedMarkUp.LastIndexOf(Marker);
                if (firstMarkerIndex == -1 || secondMarkerIndex == -1 || firstMarkerIndex == secondMarkerIndex)
                {
                    Assert.True(false, "Can't find proper marks that contains inlined method.");
                }

                var firstPartitionBeforeMarkUp = expectedMarkUp.Substring(0, firstMarkerIndex);
                var inlinedMethod = expectedMarkUp.Substring(firstMarkerIndex + 2, secondMarkerIndex - firstMarkerIndex - 2);
                var lastPartitionAfterMarkup = expectedMarkUp.Substring(secondMarkerIndex + 2);

                await TestInRegularAndScript1Async(
                    initialMarkUp,
                    string.Concat(
                        firstPartitionBeforeMarkUp,
                        inlinedMethod,
                        lastPartitionAfterMarkup),
                    keepInlinedMethod: true).ConfigureAwait(false);

                await TestInRegularAndScript1Async(
                    initialMarkUp,
                    string.Concat(
                        firstPartitionBeforeMarkUp,
                        lastPartitionAfterMarkup),
                    keepInlinedMethod: false).ConfigureAwait(false);
            }
        }

        [Fact]
        public Task TestInlineInvocationExpressionForExpressionStatement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
using System.Collections.Generic;
public class TestClass
{
    private void Caller(int i)
    {
        var h = new HashSet<int>();
        Ca[||]llee(i, h);
    }

    private bool Callee(int i, HashSet<int> set)
    {
        return set.Add(i);
    }
}",
                @"
using System.Collections.Generic;
public class TestClass
{
    private void Caller(int i)
    {
        var h = new HashSet<int>();
        h.Add(i);
    }
##
    private bool Callee(int i, HashSet<int> set)
    {
        return set.Add(i);
    }
##}");

        [Fact]
        public Task TestInlineMethodWithSingleStatement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private void Callee(int i, int j)
    {
        System.Console.WriteLine(i + j);
    }
##}");

        [Fact]
        public Task TestExtractArrowExpressionBody()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
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
##
    private void Callee(int i, int j)
        => System.Console.WriteLine(i + j);
##}");

        [Fact]
        public Task TestExtractExpressionBody()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee(int i, int j)
        => i + j;
##}");

        [Fact]
        public Task TestDefaultValueReplacementForExpressionStatement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private void Callee(int i = 1, string c = null, bool y = false)
    {
        System.Console.WriteLine(y ? i : (c ?? ""Hello"").Length);
    }
##}");

        [Fact]
        public Task TestDefaultValueReplacementForArrowExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private void Callee(int i = default, string c = default, bool y = false) =>
        System.Console.WriteLine(y ? i : (c ?? ""Hello"").Length);
##}");

        [Fact]
        public Task TestInlineMethodWithLiteralValue()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private void Callee(int i, char c, bool x, string y) =>
        System.Console.WriteLine(i + (int)c + (x ? 1 : y.Length));
##}");

        [Fact]
        public Task TestInlineMethodWithIdentifierReplacement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private void Callee(int i, int j = 100, string k = null)
    {
        System.Console.WriteLine(i + j + (k ?? """"));
    }
##}");

        [Fact]
        public Task TestInlineMethodWithMethodExtraction()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private void Callee(float s1, float s2)
    {
        System.Console.WriteLine(""This is s1"" + s1 + ""This is S2"" + s2);
    }
##
    public float SomeCaculation(float r)
    {
        return r * r * 3.14f;
    }
}");

        [Fact]
        public Task TestInlineMethodWithMethodExtractionAndRename()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
        float s11 = SomeCaculation(s1);
        float s21 = SomeCaculation(s2);
        System.Console.WriteLine(""This is s1"" + s11 + ""This is s2"" + s21);
    }
##
    private void Callee(float s1, float s2)
    {
        System.Console.WriteLine(""This is s1"" + s1 + ""This is s2"" + s2);
    }
##
    public float SomeCaculation(float r)
    {
        return r * r * 3.14f;
    }
}");

        [Fact]
        public Task TestInlineParamsArrayWithArrayImplicitInitializerExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
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
##
    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
##}");

        [Fact]
        public Task TestInlineParamsArrayWithArrayInitializerExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
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
##
    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
##}");

        [Fact]
        public Task TestInlineParamsArrayWithOneElement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
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
        int[] x = new int[] { 1 };
        System.Console.WriteLine(x.Length);
    }
##
    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
##}");

        [Fact]
        public Task TestInlineParamsArrayMethodWithIdentifier()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
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
##
    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
##}");
        [Fact]
        public Task TestInlineMethodWithNoElementInParamsArray()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
        int[] x = new int[] { };
        System.Console.WriteLine(x.Length);
    }
##
    private void Callee(params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
##}");

        [Fact]
        public Task TestInlineMethodWithParamsArray()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
public class TestClass
{
    private void Caller()
    {
        Ca[||]llee(""Hello"", 1, 2, 3, 4, 5, 6);
    }

    private void Callee(string z, params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
}",
                @"
public class TestClass
{
    private void Caller()
    {
        int[] x = new int[] { 1, 2, 3, 4, 5, 6 };
        System.Console.WriteLine(x.Length);
    }
##
    private void Callee(string z, params int[] x)
    {
        System.Console.WriteLine(x.Length);
    }
##}");

        [Fact]
        public Task TestInlineMethodWithVariableDeclaration1()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
        int x = 10;
    }
##
    private void Callee(out int z)
    {
        z = 10;
    }
##}");

        [Fact]
        public Task TestInlineMethodWithVariableDeclaration2()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
public class TestClass
{
    private void Caller()
    {
        Cal[||]lee(out var x, out var y, out var z);
    }

    private void Callee(out int z, out int x, out int y)
    {
        z = x = y = 10;
    }
}",
                @"
public class TestClass
{
    private void Caller()
    {
        int x1;
        int y1;
        int z1;
        x1 = y1 = z1 = 10;
    }
##
    private void Callee(out int z, out int x, out int y)
    {
        z = x = y = 10;
    }
##}");

        [Fact]
        public Task TestInlineMethodWithVariableDeclaration3()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
public class TestClass
{
    private void Caller()
    {
        Cal[||]lee(out var x);
    }

    private void Callee(out int z)
    {
        DoSometing(out z);
    }

    private void DoSometing(out int z)
    {
        z = 100;
    }
}",
                @"
public class TestClass
{
    private void Caller()
    {
        int x;
        DoSometing(out x);
    }
##
    private void Callee(out int z)
    {
        DoSometing(out z);
    }
##
    private void DoSometing(out int z)
    {
        z = 100;
    }
}");

        [Fact]
        public Task TestInlineCalleeSelf()
            => TestVerifier.TestInRegularAndScript1Async(
                @"
public class TestClass
{
    public TestClass()
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
    public TestClass()
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
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
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
        int temp = j + 1;
    }
##
    private int Callee(int i)
    {
        return i + 1;
    }
##}");

        [Fact]
        public Task TestInlineMethodWithNullCoalescingExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
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
##
    private int Callee(int i)
    {
        return i + 1;
    }
##}");

        [Fact]
        public Task TestInlineSimpleLambdaExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
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
##
    private System.Func<int, int, int> Callee() => (i, j) => i + j;
##}");

        [Fact]
        public Task TestInlineMethodWithGenericsArguments()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
        int[] i = new int[] { 1, 2, 3 };
        System.Console.WriteLine(typeof(int).Name.Length + i.Length + typeof(U).Name.Length);
    }
##
    private void Callee<T, U>(params T[] i)
    {
        System.Console.WriteLine(typeof(T).Name.Length + i.Length + typeof(U).Name.Length);
    }
##}");

        [Fact]
        public Task TestAwaitExpressionInMethod()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
    public Task<int> Caller(bool x)
    {
        System.Console.WriteLine("""");
        int j = x ? 1 : 2;
        return Task.FromResult(10 + j);
    }
##
    private async Task<int> Callee(int i, int j)
    {
        return await Task.FromResult(i + j);
    }
##}");

        [Fact]
        public Task TestAwaitExpressionInMethod2()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
using System.Threading.Tasks;
using System;
public class TestClass
{
    public void Caller(bool x)
    {
        System.Console.WriteLine("""");
        var f = C[||]allee();
    }

    private Func<Task> Callee()
    {
        return async () => await Task.Delay(100);
    }
}",
                @"
using System.Threading.Tasks;
using System;
public class TestClass
{
    public void Caller(bool x)
    {
        System.Console.WriteLine("""");
        var f = (Func<Task>)(async () => await Task.Delay(100));
    }
##
    private Func<Task> Callee()
    {
        return async () => await Task.Delay(100);
    }
##}");

        [Fact]
        public Task TestAwaitExpresssion1()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
using System.Threading.Tasks;
public class TestClass
{
    public Task Caller()
    {
        return Cal[||]lee();
    }

    private async Task Callee()
    {
        await Task.CompletedTask;
    }
}",
                @"
using System.Threading.Tasks;
public class TestClass
{
    public Task Caller()
    {
        return Task.CompletedTask;
    }
##
    private async Task Callee()
    {
        await Task.CompletedTask;
    }
##}");

        [Fact]
        public Task TestAwaitExpresssion2()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task Caller()
    {
        await Cal[||]lee().ConfigureAwait(false);
    }

    private async Task Callee() => await Task.CompletedTask;
}",
                @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task Caller()
    {
        await Task.CompletedTask.ConfigureAwait(false);
    }
##
    private async Task Callee() => await Task.CompletedTask;
##}");

        [Fact]
        public Task TestAwaitExpresssion3()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
using System.Threading.Tasks;
public class TestClass
{
    public Task<int> Caller()
    {
        return Cal[||]lee();
    }

    private async Task<int> Callee() => await Task.FromResult(1);
}",
                @"
using System.Threading.Tasks;
public class TestClass
{
    public Task<int> Caller()
    {
        return Task.FromResult(1);
    }
##
    private async Task<int> Callee() => await Task.FromResult(1);
##}");

        [Fact]
        public Task TestAwaitExpresssion4()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
using System.Threading.Tasks;
public class TestClass
{
    public void Caller()
    {
        var x = Cal[||]lee();
    }

    private async Task<int> Callee()
    {
        return await Task.FromResult(await SomeCalculation());
    }

    private async Task<int> SomeCalculation() => await Task.FromResult(10);
}",
                @"
using System.Threading.Tasks;
public class TestClass
{
    public async void Caller()
    {
        var x = Task.FromResult(await SomeCalculation());
    }
##
    private async Task<int> Callee()
    {
        return await Task.FromResult(await SomeCalculation());
    }
##
    private async Task<int> SomeCalculation() => await Task.FromResult(10);
}");

        [Fact]
        public Task TestAwaitExpressionInLambda()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
using System;
using System.Threading.Tasks;
public class TestClass
{
    private void Method()
    {
        Func<bool, Task<int>> x1 = (x) =>
        {
            System.Console.WriteLine("""");
            return Call[||]ee();
        };
    }

    private async Task<int> Callee()
    {
        return await Task.FromResult(10);
    }
}",
                @"
using System;
using System.Threading.Tasks;
public class TestClass
{
    private void Method()
    {
        Func<bool, Task<int>> x1 = (x) =>
        {
            System.Console.WriteLine("""");
            return Task.FromResult(10);
        };
    }
##
    private async Task<int> Callee()
    {
        return await Task.FromResult(10);
    }
##}");

        [Fact]
        public Task TestAwaitExpressionInLocalMethod()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
using System.Threading.Tasks;
public class TestClass
{
    private void Method()
    {
        Task<int> Caller(bool x)
        {
            System.Console.WriteLine("""");
            return Call[||]ee(10, x ? 1 : 2);
        }
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
    private void Method()
    {
        Task<int> Caller(bool x)
        {
            System.Console.WriteLine("""");
            int j = x ? 1 : 2;
            return Task.FromResult(10 + j);
        }
    }
##
    private async Task<int> Callee(int i, int j)
    {
        return await Task.FromResult(i + j);
    }
##}");
        [Fact]
        public Task TestInlineMethodForLambda()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
using System;
public class TestClass
{
    public void Caller()
    {
        Call[||]ee()(10);
    }

    private Func<int, int> Callee()
        => i => 1;
}",
                @"
using System;
public class TestClass
{
    public void Caller()
    {
        ((Func<int, int>)(i => 1))(10);
    }
##
    private Func<int, int> Callee()
        => i => 1;
##}");

        [Fact]
        public Task TestInlineWithinDoStatement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee(int i)
    {
        return i + 1;
    }
##
    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineWithinForStatement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee(int i)
    {
        return i + 1;
    }
##
    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineWithinIfStatement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee(int i)
    {
        return i + 1;
    }
##
    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineWithinLockStatement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private string Callee(int i)
    {
        return ""Hello"" + i;
    }
##
    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineWithinReturnStatement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private string Callee(int i)
    {
        return ""Hello"" + i;
    }
##
    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineMethodWithinThrowStatement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee(int i)
    {
        return i + 20;
    }
##
    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineWithinWhileStatement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee(int i)
    {
        return i + 1;
    }
##
    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineMethodWithinTryStatement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private bool Callee(System.Exception e, int i) => i == 1;
##
    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineMethodWithinYieldReturnStatement()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
public class TestClass2
{
    private System.Collections.Generic.IEnumerable<int> Calller()
    {
        yield return 1;
        yield return Cal[||]lee(SomeInt());
        yield return 3;
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
        yield return 3;
    }
##
    private int Callee(int i) => i + 10;
##
    private int SomeInt() => 10;
}");

        [Fact]
        public Task TestInlineExtensionMethod1()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
static class Program
{
    static void Main(string[] args)
    {
        var value = 0;
        value.Ge[||]tNext();
    }

    private static int GetNext(this int i)
    {
        return i + 1;
    }
}",
                @"
static class Program
{
    static void Main(string[] args)
    {
        var value = 0;
        int temp = value + 1;
    }
##
    private static int GetNext(this int i)
    {
        return i + 1;
    }
##}");

        [Fact]
        public Task TestInlineExtensionMethod2()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
static class Program
{
    static void Main(string[] args)
    {
        var x = 0.Ge[||]tNext();
    }

    private static int GetNext(this int i)
    {
        return i + 1;
    }
}",
                @"
static class Program
{
    static void Main(string[] args)
    {
        var x = 0 + 1;
    }
##
    private static int GetNext(this int i)
    {
        return i + 1;
    }
##}");

        [Fact]
        public Task TestInlineExtensionMethod3()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
static class Program
{
    static void Main(string[] args)
    {
        GetInt().Ge[||]tNext();
    }

    private static int GetInt() => 10;

    private static int GetNext(this int i)
    {
        return i + 1;
    }
}",
                @"
static class Program
{
    static void Main(string[] args)
    {
        int i = GetInt();
        int temp = i + 1;
    }

    private static int GetInt() => 10;
##
    private static int GetNext(this int i)
    {
        return i + 1;
    }
##}");

        [Fact]
        public Task TestInlineExpressionAsLeftValueInLeftAssociativeExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee(int i, int j)
    {
        return i + j;
    }
##}");

        [Fact]
        public Task TestInlineExpressionAsRightValueInRightAssociativeExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee(int i, int j)
    {
        return i + j;
    }
##}");

        [Fact]
        public Task TestAddExpressionWithMultiply()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee(int i, int j)
    {
        return i + j;
    }
##}");

        [Fact]
        public Task TestIsExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private bool Callee(int i, int j)
    {
        return i == j;
    }
##}");

        [Fact]
        public Task TestUnaryPlusOperator()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee()
    {
        return 1 + 2;
    }
##}");

        [Fact]
        public Task TestLogicalNotExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private bool Callee(int i, int j)
    {
        return i == j;
    }
##}");

        [Fact]
        public Task TestBitWiseNotExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee(int i, int j)
    {
        return i | j;
    }
##}");

        [Fact]
        public Task TestCastExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee(int i, int j)
    {
        return i + j;
    }
##}");

        [Fact]
        public Task TestIsPatternExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee()
    {
        return 1 | 2;
    }
##}");

        [Fact]
        public Task TestAsExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private object Callee(bool f)
    {
        return f ? ""Hello"" : ""World"";
    }
##}");

        [Fact]
        public Task TestCoalesceExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private int Callee(int? c)
    {
        return c ?? 1;
    }
##}");

        [Fact]
        public Task TestCoalesceExpressionAsRightValue()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private string Callee(string c2)
    {
        return c2 ?? ""Hello"";
    }
##}");

        [Fact]
        public Task TestSimpleMemberAccessExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private string Callee() => ""H"" + ""L"";
##}");

        [Fact]
        public Task TestElementAccessExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
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
##
    private string Callee() => ""H"" + ""L"";
##}");

        [Fact]
        public Task TestSwitchExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
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
##
    private int Callee() => 1 + 2;
##}");

        [Fact]
        public Task TestConditionalExpressionSyntax()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
public class TestClass
{
    private void Calller(int x)
    {
        var z = true;
        var z1 = false;
        var y = z ? 3 : z1 ? 1 : Ca[||]llee(x);
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
        var y = z ? 3 : z1 ? 1 : (x = 1);
    }
##
    private int Callee(int x) => x = 1;
##}");

        [Fact]
        public Task TestSuppressNullableWarningExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
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
##
    private object Callee(int x) => x = 1;
##}");

        [Fact]
        public Task TestSimpleAssignmentExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
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
##
    private int Callee(int x) => x = 1;
##}");

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public Task TestPreExpression(string op)
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
public class TestClass
{
    public void Caller()
    {
        int i = 1;
        Cal[||]lee(i);
    }

    private int Callee(int i)
    {
        return (op)i;
    }
}".Replace("(op)", op),
                @"
public class TestClass
{
    public void Caller()
    {
        int i = 1;
        (op)i;
    }
##
    private int Callee(int i)
    {
        return (op)i;
    }
##}".Replace("(op)", op));

        [Fact]
        public Task TestAwaitExpressionWithFireAndForgot()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
using System.Threading.Tasks;
public class TestClass
{
    public async void Caller()
    {
        Cal[||]lee();
    }
    private async Task Callee()
    {
        await Task.Delay(100);
    }
}",
                @"
using System.Threading.Tasks;
public class TestClass
{
    public async void Caller()
    {
        Task.Delay(100);
    }
##    private async Task Callee()
    {
        await Task.Delay(100);
    }
##}");

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public Task TestPostExpression(string op)
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
public class TestClass
{
    public void Caller()
    {
        int i = 1;
        Cal[||]lee(i);
    }

    private int Callee(int i)
    {
        return i(op);
    }
}".Replace("(op)", op),
                @"
public class TestClass
{
    public void Caller()
    {
        int i = 1;
        i(op);
    }
##
    private int Callee(int i)
    {
        return i(op);
    }
##}".Replace("(op)", op));

        [Fact]
        public Task TestObjectCreationExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
public class TestClass
{
    public void Caller()
    {
        Call[||]ee();
    }

    private object Callee()
    {
        return new object();
    }
}",
                @"
public class TestClass
{
    public void Caller()
    {
        new object();
    }
##
    private object Callee()
    {
        return new object();
    }
##}");
        [Fact]
        public Task TestConditionalInvocationExpression2()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
public class TestClass
{
    public void Caller()
    {
        Cal[||]lee()?.ToCharArray();
    }

    private string Callee() => ""Hello"" + ""World"";
}",
                @"
public class TestClass
{
    public void Caller()
    {
        (""Hello"" + ""World"")?.ToCharArray();
    }
##
    private string Callee() => ""Hello"" + ""World"";
##}");

        [Fact]
        public Task TestConditionalInvocationExpression1()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(
                @"
public class TestClass
{
    public void Caller()
    {
        Cal[||]lee();
    }

    private char[] Callee() => (""Hello"" + ""World"")?.ToCharArray();
}",
                @"
public class TestClass
{
    public void Caller()
    {
        (""Hello"" + ""World"")?.ToCharArray();
    }
##
    private char[] Callee() => (""Hello"" + ""World"")?.ToCharArray();
##}");

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        [InlineData("%")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        [InlineData("")]
        [InlineData(">>")]
        [InlineData("<<")]
        public Task TestAssignmentExpression(string op)
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
public class TestClass
{
    private void Calller(int x)
    {
        Ca[||]llee(x);
    }

    private int Callee(int x) => x (op)= 1;
}".Replace("(op)", op),
                @"
public class TestClass
{
    private void Calller(int x)
    {
        x (op)= 1;
    }
##
    private int Callee(int x) => x (op)= 1;
##}".Replace("(op)", op));

        [Fact]
        public Task TestInlineLambdaInsideInvocation()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
using System;
public class TestClass
{
    public void Caller()
    {
        var x = Cal[||]lee()();
    }

    private Func<int> Callee()
    {
        return () => 1;
    }
}
", @"
using System;
public class TestClass
{
    public void Caller()
    {
        var x = ((Func<int>)(() => 1))();
    }
##
    private Func<int> Callee()
    {
        return () => 1;
    }
##}
");

        [Fact]
        public Task TestInlineTypeCast()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
public class TestClass
{
    public void Caller()
    {
        var x = Cal[||]lee();
    }

    private long Callee()
    {
        return 1;
    }
}
", @"
public class TestClass
{
    public void Caller()
    {
        var x = (long)1;
    }
##
    private long Callee()
    {
        return 1;
    }
##}
");

        [Fact]
        public Task TestNestedConditionalInvocationExpression()
            => TestVerifier.TestBothKeepAndRemoveInlinedMethodAsync(@"
public class LinkedList
{
    public LinkedList Next { get; }
}

public class TestClass
{
    public void Caller()
    {
        var l = new LinkedList();
        Cal[||]lee(l);
    }

    private string Callee(LinkedList l)
    {
        return l?.Next?.Next?.Next?.Next?.ToString();
    }
}
", @"
public class LinkedList
{
    public LinkedList Next { get; }
}

public class TestClass
{
    public void Caller()
    {
        var l = new LinkedList();
        l?.Next?.Next?.Next?.Next?.ToString();
    }
##
    private string Callee(LinkedList l)
    {
        return l?.Next?.Next?.Next?.Next?.ToString();
    }
##}
");

    }
}
