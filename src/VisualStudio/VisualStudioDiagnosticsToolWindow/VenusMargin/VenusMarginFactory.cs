// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Roslyn.Hosting.Diagnostics.VenusMargin
{
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
        public MarginFactory(ITextEditorFactoryService textEditorFactory)
        {
            _textEditorFactory = textEditorFactory;
        }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            if (!(textViewHost.TextView.TextBuffer is IProjectionBuffer))
            {
                return null;
            }

            return new VenusMargin(textViewHost.TextView, _textEditorFactory);
        }
    }
}
