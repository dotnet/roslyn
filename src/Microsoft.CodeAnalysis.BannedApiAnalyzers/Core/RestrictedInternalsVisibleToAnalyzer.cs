// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using DiagnosticIds = Roslyn.Diagnostics.Analyzers.RoslynDiagnosticIds;

namespace Microsoft.CodeAnalysis.BannedApiAnalyzers
{
    public abstract class RestrictedInternalsVisibleToAnalyzer<TNameSyntax, TSyntaxKind> : DiagnosticAnalyzer
        where TNameSyntax : SyntaxNode
        where TSyntaxKind : struct
    {
        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.RestrictedInternalsVisibleToRuleId,
            title: BannedApiAnalyzerResources.RestrictedInternalsVisibleToTitle,
            messageFormat: BannedApiAnalyzerResources.RestrictedInternalsVisibleToMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Error,  // Force build break on invalid external access.
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: BannedApiAnalyzerResources.RestrictedInternalsVisibleToDescription,
            helpLinkUri: null, // TODO: Add help link
            customTags: WellKnownDiagnosticTags.Telemetry);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
                    VerifySymbol(typeInfo.Type as INamedTypeSymbol, name, context.ReportDiagnostic);
                },
                NameSyntaxKinds);

            // Verify all member usages in executable code.
            compilationContext.RegisterOperationAction(
                context =>
                {
                    ISymbol symbol;
                    switch (context.Operation)
                    {
                        case IObjectCreationOperation objectCreation:
                            symbol = objectCreation.Constructor;
                            break;
                        case IInvocationOperation invocation:
                            symbol = invocation.TargetMethod;
                            break;
                        case IMemberReferenceOperation memberReference:
                            symbol = memberReference.Member;
                            break;
                        default:
                            throw new NotImplementedException($"Unhandled OperationKind: {context.Operation.Kind}");
                    }

                    VerifySymbol(symbol, context.Operation.Syntax, context.ReportDiagnostic);
                },
                OperationKind.ObjectCreation,
                OperationKind.Invocation,
                OperationKind.EventReference,
                OperationKind.FieldReference,
                OperationKind.MethodReference,
                OperationKind.PropertyReference);

            return;

            // Local functions.
            static ImmutableDictionary<IAssemblySymbol, ImmutableSortedSet<string>> GetRestrictedInternalsVisibleToMap(Compilation compilation)
            {
                var restrictedInternalsVisibleToAttribute = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.RestrictedInternalsVisibleToAttribute");
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
                    foreach (var assemblyAttribute in referencedAssemblySymbol.GetAttributes())
                    {
                        // Look for ctor: "RestrictedInternalsVisibleToAttribute(string assemblyName, params string[] namespaces)"
                        if (!Equals(assemblyAttribute.AttributeClass, restrictedInternalsVisibleToAttribute) ||
                            assemblyAttribute.AttributeConstructor.Parameters.Length != 2 ||
                            assemblyAttribute.AttributeConstructor.Parameters[0].Type.SpecialType != SpecialType.System_String ||
                            !(assemblyAttribute.AttributeConstructor.Parameters[1].Type is IArrayTypeSymbol arrayType) ||
                            arrayType.Rank != 1 ||
                            arrayType.ElementType.SpecialType != SpecialType.System_String ||
                            !assemblyAttribute.AttributeConstructor.Parameters[1].IsParams)
                        {
                            continue;
                        }

                        // Ensure the Restricted IVT is for the current compilation's assembly.
                        if (assemblyAttribute.ConstructorArguments.Length != 2 ||
                            assemblyAttribute.ConstructorArguments[0].Kind != TypedConstantKind.Primitive ||
                            !(assemblyAttribute.ConstructorArguments[0].Value is string assemblyName) ||
                            !AssemblyIdentity.TryParseDisplayName(assemblyName, out var assemblyIdentity) ||
                            AssemblyIdentityComparer.Default.Compare(assemblyIdentity, compilation.Assembly.Identity) == AssemblyIdentityComparer.ComparisonResult.NotEquivalent)
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

            void VerifySymbol(ISymbol symbol, SyntaxNode node, Action<Diagnostic> reportDiagnostic)
            {
                if (symbol != null &&
                    IsBannedSymbol(symbol))
                {
                    var bannedSymbolDisplayString = symbol.ToDisplayString(SymbolDisplayFormats.QualifiedTypeAndNamespaceSymbolDisplayFormat);
                    var restrictedNamespaces = string.Join(", ", restrictedInternalsVisibleToMap[symbol.ContainingAssembly]);
                    var diagnostic = node.CreateDiagnostic(Rule, bannedSymbolDisplayString, restrictedNamespaces);
                    reportDiagnostic(diagnostic);
                }
            }

            bool IsBannedSymbol(ISymbol symbol)
            {
                // Check if the symbol belongs to an assembly to which this compilation has restricted internals access
                // and it is an internal symbol.
                if (!restrictedInternalsVisibleToMap.TryGetValue(symbol.ContainingAssembly, out var allowedNamespaces) ||
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
                        var banned = namespaceToIsBannedMap.GetOrAdd(currentNamespace, false);
                        Debug.Assert(!banned);
                        return false;
                    }

                    currentNamespace = currentNamespace.ContainingNamespace;
                }

                // Otherwise, mark all the containing namespace names of the given symbol as banned
                // and consider the given symbol as banned.
                currentNamespace = symbol.ContainingNamespace;
                while (currentNamespace != null)
                {
                    var isBanned = namespaceToIsBannedMap.GetOrAdd(currentNamespace, true);
                    Debug.Assert(isBanned);
                    currentNamespace = currentNamespace.ContainingNamespace;
                }

                return true;
            }
        }
    }
}
