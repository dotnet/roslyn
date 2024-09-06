// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AbstractBuiltInCodeStyleDiagnosticAnalyzer;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using Xunit;

public sealed class AbstractBuiltInCodeStyleDiagnosticAnalyzerTests
{
    [Fact]
    public void VerifyDiagnosticDescriptorOrderingMaintained()
    {
        var ids = Enumerable.Range(10, 20).Select(item => "IDE_" + item);

        var analyzer = new TestAnalyzer(ids);

        Assert.Equal(analyzer.SupportedDiagnostics.Select(static diagnostic => diagnostic.Id), ids);
    }

    private sealed class TestAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public TestAnalyzer(IEnumerable<string> ids)
            : base(CreateSupportedDiagnosticsWithOptionsFromIds(ids))
        {
        }

        private static ImmutableArray<(DiagnosticDescriptor, ImmutableHashSet<IOption2>)> CreateSupportedDiagnosticsWithOptionsFromIds(IEnumerable<string> ids)
        {
            var builder = ImmutableArray.CreateBuilder<(DiagnosticDescriptor, ImmutableHashSet<IOption2>)>();
            foreach (var id in ids)
            {
                var descriptor = CreateDescriptorWithId(
                    id: id,
                    enforceOnBuild: EnforceOnBuild.Never,
                    hasAnyCodeStyleOption: false,
                    title: string.Empty);

                builder.Add((descriptor, []));
            }

            return builder.ToImmutableAndClear();
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => throw new System.NotImplementedException();

        protected override void InitializeWorker(AnalysisContext context)
        {
        }
    }
}
