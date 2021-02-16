// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace IdeCoreBenchmarks
{
    public class ProjectOperationBenchmarks
    {
        private static readonly SourceText s_newText = SourceText.From("text");

        [MemoryDiagnoser]
        public class IterateDocuments
        {
            private Workspace _workspace;
            private Project _emptyProject;
            private Project _hundredProject;
            private Project _thousandsProject;

            public IterateDocuments()
            {
                // These fields are initialized in GlobalSetup
                _workspace = null!;
                _emptyProject = null!;
                _hundredProject = null!;
                _thousandsProject = null!;
            }

            [Params(0, 100, 10000)]
            public int DocumentCount { get; set; }

            private Project Project
            {
                get
                {
                    return DocumentCount switch
                    {
                        0 => _emptyProject,
                        100 => _hundredProject,
                        10000 => _thousandsProject,
                        _ => throw new NotSupportedException($"'{nameof(DocumentCount)}' is out of range"),
                    };
                }
            }

            [GlobalSetup]
            public void GlobalSetup()
            {
                _workspace = new AdhocWorkspace();

                var solution = _workspace.CurrentSolution;
                _emptyProject = CreateProject(ref solution, name: "A", documentCount: 0);
                _hundredProject = CreateProject(ref solution, name: "A", documentCount: 100);
                _thousandsProject = CreateProject(ref solution, name: "A", documentCount: 10000);

                static Project CreateProject(ref Solution solution, string name, int documentCount)
                {
                    var projectId = ProjectId.CreateNewId(name);
                    solution = solution.AddProject(projectId, name, name, LanguageNames.CSharp);

                    var emptySourceText = SourceText.From("", Encoding.UTF8);
                    for (var i = 0; i < documentCount; i++)
                    {
                        var documentName = $"{i}.cs";
                        var documentId = DocumentId.CreateNewId(projectId, documentName);
                        solution = solution.AddDocument(documentId, documentName, emptySourceText);
                    }

                    return solution.GetRequiredProject(projectId);
                }
            }

            [Benchmark(Description = "Project.DocumentIds")]
            public int DocumentIds()
            {
                var count = 0;
                foreach (var _ in Project.DocumentIds)
                {
                    count++;
                }

                return count;
            }

            [Benchmark(Description = "Project.Documents")]
            public int Documents()
            {
                var count = 0;
                foreach (var _ in Project.Documents)
                {
                    count++;
                }

                return count;
            }

            [Benchmark(Description = "Solution.WithDocumentText")]
            public void WithDocumentText()
            {
                var solution = Project.Solution;
                var documentId = Project.DocumentIds.FirstOrDefault();
                if (documentId != null)
                {
                    var _ = solution.WithDocumentText(documentId, s_newText);
                }
            }
        }
    }
}
