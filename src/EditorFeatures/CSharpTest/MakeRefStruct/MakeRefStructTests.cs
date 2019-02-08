// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.MakeRefStruct;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeRefStruct
{
    public class MakeRefStructTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        private static readonly CSharpParseOptions s_parseOptions =
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3);

        private const string SpanDeclarationSourceText = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
";

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new MakeRefStructCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        public async Task FieldInNotRefStruct()
        {
            var text = CreateTestSource(@"
struct S
{
    Span<int>[||] m;
}
");
            var expected = CreateTestSource(@"
ref struct S
{
    Span<int> m;
}
");
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        public async Task FieldInNestedClassInsideNotRefStruct()
        {
            var text = CreateTestSource(@"
struct S
{
    class C
    {
        Span<int>[||] m;
    }
}
");
            await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        public async Task FieldStaticInRefStruct()
        {
            // Note: does not compile
            var text = CreateTestSource(@"
ref struct S
{
    static Span<int>[||] m;
}
");
            await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        public async Task FieldStaticInNotRefStruct()
        {
            var text = CreateTestSource(@"
struct S
{
    static Span<int>[||] m;
}
");
            // Note: still does not compile after fix
            var expected = CreateTestSource(@"
ref struct S
{
    static Span<int> m;
}
");
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        public async Task PropInNotRefStruct()
        {
            var text = CreateTestSource(@"
struct S
{
    Span<int>[||] M { get; }
}
");
            var expected = CreateTestSource(@"
ref struct S
{
    Span<int> M { get; }
}
");
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        public async Task PropInNestedClassInsideNotRefStruct()
        {
            // Note: does not compile
            var text = CreateTestSource(@"
struct S
{
    class C
    {
        Span<int>[||] M { get; }
    }
}
");
            await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        public async Task PropStaticInRefStruct()
        {
            // Note: does not compile
            var text = CreateTestSource(@"
ref struct S
{
    static Span<int>[||] M { get; }
}
");
            await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        public async Task PropStaticInNotRefStruct()
        {
            var text = CreateTestSource(@"
struct S
{
    static Span<int>[||] M { get; }
}
");
            // Note: still does not compile after fix
            var expected = CreateTestSource(@"
ref struct S
{
    static Span<int> M { get; }
}
");
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        public async Task PartialByRefStruct()
        {
            var text = CreateTestSource(@"
ref partial struct S
{
}

struct S
{
    Span<int>[||] M { get; }
}
");
            await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        public async Task PartialStruct()
        {
            var text = CreateTestSource(@"
partial struct S
{
}

partial struct S
{
    Span<int>[||] M { get; }
}
");
            var expected = CreateTestSource(@"
partial struct S
{
}

ref partial struct S
{
    Span<int>[||] M { get; }
}
");
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        public async Task ReadonlyPartialStruct()
        {
            var text = CreateTestSource(@"
partial struct S
{
}

readonly partial struct S
{
    Span<int>[||] M { get; }
}
");
            var expected = CreateTestSource(@"
partial struct S
{
}

readonly ref partial struct S
{
    Span<int>[||] M { get; }
}
");
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }

        private static string CreateTestSource(string testSource) => SpanDeclarationSourceText + testSource;
    }
}
