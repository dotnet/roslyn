// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Intents;

[UseExportProvider]
public class RenameIntentTests : IntentTestsBase
{
    [Fact]
    public async Task TestRenameIntentAsync()
    {
        var initialText =
            """
            class C
            {
                void M()
                {
                    var thing = 1;
                    {|priorSelection:thing|}.ToString();
                }
            }
            """;
        var currentText =
            """
            class C
            {
                void M()
                {
                    var thing = 1;
                    something.ToString();
                }
            }
            """;
        var expectedText =
            """
            class C
            {
                void M()
                {
                    var something = 1;
                    something.ToString();
                }
            }
            """;

        await VerifyExpectedRenameAsync(initialText, currentText, expectedText, "something").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestRenameIntentAsync_Insert()
    {
        var initialText =
            """
            class C
            {
                void M()
                {
                    var thing = 1;
                    {|priorSelection:|}thing.ToString();
                }
            }
            """;
        var currentText =
            """
            class C
            {
                void M()
                {
                    var thing = 1;
                    something.ToString();
                }
            }
            """;
        var expectedText =
            """
            class C
            {
                void M()
                {
                    var something = 1;
                    something.ToString();
                }
            }
            """;

        await VerifyExpectedRenameAsync(initialText, currentText, expectedText, "something").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestRenameIntentAsync_Delete()
    {
        var initialText =
            """
            class C
            {
                void M()
                {
                    var something = 1;
                    {|priorSelection:some|}thing.ToString();
                }
            }
            """;
        var currentText =
            """
            class C
            {
                void M()
                {
                    var something = 1;
                    thing.ToString();
                }
            }
            """;
        var expectedText =
            """
            class C
            {
                void M()
                {
                    var thing = 1;
                    thing.ToString();
                }
            }
            """;

        await VerifyExpectedRenameAsync(initialText, currentText, expectedText, "thing").ConfigureAwait(false);
    }

    [Fact]
    public async Task TestRenameIntentAsync_MultipleFiles()
    {
        var initialText =
            """
            namespace M
            {
                public class C
                {
                    public static string {|priorSelection:SomeString|} = string.Empty;

                    void M()
                    {
                        var m = SomeString;
                    }
                }
            }
            """;
        var currentText =
            """
            namespace M
            {
                public class C
                {
                    public static string BetterString = string.Empty;

                    void M()
                    {
                        var m = SomeString;
                    }
                }
            }
            """;
        var additionalDocuments = new string[]
        {
            """
            namespace M
            {
                public class D
                {
                    void M()
                    {
                        var m = C.SomeString;
                    }
                }
            }
            """
        };

        var expectedTexts = new string[]
        {
            """
            namespace M
            {
                public class C
                {
                    public static string BetterString = string.Empty;

                    void M()
                    {
                        var m = BetterString;
                    }
                }
            }
            """,
            """
            namespace M
            {
                public class D
                {
                    void M()
                    {
                        var m = C.BetterString;
                    }
                }
            }
            """
        };

        await VerifyExpectedRenameAsync(initialText, currentText, additionalDocuments, expectedTexts, "BetterString").ConfigureAwait(false);
    }

    private static Task VerifyExpectedRenameAsync(string initialText, string currentText, string expectedText, string newName)
    {
        return VerifyExpectedTextAsync(WellKnownIntents.Rename, initialText, currentText, expectedText, intentData: $"{{ \"newName\": \"{newName}\" }}");
    }

    private static Task VerifyExpectedRenameAsync(string initialText, string currentText, string[] additionalText, string[] expectedTexts, string newName)
    {
        return VerifyExpectedTextAsync(WellKnownIntents.Rename, initialText, currentText, additionalText, expectedTexts, intentData: $"{{ \"newName\": \"{newName}\" }}");
    }
}
