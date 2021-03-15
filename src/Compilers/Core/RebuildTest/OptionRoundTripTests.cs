// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System;
using System.Reflection.PortableExecutable;
using BuildValidator;
using Castle.Core.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging;
using Xunit;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.VisualBasic;
using System.Text;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public class OptionRoundTripTests : CSharpTestBase
    {
        public static readonly CSharpCompilationOptions BaseCSharpCompliationOptions = TestOptions.DebugExe.WithDeterministic(true);

        // https://github.com/dotnet/roslyn/issues/51873
        // Once the above issue is fixed please remove the embedVbCoreRuntime option as that is working around
        // the bug. Tests need to individually decide if they want to support this.
        public static readonly VisualBasicCompilationOptions BaseVisualBasicCompliationOptions = new VisualBasicCompilationOptions(
            outputKind: OutputKind.ConsoleApplication,
            deterministic: true,
            embedVbCoreRuntime: true);

        public static readonly object[][] Platforms = ((Platform[])Enum.GetValues(typeof(Platform))).Select(p => new[] { (object)p }).ToArray();

        private static void VerifyRoundTrip<TCompilation>(TCompilation original)
            where TCompilation : Compilation
        {
            Assert.True(original.SyntaxTrees.All(x => !string.IsNullOrEmpty(x.FilePath)));
            Assert.True(original.Options.Deterministic);

            original.VerifyDiagnostics();
            var originalBytes = original.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));
            var originalReader = new PEReader(originalBytes);
            var originalPdbReader = originalReader.GetEmbeddedPdbMetadataReader();

            var factory = LoggerFactory.Create(configure => { });
            var logger = factory.CreateLogger("RoundTripVerification");
            var buildConstructor = new BuildConstructor(logger);
            var optionsReader = new CompilationOptionsReader(logger, originalPdbReader, originalReader);
            var assemblyFileName = original.AssemblyName!;
            if (typeof(TCompilation) == typeof(CSharpCompilation))
            {
                var assemblyFileExtension = original.Options.OutputKind switch
                {
                    OutputKind.DynamicallyLinkedLibrary => ".dll",
                    OutputKind.ConsoleApplication => ".exe",
                    _ => throw new InvalidOperationException(),
                };
                assemblyFileName += assemblyFileExtension;
            }

            var rebuild = buildConstructor.CreateCompilation(
                assemblyFileName,
                optionsReader,
                original.SyntaxTrees.Select(x => SyntaxTreeInfo.Create(x)).ToImmutableArray(),
                metadataReferences: original.References.ToImmutableArray());

            Assert.IsType<TCompilation>(rebuild);
            VerifyOptions(original.Options, rebuild.Options);

            using var rebuildStream = new MemoryStream();
            var result = BuildConstructor.Emit(
                rebuildStream,
                optionsReader,
                rebuild,
                embeddedTexts: ImmutableArray<EmbeddedText>.Empty,
                CancellationToken.None);
            Assert.Empty(result.Diagnostics);
            Assert.True(result.Success);

            Assert.Equal(originalBytes.ToArray(), rebuildStream.ToArray());
        }

        private static void VerifyOptions<TOptions>(TOptions originalOptions, TOptions rebuildOptions)
            where TOptions : CompilationOptions
        {
            var type = typeof(TOptions);
            foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Several options are expected to be different and they are special cased here.
                if (propertyInfo.Name == nameof(CompilationOptions.GeneralDiagnosticOption) ||
                    propertyInfo.Name == nameof(CompilationOptions.ModuleName) ||
                    propertyInfo.Name == nameof(CompilationOptions.MainTypeName) ||
                    propertyInfo.Name == nameof(CompilationOptions.WarningLevel))
                {
                    continue;
                }

                var originalValue = propertyInfo.GetValue(originalOptions);
                var rebuildValue = propertyInfo.GetValue(rebuildOptions);

                Assert.Equal(originalValue, rebuildValue);
            }
        }

        [Theory]
        [MemberData(nameof(Platforms))]
        public void Platform_RoundTrip(Platform platform)
        {
            var original = CreateCompilation(
                "class C { static void Main() { } }",
                options: BaseCSharpCompliationOptions.WithPlatform(platform),
                sourceFileName: "test.cs");

            VerifyRoundTrip(original);
        }

        [Theory]
        [MemberData(nameof(Platforms))]
        public void Platform_RoundTrip_VB(Platform platform)
        {
            var original = CreateVisualBasicCompilation(
                compilationOptions: BaseVisualBasicCompliationOptions.WithPlatform(platform).WithModuleName("test"),
                encoding: Encoding.UTF8,
                code: @"
Class C
    Shared Sub Main()
    End Sub
End Class",
                assemblyName: "test",
                sourceFileName: "test.vb");

            VerifyRoundTrip(original);
        }
    }
}
