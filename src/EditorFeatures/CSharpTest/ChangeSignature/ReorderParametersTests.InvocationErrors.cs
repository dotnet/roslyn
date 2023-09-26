// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    [Trait(Traits.Feature, Traits.Features.ChangeSignature)]
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [Fact]
        public async Task ReorderMethodParameters_InvokeOnClassName_ShouldFail()
        {
            var markup = """
                using System;
                class MyClass$$
                {
                    public void Goo(int x, string y)
                    {
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, expectedSuccess: false, expectedFailureReason: ChangeSignatureFailureKind.IncorrectKind);
        }

        [Fact]
        public async Task ReorderMethodParameters_InvokeOnField_ShouldFail()
        {
            var markup = """
                using System;
                class MyClass
                {
                    int t$$ = 2;

                    public void Goo(int x, string y)
                    {
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, expectedSuccess: false, expectedFailureReason: ChangeSignatureFailureKind.IncorrectKind);
        }

        [Fact]
        public async Task ReorderMethodParameters_CanBeStartedEvenWithNoParameters()
        {
            var markup = @"class C { void $$M() { } }";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, expectedSuccess: true);
        }

        [Fact]
        public async Task ReorderMethodParameters_InvokeOnOverloadedOperator_ShouldFail()
        {
            var markup = """
                class C
                {
                    public static C $$operator +(C a, C b)
                    {
                        return null;
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, expectedSuccess: false, expectedFailureReason: ChangeSignatureFailureKind.IncorrectKind);
        }
    }
}
