// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace IdeCoreBenchmarks;

/// <summary>
/// Benchmarks comparing three approaches for detecting whether a void method is likely used as
/// an event handler, in the context of the "Make method async" code fix:
///
///   1. <b>NoDetection (prod baseline)</b>: performs no event-handler detection at all —
///      equivalent to the current production code on <c>main</c>, which skips this check
///      entirely and always returns <see langword="false"/>.
///
///   2. <b>FAR-based</b>: calls <see cref="SymbolFinder.FindReferencesAsync(ISymbol, Solution, CancellationToken)"/> and walks each
///      reference location looking for an <see cref="IEventAssignmentOperation"/> ancestor.
///      This is the semantically accurate (but expensive) approach.
///
///   3. <b>Signature-based heuristic</b>: inspects only the method's parameter types
///      (<c>object, EventArgs</c>) without touching the call graph.
///      This is a cheap approximation.
///
/// Two parameterized scenarios stress-test how method-name frequency affects FAR cost:
///
///   <list type="bullet">
///     <item><b>CommonName</b> – the target method is named <c>OnClick</c>; the solution
///       contains <see cref="ClassCount"/> other classes that also define <c>OnClick</c>,
///       so FAR has many candidate locations to walk.</item>
///     <item><b>UniqueName</b> – the method has a name that appears nowhere else in the
///       solution, so FAR can short-circuit very quickly.</item>
///   </list>
///
/// Run with:
/// <code>
///   dotnet run -c Release --project src/Tools/IdeCoreBenchmarks/IdeCoreBenchmarks.csproj \
///       -- --filter *MakeMethodAsynchronousCodeFix*
/// </code>
/// </summary>
[MemoryDiagnoser]
public class MakeMethodAsynchronousCodeFixBenchmarks
{
    // Number of extra classes that carry a same-named method in the CommonName scenario.
    private const int ClassCount = 100;

    // Name used in the CommonName scenario – intentionally common for event handlers.
    private const string CommonMethodName = "OnClick";

    // Name used in the UniqueName scenario – deliberately unguessable.
    private const string UniqueMethodName = "OnZxYwUnique_EventHandler_Bench_9a3f2c";

    /// <summary>
    /// Selects the scenario to benchmark.
    /// "CommonName" stresses the FAR approach by flooding the index with many same-named methods.
    /// "UniqueName" mimics a typical event handler whose name is unique across the solution.
    /// </summary>
    [Params("CommonName", "UniqueName")]
    public string Scenario { get; set; } = "CommonName";

    private Solution _solution = null!;
    private IMethodSymbol _methodSymbol = null!;
    private Compilation _compilation = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var methodName = Scenario == "CommonName" ? CommonMethodName : UniqueMethodName;

        var projectId = ProjectId.CreateNewId();
        var targetDocId = DocumentId.CreateNewId(projectId);
        var subscriberDocId = DocumentId.CreateNewId(projectId);

        // Build the target class that owns the method we'll probe.
        var targetSource = BuildTargetClassSource(methodName);

        // Build extra classes that also contain a same-named method (only relevant for CommonName).
        var extraDocIds = Enumerable.Range(1, ClassCount)
            .Select(_ => DocumentId.CreateNewId(projectId))
            .ToArray();
        var extraSources = Enumerable.Range(1, ClassCount)
            .Select(i => BuildExtraClassSource(methodName, i))
            .ToArray();

        // Build the subscriber that wires the target method to an event so FAR finds
        // at least one IEventAssignmentOperation.
        var subscriberSource = BuildSubscriberSource(methodName);

        var solution = new AdhocWorkspace().CurrentSolution
            .AddProject(projectId, "TestProject", "TestAssembly", LanguageNames.CSharp)
            .WithProjectCompilationOptions(
                projectId,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            // Add mscorlib / System.Runtime so EventArgs resolves.
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(System.EventArgs).Assembly.Location))
            .AddDocument(targetDocId, "Target.cs", targetSource)
            .AddDocument(subscriberDocId, "Subscriber.cs", subscriberSource);

        for (var i = 0; i < ClassCount; i++)
            solution = solution.AddDocument(extraDocIds[i], $"Extra{i + 1}.cs", extraSources[i]);

        _solution = solution;

        var project = _solution.GetProject(projectId)!;
        _compilation = project.GetCompilationAsync().Result!;

        // Locate the method symbol inside the TargetHandler class.
        var targetType = _compilation.GetTypeByMetadataName("TargetHandler")
            ?? throw new System.InvalidOperationException("TargetHandler type not found in compilation.");
        _methodSymbol = (IMethodSymbol)(targetType.GetMembers(methodName).Single());
    }

    /// <summary>
    /// Measures the prod-main baseline: no event-handler detection at all.
    /// This is what the current production code on <c>main</c> does — it skips
    /// the check entirely, effectively treating every method as a non-event-handler.
    /// </summary>
    [Benchmark(Baseline = true)]
    public bool NoDetection() => false;

    /// <summary>
    /// Measures the original FAR-based event-handler detection.
    /// Calls <see cref="SymbolFinder.FindReferencesAsync(ISymbol, Solution, CancellationToken)"/> and walks each reference
    /// for an <see cref="IEventAssignmentOperation"/> ancestor.
    /// </summary>
    [Benchmark]
    public async Task<bool> FarBasedDetection()
        => await IsReferencedAsEventHandlerAsync(_solution, _methodSymbol, CancellationToken.None).ConfigureAwait(false);

    /// <summary>
    /// Measures the signature-based heuristic:
    /// checks only that the method has exactly two parameters of types
    /// <c>object</c> and a type derived from <see cref="System.EventArgs"/>.
    /// </summary>
    [Benchmark]
    public bool SignatureBasedDetection()
        => IsLikelyEventHandlerMethodSignature(_methodSymbol, _compilation);

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _solution = null!;
        _methodSymbol = null!;
        _compilation = null!;
    }

    // -------------------------------------------------------------------------
    // Implementations being compared (kept in sync with the production code)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Original production implementation (removed in favour of the heuristic).
    /// Finds all references to <paramref name="methodSymbol"/> and returns <see langword="true"/>
    /// if any reference is an event-handler assignment.
    /// </summary>
    private static async Task<bool> IsReferencedAsEventHandlerAsync(
        Solution solution,
        IMethodSymbol methodSymbol,
        CancellationToken cancellationToken)
    {
        var references = await SymbolFinder.FindReferencesAsync(
            methodSymbol,
            solution,
            cancellationToken).ConfigureAwait(false);

        foreach (var referencingSymbol in references)
        {
            foreach (var location in referencingSymbol.Locations)
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

    /// <summary>
    /// Current production heuristic: O(1) check on parameter types only.
    /// </summary>
    private static bool IsLikelyEventHandlerMethodSignature(IMethodSymbol methodSymbol, Compilation compilation)
    {
        if (!methodSymbol.ReturnsVoid || methodSymbol.MethodKind != MethodKind.Ordinary || methodSymbol.Parameters.Length != 2)
            return false;

        if (methodSymbol.Parameters[0].Type.SpecialType != SpecialType.System_Object)
            return false;

        var eventArgsType = compilation.GetTypeByMetadataName("System.EventArgs");
        return eventArgsType is not null &&
            methodSymbol.Parameters[1].Type.InheritsFromOrEquals(eventArgsType);
    }

    // -------------------------------------------------------------------------
    // Source-code generation helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Generates the class whose method is the benchmark target.
    /// The method is wired to a button's event inside <c>Subscribe</c>.
    /// </summary>
    private static string BuildTargetClassSource(string methodName)
        => $$"""
            using System;

            public class MyButton { public event EventHandler Clicked; }

            /// <summary>This is the class whose method we probe in the benchmark.</summary>
            public class TargetHandler
            {
                public void {{methodName}}(object sender, EventArgs e) { }

                public void Subscribe(MyButton btn) { btn.Clicked += {{methodName}}; }
            }
            """;

    /// <summary>
    /// Generates an extra class that also declares a method with the same name,
    /// adding noise to the FAR index for the CommonName scenario.
    /// </summary>
    private static string BuildExtraClassSource(string methodName, int index)
        => $$"""
            using System;

            public class ExtraHandler{{index}}
            {
                public void {{methodName}}(object sender, EventArgs e) { }
            }
            """;

    /// <summary>
    /// Generates a subscriber class that references the target method via event
    /// assignment so FAR can always find at least one <see cref="IEventAssignmentOperation"/>.
    /// </summary>
    private static string BuildSubscriberSource(string methodName)
        => $$"""
            using System;

            public class Subscriber
            {
                private readonly TargetHandler _handler = new TargetHandler();
                private readonly MyButton _btn = new MyButton();

                public void Wire() { _btn.Clicked += _handler.{{methodName}}; }
            }
            """;
}
