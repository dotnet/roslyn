// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
        internal static readonly DiagnosticDescriptor DiagnosticDescriptor = new(
            DiagnosticIds.SemanticModelGetDeclaredSymbolAlwaysReturnsNull,
            CreateLocalizableResourceString(nameof(SemanticModelGetDeclaredSymbolAlwaysReturnsNullTitle)),
            CreateLocalizableResourceString(nameof(SemanticModelGetDeclaredSymbolAlwaysReturnsNullMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(SemanticModelGetDeclaredSymbolAlwaysReturnsNullDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        internal static readonly DiagnosticDescriptor FieldDiagnosticDescriptor = new(
            DiagnosticIds.SemanticModelGetDeclaredSymbolAlwaysReturnsNullForField,
            CreateLocalizableResourceString(nameof(SemanticModelGetDeclaredSymbolAlwaysReturnsNullTitle)),
            CreateLocalizableResourceString(nameof(SemanticModelGetDeclaredSymbolAlwaysReturnsNullForFieldMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(SemanticModelGetDeclaredSymbolAlwaysReturnsNullForFieldDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptor, FieldDiagnosticDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(static context =>
            {
                var typeProvider = WellKnownTypeProvider.GetOrCreate(context.Compilation);
                IMethodSymbol? getDeclaredSymbolMethod;
                if (!typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCSharpCSharpExtensions, out var csharpExtensions)
                    || !typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisModelExtensions, out var modelExtensions)
                    || !typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCSharpSyntaxBaseFieldDeclarationSyntax, out var baseFieldDeclaration)
                    || !typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCSharpSyntaxLocalFunctionStatementSyntax, out var localFunctionStatement)
                    || !typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisSyntaxNode, out var syntaxNode)
                    || (getDeclaredSymbolMethod = (IMethodSymbol?)modelExtensions.GetMembers(nameof(ModelExtensions.GetDeclaredSymbol)).FirstOrDefault(m => m is IMethodSymbol { Parameters.Length: >= 2 })) is null)
                {
                    return;
                }

                var allowedTypes = csharpExtensions.GetMembers(nameof(CSharpExtensions.GetDeclaredSymbol))
                    .OfType<IMethodSymbol>()
                    .Where(m => m.Parameters.Length >= 2)
                    .Select(m => m.Parameters[1].Type);

                context.RegisterOperationAction(ctx => AnalyzeInvocation(ctx, getDeclaredSymbolMethod, allowedTypes, baseFieldDeclaration, localFunctionStatement, syntaxNode), OperationKind.Invocation);
            });
        }

        private static void AnalyzeInvocation(
            OperationAnalysisContext context,
            IMethodSymbol getDeclaredSymbolMethod,
            IEnumerable<ITypeSymbol> allowedTypes,
            INamedTypeSymbol baseFieldDeclarationType,
            INamedTypeSymbol localFunctionStatementType,
            INamedTypeSymbol syntaxNodeType)
        {
            var invocation = (IInvocationOperation)context.Operation;
            if (SymbolEqualityComparer.Default.Equals(invocation.TargetMethod, getDeclaredSymbolMethod))
            {
                var syntaxNodeDerivingType = invocation.Arguments.GetArgumentForParameterAtIndex(1).Value.WalkDownConversion().Type;
                if (syntaxNodeDerivingType is null || syntaxNodeDerivingType.Equals(syntaxNodeType))
                {
                    return;
                }

                if (syntaxNodeDerivingType.DerivesFrom(baseFieldDeclarationType))
                {
                    context.ReportDiagnostic(invocation.CreateDiagnostic(FieldDiagnosticDescriptor, syntaxNodeDerivingType.Name));
                }
                else if (allowedTypes.All(type => !syntaxNodeDerivingType.DerivesFrom(type, baseTypesOnly: true, checkTypeParameterConstraints: false)
                                                  && !syntaxNodeDerivingType.Equals(localFunctionStatementType, SymbolEqualityComparer.Default)))
                {
                    context.ReportDiagnostic(invocation.CreateDiagnostic(DiagnosticDescriptor, syntaxNodeDerivingType.Name));
                }
            }
        }
    }
}
