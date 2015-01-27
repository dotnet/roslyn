// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember
{
    internal abstract partial class AbstractGenerateEnumMemberService<TService, TSimpleNameSyntax, TExpressionSyntax>
    {
        private partial class GenerateEnumMemberCodeAction : CodeAction
        {
            private readonly TService service;
            private readonly Document document;
            private readonly State state;

            public GenerateEnumMemberCodeAction(
                TService service,
                Document document,
                State state)
            {
                this.service = service;
                this.document = document;
                this.state = state;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var languageServices = this.document.Project.Solution.Workspace.Services.GetLanguageServices(state.TypeToGenerateIn.Language);
                var codeGenerator = languageServices.GetService<ICodeGenerationService>();
                var semanticFacts = languageServices.GetService<ISemanticFactsService>();

                var value = semanticFacts.LastEnumValueHasInitializer(state.TypeToGenerateIn)
                    ? EnumValueUtilities.GetNextEnumValue(state.TypeToGenerateIn, cancellationToken)
                    : null;

                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var result = await codeGenerator.AddFieldAsync(
                    document.Project.Solution,
                    state.TypeToGenerateIn,
                    CodeGenerationSymbolFactory.CreateFieldSymbol(
                        attributes: null,
                        accessibility: Accessibility.Public,
                        modifiers: default(DeclarationModifiers),
                        type: state.TypeToGenerateIn,
                        name: state.IdentifierToken.ValueText,
                        hasConstantValue: value != null,
                        constantValue: value),
                    new CodeGenerationOptions(contextLocation: state.IdentifierToken.GetLocation()),
                    cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }

            public override string Title
            {
                get
                {
                    var text = FeaturesResources.GenerateEnumMemberIn;

                    return string.Format(
                        text,
                        state.IdentifierToken.ValueText,
                        state.TypeToGenerateIn.Name);
                }
            }
        }
    }
}
