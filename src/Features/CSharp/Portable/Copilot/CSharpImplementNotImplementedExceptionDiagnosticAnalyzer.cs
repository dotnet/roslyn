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
            if (notImplementedExceptionType != null)
                context.RegisterOperationAction(context => AnalyzeThrow(context, notImplementedExceptionType), OperationKind.Throw);
        });
    }

    private void AnalyzeThrow(OperationAnalysisContext context, INamedTypeSymbol notImplementedExceptionType)
    {
        var throwOperation = (IThrowOperation)context.Operation;
        if (throwOperation is
            {
                Exception: IConversionOperation
                {
                    Operand: IObjectCreationOperation
                    {
                        Constructor.ContainingType: INamedTypeSymbol constructedType,
                    },
                },
                Syntax: ThrowExpressionSyntax or ThrowStatementSyntax,
            } &&
            notImplementedExceptionType.Equals(constructedType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptor,
                throwOperation.Syntax.GetLocation()));
        }
    }
}
