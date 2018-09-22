// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddNonNullTypes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddNonNullTypes
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddNonNullTypes)]
    public class AddNonNullTypesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpAddNonNullTypesCodeFixProvider());

        [Fact]
        public async Task TestNullableOnMethodInClass()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    [|string?|] M() { }
}",
@"
[System.Runtime.CompilerServices.NonNullTypes]
class C
{
    string? M() { }
}");
        }

        [Fact]
        public async Task TestNullableInNonNullTypesFalseContext()
        {
            // https://github.com/dotnet/roslyn/issues/30099 We should detect present of NonNullTypes(false) and either not trigger, or toggle to `true`
            await TestInRegularAndScriptAsync(
@"
[System.Runtime.CompilerServices.NonNullTypes(false)]
class C
{
    [|string?|] M() { }
}",
@"
[System.Runtime.CompilerServices.NonNullTypes(false)]
[System.Runtime.CompilerServices.NonNullTypes]
class C
{
    string? M() { }
}");
        }

        [Fact]
        public async Task TestNullableWithUsing()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Runtime.CompilerServices;
class C
{
    [|string?|] M() { }
}",
@"
using System.Runtime.CompilerServices;

[NonNullTypes]
class C
{
    string? M() { }
}");
        }

        [Fact]
        public async Task TestNullableOnMethodInInterface()
        {
            await TestInRegularAndScriptAsync(
@"
interface I
{
    [|string?|] M();
}",
@"
[System.Runtime.CompilerServices.NonNullTypes]
interface I
{
    string? M();
}");
        }

        [Fact]
        public async Task TestNullableInNestedClass()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    class Nested
    {
        [|string?|] M();
    }
}",
@"
class C
{
    [System.Runtime.CompilerServices.NonNullTypes]
    class Nested
    {
        string? M();
    }
}");
        }

        [Fact]
        public async Task TestSuppression()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    string M() { return [|null!|]; }
}",
@"
[System.Runtime.CompilerServices.NonNullTypes]
class C
{
    string M() { return null!; }
}");
        }
    }
}
