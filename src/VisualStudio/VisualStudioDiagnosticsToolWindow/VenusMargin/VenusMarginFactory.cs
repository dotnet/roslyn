// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Roslyn.Hosting.Diagnostics.VenusMargin;

[Export(typeof(IWpfTextViewMarginProvider))]
[Name(VenusMargin.MarginName)]
[Order(After = PredefinedMarginNames.BottomControl)]
[Order(After = "TagNavigatorMargin")]
[MarginContainer(PredefinedMarginNames.Bottom)]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
internal sealed class MarginFactory : IWpfTextViewMarginProvider
{
    private readonly ITextEditorFactoryService _textEditorFactory;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public MarginFactory(ITextEditorFactoryService textEditorFactory)
    {
        _textEditorFactory = textEditorFactory;
    }

    public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
    {
        if (textViewHost.TextView.TextBuffer is not IProjectionBuffer)
        {
            return null;
        }

        return new VenusMargin(textViewHost.TextView, _textEditorFactory);
    }
}
