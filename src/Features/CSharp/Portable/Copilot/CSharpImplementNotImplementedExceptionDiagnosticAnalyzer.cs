// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
            {
                context.RegisterOperationBlockAction(context =>
                {
                    using var _ = SharedPools.Default<ConcurrentSet<Location>>().GetPooledObject(out var reportedLocations);
                    AnalyzeBlock(context, notImplementedExceptionType, reportedLocations);
                });
            }
        });
    }

    private void AnalyzeBlock(
        OperationBlockAnalysisContext context,
        INamedTypeSymbol notImplementedExceptionType,
        ConcurrentSet<Location> reportedLocations)
    {
        var directThrowsFound = new List<IThrowOperation>();

        // First, collect all direct throw operations that are not inside nested contexts
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
                    // Only include throws that are directly in the method body
                    if (IsDirectThrow(throwOperation, context))
                    {
                        directThrowsFound.Add(throwOperation);
                    }
                }
            }
        }

        // If we found direct throws, report diagnostics
        if (directThrowsFound.Count > 0)
        {
            // Report diagnostics for each direct throw operation
            foreach (var throwOperation in directThrowsFound)
            {
                var location = throwOperation.Syntax.GetLocation();
                if (reportedLocations.Add(location))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptor,
                        location));
                }
            }

            // Also report diagnostic on the containing member
            foreach (var location in context.OwningSymbol.Locations)
            {
                if (location.SourceTree == context.FilterTree && reportedLocations.Add(location))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptor,
                        location));
                }
            }
        }
    }

    private static bool IsDirectThrow(IThrowOperation throwOperation, OperationBlockAnalysisContext context)
    {
        // Walk up the operation tree to see if this throw is inside any nested structure
        IOperation current = throwOperation;

        while (current.Parent != null)
        {
            var parent = current.Parent;

            // Check if the parent is any of these nested contexts that should be excluded
            if (parent is IAnonymousFunctionOperation or         // Lambda or anonymous method
                ILocalFunctionOperation or                       // Local function
                ICatchClauseOperation or                         // Catch blocks
                IConditionalOperation or                         // if statements
                ILoopOperation or                                // for/foreach/while/do loops
                ISwitchOperation or                              // switch statements/cases
                ISwitchExpressionOperation or                    // switch expressions (C# 8+)
                IUsingOperation or                               // using statements/blocks
                ILockOperation or                                // lock statements
                IConditionalAccessOperation or                   // null conditional operations (?.)
                ICoalesceOperation)                              // null coalescing operations (??)
            {
                return false;
            }

            // If we reached one of the top-level operation blocks, this is a direct throw
            if (context.OperationBlocks.Contains(parent))
            {
                return true;
            }

            current = parent;
        }

        // If we directly reached the top without finding a nested context, it's a direct throw
        return true;
    }
}
