﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.SimplifyTypeNames
{
    internal abstract class SimplifyTypeNamesDiagnosticAnalyzerBase<TLanguageKindEnum> : DiagnosticAnalyzer, IBuiltInAnalyzer where TLanguageKindEnum : struct
    {
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(WorkspacesResources.NameCanBeSimplified), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        private static readonly LocalizableString s_localizableTitleSimplifyNames = new LocalizableResourceString(nameof(FeaturesResources.SimplifyNames), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly DiagnosticDescriptor s_descriptorSimplifyNames = new DiagnosticDescriptor(IDEDiagnosticIds.SimplifyNamesDiagnosticId,
                                                                    s_localizableTitleSimplifyNames,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);

        private static readonly LocalizableString s_localizableTitleSimplifyMemberAccess = new LocalizableResourceString(nameof(FeaturesResources.SimplifyMemberAccess), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly DiagnosticDescriptor s_descriptorSimplifyMemberAccess = new DiagnosticDescriptor(IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId,
                                                                    s_localizableTitleSimplifyMemberAccess,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);

        private static readonly LocalizableString s_localizableTitleSimplifyThisOrMe = new LocalizableResourceString(nameof(FeaturesResources.SimplifyThisOrMe), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly DiagnosticDescriptor s_descriptorSimplifyThisOrMe = new DiagnosticDescriptor(IDEDiagnosticIds.SimplifyThisOrMeDiagnosticId,
                                                                    s_localizableTitleSimplifyThisOrMe,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);

        private OptionSet _lazyDefaultOptionSet;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(s_descriptorSimplifyNames, s_descriptorSimplifyMemberAccess, s_descriptorSimplifyThisOrMe);
            }
        }

        protected abstract void AnalyzeNode(SyntaxNodeAnalysisContext context);

        protected abstract bool CanSimplifyTypeNameExpressionCore(SemanticModel model, SyntaxNode node, OptionSet optionSet, out TextSpan issueSpan, out string diagnosticId, CancellationToken cancellationToken);

        private OptionSet GetOptionSet(AnalyzerOptions analyzerOptions)
        {
            var workspaceOptions = analyzerOptions as WorkspaceAnalyzerOptions;
            if (workspaceOptions != null)
            {
                return workspaceOptions.Workspace.Options;
            }

            if (_lazyDefaultOptionSet == null)
            {
                Interlocked.CompareExchange(ref _lazyDefaultOptionSet, new OptionSet(null), null);
            }

            return _lazyDefaultOptionSet;
        }

        protected abstract string GetLanguageName();

        protected bool TrySimplifyTypeNameExpression(SemanticModel model, SyntaxNode node, AnalyzerOptions analyzerOptions, out Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic = default(Diagnostic);

            var optionSet = GetOptionSet(analyzerOptions);
            string diagnosticId;

            TextSpan issueSpan;
            if (!CanSimplifyTypeNameExpressionCore(model, node, optionSet, out issueSpan, out diagnosticId, cancellationToken))
            {
                return false;
            }

            if (model.SyntaxTree.OverlapsHiddenPosition(issueSpan, cancellationToken))
            {
                return false;
            }

            DiagnosticDescriptor descriptor;
            switch (diagnosticId)
            {
                case IDEDiagnosticIds.SimplifyNamesDiagnosticId:
                    descriptor = s_descriptorSimplifyNames;
                    break;

                case IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId:
                    descriptor = s_descriptorSimplifyMemberAccess;
                    break;

                case IDEDiagnosticIds.SimplifyThisOrMeDiagnosticId:
                    descriptor = s_descriptorSimplifyThisOrMe;
                    break;

                default:
                    throw ExceptionUtilities.Unreachable;
            }

            var tree = model.SyntaxTree;
            diagnostic = Diagnostic.Create(descriptor, tree.GetLocation(issueSpan));
            return true;
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
        }
    }
}
