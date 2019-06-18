// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class RudeEditDiagnosticAnalyzer : DocumentDiagnosticAnalyzer, IBuiltInAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => RudeEditDiagnosticDescriptors.AllDescriptors;
        public bool OpenFileOnly(Workspace workspace) => false;

        public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            // No syntax diagnostics produced by the EnC engine.  
            return SpecializedTasks.EmptyImmutableArray<Diagnostic>();
        }

        public override async Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
        {
            try
            {
                var encService = document.Project.Solution.Workspace.Services.GetService<IDebuggingWorkspaceService>()?.EditAndContinueServiceOpt;
                if (encService == null)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var session = encService.EditSession;
                if (session == null ||
                    session.BaseSolution.WorkspaceVersion == document.Project.Solution.WorkspaceVersion ||
                    !session.HasProject(document.Project.Id))
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var analysis = await session.GetDocumentAnalysis(document).GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (analysis.RudeEditErrors.IsDefault)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                session.LogRudeEditErrors(analysis.RudeEditErrors);
                return analysis.RudeEditErrors.SelectAsArray((e, t) => e.ToDiagnostic(t), tree);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
    }
}
