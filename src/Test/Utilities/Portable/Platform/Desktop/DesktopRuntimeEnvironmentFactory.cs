// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if NET461 || NET46
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
