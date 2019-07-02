// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            public GenerateConstructorCodeAction(
                TService service,
                Document document,
                State state,
                bool withFields)
            {
                _service = service;
                _document = document;
                _state = state;
                _withFields = withFields;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var (document, _) = await GetEditAsync(cancellationToken).ConfigureAwait(false);
                return document;
            }

            public async Task<(Document document, bool addedFields)> GetEditAsync(CancellationToken cancellationToken)
            {
                var semanticDocument = await SemanticDocument.CreateAsync(_document, cancellationToken).ConfigureAwait(false);
                var editor = new Editor(_service, semanticDocument, _state, _withFields, cancellationToken);
                return await editor.GetEditAsync().ConfigureAwait(false);
            }

            public override string Title
                => _withFields
                    ? string.Format(FeaturesResources.Generate_constructor_in_0, _state.TypeToGenerateIn.Name)
                    : string.Format(FeaturesResources.Generate_constructor_in_0_without_fields, _state.TypeToGenerateIn.Name);
        }
    }
}
