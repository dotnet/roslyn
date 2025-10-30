// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Features.Testing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;

[ExportCSharpVisualBasicStatelessLspService(typeof(CodeLensHandler)), Shared]
[Method(LSP.Methods.TextDocumentCodeLensName)]
internal sealed class CodeLensHandler : ILspServiceDocumentRequestHandler<LSP.CodeLensParams, LSP.CodeLens[]?>
{
    public const string RunTestsCommandIdentifier = "dotnet.test.run";

    private readonly IGlobalOptionService _globalOptionService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeLensHandler(IGlobalOptionService globalOptionService)
    {
        _globalOptionService = globalOptionService;
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CodeLensParams request)
        => request.TextDocument;

    public Task<LSP.CodeLens[]?> HandleRequestAsync(LSP.CodeLensParams request, RequestContext context, CancellationToken cancellationToken)
        => GetCodeLensAsync(request.TextDocument, context.GetRequiredDocument(), _globalOptionService, cancellationToken);

    internal static async Task<LSP.CodeLens[]?> GetCodeLensAsync(LSP.TextDocumentIdentifier textDocumentIdentifier, Document document, IGlobalOptionService globalOptionService, CancellationToken cancellationToken)
    {
        var referencesCodeLensEnabled = globalOptionService.GetOption(LspOptionsStorage.LspEnableReferencesCodeLens, document.Project.Language);
        var testsCodeLensEnabled = globalOptionService.GetOption(LspOptionsStorage.LspEnableTestsCodeLens, document.Project.Language);

        if (!referencesCodeLensEnabled && !testsCodeLensEnabled)
        {
            // No code lenses are enabled, just return.
            return [];
        }

        var codeLensMemberFinder = document.GetRequiredLanguageService<ICodeLensMemberFinder>();
        var members = await codeLensMemberFinder.GetCodeLensMembersAsync(document, cancellationToken).ConfigureAwait(false);

        if (members.IsEmpty)
        {
            return [];
        }

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        using var _ = ArrayBuilder<LSP.CodeLens>.GetInstance(out var codeLenses);

        if (referencesCodeLensEnabled)
        {
            await AddReferencesCodeLensAsync(codeLenses, members, document, text, textDocumentIdentifier, cancellationToken).ConfigureAwait(false);
        }

        if (!globalOptionService.GetOption(LspOptionsStorage.LspUsingDevkitFeatures) && testsCodeLensEnabled)
        {
            // Only return test codelenses if we're not using devkit.
            AddTestCodeLens(codeLenses, members, document, text, textDocumentIdentifier);
        }

        return codeLenses.ToArray();
    }

    private static async Task AddReferencesCodeLensAsync(
        ArrayBuilder<LSP.CodeLens> codeLenses,
        ImmutableArray<CodeLensMember> members,
        Document document,
        SourceText text,
        LSP.TextDocumentIdentifier textDocumentIdentifier,
        CancellationToken cancellationToken)
    {
        var syntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < members.Length; i++)
        {
            // First add references code lens.
            var member = members[i];
            var range = ProtocolConversions.TextSpanToRange(member.Span, text);
            var codeLens = new LSP.CodeLens
            {
                Range = range,
                Command = null,
                Data = new CodeLensResolveData(syntaxVersion.ToString(), i, textDocumentIdentifier)
            };

            codeLenses.Add(codeLens);
        }
    }

    private static void AddTestCodeLens(
        ArrayBuilder<LSP.CodeLens> codeLenses,
        ImmutableArray<CodeLensMember> members,
        Document document,
        SourceText text,
        LSP.TextDocumentIdentifier textDocumentIdentifier)
    {
        var testMethodFinder = document.GetLanguageService<ITestMethodFinder>();
        // The service is not implemented for all languages.
        if (testMethodFinder == null)
        {
            return;
        }

        // Find test method members.
        using var _ = ArrayBuilder<CodeLensMember>.GetInstance(out var testMethodMembers);
        foreach (var member in members)
        {
            var isTestMethod = testMethodFinder.IsTestMethod(member.Node);
            if (isTestMethod)
            {
                testMethodMembers.Add(member);
            }
        }

        // Find any test container members based on the test method members we found (e.g. find the class containing the test methods).
        var testContainerNodes = testMethodMembers.Select(member => member.Node.Parent);
        var testContainerMembers = members.Where(member => testContainerNodes.Contains(member.Node));

        // Create code lenses for all test methods.

        // The client will fill this in if applicable.
        string? runSettingsPath = null;
        foreach (var member in testMethodMembers)
        {
            var range = ProtocolConversions.TextSpanToRange(member.Span, text);
            var runTestsCodeLens = new LSP.CodeLens
            {
                Range = range,
                Command = new LSP.Command
                {
                    CommandIdentifier = RunTestsCommandIdentifier,
                    Arguments = [new RunTestsParams(textDocumentIdentifier, range, AttachDebugger: false, runSettingsPath)],
                    Title = FeaturesResources.Run_Test
                }
            };

            var debugTestCodeLens = new LSP.CodeLens
            {
                Range = range,
                Command = new LSP.Command
                {
                    CommandIdentifier = RunTestsCommandIdentifier,
                    Arguments = [new RunTestsParams(textDocumentIdentifier, range, AttachDebugger: true, runSettingsPath)],
                    Title = FeaturesResources.Debug_Test
                }
            };

            codeLenses.Add(runTestsCodeLens);
            codeLenses.Add(debugTestCodeLens);
        }

        // Create code lenses for all test containers.
        foreach (var member in testContainerMembers)
        {
            var range = ProtocolConversions.TextSpanToRange(member.Span, text);
            var runTestsCodeLens = new LSP.CodeLens
            {
                Range = range,
                Command = new LSP.Command
                {
                    CommandIdentifier = RunTestsCommandIdentifier,
                    Arguments = [new RunTestsParams(textDocumentIdentifier, range, AttachDebugger: false, runSettingsPath)],
                    Title = FeaturesResources.Run_All_Tests
                }
            };

            var debugTestsCodeLens = new LSP.CodeLens
            {
                Range = range,
                Command = new LSP.Command
                {
                    CommandIdentifier = RunTestsCommandIdentifier,
                    Arguments = [new RunTestsParams(textDocumentIdentifier, range, AttachDebugger: true, runSettingsPath)],
                    Title = FeaturesResources.Debug_All_Tests
                }
            };

            codeLenses.Add(runTestsCodeLens);
            codeLenses.Add(debugTestsCodeLens);
        }
    }
}

