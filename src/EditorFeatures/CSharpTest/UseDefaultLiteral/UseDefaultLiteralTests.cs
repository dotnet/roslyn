﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseDefaultLiteral
{
    public class UseDefaultLiteralTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseDefaultLiteralDiagnosticAnalyzer(), new CSharpUseDefaultLiteralCodeFixProvider());

        private static readonly CSharpParseOptions s_parseOptions = 
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1);

        private static readonly TestParameters s_testParameters =
            new TestParameters(parseOptions: s_parseOptions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestNotInCSharp7()
        {
            await TestMissingAsync(
@"
class C
{
    void Goo(string s = [||]default(string))
    {
    }
}", parameters: new TestParameters(
    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestInParameterList()
        {
            await TestAsync(
@"
class C
{
    void Goo(string s = [||]default(string))
    {
    }
}",
@"
class C
{
    void Goo(string s = default)
    {
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestInIfCheck()
        {
            await TestAsync(
@"
class C
{
    void Goo(string s)
    {
        if (s == [||]default(string)) { }
    }
}",
@"
class C
{
    void Goo(string s)
    {
        if (s == default) { }
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestInReturnStatement()
        {
            await TestAsync(
@"
class C
{
    string Goo()
    {
        return [||]default(string);
    }
}",
@"
class C
{
    string Goo()
    {
        return default;
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestInReturnStatement2()
        {
            await TestMissingAsync(
@"
class C
{
    string Goo()
    {
        return [||]default(int);
    }
}", parameters: s_testParameters);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestInLambda1()
        {
            await TestAsync(
@"
using System;

class C
{
    void Goo()
    {
        Func<string> f = () => [||]default(string);
    }
}",
@"
using System;

class C
{
    void Goo()
    {
        Func<string> f = () => [||]default;
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestInLambda2()
        {
            await TestMissingAsync(
@"
using System;

class C
{
    void Goo()
    {
        Func<string> f = () => [||]default(int);
    }
}", parameters: s_testParameters);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestInLocalInitializer()
        {
            await TestAsync(
@"
class C
{
    void Goo()
    {
        string s = [||]default(string);
    }
}",
@"
class C
{
    void Goo()
    {
        string s = default;
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestInLocalInitializer2()
        {
            await TestMissingAsync(
@"
class C
{
    void Goo()
    {
        string s = [||]default(int);
    }
}", parameters: s_testParameters);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestNotForVar()
        {
            await TestMissingAsync(
@"
class C
{
    void Goo()
    {
        var s = [||]default(string);
    }
}",  parameters: s_testParameters);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestInInvocationExpression()
        {
            await TestAsync(
@"
class C
{
    void Goo()
    {
        Bar([||]default(string));
    }

    void Bar(string s) { }
}",
@"
class C
{
    void Goo()
    {
        Bar(default);
    }

    void Bar(string s) { }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestNotWithMultipleOverloads()
        {
            await TestMissingAsync(
@"
class C
{
    void Goo()
    {
        Bar([||]default(string));
    }

    void Bar(string s) { }
    void Bar(int i);
}", parameters: s_testParameters);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestLeftSideOfTernary()
        {
            await TestAsync(
@"
class C
{
    void Goo(bool b)
    {
        var v = b ? [||]default(string) : default(string);
    }
}",
@"
class C
{
    void Goo(bool b)
    {
        var v = b ? default : default(string);
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestRightSideOfTernary()
        {
            await TestAsync(
@"
class C
{
    void Goo(bool b)
    {
        var v = b ? default(string) : [||]default(string);
    }
}",
@"
class C
{
    void Goo(bool b)
    {
        var v = b ? default(string) : default;
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestFixAll1()
        {
            await TestAsync(
@"
class C
{
    void Goo()
    {
        string s1 = {|FixAllInDocument:default|}(string);
        string s2 = default(string);
    }
}",
@"
class C
{
    void Goo()
    {
        string s1 = default;
        string s2 = default;
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestFixAll2()
        {
            await TestAsync(
@"
class C
{
    void Goo(bool b)
    {
        string s1 = b ? {|FixAllInDocument:default|}(string) : default(string);
    }
}",
@"
class C
{
    void Goo(bool b)
    {
        string s1 = b ? default : default(string);
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestFixAll3()
        {
            await TestAsync(
@"
class C
{
    void Goo()
    {
        string s1 = {|FixAllInDocument:default|}(string);
        string s2 = default(int);
    }
}",
@"
class C
{
    void Goo()
    {
        string s1 = default;
        string s2 = default(int);
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestDoNotOfferIfTypeWouldChange()
        {
            await TestMissingInRegularAndScriptAsync(
@"
struct S
{
    void M()
    {
        var s = new S();
        s.Equals([||]default(S));
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }
}", new TestParameters(parseOptions: s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestDoNotOfferIfTypeWouldChange2()
        {
            await TestMissingInRegularAndScriptAsync(
@"
struct S<T>
{
    void M()
    {
        var s = new S<int>();
        s.Equals([||]default(S<int>));
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }
}", new TestParameters(parseOptions: s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestOnShadowedMethod()
        {
            await TestAsync(
@"
struct S
{
    void M()
    {
        var s = new S();
        s.Equals([||]default(S));
    }

    public new bool Equals(S s) => true;
}",

@"
struct S
{
    void M()
    {
        var s = new S();
        s.Equals(default);
    }

    public new bool Equals(S s) => true;
}", parseOptions: s_parseOptions);
        }
    }
}
