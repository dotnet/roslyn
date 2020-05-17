// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    public sealed class TestSyntaxTreeOptionsProvider : SyntaxTreeOptionsProvider
    {
        private readonly Dictionary<SyntaxTree, Dictionary<string, ReportDiagnostic>> _options;

        public TestSyntaxTreeOptionsProvider(
            IEqualityComparer<string> comparer,
            params (SyntaxTree, (string, ReportDiagnostic)[])[] options)
        {
            _options = options.ToDictionary(
                x => x.Item1,
                x => x.Item2.ToDictionary(
                    x => x.Item1,
                    x => x.Item2,
                    comparer)
            );
        }

        public TestSyntaxTreeOptionsProvider(
            params (SyntaxTree, (string, ReportDiagnostic)[])[] options)
            : this(CaseInsensitiveComparison.Comparer, options)
        { }

        public TestSyntaxTreeOptionsProvider(
            SyntaxTree tree, params (string, ReportDiagnostic)[] options)
            : this(new[] { (tree, options) })
        { }

        public override bool? IsGenerated(SyntaxTree tree) => null;

        public override bool TryGetDiagnosticValue(
            SyntaxTree tree,
            string diagnosticId,
            out ReportDiagnostic severity)
        {
            if (_options.TryGetValue(tree, out var diags)
                && diags.TryGetValue(diagnosticId, out severity))
            {
                return true;
            }
            severity = ReportDiagnostic.Default;
            return false;
        }
    }
}
