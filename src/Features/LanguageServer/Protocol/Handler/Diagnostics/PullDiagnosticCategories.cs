// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;

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

        // VSLanguageServerClient's RemoteDocumentDiagnosticBroker uses this exact string to determine
        // when syntax errors are being provided via pull diagnostics. Alternatively when 17.9 preview 1 packages
        // are consumable by Roslyn, this could be updated to reference VSInternalDiagnosticKind.Syntax.Value directly.
        public const string DocumentCompilerSyntax = "syntax";

        public const string DocumentCompilerSemantic = nameof(DocumentCompilerSemantic);
        public const string DocumentAnalyzerSyntax = nameof(DocumentAnalyzerSyntax);
        public const string DocumentAnalyzerSemantic = nameof(DocumentAnalyzerSemantic);
    }
}
