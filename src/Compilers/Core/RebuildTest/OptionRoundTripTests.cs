// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Rebuild;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public class OptionRoundTripTests : CSharpTestBase
    {
        public static readonly CSharpCompilationOptions BaseCSharpCompilationOptions = TestOptions.DebugExe.WithDeterministic(true);

        public static readonly VisualBasicCompilationOptions BaseVisualBasicCompilationOptions = new VisualBasicCompilationOptions(
            outputKind: OutputKind.ConsoleApplication,
            deterministic: true);

        public static readonly object[][] Platforms = ((Platform[])Enum.GetValues(typeof(Platform))).Select(p => new[] { (object)p }).ToArray();

        [Theory]
        [MemberData(nameof(Platforms))]
        public void Platform_RoundTrip(Platform platform)
        {
            var original = CreateCompilation(
                "class C { static void Main() { } }",
                options: BaseCSharpCompilationOptions.WithPlatform(platform),
                sourceFileName: "test.cs");

            RoundTripUtil.VerifyRoundTrip(original);
        }

        [Theory]
        [MemberData(nameof(Platforms))]
        public void Platform_RoundTrip_VB(Platform platform)
        {
            var original = CreateVisualBasicCompilation(
                compilationOptions: BaseVisualBasicCompilationOptions.WithPlatform(platform).WithModuleName("test"),
                encoding: Encoding.UTF8,
                code: @"
Class C
    Shared Sub Main()
    End Sub
End Class",
                assemblyName: "test",
                sourceFileName: "test.vb");

            RoundTripUtil.VerifyRoundTrip(original);
        }

        [Theory]
        [CombinatorialData]
        public void OptimizationLevel_ParsePdbSerializedString(OptimizationLevel optimization, bool debugPlus)
        {
            var data = OptimizationLevelFacts.ToPdbSerializedString(optimization, debugPlus);
            Assert.True(OptimizationLevelFacts.TryParsePdbSerializedString(data, out var optimization2, out var debugPlus2));
            Assert.Equal(optimization, optimization2);
            Assert.Equal(debugPlus, debugPlus2);
        }

        [Fact]
        public void PortablePdb()
        {
            var original = CreateCompilation(
                @"class C { static void Main() { } }",
                options: BaseCSharpCompilationOptions,
                sourceFileName: "test.cs");

            RoundTripUtil.VerifyRoundTrip(original, new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: "test.pdb"));
        }
    }
}
