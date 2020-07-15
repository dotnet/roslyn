// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class CompareSymbolsCorrectlyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.CompareSymbolsCorrectlyTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.CompareSymbolsCorrectlyMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.CompareSymbolsCorrectlyDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly string s_symbolTypeFullName = typeof(ISymbol).FullName;
        private const string s_symbolEqualsName = nameof(ISymbol.Equals);
        public const string SymbolEqualityComparerName = "Microsoft.CodeAnalysis.SymbolEqualityComparer";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.CompareSymbolsCorrectlyRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor GetHashCodeRule = new DiagnosticDescriptor(
            DiagnosticIds.CompareSymbolsCorrectlyRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor CollectionRule = new DiagnosticDescriptor(
            DiagnosticIds.CompareSymbolsCorrectlyRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(context =>
            {
                var compilation = context.Compilation;
                var symbolType = compilation.GetOrCreateTypeByMetadataName(s_symbolTypeFullName);
                if (symbolType is null)
                {
                    return;
                }

                // Check that the EqualityComparer exists and can be used, otherwise the Roslyn version
                // being used it too low to need the change for method references
                var symbolEqualityComparerType = compilation.GetOrCreateTypeByMetadataName(SymbolEqualityComparerName);

                context.RegisterOperationAction(
                    context => HandleBinaryOperator(in context, symbolType),
                    OperationKind.BinaryOperator);

                var builderTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                builderTypesBuilder.AddIfNotNull(compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsImmutableImmutableHashSet));
                builderTypesBuilder.AddIfNotNull(compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsImmutableImmutableDictionary));

                context.RegisterOperationAction(
                    context => HandleInvocationOperation(in context, symbolType, symbolEqualityComparerType, builderTypesBuilder.ToImmutable()),
                    OperationKind.Invocation);

                if (symbolEqualityComparerType != null)
                {

                    var collectionTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                    collectionTypesBuilder.AddIfNotNull(compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericDictionary2));
                    collectionTypesBuilder.AddIfNotNull(compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericHashSet1));
                    collectionTypesBuilder.AddIfNotNull(compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsConcurrentConcurrentDictionary2));

                    RoslynDebug.Assert(symbolEqualityComparerType != null, "SymbolEqualityComparer should not be null here.");

                    context.RegisterOperationAction(
                        context => HandleObjectCreation(in context, symbolType, symbolEqualityComparerType, collectionTypesBuilder.ToImmutable()),
                        OperationKind.ObjectCreation);
                }
            });
        }

        private static void HandleBinaryOperator(in OperationAnalysisContext context, INamedTypeSymbol symbolType)
        {
            var binary = (IBinaryOperation)context.Operation;
            if (binary.OperatorKind != BinaryOperatorKind.Equals && binary.OperatorKind != BinaryOperatorKind.NotEquals)
            {
                return;
            }

            // Allow user-defined operators
            if (binary.OperatorMethod?.ContainingSymbol is INamedTypeSymbol containingType
                && containingType.SpecialType != SpecialType.System_Object)
            {
                return;
            }

            // If either operand is 'null' or 'default', do not analyze
            if (binary.LeftOperand.HasNullConstantValue() || binary.RightOperand.HasNullConstantValue())
            {
                return;
            }

            if (!IsSymbolType(binary.LeftOperand, symbolType)
                && !IsSymbolType(binary.RightOperand, symbolType))
            {
                return;
            }

            if (binary.Language == LanguageNames.VisualBasic)
            {
                if (IsSymbolClassType(binary.LeftOperand) || IsSymbolClassType(binary.RightOperand))
                {
                    return;
                }
            }

            if (IsExplicitCastToObject(binary.LeftOperand) || IsExplicitCastToObject(binary.RightOperand))
            {
                return;
            }

            context.ReportDiagnostic(binary.Syntax.GetLocation().CreateDiagnostic(Rule));
        }

        private static void HandleInvocationOperation(in OperationAnalysisContext context, INamedTypeSymbol symbolType,
            INamedTypeSymbol? symbolEqualityComparerType, ImmutableHashSet<INamedTypeSymbol> builderTypes)
        {
            var invocationOperation = (IInvocationOperation)context.Operation;
            var method = invocationOperation.TargetMethod;

            if (invocationOperation.Instance != null && !IsSymbolType(invocationOperation.Instance, symbolType))
            {
                return;
            }

            switch (method.Name)
            {
                case nameof(object.GetHashCode):
                    context.ReportDiagnostic(invocationOperation.CreateDiagnostic(GetHashCodeRule));
                    break;

                case s_symbolEqualsName when symbolEqualityComparerType != null:
                    var parameters = invocationOperation.Arguments;
                    if (parameters.All(p => IsSymbolType(p.Value, symbolType)))
                    {
                        context.ReportDiagnostic(invocationOperation.Syntax.GetLocation().CreateDiagnostic(Rule));
                    }
                    break;

                case nameof(ImmutableHashSet.CreateBuilder):
                case nameof(ImmutableHashSet.Create):
                case nameof(ImmutableHashSet.CreateRange):
                case nameof(ImmutableHashSet.ToImmutableHashSet):
                case nameof(ImmutableDictionary.ToImmutableDictionary):
                    if (symbolEqualityComparerType == null ||
                        !builderTypes.Contains(method.ContainingType) ||
                        !(invocationOperation.Type is INamedTypeSymbol returnedType))
                    {
                        break;
                    }

                    if (method.Name == nameof(ImmutableHashSet.CreateBuilder))
                    {
                        returnedType = returnedType.ContainingType;
                    }

                    if (!returnedType.TypeArguments.IsEmpty &&
                        IsSymbolType(returnedType.TypeArguments[0], symbolType) &&
                        !invocationOperation.Arguments.Any(arg => arg.Type != null && arg.Type.Equals(symbolEqualityComparerType)))
                    {
                        context.ReportDiagnostic(invocationOperation.CreateDiagnostic(CollectionRule));

                    }
                    break;
            }
        }

        private static void HandleObjectCreation(in OperationAnalysisContext context, INamedTypeSymbol symbolType,
             INamedTypeSymbol symbolEqualityComparerType, ImmutableHashSet<INamedTypeSymbol> collectionTypes)
        {
            var objectCreation = (IObjectCreationOperation)context.Operation;

            if (objectCreation.Type is INamedTypeSymbol createdType &&
                collectionTypes.Contains(createdType.OriginalDefinition) &&
                !createdType.TypeArguments.IsEmpty &&
                IsSymbolType(createdType.TypeArguments[0], symbolType) &&
                !objectCreation.Arguments.Any(arg => arg.Type.Equals(symbolEqualityComparerType)))
            {
                context.ReportDiagnostic(objectCreation.CreateDiagnostic(CollectionRule));
            }
        }

        private static bool IsSymbolType(IOperation operation, INamedTypeSymbol symbolType)
        {
            if (operation.Type is object && IsSymbolType(operation.Type, symbolType))
            {
                return true;
            }

            if (operation is IConversionOperation conversion)
            {
                return IsSymbolType(conversion.Operand, symbolType);
            }

            return false;
        }

        private static bool IsSymbolType(ITypeSymbol typeSymbol, INamedTypeSymbol symbolType)
        {
            if (typeSymbol == null)
            {
                return false;
            }

            if (typeSymbol.Equals(symbolType))
            {
                return true;
            }

            if (typeSymbol.AllInterfaces.Contains(symbolType))
            {
                return true;
            }

            return false;
        }

        private static bool IsSymbolClassType(IOperation operation)
        {
            if (operation.Type is object)
            {
                if (operation.Type.TypeKind == TypeKind.Class
                    && operation.Type.SpecialType != SpecialType.System_Object)
                {
                    return true;
                }
            }

            if (operation is IConversionOperation conversion)
            {
                return IsSymbolClassType(conversion.Operand);
            }

            return false;
        }

        private static bool IsExplicitCastToObject(IOperation operation)
        {
            if (!(operation is IConversionOperation conversion))
            {
                return false;
            }

            if (conversion.IsImplicit)
            {
                return false;
            }

            return conversion.Type?.SpecialType == SpecialType.System_Object;
        }

        public static bool UseSymbolEqualityComparer(Compilation compilation)
        => compilation.GetOrCreateTypeByMetadataName(SymbolEqualityComparerName) is object;
    }
}
