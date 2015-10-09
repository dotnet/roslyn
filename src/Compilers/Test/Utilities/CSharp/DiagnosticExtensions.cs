// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class DiagnosticExtensions
    {
        public static void Verify(this IEnumerable<DiagnosticInfo> actual, params DiagnosticDescription[] expected)
        {
            actual.Select(info => new CSDiagnostic(info, NoLocation.Singleton)).Verify(expected);
        }

        public static void Verify(this ImmutableArray<DiagnosticInfo> actual, params DiagnosticDescription[] expected)
        {
            actual.Select(info => new CSDiagnostic(info, NoLocation.Singleton)).Verify(expected);
        }

        public static string ToLocalizedString(this MessageID id)
        {
            return new LocalizableErrorArgument(id).ToString(null, null);
        }

        public static ImmutableArray<Diagnostic> GetDiagnosticsForSyntaxTree(
            this CSharpCompilation compilation,
            CompilationStage stage,
            SyntaxTree tree,
            TextSpan? filterSpanWithinTree,
            bool includeEarlierStages = true)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            return compilation.GetDiagnosticsForSemanticModel(stage, semanticModel, filterSpanWithinTree, includeEarlierStages);
        }
    }
}
