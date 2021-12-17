// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;

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
        Task FindReferencesAsync(Document document, int position, IFindUsagesContext context);

        /// <summary>
        /// Finds the implementations for the symbol at the specific position in the document,
        /// pushing the results into the context instance.
        /// </summary>
        Task FindImplementationsAsync(Document document, int position, IFindUsagesContext context);
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

        public Task FindImplementationsAsync(Document document, int position, IFindUsagesContext context)
            => _legacyService.FindImplementationsAsync(document, position, context);

        public Task FindReferencesAsync(Document document, int position, IFindUsagesContext context)
            => _legacyService.FindReferencesAsync(document, position, context);
    }
}
