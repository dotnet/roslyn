// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpSemanticModelGetDeclaredSymbolAlwaysReturnsNullAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor DiagnosticDescriptor = new(
            DiagnosticIds.SemanticModelGetDeclaredSymbolAlwaysReturnsNull,
            CreateLocalizableResourceString(nameof(SemanticModelGetDeclaredSymbolAlwaysReturnsNullTitle)),
            CreateLocalizableResourceString(nameof(SemanticModelGetDeclaredSymbolAlwaysReturnsNullMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(SemanticModelGetDeclaredSymbolAlwaysReturnsNullDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(static context =>
            {
                var typeProvider = WellKnownTypeProvider.GetOrCreate(context.Compilation);
                IMethodSymbol? getDeclaredSymbolMethod;
                if (!typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisModelExtensions, out var modelExtensions)
                    || (getDeclaredSymbolMethod = modelExtensions.GetMembers(nameof(ModelExtensions.GetDeclaredSymbol)).FirstOrDefault() as IMethodSymbol) is null
                    || !typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCSharpSyntaxGlobalStatementSyntax, out var globalStatementSymbol)
                    || !typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCSharpSyntaxIncompleteMemberSyntax, out var incompleteMemberSymbol)
                    || !typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCSharpSyntaxBaseFieldDeclarationSyntax, out var baseFieldDeclarationSymbol))
                {
                    return;
                }

                context.RegisterOperationAction(ctx => AnalyzeInvocation(ctx, getDeclaredSymbolMethod, globalStatementSymbol, incompleteMemberSymbol, baseFieldDeclarationSymbol), OperationKind.Invocation);
            });
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context, IMethodSymbol getDeclaredSymbolMethod, INamedTypeSymbol globalStatementSymbol, INamedTypeSymbol incompleteMemberSymbol, INamedTypeSymbol baseFieldDeclarationSymbol)
        {
            var invocation = (IInvocationOperation)context.Operation;
            if (SymbolEqualityComparer.Default.Equals(invocation.TargetMethod, getDeclaredSymbolMethod))
            {
                var syntaxNodeType = invocation.Arguments[1].Value.WalkDownConversion().Type;
                if (syntaxNodeType is not null && syntaxNodeType.GetBaseTypesAndThis().ToSet().Overlaps(new[] { globalStatementSymbol, incompleteMemberSymbol, baseFieldDeclarationSymbol }))
                {
                    var diagnostic = invocation.CreateDiagnostic(DiagnosticDescriptor, syntaxNodeType.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptor);
    }
}