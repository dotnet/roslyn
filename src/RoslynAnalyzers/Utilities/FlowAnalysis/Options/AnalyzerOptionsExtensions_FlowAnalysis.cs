// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Analyzer.Utilities
{
    public static partial class AnalyzerOptionsExtensions
    {
        public static InterproceduralAnalysisKind GetInterproceduralAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            InterproceduralAnalysisKind defaultValue)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetInterproceduralAnalysisKindOption(rule, tree, compilation, defaultValue)
            : defaultValue;

        public static InterproceduralAnalysisKind GetInterproceduralAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            InterproceduralAnalysisKind defaultValue)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.InterproceduralAnalysisKind, rule, tree, compilation, defaultValue);

        public static DisposeAnalysisKind GetDisposeAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            DisposeAnalysisKind defaultValue)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetDisposeAnalysisKindOption(rule, tree, compilation, defaultValue)
            : defaultValue;

        public static DisposeAnalysisKind GetDisposeAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            DisposeAnalysisKind defaultValue)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.DisposeAnalysisKind, rule, tree, compilation, defaultValue);

        public static bool GetDisposeOwnershipTransferAtConstructorOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            bool defaultValue)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetDisposeOwnershipTransferAtConstructorOption(rule, tree, compilation, defaultValue)
            : defaultValue;

        public static bool GetDisposeOwnershipTransferAtConstructorOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            bool defaultValue)
            => options.GetBoolOptionValue(EditorConfigOptionNames.DisposeOwnershipTransferAtConstructor, rule, tree, compilation, defaultValue);

        public static bool GetDisposeOwnershipTransferAtMethodCall(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            bool defaultValue)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetDisposeOwnershipTransferAtMethodCall(rule, tree, compilation, defaultValue)
            : defaultValue;

        public static bool GetDisposeOwnershipTransferAtMethodCall(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            bool defaultValue)
            => options.GetBoolOptionValue(EditorConfigOptionNames.DisposeOwnershipTransferAtMethodCall, rule, tree, compilation, defaultValue);

        public static bool GetCopyAnalysisOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            bool defaultValue)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetCopyAnalysisOption(rule, tree, compilation, defaultValue)
            : defaultValue;

        public static bool GetCopyAnalysisOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            bool defaultValue)
            => options.GetBoolOptionValue(EditorConfigOptionNames.CopyAnalysis, rule, tree, compilation, defaultValue);

        public static PointsToAnalysisKind GetPointsToAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            PointsToAnalysisKind defaultValue)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetPointsToAnalysisKindOption(rule, tree, compilation, defaultValue)
            : defaultValue;

        public static PointsToAnalysisKind GetPointsToAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            PointsToAnalysisKind defaultValue)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.PointsToAnalysisKind, rule, tree, compilation, defaultValue);
    }
}
