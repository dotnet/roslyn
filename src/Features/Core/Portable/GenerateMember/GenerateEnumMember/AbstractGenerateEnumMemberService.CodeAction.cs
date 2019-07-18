// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly TService _service;
            private readonly Document _document;
            private readonly State _state;

            public GenerateEnumMemberCodeAction(
                TService service,
                Document document,
                State state)
            {
                _service = service;
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

                var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
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
