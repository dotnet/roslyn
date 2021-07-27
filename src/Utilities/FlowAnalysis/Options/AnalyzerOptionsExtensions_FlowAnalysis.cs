// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Analyzer.Utilities
{
    internal static partial class AnalyzerOptionsExtensions
    {
        public static InterproceduralAnalysisKind GetInterproceduralAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            InterproceduralAnalysisKind defaultValue,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetInterproceduralAnalysisKindOption(rule, tree, compilation, defaultValue, cancellationToken)
            : defaultValue;

        public static InterproceduralAnalysisKind GetInterproceduralAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            InterproceduralAnalysisKind defaultValue,
            CancellationToken cancellationToken)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.InterproceduralAnalysisKind, rule, tree, compilation, defaultValue, cancellationToken);

        public static DisposeAnalysisKind GetDisposeAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            DisposeAnalysisKind defaultValue,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetDisposeAnalysisKindOption(rule, tree, compilation, defaultValue, cancellationToken)
            : defaultValue;

        public static DisposeAnalysisKind GetDisposeAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            DisposeAnalysisKind defaultValue,
            CancellationToken cancellationToken)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.DisposeAnalysisKind, rule, tree, compilation, defaultValue, cancellationToken);

        public static bool GetDisposeOwnershipTransferAtConstructorOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            bool defaultValue,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetDisposeOwnershipTransferAtConstructorOption(rule, tree, compilation, defaultValue, cancellationToken)
            : defaultValue;

        public static bool GetDisposeOwnershipTransferAtConstructorOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            bool defaultValue,
            CancellationToken cancellationToken)
            => options.GetBoolOptionValue(EditorConfigOptionNames.DisposeOwnershipTransferAtConstructor, rule, tree, compilation, defaultValue, cancellationToken);

        public static bool GetDisposeOwnershipTransferAtMethodCall(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            bool defaultValue,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetDisposeOwnershipTransferAtMethodCall(rule, tree, compilation, defaultValue, cancellationToken)
            : defaultValue;

        public static bool GetDisposeOwnershipTransferAtMethodCall(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            bool defaultValue,
            CancellationToken cancellationToken)
            => options.GetBoolOptionValue(EditorConfigOptionNames.DisposeOwnershipTransferAtMethodCall, rule, tree, compilation, defaultValue, cancellationToken);

        public static bool GetCopyAnalysisOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            bool defaultValue,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetCopyAnalysisOption(rule, tree, compilation, defaultValue, cancellationToken)
            : defaultValue;

        public static bool GetCopyAnalysisOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            bool defaultValue,
            CancellationToken cancellationToken)
            => options.GetBoolOptionValue(EditorConfigOptionNames.CopyAnalysis, rule, tree, compilation, defaultValue, cancellationToken);

        public static PointsToAnalysisKind GetPointsToAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            PointsToAnalysisKind defaultValue,
            CancellationToken cancellationToken)
            => TryGetSyntaxTreeForOption(symbol, out var tree)
                ? options.GetPointsToAnalysisKindOption(rule, tree, compilation, defaultValue, cancellationToken)
                : defaultValue;

        public static PointsToAnalysisKind GetPointsToAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            PointsToAnalysisKind defaultValue,
            CancellationToken cancellationToken)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.PointsToAnalysisKind, rule, tree, compilation, defaultValue, cancellationToken);
    }
}
