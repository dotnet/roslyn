// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static Razor.Diagnostics.Analyzers.Resources;

namespace Razor.Diagnostics.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PooledArrayBuilderAsRefAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.PooledArrayBuilderAsRef,
        CreateLocalizableResourceString(nameof(PooledArrayBuilderAsRefTitle)),
        CreateLocalizableResourceString(nameof(PooledArrayBuilderAsRefMessage)),
        DiagnosticCategory.Reliability,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(PooledArrayBuilderAsRefDescription)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var pooledArrayBuilderExtensions = context.Compilation.GetTypeByMetadataName(WellKnownTypeNames.PooledArrayBuilderExtensions);
            if (pooledArrayBuilderExtensions is null)
            {
                return;
            }

            var pooledArrayBuilderAsRef = (IMethodSymbol?)pooledArrayBuilderExtensions.GetMembers("AsRef").SingleOrDefault();
            if (pooledArrayBuilderAsRef is null)
            {
                return;
            }

            context.RegisterOperationAction(context => AnalyzeInvocation(context, pooledArrayBuilderAsRef), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, IMethodSymbol pooledArrayBuilderAsRef)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;
        if (!SymbolEqualityComparer.Default.Equals(targetMethod.OriginalDefinition, pooledArrayBuilderAsRef))
        {
            return;
        }

        var instance = invocation.Instance ?? invocation.Arguments.FirstOrDefault()?.Value;
        if (instance is not ILocalReferenceOperation localReference)
        {
            context.ReportDiagnostic(invocation.CreateDiagnostic(Rule));
            return;
        }

        var declaration = invocation.SemanticModel!.GetOperation(localReference.Local.DeclaringSyntaxReferences.Single().GetSyntax(context.CancellationToken), context.CancellationToken);
        if (declaration is not { Parent: IVariableDeclarationOperation { Parent: IVariableDeclarationGroupOperation { Parent: IUsingOperation or IUsingDeclarationOperation } } })
        {
            context.ReportDiagnostic(invocation.CreateDiagnostic(Rule));
            return;
        }
    }
}
