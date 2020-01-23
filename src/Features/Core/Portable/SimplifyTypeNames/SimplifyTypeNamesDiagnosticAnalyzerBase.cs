// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// #define LOG

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#if LOG
using System.IO;
using System.Text.RegularExpressions;
#endif

namespace Microsoft.CodeAnalysis.SimplifyTypeNames
{
    internal abstract class SimplifyTypeNamesDiagnosticAnalyzerBase<TLanguageKindEnum> : DiagnosticAnalyzer, IBuiltInAnalyzer where TLanguageKindEnum : struct
    {
#if LOG
        private static string _logFile = @"c:\temp\simplifytypenames.txt";
        private static object _logGate = new object();
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

        protected SimplifyTypeNamesDiagnosticAnalyzerBase()
        {
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

        public sealed override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(AnalyzeCompilation);
        }

        private void AnalyzeCompilation(CompilationStartAnalysisContext context)
        {
            context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        }

        protected abstract void AnalyzeSemanticModel(SemanticModelAnalysisContext context);

        protected abstract string GetLanguageName();

        public bool TrySimplify(SemanticModel model, SyntaxNode node, out Diagnostic diagnostic, OptionSet optionSet, CancellationToken cancellationToken)
        {
            if (!CanSimplifyTypeNameExpression(
                    model, node, optionSet,
                    out var issueSpan, out var diagnosticId, out var inDeclaration,
                    cancellationToken))
            {
                diagnostic = null;
                return false;
            }

            if (model.SyntaxTree.OverlapsHiddenPosition(issueSpan, cancellationToken))
            {
                diagnostic = null;
                return false;
            }

            diagnostic = CreateDiagnostic(model, optionSet, issueSpan, diagnosticId, inDeclaration);
            return true;
        }

        internal static Diagnostic CreateDiagnostic(SemanticModel model, OptionSet optionSet, TextSpan issueSpan, string diagnosticId, bool inDeclaration)
        {
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
            var diagnostic = DiagnosticHelper.Create(descriptor, tree.GetLocation(issueSpan), severity, additionalLocations: null, builder.ToImmutable());

#if LOG
            var sourceText = tree.GetText();
            sourceText.GetLineAndOffset(issueSpan.Start, out var startLineNumber, out var startOffset);
            sourceText.GetLineAndOffset(issueSpan.End, out var endLineNumber, out var endOffset);
            var logLine = tree.FilePath + "," + startLineNumber + "\t" + diagnosticId + "\t" + inDeclaration + "\t";

            var leading = sourceText.ToString(TextSpan.FromBounds(
                sourceText.Lines[startLineNumber].Start, issueSpan.Start));
            var mid = sourceText.ToString(issueSpan);
            var trailing = sourceText.ToString(TextSpan.FromBounds(
                issueSpan.End, sourceText.Lines[endLineNumber].End));

            var contents = leading + "[|" + s_newlinePattern.Replace(mid, " ") + "|]" + trailing;
            logLine += contents + "\r\n";

            lock (_logGate)
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
