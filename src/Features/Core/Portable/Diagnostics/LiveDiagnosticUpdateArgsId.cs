// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class LiveDiagnosticUpdateArgsId : AnalyzerUpdateArgsId
    {
        private string? _buildTool;

        public readonly object ProjectOrDocumentId;
        public readonly AnalysisKind Kind;

        public LiveDiagnosticUpdateArgsId(DiagnosticAnalyzer analyzer, object projectOrDocumentId, AnalysisKind kind)
            : base(analyzer)
        {
            Contract.ThrowIfNull(projectOrDocumentId);

            ProjectOrDocumentId = projectOrDocumentId;
            Kind = kind;
        }

        public override string BuildTool => _buildTool ??= ComputeBuildTool();

        private string ComputeBuildTool()
            => Analyzer.IsBuiltInAnalyzer() ? PredefinedBuildTools.Live : Analyzer.GetAnalyzerAssemblyName();

        public override bool Equals(object? obj)
        {
            if (obj is not LiveDiagnosticUpdateArgsId other)
            {
                return false;
            }

            return Kind == other.Kind && Equals(ProjectOrDocumentId, other.ProjectOrDocumentId) && base.Equals(obj);
        }

        public override int GetHashCode()
            => Hash.Combine(ProjectOrDocumentId, Hash.Combine((int)Kind, base.GetHashCode()));
    }
}
