// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class LiveDiagnosticUpdateArgsId : AnalyzerUpdateArgsId
    {
        private readonly string _analyzerPackageName;

        public readonly object ProjectOrDocumentId;
        public readonly int Kind;

        public LiveDiagnosticUpdateArgsId(DiagnosticAnalyzer analyzer, object projectOrDocumentId, int kind, string analyzerPackageName)
            : base(analyzer)
        {
            Contract.ThrowIfNull(projectOrDocumentId);

            ProjectOrDocumentId = projectOrDocumentId;
            Kind = kind;

            _analyzerPackageName = analyzerPackageName;
        }

        public override string BuildTool => _analyzerPackageName;

        public override bool Equals(object? obj)
        {
            if (!(obj is LiveDiagnosticUpdateArgsId other))
            {
                return false;
            }

            return Kind == other.Kind && Equals(ProjectOrDocumentId, other.ProjectOrDocumentId) && base.Equals(obj);
        }

        public override int GetHashCode()
            => Hash.Combine(ProjectOrDocumentId, Hash.Combine(Kind, base.GetHashCode()));
    }
}
