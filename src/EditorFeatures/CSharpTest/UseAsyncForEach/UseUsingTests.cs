// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.UseUsing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseUsing
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseUsing)]
    public partial class UseUsingTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpUseUsingCodeFixProvider());

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
        public async Task DoNotFireForAsyncUsing()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C : System.IAsyncDisposable
{
    void M()
    {
        using await (var i = [|new C()|]) { }
    }
    public System.Threading.Tasks.ValueTask DisposeAsync() => throw null;
}" + s_interface);
        }

        [Fact]
        public async Task FireForAsyncUsingWrongAsync()
        {
            await TestInRegularAndScriptAsync(
@"class C : System.IDisposable
{
    void M()
    {
        using await (var i = [|new C()|]) { }
    }
    public void Dispose() => throw null;
}",
@"class C : System.IDisposable
{
    void M()
    {
        using (var i = new C()) { }
    }
    public void Dispose() => throw null;
}");
        }
    }
}
