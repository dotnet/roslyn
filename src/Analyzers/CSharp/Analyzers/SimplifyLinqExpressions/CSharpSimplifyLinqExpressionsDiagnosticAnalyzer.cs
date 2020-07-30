// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpressions
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyLinqExpressionsDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpSimplifyLinqExpressionsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.SimplifyLinqExpressionsDiagnosticId,
                  option: null,
                  title: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Simplify_linq_expressions), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeAction, SyntaxKind.SimpleMemberAccessExpression);
        }

        private void AnalyzeAction(SyntaxNodeAnalysisContext context)
        {
            var validIdentifiers = new List<string> { "First", "Last", "Single", "Any", "Count", "SingleOrDefault", "FirstOrDefault", "LastOrDefault" };
            var memberAccess = context.Node as MemberAccessExpressionSyntax;
            var invocation = memberAccess.Expression as InvocationExpressionSyntax;

            // check if it is .Where(...)
            if (memberAccess == null ||
                !(invocation.Expression is MemberAccessExpressionSyntax expression) ||
                invocation == null ||
                expression.Name.Identifier.ValueText != "Where")
            {
                return;
            }

            // check if .Where() is followed by one of First, Last, Single, Any, Count, SingleOrDefault, FirstOrDefault, LastOrDefault
           if (!validIdentifiers.Contains(memberAccess.Name.Identifier.ValueText, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            var location = context.Node.GetLocation();
            var options = context.Compilation.Options;
            context.ReportDiagnostic(
                DiagnosticHelper.Create(Descriptor, location, Descriptor.GetEffectiveSeverity(options),
                additionalLocations: null, properties: null));

        }

    }
}
