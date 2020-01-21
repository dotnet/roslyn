#if NET472
using System.Collections.Generic;
using Roslyn.Test.Utilities;

namespace Roslyn.Test.Utilities.Desktop
{
    public sealed class DesktopRuntimeEnvironmentFactory : IRuntimeEnvironmentFactory
    {
        public IRuntimeEnvironment Create(IEnumerable<ModuleData> additionalDependencies)
        {
            return new DesktopRuntimeEnvironment(additionalDependencies);
        }
    }
}

#endif
