// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class EditAndContinueDiagnosticAnalyzer : DocumentDiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private static readonly ImmutableArray<DiagnosticDescriptor> s_supportedDiagnostics = EditAndContinueDiagnosticDescriptors.GetDescriptors();

        // Return known descriptors. This will not include module diagnostics reported on behalf of the debugger.
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => s_supportedDiagnostics;

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        public bool OpenFileOnly(OptionSet options)
            => false;

        // No syntax diagnostics produced by the EnC engine.  
        public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyImmutableArray<Diagnostic>();

        public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
        {
            var services = document.Project.Solution.Workspace.Services;
            var encService = services.GetService<IEditAndContinueWorkspaceService>();
            if (encService is null)
            {
                return SpecializedTasks.EmptyImmutableArray<Diagnostic>();
            }

            var activeStatementSpanProvider = new DocumentActiveStatementSpanProvider(async cancellationToken =>
            {
                var trackingService = services.GetRequiredService<IActiveStatementTrackingService>();
                return await trackingService.GetSpansAsync(document, cancellationToken).ConfigureAwait(false);
            });

            return encService.GetDocumentDiagnosticsAsync(document, activeStatementSpanProvider, cancellationToken);
        }
    }
}
