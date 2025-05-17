// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

// Uncomment the following line to write expected files to disk
////#define WRITE_EXPECTED

#if WRITE_EXPECTED
#warning WRITE_EXPECTED is fine for local builds, but should not be merged to the main branch.
#endif

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
using Microsoft.CodeAnalysis.ResxSourceGenerator.VisualBasic;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace Microsoft.CodeAnalysis.ResxSourceGenerator.Test
{
    public static partial class VisualBasicSourceGeneratorVerifier<TSourceGenerator>
        where TSourceGenerator : IIncrementalGenerator, new()
    {
        public class Test : VisualBasicSourceGeneratorTest<TSourceGenerator, DefaultVerifier>
        {
            private readonly string _identifier;
            private readonly string? _testFile;
            private readonly string? _testMethod;

            public Test([CallerFilePath] string? testFile = null, [CallerMemberName] string? testMethod = null)
                : this(string.Empty, testFile, testMethod)
            {
            }

            public Test(string identifier, [CallerFilePath] string? testFile = null, [CallerMemberName] string? testMethod = null)
            {
                _identifier = identifier;
                _testFile = testFile;
                _testMethod = testMethod;

#if WRITE_EXPECTED
                TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
#endif
            }

            private string ResourceName
            {
                get
                {
                    if (string.IsNullOrEmpty(_identifier))
                        return _testMethod ?? "";

                    return $"{_testMethod}_{_identifier}";
                }
            }

            protected override async Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
            {
                var resourceDirectory = Path.Combine(Path.GetDirectoryName(_testFile)!, "Resources", ResourceName);

                var (compilation, generatorDiagnostics) = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
                var expectedNames = new HashSet<string>();
                foreach (var tree in compilation.SyntaxTrees.Skip(project.DocumentIds.Count))
                {
                    WriteTreeToDiskIfNecessary(tree, resourceDirectory);
                    expectedNames.Add(Path.GetFileName(tree.FilePath));
                }

                var currentTestPrefix = $"{typeof(ResxGeneratorTests).Assembly.GetName().Name}.Resources.{ResourceName}.";
                foreach (var name in GetType().Assembly.GetManifestResourceNames())
                {
                    if (!name.StartsWith(currentTestPrefix, StringComparison.Ordinal))
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

            public Test AddGeneratedSources()
            {
                var expectedPrefix = $"{typeof(ResxGeneratorTests).Assembly.GetName().Name}.Resources.{ResourceName}.";
                foreach (var resourceName in typeof(Test).Assembly.GetManifestResourceNames())
                {
                    if (!resourceName.StartsWith(expectedPrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    using var resourceStream = typeof(ResxGeneratorTests).Assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException();
                    using var reader = new StreamReader(resourceStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
                    var name = resourceName.Substring(expectedPrefix.Length);
                    TestState.GeneratedSources.Add((typeof(VisualBasicResxGenerator), name, SourceText.From(reader.ReadToEnd(), Encoding.UTF8, SourceHashAlgorithm.Sha256)));
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
