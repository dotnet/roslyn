// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests that exercise warnings that are under control of the compiler option <see cref="CSharpCompilationOptions.WarningVersion"/>.
    /// </summary>
    public class WarningVersionTests : CompilingTestBase
    {
        [Fact]
        public void CompareStructToNull()
        {
            var source = @"
class Program
{
    public static void M(S s)
    {
        if (s == null) { }
        if (s != null) { }
    }
}
struct S
{
    public static bool operator==(S s1, S s2) => false;
    public static bool operator!=(S s1, S s2) => true;
    public override bool Equals(object other) => false;
    public override int GetHashCode() => 0;
}
";
            var whenWave5 = new[]
            {
                // (6,13): warning CS8073: The result of the expression is always 'false' since a value of type 'S' is never equal to 'null' of type 'S?'
                //         if (s == null) { }
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "s == null").WithArguments("false", "S", "S?").WithLocation(6, 13),
                // (7,13): warning CS8073: The result of the expression is always 'true' since a value of type 'S' is never equal to 'null' of type 'S?'
                //         if (s != null) { }
                Diagnostic(ErrorCode.WRN_NubExprIsConstBool2, "s != null").WithArguments("true", "S", "S?").WithLocation(7, 13)
            };
            CreateCompilation(source).VerifyDiagnostics();
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningVersion(4.9m)).VerifyDiagnostics();
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningVersion(5m)).VerifyDiagnostics(whenWave5);
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningVersion(5.0m)).VerifyDiagnostics(whenWave5);
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningVersion(5.1m)).VerifyDiagnostics(whenWave5);
        }
    }
}
