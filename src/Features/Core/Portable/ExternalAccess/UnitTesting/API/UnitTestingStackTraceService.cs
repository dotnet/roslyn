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
        private readonly Workspace _workspace;
        private readonly IStackTraceExplorerService _stackTraceService;

        public UnitTestingStackTraceService(Workspace workspace)
        {
            _workspace = workspace;
            _stackTraceService = workspace.Services.GetRequiredService<IStackTraceExplorerService>();
        }

        public async Task<ImmutableArray<Frame>> TryParseAsync(string input, CancellationToken cancellationToken)
        {
            var result = await StackTraceAnalyzer.AnalyzeAsync(input, cancellationToken).ConfigureAwait(false);
            return result.ParsedFrames.SelectAsArray(p => new Frame(p, _stackTraceService, _workspace));
        }

        public readonly struct Frame
        {
            private readonly ParsedFrame _parsedFrame;
            private readonly IStackTraceExplorerService _stackTraceService;
            private readonly Workspace _workspace;

            public Frame(ParsedFrame parsedFrame, IStackTraceExplorerService service, Workspace workspace)
            {
                _parsedFrame = parsedFrame;
                _stackTraceService = service;
                _workspace = workspace;
            }

            public (Document? document, int lineNumber) TryGetDocumentAndLine()
                => _stackTraceService.GetDocumentAndLine(_workspace.CurrentSolution, _parsedFrame);

            public async Task<Definition?> TryFindMethodDefinitionAsync(CancellationToken cancellationToken)
            {
                var definition = await _stackTraceService.TryFindDefinitionAsync(_workspace.CurrentSolution, _parsedFrame, StackFrameSymbolPart.Method, cancellationToken).ConfigureAwait(false);
                if (definition == null)
                {
                    return null;
                }

                return new Definition(definition, _workspace);
            }
        }

        public readonly struct Definition
        {
            private readonly FindUsages.DefinitionItem _definition;
            private readonly Workspace _workspace;

            public Definition(FindUsages.DefinitionItem definition, Workspace workspace)
            {
                _definition = definition;
                _workspace = workspace;
            }

            public async Task<bool> TryNavigateToAsync(bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
            {
                var canNavigate = await _definition.CanNavigateToAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (canNavigate)
                {
                    return await _definition.TryNavigateToAsync(_workspace, showInPreviewTab, activateTab, cancellationToken).ConfigureAwait(false);
                }

                return false;
            }
        }
    }
}
