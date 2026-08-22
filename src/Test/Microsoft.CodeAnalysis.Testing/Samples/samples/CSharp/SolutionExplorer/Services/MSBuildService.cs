using System;
using System.Linq;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;

namespace MSBuildWorkspaceTester.Services
{
    internal class MSBuildService : BaseService
    {
        public MSBuildService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public void Initialize()
        {
            VisualStudioInstance[] instances = GetVisualStudioInstances();
            if (instances.Length == 0)
            {
                return;
            }

            VisualStudioInstance instance = instances[0];
            RegisterVisualStudioInstance(instance);
        }

        private VisualStudioInstance[] GetVisualStudioInstances()
        {
            VisualStudioInstance[] instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            if (instances.Length == 0)
            {
                Logger.LogError("No MSBuild instances found.");
                return Array.Empty<VisualStudioInstance>();
            }

            Logger.LogInformation("The following MSBuild instances have been discovered:");
            Logger.LogInformation(string.Empty);

            for (int i = 0; i < instances.Length; i++)
            {
                VisualStudioInstance instance = instances[i];
                Logger.LogInformation($"    {i + 1}. {instance.Name} ({instance.Version})");
            }

            Logger.LogInformation(string.Empty);

            return instances;
        }

        private void RegisterVisualStudioInstance(VisualStudioInstance instance)
        {
            MSBuildLocator.RegisterInstance(instance);

            Logger.LogInformation("Registered first MSBuild instance:");
            Logger.LogInformation(string.Empty);
            Logger.LogInformation($"    Name: {instance.Name}");
            Logger.LogInformation($"    Version: {instance.Version}");
            Logger.LogInformation($"    VisualStudioRootPath: {instance.VisualStudioRootPath}");
            Logger.LogInformation($"    MSBuildPath: {instance.MSBuildPath}");
            Logger.LogInformation(string.Empty);
        }
    }
}
