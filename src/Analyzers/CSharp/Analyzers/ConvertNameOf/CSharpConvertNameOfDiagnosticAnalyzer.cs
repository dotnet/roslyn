// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNameOf
{
    /// <summary>
    /// Finds code like typeof(someType).Name and determines whether it can be changed to nameof(someType), if yes then it offers a diagnostic
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpConvertNameOfDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpConvertNameOfDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.ConvertNameOfDiagnosticId,
                   CSharpCodeStyleOptions.PreferBraces,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(
                       nameof(CSharpAnalyzersResources.Convert_type_name_to_nameof), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxAction, SyntaxKind.TypeOfExpression);
        }

        private void AnalyzeSyntaxAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            //var options = syntaxContext.Options;
            var syntaxTree = syntaxContext.Node.SyntaxTree;
            //var cancellationToken = syntaxContext.CancellationToken;
            var node = syntaxContext.Node;

            // TODO: Any relevant style options?

            // nameof was added in CSharp 6.0, so don't offer it for any languages after that time
            if (((CSharpParseOptions)syntaxTree.Options).LanguageVersion < LanguageVersion.CSharp6)
            {
                return;
            }

            // TODO: Check for compiler errors on the typeof(someType).Name declaration

            // TODO: Check that the current span is the case we're looking for

            // TODO: Filter cases that don't work

            // TODO: Create and report the right diagnostic
            var location = Location.Create(syntaxTree, node.Span);
            var additionalLocations = ImmutableArray.Create(node.GetLocation());

            syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                     Descriptor,
                     location,
                     ReportDiagnostic.Hidden,
                     additionalLocations,
                     properties: null));
        }
        // TODO: Overwrite GetAnalyzerCategory
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        // HELPERS GO HERE
    }
}
