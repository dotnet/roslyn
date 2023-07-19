// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateOverrides
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.GenerateOverrides), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers)]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
    internal partial class GenerateOverridesCodeRefactoringProvider(IPickMembersService? pickMembersService) : CodeRefactoringProvider
    {
        private readonly IPickMembersService? _pickMembersService_forTestingPurposes = pickMembersService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GenerateOverridesCodeRefactoringProvider() : this(null)
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            var helpers = document.GetRequiredLanguageService<IRefactoringHelpersService>();
            var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // We offer the refactoring when the user is either on the header of a class/struct,
            // or if they're between any members of a class/struct and are on a blank line.
            if (!helpers.IsOnTypeHeader(root, textSpan.Start, out var typeDeclaration) &&
                !helpers.IsBetweenTypeMembers(sourceText, root, textSpan.Start, out typeDeclaration))
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Only supported on classes/structs.
            var containingType = (INamedTypeSymbol)semanticModel.GetRequiredDeclaredSymbol(typeDeclaration, cancellationToken);

            var overridableMembers = containingType.GetOverridableMembers(cancellationToken);
            if (overridableMembers.Length == 0)
            {
                return;
            }

            context.RegisterRefactoring(
                new GenerateOverridesWithDialogCodeAction(
                    this, document, textSpan, containingType, overridableMembers, context.Options),
                typeDeclaration.Span);
        }
    }
}
