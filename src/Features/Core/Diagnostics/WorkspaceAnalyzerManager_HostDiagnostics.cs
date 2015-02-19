// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed partial class WorkspaceAnalyzerManager
    {
        internal static event EventHandler<WorkspaceAnalyzerExceptionDiagnosticArgs> AnalyzerExceptionDiagnostic;

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
            EventHandler<AnalyzerExceptionDiagnosticArgs> handler = (sender, args) =>
            {
                if (analyzers.Contains(args.FaultedAnalyzer))
                {
                    var workspaceArgs = new WorkspaceAnalyzerExceptionDiagnosticArgs(args, workspace, project);
                    AnalyzerExceptionDiagnostic?.Invoke(sender, workspaceArgs);
                }
            };

            AnalyzerDriverHelper.AnalyzerExceptionDiagnostic += handler;
            return handler;
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
            EventHandler<AnalyzerExceptionDiagnosticArgs> handler = (sender, args) =>
            {
                if (analyzer == args.FaultedAnalyzer)
                {
                    var workspaceArgs = new WorkspaceAnalyzerExceptionDiagnosticArgs(args, workspace, project);
                    AnalyzerExceptionDiagnostic?.Invoke(sender, workspaceArgs);
                }
            };

            AnalyzerDriverHelper.AnalyzerExceptionDiagnostic += handler;
            return handler;
        }

        internal static void UnregisterAnalyzerExceptionDiagnosticHandler(EventHandler<AnalyzerExceptionDiagnosticArgs> handler)
        {
            AnalyzerDriverHelper.AnalyzerExceptionDiagnostic -= handler;
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
    }
}
