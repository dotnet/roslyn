// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Features.Testing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using StreamJsonRpc;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;

[ExportCSharpVisualBasicStatelessLspService(typeof(CodeLensHandler)), Shared]
[Method(LSP.Methods.TextDocumentCodeLensName)]
internal sealed class CodeLensHandler : ILspServiceDocumentRequestHandler<LSP.CodeLensParams, LSP.CodeLens[]?>
{
    public const string RunTestsCommandIdentifier = "dotnet.test.run";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeLensHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CodeLensParams request)
        => request.TextDocument;

    public async Task<LSP.CodeLens[]?> HandleRequestAsync(LSP.CodeLensParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();
        var codeLensMemberFinder = document.GetRequiredLanguageService<ICodeLensMemberFinder>();
        var members = await codeLensMemberFinder.GetCodeLensMembersAsync(document, cancellationToken).ConfigureAwait(false);

        if (members.IsEmpty)
        {
            return Array.Empty<LSP.CodeLens>();
        }

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var syntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<LSP.CodeLens>.GetInstance(out var codeLenses);
        for (var i = 0; i < members.Length; i++)
        {
            // First add references code lens.
            var member = members[i];
            var range = ProtocolConversions.TextSpanToRange(member.Span, text);
            var codeLens = new LSP.CodeLens
            {
                Range = range,
                Command = null,
                Data = new CodeLensResolveData(syntaxVersion.ToString(), i, request.TextDocument)
            };

            codeLenses.Add(codeLens);
        }

        AddTestCodeLens(codeLenses, members, document, text, request.TextDocument);

        return codeLenses.ToArray();
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
        foreach (var member in testMethodMembers)
        {
            var range = ProtocolConversions.TextSpanToRange(member.Span, text);
            var runTestsCodeLens = new LSP.CodeLens
            {
                Range = range,
                Command = new LSP.Command
                {
                    CommandIdentifier = RunTestsCommandIdentifier,
                    Arguments = new object[] { new RunTestsParams(textDocumentIdentifier, range) },
                    Title = FeaturesResources.Run_Test
                }
            };

            codeLenses.Add(runTestsCodeLens);
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
                    Arguments = new object[] { new RunTestsParams(textDocumentIdentifier, range) },
                    Title = FeaturesResources.Run_All_Tests
                }
            };

            codeLenses.Add(runTestsCodeLens);
        }
    }
}

