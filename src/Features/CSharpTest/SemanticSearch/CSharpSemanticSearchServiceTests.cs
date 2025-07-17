// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SemanticSearch;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SemanticSearch;

[UseExportProvider]
public sealed class CSharpSemanticSearchServiceTests
{
    private static readonly string s_referenceAssembliesDir = Path.Combine(Path.GetDirectoryName(typeof(CSharpSemanticSearchServiceTests).Assembly.Location!)!, "SemanticSearchRefs");
    private static readonly char[] s_lineBreaks = ['\r', '\n'];

    private static string InspectLine(int position, string text)
    {
        var lineStart = text.LastIndexOfAny(s_lineBreaks, position, position) + 1;
        var lineEnd = text.IndexOfAny(s_lineBreaks, position);
        if (lineEnd < 0)
        {
            lineEnd = text.Length;
        }

        return text[lineStart..lineEnd].Trim();
    }

    private static string Inspect(UserCodeExceptionInfo info, string query)
        => $"{info.ProjectDisplayName}: {info.Span} '{InspectLine(info.Span.Start, query)}': {info.TypeName.JoinText()}: '{info.Message}'";

    private static string DefaultWorkspaceXml => """
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document FilePath="File1.cs">
                    using System;

                    namespace N
                    {
                        public class C
                        {
                            public int F = 1;
                            public void VisibleMethod(int param) { }
                            public int P { get; }
                            public event Action E;
                        }
                    }
                </Document>
            </Project>
        </Workspace>
        """;

    private static async Task VerifyCompileAndExecuteQueryAsync(
        TestWorkspace workspace,
        string query,
        string[] expectedItems)
    {
        var symbols = new List<ISymbol>();
        var observer = new MockSemanticSearchResultsObserver() { OnDefinitionFoundImpl = symbols.Add };

        var solution = workspace.CurrentSolution;
        var service = solution.Services.GetRequiredLanguageService<ISemanticSearchService>(LanguageNames.CSharp);
        var traceSource = new TraceSource("test");

        var compileResult = service.CompileQuery(solution.Services, query, s_referenceAssembliesDir, traceSource, CancellationToken.None);
        Assert.Equal(LanguageNames.CSharp, compileResult.QueryId.Language);
        Assert.Empty(compileResult.CompilationErrors);

        var executeResult = await service.ExecuteQueryAsync(solution, compileResult.QueryId, observer, traceSource, CancellationToken.None);
        Assert.Null(executeResult.ErrorMessage);

        AssertEx.Equal(expectedItems, symbols.Select(s => s.ToTestDisplayString()).OrderBy(s => s));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task CompilationQuery()
    {
        using var workspace = TestWorkspace.Create(DefaultWorkspaceXml, composition: FeaturesTestCompositions.Features);
        await VerifyCompileAndExecuteQueryAsync(workspace, """
        static IEnumerable<ISymbol> Find(Compilation compilation)
        {
            return compilation.GlobalNamespace.GetMembers("N");
        }
        """, ["N"]);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task NamespaceQuery()
    {
        using var workspace = TestWorkspace.Create(DefaultWorkspaceXml, composition: FeaturesTestCompositions.Features);
        await VerifyCompileAndExecuteQueryAsync(workspace, """
        static IEnumerable<ISymbol> Find(INamespaceSymbol n)
        {
            return n.GetMembers("C");
        }
        """, ["N.C"]);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task NamedTypeQuery()
    {
        using var workspace = TestWorkspace.Create(DefaultWorkspaceXml, composition: FeaturesTestCompositions.Features);
        await VerifyCompileAndExecuteQueryAsync(workspace, """
        static IEnumerable<ISymbol> Find(INamedTypeSymbol type)
        {
            return type.GetMembers("F");
        }
        """, ["System.Int32 N.C.F"]);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task MethodQuery()
    {
        using var workspace = TestWorkspace.Create(DefaultWorkspaceXml, composition: FeaturesTestCompositions.Features);
        await VerifyCompileAndExecuteQueryAsync(workspace, """
        static IEnumerable<ISymbol> Find(IMethodSymbol method)
        {
            return [method];
        }
        """,
        [
            "N.C..ctor()",
            "System.Int32 N.C.P.get",
            "void N.C.E.add",
            "void N.C.E.remove",
            "void N.C.VisibleMethod(System.Int32 param)"
        ]);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task FieldQuery()
    {
        using var workspace = TestWorkspace.Create(DefaultWorkspaceXml, composition: FeaturesTestCompositions.Features);
        await VerifyCompileAndExecuteQueryAsync(workspace, """
        static IEnumerable<ISymbol> Find(IFieldSymbol field)
        {
            return [field];
        }
        """,
        [
            "System.Int32 N.C.<P>k__BackingField",
            "System.Int32 N.C.F",
        ]);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task PropertyQuery()
    {
        using var workspace = TestWorkspace.Create(DefaultWorkspaceXml, composition: FeaturesTestCompositions.Features);
        await VerifyCompileAndExecuteQueryAsync(workspace, """
        static IEnumerable<ISymbol> Find(IPropertySymbol prop)
        {
            return [prop];
        }
        """,
        [
            "System.Int32 N.C.P { get; }"
        ]);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task EventQuery()
    {
        using var workspace = TestWorkspace.Create(DefaultWorkspaceXml, composition: FeaturesTestCompositions.Features);
        await VerifyCompileAndExecuteQueryAsync(workspace, """
        static IEnumerable<ISymbol> Find(IEventSymbol e)
        {
            return [e];
        }
        """,
        [
            "event System.Action N.C.E"
        ]);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task FindReferencingSyntaxNodes()
    {
        using var workspace = TestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        class C
                        {
                            void F()
                            {
                            }
                        }

                        class D
                        {
                            void R1() => new C().F();
                            void R2() => new C().F();
                        }
                    </Document>
                </Project>
            </Workspace>
        """, composition: FeaturesTestCompositions.Features);
        await VerifyCompileAndExecuteQueryAsync(workspace, """
        static async IAsyncEnumerable<ISymbol> Find(IMethodSymbol e)
        {
            if (e.Name != "F")
            {
                yield break;
            }

            foreach (var node in e.FindReferencingSyntaxNodes())
            {
                var model = await node.SyntaxTree.GetSemanticModelAsync();
                yield return model.GetEnclosingSymbol(node.SpanStart);
            }
        }
        """,
        [
            "void D.R1()",
            "void D.R2()"
        ]);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task NullReturn()
    {
        using var workspace = TestWorkspace.Create(DefaultWorkspaceXml, composition: FeaturesTestCompositions.Features);
        await VerifyCompileAndExecuteQueryAsync(workspace, """
        static IEnumerable<ISymbol> Find(Compilation compilation)
        {
            return null;
        }
        """, expectedItems: []);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task ForcedCancellation()
    {
        using var workspace = TestWorkspace.Create(DefaultWorkspaceXml, composition: FeaturesTestCompositions.Features);

        var solution = workspace.CurrentSolution;

        var service = solution.Services.GetRequiredLanguageService<ISemanticSearchService>(LanguageNames.CSharp);

        var query = """
        static IEnumerable<ISymbol> Find(Compilation compilation)
        {
            yield return compilation.GlobalNamespace.GetMembers("N").First();

            while (true)
            {
                
            }
        }
        """;

        var cancellationSource = new CancellationTokenSource();
        var exceptions = new List<UserCodeExceptionInfo>();

        var observer = new MockSemanticSearchResultsObserver()
        {
            // cancel on first result:
            OnDefinitionFoundImpl = _ => cancellationSource.Cancel(),
            OnUserCodeExceptionImpl = exceptions.Add
        };

        var traceSource = new TraceSource("test");

        var compileResult = service.CompileQuery(solution.Services, query, s_referenceAssembliesDir, traceSource, CancellationToken.None);
        Assert.Empty(compileResult.CompilationErrors);

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => service.ExecuteQueryAsync(solution, compileResult.QueryId, observer, traceSource, cancellationSource.Token));

        Assert.Empty(exceptions);
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task StackOverflow()
    {
        using var workspace = TestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        public class C
                        {
                        }
                    </Document>
                </Project>
            </Workspace>
            """, composition: FeaturesTestCompositions.Features);

        var solution = workspace.CurrentSolution;

        var service = solution.Services.GetRequiredLanguageService<ISemanticSearchService>(LanguageNames.CSharp);

        var query = """
        static IEnumerable<ISymbol> Find(Compilation compilation)
        {
            yield return compilation.GlobalNamespace.GetMembers("C").First();
            F(0);
            void F(long x)
            {
                F(x + 1);
            }
        }
        """;

        var exceptions = new List<UserCodeExceptionInfo>();
        var observer = new MockSemanticSearchResultsObserver()
        {
            OnUserCodeExceptionImpl = exceptions.Add
        };

        var traceSource = new TraceSource("test");

        var compileResult = service.CompileQuery(solution.Services, query, s_referenceAssembliesDir, traceSource, CancellationToken.None);
        Assert.Empty(compileResult.CompilationErrors);

        var result = await service.ExecuteQueryAsync(solution, compileResult.QueryId, observer, traceSource, CancellationToken.None);
        var expectedMessage = new InsufficientExecutionStackException().Message;
        AssertEx.Equal(string.Format(FeaturesResources.Semantic_search_query_terminated_with_exception, "CSharpAssembly1", expectedMessage), result.ErrorMessage);

        var exception = exceptions.Single();
        AssertEx.Equal($"CSharpAssembly1: [179..179) 'F(x + 1);': InsufficientExecutionStackException: '{expectedMessage}'", Inspect(exception, query));

        var actualTrace = exception.StackTrace.JoinText().Split([Environment.NewLine], StringSplitOptions.None).AsSpan();

        AssertEx.SequenceEqual(
        [
            "   ...",
            .. Enumerable.Repeat($"   at Program.<<Main>$>g__F|0_1(Int64 x) in {FeaturesResources.Query}:line 7", 10),
        ], actualTrace[0..11].ToArray());

        AssertEx.SequenceEqual(
        [
            .. Enumerable.Repeat($"   at Program.<<Main>$>g__F|0_1(Int64 x) in {FeaturesResources.Query}:line 7", 10),
            $"   at Program.<<Main>$>g__Find|0_0(Compilation compilation)+MoveNext() in Query:line 4",
        ], actualTrace[^14..^3].ToArray());
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task Exception()
    {
        using var workspace = TestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        public class C
                        {
                        }
                    </Document>
                </Project>
            </Workspace>
            """, composition: FeaturesTestCompositions.Features);

        var solution = workspace.CurrentSolution;

        var service = solution.Services.GetRequiredLanguageService<ISemanticSearchService>(LanguageNames.CSharp);

        var query = """
        static IEnumerable<ISymbol> Find(Compilation compilation)
        {
            return new[] { (ISymbol)null }.Select(x => 
            {
                return F(x);
            });
        }

        static ISymbol F(ISymbol s)
        {
            var x = s.ToString();
            return s;
        }
        """;

        var exceptions = new List<UserCodeExceptionInfo>();
        var observer = new MockSemanticSearchResultsObserver()
        {
            OnUserCodeExceptionImpl = exceptions.Add
        };

        var traceSource = new TraceSource("test");

        var compileResult = service.CompileQuery(solution.Services, query, s_referenceAssembliesDir, traceSource, CancellationToken.None);
        Assert.Empty(compileResult.CompilationErrors);

        var result = await service.ExecuteQueryAsync(solution, compileResult.QueryId, observer, traceSource, CancellationToken.None);
        var expectedMessage = new NullReferenceException().Message;
        AssertEx.Equal(string.Format(FeaturesResources.Semantic_search_query_terminated_with_exception, "CSharpAssembly1", expectedMessage), result.ErrorMessage);

        var exception = exceptions.Single();
        AssertEx.Equal($"CSharpAssembly1: [190..190) 'var x = s.ToString();': NullReferenceException: '{expectedMessage}'", Inspect(exception, query));

        var actualTrace = exception.StackTrace.JoinText().Split([Environment.NewLine], StringSplitOptions.None).AsSpan()[0..2].ToArray();

        AssertEx.SequenceEqual(
        [
            $"   at Program.<<Main>$>g__F|0_1(ISymbol s) in {FeaturesResources.Query}:line 11",
            $"   at Program.<>c.<<Main>$>b__0_2(ISymbol x) in {FeaturesResources.Query}:line 5",
        ], actualTrace);
    }

    /// <summary>
    /// Checks that flow pass handles semantic query code end-to-end
    /// (specifically, module cancellation and stack overflow instrumentation).
    /// </summary>
    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task FlowPass()
    {
        using var workspace = TestWorkspace.Create(DefaultWorkspaceXml, composition: FeaturesTestCompositions.Features);

        var solution = workspace.CurrentSolution;

        var service = solution.Services.GetRequiredLanguageService<ISemanticSearchService>(LanguageNames.CSharp);

        var query = """
        using Microsoft.CodeAnalysis.CSharp;
        using Microsoft.CodeAnalysis.CSharp.Syntax;

        static IEnumerable<ISymbol> Find(IMethodSymbol method)
        {
            var syntaxReference = method.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxReference != null)
            {
                while (true)
                {
                    var syntaxNode = syntaxReference.GetSyntax() as MethodDeclarationSyntax;
                    if (syntaxNode != null)
                    {
                        yield return method;
                    }

                    break;
                }
            }
        }
        """;

        var symbols = new List<ISymbol>();
        var observer = new MockSemanticSearchResultsObserver() { OnDefinitionFoundImpl = symbols.Add };
        var traceSource = new TraceSource("test");

        var compileResult = service.CompileQuery(solution.Services, query, s_referenceAssembliesDir, traceSource, CancellationToken.None);
        Assert.Empty(compileResult.CompilationErrors);

        var result = await service.ExecuteQueryAsync(solution, compileResult.QueryId, observer, traceSource, CancellationToken.None);

        Assert.Null(result.ErrorMessage);
        AssertEx.Equal(["void N.C.VisibleMethod(System.Int32 param)"], symbols.Select(s => s.ToTestDisplayString()));
    }
}
