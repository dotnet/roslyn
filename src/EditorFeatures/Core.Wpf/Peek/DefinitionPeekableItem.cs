// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Peek
{
    internal sealed class DefinitionPeekableItem : PeekableItem
    {
        private readonly Workspace _workspace;
        private readonly ProjectId _projectId;
        private readonly SymbolKey _symbolKey;
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
        private readonly IGlobalOptionService _globalOptions;
        private readonly IThreadingContext _threadingContext;

        public DefinitionPeekableItem(
            Workspace workspace, ProjectId projectId, SymbolKey symbolKey,
            IPeekResultFactory peekResultFactory,
            IMetadataAsSourceFileService metadataAsSourceService,
            IGlobalOptionService globalOptions,
            IThreadingContext threadingContext)
            : base(peekResultFactory)
        {
            _workspace = workspace;
            _projectId = projectId;
            _symbolKey = symbolKey;
            _metadataAsSourceFileService = metadataAsSourceService;
            _globalOptions = globalOptions;
            _threadingContext = threadingContext;
        }

        public override IEnumerable<IPeekRelationship> Relationships
            => [PredefinedPeekRelationships.Definitions];

        public override IPeekResultSource GetOrCreateResultSource(string relationshipName)
            => new ResultSource(this);

        private sealed class ResultSource : IPeekResultSource
        {
            private readonly DefinitionPeekableItem _peekableItem;

            public ResultSource(DefinitionPeekableItem peekableItem)
                => _peekableItem = peekableItem;

            public void FindResults(string relationshipName, IPeekResultCollection resultCollection, CancellationToken cancellationToken, IFindPeekResultsCallback callback)
            {
                if (relationshipName != PredefinedPeekRelationships.Definitions.Name)
                    return;

                // Note: this is called on a background thread, but we must block the thread since the API doesn't support proper asynchrony.
                var success = _peekableItem._threadingContext.JoinableTaskFactory.Run(async () => await FindResultsAsync(
                    resultCollection, callback, cancellationToken).ConfigureAwait(false));
                if (!success)
                    callback.ReportFailure(new Exception(EditorFeaturesResources.No_information_found));
            }

            private async Task<bool> FindResultsAsync(IPeekResultCollection resultCollection, IFindPeekResultsCallback callback, CancellationToken cancellationToken)
            {
                var workspace = _peekableItem._workspace;
                var solution = workspace.CurrentSolution;
                var project = solution.GetProject(_peekableItem._projectId);
                if (project is null)
                    return false;

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                if (compilation is null)
                    return false;

                var symbol = _peekableItem._symbolKey.Resolve(compilation, ignoreAssemblyKey: true, cancellationToken: cancellationToken).Symbol;
                if (symbol == null)
                    return false;

                var sourceLocations = symbol.Locations.Where(l => l.IsInSource).ToList();

                if (sourceLocations.Count == 0)
                {
                    // It's a symbol from metadata, so we want to go produce it from metadata
                    var options = _peekableItem._globalOptions.GetMetadataAsSourceOptions();
                    var declarationFile = await _peekableItem._metadataAsSourceFileService.GetGeneratedFileAsync(workspace, project, symbol, signaturesOnly: false, options: options, cancellationToken: cancellationToken).ConfigureAwait(false);
                    var peekDisplayInfo = new PeekResultDisplayInfo(declarationFile.DocumentTitle, declarationFile.DocumentTooltip, declarationFile.DocumentTitle, declarationFile.DocumentTooltip);
                    var identifierSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                    var entityOfInterestSpan = PeekHelpers.GetEntityOfInterestSpan(symbol, workspace, declarationFile.IdentifierLocation, cancellationToken);
                    resultCollection.Add(PeekHelpers.CreateDocumentPeekResult(declarationFile.FilePath, identifierSpan, entityOfInterestSpan, peekDisplayInfo, _peekableItem.PeekResultFactory, isReadOnly: true));
                }

                var processedSourceLocations = 0;
                foreach (var declaration in sourceLocations)
                {
                    var declarationLocation = declaration.GetMappedLineSpan();

                    var entityOfInterestSpan = PeekHelpers.GetEntityOfInterestSpan(symbol, workspace, declaration, cancellationToken);
                    resultCollection.Add(PeekHelpers.CreateDocumentPeekResult(declarationLocation.Path, declarationLocation.Span, entityOfInterestSpan, _peekableItem.PeekResultFactory));
                    callback.ReportProgress(100 * ++processedSourceLocations / sourceLocations.Count);
                }

                return true;
            }
        }
    }
}
