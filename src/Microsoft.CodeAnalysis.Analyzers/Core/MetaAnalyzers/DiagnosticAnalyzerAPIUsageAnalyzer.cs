// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Analyzer.Utilities.PooledObjects;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public abstract class DiagnosticAnalyzerApiUsageAnalyzer<TTypeSyntax> : DiagnosticAnalyzer
        where TTypeSyntax : SyntaxNode
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotUseTypesFromAssemblyRuleTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotUseTypesFromAssemblyRuleDirectMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableIndirectMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotUseTypesFromAssemblyRuleIndirectMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotUseTypesFromAssemblyRuleDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources), nameof(AnalysisContext), DiagnosticAnalyzerCorrectnessAnalyzer.RegisterCompilationStartActionName);
        private const string CodeActionMetadataName = "Microsoft.CodeAnalysis.CodeActions.CodeAction";
        private static readonly ImmutableArray<string> s_WorkspaceAssemblyNames = ImmutableArray.Create(
            "Microsoft.CodeAnalysis.Workspaces",
            "Microsoft.CodeAnalysis.CSharp.Workspaces",
            "Microsoft.CodeAnalysis.VisualBasic.Workspaces");

        public static readonly DiagnosticDescriptor DoNotUseTypesFromAssemblyDirectRule = new DiagnosticDescriptor(
            DiagnosticIds.DoNotUseTypesFromAssemblyRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor DoNotUseTypesFromAssemblyIndirectRule = new DiagnosticDescriptor(
            DiagnosticIds.DoNotUseTypesFromAssemblyRuleId,
            s_localizableTitle,
            s_localizableIndirectMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        protected abstract bool IsNamedTypeDeclarationBlock(SyntaxNode syntax);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DoNotUseTypesFromAssemblyDirectRule, DoNotUseTypesFromAssemblyIndirectRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                if (compilationStartContext.Compilation.GetOrCreateTypeByMetadataName(CodeActionMetadataName) == null)
                {
                    // No reference to core Workspaces assembly.
                    return;
                }

                INamedTypeSymbol? diagnosticAnalyzer = compilationStartContext.Compilation.GetOrCreateTypeByMetadataName(DiagnosticAnalyzerCorrectnessAnalyzer.DiagnosticAnalyzerTypeFullName);
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
                            Debug.Assert(typeToProcess.ContainingAssembly.Equals(declaredType.ContainingAssembly));
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
                                args = new[]
                                {
                                    declaredType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                                    string.Join(", ", violatingTypeNamesBuilder)
                                };
                            }
                            else
                            {
                                // Change diagnostic analyzer type '{0}' to remove all direct and/or indirect accesses to type(s) '{1}', which access type(s) '{2}'
                                rule = DoNotUseTypesFromAssemblyIndirectRule;
                                args = new[]
                                {
                                    declaredType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                                    string.Join(", ", violatingUsedTypeNamesBuilder),
                                    string.Join(", ", violatingTypeNamesBuilder)
                                };
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
                syntax = syntax.FirstAncestorOrSelf<SyntaxNode>(node => IsNamedTypeDeclarationBlock(node), ascendOutOfTrivia: false) ?? syntax;

                var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
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

        private static void AddUsedNamedTypeCore(ITypeSymbol typeOpt, PooledHashSet<INamedTypeSymbol> builder, ref bool hasAccessToTypeFromWorkspaceAssemblies)
        {
            if (typeOpt is INamedTypeSymbol usedType &&
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
                    foreach (var type in usedType.TypeArguments)
                    {
                        AddUsedNamedTypeCore(type, builder, ref hasAccessToTypeFromWorkspaceAssemblies);
                    }
                }
            }
        }
    }
}