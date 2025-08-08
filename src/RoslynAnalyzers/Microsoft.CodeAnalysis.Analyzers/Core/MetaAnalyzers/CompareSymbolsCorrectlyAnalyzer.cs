// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;

    /// <summary>
    /// RS1024: <inheritdoc cref="CompareSymbolsCorrectlyTitle"/>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class CompareSymbolsCorrectlyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = CreateLocalizableResourceString(nameof(CompareSymbolsCorrectlyTitle));
        private static readonly LocalizableString s_localizableMessage = CreateLocalizableResourceString(nameof(CompareSymbolsCorrectlyMessage));
        private static readonly LocalizableString s_localizableDescription = CreateLocalizableResourceString(nameof(CompareSymbolsCorrectlyDescription));

        private static readonly string s_symbolTypeFullName = typeof(ISymbol).FullName;
        private const string s_symbolEqualsName = nameof(ISymbol.Equals);
        private const string s_HashCodeCombineName = "Combine";

        public const string SymbolEqualityComparerName = "Microsoft.CodeAnalysis.SymbolEqualityComparer";
        public const string RulePropertyName = "Rule";

        public const string EqualityRuleName = "EqualityRule";
        public const string GetHashCodeRuleName = "GetHashCodeRule";
        public const string CollectionRuleName = "CollectionRule";

        private static readonly DiagnosticDescriptor s_equalityRule = new(
            DiagnosticIds.CompareSymbolsCorrectlyRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        private static readonly DiagnosticDescriptor s_getHashCodeRule = new(
            DiagnosticIds.CompareSymbolsCorrectlyRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(CompareSymbolsCorrectlyDescriptionGetHashCode)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        private static readonly DiagnosticDescriptor s_collectionRule = new(
            DiagnosticIds.CompareSymbolsCorrectlyRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        private static readonly ImmutableDictionary<string, string?> s_EqualityRuleProperties =
            ImmutableDictionary.CreateRange([new KeyValuePair<string, string?>(RulePropertyName, EqualityRuleName)]);

        private static readonly ImmutableDictionary<string, string?> s_GetHashCodeRuleProperties =
            ImmutableDictionary.CreateRange([new KeyValuePair<string, string?>(RulePropertyName, GetHashCodeRuleName)]);

        private static readonly ImmutableDictionary<string, string?> s_CollectionRuleProperties =
            ImmutableDictionary.CreateRange([new KeyValuePair<string, string?>(RulePropertyName, CollectionRuleName)]);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(s_equalityRule);

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
                var hasSymbolEqualityComparer = UseSymbolEqualityComparer(compilation);

                context.RegisterOperationAction(
                    context => HandleBinaryOperator(in context, symbolType),
                    OperationKind.BinaryOperator);

                var equalityComparerMethods = GetEqualityComparerMethodsToCheck(compilation);
                var systemHashCode = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemHashCode);
                var iEqualityComparer = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIEqualityComparer1);

                context.RegisterOperationAction(
                    context => HandleInvocationOperation(in context, symbolType, hasSymbolEqualityComparer, equalityComparerMethods, systemHashCode, iEqualityComparer),
                    OperationKind.Invocation);

                if (hasSymbolEqualityComparer && iEqualityComparer is not null)
                {
                    var collectionTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                    collectionTypesBuilder.AddIfNotNull(compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericDictionary2));
                    collectionTypesBuilder.AddIfNotNull(compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericHashSet1));
                    collectionTypesBuilder.AddIfNotNull(compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsConcurrentConcurrentDictionary2));

                    context.RegisterOperationAction(
                        context => HandleObjectCreation(in context, symbolType, iEqualityComparer, collectionTypesBuilder.ToImmutable()),
                        OperationKind.ObjectCreation);
                }
            });
        }

        private static void HandleBinaryOperator(in OperationAnalysisContext context, INamedTypeSymbol symbolType)
        {
            var binary = (IBinaryOperation)context.Operation;
            if (binary.OperatorKind is not BinaryOperatorKind.Equals and not BinaryOperatorKind.NotEquals)
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

            if (binary.Language == LanguageNames.VisualBasic
                && (IsSymbolClassType(binary.LeftOperand) || IsSymbolClassType(binary.RightOperand)))
            {
                return;
            }

            if (IsExplicitCastToObject(binary.LeftOperand) || IsExplicitCastToObject(binary.RightOperand))
            {
                return;
            }

            context.ReportDiagnostic(binary.Syntax.GetLocation().CreateDiagnostic(s_equalityRule, s_EqualityRuleProperties));
        }

        private static void HandleInvocationOperation(
            in OperationAnalysisContext context,
            INamedTypeSymbol symbolType,
            bool hasSymbolEqualityComparer,
            ImmutableDictionary<string, ImmutableHashSet<INamedTypeSymbol>> equalityComparerMethods,
            INamedTypeSymbol? systemHashCodeType,
            INamedTypeSymbol? iEqualityComparer)
        {
            var invocationOperation = (IInvocationOperation)context.Operation;
            var method = invocationOperation.TargetMethod;

            switch (method.Name)
            {
                case WellKnownMemberNames.ObjectGetHashCode:
                    // This is a call for an instance of ISymbol.GetHashCode()
                    // without the correct arguments
                    if (IsSymbolType(invocationOperation.Instance, symbolType))
                    {
                        context.ReportDiagnostic(invocationOperation.CreateDiagnostic(s_getHashCodeRule, s_GetHashCodeRuleProperties));
                    }

                    break;

                case s_symbolEqualsName:
                    if (hasSymbolEqualityComparer && IsNotInstanceInvocationOrNotOnSymbol(invocationOperation, symbolType))
                    {
                        var parameters = invocationOperation.Arguments;
                        if (parameters.All(p => IsSymbolType(p.Value, symbolType)))
                        {
                            context.ReportDiagnostic(invocationOperation.Syntax.GetLocation().CreateDiagnostic(s_equalityRule, s_EqualityRuleProperties));
                        }
                    }

                    break;

                case s_HashCodeCombineName:
                    // A call System.HashCode.Combine(ISymbol) will do the wrong thing and should be avoided
                    if (systemHashCodeType is not null &&
                        invocationOperation.Instance is null &&
                        systemHashCodeType.Equals(method.ContainingType, SymbolEqualityComparer.Default) &&
                        invocationOperation.Arguments.Any(arg => IsSymbolType(arg.Value, symbolType)))
                    {
                        context.ReportDiagnostic(invocationOperation.CreateDiagnostic(s_getHashCodeRule, s_GetHashCodeRuleProperties));
                    }

                    break;

                default:
                    if (equalityComparerMethods.TryGetValue(method.Name, out var possibleMethodTypes) &&
                        hasSymbolEqualityComparer &&
                        possibleMethodTypes.Contains(method.ContainingType.OriginalDefinition) &&
                        IsBehavingOnSymbolType(method, symbolType) &&
                        !invocationOperation.Arguments.Any(arg => IsSymbolType(arg.Value, iEqualityComparer)))
                    {
                        context.ReportDiagnostic(invocationOperation.CreateDiagnostic(s_collectionRule, s_CollectionRuleProperties));
                    }

                    break;
            }

            static bool IsNotInstanceInvocationOrNotOnSymbol(IInvocationOperation invocationOperation, INamedTypeSymbol symbolType)
                => invocationOperation.Instance is null || IsSymbolType(invocationOperation.Instance, symbolType);

            static bool IsBehavingOnSymbolType(IMethodSymbol? method, INamedTypeSymbol symbolType)
            {
                if (method is null)
                {
                    return false;
                }
                else if (!method.TypeArguments.IsEmpty)
                {
                    var destinationTypeIndex = method.TypeParameters
                        .Select((type, index) => type.Name == "TKey" ? index : -1)
                        .FirstOrDefault(x => x >= 0);

                    Debug.Assert(destinationTypeIndex < method.TypeArguments.Length);

                    return IsSymbolType(method.TypeArguments[destinationTypeIndex], symbolType);
                }
                else if (method.ReducedFrom != null && !method.ReducedFrom.TypeArguments.IsEmpty)
                {
                    // We are in the case where the ReducedFrom has TypeArguments but the original method doesn't.
                    // This seems to happen only for VB.NET and the only workaround is to force the construction
                    // of the ReducedFrom.
                    return IsBehavingOnSymbolType(method.GetConstructedReducedFrom(), symbolType);
                }
                else
                {
                    return false;
                }
            }
        }

        private static void HandleObjectCreation(in OperationAnalysisContext context, INamedTypeSymbol symbolType,
             INamedTypeSymbol iEqualityComparerType, ImmutableHashSet<INamedTypeSymbol> collectionTypes)
        {
            var objectCreation = (IObjectCreationOperation)context.Operation;

            if (objectCreation.Type is INamedTypeSymbol createdType &&
                collectionTypes.Contains(createdType.OriginalDefinition) &&
                !createdType.TypeArguments.IsEmpty &&
                IsSymbolType(createdType.TypeArguments[0], symbolType) &&
                !objectCreation.Arguments.Any(arg => IsSymbolType(arg.Value, iEqualityComparerType)))
            {
                context.ReportDiagnostic(objectCreation.CreateDiagnostic(s_collectionRule, s_CollectionRuleProperties));
            }
        }

        private static bool IsSymbolType(IOperation? operation, INamedTypeSymbol? symbolType)
        {
            if (operation?.Type is object && IsSymbolType(operation.Type.OriginalDefinition, symbolType))
            {
                return true;
            }

            if (operation is IConversionOperation conversion)
            {
                return IsSymbolType(conversion.Operand, symbolType);
            }

            return false;
        }

        private static bool IsSymbolType(ITypeSymbol typeSymbol, INamedTypeSymbol? symbolType)
            => typeSymbol != null
                && (SymbolEqualityComparer.Default.Equals(typeSymbol, symbolType)
                    || typeSymbol.AllInterfaces.Any(SymbolEqualityComparer.Default.Equals, symbolType));

        private static bool IsSymbolClassType(IOperation operation)
        {
            if (operation.Type is object &&
                operation.Type.TypeKind == TypeKind.Class &&
                operation.Type.SpecialType != SpecialType.System_Object)
            {
                return true;
            }

            if (operation is IConversionOperation conversion)
            {
                return IsSymbolClassType(conversion.Operand);
            }

            return false;
        }

        private static bool IsExplicitCastToObject(IOperation operation)
        {
            if (operation is not IConversionOperation conversion)
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
                if (!builder.TryGetValue(methodName, out var methodTypeSymbols))
                {
                    methodTypeSymbols = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                    builder.Add(methodName, methodTypeSymbols);
                }

                methodTypeSymbols.Add(typeSymbol);
            }
        }

        public static bool UseSymbolEqualityComparer(Compilation compilation)
            => compilation.GetOrCreateTypeByMetadataName(SymbolEqualityComparerName) is object;
    }
}
