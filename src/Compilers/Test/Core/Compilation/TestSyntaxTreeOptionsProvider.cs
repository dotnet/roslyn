// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.AnalyzerConfig;

namespace Roslyn.Utilities
{
    public sealed class TestSyntaxTreeOptionsProvider : SyntaxTreeOptionsProvider
    {
        private readonly Dictionary<SyntaxTree, Dictionary<string, ReportDiagnostic>>? _options;
        private readonly Dictionary<SyntaxTree, GeneratedKind>? _isGenerated;
        private readonly Dictionary<string, ReportDiagnostic>? _globalOptions;
        public TestSyntaxTreeOptionsProvider(
            IEqualityComparer<string> comparer,
            (string? key, ReportDiagnostic diagnostic) globalOption,
            params (SyntaxTree, (string, ReportDiagnostic)[])[] options)
        {
            _options = options.ToDictionary(
                x => x.Item1,
                x => x.Item2.ToDictionary(
                    x => x.Item1,
                    x => x.Item2,
                    comparer)
            );
            if (globalOption.key is object)
            {
                _globalOptions = new Dictionary<string, ReportDiagnostic>(Section.PropertiesKeyComparer) { { globalOption.key, globalOption.diagnostic } };
            }
            _isGenerated = null;
        }

        public TestSyntaxTreeOptionsProvider(
            params (SyntaxTree, (string, ReportDiagnostic)[])[] options)
            : this(CaseInsensitiveComparison.Comparer, globalOption: default, options)
        { }

        public TestSyntaxTreeOptionsProvider(
            (string, ReportDiagnostic) globalOption,
            params (SyntaxTree, (string, ReportDiagnostic)[])[] options)
            : this(CaseInsensitiveComparison.Comparer, globalOption: globalOption, options)
        { }

        public TestSyntaxTreeOptionsProvider(
            SyntaxTree tree, params (string, ReportDiagnostic)[] options)
            : this(globalOption: default, new[] { (tree, options) })
        { }

        public TestSyntaxTreeOptionsProvider(
            params (SyntaxTree, GeneratedKind isGenerated)[] isGenerated
        )
        {
            _options = null;
            _isGenerated = isGenerated.ToDictionary(
                x => x.Item1,
                x => x.isGenerated
            );
        }

        public override GeneratedKind IsGenerated(SyntaxTree tree, CancellationToken cancellationToken)
            => _isGenerated != null && _isGenerated.TryGetValue(tree, out var kind) ? kind : GeneratedKind.Unknown;

        public override bool TryGetDiagnosticValue(
            SyntaxTree tree,
            string diagnosticId,
            CancellationToken cancellationToken,
            out ReportDiagnostic severity)
        {
            if (_options != null &&
                _options.TryGetValue(tree, out var diags)
                && diags.TryGetValue(diagnosticId, out severity))
            {
                return true;
            }
            severity = ReportDiagnostic.Default;
            return false;
        }

        public override bool TryGetGlobalDiagnosticValue(string diagnosticId, CancellationToken cancellationToken, out ReportDiagnostic severity)
        {
            if (_globalOptions is object &&
                _globalOptions.TryGetValue(diagnosticId, out severity))
            {
                return true;
            }
            severity = ReportDiagnostic.Default;
            return false;
        }
    }
}
