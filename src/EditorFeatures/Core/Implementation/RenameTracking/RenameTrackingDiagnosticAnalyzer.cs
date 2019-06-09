// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class RenameTrackingDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer, IInProcessAnalyzer
    {
        public const string DiagnosticId = "RenameTracking";
        public static DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
            DiagnosticId, title: "", messageFormat: "", category: "",
            defaultSeverity: DiagnosticSeverity.Hidden, isEnabledByDefault: true,
            customTags: DiagnosticCustomTags.Microsoft.Append(WellKnownDiagnosticTags.NotConfigurable));

        internal const string RenameFromPropertyKey = "RenameFrom";
        internal const string RenameToPropertyKey = "RenameTo";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptor);

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        public bool OpenFileOnly(Workspace workspace)
            => true;

#pragma warning disable RS1026 // Enable concurrent execution
        public override void Initialize(AnalysisContext context)
#pragma warning restore RS1026 // Enable concurrent execution
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var diagnostics = RenameTrackingTaggerProvider.GetDiagnosticsAsync(context.Tree, DiagnosticDescriptor, context.CancellationToken).WaitAndGetResult_CanCallOnBackground(context.CancellationToken);

            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
