// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.IO.Compression;

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

                var equalityComparerMethods = GetEqualityComparerMethodsToCheck(compilation);

                context.RegisterOperationAction(
                    context => HandleInvocationOperation(in context, symbolType, symbolEqualityComparerType, equalityComparerMethods),
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
            INamedTypeSymbol? symbolEqualityComparerType, ImmutableDictionary<string, ImmutableHashSet<INamedTypeSymbol>> equalityComparerMethods)
        {
            var invocationOperation = (IInvocationOperation)context.Operation;
            var method = invocationOperation.TargetMethod;

            switch (method.Name)
            {
                case WellKnownMemberNames.ObjectGetHashCode:
                    if (IsNotInstanceInvocationOrNotOnSymbol(invocationOperation, symbolType))
                    {
                        context.ReportDiagnostic(invocationOperation.CreateDiagnostic(GetHashCodeRule));
                    }
                    break;

                case s_symbolEqualsName:
                    if (symbolEqualityComparerType != null && IsNotInstanceInvocationOrNotOnSymbol(invocationOperation, symbolType))
                    {
                        var parameters = invocationOperation.Arguments;
                        if (parameters.All(p => IsSymbolType(p.Value, symbolType)))
                        {
                            context.ReportDiagnostic(invocationOperation.Syntax.GetLocation().CreateDiagnostic(Rule));
                        }
                    }
                    break;

                default:
                    if (equalityComparerMethods.TryGetValue(method.Name, out var possibleMethodTypes))
                    {
                        var x = invocationOperation.GetInstance();
                        if (symbolEqualityComparerType != null &&
                            possibleMethodTypes.Contains(method.ContainingType.OriginalDefinition) &&
                            !method.TypeArguments.IsEmpty &&
                            IsSymbolType(method.TypeArguments[0], symbolType) &&
                            !invocationOperation.Arguments.Any(arg => arg.Type != null && arg.Type.Equals(symbolEqualityComparerType)))
                        {
                            context.ReportDiagnostic(invocationOperation.CreateDiagnostic(CollectionRule));
                        }
                    }
                    break;
            }

            static bool IsNotInstanceInvocationOrNotOnSymbol(IInvocationOperation invocationOperation, INamedTypeSymbol symbolType)
                => invocationOperation.Instance is null || IsSymbolType(invocationOperation.Instance, symbolType);
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

        private static ImmutableDictionary<string, ImmutableHashSet<INamedTypeSymbol>> GetEqualityComparerMethodsToCheck(Compilation compilation)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<INamedTypeSymbol>.Builder>();

            if (compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsImmutableImmutableHashSet, out var immutableHashSetType))
            {
                AddOrUpdate(nameof(ImmutableHashSet.CreateBuilder), immutableHashSetType);
                AddOrUpdate(nameof(ImmutableHashSet.Create), immutableHashSetType);
                AddOrUpdate(nameof(ImmutableHashSet.CreateRange), immutableHashSetType);
                AddOrUpdate(nameof(ImmutableHashSet.ToImmutableHashSet), immutableHashSetType);
            }

            if (compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsImmutableImmutableDictionary, out var immutableDictionaryType))
            {
                AddOrUpdate(nameof(ImmutableDictionary.CreateBuilder), immutableDictionaryType);
                AddOrUpdate(nameof(ImmutableDictionary.Create), immutableDictionaryType);
                AddOrUpdate(nameof(ImmutableDictionary.CreateRange), immutableDictionaryType);
                AddOrUpdate(nameof(ImmutableDictionary.ToImmutableDictionary), immutableDictionaryType);
            }

            if (compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemLinqEnumerable, out var enumerableType))
            {
                AddOrUpdate(nameof(Enumerable.Contains), enumerableType);
                AddOrUpdate(nameof(Enumerable.Distinct), enumerableType);
                AddOrUpdate(nameof(Enumerable.GroupBy), enumerableType);
                AddOrUpdate(nameof(Enumerable.GroupJoin), enumerableType);
                AddOrUpdate(nameof(Enumerable.Intersect), enumerableType);
                AddOrUpdate(nameof(Enumerable.Join), enumerableType);
                AddOrUpdate(nameof(Enumerable.SequenceEqual), enumerableType);
                AddOrUpdate(nameof(Enumerable.ToDictionary), enumerableType);
                AddOrUpdate("ToHashSet", enumerableType);
                AddOrUpdate(nameof(Enumerable.ToLookup), enumerableType);
                AddOrUpdate(nameof(Enumerable.Union), enumerableType);
            }

            return builder.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutable());

            void AddOrUpdate(string methodName, INamedTypeSymbol typeSymbol)
            {
                if (!builder.ContainsKey(methodName))
                {
                    builder.Add(methodName, ImmutableHashSet.CreateBuilder<INamedTypeSymbol>());
                }

                builder[methodName].Add(typeSymbol);
            }
        }

        public static bool UseSymbolEqualityComparer(Compilation compilation)
        => compilation.GetOrCreateTypeByMetadataName(SymbolEqualityComparerName) is object;
    }
}
