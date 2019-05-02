// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.EncapsulateField
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.EncapsulateField), Shared]
    internal class EncapsulateFieldRefactoringProvider : CodeRefactoringProvider
    {
#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public EncapsulateFieldRefactoringProvider()
        {
        }

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var service = context.Document.GetLanguageService<AbstractEncapsulateFieldService>();
            var actions = await service.GetEncapsulateFieldCodeActionsAsync(context.Document, context.Span, context.CancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }
    }
}
