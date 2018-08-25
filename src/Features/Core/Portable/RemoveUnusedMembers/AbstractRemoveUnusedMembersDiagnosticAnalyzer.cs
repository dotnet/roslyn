// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnusedMembers
{
    internal abstract class AbstractRemoveUnusedMembersDiagnosticAnalyzer
        : AbstractCodeStyleDiagnosticAnalyzer
    {
        private readonly bool _treatCompoundAssignmentAsWriteOnlyOperation;

        public AbstractRemoveUnusedMembersDiagnosticAnalyzer(bool treatCompoundAssignmentAsWriteOnlyOperation = false)
            : base(
                IDEDiagnosticIds.RemoveUnusedMembersDiagnosticId,
                new LocalizableResourceString(nameof(FeaturesResources.Remove_Unused_Private_Members), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                new LocalizableResourceString(nameof(FeaturesResources.Remove_Unused_Private_Members_Message), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _treatCompoundAssignmentAsWriteOnlyOperation = treatCompoundAssignmentAsWriteOnlyOperation;
        }

        public override bool OpenFileOnly(Workspace workspace) => false;

        // We need to analyze the whole document even for edits within a method body.
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            // We want to analyze references in generated code, but not report unused members in generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                var compilationAnalyzer = new CompilationAnalyzer(compilationStartContext.Compilation, UnnecessaryWithSuggestionDescriptor, _treatCompoundAssignmentAsWriteOnlyOperation);

                // We register following actions in the compilation:
                // 1. A symbol action for member symbols to ensure the member's unused state is initialized to true for every private member symbol.
                // 2. Operation actions for member references and invocations to detect member usages, i.e. read or read reference taken.
                // 3. A symbol start/end action for named types to report diagnostics for candidate members that have no usage in executable code.
                //
                // Note that we need to register separately for OperationKind.Invocation due to https://github.com/dotnet/roslyn/issues/26206
                
                compilationStartContext.RegisterSymbolAction(compilationAnalyzer.AnalyzeSymbolDeclaration, SymbolKind.Method, SymbolKind.Field, SymbolKind.Property, SymbolKind.Event);

                compilationStartContext.RegisterSymbolStartAction(symbolStartContext =>
                {
                    symbolStartContext.RegisterOperationAction(compilationAnalyzer.AnalyzeMemberReferenceOperation, OperationKind.FieldReference, OperationKind.MethodReference, OperationKind.PropertyReference, OperationKind.EventReference);
                    symbolStartContext.RegisterOperationAction(compilationAnalyzer.AnalyzeInvocationOperation, OperationKind.Invocation);
                    symbolStartContext.RegisterSymbolEndAction(compilationAnalyzer.OnSymbolEnd);
                }, SymbolKind.NamedType);
            });
        }

        private sealed class CompilationAnalyzer
        {
            private readonly DiagnosticDescriptor _rule;
            private readonly bool _treatCompoundAssignmentAsWriteOnlyOperation;
            private readonly object _gate;
            private readonly Dictionary<ISymbol, ValueUsageInfo> _symbolValueUsageStateMap;
            private readonly Lazy<INamedTypeSymbol> _lazyTaskType, _lazyGenericTaskType;

            public CompilationAnalyzer(Compilation compilation, DiagnosticDescriptor rule, bool treatCompoundAssignmentAsWriteOnlyOperation)
            {
                _rule = rule;
                _treatCompoundAssignmentAsWriteOnlyOperation = treatCompoundAssignmentAsWriteOnlyOperation;
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

                    // Usages which are neither value read nor value write (e.g. in nameof, typeof, sizeof)
                    // are treated as a read to avoid flagging them.
                    // https://github.com/dotnet/roslyn/issues/29519 covers improving this behavior.
                    if (valueUsageInfo == ValueUsageInfo.None)
                    {
                        valueUsageInfo = ValueUsageInfo.Read;
                    }

                    // Is this a compound assignment that must be treated as a write-only usage?
                    if (_treatCompoundAssignmentAsWriteOnlyOperation &&
                        memberReference.Parent is ICompoundAssignmentOperation compoundAssignment &&
                        compoundAssignment.Target == memberReference)
                    {
                        valueUsageInfo = ValueUsageInfo.Write;
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

            public void OnSymbolEnd(SymbolAnalysisContext symbolEndContext)
            {
                // Report diagnostics for unused candidate members.
                var members = ((INamedTypeSymbol)symbolEndContext.Symbol).GetMembers();
                foreach (var member in members)
                {
                    // Check if the underlying member is neither read nor a readable reference to the member is taken.
                    if (TryRemove(member, out var valueUsageInfo) &&
                        !valueUsageInfo.ContainsReadOrReadableRef())
                    {
                        Debug.Assert(IsCandidateSymbol(member));
                        Debug.Assert(!member.IsImplicitlyDeclared);

                        var option = TryGetCodeStyleOption(member, symbolEndContext);
                        if (option != null && option.Value)
                        {
                            var diagnostic = DiagnosticHelper.Create(
                                _rule,
                                member.Locations[0],
                                option.Notification.Severity,
                                additionalLocations: member.Locations,
                                properties: null,
                                member.ContainingType.Name,
                                member.Name);
                            symbolEndContext.ReportDiagnostic(diagnostic);
                        }
                    }
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

            private static CodeStyleOption<bool> TryGetCodeStyleOption(ISymbol memberSymbol, SymbolAnalysisContext symbolEndContext)
            {
                var optionSet = symbolEndContext.Options.GetDocumentOptionSetAsync(memberSymbol.Locations[0].SourceTree, symbolEndContext.CancellationToken).GetAwaiter().GetResult();
                return optionSet?.GetOption(CodeStyleOptions.RemoveUnusedMembers, memberSymbol.Language);
            }
        }
    }
}
