// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteFindUsages
    {
        public async Task FindImplementationsAsync(SerializableSymbolAndProjectId symbolAndProjectIdArg)
        {
            var solution = await GetSolutionAsync().ConfigureAwait(false);
            var symbolAndProjectId = await symbolAndProjectIdArg.TryRehydrateAsync(
                solution, CancellationToken).ConfigureAwait(false);

            if (symbolAndProjectId == null)
            {
                return;
            }

            var context = new FindUsagesContext(this);
            await AbstractFindUsagesService.FindImplementationsInCurrentProcessAsync(
                context, symbolAndProjectId.Value.Symbol,
                solution.GetProject(symbolAndProjectId.Value.ProjectId)).ConfigureAwait(false);
        }

        public async Task FindSymbolUsagesAsync(SerializableSymbolAndProjectId symbolAndProjectIdArg)
        {
            var solution = await GetSolutionAsync().ConfigureAwait(false);
            var symbolAndProjectId = await symbolAndProjectIdArg.TryRehydrateAsync(
                solution, CancellationToken).ConfigureAwait(false);

            if (symbolAndProjectId == null)
            {
                return;
            }

            var context = new FindUsagesContext(this);
            await AbstractFindUsagesService.FindSymbolReferencesInCurrentProcessAsync(
                context, symbolAndProjectId.Value.Symbol,
                solution.GetProject(symbolAndProjectId.Value.ProjectId)).ConfigureAwait(false);
        }

        public async Task FindLiteralUsagesAsync(string title, object value)
        {
            var solution = await GetSolutionAsync().ConfigureAwait(false);
            var context = new FindUsagesContext(this);

            await AbstractFindUsagesService.FindLiteralReferencesInCurrentProcessAsync(
                solution, title, context, value).ConfigureAwait(false);
        }

        /// <summary>
        /// An <see cref="IFindUsagesContext"/> instance we create when searching in the remote process.
        /// It will get the results of the underlying engine and will remote them over to the VS process
        /// so they can be displayed.
        /// </summary>
        private class FindUsagesContext : IFindUsagesContext
        {
            private readonly CodeAnalysisService _service;

            private readonly object _gate = new object();

            /// <summary>
            /// For each definition item we hear about and serialize over, create a unique ID to refer
            /// to that item.  When we then hear about references and serialize them over, we'll point
            /// at the corresponding ID for the definition they point to.  The VS side will then stitch
            /// things up appropriately.
            /// </summary>
            private int _nextDefinitionItemSerializationId;
            private readonly Dictionary<DefinitionItem, int> _definitionToSerializationId = new Dictionary<DefinitionItem, int>();

            public FindUsagesContext(CodeAnalysisService service)
            {
                _service = service;
            }

            public CancellationToken CancellationToken
                => _service.CancellationToken;

            public Task ReportMessageAsync(string message)
                => _service.Rpc.InvokeAsync(nameof(ReportMessageAsync), message);

            public Task SetSearchTitleAsync(string title)
                => _service.Rpc.InvokeAsync(nameof(SetSearchTitleAsync), title);

            public Task ReportProgressAsync(int current, int maximum)
                => _service.Rpc.InvokeAsync(nameof(ReportProgressAsync), current, maximum);

            public Task OnDefinitionFoundAsync(DefinitionItem definition)
            {
                int serializationId;
                lock (_gate)
                {
                    _nextDefinitionItemSerializationId++;
                    serializationId = _nextDefinitionItemSerializationId;

                    _definitionToSerializationId.Add(definition, serializationId);
                }

                return _service.Rpc.InvokeAsync(nameof(OnDefinitionFoundAsync),
                    SerializableDefinitionItem.Dehydrate(definition, serializationId));
            }

            public Task OnReferenceFoundAsync(SourceReferenceItem reference)
            {
                int definitionId;
                lock (_gate)
                {
                    definitionId = _definitionToSerializationId[reference.Definition];
                }

                return _service.Rpc.InvokeAsync(nameof(OnReferenceFoundAsync),
                    SerializableSourceReferenceItem.Dehydrate(reference, definitionId));
            }
        }
    }
}