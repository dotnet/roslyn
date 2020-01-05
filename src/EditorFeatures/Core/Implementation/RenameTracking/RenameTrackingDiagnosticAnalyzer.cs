// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class RenameTrackingDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
    {
        public const string DiagnosticId = "RenameTracking";
        public static DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
            DiagnosticId, title: "", messageFormat: "", category: "",
            defaultSeverity: DiagnosticSeverity.Hidden, isEnabledByDefault: true,
            customTags: DiagnosticCustomTags.Microsoft.Append(WellKnownDiagnosticTags.NotConfigurable));

        internal const string RenameFromPropertyKey = "RenameFrom";
        internal const string RenameToPropertyKey = "RenameTo";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptor);

        public override async Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var diagnostic = RenameTrackingTaggerProvider.TryGetDiagnostic(syntaxTree, DiagnosticDescriptor, cancellationToken);
            if (diagnostic is null)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            return ImmutableArray.Create(diagnostic);
        }

        public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyImmutableArray<Diagnostic>();
    }
}
