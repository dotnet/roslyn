// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly ImmutableArray<IDiagnosticAnalyzer> analyzers;
        private readonly string fullPath;
        private readonly string display;

        public AnalyzerImageReference(ImmutableArray<IDiagnosticAnalyzer> analyzers, string fullPath = null, string display = null)
        {
            if (analyzers.Any(a => a == null))
            {
                throw new ArgumentException("Cannot have null-valued analyzer", "analyzers");
            }

            this.analyzers = analyzers;
            this.fullPath = fullPath;
            this.display = display;
        }

        public override ImmutableArray<IDiagnosticAnalyzer> GetAnalyzers()
        {
            return this.analyzers;
        }

        public override string FullPath
        {
            get
            {
                return this.fullPath;
            }
        }

        public override string Display
        {
            get
            {
                return display ?? fullPath ?? CodeAnalysisResources.InMemoryAssembly;
            }
        }

        private string GetDebuggerDisplay()
        {
            var sb = new StringBuilder();
            sb.Append("Assembly");

            if (fullPath != null)
            {
                sb.Append(" Path='");
                sb.Append(fullPath);
                sb.Append("'");
            }

            if (display != null)
            {
                sb.Append(" Display='");
                sb.Append(display);
                sb.Append("'");
            }

            return sb.ToString();
        }
    }
}
