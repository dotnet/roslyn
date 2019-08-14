// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if NETCOREAPP2_1 || NETCOREAPP3_0

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
