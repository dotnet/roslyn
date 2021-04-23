// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    [Obsolete("Legacy API for TypeScript.  Once TypeScript moves to IVSTypeScriptFindUsagesService", error: false)]
    internal interface IFindUsagesService : ILanguageService
    {
        /// <summary>
        /// Finds the references for the symbol at the specific position in the document,
        /// pushing the results into the context instance.
        /// </summary>
        Task FindReferencesAsync(Document document, int position, IFindUsagesContext context);

        /// <summary>
        /// Finds the implementations for the symbol at the specific position in the document,
        /// pushing the results into the context instance.
        /// </summary>
        Task FindImplementationsAsync(Document document, int position, IFindUsagesContext context);
    }

    internal interface IFindUsagesServiceRenameOnceTypeScriptMovesToExternalAccess : ILanguageService
    {
        /// <summary>
        /// Finds the references for the symbol at the specific position in the document,
        /// pushing the results into the context instance.
        /// </summary>
        Task FindReferencesAsync(Document document, int position, IFindUsagesContextRenameOnceTypeScriptMovesToExternalAccess context, CancellationToken cancellationToken);

        /// <summary>
        /// Finds the implementations for the symbol at the specific position in the document,
        /// pushing the results into the context instance.
        /// </summary>
        Task FindImplementationsAsync(Document document, int position, IFindUsagesContextRenameOnceTypeScriptMovesToExternalAccess context, CancellationToken cancellationToken);
    }

    internal class FindUsagesServiceWrapper : IFindUsagesServiceRenameOnceTypeScriptMovesToExternalAccess
    {
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly IFindUsagesService _legacyService;

        public FindUsagesServiceWrapper(IFindUsagesService legacyService)
        {
            _legacyService = legacyService;
        }
#pragma warning restore CS0618 // Type or member is obsolete

        public Task FindImplementationsAsync(Document document, int position, IFindUsagesContextRenameOnceTypeScriptMovesToExternalAccess context, CancellationToken cancellationToken)
            => _legacyService.FindImplementationsAsync(document, position, new FindUsagesContextWrapper(context, cancellationToken));

        public Task FindReferencesAsync(Document document, int position, IFindUsagesContextRenameOnceTypeScriptMovesToExternalAccess context, CancellationToken cancellationToken)
            => _legacyService.FindReferencesAsync(document, position, new FindUsagesContextWrapper(context, cancellationToken));
    }

    internal class FindUsagesContextWrapper : IFindUsagesContext
    {
        private readonly IFindUsagesContextRenameOnceTypeScriptMovesToExternalAccess _context;
        private readonly CancellationToken _cancellationToken;

        public FindUsagesContextWrapper(
            IFindUsagesContextRenameOnceTypeScriptMovesToExternalAccess context,
            CancellationToken cancellationToken)
        {
            _context = context;
            _cancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken => _cancellationToken;

        public IStreamingProgressTracker ProgressTracker => _context.ProgressTracker;

        public ValueTask ReportMessageAsync(string message)
            => _context.ReportMessageAsync(message, _cancellationToken);

        public ValueTask SetSearchTitleAsync(string title)
            => _context.SetSearchTitleAsync(title, _cancellationToken);

        public ValueTask OnDefinitionFoundAsync(DefinitionItem definition)
            => _context.OnDefinitionFoundAsync(definition, _cancellationToken);

        public ValueTask OnReferenceFoundAsync(SourceReferenceItem reference)
            => _context.OnReferenceFoundAsync(reference, _cancellationToken);

        public ValueTask ReportProgressAsync(int current, int maximum)
            => default;
    }
}
