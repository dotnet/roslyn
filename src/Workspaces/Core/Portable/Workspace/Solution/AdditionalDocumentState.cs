// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal sealed class AdditionalDocumentState : TextDocumentState
{
    private readonly AdditionalText _additionalText;

    private AdditionalDocumentState(
        SolutionServices solutionServices,
        IDocumentServiceProvider documentServiceProvider,
        DocumentInfo.DocumentAttributes attributes,
        ITextAndVersionSource textAndVersionSource,
        LoadTextOptions loadTextOptions)
        : base(solutionServices, documentServiceProvider, attributes, textAndVersionSource, loadTextOptions)
    {
        _additionalText = new AdditionalTextWithState(this);
    }

    public AdditionalDocumentState(
        SolutionServices solutionServices,
        DocumentInfo documentInfo,
        LoadTextOptions loadTextOptions)
        : base(solutionServices, documentInfo, loadTextOptions)
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

    protected override TextDocumentState UpdateText(ITextAndVersionSource newTextSource, PreservationMode mode, bool incremental)
    {
        return new AdditionalDocumentState(
            this.solutionServices,
            this.Services,
            this.Attributes,
            newTextSource,
            this.LoadTextOptions);
    }
}
