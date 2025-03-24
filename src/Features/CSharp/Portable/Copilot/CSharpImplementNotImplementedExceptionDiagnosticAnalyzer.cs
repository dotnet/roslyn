// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpImplementNotImplementedExceptionDiagnosticAnalyzer()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.CopilotImplementNotImplementedExceptionDiagnosticId,
        EnforceOnBuildValues.CopilotImplementNotImplementedException,
        option: null,
        new LocalizableResourceString(
            nameof(CSharpAnalyzersResources.Implement_with_Copilot), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        configurable: false)
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            var notImplementedExceptionType = context.Compilation.GetTypeByMetadataName(typeof(NotImplementedException).FullName!);
            if (notImplementedExceptionType != null)
                context.RegisterOperationBlockAction(context => AnalyzeBlock(context, notImplementedExceptionType));
        });
    }

    private void AnalyzeBlock(OperationBlockAnalysisContext context, INamedTypeSymbol notImplementedExceptionType)
    {
        var hasThrowOperation = false;
        foreach (var block in context.OperationBlocks)
        {
            foreach (var operation in block.DescendantsAndSelf())
            {
                if (operation is IThrowOperation
                    {
                        Exception: IConversionOperation
                        {
                            Operand: IObjectCreationOperation
                            {
                                Constructor.ContainingType: INamedTypeSymbol constructedType,
                            },
                        },
                        Syntax: ThrowExpressionSyntax or ThrowStatementSyntax,
                    } throwOperation &&
                    notImplementedExceptionType.Equals(constructedType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptor,
                        throwOperation.Syntax.GetLocation()));
                    hasThrowOperation = true;
                }
            }
        }

        if (hasThrowOperation)
        {
            foreach (var location in context.OwningSymbol.Locations)
            {
                if (location.SourceTree == context.FilterTree)
                {
                    // Report diagnostic on the member name token
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptor,
                        location));
                }
            }
        }
    }
}
