// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DummyAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class DummyDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public DummyDiagnosticAnalyzer()
            : base(
                "DummyDiagnosticId",
                EnforceOnBuild.HighlyRecommended,
                option: null,
                "Title")
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterOperationAction(context =>
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Operation.Syntax.GetLocation()));
            }, OperationKind.DynamicInvocation);
        }
    }
}
