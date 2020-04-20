// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

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
        => options.GetInterproceduralAnalysisKindOption(rule, symbol.Locations[0].SourceTree, compilation, defaultValue, cancellationToken);

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
        => options.GetDisposeAnalysisKindOption(rule, symbol.Locations[0].SourceTree, compilation, defaultValue, cancellationToken);

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
        => options.GetDisposeOwnershipTransferAtConstructorOption(rule, symbol.Locations[0].SourceTree, compilation, defaultValue, cancellationToken);

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
        => options.GetDisposeOwnershipTransferAtMethodCall(rule, symbol.Locations[0].SourceTree, compilation, defaultValue, cancellationToken);

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
        => options.GetCopyAnalysisOption(rule, symbol.Locations[0].SourceTree, compilation, defaultValue, cancellationToken);

        public static bool GetCopyAnalysisOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            bool defaultValue,
            CancellationToken cancellationToken)
            => options.GetBoolOptionValue(EditorConfigOptionNames.CopyAnalysis, rule, tree, compilation, defaultValue, cancellationToken);
    }
}
