// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
