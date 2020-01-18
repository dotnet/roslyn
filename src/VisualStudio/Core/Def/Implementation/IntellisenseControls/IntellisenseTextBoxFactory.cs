// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Composition;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls
{
    [Export(typeof(IntellisenseTextBoxFactory)), Shared]
    internal class IntellisenseTextBoxFactory
    {
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly IVsEditorAdaptersFactoryService _editorAdapterFactory;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IntellisenseTextBoxFactory(
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IClassificationFormatMapService classificationFormatMapService,
            IVsEditorAdaptersFactoryService editorAdapterFactory)
        {
            _editorOperationsFactoryService = editorOperationsFactoryService;

            // Set the font used in the editor
            // The editor will automatically choose the font, color for the text
            // depending on the current language. Override the font information
            // to have uniform look and feel in the parallel watch window
            _classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(IntellisenseTextBox.AppearanceCategory);

            _editorAdapterFactory = editorAdapterFactory;
        }

        public IntellisenseTextBox Create(IntellisenseTextBoxViewModel viewModel, ContentControl container)
            => new IntellisenseTextBox(_editorOperationsFactoryService, _classificationFormatMap, _editorAdapterFactory, viewModel, container);
    }
}
