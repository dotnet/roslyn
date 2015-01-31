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
        private static LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(EditorFeaturesResources.RenameTracking), EditorFeaturesResources.ResourceManager, typeof(EditorFeaturesResources));
        private static LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(EditorFeaturesResources.RenameTo), EditorFeaturesResources.ResourceManager, typeof(EditorFeaturesResources));

        // TODO: Ideally we'd use a TriggerDiagnosticDescriptor here. However, this analyzer uses the message to communicate
        // with it's fixer about what has changed. This analysis is not trivial to do for the fixer because of the temporal nature
        // of this diagnostic. We should consider adding a field on diagnostic that is for extra data. If we have that we can
        // turn this into a trigger diagnostic. For now, this just has the "NotConfigurable" tag to not show in the ruleset editor.
        public static DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(DiagnosticId,
                                                                            s_localizableTitle,
                                                                            s_localizableMessage,
                                                                            "",
                                                                            DiagnosticSeverity.Hidden,
                                                                            isEnabledByDefault: true,
                                                                            customTags: DiagnosticCustomTags.Microsoft.Append(WellKnownDiagnosticTags.NotConfigurable));

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
    }
}
