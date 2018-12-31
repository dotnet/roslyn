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
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpMakeRefStructCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(32037, "https://github.com/dotnet/roslyn/pull/32037")]
        public async Task FieldInNotRefStruct()
        {
            var text = @"
struct S
{
    Span<int>[||] m;
}
";

            var expected = @"
ref struct S
{
    Span<int> m;
}
";
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(32037, "https://github.com/dotnet/roslyn/pull/32037")]
        public async Task FieldInNestedClassInsideNotRefStruct()
        {
            var text = @"
struct S
{
    class C
    {
        Span<int>[||] m;
    }
}
";
            await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(32037, "https://github.com/dotnet/roslyn/pull/32037")]
        public async Task FieldStaticInRefStruct()
        {
            var text = @"
ref struct S
{
    static Span<int>[||] m;
}
";
            await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(32037, "https://github.com/dotnet/roslyn/pull/32037")]
        public async Task FieldStaticInNotRefStruct()
        {
            var text = @"
struct S
{
    static Span<int>[||] m;
}
";
            var expected = @"
ref struct S
{
    static Span<int> m;
}
";
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(32037, "https://github.com/dotnet/roslyn/pull/32037")]
        public async Task PropInNotRefStruct()
        {
            var text = @"
struct S
{
    Span<int>[||] M { get; }
}
";

            var expected = @"
ref struct S
{
    Span<int> M { get; }
}
";
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(32037, "https://github.com/dotnet/roslyn/pull/32037")]
        public async Task PropInNestedClassInsideNotRefStruct()
        {
            var text = @"
struct S
{
    class C
    {
        Span<int>[||] M { get; }
    }
}
";
            await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(32037, "https://github.com/dotnet/roslyn/pull/32037")]
        public async Task PropStaticInRefStruct()
        {
            var text = @"
ref struct S
{
    static Span<int>[||] M { get; }
}
";
            await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(32037, "https://github.com/dotnet/roslyn/pull/32037")]
        public async Task PropStaticInNotRefStruct()
        {
            var text = @"
struct S
{
    static Span<int>[||] M { get; }
}
";
            var expected = @"
ref struct S
{
    static Span<int> M { get; }
}
";
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }
    }
}
