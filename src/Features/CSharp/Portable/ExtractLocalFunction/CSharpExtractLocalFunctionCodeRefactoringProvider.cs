// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractLocalFunction
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
        Name = PredefinedCodeRefactoringProviderNames.ExtractLocalFunction), Shared]
    internal class CSharpExtractLocalFunctionCodeRefactoringProvider : ExtractMethodCodeRefactoringProvider
    {
        [ImportingConstructor]
        public CSharpExtractLocalFunctionCodeRefactoringProvider()
        {
        }

        protected override async Task<CodeAction> GetCodeActionAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var result = await ExtractMethodService.ExtractMethodAsync(
                document,
                textSpan,
                localFunction: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(result);

            if (result.Succeeded || result.SucceededWithSuggestion)
            {
                // We insert an empty line between the generated local method and the previous statements if there is not one already.
                var codeAction = new MyCodeAction(FeaturesResources.Extract_local_function, c => AddRenameAnnotationAsync(result.Document, result.InvocationNameToken, c));

                return codeAction;
            }

            return default;
        }
    }
}
