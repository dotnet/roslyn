// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ErrorReporting;
using Xunit;

namespace Microsoft.VisualStudio.Extensibility.Testing.Xunit.IntegrationTests;

[IdeSettings(MinVersion = VisualStudioVersion.VS18)]
public class AbstractIdeTest : AbstractIdeIntegrationTest
{
    static AbstractIdeTest()
    {
        TestTraceListener.Install();
    }
}
