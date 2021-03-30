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
using BuildValidator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
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

            var compilationFactory = CompilationFactory.Create(assemblyFileName, optionsReader);
            var rebuild = compilationFactory.CreateCompilation(
                original.SyntaxTrees.Select(x => compilationFactory.CreateSyntaxTree(x.FilePath, x.GetText())).ToImmutableArray(),
                original.References.ToImmutableArray());

            Assert.IsType<TCompilation>(rebuild);
            VerifyCompilationOptions(original.Options, rebuild.Options);

            using var rebuildStream = new MemoryStream();
            var result = compilationFactory.Emit(
                rebuildStream,
                rebuild,
                embeddedTexts: ImmutableArray<EmbeddedText>.Empty,
                CancellationToken.None);
            Assert.Empty(result.Diagnostics);
            Assert.True(result.Success);
            Assert.Equal(originalBytes.ToArray(), rebuildStream.ToArray());
        }

#pragma warning disable 612
        private static void VerifyCompilationOptions(CompilationOptions originalOptions, CompilationOptions rebuildOptions)
        {
            var type = originalOptions.GetType();
            foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                switch (propertyInfo.Name)
                {
                    case nameof(CompilationOptions.GeneralDiagnosticOption):
                    case nameof(CompilationOptions.Features):
                    case nameof(CompilationOptions.ModuleName):
                    case nameof(CompilationOptions.MainTypeName):
                    case nameof(CompilationOptions.ConcurrentBuild):
                    case nameof(CompilationOptions.WarningLevel):
                        // Can be different and are special cased
                        break;
                    case nameof(VisualBasicCompilationOptions.ParseOptions):
                        {
                            var originalValue = propertyInfo.GetValue(originalOptions)!;
                            var rebuildValue = propertyInfo.GetValue(rebuildOptions)!;
                            VerifyParseOptions((ParseOptions)originalValue, (ParseOptions)rebuildValue);
                        }
                        break;
                    default:
                        {
                            var originalValue = propertyInfo.GetValue(originalOptions);
                            var rebuildValue = propertyInfo.GetValue(rebuildOptions);
                            Assert.Equal(originalValue, rebuildValue);
                        }
                        break;
                }
            }
        }
#pragma warning restore 612

        private static void VerifyParseOptions(ParseOptions originalOptions, ParseOptions rebuildOptions)
        {
            var type = originalOptions.GetType();
            foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                // Several options are expected to be different and they are special cased here.
                if (propertyInfo.Name == nameof(VisualBasicParseOptions.SpecifiedLanguageVersion))
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
                options: BaseCSharpCompilationOptions.WithPlatform(platform),
                sourceFileName: "test.cs");

            VerifyRoundTrip(original);
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

            VerifyRoundTrip(original);
        }
    }
}
