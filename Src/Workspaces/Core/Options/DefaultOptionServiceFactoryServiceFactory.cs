using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Services.Host;

namespace Roslyn.Services.OptionService
{
    [ExportWorkspaceServiceFactory(typeof(IOptionServiceFactoryService), WorkspaceKind.Any)]
    internal sealed class DefaultOptionServiceFactoryServiceFactory : IWorkspaceServiceFactory
    {
        // TODO: remove this once MEF is removed from the option service
        private readonly IOptionService optionService;

        private readonly IEnumerable<Lazy<IOptionMigrator, IOptionMigratorMetadata>> migrators;
        private readonly IEnumerable<Lazy<IOptionProvider, IOptionProviderMetadata>> options;

        [ImportingConstructor]
        public DefaultOptionServiceFactoryServiceFactory(
            IOptionService optionService,
            [ImportMany]IEnumerable<Lazy<IOptionMigrator, IOptionMigratorMetadata>> migrators,
            [ImportMany]IEnumerable<Lazy<IOptionProvider, IOptionProviderMetadata>> options)
        {
            this.migrators = migrators;
            this.options = options;

            this.optionService = optionService;
        }

        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new Factory(this.optionService, this.migrators, this.options);
        }

        private sealed class Factory : IOptionServiceFactoryService
        {
            private readonly IOptionService defaultOptionService;

            public Factory(
                IOptionService optionService,
                IEnumerable<Lazy<IOptionMigrator, IOptionMigratorMetadata>> migrators,
                IEnumerable<Lazy<IOptionProvider, IOptionProviderMetadata>> options)
            {
                // this.defaultOptionService = new DefaultOptionService(migrators, options);
                this.defaultOptionService = optionService;
            }

            public IOptionService GetOptionService(IDocument document)
            {
                // for now, there is no option scoped to document.
                return this.defaultOptionService;
            }
        }
    }
}
