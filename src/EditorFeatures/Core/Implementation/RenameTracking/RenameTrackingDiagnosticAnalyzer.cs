// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class RenameTrackingDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        public const string DiagnosticId = "RenameTracking";
        public static DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
            DiagnosticId, title: "", messageFormat: "", category: "",
            defaultSeverity: DiagnosticSeverity.Hidden, isEnabledByDefault: true,
            customTags: DiagnosticCustomTags.Microsoft.Append(WellKnownDiagnosticTags.NotConfigurable));

        internal const string RenameFromPropertyKey = "RenameFrom";
        internal const string RenameToPropertyKey = "RenameTo";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(DiagnosticDescriptor);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var diagnostics = RenameTrackingTaggerProvider.GetDiagnosticsAsync(context.Tree, DiagnosticDescriptor, context.CancellationToken).WaitAndGetResult(context.CancellationToken);

            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SyntaxAnalysis;
        }
    }
}
