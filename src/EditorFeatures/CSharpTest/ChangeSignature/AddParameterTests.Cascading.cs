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
public partial class ChangeSignatureTests : AbstractChangeSignatureTests
{
    [Fact]
    public async Task AddParameter_Cascade_ToImplementedMethod()
    {
        var markup = """
            interface I
            {
                void M(int x, string y);
            }

            class C : I
            {
                $$public void M(int x, string y)
                { }
            }
            """;
        var permutation = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(0)
        };
        var updatedCode = """
            interface I
            {
                void M(string y, int newIntegerParameter, int x);
            }

            class C : I
            {
                public void M(string y, int newIntegerParameter, int x)
                { }
            }
            """;

        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
    }

    [Fact]
    public async Task AddParameter_Cascade_ToImplementedMethod_WithTuples()
    {
        var markup = """
            interface I
            {
                void M((int, int) x, (string a, string b) y);
            }

            class C : I
            {
                $$public void M((int, int) x, (string a, string b) y)
                { }
            }
            """;
        var permutation = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(0)
        };
        var updatedCode = """
            interface I
            {
                void M((string a, string b) y, int newIntegerParameter, (int, int) x);
            }

            class C : I
            {
                public void M((string a, string b) y, int newIntegerParameter, (int, int) x)
                { }
            }
            """;

        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
    }

    [Fact]
    public async Task AddParameter_Cascade_ToImplementingMethod()
    {
        var markup = """
            interface I
            {
                $$void M(int x, string y);
            }

            class C : I
            {
                public void M(int x, string y)
                { }
            }
            """;
        var permutation = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(0)
        };
        var updatedCode = """
            interface I
            {
                void M(string y, int newIntegerParameter, int x);
            }

            class C : I
            {
                public void M(string y, int newIntegerParameter, int x)
                { }
            }
            """;

        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
    }

    [Fact]
    public async Task AddParameter_Cascade_ToOverriddenMethod()
    {
        var markup = """
            class B
            {
                public virtual void M(int x, string y)
                { }
            }

            class D : B
            {
                $$public override void M(int x, string y)
                { }
            }
            """;
        var permutation = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(0)
        };
        var updatedCode = """
            class B
            {
                public virtual void M(string y, int newIntegerParameter, int x)
                { }
            }

            class D : B
            {
                public override void M(string y, int newIntegerParameter, int x)
                { }
            }
            """;

        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
    }

    [Fact]
    public async Task AddParameter_Cascade_ToOverridingMethod()
    {
        var markup = """
            class B
            {
                $$public virtual void M(int x, string y)
                { }
            }

            class D : B
            {
                public override void M(int x, string y)
                { }
            }
            """;
        var permutation = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(0)
        };
        var updatedCode = """
            class B
            {
                public virtual void M(string y, int newIntegerParameter, int x)
                { }
            }

            class D : B
            {
                public override void M(string y, int newIntegerParameter, int x)
                { }
            }
            """;

        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
    }

    [Fact]
    public async Task AddParameter_Cascade_ToOverriddenMethod_Transitive()
    {
        var markup = """
            class B
            {
                public virtual void M(int x, string y)
                { }
            }

            class D : B
            {
                public override void M(int x, string y)
                { }
            }

            class D2 : D
            {
                $$public override void M(int x, string y)
                { }
            }
            """;
        var permutation = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(0)
        };
        var updatedCode = """
            class B
            {
                public virtual void M(string y, int newIntegerParameter, int x)
                { }
            }

            class D : B
            {
                public override void M(string y, int newIntegerParameter, int x)
                { }
            }

            class D2 : D
            {
                public override void M(string y, int newIntegerParameter, int x)
                { }
            }
            """;

        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
    }

    [Fact]
    public async Task AddParameter_Cascade_ToOverridingMethod_Transitive()
    {
        var markup = """
            class B
            {
                $$public virtual void M(int x, string y)
                { }
            }

            class D : B
            {
                public override void M(int x, string y)
                { }
            }

            class D2 : D
            {
                public override void M(int x, string y)
                { }
            }
            """;
        var permutation = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(0)
        };
        var updatedCode = """
            class B
            {
                public virtual void M(string y, int newIntegerParameter, int x)
                { }
            }

            class D : B
            {
                public override void M(string y, int newIntegerParameter, int x)
                { }
            }

            class D2 : D
            {
                public override void M(string y, int newIntegerParameter, int x)
                { }
            }
            """;

        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
    }

    [Fact]
    public async Task AddParameter_Cascade_ToMethods_Complex()
    {
        ////     B   I   I2
        ////      \ / \ /
        ////       D  (I3)
        ////      / \   \
        ////   $$D2  D3  C

        var markup = """
            class B { public virtual void M(int x, string y) { } }
            class D : B, I { public override void M(int x, string y) { } }
            class D2 : D { public override void $$M(int x, string y) { } }
            class D3 : D { public override void M(int x, string y) { } }
            interface I { void M(int x, string y); }
            interface I2 { void M(int x, string y); }
            interface I3 : I, I2 { }
            class C : I3 { public void M(int x, string y) { } }
            """;

        var permutation = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(0)
        };
        var updatedCode = """
            class B { public virtual void M(string y, int newIntegerParameter, int x) { } }
            class D : B, I { public override void M(string y, int newIntegerParameter, int x) { } }
            class D2 : D { public override void M(string y, int newIntegerParameter, int x) { } }
            class D3 : D { public override void M(string y, int newIntegerParameter, int x) { } }
            interface I { void M(string y, int newIntegerParameter, int x); }
            interface I2 { void M(string y, int newIntegerParameter, int x); }
            interface I3 : I, I2 { }
            class C : I3 { public void M(string y, int newIntegerParameter, int x) { } }
            """;

        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
    }

    [Fact]
    public async Task AddParameter_Cascade_ToMethods_WithDifferentParameterNames()
    {
        var markup = """
            public class B
            {
                /// <param name="x"></param>
                /// <param name="y"></param>
                public virtual int M(int x, string y)
                {
                    return 1;
                }
            }

            public class D : B
            {
                /// <param name="a"></param>
                /// <param name="b"></param>
                public override int M(int a, string b)
                {
                    return 1;
                }
            }

            public class D2 : D
            {
                /// <param name="y"></param>
                /// <param name="x"></param>
                public override int $$M(int y, string x)
                {
                    M(1, "Two");
                    ((D)this).M(1, "Two");
                    ((B)this).M(1, "Two");

                    M(1, x: "Two");
                    ((D)this).M(1, b: "Two");
                    ((B)this).M(1, y: "Two");

                    return 1;
                }
            }
            """;
        var permutation = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(0)
        };
        var updatedCode = """
            public class B
            {
                /// <param name="y"></param>
                /// <param name="newIntegerParameter"></param>
                /// <param name="x"></param>
                public virtual int M(string y, int newIntegerParameter, int x)
                {
                    return 1;
                }
            }

            public class D : B
            {
                /// <param name="b"></param>
                /// <param name="newIntegerParameter"></param>
                /// <param name="a"></param>
                public override int M(string b, int newIntegerParameter, int a)
                {
                    return 1;
                }
            }

            public class D2 : D
            {
                /// <param name="x"></param>
                /// <param name="newIntegerParameter"></param>
                /// <param name="y"></param>
                public override int M(string x, int newIntegerParameter, int y)
                {
                    M("Two", 12345, 1);
                    ((D)this).M("Two", 12345, 1);
                    ((B)this).M("Two", 12345, 1);

                    M(x: "Two", newIntegerParameter: 12345, y: 1);
                    ((D)this).M(b: "Two", newIntegerParameter: 12345, a: 1);
                    ((B)this).M(y: "Two", newIntegerParameter: 12345, x: 1);

                    return 1;
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/53091")]
    public async Task AddParameter_Cascade_Record()
    {
        var markup = """
            record $$BaseR(int A, int B);

            record DerivedR() : BaseR(0, 1);
            """;
        var permutation = new AddedParameterOrExistingIndex[]
        {
            new(1),
            new(new AddedParameter(null, "int", "C", CallSiteKind.Value, "3"), "int"),
            new(0)
        };
        var updatedCode = """
            record BaseR(int B, int C, int A);

            record DerivedR() : BaseR(1, 3, 0);
            """;

        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/53091")]
    public async Task AddParameter_Cascade_PrimaryConstructor()
    {
        var markup = """
            class $$BaseR(int A, int B);

            class DerivedR() : BaseR(0, 1);
            """;
        var permutation = new AddedParameterOrExistingIndex[]
        {
            new(1),
            new(new AddedParameter(null, "int", "C", CallSiteKind.Value, "3"), "int"),
            new(0)
        };
        var updatedCode = """
            class BaseR(int B, int C, int A);

            class DerivedR() : BaseR(1, 3, 0);
            """;

        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
    }
}
