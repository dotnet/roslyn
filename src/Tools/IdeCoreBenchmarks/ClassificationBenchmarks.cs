// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
        private readonly int _iterationCount = 10;

        private Document _document;
        private Solution _solution;
        private TextSpan _span;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var csFilePath = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable\Generated\BoundNodes.xml.Generated.cs");

            if (!File.Exists(csFilePath))
            {
                throw new ArgumentException();
            }

            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            var text = File.ReadAllText(csFilePath);

            _solution = new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, "ProjectName", "AssemblyName", LanguageNames.CSharp)
                .AddDocument(documentId, "DocumentName", text);

            var document = _solution.GetDocument(documentId);
            var root = document.GetSyntaxRootAsync(CancellationToken.None).Result.WithAdditionalAnnotations(Formatter.Annotation);
            _solution = _solution.WithDocumentSyntaxRoot(documentId, root);
            _document = _solution.GetDocument(documentId);
            var project = _solution.Projects.First();
            _span = new TextSpan(0, text.Length);
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
            for (var i = 0; i < _iterationCount; i++)
            {
                _ = GetSemanticClassificationsAsync(_document, _span);
            }
        }
    }
}
