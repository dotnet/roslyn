// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.BraceCompletion.AbstractBraceCompletionService;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    [Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
    public sealed class AutomaticLiteralCompletionTests : AbstractAutomaticBraceCompletionTests
    {
        [WpfFact]
        public void Creation()
        {
            using var session = CreateSessionSingleQuote("$$");
            Assert.NotNull(session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
        public void String_TopLevel()
        {
            using var session = CreateSessionDoubleQuote("$$");
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
        public void VerbatimString_TopLevel()
        {
            using var session = CreateSessionDoubleQuote("@$$");
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
        public void Char_TopLevel()
        {
            using var session = CreateSessionSingleQuote("$$");
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
        public void String_TopLevel2()
        {
            using var session = CreateSessionDoubleQuote("using System;$$");
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
        public void VerbatimString_TopLevel2()
        {
            using var session = CreateSessionDoubleQuote("using System;@$$");
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact]
        public void String_String()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = ""$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.Null(session);
        }

        [WpfFact]
        public void String_VerbatimString()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = ""@$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.Null(session);
        }

        [WpfFact]
        public void String_Char()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = @""$$
                    }
                }
                """;
            using var session = CreateSessionSingleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact]
        public void Method_String()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact]
        public void Method_String_Delete()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckBackspace(session.Session);
        }

        [WpfFact]
        public void Method_String_Tab()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckTab(session.Session);
        }

        [WpfFact]
        public void Method_String_Quotation()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckOverType(session.Session);
        }

        [WpfFact]
        public void VerbatimMethod_String()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = @$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact]
        public void VerbatimMethod_String_Delete()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = @$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckBackspace(session.Session);
        }

        [WpfFact]
        public void VerbatimMethod_String_Tab()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = @$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckTab(session.Session);
        }

        [WpfFact]
        public void VerbatimMethod_String_Quotation()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = @$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckOverType(session.Session);
        }

        [WpfFact]
        public void Method_InterpolatedString()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $[||]$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact]
        public void Method_InterpolatedString_Delete()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $[||]$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckBackspace(session.Session);
        }

        [WpfFact]
        public void Method_InterpolatedString_Tab()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $[||]$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckTab(session.Session);
        }

        [WpfFact]
        public void Method_InterpolatedString_Quotation()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $[||]$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckOverType(session.Session);
        }

        [WpfFact]
        public void VerbatimMethod_InterpolatedString()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $@$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact]
        public void VerbatimMethod_InterpolatedString_Delete()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $@$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckBackspace(session.Session);
        }

        [WpfFact]
        public void VerbatimMethod_InterpolatedString_Tab()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $@$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckTab(session.Session);
        }

        [WpfFact]
        public void VerbatimMethod_InterpolatedString_Quotation()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $@$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckOverType(session.Session);
        }

        [WpfFact]
        public void Preprocessor1()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                #line $$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckTab(session.Session);
        }

        [WpfFact]
        public void Preprocessor2()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                #line $$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckOverType(session.Session);
        }

        [WpfFact]
        public void Preprocessor3()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                #line $$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckBackspace(session.Session);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546047")]
        public void VerbatimStringDoubleQuote()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = @""$$
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.Null(session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59178")]
        public void String_CompleteLiteral()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = "this" + $$that";
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session, expectValidSession: false);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59178")]
        public void String_BeforeOtherString1()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $$ + " + bar";
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59178")]
        public void String_BeforeOtherString2()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = $$ + "; } ";
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59178")]
        public void String_DoNotCompleteVerbatim()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = "this" + @$$that
                            and this";
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59178")]
        public void String_CompleteLiteral_EndOfFile()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = "this" + $$that"
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session, expectValidSession: false);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/62571")]
        public void String_InArgumentList1()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = Goo($[||]$$);
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/62571")]
        public void String_InArgumentList2()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = Goo(@[||]$$);
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/62571")]
        public void String_InArgumentList3()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = Goo(@$[||]$$);
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/62571")]
        public void String_InArgumentList4()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        var s = Goo($@[||]$$);
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/62571")]
        public void String_InArgumentList5()
        {
            var code = """
                class C
                {
                    void Method()
                    {
                        // Handled by normal verbatim string handler.
                        var s = Goo(@[||]$$);
                    }
                }
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/62571")]
        public void String_GlobalStatement()
        {
            var code = """
                $[||]$$
                """;
            using var session = CreateSessionDoubleQuote(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        internal static Holder CreateSessionSingleQuote(string code)
        {
            return CreateSession(
                EditorTestWorkspace.CreateCSharp(code),
                SingleQuote.OpenCharacter, SingleQuote.CloseCharacter);
        }

        internal static Holder CreateSessionDoubleQuote(string code)
        {
            return CreateSession(
                EditorTestWorkspace.CreateCSharp(code),
                DoubleQuote.OpenCharacter, DoubleQuote.CloseCharacter);
        }
    }
}
