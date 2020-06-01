// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal abstract partial class AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax>
    {
        private class GenerateConstructorCodeAction : CodeAction
        {
            private readonly TService _service;
            private readonly Document _document;
            private readonly State _state;
            private readonly bool _withFields;
            private readonly bool _withProperties;

            public GenerateConstructorCodeAction(
                TService service,
                Document document,
                State state,
                bool withFields,
                bool withProperties)
            {
                _service = service;
                _document = document;
                _state = state;
                _withFields = withFields;
                _withProperties = withProperties;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var semanticDocument = await SemanticDocument.CreateAsync(_document, cancellationToken).ConfigureAwait(false);
                var editor = new Editor(_service, semanticDocument, _state, _withFields, _withProperties, cancellationToken);
                return await editor.GetEditAsync().ConfigureAwait(false);
            }

            public override string Title
                => _withFields ? string.Format(FeaturesResources.Generate_constructor_in_0_with_fields, _state.TypeToGenerateIn.Name) :
                   _withProperties ? string.Format(FeaturesResources.Generate_constructor_in_0_with_properties, _state.TypeToGenerateIn.Name) :
                   _state.AddingMembers ? string.Format(FeaturesResources.Generate_constructor_in_0_without_members, _state.TypeToGenerateIn.Name) :
                                          string.Format(FeaturesResources.Generate_constructor_in_0, _state.TypeToGenerateIn.Name);
            public override string EquivalenceKey => Title;
        }
    }
}
