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
        [WpfFact]
        public async Task AddOptionalParameter_ToConstructor()
        {
            var markup = """
                class B
                {
                    public B() : this(1) { }
                    public B$$(int a)
                    {
                        var q = new B(1);
                    }
                }

                class D : B
                {
                    public D() : base(1) { }
                }
                """;
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(0),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "x", CallSiteKind.Value, callSiteValue: "100", isRequired: false, defaultValue: "10"),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "y", CallSiteKind.Omitted, isRequired: false, defaultValue: "11"),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "z", CallSiteKind.Value, callSiteValue: "102", isRequired: false, defaultValue: "12")};
            var updatedCode = """
                class B
                {
                    public B() : this(1, 100, z: 102) { }
                    public B(int a, int x = 10, int y = 11, int z = 12)
                    {
                        var q = new B(1, 100, z: 102);
                    }
                }

                class D : B
                {
                    public D() : base(1, 100, z: 102) { }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/44126")]
        [WpfFact]
        public async Task AddOptionalParameter_ToConstructor_ImplicitObjectCreation()
        {
            var markup = """
                class B
                {
                    public B() : this(1) { }
                    public B$$(int a)
                    {
                        B q = new(1);
                    }
                }

                class D : B
                {
                    public D() : base(1) { }
                }
                """;
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(0),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "x", CallSiteKind.Value, callSiteValue: "100", isRequired: false, defaultValue: "10"),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "y", CallSiteKind.Omitted, isRequired: false, defaultValue: "11"),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "z", CallSiteKind.Value, callSiteValue: "102", isRequired: false, defaultValue: "12")};
            var updatedCode = """
                class B
                {
                    public B() : this(1, 100, z: 102) { }
                    public B(int a, int x = 10, int y = 11, int z = 12)
                    {
                        B q = new(1, 100, z: 102);
                    }
                }

                class D : B
                {
                    public D() : base(1, 100, z: 102) { }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact]
        public async Task AddOptionalParameter_ToIndexer()
        {
            var markup = """
                class B
                {
                    public int this$$[int a] { get { return 5; } }

                    public void M()
                    {
                        var d = this[1];
                    }
                }
                """;
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(0),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "x", CallSiteKind.Value, callSiteValue: "100", isRequired: false, defaultValue: "10"),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "y", CallSiteKind.Omitted, isRequired: false, defaultValue: "11"),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "z", CallSiteKind.Value, callSiteValue: "102", isRequired: false, defaultValue: "12")};
            var updatedCode = """
                class B
                {
                    public int this[int a, int x = 10, int y = 11, int z = 12] { get { return 5; } }

                    public void M()
                    {
                        var d = this[1, 100, z: 102];
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact]
        public async Task AddOptionalParameter_ToAttribute()
        {
            var markup = """
                [My(1)]
                class MyAttribute : System.Attribute
                {
                    public MyAttribute($$int a) { }
                }
                """;
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(0),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "x", CallSiteKind.Value, callSiteValue: "100", isRequired: false, defaultValue: "10"),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "y", CallSiteKind.Omitted, isRequired: false, defaultValue: "11"),
                AddedParameterOrExistingIndex.CreateAdded("System.Int32", "z", CallSiteKind.Value, callSiteValue: "102", isRequired: false, defaultValue: "12")};

            // TODO: The = in the attribute is a bug. You cannot specify that the attribute should use : instead in the SyntaxGenerator
            // https://github.com/dotnet/roslyn/issues/43354
            var updatedCode = """
                [My(1, 100, z = 102)]
                class MyAttribute : System.Attribute
                {
                    public MyAttribute(int a, int x = 10, int y = 11, int z = 12) { }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }
    }
}
