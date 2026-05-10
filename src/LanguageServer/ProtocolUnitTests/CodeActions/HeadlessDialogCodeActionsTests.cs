// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions;

public sealed class HeadlessDialogCodeActionsTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    protected override TestComposition Composition => base.Composition.AddParts(
        typeof(HeadlessTestPickMembersService),
        typeof(HeadlessTestLegacyGlobalOptionsWorkspaceService));

    [Theory, CombinatorialData]
    public async Task GenerateEqualsAppearsWhenPickMembersServiceRegistered(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                public int X;
                public int {|caret:|}Y;
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        var caretLocation = testLspServer.GetLocations("caret").Single();

        var results = await RunGetCodeActionsAsync(testLspServer, caretLocation);

        Assert.Contains(results, r => r.Title.Contains("Generate Equals"));
    }

    [Theory, CombinatorialData]
    public async Task GenerateConstructorFromMembersAppearsWhenPickMembersServiceRegistered(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                public int {|caret:X|};
                public int Y;
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        var caretLocation = testLspServer.GetLocations("caret").Single();

        var results = await RunGetCodeActionsAsync(testLspServer, caretLocation);

        Assert.Contains(results, r => r.Title.Contains("Generate constructor"));
    }

    [Theory, CombinatorialData]
    public async Task DialogActionsHiddenWhenPickMembersServiceMissing(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                public int X;
                public int {|caret:|}Y;
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(
            markup,
            mutatingLspWorkspace,
            new InitializationOptions { ClientCapabilities = CapabilitiesWithVSExtensions },
            // Intentionally omit HeadlessTestPickMembersService so that no IPickMembersService is registered.
            composition: base.Composition.AddParts(typeof(HeadlessTestLegacyGlobalOptionsWorkspaceService)));
        var caretLocation = testLspServer.GetLocations("caret").Single();

        var results = await RunGetCodeActionsAsync(testLspServer, caretLocation);

        // The dialog-driven actions (CodeActionWithOptions) should be filtered out when no IPickMembersService
        // is available. Non-dialog variants like "Generate Equals(...)" or auto-generated constructors stay.
        Assert.DoesNotContain(results, r => r.Title == "Generate constructor from members...");
        Assert.DoesNotContain(results, r => r.Title == "Generate Equals(...)...");
    }

    private static async Task<VSInternalCodeAction[]> RunGetCodeActionsAsync(TestLspServer testLspServer, LSP.Location caret)
    {
        var codeActionParams = new CodeActionParams
        {
            TextDocument = new LSP.TextDocumentIdentifier { DocumentUri = caret.DocumentUri },
            Range = caret.Range,
            Context = new CodeActionContext()
        };
        var result = await testLspServer.ExecuteRequestAsync<CodeActionParams, CodeAction[]>(
            LSP.Methods.TextDocumentCodeActionName, codeActionParams, CancellationToken.None);
        return [.. result!.OfType<VSInternalCodeAction>()];
    }

    [ExportWorkspaceService(typeof(IPickMembersService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class HeadlessTestPickMembersService() : IPickMembersService
    {
        public PickMembersResult PickMembers(
            string title,
            ImmutableArray<ISymbol> members,
            ImmutableArray<PickMembersOption> options = default,
            bool selectAll = true)
            => new(members, options.IsDefault ? [] : options, selectAll);
    }

    [ExportWorkspaceService(typeof(ILegacyGlobalOptionsWorkspaceService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    private sealed class HeadlessTestLegacyGlobalOptionsWorkspaceService : ILegacyGlobalOptionsWorkspaceService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HeadlessTestLegacyGlobalOptionsWorkspaceService() { }

        public bool RazorUseTabs => LineFormattingOptions.Default.UseTabs;
        public int RazorTabSize => LineFormattingOptions.Default.TabSize;
        public bool GenerateOverrides { get => true; set { } }

        public bool GetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language) => false;
        public void SetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language, bool value) { }

        public bool GetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language) => false;
        public void SetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language, bool value) { }

        public bool GetGenerateConstructorFromMembersOptionsAddNullChecks(string language) => false;
        public void SetGenerateConstructorFromMembersOptionsAddNullChecks(string language, bool value) { }

        public SyntaxFormattingOptions GetSyntaxFormattingOptions(LanguageServices languageServices)
            => SyntaxFormattingOptions.CommonDefaults;
    }
}
