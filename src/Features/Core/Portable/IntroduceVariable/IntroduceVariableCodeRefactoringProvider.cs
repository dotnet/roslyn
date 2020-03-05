﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.ConvertTupleToStruct)]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.ConvertAnonymousTypeToClass)]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.InvertConditional)]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.InvertLogical)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.IntroduceVariable), Shared]
    internal class IntroduceVariableCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        public IntroduceVariableCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var service = document.GetLanguageService<IIntroduceVariableService>();
            var action = await service.IntroduceVariableAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (action != null)
            {
                context.RegisterRefactoring(action, textSpan);
            }
        }
    }
}
