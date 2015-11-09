using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    [Export]
    internal sealed class MefHostServicesFactoryService : IMefHostExportProvider
    {
        private readonly IEnumerable<Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>> _workspaceServiceFactories;
        private readonly IEnumerable<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> _workspaceServices;
        private readonly IEnumerable<Lazy<ILanguageServiceFactory, LanguageServiceMetadata>> _languageServiceFactories;
        private readonly IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> _languageServices;
        private readonly IEnumerable<Lazy<ILanguageService, ContentTypeLanguageMetadata>> _contentTypeLanguageMetadata;

        [ImportingConstructor]
        public MefHostServicesFactoryService(
            [ImportMany] IEnumerable<Lazy<IWorkspaceServiceFactory, WorkspaceServiceMetadata>> workspaceServiceFactories,
            [ImportMany] IEnumerable<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> workspaceServices,
            [ImportMany] IEnumerable<Lazy<ILanguageServiceFactory, LanguageServiceMetadata>> languageServiceFactories,
            [ImportMany] IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> languageServices,
            [ImportMany] IEnumerable<Lazy<ILanguageService, ContentTypeLanguageMetadata>> contentTypeLanguageMetadata)
        {
            _workspaceServiceFactories = workspaceServiceFactories;
            _workspaceServices = workspaceServices;
            _languageServiceFactories = languageServiceFactories;
            _languageServices = languageServices;
            _contentTypeLanguageMetadata = contentTypeLanguageMetadata;
        }

        public IEnumerable<Lazy<TExtension, TMetadata>> GetExports<TExtension, TMetadata>()
        {
            if (typeof(TExtension) == typeof(IWorkspaceServiceFactory) && typeof(TMetadata) == typeof(WorkspaceServiceMetadata))
            {
                return (IEnumerable<Lazy<TExtension, TMetadata>>)_workspaceServiceFactories;
            }
            else if (typeof(TExtension) == typeof(IWorkspaceService) && typeof(TMetadata) == typeof(WorkspaceServiceMetadata))
            {
                return (IEnumerable<Lazy<TExtension, TMetadata>>)_workspaceServices;
            }
            else if (typeof(TExtension) == typeof(ILanguageServiceFactory) && typeof(TMetadata) == typeof(LanguageServiceMetadata))
            {
                return (IEnumerable<Lazy<TExtension, TMetadata>>)_languageServiceFactories;
            }
            else if (typeof(TExtension) == typeof(ILanguageService) && typeof(TMetadata) == typeof(LanguageServiceMetadata))
            {
                return (IEnumerable<Lazy<TExtension, TMetadata>>)_languageServices;
            }
            else if (typeof(TExtension) == typeof(ILanguageService) && typeof(TMetadata) == typeof(ContentTypeLanguageMetadata))
            {
                return (IEnumerable<Lazy<TExtension, TMetadata>>)_contentTypeLanguageMetadata;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public HostServices CreateHostServices()
        {
            return new MefHostServices(this);
        }

        private class MefHostServices : HostServices
        {
            private readonly IMefHostExportProvider _mefHostExportProvider;

            public MefHostServices(IMefHostExportProvider mefHostExportProvider)
            {
                _mefHostExportProvider = mefHostExportProvider;
            }

            protected internal override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
            {
                return new MefWorkspaceServices(_mefHostExportProvider, workspace);
            }
        }
    }
}
