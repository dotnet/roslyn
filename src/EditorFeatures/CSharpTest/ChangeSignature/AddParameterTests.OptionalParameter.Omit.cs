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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature;

[Trait(Traits.Feature, Traits.Features.ChangeSignature)]
public sealed partial class ChangeSignatureTests : AbstractChangeSignatureTests
{
    [WpfFact]
    public async Task AddOptionalParameter_ToEmptySignature_CallsiteOmitted()
    {
        var updatedSignature = new[] {
            AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Omitted, isRequired: false, defaultValue: "1") };
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
                void M(int a = 1)
                {
                    M();
                }
            }
            """);
    }

    [WpfFact]
    public async Task AddOptionalParameter_AfterRequiredParameter_CallsiteOmitted()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(0),
            AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Omitted, isRequired: false, defaultValue: "1") };
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
                void M(int x, int a = 1)
                {
                    M(1);
                }
            }
            """);
    }

    [WpfFact]
    public async Task AddOptionalParameter_BeforeOptionalParameter_CallsiteOmitted()
    {
        var updatedSignature = new[] {
            AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Omitted, isRequired: false, defaultValue: "1"),
            new AddedParameterOrExistingIndex(0) };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                void M$$(int x = 2)
                {
                    M()
                    M(2);
                    M(x: 2);
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class C
            {
                void M(int a = 1, int x = 2)
                {
                    M()
                    M(x: 2);
                    M(x: 2);
                }
            }
            """);
    }

    [WpfFact]
    public async Task AddOptionalParameter_BeforeExpandedParamsArray_CallsiteOmitted()
    {
        var updatedSignature = new[] {
            AddedParameterOrExistingIndex.CreateAdded("System.Int32", "a", CallSiteKind.Omitted, isRequired: false, defaultValue: "1"),
            new AddedParameterOrExistingIndex(0) };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                void M$$(params int[] p)
                {
                    M();
                    M(1);
                    M(1, 2);
                    M(1, 2, 3);
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class C
            {
                void M(int a = 1, params int[] p)
                {
                    M();
                    M(p: new int[] { 1 });
                    M(p: new int[] { 1, 2 });
                    M(p: new int[] { 1, 2, 3 });
                }
            }
            """);
    }

    [WpfFact]
    public async Task AddOptionalParameterWithOmittedCallsiteToAttributeConstructor()
    {
        var permutation = new[] {
            new AddedParameterOrExistingIndex(0),
            new AddedParameterOrExistingIndex(1),
            AddedParameterOrExistingIndex.CreateAdded("int", "x", CallSiteKind.Omitted, isRequired: false, defaultValue: "3"),
            new AddedParameterOrExistingIndex(2)};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            [Some(1, 2, 4)]
            class SomeAttribute : System.Attribute
            {
                public SomeAttribute$$(int a, int b, int y = 4)
                {
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            [Some(1, 2, y: 4)]
            class SomeAttribute : System.Attribute
            {
                public SomeAttribute(int a, int b, int x = 3, int y = 4)
                {
                }
            }
            """);
    }
}
