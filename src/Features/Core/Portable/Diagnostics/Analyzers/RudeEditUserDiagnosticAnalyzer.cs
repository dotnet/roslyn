// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class RudeEditDiagnosticAnalyzer : DocumentDiagnosticAnalyzer, IBuiltInAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return RudeEditDiagnosticDescriptors.AllDescriptors;
            }
        }

        public override Task AnalyzeSyntaxAsync(Document document, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            // No syntax diagnostics produced by the EnC engine.  
            return SpecializedTasks.EmptyTask;
        }

        public override async Task AnalyzeSemanticsAsync(Document document, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            try
            {
                var encService = document.Project.Solution.Workspace.Services.GetService<IEditAndContinueWorkspaceService>();
                if (encService == null)
                {
                    return;
                }

                EditSession session = encService.EditSession;
                if (session == null ||
                    session.BaseSolution.WorkspaceVersion == document.Project.Solution.WorkspaceVersion ||
                    !session.HasProject(document.Project.Id))
                {
                    return;
                }

                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var analysis = await session.GetDocumentAnalysis(document).GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (!analysis.RudeEditErrors.IsDefault)
                {
                    session.LogRudeEditErrors(analysis.RudeEditErrors);

                    foreach (var error in analysis.RudeEditErrors)
                    {
                        addDiagnostic(error.ToDiagnostic(tree));
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
        }
    }
}
