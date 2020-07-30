// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;

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
            if (!(isWhereClause(context) && canRefactor(context)))
            {
                return;
            }
            var location = context.Node.GetLocation();
            var options = context.Compilation.Options;
            context.ReportDiagnostic(
                DiagnosticHelper.Create(Descriptor, location, Descriptor.GetEffectiveSeverity(options),
                additionalLocations: null, properties: null));

        }

        private bool canRefactor(SyntaxNodeAnalysisContext context)
        {
            return true;
        }

        private static bool isWhereClause(SyntaxNodeAnalysisContext context)
        {
            //get the inner most invocatio
            var parent = context.Node.Parent;
            var children = parent.ChildNodes();
            var something = (InvocationExpressionSyntax)context.Node.Parent;
            var memberAccess = something.Expression as MemberAccessExpressionSyntax;

      

            return true;
        }
    }
}
