// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.MoveTypeToFile), Shared]
    internal class MoveTypeCodeRefactoringProvider : CodeRefactoringProvider
    {
#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public MoveTypeCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            var workspace = document.Project.Solution.Workspace;
            if (workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            if (document.IsGeneratedCode(cancellationToken))
            {
                return;
            }

            var service = document.GetLanguageService<IMoveTypeService>();
            var actions = await service.GetRefactoringAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }
    }
}
