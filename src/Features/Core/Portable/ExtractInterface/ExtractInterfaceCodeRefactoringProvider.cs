// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.ExtractInterface), Shared]
    internal class ExtractInterfaceCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExtractInterfaceCodeRefactoringProvider()
        {
        }

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            var service = document.GetLanguageService<AbstractExtractInterfaceService>();
            var actions = await service.GetExtractInterfaceCodeActionAsync(document, textSpan, context.Options, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }
    }
}
