// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Linq.Expressions;

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
            var invocation = memberAccess?.Expression as InvocationExpressionSyntax;
            var syntaxTree = context.Node.SyntaxTree;
            var semanticModel = context.SemanticModel;

            // check that this is linq to object and not something else

            // linq was implemented in C# 3.0, return is language version is less that 3.0
            if (((CSharpParseOptions)syntaxTree.Options).LanguageVersion < LanguageVersion.CSharp3)
            {
                return;
            }

            // check if it is SimpleMemberAccessExpression is Collection.Where(...)
            if (memberAccess == null ||
                invocation == null ||
                !(invocation.Expression is MemberAccessExpressionSyntax expression) ||
                expression.Name.Identifier.ValueText != "Where")
            {
                return;
            }

            // check to make sure that .Where is not user defined
            var namedType = context.Compilation?.GetTypeByMetadataName("System.Linq.Enumerable");
            var methods = namedType?.GetMembers("Where").OfType<IMethodSymbol>();

            var descendants = context.Node.Parent?.DescendantNodes().Select(m => semanticModel.GetSymbolInfo(m).Symbol);
            var originalDefinition = descendants.OfType<IMethodSymbol>()?.FirstOrDefault(m => m.OriginalDefinition.Name.Equals("Where"))?.OriginalDefinition;

            if (originalDefinition != null && !methods.Contains(originalDefinition))
            {
                return;
            }

            // check to ensure that the .Where is followed by a call with no predicate
            if (context.Node.Parent is InvocationExpressionSyntax parent && parent.ArgumentList.Arguments.Any())
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
