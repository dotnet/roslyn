// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.RemoveUnusedMembers
{
    internal abstract class AbstractRemoveUnusedMembersDiagnosticAnalyzer<TDocumentationCommentTriviaSyntax, TIdentifierNameSyntax>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TDocumentationCommentTriviaSyntax: SyntaxNode
        where TIdentifierNameSyntax : SyntaxNode
    {
        protected AbstractRemoveUnusedMembersDiagnosticAnalyzer(bool forceEnableRules)
            : base(CreateDescriptors(forceEnableRules))
        {
        }

        private static ImmutableArray<DiagnosticDescriptor> CreateDescriptors(bool forceEnableRules)
        {
            // TODO: Enable these rules by default once we have designed the Tools|Option location and UI for such code quality rules.

            // IDE0051: "Remove unused members" (Symbol is declared but never referenced)
            var removeUnusedMembersTitle = new LocalizableResourceString(nameof(FeaturesResources.Remove_Unused_Private_Members), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            var removeUnusedMembersMessage = new LocalizableResourceString(nameof(FeaturesResources.Remove_Unused_Private_Members_Message), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            var removeUnusedMembersRule = CreateDescriptor(
                IDEDiagnosticIds.RemoveUnusedMembersDiagnosticId, removeUnusedMembersTitle, removeUnusedMembersMessage, configurable: true, enabledByDefault: forceEnableRules);
            var removeUnusedMembersRuleWithFadingRule = CreateUnnecessaryDescriptor(
                IDEDiagnosticIds.RemoveUnusedMembersDiagnosticId, removeUnusedMembersTitle, removeUnusedMembersMessage, configurable: true, enabledByDefault: forceEnableRules);

            // IDE0052: "Remove unread members" (Value is written and/or symbol is referenced, but the assigned value is never read)
            var removeUnreadMembersTitle = new LocalizableResourceString(nameof(FeaturesResources.Remove_Unread_Private_Members), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            var removeUnreadMembersMessage = new LocalizableResourceString(nameof(FeaturesResources.Remove_Unread_Private_Members_Message), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            var removeUnreadMembersRule = CreateDescriptor(
                IDEDiagnosticIds.RemoveUnreadMembersDiagnosticId, removeUnreadMembersTitle, removeUnreadMembersMessage, configurable: true, enabledByDefault: forceEnableRules);
            var removeUnreadMembersRuleUnnecessaryWithFadingRule = CreateUnnecessaryDescriptor(
                IDEDiagnosticIds.RemoveUnreadMembersDiagnosticId, removeUnreadMembersTitle, removeUnreadMembersMessage, configurable: true, enabledByDefault: forceEnableRules);

            return ImmutableArray.Create(removeUnusedMembersRule, removeUnusedMembersRuleWithFadingRule,
                    removeUnreadMembersRule, removeUnreadMembersRuleUnnecessaryWithFadingRule);
        }

        private DiagnosticDescriptor RemoveUnusedMemberRule => SupportedDiagnostics[1];
        private DiagnosticDescriptor RemoveUnreadMemberRule => SupportedDiagnostics[3];

        public override bool OpenFileOnly(Workspace workspace) => false;

        // We need to analyze the whole document even for edits within a method body.
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            // We want to analyze references in generated code, but not report unused members in generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                var compilationAnalyzer = new CompilationAnalyzer(compilationStartContext.Compilation, RemoveUnusedMemberRule, RemoveUnreadMemberRule);

                // We register following actions in the compilation:
                // 1. A symbol action for member symbols to ensure the member's unused state is initialized to true for every private member symbol.
                // 2. Operation actions for member references and invocations to detect member usages, i.e. read or read reference taken.
                // 3. Operation action for invalid operations to bail out on erroneous code.
                // 4. A symbol start/end action for named types to report diagnostics for candidate members that have no usage in executable code.
                //
                // Note that we need to register separately for OperationKind.Invocation due to https://github.com/dotnet/roslyn/issues/26206

                compilationStartContext.RegisterSymbolAction(compilationAnalyzer.AnalyzeSymbolDeclaration, SymbolKind.Method, SymbolKind.Field, SymbolKind.Property, SymbolKind.Event);

                compilationStartContext.RegisterSymbolStartAction(symbolStartContext =>
                {
                    var hasInvalidOperation = false;
                    symbolStartContext.RegisterOperationAction(compilationAnalyzer.AnalyzeMemberReferenceOperation, OperationKind.FieldReference, OperationKind.MethodReference, OperationKind.PropertyReference, OperationKind.EventReference);
                    symbolStartContext.RegisterOperationAction(compilationAnalyzer.AnalyzeInvocationOperation, OperationKind.Invocation);
                    symbolStartContext.RegisterOperationAction(_ => hasInvalidOperation = true, OperationKind.Invalid);
                    symbolStartContext.RegisterSymbolEndAction(symbolEndContext => compilationAnalyzer.OnSymbolEnd(symbolEndContext, hasInvalidOperation));
                }, SymbolKind.NamedType);
            });
        }

        private sealed class CompilationAnalyzer
        {
            private readonly DiagnosticDescriptor _removeUnusedMembersRule, _removeUnreadMembersRule;
            private readonly object _gate;
            private readonly Dictionary<ISymbol, ValueUsageInfo> _symbolValueUsageStateMap;
            private readonly Lazy<INamedTypeSymbol> _lazyTaskType, _lazyGenericTaskType;

            public CompilationAnalyzer(Compilation compilation, DiagnosticDescriptor removeUnusedMembersRule, DiagnosticDescriptor removeUnreadMembersRule)
            {
                _removeUnusedMembersRule = removeUnusedMembersRule;
                _removeUnreadMembersRule = removeUnreadMembersRule;
                _gate = new object();

                // State map for candidate member symbols, with the value indicating how each symbol is used in executable code.
                _symbolValueUsageStateMap = new Dictionary<ISymbol, ValueUsageInfo>();

                _lazyTaskType = new Lazy<INamedTypeSymbol>(() => compilation.TaskType());
                _lazyGenericTaskType = new Lazy<INamedTypeSymbol>(() => compilation.TaskOfTType());
            }

            public void AnalyzeSymbolDeclaration(SymbolAnalysisContext symbolContext)
            {
                if (IsCandidateSymbol(symbolContext.Symbol))
                {
                    lock (_gate)
                    {
                        // Initialize unused state to 'true'.
                        // Note that we might receive a symbol reference (AnalyzeMemberOperation) callback before
                        // this symbol declaration callback, so even though we cannot receive duplicate callbacks for a symbol,
                        // an entry might already be present of the declared symbol here.
                        if (!_symbolValueUsageStateMap.ContainsKey(symbolContext.Symbol))
                        {
                            _symbolValueUsageStateMap.Add(symbolContext.Symbol, ValueUsageInfo.None);
                        }
                    }
                }
            }

            private void OnSymbolUsage(ISymbol memberSymbol, ValueUsageInfo usageInfo)
            {
                Debug.Assert(IsCandidateSymbol(memberSymbol));

                lock (_gate)
                {
                    // Update the usage info for the memberSymbol
                    if (_symbolValueUsageStateMap.TryGetValue(memberSymbol, out var currentUsageInfo))
                    {
                        usageInfo = currentUsageInfo | usageInfo;
                    }

                    _symbolValueUsageStateMap[memberSymbol] = usageInfo;
                }
            }

            private bool TryRemove(ISymbol memberSymbol, out ValueUsageInfo valueUsageInfo)
            {
                lock (_gate)
                {
                    if (_symbolValueUsageStateMap.TryGetValue(memberSymbol, out valueUsageInfo))
                    {
                        _symbolValueUsageStateMap.Remove(memberSymbol);
                        return true;
                    }

                    return false;
                }
            }

            public void AnalyzeMemberReferenceOperation(OperationAnalysisContext operationContext)
            {
                var memberReference = (IMemberReferenceOperation)operationContext.Operation;
                if (IsCandidateSymbol(memberReference.Member))
                {
                    // Get the value usage info.
                    var valueUsageInfo = memberReference.GetValueUsageInfo();

                    if (valueUsageInfo == ValueUsageInfo.ReadWrite)
                    {
                        Debug.Assert(memberReference.Parent is ICompoundAssignmentOperation compoundAssignment &&
                            compoundAssignment.Target == memberReference ||
                            memberReference.Parent is IIncrementOrDecrementOperation);

                        // Compound assignment or increment whose value is being dropped (parent has null type)
                        // is treated as a Write as the value was never actually 'read' in a way that is observable.
                        if (memberReference.Parent.Parent?.Type == null)
                        {
                            valueUsageInfo = ValueUsageInfo.Write;
                        }
                    }

                    OnSymbolUsage(memberReference.Member, valueUsageInfo);
                }
            }

            public void AnalyzeInvocationOperation(OperationAnalysisContext operationContext)
            {
                var invocation = (IInvocationOperation)operationContext.Operation;
                if (IsCandidateSymbol(invocation.TargetMethod))
                {
                    OnSymbolUsage(invocation.TargetMethod, ValueUsageInfo.Read);
                }
            }

            public void OnSymbolEnd(SymbolAnalysisContext symbolEndContext, bool hasInvalidOperation)
            {
                if (hasInvalidOperation)
                {
                    return;
                }

                // Report diagnostics for unused candidate members.
                ImmutableHashSet<ISymbol> symbolsReferencedInDocComments = null;
                var members = ((INamedTypeSymbol)symbolEndContext.Symbol).GetMembers();
                foreach (var member in members)
                {
                    // Check if the underlying member is neither read nor a readable reference to the member is taken.
                    if (TryRemove(member, out var valueUsageInfo) &&
                        !valueUsageInfo.ContainsReadOrReadableRef())
                    {
                        Debug.Assert(IsCandidateSymbol(member));
                        Debug.Assert(!member.IsImplicitlyDeclared);

                        if (symbolsReferencedInDocComments == null)
                        {
                            // Bail out if there are syntax errors in any of the declarations of the containing type.
                            if (HasSyntaxErrors())
                            {
                                return;
                            }

                            symbolsReferencedInDocComments = GetCandidateSymbolsReferencedInDocComments();
                        }

                        // Report IDE0051 or IDE0052 based on whether the underlying member has any Write/WritableRef/NonReadWriteRef references or not.
                        var rule = !valueUsageInfo.ContainsWriteOrWritableRef() && !valueUsageInfo.ContainsNonReadWriteRef() && !symbolsReferencedInDocComments.Contains(member)
                            ? _removeUnusedMembersRule
                            : _removeUnreadMembersRule;
                        var effectiveSeverity = rule.GetEffectiveSeverity(symbolEndContext.Compilation.Options);

                        var diagnostic = DiagnosticHelper.Create(
                            rule,
                            member.Locations[0],
                            effectiveSeverity,
                            additionalLocations: member.Locations,
                            properties: null,
                            member.ContainingType.Name,
                            member.Name);
                        symbolEndContext.ReportDiagnostic(diagnostic);
                    }
                }

                bool HasSyntaxErrors()
                {
                    foreach (var tree in symbolEndContext.Symbol.Locations.Select(l => l.SourceTree))
                    {
                        if (tree.GetDiagnostics(symbolEndContext.CancellationToken).Any(d => d.Severity == DiagnosticSeverity.Error))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                ImmutableHashSet<ISymbol> GetCandidateSymbolsReferencedInDocComments()
                {
                    var builder = ImmutableHashSet.CreateBuilder<ISymbol>();
                    foreach (var root in symbolEndContext.Symbol.Locations.Select(l => l.SourceTree.GetRoot(symbolEndContext.CancellationToken)))
                    {
                        SemanticModel lazyModel = null;
                        foreach (var node in root.DescendantNodes(descendIntoTrivia: true)
                                                 .OfType<TDocumentationCommentTriviaSyntax>()
                                                 .SelectMany(n => n.DescendantNodes().OfType<TIdentifierNameSyntax>()))
                        {
                            lazyModel = lazyModel ?? symbolEndContext.Compilation.GetSemanticModel(root.SyntaxTree);
                            var symbol = lazyModel.GetSymbolInfo(node, symbolEndContext.CancellationToken).Symbol;
                            if (symbol != null && IsCandidateSymbol(symbol))
                            {
                                builder.Add(symbol);
                            }
                        }
                    }

                    return builder.ToImmutable();
                }
            }

            private bool IsCandidateSymbol(ISymbol memberSymbol)
            {
                if (memberSymbol.DeclaredAccessibility == Accessibility.Private &&
                    !memberSymbol.IsImplicitlyDeclared)
                {
                    // Do not track accessors, as we will track the associated symbol.
                    // Also skip entry point (Main) method
                    switch (memberSymbol.Kind)
                    {
                        case SymbolKind.Method:
                            var methodSymbol = (IMethodSymbol)memberSymbol;
                            return methodSymbol.AssociatedSymbol == null && !IsEntryPoint(methodSymbol);

                        case SymbolKind.Field:
                            return ((IFieldSymbol)memberSymbol).AssociatedSymbol == null;

                        default:
                            return true;
                    }
                }

                return false;

                // Local functions.
                bool IsEntryPoint(IMethodSymbol methodSymbol)
                {
                    if (methodSymbol.Name != WellKnownMemberNames.EntryPointMethodName ||
                        !methodSymbol.IsStatic)
                    {
                        return false;
                    }

                    if (methodSymbol.ReturnsVoid)
                    {
                        return true;
                    }

                    if (methodSymbol.IsAsync &&
                        (methodSymbol.ReturnType.OriginalDefinition == _lazyTaskType.Value ||
                        methodSymbol.ReturnType.OriginalDefinition == _lazyGenericTaskType.Value))
                    {
                        return true;
                    }

                    return false;
                }
            }
        }
    }
}
