// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions;
namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer
    : AbstractRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer
{
    protected override void RegisterAttributeSyntaxAction(CompilationStartAnalysisContext context, CompilationAnalyzer compilationAnalyzer)
    {
        context.RegisterSyntaxNodeAction(context =>
        {
            var attributeList = (AttributeListSyntax)context.Node;
            switch (attributeList.Target?.Identifier.Kind())
            {
                case SyntaxKind.AssemblyKeyword:
                case SyntaxKind.ModuleKeyword:
                    foreach (var attribute in attributeList.Attributes)
                    {
                        compilationAnalyzer.AnalyzeAssemblyOrModuleAttribute(attribute, context.SemanticModel, context.ReportDiagnostic, context.CancellationToken);
                    }

                    break;
            }
        }, SyntaxKind.AttributeList);
    }
}
