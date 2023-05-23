// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion;
public class CompletionFeaturesTests : AbstractLanguageServerProtocolTests
{
    protected override TestComposition Composition => FeaturesLspComposition;

    private ClientCapabilities DefaultClientCapabilities { get; }
    = new LSP.ClientCapabilities
    {
        TextDocument = new LSP.TextDocumentClientCapabilities
        {
            Completion = new LSP.CompletionSetting
            {
                CompletionItem = new LSP.CompletionItemSetting
                {
                    CommitCharactersSupport = true,
                    LabelDetailsSupport = true,
                    ResolveSupport = new LSP.ResolveSupportSetting
                    {
                        Properties = new string[] { "documentation", "additionalTextEdits", "command", "labelDetail" }
                    }
                },

                CompletionListSetting = new LSP.CompletionListSetting
                {
                    ItemDefaults = new string[] { CompletionCapabilityHelper.EditRangePropertyName, CompletionCapabilityHelper.DataPropertyName, CompletionCapabilityHelper.CommitCharactersPropertyName }
                },
            },
        }
    };

    public CompletionFeaturesTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1801810")]
    public async Task TestDoesNotThrowInComplexEditWhenDisplayTextShorterThanDefaultSpanAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"
using System;
using System.Text;

public class A
{
    public int M()
    {
        return{|caret:|}
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, DefaultClientCapabilities);
        var caret = testLspServer.GetLocations("caret").Single();
        var completionParams = new LSP.CompletionParams()
        {
            TextDocument = CreateTextDocumentIdentifier(caret.Uri),
            Position = caret.Range.Start,
            Context = new LSP.CompletionContext()
            {
                TriggerKind = LSP.CompletionTriggerKind.Invoked,
            }
        };

        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

        var results = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None);
        AssertEx.NotNull(results);
        Assert.NotEmpty(results.Items);
    }

    [Theory, CombinatorialData]
    public async Task TestImportCompletion(bool mutatingLspWorkspace)
    {
        var markup =
@"
class A
{
    void M()
    {
        Task{|caret:|}
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, DefaultClientCapabilities);
        var completionParams = CreateCompletionParams(
            testLspServer.GetLocations("caret").Single(),
            invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
            triggerCharacter: "\0",
            triggerKind: LSP.CompletionTriggerKind.Invoked);

        // Make sure the unimported types option is on by default.
        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, true);
        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, true);

        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

        var completionResult = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None).ConfigureAwait(false);
        Assert.NotNull(completionResult.ItemDefaults.EditRange);
        Assert.NotNull(completionResult.ItemDefaults.Data);
        Assert.NotNull(completionResult.ItemDefaults.CommitCharacters);

        var actualItem = completionResult.Items.First(i => i.Label == "Task");
        Assert.Equal("System.Threading.Tasks", actualItem.LabelDetails.Description);
        Assert.Equal("~Task  System.Threading.Tasks", actualItem.SortText);
        Assert.Equal(CompletionItemKind.Class, actualItem.Kind);
        Assert.Null(actualItem.LabelDetails.Detail);
        Assert.Null(actualItem.FilterText);
        Assert.Null(actualItem.TextEdit);
        Assert.Null(actualItem.TextEditText);
        Assert.Null(actualItem.AdditionalTextEdits);
        Assert.Null(actualItem.Command);
        Assert.Null(actualItem.CommitCharacters);
        Assert.Null(actualItem.Data);
        Assert.Null(actualItem.Detail);
        Assert.Null(actualItem.Documentation);

        actualItem.Data = completionResult.ItemDefaults.Data;

        var resolvedItem = await testLspServer.ExecuteRequestAsync<LSP.CompletionItem, LSP.CompletionItem>(LSP.Methods.TextDocumentCompletionResolveName, actualItem, CancellationToken.None).ConfigureAwait(false);
        Assert.Equal("System.Threading.Tasks", resolvedItem.LabelDetails.Description);
        Assert.Equal("~Task  System.Threading.Tasks", resolvedItem.SortText);
        Assert.Equal(CompletionItemKind.Class, resolvedItem.Kind);

        var expectedAdditionalEdit = new TextEdit() { NewText = "using System.Threading.Tasks;\r\n\r\n", Range = new() { Start = new(1, 0), End = new(1, 0) } };
        AssertJsonEquals(new[] { expectedAdditionalEdit }, resolvedItem.AdditionalTextEdits);

        Assert.Null(resolvedItem.LabelDetails.Detail);
        Assert.Null(resolvedItem.FilterText);
        Assert.Null(resolvedItem.TextEdit);
        Assert.Null(resolvedItem.TextEditText);
        Assert.Null(resolvedItem.Command);
        Assert.Null(resolvedItem.Detail);

        var expectedDocumentation = new MarkupContent()
        {
            Kind = LSP.MarkupKind.PlainText,
            Value = "(awaitable) class System.Threading.Tasks.Task"
        };
        AssertJsonEquals(resolvedItem.Documentation, expectedDocumentation);
    }

    [Theory, CombinatorialData]
    public async Task TestResolveComplexEdit(bool mutatingLspWorkspace)
    {
        var markup =
@"
/// <summ{|caret:|}
class A { }";

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, DefaultClientCapabilities);
        var completionParams = CreateCompletionParams(
            testLspServer.GetLocations("caret").Single(),
            invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
            triggerCharacter: "\0",
            triggerKind: LSP.CompletionTriggerKind.Invoked);

        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

        var completionResult = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None).ConfigureAwait(false);
        Assert.NotNull(completionResult.ItemDefaults.EditRange);
        Assert.NotNull(completionResult.ItemDefaults.Data);
        Assert.NotNull(completionResult.ItemDefaults.CommitCharacters);

        var actualItem = completionResult.Items.First(i => i.Label == "summary");
        Assert.Equal(CompletionItemKind.Keyword, actualItem.Kind);
        Assert.Equal("summ", actualItem.TextEditText);
        Assert.Null(actualItem.LabelDetails);
        Assert.Null(actualItem.SortText);
        Assert.Null(actualItem.FilterText);
        Assert.Null(actualItem.TextEdit);
        Assert.Null(actualItem.AdditionalTextEdits);
        Assert.Null(actualItem.Command);
        Assert.Null(actualItem.CommitCharacters);
        Assert.Null(actualItem.Data);
        Assert.Null(actualItem.Detail);
        Assert.Null(actualItem.Documentation);

        actualItem.Data = completionResult.ItemDefaults.Data;

        var resolvedItem = await testLspServer.ExecuteRequestAsync<LSP.CompletionItem, LSP.CompletionItem>(LSP.Methods.TextDocumentCompletionResolveName, actualItem, CancellationToken.None).ConfigureAwait(false);
        var expectedEdit = new TextEdit { Range = new LSP.Range { Start = new(1, 5), End = new(1, 9) }, NewText = "summary" };

        Assert.Equal(DefaultLspCompletionResultCreationService.CompleteComplexEditCommand, resolvedItem.Command.CommandIdentifier);
        Assert.Equal(nameof(DefaultLspCompletionResultCreationService.CompleteComplexEditCommand), resolvedItem.Command.Title);
        Assert.Equal(completionParams.TextDocument.Uri, new System.Uri((string)resolvedItem.Command.Arguments[0]));
        AssertJsonEquals(expectedEdit, resolvedItem.Command.Arguments[1]);
        Assert.Equal(false, resolvedItem.Command.Arguments[2]);
        Assert.Equal((long)14, resolvedItem.Command.Arguments[3]);
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1755955")]
    public async Task TestSoftSelectionWhenFilterTextIsEmptyAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"
using System;
using System.Text;

public class A
{
    public void M(string someText)
    {
        var x = new StringBuilder();
        x.Append({|caret:|}
    }
}";

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, DefaultClientCapabilities);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        await testLspServer.OpenDocumentAsync(caretLocation.Uri);

        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, true);

        var completionParams = CreateCompletionParams(
            caretLocation,
            invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
            triggerCharacter: "(",
            triggerKind: LSP.CompletionTriggerKind.Invoked);

        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
        var results = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None).ConfigureAwait(false);

        Assert.True(results.IsIncomplete);
        var someTextItem = results.Items.First(item => item.Label == "someText");

        if (results.ItemDefaults.CommitCharacters == null)
        {
            Assert.True(!someTextItem.Preselect && someTextItem.CommitCharacters != null && someTextItem.CommitCharacters.Length == 0);
        }
        else
        {
            Assert.True(results.ItemDefaults.CommitCharacters.Length == 0);
            Assert.True(!someTextItem.Preselect && someTextItem.CommitCharacters == null);
        }

        await testLspServer.InsertTextAsync(caretLocation.Uri, (caretLocation.Range.End.Line, caretLocation.Range.End.Character, "s"));

        completionParams = CreateCompletionParams(
            GetLocationPlusOne(caretLocation),
            invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
            triggerCharacter: "s",
            triggerKind: LSP.CompletionTriggerKind.TriggerForIncompleteCompletions);

        results = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None).ConfigureAwait(false);
        someTextItem = results.Items.First(item => item.Label == "someText");

        if (results.ItemDefaults.CommitCharacters == null)
        {
            Assert.False(someTextItem.Preselect);
            Assert.NotEmpty(someTextItem.CommitCharacters);
        }
        else
        {
            Assert.NotEmpty(results.ItemDefaults.CommitCharacters);
            Assert.False(someTextItem.Preselect);
            Assert.Null(someTextItem.CommitCharacters);
        }
    }
}
