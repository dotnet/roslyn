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

public sealed class HeadlessDialogCodeActionsTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    protected override TestComposition Composition
        => base.Composition.AddParts(typeof(HeadlessTestPickMembersService), typeof(HeadlessTestLegacyGlobalOptionsWorkspaceService));

    [Theory, CombinatorialData]
    public async Task GenerateEqualsAppearsWhenPickMembersServiceRegistered(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class {|caret:|}A
            {
                int X;
                int Y;
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(
            markup, mutatingLspWorkspace,
            initializationOptions: new InitializationOptions { ClientCapabilities = new VSInternalClientCapabilities { SupportsVisualStudioExtensions = true } });

        var caret = testLspServer.GetLocations("caret").Single();
        var results = await ExecuteRequestAsync(testLspServer, caret);

        Assert.Contains(results, a => a.Title.StartsWith("Generate Equals"));
    }

    [Theory, CombinatorialData]
    public async Task GenerateConstructorFromMembersAppearsWhenPickMembersServiceRegistered(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class {|caret:|}A
            {
                int X;
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(
            markup, mutatingLspWorkspace,
            initializationOptions: new InitializationOptions { ClientCapabilities = new VSInternalClientCapabilities { SupportsVisualStudioExtensions = true } });

        var caret = testLspServer.GetLocations("caret").Single();
        var results = await ExecuteRequestAsync(testLspServer, caret);

        Assert.Contains(results, a => a.Title.Contains("constructor", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<CodeAction[]> ExecuteRequestAsync(TestLspServer testLspServer, LSP.Location caret)
    {
        var parameters = new CodeActionParams
        {
            TextDocument = new TextDocumentIdentifier { DocumentUri = caret.DocumentUri },
            Range = caret.Range,
            Context = new CodeActionContext(),
        };
        var result = await testLspServer.ExecuteRequestAsync<CodeActionParams, CodeAction[]>(
            LSP.Methods.TextDocumentCodeActionName, parameters, CancellationToken.None);
        return result ?? [];
    }

    [ExportWorkspaceService(typeof(IPickMembersService)), Shared, PartNotDiscoverable]
    private sealed class HeadlessTestPickMembersService : IPickMembersService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HeadlessTestPickMembersService() { }

        public PickMembersResult PickMembers(
            string title,
            ImmutableArray<ISymbol> members,
            ImmutableArray<PickMembersOption> options = default,
            bool selectAll = true)
            => new(members, options.IsDefault ? [] : options, selectAll);
    }

    [ExportWorkspaceService(typeof(ILegacyGlobalOptionsWorkspaceService)), Shared, PartNotDiscoverable]
    private sealed class HeadlessTestLegacyGlobalOptionsWorkspaceService : ILegacyGlobalOptionsWorkspaceService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HeadlessTestLegacyGlobalOptionsWorkspaceService() { }

        public bool RazorUseTabs => false;
        public int RazorTabSize => 4;
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
