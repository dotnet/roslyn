// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseIndexOrRangeOperator
{
    public class UseRangeOperatorTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseRangeOperatorDiagnosticAnalyzer(), new CSharpUseRangeOperatorCodeFixProvider());

        private static readonly CSharpParseOptions s_parseOptions =
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);

        private static readonly TestParameters s_testParameters =
            new TestParameters(parseOptions: s_parseOptions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestNotInCSharp7()
        {
            await TestMissingAsync(
@"
class C
{
    void Goo(string s)
    {
        var v = s.Substring([||]1, s.Length - 1);
    }
}", parameters: new TestParameters(
    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestWithMissingReference()
        {
            // We are explicitly *not* passing: CommonReferences="true" here.  We want to 
            // validate we don't crash with missing references.
            await TestMissingAsync(
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"">
        <Document>
class C
{
    void Goo(string s)
    {
        var v = s.Substring([||]1, s.Length - 1);
    }
}
        </Document>
    </Project>
</Workspace>");
        }

        [WorkItem(36909, "https://github.com/dotnet/roslyn/issues/36909")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestNotWithoutSystemRange()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    void Goo(string s)
    {
        var v = s.Substring([||]1, s.Length - 1);
    }
}", new TestParameters(parseOptions: s_parseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestSimple()
        {
            await TestAsync(
@"
namespace System { public struct Range { } }
class C
{
    void Goo(string s)
    {
        var v = s.Substring([||]1, s.Length - 1);
    }
}",
@"
namespace System { public struct Range { } }
class C
{
    void Goo(string s)
    {
        var v = s[1..];
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestComplexSubstraction()
        {
            await TestAsync(
@"
namespace System { public struct Range { } }
class C
{
    void Goo(string s, int bar, int baz)
    {
        var v = s.Substring([||]bar, s.Length - baz - bar);
    }
}",
@"
namespace System { public struct Range { } }
class C
{
    void Goo(string s, int bar, int baz)
    {
        var v = s[bar..^baz];
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestConstantSubtraction1()
        {
            await TestAsync(
@"
namespace System { public struct Range { } }
class C
{
    void Goo(string s)
    {
        var v = s.Substring([||]1, s.Length - 2);
    }
}",
@"
namespace System { public struct Range { } }
class C
{
    void Goo(string s)
    {
        var v = s[1..^1];
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestNonStringType()
        {
            await TestAsync(
@"
namespace System { public struct Range { } }
struct S { public S Slice(int start, int length); public int Length { get; } public S this[System.Range] { get; } }
class C
{
    void Goo(S s)
    {
        var v = s.Slice([||]1, s.Length - 2);
    }
}",
@"
namespace System { public struct Range { } }
struct S { public S Slice(int start, int length); public int Length { get; } public S this[System.Range] { get; } }
class C
{
    void Goo(S s)
    {
        var v = s[1..^1];
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestMethodToMethod()
        {
            await TestAsync(
@"
namespace System { public struct Range { } }
struct S { public int Slice(int start, int length); public int Length { get; } public int Slice(System.Range r); }
class C
{
    void Goo(S s)
    {
        var v = s.Slice([||]1, s.Length - 2);
    }
}",
@"
namespace System { public struct Range { } }
struct S { public int Slice(int start, int length); public int Length { get; } public int Slice(System.Range r); }
class C
{
    void Goo(S s)
    {
        var v = s.Slice(1..^1);
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
        public async Task TestFixAllInvocationToElementAccess1()
        {
            // Note: once the IOp tree has support for range operators, this should 
            // simplify even further.
            await TestAsync(
@"
namespace System { public struct Range { } }
class C
{
    void Goo(string s, string t)
    {
        var v = t.Substring(s.Substring({|FixAllInDocument:|}1, s.Length - 2)[0], t.Length - s.Substring(1, s.Length - 2)[0]);
    }
}",
@"
namespace System { public struct Range { } }
class C
{
    void Goo(string s, string t)
    {
        var v = t.Substring(s[1..^1][0], t.Length - s[1..^1][0]);
    }
}", parseOptions: s_parseOptions);
        }
    }
}
