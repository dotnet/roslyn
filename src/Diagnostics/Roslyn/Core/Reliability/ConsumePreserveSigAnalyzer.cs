// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class ConsumePreserveSigAnalyzer<TSyntaxKind> : DiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        private static LocalizableString localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.ConsumePreserveSigTitle), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.ConsumePreserveSigMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.ConsumePreserveSigDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        internal static readonly DiagnosticDescriptor ConsumePreserveSigAnalyzerDescriptor = new DiagnosticDescriptor(
            RoslynDiagnosticIds.ConsumePreserveSigRuleId,
            localizableTitle,
            localizableMessage,
            "Reliability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private INamedTypeSymbol lazyPreserveSigType;

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(ConsumePreserveSigAnalyzerDescriptor);
            }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                lazyPreserveSigType = compilationContext.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.PreserveSigAttribute");
                if (lazyPreserveSigType != null)
                {
                    compilationContext.RegisterSyntaxNodeAction(AnalyzeNode, ImmutableArray.Create(InvocationExpressionSyntaxKind));
                }
            });
        }

        protected abstract TSyntaxKind InvocationExpressionSyntaxKind { get; }
        protected abstract bool IsExpressionStatementSyntaxKind(int rawKind);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (this.lazyPreserveSigType == null)
            {
                return;
            }

            var node = context.Node;
            if (!IsExpressionStatementSyntaxKind(node.Parent.RawKind))
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol;
            if (symbol == null)
            {
                return;
            }

            foreach (var attributeData in symbol.GetAttributes())
            {
                if (attributeData.AttributeClass.Equals(lazyPreserveSigType))
                {
                    var diagnostic = Diagnostic.Create(ConsumePreserveSigAnalyzerDescriptor, node.GetLocation(), symbol);
                    context.ReportDiagnostic(diagnostic);
                    return;
                }
            }
        }
    }
}
