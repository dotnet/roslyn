// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Helpers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;

    /// <summary>
    /// RS1022: <inheritdoc cref="DoNotUseTypesFromAssemblyRuleTitle"/>
    /// </summary>
    public abstract class DiagnosticAnalyzerApiUsageAnalyzer<TTypeSyntax> : DiagnosticAnalyzer
        where TTypeSyntax : SyntaxNode
    {
        private static readonly LocalizableString s_localizableTitle = CreateLocalizableResourceString(nameof(DoNotUseTypesFromAssemblyRuleTitle));
        private static readonly LocalizableString s_localizableDescription = CreateLocalizableResourceString(nameof(DoNotUseTypesFromAssemblyRuleDescription), nameof(AnalysisContext), DiagnosticWellKnownNames.RegisterCompilationStartActionName);
        private const string CodeActionMetadataName = "Microsoft.CodeAnalysis.CodeActions.CodeAction";
        private const string HelpLinkUri = "https://github.com/dotnet/roslyn/blob/main/docs/roslyn-analyzers/rules/RS1022.md";
        private static readonly ImmutableArray<string> s_WorkspaceAssemblyNames = ImmutableArray.Create(
            "Microsoft.CodeAnalysis.Workspaces",
            "Microsoft.CodeAnalysis.CSharp.Workspaces",
            "Microsoft.CodeAnalysis.VisualBasic.Workspaces");

        public static readonly DiagnosticDescriptor DoNotUseTypesFromAssemblyDirectRule = new(
            DiagnosticIds.DoNotUseTypesFromAssemblyRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotUseTypesFromAssemblyRuleDirectMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            helpLinkUri: HelpLinkUri,
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        public static readonly DiagnosticDescriptor DoNotUseTypesFromAssemblyIndirectRule = new(
            DiagnosticIds.DoNotUseTypesFromAssemblyRuleId,
            s_localizableTitle,
            CreateLocalizableResourceString(nameof(DoNotUseTypesFromAssemblyRuleIndirectMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            helpLinkUri: HelpLinkUri,
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        protected abstract bool IsNamedTypeDeclarationBlock(SyntaxNode syntax);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DoNotUseTypesFromAssemblyDirectRule, DoNotUseTypesFromAssemblyIndirectRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                // This analyzer is disabled by default via a configuration option that also applies to RS1038. It only
                // needs to proceed if .globalconfig contains the following line to enable it:
                //
                // roslyn_correctness.assembly_reference_validation = relaxed
                if (CompilerExtensionStrictApiAnalyzer.IsStrictAnalysisEnabled(compilationStartContext.Options))
                {
                    // RS1038 is being applied instead of RS1022
                    return;
                }

                if (compilationStartContext.Compilation.GetOrCreateTypeByMetadataName(CodeActionMetadataName) == null)
                {
                    // No reference to core Workspaces assembly.
                    return;
                }

                INamedTypeSymbol? diagnosticAnalyzer = compilationStartContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzer);
                if (diagnosticAnalyzer == null)
                {
                    // Does not contain any diagnostic analyzers.
                    return;
                }

                var hasAccessToTypeFromWorkspaceAssemblies = false;
                var namedTypesToAccessedTypesMap = new ConcurrentDictionary<INamedTypeSymbol, ImmutableHashSet<INamedTypeSymbol>>();
                var diagnosticAnalyzerTypes = new ConcurrentBag<INamedTypeSymbol>();
                compilationStartContext.RegisterSymbolAction(symbolContext =>
                {
                    var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                    if (namedType.DerivesFrom(diagnosticAnalyzer, baseTypesOnly: true))
                    {
                        diagnosticAnalyzerTypes.Add(namedType);
                    }

                    var usedTypes = GetUsedNamedTypes(namedType, symbolContext.Compilation, symbolContext.CancellationToken, ref hasAccessToTypeFromWorkspaceAssemblies);
                    var added = namedTypesToAccessedTypesMap.TryAdd(namedType, usedTypes);
                    Debug.Assert(added);
                }, SymbolKind.NamedType);

                compilationStartContext.RegisterCompilationEndAction(compilationEndContext =>
                {
                    if (diagnosticAnalyzerTypes.IsEmpty || !hasAccessToTypeFromWorkspaceAssemblies)
                    {
                        return;
                    }

                    var typesToProcess = new Queue<INamedTypeSymbol>();
                    var processedTypes = new HashSet<INamedTypeSymbol>();
                    var violatingTypeNamesBuilder = new SortedSet<string>();
                    var violatingUsedTypeNamesBuilder = new SortedSet<string>();
                    foreach (INamedTypeSymbol declaredType in namedTypesToAccessedTypesMap.Keys)
                    {
                        if (!diagnosticAnalyzerTypes.Contains(declaredType))
                        {
                            continue;
                        }

                        typesToProcess.Clear();
                        processedTypes.Clear();
                        violatingTypeNamesBuilder.Clear();
                        violatingUsedTypeNamesBuilder.Clear();
                        typesToProcess.Enqueue(declaredType);
                        do
                        {
                            var typeToProcess = typesToProcess.Dequeue();
                            Debug.Assert(SymbolEqualityComparer.Default.Equals(typeToProcess.ContainingAssembly, declaredType.ContainingAssembly));
                            Debug.Assert(namedTypesToAccessedTypesMap.ContainsKey(typeToProcess));

                            foreach (INamedTypeSymbol usedType in namedTypesToAccessedTypesMap[typeToProcess])
                            {
                                if (usedType.ContainingAssembly != null &&
                                    s_WorkspaceAssemblyNames.Contains(usedType.ContainingAssembly.Name))
                                {
                                    violatingTypeNamesBuilder.Add(usedType.ToDisplayString());
                                    violatingUsedTypeNamesBuilder.Add(typeToProcess.ToDisplayString());
                                }

                                if (!processedTypes.Contains(usedType) && namedTypesToAccessedTypesMap.ContainsKey(usedType))
                                {
                                    typesToProcess.Enqueue(usedType);
                                }
                            }

                            processedTypes.Add(typeToProcess);
                        } while (typesToProcess.Count != 0);

                        if (violatingTypeNamesBuilder.Count > 0)
                        {
                            string[] args;
                            DiagnosticDescriptor rule;
                            if (violatingUsedTypeNamesBuilder.Count == 1 && violatingUsedTypeNamesBuilder.Single() == declaredType.ToDisplayString())
                            {
                                // Change diagnostic analyzer type '{0}' to remove all direct accesses to type(s) '{1}'
                                rule = DoNotUseTypesFromAssemblyDirectRule;
                                args =
                                [
                                    declaredType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                                    string.Join(", ", violatingTypeNamesBuilder)
                                ];
                            }
                            else
                            {
                                // Change diagnostic analyzer type '{0}' to remove all direct and/or indirect accesses to type(s) '{1}', which access type(s) '{2}'
                                rule = DoNotUseTypesFromAssemblyIndirectRule;
                                args =
                                [
                                    declaredType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                                    string.Join(", ", violatingUsedTypeNamesBuilder),
                                    string.Join(", ", violatingTypeNamesBuilder)
                                ];
                            }

                            Diagnostic diagnostic = declaredType.CreateDiagnostic(rule, args);
                            compilationEndContext.ReportDiagnostic(diagnostic);
                        }
                    }
                });
            });
        }

        private ImmutableHashSet<INamedTypeSymbol> GetUsedNamedTypes(INamedTypeSymbol namedType, Compilation compilation, CancellationToken cancellationToken, ref bool hasAccessToTypeFromWorkspaceAssemblies)
        {
            var builder = PooledHashSet<INamedTypeSymbol>.GetInstance();
            foreach (var decl in namedType.DeclaringSyntaxReferences)
            {
                var syntax = decl.GetSyntax(cancellationToken);

                // GetSyntax for VB returns the StatementSyntax instead of BlockSyntax node.
                syntax = syntax.FirstAncestorOrSelf<SyntaxNode>(IsNamedTypeDeclarationBlock, ascendOutOfTrivia: false) ?? syntax;

#pragma warning disable RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer
                var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
#pragma warning restore RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer
                var nodesToProcess = new Queue<(SyntaxNode node, bool inExecutableCode)>();
                nodesToProcess.Enqueue((node: syntax, inExecutableCode: false));

                do
                {
                    (SyntaxNode node, bool inExecutableCode) nodeToProcess = nodesToProcess.Dequeue();
                    var node = nodeToProcess.node;
                    var inExecutableCode = nodeToProcess.inExecutableCode;
                    if (!inExecutableCode && !ReferenceEquals(node, syntax) && IsNamedTypeDeclarationBlock(node))
                    {
                        // Skip type member declarations.
                        continue;
                    }

                    if (node is TTypeSyntax typeSyntax)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(typeSyntax, cancellationToken);
                        AddUsedNamedTypeCore(typeInfo.Type, builder, ref hasAccessToTypeFromWorkspaceAssemblies);
                    }

                    if (!inExecutableCode)
                    {
                        var operationBlock = semanticModel.GetOperation(node, cancellationToken);
                        if (operationBlock != null)
                        {
                            // Add used types within executable code in the operation tree.
                            inExecutableCode = true;
                            foreach (var operation in operationBlock.DescendantsAndSelf())
                            {
                                AddUsedNamedTypeCore(operation.Type, builder, ref hasAccessToTypeFromWorkspaceAssemblies);

                                // Handle static member accesses specially as there is no operation for static type off which the member is accessed.
                                if (operation is IMemberReferenceOperation memberReference &&
                                    memberReference.Member.IsStatic)
                                {
                                    AddUsedNamedTypeCore(memberReference.Member.ContainingType, builder, ref hasAccessToTypeFromWorkspaceAssemblies);
                                }
                                else if (operation is IInvocationOperation invocation &&
                                    (invocation.TargetMethod.IsStatic || invocation.TargetMethod.IsExtensionMethod))
                                {
                                    AddUsedNamedTypeCore(invocation.TargetMethod.ContainingType, builder, ref hasAccessToTypeFromWorkspaceAssemblies);
                                }
                            }
                        }
                    }

                    foreach (var child in node.ChildNodes())
                    {
                        nodesToProcess.Enqueue((child, inExecutableCode));
                    }
                } while (nodesToProcess.Count != 0);
            }

            return builder.ToImmutableAndFree();
        }

        private static void AddUsedNamedTypeCore(ITypeSymbol? type, PooledHashSet<INamedTypeSymbol> builder, ref bool hasAccessToTypeFromWorkspaceAssemblies)
        {
            if (type is INamedTypeSymbol usedType &&
                usedType.TypeKind != TypeKind.Error)
            {
                builder.Add(usedType);

                if (!hasAccessToTypeFromWorkspaceAssemblies &&
                    usedType.ContainingAssembly != null &&
                    s_WorkspaceAssemblyNames.Contains(usedType.ContainingAssembly.Name))
                {
                    hasAccessToTypeFromWorkspaceAssemblies = true;
                }

                if (usedType.IsGenericType)
                {
                    foreach (var typeArgument in usedType.TypeArguments)
                    {
                        AddUsedNamedTypeCore(typeArgument, builder, ref hasAccessToTypeFromWorkspaceAssemblies);
                    }
                }
            }
        }
    }
}
