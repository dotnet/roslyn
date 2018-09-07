﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
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
            // https://github.com/dotnet/roslyn/issues/29519

            // IDE0051: "Remove unused members" (Symbol is declared but never referenced)
            var removeUnusedMembersTitle = new LocalizableResourceString(nameof(FeaturesResources.Remove_unused_private_members), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            var removeUnusedMembersMessage = new LocalizableResourceString(nameof(FeaturesResources.Type_0_has_an_unused_private_member_1_which_can_be_removed), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            var removeUnusedMembersRule = CreateDescriptor(
                IDEDiagnosticIds.RemoveUnusedMembersDiagnosticId, removeUnusedMembersTitle, removeUnusedMembersMessage, configurable: true, enabledByDefault: forceEnableRules);
            var removeUnusedMembersRuleWithFadingRule = CreateUnnecessaryDescriptor(
                IDEDiagnosticIds.RemoveUnusedMembersDiagnosticId, removeUnusedMembersTitle, removeUnusedMembersMessage, configurable: true, enabledByDefault: forceEnableRules);

            // IDE0052: "Remove unread members" (Value is written and/or symbol is referenced, but the assigned value is never read)
            var removeUnreadMembersTitle = new LocalizableResourceString(nameof(FeaturesResources.Remove_unread_private_members), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            var removeUnreadMembersMessage = new LocalizableResourceString(nameof(FeaturesResources.Type_0_has_a_private_member_1_that_can_be_removed_as_the_value_assigned_to_it_is_never_read), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            var removeUnreadMembersRule = CreateDescriptor(
                IDEDiagnosticIds.RemoveUnreadMembersDiagnosticId, removeUnreadMembersTitle, removeUnreadMembersMessage, configurable: true, enabledByDefault: forceEnableRules);
            var removeUnreadMembersRuleUnnecessaryWithFadingRule = CreateUnnecessaryDescriptor(
                IDEDiagnosticIds.RemoveUnreadMembersDiagnosticId, removeUnreadMembersTitle, removeUnreadMembersMessage, configurable: true, enabledByDefault: forceEnableRules);

            return ImmutableArray.Create(removeUnusedMembersRule, removeUnusedMembersRuleWithFadingRule,
                    removeUnreadMembersRule, removeUnreadMembersRuleUnnecessaryWithFadingRule);
        }

        // See CreateDescriptors method above for the indices.
        // We should be able to cleanup the implementation to avoid hard coded indices
        // once https://github.com/dotnet/roslyn/issues/29519 is implemented.
        private DiagnosticDescriptor RemoveUnusedMemberRule => SupportedDiagnostics[1];
        private DiagnosticDescriptor RemoveUnreadMemberRule => SupportedDiagnostics[3];

        public override bool OpenFileOnly(Workspace workspace) => false;

        // We need to analyze the whole document even for edits within a method body,
        // because we might add or remove references to members in executable code.
        // For example, if we had an unused field with no references, then editing any single method body
        // to reference this field should clear the unused field diagnostic.
        // Hence, we need to re-analyze the declarations in the whole file for any edits within the document. 
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            // We want to analyze references in generated code, but not report unused members in generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(compilationStartContext
                => CompilationAnalyzer.CreateAndRegisterActions(compilationStartContext, RemoveUnusedMemberRule, RemoveUnreadMemberRule));
        }

        private sealed class CompilationAnalyzer
        {
            private readonly DiagnosticDescriptor _removeUnusedMembersRule, _removeUnreadMembersRule;
            private readonly object _gate;
            private readonly Dictionary<ISymbol, ValueUsageInfo> _symbolValueUsageStateMap;
            private readonly INamedTypeSymbol _taskType, _genericTaskType;

            private CompilationAnalyzer(Compilation compilation, DiagnosticDescriptor removeUnusedMembersRule, DiagnosticDescriptor removeUnreadMembersRule)
            {
                _removeUnusedMembersRule = removeUnusedMembersRule;
                _removeUnreadMembersRule = removeUnreadMembersRule;
                _gate = new object();

                // State map for candidate member symbols, with the value indicating how each symbol is used in executable code.
                _symbolValueUsageStateMap = new Dictionary<ISymbol, ValueUsageInfo>();

                _taskType = compilation.TaskType();
                _genericTaskType = compilation.TaskOfTType();
            }

            public static void CreateAndRegisterActions(CompilationStartAnalysisContext compilationStartContext, DiagnosticDescriptor removeUnusedMembersRule, DiagnosticDescriptor removeUnreadMembersRule)
            {
                var compilationAnalyzer = new CompilationAnalyzer(compilationStartContext.Compilation, removeUnusedMembersRule, removeUnreadMembersRule);
                compilationAnalyzer.RegisterActions(compilationStartContext);
            }

            private void RegisterActions(CompilationStartAnalysisContext compilationStartContext)
            {
                // We register following actions in the compilation:
                // 1. A symbol action for member symbols to ensure the member's unused state is initialized to true for every private member symbol.
                // 2. Operation actions for member references and invocations to detect member usages, i.e. read or read reference taken.
                // 3. Operation action for field initializers to detect non-constant initialization.
                // 4. Operation action for invalid operations to bail out on erroneous code.
                // 5. A symbol start/end action for named types to report diagnostics for candidate members that have no usage in executable code.
                //
                // Note that we need to register separately for OperationKind.Invocation due to https://github.com/dotnet/roslyn/issues/26206

                compilationStartContext.RegisterSymbolAction(AnalyzeSymbolDeclaration, SymbolKind.Method, SymbolKind.Field, SymbolKind.Property, SymbolKind.Event);

                compilationStartContext.RegisterSymbolStartAction(symbolStartContext =>
                {
                    var hasInvalidOperation = false;
                    symbolStartContext.RegisterOperationAction(AnalyzeMemberReferenceOperation, OperationKind.FieldReference, OperationKind.MethodReference, OperationKind.PropertyReference, OperationKind.EventReference);
                    symbolStartContext.RegisterOperationAction(AnalyzeFieldInitializer, OperationKind.FieldInitializer);
                    symbolStartContext.RegisterOperationAction(AnalyzeInvocationOperation, OperationKind.Invocation);
                    symbolStartContext.RegisterOperationAction(_ => hasInvalidOperation = true, OperationKind.Invalid);
                    symbolStartContext.RegisterSymbolEndAction(symbolEndContext => OnSymbolEnd(symbolEndContext, hasInvalidOperation));
                }, SymbolKind.NamedType);
            }

            private void AnalyzeSymbolDeclaration(SymbolAnalysisContext symbolContext)
            {
                if (IsCandidateSymbol(symbolContext.Symbol))
                {
                    lock (_gate)
                    {
                        // Initialize unused state to 'ValueUsageInfo.None' to indicate that
                        // no read/write references have been encountered yet for this symbol.
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

            private void AnalyzeFieldInitializer(OperationAnalysisContext operationContext)
            {
                // Check if the initialized fields are being initialized a non-constant value.
                // If so, we want to consider these fields as being written to,
                // so that we conservatively report an "Unread member" diagnostic instead of an "Unused member" diagnostic.
                // This ensures that we do not offer a code fix for these fields that silently removes the initializer,
                // as a non-constant initializer might have side-effects, which need to be preserved.
                // On the other hand, initialization with a constant value can have no side-effects, and is safe to be removed.
                var initializer = (IFieldInitializerOperation)operationContext.Operation;
                if (!initializer.Value.ConstantValue.HasValue)
                {
                    foreach (var field in initializer.InitializedFields)
                    {
                        OnSymbolUsage(field, ValueUsageInfo.Write);
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

            private void AnalyzeMemberReferenceOperation(OperationAnalysisContext operationContext)
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
                        //
                        // Consider the following example:
                        //      class C
                        //      {
                        //          private int _f1 = 0, _f2 = 0;
                        //          public void M1() { _f1++; }
                        //          public int M2() { return _f2++; }
                        //      }
                        //
                        // Note that the increment operation '_f1++' is child of an expression statement, which drops the result of the increment.
                        // while the increment operation '_f2++' is child of a return statement, which uses the result of the increment.
                        // For the above test, '_f1' can be safely removed without affecting the semantics of the program, while '_f2' cannot be removed.

                        if (memberReference.Parent.Parent?.Type == null)
                        {
                            valueUsageInfo = ValueUsageInfo.Write;
                        }
                    }

                    OnSymbolUsage(memberReference.Member, valueUsageInfo);
                }
            }

            private void AnalyzeInvocationOperation(OperationAnalysisContext operationContext)
            {
                var invocation = (IInvocationOperation)operationContext.Operation;
                if (IsCandidateSymbol(invocation.TargetMethod))
                {
                    // A method invocation is considered as a read reference to the symbol
                    // to ensure that we consider the method as "used".
                    OnSymbolUsage(invocation.TargetMethod, ValueUsageInfo.Read);
                }
            }

            private void OnSymbolEnd(SymbolAnalysisContext symbolEndContext, bool hasInvalidOperation)
            {
                // We bail out reporting diagnostics for named types which have any invalid operations, i.e. erroneous code.
                // We do so to ensure that we don't report false positives during editing scenarios in the IDE, where the user
                // is still editing code and fixing unresolved references to symbols, such as overload resolution errors.
                if (hasInvalidOperation)
                {
                    return;
                }

                // Report diagnostics for unused candidate members.
                var first = true;
                PooledHashSet<ISymbol> symbolsReferencedInDocComments = null;
                try
                {
                    var namedType = (INamedTypeSymbol)symbolEndContext.Symbol;
                    foreach (var member in namedType.GetMembers())
                    {
                        // Check if the underlying member is neither read nor a readable reference to the member is taken.
                        // If so, we flag the member as either unused (never written) or unread (written but not read).
                        if (TryRemove(member, out var valueUsageInfo) &&
                            !valueUsageInfo.ContainsReadOrReadableRef())
                        {
                            Debug.Assert(IsCandidateSymbol(member));
                            Debug.Assert(!member.IsImplicitlyDeclared);

                            if (first)
                            {
                                // Bail out if there are syntax errors in any of the declarations of the containing type.
                                // Note that we check this only for the first time that we report an unused or unread member for the containing type.
                                if (HasSyntaxErrors(namedType, symbolEndContext.CancellationToken))
                                {
                                    return;
                                }

                                // Compute the set of candidate symbols referenced in all the documentation comments within the named type declarations.
                                // This set is computed once and used for all the iterations of the loop.
                                symbolsReferencedInDocComments = GetCandidateSymbolsReferencedInDocComments(namedType, symbolEndContext.Compilation, symbolEndContext.CancellationToken);
                                first = false;
                            }

                            // Report IDE0051 or IDE0052 based on whether the underlying member has any Write/WritableRef/NonReadWriteRef references or not.
                            var rule = !valueUsageInfo.ContainsWriteOrWritableRef() && !valueUsageInfo.ContainsNonReadWriteRef() && !symbolsReferencedInDocComments.Contains(member)
                                ? _removeUnusedMembersRule
                                : _removeUnreadMembersRule;
                            var effectiveSeverity = rule.GetEffectiveSeverity(symbolEndContext.Compilation.Options);

                            // Most of the members should have a single location, except for partial methods.
                            // We report the diagnostic on the first location of the member.
                            var diagnostic = DiagnosticHelper.Create(
                                rule,
                                member.Locations[0],
                                effectiveSeverity,
                                additionalLocations: null,
                                properties: null,
                                member.ContainingType.Name,
                                member.Name);
                            symbolEndContext.ReportDiagnostic(diagnostic);
                        }
                    }
                }
                finally
                {
                    symbolsReferencedInDocComments?.Free();
                }

                return;
            }

            private static bool HasSyntaxErrors(INamedTypeSymbol namedTypeSymbol, CancellationToken cancellationToken)
            {
                foreach (var tree in namedTypeSymbol.Locations.Select(l => l.SourceTree))
                {
                    if (tree.GetDiagnostics(cancellationToken).Any(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        return true;
                    }
                }

                return false;
            }

            PooledHashSet<ISymbol> GetCandidateSymbolsReferencedInDocComments(INamedTypeSymbol namedTypeSymbol, Compilation compilation, CancellationToken cancellationToken)
            {
                var builder = PooledHashSet<ISymbol>.GetInstance();
                foreach (var root in namedTypeSymbol.Locations.Select(l => l.SourceTree.GetRoot(cancellationToken)))
                {
                    SemanticModel lazyModel = null;
                    foreach (var node in root.DescendantNodes(descendIntoTrivia: true)
                                             .OfType<TDocumentationCommentTriviaSyntax>()
                                             .SelectMany(n => n.DescendantNodes().OfType<TIdentifierNameSyntax>()))
                    {
                        lazyModel = lazyModel ?? compilation.GetSemanticModel(root.SyntaxTree);
                        var symbol = lazyModel.GetSymbolInfo(node, cancellationToken).Symbol;
                        if (symbol != null && IsCandidateSymbol(symbol))
                        {
                            builder.Add(symbol);
                        }
                    }
                }

                return builder;
            }

            private bool IsCandidateSymbol(ISymbol memberSymbol)
            {
                if (memberSymbol.DeclaredAccessibility == Accessibility.Private &&
                    !memberSymbol.IsImplicitlyDeclared)
                {
                    // Do not track accessors, as we will track the associated symbol.
                    switch (memberSymbol.Kind)
                    {
                        case SymbolKind.Method:
                            // Skip following methods:
                            //   1. Entry point (Main) method
                            //   2. Abstract/Virtual/Override methods
                            //   3. Extern methods
                            //   4. Interface implementation methods
                            var methodSymbol = (IMethodSymbol)memberSymbol;
                            return methodSymbol.AssociatedSymbol == null &&
                                !IsEntryPoint(methodSymbol) &&
                                !methodSymbol.IsAbstract &&
                                !methodSymbol.IsVirtual &&
                                !methodSymbol.IsOverride &&
                                !methodSymbol.IsExtern &&
                                methodSymbol.ExplicitInterfaceImplementations.IsEmpty;

                        case SymbolKind.Field:
                            return ((IFieldSymbol)memberSymbol).AssociatedSymbol == null;

                        default:
                            return true;
                    }
                }

                return false;
            }

            private bool IsEntryPoint(IMethodSymbol methodSymbol)
                => methodSymbol.Name == WellKnownMemberNames.EntryPointMethodName &&
                   methodSymbol.IsStatic &&
                   (methodSymbol.ReturnsVoid ||
                    methodSymbol.ReturnType.OriginalDefinition == _taskType ||
                    methodSymbol.ReturnType.OriginalDefinition == _genericTaskType);
        }
    }
}
