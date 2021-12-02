// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.API
{
    internal sealed class UnitTestingStackTraceService
    {
        private readonly IStackTraceExplorerService _stackTraceService;

        public UnitTestingStackTraceService(HostWorkspaceServices services)
            => _stackTraceService = services.GetRequiredService<IStackTraceExplorerService>();

        public async Task<ImmutableArray<Frame>> ParseAsync(string input, CancellationToken cancellationToken)
        {
            var result = await StackTraceAnalyzer.AnalyzeAsync(input, cancellationToken).ConfigureAwait(false);
            return result.ParsedFrames.SelectAsArray(p => new Frame(p, _stackTraceService));
        }

        public readonly struct Frame
        {
            private readonly ParsedFrame _parsedFrame;
            private readonly IStackTraceExplorerService _stackTraceService;

            public Frame(ParsedFrame parsedFrame, IStackTraceExplorerService service)
            {
                _parsedFrame = parsedFrame;
                _stackTraceService = service;
            }

            public (Document? document, int lineNumber) TryGetDocumentAndLine(Solution solution) 
                => _stackTraceService.GetDocumentAndLine(solution, _parsedFrame);

            public async Task<Definition?> TryFindMethodDefinitionAsync(Solution solution, CancellationToken cancellationToken)
            {
                var definition = await _stackTraceService.TryFindDefinitionAsync(solution, _parsedFrame, StackFrameSymbolPart.Method, cancellationToken).ConfigureAwait(false);
                if (definition == null)
                {
                    return null;
                }

                return new Definition(definition);
            }
        }

        public readonly struct Definition
        {
            private readonly FindUsages.DefinitionItem _definition;

            public Definition(FindUsages.DefinitionItem definition)
            {
                _definition = definition;
            }

            public async Task<bool> TryNavigateToAsync(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
            {
                var canNavigate = await _definition.CanNavigateToAsync(workspace, cancellationToken).ConfigureAwait(false);
                if (canNavigate)
                {
                    return await _definition.TryNavigateToAsync(workspace, showInPreviewTab, activateTab, cancellationToken).ConfigureAwait(false);
                }

                return false;
            }
        }
    }
}
