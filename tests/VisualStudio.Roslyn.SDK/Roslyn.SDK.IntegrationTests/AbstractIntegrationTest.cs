// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Extensibility.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    [IdeSettings(MinVersion = VisualStudioVersion.VS2022, MaxAttempts = 2)]
    public abstract class AbstractIntegrationTest : AbstractIdeIntegrationTest
    {
    }
}
