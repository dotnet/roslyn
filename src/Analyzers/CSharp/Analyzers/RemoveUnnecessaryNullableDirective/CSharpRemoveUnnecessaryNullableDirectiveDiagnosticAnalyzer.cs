// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Analyzers.RemoveUnnecessaryNullableDirective;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryNullableDirective
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpRemoveUnnecessaryNullableDirectiveDiagnosticAnalyzer
        : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer
    {
        public CSharpRemoveUnnecessaryNullableDirectiveDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveUnnecessaryNullableDirectiveDiagnosticId,
                   EnforceOnBuildValues.RemoveUnnecessaryNullableDirective,
                   option: null,
                   fadingOption: null,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Remove_unnecessary_nullable_directive), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Nullable_directive_is_unnecessary), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
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
        private static bool IsIgnoredCodeBlock(SyntaxNode codeBlock)
        {
            // Avoid analysis of compilation units and types in AnalyzeCodeBlock. These nodes appear in code block
            // callbacks when they include attributes, but analysis of the node at this level would block more efficient
            // analysis of descendant members.
            return codeBlock.Kind() is
                SyntaxKind.CompilationUnit or
                SyntaxKind.ClassDeclaration or
                SyntaxKind.RecordDeclaration or
                SyntaxKind.StructDeclaration or
                SyntaxKind.RecordStructDeclaration or
                SyntaxKind.InterfaceDeclaration or
                SyntaxKind.DelegateDeclaration or
                SyntaxKind.EnumDeclaration;
        }

        private static bool IsReducing([NotNullWhen(true)] NullableContextOptions? oldOptions, [NotNullWhen(true)] NullableContextOptions? newOptions)
        {
            return oldOptions is { } oldOptionsValue
                && newOptions is { } newOptionsValue
                && newOptionsValue != oldOptionsValue
                && (oldOptionsValue & newOptionsValue) == newOptionsValue;
        }

        private static ImmutableArray<TextSpan> AnalyzeCodeBlock(CodeBlockAnalysisContext context, int positionOfFirstReducingNullableDirective)
        {
            using var simplifier = new NullableImpactingSpanWalker(context.SemanticModel, positionOfFirstReducingNullableDirective, ignoredSpans: null, context.CancellationToken);
            simplifier.Visit(context.CodeBlock);
            return simplifier.Spans;
        }

        private ImmutableArray<Diagnostic> AnalyzeSemanticModel(SemanticModelAnalysisContext context, int positionOfFirstReducingNullableDirective, TextSpanIntervalTree? codeBlockIntervalTree, TextSpanIntervalTree? possibleNullableImpactIntervalTree)
        {
            var root = context.SemanticModel.SyntaxTree.GetCompilationUnitRoot(context.CancellationToken);

            using (var simplifier = new NullableImpactingSpanWalker(context.SemanticModel, positionOfFirstReducingNullableDirective, ignoredSpans: codeBlockIntervalTree, context.CancellationToken))
            {
                simplifier.Visit(root);
                possibleNullableImpactIntervalTree ??= new TextSpanIntervalTree();
                foreach (var interval in simplifier.Spans)
                {
                    possibleNullableImpactIntervalTree.AddIntervalInPlace(interval);
                }
            }

            using var diagnostics = TemporaryArray<Diagnostic>.Empty;

            var compilationOptions = ((CSharpCompilationOptions)context.SemanticModel.Compilation.Options).NullableContextOptions;

            NullableDirectiveTriviaSyntax? previousRetainedDirective = null;
            NullableContextOptions? retainedOptions = compilationOptions;

            NullableDirectiveTriviaSyntax? currentOptionsDirective = null;
            var currentOptions = retainedOptions;

            for (var directive = root.GetFirstDirective(); directive is not null; directive = directive.GetNextDirective())
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (directive is NullableDirectiveTriviaSyntax nullableDirectiveTrivia)
                {
                    // Once we reach a new directive, check to see if we can remove the previous directive
                    var removedCurrent = false;
                    if (IsReducing(retainedOptions, currentOptions))
                    {
                        // We can't have found a reducing directive and not know which directive it was
                        Contract.ThrowIfNull(currentOptionsDirective);

                        if (possibleNullableImpactIntervalTree is null
                            || !possibleNullableImpactIntervalTree.HasIntervalThatOverlapsWith(currentOptionsDirective.Span.End, nullableDirectiveTrivia.SpanStart - currentOptionsDirective.Span.End))
                        {
                            diagnostics.Add(Diagnostic.Create(Descriptor, currentOptionsDirective.GetLocation()));
                        }
                    }

                    if (!removedCurrent)
                    {
                        previousRetainedDirective = currentOptionsDirective;
                        retainedOptions = currentOptions;
                    }

                    currentOptionsDirective = nullableDirectiveTrivia;
                    currentOptions = CSharpRemoveRedundantNullableDirectiveDiagnosticAnalyzer.GetNullableContextOptions(compilationOptions, currentOptions, nullableDirectiveTrivia);
                }
                else if (directive.Kind() is
                    SyntaxKind.IfDirectiveTrivia or
                    SyntaxKind.ElifDirectiveTrivia or
                    SyntaxKind.ElseDirectiveTrivia)
                {
                    possibleNullableImpactIntervalTree ??= new TextSpanIntervalTree();
                    possibleNullableImpactIntervalTree.AddIntervalInPlace(directive.Span);
                }
            }

            // Once we reach the end of the file, check to see if we can remove the last directive
            if (IsReducing(retainedOptions, currentOptions))
            {
                // We can't have found a reducing directive and not know which directive it was
                Contract.ThrowIfNull(currentOptionsDirective);

                if (possibleNullableImpactIntervalTree is null
                    || !possibleNullableImpactIntervalTree.HasIntervalThatOverlapsWith(currentOptionsDirective.Span.End, root.Span.End - currentOptionsDirective.Span.End))
                {
                    diagnostics.Add(Diagnostic.Create(Descriptor, currentOptionsDirective.GetLocation()));
                }
            }

            return diagnostics.ToImmutableAndClear();
        }

        private sealed class SyntaxTreeState
        {
            private SyntaxTreeState(bool completed, int? positionOfFirstReducingNullableDirective)
            {
                Completed = completed;
                PositionOfFirstReducingNullableDirective = positionOfFirstReducingNullableDirective;
                if (!completed)
                {
                    IntervalTree = new TextSpanIntervalTree();
                    PossibleNullableImpactIntervalTree = new TextSpanIntervalTree();
                }
            }

            [MemberNotNullWhen(false, nameof(PositionOfFirstReducingNullableDirective), nameof(IntervalTree), nameof(PossibleNullableImpactIntervalTree))]
            public bool Completed { get; private set; }
            public int? PositionOfFirstReducingNullableDirective { get; }
            public TextSpanIntervalTree? IntervalTree { get; }
            public TextSpanIntervalTree? PossibleNullableImpactIntervalTree { get; }

            public static SyntaxTreeState Create(bool defaultCompleted, NullableContextOptions compilationOptions, SyntaxTree tree, CancellationToken cancellationToken)
            {
                var root = tree.GetCompilationUnitRoot(cancellationToken);

                // This analyzer only needs to process syntax trees that contain at least one #nullable directive that
                // reduces the nullable analysis scope.
                int? positionOfFirstReducingNullableDirective = null;

                NullableContextOptions? currentOptions = compilationOptions;
                for (var directive = root.GetFirstDirective(); directive is not null; directive = directive.GetNextDirective())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (directive is NullableDirectiveTriviaSyntax nullableDirectiveTrivia)
                    {
                        var newOptions = CSharpRemoveRedundantNullableDirectiveDiagnosticAnalyzer.GetNullableContextOptions(compilationOptions, currentOptions, nullableDirectiveTrivia);
                        if (IsReducing(currentOptions, newOptions))
                        {
                            positionOfFirstReducingNullableDirective = directive.SpanStart;
                            break;
                        }

                        currentOptions = newOptions;
                    }
                }

                return new SyntaxTreeState(completed: defaultCompleted || positionOfFirstReducingNullableDirective is null, positionOfFirstReducingNullableDirective);
            }

            [MemberNotNullWhen(true, nameof(PositionOfFirstReducingNullableDirective), nameof(IntervalTree), nameof(PossibleNullableImpactIntervalTree))]
            public bool TryProceedWithInterval(TextSpan span)
                => TryProceedOrReportNullableImpactingSpans(span, nullableImpactingSpans: null);

            [MemberNotNullWhen(true, nameof(PositionOfFirstReducingNullableDirective), nameof(IntervalTree), nameof(PossibleNullableImpactIntervalTree))]
            public bool TryReportNullableImpactingSpans(TextSpan span, ImmutableArray<TextSpan> nullableImpactingSpans)
                => TryProceedOrReportNullableImpactingSpans(span, nullableImpactingSpans);

            [MemberNotNullWhen(true, nameof(PositionOfFirstReducingNullableDirective), nameof(IntervalTree), nameof(PossibleNullableImpactIntervalTree))]
            private bool TryProceedOrReportNullableImpactingSpans(TextSpan span, ImmutableArray<TextSpan>? nullableImpactingSpans)
            {
                if (Completed)
                    return false;

                lock (this)
                {
                    if (Completed)
                        return false;

                    if (IntervalTree.HasIntervalThatOverlapsWith(span.Start, span.End))
                        return false;

                    if (nullableImpactingSpans is { } spans)
                    {
                        foreach (var nullableImpactingSpan in spans)
                            PossibleNullableImpactIntervalTree.AddIntervalInPlace(nullableImpactingSpan);
                    }

                    return true;
                }
            }

            internal void MarkComplete()
            {
                if (Completed)
                    return;

                lock (this)
                {
                    Completed = true;
                }
            }
        }

        private class AnalyzerImpl(CSharpRemoveUnnecessaryNullableDirectiveDiagnosticAnalyzer analyzer)
        {
            private readonly CSharpRemoveUnnecessaryNullableDirectiveDiagnosticAnalyzer _analyzer = analyzer;

            /// <summary>
            /// Tracks the analysis state of syntax trees in a compilation.
            /// </summary>
            private readonly ConcurrentDictionary<SyntaxTree, SyntaxTreeState> _codeBlockIntervals
                = new();

            public void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
            {
                if (IsIgnoredCodeBlock(context.CodeBlock))
                    return;

                var root = context.GetAnalysisRoot(findInTrivia: true);

                // Bail out if the root contains no nullable directives.
                if (!root.ContainsDirective(SyntaxKind.NullableDirectiveTrivia))
                    return;

                var syntaxTreeState = GetOrCreateSyntaxTreeState(context.CodeBlock.SyntaxTree, defaultCompleted: false, context.SemanticModel, context.CancellationToken);
                if (!syntaxTreeState.TryProceedWithInterval(context.CodeBlock.FullSpan))
                    return;

                var nullableImpactingSpans = CSharpRemoveUnnecessaryNullableDirectiveDiagnosticAnalyzer.AnalyzeCodeBlock(context, syntaxTreeState.PositionOfFirstReducingNullableDirective.Value);

                // After this point, cancellation is not allowed due to possible state alteration
                syntaxTreeState.TryReportNullableImpactingSpans(context.CodeBlock.FullSpan, nullableImpactingSpans);
            }

            public void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
            {
                var root = context.GetAnalysisRoot(findInTrivia: true);

                // Bail out if the root contains no nullable directives.
                if (!root.ContainsDirective(SyntaxKind.NullableDirectiveTrivia))
                    return;

                // Get the state information for the syntax tree. If the state information is not available, it is
                // initialized directly to a completed state, ensuring that concurrent (or future) calls to
                // AnalyzeCodeBlock will always read completed==true, and intervalTree does not need to be initialized
                // to a non-null value.
                var syntaxTreeState = GetOrCreateSyntaxTreeState(context.SemanticModel.SyntaxTree, defaultCompleted: true, context.SemanticModel, context.CancellationToken);

                syntaxTreeState.MarkComplete();

                if (syntaxTreeState.PositionOfFirstReducingNullableDirective is not { } positionOfFirstReducingNullableDirective)
                    return;

                var diagnostics = _analyzer.AnalyzeSemanticModel(context, positionOfFirstReducingNullableDirective, syntaxTreeState.IntervalTree, syntaxTreeState.PossibleNullableImpactIntervalTree);

                // After this point, cancellation is not allowed due to possible state alteration
                foreach (var diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }

            private SyntaxTreeState GetOrCreateSyntaxTreeState(SyntaxTree tree, bool defaultCompleted, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                return _codeBlockIntervals.GetOrAdd(
                    tree,
                    static (tree, arg) => SyntaxTreeState.Create(arg.defaultCompleted, arg.options, tree, arg.cancellationToken),
                    (defaultCompleted, options: ((CSharpCompilationOptions)semanticModel.Compilation.Options).NullableContextOptions, cancellationToken));
            }
        }
    }
}
