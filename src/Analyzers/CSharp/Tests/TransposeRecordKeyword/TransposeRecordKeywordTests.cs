// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.TransposeRecordKeyword;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TransposeRecordKeyword;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpTransposeRecordKeywordCodeFixProvider>;

public sealed class TransposeRecordKeywordTests
{
    [Fact]
    public Task TestStructRecord()
        => new VerifyCS.Test
        {
            TestCode = @"struct {|CS9012:record|} C { }",
            FixedCode = @"record struct C { }",
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();

    [Fact]
    public Task TestClassRecord()
        => new VerifyCS.Test
        {
            TestCode = @"class {|CS9012:record|} C { }",
            FixedCode = @"record class C { }",
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();

    [Fact]
    public Task TestWithModifiers()
        => new VerifyCS.Test
        {
            TestCode = @"public struct {|CS9012:record|} C { }",
            FixedCode = @"public record struct C { }",
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();

    [Fact]
    public Task TestWithComment()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithDocComment()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithAttributes1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithAttributes2()
        => new VerifyCS.Test
        {
            TestCode = """
            [System.CLSCompliant(false)] struct {|CS9012:record|} C { }
            """,
            FixedCode = """
            [System.CLSCompliant(false)] record struct C { }
            """,
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();

    [Fact]
    public Task TestNestedRecord()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNestedRecordWithComments()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTriviaBeforeAfter()
        => new VerifyCS.Test
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
