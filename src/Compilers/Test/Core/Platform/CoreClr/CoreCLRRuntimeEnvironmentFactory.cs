// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if NETCOREAPP

using System;
using System.Collections.Generic;
using Roslyn.Test.Utilities;

namespace Roslyn.Test.Utilities.CoreClr
{
    public sealed class CoreCLRRuntimeEnvironmentFactory : IRuntimeEnvironmentFactory
    {
        public IRuntimeEnvironment Create(IEnumerable<ModuleData> additionalDependencies)
            => new CoreCLRRuntimeEnvironment(additionalDependencies);
    }
}
#endif
