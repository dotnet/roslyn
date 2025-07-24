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
            await VerifyCS.VerifyRefactoringAsync("""
                public class C
                {
                    string s = @"
                [||]class D { } //
                ";
                }
                """, """
                public class C
                {
                    string s = @"
                class D { } // 1
                ";
                }
                """);
        }

        [Fact]
        public async Task TestAsync_RawStringLiteral()
        {
            await VerifyCSharp11Async(""""
public class C
{
    string s = """
[||]class D { } //
""";
}
"""", """"
public class C
{
    string s = """
class D { } // 1
""";
}
"""");
        }

        [Fact]
        public async Task TestAsync_RawStringLiteral_Indented()
        {
            await VerifyCSharp11Async(""""
public class C
{
    string s = """
        [||]class D { } //
        """;
}
"""", """"
public class C
{
    string s = """
        class D { } // 1
        """;
}
"""");
        }

        [Fact]
        public async Task TestAsync_RawStringLiteral_Indented_Multiple()
        {
            await VerifyCSharp11Async(""""
public class C
{
    string s = """
        [||]class D { } //
        class E { } //,
        """;
}
"""", """"
public class C
{
    string s = """
        class D { } // 1
        class E { } // 2, 3
        """;
}
"""");
        }

        [Fact]
        public async Task CSharp_VerifyFix_WithTriviaAsync()
        {
            await VerifyCS.VerifyRefactoringAsync("""
                public class C
                {
                    string s =
                [||]/*before*/ @"
                class D { } //
                " /*after*/ ;
                }
                """, """
                public class C
                {
                    string s =
                /*before*/ @"
                class D { } // 1
                " /*after*/ ;
                }
                """);
        }

        [Fact]
        public async Task CSharp_VerifyFix_NonNumberCommentsLeftAloneAsync()
        {
            await VerifyCS.VerifyRefactoringAsync("""
                public class C
                {
                    string s = @"
                [||]//
                class D //
                {//
                } // test
                ";
                }
                """, """
                public class C
                {
                    string s = @"
                //
                class D // 1
                {//
                } // test
                ";
                }
                """);
        }

        [Fact]
        public async Task CSharp_VerifyFix_MultipleCommasAsync()
        {
            await VerifyCS.VerifyRefactoringAsync("""
                public class C
                {
                    string s = @"
                [||]class D //1
                { //,
                } //
                ";
                }
                """, """
                public class C
                {
                    string s = @"
                class D // 1
                { // 2, 3
                } // 4
                ";
                }
                """);
        }

        [Fact]
        public async Task CSharp_VerifyFix_LastLineAsync()
        {
            await VerifyCS.VerifyRefactoringAsync("""
                public class C
                {
                    string s = @"[||]class D { } //";
                }
                """, """
                public class C
                {
                    string s = @"class D { } // 1";
                }
                """);
        }

        [Fact]
        public async Task CountOverTenAsync()
        {
            await VerifyCS.VerifyRefactoringAsync("""
                public class C
                {
                    string s = @"
                [||]class D { } // ,,,,,,,,,,,,
                ";
                }
                """, """
                public class C
                {
                    string s = @"
                class D { } // 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13
                ";
                }
                """);
        }

        [Fact]
        public async Task EmptyNumberIsImproperAsync()
        {
            await VerifyCS.VerifyRefactoringAsync("""
                public class C
                {
                    string s = @"
                [||]class D // 1
                { // 2, 3
                } //
                ";
                }
                """, """
                public class C
                {
                    string s = @"
                class D // 1
                { // 2, 3
                } // 4
                ";
                }
                """);
        }

        [Fact]
        public async Task EmptyNumberBeforeCommaIsImproperAsync()
        {
            await VerifyCS.VerifyRefactoringAsync("""
                public class C
                {
                    string s = @"
                [||]class C // 1
                { // , 3
                }
                ";
                }
                """, """
                public class C
                {
                    string s = @"
                class C // 1
                { // 2, 3
                }
                ";
                }
                """);
        }

        [Fact]
        public async Task EmptyCommentOnEmptyLineIsProperAsync()
        {
            await VerifyCS.VerifyRefactoringAsync("""
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
                """, """
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
                """);
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
