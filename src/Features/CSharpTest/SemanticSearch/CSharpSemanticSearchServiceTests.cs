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
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindUsages;
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

    private static string Inspect(DefinitionItem def)
        => string.Join("", def.DisplayParts.Select(p => p.Text));

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

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task SimpleQuery()
    {
        using var workspace = TestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        namespace N
                        {
                            public partial class C
                            {
                                public void VisibleMethod() { }
                            }
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
            return compilation.GlobalNamespace.GetMembers("N");
        }
        """;

        var results = new List<DefinitionItem>();
        var observer = new MockSemanticSearchResultsObserver() { OnDefinitionFoundImpl = results.Add };
        var traceSource = new TraceSource("test");

        var options = workspace.GlobalOptions.GetClassificationOptionsProvider();
        var result = await service.ExecuteQueryAsync(solution, query, s_referenceAssembliesDir, observer, options, traceSource, CancellationToken.None);

        Assert.Null(result.ErrorMessage);
        AssertEx.Equal(["namespace N"], results.Select(Inspect));
    }

    [ConditionalFact(typeof(CoreClrOnly))]
    public async Task ForcedCancellation()
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
        var options = workspace.GlobalOptions.GetClassificationOptionsProvider();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.ExecuteQueryAsync(solution, query, s_referenceAssembliesDir, observer, options, traceSource, cancellationSource.Token));

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
        var options = workspace.GlobalOptions.GetClassificationOptionsProvider();

        var result = await service.ExecuteQueryAsync(solution, query, s_referenceAssembliesDir, observer, options, traceSource, CancellationToken.None);
        var expectedMessage = new InsufficientExecutionStackException().Message;
        AssertEx.Equal(string.Format(FeaturesResources.Semantic_search_query_terminated_with_exception, "CSharpAssembly1", expectedMessage), result.ErrorMessage);

        var exception = exceptions.Single();
        AssertEx.Equal($"CSharpAssembly1: [179..179) 'F(x + 1);': InsufficientExecutionStackException: '{expectedMessage}'", Inspect(exception, query));

        AssertEx.Equal(
            "   ..." + Environment.NewLine +
            string.Join(Environment.NewLine, Enumerable.Repeat($"   at Program.<<Main>$>g__F|0_1(Int64 x) in {FeaturesResources.Query}:line 7", 31)) + Environment.NewLine +
            $"   at Program.<<Main>$>g__Find|0_0(Compilation compilation)+MoveNext() in Query:line 4" + Environment.NewLine,
            exception.StackTrace.JoinText());
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
        var options = workspace.GlobalOptions.GetClassificationOptionsProvider();

        var result = await service.ExecuteQueryAsync(solution, query, s_referenceAssembliesDir, observer, options, traceSource, CancellationToken.None);
        var expectedMessage = new NullReferenceException().Message;
        AssertEx.Equal(string.Format(FeaturesResources.Semantic_search_query_terminated_with_exception, "CSharpAssembly1", expectedMessage), result.ErrorMessage);

        var exception = exceptions.Single();
        AssertEx.Equal($"CSharpAssembly1: [190..190) 'var x = s.ToString();': NullReferenceException: '{expectedMessage}'", Inspect(exception, query));
        AssertEx.Equal(
            $"   at Program.<<Main>$>g__F|0_1(ISymbol s) in {FeaturesResources.Query}:line 11" + Environment.NewLine +
            $"   at Program.<>c.<<Main>$>b__0_2(ISymbol x) in {FeaturesResources.Query}:line 5" + Environment.NewLine +
            $"   at System.Linq.Enumerable.ArraySelectIterator`2.MoveNext()" + Environment.NewLine,
            exception.StackTrace.JoinText());
    }
}
