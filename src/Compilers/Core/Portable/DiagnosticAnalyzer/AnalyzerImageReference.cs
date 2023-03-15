// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents an in-memory analyzer reference image.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public sealed class AnalyzerImageReference : AnalyzerReference
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
        private readonly string? _fullPath;
        private readonly string? _display;
        private readonly string _id;

        public AnalyzerImageReference(ImmutableArray<DiagnosticAnalyzer> analyzers, string? fullPath = null, string? display = null)
        {
            if (analyzers.Any(static a => a == null))
            {
                throw new ArgumentException("Cannot have null-valued analyzer", nameof(analyzers));
            }

            _analyzers = analyzers;
            _fullPath = fullPath;
            _display = display;
            _id = Guid.NewGuid().ToString();
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
        {
            return _analyzers;
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
        {
            return _analyzers;
        }

        public override string? FullPath
        {
            get
            {
                return _fullPath;
            }
        }

        public override string Display
        {
            get
            {
                return _display ?? _fullPath ?? CodeAnalysisResources.InMemoryAssembly;
            }
        }

        public override object Id
        {
            get
            {
                return _id;
            }
        }

        private string GetDebuggerDisplay()
        {
            var sb = new StringBuilder();
            sb.Append("Assembly");

            if (_fullPath != null)
            {
                sb.Append(" Path='");
                sb.Append(_fullPath);
                sb.Append("'");
            }

            if (_display != null)
            {
                sb.Append(" Display='");
                sb.Append(_display);
                sb.Append("'");
            }

            return sb.ToString();
        }
    }
}
