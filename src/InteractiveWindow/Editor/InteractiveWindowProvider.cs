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
        private readonly IContentTypeRegistryService contentTypeRegistry;
        private readonly ITextBufferFactoryService bufferFactory;
        private readonly IProjectionBufferFactoryService projectionBufferFactory;
        private readonly IEditorOperationsFactoryService editorOperationsFactory;
        private readonly ITextEditorFactoryService editorFactory;
        private readonly IIntellisenseSessionStackMapService intellisenseSessionStackMap;
        private readonly ISmartIndentationService smartIndenterService;
        private readonly IInteractiveWindowEditorFactoryService windowFactoryService;

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
            this.contentTypeRegistry = contentTypeRegistry;
            this.bufferFactory = bufferFactory;
            this.projectionBufferFactory = projectionBufferFactory;
            this.editorOperationsFactory = editorOperationsFactory;
            this.editorFactory = editorFactory;
            this.intellisenseSessionStackMap = intellisenseSessionStackMap;
            this.smartIndenterService = smartIndenterService;
            this.windowFactoryService = windowFactoryService;
        }

        public IInteractiveWindow CreateWindow(IInteractiveEvaluator evaluator)
        {
            if (evaluator == null)
            {
                throw new ArgumentNullException(nameof(evaluator));
            }

            return new InteractiveWindow(
                windowFactoryService,
                contentTypeRegistry,
                bufferFactory,
                projectionBufferFactory,
                editorOperationsFactory,
                editorFactory,
                intellisenseSessionStackMap,
                smartIndenterService, 
                evaluator);
        }
    }
}
