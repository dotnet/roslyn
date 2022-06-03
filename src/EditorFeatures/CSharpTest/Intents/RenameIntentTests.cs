// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Intents;

[UseExportProvider]
public class RenameIntentTests : IntentTestsBase
{
    [Fact]
    public async Task TestRenameIntentAsync()
    {
        var initialText =
@"class C
{
    void M()
    {
        var thing = 1;
        {|typed:something|}.ToString();
    }
}";
        var expectedText =
@"class C
{
    void M()
    {
        var something = 1;
        something.ToString();
    }
}";

        await VerifyExpectedRenameAsync(initialText, expectedText, "thing", "something").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestRenameIntentAsync_Insert()
    {
        var initialText =
@"class C
{
    void M()
    {
        var thing = 1;
        {|typed:some|}thing.ToString();
    }
}";
        var expectedText =
@"class C
{
    void M()
    {
        var something = 1;
        something.ToString();
    }
}";

        await VerifyExpectedRenameAsync(initialText, expectedText, string.Empty, "something").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestRenameIntentAsync_Delete()
    {
        var initialText =
@"class C
{
    void M()
    {
        var something = 1;
        {|typed:|}thing.ToString();
    }
}";
        var expectedText =
@"class C
{
    void M()
    {
        var thing = 1;
        thing.ToString();
    }
}";

        await VerifyExpectedRenameAsync(initialText, expectedText, "some", "thing").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestRenameIntentAsync_MultipleFiles()
    {
        var initialText =
@"namespace M
{
    public class C
    {
        public static string {|typed:BetterString|} = string.Empty;

        void M()
        {
            var m = SomeString;
        }
    }
}";
        var additionalDocuments = new string[]
        {
@"namespace M
{
    public class D
    {
        void M()
        {
            var m = C.SomeString;
        }
    }
}"
        };

        var expectedTexts = new string[]
        {
@"namespace M
{
    public class C
    {
        public static string BetterString = string.Empty;

        void M()
        {
            var m = BetterString;
        }
    }
}",
@"namespace M
{
    public class D
    {
        void M()
        {
            var m = C.BetterString;
        }
    }
}"
        };

        await VerifyExpectedRenameAsync(initialText, additionalDocuments, expectedTexts, "SomeString", "BetterString").ConfigureAwait(false);
    }

    private static Task VerifyExpectedRenameAsync(string initialText, string expectedText, string priorText, string newName)
    {
        return VerifyExpectedTextAsync(WellKnownIntents.Rename, initialText, expectedText, intentData: $"{{ \"newName\": \"{newName}\" }}", priorText: priorText);
    }

    private static Task VerifyExpectedRenameAsync(string initialText, string[] additionalText, string[] expectedTexts, string priorText, string newName)
    {
        return VerifyExpectedTextAsync(WellKnownIntents.Rename, initialText, additionalText, expectedTexts, intentData: $"{{ \"newName\": \"{newName}\" }}", priorText: priorText);
    }
}
