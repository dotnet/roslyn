// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    using VerifyCS = CSharpCodeFixVerifier<
        UseExpressionBodyDiagnosticAnalyzer,
        UseExpressionBodyCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
    public class UseExpressionBodyForAccessorsTests
    {
        private static async Task TestWithUseExpressionBody(
            string code,
            string fixedCode,
            LanguageVersion version = LanguageVersion.CSharp8)
        {
            var test = new VerifyCS.Test
            {
                ReferenceAssemblies = version == LanguageVersion.CSharp9 ? ReferenceAssemblies.Net.Net50 : ReferenceAssemblies.Default,
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = version,
                Options =
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenPossible  },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.Never },
                },
            };

            await test.RunAsync();
        }

        private static async Task TestWithUseExpressionBodyIncludingPropertiesAndIndexers(string code, string fixedCode, LanguageVersion version = LanguageVersion.CSharp8)
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = version == LanguageVersion.CSharp9 ? ReferenceAssemblies.Net.Net50 : ReferenceAssemblies.Default,
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = version,
                Options =
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenPossible  },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenPossible },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.WhenPossible },
                }
            }.RunAsync();
        }

        private static async Task TestWithUseBlockBodyIncludingPropertiesAndIndexers(string code, string fixedCode, LanguageVersion version = LanguageVersion.CSharp8)
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = version == LanguageVersion.CSharp9 ? ReferenceAssemblies.Net.Net50 : ReferenceAssemblies.Default,
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = version,
                Options =
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never  },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.Never },
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestUseExpressionBody1()
        {
            var code = """
                class C
                {
                    int Bar() { return 0; }

                    int Goo
                    {
                        {|IDE0027:get
                        {
                            return Bar();
                        }|}
                    }
                }
                """;
            var fixedCode = """
                class C
                {
                    int Bar() { return 0; }

                    int Goo
                    {
                        get => Bar();
                    }
                }
                """;
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact]
        public async Task TestUpdatePropertyInsteadOfAccessor()
        {
            // TODO: Should this test move to properties tests?
            var code = """
                class C
                {
                    int Bar() { return 0; }

                    {|IDE0025:int Goo
                    {
                        get
                        {
                            return Bar();
                        }
                    }|}
                }
                """;
            var fixedCode = """
                class C
                {
                    int Bar() { return 0; }

                    int Goo => Bar();
                }
                """;
            await TestWithUseExpressionBodyIncludingPropertiesAndIndexers(code, fixedCode);
        }

        [Fact]
        public async Task TestOnIndexer1()
        {
            var code = """
                class C
                {
                    int Bar() { return 0; }

                    int this[int i]
                    {
                        {|IDE0027:get
                        {
                            return Bar();
                        }|}
                    }
                }
                """;
            var fixedCode = """
                class C
                {
                    int Bar() { return 0; }

                    int this[int i]
                    {
                        get => Bar();
                    }
                }
                """;
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact]
        public async Task TestUpdateIndexerIfIndexerAndAccessorCanBeUpdated()
        {
            // TODO: Should this test move to indexers tests?
            var code = """
                class C
                {
                    int Bar() { return 0; }

                    {|IDE0026:int this[int i]
                    {
                        get
                        {
                            return Bar();
                        }
                    }|}
                }
                """;
            var fixedCode = """
                class C
                {
                    int Bar() { return 0; }

                    int this[int i] => Bar();
                }
                """;
            await TestWithUseExpressionBodyIncludingPropertiesAndIndexers(code, fixedCode);
        }

        [Fact]
        public async Task TestOnSetter1()
        {
            var code = """
                class C
                {
                    void Bar() { }

                    int Goo
                    {
                        {|IDE0027:set
                        {
                            Bar();
                        }|}
                    }
                }
                """;
            var fixedCode = """
                class C
                {
                    void Bar() { }

                    int Goo
                    {
                        set => Bar();
                    }
                }
                """;
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact]
        public async Task TestOnInit1()
        {
            var code =
                """
                class C
                {
                    int Goo
                    {
                        {|IDE0027:init
                        {
                            Bar();
                        }|}
                    }

                    int Bar() { return 0; }
                }
                """;
            var fixedCode =
                """
                class C
                {
                    int Goo
                    {
                        init => Bar();
                    }

                    int Bar() { return 0; }
                }
                """;
            await TestWithUseExpressionBody(code, fixedCode, LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestMissingWithOnlySetter()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                class C
                {
                    void Bar() { }

                    int Goo
                    {
                        set => Bar();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithOnlyInit()
        {
            var code =
                """
                class C
                {
                    int Goo
                    {
                        init => Bar();
                    }

                    int Bar() { return 0; }
                }
                """;

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = code,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact]
        public async Task TestUseExpressionBody3()
        {
            var code = """
                using System;

                class C
                {
                    int Goo
                    {
                        {|IDE0027:get
                        {
                            throw new NotImplementedException();
                        }|}
                    }
                }
                """;
            var fixedCode = """
                using System;

                class C
                {
                    int Goo
                    {
                        get => throw new NotImplementedException();
                    }
                }
                """;
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact]
        public async Task TestUseExpressionBody4()
        {
            var code = """
                using System;

                class C
                {
                    int Goo
                    {
                        {|IDE0027:get
                        {
                            throw new NotImplementedException(); // comment
                        }|}
                    }
                }
                """;
            var fixedCode = """
                using System;

                class C
                {
                    int Goo
                    {
                        get => throw new NotImplementedException(); // comment
                    }
                }
                """;
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59255")]
        public async Task TestUseExpressionBody5()
        {
            var code = """
                using System;

                class C
                {
                    event EventHandler Goo
                    {
                        {|IDE0027:add
                        {
                            throw new NotImplementedException();
                        }|}

                        {|IDE0027:remove
                        {
                            throw new NotImplementedException();
                        }|}
                    }
                }
                """;
            var fixedCode = """
                using System;

                class C
                {
                    event EventHandler Goo
                    {
                        add => throw new NotImplementedException();

                        remove => throw new NotImplementedException();
                    }
                }
                """;
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact]
        public async Task TestUseBlockBody1()
        {
            var code = """
                class C
                {
                    int Bar() { return 0; }

                    int Goo
                    {
                        {|IDE0027:get => Bar();|}
                    }
                }
                """;
            var fixedCode = """
                class C
                {
                    int Bar() { return 0; }

                    int Goo
                    {
                        get
                        {
                            return Bar();
                        }
                    }
                }
                """;
            await TestWithUseBlockBodyIncludingPropertiesAndIndexers(code, fixedCode);
        }

        [Fact]
        public async Task TestUseBlockBodyForSetter1()
        {
            var code = """
                class C
                {
                    void Bar() { }

                    int Goo
                    {
                        {|IDE0027:set => Bar();|}
                        }
                    }
                """;
            var fixedCode = """
                class C
                {
                    void Bar() { }

                    int Goo
                    {
                        set
                        {
                            Bar();
                        }
                    }
                }
                """;
            await TestWithUseBlockBodyIncludingPropertiesAndIndexers(code, fixedCode);
        }

        [Fact]
        public async Task TestUseBlockBodyForInit1()
        {
            var code =
                """
                class C
                {
                    int Goo
                    {
                        {|IDE0027:init => Bar();|}
                        }

                    int Bar() { return 0; }
                    }
                """;
            var fixedCode =
                """
                class C
                {
                    int Goo
                    {
                        init
                        {
                            Bar();
                        }
                    }

                    int Bar() { return 0; }
                    }
                """;

            await TestWithUseBlockBodyIncludingPropertiesAndIndexers(code, fixedCode, LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestUseBlockBody3()
        {
            var code = """
                using System;

                class C
                {
                    int Goo
                    {
                        {|IDE0027:get => throw new NotImplementedException();|}
                        }
                    }
                """;
            var fixedCode = """
                using System;

                class C
                {
                    int Goo
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """;
            await TestWithUseBlockBodyIncludingPropertiesAndIndexers(code, fixedCode);
        }

        [Fact]
        public async Task TestUseBlockBody4()
        {
            var code = """
                using System;

                class C
                {
                    int Goo
                    {
                        {|IDE0027:get => throw new NotImplementedException();|} // comment
                    }
                }
                """;
            var fixedCode = """
                using System;

                class C
                {
                    int Goo
                    {
                        get
                        {
                            throw new NotImplementedException(); // comment
                        }
                    }
                }
                """;
            await TestWithUseBlockBodyIncludingPropertiesAndIndexers(code, fixedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31308")]
        public async Task TestUseBlockBody5()
        {
            var code = """
                class C
                {
                    C this[int index]
                    {
                        get => default;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                Options =
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenOnSingleLine, NotificationOption2.None },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenOnSingleLine, NotificationOption2.None },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.WhenOnSingleLine, NotificationOption2.None },
                }
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59255")]
        public async Task TestUseBlockBody6()
        {
            var code = """
                using System;

                class C
                {
                    event EventHandler Goo
                    {
                        {|IDE0027:add => throw new NotImplementedException();|}
                        {|IDE0027:remove => throw new NotImplementedException();|}
                        }
                    }
                """;
            var fixedCode = """
                using System;

                class C
                {
                    event EventHandler Goo
                    {
                        add
                        {
                            throw new NotImplementedException();
                        }

                        remove
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """;
            await TestWithUseBlockBodyIncludingPropertiesAndIndexers(code, fixedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20350")]
        public async Task TestAccessorListFormatting()
        {
            var code = """
                class C
                {
                    int Bar() { return 0; }

                    int Goo { {|IDE0027:get => Bar();|} }
                }
                """;
            var fixedCode = """
                class C
                {
                    int Bar() { return 0; }

                    int Goo
                    {
                        get
                        {
                            return Bar();
                        }
                    }
                }
                """;
            await TestWithUseBlockBodyIncludingPropertiesAndIndexers(code, fixedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20350")]
        public async Task TestAccessorListFormatting_FixAll()
        {
            var code = """
                class C
                {
                    int Bar() { return 0; }

                    int Goo { {|IDE0027:get => Bar();|} {|IDE0027:set => Bar();|} }
                }
                """;
            var fixedCode = """
                class C
                {
                    int Bar() { return 0; }

                    int Goo
                    {
                        get { return Bar(); }
                        set
                        {
                            Bar();
                        }
                    }
                }
                """;
            var batchFixedCode = """
                class C
                {
                    int Bar() { return 0; }

                    int Goo
                    {
                        get
                        {
                            return Bar();
                        }

                        set
                        {
                            Bar();
                        }
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                BatchFixedCode = batchFixedCode,
                Options =
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never  },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.Never },
                },
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20350")]
        public async Task TestAccessorListFormatting_FixAll2()
        {
            var code =
                """
                class C
                {
                    int Goo { {|IDE0027:get => Bar();|} {|IDE0027:init => Bar();|} }

                    int Bar() { return 0; }
                }
                """;
            var fixedCode =
                """
                class C
                {
                    int Goo
                    {
                        get { return Bar(); }
                        init
                        {
                            Bar();
                        }
                    }

                    int Bar() { return 0; }
                }
                """;
            var batchFixedCode =
                """
                class C
                {
                    int Goo
                    {
                        get
                        {
                            return Bar();
                        }

                        init
                        {
                            Bar();
                        }
                    }

                    int Bar() { return 0; }
                }
                """;

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = code,
                FixedCode = fixedCode,
                BatchFixedCode = batchFixedCode,
                LanguageVersion = LanguageVersion.CSharp9,
                Options =
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.Never },
                }
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
        public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp7()
        {
            var code = """
                using System;
                class C
                {
                    int Goo { {|IDE0027:get {|CS8059:=>|} {|CS8059:throw|} new NotImplementedException();|} }
                }
                """;
            var fixedCode = """
                using System;
                class C
                {
                    int Goo
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """;
            await TestWithUseExpressionBody(code, fixedCode, LanguageVersion.CSharp6);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
        public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp7_FixAll()
        {
            var code = """
                using System;
                class C
                {
                    int Goo { {|IDE0027:get {|CS8059:=>|} {|CS8059:throw|} new NotImplementedException();|} }
                    int Bar { {|IDE0027:get {|CS8059:=>|} {|CS8059:throw|} new NotImplementedException();|} }
                }
                """;
            var fixedCode = """
                using System;
                class C
                {
                    int Goo
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }
                    int Bar
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """;
            await TestWithUseExpressionBody(code, fixedCode, LanguageVersion.CSharp6);
        }
    }
}
