// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.Extensibility.Testing.Xunit.IntegrationTests;

[IdeSettings(MinVersion = VisualStudioVersion.VS18)]
public class AbstractIdeTest : AbstractIdeIntegrationTest
{
    static AbstractIdeTest()
    {
        // Make sure to run the module initializer for Roslyn.Test.Utilities before installing TestTraceListener, or
        // it will replace it with ThrowingTraceListener later.
        RuntimeHelpers.RunModuleConstructor(typeof(TestBase).Module.ModuleHandle);
        TestTraceListener.Install();
    }
}
