// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateOverrides
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.GenerateOverrides), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers)]
    internal partial class GenerateOverridesCodeRefactoringProvider : CodeRefactoringProvider
    {
        private readonly IPickMembersService _pickMembersService_forTestingPurposes;

        [ImportingConstructor]
        public GenerateOverridesCodeRefactoringProvider() : this(null)
        {
        }

        public GenerateOverridesCodeRefactoringProvider(IPickMembersService pickMembersService)
        {
            _pickMembersService_forTestingPurposes = pickMembersService;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // We offer the refactoring when the user is either on the header of a class/struct,
            // or if they're between any members of a class/struct and are on a blank line.
            if (!syntaxFacts.IsOnTypeHeader(root, textSpan.Start, out _) &&
                !syntaxFacts.IsBetweenTypeMembers(sourceText, root, textSpan.Start))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Only supported on classes/structs.
            var containingType = AbstractGenerateFromMembersCodeRefactoringProvider.GetEnclosingNamedType(
                semanticModel, root, textSpan.Start);

            var overridableMembers = containingType.GetOverridableMembers(cancellationToken);
            if (overridableMembers.Length == 0)
            {
                return;
            }

            context.RegisterRefactoring(
                new GenerateOverridesWithDialogCodeAction(
                    this, document, textSpan, containingType, overridableMembers),
                textSpan);
        }
    }
}
