// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Analyzer options with workspace.
    /// These are used to fetch the workspace options by our internal analyzers (e.g. simplification analyzer).
    /// </summary>
    internal sealed class WorkspaceAnalyzerOptions : AnalyzerOptions
    {
        public WorkspaceAnalyzerOptions(AnalyzerOptions options, Workspace workspace)
            : base(options.AdditionalFiles)
        {
            this.Workspace = workspace;
        }

        public Workspace Workspace { get; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = obj as WorkspaceAnalyzerOptions;
            return other != null &&
                this.Workspace == other.Workspace &&
                base.Equals(other);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Workspace, base.GetHashCode());
        }
    }
}
