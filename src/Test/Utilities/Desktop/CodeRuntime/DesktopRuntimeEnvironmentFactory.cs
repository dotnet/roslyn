using System.Collections.Generic;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities.CodeRuntime
{
    public sealed class DesktopRuntimeEnvironmentFactory : IRuntimeEnvironmentFactory
    {
        public IRuntimeEnvironment Create(IEnumerable<ModuleData> additionalDependencies)
        {
            return new DesktopRuntimeEnvironment(additionalDependencies);
        }
    }
}
