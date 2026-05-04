// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion;

public sealed class BuildOnlyDiagnosticIdsHandlerTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/5728")]
    public async Task TestCSharpBuildOnlyDiagnosticIdsAsync(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync("class C { }", mutatingLspWorkspace);

        var result = await testLspServer.ExecuteRequest0Async<BuildOnlyDiagnosticIdsResult>(BuildOnlyDiagnosticIdsHandler.BuildOnlyDiagnosticIdsMethodName,
            CancellationToken.None);

        var expectedBuildOnlyDiagnosticIds = GetExpectedBuildOnlyDiagnosticIds(LanguageNames.CSharp);
        AssertEx.SetEqual(expectedBuildOnlyDiagnosticIds, result.Ids);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/5728")]
    public async Task TestVisualBasicBuildOnlyDiagnosticIdsAsync(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup: """
            Class C
            End Class
            """, mutatingLspWorkspace);

        var result = await testLspServer.ExecuteRequest0Async<BuildOnlyDiagnosticIdsResult>(BuildOnlyDiagnosticIdsHandler.BuildOnlyDiagnosticIdsMethodName,
            CancellationToken.None);

        var expectedBuildOnlyDiagnosticIds = GetExpectedBuildOnlyDiagnosticIds(LanguageNames.VisualBasic);
        AssertEx.SetEqual(expectedBuildOnlyDiagnosticIds, result.Ids);
    }

    private protected override TestAnalyzerReferenceByLanguage CreateTestAnalyzersReference()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticAnalyzer>>();
        builder.Add(LanguageNames.CSharp, [DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp), new BuildOnlyAnalyzer(), new LiveAnalyzer()]);
        builder.Add(LanguageNames.VisualBasic, [DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.VisualBasic), new BuildOnlyAnalyzer(), new LiveAnalyzer()]);
        return new(builder.ToImmutableDictionary());
    }

    private static string[] GetExpectedBuildOnlyDiagnosticIds(string languageName)
    {
        using var _ = ArrayBuilder<string>.GetInstance(out var builder);

        // NOTE: 'CSharpLspBuildOnlyDiagnosticsTests' and 'VisualBasicLspBuildOnlyDiagnosticsTests' already verify that
        // the corresponding build-only diagnostic providers return expected compiler build-only diagnostic IDs.
        // So, here we just directly append 'attribute.BuildOnlyDiagnostics' from these providers to our expected build-only diagnostic IDs.
        var compilerBuildOnlyDiagnosticsType = languageName switch
        {
            LanguageNames.CSharp => typeof(CSharp.LanguageServer.CSharpLspBuildOnlyDiagnostics),
            LanguageNames.VisualBasic => typeof(VisualBasic.LanguageServer.VisualBasicLspBuildOnlyDiagnostics),
            _ => null
        };

        if (compilerBuildOnlyDiagnosticsType != null)
        {
            var attribute = compilerBuildOnlyDiagnosticsType.GetCustomAttribute<LspBuildOnlyDiagnosticsAttribute>();
            builder.AddRange(attribute.BuildOnlyDiagnostics);
        }

        builder.Add(BuildOnlyAnalyzer.Id);
        return builder.ToArray();
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    private sealed class BuildOnlyAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = "BuildOnly0001";
        private static readonly DiagnosticDescriptor s_descriptor = new(Id, "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true, customTags: [WellKnownDiagnosticTags.CompilationEnd]);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_descriptor];

        public override void Initialize(AnalysisContext context)
        {
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    private sealed class LiveAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = "Live0001";
        private static readonly DiagnosticDescriptor s_descriptor = new(Id, "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_descriptor];

        public override void Initialize(AnalysisContext context)
        {
        }
    }
}
