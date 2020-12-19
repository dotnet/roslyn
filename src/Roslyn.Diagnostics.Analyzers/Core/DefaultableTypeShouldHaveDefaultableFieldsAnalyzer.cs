// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable disable warnings

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.Lightup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NullableAnnotation = Analyzer.Utilities.Lightup.NullableAnnotation;

namespace Roslyn.Diagnostics.Analyzers
{
#pragma warning disable RS1004 // Recommend adding language support to diagnostic analyzer
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning restore RS1004 // Recommend adding language support to diagnostic analyzer
    public class DefaultableTypeShouldHaveDefaultableFieldsAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DefaultableTypeShouldHaveDefaultableFieldsTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DefaultableTypeShouldHaveDefaultableFieldsMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DefaultableTypeShouldHaveDefaultableFieldsDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.DefaultableTypeShouldHaveDefaultableFieldsRuleId,
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
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(context =>
            {
                var nonDefaultableAttribute = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.RoslynUtilitiesNonDefaultableAttribute);
                if (nonDefaultableAttribute is null)
                    return;

                var knownNonDefaultableTypes = new ConcurrentDictionary<ITypeSymbol, bool>();
                context.RegisterSymbolAction(context => AnalyzeField(context, nonDefaultableAttribute, knownNonDefaultableTypes), SymbolKind.Field);
                context.RegisterSymbolAction(context => AnalyzeNamedType(context, nonDefaultableAttribute, knownNonDefaultableTypes), SymbolKind.NamedType);
            });
        }

        private static void AnalyzeField(SymbolAnalysisContext context, INamedTypeSymbol nonDefaultableAttribute, ConcurrentDictionary<ITypeSymbol, bool> knownNonDefaultableTypes)
        {
            AnalyzeField(context, (IFieldSymbol)context.Symbol, nonDefaultableAttribute, knownNonDefaultableTypes);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context, INamedTypeSymbol nonDefaultableAttribute, ConcurrentDictionary<ITypeSymbol, bool> knownNonDefaultableTypes)
        {
            var namedType = (INamedTypeSymbol)context.Symbol;
            foreach (var member in namedType.GetMembers())
            {
                if (member.Kind != SymbolKind.Field)
                    continue;

                if (!member.IsImplicitlyDeclared)
                    continue;

                AnalyzeField(context, (IFieldSymbol)member, nonDefaultableAttribute, knownNonDefaultableTypes);
            }
        }

        private static void AnalyzeField(SymbolAnalysisContext originalContext, IFieldSymbol field, INamedTypeSymbol nonDefaultableAttribute, ConcurrentDictionary<ITypeSymbol, bool> knownNonDefaultableTypes)
        {
            if (field.IsStatic)
                return;

            var containingType = field.ContainingType;
            if (containingType.TypeKind != TypeKind.Struct)
                return;

            if (!IsDefaultable(containingType, nonDefaultableAttribute, knownNonDefaultableTypes))
            {
                // A non-defaultable type is allowed to have fields of any type
                return;
            }

            if (IsDefaultable(field.Type, nonDefaultableAttribute, knownNonDefaultableTypes))
            {
                // Any type is allowed to have defaultable fields
                return;
            }

#pragma warning disable RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer
            var semanticModel = originalContext.Compilation.GetSemanticModel(field.Locations[0].SourceTree);
#pragma warning restore RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer
            if (!semanticModel.GetNullableContext(field.Locations[0].SourceSpan.Start).WarningsEnabled())
            {
                // Warnings are not enabled for this field
                return;
            }

            var sourceSymbol = (field.IsImplicitlyDeclared ? field.AssociatedSymbol : null) ?? field;
            originalContext.ReportDiagnostic(field.CreateDiagnostic(Rule, field.ContainingType, sourceSymbol.Name));
        }

        private static bool IsDefaultable(ITypeSymbol type, INamedTypeSymbol nonDefaultableAttribute, ConcurrentDictionary<ITypeSymbol, bool> knownNonDefaultableTypes)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Interface:
                case TypeKind.Delegate:
                    return type.NullableAnnotation() != NullableAnnotation.NotAnnotated;

                case TypeKind.Enum:
                    return true;

                case TypeKind.Struct:
                    if (type is not INamedTypeSymbol namedType)
                        return true;

                    if (knownNonDefaultableTypes.TryGetValue(namedType, out var isNonDefaultable))
                        return !isNonDefaultable;

                    isNonDefaultable = namedType.HasAttribute(nonDefaultableAttribute);
                    return !knownNonDefaultableTypes.GetOrAdd(namedType, isNonDefaultable);

                case TypeKind.TypeParameter:
                    if (knownNonDefaultableTypes.TryGetValue(type, out isNonDefaultable))
                        return !isNonDefaultable;

                    isNonDefaultable = type.HasAttribute(nonDefaultableAttribute);
                    return !knownNonDefaultableTypes.GetOrAdd(type, isNonDefaultable);

                case TypeKind.Unknown:
                case TypeKind.Array:
                case TypeKind.Dynamic:
                case TypeKind.Error:
                case TypeKind.Module:
                case TypeKind.Pointer:
                case TypeKind.Submission:
                default:
                    return true;
            }
        }
    }
}
