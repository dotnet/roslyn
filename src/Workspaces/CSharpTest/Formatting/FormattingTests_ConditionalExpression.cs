// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public sealed class FormattingTests_ConditionalExpression : CSharpFormattingTestBase
{
    [Fact]
    public Task TestConditionalExpressionWithinStatement()
        => AssertNoFormattingChangesAsync("""
            class Test
            {
                void Method()
                {
                    ErrorCode code = overridingMemberIsObsolete
                        ? ErrorCode.WRN_ObsoleteOverridingNonObsolete
                        : ErrorCode.WRN_NonObsoleteOverridingObsolete;
                }
            }
            """);

    [Fact]
    public Task TestConditionalExpressionWithinIfStatement()
        => AssertNoFormattingChangesAsync("""
            class Test
            {
                void Method()
                {
                    if (condition)
                    {
                        ErrorCode code = overridingMemberIsObsolete
                            ? ErrorCode.WRN_ObsoleteOverridingNonObsolete
                            : ErrorCode.WRN_NonObsoleteOverridingObsolete;
                    }
                }
            }
            """);

    [Fact]
    public Task TestConditionalExpressionNestedDeeply()
        => AssertNoFormattingChangesAsync("""
            class SourceMemberContainerSymbol
            {
                void checkOverride()
                {
                    if (overridingMemberIsObsolete != leastOverriddenMemberIsObsolete)
                    {
                        ErrorCode code = overridingMemberIsObsolete
                            ? ErrorCode.WRN_ObsoleteOverridingNonObsolete
                            : ErrorCode.WRN_NonObsoleteOverridingObsolete;

                        diagnostics.Add(code, overridingMemberLocation, overridingMember, leastOverriddenMember);
                    }
                }
            }
            """);

    [Fact]
    public Task TestConditionalExpressionMisalignedColon()
        => AssertFormatAsync("""
            class Test
            {
                void Method()
                {
                    ErrorCode code = overridingMemberIsObsolete
                        ? ErrorCode.WRN_ObsoleteOverridingNonObsolete
                : ErrorCode.WRN_NonObsoleteOverridingObsolete;
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    ErrorCode code = overridingMemberIsObsolete
                        ? ErrorCode.WRN_ObsoleteOverridingNonObsolete
                        : ErrorCode.WRN_NonObsoleteOverridingObsolete;
                }
            }
            """);

    [Fact]
    public Task TestConditionalExpressionOverIndentedColon()
        => AssertFormatAsync("""
            class Test
            {
                void Method()
                {
                    ErrorCode code = overridingMemberIsObsolete
                        ? ErrorCode.WRN_ObsoleteOverridingNonObsolete
                                : ErrorCode.WRN_NonObsoleteOverridingObsolete;
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    ErrorCode code = overridingMemberIsObsolete
                        ? ErrorCode.WRN_ObsoleteOverridingNonObsolete
                        : ErrorCode.WRN_NonObsoleteOverridingObsolete;
                }
            }
            """);
}
