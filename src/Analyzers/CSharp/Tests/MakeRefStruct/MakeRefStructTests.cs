// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.MakeRefStruct;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeRefStruct;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
public class MakeRefStructTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    private static readonly CSharpParseOptions s_parseOptions =
        CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3);

    public MakeRefStructTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    private const string SpanDeclarationSourceText = """
        using System;
        namespace System
        {
            public readonly ref struct Span<T> 
            {
                unsafe public Span(void* pointer, int length) { }
            }
        }


        """;

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new MakeRefStructCodeFixProvider());

    [Fact]
    public async Task FieldInNotRefStruct()
    {
        var text = CreateTestSource("""
            struct S
            {
                Span<int>[||] m;
            }
            """);
        var expected = CreateTestSource("""
            ref struct S
            {
                Span<int> m;
            }
            """);
        await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
    }

    [Fact]
    public async Task FieldInRecordStruct()
    {
        var text = CreateTestSource("""
            record struct S
            {
                Span<int>[||] m;
            }
            """);
        await TestMissingInRegularAndScriptAsync(text, new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12)));
    }

    [Fact]
    public async Task FieldInNestedClassInsideNotRefStruct()
    {
        var text = CreateTestSource("""
            struct S
            {
                class C
                {
                    Span<int>[||] m;
                }
            }
            """);
        await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
    }

    [Fact]
    public async Task FieldStaticInRefStruct()
    {
        // Note: does not compile
        var text = CreateTestSource("""
            ref struct S
            {
                static Span<int>[||] m;
            }
            """);
        await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
    }

    [Fact]
    public async Task FieldStaticInNotRefStruct()
    {
        var text = CreateTestSource("""
            struct S
            {
                static Span<int>[||] m;
            }
            """);
        // Note: still does not compile after fix
        var expected = CreateTestSource("""
            ref struct S
            {
                static Span<int> m;
            }
            """);
        await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
    }

    [Fact]
    public async Task PropInNotRefStruct()
    {
        var text = CreateTestSource("""
            struct S
            {
                Span<int>[||] M { get; }
            }
            """);
        var expected = CreateTestSource("""
            ref struct S
            {
                Span<int> M { get; }
            }
            """);
        await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
    }

    [Fact]
    public async Task PropInNestedClassInsideNotRefStruct()
    {
        // Note: does not compile
        var text = CreateTestSource("""
            struct S
            {
                class C
                {
                    Span<int>[||] M { get; }
                }
            }
            """);
        await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
    }

    [Fact]
    public async Task PropStaticInRefStruct()
    {
        // Note: does not compile
        var text = CreateTestSource("""
            ref struct S
            {
                static Span<int>[||] M { get; }
            }
            """);
        await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
    }

    [Fact]
    public async Task PropStaticInNotRefStruct()
    {
        var text = CreateTestSource("""
            struct S
            {
                static Span<int>[||] M { get; }
            }
            """);
        // Note: still does not compile after fix
        var expected = CreateTestSource("""
            ref struct S
            {
                static Span<int> M { get; }
            }
            """);
        await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
    }

    [Fact]
    public async Task PartialByRefStruct()
    {
        var text = CreateTestSource("""
            ref partial struct S
            {
            }

            struct S
            {
                Span<int>[||] M { get; }
            }
            """);
        await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
    }

    [Fact]
    public async Task PartialStruct()
    {
        var text = CreateTestSource("""
            partial struct S
            {
            }

            partial struct S
            {
                Span<int>[||] M { get; }
            }
            """);
        var expected = CreateTestSource("""
            partial struct S
            {
            }

            ref partial struct S
            {
                Span<int>[||] M { get; }
            }
            """);
        await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
    }

    [Fact]
    public async Task ReadonlyPartialStruct()
    {
        var text = CreateTestSource("""
            partial struct S
            {
            }

            readonly partial struct S
            {
                Span<int>[||] M { get; }
            }
            """);
        var expected = CreateTestSource("""
            partial struct S
            {
            }

            readonly ref partial struct S
            {
                Span<int>[||] M { get; }
            }
            """);
        await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
    }

    private static string CreateTestSource(string testSource) => SpanDeclarationSourceText + testSource;
}
