// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;

internal interface ITextBoxControlImpl
{
    Control Control { get; }
    string Text { get; set; }
    IOleCommandTarget CommandTarget { get; }
    IWpfTextView View { get; }
}

internal interface ISemanticSearchCopilotUIProviderImpl
{
    ITextBoxControlImpl GetTextBox();
}
