// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Debugger.Contracts;
using Microsoft.CodeAnalysis.Simplification;

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

        public CodeActionRequestPriority RequestPriority => CodeActionRequestPriority.Normal;

        public bool OpenFileOnly(SimplifierOptions? options)
            => false;

        // No syntax diagnostics produced by the EnC engine.  
        public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyImmutableArray<Diagnostic>();

        public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
        {
            if (!DebuggerContractVersionCheck.IsRequiredDebuggerContractVersionAvailable())
            {
                return SpecializedTasks.EmptyImmutableArray<Diagnostic>();
            }

            return AnalyzeSemanticsImplAsync(document, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsImplAsync(Document designTimeDocument, CancellationToken cancellationToken)
        {
            var workspace = designTimeDocument.Project.Solution.Workspace;

            if (workspace.Services.HostServices is not IMefHostExportProvider mefServices)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            // avoid creating and synchronizing compile-time solution if the Hot Reload/EnC session is not active
            if (mefServices.GetExports<EditAndContinueLanguageService>().SingleOrDefault()?.Value.IsSessionActive != true)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var designTimeSolution = designTimeDocument.Project.Solution;
            var compileTimeSolution = workspace.Services.GetRequiredService<ICompileTimeSolutionProvider>().GetCompileTimeSolution(designTimeSolution);

            var compileTimeDocument = await CompileTimeSolutionProvider.TryGetCompileTimeDocumentAsync(designTimeDocument, compileTimeSolution, cancellationToken).ConfigureAwait(false);
            if (compileTimeDocument == null)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            // EnC services should never be called on a design-time solution.

            var proxy = new RemoteEditAndContinueServiceProxy(workspace);

            var activeStatementSpanProvider = new ActiveStatementSpanProvider(async (documentId, filePath, cancellationToken) =>
            {
                var trackingService = workspace.Services.GetRequiredService<IActiveStatementTrackingService>();
                return await trackingService.GetSpansAsync(compileTimeSolution, documentId, filePath, cancellationToken).ConfigureAwait(false);
            });

            return await proxy.GetDocumentDiagnosticsAsync(compileTimeDocument, designTimeDocument, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
        }
    }
}
