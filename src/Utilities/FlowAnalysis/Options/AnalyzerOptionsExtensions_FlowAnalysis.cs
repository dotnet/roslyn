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
            InterproceduralAnalysisKind defaultValue,
            CancellationToken cancellationToken)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.InterproceduralAnalysisKind, rule, defaultValue, cancellationToken);

        public static DisposeAnalysisKind GetDisposeAnalysisKindOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            DisposeAnalysisKind defaultValue,
            CancellationToken cancellationToken)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.DisposeAnalysisKind, rule, defaultValue, cancellationToken);

        public static bool GetDisposeOwnershipTransferAtConstructorOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            bool defaultValue,
            CancellationToken cancellationToken)
            => options.GetBoolOptionValue(EditorConfigOptionNames.DisposeOwnershipTransferAtConstructor, rule, defaultValue, cancellationToken);

        public static bool GetDisposeOwnershipTransferAtMethodCall(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            bool defaultValue,
            CancellationToken cancellationToken)
            => options.GetBoolOptionValue(EditorConfigOptionNames.DisposeOwnershipTransferAtMethodCall, rule, defaultValue, cancellationToken);

        public static bool GetCopyAnalysisOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            bool defaultValue,
            CancellationToken cancellationToken)
            => options.GetBoolOptionValue(EditorConfigOptionNames.CopyAnalysis, rule, defaultValue, cancellationToken);
    }
}
