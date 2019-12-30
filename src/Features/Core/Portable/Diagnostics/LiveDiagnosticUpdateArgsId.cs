// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class LiveDiagnosticUpdateArgsId : AnalyzerUpdateArgsId
    {
        private readonly string _analyzerPackageName;

        public readonly ProjectOrDocumentId ProjectOrDocumentId;
        public readonly int Kind;

        public LiveDiagnosticUpdateArgsId(DiagnosticAnalyzer analyzer, ProjectOrDocumentId projectOrDocumentId, int kind, string analyzerPackageName)
            : base(analyzer)
        {
            Contract.ThrowIfTrue(projectOrDocumentId.IsDefault);

            ProjectOrDocumentId = projectOrDocumentId;
            Kind = kind;

            _analyzerPackageName = analyzerPackageName;
        }

        public override string BuildTool => _analyzerPackageName;

        public override bool Equals(object obj)
        {
            if (!(obj is LiveDiagnosticUpdateArgsId other))
            {
                return false;
            }

            return Kind == other.Kind && ProjectOrDocumentId.Equals(other.ProjectOrDocumentId) && base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(ProjectOrDocumentId.GetHashCode(), Hash.Combine(Kind, base.GetHashCode()));
        }
    }
}
