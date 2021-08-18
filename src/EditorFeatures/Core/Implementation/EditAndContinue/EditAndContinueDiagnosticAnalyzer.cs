// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
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
            var workspace = document.Project.Solution.Workspace;

            // do not load EnC service and its dependencies if the app is not running:
            var debuggingService = workspace.Services.GetRequiredService<IDebuggingWorkspaceService>();
            if (debuggingService.CurrentDebuggingState == DebuggingState.Design)
            {
                return SpecializedTasks.EmptyImmutableArray<Diagnostic>();
            }

            return AnalyzeSemanticsImplAsync(document, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsImplAsync(Document document, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var proxy = new RemoteEditAndContinueServiceProxy(workspace);

            var activeStatementSpanProvider = new DocumentActiveStatementSpanProvider(async cancellationToken =>
            {
                var trackingService = workspace.Services.GetRequiredService<IActiveStatementTrackingService>();
                return await trackingService.GetSpansAsync(document, cancellationToken).ConfigureAwait(false);
            });

            return proxy.GetDocumentDiagnosticsAsync(document, activeStatementSpanProvider, cancellationToken).AsTask();
        }
    }
}
