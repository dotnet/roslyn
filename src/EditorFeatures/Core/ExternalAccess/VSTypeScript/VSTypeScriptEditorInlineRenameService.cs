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
        private readonly Lazy<IVSTypeScriptEditorInlineRenameService> _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptEditorInlineRenameService(Lazy<IVSTypeScriptEditorInlineRenameService> service)
        {
            Contract.ThrowIfNull(service);
            _service = service;
        }

        public async Task<IInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var info = await _service.Value.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (info != null)
            {
                return new VSTypeScriptInlineRenameInfo(info);
            }
            else
            {
                return null;
            }
        }
    }
}
