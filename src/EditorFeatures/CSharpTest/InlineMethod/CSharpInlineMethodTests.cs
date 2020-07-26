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
        public Task TestSingleStatementWithoutAnyExtraChange()
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
                // TODO: Handle the indentation correctly. 
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
        public Task TestInlineMethodWithIdentiferReplacement()
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
        Cal[||]lee(SomeCaculation(r1), SomeCaculation(r2))
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
        Ca[||]llee(SomeCaculation(s1), SomeCaculation(s2))
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
        float s3 = SomeCaculation(r1);
        float s4 = SomeCaculation(r2);
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
        public Task InlineMethodWithVariableExtrationForOut()
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
}
",
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
}
");
    }
}
