// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(IFindUsagesService), InternalLanguageNames.TypeScript), Shared]
    internal sealed class VSTypeScriptFindUsagesService : IFindUsagesService
    {
        private readonly IVSTypeScriptFindUsagesService _underlyingService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptFindUsagesService(IVSTypeScriptFindUsagesService underlyingService)
        {
            _underlyingService = underlyingService;
        }

        public Task FindReferencesAsync(IFindUsagesContext context, Document document, int position, CancellationToken cancellationToken)
            => _underlyingService.FindReferencesAsync(document, position, new Context(context), cancellationToken);

        public Task FindImplementationsAsync(IFindUsagesContext context, Document document, int position, CancellationToken cancellationToken)
            => _underlyingService.FindImplementationsAsync(document, position, new Context(context), cancellationToken);

        private sealed class Context : IVSTypeScriptFindUsagesContext
        {
            private readonly IFindUsagesContext _context;

            public Context(IFindUsagesContext context)
            {
                _context = context;
            }

            public IVSTypeScriptStreamingProgressTracker ProgressTracker
                => new ProgressTracker(_context.ProgressTracker);

            public ValueTask ReportMessageAsync(string message, CancellationToken cancellationToken)
                => _context.ReportMessageAsync(message, cancellationToken);

            public ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken)
                => _context.SetSearchTitleAsync(title, cancellationToken);

            public ValueTask OnDefinitionFoundAsync(VSTypeScriptDefinitionItem definition, CancellationToken cancellationToken)
                => _context.OnDefinitionFoundAsync(definition.UnderlyingObject, cancellationToken);

            public ValueTask OnReferenceFoundAsync(VSTypeScriptSourceReferenceItem reference, CancellationToken cancellationToken)
                => _context.OnReferenceFoundAsync(reference.UnderlyingObject, cancellationToken);

            public ValueTask OnCompletedAsync(CancellationToken cancellationToken)
                => ValueTaskFactory.CompletedTask;
        }

        private sealed class ProgressTracker : IVSTypeScriptStreamingProgressTracker
        {
            private readonly IStreamingProgressTracker _progressTracker;

            public ProgressTracker(IStreamingProgressTracker progressTracker)
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
