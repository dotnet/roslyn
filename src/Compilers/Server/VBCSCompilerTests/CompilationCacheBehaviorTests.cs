// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests;

public sealed class CompilationCacheBehaviorTests(ITestOutputHelper testOutputHelper) : TestBase
{
    private readonly ICompilerServerLogger _logger = new XunitCompilerServerLogger(testOutputHelper);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CacheHit_OmitsWarningDiagnosticsFromOutput(bool visualBasic)
    {
        var workingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var supportAssembly = CreateWarningAnalyzerAssembly(workingDirectory);
        var sourceFileName = visualBasic ? "test.vb" : "test.cs";
        var outputFileName = "test.dll";
        var source = visualBasic
            ? """
            Public Class TestType
            End Class
            """
            : """
            public class TestType
            {
            }
            """;

        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(visualBasic, serverData.PipeName, sourceFileName, outputFileName, additionalArguments: $"/a:{supportAssembly}", cachePath: cacheDirectory.Path);

        var (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory, [new(sourceFileName, source)]);
        Assert.Equal(0, exitCode);
        Assert.Contains("warning CACHWARN001", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Compilation result restored from cache.", output, StringComparison.Ordinal);

        File.Delete(Path.Combine(workingDirectory.Path, outputFileName));

        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.DoesNotContain("warning CACHWARN001", output, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(workingDirectory.Path, outputFileName)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CacheHit_DoesNotRecreateTouchedFiles(bool visualBasic)
    {
        var workingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var sourceFileName = visualBasic ? "test.vb" : "test.cs";
        var outputFileName = "test.dll";
        var touchedFilesBase = Path.Combine(workingDirectory.Path, "touched");
        var source = visualBasic
            ? """
            Public Class TestType
            End Class
            """
            : """
            public class TestType
            {
            }
            """;

        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(visualBasic, serverData.PipeName, sourceFileName, outputFileName, additionalArguments: $"/touchedfiles:{touchedFilesBase}", cachePath: cacheDirectory.Path);

        var (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory, [new(sourceFileName, source)]);
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(touchedFilesBase + ".read"));
        Assert.True(File.Exists(touchedFilesBase + ".write"));

        File.Delete(Path.Combine(workingDirectory.Path, outputFileName));
        File.Delete(touchedFilesBase + ".read");
        File.Delete(touchedFilesBase + ".write");

        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.False(File.Exists(touchedFilesBase + ".read"));
        Assert.False(File.Exists(touchedFilesBase + ".write"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CacheHit_OmitsReportAnalyzerOutput(bool visualBasic)
    {
        var workingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var supportAssembly = CreateReportAnalyzerAssembly(workingDirectory);
        var sourceFileName = visualBasic ? "test.vb" : "test.cs";
        var outputFileName = "test.dll";
        var source = visualBasic
            ? """
            Public Class TestType
            End Class
            """
            : """
            public class TestType
            {
            }
            """;

        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(visualBasic, serverData.PipeName, sourceFileName, outputFileName, additionalArguments: $"/reportanalyzer /a:{supportAssembly}", cachePath: cacheDirectory.Path);

        var (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory, [new(sourceFileName, source)]);
        Assert.Equal(0, exitCode);
        Assert.Contains("Total analyzer execution time:", output, StringComparison.Ordinal);
        Assert.Contains("Total generator execution time:", output, StringComparison.Ordinal);
        Assert.Contains("CacheWarningAnalyzer", output, StringComparison.Ordinal);
        Assert.Contains("CacheReportGenerator", output, StringComparison.Ordinal);

        File.Delete(Path.Combine(workingDirectory.Path, outputFileName));

        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Total analyzer execution time:", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Total generator execution time:", output, StringComparison.Ordinal);
        Assert.DoesNotContain("CacheWarningAnalyzer", output, StringComparison.Ordinal);
        Assert.DoesNotContain("CacheReportGenerator", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CacheHit_OmitsReportIvtsOutput(bool visualBasic)
    {
        var workingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var referenceSourceFileName = visualBasic ? "FriendLib.vb" : "FriendLib.cs";
        var consumerSourceFileName = visualBasic ? "Consumer.vb" : "Consumer.cs";
        var referenceOutputFileName = "FriendLib.dll";
        var consumerOutputFileName = "Consumer.dll";
        var referenceSource = visualBasic
            ? """
            Imports System.Runtime.CompilerServices

            <Assembly: InternalsVisibleTo("Consumer")>
            Friend Class BaseType
            End Class
            """
            : """
            using System.Runtime.CompilerServices;

            [assembly: InternalsVisibleTo("Consumer")]
            internal class BaseType
            {
            }
            """;
        var consumerSource = visualBasic
            ? """
            Friend Class Consumer
                Inherits BaseType
            End Class
            """
            : """
            internal class Consumer : BaseType
            {
            }
            """;

        using var serverData = await ServerUtil.CreateServer(_logger);

        var referenceArguments = BuildCompilationArguments(visualBasic, serverData.PipeName, referenceSourceFileName, referenceOutputFileName, cachePath: cacheDirectory.Path);
        var (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), referenceArguments, workingDirectory, [new(referenceSourceFileName, referenceSource)]);
        Assert.Equal(0, exitCode);

        var consumerArguments = BuildCompilationArguments(
            visualBasic,
            serverData.PipeName,
            consumerSourceFileName,
            consumerOutputFileName,
            additionalArguments: $"/reportivts /r:{referenceOutputFileName}",
            cachePath: cacheDirectory.Path);

        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), consumerArguments, workingDirectory, [new(consumerSourceFileName, consumerSource)]);
        Assert.Equal(0, exitCode);
        Assert.Contains("Printing 'InternalsVisibleToAttribute' information", output, StringComparison.Ordinal);
        Assert.Contains("Grants IVT to current assembly: True", output, StringComparison.Ordinal);

        File.Delete(Path.Combine(workingDirectory.Path, consumerOutputFileName));

        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), consumerArguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Printing 'InternalsVisibleToAttribute' information", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CacheHit_DoesNotRecreateGeneratedFiles()
    {
        var workingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var sourceFileName = "test.cs";
        var outputFileName = "test.dll";
        var generatedFilesDirectory = Path.Combine(workingDirectory.Path, "generated");
        var generatorAssembly = CreateGeneratedFilesGeneratorAssembly(workingDirectory);
        var source = """
            public class TestType
            {
            }
            """;

        Directory.CreateDirectory(generatedFilesDirectory);

        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(
            visualBasic: false,
            serverData.PipeName,
            sourceFileName,
            outputFileName,
            additionalArguments: $"/langversion:preview /generatedfilesout:{generatedFilesDirectory} /a:{generatorAssembly}",
            cachePath: cacheDirectory.Path);

        var (exitCode, output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments, workingDirectory, [new(sourceFileName, source)]);
        Assert.Equal(0, exitCode);
        Assert.Single(Directory.GetFiles(generatedFilesDirectory, "generatedSource.cs", SearchOption.AllDirectories));

        File.Delete(Path.Combine(workingDirectory.Path, outputFileName));
        Directory.Delete(generatedFilesDirectory, recursive: true);

        (exitCode, output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.False(Directory.Exists(generatedFilesDirectory));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CacheHit_DoesNotRecreateDebugDeterminismKey(bool visualBasic)
    {
        var workingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var sourceFileName = visualBasic ? "test.vb" : "test.cs";
        var outputFileName = "test.dll";
        var pdbFileName = "test.pdb";
        var keyFileName = "test.dll.key";
        var source = visualBasic
            ? """
            Public Class TestType
            End Class
            """
            : """
            public class TestType
            {
            }
            """;

        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(
            visualBasic,
            serverData.PipeName,
            sourceFileName,
            outputFileName,
            additionalArguments: "/debug:portable /features:debug-determinism",
            cachePath: cacheDirectory.Path);

        var (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory, [new(sourceFileName, source)]);
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(workingDirectory.Path, keyFileName)));

        File.Delete(Path.Combine(workingDirectory.Path, outputFileName));
        File.Delete(Path.Combine(workingDirectory.Path, pdbFileName));
        File.Delete(Path.Combine(workingDirectory.Path, keyFileName));

        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(workingDirectory.Path, outputFileName)));
        Assert.True(File.Exists(Path.Combine(workingDirectory.Path, pdbFileName)));
        Assert.False(File.Exists(Path.Combine(workingDirectory.Path, keyFileName)));
    }

    private static RequestLanguage GetLanguage(bool visualBasic)
        => visualBasic ? RequestLanguage.VisualBasicCompile : RequestLanguage.CSharpCompile;

    private static string BuildCompilationArguments(bool visualBasic, string pipeName, string sourceFileName, string outputFileName, string additionalArguments = "", string cachePath = null)
    {
        var languageArguments = visualBasic ? "/vbruntime*" : "";
        var segments = new[]
        {
            sourceFileName,
            $"/shared:{pipeName}",
            "/deterministic+",
            "/nologo",
            "/t:library",
            $"/out:{outputFileName}",
            cachePath is null ? "" : $"/features:{CompilerOptionParseUtilities.UseGlobalCacheFeatureFlag}={cachePath}",
            languageArguments,
            additionalArguments,
        };

        return string.Join(" ", segments.Where(static segment => !string.IsNullOrWhiteSpace(segment)));
    }

    private static string CreateWarningAnalyzerAssembly(TempDirectory directory)
        => EmitSupportAssembly(
            directory,
            "CacheWarningAnalyzer",
            """
            using System.Collections.Immutable;
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.Diagnostics;

            [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
            public sealed class CacheWarningAnalyzer : DiagnosticAnalyzer
            {
                private static readonly DiagnosticDescriptor s_rule = new(
                    "CACHWARN001",
                    "Cache warning",
                    "Cache warning",
                    "Testing",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);

                public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

                public override void Initialize(AnalysisContext context)
                    => context.RegisterSyntaxTreeAction(static context => context.ReportDiagnostic(Diagnostic.Create(s_rule, context.Tree.GetRoot().GetLocation())));
            }
            """);

    private static string CreateReportAnalyzerAssembly(TempDirectory directory)
        => EmitSupportAssembly(
            directory,
            "CacheReportAnalyzer",
            """
            using System.Collections.Immutable;
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.Diagnostics;

            [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
            public sealed class CacheWarningAnalyzer : DiagnosticAnalyzer
            {
                private static readonly DiagnosticDescriptor s_rule = new(
                    "CACHWARN001",
                    "Cache warning",
                    "Cache warning",
                    "Testing",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);

                public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

                public override void Initialize(AnalysisContext context)
                    => context.RegisterSyntaxTreeAction(static context => context.ReportDiagnostic(Diagnostic.Create(s_rule, context.Tree.GetRoot().GetLocation())));
            }

            [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
            public sealed class CacheReportGenerator : ISourceGenerator
            {
                public void Initialize(GeneratorInitializationContext context)
                {
                }

                public void Execute(GeneratorExecutionContext context)
                {
                }
            }
            """);

    private static string CreateGeneratedFilesGeneratorAssembly(TempDirectory directory)
        => EmitSupportAssembly(
            directory,
            "CacheGeneratedFilesGenerator",
            """
            using Microsoft.CodeAnalysis;

            [Generator(LanguageNames.CSharp)]
            public sealed class CacheGeneratedFileGenerator : ISourceGenerator
            {
                public void Initialize(GeneratorInitializationContext context)
                {
                }

                public void Execute(GeneratorExecutionContext context)
                {
                    context.AddSource("generatedSource.cs", "public class GeneratedFromCache { }");
                }
            }
            """);

    private static string EmitSupportAssembly(TempDirectory directory, string assemblyName, string source)
    {
        var path = Path.Combine(directory.Path, assemblyName + ".dll");
        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: GetSupportAssemblyReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var emitResult = compilation.Emit(path);
        Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));
        return path;
    }

    private static IEnumerable<MetadataReference> GetSupportAssemblyReferences()
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .ToList();

        var codeAnalysisAssemblyPath = typeof(ISourceGenerator).Assembly.Location;
        if (!references.Any(reference => string.Equals(reference.Display, codeAnalysisAssemblyPath, StringComparison.OrdinalIgnoreCase)))
        {
            references.Add(MetadataReference.CreateFromFile(codeAnalysisAssemblyPath));
        }

        return references;
    }

    private static void ReferenceNetstandardDllIfCoreClr(TempDirectory currentDirectory, List<string> arguments)
    {
        var filePath = Path.Combine(currentDirectory.Path, "netstandard.dll");
        File.WriteAllBytes(filePath, NetStandard20.Resources.netstandard);
        arguments.Add("/nostdlib");
        arguments.Add("/r:netstandard.dll");
    }

    private static void CreateFiles(TempDirectory currentDirectory, IEnumerable<KeyValuePair<string, string>> files)
    {
        if (files == null)
        {
            return;
        }

        foreach (var pair in files)
        {
            currentDirectory.CreateFile(pair.Key).WriteAllText(pair.Value);
        }
    }

    private static (T Result, string Output) UseTextWriter<T>(Encoding encoding, Func<TextWriter, T> func)
    {
        MemoryStream memoryStream;
        TextWriter writer;
        if (encoding == null)
        {
            memoryStream = null;
            writer = new StringWriter();
        }
        else
        {
            memoryStream = new MemoryStream();
            writer = new StreamWriter(memoryStream, encoding);
        }

        var result = func(writer);
        writer.Flush();

        if (memoryStream != null)
        {
            return (result, encoding.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length));
        }

        return (result, ((StringWriter)writer).ToString());
    }

    private (int ExitCode, string Output) RunCommandLineCompiler(
        RequestLanguage language,
        string argumentsSingle,
        TempDirectory currentDirectory,
        IEnumerable<KeyValuePair<string, string>> filesInDirectory = null,
        IEnumerable<KeyValuePair<string, string>> additionalEnvironmentVars = null,
        Encoding redirectEncoding = null)
    {
        var arguments = new List<string>(argumentsSingle.Split(' '))
        {
            "/preferreduilang:en"
        };

        ReferenceNetstandardDllIfCoreClr(currentDirectory, arguments);
        CreateFiles(currentDirectory, filesInDirectory);

        var client = ServerUtil.CreateBuildClient(language, _logger);
        var buildPaths = new BuildPaths(
            clientDir: Path.GetDirectoryName(typeof(CommonCompiler).Assembly.Location),
            workingDir: currentDirectory.Path,
            sdkDir: ServerUtil.DefaultSdkDirectory,
            tempDir: Path.GetTempPath());

        var (result, output) = UseTextWriter(
            redirectEncoding,
            writer => ApplyEnvironmentVariables(additionalEnvironmentVars, () => client.RunCompilation(arguments, buildPaths, writer)));

        Assert.True(result.RanOnServer);
        return (result.ExitCode, output);
    }
}
#endif
