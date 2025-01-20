// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.UseSystemHashCode;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
internal sealed class UseSystemHashCodeDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public UseSystemHashCodeDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseSystemHashCode,
               EnforceOnBuildValues.UseSystemHashCode,
               option: null,
               new LocalizableResourceString(nameof(AnalyzersResources.Use_System_HashCode), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
               new LocalizableResourceString(nameof(AnalyzersResources.GetHashCode_implementation_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(c =>
        {
            // var hashCodeType = c.Compilation.GetTypeByMetadataName("System.HashCode");
            if (HashCodeAnalyzer.TryGetAnalyzer(c.Compilation, out var analyzer))
            {
                c.RegisterOperationBlockAction(ctx => AnalyzeOperationBlock(analyzer, ctx));
            }
        });
    }

    private void AnalyzeOperationBlock(HashCodeAnalyzer analyzer, OperationBlockAnalysisContext context)
    {
        if (context.OperationBlocks.Length != 1)
            return;

        var owningSymbol = context.OwningSymbol;
        var diagnosticLocation = owningSymbol.Locations[0];
        if (!context.ShouldAnalyzeSpan(diagnosticLocation.SourceSpan))
            return;

        var operation = context.OperationBlocks[0];
        var (accessesBase, hashedMembers, statements) = analyzer.GetHashedMembers(owningSymbol, operation);
        var elementCount = (accessesBase ? 1 : 0) + (hashedMembers.IsDefaultOrEmpty ? 0 : hashedMembers.Length);

        // No members to call into HashCode.Combine with.  Don't offer anything here.
        if (elementCount == 0)
            return;

        // Just one member to call into HashCode.Combine. Only offer this if we have multiple statements that we can
        // reduce to a single statement.  It's not worth it to offer to replace:
        //
        //      `return x.GetHashCode();` with `return HashCode.Combine(x);`
        //
        // But it is work it to offer to replace:
        //
        //      `return (a, b).GetHashCode();` with `return HashCode.Combine(a, b);`
        if (elementCount == 1 && statements.Length < 2)
            return;

        // We've got multiple members to hash, or multiple statements that can be reduced at this point.
        Debug.Assert(elementCount >= 2 || statements.Length >= 2);

        var option = context.Options.GetAnalyzerOptions(operation.Syntax.SyntaxTree).PreferSystemHashCode;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var cancellationToken = context.CancellationToken;
        var operationLocation = operation.Syntax.GetLocation();
        var declarationLocation = context.OwningSymbol.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken).GetLocation();
        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            diagnosticLocation,
            option.Notification,
            context.Options,
            [operationLocation, declarationLocation],
            ImmutableDictionary<string, string?>.Empty));
    }
}
