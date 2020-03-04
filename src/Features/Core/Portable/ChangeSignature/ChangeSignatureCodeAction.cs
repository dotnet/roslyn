﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal class ChangeSignatureCodeAction : CodeActionWithOptions
    {
        private readonly AbstractChangeSignatureService _changeSignatureService;
        private readonly ChangeSignatureAnalyzedContext _context;

        public ChangeSignatureCodeAction(AbstractChangeSignatureService changeSignatureService, ChangeSignatureAnalyzedContext context)
        {
            _changeSignatureService = changeSignatureService;
            _context = context;
        }

        public override string Title => FeaturesResources.Change_signature;

        public override object GetOptions(CancellationToken cancellationToken)
        {
            return _changeSignatureService.GetChangeSignatureOptions(_context);
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            if (options is ChangeSignatureOptionsResult changeSignatureOptions && !changeSignatureOptions.IsCancelled)
            {
                var changeSignatureResult = _changeSignatureService.ChangeSignatureWithContext(_context, changeSignatureOptions, cancellationToken);

                if (changeSignatureResult.Succeeded)
                {
                    return Task.FromResult<IEnumerable<CodeActionOperation>>(new CodeActionOperation[] { new ApplyChangesOperation(changeSignatureResult.UpdatedSolution) });
                }
            }

            return SpecializedTasks.EmptyEnumerable<CodeActionOperation>();
        }
    }
}
