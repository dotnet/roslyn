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
        private AbstractChangeSignatureService changeSignatureService;
        private ChangeSignatureAnalyzedContext context;

        public ChangeSignatureCodeAction(AbstractChangeSignatureService changeSignatureService, ChangeSignatureAnalyzedContext context)
        {
            this.changeSignatureService = changeSignatureService;
            this.context = context;
        }

        public override string Title
        {
            get { return FeaturesResources.ChangeSignature; }
        }

        public override object GetOptions(CancellationToken cancellationToken)
        {
            return changeSignatureService.GetChangeSignatureOptions(context, cancellationToken);
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            var changeSignatureOptions = options as ChangeSignatureOptionsResult;
            if (changeSignatureOptions != null && !changeSignatureOptions.IsCancelled)
            {
                var changeSignatureResult = changeSignatureService.ChangeSignatureWithContext(context, changeSignatureOptions, cancellationToken);

                if (changeSignatureResult.Succeeded)
                {
                    return Task.FromResult<IEnumerable<CodeActionOperation>>(new CodeActionOperation[] { new ApplyChangesOperation(changeSignatureResult.UpdatedSolution) });
                }
            }

            return SpecializedTasks.EmptyEnumerable<CodeActionOperation>();
        }
    }
}
