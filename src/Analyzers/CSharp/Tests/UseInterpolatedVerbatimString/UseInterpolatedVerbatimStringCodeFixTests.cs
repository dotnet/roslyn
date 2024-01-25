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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseInterpolatedVerbatimString
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseInterpolatedVerbatimString)]
    public class CSharpUseInterpolatedVerbatimStringCodeFixTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
    {
        public CSharpUseInterpolatedVerbatimStringCodeFixTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpUseInterpolatedVerbatimStringCodeFixProvider());

        [Fact]
        public async Task Simple()
        {
            await TestInRegularAndScript1Async(
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
                """, parameters: new TestParameters().WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));
        }

        [Fact]
        public async Task AfterString()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    void M()
                    {
                        var s = @$"hello"[||];
                    }
                }
                """, parameters: new TestParameters().WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));
        }

        [Fact]
        public async Task InCall()
        {
            await TestInRegularAndScript1Async(
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
                """, parameters: new TestParameters().WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));
        }

        [Fact]
        public async Task FixAllInDocument()
        {
            await TestInRegularAndScript1Async(
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
                """, parameters: new TestParameters().WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));
        }

        [Fact]
        public async Task MissingOnInterpolatedVerbatimString()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        var s = $[||]@"hello";
                    }
                }
                """, parameters: new TestParameters().WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));
        }

        [Fact]
        public async Task MissingInCSharp8()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        var s = @[||]$"hello";
                    }
                }
                """, parameters: new TestParameters().WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp8)));
        }
    }
}
