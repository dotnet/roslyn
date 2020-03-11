// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.InitializeName)]
    internal class InitializeHandler : IRequestHandler<InitializeParams, InitializeResult>
    {
        [ImportingConstructor]
        public InitializeHandler()
        {
        }

        public Task<InitializeResult> HandleRequestAsync(Solution solution, InitializeParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            var csharpCompletionService = solution.Workspace.Services.GetRequiredLanguageService<Completion.CompletionService>(LanguageNames.CSharp);
            var vbCompletionService = solution.Workspace.Services.GetRequiredLanguageService<Completion.CompletionService>(LanguageNames.VisualBasic);
            var allProviders = csharpCompletionService.GetCompletionProviders().Concat(vbCompletionService.GetCompletionProviders());
            var triggerCharacters = allProviders.SelectMany(p => p.PossibleTriggerCharacters).Distinct().Select(c => c.ToString()).ToArray();

            return Task.FromResult(new InitializeResult
            {
                Capabilities = new ServerCapabilities
                {
                    DefinitionProvider = true,
                    RenameProvider = true,
                    ImplementationProvider = true,
                    CompletionProvider = new CompletionOptions { ResolveProvider = true, TriggerCharacters = triggerCharacters },
                    SignatureHelpProvider = new SignatureHelpOptions { TriggerCharacters = new[] { "(", "," } },
                    DocumentSymbolProvider = true,
                    WorkspaceSymbolProvider = true,
                    DocumentFormattingProvider = true,
                    DocumentRangeFormattingProvider = true,
                    DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions { FirstTriggerCharacter = "}", MoreTriggerCharacter = new[] { ";", "\n" } },
                    DocumentHighlightProvider = true,
                }
            });
        }
    }
}
