// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using DiagnosticIds = Roslyn.Diagnostics.Analyzers.RoslynDiagnosticIds;

namespace Microsoft.CodeAnalysis.BannedApiAnalyzers
{
    using static BannedApiAnalyzerResources;

    /// <summary>
    /// RS0035: <inheritdoc cref="RestrictedInternalsVisibleToTitle"/>
    /// </summary>
    public abstract class RestrictedInternalsVisibleToAnalyzer<TNameSyntax, TSyntaxKind> : DiagnosticAnalyzer
        where TNameSyntax : SyntaxNode
        where TSyntaxKind : struct
    {
        public static readonly DiagnosticDescriptor Rule = new(
            id: DiagnosticIds.RestrictedInternalsVisibleToRuleId,
            title: CreateLocalizableResourceString(nameof(RestrictedInternalsVisibleToTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(RestrictedInternalsVisibleToMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Error,  // Force build break on invalid external access.
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(RestrictedInternalsVisibleToDescription)),
            helpLinkUri: null, // TODO: Add help link
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected abstract ImmutableArray<TSyntaxKind> NameSyntaxKinds { get; }

        protected abstract bool IsInTypeOnlyContext(TNameSyntax node);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // Analyzer needs to get callbacks for generated code, and might report diagnostics in generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext compilationContext)
        {
            var restrictedInternalsVisibleToMap = GetRestrictedInternalsVisibleToMap(compilationContext.Compilation);
            if (restrictedInternalsVisibleToMap.IsEmpty)
            {
                return;
            }

            var namespaceToIsBannedMap = new ConcurrentDictionary<INamespaceSymbol, /*isBanned*/bool>();

            // Verify all explicit type name specifications in declarations and executable code.
            compilationContext.RegisterSyntaxNodeAction(
                context =>
                {
                    var name = (TNameSyntax)context.Node;
                    if (!IsInTypeOnlyContext(name) ||
                        name.Parent is TNameSyntax)
                    {
                        // Bail out if we are not in type only context or the parent is also a name
                        // which will be analyzed separately.
                        return;
                    }

                    var typeInfo = context.SemanticModel.GetTypeInfo(name, context.CancellationToken);
                    VerifySymbol(typeInfo.Type as INamedTypeSymbol, name,
                        context.ReportDiagnostic, restrictedInternalsVisibleToMap, namespaceToIsBannedMap);
                },
                NameSyntaxKinds);

            // Verify all member usages in executable code.
            compilationContext.RegisterOperationAction(
                context =>
                {
                    var symbol = context.Operation switch
                    {
                        IObjectCreationOperation objectCreation => objectCreation.Constructor,
                        IInvocationOperation invocation => invocation.TargetMethod,
                        IMemberReferenceOperation memberReference => memberReference.Member,
                        IConversionOperation conversion => conversion.OperatorMethod,
                        IUnaryOperation unary => unary.OperatorMethod,
                        IBinaryOperation binary => binary.OperatorMethod,
                        IIncrementOrDecrementOperation incrementOrDecrement => incrementOrDecrement.OperatorMethod,
                        _ => throw new NotImplementedException($"Unhandled OperationKind: {context.Operation.Kind}"),
                    };

                    VerifySymbol(symbol, context.Operation.Syntax,
                        context.ReportDiagnostic, restrictedInternalsVisibleToMap, namespaceToIsBannedMap);
                },
                OperationKind.ObjectCreation,
                OperationKind.Invocation,
                OperationKind.EventReference,
                OperationKind.FieldReference,
                OperationKind.MethodReference,
                OperationKind.PropertyReference,
                OperationKind.Conversion,
                OperationKind.UnaryOperator,
                OperationKind.BinaryOperator,
                OperationKind.Increment,
                OperationKind.Decrement);
        }

        private static ImmutableDictionary<IAssemblySymbol, ImmutableSortedSet<string>> GetRestrictedInternalsVisibleToMap(Compilation compilation)
        {
            var restrictedInternalsVisibleToAttribute = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeCompilerServicesRestrictedInternalsVisibleToAttribute);
            if (restrictedInternalsVisibleToAttribute == null)
            {
                return ImmutableDictionary<IAssemblySymbol, ImmutableSortedSet<string>>.Empty;
            }

            var builder = ImmutableDictionary.CreateBuilder<IAssemblySymbol, ImmutableSortedSet<string>>();
            foreach (var referencedAssemblySymbol in compilation.References.Select(compilation.GetAssemblyOrModuleSymbol).OfType<IAssemblySymbol>())
            {
                // Check IVT
                if (!referencedAssemblySymbol.GivesAccessTo(compilation.Assembly))
                {
                    continue;
                }

                var namespaceNameComparer = compilation.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
                var namespaceBuilder = ImmutableSortedSet.CreateBuilder(namespaceNameComparer);
                foreach (var assemblyAttribute in referencedAssemblySymbol.GetAttributes(restrictedInternalsVisibleToAttribute))
                {
                    // Look for ctor: "RestrictedInternalsVisibleToAttribute(string assemblyName, params string[] namespaces)"
                    if (assemblyAttribute.AttributeConstructor is null ||
                        assemblyAttribute.AttributeConstructor.Parameters.Length != 2 ||
                        assemblyAttribute.AttributeConstructor.Parameters[0].Type.SpecialType != SpecialType.System_String ||
                        assemblyAttribute.AttributeConstructor.Parameters[1].Type is not IArrayTypeSymbol arrayType ||
                        arrayType.Rank != 1 ||
                        arrayType.ElementType.SpecialType != SpecialType.System_String ||
                        !assemblyAttribute.AttributeConstructor.Parameters[1].IsParams)
                    {
                        continue;
                    }

                    // Ensure the Restricted IVT is for the current compilation's assembly.
                    if (assemblyAttribute.ConstructorArguments.Length != 2 ||
                        assemblyAttribute.ConstructorArguments[0].Kind != TypedConstantKind.Primitive ||
                        assemblyAttribute.ConstructorArguments[0].Value is not string assemblyName ||
                        !string.Equals(assemblyName, compilation.Assembly.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Ensure second constructor argument is string array.
                    if (assemblyAttribute.ConstructorArguments[1].Kind != TypedConstantKind.Array ||
                        !(assemblyAttribute.ConstructorArguments[1].Values is var namespaceConstants))
                    {
                        continue;
                    }

                    // Add namespaces specified in the second constructor argument.
                    foreach (TypedConstant namespaceConstant in namespaceConstants)
                    {
                        if (namespaceConstant.Kind == TypedConstantKind.Primitive &&
                            namespaceConstant.Value is string namespaceName)
                        {
                            namespaceBuilder.Add(namespaceName);
                        }
                    }
                }

                if (namespaceBuilder.Count > 0)
                {
                    builder.Add(referencedAssemblySymbol, namespaceBuilder.ToImmutable());
                }
            }

            return builder.ToImmutable();
        }

        private static void VerifySymbol(
            ISymbol? symbol,
            SyntaxNode node,
            Action<Diagnostic> reportDiagnostic,
            ImmutableDictionary<IAssemblySymbol, ImmutableSortedSet<string>> restrictedInternalsVisibleToMap,
            ConcurrentDictionary<INamespaceSymbol, bool> namespaceToIsBannedMap)
        {
            if (symbol != null &&
                IsBannedSymbol(symbol, restrictedInternalsVisibleToMap, namespaceToIsBannedMap))
            {
                var bannedSymbolDisplayString = symbol.ToDisplayString(Analyzer.Utilities.SymbolDisplayFormats.QualifiedTypeAndNamespaceSymbolDisplayFormat);
                var assemblyName = symbol.ContainingAssembly.Name;
                var restrictedNamespaces = string.Join(", ", restrictedInternalsVisibleToMap[symbol.ContainingAssembly]);
                var diagnostic = node.CreateDiagnostic(Rule, bannedSymbolDisplayString, assemblyName, restrictedNamespaces);
                reportDiagnostic(diagnostic);
            }
        }

        private static bool IsBannedSymbol(
            ISymbol symbol,
            ImmutableDictionary<IAssemblySymbol, ImmutableSortedSet<string>> restrictedInternalsVisibleToMap,
            ConcurrentDictionary<INamespaceSymbol, bool> namespaceToIsBannedMap)
        {
            // Check if the symbol belongs to an assembly to which this compilation has restricted internals access
            // and it is an internal symbol.
            if (symbol.ContainingAssembly == null ||
                !restrictedInternalsVisibleToMap.TryGetValue(symbol.ContainingAssembly, out var allowedNamespaces) ||
                symbol.GetResultantVisibility() != SymbolVisibility.Internal)
            {
                return false;
            }

            // Walk up containing namespace chain to explicitly look for an allowed namespace
            // with restricted internals access.
            var currentNamespace = symbol.ContainingNamespace;
            while (currentNamespace != null && !currentNamespace.IsGlobalNamespace)
            {
                // Check if we have already computed whether this namespace is banned or not.
                if (namespaceToIsBannedMap.TryGetValue(currentNamespace, out var isBanned))
                {
                    return isBanned;
                }

                // Check if this namespace is explicitly marked as allowed through restricted IVT.
                if (allowedNamespaces.Contains(currentNamespace.ToDisplayString()))
                {
                    MarkIsBanned(symbol.ContainingNamespace, currentNamespace, namespaceToIsBannedMap, banned: false);
                    return false;
                }

                currentNamespace = currentNamespace.ContainingNamespace;
            }

            // Otherwise, mark all the containing namespace names of the given symbol as banned
            // and consider the given symbol as banned.
            MarkIsBanned(symbol.ContainingNamespace, currentNamespace, namespaceToIsBannedMap, banned: true);
            return true;
        }

        private static void MarkIsBanned(
            INamespaceSymbol? startNamespace,
            INamespaceSymbol? uptoNamespace,
            ConcurrentDictionary<INamespaceSymbol, bool> namespaceToIsBannedMap,
            bool banned)
        {
            var currentNamespace = startNamespace;
            while (currentNamespace != null)
            {
                var saved = namespaceToIsBannedMap.GetOrAdd(currentNamespace, banned);
                Debug.Assert(saved == banned);

                if (Equals(currentNamespace, uptoNamespace))
                {
                    break;
                }

                currentNamespace = currentNamespace.ContainingNamespace;
            }
        }
    }
}

