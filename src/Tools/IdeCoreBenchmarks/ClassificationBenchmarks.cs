// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class ClassificationBenchmarks
    {
        private Solution _solution;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var csFilePath = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable");

            var files = Directory.GetFiles(csFilePath, "*.cs", SearchOption.AllDirectories);

            var projectId = ProjectId.CreateNewId();
            _solution = new AdhocWorkspace().CurrentSolution
               .AddProject(projectId, "ProjectName", "AssemblyName", LanguageNames.CSharp);
            for (var i = 0; i < files.Length; i++)
            {
                if (!File.Exists(files[i]))
                {
                    throw new ArgumentException();
                }

                var documentId = DocumentId.CreateNewId(projectId);
                var text = File.ReadAllText(files[i]);
                _solution = _solution.AddDocument(documentId, "File" + i, text);
            }

            var project = _solution.Projects.First();
        }

        protected static async Task<ImmutableArray<ClassifiedSpan>> GetSemanticClassificationsAsync(Document document, TextSpan span)
        {
            var service = document.GetRequiredLanguageService<IClassificationService>();
            var options = ClassificationOptions.From(document.Project);
            using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var result);
            await service.AddSemanticClassificationsAsync(document, span, options, result, CancellationToken.None);
            return result.ToImmutable();
        }

        [Benchmark]
        public void ClassifyDocument()
        {
            foreach (var project in _solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var text = document.GetTextAsync().Result.ToString();
                    var span = new TextSpan(0, text.Length);
                    _ = GetSemanticClassificationsAsync(document, span);
                }
            }

        }
    }
}
