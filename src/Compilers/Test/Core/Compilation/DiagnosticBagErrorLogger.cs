// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal sealed class DiagnosticBagErrorLogger : ErrorLogger
    {
        internal readonly DiagnosticBag Diagnostics;

        internal DiagnosticBagErrorLogger(DiagnosticBag diagnostics)
        {
            Diagnostics = diagnostics;
        }

        public override void LogDiagnostic(Diagnostic diagnostic, SuppressionInfo? suppressionInfo)
        {
            Diagnostics.Add(diagnostic);
        }

        public override void AddAnalyzerDescriptorsAndExecutionTime(ImmutableArray<(DiagnosticDescriptor Descriptor, DiagnosticDescriptorErrorLoggerInfo Info)> descriptors, double totalAnalyzerExecutionTime)
        {
        }
    }
}
