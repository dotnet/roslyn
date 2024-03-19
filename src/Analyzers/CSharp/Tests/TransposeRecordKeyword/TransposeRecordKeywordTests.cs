// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.TransposeRecordKeyword;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TransposeRecordKeyword
{
    using VerifyCS = CSharpCodeFixVerifier<
        EmptyDiagnosticAnalyzer,
        CSharpTransposeRecordKeywordCodeFixProvider>;

    public class TransposeRecordKeywordTests
    {
        [Fact]
        public async Task TestStructRecord()
        {
            await new VerifyCS.Test
            {
                TestCode = @"struct {|CS9012:record|} C { }",
                FixedCode = @"record struct C { }",
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact]
        public async Task TestClassRecord()
        {
            await new VerifyCS.Test
            {
                TestCode = @"class {|CS9012:record|} C { }",
                FixedCode = @"record class C { }",
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithModifiers()
        {
            await new VerifyCS.Test
            {
                TestCode = @"public struct {|CS9012:record|} C { }",
                FixedCode = @"public record struct C { }",
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithComment()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                // my struct
                public struct {|CS9012:record|} C { }
                """,
                FixedCode = """
                // my struct
                public record struct C { }
                """,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithDocComment()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                /// <summary></summary>
                public struct {|CS9012:record|} C { }
                """,
                FixedCode = """
                /// <summary></summary>
                public record struct C { }
                """,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithAttributes1()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                [System.CLSCompliant(false)]
                struct {|CS9012:record|} C { }
                """,
                FixedCode = """
                [System.CLSCompliant(false)]
                record struct C { }
                """,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithAttributes2()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                [System.CLSCompliant(false)] struct {|CS9012:record|} C { }
                """,
                FixedCode = """
                [System.CLSCompliant(false)] record struct C { }
                """,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact]
        public async Task TestNestedRecord()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                class {|CS9012:record|} C
                {
                    struct {|CS9012:record|} D { }
                }
                """,
                FixedCode = """
                record class C
                {
                    record struct D { }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact]
        public async Task TestNestedRecordWithComments()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                // my class
                class {|CS9012:record|} C
                {
                    // my struct
                    struct {|CS9012:record|} D { }
                }
                """,
                FixedCode = """
                // my class
                record class C
                {
                    // my struct
                    record struct D { }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }

        [Fact]
        public async Task TestTriviaBeforeAfter()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                /*1*/
                class /**/
                /*3*/
                {|CS9012:record|} /*4*/ C { }
                """,
                FixedCode = """
                /*1*/
                record /**/
                /*3*/
                class /*4*/ C { }
                """,
                LanguageVersion = LanguageVersion.CSharp10
            }.RunAsync();
        }
    }
}
