// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature;

[Trait(Traits.Feature, Traits.Features.ChangeSignature)]
public sealed partial class ChangeSignatureTests : AbstractChangeSignatureTests
{
    [Fact]
    public Task ReorderMethodParameters_InvokeOnClassName_ShouldFail()
        => TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            using System;
            class MyClass$$
            {
                public void Goo(int x, string y)
                {
                }
            }
            """, expectedSuccess: false, expectedFailureReason: ChangeSignatureFailureKind.IncorrectKind);

    [Fact]
    public Task ReorderMethodParameters_InvokeOnField_ShouldFail()
        => TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            using System;
            class MyClass
            {
                int t$$ = 2;

                public void Goo(int x, string y)
                {
                }
            }
            """, expectedSuccess: false, expectedFailureReason: ChangeSignatureFailureKind.IncorrectKind);

    [Fact]
    public Task ReorderMethodParameters_CanBeStartedEvenWithNoParameters()
        => TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, @"class C { void $$M() { } }", expectedSuccess: true);

    [Fact]
    public Task ReorderMethodParameters_InvokeOnOverloadedOperator_ShouldFail()
        => TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                public static C $$operator +(C a, C b)
                {
                    return null;
                }
            }
            """, expectedSuccess: false, expectedFailureReason: ChangeSignatureFailureKind.IncorrectKind);
}
