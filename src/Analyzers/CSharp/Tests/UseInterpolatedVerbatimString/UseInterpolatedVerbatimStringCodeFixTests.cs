// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseInterpolatedVerbatimString;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseInterpolatedVerbatimString;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseInterpolatedVerbatimString)]
public sealed class CSharpUseInterpolatedVerbatimStringCodeFixTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public CSharpUseInterpolatedVerbatimStringCodeFixTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpUseInterpolatedVerbatimStringCodeFixProvider());

    [Fact]
    public Task Simple()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var s = @[||]$"hello";
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    var s = $@"hello";
                }
            }
            """, parameters: TestParameters.Default.WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));

    [Fact]
    public Task AfterString()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var s = @$"hello"[||];
                }
            }
            """, parameters: TestParameters.Default.WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));

    [Fact]
    public Task InCall()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string x)
                {
                    var s = M(@[||]$"hello");
                }
            }
            """,
            """
            class C
            {
                void M(string x)
                {
                    var s = M($@"hello");
                }
            }
            """, parameters: TestParameters.Default.WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));

    [Fact]
    public Task FixAllInDocument()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var s = {|FixAllInDocument:@$"|}hello";
                    var s2 = @$"hello";
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    var s = $@"hello";
                    var s2 = $@"hello";
                }
            }
            """, parameters: TestParameters.Default.WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));

    [Fact]
    public Task MissingOnInterpolatedVerbatimString()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    var s = $[||]@"hello";
                }
            }
            """, parameters: TestParameters.Default.WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));

    [Fact]
    public Task MissingInCSharp8()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    var s = @[||]$"hello";
                }
            }
            """, parameters: TestParameters.Default.WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp8)));
}
