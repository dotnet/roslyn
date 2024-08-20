// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Serialization;

internal partial class SerializerService
{
    /// <summary>
    /// No methods on this serialized type should be called.  It exists as a placeholder to allow the data to be
    /// transmitted from the host to the remote side.  On the remote side we will first collect *all* of these
    /// serialized analyzer references, then create the actual <see cref="AnalyzerFileReference"/>s in their own safe
    /// AssemblyLoadContext distinct from everything else.
    /// </summary>
    public sealed class SerializedAnalyzerReference(
        string fullPath,
        Guid Mvid) : AnalyzerReference
    {
        public override string FullPath { get; } = fullPath;

        public Guid GetMvidForTestingOnly() => Mvid;

        public override object Id
            => throw new InvalidOperationException();

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
            => throw new InvalidOperationException();

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
            => throw new InvalidOperationException();

        [Obsolete]
        public override ImmutableArray<ISourceGenerator> GetGenerators()
            => throw new InvalidOperationException();

        public override ImmutableArray<ISourceGenerator> GetGenerators(string language)
            => throw new InvalidOperationException();

        public override ImmutableArray<ISourceGenerator> GetGeneratorsForAllLanguages()
            => throw new InvalidOperationException();
    }
}
