// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.DeclareAsNullable;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.DeclareAsNullable
{
    // TODO:
    // a.b.c.s$$ == null
    // somet$$hing?.x
    // symbol not from this project
    // x is null
    // (object)x == null
    // ReferenceEquals and other equality methods

    [Trait(Traits.Feature, Traits.Features.CodeActionsPossiblyDeclareAsNullable)]
    public class PossiblyDeclareAsNullableRefactoringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new PossiblyDeclareAsNullableCodeRefactoringProvider();

        private static readonly TestParameters s_nullableFeature = new TestParameters(parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));

        [Fact]
        public async Task TestParameterEqualsNull()
        {
            var code = @"
class C
{
    static void M(string s)
    {
        if (s [|==|] null)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    static void M(string? s)
    {
        if (s == null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestAlreadyNullableStringType()
        {
            var code = @"
class C
{
    static void M(string? s)
    {
        if (s [|==|] null)
        {
            return;
        }
    }
}";

            await TestMissingInRegularAndScriptAsync(code, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestNullableValueType()
        {
            var code = @"
class C
{
    static void M(int? s)
    {
        if (s [|==|] null)
        {
            return;
        }
    }
}";

            await TestMissingInRegularAndScriptAsync(code, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestCursorOnEquals()
        {
            var code = @"
class C
{
    static void M(string s)
    {
        if (s ==[||] null)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    static void M(string? s)
    {
        if (s == null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestCursorOnNull()
        {
            var code = @"
class C
{
    static void M(string s)
    {
        if (s == [||]null)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    static void M(string? s)
    {
        if (s == null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestParameterNotEqualsNull()
        {
            var code = @"
class C
{
    static void M(string s)
    {
        if (s [|!=|] null)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    static void M(string? s)
    {
        if (s != null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestNullEqualsParameter()
        {
            var code = @"
class C
{
    static void M(string s)
    {
        if (null [|==|] s)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    static void M(string? s)
    {
        if (null == s)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestLocalEqualsNull()
        {
            var code = @"
class C
{
    static void M()
    {
        string s = M2();
        if (s [|==|] null)
        {
            return;
        }
    }
    static string M2() => throw null;
}";

            var expected = @"
class C
{
    static void M()
    {
        string? s = M2();
        if (s == null)
        {
            return;
        }
    }
    static string M2() => throw null;
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestFieldEqualsNull()
        {
            var code = @"
class C
{
    string field;
    static void M(C c)
    {
        if (c.field [|==|] null)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    string? field;
    static void M(C c)
    {
        if (c.field == null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestMultiLocalEqualsNull()
        {
            var code = @"
class C
{
    static void M()
    {
        string s = M2(), y = M2();
        if (s [|==|] null)
        {
            return;
        }
    }
    static string M2() => throw null;
}";

            await TestMissingInRegularAndScriptAsync(code, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestPropertyEqualsNull()
        {
            var code = @"
class C
{
    string s { get; set; }
    static void M()
    {
        if (s [|==|] null)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    string? s { get; set; }
    static void M()
    {
        if (s == null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestMethodEqualsNull()
        {
            var code = @"
class C
{
    static void M()
    {
        if (M2() [|==|] null)
        {
            return;
        }
    }
    static string M2() => throw null;
}";

            var expected = @"
class C
{
    static void M()
    {
        if (M2() == null)
        {
            return;
        }
    }
    static string? M2() => throw null;
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestGenericMethodEqualsNull()
        {
            var code = @"
class C
{
    static void M()
    {
        if (M2<string>() [|==|] null)
        {
            return;
        }
    }
    static T M2<T>() => throw null;
}";

            await TestMissingInRegularAndScriptAsync(code, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task TestPartialMethodEqualsNull()
        {
            var code = @"
partial class C
{
    void M()
    {
        if (M2() [|==|] null)
        {
            return;
        }
    }
    partial string M2();
}
partial class C
{
    partial string M2() => throw null;
}";

            await TestMissingInRegularAndScriptAsync(code, parameters: s_nullableFeature);
        }
    }
}
