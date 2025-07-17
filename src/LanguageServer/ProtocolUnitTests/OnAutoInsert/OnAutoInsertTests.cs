﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.OnAutoInsert;

[Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
public sealed class OnAutoInsertTests : AbstractLanguageServerProtocolTests
{
    public OnAutoInsertTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public Task OnAutoInsert_CommentCharacter(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("/", """
            class A
            {
                ///{|type:|}
                void M()
                {
                }
            }
            """, """
            class A
            {
                /// <summary>
            /// $0
            /// </summary>
                void M()
                {
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_CommentCharacter_WithComment(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("/", """
            class A
            {
                ///{|type:|} This is an existing comment
                void M()
                {
                }
            }
            """, """
            class A
            {
                /// <summary>
            /// $0This is an existing comment
            /// </summary>
                void M()
                {
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_CommentCharacter_WithComment_NoSpace(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("/", """
            class A
            {
                ///{|type:|}This is an existing comment
                void M()
                {
                }
            }
            """, """
            class A
            {
                /// <summary>
            /// $0This is an existing comment
            /// </summary>
                void M()
                {
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_CommentCharacter_VB(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("'", """
            Class A
                '''{|type:|}
                Sub M()
                End Sub
            End Class
            """, """
            Class A
                ''' <summary>
            ''' $0
            ''' </summary>
                Sub M()
                End Sub
            End Class
            """, mutatingLspWorkspace, languageName: LanguageNames.VisualBasic);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_ParametersAndReturns(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("/", """
            class A
            {
                ///{|type:|}
                string M(int foo, bool bar)
                {
                }
            }
            """, """
            class A
            {
                /// <summary>
            /// $0
            /// </summary>
            /// <param name="foo"></param>
            /// <param name="bar"></param>
            /// <returns></returns>
                string M(int foo, bool bar)
                {
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_CommentCharacterInsideMethod_Ignored(bool mutatingLspWorkspace)
        => VerifyNoResult("/", """
            class A
            {
                void M()
                {
                    ///{|type:|}
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_VisualBasicCommentCharacter_Ignored(bool mutatingLspWorkspace)
        => VerifyNoResult("'", """
            class A
            {
                '''{|type:|}
                void M()
                {
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_EnterKey(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("\n", """
            class A
            {
                /// <summary>
                /// Foo
                /// </summary>
            {|type:|}
                void M()
                {
                }
            }
            """, """
            class A
            {
                /// <summary>
                /// Foo
                /// </summary>
                /// $0
                void M()
                {
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_EnterKey2(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("\n", """
            class A
            {
                /// <summary>
                /// Foo
            {|type:|}
                /// </summary>
                void M()
                {
                }
            }
            """, """
            class A
            {
                /// <summary>
                /// Foo
                /// $0
                /// </summary>
                void M()
                {
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_EnterKey3(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("\n", """
            class A
            {
                ///
            {|type:|}
                string M(int foo, bool bar)
                {
                }
            }
            """, """
            class A
            {
                /// <summary>
                /// $0
                /// </summary>
                /// <param name="foo"></param>
                /// <param name="bar"></param>
                /// <returns></returns>
                string M(int foo, bool bar)
                {
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_BraceFormatting(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("\n", """
            class A
            {
                void M() {{|type:|}
                }
            }
            """, """
            class A
            {
                void M()
                {
                    $0
                }
            }
            """, mutatingLspWorkspace, serverKind: WellKnownLspServerKinds.RazorLspServer);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_BraceFormattingWithTabs(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("\n", """
            class A
            {
                void M() {{|type:|}
                }
            }
            """, """
            class A
            {
                void M()
            	{
            		$0
            	}
            }
            """, mutatingLspWorkspace, insertSpaces: false, tabSize: 4, serverKind: WellKnownLspServerKinds.RazorLspServer);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_BraceFormattingInsideMethod(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("\n", """
            class A
            {
                void M()
                {
                    if (true) {{|type:|}
                    }
                }
            }
            """, """
            class A
            {
                void M()
                {
                    if (true)
                    {
                        $0
                    }
                }
            }
            """, mutatingLspWorkspace, serverKind: WellKnownLspServerKinds.RazorLspServer);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_BraceFormattingNoResultInInterpolation(bool mutatingLspWorkspace)
        => VerifyNoResult("\n", """
            class A
            {
                void M()
                {
                    var s = $"Hello {{|type:|}
                    }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1260219")]
    public Task OnAutoInsert_BraceFormattingDoesNotInsertExtraEmptyLines(bool mutatingLspWorkspace)
        => VerifyNoResult("\n", """
            class A
            {
                void M()
                {

                    {|type:|}
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1260219")]
    public Task OnAutoInsert_BraceFormattingDoesNotMoveCaretOnEnterInsideBraces(bool mutatingLspWorkspace)
        => VerifyNoResult("\n", """
            class A
            {
                void M()
                {{|type:|}


                }
            }
            """, mutatingLspWorkspace);

    private async Task VerifyMarkupAndExpected(
        string characterTyped,
        string markup,
        string expected,
        bool mutatingLspWorkspace,
        bool insertSpaces = true,
        int tabSize = 4,
        string languageName = LanguageNames.CSharp,
        WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.AlwaysActiveVSLspServer)
    {
        Task<TestLspServer> testLspServerTask;
        if (languageName == LanguageNames.CSharp)
        {
            testLspServerTask = CreateTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions { ClientCapabilities = CapabilitiesWithVSExtensions, ServerKind = serverKind });
        }
        else if (languageName == LanguageNames.VisualBasic)
        {
            testLspServerTask = CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions { ClientCapabilities = CapabilitiesWithVSExtensions, ServerKind = serverKind });
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(languageName);
        }

        await using var testLspServer = await testLspServerTask;
        var locationTyped = testLspServer.GetLocations("type").Single();

        var document = await testLspServer.GetDocumentAsync(locationTyped.DocumentUri);
        var documentText = await document.GetTextAsync();

        var result = await RunOnAutoInsertAsync(testLspServer, characterTyped, locationTyped, insertSpaces, tabSize);

        AssertEx.NotNull(result);
        Assert.Equal(InsertTextFormat.Snippet, result.TextEditFormat);
        var actualText = ApplyTextEdits([result.TextEdit], documentText);
        Assert.Equal(expected, actualText);
    }

    private async Task VerifyNoResult(string characterTyped, string markup, bool mutatingLspWorkspace, bool insertSpaces = true, int tabSize = 4)
    {
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var locationTyped = testLspServer.GetLocations("type").Single();
        var documentText = await (await testLspServer.GetDocumentAsync(locationTyped.DocumentUri)).GetTextAsync();

        var result = await RunOnAutoInsertAsync(testLspServer, characterTyped, locationTyped, insertSpaces, tabSize);

        Assert.Null(result);
    }

    private static async Task<LSP.VSInternalDocumentOnAutoInsertResponseItem?> RunOnAutoInsertAsync(
        TestLspServer testLspServer,
        string characterTyped,
        LSP.Location locationTyped,
        bool insertSpaces,
        int tabSize)
    {
        return await testLspServer.ExecuteRequestAsync<LSP.VSInternalDocumentOnAutoInsertParams, LSP.VSInternalDocumentOnAutoInsertResponseItem?>(VSInternalMethods.OnAutoInsertName,
            CreateDocumentOnAutoInsertParams(characterTyped, locationTyped, insertSpaces, tabSize), CancellationToken.None);
    }

    private static LSP.VSInternalDocumentOnAutoInsertParams CreateDocumentOnAutoInsertParams(
        string characterTyped,
        LSP.Location locationTyped,
        bool insertSpaces,
        int tabSize)
        => new LSP.VSInternalDocumentOnAutoInsertParams
        {
            Position = locationTyped.Range.Start,
            Character = characterTyped,
            TextDocument = CreateTextDocumentIdentifier(locationTyped.DocumentUri),
            Options = new LSP.FormattingOptions
            {
                InsertSpaces = insertSpaces,
                TabSize = tabSize
            }
        };
}
