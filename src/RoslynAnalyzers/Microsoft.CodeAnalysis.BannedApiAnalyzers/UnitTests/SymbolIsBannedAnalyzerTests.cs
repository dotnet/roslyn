// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;

using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.BannedApiAnalyzers.CSharpSymbolIsBannedAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.VisualBasic.BannedApiAnalyzers.BasicSymbolIsBannedAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.BannedApiAnalyzers.UnitTests
{
    // For specification of document comment IDs see https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/documentation-comments#processing-the-documentation-file

    public class SymbolIsBannedAnalyzerTests
    {
        private const string BannedSymbolsFileName = "BannedSymbols.txt";

        private static DiagnosticResult GetCSharpResultAt(int markupKey, DiagnosticDescriptor descriptor, string bannedMemberName, string message)
            => VerifyCS.Diagnostic(descriptor)
                .WithLocation(markupKey)
                .WithArguments(bannedMemberName, message);

        private static DiagnosticResult GetBasicResultAt(int markupKey, DiagnosticDescriptor descriptor, string bannedMemberName, string message)
            => VerifyVB.Diagnostic(descriptor)
                .WithLocation(markupKey)
                .WithArguments(bannedMemberName, message);

        private static async Task VerifyBasicAnalyzerAsync(string source, string bannedApiText, params DiagnosticResult[] expected)
        {
            var test = new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (BannedSymbolsFileName, bannedApiText) },
                },
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static async Task VerifyCSharpAnalyzerAsync(string source, string bannedApiText, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (BannedSymbolsFileName, bannedApiText) },
                },
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        #region Diagnostic tests

        [Fact]
        public async Task NoDiagnosticForNoBannedTextAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("class C { }");
            await VerifyVB.VerifyAnalyzerAsync("""
                Class C
                End Class
                """);
        }

        [Fact]
        public Task NoDiagnosticReportedForEmptyBannedTextAsync()
            => VerifyCSharpAnalyzerAsync(@"", @"");

        [Fact]
        public Task NoDiagnosticReportedForMultilineBlankBannedTextAsync()
            => VerifyCSharpAnalyzerAsync("""

                """, @"");

        [Fact]
        public Task NoDiagnosticReportedForMultilineMessageOnlyBannedTextAsync()
            => VerifyCSharpAnalyzerAsync(@"", """
                ;first
                  ;second
                ;third // comment
                """);

        [Fact]
        public async Task DiagnosticReportedForDuplicateBannedApiLinesAsync()
        {
            var expected = new DiagnosticResult(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule)
                .WithLocation(1)
                .WithLocation(0)
                .WithArguments("System.Console");
            await VerifyCSharpAnalyzerAsync(@"", """
                {|#0:T:System.Console|}
                {|#1:T:System.Console|}
                """, expected);
        }

        [Fact]
        public async Task DiagnosticReportedForDuplicateBannedApiLinesWithDifferentIdsAsync()
        {
            var expected = new DiagnosticResult(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule)
                .WithLocation(1)
                .WithLocation(0)
                .WithArguments("System.Console");
            await VerifyCSharpAnalyzerAsync(@"", """
                {|#0:T:System.Console;Message 1|}
                {|#1:TSystem.Console;Message 2|}
                """, expected);
        }

        [Fact]
        public async Task DiagnosticReportedForDuplicateBannedApiLinesInDifferentFiles()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { @"" },
                    AdditionalFiles =
                    {
                        ("BannedSymbols.txt", @"{|#0:T:System.Console|}") ,
                        ("BannedSymbols.Other.txt", @"{|#1:T:System.Console|}")
                    },
                },
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule)
                        .WithLocation(0)
                        .WithLocation(1)
                        .WithArguments("System.Console")
                }
            };

            await test.RunAsync();
        }

        #region Comments in BannedSymbols.txt tests

        [Fact]
        public async Task DiagnosticReportedForDuplicateBannedApiLinesWithCommentsAsync()
        {
            var bannedText = """
                {|#0:T:System.Console|}
                """ + '\t' + """
                 //comment here with whitespace before it
                {|#1:T:System.Console|}//should not affect the reported duplicate
                """;

            var expected = new DiagnosticResult(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule)
                .WithLocation(1)
                .WithLocation(0)
                .WithArguments("System.Console");
            await VerifyCSharpAnalyzerAsync(@"", bannedText, expected);
        }

        [Fact]
        public async Task DiagnosticReportedForDuplicateBannedApiLinesWithCommentsInDifferentFiles()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { @"" },
                    AdditionalFiles =
                    {
                        ("BannedSymbols.txt", @"{|#0:T:System.Console|} //ignore this") ,
                        ("BannedSymbols.Other.txt", @"{|#1:T:System.Console|} //comment will not affect result")
                    },
                },
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule)
                        .WithLocation(0)
                        .WithLocation(1)
                        .WithArguments("System.Console")
                }
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task NoDiagnosticReportedForCommentLine()
        {
            var source = @"";
            var bannedText = @"// this comment line will be ignored";

            await VerifyCSharpAnalyzerAsync(source, bannedText);
            await VerifyBasicAnalyzerAsync(source, bannedText);
        }

        [Fact]
        public async Task NoDiagnosticReportedForCommentedOutBannedApiLineAsync()
        {
            var bannedText = @"//T:N.Banned";
            await VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class Banned { }
                    class C
                    {
                        void M()
                        {
                            var c = new Banned();
                        }
                    }
                }
                """, bannedText);
            await VerifyBasicAnalyzerAsync("""
                Namespace N
                    Class Banned : End Class
                    Class C
                        Sub M()
                            Dim c As New Banned()
                        End Sub
                    End Class
                End Namespace
                """, bannedText);
        }

        [Fact]
        public async Task NoDiagnosticReportedForCommentLineThatBeginsWithWhitespaceAsync()
        {
            var bannedText = @"  // comment here";
            await VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class Banned { }
                    class C
                    {
                        void M()
                        {
                            var c = new Banned();
                        }
                    }
                }
                """, bannedText);
            await VerifyBasicAnalyzerAsync("""
                Namespace N
                    Class Banned : End Class
                    Class C
                        Sub M()
                            Dim c As New Banned()
                        End Sub
                    End Class
                End Namespace
                """, bannedText);
        }

        [Fact]
        public async Task NoDiagnosticReportedForCommentedOutDuplicateBannedApiLinesAsync()
        {
            var source = @"";
            var bannedText = """
                //T:System.Console
                //T:System.Console
                """;

            await VerifyCSharpAnalyzerAsync(source, bannedText);
            await VerifyBasicAnalyzerAsync(source, bannedText);
        }

        [Fact]
        public async Task DiagnosticReportedForBannedApiLineWithCommentAtTheEndAsync()
        {
            var bannedText = @"T:N.Banned//comment here";
            await VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class Banned { }
                    class C
                    {
                        void M()
                        {
                            var c = {|#0:new Banned()|};
                        }
                    }
                }
                """, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
            await VerifyBasicAnalyzerAsync("""
                Namespace N
                    Class Banned : End Class
                    Class C
                        Sub M()
                            Dim c As {|#0:New Banned()|}
                        End Sub
                    End Class
                End Namespace
                """, bannedText, GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
        }

        [Fact]
        public async Task DiagnosticReportedForInterfaceImplementationAsync()
        {
            var bannedText = "T:N.IBanned//comment here";
            await VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    interface IBanned { }
                    class C : {|#0:IBanned|}
                    {
                    }
                }
                """, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "IBanned", ""));
            await VerifyBasicAnalyzerAsync("""
                Namespace N
                    Interface IBanned : End Interface
                    Class C
                        Implements {|#0:IBanned|}
                    End Class
                End Namespace
                """, bannedText, GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "IBanned", ""));

        }

        [Fact]
        public async Task DiagnosticReportedForClassInheritanceAsync()
        {
            var bannedText = "T:N.Banned//comment here";
            await VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class Banned { }
                    class C : {|#0:Banned|}
                    {
                    }
                }
                """, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
            await VerifyBasicAnalyzerAsync("""
                Namespace N
                    Class Banned : End Class
                    Class C
                        Inherits {|#0:Banned|}
                    End Class
                End Namespace
                """, bannedText, GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));

        }

        [Fact]
        public async Task DiagnosticReportedForBannedApiLineWithWhitespaceThenCommentAtTheEndAsync()
        {
            var bannedText = "T:N.Banned \t //comment here";
            await VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class Banned { }
                    class C
                    {
                        void M()
                        {
                            var c = {|#0:new Banned()|};
                        }
                    }
                }
                """, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
            await VerifyBasicAnalyzerAsync("""
                Namespace N
                    Class Banned : End Class
                    Class C
                        Sub M()
                            Dim c As {|#0:New Banned()|}
                        End Sub
                    End Class
                End Namespace
                """, bannedText, GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
        }

        [Fact]
        public async Task DiagnosticReportedForBannedApiLineWithMessageThenWhitespaceFollowedByCommentAtTheEndMustNotIncludeWhitespaceInMessageAsync()
        {
            var bannedText = "T:N.Banned;message \t //comment here";
            await VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class Banned { }
                    class C
                    {
                        void M()
                        {
                            var c = {|#0:new Banned()|};
                        }
                    }
                }
                """, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ": message"));
            await VerifyBasicAnalyzerAsync("""
                Namespace N
                    Class Banned : End Class
                    Class C
                        Sub M()
                            Dim c As {|#0:New Banned()|}
                        End Sub
                    End Class
                End Namespace
                """, bannedText, GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ": message"));
        }

        #endregion Comments in BannedSymbols.txt tests

        [Fact]
        public Task CSharp_BannedApiFile_MessageIncludedInDiagnosticAsync()
            => VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class Banned { }
                    class C
                    {
                        void M()
                        {
                            var c = {|#0:new Banned()|};
                        }
                    }
                }
                """, @"T:N.Banned;Use NonBanned instead", GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ": Use NonBanned instead"));

        [Fact]
        public Task CSharp_BannedApiFile_WhiteSpaceAsync()
            => VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class Banned { }
                    class C
                    {
                        void M()
                        {
                            var c = {|#0:new Banned()|};
                        }
                    }
                }
                """, """
                T:N.Banned
                """, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));

        [Fact]
        public Task CSharp_BannedApiFile_WhiteSpaceWithMessageAsync()
            => VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class Banned { }
                    class C
                    {
                        void M()
                        {
                            var c = {|#0:new Banned()|};
                        }
                    }
                }
                """, @"T:N.Banned ; Use NonBanned instead ", GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ": Use NonBanned instead"));

        [Fact]
        public Task CSharp_BannedApiFile_EmptyMessageAsync()
            => VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class Banned { }
                    class C
                    {
                        void M()
                        {
                            var c = {|#0:new Banned()|};
                        }
                    }
                }
                """, @"T:N.Banned;", GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));

        [Fact]
        public async Task CSharp_BannedApi_MultipleFiles()
        {
            var source = """
                namespace N
                {
                    class BannedA { }
                    class BannedB { }
                    class NotBanned { }
                    class C
                    {
                        void M()
                        {
                            var a = {|#0:new BannedA()|};
                            var b = {|#1:new BannedB()|};
                            var c = new NotBanned();
                        }
                    }
                }
                """;

            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles =
                    {
                        ("BannedSymbols.txt", @"T:N.BannedA") ,
                        ("BannedSymbols.Other.txt", @"T:N.BannedB"),
                        ("OtherFile.txt", @"T:N.NotBanned")
                    },
                },
                ExpectedDiagnostics =
                {
                    GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedA", ""),
                    GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedB", "")
                }
            };

            await test.RunAsync();
        }

        [Fact]
        public Task CSharp_BannedType_ConstructorAsync()
            => VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class Banned { }
                    class C
                    {
                        void M()
                        {
                            var c = {|#0:new Banned()|};
                        }
                    }
                }
                """, """
                T:N.Banned
                """, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));

        [Fact]
        public Task CSharp_BannedNamespace_ConstructorAsync()
            => VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class C
                    {
                        void M()
                        {
                            var c = {|#0:new N.C()|};
                        }
                    }
                }
                """, """
                N:N
                """, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""));

        [Fact]
        public Task CSharp_BannedNamespace_Parent_ConstructorAsync()
            => VerifyCSharpAnalyzerAsync("""
                namespace N.NN
                {
                    class C
                    {
                        void M()
                        {
                            var a = {|#0:new N.NN.C()|};
                        }
                    }
                }
                """, """
                N:N
                """, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""));

        [Fact]
        public Task CSharp_BannedNamespace_MethodGroupAsync()
            => VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    delegate void D();
                    class C
                    {
                        void M()
                        {
                            D d = {|#0:M|};
                        }
                    }
                }
                """, """
                N:N
                """, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""));

        [Fact]
        public Task CSharp_BannedNamespace_PropertyAsync()
            => VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class C
                    {
                        public int P { get; set; }
                        void M()
                        {
                            {|#0:P|} = {|#1:P|};
                        }
                    }
                }
                """, """
                N:N
                """,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""));

        [Fact]
        public Task CSharp_BannedNamespace_MethodAsync()
            => VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    interface I
                    {
                        void M();
                    }
                }

                class C
                {
                    void M()
                    {
                        N.I i = null;
                        {|#0:i.M()|};
                    }
                }
                """, @"N:N", GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""));

        [Fact]
        public Task CSharp_BannedNamespace_TypeOfArgument()
            => VerifyCSharpAnalyzerAsync("""
                namespace N
                {
                    class Banned {  }
                }
                class C
                {
                    void M()
                    {
                        var type = {|#0:typeof(N.Banned)|};
                    }
                }
                """, @"N:N", GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""));

        [Fact]
        public Task CSharp_BannedNamespace_Constituent()
            => VerifyCSharpAnalyzerAsync("""
                class C
                {
                    void M()
                    {
                        var thread = {|#0:new System.Threading.Thread((System.Threading.ThreadStart)null)|};
                    }
                }
                """, @"N:System.Threading", GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "System.Threading", ""));

        [Fact]
        public Task CSharp_BannedGenericType_ConstructorAsync()
            => VerifyCSharpAnalyzerAsync("""
                class C
                {
                    void M()
                    {
                        var c = {|#0:new System.Collections.Generic.List<string>()|};
                    }
                }
                """, """
                T:System.Collections.Generic.List`1
                """, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "List<T>", ""));

        [Fact]
        public Task CSharp_BannedType_AsTypeArgumentAsync()
            => VerifyCSharpAnalyzerAsync("""
                struct C {}

                class G<T>
                {
                    class N<U>
                    { }

                    unsafe void M()
                    {
                        var b = {|#0:new G<C>()|};
                        var c = {|#1:new G<C>.N<int>()|};
                        var d = {|#2:new G<int>.N<C>()|};
                        var e = {|#3:new G<G<int>.N<C>>.N<int>()|};
                        var f = {|#4:new G<G<C>.N<int>>.N<int>()|};
                        var g = {|#5:new C[42]|};
                        var h = {|#6:new G<C[]>()|};
                        fixed (C* i = {|#7:&g[0]|}) { }
                    }
                }
                """, """
                T:C
                """,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(3, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(4, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(5, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(6, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(7, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task CSharp_BannedNestedType_ConstructorAsync()
            => VerifyCSharpAnalyzerAsync("""
                class C
                {
                    class Nested { }
                    void M()
                    {
                        var n = {|#0:new Nested()|};
                    }
                }
                """, """
                T:C.Nested
                """, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Nested", ""));

        [Fact]
        public Task CSharp_BannedType_MethodOnNestedTypeAsync()
            => VerifyCSharpAnalyzerAsync("""
                class C
                {
                    public static class Nested
                    {
                        public static void M() { }
                    }
                }

                class D
                {
                    void M2()
                    {
                        {|#0:C.Nested.M()|};
                    }
                }
                """, """
                T:C
                """, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task CSharp_BannedInterface_MethodAsync()
            => VerifyCSharpAnalyzerAsync("""
                interface I
                {
                    void M();
                }

                class C
                {
                    void M()
                    {
                        I i = null;
                        {|#0:i.M()|};
                    }
                }
                """, @"T:I", GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "I", ""));

        [Fact]
        public Task CSharp_BannedClass_OperatorsAsync()
            => VerifyCSharpAnalyzerAsync("""
                class C
                {
                    public static implicit operator C(int i) => {|#0:new C()|};
                    public static explicit operator C(float f) => {|#1:new C()|};
                    public static C operator +(C c, int i) => c;
                    public static C operator ++(C c) => c;
                    public static C operator -(C c) => c;

                    void M()
                    {
                        C c = {|#2:0|};        // implicit conversion.
                        c = {|#3:(C)1.0f|};    // Explicit conversion.
                        c = {|#4:c + 1|};      // Binary operator.
                        {|#5:c++|};            // Increment or decrement.
                        c = {|#6:-c|};         // Unary operator.
                    }
                }
                """, @"T:C",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(3, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(4, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(5, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(6, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task CSharp_BannedClass_PropertyAsync()
            => VerifyCSharpAnalyzerAsync("""
                class C
                {
                    public int P { get; set; }
                    void M()
                    {
                        {|#0:P|} = {|#1:P|};
                    }
                }
                """, @"T:C",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task CSharp_BannedClass_FieldAsync()
            => VerifyCSharpAnalyzerAsync("""
                class C
                {
                    public int F;
                    void M()
                    {
                        {|#0:F|} = {|#1:F|};
                    }
                }
                """, @"T:C",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task CSharp_BannedClass_EventAsync()
            => VerifyCSharpAnalyzerAsync("""
                using System;

                class C
                {
                    public event EventHandler E;
                    void M()
                    {
                        {|#0:E|} += null;
                        {|#1:E|} -= null;
                        {|#2:E|}(null, EventArgs.Empty);
                    }
                }
                """, @"T:C",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task CSharp_BannedClass_MethodGroupAsync()
            => VerifyCSharpAnalyzerAsync("""
                delegate void D();
                class C
                {
                    void M()
                    {
                        D d = {|#0:M|};
                    }
                }
                """, @"T:C",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task CSharp_BannedClass_DocumentationReferenceAsync()
            => VerifyCSharpAnalyzerAsync("""
                class C { }

                /// <summary><see cref="{|#0:C|}" /></summary>
                class D { }
                """, @"T:C",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task CSharp_BannedAttribute_UsageOnTypeAsync()
            => VerifyCSharpAnalyzerAsync("""
                using System;

                [AttributeUsage(AttributeTargets.All, Inherited = true)]
                class BannedAttribute : Attribute { }

                [{|#0:Banned|}]
                class C { }
                class D : C { }
                """, @"T:BannedAttribute",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""),
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));

        [Fact]
        public Task CSharp_BannedAttribute_UsageOnMemberAsync()
            => VerifyCSharpAnalyzerAsync("""
                using System;

                [AttributeUsage(AttributeTargets.All, Inherited = true)]
                class BannedAttribute : Attribute { }

                class C 
                {
                    [{|#0:Banned|}]
                    public int SomeProperty { get; }
                }
                """, @"T:BannedAttribute",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""),
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));

        [Fact]
        public Task CSharp_BannedAttribute_UsageOnAssemblyAsync()
            => VerifyCSharpAnalyzerAsync("""
                using System;

                [assembly: {|#0:BannedAttribute|}]

                [AttributeUsage(AttributeTargets.All, Inherited = true)]
                class BannedAttribute : Attribute { }
                """, @"T:BannedAttribute",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""),
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));

        [Fact]
        public Task CSharp_BannedAttribute_UsageOnModuleAsync()
            => VerifyCSharpAnalyzerAsync("""
                using System;

                [module: {|#0:BannedAttribute|}]

                [AttributeUsage(AttributeTargets.All, Inherited = true)]
                class BannedAttribute : Attribute { }
                """, @"T:BannedAttribute",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""),
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));

        [Fact]
        public async Task CSharp_BannedConstructorAsync()
        {
            var source = """
                namespace N
                {
                    class Banned
                    {
                        public Banned() {}
                        public Banned(int i) {}
                    }
                    class C
                    {
                        void M()
                        {
                            var c = {|#0:new Banned()|};
                            var d = {|#1:new Banned(1)|};
                        }
                    }
                }
                """;
            await VerifyCSharpAnalyzerAsync(
                source,
                @"M:N.Banned.#ctor",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned.Banned()", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                @"M:N.Banned.#ctor(System.Int32)",
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned.Banned(int)", ""));
        }

        [Fact]
        public async Task Csharp_BannedConstructor_AttributeAsync()
        {
            var source = """
                using System;

                [AttributeUsage(AttributeTargets.All, Inherited = true)]
                class BannedAttribute : Attribute
                {
                    public BannedAttribute() {}
                    public BannedAttribute(int banned) {}
                    public BannedAttribute(string notBanned) {}
                }

                class C
                {
                    [{|#0:Banned|}]
                    public int SomeProperty { get; }

                    [{|#1:Banned(1)|}]
                    public void SomeMethod() {}

                    [Banned("")]
                    class D {}
                }
                """;
            await VerifyCSharpAnalyzerAsync(
                source,
                @"M:BannedAttribute.#ctor",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute.BannedAttribute()", ""),
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute.BannedAttribute()", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                @"M:BannedAttribute.#ctor(System.Int32)",
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute.BannedAttribute(int)", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute.BannedAttribute(int)", ""));
        }

        [Fact]
        public async Task CSharp_BannedMethodAsync()
        {
            var source = """
                namespace N
                {
                    class C
                    {
                        public void Banned() {}
                        public void Banned(int i) {}
                        public void Banned<T>(T t) {}

                        void M()
                        {
                            {|#0:Banned()|};
                            {|#1:Banned(1)|};
                            {|#2:Banned<string>("")|};
                        }
                    }

                    class D<T>
                    {
                        public void Banned() {}
                        public void Banned(int i) {}
                        public void Banned<U>(U u) {}

                        void M()
                        {
                            {|#3:Banned()|};
                            {|#4:Banned(1)|};
                            {|#5:Banned<string>("")|};
                        }
                    }
                }
                """;
            await VerifyCSharpAnalyzerAsync(
                source,
                @"M:N.C.Banned",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned()", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                @"M:N.C.Banned(System.Int32)",
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned(int)", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                @"M:N.C.Banned``1(``0)",
                GetCSharpResultAt(2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned<T>(T)", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                @"M:N.D`1.Banned()",
                GetCSharpResultAt(3, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "D<T>.Banned()", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                @"M:N.D`1.Banned(System.Int32)",
                GetCSharpResultAt(4, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "D<T>.Banned(int)", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                @"M:N.D`1.Banned``1(``0)",
                GetCSharpResultAt(5, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "D<T>.Banned<U>(U)", ""));
        }

        [Fact]
        public Task CSharp_BannedPropertyAsync()
            => VerifyCSharpAnalyzerAsync(
                """
                namespace N
                {
                    class C
                    {
                        public int Banned { get; set; }

                        void M()
                        {
                            {|#0:Banned|} = {|#1:Banned|};
                        }
                    }
                }
                """,
                @"P:N.C.Banned",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""));

        [Fact]
        public Task CSharp_BannedFieldAsync()
            => VerifyCSharpAnalyzerAsync(
                """
                namespace N
                {
                    class C
                    {
                        public int Banned;

                        void M()
                        {
                            {|#0:Banned|} = {|#1:Banned|};
                        }
                    }
                }
                """,
                @"F:N.C.Banned",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""));

        [Fact]
        public Task CSharp_BannedEventAsync()
            => VerifyCSharpAnalyzerAsync(
                """
                namespace N
                {
                    class C
                    {
                        public event System.Action Banned;

                        void M()
                        {
                            {|#0:Banned|} += null;
                            {|#1:Banned|} -= null;
                            {|#2:Banned|}();
                        }
                    }
                }
                """,
                @"E:N.C.Banned",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""),
                GetCSharpResultAt(2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""));

        [Fact]
        public Task CSharp_BannedMethodGroupAsync()
            => VerifyCSharpAnalyzerAsync(
                """
                namespace N
                {
                    class C
                    {
                        public void Banned() {}

                        void M()
                        {
                            System.Action b = {|#0:Banned|};
                        }
                    }
                }
                """,
                @"M:N.C.Banned",
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned()", ""));

        [Fact]
        public Task CSharp_NoDiagnosticClass_TypeOfArgument()
            => VerifyCSharpAnalyzerAsync("""
                class Banned {  }
                class C
                {
                    void M()
                    {
                        var type = {|#0:typeof(C)|};
                    }
                }
                """, @"T:Banned");

        [Fact]
        public Task CSharp_BannedClass_TypeOfArgument()
            => VerifyCSharpAnalyzerAsync("""
                class Banned {  }
                class C
                {
                    void M()
                    {
                        var type = {|#0:typeof(Banned)|};
                    }
                }
                """, @"T:Banned", GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));

        [Fact, WorkItem(3295, "https://github.com/dotnet/roslyn-analyzers/issues/3295")]
        public Task CSharp_BannedAbstractVirtualMemberAlsoBansOverrides_RootLevelIsBannedAsync()
            => VerifyCSharpAnalyzerAsync("""
                using System;

                namespace N
                {
                    public abstract class C1
                    {
                        public abstract void Method1();
                        public abstract int Property1 { get; set; }
                        public abstract event Action Event1;

                        public virtual void Method2() {}
                        public virtual int Property2 { get; set; }
                        public virtual event Action Event2;
                    }

                    public class C2 : C1
                    {
                        public override void Method1() {}
                        public override int Property1 { get; set; }
                        public override event Action Event1;

                        public override void Method2()
                        {
                            {|RS0030:base.Method2()|};
                        }

                        public override int Property2 { get; set; }
                        public override event Action Event2;

                        void M1()
                        {
                            {|RS0030:Method1()|};
                            if ({|RS0030:Property1|} == 42 && {|RS0030:Event1|} != null) {}

                            {|RS0030:Method2()|};
                            if ({|RS0030:Property2|} == 42 && {|RS0030:Event2|} != null) {}
                        }
                    }

                    public class C3 : C2
                    {
                        public override void Method1()
                        {
                            {|RS0030:base.Method1()|};
                        }

                        public override int Property1 { get; set; }
                        public override event Action Event1;

                        public override void Method2()
                        {
                            {|RS0030:base.Method2()|};
                        }

                        public override int Property2 { get; set; }
                        public override event Action Event2;

                        void M2()
                        {
                            {|RS0030:Method1()|};
                            if ({|RS0030:Property1|} == 42 && {|RS0030:Event1|} != null) {}

                            {|RS0030:Method2()|};
                            if ({|RS0030:Property2|} == 42 && {|RS0030:Event2|} != null) {}
                        }
                    }
                }
                """, """
                M:N.C1.Method1
                P:N.C1.Property1
                E:N.C1.Event1
                M:N.C1.Method2
                P:N.C1.Property2
                E:N.C1.Event2
                """);

        [Fact, WorkItem(3295, "https://github.com/dotnet/roslyn-analyzers/issues/3295")]
        public Task CSharp_BannedAbstractVirtualMemberBansCorrectOverrides_MiddleLevelIsBannedAsync()
            => VerifyCSharpAnalyzerAsync("""
                using System;

                namespace N
                {
                    public abstract class C1
                    {
                        public abstract void Method1();
                        public abstract int Property1 { get; set; }
                        public abstract event Action Event1;

                        public virtual void Method2() {}
                        public virtual int Property2 { get; set; }
                        public virtual event Action Event2;
                    }

                    public class C2 : C1
                    {
                        public override void Method1() {}
                        public override int Property1 { get; set; }
                        public override event Action Event1;

                        public override void Method2()
                        {
                            base.Method2();
                        }

                        public override int Property2 { get; set; }
                        public override event Action Event2;

                        void M1()
                        {
                            {|RS0030:Method1()|};
                            if ({|RS0030:Property1|} == 42 && {|RS0030:Event1|} != null) {}

                            {|RS0030:Method2()|};
                            if ({|RS0030:Property2|} == 42 && {|RS0030:Event2|} != null) {}
                        }
                    }

                    public class C3 : C2
                    {
                        public override void Method1()
                        {
                            {|RS0030:base.Method1()|};
                        }

                        public override int Property1 { get; set; }
                        public override event Action Event1;

                        public override void Method2()
                        {
                            {|RS0030:base.Method2()|};
                        }

                        public override int Property2 { get; set; }
                        public override event Action Event2;

                        void M2()
                        {
                            {|RS0030:Method1()|};
                            if ({|RS0030:Property1|} == 42 && {|RS0030:Event1|} != null) {}

                            {|RS0030:Method2()|};
                            if ({|RS0030:Property2|} == 42 && {|RS0030:Event2|} != null) {}
                        }
                    }
                }
                """, """
                M:N.C2.Method1
                P:N.C2.Property1
                E:N.C2.Event1
                M:N.C2.Method2
                P:N.C2.Property2
                E:N.C2.Event2
                """);

        [Fact, WorkItem(3295, "https://github.com/dotnet/roslyn-analyzers/issues/3295")]
        public Task CSharp_BannedAbstractVirtualMemberBansCorrectOverrides_LeafLevelIsBannedAsync()
            => VerifyCSharpAnalyzerAsync("""
                using System;

                namespace N
                {
                    public abstract class C1
                    {
                        public abstract void Method1();
                        public abstract int Property1 { get; set; }
                        public abstract event Action Event1;

                        public virtual void Method2() {}
                        public virtual int Property2 { get; set; }
                        public virtual event Action Event2;
                    }

                    public class C2 : C1
                    {
                        public override void Method1() {}
                        public override int Property1 { get; set; }
                        public override event Action Event1;

                        public override void Method2()
                        {
                            base.Method2();
                        }

                        public override int Property2 { get; set; }
                        public override event Action Event2;

                        void M1()
                        {
                            Method1();
                            if (Property1 == 42 && Event1 != null) {}

                            Method2();
                            if (Property2 == 42 && Event2 != null) {}
                        }
                    }

                    public class C3 : C2
                    {
                        public override void Method1()
                        {
                            base.Method1();
                        }

                        public override int Property1 { get; set; }
                        public override event Action Event1;

                        public override void Method2()
                        {
                            base.Method2();
                        }

                        public override int Property2 { get; set; }
                        public override event Action Event2;

                        void M2()
                        {
                            {|RS0030:Method1()|};
                            if ({|RS0030:Property1|} == 42 && {|RS0030:Event1|} != null) {}

                            {|RS0030:Method2()|};
                            if ({|RS0030:Property2|} == 42 && {|RS0030:Event2|} != null) {}
                        }
                    }
                }
                """, """
                M:N.C3.Method1
                P:N.C3.Property1
                E:N.C3.Event1
                M:N.C3.Method2
                P:N.C3.Property2
                E:N.C3.Event2
                """);

        [Fact]
        public Task CSharp_InvalidOverrideDefinitionAsync()
            => VerifyCSharpAnalyzerAsync("""
                using System;

                namespace N
                {
                    public class C1
                    {
                        public void Method1() {}
                    }

                    public class C2 : C1
                    {
                        public override void {|CS0506:Method1|}() {}

                        void M1()
                        {
                            Method1();
                        }
                    }
                }
                """, @"M:N.C1.Method1");

        [Fact]
        public async Task VisualBasic_BannedApi_MultipleFiles()
        {
            var source = """
                Namespace N
                    Class BannedA : End Class
                    Class BannedB : End Class
                    Class NotBanned : End Class
                    Class C
                        Sub M()
                            Dim a As {|#0:New BannedA()|}
                            Dim b As {|#1:New BannedB()|}
                            Dim c As New NotBanned()
                        End Sub
                    End Class
                End Namespace
                """;

            var test = new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles =
                    {
                        ("BannedSymbols.txt", @"T:N.BannedA") ,
                        ("BannedSymbols.Other.txt", @"T:N.BannedB"),
                        ("OtherFile.txt", @"T:N.NotBanned")
                    },
                },
                ExpectedDiagnostics =
                {
                    GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedA", ""),
                    GetBasicResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedB", "")
                }
            };

            await test.RunAsync();
        }

        [Fact]
        public Task VisualBasic_BannedType_ConstructorAsync()
            => VerifyBasicAnalyzerAsync("""
                Namespace N
                    Class Banned : End Class
                    Class C
                        Sub M()
                            Dim c As {|#0:New Banned()|}
                        End Sub
                    End Class
                End Namespace
                """, @"T:N.Banned", GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));

        [Fact]
        public Task VisualBasic_BannedGenericType_ConstructorAsync()
            => VerifyBasicAnalyzerAsync("""
                Class C
                    Sub M()
                        Dim c = {|#0:New System.Collections.Generic.List(Of String)()|}
                    End Sub
                End Class
                """, """
                T:System.Collections.Generic.List`1
                """, GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "List(Of T)", ""));

        [Fact]
        public Task VisualBasic_BannedNestedType_ConstructorAsync()
            => VerifyBasicAnalyzerAsync("""
                Class C
                    Class Nested : End Class
                    Sub M()
                        Dim n As {|#0:New Nested()|}
                    End Sub
                End Class
                """, """
                T:C.Nested
                """, GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Nested", ""));

        [Fact]
        public Task VisualBasic_BannedType_MethodOnNestedTypeAsync()
            => VerifyBasicAnalyzerAsync("""
                Class C
                    Public Class Nested
                        Public Shared Sub M() : End Sub
                    End Class
                End Class

                Class D
                    Sub M2()
                        {|#0:C.Nested.M()|}
                    End Sub
                End Class
                """, """
                T:C
                """, GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task VisualBasic_BannedInterface_MethodAsync()
            => VerifyBasicAnalyzerAsync("""
                Interface I
                    Sub M()
                End Interface

                Class C
                    Sub M()
                        Dim i As I = Nothing
                        {|#0:i.M()|}
                    End Sub
                End Class
                """, @"T:I", GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "I", ""));

        [Fact]
        public Task VisualBasic_BannedClass_PropertyAsync()
            => VerifyBasicAnalyzerAsync("""
                Class C
                    Public Property P As Integer
                    Sub M()
                        {|#0:P|} = {|#1:P|}
                    End Sub
                End Class
                """, @"T:C",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetBasicResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task VisualBasic_BannedClass_FieldAsync()
            => VerifyBasicAnalyzerAsync("""
                Class C
                    Public F As Integer
                    Sub M()
                        {|#0:F|} = {|#1:F|}
                    End Sub
                End Class
                """, @"T:C",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetBasicResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task VisualBasic_BannedClass_EventAsync()
            => VerifyBasicAnalyzerAsync("""
                Imports System

                Class C
                    public Event E As EventHandler
                    Sub M()
                        AddHandler {|#0:E|}, Nothing
                        RemoveHandler {|#1:E|}, Nothing
                        RaiseEvent {|#2:E|}(Me, EventArgs.Empty)
                    End Sub
                End Class
                """, @"T:C",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetBasicResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetBasicResultAt(2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task VisualBasic_BannedClass_MethodGroupAsync()
            => VerifyBasicAnalyzerAsync("""
                Delegate Sub D()
                Class C
                    Sub M()
                        Dim d as D = {|#0:AddressOf M|}
                    End Sub
                End Class
                """, @"T:C",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact]
        public Task VisualBasic_BannedAttribute_UsageOnTypeAsync()
            => VerifyBasicAnalyzerAsync("""
                Imports System

                <AttributeUsage(AttributeTargets.All, Inherited:=true)>
                Class BannedAttribute
                    Inherits Attribute
                End Class

                <{|#0:Banned|}>
                Class C
                End Class
                Class D
                    Inherits C
                End Class
                """, @"T:BannedAttribute",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""),
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));

        [Fact]
        public Task VisualBasic_BannedAttribute_UsageOnMemberAsync()
            => VerifyBasicAnalyzerAsync("""
                Imports System

                <AttributeUsage(System.AttributeTargets.All, Inherited:=True)>
                Class BannedAttribute
                    Inherits System.Attribute
                End Class

                Class C
                    <{|#0:Banned|}>
                    Public ReadOnly Property SomeProperty As Integer
                End Class
                """, @"T:BannedAttribute",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""),
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));

        [Fact]
        public Task VisualBasic_BannedAttribute_UsageOnAssemblyAsync()
            => VerifyBasicAnalyzerAsync("""
                Imports System

                <{|#0:Assembly:BannedAttribute|}>

                <AttributeUsage(AttributeTargets.All, Inherited:=True)>
                Class BannedAttribute
                    Inherits Attribute
                End Class
                """, @"T:BannedAttribute",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""),
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));

        [Fact]
        public Task VisualBasic_BannedAttribute_UsageOnModuleAsync()
            => VerifyBasicAnalyzerAsync("""
                Imports System

                <{|#0:Module:BannedAttribute|}>

                <AttributeUsage(AttributeTargets.All, Inherited:=True)>
                Class BannedAttribute
                    Inherits Attribute
                End Class
                """, @"T:BannedAttribute",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""),
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));

        [Fact]
        public async Task VisualBasic_BannedConstructorAsync()
        {
            var source = """
                Namespace N
                    Class Banned
                        Sub New : End Sub
                        Sub New(ByVal I As Integer) : End Sub
                    End Class
                    Class C
                        Sub M()
                            Dim c As {|#0:New Banned()|}
                            Dim d As {|#1:New Banned(1)|}
                        End Sub
                    End Class
                End Namespace
                """;
            await VerifyBasicAnalyzerAsync(
                source,
                @"M:N.Banned.#ctor",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub New()", ""));

            await VerifyBasicAnalyzerAsync(
                source,
                @"M:N.Banned.#ctor(System.Int32)",
                GetBasicResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub New(I As Integer)", ""));
        }

        [Fact]
        public async Task VisualBasic_BannedConstructor_AttributeAsync()
        {
            var source = """
                Imports System

                <AttributeUsage(System.AttributeTargets.All, Inherited:=True)>
                Class BannedAttribute
                    Inherits System.Attribute

                    Sub New : End Sub
                    Sub New(ByVal Banned As Integer) : End Sub
                    Sub New(ByVal NotBanned As String) : End Sub
                End Class

                Class C
                    <{|#0:Banned|}>
                    Public ReadOnly Property SomeProperty As Integer

                    <{|#1:Banned(1)|}>
                    Public Sub SomeMethod : End Sub

                    <Banned("")>
                    Class D : End Class
                End Class
                """;
            await VerifyBasicAnalyzerAsync(
                source,
                @"M:BannedAttribute.#ctor",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub New()", ""),
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub New()", ""));

            await VerifyBasicAnalyzerAsync(
                source,
                @"M:BannedAttribute.#ctor(System.Int32)",
                GetBasicResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub New(Banned As Integer)", ""),
                GetBasicResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub New(Banned As Integer)", ""));
        }

        [Fact]
        public async Task VisualBasic_BannedMethodAsync()
        {
            var source = """
                Namespace N
                    Class C
                        Sub Banned : End Sub
                        Sub Banned(ByVal I As Integer) : End Sub
                        Sub M()
                            {|#0:Me.Banned()|}
                            {|#1:Me.Banned(1)|}
                        End Sub
                    End Class
                End Namespace
                """;
            await VerifyBasicAnalyzerAsync(
                source,
                @"M:N.C.Banned",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub Banned()", ""));

            await VerifyBasicAnalyzerAsync(
                source,
                @"M:N.C.Banned(System.Int32)",
                GetBasicResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub Banned(I As Integer)", ""));
        }

        [Fact]
        public Task VisualBasic_BannedPropertyAsync()
            => VerifyBasicAnalyzerAsync(
                """
                Namespace N
                    Class C
                        Public Property Banned As Integer
                        Sub M()
                            {|#0:Banned|} = {|#1:Banned|}
                        End Sub
                    End Class
                End Namespace
                """,
                @"P:N.C.Banned",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Property Banned As Integer", ""),
                GetBasicResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Property Banned As Integer", ""));

        [Fact]
        public Task VisualBasic_BannedFieldAsync()
            => VerifyBasicAnalyzerAsync(
                """
                Namespace N
                    Class C
                        Public Banned As Integer
                        Sub M()
                            {|#0:Banned|} = {|#1:Banned|}
                        End Sub
                    End Class
                End Namespace
                """,
                @"F:N.C.Banned",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Banned As Integer", ""),
                GetBasicResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Banned As Integer", ""));

        [Fact]
        public Task VisualBasic_BannedEventAsync()
            => VerifyBasicAnalyzerAsync(
                """
                Namespace N
                    Class C
                        Public Event Banned As System.Action
                        Sub M()
                            AddHandler {|#0:Banned|}, Nothing
                            RemoveHandler {|#1:Banned|}, Nothing
                            RaiseEvent {|#2:Banned|}()
                        End Sub
                    End Class
                End Namespace
                """,
                @"E:N.C.Banned",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Event Banned As Action", ""),
                GetBasicResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Event Banned As Action", ""),
                GetBasicResultAt(2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Event Banned As Action", ""));

        [Fact]
        public Task VisualBasic_BannedMethodGroupAsync()
            => VerifyBasicAnalyzerAsync(
                """
                Namespace N
                    Class C
                        Public Sub Banned() : End Sub
                        Sub M()
                            Dim b As System.Action = {|#0:AddressOf Banned|}
                        End Sub
                    End Class
                End Namespace
                """,
                @"M:N.C.Banned",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub Banned()", ""));

        [Fact]
        public Task VisualBasic_BannedClass_DocumentationReferenceAsync()
            => VerifyBasicAnalyzerAsync("""
                Class C : End Class

                ''' <summary><see cref="{|#0:C|}" /></summary>
                Class D : End Class
                """, @"T:C",
                GetBasicResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));

        [Fact, WorkItem(3295, "https://github.com/dotnet/roslyn-analyzers/issues/3295")]
        public Task VisualBasic_BannedAbstractVirtualMemberAlsoBansOverrides_RootLevelIsBannedAsync()
            => VerifyBasicAnalyzerAsync("""
                Imports System

                Namespace N
                    Public MustInherit Class C1
                        Public MustOverride Sub Method1()
                        Public MustOverride Property Property1 As Integer

                        Public Overridable Sub Method2()
                        End Sub

                        Public Overridable Property Property2 As Integer
                    End Class

                    Public Class C2
                        Inherits C1

                        Public Overrides Sub Method1()
                        End Sub

                        Public Overrides Property Property1 As Integer

                        Public Overrides Sub Method2()
                            {|RS0030:MyBase.Method2()|}
                        End Sub

                        Public Overrides Property Property2 As Integer

                        Private Sub M1()
                            {|RS0030:Method1()|}

                            If {|RS0030:Property1|} = 42 Then
                            End If

                            {|RS0030:Method2()|}

                            If {|RS0030:Property2|} = 42 Then
                            End If
                        End Sub
                    End Class

                    Public Class C3
                        Inherits C2

                        Public Overrides Sub Method1()
                            {|RS0030:MyBase.Method1()|}
                        End Sub

                        Public Overrides Property Property1 As Integer

                        Public Overrides Sub Method2()
                            {|RS0030:MyBase.Method2()|}
                        End Sub

                        Public Overrides Property Property2 As Integer

                        Private Sub M2()
                            {|RS0030:Method1()|}

                            If {|RS0030:Property1|} = 42 Then
                            End If

                            {|RS0030:Method2()|}

                            If {|RS0030:Property2|} = 42 Then
                            End If
                        End Sub
                    End Class
                End Namespace
                """, """
                M:N.C1.Method1
                P:N.C1.Property1
                E:N.C1.Event1
                M:N.C1.Method2
                P:N.C1.Property2
                E:N.C1.Event2
                """);

        [Fact, WorkItem(3295, "https://github.com/dotnet/roslyn-analyzers/issues/3295")]
        public Task VisualBasic_BannedAbstractVirtualMemberAlsoBansOverrides_MiddleLevelIsBannedAsync()
            => VerifyBasicAnalyzerAsync("""
                Imports System

                Namespace N
                    Public MustInherit Class C1
                        Public MustOverride Sub Method1()
                        Public MustOverride Property Property1 As Integer

                        Public Overridable Sub Method2()
                        End Sub

                        Public Overridable Property Property2 As Integer
                    End Class

                    Public Class C2
                        Inherits C1

                        Public Overrides Sub Method1()
                        End Sub

                        Public Overrides Property Property1 As Integer

                        Public Overrides Sub Method2()
                            MyBase.Method2()
                        End Sub

                        Public Overrides Property Property2 As Integer

                        Private Sub M1()
                            {|RS0030:Method1()|}

                            If {|RS0030:Property1|} = 42 Then
                            End If

                            {|RS0030:Method2()|}

                            If {|RS0030:Property2|} = 42 Then
                            End If
                        End Sub
                    End Class

                    Public Class C3
                        Inherits C2

                        Public Overrides Sub Method1()
                            {|RS0030:MyBase.Method1()|}
                        End Sub

                        Public Overrides Property Property1 As Integer

                        Public Overrides Sub Method2()
                            {|RS0030:MyBase.Method2()|}
                        End Sub

                        Public Overrides Property Property2 As Integer

                        Private Sub M2()
                            {|RS0030:Method1()|}

                            If {|RS0030:Property1|} = 42 Then
                            End If

                            {|RS0030:Method2()|}

                            If {|RS0030:Property2|} = 42 Then
                            End If
                        End Sub
                    End Class
                End Namespace
                """, """
                M:N.C2.Method1
                P:N.C2.Property1
                E:N.C2.Event1
                M:N.C2.Method2
                P:N.C2.Property2
                E:N.C2.Event2
                """);

        [Fact, WorkItem(3295, "https://github.com/dotnet/roslyn-analyzers/issues/3295")]
        public Task VisualBasic_BannedAbstractVirtualMemberAlsoBansOverrides_LeafLevelIsBannedAsync()
            => VerifyBasicAnalyzerAsync("""
                Imports System

                Namespace N
                    Public MustInherit Class C1
                        Public MustOverride Sub Method1()
                        Public MustOverride Property Property1 As Integer

                        Public Overridable Sub Method2()
                        End Sub

                        Public Overridable Property Property2 As Integer
                    End Class

                    Public Class C2
                        Inherits C1

                        Public Overrides Sub Method1()
                        End Sub

                        Public Overrides Property Property1 As Integer

                        Public Overrides Sub Method2()
                            MyBase.Method2()
                        End Sub

                        Public Overrides Property Property2 As Integer

                        Private Sub M1()
                            Method1()

                            If Property1 = 42 Then
                            End If

                            Method2()

                            If Property2 = 42 Then
                            End If
                        End Sub
                    End Class

                    Public Class C3
                        Inherits C2

                        Public Overrides Sub Method1()
                            MyBase.Method1()
                        End Sub

                        Public Overrides Property Property1 As Integer

                        Public Overrides Sub Method2()
                            MyBase.Method2()
                        End Sub

                        Public Overrides Property Property2 As Integer

                        Private Sub M2()
                            {|RS0030:Method1()|}

                            If {|RS0030:Property1|} = 42 Then
                            End If

                            {|RS0030:Method2()|}

                            If {|RS0030:Property2|} = 42 Then
                            End If
                        End Sub
                    End Class
                End Namespace
                """, """
                M:N.C3.Method1
                P:N.C3.Property1
                E:N.C3.Event1
                M:N.C3.Method2
                P:N.C3.Property2
                E:N.C3.Event2
                """);

        #endregion

        #region Generated code exclusion tests

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/82114")]
        public Task ExcludeGeneratedCode_Enabled()
        {
            return new VerifyCS.Test
            {
                TestCode = """
                    // <auto-generated/>
                    namespace N
                    {
                        class Banned { }
                        class C
                        {
                            void M()
                            {
                                var c = {|#0:new Banned()|};
                            }
                        }
                    }
                    """,
                TestState =
                {
                    AdditionalFiles = { (BannedSymbolsFileName, "T:N.Banned") },
                    AnalyzerConfigFiles =
                    {
                        ("/.globalconfig", """
                            is_global = true
                            banned_api_analyzer.exclude_generated_code = true
                            """)
                    },
                },
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/82114")]
        public Task ExcludeGeneratedCode_Disabled()
        {
            return new VerifyCS.Test
            {
                TestCode = """
                    // <auto-generated/>
                    namespace N
                    {
                        class Banned { }
                        class C
                        {
                            void M()
                            {
                                var c = {|#0:new Banned()|};
                            }
                        }
                    }
                    """,
                TestState =
                {
                    AdditionalFiles = { (BannedSymbolsFileName, "T:N.Banned") },
                    AnalyzerConfigFiles =
                    {
                        ("/.globalconfig", """
                            is_global = true
                            banned_api_analyzer.exclude_generated_code = false
                            """)
                    },
                },
                ExpectedDiagnostics =
                {
                    GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", "")
                }
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/82114")]
        public Task ExcludeGeneratedCode_DefaultBehavior()
        {
            return new VerifyCS.Test
            {
                TestCode = """
                    // <auto-generated/>
                    namespace N
                    {
                        class Banned { }
                        class C
                        {
                            void M()
                            {
                                var c = {|#0:new Banned()|};
                            }
                        }
                    }
                    """,
                TestState =
                {
                    AdditionalFiles = { (BannedSymbolsFileName, "T:N.Banned") },
                },
                ExpectedDiagnostics =
                {
                    GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", "")
                }
            }
            .RunAsync();
        }

        #endregion
    }
}
