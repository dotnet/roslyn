// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.UseAsyncForEach;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseAsyncForEach
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseAsyncForEach)]
    public partial class UseAsyncForEachTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpUseAsyncForEachCodeFixProvider());

        [Fact]
        public async Task DoNotFireForForEach()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        foreach (var i in [|new C()|]) { }
    }
    public Enumerator GetEnumerator() => throw null;
    public sealed class Enumerator
    {
        public bool MoveNext() => throw null;
        public int Current { get => throw null; }
    }
}");
        }

        [Fact]
        public async Task FireForForEachWrongAsync()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        foreach (var i in [|new C()|]) { }
    }
    public Enumerator GetAsyncEnumerator() => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
        public int Current { get => throw null; }
    }
}",
@"class C
{
    void M()
    {
        foreach await (var i in new C()) { }
    }
    public Enumerator GetAsyncEnumerator() => throw null;
    public sealed class Enumerator
    {
        public System.Threading.Tasks.Task<bool> MoveNextAsync() => throw null;
        public int Current { get => throw null; }
    }
}");
        }
    }
}
