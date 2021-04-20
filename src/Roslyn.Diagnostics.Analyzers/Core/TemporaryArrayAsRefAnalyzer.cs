// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Roslyn.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class TemporaryArrayAsRefAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.TemporaryArrayAsRefTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.TemporaryArrayAsRefMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.TemporaryArrayAsRefDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.TemporaryArrayAsRefRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(context =>
            {
                var temporaryArrayExtensions = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisSharedCollectionsTemporaryArrayExtensions);
                if (temporaryArrayExtensions is null)
                    return;

                var temporaryArrayAsRef = (IMethodSymbol)temporaryArrayExtensions.GetMembers("AsRef").SingleOrDefault();
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

            var declaration = invocation.SemanticModel.GetOperation(localReference.Local.DeclaringSyntaxReferences.Single().GetSyntax(context.CancellationToken), context.CancellationToken);
            if (declaration is not { Parent: IVariableDeclarationOperation { Parent: IVariableDeclarationGroupOperation { Parent: IUsingOperation or IUsingDeclarationOperation } } })
            {
                context.ReportDiagnostic(invocation.CreateDiagnostic(Rule));
                return;
            }
        }
    }
}
