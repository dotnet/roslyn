// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
            if (notImplementedExceptionType is null)
                return;

            context.RegisterOperationBlockAction(context => AnalyzeOperationBlock(context, notImplementedExceptionType));
        });
    }

    private void AnalyzeOperationBlock(
        OperationBlockAnalysisContext context,
        INamedTypeSymbol notImplementedExceptionType)
    {
        foreach (var block in context.OperationBlocks)
            AnalyzeBlock(block);

        void AnalyzeBlock(IOperation block)
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
                        Syntax: var throwSyntax
                    } throwOperation &&
                    notImplementedExceptionType.Equals(constructedType))
                {
                    // Report diagnostic for each throw operation
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptor,
                        throwOperation.Syntax.GetLocation()));

                    // If the throw is the top-level operation in the containing symbol, report a diagnostic on the
                    // symbol as well. Note: consider reporting on the entire symbol, instead of just the name.  And in
                    // this case, do not report directly on the throw as well.
                    if (ShouldReportAsTopLevel(block, operation))
                    {
                        foreach (var location in context.OwningSymbol.Locations)
                        {
                            if (location.SourceTree == context.FilterTree)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    Descriptor,
                                    location));
                            }
                        }
                    }
                }
            }
        }
    }

    private static bool ShouldReportAsTopLevel(IOperation block, IOperation operation)
    {
        if (block is IBlockOperation { Operations: [var child] })
        {
            // Handle: { throw new NotImplementedException(); }
            if (child == operation)
                return true;

            // Handle: => throw new NotImplementedException();
            if (child is IReturnOperation
                {
                    ReturnedValue: IConversionOperation
                    {
                        Operand: var operand
                    }
                } && operand == operation &&
                // Excluding property declarations with expression bodies
                // Because their location is an exact match with the throw operation
                // and we don't want to report the same location twice
                operation.Syntax.Parent is not ArrowExpressionClauseSyntax { Parent: PropertyDeclarationSyntax })
            {
                // Include expression-bodied methods and get accessors
                return true;
            }

            // Handle expression-bodied set accessors
            if (child is IExpressionStatementOperation { Operation: var throwOperation } && throwOperation == operation)
                return true;
        }

        return false;
    }
}
