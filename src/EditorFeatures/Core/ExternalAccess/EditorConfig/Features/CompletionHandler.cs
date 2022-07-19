// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.AccessControl;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features
{
    [ExportStatelessLspService(typeof(CompletionHandler), ProtocolConstants.EditorConfigLanguageContract), Shared]
    [Method(Methods.TextDocumentCompletionName)]
    internal class CompletionHandler : IRequestHandler<CompletionParams, CompletionList?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHandler()
        {
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(CompletionParams request) => request.TextDocument;

        public async Task<CompletionList?> HandleRequestAsync(CompletionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.AdditionalDocument;
            Contract.ThrowIfNull(document);

            var workspace = context.Solution?.Workspace;
            Contract.ThrowIfNull(workspace);

            Contract.ThrowIfNull(request.Context);

            var filePath = document.FilePath;
            Contract.ThrowIfNull(filePath);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var offset = text.Lines.GetPosition(ProtocolConversions.PositionToLinePosition(request.Position));

            var settingsAggregator = workspace.Services.GetRequiredService<ISettingsAggregator>();

            var codeStyleProvider = settingsAggregator.GetSettingsProvider<CodeStyleSetting>(filePath);
            var codeStyleSettings = codeStyleProvider?.GetCurrentDataSnapshot();
            var cs = codeStyleSettings?.Select(setting => setting);

            var whitespaceProvider = settingsAggregator.GetSettingsProvider<WhitespaceSetting>(filePath);
            var whitespaceSettings = whitespaceProvider?.GetCurrentDataSnapshot();
            var ws = whitespaceSettings?.Select(setting => setting.Key.Option.StorageLocations.FirstOrDefault());

            var analyzerProvider = settingsAggregator.GetSettingsProvider<AnalyzerSetting>(filePath);
            var analyzerSettings = analyzerProvider?.GetCurrentDataSnapshot();
            var ans = analyzerSettings?.Select(setting => setting);

            var namingStyleProvider = settingsAggregator.GetSettingsProvider<NamingStyleSetting>(filePath);
            var namingStyleSettings = namingStyleProvider?.GetCurrentDataSnapshot();
            var ns = analyzerSettings?.Select(setting => setting);

            if (request.Context.TriggerCharacter == "=")
            {
                return new CompletionList
                {
                    Items = new CompletionItem[]
                    {
                        new CompletionItem
                        {
                            Label = "true",
                            InsertText = " true",
                            Documentation = "This is documentation",
                            Kind = CompletionItemKind.Value,
                        },
                        new CompletionItem
                        {
                            Label = "false",
                            InsertText = " false",
                            Documentation = "This is documentation",
                            Kind = CompletionItemKind.Value,
                        }
                    }
                };
            }

            return new CompletionList
            {
                Items = new CompletionItem[]
                {
                    new CompletionItem
                    {
                        Label = "csharp_new_line_before_else",
                        InsertText = "csharp_new_line_before_else",
                        Documentation = "This is documentation",
                        Kind = CompletionItemKind.Variable,
                    }
                }
            };
        }
    }
}
