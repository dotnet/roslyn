using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Roslyn.Test.Utilities
{
    public sealed class DesktopRuntimeEnvironmentFactory : IRuntimeEnvironmentFactory
    {
        public IRuntimeEnvironment Create(IEnumerable<ModuleData> additionalDependencies)
        {
            return new HostedRuntimeEnvironment(additionalDependencies);
        }
    }
}
