// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.ChangeSignature;

internal sealed class ChangeSignatureCodeAction(AbstractChangeSignatureService changeSignatureService, ChangeSignatureAnalysisSucceededContext context) : CodeActionWithOptions
{
    private readonly AbstractChangeSignatureService _changeSignatureService = changeSignatureService;
    private readonly ChangeSignatureAnalysisSucceededContext _context = context;

    /// <summary>
    /// This code action currently pops up a confirmation dialog to the user.  As such, it does more than make
    /// document changes (and is thus restricted in which hosts it can run).
    /// </summary>
    public override ImmutableArray<string> Tags => RequiresNonDocumentChangeTags;

    public override string Title => FeaturesResources.Change_signature;

    public override object? GetOptions(CancellationToken cancellationToken)
        => AbstractChangeSignatureService.GetChangeSignatureOptions(_context);

    protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
        object options, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        if (options is ChangeSignatureOptionsResult changeSignatureOptions && changeSignatureOptions != null)
        {
            var changeSignatureResult = await _changeSignatureService.ChangeSignatureWithContextAsync(_context, changeSignatureOptions, cancellationToken).ConfigureAwait(false);

            if (changeSignatureResult.Succeeded)
                return [new ChangeSignatureCodeActionOperation(changeSignatureResult.UpdatedSolution, changeSignatureResult.ConfirmationMessage)];
        }

        return [];
    }
}
