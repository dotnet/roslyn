// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[Shared]
[ExportLanguageService(typeof(IEditorInlineRenameService), InternalLanguageNames.TypeScript)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptEditorInlineRenameService(
    [Import(AllowDefault = true)] Lazy<VSTypeScriptEditorInlineRenameServiceImplementation>? service) : IEditorInlineRenameService
{
    private readonly Lazy<VSTypeScriptEditorInlineRenameServiceImplementation>? _service = service;

    public Task<ImmutableDictionary<string, string[]>> GetRenameContextAsync(IInlineRenameInfo renameInfo, CancellationToken cancellationToken)
    {
        return Task.FromResult(ImmutableDictionary<string, string[]>.Empty);
    }

    public async Task<IInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken)
    {
        if (_service != null)
        {
            return await _service.Value.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
        }

        return AbstractEditorInlineRenameService.DefaultFailureInfo;
    }
}
