// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SimplifyTypeNames
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

        private static readonly DiagnosticDescriptor s_descriptorPreferBuiltinOrFrameworkType = new DiagnosticDescriptor(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId,
            s_localizableTitleSimplifyNames,
            s_localizableMessage,
            DiagnosticCategory.Style,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            customTags: DiagnosticCustomTags.Unnecessary);

        internal abstract bool IsCandidate(SyntaxNode node);
        internal abstract bool CanSimplifyTypeNameExpression(
            SemanticModel model, SyntaxNode node, OptionSet optionSet,
            out TextSpan issueSpan, out string diagnosticId, out bool inDeclaration,
            CancellationToken cancellationToken);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(
                    s_descriptorSimplifyNames,
                    s_descriptorSimplifyMemberAccess,
                    s_descriptorPreferBuiltinOrFrameworkType);

        private readonly ImmutableArray<TLanguageKindEnum> _kindsOfInterest;

        protected SimplifyTypeNamesDiagnosticAnalyzerBase(ImmutableArray<TLanguageKindEnum> kindsOfInterest)
        {
            _kindsOfInterest = kindsOfInterest;
        }

        public bool OpenFileOnly(Workspace workspace)
        {
            var preferTypeKeywordInDeclarationOption = workspace.Options.GetOption(
                CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, GetLanguageName()).Notification;
            var preferTypeKeywordInMemberAccessOption = workspace.Options.GetOption(
                CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, GetLanguageName()).Notification;

            return !(preferTypeKeywordInDeclarationOption == NotificationOption.Warning || preferTypeKeywordInDeclarationOption == NotificationOption.Error ||
                     preferTypeKeywordInMemberAccessOption == NotificationOption.Warning || preferTypeKeywordInMemberAccessOption == NotificationOption.Error);
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, _kindsOfInterest);
        }

        protected abstract void AnalyzeNode(SyntaxNodeAnalysisContext context);

        protected abstract bool CanSimplifyTypeNameExpressionCore(
            SemanticModel model, SyntaxNode node, OptionSet optionSet,
            out TextSpan issueSpan, out string diagnosticId, out bool inDeclaration,
            CancellationToken cancellationToken);

        protected abstract string GetLanguageName();

        protected bool TrySimplifyTypeNameExpression(SemanticModel model, SyntaxNode node, AnalyzerOptions analyzerOptions, out Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic = default;

            var syntaxTree = node.SyntaxTree;
            var optionSet = analyzerOptions.GetAnalyzerOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();

            if (!CanSimplifyTypeNameExpressionCore(
                    model, node, optionSet,
                    out var issueSpan, out var diagnosticId, out var inDeclaration,
                    cancellationToken))
            {
                return false;
            }

            if (model.SyntaxTree.OverlapsHiddenPosition(issueSpan, cancellationToken))
            {
                return false;
            }

            PerLanguageOption<CodeStyleOption<bool>> option;
            DiagnosticDescriptor descriptor;
            ReportDiagnostic severity;
            switch (diagnosticId)
            {
                case IDEDiagnosticIds.SimplifyNamesDiagnosticId:
                    descriptor = s_descriptorSimplifyNames;
                    severity = descriptor.DefaultSeverity.ToReportDiagnostic();
                    break;

                case IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId:
                    descriptor = s_descriptorSimplifyMemberAccess;
                    severity = descriptor.DefaultSeverity.ToReportDiagnostic();
                    break;

                case IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId:
                    option = inDeclaration
                        ? CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration
                        : CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess;
                    descriptor = s_descriptorPreferBuiltinOrFrameworkType;

                    var optionValue = optionSet.GetOption(option, GetLanguageName());
                    severity = optionValue.Notification.Severity;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(diagnosticId);
            }

            if (descriptor == null)
            {
                return false;
            }

            var tree = model.SyntaxTree;
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder["OptionName"] = nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess); // TODO: need the actual one
            builder["OptionLanguage"] = model.Language;
            diagnostic = DiagnosticHelper.Create(descriptor, tree.GetLocation(issueSpan), severity, additionalLocations: null, builder.ToImmutable());
            return true;
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
