// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [WorkItem(8333, "https://github.com/dotnet/roslyn/issues/8333")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInExpressionBody()
        {
            var markup = @"
class Ext
{
    void Goo(int a, int b) => [||]0;
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(1905, "https://github.com/dotnet/roslyn/issues/1905")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestAfterSemicolonForInvocationInExpressionStatement_ViaCommand()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        M1(1, 2);$$
        M2(1, 2, 3);
    }

    static void M1(int x, int y) { }

    static void M2(int x, int y, int z) { }
}";
            var expectedCode = @"
class Program
{
    static void Main(string[] args)
    {
        M1(2, 1);
        M2(1, 2, 3);
    }

    static void M1(int y, int x) { }

    static void M2(int x, int y, int z) { }
}";

            await TestChangeSignatureViaCommandAsync(
                LanguageNames.CSharp,
                markup: markup,
                updatedSignature: new[] { 1, 0 },
                expectedUpdatedInvocationDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestOnLambdaWithTwoDiscardParameters_ViaCommand()
        {
            var markup = @"
class Program
{
    static void M()
    {
        System.Func<int, string, int> f = $$(int _, string _) => 1;
    }
}";
            var expectedCode = @"
class Program
{
    static void M()
    {
        System.Func<int, string, int> f = (string _, int _) => 1;
    }
}";

            await TestChangeSignatureViaCommandAsync(
                LanguageNames.CSharp,
                markup: markup,
                updatedSignature: new[] { 1, 0 },
                expectedUpdatedInvocationDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestOnAnonymousMethodWithTwoParameters_ViaCommand()
        {
            var markup = @"
class Program
{
    static void M()
    {
        System.Func<int, string, int> f = [||]delegate(int x, string y) { return 1; };
    }
}";
            await TestMissingAsync(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestOnAnonymousMethodWithTwoDiscardParameters_ViaCommand()
        {
            var markup = @"
class Program
{
    static void M()
    {
        System.Func<int, string, int> f = [||]delegate(int _, string _) { return 1; };
    }
}";
            await TestMissingAsync(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestAfterSemicolonForInvocationInExpressionStatement_ViaCodeAction()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        M1(1, 2);[||]
        M2(1, 2, 3);
    }

    static void M1(int x, int y) { }

    static void M2(int x, int y, int z) { }
}";

            await TestMissingAsync(markup);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingWhitespace()
        {
            var markup = @"
class Ext
{
    [||]
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingTrivia()
        {
            var markup = @"
class Ext
{
    // [||]
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingTrivia2()
        {
            var markup = @"
class Ext
{
    [||]//
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingDocComment()
        {
            var markup = @"
class Ext
{
    /// [||]
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingDocComment2()
        {
            var markup = @"
class Ext
{
    [||]///
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingAttributes1()
        {
            var markup = @"
class Ext
{
    [||][X]
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingAttributes2()
        {
            var markup = @"
class Ext
{
    [[||]X]
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingAttributes3()
        {
            var markup = @"
class Ext
{
    [X][||]
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInConstraints()
        {
            var markup = @"
class Ext
{
    void Goo<T>(int a, int b) where [||]T : class
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }
    }
}
