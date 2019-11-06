// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractLocalMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
        Name = PredefinedCodeRefactoringProviderNames.ExtractLocalMethod), Shared]
    internal class CSharpExtractLocalMethodCodeRefactoringProvider : ExtractMethodCodeRefactoringProvider
    {
        [ImportingConstructor]
        public CSharpExtractLocalMethodCodeRefactoringProvider()
        {
        }

        internal override async Task<(CodeAction action, string methodBlock)> GetCodeActionAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var preferStatic = documentOptions.GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction).Value;

            var result = await ExtractMethodService.ExtractMethodAsync(
                document,
                textSpan,
                extractLocalMethod: true,
                preferStatic: preferStatic,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(result);

            if (result.Succeeded || result.SucceededWithSuggestion)
            {
                var description = documentOptions.GetOption(ExtractMethodOptions.AllowMovingDeclaration) ?
                                      FeaturesResources.Extract_Local_Method_plus_Local : FeaturesResources.Extract_Local_Method;

                var codeAction = new MyCodeAction(description, c => AddRenameAnnotationAsync(result.Document, result.InvocationNameToken, c));
                var methodBlock = result.MethodDeclarationNode;

                return (codeAction, methodBlock.ToString());
            }

            return default;
        }
    }
}
