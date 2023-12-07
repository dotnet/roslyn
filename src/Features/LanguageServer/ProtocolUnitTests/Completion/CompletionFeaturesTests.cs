// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

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
    [WorkItem("https://github.com/dotnet/roslyn/issues/68791")]
    public async Task TestImportCompletionForType(bool mutatingLspWorkspace, bool isInUsingStatement)
    {
        var markup = isInUsingStatement
            ? @"global using static Task{|caret:|}"
            : @"
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

        TextEdit expectedAdditionalEdit = isInUsingStatement
            ? new() { NewText = "System.Threading.Tasks.Task", Range = new() { Start = new(0, 20), End = new(0, 24) } }
            : new() { NewText = "using System.Threading.Tasks;\r\n\r\n", Range = new() { Start = new(1, 0), End = new(1, 0) } };

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
    [WorkItem("https://github.com/dotnet/roslyn/issues/69576")]
    public async Task TestImportCompletionForExtensionMethod(bool mutatingLspWorkspace)
    {
        var markup =
@"
namespace NS2
{
    public static class ExtensionClass
    {
        public static bool ExtensionMethod(this object o) => true;
    }
}

namespace NS1
{
    class Program
    {
        void M(object o)
        {
            o.{|caret:|}
        }
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, DefaultClientCapabilities);
        var completionParams = CreateCompletionParams(
            testLspServer.GetLocations("caret").Single(),
            invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
            triggerCharacter: "\0",
            triggerKind: LSP.CompletionTriggerKind.Invoked);

        // Make sure the import completion option is on.
        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, true);
        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, true);

        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

        var completionResult = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None).ConfigureAwait(false);
        Assert.NotNull(completionResult.ItemDefaults.EditRange);
        Assert.NotNull(completionResult.ItemDefaults.Data);
        Assert.NotNull(completionResult.ItemDefaults.CommitCharacters);

        var actualItem = completionResult.Items.First(i => i.Label == "ExtensionMethod");
        Assert.Equal("NS2", actualItem.LabelDetails.Description);
        Assert.Equal("~ExtensionMethod NS2", actualItem.SortText);
        Assert.Equal(CompletionItemKind.Method, actualItem.Kind);
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
        Assert.Equal("NS2", resolvedItem.LabelDetails.Description);
        Assert.Equal("~ExtensionMethod NS2", resolvedItem.SortText);
        Assert.Equal(CompletionItemKind.Method, resolvedItem.Kind);

        var expectedAdditionalEdit = new TextEdit() { NewText = "using NS2;\r\n\r\n", Range = new() { Start = new(1, 0), End = new(1, 0) } };
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
            Value = "(extension) bool object.ExtensionMethod()"
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
        Assert.Equal(completionParams.TextDocument.Uri, ProtocolConversions.CreateAbsoluteUri((string)resolvedItem.Command.Arguments[0]));
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

    [Theory, CombinatorialData]
    public async Task TestPromotingDefaultCommitCharactersAsync(bool mutatingLspWorkspace, bool hasDefaultCommitCharCapability)
    {
        var markup =
@"using System;
class A
{
    void M()
    {
        item{|caret:|}
    }
}";
        var clientCapability = DefaultClientCapabilities;
        if (!hasDefaultCommitCharCapability)
        {
            clientCapability.TextDocument.Completion.CompletionListSetting.ItemDefaults
                = new string[] { CompletionCapabilityHelper.EditRangePropertyName, CompletionCapabilityHelper.DataPropertyName };
        }

        await using var testLspServer = await CreateTestLspServerAsync(new[] { markup }, LanguageNames.CSharp, mutatingLspWorkspace,
            new InitializationOptions { ClientCapabilities = clientCapability, CallInitialized = true },
            extraExportedTypes: new[] { typeof(CSharpLspMockCompletionService.Factory) }.ToList());

        var mockService = testLspServer.TestWorkspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService<CompletionService>() as CSharpLspMockCompletionService;
        mockService.NonDefaultRule = CompletionItemRules.Default.WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ' ', '('));

        // return 10 items, all use default commit characters
        mockService.ItemCounts = (10, 0);

        var completionParams = CreateCompletionParams(
            testLspServer.GetLocations("caret").Single(),
            invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
            triggerCharacter: "\0",
            triggerKind: LSP.CompletionTriggerKind.Invoked);

        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

        var results = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None);

        Assert.NotNull(results);
        Assert.NotEmpty(results.Items);

        var defaultCharArray = CompletionRules.Default.DefaultCommitCharacters.Select(c => c.ToString()).ToArray();

        if (hasDefaultCommitCharCapability)
        {
            // if default commit char on list is supported, then it should be set to default array
            // and all item commit chars should be null
            Assert.NotNull(results.ItemDefaults.CommitCharacters);
            AssertEx.SetEqual(defaultCharArray, results.ItemDefaults.CommitCharacters);

            Assert.All(results.Items, (item) => Assert.Null(item.CommitCharacters));
        }
        else
        {
            // otherwise, the list default should be null, and all item commit chars should be set to null too
            // so client will use the default array we returned as part of server capability.
            Assert.Null(results.ItemDefaults.CommitCharacters);
            Assert.All(results.Items, (item) => Assert.Null(item.CommitCharacters));
        }
    }

    [Theory, CombinatorialData]
    public async Task TestUsingServerDefaultCommitCharacters(bool mutatingLspWorkspace, bool shouldPromoteDefaultCommitCharsToList)
    {
        var markup = "Item{|caret:|}";
        await using var testLspServer = await CreateTestLspServerAsync(new[] { markup }, LanguageNames.CSharp, mutatingLspWorkspace,
            new InitializationOptions { ClientCapabilities = DefaultClientCapabilities, CallInitialized = true },
            extraExportedTypes: new[] { typeof(CSharpLspMockCompletionService.Factory) }.ToList());

        var mockService = testLspServer.TestWorkspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService<CompletionService>() as CSharpLspMockCompletionService;
        mockService.NonDefaultRule = CompletionItemRules.Default.WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ' ', '('));
        mockService.ItemCounts = shouldPromoteDefaultCommitCharsToList ? (20, 10) : (10, 20);

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
        Assert.NotNull(results.ItemDefaults.CommitCharacters);

        var defaultCharArray = CompletionRules.Default.DefaultCommitCharacters.Select(c => c.ToString()).ToArray();
        var nonDefaultCharArray = AbstractLspCompletionResultCreationService.CreateCommitCharacterArrayFromRules(mockService.NonDefaultRule);

        if (shouldPromoteDefaultCommitCharsToList)
        {
            AssertEx.SetEqual(defaultCharArray, results.ItemDefaults.CommitCharacters);
            foreach (var item in results.Items)
            {
                if (item.Label.StartsWith("ItemWithDefaultChar"))
                {
                    Assert.Null(item.CommitCharacters);
                }
                else if (item.Label.StartsWith("ItemWithNonDefaultChar"))
                {
                    Assert.NotNull(item.CommitCharacters);
                    AssertEx.SetEqual(nonDefaultCharArray, item.CommitCharacters);
                }
            }
        }
        else
        {
            AssertEx.SetEqual(nonDefaultCharArray, results.ItemDefaults.CommitCharacters);
            foreach (var item in results.Items)
            {
                if (item.Label.StartsWith("ItemWithDefaultChar"))
                {
                    Assert.NotNull(item.CommitCharacters);
                    AssertEx.SetEqual(defaultCharArray, item.CommitCharacters);
                }
                else if (item.Label.StartsWith("ItemWithNonDefaultChar"))
                {
                    Assert.Null(item.CommitCharacters);
                }
            }
        }
    }

    private sealed class CSharpLspMockCompletionService : CompletionService
    {
        private CSharpLspMockCompletionService(SolutionServices services, IAsynchronousOperationListenerProvider listenerProvider) : base(services, listenerProvider)
        {
        }

        public override string Language => LanguageNames.CSharp;

        internal override CompletionRules GetRules(CodeAnalysis.Completion.CompletionOptions options)
            => CompletionRules.Default;

        internal override bool ShouldTriggerCompletion(
            Project project, LanguageServices languageServices, SourceText text, int caretPosition, CompletionTrigger trigger,
            CodeAnalysis.Completion.CompletionOptions options, OptionSet passThroughOptions, ImmutableHashSet<string> roles = null)
        {
            return true;
        }

        public CompletionItemRules NonDefaultRule { get; set; } = CompletionItemRules.Default;

        public (int defaultItemCount, int nonDefaultItemCount) ItemCounts { get; set; }

        internal override async Task<CodeAnalysis.Completion.CompletionList> GetCompletionsAsync(
            Document document, int caretPosition, CodeAnalysis.Completion.CompletionOptions options, OptionSet passThroughOptions,
            CompletionTrigger trigger = default, ImmutableHashSet<string> roles = null, CancellationToken cancellationToken = default)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var defaultItemSpan = GetDefaultCompletionListSpan(text, caretPosition);

            var builder = ImmutableArray.CreateBuilder<CodeAnalysis.Completion.CompletionItem>();

            for (var i = 0; i < ItemCounts.defaultItemCount; ++i)
                builder.Add(CodeAnalysis.Completion.CompletionItem.Create($"ItemWithDefaultChar{i}", rules: CompletionItemRules.Default));

            for (var i = 0; i < ItemCounts.nonDefaultItemCount; ++i)
                builder.Add(CodeAnalysis.Completion.CompletionItem.Create($"ItemNonDefaultChar{i}", rules: NonDefaultRule));

            return CodeAnalysis.Completion.CompletionList.Create(defaultItemSpan, builder.ToImmutable());
        }

        [ExportLanguageServiceFactory(typeof(CompletionService), LanguageNames.CSharp, ServiceLayer.Test), Shared]
        internal sealed class Factory : ILanguageServiceFactory
        {
            private readonly IAsynchronousOperationListenerProvider _listenerProvider;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory(IAsynchronousOperationListenerProvider listenerProvider)
            {
                _listenerProvider = listenerProvider;
            }

            public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            {
                return new CSharpLspMockCompletionService(languageServices.LanguageServices.SolutionServices, _listenerProvider);
            }
        }
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/5777")]
    public async Task EditRangeShouldEndAtCursorPosition(bool mutatingLspWorkspace)
    {
        var markup =
@"public class C1 {}

pub{|caret:|}class";
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
        Assert.Equal(new() { Start = new(2, 0), End = caret.Range.Start }, results.ItemDefaults.EditRange.Value.First);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/5916")]
    public async Task TestResolveImportCompletionWithIdenticalLabel(bool mutatingLspWorkspace)
    {
        var markup =
@"
namespace Namespace1
{
    class MyClass {}
}
namespace Namespace2
{
    class MyClass {}
}
namespace Program
{
    class A
    {
        void M()
        {
            MyClass{|caret:|}
        }
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

        var myClassItems = completionResult.Items.Where(i => i.Label == "MyClass").ToImmutableArray();
        var itemFromNS1 = myClassItems.Single(i => i.LabelDetails?.Description == "Namespace1");
        var itemFromNS2 = myClassItems.Single(i => i.LabelDetails?.Description == "Namespace2");

        itemFromNS1.Data = completionResult.ItemDefaults.Data;
        itemFromNS2.Data = completionResult.ItemDefaults.Data;

        // Remove the label details as this is the behavior of the VSCode client when resolving completion items.
        itemFromNS1.LabelDetails = null;
        itemFromNS2.LabelDetails = null;

        var resolvedItem1 = await testLspServer.ExecuteRequestAsync<LSP.CompletionItem, LSP.CompletionItem>(LSP.Methods.TextDocumentCompletionResolveName, itemFromNS1, CancellationToken.None).ConfigureAwait(false);
        Assert.Equal("Namespace1", resolvedItem1.LabelDetails.Description);
        Assert.Equal("~MyClass Namespace1", resolvedItem1.SortText);
        Assert.Equal(CompletionItemKind.Class, resolvedItem1.Kind);

        var expectedAdditionalEdit1 = new TextEdit() { NewText = "using Namespace1;\r\n\r\n", Range = new() { Start = new(1, 0), End = new(1, 0) } };
        AssertJsonEquals(new[] { expectedAdditionalEdit1 }, resolvedItem1.AdditionalTextEdits);

        var resolvedItem2 = await testLspServer.ExecuteRequestAsync<LSP.CompletionItem, LSP.CompletionItem>(LSP.Methods.TextDocumentCompletionResolveName, itemFromNS2, CancellationToken.None).ConfigureAwait(false);
        Assert.Equal("Namespace2", resolvedItem2.LabelDetails.Description);
        Assert.Equal("~MyClass Namespace2", resolvedItem2.SortText);
        Assert.Equal(CompletionItemKind.Class, resolvedItem2.Kind);

        var expectedAdditionalEdit2 = new TextEdit() { NewText = "using Namespace2;\r\n\r\n", Range = new() { Start = new(1, 0), End = new(1, 0) } };
        AssertJsonEquals(new[] { expectedAdditionalEdit2 }, resolvedItem2.AdditionalTextEdits);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/5732")]
    public async Task TestEmptyCommitCharsInSuggestionMode(bool mutatingLspWorkspace)
    {
        var markup =
@"
using System.Collections.Generic;
using System.Linq;
public class C
{
    public Foo(List<int> myList)
    {
        var foo = myList.Where(i{|caret:|})
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
        Assert.Empty(results.ItemDefaults.CommitCharacters);
        Assert.True(results.Items.All(item => item.CommitCharacters is null));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/5988")]
    public async Task TestSoftSelectionWhenFilterTextIsEmptyForPreselectItemAsync(bool mutatingLspWorkspace)
    {
        var markup = "{|caret:|}";
        await using var testLspServer = await CreateTestLspServerAsync(new[] { markup }, LanguageNames.CSharp, mutatingLspWorkspace,
            new InitializationOptions { ClientCapabilities = DefaultClientCapabilities, CallInitialized = true },
            extraExportedTypes: new[] { typeof(CSharpLspMockCompletionService.Factory) }.ToList());

        var mockService = testLspServer.TestWorkspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService<CompletionService>() as CSharpLspMockCompletionService;
        mockService.NonDefaultRule = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect);
        mockService.ItemCounts = (10, 10);

        var caret = testLspServer.GetLocations("caret").Single();
        await testLspServer.OpenDocumentAsync(caret.Uri);

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
        Assert.True(results.IsIncomplete);
        AssertEx.Empty(results.ItemDefaults.CommitCharacters);

        foreach (var item in results.Items)
            Assert.Null(item.CommitCharacters);

        await testLspServer.InsertTextAsync(caret.Uri, (caret.Range.End.Line, caret.Range.End.Character, "i"));

        completionParams = CreateCompletionParams(
            GetLocationPlusOne(caret),
            invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
            triggerCharacter: "i",
            triggerKind: LSP.CompletionTriggerKind.TriggerForIncompleteCompletions);

        results = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None).ConfigureAwait(false);
        Assert.False(results.IsIncomplete);
        var defaultCharArray = CompletionRules.Default.DefaultCommitCharacters.Select(c => c.ToString()).ToArray();
        AssertEx.SetEqual(defaultCharArray, results.ItemDefaults.CommitCharacters);

        foreach (var item in results.Items)
            Assert.Null(item.CommitCharacters);
    }

    private sealed class CSharpLspThrowExceptionOnChangeCompletionService : CompletionService
    {
        private CSharpLspThrowExceptionOnChangeCompletionService(SolutionServices services, IAsynchronousOperationListenerProvider listenerProvider) : base(services, listenerProvider)
        {
        }

        public override string Language => LanguageNames.CSharp;

        internal override CompletionRules GetRules(CodeAnalysis.Completion.CompletionOptions options)
            => CompletionRules.Default;

        internal override bool ShouldTriggerCompletion(
            Project project, LanguageServices languageServices, SourceText text, int caretPosition, CompletionTrigger trigger,
            CodeAnalysis.Completion.CompletionOptions options, OptionSet passThroughOptions, ImmutableHashSet<string> roles = null)
        {
            return true;
        }

        public ImmutableArray<CodeAnalysis.Completion.CompletionItem> ReturnedItems { get; set; } = ImmutableArray<CodeAnalysis.Completion.CompletionItem>.Empty;

        public (int defaultItemCount, int nonDefaultItemCount) ItemCounts { get; set; }

        internal override async Task<CodeAnalysis.Completion.CompletionList> GetCompletionsAsync(
            Document document, int caretPosition, CodeAnalysis.Completion.CompletionOptions options, OptionSet passThroughOptions,
            CompletionTrigger trigger = default, ImmutableHashSet<string> roles = null, CancellationToken cancellationToken = default)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var defaultItemSpan = GetDefaultCompletionListSpan(text, caretPosition);

            return CodeAnalysis.Completion.CompletionList.Create(defaultItemSpan, ReturnedItems);
        }

        public override Task<CompletionChange> GetChangeAsync(Document document, CodeAnalysis.Completion.CompletionItem item, char? commitCharacter = null, CancellationToken cancellationToken = default)
        {
            Assert.Contains(item, ReturnedItems);
            throw new Exception("GetChangeAsync throws");
        }

        [ExportLanguageServiceFactory(typeof(CompletionService), LanguageNames.CSharp, ServiceLayer.Test), Shared]
        internal sealed class Factory : ILanguageServiceFactory
        {
            private readonly IAsynchronousOperationListenerProvider _listenerProvider;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory(IAsynchronousOperationListenerProvider listenerProvider)
            {
                _listenerProvider = listenerProvider;
            }

            public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            {
                return new CSharpLspThrowExceptionOnChangeCompletionService(languageServices.LanguageServices.SolutionServices, _listenerProvider);
            }
        }
    }

    [Theory, CombinatorialData]
    public async Task TestHandleExceptionFromGetCompletionChange(bool mutatingLspWorkspace)
    {
        var markup = "Item {|caret:|}";
        await using var testLspServer = await CreateTestLspServerAsync(new[] { markup }, LanguageNames.CSharp, mutatingLspWorkspace,
            new InitializationOptions { ClientCapabilities = DefaultClientCapabilities, CallInitialized = true },
            extraExportedTypes: new[] { typeof(CSharpLspThrowExceptionOnChangeCompletionService.Factory) }.ToList());

        var mockService = testLspServer.TestWorkspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService<CompletionService>() as CSharpLspThrowExceptionOnChangeCompletionService;
        var builder = ImmutableArray.CreateBuilder<CodeAnalysis.Completion.CompletionItem>();
        builder.Add(CodeAnalysis.Completion.CompletionItem.Create("SimpleItem"));

        var importItem = CodeAnalysis.Completion.CompletionItem.Create("ExpandedItem");
        importItem.Flags |= CodeAnalysis.Completion.CompletionItemFlags.Expanded;
        builder.Add(importItem);

        builder.Add(CodeAnalysis.Completion.CompletionItem.Create("ComplexItem", isComplexTextEdit: true));

        mockService.ReturnedItems = builder.ToImmutable();

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

        // getting and resolving completions should not throw
        var results = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None);
        foreach (var item in results.Items)
        {
            item.Data = results.ItemDefaults.Data;
            var resolvedItem = await testLspServer.ExecuteRequestAsync<LSP.CompletionItem, LSP.CompletionItem>(LSP.Methods.TextDocumentCompletionResolveName, item, CancellationToken.None).ConfigureAwait(false);

            if (item.Label == "SimpleItem")
            {
                Assert.Null(item.TextEditText);
                Assert.Null(resolvedItem.AdditionalTextEdits);
                Assert.Null(resolvedItem.Command);
            }
            else if (item.Label == "ExpandedItem")
            {
                Assert.Null(item.TextEditText);
                Assert.Null(resolvedItem.AdditionalTextEdits);
                Assert.Null(resolvedItem.Command);
            }
            else if (item.Label == "ComplexItem")
            {
                Assert.Equal("", item.TextEditText);
                Assert.Null(item.TextEdit);
                Assert.Null(resolvedItem.AdditionalTextEdits);

                Assert.Equal(nameof(DefaultLspCompletionResultCreationService.CompleteComplexEditCommand), resolvedItem.Command.Title);
                Assert.Equal(DefaultLspCompletionResultCreationService.CompleteComplexEditCommand, resolvedItem.Command.CommandIdentifier);

                Assert.Equal(completionParams.TextDocument.Uri, ProtocolConversions.CreateAbsoluteUri((string)resolvedItem.Command.Arguments[0]));

                var expectedEdit = new TextEdit { Range = new LSP.Range { Start = new(0, 5), End = new(0, 5) }, NewText = "ComplexItem" };
                AssertJsonEquals(expectedEdit, resolvedItem.Command.Arguments[1]);

                Assert.Equal(false, resolvedItem.Command.Arguments[2]);
                Assert.Equal((long)-1, resolvedItem.Command.Arguments[3]);
            }
        }
    }

    [Theory, CombinatorialData]
    public async Task TestOverrideCompletionWithOutCommonReferences(bool mutatingLspWorkspace)
    {
        var markup = """
                     public abstract class BaseClass
                     {
                         public abstract bool AbstractMethod(int x);
                     }
                     
                     public class MyClass : BaseClass
                     {
                         override {|caret:|}
                     }
                     """;
        await using var testLspServer = await CreateTestLspServerAsync(new[] { markup }, LanguageNames.CSharp, mutatingLspWorkspace,
            new InitializationOptions { ClientCapabilities = DefaultClientCapabilities, CallInitialized = true }, commonReferences: false);

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

        // getting and resolving completions should not throw

        var results = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None);
        var item = results.Items.Single(i => i.FilterText == "AbstractMethod");
        Assert.Equal("", item.TextEditText);
        Assert.Null(item.TextEdit);

        item.Data = results.ItemDefaults.Data;

        var resolvedItem = await testLspServer.ExecuteRequestAsync<LSP.CompletionItem, LSP.CompletionItem>(LSP.Methods.TextDocumentCompletionResolveName, item, CancellationToken.None).ConfigureAwait(false);

        Assert.Null(resolvedItem.AdditionalTextEdits);

        Assert.Equal(nameof(DefaultLspCompletionResultCreationService.CompleteComplexEditCommand), resolvedItem.Command.Title);
        Assert.Equal(DefaultLspCompletionResultCreationService.CompleteComplexEditCommand, resolvedItem.Command.CommandIdentifier);

        Assert.Equal(completionParams.TextDocument.Uri, ProtocolConversions.CreateAbsoluteUri((string)resolvedItem.Command.Arguments[0]));

        var expectedEdit = new TextEdit { Range = new LSP.Range { Start = new(7, 4), End = new(7, 13) }, NewText = "public override global::System.Boolean AbstractMethod(global::System.Int32 x)\r\n    {\r\n        throw new System.NotImplementedException();\r\n    }" };
        AssertJsonEquals(expectedEdit, resolvedItem.Command.Arguments[1]);

        Assert.Equal(false, resolvedItem.Command.Arguments[2]);
        Assert.Equal((long)268, resolvedItem.Command.Arguments[3]);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/6495")]
    public async Task FilteringShouldBeDoneByTextBeforeCursorLocation(bool mutatingLspWorkspace)
    {
        var markup =
@"
public class Z
{
    public int M()
    {
        int ia, ib, ic, ifa, ifb, ifc; 
        i{|caret:|}Exception
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, DefaultClientCapabilities);
        var caret = testLspServer.GetLocations("caret").Single();
        await testLspServer.OpenDocumentAsync(caret.Uri);

        var completionParams = new LSP.CompletionParams()
        {
            TextDocument = CreateTextDocumentIdentifier(caret.Uri),
            Position = caret.Range.Start,
            Context = new LSP.CompletionContext()
            {
                TriggerKind = LSP.CompletionTriggerKind.Invoked,
            }
        };

        var globalOptions = testLspServer.TestWorkspace.GetService<IGlobalOptionService>();
        var listMaxSize = 3;

        globalOptions.SetGlobalOption(LspOptionsStorage.MaxCompletionListSize, listMaxSize);

        // Because of the limit in list size, we should not have item "if" returned here
        var results = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None);
        AssertEx.NotNull(results);
        Assert.True(results.IsIncomplete);
        Assert.Equal(listMaxSize, results.Items.Length);
        Assert.False(results.Items.Any(i => i.Label == "if"));

        await testLspServer.InsertTextAsync(caret.Uri, (caret.Range.End.Line, caret.Range.End.Character, "f"));

        completionParams = CreateCompletionParams(
            GetLocationPlusOne(caret),
            invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
            triggerCharacter: "f",
            triggerKind: LSP.CompletionTriggerKind.TriggerForIncompleteCompletions);

        // Now that user typed "Z", we should have item "Z" in the updated list since it's a perfect match
        results = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None);
        Assert.True(results.IsIncomplete);
        Assert.Equal(listMaxSize, results.Items.Length);
        Assert.True(results.Items.Any(i => i.Label == "if"));

    }
}
