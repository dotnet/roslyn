// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Roslyn.Utilities;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using System;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [Shared]
    [ExportLanguageService(typeof(IEditorInlineRenameService), InternalLanguageNames.TypeScript)]
    internal sealed class VSTypeScriptEditorInlineRenameService : IEditorInlineRenameService
    {
        [Obsolete]
        private readonly Lazy<IVSTypeScriptEditorInlineRenameService>? _legacyService;

        private readonly Lazy<VSTypeScriptEditorInlineRenameServiceImplementation>? _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptEditorInlineRenameService(
            [Import(AllowDefault = true)] Lazy<IVSTypeScriptEditorInlineRenameService>? legacyService,
            [Import(AllowDefault = true)] Lazy<VSTypeScriptEditorInlineRenameServiceImplementation>? service)
        {
            _service = service;
            _legacyService = legacyService;
        }

        public async Task<IInlineRenameInfo?> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            if (_legacyService != null)
            {
                var info = await _legacyService.Value.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
                return (info is null) ? null : new VSTypeScriptInlineRenameInfoLegacyWrapper(info);
            }
#pragma warning restore

            if (_service != null)
            {
                return await _service.Value.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }
    }
}
