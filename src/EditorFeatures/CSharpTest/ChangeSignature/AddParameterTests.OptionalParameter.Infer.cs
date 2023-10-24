// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    [Trait(Traits.Feature, Traits.Features.ChangeSignature)]
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [Fact]
        public async Task AddOptionalParameter_CallsiteInferred_NoOptions()
        {
            var markup = """
                class C
                {
                    void M$$()
                    {
                        M();
                    }
                }
                """;
            var updatedSignature = new[] {
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
            var updatedCode = """
                class C
                {
                    void M(int a)
                    {
                        M(TODO);
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddOptionalParameter_CallsiteInferred_SingleLocal()
        {
            var markup = """
                class C
                {
                    void M$$()
                    {
                        int x = 7;
                        M();
                    }
                }
                """;
            var updatedSignature = new[] {
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
            var updatedCode = """
                class C
                {
                    void M(int a)
                    {
                        int x = 7;
                        M(x);
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddOptionalParameter_CallsiteInferred_NotOnInaccessibleLocal()
        {
            var markup = """
                class C
                {
                    void M$$()
                    {
                        M();
                        int x = 7;
                    }
                }
                """;
            var updatedSignature = new[] {
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
            var updatedCode = """
                class C
                {
                    void M(int a)
                    {
                        M(TODO);
                        int x = 7;
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddOptionalParameter_CallsiteInferred_MultipleLocals()
        {
            var markup = """
                class C
                {
                    void M$$()
                    {
                        int x = 7;
                        int y = 8;
                        M();
                    }
                }
                """;
            var updatedSignature = new[] {
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
            var updatedCode = """
                class C
                {
                    void M(int a)
                    {
                        int x = 7;
                        int y = 8;
                        M(y);
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddOptionalParameter_CallsiteInferred_SingleParameter()
        {
            var markup = """
                class C
                {
                    void M$$(int x)
                    {
                        M(1);
                    }
                }
                """;
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(0),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
            var updatedCode = """
                class C
                {
                    void M(int x, int a)
                    {
                        M(1, x);
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddOptionalParameter_CallsiteInferred_SingleField()
        {
            var markup = """
                class C
                {
                    int x = 8;

                    void M$$()
                    {
                        M();
                    }
                }
                """;
            var updatedSignature = new[] {
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
            var updatedCode = """
                class C
                {
                    int x = 8;

                    void M(int a)
                    {
                        M(x);
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddOptionalParameter_CallsiteInferred_SingleProperty()
        {
            var markup = """
                class C
                {
                    int X { get; set; }

                    void M$$()
                    {
                        M();
                    }
                }
                """;
            var updatedSignature = new[] {
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Inferred) };
            var updatedCode = """
                class C
                {
                    int X { get; set; }

                    void M(int a)
                    {
                        M(X);
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddOptionalParameter_CallsiteInferred_ImplicitlyConvertable()
        {
            var markup = """
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
                """;
            var updatedSignature = new[] {
                AddedParameterOrExistingIndex.CreateAdded("B", "b", CallSiteKind.Inferred) };
            var updatedCode = """
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
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }
    }
}
