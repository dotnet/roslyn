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

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/86198")]
    public async Task CacheHit_ReplaysAnalyzerWarnings(bool visualBasic)
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
        var originalOutput = output;

        File.Delete(Path.Combine(workingDirectory.Path, outputFileName));

        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.Equal(originalOutput + "Compilation result restored from cache." + Environment.NewLine, output);
        Assert.True(File.Exists(Path.Combine(workingDirectory.Path, outputFileName)));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/86198")]
    public async Task CacheHit_ReplaysCompilerWarnings(bool visualBasic, bool warningsAsErrors, bool suppressWarnings)
    {
        var workingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var sourceFileName = visualBasic ? "test.vb" : "test.cs";
        var source = visualBasic
            ? """
            Public Class C
                <System.Obsolete("Example warning")>
                Public Shared Sub Old()
                End Sub

                Public Shared Sub M()
                    Old()
                End Sub
            End Class
            """
            : """
            public class C
            {
                [System.Obsolete("Example warning")]
                public static void Old() { }

                public static void M() => Old();
            }
            """;

        using var serverData = await ServerUtil.CreateServer(_logger);
        var warningId = visualBasic ? "BC40000" : "CS0618";
        var additionalArguments = warningsAsErrors ? "/warnaserror+" : "/warnaserror-";
        if (suppressWarnings)
        {
            additionalArguments += $" /nowarn:{warningId}";
        }
        var arguments = BuildCompilationArguments(visualBasic, serverData.PipeName, sourceFileName, "test.dll", additionalArguments, cachePath: cacheDirectory.Path);
        var expectedExitCode = warningsAsErrors && !suppressWarnings ? 1 : 0;

        var (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory, [new(sourceFileName, source)]);
        Assert.Equal(expectedExitCode, exitCode);
        if (suppressWarnings)
        {
            Assert.DoesNotContain(warningId, output, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains((warningsAsErrors ? "error " : "warning ") + warningId, output, StringComparison.Ordinal);
        }
        Assert.DoesNotContain("Compilation result restored from cache.", output, StringComparison.Ordinal);
        var originalOutput = output;

        File.Delete(Path.Combine(workingDirectory.Path, "test.dll"));

        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory);
        Assert.Equal(expectedExitCode, exitCode);
        Assert.Equal(originalOutput + (expectedExitCode == 0 ? "Compilation result restored from cache." + Environment.NewLine : ""), output);
        Assert.Equal(expectedExitCode == 0, File.Exists(Path.Combine(workingDirectory.Path, "test.dll")));
    }

    [Theory, CombinatorialData]
    public async Task CacheHit_UsesCurrentAnalyzerConfig(bool visualBasic, bool globalConfig, bool suppressInitially)
    {
        var workingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var sourceFileName = visualBasic ? "test.vb" : "test.cs";
        var compilerWarningId = visualBasic ? "BC40000" : "CS0618";
        var source = visualBasic
            ? """
            Public Class C
                <System.Obsolete("Example warning")>
                Public Shared Sub Old()
                End Sub

                Public Shared Sub M()
                    Old()
                End Sub
            End Class
            """
            : """
            public class C
            {
                [System.Obsolete("Example warning")]
                public static void Old() { }

                public static void M() => Old();
            }
            """;
        var analyzerAssembly = CreateWarningAnalyzerAssembly(workingDirectory);
        var configPath = Path.Combine(workingDirectory.Path, globalConfig ? ".globalconfig" : ".editorconfig");
        writeConfig(suppressInitially);
        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(
            visualBasic, serverData.PipeName, sourceFileName, "test.dll",
            additionalArguments: $"/a:{analyzerAssembly} /analyzerconfig:{configPath}", cachePath: cacheDirectory.Path);

        var (exitCode, originalOutput) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory, [new(sourceFileName, source)]);
        Assert.Equal(0, exitCode);
        verifyWarnings(originalOutput, suppressInitially);
        Assert.DoesNotContain("Compilation result restored from cache.", originalOutput, StringComparison.Ordinal);

        (exitCode, var output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Equal(originalOutput + "Compilation result restored from cache." + Environment.NewLine, output);

        var originalTimestamp = File.GetLastWriteTimeUtc(configPath);
        writeConfig(!suppressInitially);
        File.SetLastWriteTimeUtc(configPath, originalTimestamp);
        (exitCode, var changedOutput) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        verifyWarnings(changedOutput, !suppressInitially);
        Assert.DoesNotContain("Compilation result restored from cache.", changedOutput, StringComparison.Ordinal);

        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Equal(changedOutput + "Compilation result restored from cache." + Environment.NewLine, output);

        writeConfig(suppressInitially);
        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Equal(originalOutput + "Compilation result restored from cache." + Environment.NewLine, output);

        void writeConfig(bool suppress)
        {
            var header = globalConfig ? "is_global = true" : "root = true" + Environment.NewLine + "[*.{cs,vb}]";
            var severity = suppress ? "none" : "warning";
            File.WriteAllText(configPath, $"""
                {header}
                dotnet_diagnostic.{compilerWarningId}.severity = {severity}
                dotnet_diagnostic.CACHWARN001.severity = {severity}
                """);
        }

        void verifyWarnings(string output, bool suppressed)
        {
            foreach (var warningId in new[] { compilerWarningId, "CACHWARN001" })
            {
                if (suppressed)
                {
                    Assert.DoesNotContain(warningId, output, StringComparison.Ordinal);
                }
                else
                {
                    Assert.Contains("warning " + warningId, output, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public async Task CacheHit_UsesCurrentGeneratorConfig()
    {
        var workingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var configPath = Path.Combine(workingDirectory.Path, ".globalconfig");
        var generatorAssembly = EmitSupportAssembly(workingDirectory, "ConfigGenerator", """
            using Microsoft.CodeAnalysis;

            [Generator]
            public sealed class ConfigGenerator : ISourceGenerator
            {
                public void Initialize(GeneratorInitializationContext context) { }

                public void Execute(GeneratorExecutionContext context)
                {
                    context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("cache_value", out var value);
                    context.AddSource("generatedSource.cs", "public class Generated { public const int Value = " + value + "; }");
                }
            }
            """);
        File.WriteAllText(configPath, "is_global = true\ncache_value = 1");
        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(
            false, serverData.PipeName, "test.cs", "test.dll",
            additionalArguments: $"/a:{generatorAssembly} /analyzerconfig:{configPath}", cachePath: cacheDirectory.Path);
        var (exitCode, output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments, workingDirectory, [new("test.cs", "public class C { }")]);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("warning ", output, StringComparison.Ordinal);
        var originalAssembly = File.ReadAllBytes(Path.Combine(workingDirectory.Path, "test.dll"));

        File.WriteAllText(configPath, "is_global = true\ncache_value = 2");
        (exitCode, output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.DoesNotContain("warning ", output, StringComparison.Ordinal);
        Assert.False(originalAssembly.SequenceEqual(File.ReadAllBytes(Path.Combine(workingDirectory.Path, "test.dll"))));

        (exitCode, output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);
    }

    [Theory, CombinatorialData]
    public async Task CacheHit_UsesEffectiveSourceConfig(bool visualBasic)
    {
        var directory = Temp.CreateDirectory();
        var firstDirectory = directory.CreateDirectory("first");
        var secondDirectory = directory.CreateDirectory("second");
        var cacheDirectory = Temp.CreateDirectory();
        var sourceFileName = visualBasic ? "test.vb" : "test.cs";
        var warningId = visualBasic ? "BC40000" : "CS0618";
        var source = visualBasic
            ? """
            Public Class C
                <System.Obsolete("Example warning")>
                Public Shared Sub Old()
                End Sub
                Public Shared Sub M()
                    Old()
                End Sub
            End Class
            """
            : """
            public class C
            {
                [System.Obsolete("Example warning")]
                public static void Old() { }
                public static void M() => Old();
            }
            """;
        var sourcePath = Path.Combine(firstDirectory.Path, sourceFileName).Replace('\\', '/');
        var configPath = directory.CreateFile(".globalconfig").WriteAllText($"""
            is_global = true
            [{sourcePath}]
            dotnet_diagnostic.{warningId}.severity = none
            """).Path;
        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(
            visualBasic, serverData.PipeName, sourceFileName, "test.dll",
            additionalArguments: $"/analyzerconfig:{configPath} /pathmap:{firstDirectory.Path}=/_/,{secondDirectory.Path}=/_/",
            cachePath: cacheDirectory.Path);

        var (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, firstDirectory, [new(sourceFileName, source)]);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("warning ", output, StringComparison.Ordinal);
        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, firstDirectory);
        Assert.Equal(0, exitCode);
        Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);

        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, secondDirectory, [new(sourceFileName, source)]);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.Contains("warning " + warningId, output, StringComparison.Ordinal);
        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, secondDirectory);
        Assert.Equal(0, exitCode);
        Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.Contains("warning " + warningId, output, StringComparison.Ordinal);
    }

    [Theory, CombinatorialData]
    public async Task CacheHit_UsesEffectiveGeneratorConfig(bool additionalFile)
    {
        var directory = Temp.CreateDirectory();
        var firstDirectory = directory.CreateDirectory("first");
        var secondDirectory = directory.CreateDirectory("second");
        var cacheDirectory = Temp.CreateDirectory();
        var inputFileName = additionalFile ? "data.txt" : "test.cs";
        var inputPath = Path.Combine(firstDirectory.Path, inputFileName).Replace('\\', '/');
        var configPath = directory.CreateFile(".globalconfig").WriteAllText($"""
            is_global = true
            cache_value = 2
            [{inputPath}]
            cache_value = 1
            """).Path;
        var generatorAssembly = EmitSupportAssembly(directory, "PerFileConfigGenerator", """
            using System.Linq;
            using Microsoft.CodeAnalysis;

            [Generator]
            public sealed class PerFileConfigGenerator : ISourceGenerator
            {
                public void Initialize(GeneratorInitializationContext context) { }

                public void Execute(GeneratorExecutionContext context)
                {
                    var options = context.AdditionalFiles.IsEmpty
                        ? context.AnalyzerConfigOptions.GetOptions(context.Compilation.SyntaxTrees.First())
                        : context.AnalyzerConfigOptions.GetOptions(context.AdditionalFiles[0]);
                    options.TryGetValue("cache_value", out var value);
                    context.AddSource("generatedSource.cs", "public class Generated { public const int Value = " + value + "; }");
                }
            }
            """);
        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(
            false, serverData.PipeName, "test.cs", "test.dll",
            additionalArguments: $"/a:{generatorAssembly} /analyzerconfig:{configPath} /generatedfilesout:{directory.Path} /pathmap:{firstDirectory.Path}=/_/,{secondDirectory.Path}=/_/"
                + (additionalFile ? " /additionalfile:data.txt" : ""),
            cachePath: cacheDirectory.Path);
        KeyValuePair<string, string>[] files = additionalFile
            ? [new("test.cs", "public class C { }"), new("data.txt", "data")]
            : [new("test.cs", "public class C { }")];

        var (exitCode, output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments, firstDirectory, files);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("warning ", output, StringComparison.Ordinal);
        Assert.Equal(1, getGeneratedValue(firstDirectory));

        (exitCode, output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments, secondDirectory, files);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.DoesNotContain("warning ", output, StringComparison.Ordinal);
        Assert.Equal(2, getGeneratedValue(secondDirectory));
        (exitCode, output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments, secondDirectory);
        Assert.Equal(0, exitCode);
        Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.Equal(2, getGeneratedValue(secondDirectory));

        static object getGeneratedValue(TempDirectory directory)
        {
            var reference = MetadataReference.CreateFromImage(File.ReadAllBytes(Path.Combine(directory.Path, "test.dll")));
            Compilation compilation = CSharpCompilation.Create("Check", references: [reference]);
            return ((IFieldSymbol)compilation.GetTypeByMetadataName("Generated").GetMembers("Value").Single()).ConstantValue;
        }
    }

    [Fact]
    public async Task CacheHit_UsesCurrentAnalyzerConfigPathInWarnings()
    {
        var workingDirectory = Temp.CreateDirectory();
        var nestedDirectory = workingDirectory.CreateDirectory("nested");
        var cacheDirectory = Temp.CreateDirectory();
        const string configContent = "[*.txt]\ndotnet_diagnostic.CACHWARN001.severity = invalid";
        var firstConfigPath = workingDirectory.CreateFile(".editorconfig").WriteAllText(configContent).Path;
        var secondConfigPath = nestedDirectory.CreateFile(".editorconfig").WriteAllText(configContent).Path;
        var additionalFileName = Path.Combine("nested", "data.txt");
        var analyzerAssembly = CreateWarningAnalyzerAssembly(workingDirectory);
        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(
            false, serverData.PipeName, "test.cs", "test.dll",
            additionalArguments: $"/a:{analyzerAssembly} /additionalfile:{additionalFileName} /pathmap:{nestedDirectory.Path}=/_/,{workingDirectory.Path}=/_/",
            cachePath: cacheDirectory.Path);

        var (exitCode, output) = RunCommandLineCompiler(
            RequestLanguage.CSharpCompile, arguments + $" /analyzerconfig:{firstConfigPath}", workingDirectory,
            [new("test.cs", "public class C { }"), new(additionalFileName, "data")]);
        Assert.Equal(0, exitCode);
        Assert.Contains("warning InvalidSeverityInAnalyzerConfig", output, StringComparison.Ordinal);
        Assert.Contains(firstConfigPath, output, StringComparison.Ordinal);

        (exitCode, output) = RunCommandLineCompiler(
            RequestLanguage.CSharpCompile, arguments + $" /analyzerconfig:{secondConfigPath}", workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.Contains("warning InvalidSeverityInAnalyzerConfig", output, StringComparison.Ordinal);
        Assert.Contains(secondConfigPath, output, StringComparison.Ordinal);
        Assert.DoesNotContain("'" + firstConfigPath + "'", output, StringComparison.Ordinal);
    }

    [Theory, CombinatorialData]
    public async Task CacheHit_UsesEffectiveGeneratedFileConfig(bool relocateConfig)
    {
        var workingDirectory = Temp.CreateDirectory();
        var firstDirectory = workingDirectory.CreateDirectory("generated1").Path;
        var secondDirectory = workingDirectory.CreateDirectory("generated2").Path;
        var cacheDirectory = Temp.CreateDirectory();
        const string configContent = """
            [generated1/**/*.cs]
            suppress_generated = true
            """;
        var configPath = workingDirectory.CreateFile(".editorconfig").WriteAllText(configContent).Path;
        var secondConfigPath = relocateConfig
            ? workingDirectory.CreateFile(Path.Combine("generated1", ".editorconfig")).WriteAllText(configContent).Path
            : configPath;
        var supportAssembly = EmitSupportAssembly(workingDirectory, "GeneratedConfig", """
            using System.Collections.Immutable;
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.Diagnostics;

            [Generator]
            public sealed class Generator : ISourceGenerator
            {
                public void Initialize(GeneratorInitializationContext context) { }
                public void Execute(GeneratorExecutionContext context)
                    => context.AddSource("generatedSource.cs", "public class Generated { }");
            }

            [DiagnosticAnalyzer(LanguageNames.CSharp)]
            public sealed class Analyzer : DiagnosticAnalyzer
            {
                private static readonly DiagnosticDescriptor s_rule = new(
                    "CACHWARN001", "Cache warning", "Cache warning", "Testing", DiagnosticSeverity.Warning, true);
                public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);
                public override void Initialize(AnalysisContext context)
                {
                    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
                    context.RegisterSyntaxTreeAction(context =>
                    {
                        if (context.Tree.FilePath.EndsWith("generatedSource.cs") &&
                            !context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree).TryGetValue("suppress_generated", out _))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(s_rule, context.Tree.GetRoot().GetLocation()));
                        }
                    });
                }
            }
            """);
        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(
            false, serverData.PipeName, "test.cs", "test.dll",
            additionalArguments: $"/a:{supportAssembly} /pathmap:{firstDirectory}=/_/,{workingDirectory.Path}=/_/",
            cachePath: cacheDirectory.Path);
        var (exitCode, output) = RunCommandLineCompiler(
            RequestLanguage.CSharpCompile, arguments + $" /analyzerconfig:{configPath} /generatedfilesout:{firstDirectory}", workingDirectory,
            [new("test.cs", "public class C { }")]);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("warning ", output, StringComparison.Ordinal);

        (exitCode, output) = RunCommandLineCompiler(
            RequestLanguage.CSharpCompile,
            arguments + $" /analyzerconfig:{secondConfigPath} /generatedfilesout:{(relocateConfig ? firstDirectory : secondDirectory)}", workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("Compilation result restored from cache.", output, StringComparison.Ordinal);
        Assert.Contains("warning CACHWARN001", output, StringComparison.Ordinal);
    }

    [Theory, CombinatorialData]
    public async Task CacheHit_UsesCurrentGeneratedSourceDirectory(bool initiallyExplicit, bool hasWarnings)
    {
        var workingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var firstDirectory = workingDirectory.CreateDirectory("generated1").Path;
        var secondDirectory = workingDirectory.CreateDirectory("generated2").Path;
        var generatorAssembly = CreateGeneratedFilesGeneratorAssembly(
            workingDirectory, (hasWarnings ? "#warning Generated warning\n" : "") + "public class GeneratedFromCache { }");
        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(
            false, serverData.PipeName, "test.cs", "test.dll",
            additionalArguments: $"/a:{generatorAssembly}", cachePath: cacheDirectory.Path);
        var firstArguments = arguments + (initiallyExplicit ? $" /generatedfilesout:{firstDirectory}" : "");

        var (exitCode, originalOutput) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, firstArguments, workingDirectory, [new("test.cs", "public class C { }")]);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("Compilation result restored from cache.", originalOutput, StringComparison.Ordinal);
        if (hasWarnings)
        {
            Assert.Contains("warning CS1030", originalOutput, StringComparison.Ordinal);
            Assert.Contains(initiallyExplicit ? "generated1" : "CacheGeneratedFilesGenerator", originalOutput, StringComparison.Ordinal);
        }

        (exitCode, var output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments + $" /generatedfilesout:{secondDirectory}", workingDirectory);
        Assert.Equal(0, exitCode);
        if (hasWarnings)
        {
            Assert.DoesNotContain("Compilation result restored from cache.", output, StringComparison.Ordinal);
            Assert.Contains("generated2", output, StringComparison.Ordinal);
            Assert.Contains("warning CS1030", output, StringComparison.Ordinal);
            Assert.DoesNotContain("generated1", output, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);
        }

        (exitCode, output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, firstArguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Equal(originalOutput + "Compilation result restored from cache." + Environment.NewLine, output);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/86198")]
    public async Task CacheHit_DoesNotReplayWarningsInDifferentContext(bool changeCulture)
    {
        var workingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var supportAssembly = CreateWarningAnalyzerAssembly(workingDirectory);
        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(false, serverData.PipeName, "test.cs", "test.dll", additionalArguments: $"/a:{supportAssembly}", cachePath: cacheDirectory.Path);

        var (exitCode, originalOutput) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments, workingDirectory, [new("test.cs", "public class C { }")]);
        Assert.Equal(0, exitCode);
        Assert.Contains("warning CACHWARN001", originalOutput, StringComparison.Ordinal);

        var differentArguments = changeCulture ? arguments : arguments + " /fullpaths /errorendlocation";
        var (differentExitCode, differentOutput) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, differentArguments, workingDirectory, preferredUiLanguage: changeCulture ? "fr" : "en");
        Assert.Equal(0, differentExitCode);
        Assert.DoesNotContain("Compilation result restored from cache.", differentOutput, StringComparison.Ordinal);
        Assert.Contains("CACHWARN001", differentOutput, StringComparison.Ordinal);
        if (!changeCulture)
        {
            Assert.StartsWith(Path.Combine(workingDirectory.Path, "test.cs") + "(1,1,", differentOutput, StringComparison.Ordinal);
        }

        (exitCode, var output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Equal(originalOutput + "Compilation result restored from cache." + Environment.NewLine, output);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/86198")]
    public async Task CacheHit_UsesCurrentWorkingDirectoryForDiagnostics(bool hasWarnings)
    {
        var workingDirectory = Temp.CreateDirectory();
        var otherWorkingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var sourcePath = Path.Combine(workingDirectory.Path, "test.cs");
        var supportAssembly = CreateWarningAnalyzerAssembly(workingDirectory);
        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(false, serverData.PipeName, sourcePath, "test.dll", additionalArguments: hasWarnings ? $"/a:{supportAssembly}" : "", cachePath: cacheDirectory.Path);

        var (exitCode, output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments, workingDirectory, [new("test.cs", "public class C { }")]);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("Compilation result restored from cache.", output, StringComparison.Ordinal);

        (exitCode, output) = RunCommandLineCompiler(RequestLanguage.CSharpCompile, arguments, otherWorkingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Equal(!hasWarnings, output.Contains("Compilation result restored from cache.", StringComparison.Ordinal));
        if (hasWarnings)
        {
            Assert.Contains(sourcePath + "(1,1): warning CACHWARN001", output, StringComparison.Ordinal);
        }
    }

    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/84889")]
    public async Task CacheHit_SupportsBinaryAdditionalFiles(bool visualBasic)
    {
        var workingDirectory = Temp.CreateDirectory();
        var cacheDirectory = Temp.CreateDirectory();
        var sourceFileName = visualBasic ? "test.vb" : "test.cs";
        var outputFileName = "test.dll";
        var additionalFileName = "additional.bin";
        var outputFilePath = Path.Combine(workingDirectory.Path, outputFileName);
        var additionalFilePath = Path.Combine(workingDirectory.Path, additionalFileName);
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

        File.WriteAllBytes(additionalFilePath, [0, 0]);

        using var serverData = await ServerUtil.CreateServer(_logger);
        var arguments = BuildCompilationArguments(
            visualBasic,
            serverData.PipeName,
            sourceFileName,
            outputFileName,
            additionalArguments: $"/additionalfile:{additionalFileName}",
            cachePath: cacheDirectory.Path);

        var (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory, [new(sourceFileName, source)]);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("CS2015", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Compilation result restored from cache.", output, StringComparison.Ordinal);

        File.Delete(outputFilePath);

        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.Contains("Compilation result restored from cache.", output, StringComparison.Ordinal);

        File.Delete(outputFilePath);
        File.WriteAllBytes(additionalFilePath, [0, 0, 1]);

        (exitCode, output) = RunCommandLineCompiler(GetLanguage(visualBasic), arguments, workingDirectory);
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("Compilation result restored from cache.", output, StringComparison.Ordinal);
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
            cachePath is null ? "" : $"/features:{Feature.UseGlobalCache}={cachePath}",
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

    private static string CreateGeneratedFilesGeneratorAssembly(TempDirectory directory, string generatedSource = "public class GeneratedFromCache { }")
        => EmitSupportAssembly(
            directory,
            "CacheGeneratedFilesGenerator",
            $$"""
            using Microsoft.CodeAnalysis;

            [Generator(LanguageNames.CSharp)]
            public sealed class CacheGeneratedFileGenerator : ISourceGenerator
            {
                public void Initialize(GeneratorInitializationContext context)
                {
                }

                public void Execute(GeneratorExecutionContext context)
                {
                    context.AddSource("generatedSource.cs", {{CSharp.SymbolDisplay.FormatLiteral(generatedSource, quote: true)}});
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
        Encoding redirectEncoding = null,
        string preferredUiLanguage = "en")
    {
        var arguments = new List<string>(argumentsSingle.Split(' '))
        {
            $"/preferreduilang:{preferredUiLanguage}"
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
