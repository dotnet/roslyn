// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.NameArguments;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NameArguments
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsNameArguments)]
    public class NameArgumentsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpNameArgumentsDiagnosticAnalyzer(), new CSharpNameArgumentsCodeFixProvider());

        private static readonly CSharpParseOptions s_parseOptions =
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

        [Fact]
        public async Task TestLiteralInInvocation()
        {
            await TestAsync(
@"
class C
{
    void M(int a, int b)
    {
        M(a, [||]default);
    }
}",
@"
class C
{
    void M(int a, int b)
    {
        M(a, b: default);
    }
}", parseOptions: s_parseOptions);
        }

        [Fact]
        public async Task TestLiteralInCreation()
        {
            await TestAsync(
@"
class C
{
    C(int a, int b)
    {
        new C(a, [||]2);
    }
}",
@"
class C
{
    C(int a, int b)
    {
        new C(a, b: 2);
    }
}", parseOptions: s_parseOptions);
        }

        [Fact]
        public async Task TestLiteralInAttribute()
        {
            await TestAsync(
@"
[C(C.a, [||]""hello"")]
public class C : System.Attribute
{
    public static int a = 0;
    public C(int a, string b)
    {
    }
}",
@"
[C(C.a, b: ""hello"")]
public class C : System.Attribute
{
    public static int a = 0;
    public C(int a, string b)
    {
    }
}", parseOptions: s_parseOptions);
        }

        [Fact]
        public async Task TestLiteralInAttribute2()
        {
            await TestActionCountAsync(
@"
[C(C.a, [||]P = 2)]
public class C : System.Attribute
{
    public static int a = 0;
    public int P { get; set; }
    public C(int a)
    {
    }
}",
count: 0, parameters: new TestParameters(s_parseOptions));
        }

        [Fact]
        public async Task TestLiteralInThis()
        {
            await TestAsync(
@"
class C
{
    static int a = 1;
    C() : this(a, [||]2) { }
    C(int a, int b) { }
}",
@"
class C
{
    static int a = 1;
    C() : this(a, b: 2) { }
    C(int a, int b) { }
}", parseOptions: s_parseOptions);
        }

        [Fact]
        public async Task TestLiteralInBase()
        {
            await TestAsync(
@"
class C : B
{
    static int a = 1;
    C() : base(a, [||]2) { }
}
class B
{
    public B(int a, int b) { }
}",
@"
class C : B
{
    static int a = 1;
    C() : base(a, b: 2) { }
}
class B
{
    public B(int a, int b) { }
}", parseOptions: s_parseOptions);
        }

        [Fact]
        public async Task TestLiteralInIndexer()
        {
            await TestAsync(
@"
class C
{
    static int a = 0;
    int this[int a, int b]
    {
        get => throw null;
    }
    void M()
    {
        _ = this[a, [||]2];
    }
}",
@"
class C
{
    static int a = 0;
    int this[int a, int b]
    {
        get => throw null;
    }
    void M()
    {
        _ = this[a, b: 2];
    }
}", parseOptions: s_parseOptions);
        }

        [Fact]
        public async Task TestSecondLiteralArgumentWithCSharp7()
        {
            await TestActionCountAsync(
@"
class C
{
    void M(int a, int b)
    {
        M(a, [||]2);
    }
}", count: 0, parameters: new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7)));
        }

        [Fact]
        public async Task TestLiteralInParams()
        {
            await TestActionCountAsync(
@"
class C
{
    void M(int a, params int[] b)
    {
        M(a, [||]2);
    }
}", count: 0);
        }

        [Fact]
        public async Task TestLiteralOnMissingOverload()
        {
            await TestActionCountAsync(
@"
class C
{
    void M(int a, int b, int c)
    {
        M(a, [||]2, 3);
    }
}", count: 0);
        }

        [Fact]
        public async Task TestFixAllWithTrivia()
        {
            await TestAsync(
@"
class C
{
    void M(int a, int b, int c)
    {
        M(a, /*before*/ {|FixAllInDocument:2|} /*after*/, /*before*/ 3 /*after*/);
    }
}",
@"
class C
{
    void M(int a, int b, int c)
    {
        M(a, /*before*/ b: 2 /*after*/, /*before*/ c: 3 /*after*/);
    }
}", parseOptions: s_parseOptions);
        }

        [Fact]
        public async Task TestFixAllWithTriviaAndNewlines()
        {
            await TestAsync(
@"
class C
{
    void M(int a, int b, int c)
    {
        M(a,
            /*before*/ {|FixAllInDocument:2|} /*after*/,
            /*before*/ 3 /*after*/);
    }
}",
@"
class C
{
    void M(int a, int b, int c)
    {
        M(a,
            /*before*/ b: 2 /*after*/,
            /*before*/ c: 3 /*after*/);
    }
}", parseOptions: s_parseOptions);
        }

        [Fact]
        public async Task TestFixAllWithNestings()
        {
            await TestAsync(
@"
class C
{
    int M(int a, int b, int c)
    {
        return M(a, {|FixAllInDocument:2|}, M(a, 2, 3));
    }
}",
@"
class C
{
    int M(int a, int b, int c)
    {
        return M(a, b: 2, M(a, b: 2, c: 3));
    }
}", parseOptions: s_parseOptions);
        }

        [Fact]
        public async Task TestFixAllWithParams()
        {
            await TestAsync(
@"
class C
{
    int M(int a, params int[] b)
    {
        return M({|FixAllInDocument:1|}, 2, 3);
    }
}",
@"
class C
{
    int M(int a, params int[] b)
    {
        return M(a: 1, 2, 3);
    }
}", parseOptions: s_parseOptions);
        }
    }
}
