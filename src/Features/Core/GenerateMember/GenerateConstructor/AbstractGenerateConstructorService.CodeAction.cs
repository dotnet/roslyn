// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal abstract partial class AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax>
    {
        private class GenerateConstructorCodeAction : CodeAction
        {
            private readonly State _state;
            private readonly Document _document;
            private readonly TService _service;

            public GenerateConstructorCodeAction(
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
                var semanticDocument = await SemanticDocument.CreateAsync(_document, cancellationToken).ConfigureAwait(false);
                var editor = new Editor(_service, semanticDocument, _state, cancellationToken);
                return await editor.GetEditAsync().ConfigureAwait(false);
            }

            public override string Title
            {
                get
                {
                    return string.Format(FeaturesResources.GenerateNewConstructorIn,
                        _state.TypeToGenerateIn.Name);
                }
            }
        }
    }
}
