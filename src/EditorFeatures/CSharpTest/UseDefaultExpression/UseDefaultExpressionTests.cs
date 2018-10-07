// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseDefaultExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseDefaultExpression
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultExpression)]
    public sealed class UseDefaultExpressionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpUseDefaultExpressionCodeFixProvider());

        private static readonly TestParameters s_csharpLatest =
            new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

        private static readonly TestParameters s_csharp7_1 =
            new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1));

        private static readonly TestParameters s_csharp7_0 =
            new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7));

        [Fact]
        public async Task TestCSharpLatest_InCaseSwitchLabel()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (0) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch (0) { case default(int): }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_InCasePatternSwitchLabel()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (0) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (0) { case default(int) when true: }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_InIsPattern()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (true is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (true is default(bool)) { }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_NotInsideExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int i = [||]default;
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (0) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch (0) { case default(int): }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_InParentheses()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (0) { case ([||]default): }
    }
}",
@"class C
{
    void M()
    {
        switch (0) { case (default(int)): }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_NotInsideCast()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (0) { case (int)[||]default: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_NotOnDefaultExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (0) { case [||]default(int): }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_InvalidType()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch () { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch () { case default(object): }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_TupleType()
        {
            // Note that the default value of a tuple is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch ((0, true)) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch ((0, true)) { case default((int, bool)): }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (0) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (0) { case default(int) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_InParentheses()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (0) { case ([||]default) when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (0) { case (default(int)) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_NotInsideCast()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (0) { case (int)[||]default when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_NotOnDefaultExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (0) { case [||]default(int) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_InvalidType()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch () { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch () { case default(object) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_TupleType()
        {
            // Note that the default value of a tuple is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch ((0, true)) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch ((0, true)) { case default((int, bool)) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (true is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (true is default(bool)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_InParentheses()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (true is ([||]default)) { }
    }
}",
@"class C
{
    void M()
    {
        if (true is (default(bool))) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_NotInsideCast()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (true is (bool)[||]default) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_NotOnDefaultExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (true is [||]default(bool)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_InvalidType1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (null is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (null is default(object)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_InvalidType2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (default is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (default is default(object)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_InvalidType3()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (() => { } is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (() => { } is default(object)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_AnonymousType()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (new { a = 0 } is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (new { a = 0 } is default(object)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_QualifiedType()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        System.Action value = null;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        System.Action value = null;
        if (value is default(System.Action)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_TupleType()
        {
            // Note that the default value of a tuple is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if ((0, true) is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if ((0, true) is default((int, bool))) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Trivia()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (true is
            /*a*/ [||]default /*b*/) { }
    }
}",
@"class C
{
    void M()
    {
        if (true is
            /*a*/ default(bool) /*b*/) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_NotInsideExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int i = [||]default;
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_NotInsideExpression_InvalidType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var v = [||]default;
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_0_InCaseSwitchLabel()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (0) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch (0) { case default(int): }
    }
}", parameters: s_csharp7_0);
        }

        [Fact]
        public async Task TestCSharp7_0_InCasePatternSwitchLabel()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (0) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (0) { case default(int) when true: }
    }
}", parameters: s_csharp7_0);
        }

        [Fact]
        public async Task TestCSharp7_0_InIsPattern()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (true is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (true is default(bool)) { }
    }
}", parameters: s_csharp7_0);
        }

        [Fact]
        public async Task TestCSharp7_0_InsideExpression()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int i = [||]default;
    }
}",
@"class C
{
    void M()
    {
        int i = default(int);
    }
}", parameters: s_csharp7_0);
        }

        [Fact]
        public async Task TestCSharp7_0_InsideExpression_InvalidType()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        var v = [||]default;
    }
}",
@"class C
{
    void M()
    {
        var v = default(object);
    }
}", parameters: s_csharp7_0);
        }

        [Fact]
        public async Task TestCSharp7_0_InsideExpression_AmbiguousType()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        System.Console.WriteLine([||]default);
    }
}",
@"class C
{
    void M()
    {
        System.Console.WriteLine(default(object));
    }
}", parameters: s_csharp7_0);
        }

        [Fact]
        public async Task TestCSharp7_0_NotOnDifferentFeatureIntroducedIn7_1_InferredTupleNames()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int value = 0;
        System.Console.WriteLine((value, 0).[||]value);
    }
}", parameters: s_csharp7_0);

            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int value = 0;
        System.Console.WriteLine((value, 0).[|value|]);
    }
}", parameters: s_csharp7_0);
        }

        [Fact]
        public async Task TestCSharp7_0_NotOnDifferentFeatureIntroducedIn7_1_GenericPatternMatching()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M<T>(T t)
    {
        if (t is [||]string s)
        {
        }
    }
}", parameters: s_csharp7_0);

            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M<T>(T t)
    {
        if (t is [|string|] s)
        {
        }
    }
}", parameters: s_csharp7_0);
        }

        [Fact]
        public async Task TestCSharp6Lower_InsideExpression()
        {
            foreach (var languageVersion in new[] {
                LanguageVersion.CSharp6,
                LanguageVersion.CSharp5,
                LanguageVersion.CSharp4,
                LanguageVersion.CSharp3,
                LanguageVersion.CSharp2,
                })
            {
                await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int i = [||]default;
    }
}",
@"class C
{
    void M()
    {
        int i = default(int);
    }
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion)));
            }
        }

        [Fact]
        public async Task TestCSharp1_NotInsideExpression()
        {
            // default expressions are not available in C# 1
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int i = [||]default;
    }
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp1)));
        }
    }
}
