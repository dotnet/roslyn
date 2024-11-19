// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.UseTupleSwap;
using Microsoft.CodeAnalysis.CSharp.UseUnboundTypeInNameOf;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseUnboundTypeInNameOf;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseUnboundTypeInNameOfDiagnosticAnalyzer,
    CSharpUseUnboundTypeInNameOfCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseUnboundTypeInNameOf)]
public partial class UseUnboundTypeInNameOfTests
{
    [Fact]
    public async Task TestBaseCase()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = [|nameof|](List<int>);
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(List<>);
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMissingBeforeCSharp14()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(List<int>);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMissingWithFeatureOff()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(List<int>);
                    }
                }
                """,
            Options =
            {
                { CSharpCodeStyleOptions.PreferUnboundTypeInNameOf, false, CodeStyle.NotificationOption2.Silent }
            },
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }
}
