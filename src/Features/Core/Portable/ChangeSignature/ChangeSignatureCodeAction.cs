﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal class ChangeSignatureCodeAction : CodeActionWithOptions
    {
        private readonly AbstractChangeSignatureService _changeSignatureService;
        private readonly ChangeSignatureAnalysisSucceededContext _context;

        public ChangeSignatureCodeAction(AbstractChangeSignatureService changeSignatureService, ChangeSignatureAnalysisSucceededContext context)
        {
            _changeSignatureService = changeSignatureService;
            _context = context;
        }

        public override string Title => FeaturesResources.Change_signature;

        public override object? GetOptions(CancellationToken cancellationToken)
            => AbstractChangeSignatureService.GetChangeSignatureOptions(_context);

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            if (options is ChangeSignatureOptionsResult changeSignatureOptions && changeSignatureOptions != null)
            {
                var changeSignatureResult = await _changeSignatureService.ChangeSignatureWithContextAsync(_context, changeSignatureOptions, cancellationToken).ConfigureAwait(false);

                if (changeSignatureResult.Succeeded)
                {
                    return SpecializedCollections.SingletonEnumerable<CodeActionOperation>(new ChangeSignatureCodeActionOperation(changeSignatureResult.UpdatedSolution, changeSignatureResult.ConfirmationMessage));
                }
            }

            return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
        }
    }
}
