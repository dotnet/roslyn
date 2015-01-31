// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.AddImport
{
    internal abstract class AddImportDiagnosticAnalyzerBase<TLanguageKindEnum, TSimpleNameSyntax, TQualifiedNameSyntax, TIncompleteMemberSyntax> : DiagnosticAnalyzer, IBuiltInAnalyzer
        where TLanguageKindEnum : struct
        where TSimpleNameSyntax : SyntaxNode
        where TQualifiedNameSyntax : SyntaxNode
        where TIncompleteMemberSyntax : SyntaxNode
    {
        protected abstract DiagnosticDescriptor DiagnosticDescriptor { get; }
        protected abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(DiagnosticDescriptor);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, this.SyntaxKindsOfInterest.ToArray());
        }

        protected DiagnosticDescriptor GetDiagnosticDescriptor(string id, string messageFormat)
        {
            // it is not configurable diagnostic, title doesn't matter
            return new DiagnosticDescriptor(
                id, string.Empty, messageFormat,
                DiagnosticCategory.Compiler,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                customTags: DiagnosticCustomTags.Microsoft.Append(WellKnownDiagnosticTags.NotConfigurable));
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var member = (TIncompleteMemberSyntax)context.Node;

            Func<SyntaxNode, bool> isQualifiedOrSimpleName = (SyntaxNode n) => n is TQualifiedNameSyntax || n is TSimpleNameSyntax;
            var typeNames = member.DescendantNodes().Where(n => isQualifiedOrSimpleName(n) && !n.Span.IsEmpty);
            foreach (var typeName in typeNames)
            {
                var info = context.SemanticModel.GetSymbolInfo(typeName);
                if (info.Symbol != null || info.CandidateSymbols.Length > 0)
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, typeName.GetLocation(), typeName.ToString()));
            }
        }
    }
}
