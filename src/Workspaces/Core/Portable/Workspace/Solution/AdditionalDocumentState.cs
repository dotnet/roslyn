// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class AdditionalDocumentState : TextDocumentState
    {
        private readonly AdditionalText _additionalText;

        private AdditionalDocumentState(
            HostWorkspaceServices solutionServices,
            IDocumentServiceProvider documentServiceProvider,
            DocumentInfo.DocumentAttributes attributes,
            SourceText? sourceText,
            ValueSource<TextAndVersion> textAndVersionSource)
            : base(solutionServices, documentServiceProvider, attributes, sourceText, textAndVersionSource)
        {
            _additionalText = new AdditionalTextWithState(this);
        }

        public AdditionalDocumentState(
            DocumentInfo documentInfo,
            HostWorkspaceServices solutionServices)
            : base(documentInfo, solutionServices)
        {
            _additionalText = new AdditionalTextWithState(this);
        }

        public AdditionalText AdditionalText => _additionalText;

        public new AdditionalDocumentState UpdateText(TextLoader loader, PreservationMode mode)
            => (AdditionalDocumentState)base.UpdateText(loader, mode);

        public new AdditionalDocumentState UpdateText(SourceText text, PreservationMode mode)
            => (AdditionalDocumentState)base.UpdateText(text, mode);

        public new AdditionalDocumentState UpdateText(TextAndVersion newTextAndVersion, PreservationMode mode)
            => (AdditionalDocumentState)base.UpdateText(newTextAndVersion, mode);

        protected override TextDocumentState UpdateText(ValueSource<TextAndVersion> newTextSource, PreservationMode mode, bool incremental)
        {
            return new AdditionalDocumentState(
                this.solutionServices,
                this.Services,
                this.Attributes,
                this.sourceText,
                newTextSource);
        }
    }
}
