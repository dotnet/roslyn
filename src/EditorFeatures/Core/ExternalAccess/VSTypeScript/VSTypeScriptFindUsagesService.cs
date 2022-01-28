// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(IFindUsagesService), InternalLanguageNames.TypeScript), Shared]
    internal class VSTypeScriptFindUsagesService : IFindUsagesService
    {
        private readonly IVSTypeScriptFindUsagesService _underlyingService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptFindUsagesService(IVSTypeScriptFindUsagesService underlyingService)
        {
            _underlyingService = underlyingService;
        }

        public Task FindReferencesAsync(Document document, int position, IFindUsagesContext context, CancellationToken cancellationToken)
            => _underlyingService.FindReferencesAsync(document, position, new VSTypeScriptFindUsagesContext(context), cancellationToken);

        public Task FindImplementationsAsync(Document document, int position, IFindUsagesContext context, CancellationToken cancellationToken)
            => _underlyingService.FindImplementationsAsync(document, position, new VSTypeScriptFindUsagesContext(context), cancellationToken);

        private class VSTypeScriptFindUsagesContext : IVSTypeScriptFindUsagesContext
        {
            private readonly IFindUsagesContext _context;
            private readonly Dictionary<VSTypeScriptDefinitionItem, DefinitionItem> _definitionItemMap = new();

            public VSTypeScriptFindUsagesContext(IFindUsagesContext context)
            {
                _context = context;
            }

            public IVSTypeScriptStreamingProgressTracker ProgressTracker => new VSTypeScriptStreamingProgressTracker(_context.ProgressTracker);

            public ValueTask ReportMessageAsync(string message, CancellationToken cancellationToken)
                => _context.ReportMessageAsync(message, cancellationToken);

            public ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken)
                => _context.SetSearchTitleAsync(title, cancellationToken);

            private DefinitionItem GetOrCreateDefinitionItem(VSTypeScriptDefinitionItem item)
            {
                lock (_definitionItemMap)
                {
                    if (!_definitionItemMap.TryGetValue(item, out var result))
                    {
                        result = DefinitionItem.Create(
                            item.Tags,
                            item.DisplayParts,
                            item.SourceSpans,
                            item.NameDisplayParts,
                            item.Properties,
                            item.DisplayableProperties,
                            item.DisplayIfNoReferences);
                        _definitionItemMap.Add(item, result);
                    }

                    return result;
                }
            }

            public ValueTask OnDefinitionFoundAsync(VSTypeScriptDefinitionItem definition, CancellationToken cancellationToken)
            {
                var item = GetOrCreateDefinitionItem(definition);
                return _context.OnDefinitionFoundAsync(item, cancellationToken);
            }

            public ValueTask OnReferenceFoundAsync(VSTypeScriptSourceReferenceItem reference, CancellationToken cancellationToken)
            {
                var item = GetOrCreateDefinitionItem(reference.Definition);
                return _context.OnReferenceFoundAsync(new SourceReferenceItem(item, reference.SourceSpan, reference.SymbolUsageInfo), cancellationToken);
            }
        }

        private class VSTypeScriptStreamingProgressTracker : IVSTypeScriptStreamingProgressTracker
        {
            private readonly IStreamingProgressTracker _progressTracker;

            public VSTypeScriptStreamingProgressTracker(IStreamingProgressTracker progressTracker)
            {
                _progressTracker = progressTracker;
            }

            public ValueTask AddItemsAsync(int count, CancellationToken cancellationToken)
                => _progressTracker.AddItemsAsync(count, cancellationToken);

            public ValueTask ItemCompletedAsync(CancellationToken cancellationToken)
                => _progressTracker.ItemCompletedAsync(cancellationToken);
        }
    }
}
