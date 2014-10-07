using System;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Threading;
using Roslyn.Services.Host;
using Roslyn.Services.LanguageServices;
using Roslyn.Services.WorkspaceServices;

namespace Roslyn.Services
{
    internal static partial class DefaultServices
    {
        private static ILanguageServiceProviderFactory defaultLanguageServicesFactory;

        public static IWorkspaceServiceProviderFactory WorkspaceServicesFactory
        {
            get
            {
                var primaryWorkspace = Workspace.PrimaryWorkspace;
                if (primaryWorkspace != null)
                {
                    return primaryWorkspace.WorkspaceServices.Factory;
                }
                else
                {
                    return DefaultComposition.Composition.GetExportedValue<IWorkspaceServiceProviderFactory>();
                }
            }
        }

        public static ILanguageServiceProviderFactory LanguageServicesFactory
        {
            get
            {
                var primaryWorkspace = Workspace.PrimaryWorkspace;
                if (primaryWorkspace != null)
                {
                    return primaryWorkspace.WorkspaceServices.GetService<ILanguageServiceProviderFactory>();
                }
                else
                {
                    if (defaultLanguageServicesFactory == null)
                    {
                        System.Threading.Interlocked.CompareExchange(ref defaultLanguageServicesFactory, WorkspaceServicesFactory.CreateWorkspaceServiceProvider(WorkspaceKind.Host).GetService<ILanguageServiceProviderFactory>(), null);
                    }

                    return defaultLanguageServicesFactory;
                }
            }
        }

        internal static AggregateCatalog GetDefaultCatalogWithTestOverrides(ComposablePartCatalog catalog)
        {
            return new AggregateCatalog(catalog, DefaultComposition.Composition.Catalog);
        }
    }
}