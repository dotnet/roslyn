// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            if (options is ChangeSignatureOptionsResult changeSignatureOptions && !changeSignatureOptions.IsCancelled)
            {
                var changeSignatureResult = await _changeSignatureService.ChangeSignatureWithContextAsync(_context, changeSignatureOptions, cancellationToken).ConfigureAwait(false);

                if (changeSignatureResult.Succeeded)
                {
                    return new CodeActionOperation[] { new ApplyChangesOperation(changeSignatureResult.UpdatedSolution) };
                }
            }

            return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
        }
    }
}
