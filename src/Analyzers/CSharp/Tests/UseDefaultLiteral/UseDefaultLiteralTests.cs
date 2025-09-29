// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseDefaultLiteral;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseDefaultLiteralDiagnosticAnalyzer,
    CSharpUseDefaultLiteralCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
public sealed class UseDefaultLiteralTests
{
    [Fact]
    public Task TestNotInCSharp7()
        => new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                void Goo(string s = default(string))
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7
        }.RunAsync();

    [Fact]
    public Task TestInParameterList()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void Goo(string s = [|default(string)|])
                    {
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(string s = default)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestInIfCheck()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void Goo(string s)
                    {
                        if (s == [|default(string)|]) { }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(string s)
                    {
                        if (s == default) { }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestInReturnStatement()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string Goo()
                    {
                        return [|default(string)|];
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    string Goo()
                    {
                        return default;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestInReturnStatement2()
        => new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                string Goo()
                {
                    return {|CS0029:default(int)|};
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestInLambda1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<string> f = () => [|default(string)|];
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<string> f = () => default;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestInLambda2()
        => new VerifyCS.Test()
        {
            TestCode = """
            using System;

            class C
            {
                void Goo()
                {
                    Func<string> f = () => {|CS1662:{|CS0029:default(int)|}|};
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestInLocalInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void Goo()
                    {
                        string s = [|default(string)|];
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo()
                    {
                        string s = default;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestInLocalInitializer2()
        => new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                void Goo()
                {
                    string s = {|CS0029:default(int)|};
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestNotForVar()
        => new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                void Goo()
                {
                    var s = default(string);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestInInvocationExpression()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void Goo()
                    {
                        Bar([|default(string)|]);
                    }

                    void Bar(string s) { }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo()
                    {
                        Bar(default);
                    }

                    void Bar(string s) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestNotWithMultipleOverloads()
        => new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                void Goo()
                {
                    Bar(default(string));
                }

                void Bar(string s) { }
                void Bar(int i) { }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestLeftSideOfTernary()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void Goo(bool b)
                    {
                        var v = b ? [|default(string)|] : [|default(string)|];
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(bool b)
                    {
                        var v = b ? default : default(string);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1,
            DiagnosticSelector = d => d[0],
            CodeFixTestBehaviors = Testing.CodeFixTestBehaviors.FixOne | Testing.CodeFixTestBehaviors.SkipFixAllCheck
        }.RunAsync();

    [Fact]
    public Task TestRightSideOfTernary()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void Goo(bool b)
                    {
                        var v = b ? [|default(string)|] : [|default(string)|];
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(bool b)
                    {
                        var v = b ? default(string) : default;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1,
            DiagnosticSelector = d => d[1],
            CodeFixTestBehaviors = Testing.CodeFixTestBehaviors.FixOne | Testing.CodeFixTestBehaviors.SkipFixAllCheck
        }.RunAsync();

    [Fact]
    public Task TestFixAll1()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void Goo()
                    {
                        string s1 = [|default(string)|];
                        string s2 = [|default(string)|];
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo()
                    {
                        string s1 = default;
                        string s2 = default;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestFixAll2()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void Goo(bool b)
                    {
                        string s1 = b ? [|default(string)|] : [|default(string)|];
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(bool b)
                    {
                        string s1 = b ? default : default(string);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestFixAll3()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void Goo()
                    {
                        string s1 = [|default(string)|];
                        string s2 = {|CS0029:default(int)|};
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo()
                    {
                        string s1 = default;
                        string s2 = {|CS0029:default(int)|};
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestDoNotOfferIfTypeWouldChange()
        => new VerifyCS.Test()
        {
            TestCode = """
            struct S
            {
                void M()
                {
                    var s = new S();
                    s.Equals(default(S));
                }

                public override bool Equals(object obj)
                {
                    return base.Equals(obj);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestDoNotOfferIfTypeWouldChange2()
        => new VerifyCS.Test()
        {
            TestCode = """
            struct S<T>
            {
                void M()
                {
                    var s = new S<int>();
                    s.Equals(default(S<int>));
                }

                public override bool Equals(object obj)
                {
                    return base.Equals(obj);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestOnShadowedMethod()
        => new VerifyCS.Test
        {
            TestCode = """
                struct S
                {
                    void M()
                    {
                        var s = new S();
                        s.Equals([|default(S)|]);
                    }

                    public new bool Equals(S s) => true;
                }
                """,
            FixedCode = """
                struct S
                {
                    void M()
                    {
                        var s = new S();
                        s.Equals(default);
                    }

                    public new bool Equals(S s) => true;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25456")]
    public Task TestNotInSwitchCase()
        => new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                void M()
                {
                    switch (true)
                    {
                        case default(bool):
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestNotInSwitchCase_InsideParentheses()
        => new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                void M()
                {
                    switch (true)
                    {
                        case (default(bool)):
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestInSwitchCase_InsideCast()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        switch (true)
                        {
                            case (bool)[|default(bool)|]:
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        switch (true)
                        {
                            case (bool)default:
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestNotInPatternSwitchCase()
        => new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                void M()
                {
                    switch (true)
                    {
                        case default(bool) when true:
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestNotInPatternSwitchCase_InsideParentheses()
        => new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                void M()
                {
                    switch (true)
                    {
                        case (default(bool)) when true:
                    }
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestInPatternSwitchCase_InsideCast()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        switch (true)
                        {
                            case (bool)[|default(bool)|] when true:
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        switch (true)
                        {
                            case (bool)default when true:
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestInPatternSwitchCase_InsideWhenClause()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        switch (true)
                        {
                            case default(bool) when [|default(bool)|]:
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        switch (true)
                        {
                            case default(bool) when default:
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestNotInPatternIs()
        => new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                void M()
                {
                    if (true is default(bool));
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestNotInPatternIs_InsideParentheses()
        => new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                void M()
                {
                    if (true is (default(bool)));
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();

    [Fact]
    public Task TestInPatternIs_InsideCast()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        if (true is (bool)[|default(bool)|]);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        if (true is (bool)default);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();
}
