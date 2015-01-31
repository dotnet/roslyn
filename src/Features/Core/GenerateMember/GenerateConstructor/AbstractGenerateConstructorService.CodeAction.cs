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
            private readonly State state;
            private readonly Document document;
            private readonly TService service;

            public GenerateConstructorCodeAction(
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
                var semanticDocument = await SemanticDocument.CreateAsync(this.document, cancellationToken).ConfigureAwait(false);
                var editor = new Editor(service, semanticDocument, state, cancellationToken);
                return await editor.GetEditAsync().ConfigureAwait(false);
            }

            public override string Title
            {
                get
                {
                    return string.Format(FeaturesResources.GenerateNewConstructorIn,
                        state.TypeToGenerateIn.Name);
                }
            }
        }
    }
}
