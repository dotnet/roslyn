// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if NET472
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Test.Utilities;

namespace Roslyn.Test.Utilities.Desktop
{
    public sealed class DesktopRuntimeEnvironmentFactory : IRuntimeEnvironmentFactory
    {
        public IRuntimeEnvironment Create(ModuleData mainModule, ImmutableArray<ModuleData> modules) =>
            new DesktopRuntimeEnvironment(mainModule, modules);

        public (string Output, string ErrorOutput) CaptureOutput(Action action, int? maxOutputLength)
        {
            DesktopRuntimeEnvironment.Capture(action, maxOutputLength, out var output, out var errorOutput);
            return (output, errorOutput);
        }
    }
}

#endif
