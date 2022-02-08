// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class Emit2 : EmitMetadataTestBase
    {
        [Fact]
        public void Test()
        {
            var source = @"System.Console.Write(""I'm a new test project. Please delete this test."");";
            CompileAndVerify(source, expectedOutput: "I'm a new test project. Please delete this test.");
        }
    }
}
