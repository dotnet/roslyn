// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace IdeCoreBenchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, invocationCount: 1)]
public class SwitchStatementBenchmarks
{
    [Params(100, 1000, 5000, 10000)]
    public int SwitchCount
    {
        get;
        set;
    }

    private static string CreateSourceFile(int switchCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            """
            class TestClass
            {
              int TestMethod(string arg)
              {
                switch (arg)
                {
            """);

        for (var i = 0; i < switchCount; i++)
        {
            builder.AppendLine(
                $"""
                      case "Text{i}": return {i};
                """);
        }

        builder.AppendLine(
            """
                  default: return 0;
                }
              }
            }

            """);

        return builder.ToString();
    }

    [Benchmark]
    public object BindFile()
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        using var workspace = new AdhocWorkspace();

        var solution = workspace.CurrentSolution
            .AddProject(projectId, "ProjectName", "AssemblyName", LanguageNames.CSharp)
            .AddDocument(documentId, "DocumentName", CreateSourceFile(SwitchCount));

        solution = solution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        solution = solution.WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var project = solution.Projects.First();
        var compilation = project.GetCompilationAsync().Result;
        return compilation.EmitToStream();
    }
}
