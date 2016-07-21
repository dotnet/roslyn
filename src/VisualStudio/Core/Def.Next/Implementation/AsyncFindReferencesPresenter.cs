using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [Export(typeof(IAsyncFindReferencesPresenter)), Shared]
    internal partial class AsyncFindReferencesPresenter :
        ForegroundThreadAffinitizedObject, IAsyncFindReferencesPresenter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAsynchronousOperationListener _asyncListener;

        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;

        private readonly ClassificationTypeMap _typeMap;
        private readonly IFindAllReferencesService _vsFindAllReferencesService;

        [ImportingConstructor]
        public AsyncFindReferencesPresenter(
            Shell.SVsServiceProvider serviceProvider,
            ITextBufferFactoryService textBufferFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            ClassificationTypeMap typeMap,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _serviceProvider = serviceProvider;
            _textBufferFactoryService = textBufferFactoryService;
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
            _typeMap = typeMap;
            _asyncListener = new AggregateAsynchronousOperationListener(
                asyncListeners, FeatureAttribute.ReferenceHighlighting);

            _vsFindAllReferencesService = (IFindAllReferencesService)_serviceProvider.GetService(typeof(SVsFindAllReferences));
        }

        public FindReferencesContext StartSearch()
        {
            this.AssertIsForeground();

            // Get the appropriate window for FAR results to go into.
            var window = _vsFindAllReferencesService.StartSearch("");

            // Make the data source that will feed data into this window.
            var dataSource = new TableDataSourceFindReferencesContext(this, window);

            // And return the data source so that the FindRefs engine can report results
            // which the data source can then create the appropriate presentation items for
            // for the window.
            return dataSource;
        }
    }
}