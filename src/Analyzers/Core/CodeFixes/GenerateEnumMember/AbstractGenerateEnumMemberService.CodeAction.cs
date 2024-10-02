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
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember;

internal abstract partial class AbstractGenerateEnumMemberService<TService, TSimpleNameSyntax, TExpressionSyntax>
{
    private sealed partial class GenerateEnumMemberCodeAction(Document document, State state) : CodeAction
    {
        private readonly Document _document = document;
        private readonly State _state = state;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var languageServices = _document.Project.Solution.Workspace.Services.GetExtendedLanguageServices(_state.TypeToGenerateIn.Language);
            var codeGenerator = languageServices.GetService<ICodeGenerationService>();
            var semanticFacts = languageServices.GetService<ISemanticFactsService>();

            var value = semanticFacts.LastEnumValueHasInitializer(_state.TypeToGenerateIn)
                ? EnumValueUtilities.GetNextEnumValue(_state.TypeToGenerateIn)
                : null;

            var result = await codeGenerator.AddFieldAsync(
                new CodeGenerationSolutionContext(
                    _document.Project.Solution,
                    new CodeGenerationContext(
                        contextLocation: _state.IdentifierToken.GetLocation())),
                _state.TypeToGenerateIn,
                CodeGenerationSymbolFactory.CreateFieldSymbol(
                    attributes: default,
                    accessibility: Accessibility.Public,
                    modifiers: default,
                    type: _state.TypeToGenerateIn,
                    name: _state.IdentifierToken.ValueText,
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
                    CodeFixesResources.Generate_enum_member_0, _state.IdentifierToken.ValueText);
            }
        }
    }
}
