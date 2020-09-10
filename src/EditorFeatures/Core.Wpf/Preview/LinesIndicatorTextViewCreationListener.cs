// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(StandardContentTypeNames.Any)]
    [TextViewRole(TextViewRoles.PreviewRole)]
    internal sealed class LinesIndicatorTextViewCreationListener : IWpfTextViewCreationListener
    {
#pragma warning disable 649, 169
        [Export(typeof(AdornmentLayerDefinition))]
        [Name(nameof(LinesIndicator))]
        [Order(After = PredefinedAdornmentLayers.Text, Before = PredefinedAdornmentLayers.Caret)]
        private readonly AdornmentLayerDefinition? _editorAdornmentLayer;
#pragma warning restore 649, 169

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LinesIndicatorTextViewCreationListener(
                                                      [Import] ITextBufferFactoryService textBufferFactoryService,
                                                      [Import] IClassificationFormatMapService classificationFormatMapService,
                                                      [Import] IEditorFormatMapService editorFormatMapService,
                                                      [Import] IClassificationTypeRegistryService classificationTypeRegistryService)
        {
            this.TextBufferFactoryService = textBufferFactoryService;
            this.ClassificationFormatMapService = classificationFormatMapService;
            this.EditorFormatMapService = editorFormatMapService;
            this.ClassificationTypeRegistryService = classificationTypeRegistryService;
        }

        internal ITextBufferFactoryService TextBufferFactoryService { get; }

        internal IClassificationFormatMapService ClassificationFormatMapService { get; }

        internal IEditorFormatMapService EditorFormatMapService { get; }

        internal IClassificationTypeRegistryService ClassificationTypeRegistryService { get; }

        public void TextViewCreated(IWpfTextView textView)
        {
            if (textView.TextViewModel is IDifferenceTextViewModel model)
            {
                _ = new LinesIndicator(textView, model, this);
            }
        }
    }
}
