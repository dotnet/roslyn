// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.ChangeSignature), Shared]
    internal class ChangeSignatureCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        public ChangeSignatureCodeRefactoringProvider()
        {
        }

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            if (span.IsEmpty)
            {
                var service = document.GetLanguageService<AbstractChangeSignatureService>();
                var actions = await service.GetChangeSignatureCodeActionAsync(document, span, cancellationToken).ConfigureAwait(false);
                context.RegisterRefactorings(actions);
            }
        }
    }
}
