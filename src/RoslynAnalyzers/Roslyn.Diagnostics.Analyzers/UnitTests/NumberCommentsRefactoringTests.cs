// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeRefactoringVerifier<Roslyn.Diagnostics.Analyzers.NumberCommentsRefactoring>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class NumberCommentsRefactoringTests
    {
        [Fact]
        public async Task TestAsync()
        {
            const string source = """
                public class C
                {
                    string s = @"
                [||]class D { } //
                ";
                }
                """;
            const string fixedSource = """
                public class C
                {
                    string s = @"
                class D { } // 1
                ";
                }
                """;
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task TestAsync_RawStringLiteral()
        {
            const string source = """"
public class C
{
    string s = """
[||]class D { } //
""";
}
"""";
            const string fixedSource = """"
public class C
{
    string s = """
class D { } // 1
""";
}
"""";
            await VerifyCSharp11Async(source, fixedSource);
        }

        [Fact]
        public async Task TestAsync_RawStringLiteral_Indented()
        {
            const string source = """"
public class C
{
    string s = """
        [||]class D { } //
        """;
}
"""";
            const string fixedSource = """"
public class C
{
    string s = """
        class D { } // 1
        """;
}
"""";
            await VerifyCSharp11Async(source, fixedSource);
        }

        [Fact]
        public async Task TestAsync_RawStringLiteral_Indented_Multiple()
        {
            const string source = """"
public class C
{
    string s = """
        [||]class D { } //
        class E { } //,
        """;
}
"""";
            const string fixedSource = """"
public class C
{
    string s = """
        class D { } // 1
        class E { } // 2, 3
        """;
}
"""";
            await VerifyCSharp11Async(source, fixedSource);
        }

        [Fact]
        public async Task CSharp_VerifyFix_WithTriviaAsync()
        {
            const string source = """
                public class C
                {
                    string s =
                [||]/*before*/ @"
                class D { } //
                " /*after*/ ;
                }
                """;
            const string fixedSource = """
                public class C
                {
                    string s =
                /*before*/ @"
                class D { } // 1
                " /*after*/ ;
                }
                """;
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task CSharp_VerifyFix_NonNumberCommentsLeftAloneAsync()
        {
            const string source = """
                public class C
                {
                    string s = @"
                [||]//
                class D //
                {//
                } // test
                ";
                }
                """;
            const string fixedSource = """
                public class C
                {
                    string s = @"
                //
                class D // 1
                {//
                } // test
                ";
                }
                """;
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task CSharp_VerifyFix_MultipleCommasAsync()
        {
            const string source = """
                public class C
                {
                    string s = @"
                [||]class D //1
                { //,
                } //
                ";
                }
                """;
            const string fixedSource = """
                public class C
                {
                    string s = @"
                class D // 1
                { // 2, 3
                } // 4
                ";
                }
                """;
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task CSharp_VerifyFix_LastLineAsync()
        {
            const string source = """
                public class C
                {
                    string s = @"[||]class D { } //";
                }
                """;
            const string fixedSource = """
                public class C
                {
                    string s = @"class D { } // 1";
                }
                """;
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task CountOverTenAsync()
        {
            const string source = """
                public class C
                {
                    string s = @"
                [||]class D { } // ,,,,,,,,,,,,
                ";
                }
                """;
            const string fixedSource = """
                public class C
                {
                    string s = @"
                class D { } // 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13
                ";
                }
                """;
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task EmptyNumberIsImproperAsync()
        {
            const string source = """
                public class C
                {
                    string s = @"
                [||]class D // 1
                { // 2, 3
                } //
                ";
                }
                """;

            const string fixedSource = """
                public class C
                {
                    string s = @"
                class D // 1
                { // 2, 3
                } // 4
                ";
                }
                """;

            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task EmptyNumberBeforeCommaIsImproperAsync()
        {
            const string source = """
                public class C
                {
                    string s = @"
                [||]class C // 1
                { // , 3
                }
                ";
                }
                """;

            const string fixedSource = """
                public class C
                {
                    string s = @"
                class C // 1
                { // 2, 3
                }
                ";
                }
                """;

            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task EmptyCommentOnEmptyLineIsProperAsync()
        {
            const string source = """
                public class C
                {
                    string s = @"
                // much stuff
                //
                // more stuff
                [||]class C // 1
                { // 2, 3
                } //
                ";
                }
                """;

            const string fixedSource = """
                public class C
                {
                    string s = @"
                // much stuff
                //
                // more stuff
                class C // 1
                { // 2, 3
                } // 4
                ";
                }
                """;

            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        #region Utilities
        private async Task VerifyCSharp11Async(string source, string fixedSource)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11,
            };

            test.ExpectedDiagnostics.AddRange(DiagnosticResult.EmptyDiagnosticResults);
            await test.RunAsync();
        }
        #endregion
    }
}
