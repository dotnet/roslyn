// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// #define LOG

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#if LOG
using System.IO;
using System.Text.RegularExpressions;
#endif

namespace Microsoft.CodeAnalysis.SimplifyTypeNames
{
    internal abstract class SimplifyTypeNamesDiagnosticAnalyzerBase<TLanguageKindEnum, TSimplifierOptions>
        : DiagnosticAnalyzer, IBuiltInAnalyzer
        where TLanguageKindEnum : struct
        where TSimplifierOptions : SimplifierOptions
    {
#if LOG
        private static string _logFile = @"c:\temp\simplifytypenames.txt";
        private static object _logGate = new object();
        private static readonly Regex s_newlinePattern = new Regex(@"[\r\n]+");
#endif

        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(WorkspacesResources.Name_can_be_simplified), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        private static readonly LocalizableString s_localizableTitleSimplifyNames = new LocalizableResourceString(nameof(FeaturesResources.Simplify_Names), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly DiagnosticDescriptor s_descriptorSimplifyNames = new(IDEDiagnosticIds.SimplifyNamesDiagnosticId,
                                                                    s_localizableTitleSimplifyNames,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true,
                                                                    helpLinkUri: DiagnosticHelper.GetHelpLinkForDiagnosticId(IDEDiagnosticIds.SimplifyNamesDiagnosticId),
                                                                    customTags: DiagnosticCustomTags.Unnecessary.Concat(EnforceOnBuildValues.SimplifyNames.ToCustomTag()).ToArray());

        private static readonly LocalizableString s_localizableTitleSimplifyMemberAccess = new LocalizableResourceString(nameof(FeaturesResources.Simplify_Member_Access), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly DiagnosticDescriptor s_descriptorSimplifyMemberAccess = new(IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId,
                                                                    s_localizableTitleSimplifyMemberAccess,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Style,
                                                                    DiagnosticSeverity.Hidden,
                                                                    isEnabledByDefault: true,
                                                                    helpLinkUri: DiagnosticHelper.GetHelpLinkForDiagnosticId(IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId),
                                                                    customTags: DiagnosticCustomTags.Unnecessary.Concat(EnforceOnBuildValues.SimplifyMemberAccess.ToCustomTag()).ToArray());

        private static readonly DiagnosticDescriptor s_descriptorPreferBuiltinOrFrameworkType = new(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId,
            s_localizableTitleSimplifyNames,
            s_localizableMessage,
            DiagnosticCategory.Style,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: DiagnosticHelper.GetHelpLinkForDiagnosticId(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId),
            customTags: DiagnosticCustomTags.Unnecessary.Concat(EnforceOnBuildValues.PreferBuiltInOrFrameworkType.ToCustomTag()).ToArray());

        internal abstract bool IsCandidate(SyntaxNode node);
        internal abstract bool CanSimplifyTypeNameExpression(
            SemanticModel model, SyntaxNode node, TSimplifierOptions options,
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

        public CodeActionRequestPriority RequestPriority => CodeActionRequestPriority.Normal;

        public bool OpenFileOnly(SimplifierOptions? options)
        {
            // analyzer is only active in C# and VB projects
            Contract.ThrowIfNull(options);

            return
                !(options.PreferPredefinedTypeKeywordInDeclaration.Notification.Severity is ReportDiagnostic.Warn or ReportDiagnostic.Error ||
                  options.PreferPredefinedTypeKeywordInMemberAccess.Notification.Severity is ReportDiagnostic.Warn or ReportDiagnostic.Error);
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(AnalyzeCompilation);
        }

        private void AnalyzeCompilation(CompilationStartAnalysisContext context)
        {
            var analyzer = new AnalyzerImpl(this);
            context.RegisterCodeBlockAction(analyzer.AnalyzeCodeBlock);
            context.RegisterSemanticModelAction(analyzer.AnalyzeSemanticModel);
        }

        /// <summary>
        /// Determine if a code block is eligible for analysis by <see cref="AnalyzeCodeBlock"/>.
        /// </summary>
        /// <param name="codeBlock">The syntax node provided via <see cref="CodeBlockAnalysisContext.CodeBlock"/>.</param>
        /// <returns><see langword="true"/> if the code block should be analyzed by <see cref="AnalyzeCodeBlock"/>;
        /// otherwise, <see langword="false"/> to skip analysis of the block. If a block is skipped, one or more child
        /// blocks may be analyzed by <see cref="AnalyzeCodeBlock"/>, and any remaining spans can be analyzed by
        /// <see cref="AnalyzeSemanticModel"/>.</returns>
        protected abstract bool IsIgnoredCodeBlock(SyntaxNode codeBlock);
        protected abstract ImmutableArray<Diagnostic> AnalyzeCodeBlock(CodeBlockAnalysisContext context);
        protected abstract ImmutableArray<Diagnostic> AnalyzeSemanticModel(SemanticModelAnalysisContext context, SimpleIntervalTree<TextSpan, TextSpanIntervalIntrospector>? codeBlockIntervalTree);

        protected abstract string GetLanguageName();

        public bool TrySimplify(SemanticModel model, SyntaxNode node, [NotNullWhen(true)] out Diagnostic? diagnostic, TSimplifierOptions options, CancellationToken cancellationToken)
        {
            if (!CanSimplifyTypeNameExpression(
                    model, node, options,
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

            diagnostic = CreateDiagnostic(model, options, issueSpan, diagnosticId, inDeclaration);
            return true;
        }

        internal static Diagnostic CreateDiagnostic(SemanticModel model, TSimplifierOptions options, TextSpan issueSpan, string diagnosticId, bool inDeclaration)
        {
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
                    var optionValue = inDeclaration
                        ? options.PreferPredefinedTypeKeywordInDeclaration
                        : options.PreferPredefinedTypeKeywordInMemberAccess;

                    descriptor = s_descriptorPreferBuiltinOrFrameworkType;
                    severity = optionValue.Notification.Severity;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(diagnosticId);
            }

            var tree = model.SyntaxTree;
            var builder = ImmutableDictionary.CreateBuilder<string, string?>();
            builder["OptionName"] = nameof(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess); // TODO: need the actual one
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

        private class AnalyzerImpl
        {
            private readonly SimplifyTypeNamesDiagnosticAnalyzerBase<TLanguageKindEnum, TSimplifierOptions> _analyzer;

            /// <summary>
            /// Tracks the analysis state of syntax trees in a compilation. Each syntax tree has the properties:
            /// <list type="bullet">
            /// <item><description>
            /// <para><c>completed</c>: <see langword="true"/> to indicate that <c>intervalTree</c> has been obtained
            /// for use in a <see cref="SemanticModelAnalysisContext"/> callback; otherwise, <see langword="false"/> to
            /// indicate that <c>intervalTree</c> may be updated by adding a new non-overlapping <see cref="TextSpan"/>
            /// for analysis performed by a <see cref="CodeBlockAnalysisContext"/> callback.</para>
            ///
            /// <para>This field also serves as the lock object for updating both <c>completed</c> and
            /// <c>intervalTree</c>.</para>
            /// </description></item>
            /// <item><description>
            /// <para><c>intervalTree</c>: the set of intervals analyzed by <see cref="CodeBlockAnalysisContext"/>
            /// callbacks, and therefore do not need to be analyzed again by a
            /// <see cref="SemanticModelAnalysisContext"/> callback.</para>
            ///
            /// <para>This field may only be accessed while <c>completed</c> is locked, and is not valid after
            /// <c>completed</c> is <see langword="true"/>.</para>
            /// </description></item>
            /// </list>
            /// </summary>
            private readonly ConcurrentDictionary<SyntaxTree, (StrongBox<bool> completed, SimpleIntervalTree<TextSpan, TextSpanIntervalIntrospector>? intervalTree)> _codeBlockIntervals
                = new();

            public AnalyzerImpl(SimplifyTypeNamesDiagnosticAnalyzerBase<TLanguageKindEnum, TSimplifierOptions> analyzer)
                => _analyzer = analyzer;

            public void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
            {
                if (_analyzer.IsIgnoredCodeBlock(context.CodeBlock))
                    return;

                var (completed, intervalTree) = _codeBlockIntervals.GetOrAdd(context.CodeBlock.SyntaxTree, _ => (new StrongBox<bool>(false), SimpleIntervalTree.Create(new TextSpanIntervalIntrospector(), Array.Empty<TextSpan>())));
                if (completed.Value)
                    return;

                RoslynDebug.AssertNotNull(intervalTree);
                if (!TryProceedWithInterval(addIfAvailable: false, context.CodeBlock.FullSpan, completed, intervalTree))
                    return;

                var diagnostics = _analyzer.AnalyzeCodeBlock(context);

                // After this point, cancellation is not allowed due to possible state alteration
                if (!TryProceedWithInterval(addIfAvailable: true, context.CodeBlock.FullSpan, completed, intervalTree))
                    return;

                foreach (var diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }

                static bool TryProceedWithInterval(bool addIfAvailable, TextSpan span, StrongBox<bool> completed, SimpleIntervalTree<TextSpan, TextSpanIntervalIntrospector> intervalTree)
                {
                    lock (completed)
                    {
                        if (completed.Value)
                            return false;

                        if (intervalTree.HasIntervalThatOverlapsWith(span.Start, span.End))
                            return false;

                        if (addIfAvailable)
                            intervalTree.AddIntervalInPlace(span);

                        return true;
                    }
                }
            }

            public void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
            {
                // Get the state information for the syntax tree. If the state information is not available, it is
                // initialized directly to a completed state, ensuring that concurrent (or future) calls to
                // AnalyzeCodeBlock will always read completed==true, and intervalTree does not need to be initialized
                // to a non-null value.
                var (completed, intervalTree) = _codeBlockIntervals.GetOrAdd(context.SemanticModel.SyntaxTree, syntaxTree => (new StrongBox<bool>(true), null));

                // Since SemanticModel callbacks only occur once per syntax tree, the completed state can be safely read
                // here. It will have one of the values:
                //
                //   false: the state was initialized in AnalyzeCodeBlock, and intervalTree will be a non-null tree.
                //   true: the state was initialized on the previous line, and either intervalTree will be null, or
                //         a previous call to AnalyzeSemanticModel was cancelled and the new one will operate on the
                //         same interval tree presented during the previous call.
                if (!completed.Value)
                {
                    // This lock ensures we do not use intervalTree while it is being updated by a concurrent call to
                    // AnalyzeCodeBlock.
                    lock (completed)
                    {
                        // Prevent future code block callbacks from analyzing more spans within this tree
                        completed.Value = true;
                    }
                }

                var diagnostics = _analyzer.AnalyzeSemanticModel(context, intervalTree);

                // After this point, cancellation is not allowed due to possible state alteration
                foreach (var diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
