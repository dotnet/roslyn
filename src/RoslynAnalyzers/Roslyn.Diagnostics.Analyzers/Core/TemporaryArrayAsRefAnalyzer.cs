// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Roslyn.Diagnostics.Analyzers
{
    using static RoslynDiagnosticsAnalyzersResources;

    /// <summary>
    /// RS0049: <inheritdoc cref="TemporaryArrayAsRefTitle"/>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class TemporaryArrayAsRefAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.TemporaryArrayAsRefRuleId,
            CreateLocalizableResourceString(nameof(TemporaryArrayAsRefTitle)),
            CreateLocalizableResourceString(nameof(TemporaryArrayAsRefMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(TemporaryArrayAsRefDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(context =>
            {
                var temporaryArrayExtensions = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisSharedCollectionsTemporaryArrayExtensions);
                if (temporaryArrayExtensions is null)
                    return;

                var temporaryArrayAsRef = (IMethodSymbol?)temporaryArrayExtensions.GetMembers("AsRef").SingleOrDefault();
                if (temporaryArrayAsRef is null)
                    return;

                context.RegisterOperationAction(context => AnalyzeInvocation(context, temporaryArrayAsRef), OperationKind.Invocation);
            });
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context, IMethodSymbol temporaryArrayAsRef)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var targetMethod = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;
            if (!Equals(targetMethod.OriginalDefinition, temporaryArrayAsRef))
                return;

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
}
