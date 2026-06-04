// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace IdeCoreBenchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 1, iterationCount: 5, invocationCount: 1)]
public class MakeMethodAsynchronousBenchmarks
{
    private const string TargetTypeName = "TargetType";
    private const string TargetMethodName = "Target";
    private const int ConsumerProjectCount = 12;
    private const int DocumentsPerProject = 30;

    private AdhocWorkspace _eventHandlerWorkspace = null!;
    private Document _eventHandlerDocument = null!;
    private IMethodSymbol _eventHandlerMethod = null!;
    private AdhocWorkspace _nonEventHandlerWorkspace = null!;
    private Document _nonEventHandlerDocument = null!;
    private IMethodSymbol _nonEventHandlerMethod = null!;

    [IterationSetup]
    public void IterationSetup()
    {
        IterationCleanup();
        (_nonEventHandlerWorkspace, _nonEventHandlerDocument, _nonEventHandlerMethod) = CreateBenchmarkState(referencedAsEventHandler: false);
        (_eventHandlerWorkspace, _eventHandlerDocument, _eventHandlerMethod) = CreateBenchmarkState(referencedAsEventHandler: true);
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _nonEventHandlerWorkspace?.Dispose();
        _eventHandlerWorkspace?.Dispose();
    }

    [Benchmark(Baseline = true, Description = "No event assignment")]
    public bool NoEventAssignment()
        => IsReferencedAsEventHandlerAsync(_nonEventHandlerDocument, _nonEventHandlerMethod, CancellationToken.None).GetAwaiter().GetResult();

    [Benchmark(Description = "With event assignment")]
    public bool WithEventAssignment()
        => IsReferencedAsEventHandlerAsync(_eventHandlerDocument, _eventHandlerMethod, CancellationToken.None).GetAwaiter().GetResult();

    private static (AdhocWorkspace workspace, Document document, IMethodSymbol methodSymbol) CreateBenchmarkState(bool referencedAsEventHandler)
    {
        var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution;

        var targetProjectId = ProjectId.CreateNewId("TargetProject");
        solution = solution
            .AddProject(CreateProjectInfo(targetProjectId, "TargetProject"))
            .AddMetadataReference(targetProjectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var targetDocumentId = DocumentId.CreateNewId(targetProjectId, "Target.cs");
        solution = solution.AddDocument(targetDocumentId, "Target.cs", CreateTargetSource());

        for (var projectIndex = 0; projectIndex < ConsumerProjectCount; projectIndex++)
        {
            var projectId = ProjectId.CreateNewId($"Consumer{projectIndex}");
            solution = solution
                .AddProject(CreateProjectInfo(projectId, $"Consumer{projectIndex}"))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddProjectReference(projectId, new ProjectReference(targetProjectId));

            for (var documentIndex = 0; documentIndex < DocumentsPerProject; documentIndex++)
            {
                var isLastDocument = projectIndex == ConsumerProjectCount - 1 && documentIndex == DocumentsPerProject - 1;
                var documentId = DocumentId.CreateNewId(projectId, $"Consumer{projectIndex}_{documentIndex}.cs");
                solution = solution.AddDocument(
                    documentId,
                    $"Consumer{projectIndex}_{documentIndex}.cs",
                    CreateConsumerSource(projectIndex, documentIndex, referencedAsEventHandler && isLastDocument));
            }
        }

        var targetDocument = solution.GetRequiredDocument(targetDocumentId);
        var semanticModel = targetDocument.GetRequiredSemanticModelAsync(CancellationToken.None).GetAwaiter().GetResult();
        var syntaxRoot = targetDocument.GetRequiredSyntaxRootAsync(CancellationToken.None).GetAwaiter().GetResult();
        var methodDeclaration = syntaxRoot
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(static m => m.Identifier.ValueText == TargetMethodName);

        var methodSymbol = (IMethodSymbol)semanticModel.GetRequiredDeclaredSymbol(methodDeclaration, CancellationToken.None);
        return (workspace, targetDocument, methodSymbol);
    }

    private static ProjectInfo CreateProjectInfo(ProjectId projectId, string name)
        => ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            name,
            name,
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static string CreateTargetSource()
        => """
        using System;

        public sealed class TargetType
        {
            public event EventHandler E;

            public void Target(object sender, EventArgs e)
            {
            }

            public void Raise()
                => E?.Invoke(this, EventArgs.Empty);
        }
        """;

    private static string CreateConsumerSource(int projectIndex, int documentIndex, bool subscribeToEvent)
    {
        var builder = new StringBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine();
        builder.AppendLine($"internal sealed class Consumer_{projectIndex}_{documentIndex}");
        builder.AppendLine("{");
        builder.AppendLine("    public void M()");
        builder.AppendLine("    {");
        builder.AppendLine($"        var target = new {TargetTypeName}();");

        if (subscribeToEvent)
            builder.AppendLine($"        target.E += target.{TargetMethodName};");
        else
            builder.AppendLine($"        target.{TargetMethodName}(this, EventArgs.Empty);");

        builder.AppendLine("    }");
        builder.AppendLine();

        for (var i = 0; i < 20; i++)
        {
            builder.AppendLine($"    private int Helper{i}(int value)");
            builder.AppendLine("    {");
            builder.AppendLine($"        return value + {projectIndex + documentIndex + i};");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static async Task<bool> IsReferencedAsEventHandlerAsync(Document document, IMethodSymbol methodSymbol, CancellationToken cancellationToken)
    {
        var references = await SymbolFinder.FindReferencesAsync(
            methodSymbol,
            document.Project.Solution,
            cancellationToken).ConfigureAwait(false);

        foreach (var referencedSymbol in references)
        {
            foreach (var location in referencedSymbol.Locations)
            {
                if (location.IsImplicit || location.Document is null)
                    continue;

                var syntaxRoot = await location.Document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var syntaxNode = syntaxRoot.FindNode(location.Location.SourceSpan, getInnermostNodeForTie: true);
                var semanticModel = await location.Document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var operation = semanticModel.GetOperation(syntaxNode, cancellationToken);
                for (var current = operation; current != null; current = current.Parent)
                {
                    if (current is IEventAssignmentOperation)
                        return true;
                }
            }
        }

        return false;
    }
}
