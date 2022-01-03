// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

// Uncomment the following line to write expected files to disk
////#define WRITE_EXPECTED

namespace Microsoft.VisualStudio.Extensibility.Testing.SourceGenerator.UnitTests.Verifiers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Testing;
    using Microsoft.CodeAnalysis.Testing;
    using Microsoft.CodeAnalysis.Testing.Verifiers;

    public static partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
        where TSourceGenerator : IIncrementalGenerator, new()
    {
        public class Test : CSharpSourceGeneratorTest<EmptySourceGeneratorProvider, XUnitVerifier>
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

            protected override IEnumerable<ISourceGenerator> GetSourceGenerators()
            {
                yield return new TSourceGenerator().AsSourceGenerator();
            }

            protected override CompilationOptions CreateCompilationOptions()
            {
                var compilationOptions = (CSharpCompilationOptions)base.CreateCompilationOptions();
                return compilationOptions.WithWarningLevel(99);
            }

            protected override ParseOptions CreateParseOptions()
            {
                return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
            }

            protected override async Task<Compilation> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
            {
                var resourceDirectory = Path.Combine(Path.GetDirectoryName(_testFile), "Resources", _testMethod);

                var compilation = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
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

                return compilation;
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
