// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AvoidUnusedMembers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class AvoidUnusedMembersDiagnosticAnalyzer
        : AbstractCodeStyleDiagnosticAnalyzer
    {
        internal static string UnunsedMemberNameProperty = nameof(UnunsedMemberNameProperty);
        internal static string UnunsedMemberKindProperty = nameof(UnunsedMemberKindProperty);

        private readonly DiagnosticDescriptor _noReadsUnnecessaryWithSuggestionDescriptor;

        public AvoidUnusedMembersDiagnosticAnalyzer()
            : base(
                IDEDiagnosticIds.AvoidUnusedMembersDiagnosticId,
                new LocalizableResourceString(nameof(FeaturesResources.Avoid_Unused_Members_Title), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                new LocalizableResourceString(nameof(FeaturesResources.Avoid_Unused_Members_Message), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _noReadsUnnecessaryWithSuggestionDescriptor = new DiagnosticDescriptor(
                UnnecessaryWithSuggestionDescriptor.Id,
                UnnecessaryWithSuggestionDescriptor.Title,
                new LocalizableResourceString(nameof(FeaturesResources.Avoid_Members_Without_Reads_Message), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                UnnecessaryWithSuggestionDescriptor.Category,
                UnnecessaryWithSuggestionDescriptor.DefaultSeverity,
                UnnecessaryWithSuggestionDescriptor.IsEnabledByDefault,
                UnnecessaryWithSuggestionDescriptor.Description,
                UnnecessaryWithSuggestionDescriptor.HelpLinkUri,
                UnnecessaryWithSuggestionDescriptor.CustomTags.ToArray());
        }

        public override bool OpenFileOnly(Workspace workspace) => false;

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // We want to analyze references in generated code, but not report unused members in generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                var entryPoint = compilationStartContext.Compilation.GetEntryPoint(compilationStartContext.CancellationToken);

                // State map for candidate member symbols, with the value indicating how each symbol is used in executable code.
                var symbolValueUsageStateMap = new ConcurrentDictionary<ISymbol, ValueUsageInfo>();

                // We register following actions in the compilation:
                // 1. A symbol action for member symbols to ensure the member's unused state is initialized to true for every private member symbol.
                // 2. Operation actions for member references and invocations to detect member usages, i.e. read or read reference taken.
                // 3. A symbol start/end action for named types to report diagnostics for candidate members that have no usage in executable code.

                compilationStartContext.RegisterSymbolAction(AnalyzeSymbolDeclaration, SymbolKind.Method, SymbolKind.Field, SymbolKind.Property, SymbolKind.Event);

                compilationStartContext.RegisterSymbolStartAction(symbolStartContext =>
                {
                    symbolStartContext.RegisterOperationAction(AnalyzeMemberReferenceOperation, OperationKind.FieldReference, OperationKind.MethodReference, OperationKind.PropertyReference, OperationKind.EventReference);
                    symbolStartContext.RegisterOperationAction(AnalyzeInvocationOperation, OperationKind.Invocation);
                    symbolStartContext.RegisterSymbolEndAction(OnSymbolEnd);
                }, SymbolKind.NamedType);

                return;

                // Local functions.
                void AnalyzeSymbolDeclaration(SymbolAnalysisContext symbolContext)
                {
                    if (IsCandidateSymbol(symbolContext.Symbol))
                    {
                        // Initialize unused state to 'true'.
                        symbolValueUsageStateMap.GetOrAdd(symbolContext.Symbol, valueFactory: s => ValueUsageInfo.None);
                    }
                }

                void OnSymbolUsage(ISymbol memberSymbol, ValueUsageInfo usageInfo)
                {
                    Debug.Assert(IsCandidateSymbol(memberSymbol));

                    // Update the usage info for the memberSymbol
                    symbolValueUsageStateMap.AddOrUpdate(
                        memberSymbol,
                        addValue: usageInfo,
                        updateValueFactory: (sym, currentUsageInfo) => currentUsageInfo | usageInfo);
                }

                void AnalyzeMemberReferenceOperation(OperationAnalysisContext operationContext)
                {
                    var memberReference = (IMemberReferenceOperation)operationContext.Operation;
                    if (IsCandidateSymbol(memberReference.Member))
                    {
                        // Get the value usage info.
                        var valueUsageInfo = memberReference.GetValueUsageInfo();

                        // Special case for VB
                        // Compound assigment is a statement in VB that does not return a value.
                        // So, we treat this usage as a write-only usage.
                        if (memberReference.Language == LanguageNames.VisualBasic &&
                            memberReference.Parent is ICompoundAssignmentOperation compoundAssignment &&
                            compoundAssignment.Target == memberReference)
                        {
                            valueUsageInfo = ValueUsageInfo.Write;
                        }

                        OnSymbolUsage(memberReference.Member, valueUsageInfo);
                    }
                }

                void AnalyzeInvocationOperation(OperationAnalysisContext operationContext)
                {
                    var invocation = (IInvocationOperation)operationContext.Operation;
                    if (IsCandidateSymbol(invocation.TargetMethod))
                    {
                        OnSymbolUsage(invocation.TargetMethod, ValueUsageInfo.Read);
                    }
                }

                void OnSymbolEnd(SymbolAnalysisContext symbolEndContext)
                {
                    // Report diagnostics for unused candidate members.
                    var members = ((INamedTypeSymbol)symbolEndContext.Symbol).GetMembers();
                    foreach (var member in members)
                    {
                        // Check if the underlying member is neither read nor a readable reference to the member is taken.
                        if (symbolValueUsageStateMap.TryRemove(member, out var valueUsageInfo) &&
                            (valueUsageInfo & (ValueUsageInfo.Read | ValueUsageInfo.ReadableRef)) == 0)
                        {
                            Debug.Assert(IsCandidateSymbol(member));

                            var option = GetCodeStyleOption(member, symbolEndContext);
                            if (option != null && option.Value)
                            {
                                // Give appropriate message based on usage:
                                //  1. No read or writes:   Type '{0}' has an unused private member '{1}' which can be removed.
                                //  2. Only writes:         Type '{0}' has a private member '{1}' which can be removed as the value assigned to it is never used.

                                var rule = (valueUsageInfo & (ValueUsageInfo.Write | ValueUsageInfo.WritableRef)) != 0 ?
                                    _noReadsUnnecessaryWithSuggestionDescriptor :
                                    UnnecessaryWithSuggestionDescriptor;

                                var additionalLocations = member.Locations.Skip(1).ToImmutableArrayOrEmpty();
                                var diagnostic = DiagnosticHelper.Create(
                                    rule,
                                    member.Locations[0],
                                    option.Notification.Severity,
                                    additionalLocations,
                                    properties: ImmutableDictionary.CreateRange(new[] {
                                        new KeyValuePair<string, string>(UnunsedMemberNameProperty, member.Name),
                                        new KeyValuePair<string, string>(UnunsedMemberKindProperty, member.Kind.ToString()) }),
                                    member.ContainingSymbol.Name,
                                    member.Name);
                                symbolEndContext.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }

                bool IsCandidateSymbol(ISymbol memberSymbol)
                {
                    if (memberSymbol.DeclaredAccessibility == Accessibility.Private &&
                        !memberSymbol.IsImplicitlyDeclared)
                    {
                        // Do not track accessors, as we will track the associated symbol.
                        switch (memberSymbol.Kind)
                        {
                            case SymbolKind.Method:
                                return ((IMethodSymbol)memberSymbol).AssociatedSymbol == null &&
                                    memberSymbol != entryPoint;

                            case SymbolKind.Field:
                                return ((IFieldSymbol)memberSymbol).AssociatedSymbol == null;

                            default:
                                return true;
                        }
                    }

                    return false;
                }

                CodeStyleOption<bool> GetCodeStyleOption(ISymbol memberSymbol, SymbolAnalysisContext symbolEndContext)
                {
                    var optionSet = symbolEndContext.Options.GetDocumentOptionSetAsync(memberSymbol.Locations[0].SourceTree, symbolEndContext.CancellationToken).GetAwaiter().GetResult();
                    return optionSet?.GetOption(CodeStyleOptions.AvoidUnusedMembers, memberSymbol.Language);
                }
            });
        }
    }
}
