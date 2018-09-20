// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.RemoveUnusedMembers
{
    internal abstract class AbstractRemoveUnusedMembersDiagnosticAnalyzer<TDocumentationCommentTriviaSyntax, TIdentifierNameSyntax>
        : AbstractCodeQualityDiagnosticAnalyzer
        where TDocumentationCommentTriviaSyntax: SyntaxNode
        where TIdentifierNameSyntax : SyntaxNode
    {
        // IDE0051: "Remove unused members" (Symbol is declared but never referenced)
        private static readonly DiagnosticDescriptor s_removeUnusedMembersRule = CreateDescriptor(
            IDEDiagnosticIds.RemoveUnusedMembersDiagnosticId,
            new LocalizableResourceString(nameof(FeaturesResources.Remove_unused_private_members), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            new LocalizableResourceString(nameof(FeaturesResources.Private_member_0_is_unused), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            isUnneccessary: true);

        // IDE0052: "Remove unread members" (Value is written and/or symbol is referenced, but the assigned value is never read)
        private static readonly DiagnosticDescriptor s_removeUnreadMembersRule = CreateDescriptor(
            IDEDiagnosticIds.RemoveUnreadMembersDiagnosticId,
            new LocalizableResourceString(nameof(FeaturesResources.Remove_unread_private_members), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            new LocalizableResourceString(nameof(FeaturesResources.Private_member_0_can_be_removed_as_the_value_assigned_to_it_is_never_read), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            isUnneccessary: true);

        protected AbstractRemoveUnusedMembersDiagnosticAnalyzer()
            : base(ImmutableArray.Create(s_removeUnusedMembersRule, s_removeUnreadMembersRule),
                   GeneratedCodeAnalysisFlags.Analyze) // We want to analyze references in generated code, but not report unused members in generated code.
        {
        }

        public override bool OpenFileOnly(Workspace workspace) => false;

        // We need to analyze the whole document even for edits within a method body,
        // because we might add or remove references to members in executable code.
        // For example, if we had an unused field with no references, then editing any single method body
        // to reference this field should clear the unused field diagnostic.
        // Hence, we need to re-analyze the declarations in the whole file for any edits within the document. 
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(compilationStartContext
                => CompilationAnalyzer.CreateAndRegisterActions(compilationStartContext));

        private sealed class CompilationAnalyzer
        {
            private readonly object _gate;
            private readonly Dictionary<ISymbol, ValueUsageInfo> _symbolValueUsageStateMap;
            private readonly INamedTypeSymbol _taskType, _genericTaskType, _debuggerDisplayAttributeType, _structLayoutAttributeType;

            private CompilationAnalyzer(Compilation compilation)
            {
                _gate = new object();

                // State map for candidate member symbols, with the value indicating how each symbol is used in executable code.
                _symbolValueUsageStateMap = new Dictionary<ISymbol, ValueUsageInfo>();

                _taskType = compilation.TaskType();
                _genericTaskType = compilation.TaskOfTType();
                _debuggerDisplayAttributeType = compilation.DebuggerDisplayAttributeType();
                _structLayoutAttributeType = compilation.StructLayoutAttributeType();
            }

            public static void CreateAndRegisterActions(CompilationStartAnalysisContext compilationStartContext)
            {
                var compilationAnalyzer = new CompilationAnalyzer(compilationStartContext.Compilation);
                compilationAnalyzer.RegisterActions(compilationStartContext);
            }

            private void RegisterActions(CompilationStartAnalysisContext compilationStartContext)
            {
                // We register following actions in the compilation:
                // 1. A symbol action for member symbols to ensure the member's unused state is initialized to true for every private member symbol.
                // 2. Operation actions for member references, invocations and object creations to detect member usages, i.e. read or read reference taken.
                // 3. Operation action for field initializers to detect non-constant initialization.
                // 4. Operation action for invalid operations to bail out on erroneous code.
                // 5. A symbol start/end action for named types to report diagnostics for candidate members that have no usage in executable code.
                //
                // Note that we need to register separately for OperationKind.Invocation and OperationKind.ObjectCreation due to https://github.com/dotnet/roslyn/issues/26206

                compilationStartContext.RegisterSymbolAction(AnalyzeSymbolDeclaration, SymbolKind.Method, SymbolKind.Field, SymbolKind.Property, SymbolKind.Event);

                compilationStartContext.RegisterSymbolStartAction(symbolStartContext =>
                {
                    if (symbolStartContext.Symbol.GetAttributes().Any(a => a.AttributeClass == _structLayoutAttributeType))
                    {
                        // Bail out for types with 'StructLayoutAttribute' as the ordering of the members is critical,
                        // and removal of unused members might break semantics.
                        return;
                    }

                    var hasInvalidOperation = false;
                    symbolStartContext.RegisterOperationAction(AnalyzeMemberReferenceOperation, OperationKind.FieldReference, OperationKind.MethodReference, OperationKind.PropertyReference, OperationKind.EventReference);
                    symbolStartContext.RegisterOperationAction(AnalyzeFieldInitializer, OperationKind.FieldInitializer);
                    symbolStartContext.RegisterOperationAction(AnalyzeInvocationOperation, OperationKind.Invocation);
                    symbolStartContext.RegisterOperationAction(AnalyzeObjectCreationOperation, OperationKind.ObjectCreation);
                    symbolStartContext.RegisterOperationAction(_ => hasInvalidOperation = true, OperationKind.Invalid);
                    symbolStartContext.RegisterSymbolEndAction(symbolEndContext => OnSymbolEnd(symbolEndContext, hasInvalidOperation));
                }, SymbolKind.NamedType);
            }

            private void AnalyzeSymbolDeclaration(SymbolAnalysisContext symbolContext)
            {
                var symbol = symbolContext.Symbol.OriginalDefinition;
                if (IsCandidateSymbol(symbol))
                {
                    lock (_gate)
                    {
                        // Initialize unused state to 'ValueUsageInfo.None' to indicate that
                        // no read/write references have been encountered yet for this symbol.
                        // Note that we might receive a symbol reference (AnalyzeMemberOperation) callback before
                        // this symbol declaration callback, so even though we cannot receive duplicate callbacks for a symbol,
                        // an entry might already be present of the declared symbol here.
                        if (!_symbolValueUsageStateMap.ContainsKey(symbol))
                        {
                            _symbolValueUsageStateMap.Add(symbol, ValueUsageInfo.None);
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
                        if (IsCandidateSymbol(field))
                        {
                            OnSymbolUsage(field, ValueUsageInfo.Write);
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

            private void AnalyzeMemberReferenceOperation(OperationAnalysisContext operationContext)
            {
                var memberReference = (IMemberReferenceOperation)operationContext.Operation;
                var memberSymbol = memberReference.Member.OriginalDefinition;
                if (IsCandidateSymbol(memberSymbol))
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

                    OnSymbolUsage(memberSymbol, valueUsageInfo);
                }
            }

            private void AnalyzeInvocationOperation(OperationAnalysisContext operationContext)
            {
                var targetMethod = ((IInvocationOperation)operationContext.Operation).TargetMethod.OriginalDefinition;
                if (IsCandidateSymbol(targetMethod))
                {
                    // A method invocation is considered as a read reference to the symbol
                    // to ensure that we consider the method as "used".
                    OnSymbolUsage(targetMethod, ValueUsageInfo.Read);
                }
            }

            private void AnalyzeObjectCreationOperation(OperationAnalysisContext operationContext)
            {
                var constructor = ((IObjectCreationOperation)operationContext.Operation).Constructor.OriginalDefinition;
                if (IsCandidateSymbol(constructor))
                {
                    // An object creation is considered as a read reference to the constructor
                    // to ensure that we consider the constructor as "used".
                    OnSymbolUsage(constructor, ValueUsageInfo.Read);
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
                ArrayBuilder<string> debuggerDisplayAttributeArguments = null;
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

                                // Compute the set of string arguments to DebuggerDisplay attributes applied to any symbol within the named type declaration.
                                // These strings may have an embedded reference to the symbol.
                                // This set is computed once and used for all the iterations of the loop.
                                debuggerDisplayAttributeArguments = GetDebuggerDisplayAttributeArguments(namedType);

                                first = false;
                            }

                            // Simple heuristic for members referenced in DebuggerDisplayAttribute's string argument:
                            // bail out if any of the DebuggerDisplay string arguments contains the member name.
                            // In future, we can consider improving this heuristic to parse the embedded expression
                            // and resolve symbol references.
                            if (debuggerDisplayAttributeArguments.Any(arg => arg.Contains(member.Name)))
                            {
                                continue;
                            }

                            // Report IDE0051 or IDE0052 based on whether the underlying member has any Write/WritableRef/NonReadWriteRef references or not.
                            var rule = !valueUsageInfo.ContainsWriteOrWritableRef() && !valueUsageInfo.ContainsNonReadWriteRef() && !symbolsReferencedInDocComments.Contains(member)
                                ? s_removeUnusedMembersRule
                                : s_removeUnreadMembersRule;

                            // Most of the members should have a single location, except for partial methods.
                            // We report the diagnostic on the first location of the member.
                            var diagnostic = Diagnostic.Create(
                                rule,
                                member.Locations[0],
                                additionalLocations: null,
                                properties: null,
                                $"{member.ContainingType.Name}.{member.Name}");
                            symbolEndContext.ReportDiagnostic(diagnostic);
                        }
                    }
                }
                finally
                {
                    symbolsReferencedInDocComments?.Free();
                    debuggerDisplayAttributeArguments?.Free();
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

            ArrayBuilder<string> GetDebuggerDisplayAttributeArguments(INamedTypeSymbol namedTypeSymbol)
            {
                var builder = ArrayBuilder<string>.GetInstance();
                AddDebuggerDisplayAttributeArguments(namedTypeSymbol, builder);
                return builder;
            }

            void AddDebuggerDisplayAttributeArguments(INamedTypeSymbol namedTypeSymbol, ArrayBuilder<string> builder)
            {
                AddDebuggerDisplayAttributeArgumentsCore(namedTypeSymbol, builder);

                foreach (var member in namedTypeSymbol.GetMembers())
                {
                    switch (member)
                    {
                        case INamedTypeSymbol nestedType:
                            AddDebuggerDisplayAttributeArguments(nestedType, builder);
                            break;

                        case IPropertySymbol property:
                        case IFieldSymbol field:
                            AddDebuggerDisplayAttributeArgumentsCore(member, builder);
                            break;
                    }
                }
            }

            void AddDebuggerDisplayAttributeArgumentsCore(ISymbol symbol, ArrayBuilder<string> builder)
            {
                foreach (var attribute in symbol.GetAttributes())
                {
                    if (attribute.AttributeClass == _debuggerDisplayAttributeType &&
                        attribute.ConstructorArguments.Length == 1 &&
                        attribute.ConstructorArguments[0] is var arg &&
                        arg.Kind == TypedConstantKind.Primitive &&
                        arg.Type.SpecialType == SpecialType.System_String)
                    {
                        var value = arg.Value as string;
                        if (value != null)
                        {
                            builder.Add(value);
                        }
                    }
                }
            }

            private bool IsCandidateSymbol(ISymbol memberSymbol)
            {
                Debug.Assert(memberSymbol == memberSymbol.OriginalDefinition);

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
                            //   5. Constructors with no parameters.
                            //   6. Static constructors.
                            //   7. Destructors.
                            var methodSymbol = (IMethodSymbol)memberSymbol;
                            switch (methodSymbol.MethodKind)
                            {
                                case MethodKind.Constructor:
                                    return methodSymbol.Parameters.Length > 0;

                                case MethodKind.StaticConstructor:
                                case MethodKind.Destructor:
                                    return false;

                                default:
                                    return methodSymbol.AssociatedSymbol == null &&
                                           !IsEntryPoint(methodSymbol) &&
                                           !methodSymbol.IsAbstract &&
                                           !methodSymbol.IsVirtual &&
                                           !methodSymbol.IsOverride &&
                                           !methodSymbol.IsExtern &&
                                           methodSymbol.ExplicitInterfaceImplementations.IsEmpty;
                            }

                        case SymbolKind.Field:
                            return ((IFieldSymbol)memberSymbol).AssociatedSymbol == null;

                        case SymbolKind.Property:
                            return ((IPropertySymbol)memberSymbol).ExplicitInterfaceImplementations.IsEmpty;

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