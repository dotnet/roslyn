// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnusedMembers;

internal abstract class AbstractRemoveUnusedMembersDiagnosticAnalyzer<
    TDocumentationCommentTriviaSyntax,
    TIdentifierNameSyntax,
    TTypeDeclarationSyntax,
    TMemberDeclarationSyntax>
    : AbstractCodeQualityDiagnosticAnalyzer
    where TDocumentationCommentTriviaSyntax : SyntaxNode
    where TIdentifierNameSyntax : SyntaxNode
    where TTypeDeclarationSyntax : TMemberDeclarationSyntax
    where TMemberDeclarationSyntax : SyntaxNode
{
    /// <summary>
    /// Produces names like TypeName.MemberName
    /// </summary>
    private static readonly SymbolDisplayFormat ContainingTypeAndNameOnlyFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

    // IDE0051: "Remove unused members" (Symbol is declared but never referenced)
    private static readonly DiagnosticDescriptor s_removeUnusedMembersRule = CreateDescriptor(
        IDEDiagnosticIds.RemoveUnusedMembersDiagnosticId,
        EnforceOnBuildValues.RemoveUnusedMembers,
        new LocalizableResourceString(nameof(AnalyzersResources.Remove_unused_private_members), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Private_member_0_is_unused), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        hasAnyCodeStyleOption: false, isUnnecessary: false);

    // IDE0052: "Remove unread members" (Value is written and/or symbol is referenced, but the assigned value is never read)
    // Internal for testing
    internal static readonly DiagnosticDescriptor s_removeUnreadMembersRule = CreateDescriptor(
        IDEDiagnosticIds.RemoveUnreadMembersDiagnosticId,
        EnforceOnBuildValues.RemoveUnreadMembers,
        new LocalizableResourceString(nameof(AnalyzersResources.Remove_unread_private_members), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Private_member_0_can_be_removed_as_the_value_assigned_to_it_is_never_read), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        hasAnyCodeStyleOption: false, isUnnecessary: false);

    protected AbstractRemoveUnusedMembersDiagnosticAnalyzer()
        : base([s_removeUnusedMembersRule, s_removeUnreadMembersRule],
               GeneratedCodeAnalysisFlags.Analyze) // We want to analyze references in generated code, but not report unused members in generated code.
    {
    }

    protected abstract IEnumerable<TTypeDeclarationSyntax> GetTypeDeclarations(INamedTypeSymbol namedType, CancellationToken cancellationToken);
    protected abstract SyntaxList<TMemberDeclarationSyntax> GetMembers(TTypeDeclarationSyntax typeDeclaration);
    protected abstract SyntaxNode GetParentIfSoleDeclarator(SyntaxNode declaration);

    // We need to analyze the whole document even for edits within a method body,
    // because we might add or remove references to members in executable code.
    // For example, if we had an unused field with no references, then editing any single method body
    // to reference this field should clear the unused field diagnostic.
    // Hence, we need to re-analyze the declarations in the whole file for any edits within the document. 
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(compilationStartContext
            => CompilationAnalyzer.CreateAndRegisterActions(compilationStartContext, this));

    /// <summary>
    /// Override this method to register custom language specific actions to find symbol usages.
    /// </summary>
    protected virtual void HandleNamedTypeSymbolStart(SymbolStartAnalysisContext context, Action<ISymbol, ValueUsageInfo> onSymbolUsageFound)
    {
    }

    private sealed class CompilationAnalyzer
    {
        private readonly object _gate;
        /// <summary>
        /// State map for candidate member symbols, with the value indicating how each symbol is used in executable code.
        /// </summary>
        private readonly Dictionary<ISymbol, ValueUsageInfo> _symbolValueUsageStateMap = [];
        /// <summary>
        /// List of properties that have a 'get' accessor usage, while the value itself is not used, e.g.:
        /// <code>
        /// class C
        /// {
        ///     private int P { get; set; }
        ///     public void M() { P++; }
        /// }
        /// </code>
        /// Here, 'get' accessor is used in an increment operation, but the result of the increment operation isn't used and 'P' itself is not used anywhere else, so it can be safely removed
        /// </summary>
        private readonly HashSet<IPropertySymbol> _propertiesWithShadowGetAccessorUsages = [];
        private readonly INamedTypeSymbol? _taskType, _genericTaskType, _debuggerDisplayAttributeType, _structLayoutAttributeType;
        private readonly INamedTypeSymbol? _eventArgsType;
        private readonly INamedTypeSymbol? _iNotifyCompletionType;
        private readonly DeserializationConstructorCheck _deserializationConstructorCheck;
        private readonly ImmutableHashSet<INamedTypeSymbol?> _attributeSetForMethodsToIgnore;
        private readonly AbstractRemoveUnusedMembersDiagnosticAnalyzer<TDocumentationCommentTriviaSyntax, TIdentifierNameSyntax, TTypeDeclarationSyntax, TMemberDeclarationSyntax> _analyzer;

        private CompilationAnalyzer(
            Compilation compilation,
            AbstractRemoveUnusedMembersDiagnosticAnalyzer<TDocumentationCommentTriviaSyntax, TIdentifierNameSyntax, TTypeDeclarationSyntax, TMemberDeclarationSyntax> analyzer)
        {
            _gate = new object();
            _analyzer = analyzer;

            _taskType = compilation.TaskType();
            _genericTaskType = compilation.TaskOfTType();
            _debuggerDisplayAttributeType = compilation.DebuggerDisplayAttributeType();
            _structLayoutAttributeType = compilation.StructLayoutAttributeType();
            _eventArgsType = compilation.EventArgsType();
            _iNotifyCompletionType = compilation.GetBestTypeByMetadataName(typeof(INotifyCompletion).FullName!);
            _deserializationConstructorCheck = new DeserializationConstructorCheck(compilation);
            _attributeSetForMethodsToIgnore = ImmutableHashSet.CreateRange<INamedTypeSymbol?>(GetAttributesForMethodsToIgnore(compilation));
        }

        private static Location GetDiagnosticLocation(ISymbol symbol)
            => symbol.Locations[0];

        private static IEnumerable<INamedTypeSymbol> GetAttributesForMethodsToIgnore(Compilation compilation)
        {
            // Ignore methods with special serialization attributes, which are invoked by the runtime
            // for deserialization.
            var onDeserializingAttribute = compilation.OnDeserializingAttribute();
            if (onDeserializingAttribute != null)
            {
                yield return onDeserializingAttribute;
            }

            var onDeserializedAttribute = compilation.OnDeserializedAttribute();
            if (onDeserializedAttribute != null)
            {
                yield return onDeserializedAttribute;
            }

            var onSerializingAttribute = compilation.OnSerializingAttribute();
            if (onSerializingAttribute != null)
            {
                yield return onSerializingAttribute;
            }

            var onSerializedAttribute = compilation.OnSerializedAttribute();
            if (onSerializedAttribute != null)
            {
                yield return onSerializedAttribute;
            }

            var comRegisterFunctionAttribute = compilation.ComRegisterFunctionAttribute();
            if (comRegisterFunctionAttribute != null)
            {
                yield return comRegisterFunctionAttribute;
            }

            var comUnregisterFunctionAttribute = compilation.ComUnregisterFunctionAttribute();
            if (comUnregisterFunctionAttribute != null)
            {
                yield return comUnregisterFunctionAttribute;
            }
        }

        public static void CreateAndRegisterActions(
            CompilationStartAnalysisContext compilationStartContext,
            AbstractRemoveUnusedMembersDiagnosticAnalyzer<TDocumentationCommentTriviaSyntax, TIdentifierNameSyntax, TTypeDeclarationSyntax, TMemberDeclarationSyntax> analyzer)
        {
            var compilationAnalyzer = new CompilationAnalyzer(compilationStartContext.Compilation, analyzer);
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

            Action<ISymbol, ValueUsageInfo> onSymbolUsageFound = OnSymbolUsage;
            compilationStartContext.RegisterSymbolStartAction(symbolStartContext =>
            {
                if (!ShouldAnalyze(symbolStartContext, (INamedTypeSymbol)symbolStartContext.Symbol))
                    return;

                var hasUnsupportedOperation = false;
                symbolStartContext.RegisterOperationAction(AnalyzeMemberReferenceOperation, OperationKind.FieldReference, OperationKind.MethodReference, OperationKind.PropertyReference, OperationKind.EventReference);
                symbolStartContext.RegisterOperationAction(AnalyzeFieldInitializer, OperationKind.FieldInitializer);
                symbolStartContext.RegisterOperationAction(AnalyzeInvocationOperation, OperationKind.Invocation);
                symbolStartContext.RegisterOperationAction(AnalyzeNameOfOperation, OperationKind.NameOf);
                symbolStartContext.RegisterOperationAction(AnalyzeObjectCreationOperation, OperationKind.ObjectCreation);

                // We bail out reporting diagnostics for named types if it contains following kind of operations:
                //  1. Invalid operations, i.e. erroneous code:
                //     We do so to ensure that we don't report false positives during editing scenarios in the IDE, where the user
                //     is still editing code and fixing unresolved references to symbols, such as overload resolution errors.
                //  2. Dynamic operations, where we do not know the exact member being referenced at compile time.
                //  3. Operations with OperationKind.None.
                symbolStartContext.RegisterOperationAction(_ => hasUnsupportedOperation = true, OperationKind.Invalid, OperationKind.None,
                    OperationKind.DynamicIndexerAccess, OperationKind.DynamicInvocation, OperationKind.DynamicMemberReference, OperationKind.DynamicObjectCreation);

                symbolStartContext.RegisterSymbolEndAction(symbolEndContext => OnSymbolEnd(symbolEndContext, hasUnsupportedOperation));

                // Register custom language-specific actions, if any.
                _analyzer.HandleNamedTypeSymbolStart(symbolStartContext, onSymbolUsageFound);
            }, SymbolKind.NamedType);

            bool ShouldAnalyze(SymbolStartAnalysisContext context, INamedTypeSymbol namedType)
            {
                // Check if we have at least one candidate symbol in analysis scope.
                foreach (var member in namedType.GetMembers())
                {
                    if (IsCandidateSymbol(member.OriginalDefinition)
                        && context.ShouldAnalyzeLocation(GetDiagnosticLocation(member)))
                    {
                        return true;
                    }
                }

                // We have to analyze nested types if containing type contains a candidate field in analysis scope.
                if (namedType.ContainingType is { } containingType)
                    return ShouldAnalyze(context, containingType);

                return false;
            }
        }

        private void AnalyzeSymbolDeclaration(SymbolAnalysisContext symbolContext)
        {
            var symbol = symbolContext.Symbol.OriginalDefinition;
            if (IsCandidateSymbol(symbol)
                && symbolContext.ShouldAnalyzeLocation(GetDiagnosticLocation(symbol)))
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
                    OnSymbolUsage(field, ValueUsageInfo.Write);
                }
            }
        }

        private void OnSymbolUsage(ISymbol? memberSymbol, ValueUsageInfo usageInfo)
        {
            if (!IsCandidateSymbol(memberSymbol))
            {
                return;
            }

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
                var valueUsageInfo = memberReference.GetValueUsageInfo(operationContext.ContainingSymbol);

                if (valueUsageInfo == ValueUsageInfo.ReadWrite)
                {
                    Debug.Assert(memberReference.Parent is ICompoundAssignmentOperation compoundAssignment &&
                        compoundAssignment.Target == memberReference ||
                        memberReference.Parent is ICoalesceAssignmentOperation coalesceAssignment &&
                        coalesceAssignment.Target == memberReference ||
                        memberReference.Parent is IIncrementOrDecrementOperation ||
                        memberReference.Parent is IReDimClauseOperation reDimClause && reDimClause.Operand == memberReference);

                    // Compound assignment or increment whose value is being dropped (parent is an expression statement)
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
                    // Additionally, we special case ICoalesceAssignmentOperation (??=) and treat it as a read-write,
                    // see https://github.com/dotnet/roslyn/issues/66975 for more details

                    if (memberReference?.Parent?.Parent is IExpressionStatementOperation &&
                        memberReference.Parent is not ICoalesceAssignmentOperation)
                    {
                        valueUsageInfo = ValueUsageInfo.Write;

                        // If the symbol is a property, than mark it as having shadow 'get' accessor usages.
                        // Later we will produce message "Private member X can be removed as the value assigned to it is never read"
                        // rather than "Private property X can be converted to a method as its get accessor is never invoked" depending on this information.
                        if (memberSymbol is IPropertySymbol propertySymbol)
                        {
                            lock (_gate)
                            {
                                _propertiesWithShadowGetAccessorUsages.Add(propertySymbol);
                            }
                        }
                    }
                }

                OnSymbolUsage(memberSymbol, valueUsageInfo);
            }
        }

        private void AnalyzeInvocationOperation(OperationAnalysisContext operationContext)
        {
            var targetMethod = ((IInvocationOperation)operationContext.Operation).TargetMethod.OriginalDefinition;

            // A method invocation is considered as a read reference to the symbol
            // to ensure that we consider the method as "used".
            OnSymbolUsage(targetMethod, ValueUsageInfo.Read);

            // If the invoked method is a reduced extension method, also mark the original
            // method from which it was reduced as "used".
            if (targetMethod.ReducedFrom != null)
            {
                OnSymbolUsage(targetMethod.ReducedFrom, ValueUsageInfo.Read);
            }
        }

        private void AnalyzeNameOfOperation(OperationAnalysisContext operationContext)
        {
            // 'nameof(argument)' is very commonly used for reading/writing to 'argument' in following ways:
            //   1. Reflection based usage: See https://github.com/dotnet/roslyn/issues/32488
            //   2. Custom/Test frameworks: See https://github.com/dotnet/roslyn/issues/32008 and https://github.com/dotnet/roslyn/issues/31581
            // We treat 'nameof(argument)' as ValueUsageInfo.ReadWrite instead of ValueUsageInfo.NameOnly to avoid such false positives.

            var nameofArgument = ((INameOfOperation)operationContext.Operation).Argument;

            if (nameofArgument is IMemberReferenceOperation memberReference)
            {
                OnSymbolUsage(memberReference.Member.OriginalDefinition, ValueUsageInfo.ReadWrite);
                return;
            }

            // Workaround for https://github.com/dotnet/roslyn/issues/19965
            // IOperation API does not expose potential references to methods/properties within
            // a bound method group/property group.
            var symbolInfo = nameofArgument.SemanticModel!.GetSymbolInfo(nameofArgument.Syntax, operationContext.CancellationToken);
            foreach (var symbol in symbolInfo.GetAllSymbols())
            {
                switch (symbol.Kind)
                {
                    // Handle potential references to methods/properties from missing IOperation
                    // for method group/property group.
                    case SymbolKind.Method:
                    case SymbolKind.Property:
                        OnSymbolUsage(symbol.OriginalDefinition, ValueUsageInfo.ReadWrite);
                        break;
                }
            }
        }

        private void AnalyzeObjectCreationOperation(OperationAnalysisContext operationContext)
        {
            var constructor = ((IObjectCreationOperation)operationContext.Operation).Constructor?.OriginalDefinition;

            // An object creation is considered as a read reference to the constructor
            // to ensure that we consider the constructor as "used".
            OnSymbolUsage(constructor, ValueUsageInfo.Read);
        }

        private void OnSymbolEnd(SymbolAnalysisContext symbolEndContext, bool hasUnsupportedOperation)
        {
            var cancellationToken = symbolEndContext.CancellationToken;
            if (hasUnsupportedOperation)
            {
                return;
            }

            if (symbolEndContext.Symbol.GetAttributes().Any(static (a, self) => a.AttributeClass == self._structLayoutAttributeType, this))
            {
                // Bail out for types with 'StructLayoutAttribute' as the ordering of the members is critical,
                // and removal of unused members might break semantics.
                return;
            }

            // Report diagnostics for unused candidate members.
            var first = true;
            using var _1 = PooledHashSet<ISymbol>.GetInstance(out var symbolsReferencedInDocComments);
            using var _2 = ArrayBuilder<string>.GetInstance(out var debuggerDisplayAttributeArguments);

            var entryPoint = symbolEndContext.Compilation.GetEntryPoint(cancellationToken);

            var namedType = (INamedTypeSymbol)symbolEndContext.Symbol;
            foreach (var member in namedType.GetMembers())
            {
                if (SymbolEqualityComparer.Default.Equals(entryPoint, member))
                {
                    continue;
                }

                // Check if the underlying member is neither read nor a readable reference to the member is taken.
                // If so, we flag the member as either unused (never written) or unread (written but not read).
                if (TryRemove(member, out var valueUsageInfo) &&
                    !valueUsageInfo.IsReadFrom())
                {
                    Debug.Assert(IsCandidateSymbol(member));
                    Debug.Assert(!member.IsImplicitlyDeclared);

                    if (first)
                    {
                        // Bail out if there are syntax errors in any of the declarations of the containing type.
                        // Note that we check this only for the first time that we report an unused or unread member for the containing type.
                        if (HasSyntaxErrors(namedType, cancellationToken))
                        {
                            return;
                        }

                        // Compute the set of candidate symbols referenced in all the documentation comments within the named type declarations.
                        // This set is computed once and used for all the iterations of the loop.
                        AddCandidateSymbolsReferencedInDocComments(
                            namedType, symbolEndContext.Compilation, symbolsReferencedInDocComments, cancellationToken);

                        // Compute the set of string arguments to DebuggerDisplay attributes applied to any symbol within the named type declaration.
                        // These strings may have an embedded reference to the symbol.
                        // This set is computed once and used for all the iterations of the loop.
                        AddDebuggerDisplayAttributeArguments(namedType, debuggerDisplayAttributeArguments);

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
                    var rule = !valueUsageInfo.IsWrittenTo() && !valueUsageInfo.IsNameOnly() && !symbolsReferencedInDocComments.Contains(member)
                        ? s_removeUnusedMembersRule
                        : s_removeUnreadMembersRule;

                    // Do not flag write-only properties that are not read.
                    // Write-only properties are assumed to have side effects
                    // visible through other means than a property getter.
                    if (rule == s_removeUnreadMembersRule &&
                        member is IPropertySymbol property &&
                        property.IsWriteOnly)
                    {
                        continue;
                    }

                    var diagnosticLocation = GetDiagnosticLocation(member);
                    var fadingLocation = member.DeclaringSyntaxReferences.FirstOrDefault(
                        r => r.SyntaxTree == diagnosticLocation.SourceTree && r.Span.Contains(diagnosticLocation.SourceSpan));

                    var fadingNode = fadingLocation?.GetSyntax(cancellationToken);
                    fadingNode = fadingNode != null ? this._analyzer.GetParentIfSoleDeclarator(fadingNode) : null;

                    // Most of the members should have a single location, except for partial methods.
                    // We report the diagnostic on the first location of the member.
                    symbolEndContext.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                        rule,
                        diagnosticLocation,
                        NotificationOption2.ForSeverity(rule.DefaultSeverity),
                        symbolEndContext.Options,
                        additionalLocations: [],
                        additionalUnnecessaryLocations: fadingNode is null ? [] : [fadingNode.GetLocation()],
                        properties: null,
                        GetMessage(rule, member)));
                }
            }
        }

        private LocalizableString GetMessage(
           DiagnosticDescriptor rule,
           ISymbol member)
        {
            var messageFormat = rule.MessageFormat;
            if (rule == s_removeUnreadMembersRule)
            {
                // IDE0052 has a different message for method and property symbols.
                switch (member)
                {
                    case IMethodSymbol:
                        messageFormat = AnalyzersResources.Private_method_0_can_be_removed_as_it_is_never_invoked;
                        break;

                    case IPropertySymbol property:
                        // We change the message only if both 'get' and 'set' accessors are present and
                        // there are no shadow 'get' accessor usages. Otherwise the message will be confusing
                        if (property.GetMethod != null && property.SetMethod != null &&
                            !_propertiesWithShadowGetAccessorUsages.Contains(property))
                        {
                            messageFormat = AnalyzersResources.Private_property_0_can_be_converted_to_a_method_as_its_get_accessor_is_never_invoked;
                        }

                        break;
                }
            }

            return new DiagnosticHelper.LocalizableStringWithArguments(
                messageFormat, member.ToDisplayString(ContainingTypeAndNameOnlyFormat));
        }

        private static bool HasSyntaxErrors(INamedTypeSymbol namedTypeSymbol, CancellationToken cancellationToken)
        {
            foreach (var tree in namedTypeSymbol.Locations.Select(l => l.SourceTree).Distinct().WhereNotNull())
            {
                if (tree.GetDiagnostics(cancellationToken).Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddCandidateSymbolsReferencedInDocComments(
            INamedTypeSymbol namedTypeSymbol,
            Compilation compilation,
            HashSet<ISymbol> builder,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<TDocumentationCommentTriviaSyntax>.GetInstance(out var documentationComments);
            AddAllDocumentationComments();

            // Group by syntax tree so we can process all partial types within a tree at once, using just a single
            // semantic model.
            foreach (var group in documentationComments.GroupBy(d => d.SyntaxTree))
            {
                var syntaxTree = group.Key;
                SemanticModel? lazyModel = null;

                foreach (var docComment in group)
                {
                    // Note: we could likely optimize this further by only analyzing identifier nodes that have a
                    // matching name to one of the candidate symbols we care about.
                    foreach (var node in docComment.DescendantNodes().OfType<TIdentifierNameSyntax>())
                    {
                        lazyModel ??= compilation.GetSemanticModel(syntaxTree);
                        var symbol = lazyModel.GetSymbolInfo(node, cancellationToken).Symbol?.OriginalDefinition;

                        if (IsCandidateSymbol(symbol))
                            builder.Add(symbol);
                    }
                }
            }

            return;

            void AddAllDocumentationComments()
            {
                using var _ = ArrayBuilder<TTypeDeclarationSyntax>.GetInstance(out var stack);

                // Defer to subclass to give us the type decl nodes for this named type.
                foreach (var typeDeclaration in _analyzer.GetTypeDeclarations(namedTypeSymbol, cancellationToken))
                {
                    stack.Clear();
                    stack.Push(typeDeclaration);

                    while (stack.TryPop(out var currentType))
                    {
                        // Add the doc comments on the type itself.
                        AddDocumentationComments(currentType, documentationComments);

                        // Walk each member
                        foreach (var member in _analyzer.GetMembers(currentType))
                        {
                            if (member is TTypeDeclarationSyntax childType)
                            {
                                // If the member is a nested type, recurse into it.
                                stack.Push(childType);
                            }
                            else
                            {
                                // Otherwise, add the doc comments on the member itself.
                                AddDocumentationComments(member, documentationComments);
                            }
                        }
                    }
                }
            }

            static void AddDocumentationComments(
                SyntaxNode memberDeclaration, ArrayBuilder<TDocumentationCommentTriviaSyntax> documentationComments)
            {
                var firstToken = memberDeclaration.GetFirstToken();
                if (!firstToken.HasStructuredTrivia)
                    return;

                foreach (var trivia in firstToken.LeadingTrivia)
                {
                    if (trivia.HasStructure)
                        documentationComments.AddIfNotNull(trivia.GetStructure() as TDocumentationCommentTriviaSyntax);
                }
            }
        }

        private void AddDebuggerDisplayAttributeArguments(INamedTypeSymbol namedTypeSymbol, ArrayBuilder<string> builder)
        {
            AddDebuggerDisplayAttributeArgumentsCore(namedTypeSymbol, builder);

            foreach (var member in namedTypeSymbol.GetMembers())
            {
                switch (member)
                {
                    case INamedTypeSymbol nestedType:
                        AddDebuggerDisplayAttributeArguments(nestedType, builder);
                        break;

                    case IPropertySymbol or IFieldSymbol:
                        AddDebuggerDisplayAttributeArgumentsCore(member, builder);
                        break;
                }
            }
        }

        private void AddDebuggerDisplayAttributeArgumentsCore(ISymbol symbol, ArrayBuilder<string> builder)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass == _debuggerDisplayAttributeType &&
                    attribute.ConstructorArguments is [{ Kind: TypedConstantKind.Primitive, Type.SpecialType: SpecialType.System_String, Value: string value }])
                {
                    builder.Add(value);
                }
            }
        }

        /// <summary>
        /// Returns true if the given symbol meets the following criteria to be
        /// a candidate for dead code analysis:
        ///     1. It is marked as "private".
        ///     2. It is not an implicitly declared symbol.
        ///     3. It is either a method, field, property or an event.
        ///     4. If method, then it is a constructor OR a method with <see cref="MethodKind.Ordinary"/>,
        ///        such that is meets a few criteria (see implementation details below).
        ///     5. If field, then it must not be a backing field for an auto property.
        ///        Backing fields have a non-null <see cref="IFieldSymbol.AssociatedSymbol"/>.
        ///     6. If property, then it must not be an explicit interface property implementation
        ///        or the 'IsCompleted' property which is needed to make a type awaitable.
        ///     7. If event, then it must not be an explicit interface event implementation.
        /// </summary>
        private bool IsCandidateSymbol([NotNullWhen(true)] ISymbol? memberSymbol)
        {
            if (memberSymbol is null)
                return false;

            Debug.Assert(memberSymbol == memberSymbol.OriginalDefinition);

            if (memberSymbol.DeclaredAccessibility == Accessibility.Private &&
                !memberSymbol.IsImplicitlyDeclared)
            {
                switch (memberSymbol.Kind)
                {
                    case SymbolKind.Method:
                        var methodSymbol = (IMethodSymbol)memberSymbol;
                        switch (methodSymbol.MethodKind)
                        {
                            case MethodKind.Constructor:
                                // It is fine to have an unused private constructor
                                // without parameters.
                                // This is commonly used for static holder types
                                // that want to block instantiation of the type.
                                if (methodSymbol.Parameters.Length == 0)
                                {
                                    return false;
                                }

                                // Having a private copy constructor in a record means it's implicitly used by
                                // the record's clone method
                                if (methodSymbol.ContainingType.IsRecord &&
                                    methodSymbol.Parameters.Length == 1 &&
                                    methodSymbol.Parameters[0].RefKind == RefKind.None &&
                                    methodSymbol.Parameters[0].Type.Equals(memberSymbol.ContainingType))
                                {
                                    return false;
                                }

                                // ISerializable constructor is invoked by the runtime for deserialization
                                // and it is a common pattern to have a private serialization constructor
                                // that is not explicitly referenced in code.
                                if (_deserializationConstructorCheck.IsDeserializationConstructor(methodSymbol))
                                {
                                    return false;
                                }

                                return true;

                            case MethodKind.Ordinary:
                                // Do not track accessors, as we will track/flag the associated symbol.
                                if (methodSymbol.AssociatedSymbol != null)
                                {
                                    return false;
                                }

                                // Do not flag unused entry point (Main) method.
                                if (methodSymbol.IsEntryPoint(_taskType, _genericTaskType))
                                {
                                    return false;
                                }

                                // It is fine to have unused virtual/abstract/overrides/extern
                                // methods as they might be used in another type in the containing
                                // type's type hierarchy.
                                if (methodSymbol.IsAbstract ||
                                    methodSymbol.IsVirtual ||
                                    methodSymbol.IsOverride ||
                                    methodSymbol.IsExtern)
                                {
                                    return false;
                                }

                                // Explicit interface implementations are not referenced explicitly,
                                // but are still used.
                                if (!methodSymbol.ExplicitInterfaceImplementations.IsEmpty)
                                {
                                    return false;
                                }

                                // Ignore methods with special attributes that indicate special/reflection
                                // based access.
                                if (IsMethodWithSpecialAttribute(methodSymbol))
                                {
                                    return false;
                                }

                                // ShouldSerializeXXX and ResetXXX are ok if there is a matching
                                // property XXX as they are used by the windows designer property grid
                                if (IsShouldSerializeOrResetPropertyMethod(methodSymbol))
                                {
                                    return false;
                                }

                                // Ignore methods with event handler signature
                                // as lot of ASP.NET types have many special event handlers
                                // that are invoked with reflection (e.g. Application_XXX, Page_XXX,
                                // OnTransactionXXX, etc).
                                if (methodSymbol.HasEventHandlerSignature(_eventArgsType))
                                {
                                    return false;
                                }

                                // Ignore methods which make a type awaitable.
                                if (_iNotifyCompletionType != null && Roslyn.Utilities.ImmutableArrayExtensions.Contains(methodSymbol.ContainingType.AllInterfaces, _iNotifyCompletionType, SymbolEqualityComparer.Default)
                                    && methodSymbol.Name is "GetAwaiter" or "GetResult")
                                {
                                    return false;
                                }

                                return true;

                            default:
                                return false;
                        }

                    case SymbolKind.Field:
                        return ((IFieldSymbol)memberSymbol).AssociatedSymbol == null;

                    case SymbolKind.Property:
                        if (_iNotifyCompletionType != null && memberSymbol.ContainingType.AllInterfaces.Contains(_iNotifyCompletionType) && memberSymbol.Name == "IsCompleted")
                        {
                            return false;
                        }

                        return ((IPropertySymbol)memberSymbol).ExplicitInterfaceImplementations.IsEmpty;

                    case SymbolKind.Event:
                        return ((IEventSymbol)memberSymbol).ExplicitInterfaceImplementations.IsEmpty;
                }
            }

            return false;
        }

        private bool IsMethodWithSpecialAttribute(IMethodSymbol methodSymbol)
            => methodSymbol.GetAttributes().Any(static (a, self) => self._attributeSetForMethodsToIgnore.Contains(a.AttributeClass), this);

        private static bool IsShouldSerializeOrResetPropertyMethod(IMethodSymbol methodSymbol)
        {
            // "bool ShouldSerializeXXX()" and "void ResetXXX()" are ok if there is a matching
            // property XXX as they are used by the windows designer property grid
            // Note that we do a case sensitive compare for compatibility with legacy FxCop
            // implementation of this rule.

            return methodSymbol.Parameters.IsEmpty &&
                (IsSpecialMethodWithMatchingProperty("ShouldSerialize") && methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean ||
                 IsSpecialMethodWithMatchingProperty("Reset") && methodSymbol.ReturnsVoid);

            // Local functions.
            bool IsSpecialMethodWithMatchingProperty(string prefix)
            {
                if (methodSymbol.Name.StartsWith(prefix))
                {
                    var suffix = methodSymbol.Name[prefix.Length..];
                    return suffix.Length > 0 &&
                        methodSymbol.ContainingType.GetMembers(suffix).Any(static m => m is IPropertySymbol);
                }

                return false;
            }
        }
    }
}
