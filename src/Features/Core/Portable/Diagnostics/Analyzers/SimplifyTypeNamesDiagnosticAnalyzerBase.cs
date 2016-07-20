// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics.QualifyMemberAccess;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.SimplifyTypeNames
{
    internal abstract class SimplifyTypeNamesDiagnosticAnalyzerBase<TLanguageKindEnum> : DiagnosticAnalyzer, IBuiltInAnalyzer where TLanguageKindEnum : struct
    {
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(WorkspacesResources.Name_can_be_simplified), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        private static readonly LocalizableString s_localizableTitleSimplifyNames = new LocalizableResourceString(nameof(FeaturesResources.Simplify_Names), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly DiagnosticDescriptor s_descriptorSimplifyNames = new DiagnosticDescriptor(IDEDiagnosticIds.SimplifyNamesDiagnosticId,
                                                                    s_localizableTitleSimplifyNames,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);

        private static readonly LocalizableString s_localizableTitleSimplifyMemberAccess = new LocalizableResourceString(nameof(FeaturesResources.Simplify_Member_Access), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly DiagnosticDescriptor s_descriptorSimplifyMemberAccess = new DiagnosticDescriptor(IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId,
                                                                    s_localizableTitleSimplifyMemberAccess,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);

        private static readonly LocalizableString s_localizableTitleRemoveThisOrMe = new LocalizableResourceString(nameof(FeaturesResources.Remove_qualification), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly DiagnosticDescriptor s_descriptorRemoveThisOrMeHidden = new DiagnosticDescriptor(IDEDiagnosticIds.RemoveQualificationDiagnosticId,
                                                                    s_localizableTitleRemoveThisOrMe,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);
        private static readonly DiagnosticDescriptor s_descriptorRemoveThisOrMeInfo = new DiagnosticDescriptor(IDEDiagnosticIds.RemoveQualificationDiagnosticId,
                                                                    s_localizableTitleRemoveThisOrMe,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Info,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);
        private static readonly DiagnosticDescriptor s_descriptorRemoveThisOrMeWarning = new DiagnosticDescriptor(IDEDiagnosticIds.RemoveQualificationDiagnosticId,
                                                                    s_localizableTitleRemoveThisOrMe,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Warning,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);
        private static readonly DiagnosticDescriptor s_descriptorRemoveThisOrMeError = new DiagnosticDescriptor(IDEDiagnosticIds.RemoveQualificationDiagnosticId,
                                                                    s_localizableTitleRemoveThisOrMe,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Error,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    s_descriptorSimplifyNames,
                    s_descriptorSimplifyMemberAccess,
                    s_descriptorRemoveThisOrMeHidden,
                    s_descriptorRemoveThisOrMeInfo,
                    s_descriptorRemoveThisOrMeWarning,
                    s_descriptorRemoveThisOrMeError);
            }
        }

        protected abstract void AnalyzeNode(SyntaxNodeAnalysisContext context);

        protected abstract bool CanSimplifyTypeNameExpressionCore(SemanticModel model, SyntaxNode node, OptionSet optionSet, out TextSpan issueSpan, out string diagnosticId, CancellationToken cancellationToken);

        protected abstract string GetLanguageName();

        protected bool TrySimplifyTypeNameExpression(SemanticModel model, SyntaxNode node, AnalyzerOptions analyzerOptions, out Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic = default(Diagnostic);

            var optionSet = analyzerOptions.GetOptionSet();
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

                case IDEDiagnosticIds.RemoveQualificationDiagnosticId:
                    descriptor = GetRemoveQualificationDiagnosticDescriptor(model, node, optionSet, cancellationToken);
                    break;

                default:
                    throw ExceptionUtilities.Unreachable;
            }

            if (descriptor == null)
            {
                return false;
            }

            var tree = model.SyntaxTree;
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder["OptionName"] = nameof(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess); // TODO: need the actual one
            builder["OptionLanguage"] = model.Language;
            diagnostic = Diagnostic.Create(descriptor, tree.GetLocation(issueSpan), builder.ToImmutable());
            return true;
        }

        private DiagnosticDescriptor GetRemoveQualificationDiagnosticDescriptor(SemanticModel model, SyntaxNode node, OptionSet optionSet, CancellationToken cancellationToken)
        {
            var symbolInfo = model.GetSymbolInfo(node, cancellationToken);
            if (symbolInfo.Symbol == null)
            {
                return null;
            }

            var applicableOption = QualifyMemberAccessDiagnosticAnalyzerBase<TLanguageKindEnum>.GetApplicableOptionFromSymbolKind(symbolInfo.Symbol.Kind);
            var optionValue = optionSet.GetOption(applicableOption, GetLanguageName());
            switch (optionValue.Notification.Value)
            {
                case DiagnosticSeverity.Hidden:
                    return s_descriptorRemoveThisOrMeHidden;
                case DiagnosticSeverity.Info:
                    return s_descriptorRemoveThisOrMeInfo;
                case DiagnosticSeverity.Warning:
                    return s_descriptorRemoveThisOrMeWarning;
                case DiagnosticSeverity.Error:
                    return s_descriptorRemoveThisOrMeError;
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
        }
    }
}
