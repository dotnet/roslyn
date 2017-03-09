// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
{
    internal class TestSignatureHelpService : SignatureHelpService
    {
        private readonly SignatureHelpProvider _provider;

        public TestSignatureHelpService(SignatureHelpProvider provider)
        {
            _provider = provider;
        }

        public override async Task<SignatureList> GetSignaturesAsync(Document document, int caretPosition, SignatureHelpTrigger trigger = default(SignatureHelpTrigger), OptionSet options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var context = new SignatureContext(_provider, document, caretPosition, trigger, options ?? document.Project.Solution.Workspace.Options, cancellationToken);
            await _provider.ProvideSignaturesAsync(context).ConfigureAwait(false);
            return context.ToSignatureList();
        }

        public override Task<ImmutableArray<TaggedText>> GetItemDocumentationAsync(Document document, SignatureHelpItem item, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _provider.GetItemDocumentationAsync(document, item, cancellationToken);
        }

        public override Task<ImmutableArray<TaggedText>> GetParameterDocumentationAsync(Document document, SignatureHelpParameter parameter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _provider.GetParameterDocumentationAsync(document, parameter, cancellationToken);
        }
    }
}
