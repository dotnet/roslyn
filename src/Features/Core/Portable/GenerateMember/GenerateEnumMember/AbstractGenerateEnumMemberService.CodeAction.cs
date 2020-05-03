// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember
{
    internal abstract partial class AbstractGenerateEnumMemberService<TService, TSimpleNameSyntax, TExpressionSyntax>
    {
        private partial class GenerateEnumMemberCodeAction : CodeAction
        {
            private readonly Document _document;
            private readonly State _state;

            public GenerateEnumMemberCodeAction(
                Document document,
                State state)
            {
                _document = document;
                _state = state;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var languageServices = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
                var codeGenerator = languageServices.GetService<ICodeGenerationService>();
                var semanticFacts = languageServices.GetService<ISemanticFactsService>();

                var value = semanticFacts.LastEnumValueHasInitializer(_state.TypeToGenerateIn)
                    ? EnumValueUtilities.GetNextEnumValue(_state.TypeToGenerateIn)
                    : null;

                var result = await codeGenerator.AddFieldAsync(
                    _document.Project.Solution,
                    _state.TypeToGenerateIn,
                    CodeGenerationSymbolFactory.CreateFieldSymbol(
                        attributes: default,
                        accessibility: Accessibility.Public,
                        modifiers: default,
                        type: _state.TypeToGenerateIn,
                        name: _state.IdentifierToken.ValueText,
                        hasConstantValue: value != null,
                        constantValue: value),
                    new CodeGenerationOptions(contextLocation: _state.IdentifierToken.GetLocation()),
                    cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }

            public override string Title
            {
                get
                {
                    var text = FeaturesResources.Generate_enum_member_1_0;

                    return string.Format(
                        text,
                        _state.IdentifierToken.ValueText,
                        _state.TypeToGenerateIn.Name);
                }
            }
        }
    }
}
