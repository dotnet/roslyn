// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.UseAsyncUsing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseAsyncUsing
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseAsyncUsing)]
    public partial class UseAsyncUsingTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpUseAsyncUsingCodeFixProvider());

        private static readonly string s_interface = @"
namespace System
{
    public interface IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask DisposeAsync();
    }
}
";

        [Fact]
        public async Task DoNotFireForUsing()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C : System.Disposable
{
    void M()
    {
        using (var i = [|new C()|]) { }
    }
    public void Dispose() => throw null;
}");
        }

        [Fact]
        public async Task FireForUsingWrongAsync()
        {
            await TestInRegularAndScriptAsync(
@"class C : System.IAsyncDisposable
{
    void M()
    {
        using (var i = [|new C()|]) { }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync() => throw null;
}" + s_interface,
@"class C : System.IAsyncDisposable
{
    void M()
    {
        using await (var i = new C()) { }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync() => throw null;
}" + s_interface);
        }
    }
}
