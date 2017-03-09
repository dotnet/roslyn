using System;
using System.Collections.Generic;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities.CodeRuntime
{
    public sealed class CoreCLRRuntimeEnvironmentFactory : IRuntimeEnvironmentFactory
    {
        public IRuntimeEnvironment Create(IEnumerable<ModuleData> additionalDependencies)
            => new CoreCLRRuntimeEnvironment(additionalDependencies);
    }
}
