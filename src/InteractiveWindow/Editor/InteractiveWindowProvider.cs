// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    [Export(typeof(IInteractiveWindowFactoryService))]
    internal class InteractiveWindowProvider : IInteractiveWindowFactoryService
    {
        private readonly IContentTypeRegistryService _contentTypeRegistry;
        private readonly ITextBufferFactoryService _bufferFactory;
        private readonly IProjectionBufferFactoryService _projectionBufferFactory;
        private readonly IEditorOperationsFactoryService _editorOperationsFactory;
        private readonly ITextEditorFactoryService _editorFactory;
        private readonly IIntellisenseSessionStackMapService _intellisenseSessionStackMap;
        private readonly ISmartIndentationService _smartIndenterService;
        private readonly IInteractiveWindowEditorFactoryService _windowFactoryService;

        [ImportingConstructor]
        public InteractiveWindowProvider(
            IContentTypeRegistryService contentTypeRegistry,
            ITextBufferFactoryService bufferFactory,
            IProjectionBufferFactoryService projectionBufferFactory,
            IEditorOperationsFactoryService editorOperationsFactory,
            ITextEditorFactoryService editorFactory,
            IIntellisenseSessionStackMapService intellisenseSessionStackMap,
            ISmartIndentationService smartIndenterService,
            IInteractiveWindowEditorFactoryService windowFactoryService)
        {
            _contentTypeRegistry = contentTypeRegistry;
            _bufferFactory = bufferFactory;
            _projectionBufferFactory = projectionBufferFactory;
            _editorOperationsFactory = editorOperationsFactory;
            _editorFactory = editorFactory;
            _intellisenseSessionStackMap = intellisenseSessionStackMap;
            _smartIndenterService = smartIndenterService;
            _windowFactoryService = windowFactoryService;
        }

        public IInteractiveWindow CreateWindow(IInteractiveEvaluator evaluator)
        {
            if (evaluator == null)
            {
                throw new ArgumentNullException(nameof(evaluator));
            }

            return new InteractiveWindow(
                _windowFactoryService,
                _contentTypeRegistry,
                _bufferFactory,
                _projectionBufferFactory,
                _editorOperationsFactory,
                _editorFactory,
                _intellisenseSessionStackMap,
                _smartIndenterService,
                evaluator);
        }
    }
}
