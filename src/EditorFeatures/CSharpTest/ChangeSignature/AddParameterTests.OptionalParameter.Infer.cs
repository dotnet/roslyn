// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature;

[Trait(Traits.Feature, Traits.Features.ChangeSignature)]
public sealed partial class ChangeSignatureTests : AbstractChangeSignatureTests
{
    [Fact]
    public async Task AddOptionalParameter_CallsiteInferred_NoOptions()
    {
        var updatedSignature = new[] {
            AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                void M$$()
                {
                    M();
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class C
            {
                void M(int a)
                {
                    M(TODO);
                }
            }
            """);
    }

    [Fact]
    public async Task AddOptionalParameter_CallsiteInferred_SingleLocal()
    {
        var updatedSignature = new[] {
            AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                void M$$()
                {
                    int x = 7;
                    M();
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class C
            {
                void M(int a)
                {
                    int x = 7;
                    M(x);
                }
            }
            """);
    }

    [Fact]
    public async Task AddOptionalParameter_CallsiteInferred_NotOnInaccessibleLocal()
    {
        var updatedSignature = new[] {
            AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                void M$$()
                {
                    M();
                    int x = 7;
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class C
            {
                void M(int a)
                {
                    M(TODO);
                    int x = 7;
                }
            }
            """);
    }

    [Fact]
    public async Task AddOptionalParameter_CallsiteInferred_MultipleLocals()
    {
        var updatedSignature = new[] {
            AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                void M$$()
                {
                    int x = 7;
                    int y = 8;
                    M();
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class C
            {
                void M(int a)
                {
                    int x = 7;
                    int y = 8;
                    M(y);
                }
            }
            """);
    }

    [Fact]
    public async Task AddOptionalParameter_CallsiteInferred_SingleParameter()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(0),
            AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                void M$$(int x)
                {
                    M(1);
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class C
            {
                void M(int x, int a)
                {
                    M(1, x);
                }
            }
            """);
    }

    [Fact]
    public async Task AddOptionalParameter_CallsiteInferred_SingleField()
    {
        var updatedSignature = new[] {
            AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                int x = 8;

                void M$$()
                {
                    M();
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class C
            {
                int x = 8;

                void M(int a)
                {
                    M(x);
                }
            }
            """);
    }

    [Fact]
    public async Task AddOptionalParameter_CallsiteInferred_SingleProperty()
    {
        var updatedSignature = new[] {
            AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                int X { get; set; }

                void M$$()
                {
                    M();
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class C
            {
                int X { get; set; }

                void M(int a)
                {
                    M(X);
                }
            }
            """);
    }

    [Fact]
    public async Task AddOptionalParameter_CallsiteInferred_ImplicitlyConvertable()
    {
        var updatedSignature = new[] {
            AddedParameterOrExistingIndex.CreateAdded("B", "b", CallSiteKind.Inferred) };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class B { }
            class D : B { }

            class C
            {
                void M$$()
                {
                    D d = null;
                    M();
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class B { }
            class D : B { }

            class C
            {
                void M(B b)
                {
                    D d = null;
                    M(d);
                }
            }
            """);
    }
}
