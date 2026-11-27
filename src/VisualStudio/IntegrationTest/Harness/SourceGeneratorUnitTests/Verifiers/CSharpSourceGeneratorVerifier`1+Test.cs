// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Uncomment the following line to write expected files to disk
//#define WRITE_EXPECTED

#if WRITE_EXPECTED
#warning WRITE_EXPECTED is fine for local builds, but should not be merged to the main branch.
#endif

namespace Microsoft.VisualStudio.Extensibility.Testing.SourceGenerator.UnitTests.Verifiers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Testing;
    using Microsoft.CodeAnalysis.Testing;

    public static partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
        where TSourceGenerator : IIncrementalGenerator, new()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public class Test : CSharpSourceGeneratorTest<EmptySourceGeneratorProvider, DefaultVerifier>
#pragma warning restore CS0618 // Type or member is obsolete
        {
            private readonly string? _testFile;
            private readonly string? _testMethod;

            public Test([CallerFilePath] string? testFile = null, [CallerMemberName] string? testMethod = null)
            {
                CompilerDiagnostics = CompilerDiagnostics.Warnings;

                _testFile = testFile;
                _testMethod = testMethod;

#if WRITE_EXPECTED
                TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
#endif
            }

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

            protected override IEnumerable<Type> GetSourceGenerators()
            {
                yield return typeof(TSourceGenerator);
            }

            protected override CompilationOptions CreateCompilationOptions()
            {
                var compilationOptions = (CSharpCompilationOptions)base.CreateCompilationOptions();
                return compilationOptions
                    .WithAllowUnsafe(false)
                    .WithWarningLevel(99)
                    .WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItem("CS8019", ReportDiagnostic.Warn));
            }

            protected override ParseOptions CreateParseOptions()
            {
                return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
            }

            protected override async Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
            {
                if (project is null)
                {
                    throw new ArgumentNullException(nameof(project));
                }

                var resourceDirectory = Path.Combine(Path.GetDirectoryName(_testFile), "Resources", _testMethod);

                var (compilation, generatorDiagnostics) = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
                var expectedNames = new HashSet<string>();
                foreach (var tree in compilation.SyntaxTrees.Skip(project.DocumentIds.Count))
                {
                    WriteTreeToDiskIfNecessary(tree, resourceDirectory);
                    expectedNames.Add(Path.GetFileName(tree.FilePath));
                }

                var currentTestPrefix = $"{typeof(TestServicesSourceGeneratorTests).Namespace}.Resources.{_testMethod}.";
                foreach (var name in GetType().Assembly.GetManifestResourceNames())
                {
                    if (!name.StartsWith(currentTestPrefix))
                    {
                        continue;
                    }

                    if (!expectedNames.Contains(name.Substring(currentTestPrefix.Length)))
                    {
                        throw new InvalidOperationException($"Unexpected test resource: {name.Substring(currentTestPrefix.Length)}");
                    }
                }

                return (compilation, generatorDiagnostics);
            }

            public Test AddGeneratedSources([CallerMemberName] string? testMethod = null)
            {
                var expectedPrefix = $"{typeof(TestServicesSourceGeneratorTests).Namespace}.Resources.{testMethod}.";
                foreach (var resourceName in typeof(Test).Assembly.GetManifestResourceNames())
                {
                    if (!resourceName.StartsWith(expectedPrefix))
                    {
                        continue;
                    }

                    using var resourceStream = typeof(TestServicesSourceGeneratorTests).Assembly.GetManifestResourceStream(resourceName);
                    if (resourceStream is null)
                    {
                        throw new InvalidOperationException();
                    }

                    using var reader = new StreamReader(resourceStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
                    var name = resourceName.Substring(expectedPrefix.Length);
                    TestState.GeneratedSources.Add((typeof(TestServicesSourceGenerator), name, reader.ReadToEnd()));
                }

                return this;
            }

            [Conditional("WRITE_EXPECTED")]
            private static void WriteTreeToDiskIfNecessary(SyntaxTree tree, string resourceDirectory)
            {
                if (tree.Encoding is null)
                {
                    throw new ArgumentException("Syntax tree encoding was not specified");
                }

                var name = Path.GetFileName(tree.FilePath);
                var filePath = Path.Combine(resourceDirectory, name);
                Directory.CreateDirectory(resourceDirectory);
                File.WriteAllText(filePath, tree.GetText().ToString(), tree.Encoding);
            }
        }
    }
}
