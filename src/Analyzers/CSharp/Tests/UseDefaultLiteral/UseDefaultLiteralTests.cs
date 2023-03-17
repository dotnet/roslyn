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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseDefaultLiteral
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpUseDefaultLiteralDiagnosticAnalyzer,
        CSharpUseDefaultLiteralCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
    public class UseDefaultLiteralTests
    {
        [Fact]
        public async Task TestNotInCSharp7()
        {
            var code = """
                class C
                {
                    void Goo(string s = default(string))
                    {
                    }
                }
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7
            }.RunAsync();
        }

        [Fact]
        public async Task TestInParameterList()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestInIfCheck()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestInReturnStatement()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestInReturnStatement2()
        {
            var code = """
                class C
                {
                    string Goo()
                    {
                        return {|CS0029:default(int)|};
                    }
                }
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestInLambda1()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestInLambda2()
        {
            var code = """
                using System;

                class C
                {
                    void Goo()
                    {
                        Func<string> f = () => {|CS1662:{|CS0029:default(int)|}|};
                    }
                }
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestInLocalInitializer()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestInLocalInitializer2()
        {
            var code = """
                class C
                {
                    void Goo()
                    {
                        string s = {|CS0029:default(int)|};
                    }
                }
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotForVar()
        {
            var code = """
                class C
                {
                    void Goo()
                    {
                        var s = default(string);
                    }
                }
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestInInvocationExpression()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestNotWithMultipleOverloads()
        {
            var code = """
                class C
                {
                    void Goo()
                    {
                        Bar(default(string));
                    }

                    void Bar(string s) { }
                    void Bar(int i) { }
                }
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestLeftSideOfTernary()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestRightSideOfTernary()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestFixAll1()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestFixAll2()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestFixAll3()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestDoNotOfferIfTypeWouldChange()
        {
            var code = """
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
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestDoNotOfferIfTypeWouldChange2()
        {
            var code = """
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
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestOnShadowedMethod()
        {
            await new VerifyCS.Test
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25456")]
        public async Task TestNotInSwitchCase()
        {
            var code = """
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
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotInSwitchCase_InsideParentheses()
        {
            var code = """
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
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestInSwitchCase_InsideCast()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestNotInPatternSwitchCase()
        {
            var code = """
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
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotInPatternSwitchCase_InsideParentheses()
        {
            var code = """
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
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestInPatternSwitchCase_InsideCast()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestInPatternSwitchCase_InsideWhenClause()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task TestNotInPatternIs()
        {
            var code = """
                class C
                {
                    void M()
                    {
                        if (true is default(bool));
                    }
                }
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotInPatternIs_InsideParentheses()
        {
            var code = """
                class C
                {
                    void M()
                    {
                        if (true is (default(bool)));
                    }
                }
                """;

            await new VerifyCS.Test()
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp7_1
            }.RunAsync();
        }

        [Fact]
        public async Task TestInPatternIs_InsideCast()
        {
            await new VerifyCS.Test
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
    }
}
