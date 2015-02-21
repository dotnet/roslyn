// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AbstractHostDiagnosticUpdateSource
    {
        // TODO(mavasani): Fix the event to be an instance field.
        internal static event EventHandler<WorkspaceAnalyzerExceptionDiagnosticArgs> AnalyzerExceptionDiagnostic;

        protected AbstractHostDiagnosticUpdateSource()
        {
            // Register for exception diagnostics from workspace's analyzer manager.
            AnalyzerExceptionDiagnostic += OnAnalyzerExceptionDiagnostic;
        }

        internal void UnregisterDiagnosticUpdateSource()
        {
            // Unregister for exception diagnostics from workspace's analyzer manager.
            AnalyzerExceptionDiagnostic -= OnAnalyzerExceptionDiagnostic;
        }

        internal static EventHandler<AnalyzerExceptionDiagnosticArgs> RegisterAnalyzerExceptionDiagnosticHandler(ImmutableArray<DiagnosticAnalyzer> analyzers, Workspace workspace)
        {
            return RegisterAnalyzerExceptionDiagnosticHandler(analyzers, workspace, project: null);
        }

        internal static EventHandler<AnalyzerExceptionDiagnosticArgs> RegisterAnalyzerExceptionDiagnosticHandler(ImmutableArray<DiagnosticAnalyzer> analyzers, Project project)
        {
            return RegisterAnalyzerExceptionDiagnosticHandler(analyzers, project.Solution.Workspace, project);
        }

        private static EventHandler<AnalyzerExceptionDiagnosticArgs> RegisterAnalyzerExceptionDiagnosticHandler(ImmutableArray<DiagnosticAnalyzer> analyzers, Workspace workspace, Project project)
        {
            Action<object, AnalyzerExceptionDiagnosticArgs> onAnalyzerExceptionDiagnostic = (sender, args) =>
                ReportAnalyzerExceptionDiagnostic(sender, args, workspace, project);

            return AnalyzerDriverHelper.RegisterAnalyzerExceptionDiagnosticHandler(analyzers, onAnalyzerExceptionDiagnostic);
        }

        internal static EventHandler<AnalyzerExceptionDiagnosticArgs> RegisterAnalyzerExceptionDiagnosticHandler(DiagnosticAnalyzer analyzer, Workspace workspace)
        {
            return RegisterAnalyzerExceptionDiagnosticHandler(analyzer, workspace, project: null);
        }

        internal static EventHandler<AnalyzerExceptionDiagnosticArgs> RegisterAnalyzerExceptionDiagnosticHandler(DiagnosticAnalyzer analyzer, Project project)
        {
            return RegisterAnalyzerExceptionDiagnosticHandler(analyzer, project.Solution.Workspace, project);
        }

        private static EventHandler<AnalyzerExceptionDiagnosticArgs> RegisterAnalyzerExceptionDiagnosticHandler(DiagnosticAnalyzer analyzer, Workspace workspace, Project project)
        {
            Action<object, AnalyzerExceptionDiagnosticArgs> onAnalyzerExceptionDiagnostic = (sender, args) =>
                ReportAnalyzerExceptionDiagnostic(sender, args, workspace, project);

            return AnalyzerDriverHelper.RegisterAnalyzerExceptionDiagnosticHandler(analyzer, onAnalyzerExceptionDiagnostic);
        }

        internal static void UnregisterAnalyzerExceptionDiagnosticHandler(EventHandler<AnalyzerExceptionDiagnosticArgs> handler)
        {
            AnalyzerDriverHelper.UnregisterAnalyzerExceptionDiagnosticHandler(handler);
        }

        internal static void ReportAnalyzerExceptionDiagnostic(object sender, DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Workspace workspace)
        {
            ReportAnalyzerExceptionDiagnostic(sender, analyzer, diagnostic, workspace, project: null);
        }

        internal static void ReportAnalyzerExceptionDiagnostic(object sender, DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Project project)
        {
            ReportAnalyzerExceptionDiagnostic(sender, analyzer, diagnostic, project.Solution.Workspace, project);
        }

        private static void ReportAnalyzerExceptionDiagnostic(object sender, DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Workspace workspace, Project project)
        {
            var args = new WorkspaceAnalyzerExceptionDiagnosticArgs(analyzer, diagnostic, workspace, project);
            AnalyzerExceptionDiagnostic?.Invoke(sender, args);
        }

        private static void ReportAnalyzerExceptionDiagnostic(object sender, AnalyzerExceptionDiagnosticArgs args, Workspace workspace, Project project)
        {
            var workspaceArgs = new WorkspaceAnalyzerExceptionDiagnosticArgs(args.FaultedAnalyzer, args.Diagnostic, workspace, project);
            AnalyzerExceptionDiagnostic?.Invoke(sender, workspaceArgs);
        }
    }
}
