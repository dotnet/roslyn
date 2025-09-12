// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SimplifyTypeNames;

internal abstract class SimplifyTypeNamesDiagnosticAnalyzerBase<TLanguageKindEnum, TSimplifierOptions>
    : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer
    where TLanguageKindEnum : struct
    where TSimplifierOptions : SimplifierOptions
{
    private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(AnalyzersResources.Name_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

    private static readonly LocalizableString s_localizableTitleSimplifyNames = new LocalizableResourceString(nameof(AnalyzersResources.Simplify_Names), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
    private static readonly DiagnosticDescriptor s_descriptorSimplifyNames = CreateDescriptorWithId(IDEDiagnosticIds.SimplifyNamesDiagnosticId,
        EnforceOnBuildValues.SimplifyNames,
        hasAnyCodeStyleOption: false,
        s_localizableTitleSimplifyNames,
        s_localizableMessage,
        isUnnecessary: true);

    private static readonly LocalizableString s_localizableTitleSimplifyMemberAccess = new LocalizableResourceString(nameof(AnalyzersResources.Simplify_Member_Access), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
    private static readonly DiagnosticDescriptor s_descriptorSimplifyMemberAccess = CreateDescriptorWithId(IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId,
        EnforceOnBuildValues.SimplifyMemberAccess,
        hasAnyCodeStyleOption: false,
        s_localizableTitleSimplifyMemberAccess,
        s_localizableMessage,
        isUnnecessary: true);

    private static readonly DiagnosticDescriptor s_descriptorPreferBuiltinOrFrameworkType = CreateDescriptorWithId(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId,
        EnforceOnBuildValues.PreferBuiltInOrFrameworkType,
        hasAnyCodeStyleOption: true,
        s_localizableTitleSimplifyNames,
        s_localizableMessage,
        isUnnecessary: true);

    protected SimplifyTypeNamesDiagnosticAnalyzerBase()
        : base(
            [
                (s_descriptorSimplifyNames, []),
                (s_descriptorSimplifyMemberAccess, []),
                (s_descriptorPreferBuiltinOrFrameworkType,
                [
                    CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration,
                    CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess,
                ])
            ],
            fadingOption: null)
    {
    }

    internal abstract bool IsCandidate(SyntaxNode node);
    internal abstract bool CanSimplifyTypeNameExpression(
        SemanticModel model, SyntaxNode node, TSimplifierOptions options,
        out TextSpan issueSpan, out string diagnosticId, out bool inDeclaration,
        CancellationToken cancellationToken);

    protected static ImmutableArray<NotificationOption2> GetAllNotifications(SimplifierOptions options)
        => [
            options.PreferPredefinedTypeKeywordInDeclaration.Notification,
            options.PreferPredefinedTypeKeywordInMemberAccess.Notification,
        ];

    protected sealed override void InitializeWorker(AnalysisContext context)
    {
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
    protected abstract ImmutableArray<Diagnostic> AnalyzeCodeBlock(CodeBlockAnalysisContext context, SyntaxNode root);
    protected abstract ImmutableArray<Diagnostic> AnalyzeSemanticModel(SemanticModelAnalysisContext context, SyntaxNode root, TextSpanMutableIntervalTree? codeBlockIntervalTree);

    public bool TrySimplify(SemanticModel model, SyntaxNode node, [NotNullWhen(true)] out Diagnostic? diagnostic, TSimplifierOptions options, AnalyzerOptions analyzerOptions, CancellationToken cancellationToken)
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

        diagnostic = CreateDiagnostic(model, options, analyzerOptions, issueSpan, diagnosticId, inDeclaration);
        return true;
    }

    internal static Diagnostic CreateDiagnostic(SemanticModel model, TSimplifierOptions options, AnalyzerOptions analyzerOptions, TextSpan issueSpan, string diagnosticId, bool inDeclaration)
    {
        DiagnosticDescriptor descriptor;
        NotificationOption2 notificationOption;
        switch (diagnosticId)
        {
            case IDEDiagnosticIds.SimplifyNamesDiagnosticId:
                descriptor = s_descriptorSimplifyNames;
                notificationOption = descriptor.DefaultSeverity.ToNotificationOption(isOverridenSeverity: false);
                break;

            case IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId:
                descriptor = s_descriptorSimplifyMemberAccess;
                notificationOption = descriptor.DefaultSeverity.ToNotificationOption(isOverridenSeverity: false);
                break;

            case IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId:
                var optionValue = inDeclaration
                    ? options.PreferPredefinedTypeKeywordInDeclaration
                    : options.PreferPredefinedTypeKeywordInMemberAccess;

                descriptor = s_descriptorPreferBuiltinOrFrameworkType;
                notificationOption = optionValue.Notification;
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(diagnosticId);
        }

        var tree = model.SyntaxTree;
        var builder = ImmutableDictionary.CreateBuilder<string, string?>();
        builder["OptionName"] = nameof(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess); // TODO: need the actual one
        builder["OptionLanguage"] = model.Language;
        var diagnostic = DiagnosticHelper.Create(descriptor, tree.GetLocation(issueSpan), notificationOption, analyzerOptions, additionalLocations: null, builder.ToImmutable());

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

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    private sealed class AnalyzerImpl(SimplifyTypeNamesDiagnosticAnalyzerBase<TLanguageKindEnum, TSimplifierOptions> analyzer)
    {
        private readonly SimplifyTypeNamesDiagnosticAnalyzerBase<TLanguageKindEnum, TSimplifierOptions> _analyzer = analyzer;

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
        private readonly ConcurrentDictionary<SyntaxTree, (StrongBox<bool> completed, TextSpanMutableIntervalTree? intervalTree)> _codeBlockIntervals = [];

        public void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
        {
            if (_analyzer.IsIgnoredCodeBlock(context.CodeBlock))
                return;

            var (completed, intervalTree) = _codeBlockIntervals.GetOrAdd(context.CodeBlock.SyntaxTree, _ => (new StrongBox<bool>(false), new TextSpanMutableIntervalTree()));
            if (completed.Value)
                return;

            RoslynDebug.AssertNotNull(intervalTree);
            if (!TryProceedWithInterval(addIfAvailable: false, context.CodeBlock.FullSpan, completed, intervalTree))
                return;

            var root = context.GetAnalysisRoot(findInTrivia: true);
            var diagnostics = _analyzer.AnalyzeCodeBlock(context, root);

            // After this point, cancellation is not allowed due to possible state alteration
            if (!TryProceedWithInterval(addIfAvailable: root == context.CodeBlock, context.CodeBlock.FullSpan, completed, intervalTree))
                return;

            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            static bool TryProceedWithInterval(bool addIfAvailable, TextSpan span, StrongBox<bool> completed, TextSpanMutableIntervalTree intervalTree)
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
            if (!completed.Value && !context.FilterSpan.HasValue)
            {
                // This lock ensures we do not use intervalTree while it is being updated by a concurrent call to
                // AnalyzeCodeBlock.
                lock (completed)
                {
                    // Prevent future code block callbacks from analyzing more spans within this tree
                    completed.Value = true;
                }
            }

            var root = context.GetAnalysisRoot(findInTrivia: true);
            var diagnostics = _analyzer.AnalyzeSemanticModel(context, root, intervalTree);

            // After this point, cancellation is not allowed due to possible state alteration
            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
