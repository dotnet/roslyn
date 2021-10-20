// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditorConfigSettings.DataProvider
{
    public partial class DataProviderTests
    {
        private class MockAnalyzerReference : AnalyzerReference
        {
            public readonly CodeFixProvider? Fixer;
            public readonly ImmutableArray<DiagnosticAnalyzer> Analyzers;

            private static readonly CodeFixProvider s_defaultFixer = new MockFixer();
            private static readonly ImmutableArray<DiagnosticAnalyzer> s_defaultAnalyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new MockDiagnosticAnalyzer());

            public MockAnalyzerReference(CodeFixProvider? fixer, ImmutableArray<DiagnosticAnalyzer> analyzers)
            {
                Fixer = fixer;
                Analyzers = analyzers;
            }

            public MockAnalyzerReference()
                : this(s_defaultFixer, s_defaultAnalyzers)
            {
            }

            public MockAnalyzerReference(CodeFixProvider? fixer)
                : this(fixer, s_defaultAnalyzers)
            {
            }

            public override string Display => "MockAnalyzerReference";

            public override string FullPath => string.Empty;

            public override object Id => "MockAnalyzerReference";

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
                => Analyzers;

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
                => Analyzers;

            public ImmutableArray<CodeFixProvider> GetFixers()
                => Fixer != null ? ImmutableArray.Create(Fixer) : ImmutableArray<CodeFixProvider>.Empty;

            public class MockFixer : CodeFixProvider
            {
                public const string Id = "MyDiagnostic";
                public bool Called;
                public int ContextDiagnosticsCount;

                public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Id);

                public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
                {
                    Called = true;
                    ContextDiagnosticsCount = context.Diagnostics.Length;
                    return Task.CompletedTask;
                }
            }

            public class MockDiagnosticAnalyzer : DiagnosticAnalyzer
            {
                public MockDiagnosticAnalyzer(ImmutableArray<(string id, string category)> reportedDiagnosticIdsWithCategories)
                    => SupportedDiagnostics = CreateSupportedDiagnostics(reportedDiagnosticIdsWithCategories);

                public MockDiagnosticAnalyzer(string diagnosticId, string category)
                    : this(ImmutableArray.Create((diagnosticId, category)))
                {
                }

                public MockDiagnosticAnalyzer(ImmutableArray<string> reportedDiagnosticIds)
                    : this(reportedDiagnosticIds.SelectAsArray(id => (id, "InternalCategory")))
                {
                }

                public MockDiagnosticAnalyzer()
                    : this(ImmutableArray.Create(MockFixer.Id))
                {
                }

                private static ImmutableArray<DiagnosticDescriptor> CreateSupportedDiagnostics(ImmutableArray<(string id, string category)> reportedDiagnosticIdsWithCategories)
                {
                    var builder = ArrayBuilder<DiagnosticDescriptor>.GetInstance();
                    foreach (var (diagnosticId, category) in reportedDiagnosticIdsWithCategories)
                    {
                        var descriptor = new DiagnosticDescriptor(diagnosticId, "MockDiagnostic", "MockDiagnostic", category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
                        builder.Add(descriptor);
                    }

                    return builder.ToImmutableAndFree();
                }

                public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

                public override void Initialize(AnalysisContext context)
                {
                    context.RegisterSyntaxTreeAction(c =>
                    {
                        foreach (var descriptor in SupportedDiagnostics)
                        {
                            c.ReportDiagnostic(Diagnostic.Create(descriptor, c.Tree.GetLocation(TextSpan.FromBounds(0, 0))));
                        }
                    });
                }
            }
        }
    }
}
