﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractEditorInlineRenameService : IEditorInlineRenameService
{
    private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;
    private readonly IGlobalOptionService _globalOptions;

    protected AbstractEditorInlineRenameService(IEnumerable<IRefactorNotifyService> refactorNotifyServices, IGlobalOptionService globalOptions)
    {
        _refactorNotifyServices = refactorNotifyServices;
        _globalOptions = globalOptions;
    }

    public async Task<IInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var symbolicInfo = await SymbolicRenameInfo.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (symbolicInfo.LocalizedErrorMessage != null)
            return new FailureInlineRenameInfo(symbolicInfo.LocalizedErrorMessage);

        return new SymbolInlineRenameInfo(
            _refactorNotifyServices, symbolicInfo, _globalOptions.CreateProvider(), cancellationToken);
    }

    public bool IsRenameContextSupported => true;

    public Task<ImmutableDictionary<string, ImmutableArray<string>>> GetRenameContextAsync(IInlineRenameInfo inlineRenameInfo, IInlineRenameLocationSet inlineRenameLocationSet, CancellationToken cancellationToken)
    {
        return GetRenameContextCoreAsync(inlineRenameInfo, inlineRenameLocationSet, cancellationToken);
    }

    protected virtual Task<ImmutableDictionary<string, ImmutableArray<string>>> GetRenameContextCoreAsync(IInlineRenameInfo inlineRenameInfo, IInlineRenameLocationSet inlineRenameLocationSet, CancellationToken cancellationToken)
    {
        return Task.FromResult(ImmutableDictionary<string, ImmutableArray<string>>.Empty);
    }
}
