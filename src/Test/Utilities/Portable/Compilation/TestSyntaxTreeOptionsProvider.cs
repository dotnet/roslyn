﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    public sealed class TestSyntaxTreeOptionsProvider : SyntaxTreeOptionsProvider
    {
        private readonly Dictionary<SyntaxTree, Dictionary<string, ReportDiagnostic>>? _options;
        private readonly Dictionary<SyntaxTree, bool?>? _isGenerated;

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
            _isGenerated = null;
        }

        public TestSyntaxTreeOptionsProvider(
            params (SyntaxTree, (string, ReportDiagnostic)[])[] options)
            : this(CaseInsensitiveComparison.Comparer, options)
        { }

        public TestSyntaxTreeOptionsProvider(
            SyntaxTree tree, params (string, ReportDiagnostic)[] options)
            : this(new[] { (tree, options) })
        { }

        public TestSyntaxTreeOptionsProvider(
            params (SyntaxTree, bool? isGenerated)[] isGenerated
        )
        {
            _options = null;
            _isGenerated = isGenerated.ToDictionary(
                x => x.Item1,
                x => x.Item2
            );
        }

        public override bool? IsGenerated(SyntaxTree tree)
        => _isGenerated != null && _isGenerated.TryGetValue(tree, out var val) ? val : null;

        public override bool TryGetDiagnosticValue(
            SyntaxTree tree,
            string diagnosticId,
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
    }
}
