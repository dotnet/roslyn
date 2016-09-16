// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal abstract class SignatureHelpProvider
    {
        internal string Name { get; }

        protected SignatureHelpProvider()
        {
            this.Name = this.GetType().Name;
        }

        public virtual bool IsTriggerCharacter(char ch)
        {
            return false;
        }

        public virtual bool IsRetriggerCharacter(char ch)
        {
            return false;
        }

        public virtual Task ProvideSignaturesAsync(SignatureContext context)
        {
            return Task.FromResult(false);
        }

        public virtual Task<ImmutableArray<TaggedText>> GetItemDocumentationAsync(Document document, SignatureHelpItem item, CancellationToken cancellationToken)
        {
            return SignatureHelpService.EmptyTextTask;
        }

        public virtual Task<ImmutableArray<TaggedText>> GetParameterDocumentationAsync(Document document, SignatureHelpParameter parameter, CancellationToken cancellationToken)
        {
            return SignatureHelpService.EmptyTextTask;
        }
    }
}
