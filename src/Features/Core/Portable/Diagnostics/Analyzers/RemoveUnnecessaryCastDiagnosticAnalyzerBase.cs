// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.RemoveUnnecessaryCast
{
    internal abstract class RemoveUnnecessaryCastDiagnosticAnalyzerBase<TLanguageKindEnum> : DiagnosticAnalyzer, IBuiltInAnalyzer where TLanguageKindEnum : struct
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FeaturesResources.RemoveUnnecessaryCast), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(WorkspacesResources.CastIsRedundant), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId,
                                                                    s_localizableTitle,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);

        #region Interface methods

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(s_descriptor);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                (nodeContext) =>
                    {
                        Diagnostic diagnostic;
                        if (TryRemoveCastExpression(nodeContext.SemanticModel, nodeContext.Node, out diagnostic, nodeContext.CancellationToken))
                        {
                            nodeContext.ReportDiagnostic(diagnostic);
                        }
                    },
                this.SyntaxKindsOfInterest.ToArray());
        }

        public abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }

        #endregion

        protected abstract bool IsUnnecessaryCast(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract TextSpan GetDiagnosticSpan(SyntaxNode node);

        private bool TryRemoveCastExpression(
            SemanticModel model, SyntaxNode node, out Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic = default(Diagnostic);

            if (!IsUnnecessaryCast(model, node, cancellationToken))
            {
                return false;
            }

            var tree = model.SyntaxTree;
            var span = GetDiagnosticSpan(node);
            if (tree.OverlapsHiddenPosition(span, cancellationToken))
            {
                return false;
            }

            diagnostic = Diagnostic.Create(s_descriptor, tree.GetLocation(span));
            return true;
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
        }
    }
}
