// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public class AllOptionsTests
{
    [Fact]
    public void TestOptions()
    {
        var info = OptionsTestInfo.CollectOptions(Path.GetDirectoryName(typeof(AllOptionsTests).Assembly.Location));

        File.WriteAllText(@"C:\temp\vsoptions.txt", info.ToString());
    }
}
