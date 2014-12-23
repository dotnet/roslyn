using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer]
    public sealed class EquatableAnalyzer : DiagnosticAnalyzer
    {
        private const string IEquatableMetadataName = "System.IEquatable`1";

        private static LocalizableString localizableTitleImplementIEquatable = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.ImplementIEquatableDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString localizableMessageImplementIEquatable = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.ImplementIEquatableMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        private static readonly DiagnosticDescriptor ImplementIEquatableDescriptor = new DiagnosticDescriptor(
            RoslynDiagnosticIds.ImplementIEquatableRuleId,
            localizableTitleImplementIEquatable,
            localizableMessageImplementIEquatable,
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static LocalizableString localizableTitleOverridesObjectEquals = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.OverrideObjectEqualsDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString localizableMessageOverridesObjectEquals = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.OverrideObjectEqualsMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        private static readonly DiagnosticDescriptor OverridesObjectEqualsDescriptor = new DiagnosticDescriptor(
            RoslynDiagnosticIds.OverrideObjectEqualsRuleId,
            localizableTitleOverridesObjectEquals,
            localizableMessageOverridesObjectEquals,
            "Reliability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
       
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(ImplementIEquatableDescriptor, OverridesObjectEqualsDescriptor);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(InitializeCore);
        }

        private void InitializeCore(CompilationStartAnalysisContext context)
        {
            var objectType = context.Compilation.GetSpecialType(SpecialType.System_Object);
            var equatableType = context.Compilation.GetTypeByMetadataName(IEquatableMetadataName);
            if (objectType != null && equatableType != null)
            {
                context.RegisterSymbolAction(c => AnalyzeSymbol(c, objectType, equatableType), SymbolKind.NamedType);
            }
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol objectType, INamedTypeSymbol equatableType)
        {
            var namedType = context.Symbol as INamedTypeSymbol;
            if (namedType == null || !(namedType.TypeKind == TypeKind.Struct || namedType.TypeKind == TypeKind.Class))
            {
                return;
            }

            var methodSymbol = namedType
                .GetMembers("Equals")
                .OfType<IMethodSymbol>()
                .Where(m => IsObjectEqualsOverride(m, objectType))
                .FirstOrDefault();
            var overridesObjectEquals = methodSymbol != null;

            var constructedEquatable = equatableType.Construct(namedType);
            var implementation = namedType
                .Interfaces
                .Where(x => x.Equals(constructedEquatable))
                .FirstOrDefault();
            var implementsEquatable = implementation != null;

            if (overridesObjectEquals && !implementsEquatable && namedType.TypeKind == TypeKind.Struct)
            { 
                context.ReportDiagnostic(Diagnostic.Create(ImplementIEquatableDescriptor, methodSymbol.Locations[0], namedType));
            }

            if (!overridesObjectEquals && implementsEquatable)
            {
                context.ReportDiagnostic(Diagnostic.Create(OverridesObjectEqualsDescriptor, namedType.Locations[0], namedType));
            }
        }

        private bool IsObjectEqualsOverride(IMethodSymbol methodSymbol, INamedTypeSymbol objectType)
        {
            if (!methodSymbol.IsOverride)
            {
                return false;
            }

            if (methodSymbol.Parameters.Length != 1 || 
                !methodSymbol.Parameters[0].Type.Equals(objectType))
            {
                return false;
            }

            if (methodSymbol.ReturnType.SpecialType != SpecialType.System_Boolean)
            {
                return false;
            }

            do
            {
                methodSymbol = methodSymbol.OverriddenMethod;
            } 
            while (methodSymbol.IsOverride);

            return methodSymbol.ContainingType.Equals(objectType);
        }
    }
}
