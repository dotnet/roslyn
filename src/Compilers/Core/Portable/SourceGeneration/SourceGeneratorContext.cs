// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In Progress")]
    public readonly struct SourceGeneratorContext
    {
        internal SourceGeneratorContext(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken = default)
        {
            Compilation = compilation;
            AnalyzerOptions = options;
            CancellationToken = cancellationToken;
            AdditionalSources = new AdditionalSourcesCollection();
        }

        public Compilation Compilation { get; }

        // PROTOTYPE: replace AnalyzerOptions with an differently named type that is otherwise identical.
        // The concern being that something added to one isn't necessarily applicable to the other.
        public AnalyzerOptions AnalyzerOptions { get; }

        public CancellationToken CancellationToken { get; }

        public AdditionalSourcesCollection AdditionalSources { get; }

        public void ReportDiagnostic(Diagnostic diagnostic) { throw new NotImplementedException(); }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In progress")]
    // PROTOTYPE: this is going to need to track the input and output compilations that occured
    public struct UpdateContext
    {
        internal UpdateContext(ImmutableArray<GeneratedSourceText> sources, CancellationToken cancellationToken = default)
        {
            AdditionalSources = new AdditionalSourcesCollection(sources);
            Succeeded = true;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public AdditionalSourcesCollection AdditionalSources { get; }

        public bool Succeeded { get; set; }

        public void ReportDiagnostic(Diagnostic diagnostic) { throw new NotImplementedException(); }
    }
}
