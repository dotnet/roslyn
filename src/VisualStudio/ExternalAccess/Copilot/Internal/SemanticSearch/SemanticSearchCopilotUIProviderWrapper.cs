// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SemanticSearch;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.SemanticSearch;

[Export(typeof(ISemanticSearchCopilotUIProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SemanticSearchCopilotUIProviderWrapper(
    [Import(AllowDefault = true)] Lazy<ISemanticSearchCopilotUIProviderImpl>? impl) : ISemanticSearchCopilotUIProvider
{
    private sealed class TextBoxWrapper(ITextBoxControlImpl impl) : ITextBoxControl
    {
        Control ITextBoxControl.Control => impl.Control;
        string ITextBoxControl.Text { get => impl.Text; set => impl.Text = value; }
        IOleCommandTarget ITextBoxControl.CommandTarget => impl.CommandTarget;
        IWpfTextView ITextBoxControl.View => impl.View;
    }

    bool ISemanticSearchCopilotUIProvider.IsAvailable
        => impl != null;

    ITextBoxControl ISemanticSearchCopilotUIProvider.GetTextBox()
    {
        Contract.ThrowIfNull(impl);
        return new TextBoxWrapper(impl.Value.GetTextBox());
    }
}
