using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences
{
    [Export(typeof(IAsyncFindReferencesPresenter))]
    internal sealed partial class AsyncFindReferencesPresenter :
        ForegroundThreadAffinitizedObject, IAsyncFindReferencesPresenter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITableManagerProvider _tableManagerProvider;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        public AsyncFindReferencesPresenter(
            Shell.SVsServiceProvider serviceProvider,
            ITableManagerProvider tableManagerProvider,
            ClassificationTypeMap typeMap,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _serviceProvider = serviceProvider;
            _tableManagerProvider = tableManagerProvider;
            _typeMap = typeMap;
            _asyncListener = new AggregateAsynchronousOperationListener(
                asyncListeners, FeatureAttribute.ReferenceHighlighting);
        }

        public FindReferencesContext StartSearch()
        {
            this.AssertIsForeground();

            var manager = _tableManagerProvider.GetTableManager("FindAllReferences");
            if (manager == null)
            {
                return null;
            }

            var vsuiShell = (IVsUIShell)_serviceProvider.GetService(typeof(SVsUIShell));

            /// NOTE(cyrusn): This hardcoded guid value is only needed as we prototype the new 
            /// FindAllRefs experience. Once the new experience is a property part of the Dev15 
            /// API, then we will not need this and we can refer to a well known ID properly.
            IVsWindowFrame window;
            vsuiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, new Guid("a80febb4-e7e0-4147-b476-21aaf2453969"), out window);
            if (window != null)
            {
                window.Show();
            }

            foreach (var source in manager.Sources.ToArray())
            {
                manager.RemoveSource(source);
            }

            var dataSource = new DataSource(_typeMap, _asyncListener);
            manager.AddSource(dataSource);

            return dataSource;
        }
    }
}
