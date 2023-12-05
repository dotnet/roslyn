// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    internal static class PullDiagnosticCategories
    {
        /// <summary>
        /// Task list items.  Can be for Document or Workspace pull requests.
        /// </summary>
        public static readonly string Task = VSInternalDiagnosticKind.Task.Value;

        // Workspace categories

        /// <summary>
        /// Diagnostics for workspace documents and project.  We don't support fine-grained diagnostics requests for these (yet).
        /// </summary>
        public const string WorkspaceDocumentsAndProject = nameof(WorkspaceDocumentsAndProject);

        // Fine-grained document pull categories to allow diagnostics to more quickly reach the user.

        public const string DocumentCompilerSyntax = nameof(DocumentCompilerSyntax);
        public const string DocumentCompilerSemantic = nameof(DocumentCompilerSemantic);
        public const string DocumentAnalyzerSyntax = nameof(DocumentAnalyzerSyntax);
        public const string DocumentAnalyzerSemantic = nameof(DocumentAnalyzerSemantic);
    }
}
