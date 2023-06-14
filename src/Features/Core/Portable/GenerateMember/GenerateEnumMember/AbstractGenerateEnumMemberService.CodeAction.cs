// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember
{
    internal abstract partial class AbstractGenerateEnumMemberService<TService, TSimpleNameSyntax, TExpressionSyntax>
    {
        private partial class GenerateEnumMemberCodeAction(
            Document document,
            State state,
            CodeAndImportGenerationOptionsProvider fallbackOptions) : CodeAction
        {
            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var languageServices = document.Project.Solution.Services.GetLanguageServices(state.TypeToGenerateIn.Language);
                var codeGenerator = languageServices.GetService<ICodeGenerationService>();
                var semanticFacts = languageServices.GetService<ISemanticFactsService>();

                var value = semanticFacts.LastEnumValueHasInitializer(state.TypeToGenerateIn)
                    ? EnumValueUtilities.GetNextEnumValue(state.TypeToGenerateIn)
                    : null;

                var result = await codeGenerator.AddFieldAsync(
                    new CodeGenerationSolutionContext(
                        document.Project.Solution,
                        new CodeGenerationContext(
                            contextLocation: state.IdentifierToken.GetLocation()),
                        fallbackOptions),
                    state.TypeToGenerateIn,
                    CodeGenerationSymbolFactory.CreateFieldSymbol(
                        attributes: default,
                        accessibility: Accessibility.Public,
                        modifiers: default,
                        type: state.TypeToGenerateIn,
                        name: state.IdentifierToken.ValueText,
                        hasConstantValue: value != null,
                        constantValue: value),
                    cancellationToken).ConfigureAwait(false);

                return result;
            }

            public override string Title
            {
                get
                {
                    return string.Format(
                        FeaturesResources.Generate_enum_member_0, state.IdentifierToken.ValueText);
                }
            }
        }
    }
}
