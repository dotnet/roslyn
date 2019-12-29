// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#define LOG

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
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
#if LOG
        private static string _logFile = @"c:\temp\simplifytypenames.txt";
        private static object _gate = new object();
        private static readonly Regex s_newlinePattern = new Regex(@"[\r\n]+");
#endif

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

        public bool OpenFileOnly(OptionSet options)
        {
            var preferTypeKeywordInDeclarationOption = options.GetOption(
                CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, GetLanguageName()).Notification;
            var preferTypeKeywordInMemberAccessOption = options.GetOption(
                CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, GetLanguageName()).Notification;

            return !(preferTypeKeywordInDeclarationOption == NotificationOption.Warning || preferTypeKeywordInDeclarationOption == NotificationOption.Error ||
                     preferTypeKeywordInMemberAccessOption == NotificationOption.Warning || preferTypeKeywordInMemberAccessOption == NotificationOption.Error);
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, _kindsOfInterest);
            // context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        }

        protected abstract void AnalyzeNode(SyntaxNodeAnalysisContext context);
        protected abstract void AnalyzeSemanticModel(SemanticModelAnalysisContext context);

        protected abstract bool CanSimplifyTypeNameExpressionCore(
            SemanticModel model, SyntaxNode node, OptionSet optionSet,
            out TextSpan issueSpan, out string diagnosticId, out bool inDeclaration,
            CancellationToken cancellationToken);

        protected abstract string GetLanguageName();

        protected bool TrySimplifyTypeNameExpression(SemanticModel model, SyntaxNode node, AnalyzerOptions analyzerOptions, out Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic = default;

            var syntaxTree = node.SyntaxTree;
            var optionSet = analyzerOptions.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return false;
            }

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

            diagnostic = CreateDiagnostic(model, optionSet, issueSpan, diagnosticId, inDeclaration);
            return true;
        }

        internal static Diagnostic CreateDiagnostic(
            SemanticModel model, OptionSet optionSet, TextSpan issueSpan,
            string diagnosticId, bool inDeclaration)
        {
            Diagnostic diagnostic;
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

                    var optionValue = optionSet.GetOption(option, model.Language);
                    severity = optionValue.Notification.Severity;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(diagnosticId);
            }

            var tree = model.SyntaxTree;
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder["OptionName"] = nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess); // TODO: need the actual one
            builder["OptionLanguage"] = model.Language;
            diagnostic = DiagnosticHelper.Create(descriptor, tree.GetLocation(issueSpan), severity, additionalLocations: null, builder.ToImmutable());

#if LOG
            var logLine = tree.FilePath + "\t" + diagnosticId + "\t" + inDeclaration + "\t";
            var sourceText = tree.GetText();
            sourceText.GetLineAndOffset(issueSpan.Start, out var startLineNumber, out var startOffset);
            sourceText.GetLineAndOffset(issueSpan.End, out var endLineNumber, out var endOffset);

            var leading = sourceText.ToString(TextSpan.FromBounds(
                sourceText.Lines[startLineNumber].Start, issueSpan.Start));
            var mid = sourceText.ToString(issueSpan);
            var trailing = sourceText.ToString(TextSpan.FromBounds(
                issueSpan.End, sourceText.Lines[endLineNumber].End));

            var contents = leading + "[|" + s_newlinePattern.Replace(mid, " ") + "|]" + trailing;
            logLine += contents + "\r\n";

            lock (_gate)
            {
                File.AppendAllText(_logFile, logLine);
            }
#endif

            return diagnostic;
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
