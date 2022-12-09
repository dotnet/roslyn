// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public class AllOptionsTests
{
    [Fact]
    public void TestOptions()
    {
        OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(AllOptionsTests).Assembly.Location));
    }
}
