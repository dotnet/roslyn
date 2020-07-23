// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;
using System;
using System.Text;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class CovariantReturnTests : EmitMetadataTestBase
    {
        [Fact]
        public void SimpleCovariantReturnEndToEndTest()
        {
            var source = @"
using System;
class Base
{
    public virtual object M() => ""Base.M"";
}
class Derived : Base
{
    public override string M() => ""Derived.M"";
}
class Program
{
    static void Main()
    {
        Derived d = new Derived();
        Base b = d;
        string s = d.M();
        object o = b.M();
        Console.WriteLine(s.ToString());
        Console.WriteLine(o.ToString());
    }
}
";
            var compilation = CreateCompilation(
                source,
                options: TestOptions.DebugExe,
                parseOptions: TestOptions.WithCovariantReturns,
                targetFramework: TargetFramework.NetCoreApp30); // PROTOTYPE(ngafter): this should be NetCore50
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"Derived.M
Derived.M";

            CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }
    }
}
