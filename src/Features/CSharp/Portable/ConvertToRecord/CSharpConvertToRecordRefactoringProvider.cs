// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRecord
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToRecord), Shared]
    internal sealed class CSharpConvertToRecordRefactoringProvider : CodeRefactoringProvider
    {

        private IGlobalOptionService _globalOptionService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConvertToRecordRefactoringProvider(IGlobalOptionService globalOptionService)
        {
            _globalOptionService = globalOptionService;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            if (_globalOptionService.GetOption(ConvertToRecordOptions.Disable))
            {
                return null;
            }

            var (document, _, cancellationToken) = context;

            var typeDeclaration = await context.TryGetRelevantNodeAsync<TypeDeclarationSyntax>().ConfigureAwait(false);
            if (typeDeclaration == null)
            {
                return;
            }

            var action = await ConvertToRecordEngine
                .GetCodeActionAsync(document, typeDeclaration, cancellationToken).ConfigureAwait(false);
            if (action != null)
            {
                context.RegisterRefactoring(action);
            }
        }
    }
}
