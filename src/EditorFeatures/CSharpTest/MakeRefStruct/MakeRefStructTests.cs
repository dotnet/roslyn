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

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpMakeRefStructCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(31831, "https://github.com/dotnet/roslyn/issues/31831")]
        public async Task FieldInNotRefStruct()
        {
            var text = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}

struct S
{
    Span<int>[||] m;
}
";

            var expected = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}

ref struct S
{
    Span<int> m;
}
";
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(31831, "https://github.com/dotnet/roslyn/issues/31831")]
        public async Task FieldInNestedClassInsideNotRefStruct()
        {
            var text = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}

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
        [WorkItem(31831, "https://github.com/dotnet/roslyn/issues/31831")]
        public async Task FieldStaticInRefStruct()
        {
            var text = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}

ref struct S
{
    static Span<int>[||] m;
}
";
            await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(31831, "https://github.com/dotnet/roslyn/issues/31831")]
        public async Task FieldStaticInNotRefStruct()
        {
            var text = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}

struct S
{
    static Span<int>[||] m;
}
";
            var expected = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}

ref struct S
{
    static Span<int> m;
}
";
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(31831, "https://github.com/dotnet/roslyn/issues/31831")]
        public async Task PropInNotRefStruct()
        {
            var text = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}

struct S
{
    Span<int>[||] M { get; }
}
";

            var expected = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}

ref struct S
{
    Span<int> M { get; }
}
";
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(31831, "https://github.com/dotnet/roslyn/issues/31831")]
        public async Task PropInNestedClassInsideNotRefStruct()
        {
            var text = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}

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
        [WorkItem(31831, "https://github.com/dotnet/roslyn/issues/31831")]
        public async Task PropStaticInRefStruct()
        {
            var text = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}

ref struct S
{
    static Span<int>[||] M { get; }
}
";
            await TestMissingInRegularAndScriptAsync(text, new TestParameters(s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeRefStruct)]
        [WorkItem(31831, "https://github.com/dotnet/roslyn/issues/31831")]
        public async Task PropStaticInNotRefStruct()
        {
            var text = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}

struct S
{
    static Span<int>[||] M { get; }
}
";
            var expected = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}

ref struct S
{
    static Span<int> M { get; }
}
";
            await TestInRegularAndScriptAsync(text, expected, parseOptions: s_parseOptions);
        }
    }
}
