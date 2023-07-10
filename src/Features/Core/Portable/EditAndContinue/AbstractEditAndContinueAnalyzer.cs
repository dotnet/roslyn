// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal abstract class AbstractEditAndContinueAnalyzer : IEditAndContinueAnalyzer
    {
        internal const int DefaultStatementPart = 0;
        private const string CreateNewOnMetadataUpdateAttributeName = "CreateNewOnMetadataUpdateAttribute";

        /// <summary>
        /// Contains enough information to determine whether two symbols have the same signature.
        /// </summary>
        private static readonly SymbolDisplayFormat s_unqualifiedMemberDisplayFormat =
            new(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static readonly SymbolDisplayFormat s_fullyQualifiedMemberDisplayFormat =
            new(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeContainingType,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        // used by tests to validate correct handlign of unexpected exceptions
        private readonly Action<SyntaxNode>? _testFaultInjector;

        protected AbstractEditAndContinueAnalyzer(Action<SyntaxNode>? testFaultInjector)
        {
            _testFaultInjector = testFaultInjector;
        }

        private static TraceLog Log
            => EditAndContinueService.AnalysisLog;

        internal abstract bool ExperimentalFeaturesEnabled(SyntaxTree tree);

        /// <summary>
        /// Finds member declaration node(s) containing given <paramref name="node"/>.
        /// Specified <paramref name="node"/> may be either a node of the declaration body or an active node that belongs to the declaration.
        /// </summary>
        /// <remarks>
        /// The implementation has to decide what kinds of nodes in top-level match relationship represent a declaration.
        /// Every member declaration must be represented by exactly one node, but not all nodes have to represent a declaration.
        /// 
        /// Note that in some cases the set of nodes of the declaration body may differ from the set of active nodes that 
        /// belong to the declaration. For example, in <c>Dim a, b As New T</c> the sets for member <c>a</c> are
        /// { <c>New</c>, <c>T</c> } and { <c>a</c> }, respectively.
        /// 
        /// May return multiple declarations if the specified <paramref name="node"/> belongs to multiple declarations,
        /// such as in VB <c>Dim a, b As New T</c> case when <paramref name="node"/> is e.g. <c>T</c>.
        /// </remarks>
        internal abstract bool TryFindMemberDeclaration(SyntaxNode? root, SyntaxNode node, out OneOrMany<SyntaxNode> declarations);

        /// <summary>
        /// If the specified node represents a member declaration returns an object that represents its body.
        /// </summary>
        /// <param name="node">A node representing a declaration or a top-level edit node.</param>
        /// 
        /// <returns>
        /// Null for nodes that don't represent declarations.
        /// </returns>
        internal abstract MemberBody? TryGetDeclarationBody(SyntaxNode node);

        /// <summary>
        /// True if the specified <paramref name="declaration"/> node shares body with another declaration.
        /// </summary>
        internal abstract bool IsDeclarationWithSharedBody(SyntaxNode declaration);

        /// <summary>
        /// Returns an envelope that contains all possible breakpoint spans of the body of the <paramref name="declaration"/>
        /// and no breakpoint spans that do not belong to the body.
        /// 
        /// Returns default if the declaration does not have any breakpoint spans.
        /// </summary>
        internal ActiveStatementEnvelope GetActiveSpanEnvelope(SyntaxNode declaration)
            => TryGetDeclarationBody(declaration)?.Envelope ?? default;

        /// <summary>
        /// Returns an ancestor that encompasses all active and statement level 
        /// nodes that belong to the member represented by <paramref name="bodyOrMatchRoot"/>.
        /// </summary>
        protected SyntaxNode? GetEncompassingAncestor(SyntaxNode? bodyOrMatchRoot)
        {
            if (bodyOrMatchRoot == null)
            {
                return null;
            }

            var root = GetEncompassingAncestorImpl(bodyOrMatchRoot);
            Debug.Assert(root.Span.Contains(bodyOrMatchRoot.Span));
            return root;
        }

        protected abstract SyntaxNode GetEncompassingAncestorImpl(SyntaxNode bodyOrMatchRoot);

        /// <summary>
        /// Returns a node that represents a body of a lambda containing specified <paramref name="node"/>,
        /// or null if the node isn't contained in a lambda. If a node is returned it must uniquely represent the lambda,
        /// i.e. be no two distinct nodes may represent the same lambda.
        /// </summary>
        protected abstract LambdaBody? FindEnclosingLambdaBody(SyntaxNode root, SyntaxNode node);

        protected abstract Match<SyntaxNode> ComputeTopLevelMatch(SyntaxNode oldCompilationUnit, SyntaxNode newCompilationUnit);
        protected abstract BidirectionalMap<SyntaxNode>? ComputeParameterMap(SyntaxNode oldDeclaration, SyntaxNode newDeclaration);
        protected abstract IEnumerable<SequenceEdit> GetSyntaxSequenceEdits(ImmutableArray<SyntaxNode> oldNodes, ImmutableArray<SyntaxNode> newNodes);

        protected abstract bool TryGetEnclosingBreakpointSpan(SyntaxNode root, int position, out TextSpan span);

        /// <summary>
        /// Get the active span that corresponds to specified node (or its part).
        /// </summary>
        /// <returns>
        /// True if the node has an active span associated with it, false otherwise.
        /// </returns>
        protected abstract bool TryGetActiveSpan(SyntaxNode node, int statementPart, int minLength, out TextSpan span);

        /// <summary>
        /// Yields potential active statements around the specified active statement
        /// starting with siblings following the statement, then preceding the statement, follows with its parent, its following siblings, etc.
        /// </summary>
        /// <returns>
        /// Pairs of (node, statement part), or (node, -1) indicating there is no logical following statement.
        /// The enumeration continues until the root is reached.
        /// </returns>
        protected abstract IEnumerable<(SyntaxNode statement, int statementPart)> EnumerateNearStatements(SyntaxNode statement);

        protected abstract bool StatementLabelEquals(SyntaxNode node1, SyntaxNode node2);

        /// <summary>
        /// Returns true if the code emitted for the old active statement part (<paramref name="statementPart"/> of <paramref name="oldStatement"/>) 
        /// is the same as the code emitted for the corresponding new active statement part (<paramref name="statementPart"/> of <paramref name="newStatement"/>). 
        /// </summary>
        /// <remarks>
        /// A rude edit is reported if an active statement is changed and this method returns true.
        /// </remarks>
        protected abstract bool AreEquivalentActiveStatements(SyntaxNode oldStatement, SyntaxNode newStatement, int statementPart);

        protected abstract bool IsNamespaceDeclaration(SyntaxNode node);
        protected abstract bool IsCompilationUnitWithGlobalStatements(SyntaxNode node);
        protected abstract bool IsGlobalStatement(SyntaxNode node);

        /// <summary>
        /// Returns all top-level type declarations (non-nested) for a given compilation unit node.
        /// </summary>
        protected abstract IEnumerable<SyntaxNode> GetTopLevelTypeDeclarations(SyntaxNode compilationUnit);

        /// <summary>
        /// Returns all symbols associated with an edit and an actual edit kind, which may be different then the specified one.
        /// Returns an empty set if the edit is not associated with any symbols.
        /// </summary>
        protected abstract OneOrMany<(ISymbol? oldSymbol, ISymbol? newSymbol, EditKind editKind)> GetSymbolEdits(
            EditKind editKind,
            SyntaxNode? oldNode,
            SyntaxNode? newNode,
            SemanticModel? oldModel,
            SemanticModel newModel,
            IReadOnlyDictionary<SyntaxNode, EditKind> editMap,
            CancellationToken cancellationToken);

        /// <summary>
        /// Enumerates all use sites of a specified variable within the specified syntax subtrees.
        /// </summary>
        protected abstract IEnumerable<SyntaxNode> GetVariableUseSites(IEnumerable<SyntaxNode> roots, ISymbol localOrParameter, SemanticModel model, CancellationToken cancellationToken);

        protected abstract bool AreHandledEventsEqual(IMethodSymbol oldMethod, IMethodSymbol newMethod);

        // diagnostic spans:
        protected abstract TextSpan? TryGetDiagnosticSpan(SyntaxNode node, EditKind editKind);

        internal TextSpan GetDiagnosticSpan(SyntaxNode node, EditKind editKind)
          => TryGetDiagnosticSpan(node, editKind) ?? node.Span;

        protected virtual TextSpan GetBodyDiagnosticSpan(SyntaxNode node, EditKind editKind)
        {
            var current = node.Parent;
            while (true)
            {
                if (current == null)
                {
                    return node.Span;
                }

                var span = TryGetDiagnosticSpan(current, editKind);
                if (span != null)
                {
                    return span.Value;
                }

                current = current.Parent;
            }
        }

        internal abstract TextSpan GetLambdaParameterDiagnosticSpan(SyntaxNode lambda, int ordinal);

        // display names:

        internal string GetDisplayKindAndName(ISymbol symbol, string? displayName = null, bool fullyQualify = false)
        {
            displayName ??= GetDisplayName(symbol);
            var format = fullyQualify ? s_fullyQualifiedMemberDisplayFormat : s_unqualifiedMemberDisplayFormat;

            return (symbol is IParameterSymbol { ContainingType: not { TypeKind: TypeKind.Delegate } })
                ? string.Format(
                    FeaturesResources.symbol_kind_and_name_of_member_kind_and_name,
                    displayName,
                    symbol.Name,
                    GetDisplayName(symbol.ContainingSymbol),
                    symbol.ContainingSymbol.ToDisplayString(format))
                : string.Format(
                    FeaturesResources.member_kind_and_name,
                    displayName,
                    symbol.ToDisplayString(format));
        }

        internal string GetDisplayName(SyntaxNode node, EditKind editKind = EditKind.Update)
          => TryGetDisplayName(node, editKind) ?? throw ExceptionUtilities.UnexpectedValue(node.GetType().Name);

        internal string GetDisplayName(ISymbol symbol)
            => symbol.Kind switch
            {
                SymbolKind.Event => FeaturesResources.event_,
                SymbolKind.Field => GetDisplayName((IFieldSymbol)symbol),
                SymbolKind.Method => GetDisplayName((IMethodSymbol)symbol),
                SymbolKind.NamedType => GetDisplayName((INamedTypeSymbol)symbol),
                SymbolKind.Parameter => FeaturesResources.parameter,
                SymbolKind.Property => GetDisplayName((IPropertySymbol)symbol),
                SymbolKind.TypeParameter => FeaturesResources.type_parameter,
                _ => throw ExceptionUtilities.UnexpectedValue(symbol.Kind)
            };

        internal virtual string GetDisplayName(IPropertySymbol symbol)
            => FeaturesResources.property_;

        internal virtual string GetDisplayName(INamedTypeSymbol symbol)
            => symbol.TypeKind switch
            {
                TypeKind.Class => FeaturesResources.class_,
                TypeKind.Interface => FeaturesResources.interface_,
                TypeKind.Delegate => FeaturesResources.delegate_,
                TypeKind.Enum => FeaturesResources.enum_,
                TypeKind.TypeParameter => FeaturesResources.type_parameter,
                _ => FeaturesResources.type,
            };

        internal virtual string GetDisplayName(IFieldSymbol symbol)
            => symbol.IsConst ? ((symbol.ContainingType.TypeKind == TypeKind.Enum) ? FeaturesResources.enum_value : FeaturesResources.const_field) :
               FeaturesResources.field;

        internal virtual string GetDisplayName(IMethodSymbol symbol)
            => symbol.MethodKind switch
            {
                MethodKind.Constructor => FeaturesResources.constructor,
                MethodKind.PropertyGet or MethodKind.PropertySet => FeaturesResources.property_accessor,
                MethodKind.EventAdd or MethodKind.EventRaise or MethodKind.EventRemove => FeaturesResources.event_accessor,
                MethodKind.BuiltinOperator or MethodKind.UserDefinedOperator or MethodKind.Conversion => FeaturesResources.operator_,
                _ => FeaturesResources.method,
            };

        /// <summary>
        /// Returns the display name of an ancestor node that contains the specified node and has a display name.
        /// </summary>
        protected virtual string GetBodyDisplayName(SyntaxNode node, EditKind editKind = EditKind.Update)
        {
            var current = node.Parent;

            if (current == null)
            {
                var displayName = TryGetDisplayName(node, editKind);
                if (displayName != null)
                {
                    return displayName;
                }
            }

            while (true)
            {
                if (current == null)
                {
                    throw ExceptionUtilities.UnexpectedValue(node.GetType().Name);
                }

                var displayName = TryGetDisplayName(current, editKind);
                if (displayName != null)
                {
                    return displayName;
                }

                current = current.Parent;
            }
        }

        protected abstract string? TryGetDisplayName(SyntaxNode node, EditKind editKind);

        protected virtual string GetSuspensionPointDisplayName(SyntaxNode node, EditKind editKind)
            => GetDisplayName(node, editKind);

        protected abstract string LineDirectiveKeyword { get; }
        protected abstract ushort LineDirectiveSyntaxKind { get; }
        protected abstract SymbolDisplayFormat ErrorDisplayFormat { get; }
        protected abstract List<SyntaxNode> GetExceptionHandlingAncestors(SyntaxNode node, bool isNonLeaf);
        protected abstract TextSpan GetExceptionHandlingRegion(SyntaxNode node, out bool coversAllChildren);

        internal abstract void ReportTopLevelSyntacticRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, Edit<SyntaxNode> edit, Dictionary<SyntaxNode, EditKind> editMap);
        internal abstract void ReportEnclosingExceptionHandlingRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, IEnumerable<Edit<SyntaxNode>> exceptionHandlingEdits, SyntaxNode oldStatement, TextSpan newStatementSpan);
        internal abstract void ReportOtherRudeEditsAroundActiveStatement(ArrayBuilder<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, SyntaxNode oldStatement, SyntaxNode newStatement, bool isNonLeaf);
        internal abstract void ReportMemberOrLambdaBodyUpdateRudeEditsImpl(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode newDeclaration, DeclarationBody newBody, TextSpan? span);
        internal abstract void ReportInsertedMemberSymbolRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, ISymbol newSymbol, SyntaxNode newNode, bool insertingIntoExistingContainingType);
        internal abstract void ReportStateMachineSuspensionPointRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, SyntaxNode newNode);

        internal abstract Func<SyntaxNode, bool> IsLambda { get; }
        internal abstract bool IsInterfaceDeclaration(SyntaxNode node);
        internal abstract bool IsRecordDeclaration(SyntaxNode node);

        /// <summary>
        /// True if the node represents any form of a function definition nested in another function body (i.e. anonymous function, lambda, local function).
        /// </summary>
        internal abstract bool IsNestedFunction(SyntaxNode node);

        internal abstract bool IsLocalFunction(SyntaxNode node);
        internal abstract bool IsGenericLocalFunction(SyntaxNode node);
        internal abstract bool IsClosureScope(SyntaxNode node);
        internal abstract IMethodSymbol GetLambdaExpressionSymbol(SemanticModel model, SyntaxNode lambdaExpression, CancellationToken cancellationToken);
        internal abstract SyntaxNode? GetContainingQueryExpression(SyntaxNode node);
        internal abstract bool QueryClauseLambdasTypeEquivalent(SemanticModel oldModel, SyntaxNode oldNode, SemanticModel newModel, SyntaxNode newNode, CancellationToken cancellationToken);

        internal bool ContainsLambda(MemberBody body)
        {
            var isLambda = IsLambda;
            return body.RootNodes.Any(static (root, isLambda) => root.DescendantNodesAndSelf().Any(isLambda), isLambda);
        }

        /// <summary>
        /// Returns true if the parameters of the symbol are lifted into a scope that is different from the symbol's body.
        /// </summary>
        internal abstract bool HasParameterClosureScope(ISymbol member);

        /// <summary>
        /// Returns all lambda bodies of a node representing a lambda, 
        /// or false if the node doesn't represent a lambda.
        /// </summary>
        /// <remarks>
        /// C# anonymous function expression and VB lambda expression both have a single body
        /// (in VB the body is the header of the lambda expression).
        /// 
        /// Some lambda queries (group by, join by) have two bodies.
        /// </remarks>
        internal abstract bool TryGetLambdaBodies(SyntaxNode node, [NotNullWhen(true)] out LambdaBody? body1, out LambdaBody? body2);

        internal abstract bool IsStateMachineMethod(SyntaxNode declaration);

        /// <summary>
        /// Returns the type declaration that contains a specified <paramref name="node"/>.
        /// This can be class, struct, interface, record or enum declaration.
        /// </summary>
        internal abstract SyntaxNode? TryGetContainingTypeDeclaration(SyntaxNode node);

        /// <summary>
        /// Returns the declaration of 
        /// - a property, indexer or event declaration whose accessor is the specified <paramref name="node"/>,
        /// - a method, an indexer, a type (delegate), or primary constructor parameter list if the <paramref name="node"/> is a parameter,
        /// - a method or an type if the <paramref name="node"/> is a type parameter.
        /// </summary>
        internal abstract bool TryGetAssociatedMemberDeclaration(SyntaxNode node, EditKind editKind, [NotNullWhen(true)] out SyntaxNode? declaration);

        internal abstract bool HasBackingField(SyntaxNode propertyDeclaration);

        /// <summary>
        /// Return true if the declaration is a field/property declaration with an initializer. 
        /// Shall return false for enum members.
        /// </summary>
        internal abstract bool IsDeclarationWithInitializer(SyntaxNode declaration);

        /// <summary>
        /// True if <paramref name="declaration"/> is a declaration node of a primary constructor (i.e. parameter list of a type declaration).
        /// </summary>
        /// <remarks>
        /// <see cref="ISymbol.DeclaringSyntaxReferences"/> of a primary constructor returns the type declaration syntax.
        /// This is inconvenient for EnC analysis since it doesn't allow us to distinguish declaration of the type from the constructor.
        /// E.g. delete/insert of a primary constructor is not the same as delete/insert of the entire type declaration.
        /// </remarks>
        internal abstract bool IsPrimaryConstructorDeclaration(SyntaxNode declaration);

        /// <summary>
        /// Return true if <paramref name="symbol"/> is a constructor to which field/property initializers are emitted. 
        /// </summary>
        internal abstract bool IsConstructorWithMemberInitializers(ISymbol symbol, CancellationToken cancellationToken);

        internal abstract bool IsPartial(INamedTypeSymbol type);

        internal abstract SyntaxNode EmptyCompilationUnit { get; }

        private static readonly SourceText s_emptySource = SourceText.From("");

        #region Document Analysis 

        public async Task<DocumentAnalysisResults> AnalyzeDocumentAsync(
            Project oldProject,
            AsyncLazy<ActiveStatementsMap> lazyOldActiveStatementMap,
            Document newDocument,
            ImmutableArray<LinePositionSpan> newActiveStatementSpans,
            AsyncLazy<EditAndContinueCapabilities> lazyCapabilities,
            CancellationToken cancellationToken)
        {
            var filePath = newDocument.FilePath;

            Debug.Assert(newDocument.State.SupportsEditAndContinue());
            Debug.Assert(!newActiveStatementSpans.IsDefault);
            Debug.Assert(newDocument.SupportsSyntaxTree);
            Debug.Assert(newDocument.SupportsSemanticModel);
            Debug.Assert(filePath != null);

            // assume changes until we determine there are none so that EnC is blocked on unexpected exception:
            var hasChanges = true;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                SyntaxTree? oldTree;
                SyntaxNode oldRoot;
                SourceText oldText;

                var oldDocument = await oldProject.GetDocumentAsync(newDocument.Id, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                if (oldDocument != null)
                {
                    oldTree = await oldDocument.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    oldRoot = await oldTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    oldText = await oldDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    oldTree = null;
                    oldRoot = EmptyCompilationUnit;
                    oldText = s_emptySource;
                }

                var newTree = await newDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(newTree);

                // Changes in parse options might change the meaning of the code even if nothing else changed.
                // The IDE should disallow changing the options during debugging session. 
                Debug.Assert(oldTree == null || oldTree.Options.Equals(newTree.Options));

                var newRoot = await newTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var newText = await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                hasChanges = !oldText.ContentEquals(newText);

                _testFaultInjector?.Invoke(newRoot);
                cancellationToken.ThrowIfCancellationRequested();

                // TODO: newTree.HasErrors?
                var syntaxDiagnostics = newRoot.GetDiagnostics();
                var syntaxError = syntaxDiagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
                if (syntaxError != null)
                {
                    // Bail, since we can't do syntax diffing on broken trees (it would not produce useful results anyways).
                    // If we needed to do so for some reason, we'd need to harden the syntax tree comparers.
                    Log.Write("Syntax errors found in '{0}'", filePath);
                    return DocumentAnalysisResults.SyntaxErrors(newDocument.Id, filePath, ImmutableArray<RudeEditDiagnostic>.Empty, syntaxError, hasChanges);
                }

                if (!hasChanges)
                {
                    // The document might have been closed and reopened, which might have triggered analysis. 
                    // If the document is unchanged don't continue the analysis since 
                    // a) comparing texts is cheaper than diffing trees
                    // b) we need to ignore errors in unchanged documents

                    Log.Write("Document unchanged: '{0}'", filePath);
                    return DocumentAnalysisResults.Unchanged(newDocument.Id, filePath);
                }

                // Disallow modification of a file with experimental features enabled.
                // These features may not be handled well by the analysis below.
                if (ExperimentalFeaturesEnabled(newTree))
                {
                    Log.Write("Experimental features enabled in '{0}'", filePath);

                    return DocumentAnalysisResults.SyntaxErrors(newDocument.Id, filePath, ImmutableArray.Create(
                        new RudeEditDiagnostic(RudeEditKind.ExperimentalFeaturesEnabled, default)), syntaxError: null, hasChanges);
                }

                var capabilities = new EditAndContinueCapabilitiesGrantor(await lazyCapabilities.GetValueAsync(cancellationToken).ConfigureAwait(false));
                var oldActiveStatementMap = await lazyOldActiveStatementMap.GetValueAsync(cancellationToken).ConfigureAwait(false);

                // If the document has changed at all, lets make sure Edit and Continue is supported
                if (!capabilities.Grant(EditAndContinueCapabilities.Baseline))
                {
                    return DocumentAnalysisResults.SyntaxErrors(newDocument.Id, filePath, ImmutableArray.Create(
                       new RudeEditDiagnostic(RudeEditKind.NotSupportedByRuntime, default)), syntaxError: null, hasChanges);
                }

                // We are in break state when there are no active statements.
                var inBreakState = !oldActiveStatementMap.IsEmpty;

                // We do calculate diffs even if there are semantic errors for the following reasons: 
                // 1) We need to be able to find active spans in the new document. 
                //    If we didn't calculate them we would only rely on tracking spans (might be ok).
                // 2) If there are syntactic rude edits we'll report them faster without waiting for semantic analysis.
                //    The user may fix them before they address all the semantic errors.

                using var _2 = ArrayBuilder<RudeEditDiagnostic>.GetInstance(out var diagnostics);

                cancellationToken.ThrowIfCancellationRequested();

                var topMatch = ComputeTopLevelMatch(oldRoot, newRoot);
                var syntacticEdits = topMatch.GetTreeEdits();
                var editMap = BuildEditMap(syntacticEdits);

                ReportTopLevelSyntacticRudeEdits(diagnostics, syntacticEdits, editMap);

                cancellationToken.ThrowIfCancellationRequested();

                using var _3 = ArrayBuilder<(SyntaxNode OldNode, SyntaxNode NewNode, TextSpan DiagnosticSpan)>.GetInstance(out var triviaEdits);
                using var _4 = ArrayBuilder<SequencePointUpdates>.GetInstance(out var lineEdits);

                AnalyzeTrivia(
                    topMatch,
                    editMap,
                    triviaEdits,
                    lineEdits,
                    cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                var oldActiveStatements = (oldTree == null) ? ImmutableArray<UnmappedActiveStatement>.Empty :
                    oldActiveStatementMap.GetOldActiveStatements(this, oldTree, oldText, oldRoot, cancellationToken);

                var newActiveStatements = ImmutableArray.CreateBuilder<ActiveStatement>(oldActiveStatements.Length);
                newActiveStatements.Count = oldActiveStatements.Length;

                var newExceptionRegions = ImmutableArray.CreateBuilder<ImmutableArray<SourceFileSpan>>(oldActiveStatements.Length);
                newExceptionRegions.Count = oldActiveStatements.Length;

                var semanticEdits = await AnalyzeSemanticsAsync(
                    syntacticEdits,
                    editMap,
                    oldActiveStatements,
                    newActiveStatementSpans,
                    triviaEdits,
                    oldProject,
                    oldDocument,
                    newDocument,
                    newText,
                    diagnostics,
                    newActiveStatements,
                    newExceptionRegions,
                    capabilities,
                    inBreakState,
                    cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                AnalyzeUnchangedActiveMemberBodies(diagnostics, syntacticEdits.Match, newText, oldActiveStatements, newActiveStatementSpans, newActiveStatements, newExceptionRegions, cancellationToken);
                Debug.Assert(newActiveStatements.All(a => a != null));

                var hasRudeEdits = diagnostics.Count > 0;
                if (hasRudeEdits)
                {
                    LogRudeEdits(diagnostics, newText, filePath);
                }
                else
                {
                    Log.Write("Capabilities required by '{0}': {1}", filePath, capabilities.GrantedCapabilities);
                }

                return new DocumentAnalysisResults(
                    newDocument.Id,
                    filePath,
                    newActiveStatements.MoveToImmutable(),
                    diagnostics.ToImmutable(),
                    syntaxError: null,
                    hasRudeEdits ? default : semanticEdits,
                    hasRudeEdits ? default : newExceptionRegions.MoveToImmutable(),
                    hasRudeEdits ? default : lineEdits.ToImmutable(),
                    hasRudeEdits ? default : capabilities.GrantedCapabilities,
                    hasChanges: true,
                    hasSyntaxErrors: false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                // The same behavior as if there was a syntax error - we are unable to analyze the document. 
                // We expect OOM to be thrown during the analysis if the number of top-level entities is too large.
                // In such case we report a rude edit for the document. If the host is actually running out of memory,
                // it might throw another OOM here or later on.
                var diagnostic = (e is OutOfMemoryException)
                    ? new RudeEditDiagnostic(RudeEditKind.SourceFileTooBig, span: default, arguments: new[] { newDocument.FilePath })
                    : new RudeEditDiagnostic(RudeEditKind.InternalError, span: default, arguments: new[] { newDocument.FilePath, e.ToString() });

                // Report as "syntax error" - we can't analyze the document
                return DocumentAnalysisResults.SyntaxErrors(newDocument.Id, filePath, ImmutableArray.Create(diagnostic), syntaxError: null, hasChanges);
            }

            static void LogRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, SourceText text, string filePath)
            {
                foreach (var diagnostic in diagnostics)
                {
                    int lineNumber;
                    string? lineText;
                    try
                    {
                        var line = text.Lines.GetLineFromPosition(diagnostic.Span.Start);
                        lineNumber = line.LineNumber;
                        lineText = text.ToString(TextSpan.FromBounds(diagnostic.Span.Start, Math.Min(diagnostic.Span.Start + 120, line.End)));
                    }
                    catch
                    {
                        lineNumber = -1;
                        lineText = null;
                    }

                    Log.Write("Rude edit {0}:{1} '{2}' line {3}: '{4}'", diagnostic.Kind, diagnostic.SyntaxKind, filePath, lineNumber, lineText);
                }
            }
        }

        private void ReportTopLevelSyntacticRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, EditScript<SyntaxNode> syntacticEdits, Dictionary<SyntaxNode, EditKind> editMap)
        {
            foreach (var edit in syntacticEdits.Edits)
            {
                ReportTopLevelSyntacticRudeEdits(diagnostics, syntacticEdits.Match, edit, editMap);
            }
        }

        internal Dictionary<SyntaxNode, EditKind> BuildEditMap(EditScript<SyntaxNode> editScript)
        {
            var map = new Dictionary<SyntaxNode, EditKind>(editScript.Edits.Length);

            foreach (var edit in editScript.Edits)
            {
                // do not include reorder and move edits

                if (edit.Kind is EditKind.Delete or EditKind.Update)
                {
                    map.Add(edit.OldNode, edit.Kind);
                }

                if (edit.Kind is EditKind.Insert or EditKind.Update)
                {
                    map.Add(edit.NewNode, edit.Kind);
                }
            }

            // When a global statement is updated, inserted or deleted it means that the containing
            // compilation unit has been updated. This update is not recorded in the edit script
            // since compilation unit contains other items then global statements as well and 
            // we only want it to be updated in presence of changed global statements.
            if ((IsCompilationUnitWithGlobalStatements(editScript.Match.OldRoot) || IsCompilationUnitWithGlobalStatements(editScript.Match.NewRoot)) &&
                map.Any(entry => IsGlobalStatement(entry.Key)))
            {
                map.Add(editScript.Match.OldRoot, EditKind.Update);
                map.Add(editScript.Match.NewRoot, EditKind.Update);
            }

            return map;
        }

        #endregion

        #region Syntax Analysis

        private void AnalyzeUnchangedActiveMemberBodies(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            Match<SyntaxNode> topMatch,
            SourceText newText,
            ImmutableArray<UnmappedActiveStatement> oldActiveStatements,
            ImmutableArray<LinePositionSpan> newActiveStatementSpans,
            [In, Out] ImmutableArray<ActiveStatement>.Builder newActiveStatements,
            [In, Out] ImmutableArray<ImmutableArray<SourceFileSpan>>.Builder newExceptionRegions,
            CancellationToken cancellationToken)
        {
            Debug.Assert(!newActiveStatementSpans.IsDefault);
            Debug.Assert(newActiveStatementSpans.IsEmpty || oldActiveStatements.Length == newActiveStatementSpans.Length);
            Debug.Assert(oldActiveStatements.Length == newActiveStatements.Count);
            Debug.Assert(oldActiveStatements.Length == newExceptionRegions.Count);

            // Active statements in methods that were not updated 
            // are not changed but their spans might have been. 

            for (var i = 0; i < newActiveStatements.Count; i++)
            {
                if (newActiveStatements[i] == null)
                {
                    Contract.ThrowIfFalse(newExceptionRegions[i].IsDefault);

                    var oldStatementSpan = oldActiveStatements[i].UnmappedSpan;

                    var node = TryGetNode(topMatch.OldRoot, oldStatementSpan.Start);

                    // Guard against invalid active statement spans (in case PDB was somehow out of sync with the source).
                    if (node != null && TryFindMemberDeclaration(topMatch.OldRoot, node, out var oldMemberDeclarations))
                    {
                        foreach (var oldMember in oldMemberDeclarations)
                        {
                            var hasPartner = topMatch.TryGetNewNode(oldMember, out var newMember);
                            Contract.ThrowIfFalse(hasPartner);

                            var oldBody = TryGetDeclarationBody(oldMember);
                            var newBody = TryGetDeclarationBody(newMember);

                            // Guard against invalid active statement spans (in case PDB was somehow out of sync with the source).
                            if (oldBody == null || newBody == null)
                            {
                                Log.Write("Invalid active statement span: [{0}..{1})", oldStatementSpan.Start, oldStatementSpan.End);
                                continue;
                            }

                            var statementPart = -1;
                            SyntaxNode? newStatement = null;

                            // We seed the method body matching algorithm with tracking spans (unless they were deleted)
                            // to get precise matching.
                            if (TryGetTrackedStatement(newActiveStatementSpans, i, newText, newMember, newBody, out var trackedStatement, out var trackedStatementPart))
                            {
                                // Adjust for active statements that cover more than the old member span.
                                // For example, C# variable declarators that represent field initializers:
                                //   [|public int <<F = Expr()>>;|]
                                var adjustedOldStatementStart = oldMember.FullSpan.Contains(oldStatementSpan.Start) ? oldStatementSpan.Start : oldMember.SpanStart;

                                // The tracking span might have been moved outside of lambda.
                                // It is not an error to move the statement - we just ignore it.
                                var oldEnclosingLambdaBody = FindEnclosingLambdaBody(oldBody.EncompassingAncestor, oldMember.FindToken(adjustedOldStatementStart).Parent!);
                                var newEnclosingLambdaBody = FindEnclosingLambdaBody(newBody.EncompassingAncestor, trackedStatement);
                                if (oldEnclosingLambdaBody == newEnclosingLambdaBody)
                                {
                                    newStatement = trackedStatement;
                                    statementPart = trackedStatementPart;
                                }
                            }

                            if (newStatement == null)
                            {
                                Contract.ThrowIfFalse(statementPart == -1);
                                oldBody.FindStatementAndPartner(oldStatementSpan, newBody, out newStatement, out statementPart);
                                Contract.ThrowIfNull(newStatement);
                            }

                            if (diagnostics.Count == 0)
                            {
                                var ancestors = GetExceptionHandlingAncestors(newStatement, oldActiveStatements[i].Statement.IsNonLeaf);
                                newExceptionRegions[i] = GetExceptionRegions(ancestors, newStatement.SyntaxTree, cancellationToken).Spans;
                            }

                            // Even though the body of the declaration haven't changed, 
                            // changes to its header might have caused the active span to become unavailable.
                            // (e.g. In C# "const" was added to modifiers of a field with an initializer).
                            var newStatementSpan = FindClosestActiveSpan(newStatement, statementPart);

                            newActiveStatements[i] = GetActiveStatementWithSpan(oldActiveStatements[i], newBody.SyntaxTree, newStatementSpan, diagnostics, cancellationToken);
                        }
                    }
                    else
                    {
                        Log.Write("Invalid active statement span: [{0}..{1})", oldStatementSpan.Start, oldStatementSpan.End);
                    }

                    // we were not able to determine the active statement location (PDB data might be invalid)
                    if (newActiveStatements[i] == null)
                    {
                        newActiveStatements[i] = oldActiveStatements[i].Statement.WithSpan(default);
                        newExceptionRegions[i] = ImmutableArray<SourceFileSpan>.Empty;
                    }
                }
            }
        }

        internal readonly struct ActiveNode(int activeStatementIndex, SyntaxNode oldNode, LambdaBody? enclosingLambdaBody, int statementPart, SyntaxNode? newTrackedNode)
        {
            public readonly int ActiveStatementIndex = activeStatementIndex;
            public readonly SyntaxNode OldNode = oldNode;
            public readonly SyntaxNode? NewTrackedNode = newTrackedNode;
            public readonly LambdaBody? EnclosingLambdaBody = enclosingLambdaBody;
            public readonly int StatementPart = statementPart;
        }

        /// <summary>
        /// Information about an active and/or a matched lambda.
        /// </summary>
        internal readonly struct LambdaInfo
        {
            // non-null for an active lambda (lambda containing an active statement)
            public readonly List<int>? ActiveNodeIndices;

            // both fields are non-null for a matching lambda (lambda that exists in both old and new document):
            public readonly Match<SyntaxNode>? Match;
            public readonly LambdaBody? NewBody;

            public LambdaInfo(List<int> activeNodeIndices)
                : this(activeNodeIndices, null, null)
            {
            }

            private LambdaInfo(List<int>? activeNodeIndices, Match<SyntaxNode>? match, LambdaBody? newLambdaBody)
            {
                ActiveNodeIndices = activeNodeIndices;
                Match = match;
                NewBody = newLambdaBody;
            }

            public bool HasActiveStatement
                => ActiveNodeIndices != null;

            public LambdaInfo WithMatch(Match<SyntaxNode> match, LambdaBody newLambdaBody)
                => new(ActiveNodeIndices, match, newLambdaBody);
        }

        private void AnalyzeChangedMemberBody(
            SyntaxNode oldDeclaration,
            SyntaxNode newDeclaration,
            MemberBody oldBody,
            MemberBody? newBody,
            SemanticModel oldModel,
            SemanticModel newModel,
            ISymbol oldMember,
            ISymbol newMember,
            SourceText newText,
            ImmutableArray<UnmappedActiveStatement> oldActiveStatements,
            ImmutableArray<LinePositionSpan> newActiveStatementSpans,
            EditAndContinueCapabilitiesGrantor capabilities,
            [Out] ImmutableArray<ActiveStatement>.Builder newActiveStatements,
            [Out] ImmutableArray<ImmutableArray<SourceFileSpan>>.Builder newExceptionRegions,
            [Out] ArrayBuilder<RudeEditDiagnostic> diagnostics,
            out Func<SyntaxNode, SyntaxNode?>? syntaxMap,
            CancellationToken cancellationToken)
        {
            Debug.Assert(!newActiveStatementSpans.IsDefault);
            Debug.Assert(newActiveStatementSpans.IsEmpty || oldActiveStatements.Length == newActiveStatementSpans.Length);
            Debug.Assert(oldActiveStatements.IsEmpty || oldActiveStatements.Length == newActiveStatements.Count);
            Debug.Assert(newActiveStatements.Count == newExceptionRegions.Count);

            syntaxMap = null;

            var activeStatementIndices = GetOverlappingActiveStatements(oldBody, oldActiveStatements);

            if (newBody == null)
            {
                // The body has been deleted.
                var newSpan = FindClosestActiveSpan(newDeclaration, DefaultStatementPart);
                Debug.Assert(newSpan != default);

                foreach (var activeStatementIndex in activeStatementIndices)
                {
                    // We have already calculated the new location of this active statement when analyzing another member declaration.
                    // This may only happen when two or more member declarations share the same body (VB AsNew clause).
                    if (newActiveStatements[activeStatementIndex] != null)
                    {
                        Debug.Assert(IsDeclarationWithSharedBody(newDeclaration));
                        continue;
                    }

                    newActiveStatements[activeStatementIndex] = GetActiveStatementWithSpan(oldActiveStatements[activeStatementIndex], newDeclaration.SyntaxTree, newSpan, diagnostics, cancellationToken);
                    newExceptionRegions[activeStatementIndex] = ImmutableArray<SourceFileSpan>.Empty;
                }

                return;
            }

            try
            {
                _testFaultInjector?.Invoke(newBody.RootNodes.First());

                // Populated with active lambdas and matched lambdas. 
                // Unmatched non-active lambdas are not included.
                // { old-lambda-body -> info }
                Dictionary<LambdaBody, LambdaInfo>? lazyActiveOrMatchedLambdas = null;

                // finds leaf nodes that correspond to the old active statements:
                using var _ = ArrayBuilder<ActiveNode>.GetInstance(out var activeNodes);
                foreach (var activeStatementIndex in activeStatementIndices)
                {
                    var oldStatementSpan = oldActiveStatements[activeStatementIndex].UnmappedSpan;

                    var oldStatementSyntax = oldBody.FindStatement(oldStatementSpan, out var statementPart);
                    Contract.ThrowIfNull(oldStatementSyntax);

                    var oldEnclosingLambdaBody = FindEnclosingLambdaBody(oldBody.EncompassingAncestor, oldStatementSyntax);
                    if (oldEnclosingLambdaBody != null)
                    {
                        lazyActiveOrMatchedLambdas ??= new Dictionary<LambdaBody, LambdaInfo>();

                        if (!lazyActiveOrMatchedLambdas.TryGetValue(oldEnclosingLambdaBody, out var lambda))
                        {
                            lambda = new LambdaInfo(new List<int>());
                            lazyActiveOrMatchedLambdas.Add(oldEnclosingLambdaBody, lambda);
                        }

                        lambda.ActiveNodeIndices!.Add(activeNodes.Count);
                    }

                    SyntaxNode? trackedNode = null;

                    if (TryGetTrackedStatement(newActiveStatementSpans, activeStatementIndex, newText, newDeclaration, newBody, out var newStatementSyntax, out var _))
                    {
                        var newEnclosingLambdaBody = FindEnclosingLambdaBody(newBody.EncompassingAncestor, newStatementSyntax);

                        // The tracking span might have been moved outside of the lambda span.
                        // It is not an error to move the statement - we just ignore it.
                        if (oldEnclosingLambdaBody == newEnclosingLambdaBody &&
                            StatementLabelEquals(oldStatementSyntax, newStatementSyntax))
                        {
                            trackedNode = newStatementSyntax;
                        }
                    }

                    activeNodes.Add(new ActiveNode(activeStatementIndex, oldStatementSyntax, oldEnclosingLambdaBody, statementPart, trackedNode));
                }

                var activeNodesInBody = activeNodes.Where(n => n.EnclosingLambdaBody == null).ToArray();

                var bodyMatch = ComputeMatch(oldBody, newBody, activeNodesInBody);
                var map = ComputeMap(bodyMatch, activeNodes, ref lazyActiveOrMatchedLambdas);

                var oldStateMachineInfo = oldBody.GetStateMachineInfo();
                var newStateMachineInfo = newBody.GetStateMachineInfo();
                ReportStateMachineBodyUpdateRudeEdits(bodyMatch, oldStateMachineInfo, newDeclaration, newStateMachineInfo, hasActiveStatement: activeNodesInBody.Length != 0, diagnostics);

                ReportMemberOrLambdaBodyUpdateRudeEdits(
                    diagnostics,
                    oldModel,
                    oldDeclaration,
                    oldMember,
                    newDeclaration,
                    newBody,
                    newMember,
                    bodyMatch,
                    capabilities,
                    oldStateMachineInfo,
                    newStateMachineInfo);

                ReportLambdaAndClosureRudeEdits(
                    oldModel,
                    oldMember,
                    oldBody,
                    oldDeclaration,
                    newModel,
                    newMember,
                    newBody,
                    newDeclaration,
                    bodyMatch,
                    lazyActiveOrMatchedLambdas,
                    map,
                    capabilities,
                    diagnostics,
                    out var newBodyHasLambdas,
                    cancellationToken);

                // We need to provide syntax map to the compiler if 
                // 1) The new member has a active statement
                //    The values of local variables declared or synthesized in the method have to be preserved.
                // 2) The new member generates a state machine 
                //    In case the state machine is suspended we need to preserve variables.
                // 3) The new member contains lambdas
                //    We need to map new lambdas in the method to the matching old ones. 
                //    If the old method has lambdas but the new one doesn't there is nothing to preserve.
                // 4) Constructor that emits initializers is updated.
                //    We create syntax map even if it's not necessary: if any data member initializers are active/contain lambdas.
                //    Since initializers are usually simple the map should not be large enough to make it worth optimizing it away.
                if (!activeNodes.IsEmpty() ||
                    newStateMachineInfo.HasSuspensionPoints ||
                    newBodyHasLambdas ||
                    IsConstructorWithMemberInitializers(newMember, cancellationToken) ||
                    IsDeclarationWithInitializer(oldDeclaration) ||
                    IsDeclarationWithInitializer(newDeclaration))
                {
                    syntaxMap = CreateSyntaxMap(map.Reverse);
                }

                foreach (var activeNode in activeNodes)
                {
                    var activeStatementIndex = activeNode.ActiveStatementIndex;
                    var hasMatching = false;
                    var isNonLeaf = oldActiveStatements[activeStatementIndex].Statement.IsNonLeaf;
                    var isPartiallyExecuted = (oldActiveStatements[activeStatementIndex].Statement.Flags & ActiveStatementFlags.PartiallyExecuted) != 0;
                    var statementPart = activeNode.StatementPart;
                    var oldStatementSyntax = activeNode.OldNode;
                    var oldEnclosingLambdaBody = activeNode.EnclosingLambdaBody;

                    newExceptionRegions[activeStatementIndex] = ImmutableArray<SourceFileSpan>.Empty;

                    TextSpan newSpan;
                    SyntaxNode? newStatementSyntax;
                    Match<SyntaxNode>? match;

                    if (oldEnclosingLambdaBody == null)
                    {
                        match = bodyMatch;

                        hasMatching = oldBody.TryMatchActiveStatement(newBody, oldStatementSyntax, statementPart, out newStatementSyntax) ||
                                      match.TryGetNewNode(oldStatementSyntax, out newStatementSyntax);
                    }
                    else
                    {
                        RoslynDebug.Assert(lazyActiveOrMatchedLambdas != null);

                        var oldLambdaInfo = lazyActiveOrMatchedLambdas[oldEnclosingLambdaBody];
                        var newEnclosingLambdaBody = oldLambdaInfo.NewBody;
                        match = oldLambdaInfo.Match;

                        if (match != null)
                        {
                            RoslynDebug.Assert(newEnclosingLambdaBody != null); // matching lambda has body

                            hasMatching = oldEnclosingLambdaBody.TryMatchActiveStatement(newEnclosingLambdaBody, oldStatementSyntax, statementPart, out newStatementSyntax) ||
                                          match.TryGetNewNode(oldStatementSyntax, out newStatementSyntax);
                        }
                        else
                        {
                            // Lambda match is null if lambdas can't be matched, 
                            // in such case we won't have active statement matched either.
                            hasMatching = false;
                            newStatementSyntax = null;
                        }
                    }

                    if (hasMatching)
                    {
                        RoslynDebug.Assert(newStatementSyntax != null);
                        RoslynDebug.Assert(match != null);

                        // The matching node doesn't produce sequence points.
                        // E.g. "const" keyword is inserted into a local variable declaration with an initializer.
                        newSpan = FindClosestActiveSpan(newStatementSyntax, statementPart);

                        if ((isNonLeaf || isPartiallyExecuted) && !AreEquivalentActiveStatements(oldStatementSyntax, newStatementSyntax, statementPart))
                        {
                            // rude edit: non-leaf active statement changed
                            diagnostics.Add(new RudeEditDiagnostic(isNonLeaf ? RudeEditKind.ActiveStatementUpdate : RudeEditKind.PartiallyExecutedActiveStatementUpdate, newSpan));
                        }

                        // other statements around active statement:
                        ReportOtherRudeEditsAroundActiveStatement(diagnostics, match, oldStatementSyntax, newStatementSyntax, isNonLeaf);
                    }
                    else if (match == null)
                    {
                        RoslynDebug.Assert(oldEnclosingLambdaBody != null);
                        RoslynDebug.Assert(lazyActiveOrMatchedLambdas != null);

                        newSpan = GetDeletedNodeDiagnosticSpan(oldEnclosingLambdaBody, bodyMatch, lazyActiveOrMatchedLambdas);

                        // Lambda containing the active statement can't be found in the new source.
                        var oldLambda = oldEnclosingLambdaBody.GetLambda();
                        diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.ActiveStatementLambdaRemoved, newSpan, oldLambda,
                            new[] { GetDisplayName(oldLambda) }));
                    }
                    else
                    {
                        newSpan = GetDeletedNodeActiveSpan(match.Matches, oldStatementSyntax);

                        if (isNonLeaf || isPartiallyExecuted)
                        {
                            // rude edit: internal active statement deleted
                            diagnostics.Add(
                                new RudeEditDiagnostic(isNonLeaf ? RudeEditKind.DeleteActiveStatement : RudeEditKind.PartiallyExecutedActiveStatementDelete,
                                GetDeletedNodeDiagnosticSpan(match.Matches, oldStatementSyntax),
                                arguments: new[] { FeaturesResources.code }));
                        }
                    }

                    // If there was a lambda, but we couldn't match its body to the new tree, then the lambda was
                    // removed, so we don't need to check it for active statements. If there wasn't a lambda then
                    // match here will be the same as bodyMatch.
                    if (match is not null)
                    {
                        // exception handling around the statement:
                        CalculateExceptionRegionsAroundActiveStatement(
                            match,
                            oldStatementSyntax,
                            newStatementSyntax,
                            newSpan,
                            activeStatementIndex,
                            isNonLeaf,
                            newExceptionRegions,
                            diagnostics,
                            cancellationToken);
                    }

                    // We have already calculated the new location of this active statement when analyzing another member declaration.
                    // This may only happen when two or more member declarations share the same body (VB AsNew clause).
                    Debug.Assert(IsDeclarationWithSharedBody(newDeclaration) || newActiveStatements[activeStatementIndex] == null);
                    Debug.Assert(newSpan != default);

                    newActiveStatements[activeStatementIndex] = GetActiveStatementWithSpan(oldActiveStatements[activeStatementIndex], newDeclaration.SyntaxTree, newSpan, diagnostics, cancellationToken);
                }
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                // Set the new spans of active statements overlapping the method body to match the old spans.
                // Even though these might be now outside of the method body it's ok since we report a rude edit and don't allow to continue.

                foreach (var i in activeStatementIndices)
                {
                    newActiveStatements[i] = oldActiveStatements[i].Statement;
                    newExceptionRegions[i] = ImmutableArray<SourceFileSpan>.Empty;
                }

                string declarationName;
                try
                {
                    declarationName = GetDisplayName(newDeclaration);
                }
                catch
                {
                    declarationName = $"<node {newDeclaration.RawKind} has no display name>";
                }

                var bodySpan = GetDiagnosticSpan(newDeclaration, EditKind.Update);

                // We expect OOM to be thrown during the analysis if the number of statements is too large.
                // In such case we report a rude edit for the document. If the host is actually running out of memory,
                // it might throw another OOM here or later on.
                if (e is OutOfMemoryException)
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.MemberBodyTooBig,
                        bodySpan,
                        newDeclaration,
                        arguments: new[] { declarationName }));
                }
                else
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.MemberBodyInternalError,
                        bodySpan,
                        newDeclaration,
                        arguments: new[] { declarationName, e.ToString() }));
                }
            }
        }

        private bool TryGetTrackedStatement(ImmutableArray<LinePositionSpan> activeStatementSpans, int index, SourceText text, SyntaxNode declaration, MemberBody body, [NotNullWhen(true)] out SyntaxNode? trackedStatement, out int trackedStatementPart)
        {
            trackedStatement = null;
            trackedStatementPart = -1;

            // Active statements are not tracked in this document (e.g. the file is closed).
            if (activeStatementSpans.IsEmpty)
            {
                return false;
            }

            var trackedLineSpan = activeStatementSpans[index];
            if (trackedLineSpan == default)
            {
                return false;
            }

            var trackedSpan = text.Lines.GetTextSpan(trackedLineSpan);

            // The tracking span might have been deleted or moved outside of the member span.
            // It is not an error to move the statement - we just ignore it.
            // Consider: Instead of checking here, explicitly handle all cases when active statements can be outside of the body in FindStatement and 
            // return false if the requested span is outside of the active envelope.
            var (envelope, hole) = GetActiveSpanEnvelope(declaration);
            if (!envelope.Contains(trackedSpan) || hole.Contains(trackedSpan))
            {
                return false;
            }

            trackedStatement = body.FindStatement(trackedSpan, out trackedStatementPart);
            return true;
        }

        private ActiveStatement GetActiveStatementWithSpan(UnmappedActiveStatement oldStatement, SyntaxTree newTree, TextSpan newSpan, ArrayBuilder<RudeEditDiagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var mappedLineSpan = newTree.GetMappedLineSpan(newSpan, cancellationToken);
            if (mappedLineSpan.HasMappedPath && mappedLineSpan.Path != oldStatement.Statement.FileSpan.Path)
            {
                // changing the source file of an active statement
                diagnostics.Add(new RudeEditDiagnostic(
                    RudeEditKind.UpdateAroundActiveStatement,
                    newSpan,
                    LineDirectiveSyntaxKind,
                    arguments: new[] { string.Format(FeaturesResources._0_directive, LineDirectiveKeyword) }));
            }

            return oldStatement.Statement.WithFileSpan(mappedLineSpan);
        }

        private void CalculateExceptionRegionsAroundActiveStatement(
            Match<SyntaxNode> bodyMatch,
            SyntaxNode oldStatementSyntax,
            SyntaxNode? newStatementSyntax,
            TextSpan newStatementSyntaxSpan,
            int ordinal,
            bool isNonLeaf,
            ImmutableArray<ImmutableArray<SourceFileSpan>>.Builder newExceptionRegions,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            if (newStatementSyntax == null)
            {
                if (!bodyMatch.NewRoot.Span.Contains(newStatementSyntaxSpan.Start))
                {
                    return;
                }

                newStatementSyntax = bodyMatch.NewRoot.FindToken(newStatementSyntaxSpan.Start).Parent;

                Contract.ThrowIfNull(newStatementSyntax);
            }

            var oldAncestors = GetExceptionHandlingAncestors(oldStatementSyntax, isNonLeaf);
            var newAncestors = GetExceptionHandlingAncestors(newStatementSyntax, isNonLeaf);

            if (oldAncestors.Count > 0 || newAncestors.Count > 0)
            {
                var edits = bodyMatch.GetSequenceEdits(oldAncestors, newAncestors);
                ReportEnclosingExceptionHandlingRudeEdits(diagnostics, edits, oldStatementSyntax, newStatementSyntaxSpan);

                // Exception regions are not needed in presence of errors.
                if (diagnostics.Count == 0)
                {
                    Debug.Assert(oldAncestors.Count == newAncestors.Count);
                    newExceptionRegions[ordinal] = GetExceptionRegions(newAncestors, newStatementSyntax.SyntaxTree, cancellationToken).Spans;
                }
            }
        }

        /// <summary>
        /// Calculates a syntax map of the entire method body including all lambda bodies it contains.
        /// </summary>
        private BidirectionalMap<SyntaxNode> ComputeMap(
            Match<SyntaxNode> memberBodyMatch,
            ArrayBuilder<ActiveNode> memberBodyActiveNodes,
            ref Dictionary<LambdaBody, LambdaInfo>? lazyActiveOrMatchedLambdas)
        {
            ArrayBuilder<Match<SyntaxNode>>? lambdaBodyMatches = null;
            var currentLambdaBodyMatch = -1;
            var currentBodyMatch = memberBodyMatch;

            while (true)
            {
                foreach (var (oldNode, newNode) in currentBodyMatch.Matches)
                {
                    // Skip root, only enumerate body matches.
                    if (oldNode == currentBodyMatch.OldRoot)
                    {
                        Debug.Assert(newNode == currentBodyMatch.NewRoot);
                        continue;
                    }

                    if (TryGetLambdaBodies(oldNode, out var oldLambdaBody1, out var oldLambdaBody2))
                    {
                        lambdaBodyMatches ??= ArrayBuilder<Match<SyntaxNode>>.GetInstance();
                        lazyActiveOrMatchedLambdas ??= new Dictionary<LambdaBody, LambdaInfo>();

                        var newLambdaBody1 = oldLambdaBody1.TryGetPartnerLambdaBody(newNode);
                        if (newLambdaBody1 != null)
                        {
                            lambdaBodyMatches.Add(ComputeMatch(oldLambdaBody1, newLambdaBody1, memberBodyActiveNodes, lazyActiveOrMatchedLambdas));
                        }

                        if (oldLambdaBody2 != null)
                        {
                            var newLambdaBody2 = oldLambdaBody2.TryGetPartnerLambdaBody(newNode);
                            if (newLambdaBody2 != null)
                            {
                                lambdaBodyMatches.Add(ComputeMatch(oldLambdaBody2, newLambdaBody2, memberBodyActiveNodes, lazyActiveOrMatchedLambdas));
                            }
                        }
                    }
                }

                currentLambdaBodyMatch++;
                if (lambdaBodyMatches == null || currentLambdaBodyMatch == lambdaBodyMatches.Count)
                {
                    break;
                }

                currentBodyMatch = lambdaBodyMatches[currentLambdaBodyMatch];
            }

            if (lambdaBodyMatches == null)
            {
                return BidirectionalMap<SyntaxNode>.FromMatch(memberBodyMatch);
            }

            var map = new Dictionary<SyntaxNode, SyntaxNode>();
            var reverseMap = new Dictionary<SyntaxNode, SyntaxNode>();

            // include all matches, including the root:
            map.AddRange(memberBodyMatch.Matches);
            reverseMap.AddRange(memberBodyMatch.ReverseMatches);

            foreach (var lambdaBodyMatch in lambdaBodyMatches)
            {
                foreach (var (oldNode, newNode) in lambdaBodyMatch.Matches)
                {
                    if (!map.ContainsKey(oldNode))
                    {
                        map[oldNode] = newNode;
                        reverseMap[newNode] = oldNode;
                    }
                }
            }

            lambdaBodyMatches?.Free();

            return new BidirectionalMap<SyntaxNode>(map, reverseMap);
        }

        private static Match<SyntaxNode> ComputeMatch(
            LambdaBody oldLambdaBody,
            LambdaBody newLambdaBody,
            IReadOnlyList<ActiveNode> memberBodyActiveNodes,
            [Out] Dictionary<LambdaBody, LambdaInfo> activeOrMatchedLambdas)
        {
            IEnumerable<ActiveNode> activeNodesInLambdaBody;
            if (activeOrMatchedLambdas.TryGetValue(oldLambdaBody, out var info))
            {
                // Lambda may be matched but not be active.
                activeNodesInLambdaBody = info.ActiveNodeIndices?.Select(i => memberBodyActiveNodes[i]) ?? Array.Empty<ActiveNode>();
            }
            else
            {
                // If the lambda body isn't in the map it doesn't have any active/tracked statements.
                activeNodesInLambdaBody = Array.Empty<ActiveNode>();
                info = new LambdaInfo();
            }

            var lambdaBodyMatch = ComputeMatch(oldLambdaBody, newLambdaBody, activeNodesInLambdaBody);

            activeOrMatchedLambdas[oldLambdaBody] = info.WithMatch(lambdaBodyMatch, newLambdaBody);

            return lambdaBodyMatch;
        }

        /// <summary>
        /// Called for a member body and for bodies of all lambdas and local functions (recursively) found in the member body.
        /// </summary>
        private static Match<SyntaxNode> ComputeMatch(DeclarationBody oldBody, DeclarationBody newBody, IEnumerable<ActiveNode> activeNodes)
            => oldBody.ComputeMatch(newBody, knownMatches: GetMatchingActiveNodes(activeNodes));

        private void ReportStateMachineBodyUpdateRudeEdits(
            Match<SyntaxNode> match,
            StateMachineInfo oldStateMachineInfo,
            SyntaxNode newDeclaration,
            StateMachineInfo newStateMachineInfo,
            bool hasActiveStatement,
            ArrayBuilder<RudeEditDiagnostic> diagnostics)
        {
            // Consider following cases:
            // 1) The new method contains yields/awaits but the old doesn't.
            //    If the method has active statements report rude edits for each inserted yield/await (insert "around" an active statement).
            // 2) The old method is async/iterator, the new method is not and it contains an active statement.
            //    Report rude edit since we can't remap IP from MoveNext to the kickoff method.
            //    Note that iterators in VB don't need to contain yield, so this case is not covered by change in number of yields.

            if (oldStateMachineInfo.HasSuspensionPoints)
            {
                foreach (var (oldNode, newNode) in match.Matches)
                {
                    ReportStateMachineSuspensionPointRudeEdits(diagnostics, oldNode, newNode);
                }
            }

            // It is allowed to update a regular method to an async method or an iterator.
            // The only restriction is a presence of an active statement in the method body
            // since the debugger does not support remapping active statements to a different method.
            if (hasActiveStatement && oldStateMachineInfo.IsStateMachine != newStateMachineInfo.IsStateMachine)
            {
                diagnostics.Add(new RudeEditDiagnostic(
                    RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement,
                    GetDiagnosticSpan(newDeclaration, EditKind.Update)));
            }

            // report removing async as rude:
            if (oldStateMachineInfo.IsAsync && !newStateMachineInfo.IsAsync)
            {
                diagnostics.Add(new RudeEditDiagnostic(
                    RudeEditKind.ChangingFromAsynchronousToSynchronous,
                    GetDiagnosticSpan(newDeclaration, EditKind.Update),
                    newDeclaration,
                    new[] { GetDisplayName(newDeclaration) }));
            }

            // VB supports iterator lambdas/methods without yields
            if (oldStateMachineInfo.IsIterator && !newStateMachineInfo.IsIterator)
            {
                diagnostics.Add(new RudeEditDiagnostic(
                    RudeEditKind.ModifiersUpdate,
                    GetDiagnosticSpan(newDeclaration, EditKind.Update),
                    newDeclaration,
                    new[] { GetDisplayName(newDeclaration) }));
            }
        }

        internal virtual void ReportStateMachineSuspensionPointDeletedRudeEdit(ArrayBuilder<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, SyntaxNode deletedSuspensionPoint)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.Delete,
                GetDeletedNodeDiagnosticSpan(match.Matches, deletedSuspensionPoint),
                deletedSuspensionPoint,
                new[] { GetSuspensionPointDisplayName(deletedSuspensionPoint, EditKind.Delete) }));
        }

        internal virtual void ReportStateMachineSuspensionPointInsertedRudeEdit(ArrayBuilder<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, SyntaxNode insertedSuspensionPoint, bool aroundActiveStatement)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                aroundActiveStatement ? RudeEditKind.InsertAroundActiveStatement : RudeEditKind.Insert,
                GetDiagnosticSpan(insertedSuspensionPoint, EditKind.Insert),
                insertedSuspensionPoint,
                new[] { GetSuspensionPointDisplayName(insertedSuspensionPoint, EditKind.Insert) }));
        }

        private static List<KeyValuePair<SyntaxNode, SyntaxNode>>? GetMatchingActiveNodes(IEnumerable<ActiveNode> activeNodes)
        {
            // add nodes that are tracked by the editor buffer to known matches:
            List<KeyValuePair<SyntaxNode, SyntaxNode>>? lazyKnownMatches = null;

            foreach (var activeNode in activeNodes)
            {
                if (activeNode.NewTrackedNode != null)
                {
                    lazyKnownMatches ??= new List<KeyValuePair<SyntaxNode, SyntaxNode>>();
                    lazyKnownMatches.Add(KeyValuePairUtil.Create(activeNode.OldNode, activeNode.NewTrackedNode));
                }
            }

            return lazyKnownMatches;
        }

        public ActiveStatementExceptionRegions GetExceptionRegions(SyntaxNode syntaxRoot, TextSpan unmappedActiveStatementSpan, bool isNonLeaf, CancellationToken cancellationToken)
        {
            var token = syntaxRoot.FindToken(unmappedActiveStatementSpan.Start);
            var ancestors = GetExceptionHandlingAncestors(token.Parent!, isNonLeaf);
            return GetExceptionRegions(ancestors, syntaxRoot.SyntaxTree, cancellationToken);
        }

        private ActiveStatementExceptionRegions GetExceptionRegions(List<SyntaxNode> exceptionHandlingAncestors, SyntaxTree tree, CancellationToken cancellationToken)
        {
            if (exceptionHandlingAncestors.Count == 0)
            {
                return new ActiveStatementExceptionRegions(ImmutableArray<SourceFileSpan>.Empty, isActiveStatementCovered: false);
            }

            var isCovered = false;
            using var _ = ArrayBuilder<SourceFileSpan>.GetInstance(out var result);

            for (var i = exceptionHandlingAncestors.Count - 1; i >= 0; i--)
            {
                var span = GetExceptionHandlingRegion(exceptionHandlingAncestors[i], out var coversAllChildren);

                // TODO: https://github.com/dotnet/roslyn/issues/52971
                // 1) Check that the span doesn't cross #line pragmas with different file mappings.
                // 2) Check that the mapped path does not change and report rude edits if it does.
                result.Add(tree.GetMappedLineSpan(span, cancellationToken));

                // Exception regions describe regions of code that can't be edited.
                // If the span covers all the children nodes we don't need to descend further.
                if (coversAllChildren)
                {
                    isCovered = true;
                    break;
                }
            }

            return new ActiveStatementExceptionRegions(result.ToImmutable(), isCovered);
        }

        private TextSpan GetDeletedNodeDiagnosticSpan(LambdaBody deletedLambdaBody, Match<SyntaxNode> match, Dictionary<LambdaBody, LambdaInfo> lambdaInfos)
        {
            var oldLambdaBody = deletedLambdaBody;
            while (true)
            {
                var oldLambda = oldLambdaBody.GetLambda();
                var oldParentLambdaBody = FindEnclosingLambdaBody(match.OldRoot, oldLambda);
                if (oldParentLambdaBody == null)
                {
                    return GetDeletedNodeDiagnosticSpan(match.Matches, oldLambda);
                }

                if (lambdaInfos.TryGetValue(oldParentLambdaBody, out var lambdaInfo) && lambdaInfo.Match != null)
                {
                    return GetDeletedNodeDiagnosticSpan(lambdaInfo.Match.Matches, oldLambda);
                }

                oldLambdaBody = oldParentLambdaBody;
            }
        }

        private TextSpan FindClosestActiveSpan(SyntaxNode statement, int statementPart)
        {
            if (TryGetActiveSpan(statement, statementPart, minLength: statement.Span.Length, out var span))
            {
                return span;
            }

            // The node doesn't have sequence points.
            // E.g. "const" keyword is inserted into a local variable declaration with an initializer.
            foreach (var (node, part) in EnumerateNearStatements(statement))
            {
                if (part == -1)
                {
                    return node.Span;
                }

                if (TryGetActiveSpan(node, part, minLength: 0, out span))
                {
                    return span;
                }
            }

            // This might occur in cases where we report rude edit, so the exact location of the active span doesn't matter.
            // For example, when a method expression body is removed in C#.
            return statement.Span;
        }

        internal TextSpan GetDeletedNodeActiveSpan(IReadOnlyDictionary<SyntaxNode, SyntaxNode> forwardMap, SyntaxNode deletedNode)
        {
            foreach (var (oldNode, part) in EnumerateNearStatements(deletedNode))
            {
                if (part == -1)
                {
                    break;
                }

                if (forwardMap.TryGetValue(oldNode, out var newNode))
                {
                    return FindClosestActiveSpan(newNode, part);
                }
            }

            return GetDeletedNodeDiagnosticSpan(forwardMap, deletedNode);
        }

        internal TextSpan GetDeletedDeclarationActiveSpan(IReadOnlyDictionary<SyntaxNode, SyntaxNode> forwardMap, SyntaxNode deletedDeclaration)
        {
            if (IsDeclarationWithInitializer(deletedDeclaration))
            {
                return GetDeletedNodeActiveSpan(forwardMap, deletedDeclaration);
            }

            // TODO: if the member isn't a field/property we should return empty span.
            // We need to adjust the tracking span design and UpdateUneditedSpans to account for such empty spans.

            var hasAncestor = TryGetMatchingAncestor(forwardMap, deletedDeclaration, out var newAncestor);
            Debug.Assert(hasAncestor && newAncestor != null);

            // the only matching ancestor is teh compilation unit:
            if (newAncestor.Parent == null)
            {
                return default;
            }

            return GetDiagnosticSpan(newAncestor, EditKind.Delete);
        }

        internal TextSpan GetDeletedNodeDiagnosticSpan(IReadOnlyDictionary<SyntaxNode, SyntaxNode> forwardMap, SyntaxNode deletedNode)
        {
            var hasAncestor = TryGetMatchingAncestor(forwardMap, deletedNode, out var newAncestor);
            Debug.Assert(hasAncestor && newAncestor != null);
            return GetDiagnosticSpan(newAncestor, EditKind.Delete);
        }

        /// <summary>
        /// Finds the inner-most ancestor of the specified node that has a matching node in the new tree.
        /// </summary>
        private static bool TryGetMatchingAncestor(IReadOnlyDictionary<SyntaxNode, SyntaxNode> forwardMap, SyntaxNode? oldNode, [NotNullWhen(true)] out SyntaxNode? newAncestor)
        {
            while (oldNode != null)
            {
                if (forwardMap.TryGetValue(oldNode, out newAncestor))
                {
                    return true;
                }

                oldNode = oldNode.Parent;
            }

            // only happens if original oldNode is a root, 
            // otherwise we always find a matching ancestor pair (roots).
            newAncestor = null;
            return false;
        }

        private static IEnumerable<int> GetOverlappingActiveStatements(MemberBody body, ImmutableArray<UnmappedActiveStatement> statements)
        {
            var (span, hole) = body.Envelope;

            var range = ActiveStatementsMap.GetSpansStartingInSpan(
                span.Start,
                span.End,
                statements,
                startPositionComparer: (x, y) => x.UnmappedSpan.Start.CompareTo(y));

            for (var i = range.Start.Value; i < range.End.Value; i++)
            {
                if (!hole.Contains(statements[i].UnmappedSpan.Start))
                {
                    yield return i;
                }
            }
        }

        protected static bool HasParentEdit(IReadOnlyDictionary<SyntaxNode, EditKind> editMap, Edit<SyntaxNode> edit)
        {
            SyntaxNode node;
            switch (edit.Kind)
            {
                case EditKind.Insert:
                    node = edit.NewNode;
                    break;

                case EditKind.Delete:
                    node = edit.OldNode;
                    break;

                default:
                    return false;
            }

            return HasEdit(editMap, node.Parent, edit.Kind);
        }

        protected static bool HasEdit(IReadOnlyDictionary<SyntaxNode, EditKind> editMap, SyntaxNode? node, EditKind editKind)
        {
            return
                node is object &&
                editMap.TryGetValue(node, out var parentEdit) &&
                parentEdit == editKind;
        }

        #endregion

        #region Rude Edits around Active Statement 

        protected void AddAroundActiveStatementRudeDiagnostic(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode? oldNode, SyntaxNode? newNode, TextSpan newActiveStatementSpan)
        {
            if (oldNode == null)
            {
                RoslynDebug.Assert(newNode != null);
                AddRudeInsertAroundActiveStatement(diagnostics, newNode);
            }
            else if (newNode == null)
            {
                RoslynDebug.Assert(oldNode != null);
                AddRudeDeleteAroundActiveStatement(diagnostics, oldNode, newActiveStatementSpan);
            }
            else
            {
                AddRudeUpdateAroundActiveStatement(diagnostics, newNode);
            }
        }

        protected void AddRudeUpdateAroundActiveStatement(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode newNode)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.UpdateAroundActiveStatement,
                GetDiagnosticSpan(newNode, EditKind.Update),
                newNode,
                new[] { GetDisplayName(newNode, EditKind.Update) }));
        }

        protected void AddRudeInsertAroundActiveStatement(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode newNode)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.InsertAroundActiveStatement,
                GetDiagnosticSpan(newNode, EditKind.Insert),
                newNode,
                new[] { GetDisplayName(newNode, EditKind.Insert) }));
        }

        protected void AddRudeDeleteAroundActiveStatement(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, TextSpan newActiveStatementSpan)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                RudeEditKind.DeleteAroundActiveStatement,
                newActiveStatementSpan,
                oldNode,
                new[] { GetDisplayName(oldNode, EditKind.Delete) }));
        }

        protected void ReportUnmatchedStatements<TSyntaxNode>(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            Match<SyntaxNode> match,
            Func<SyntaxNode, bool> nodeSelector,
            SyntaxNode oldActiveStatement,
            SyntaxNode newActiveStatement,
            Func<TSyntaxNode, TSyntaxNode, bool> areEquivalent,
            Func<TSyntaxNode, TSyntaxNode, bool>? areSimilar)
            where TSyntaxNode : SyntaxNode
        {
            var newNodes = GetAncestors(GetEncompassingAncestor(match.NewRoot), newActiveStatement, nodeSelector);
            if (newNodes == null)
            {
                return;
            }

            var oldNodes = GetAncestors(GetEncompassingAncestor(match.OldRoot), oldActiveStatement, nodeSelector);

            int matchCount;
            if (oldNodes != null)
            {
                matchCount = MatchNodes(oldNodes, newNodes, diagnostics: null, match: match, comparer: areEquivalent);

                // Do another pass over the nodes to improve error messages.
                if (areSimilar != null && matchCount < Math.Min(oldNodes.Count, newNodes.Count))
                {
                    matchCount += MatchNodes(oldNodes, newNodes, diagnostics: diagnostics, match: null, comparer: areSimilar);
                }
            }
            else
            {
                matchCount = 0;
            }

            if (matchCount < newNodes.Count)
            {
                ReportRudeEditsAndInserts(oldNodes, newNodes, diagnostics);
            }
        }

        private void ReportRudeEditsAndInserts(List<SyntaxNode?>? oldNodes, List<SyntaxNode?> newNodes, ArrayBuilder<RudeEditDiagnostic> diagnostics)
        {
            var oldNodeCount = (oldNodes != null) ? oldNodes.Count : 0;

            for (var i = 0; i < newNodes.Count; i++)
            {
                var newNode = newNodes[i];

                if (newNode != null)
                {
                    // Any difference can be expressed as insert, delete & insert, edit, or move & edit.
                    // Heuristic: If the nesting levels of the old and new nodes are the same we report an edit.
                    // Otherwise we report an insert.
                    if (i < oldNodeCount && oldNodes![i] != null)
                    {
                        AddRudeUpdateAroundActiveStatement(diagnostics, newNode);
                    }
                    else
                    {
                        AddRudeInsertAroundActiveStatement(diagnostics, newNode);
                    }
                }
            }
        }

        private int MatchNodes<TSyntaxNode>(
            List<SyntaxNode?> oldNodes,
            List<SyntaxNode?> newNodes,
            ArrayBuilder<RudeEditDiagnostic>? diagnostics,
            Match<SyntaxNode>? match,
            Func<TSyntaxNode, TSyntaxNode, bool> comparer)
            where TSyntaxNode : SyntaxNode
        {
            var matchCount = 0;
            var oldIndex = 0;
            for (var newIndex = 0; newIndex < newNodes.Count; newIndex++)
            {
                var newNode = newNodes[newIndex];
                if (newNode == null)
                {
                    continue;
                }

                SyntaxNode? oldNode;
                while (oldIndex < oldNodes.Count)
                {
                    oldNode = oldNodes[oldIndex];

                    if (oldNode != null)
                    {
                        break;
                    }

                    // node has already been matched with a previous new node:
                    oldIndex++;
                }

                if (oldIndex == oldNodes.Count)
                {
                    break;
                }

                var i = -1;
                if (match == null)
                {
                    i = IndexOfEquivalent(newNode, oldNodes, oldIndex, comparer);
                }
                else if (match.TryGetOldNode(newNode, out var partner) && comparer((TSyntaxNode)partner, (TSyntaxNode)newNode))
                {
                    i = oldNodes.IndexOf(partner, oldIndex);
                }

                if (i >= 0)
                {
                    // we have an update or an exact match:
                    oldNodes[i] = null;
                    newNodes[newIndex] = null;
                    matchCount++;

                    if (diagnostics != null)
                    {
                        AddRudeUpdateAroundActiveStatement(diagnostics, newNode);
                    }
                }
            }

            return matchCount;
        }

        private static int IndexOfEquivalent<TSyntaxNode>(SyntaxNode newNode, List<SyntaxNode?> oldNodes, int startIndex, Func<TSyntaxNode, TSyntaxNode, bool> comparer)
            where TSyntaxNode : SyntaxNode
        {
            for (var i = startIndex; i < oldNodes.Count; i++)
            {
                var oldNode = oldNodes[i];
                if (oldNode != null && comparer((TSyntaxNode)oldNode, (TSyntaxNode)newNode))
                {
                    return i;
                }
            }

            return -1;
        }

        private static List<SyntaxNode?>? GetAncestors(SyntaxNode? root, SyntaxNode node, Func<SyntaxNode, bool> nodeSelector)
        {
            List<SyntaxNode?>? list = null;
            var current = node;

            while (current is object && current != root)
            {
                if (nodeSelector(current))
                {
                    list ??= new List<SyntaxNode?>();
                    list.Add(current);
                }

                current = current.Parent;
            }

            list?.Reverse();

            return list;
        }

        #endregion

        #region Trivia Analysis

        /// <summary>
        /// Top-level edit script does not contain edits for a member if only trivia changed in its body.
        /// It also does not reflect changes in line mapping directives.
        /// Members that are unchanged but their location in the file changes are not considered updated.
        /// This method calculates line and trivia edits for all these cases.
        /// 
        /// The resulting line edits are grouped by mapped document path and sorted by <see cref="SourceLineUpdate.OldLine"/> in each group.
        /// </summary>
        private void AnalyzeTrivia(
            Match<SyntaxNode> topMatch,
            IReadOnlyDictionary<SyntaxNode, EditKind> editMap,
            [Out] ArrayBuilder<(SyntaxNode OldNode, SyntaxNode NewNode, TextSpan DiagnosticSpan)> triviaEdits,
            [Out] ArrayBuilder<SequencePointUpdates> lineEdits,
            CancellationToken cancellationToken)
        {
            var oldTree = topMatch.OldRoot.SyntaxTree;
            var newTree = topMatch.NewRoot.SyntaxTree;

            // note: range [oldStartLine, oldEndLine] is end-inclusive
            using var _ = ArrayBuilder<(string filePath, int oldStartLine, int oldEndLine, int delta, SyntaxNode oldNode, SyntaxNode newNode)>.GetInstance(out var segments);

            foreach (var (oldNode, newNode) in topMatch.Matches)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (editMap.ContainsKey(newNode))
                {
                    // Updated or inserted members will be (re)generated and don't need line edits.
                    Debug.Assert(editMap[newNode] is EditKind.Update or EditKind.Insert);
                    continue;
                }

                var newTokens = TryGetDeclarationBody(newNode)?.GetActiveTokens();
                if (newTokens == null)
                {
                    continue;
                }

                // A (rude) edit could have been made that changes whether the node may contain active statements,
                // so although the nodes match they might not have the same active tokens.
                // E.g. field declaration changed to const field declaration.
                var oldTokens = TryGetDeclarationBody(oldNode)?.GetActiveTokens();
                if (oldTokens == null)
                {
                    continue;
                }

                var newTokensEnum = newTokens.GetEnumerator();
                var oldTokensEnum = oldTokens.GetEnumerator();

                // We enumerate tokens of the body and split them into segments.
                // Each segment has sequence points mapped to the same file and also all lines the segment covers map to the same line delta.
                // The first token of a segment must be the first token that starts on the line. If the first segment token was in the middle line 
                // the previous token on the same line would have different line delta and we wouldn't be able to map both of them at the same time.
                // All segments are included in the segments list regardless of their line delta (even when it's 0 - i.e. the lines did not change).
                // This is necessary as we need to detect collisions of multiple segments with different deltas later on.

                var lastNewToken = default(SyntaxToken);
                var lastOldStartLine = -1;
                var lastOldFilePath = (string?)null;
                var requiresUpdate = false;

                var firstSegmentIndex = segments.Count;
                var currentSegment = (path: (string?)null, oldStartLine: 0, delta: 0, firstOldNode: (SyntaxNode?)null, firstNewNode: (SyntaxNode?)null);
                var rudeEditSpan = default(TextSpan);

                // Check if the breakpoint span that covers the first node of the segment can be translated from the old to the new by adding a line delta.
                // If not we need to recompile the containing member since we are not able to produce line update for it.
                // The first node of the segment can be the first node on its line but the breakpoint span might start on the previous line.
                bool IsCurrentSegmentBreakpointSpanMappable()
                {
                    var oldNode = currentSegment.firstOldNode;
                    var newNode = currentSegment.firstNewNode;
                    Contract.ThrowIfNull(oldNode);
                    Contract.ThrowIfNull(newNode);

                    // Some nodes (e.g. const local declaration) may not be covered by a breakpoint span.
                    if (!TryGetEnclosingBreakpointSpan(oldNode, oldNode.SpanStart, out var oldBreakpointSpan) ||
                        !TryGetEnclosingBreakpointSpan(newNode, newNode.SpanStart, out var newBreakpointSpan))
                    {
                        return true;
                    }

                    var oldMappedBreakpointSpan = (SourceFileSpan)oldTree.GetMappedLineSpan(oldBreakpointSpan, cancellationToken);
                    var newMappedBreakpointSpan = (SourceFileSpan)newTree.GetMappedLineSpan(newBreakpointSpan, cancellationToken);

                    if (oldMappedBreakpointSpan.AddLineDelta(currentSegment.delta) == newMappedBreakpointSpan)
                    {
                        return true;
                    }

                    rudeEditSpan = newBreakpointSpan;
                    return false;
                }

                void AddCurrentSegment()
                {
                    Debug.Assert(currentSegment.path != null);
                    Debug.Assert(lastOldStartLine >= 0);

                    // segment it ends on the line where the previous token starts (lastOldStartLine)
                    segments.Add((currentSegment.path, currentSegment.oldStartLine, lastOldStartLine, currentSegment.delta, oldNode, newNode));
                }

                bool oldHasToken;
                bool newHasToken;

                while (true)
                {
                    oldHasToken = oldTokensEnum.MoveNext();
                    newHasToken = newTokensEnum.MoveNext();

                    // If tokens differ in other parts then in trivia, skip the current matching node pair since we are only looking for trivia changes.
                    // This may happen when active tokens of member bodies overlap with nodes that do not represent a body.
                    // For example, in VB an initializer of a variable declarator node is included in the bodies of each modified identifier listed in the declarator,
                    // the declarator itself doesn't represent a body.
                    if (oldHasToken != newHasToken || oldHasToken && !AreEqualIgnoringTrivia(oldTokensEnum.Current, newTokensEnum.Current))
                    {
                        requiresUpdate = false;
                        break;
                    }

                    if (!oldHasToken)
                    {
                        if (!IsCurrentSegmentBreakpointSpanMappable())
                        {
                            requiresUpdate = true;
                        }
                        else
                        {
                            // add last segment of the method body:
                            AddCurrentSegment();
                        }

                        break;
                    }

                    var oldSpan = oldTokensEnum.Current.Span;
                    var newSpan = newTokensEnum.Current.Span;

                    var oldMappedSpan = oldTree.GetMappedLineSpan(oldSpan, cancellationToken);
                    var newMappedSpan = newTree.GetMappedLineSpan(newSpan, cancellationToken);

                    var oldStartLine = oldMappedSpan.Span.Start.Line;
                    var newStartLine = newMappedSpan.Span.Start.Line;
                    var lineDelta = newStartLine - oldStartLine;

                    // If any tokens in the method change their mapped column or mapped path the method must be recompiled 
                    // since the Debugger/SymReader does not support these updates.
                    if (oldMappedSpan.Span.Start.Character != newMappedSpan.Span.Start.Character)
                    {
                        requiresUpdate = true;
                        break;
                    }

                    if (currentSegment.path != oldMappedSpan.Path || currentSegment.delta != lineDelta)
                    {
                        // end of segment:
                        if (currentSegment.path != null)
                        {
                            // Previous token start line is the same as this token start line, but the previous token line delta is not the same.
                            // We can't therefore map the old start line to a new one using line delta since that would affect both tokens the same.
                            if (lastOldStartLine == oldStartLine && string.Equals(lastOldFilePath, oldMappedSpan.Path))
                            {
                                requiresUpdate = true;
                                break;
                            }

                            if (!IsCurrentSegmentBreakpointSpanMappable())
                            {
                                requiresUpdate = true;
                                break;
                            }

                            // add current segment:
                            AddCurrentSegment();
                        }

                        // start new segment:
                        currentSegment = (oldMappedSpan.Path, oldStartLine, lineDelta, oldTokensEnum.Current.Parent, newTokensEnum.Current.Parent);
                    }

                    lastNewToken = newTokensEnum.Current;
                    lastOldStartLine = oldStartLine;
                    lastOldFilePath = oldMappedSpan.Path;
                }

                // All tokens of a member body have been processed now.
                if (requiresUpdate)
                {
                    // report the rude edit for the span of tokens that forced recompilation:
                    if (rudeEditSpan.IsEmpty)
                    {
                        rudeEditSpan = TextSpan.FromBounds(
                            lastNewToken.HasTrailingTrivia ? lastNewToken.Span.End : newTokensEnum.Current.FullSpan.Start,
                            newTokensEnum.Current.SpanStart);
                    }

                    triviaEdits.Add((oldNode, newNode, rudeEditSpan));

                    // remove all segments added for the current member body:
                    segments.Count = firstSegmentIndex;
                }
            }

            if (segments.Count == 0)
            {
                return;
            }

            // sort segments by file and then by start line:
            segments.Sort((x, y) =>
            {
                var result = string.CompareOrdinal(x.filePath, y.filePath);
                return (result != 0) ? result : x.oldStartLine.CompareTo(y.oldStartLine);
            });

            // Calculate line updates based on segments.
            // If two segments with different line deltas overlap we need to recompile all overlapping members except for the first one.
            // The debugger does not apply line deltas to recompiled methods and hence we can chose to recompile either of the overlapping segments
            // and apply line delta to the others.
            // 
            // The line delta is applied to the start line of a sequence point. If start lines of two sequence points mapped to the same location
            // before the delta is applied then they will point to the same location after the delta is applied. But that wouldn't be correct
            // if two different mappings required applying different deltas and thus different locations.
            // This also applies when two methods are on the same line in the old version and they move by different deltas.

            using var _1 = ArrayBuilder<SourceLineUpdate>.GetInstance(out var documentLineEdits);

            var currentDocumentPath = segments[0].filePath;
            var previousOldEndLine = -1;
            var previousLineDelta = 0;
            foreach (var segment in segments)
            {
                if (segment.filePath != currentDocumentPath)
                {
                    // store results for the previous document:
                    if (documentLineEdits.Count > 0)
                    {
                        lineEdits.Add(new SequencePointUpdates(currentDocumentPath, documentLineEdits.ToImmutableAndClear()));
                    }

                    // switch to the next document:
                    currentDocumentPath = segment.filePath;
                    previousOldEndLine = -1;
                    previousLineDelta = 0;
                }
                else if (segment.oldStartLine <= previousOldEndLine && segment.delta != previousLineDelta)
                {
                    // The segment overlaps the previous one that has a different line delta. We need to recompile the method.
                    // The debugger filters out line deltas that correspond to recompiled methods so we don't need to.
                    triviaEdits.Add((segment.oldNode, segment.newNode, segment.newNode.Span));
                    continue;
                }

                // If the segment being added does not start on the line immediately following the previous segment end line
                // we need to insert another line update that resets the delta to 0 for the lines following the end line.
                if (documentLineEdits.Count > 0 && segment.oldStartLine > previousOldEndLine + 1)
                {
                    Debug.Assert(previousOldEndLine >= 0);
                    documentLineEdits.Add(new SourceLineUpdate(previousOldEndLine + 1, previousOldEndLine + 1));
                    previousLineDelta = 0;
                }

                // Skip segment that doesn't change line numbers - the line edit would have no effect.
                // It was only added to facilitate detection of overlap with other segments.
                // Also skip the segment if the last line update has the same line delta as
                // consecutive same line deltas has the same effect as a single one.
                if (segment.delta != 0 && segment.delta != previousLineDelta)
                {
                    documentLineEdits.Add(new SourceLineUpdate(segment.oldStartLine, segment.oldStartLine + segment.delta));
                }

                previousOldEndLine = segment.oldEndLine;
                previousLineDelta = segment.delta;
            }

            if (currentDocumentPath != null && documentLineEdits.Count > 0)
            {
                lineEdits.Add(new SequencePointUpdates(currentDocumentPath, documentLineEdits.ToImmutable()));
            }
        }

        #endregion

        #region Semantic Analysis

        private sealed class AssemblyEqualityComparer : IEqualityComparer<IAssemblySymbol?>
        {
            public static readonly IEqualityComparer<IAssemblySymbol?> Instance = new AssemblyEqualityComparer();

            public bool Equals(IAssemblySymbol? x, IAssemblySymbol? y)
            {
                // Types defined in old source assembly need to be treated as equivalent to types in the new source assembly,
                // provided that they only differ in their containing assemblies.
                // 
                // The old source symbol has the same identity as the new one.
                // Two distinct assembly symbols that are referenced by the compilations have to have distinct identities.
                // If the compilation has two metadata references whose identities unify the compiler de-dups them and only creates
                // a single PE symbol. Thus comparing assemblies by identity partitions them so that each partition
                // contains assemblies that originated from the same Gen0 assembly.

                return Equals(x?.Identity, y?.Identity);
            }

            public int GetHashCode(IAssemblySymbol? obj)
                => obj?.Identity.GetHashCode() ?? 0;
        }

        // Ignore tuple element changes, nullability and dynamic. These type changes do not affect runtime type.
        // They only affect custom attributes emitted on the members - all runtimes are expected to accept
        // custom attribute updates in metadata deltas, even if they do not have any observable effect.
        private static readonly SymbolEquivalenceComparer s_runtimeSymbolEqualityComparer = new(
            AssemblyEqualityComparer.Instance, distinguishRefFromOut: true, tupleNamesMustMatch: false, ignoreNullableAnnotations: true);

        private static readonly SymbolEquivalenceComparer s_exactSymbolEqualityComparer = new(
            AssemblyEqualityComparer.Instance, distinguishRefFromOut: true, tupleNamesMustMatch: true, ignoreNullableAnnotations: false);

        protected static bool SymbolsEquivalent(ISymbol oldSymbol, ISymbol newSymbol)
            => s_exactSymbolEqualityComparer.Equals(oldSymbol, newSymbol);

        protected static bool SignaturesEquivalent(ImmutableArray<IParameterSymbol> oldParameters, ITypeSymbol oldReturnType, ImmutableArray<IParameterSymbol> newParameters, ITypeSymbol newReturnType)
            => ParameterTypesEquivalent(oldParameters, newParameters, exact: false) &&
               s_runtimeSymbolEqualityComparer.Equals(oldReturnType, newReturnType); // TODO: should check ref, ref readonly, custom mods

        protected static bool ParameterTypesEquivalent(ImmutableArray<IParameterSymbol> oldParameters, ImmutableArray<IParameterSymbol> newParameters, bool exact)
            => oldParameters.SequenceEqual(newParameters, exact, ParameterTypesEquivalent);

        protected static bool CustomModifiersEquivalent(CustomModifier oldModifier, CustomModifier newModifier, bool exact)
            => oldModifier.IsOptional == newModifier.IsOptional &&
               TypesEquivalent(oldModifier.Modifier, newModifier.Modifier, exact);

        protected static bool CustomModifiersEquivalent(ImmutableArray<CustomModifier> oldModifiers, ImmutableArray<CustomModifier> newModifiers, bool exact)
            => oldModifiers.SequenceEqual(newModifiers, exact, CustomModifiersEquivalent);

        protected static bool ReturnTypesEquivalent(IMethodSymbol oldMethod, IMethodSymbol newMethod, bool exact)
            => oldMethod.ReturnsByRef == newMethod.ReturnsByRef &&
               oldMethod.ReturnsByRefReadonly == newMethod.ReturnsByRefReadonly &&
               CustomModifiersEquivalent(oldMethod.ReturnTypeCustomModifiers, newMethod.ReturnTypeCustomModifiers, exact) &&
               CustomModifiersEquivalent(oldMethod.RefCustomModifiers, newMethod.RefCustomModifiers, exact) &&
               TypesEquivalent(oldMethod.ReturnType, newMethod.ReturnType, exact);

        protected static bool ReturnTypesEquivalent(IPropertySymbol oldProperty, IPropertySymbol newProperty, bool exact)
            => oldProperty.ReturnsByRef == newProperty.ReturnsByRef &&
               oldProperty.ReturnsByRefReadonly == newProperty.ReturnsByRefReadonly &&
               CustomModifiersEquivalent(oldProperty.TypeCustomModifiers, newProperty.TypeCustomModifiers, exact) &&
               CustomModifiersEquivalent(oldProperty.RefCustomModifiers, newProperty.RefCustomModifiers, exact) &&
               TypesEquivalent(oldProperty.Type, newProperty.Type, exact);

        protected static bool ReturnTypesEquivalent(IEventSymbol oldEvent, IEventSymbol newEvent, bool exact)
            => TypesEquivalent(oldEvent.Type, newEvent.Type, exact);

        // Note: SignatureTypeEquivalenceComparer compares dynamic and object the same.
        protected static bool TypesEquivalent(ITypeSymbol? oldType, ITypeSymbol? newType, bool exact)
            => (exact ? s_exactSymbolEqualityComparer : (IEqualityComparer<ITypeSymbol?>)s_runtimeSymbolEqualityComparer.SignatureTypeEquivalenceComparer).Equals(oldType, newType);

        protected static bool TypesEquivalent<T>(ImmutableArray<T> oldTypes, ImmutableArray<T> newTypes, bool exact) where T : ITypeSymbol
            => oldTypes.SequenceEqual(newTypes, exact, (x, y, exact) => TypesEquivalent(x, y, exact));

        protected static bool ParameterTypesEquivalent(IParameterSymbol oldParameter, IParameterSymbol newParameter, bool exact)
            => (exact ? s_exactSymbolEqualityComparer : s_runtimeSymbolEqualityComparer).ParameterEquivalenceComparer.Equals(oldParameter, newParameter);

        protected static bool TypeParameterConstraintsEquivalent(ITypeParameterSymbol oldParameter, ITypeParameterSymbol newParameter, bool exact)
            => TypesEquivalent(oldParameter.ConstraintTypes, newParameter.ConstraintTypes, exact) &&
               oldParameter.HasReferenceTypeConstraint == newParameter.HasReferenceTypeConstraint &&
               oldParameter.HasValueTypeConstraint == newParameter.HasValueTypeConstraint &&
               oldParameter.HasConstructorConstraint == newParameter.HasConstructorConstraint &&
               oldParameter.HasNotNullConstraint == newParameter.HasNotNullConstraint &&
               oldParameter.HasUnmanagedTypeConstraint == newParameter.HasUnmanagedTypeConstraint &&
               oldParameter.Variance == newParameter.Variance;

        protected static bool TypeParametersEquivalent(ImmutableArray<ITypeParameterSymbol> oldParameters, ImmutableArray<ITypeParameterSymbol> newParameters, bool exact)
            => oldParameters.SequenceEqual(newParameters, exact, (oldParameter, newParameter, exact) => oldParameter.Name == newParameter.Name && TypeParameterConstraintsEquivalent(oldParameter, newParameter, exact));

        protected static bool BaseTypesEquivalent(INamedTypeSymbol oldType, INamedTypeSymbol newType, bool exact)
            => TypesEquivalent(oldType.BaseType, newType.BaseType, exact) &&
               TypesEquivalent(oldType.AllInterfaces, newType.AllInterfaces, exact);

        protected static bool MemberSignaturesEquivalent(
            ISymbol? oldMember,
            ISymbol? newMember,
            Func<ImmutableArray<IParameterSymbol>, ITypeSymbol, ImmutableArray<IParameterSymbol>, ITypeSymbol, bool>? signatureComparer = null)
        {
            if (oldMember == newMember)
            {
                return true;
            }

            if (oldMember == null || newMember == null || oldMember.Kind != newMember.Kind)
            {
                return false;
            }

            signatureComparer ??= SignaturesEquivalent;

            switch (oldMember.Kind)
            {
                case SymbolKind.Field:
                    var oldField = (IFieldSymbol)oldMember;
                    var newField = (IFieldSymbol)newMember;
                    return signatureComparer(ImmutableArray<IParameterSymbol>.Empty, oldField.Type, ImmutableArray<IParameterSymbol>.Empty, newField.Type);

                case SymbolKind.Property:
                    var oldProperty = (IPropertySymbol)oldMember;
                    var newProperty = (IPropertySymbol)newMember;
                    return signatureComparer(oldProperty.Parameters, oldProperty.Type, newProperty.Parameters, newProperty.Type);

                case SymbolKind.Method:
                    var oldMethod = (IMethodSymbol)oldMember;
                    var newMethod = (IMethodSymbol)newMember;
                    return signatureComparer(oldMethod.Parameters, oldMethod.ReturnType, newMethod.Parameters, newMethod.ReturnType);

                default:
                    throw ExceptionUtilities.UnexpectedValue(oldMember.Kind);
            }
        }

        /// <summary>
        /// Aggregates information needed to emit updates of constructors that contain member initialization.
        /// </summary>
        private sealed class MemberInitializationUpdates(INamedTypeSymbol oldType)
        {
            public readonly INamedTypeSymbol OldType = oldType;

            /// <summary>
            /// Contains syntax maps for all changed data member initializers or constructor declarations (of constructors emitting initializers)
            /// in the currently analyzed document. The key is the new declaration of the member.
            /// </summary>
            public readonly Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode?>?> ChangedDeclarations = new Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode?>?>();

            /// <summary>
            /// True if a member initializer has been deleted
            /// (<see cref="ChangedDeclarations"/> only contains syntax nodes of new declarations, which are not available for deleted members).
            /// </summary>
            public bool HasDeletedMemberInitializer;
        }

        private async Task<ImmutableArray<SemanticEditInfo>> AnalyzeSemanticsAsync(
            EditScript<SyntaxNode> editScript,
            IReadOnlyDictionary<SyntaxNode, EditKind> editMap,
            ImmutableArray<UnmappedActiveStatement> oldActiveStatements,
            ImmutableArray<LinePositionSpan> newActiveStatementSpans,
            IReadOnlyList<(SyntaxNode OldNode, SyntaxNode NewNode, TextSpan DiagnosticSpan)> triviaEdits,
            Project oldProject,
            Document? oldDocument,
            Document newDocument,
            SourceText newText,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            ImmutableArray<ActiveStatement>.Builder newActiveStatements,
            ImmutableArray<ImmutableArray<SourceFileSpan>>.Builder newExceptionRegions,
            EditAndContinueCapabilitiesGrantor capabilities,
            bool inBreakState,
            CancellationToken cancellationToken)
        {
            Debug.Assert(inBreakState || newActiveStatementSpans.IsEmpty);

            if (editScript.Edits.Length == 0 && triviaEdits.Count == 0)
            {
                return ImmutableArray<SemanticEditInfo>.Empty;
            }

            // { new type -> constructor update }
            PooledDictionary<INamedTypeSymbol, MemberInitializationUpdates>? instanceConstructorEdits = null;
            PooledDictionary<INamedTypeSymbol, MemberInitializationUpdates>? staticConstructorEdits = null;

            using var _1 = PooledHashSet<ISymbol>.GetInstance(out var processedSymbols);
            using var _2 = ArrayBuilder<SemanticEditInfo>.GetInstance(out var semanticEdits);

            try
            {
                var oldModel = (oldDocument != null) ? await oldDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false) : null;
                var newModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var oldCompilation = oldModel?.Compilation ?? await oldProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var newCompilation = newModel.Compilation;

                INamedTypeSymbol? lazyLayoutAttribute = null;

                foreach (var edit in editScript.Edits)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Debug.Assert(edit.OldNode is null || edit.NewNode is null || IsNamespaceDeclaration(edit.OldNode) == IsNamespaceDeclaration(edit.NewNode));

                    // We can ignore namespace nodes in newly added documents (old model is not available) since 
                    // all newly added types in these namespaces will have their own syntax edit.
                    var symbolEdits = oldModel != null && IsNamespaceDeclaration(edit.OldNode ?? edit.NewNode!)
                        ? OneOrMany.Create(GetNamespaceSymbolEdits(oldModel, newModel, cancellationToken))
                        : GetSymbolEdits(edit.Kind, edit.OldNode, edit.NewNode, oldModel, newModel, editMap, cancellationToken);

                    foreach (var symbolEdit in symbolEdits)
                    {
                        Func<SyntaxNode, SyntaxNode?>? syntaxMap;
                        SemanticEditKind editKind;

                        var (oldSymbol, newSymbol, syntacticEditKind) = symbolEdit;
                        var symbol = newSymbol ?? oldSymbol;
                        Contract.ThrowIfNull(symbol);

                        if (!processedSymbols.Add(symbol))
                        {
                            continue;
                        }

                        if (syntacticEditKind == EditKind.Move)
                        {
                            Debug.Assert(oldSymbol is INamedTypeSymbol);
                            Debug.Assert(newSymbol is INamedTypeSymbol);

                            var oldSymbolInNewCompilation = SymbolKey.Create(oldSymbol, cancellationToken).Resolve(newCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;
                            var newSymbolInOldCompilation = SymbolKey.Create(newSymbol, cancellationToken).Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;

                            if (oldSymbolInNewCompilation == null || newSymbolInOldCompilation == null)
                            {
                                if (TypesEquivalent(oldSymbol.ContainingType, newSymbol.ContainingType, exact: false) &&
                                    !SymbolsEquivalent(oldSymbol.ContainingNamespace, newSymbol.ContainingNamespace))
                                {
                                    // pick the first declaration in the new file that contains the namespace change:
                                    var newTypeDeclaration = GetSymbolDeclarationSyntax(newSymbol, refs => refs.First(r => r.SyntaxTree == edit.NewNode!.SyntaxTree), cancellationToken);

                                    diagnostics.Add(new RudeEditDiagnostic(
                                        RudeEditKind.ChangingNamespace,
                                        GetDiagnosticSpan(newTypeDeclaration, EditKind.Update),
                                        newTypeDeclaration,
                                        new[] { GetDisplayName(newTypeDeclaration), oldSymbol.ContainingNamespace.ToDisplayString(), newSymbol.ContainingNamespace.ToDisplayString() }));
                                }
                                else
                                {
                                    ReportUpdateRudeEdit(diagnostics, RudeEditKind.Move, edit.NewNode!);
                                }
                            }

                            continue;
                        }

                        var symbolKey = SymbolKey.Create(symbol, cancellationToken);

                        // Ignore ambiguous resolution result - it may happen if there are semantic errors in the compilation.
                        oldSymbol ??= symbolKey.Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;
                        newSymbol ??= symbolKey.Resolve(newCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;

                        var (oldDeclaration, newDeclaration) = GetSymbolDeclarationNodes(oldSymbol, newSymbol, edit.OldNode, edit.NewNode);

                        // The syntax change implies an update of the associated symbol but the old/new symbol does not actually exist.
                        // Treat the edit as Insert/Delete. This may happen e.g. when all C# global statements are removed, the first one is added or they are moved to another file.
                        if (syntacticEditKind == EditKind.Update)
                        {
                            if (oldSymbol == null || oldDeclaration == null || oldDeclaration != null && oldDeclaration.SyntaxTree != oldModel?.SyntaxTree)
                            {
                                syntacticEditKind = EditKind.Insert;
                            }
                            else if (newSymbol == null || newDeclaration == null || newDeclaration != null && newDeclaration.SyntaxTree != newModel.SyntaxTree)
                            {
                                syntacticEditKind = EditKind.Delete;
                            }
                        }

                        if (!inBreakState)
                        {
                            // Delete/insert/update edit of a member of a reloadable type (including nested types) results in Replace edit of the containing type.
                            // If a Delete edit is part of delete-insert operation (member moved to a different partial type declaration or to a different file)
                            // skip producing Replace semantic edit for this Delete edit as one will be reported by the corresponding Insert edit.

                            var oldContainingType = oldSymbol?.ContainingType;
                            var newContainingType = newSymbol?.ContainingType;
                            var containingType = newContainingType ?? oldContainingType;

                            if (containingType != null && (syntacticEditKind != EditKind.Delete || newSymbol == null))
                            {
                                var containingTypeSymbolKey = SymbolKey.Create(containingType, cancellationToken);
                                oldContainingType ??= (INamedTypeSymbol?)containingTypeSymbolKey.Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;
                                newContainingType ??= (INamedTypeSymbol?)containingTypeSymbolKey.Resolve(newCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;

                                if (oldContainingType != null && newContainingType != null && IsReloadable(oldContainingType))
                                {
                                    if (processedSymbols.Add(newContainingType))
                                    {
                                        if (capabilities.Grant(EditAndContinueCapabilities.NewTypeDefinition))
                                        {
                                            semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Replace, containingTypeSymbolKey, syntaxMap: null, syntaxMapTree: null,
                                                IsPartialEdit(oldContainingType, newContainingType, editScript.Match.OldRoot.SyntaxTree, editScript.Match.NewRoot.SyntaxTree) ? containingTypeSymbolKey : null));
                                        }
                                        else
                                        {
                                            ReportUpdateRudeEdit(diagnostics, RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime, newContainingType, newDeclaration, cancellationToken);
                                        }
                                    }

                                    continue;
                                }
                            }

                            var oldType = oldSymbol as INamedTypeSymbol;
                            var newType = newSymbol as INamedTypeSymbol;

                            // Deleting a reloadable type is a rude edit, reported the same as for non-reloadable.
                            // Adding a reloadable type is a standard type addition (TODO: unless added to a reloadable type?).
                            // Making reloadable attribute non-reloadable results in a new version of the type that is
                            // not reloadable but does not update the old version in-place.
                            if (syntacticEditKind != EditKind.Delete && oldType != null && newType != null && IsReloadable(oldType))
                            {
                                if (symbol == newType || processedSymbols.Add(newType))
                                {
                                    if (oldType.Name != newType.Name)
                                    {
                                        // https://github.com/dotnet/roslyn/issues/54886
                                        ReportUpdateRudeEdit(diagnostics, RudeEditKind.Renamed, newType, newDeclaration, cancellationToken);
                                    }
                                    else if (oldType.Arity != newType.Arity)
                                    {
                                        // https://github.com/dotnet/roslyn/issues/54881
                                        ReportUpdateRudeEdit(diagnostics, RudeEditKind.ChangingTypeParameters, newType, newDeclaration, cancellationToken);
                                    }
                                    else if (!capabilities.Grant(EditAndContinueCapabilities.NewTypeDefinition))
                                    {
                                        ReportUpdateRudeEdit(diagnostics, RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime, newType, newDeclaration, cancellationToken);
                                    }
                                    else
                                    {
                                        semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Replace, symbolKey, syntaxMap: null, syntaxMapTree: null,
                                            IsPartialEdit(oldType, newType, editScript.Match.OldRoot.SyntaxTree, editScript.Match.NewRoot.SyntaxTree) ? symbolKey : null));
                                    }
                                }

                                continue;
                            }
                        }

                        switch (syntacticEditKind)
                        {
                            case EditKind.Delete:
                                {
                                    Contract.ThrowIfNull(oldModel);
                                    Contract.ThrowIfNull(oldSymbol);
                                    Contract.ThrowIfNull(oldDeclaration);

                                    syntaxMap = null;
                                    editKind = SemanticEditKind.Delete;

                                    ReportDeletedMemberActiveStatementsRudeEdits();

                                    // Check if the declaration has been moved from one document to another.
                                    if (newSymbol is { } and not IMethodSymbol { IsPartialDefinition: true })
                                    {
                                        // Symbol has actually not been deleted but rather moved to another document, another partial type declaration
                                        // or replaced with an implicitly generated one (e.g. parameterless constructor, auto-generated record methods, etc.)

                                        // Ignore the delete if there is going to be an insert corresponding to the new symbol that will create an update edit.
                                        if (HasInsertMatchingDelete(newSymbol, oldCompilation, cancellationToken))
                                        {
                                            continue;
                                        }

                                        if (IsDeclarationWithInitializer(oldDeclaration))
                                        {
                                            // Note initializer deletion.
                                            DeferConstructorEdit(oldSymbol.ContainingType, newSymbol.ContainingType, newDeclaration: null, syntaxMap, oldSymbol.IsStatic, isMemberWithDeletedInitializer: true);
                                        }

                                        if (IsConstructorWithMemberInitializers(oldSymbol, cancellationToken))
                                        {
                                            // If a constructor is deleted and replaced by an implicit one the update needs to aggregate updates (syntax maps) of all data member initializers.
                                            DeferConstructorEdit(oldSymbol.ContainingType, newSymbol.ContainingType, newDeclaration: null, syntaxMap, oldSymbol.IsStatic, isMemberWithDeletedInitializer: false);

                                            processedSymbols.Remove(oldSymbol);
                                            continue;
                                        }

                                        if (oldSymbol.ContainingType.IsRecord &&
                                            newSymbol.ContainingType.IsRecord &&
                                            newSymbol is IPropertySymbol newProperty &&
                                            IsPrimaryConstructorParameterMatchingSymbol(newSymbol, cancellationToken))
                                        {
                                            AnalyzeRecordPropertyReplacement((IPropertySymbol)oldSymbol, newProperty, isDeleteEdit: true);
                                            editKind = SemanticEditKind.Update;
                                            break;
                                        }

                                        // there is no insert edit for an implicit declaration, therefore we need to issue an update:
                                        editKind = SemanticEditKind.Update;
                                        break;
                                    }

                                    var diagnosticSpan = GetDeletedNodeDiagnosticSpan(editScript.Match.Matches, oldDeclaration);

                                    // If we got here for a global statement then the actual edit is a delete of the synthesized Main method
                                    if (IsGlobalMain(oldSymbol))
                                    {
                                        diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.Delete, diagnosticSpan, edit.OldNode, new[] { GetDisplayName(edit.OldNode!, EditKind.Delete) }));
                                        continue;
                                    }

                                    var rudeEditKind = RudeEditKind.Delete;

                                    // If the associated member declaration (parameter/type parameter -> method) has also been deleted skip
                                    // the delete of the symbol as it will be deleted by the delete of the associated member. We pass the edit kind
                                    // in here to avoid property/event accessors from being caught up in this, because those deletes we want to process
                                    // separately, below.
                                    //
                                    // Associated member declarations must be in the same document as the symbol, so we don't need to resolve their symbol.
                                    // In some cases the symbol even can't be resolved unambiguously. Consider e.g. resolving a method with its parameter deleted -
                                    // we wouldn't know which overload to resolve to.
                                    if (TryGetAssociatedMemberDeclaration(oldDeclaration, EditKind.Delete, out var oldAssociatedMemberDeclaration))
                                    {
                                        if (HasEdit(editMap, oldAssociatedMemberDeclaration, EditKind.Delete))
                                        {
                                            continue;
                                        }

                                        // We allow deleting parameters, by issuing delete and insert edits for the old and new method
                                        if (oldSymbol is IParameterSymbol oldParameter)
                                        {
                                            if (TryAddParameterInsertOrDeleteEdits(semanticEdits, oldParameter, oldModel, newModel, capabilities, syntaxMap, editScript, processedSymbols, cancellationToken, out var notSupportedByRuntime))
                                            {
                                                continue;
                                            }

                                            if (notSupportedByRuntime)
                                            {
                                                rudeEditKind = RudeEditKind.DeleteNotSupportedByRuntime;
                                            }
                                        }

                                        // deleting is not allowed

                                        diagnostics.Add(new RudeEditDiagnostic(
                                            rudeEditKind,
                                            diagnosticSpan,
                                            oldDeclaration,
                                            new[] { GetDisplayKindAndName(oldSymbol, GetDisplayName(oldDeclaration, EditKind.Delete), fullyQualify: diagnosticSpan.IsEmpty) }));

                                        continue;
                                    }

                                    if (oldSymbol.ContainingType == null)
                                    {
                                        // deleting type is not allowed
                                        Debug.Assert(oldSymbol is INamedTypeSymbol);

                                        diagnostics.Add(new RudeEditDiagnostic(
                                            rudeEditKind,
                                            diagnosticSpan,
                                            oldDeclaration,
                                            new[] { GetDisplayKindAndName(oldSymbol, GetDisplayName(oldDeclaration, EditKind.Delete), fullyQualify: diagnosticSpan.IsEmpty) }));

                                        continue;
                                    }

                                    // Check if the symbol being deleted is a member of a type that's also being deleted.
                                    // If so, skip the member deletion and only report the containing symbol deletion.
                                    var containingTypeKey = SymbolKey.Create(oldSymbol.ContainingType, cancellationToken);
                                    var newContainingType = (INamedTypeSymbol?)containingTypeKey.Resolve(newCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;
                                    if (newContainingType == null)
                                    {
                                        continue;
                                    }

                                    if (!AllowsDeletion(oldSymbol))
                                    {
                                        diagnostics.Add(new RudeEditDiagnostic(
                                            rudeEditKind,
                                            diagnosticSpan,
                                            oldDeclaration,
                                            new[] { GetDisplayKindAndName(oldSymbol, GetDisplayName(oldDeclaration, EditKind.Delete), fullyQualify: diagnosticSpan.IsEmpty) }));

                                        continue;
                                    }

                                    if (IsDeclarationWithInitializer(oldDeclaration))
                                    {
                                        DeferConstructorEdit(oldSymbol.ContainingType, newContainingType, oldDeclaration, syntaxMap, oldSymbol.IsStatic, isMemberWithDeletedInitializer: true);
                                    }

                                    // If a property or field is deleted from a record the synthesized members may change
                                    // (PrintMembers print all properties and fields, Equals and GHC compare all data members, etc.)
                                    if (SymbolPresenceAffectsSynthesizedRecordMembers(oldSymbol))
                                    {
                                        // If the deleted member has been replaced by another member (of a different kind, otherwise newSymbol would be non-null) of the same name
                                        // we should not update record members as they will be updated by an insertion edit of the other member.
                                        // An insert edit must exist for the other member, otherwise we would have two members in the old type of the same name but different kind (field/property).
                                        var newMatchingSymbol = newContainingType.GetMembers(oldSymbol.Name).FirstOrDefault(m => m is IPropertySymbol or IFieldSymbol);
                                        if (newMatchingSymbol is null)
                                        {
                                            AddSynthesizedRecordMethodUpdatesForPropertyChange(semanticEdits, newModel.Compilation, newContainingType, cancellationToken);
                                        }
                                    }

                                    AddDeleteEditsForMemberAndAccessors(semanticEdits, oldSymbol, containingTypeKey, syntaxMap, partialType: null, cancellationToken);

                                    // Note: Delete of a constructor does not need to be deferred since it does not affect other constructors.
                                    // We do need to handle deletion of a primary record constructor though.
                                    if (oldSymbol.ContainingType.IsRecord && IsPrimaryConstructor(oldSymbol, cancellationToken))
                                    {
                                        var oldPrimaryConstructor = (IMethodSymbol)oldSymbol;

                                        // Deconstructor delete:
                                        AddDeconstructorEdits(semanticEdits, oldPrimaryConstructor, otherConstructor: null, containingTypeKey, oldCompilation, newCompilation, syntaxMap: null, processedSymbols, isParameterDelete: true, cancellationToken);

                                        // Synthesized method updates:
                                        AddSynthesizedRecordMethodUpdatesForPropertyChange(semanticEdits, newModel.Compilation, newContainingType, cancellationToken);
                                    }

                                    continue;
                                }

                            case EditKind.Insert:
                                {
                                    Contract.ThrowIfNull(newModel);
                                    Contract.ThrowIfNull(newSymbol);
                                    Contract.ThrowIfNull(newDeclaration);

                                    syntaxMap = null;

                                    editKind = SemanticEditKind.Insert;
                                    INamedTypeSymbol? oldContainingType;
                                    var newContainingType = newSymbol.ContainingType;

                                    // Check if the declaration has been moved from one document to another.
                                    if (oldSymbol != null)
                                    {
                                        editKind = SemanticEditKind.Update;

                                        // Symbol has actually not been inserted but rather moved between documents or partial type declarations,
                                        // or is replacing an implicitly generated one (e.g. parameterless constructor, auto-generated record methods, etc.)
                                        oldContainingType = oldSymbol.ContainingType;

                                        if (oldSymbol is IPropertySymbol { ContainingType.IsRecord: true, GetMethod.IsImplicitlyDeclared: true, SetMethod.IsImplicitlyDeclared: true } oldRecordProperty &&
                                            IsPrimaryConstructorParameterMatchingSymbol(oldSymbol, cancellationToken))
                                        {
                                            AnalyzeRecordPropertyReplacement(oldRecordProperty, (IPropertySymbol)newSymbol, isDeleteEdit: false);
                                            break;
                                        }

                                        // When a method is moved to a different declaration and its parameters are changed at the same time
                                        // the new method symbol key will not resolve to the old one since the parameters are different.
                                        // As a result we will report separate delete and insert rude edits.
                                        //
                                        // For delegates, however, the symbol key will resolve to the old type so we need to report
                                        // rude edits here.
                                        if (oldSymbol is INamedTypeSymbol { DelegateInvokeMethod: not null and var oldDelegateInvoke } &&
                                            newSymbol is INamedTypeSymbol { DelegateInvokeMethod: not null and var newDelegateInvoke })
                                        {
                                            if (!ParameterTypesEquivalent(oldDelegateInvoke.Parameters, newDelegateInvoke.Parameters, exact: false))
                                            {
                                                ReportUpdateRudeEdit(diagnostics, RudeEditKind.ChangingParameterTypes, newSymbol, newDeclaration, cancellationToken);
                                            }
                                        }

                                        if (oldSymbol.DeclaringSyntaxReferences.Length == 1 && newSymbol.DeclaringSyntaxReferences.Length == 1)
                                        {
                                            Contract.ThrowIfNull(oldDeclaration);

                                            // Handles partial methods and explicitly implemented properties that implement positional parameters of records

                                            // TODO: update, should this require declaring syntax refs? https://github.com/dotnet/roslyn/issues/68738
                                            // We ignore partial method definition parts when processing edits (GetSymbolForEdit).
                                            // The only declaration in compilation without syntax errors that can have multiple declaring references is a type declaration.
                                            // We can therefore ignore any symbols that have more than one declaration.
                                            ReportTypeLayoutUpdateRudeEdits(diagnostics, newSymbol, newDeclaration, newModel, ref lazyLayoutAttribute);

                                            var oldBody = TryGetDeclarationBody(oldDeclaration);
                                            if (oldBody != null)
                                            {
                                                // The old symbol's declaration syntax may be located in a different document than the old version of the current document.
                                                var oldSyntaxDocument = oldProject.Solution.GetRequiredDocument(oldDeclaration.SyntaxTree);
                                                var oldSyntaxModel = await oldSyntaxDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                                                var oldSyntaxText = await oldSyntaxDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                                                var newBody = TryGetDeclarationBody(newDeclaration);

                                                // Skip analysis of active statements. We already report rude edit for removal of code containing
                                                // active statements in the old declaration and don't currently support moving active statements.
                                                AnalyzeChangedMemberBody(
                                                    oldDeclaration,
                                                    newDeclaration,
                                                    oldBody,
                                                    newBody,
                                                    oldSyntaxModel,
                                                    newModel,
                                                    oldSymbol,
                                                    newSymbol,
                                                    newText,
                                                    oldActiveStatements: ImmutableArray<UnmappedActiveStatement>.Empty,
                                                    newActiveStatementSpans: ImmutableArray<LinePositionSpan>.Empty,
                                                    capabilities: capabilities,
                                                    newActiveStatements,
                                                    newExceptionRegions,
                                                    diagnostics,
                                                    out syntaxMap,
                                                    cancellationToken);
                                            }

                                            // If a constructor changes from including initializers to not including initializers
                                            // we don't need to aggregate syntax map from all initializers for the constructor update semantic edit.
                                            var isNewConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(newSymbol, cancellationToken);
                                            var isDeclarationWithInitializer = IsDeclarationWithInitializer(oldDeclaration) || IsDeclarationWithInitializer(newDeclaration);

                                            if (isNewConstructorWithMemberInitializers || isDeclarationWithInitializer)
                                            {
                                                if (isNewConstructorWithMemberInitializers)
                                                {
                                                    processedSymbols.Remove(newSymbol);
                                                }

                                                if (isDeclarationWithInitializer)
                                                {
                                                    AnalyzeSymbolUpdate(oldSymbol, newSymbol, edit.NewNode, newModel, editScript.Match, capabilities, diagnostics, semanticEdits, syntaxMap, processedSymbols, cancellationToken);
                                                }

                                                DeferConstructorEdit(oldSymbol.ContainingType, newContainingType, newDeclaration, syntaxMap, newSymbol.IsStatic,
                                                    isMemberWithDeletedInitializer: IsDeclarationWithInitializer(oldDeclaration) && !IsDeclarationWithInitializer(newDeclaration));

                                                // Don't add a separate semantic edit.
                                                // Updates of data members with initializers and constructors that emit initializers will be aggregated and added later.
                                                continue;
                                            }
                                        }
                                    }
                                    else if (TryGetAssociatedMemberDeclaration(newDeclaration, EditKind.Insert, out var newAssociatedMemberDeclaration) &&
                                             HasEdit(editMap, newAssociatedMemberDeclaration, EditKind.Insert))
                                    {
                                        // If the symbol is an accessor and the containing property/indexer/event declaration has also been inserted
                                        // the insert of the accessor as it will be inserted by the property/indexer/event.
                                        // Similarly for (type) parameters and their containing symbol.
                                        continue;
                                    }
                                    else if (newSymbol is ITypeParameterSymbol)
                                    {
                                        diagnostics.Add(new RudeEditDiagnostic(
                                            RudeEditKind.Insert,
                                            GetDiagnosticSpan(newDeclaration, EditKind.Insert),
                                            newDeclaration,
                                            arguments: new[] { GetDisplayName(newDeclaration, EditKind.Insert) }));

                                        continue;
                                    }
                                    else if (newSymbol is IParameterSymbol newParameter)
                                    {
                                        if (!TryAddParameterInsertOrDeleteEdits(semanticEdits, newParameter, newModel, oldModel, capabilities, syntaxMap, editScript, processedSymbols, cancellationToken, out var notSupportedByRuntime))
                                        {
                                            diagnostics.Add(new RudeEditDiagnostic(
                                               notSupportedByRuntime ? RudeEditKind.InsertNotSupportedByRuntime : RudeEditKind.Insert,
                                               GetDiagnosticSpan(newDeclaration, EditKind.Insert),
                                               newDeclaration,
                                               arguments: new[] { GetDisplayName(newDeclaration, EditKind.Insert) }));
                                        }

                                        continue;
                                    }
                                    else if (newContainingType != null && !IsGlobalMain(newSymbol))
                                    {
                                        // The edit actually adds a new symbol into an existing or a new type.

                                        var containingSymbolKey = SymbolKey.Create(newContainingType, cancellationToken);
                                        oldContainingType = containingSymbolKey.Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol as INamedTypeSymbol;

                                        if (oldContainingType != null && !CanAddNewMemberToExistingType(newSymbol, capabilities, cancellationToken))
                                        {
                                            diagnostics.Add(new RudeEditDiagnostic(
                                                RudeEditKind.InsertNotSupportedByRuntime,
                                                GetDiagnosticSpan(newDeclaration, EditKind.Insert),
                                                newDeclaration,
                                                arguments: new[] { GetDisplayName(newDeclaration, EditKind.Insert) }));
                                        }

                                        // Check rude edits for each member even if it is inserted into a new type.
                                        ReportInsertedMemberSymbolRudeEdits(diagnostics, newSymbol, newDeclaration, insertingIntoExistingContainingType: oldContainingType != null);

                                        if (oldContainingType == null)
                                        {
                                            // Insertion of a new symbol into a new type.
                                            // We'll produce a single insert edit for the entire type.
                                            continue;
                                        }

                                        // Report rude edits for changes to data member changes of a type with an explicit layout.
                                        // We disallow moving a data member of a partial type with explicit layout even when it actually does not change the layout.
                                        // We could compare the exact order of the members but the scenario is unlikely to occur.
                                        ReportTypeLayoutUpdateRudeEdits(diagnostics, newSymbol, newDeclaration, newModel, ref lazyLayoutAttribute);

                                        // If a property or field is inserted into a record the synthesized members may change
                                        // (PrintMembers print all properties and fields, Equals and GHC compare all data members, etc.)
                                        if (SymbolPresenceAffectsSynthesizedRecordMembers(newSymbol))
                                        {
                                            AddSynthesizedRecordMethodUpdatesForPropertyChange(semanticEdits, newCompilation, newContainingType, cancellationToken);
                                        }
                                    }
                                    else
                                    {
                                        // adds a new top-level type, or a global statement where none existed before, which is
                                        // therefore inserting the <Program>$ type
                                        Contract.ThrowIfFalse(newSymbol is INamedTypeSymbol || IsGlobalMain(newSymbol));

                                        if (!capabilities.Grant(EditAndContinueCapabilities.NewTypeDefinition))
                                        {
                                            diagnostics.Add(new RudeEditDiagnostic(
                                                RudeEditKind.InsertNotSupportedByRuntime,
                                                GetDiagnosticSpan(newDeclaration, EditKind.Insert),
                                                newDeclaration,
                                                arguments: new[] { GetDisplayName(newDeclaration, EditKind.Insert) }));
                                        }

                                        oldContainingType = null;
                                        ReportInsertedMemberSymbolRudeEdits(diagnostics, newSymbol, newDeclaration, insertingIntoExistingContainingType: false);
                                    }

                                    var isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(newSymbol, cancellationToken);
                                    if (isConstructorWithMemberInitializers || IsDeclarationWithInitializer(newDeclaration))
                                    {
                                        Contract.ThrowIfNull(newContainingType);
                                        Contract.ThrowIfNull(oldContainingType);

                                        // TODO (bug https://github.com/dotnet/roslyn/issues/2504)
                                        if (isConstructorWithMemberInitializers &&
                                            editKind == SemanticEditKind.Insert &&
                                            IsPartial(newContainingType) &&
                                            HasMemberInitializerContainingLambda(oldContainingType, newSymbol.IsStatic, cancellationToken))
                                        {
                                            // rude edit: Adding a constructor to a type with a field or property initializer that contains an anonymous function
                                            diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, GetDiagnosticSpan(newDeclaration, EditKind.Insert)));
                                            break;
                                        }

                                        DeferConstructorEdit(oldContainingType, newContainingType, newDeclaration, syntaxMap, newSymbol.IsStatic, isMemberWithDeletedInitializer: false);

                                        if (isConstructorWithMemberInitializers)
                                        {
                                            processedSymbols.Remove(newSymbol);
                                        }

                                        if (isConstructorWithMemberInitializers || editKind == SemanticEditKind.Update)
                                        {
                                            // Don't add a separate semantic edit.
                                            // Edits of data members with initializers and constructors that emit initializers will be aggregated and added later.
                                            continue;
                                        }

                                        // A semantic edit to create the field/property is gonna be added.
                                        Contract.ThrowIfFalse(editKind == SemanticEditKind.Insert);
                                    }
                                }

                                break;

                            case EditKind.Update:
                                {
                                    Contract.ThrowIfNull(oldModel);
                                    Contract.ThrowIfNull(newModel);
                                    Contract.ThrowIfNull(oldSymbol);
                                    Contract.ThrowIfNull(newSymbol);

                                    editKind = SemanticEditKind.Update;
                                    syntaxMap = null;

                                    // Partial type declarations and their type parameters.
                                    if (oldSymbol.DeclaringSyntaxReferences.Length != 1 && newSymbol.DeclaringSyntaxReferences.Length != 1)
                                    {
                                        break;
                                    }

                                    Contract.ThrowIfNull(oldDeclaration);
                                    Contract.ThrowIfNull(newDeclaration);

                                    var oldBody = TryGetDeclarationBody(oldDeclaration);
                                    if (oldBody != null)
                                    {
                                        var newBody = TryGetDeclarationBody(newDeclaration);

                                        AnalyzeChangedMemberBody(
                                            oldDeclaration,
                                            newDeclaration,
                                            oldBody,
                                            newBody,
                                            oldModel,
                                            newModel,
                                            oldSymbol,
                                            newSymbol,
                                            newText,
                                            oldActiveStatements,
                                            newActiveStatementSpans,
                                            capabilities,
                                            newActiveStatements,
                                            newExceptionRegions,
                                            diagnostics,
                                            out syntaxMap,
                                            cancellationToken);
                                    }

                                    // If a constructor changes from including initializers to not including initializers
                                    // we don't need to aggregate syntax map from all initializers for the constructor update semantic edit.
                                    var isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(newSymbol, cancellationToken);
                                    var isDeclarationWithInitializer = IsDeclarationWithInitializer(oldDeclaration) || IsDeclarationWithInitializer(newDeclaration);

                                    if (isConstructorWithMemberInitializers || isDeclarationWithInitializer)
                                    {
                                        if (isConstructorWithMemberInitializers)
                                        {
                                            processedSymbols.Remove(newSymbol);
                                        }

                                        if (isDeclarationWithInitializer)
                                        {
                                            AnalyzeSymbolUpdate(oldSymbol, newSymbol, edit.NewNode, newModel, editScript.Match, capabilities, diagnostics, semanticEdits, syntaxMap, processedSymbols, cancellationToken);
                                        }

                                        DeferConstructorEdit(oldSymbol.ContainingType, newSymbol.ContainingType, newDeclaration, syntaxMap, newSymbol.IsStatic,
                                            isMemberWithDeletedInitializer: IsDeclarationWithInitializer(oldDeclaration) && !IsDeclarationWithInitializer(newDeclaration));

                                        // Don't add a separate semantic edit.
                                        // Updates of data members with initializers and constructors that emit initializers will be aggregated and added later.
                                        continue;
                                    }
                                }

                                break;

                            default:
                                throw ExceptionUtilities.UnexpectedValue(edit.Kind);
                        }

                        void AnalyzeRecordPropertyReplacement(IPropertySymbol oldProperty, IPropertySymbol newProperty, bool isDeleteEdit)
                        {
                            Debug.Assert(newDeclaration != null);
                            Debug.Assert(oldProperty.ContainingType.IsRecord);
                            Debug.Assert(newProperty.ContainingType.IsRecord);

                            // The synthesized auto-property is `T P { get; init; } = P`.
                            // If the initializer is different from `P` the primary constructor needs to be updated.
                            // Note: we update the constructor regardless of the initializer exact shape, but we could check for it.
                            DeferConstructorEdit(oldProperty.ContainingType, newProperty.ContainingType, newDeclaration: null, syntaxMap, oldProperty.IsStatic, isMemberWithDeletedInitializer: true);

                            var (customProperty, synthesizedProperty) = isDeleteEdit ? (oldProperty, newProperty) : (newProperty, oldProperty);

                            // new property is synthesized
                            Debug.Assert(synthesizedProperty.SetMethod is { IsInitOnly: true });

                            if (customProperty.SetMethod == null)
                            {
                                // Custom read-only property replaced with synthesized auto-property
                                if (isDeleteEdit)
                                {
                                    AddInsertEditsForMemberAndAccessors(semanticEdits, synthesizedProperty.SetMethod, syntaxMap, partialType: null, processedSymbols, cancellationToken);
                                }
                                else
                                {
                                    AddDeleteEditsForMemberAndAccessors(semanticEdits, synthesizedProperty.SetMethod, SymbolKey.Create(oldProperty.ContainingType, cancellationToken), syntaxMap, partialType: null, cancellationToken);
                                }
                            }

                            // The synthesized property replacing the deleted one will be an auto-property.
                            // If the accessor had body or the property changed accessibility then synthesized record members might be affected.
                            AddSynthesizedRecordMethodUpdatesForPropertyChange(semanticEdits, newCompilation, newProperty.ContainingType, cancellationToken);
                        }

                        void ReportDeletedMemberActiveStatementsRudeEdits()
                        {
                            Contract.ThrowIfNull(oldDeclaration);
                            Contract.ThrowIfNull(oldSymbol);

                            var oldBody = TryGetDeclarationBody(oldDeclaration);
                            if (oldBody == null)
                            {
                                return;
                            }

                            var activeStatementIndices = GetOverlappingActiveStatements(oldBody, oldActiveStatements);
                            if (!activeStatementIndices.Any())
                            {
                                return;
                            }

                            // A delete edit might be created for a symbol whose declaration just moved within the same syntax tree.
                            // An active statement in a body of such symbol will be updated when the body change is analyzed and
                            // should not be deleted here.
                            //
                            // An example is a VB field declaration with multiple names:
                            //   Dim [|a|], b As New C() -> Dim [|a As New C()|]
                            // In the old compilation fields 'a' and 'b' are each represented by a modified identifier.
                            // In the new compilation field 'a' is represented by the variable declarator.
                            // Two deletes are created for this change: Delete(ModIdf('a')) and Delete(ModIdf('b')), only the second one actually deletes the field.
                            //
                            // TODO: consider supporting moving AS to a different file -- https://github.com/dotnet/roslyn/issues/51177.
                            //
                            if (newSymbol?.DeclaringSyntaxReferences.Length == 1 && newDeclaration?.SyntaxTree == newModel.SyntaxTree)
                            {
                                return;
                            }

                            var newSpan = GetDeletedDeclarationActiveSpan(editScript.Match.Matches, oldDeclaration);

                            foreach (var index in activeStatementIndices)
                            {
                                Debug.Assert(newActiveStatements[index] == null);

                                newActiveStatements[index] = GetActiveStatementWithSpan(oldActiveStatements[index], editScript.Match.NewRoot.SyntaxTree, newSpan, diagnostics, cancellationToken);
                                newExceptionRegions[index] = ImmutableArray<SourceFileSpan>.Empty;
                            }

                            ReportDeletedMemberRudeEdit(diagnostics, oldSymbol, newModel, RudeEditKind.DeleteActiveStatement, cancellationToken);
                        }

                        Contract.ThrowIfFalse(editKind is SemanticEditKind.Update or SemanticEditKind.Insert);

                        if (editKind == SemanticEditKind.Update)
                        {
                            Contract.ThrowIfNull(oldSymbol);

                            AnalyzeSymbolUpdate(oldSymbol, newSymbol, edit.NewNode, newModel, editScript.Match, capabilities, diagnostics, semanticEdits, syntaxMap, processedSymbols, cancellationToken);

                            if (newSymbol is INamedTypeSymbol or IFieldSymbol or IParameterSymbol or ITypeParameterSymbol)
                            {
                                continue;
                            }

                            // For renames where the symbol allows deletion, we don't create an update edit, we create a delete
                            // and an add. During emit an empty body will be created for the old name.
                            var createDeleteAndInsertEdits = oldSymbol.Name != newSymbol.Name;

                            // When a methods parameters are reordered or there is an insert or an add, we need to handle things differently
                            if (oldSymbol is IMethodSymbol oldMethod &&
                                newSymbol is IMethodSymbol newMethod)
                            {
                                // For inserts and deletes, the edits for the parameter itself will do the work
                                if (oldMethod.Parameters.Length != newMethod.Parameters.Length)
                                {
                                    continue;
                                }

                                // For reordering of parameters we need to report insert and delete edits, but we also need to account for
                                // renames if the runtime doesn't support it. We track this with a syntax node that we can use to report
                                // the rude edit.
                                IParameterSymbol? renamedParameter = null;
                                for (var i = 0; i < oldMethod.Parameters.Length; i++)
                                {
                                    var rudeEditKind = RudeEditKind.None;
                                    var hasParameterTypeChange = false;
                                    var unused = false;
                                    AnalyzeParameterType(oldMethod.Parameters[i], newMethod.Parameters[i], capabilities, ref rudeEditKind, ref unused, ref hasParameterTypeChange, cancellationToken);

                                    createDeleteAndInsertEdits |= hasParameterTypeChange;
                                    renamedParameter ??= oldMethod.Parameters[i].Name != newMethod.Parameters[i].Name ? newMethod.Parameters[i] : null;
                                }

                                if (!createDeleteAndInsertEdits && renamedParameter is not null && !capabilities.Grant(EditAndContinueCapabilities.UpdateParameters))
                                {
                                    processedSymbols.Add(renamedParameter);
                                    ReportUpdateRudeEdit(diagnostics, RudeEditKind.RenamingNotSupportedByRuntime, renamedParameter, GetRudeEditDiagnosticNode(renamedParameter, cancellationToken), cancellationToken);
                                    continue;
                                }
                            }

                            // Sometimes when members are moved between documents in partial classes, they can appear as renames,
                            // so we also check that the old symbol can't be resolved in the new compilation
                            if (createDeleteAndInsertEdits &&
                                AllowsDeletion(oldSymbol) &&
                                CanAddNewMemberToExistingType(oldSymbol, capabilities, cancellationToken) &&
                                SymbolKey.Create(oldSymbol, cancellationToken).Resolve(newCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol is null)
                            {
                                Contract.ThrowIfNull(oldDeclaration);

                                var oldBody = TryGetDeclarationBody(oldDeclaration);
                                if (oldBody != null && GetOverlappingActiveStatements(oldBody, oldActiveStatements).Any())
                                {
                                    Contract.ThrowIfNull(newDeclaration);
                                    AddRudeUpdateAroundActiveStatement(diagnostics, newDeclaration);
                                }
                                else
                                {
                                    var containingSymbolKey = SymbolKey.Create(oldSymbol.ContainingType, cancellationToken);

                                    AddDeleteEditsForMemberAndAccessors(semanticEdits, oldSymbol, containingSymbolKey, syntaxMap, partialType: null, cancellationToken);
                                    AddInsertEditsForMemberAndAccessors(semanticEdits, newSymbol, syntaxMap,
                                        partialType: IsPartialEdit(oldSymbol, newSymbol, editScript.Match.OldRoot.SyntaxTree, editScript.Match.NewRoot.SyntaxTree) ? symbolKey : null, processedSymbols,
                                        cancellationToken);
                                }

                                continue;
                            }

                            if (newSymbol is IPropertySymbol or IEventSymbol)
                            {
                                continue;
                            }
                        }

                        semanticEdits.Add(new SemanticEditInfo(editKind, symbolKey, syntaxMap, syntaxMapTree: null,
                            IsPartialEdit(oldSymbol, newSymbol, editScript.Match.OldRoot.SyntaxTree, editScript.Match.NewRoot.SyntaxTree) ? symbolKey : null));
                    }
                }

                foreach (var (oldEditNode, newEditNode, diagnosticSpan) in triviaEdits)
                {
                    Contract.ThrowIfNull(oldModel);
                    Contract.ThrowIfNull(newModel);

                    foreach (var (oldSymbol, newSymbol, editKind) in GetSymbolEdits(EditKind.Update, oldEditNode, newEditNode, oldModel, newModel, editMap, cancellationToken))
                    {
                        // Trivia edits are only calculated for member bodies and each member has a symbol.
                        Contract.ThrowIfNull(newSymbol);
                        Contract.ThrowIfNull(oldSymbol);

                        if (!processedSymbols.Add(newSymbol))
                        {
                            // symbol already processed
                            continue;
                        }

                        var (oldDeclaration, newDeclaration) = GetSymbolDeclarationNodes(oldSymbol, newSymbol, oldEditNode, newEditNode);
                        Contract.ThrowIfNull(oldDeclaration);
                        Contract.ThrowIfNull(newDeclaration);

                        var oldContainingType = oldSymbol.ContainingType;
                        var newContainingType = newSymbol.ContainingType;
                        Contract.ThrowIfNull(oldContainingType);
                        Contract.ThrowIfNull(newContainingType);

                        if (IsReloadable(oldContainingType))
                        {
                            if (processedSymbols.Add(newContainingType))
                            {
                                if (capabilities.Grant(EditAndContinueCapabilities.NewTypeDefinition))
                                {
                                    var containingTypeSymbolKey = SymbolKey.Create(oldContainingType, cancellationToken);
                                    semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Replace, containingTypeSymbolKey, syntaxMap: null, syntaxMapTree: null,
                                        IsPartialEdit(oldContainingType, newContainingType, editScript.Match.OldRoot.SyntaxTree, editScript.Match.NewRoot.SyntaxTree) ? containingTypeSymbolKey : null));
                                }
                                else
                                {
                                    ReportUpdateRudeEdit(diagnostics, RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime, newContainingType, newDeclaration, cancellationToken);
                                }
                            }

                            continue;
                        }

                        // We need to provide syntax map to the compiler if the member is active (see member update above):
                        var oldBody = TryGetDeclarationBody(oldDeclaration);
                        var newBody = TryGetDeclarationBody(newDeclaration);

                        // only trivia changed:
                        Debug.Assert(oldBody is null == newBody is null);
                        Debug.Assert(IsConstructorWithMemberInitializers(oldSymbol, cancellationToken) == IsConstructorWithMemberInitializers(newSymbol, cancellationToken));
                        Debug.Assert(IsDeclarationWithInitializer(oldDeclaration) == IsDeclarationWithInitializer(newDeclaration));

                        Func<SyntaxNode, SyntaxNode?>? syntaxMap = null;

                        if (oldBody != null)
                        {
                            Debug.Assert(newBody != null);

                            var isActiveMember =
                                GetOverlappingActiveStatements(oldBody, oldActiveStatements).Any() ||
                                IsStateMachineMethod(oldDeclaration) ||
                                ContainsLambda(oldBody);

                            syntaxMap = isActiveMember ? CreateSyntaxMapForEquivalentNodes(oldBody, newBody) : null;

                            var isConstructorWithMemberInitializers = IsConstructorWithMemberInitializers(newSymbol, cancellationToken);
                            var isDeclarationWithInitializer = IsDeclarationWithInitializer(newDeclaration);

                            if (isConstructorWithMemberInitializers || isDeclarationWithInitializer)
                            {
                                // TODO: only create syntax map if any field initializers are active/contain lambdas or this is a partial type
                                syntaxMap ??= CreateSyntaxMapForEquivalentNodes(oldBody, newBody);

                                if (isConstructorWithMemberInitializers)
                                {
                                    processedSymbols.Remove(newSymbol);
                                }

                                DeferConstructorEdit(oldContainingType, newContainingType, newDeclaration, syntaxMap, newSymbol.IsStatic, isMemberWithDeletedInitializer: false);

                                // Don't add a separate semantic edit.
                                // Updates of data members with initializers and constructors that emit initializers will be aggregated and added later.
                                continue;
                            }
                        }

                        // updating generic methods and types
                        if (InGenericContext(oldSymbol) && !capabilities.Grant(EditAndContinueCapabilities.GenericUpdateMethod))
                        {
                            diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, diagnosticSpan, newEditNode, new[] { GetDisplayName(newEditNode) }));
                            continue;
                        }

                        var symbolKey = SymbolKey.Create(newSymbol, cancellationToken);
                        semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, symbolKey, syntaxMap, syntaxMapTree: null,
                            IsPartialEdit(oldSymbol, newSymbol, editScript.Match.OldRoot.SyntaxTree, editScript.Match.NewRoot.SyntaxTree) ? symbolKey : null));
                    }
                }

                if (instanceConstructorEdits != null)
                {
                    AddConstructorEdits(
                        instanceConstructorEdits,
                        editScript.Match,
                        oldModel,
                        oldCompilation,
                        newModel,
                        processedSymbols,
                        capabilities,
                        isStatic: false,
                        semanticEdits,
                        diagnostics,
                        cancellationToken);
                }

                if (staticConstructorEdits != null)
                {
                    AddConstructorEdits(
                        staticConstructorEdits,
                        editScript.Match,
                        oldModel,
                        oldCompilation,
                        newModel,
                        processedSymbols,
                        capabilities,
                        isStatic: true,
                        semanticEdits,
                        diagnostics,
                        cancellationToken);
                }

                // Called when a body of a constructor or an initializer of a member is updated or inserted.
                // newDeclaration is the declaration node of an updated/inserted constructor or a member with an initializer,
                // or null if the constructor or member has been deleted.
                void DeferConstructorEdit(
                    INamedTypeSymbol oldType,
                    INamedTypeSymbol newType,
                    SyntaxNode? newDeclaration,
                    Func<SyntaxNode, SyntaxNode?>? syntaxMap,
                    bool isStatic,
                    bool isMemberWithDeletedInitializer)
                {
                    Dictionary<INamedTypeSymbol, MemberInitializationUpdates> constructorEdits;
                    if (isStatic)
                    {
                        constructorEdits = staticConstructorEdits ??= PooledDictionary<INamedTypeSymbol, MemberInitializationUpdates>.GetInstance();
                    }
                    else
                    {
                        constructorEdits = instanceConstructorEdits ??= PooledDictionary<INamedTypeSymbol, MemberInitializationUpdates>.GetInstance();
                    }

                    if (!constructorEdits.TryGetValue(newType, out var constructorEdit))
                    {
                        constructorEdits.Add(newType, constructorEdit = new MemberInitializationUpdates(oldType));
                    }

                    if (newDeclaration != null && !constructorEdit.ChangedDeclarations.ContainsKey(newDeclaration))
                    {
                        constructorEdit.ChangedDeclarations.Add(newDeclaration, syntaxMap);
                    }

                    constructorEdit.HasDeletedMemberInitializer |= isMemberWithDeletedInitializer;
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
            finally
            {
                instanceConstructorEdits?.Free();
                staticConstructorEdits?.Free();
            }

            return semanticEdits.Distinct(SemanticEditInfoComparer.Instance).ToImmutableArray();

            // If the symbol has a single declaring reference use its syntax node for further analysis.
            // Some syntax edits may not be directly associated with the declarations.
            // For example, in VB an update to AsNew clause of a multi-variable field declaration results in update to multiple symbols associated 
            // with the variable declaration. But we need to analyse each symbol's modified identifier separately.
            (SyntaxNode? oldDeclaration, SyntaxNode? newDeclaration) GetSymbolDeclarationNodes(ISymbol? oldSymbol, ISymbol? newSymbol, SyntaxNode? oldNode, SyntaxNode? newNode)
            {
                return (
                    (oldSymbol != null && oldSymbol.DeclaringSyntaxReferences.Length == 1) ?
                        GetSymbolDeclarationSyntax(oldSymbol, cancellationToken) : oldNode,
                    (newSymbol != null && newSymbol.DeclaringSyntaxReferences.Length == 1) ?
                        GetSymbolDeclarationSyntax(newSymbol, cancellationToken) : newNode);
            }
        }

        /// <summary>
        /// Adds a delete and insert edit for the old and new symbols that have had a parameter inserted or deleted.
        /// Not used if the symbol containing deleted/inserted parameters was itself deleted/inserted.
        /// </summary>
        /// <param name="parameterSymbol">The parameter that has been added or deleted</param>
        /// <param name="otherModel">The semantic model from the compilation without the <paramref name="parameterSymbol"/> (i.e the old compilation for insert, or new compilation for deletes)</param>
        /// <param name="notSupportedByRuntime">Whether the edit should be rejected because the runtime doesn't support inserting new methods. Otherwise a normal rude edit is appropriate.</param>
        /// <returns>Returns whether semantic edits were added, or if not then a rude edit should be created</returns>
        private bool TryAddParameterInsertOrDeleteEdits(
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            IParameterSymbol parameterSymbol,
            SemanticModel model,
            SemanticModel? otherModel,
            EditAndContinueCapabilitiesGrantor capabilities,
            Func<SyntaxNode, SyntaxNode?>? syntaxMap,
            EditScript<SyntaxNode> editScript,
            HashSet<ISymbol> processedSymbols,
            CancellationToken cancellationToken,
            out bool notSupportedByRuntime)
        {
            var member = parameterSymbol.ContainingSymbol;
            Debug.Assert(member is IPropertySymbol or IMethodSymbol);

            notSupportedByRuntime = false;

            // Since we're inserting (or deleting) a parameter node, oldSymbol (or newSymbol) would have been null,
            // and a symbol key won't map to the other compilation because the parameters are different, so we have to go back to the edit map
            // to find the declaration that contains the parameter, and its partner, and then its symbol, so we need to be sure we can get
            // to syntax, and have a semantic model to get back to symbols.
            if (otherModel is null ||
                member.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            // We can ignore parameter inserts and deletes for partial method definitions, as we'll report them on the implementation.
            // We return true here so no rude edit is raised.
            if (member is IMethodSymbol { IsPartialDefinition: true })
            {
                return true;
            }

            // We don't support delegate parameters
            if (member.ContainingType.IsDelegateType())
            {
                return false;
            }

            // Find the node that matches this declaration
            SyntaxNode otherContainingDeclaration;
            bool isParameterDelete;
            var containingDeclaration = GetSymbolDeclarationSyntax(member, cancellationToken);
            if (editScript.Match.TryGetOldNode(containingDeclaration, out var oldNode))
            {
                otherContainingDeclaration = oldNode;
                isParameterDelete = false;
            }
            else if (editScript.Match.TryGetNewNode(containingDeclaration, out var newNode))
            {
                otherContainingDeclaration = newNode;
                isParameterDelete = true;
            }
            else
            {
                return false;
            }

            // Member from the other compilation that does not have the parameter and matches the member that does.
            var otherMember = GetRequiredDeclaredSymbol(otherModel, otherContainingDeclaration, cancellationToken);
            Debug.Assert(otherMember is IPropertySymbol or IMethodSymbol);

            if (!AllowsDeletion(otherMember))
            {
                return false;
            }

            // Now we can work out which is the old and which is the new, depending on which map we found
            // the match in
            var (oldContainingSymbol, newContainingSymbol) = isParameterDelete ? (member, otherMember) : (otherMember, member);

            if (!CanRenameOrChangeSignature(oldContainingSymbol, newContainingSymbol, capabilities, cancellationToken))
            {
                notSupportedByRuntime = true;
                return false;
            }

            var containingTypeKey = SymbolKey.Create(oldContainingSymbol.ContainingType, cancellationToken);
            AddSynthesizedMemberEditsForParameterChange(semanticEdits, parameterSymbol, otherMember, containingTypeKey, model, otherModel, syntaxMap, processedSymbols, isParameterDelete, cancellationToken);

            AddDeleteAndInsertEditsForMemberAndAccessors(semanticEdits, oldContainingSymbol, newContainingSymbol, containingTypeKey, syntaxMap, processedSymbols, cancellationToken);
            return true;
        }

        /// <summary>
        /// Adds edits of synthesized members that may be affected by a <paramref name="parameterSymbol"/> change.
        /// </summary>
        /// <param name="otherMember">Is the member in the other compilation corresponding to the member whose <paramref name="parameterSymbol"/> is being changed.</param>
        private void AddSynthesizedMemberEditsForParameterChange(
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            IParameterSymbol parameterSymbol,
            ISymbol otherMember,
            SymbolKey containingTypeKey,
            SemanticModel model,
            SemanticModel otherModel,
            Func<SyntaxNode, SyntaxNode?>? syntaxMap,
            HashSet<ISymbol> processedSymbols,
            bool isParameterDelete,
            CancellationToken cancellationToken)
        {
            var member = parameterSymbol.ContainingSymbol;
            Debug.Assert(member is IPropertySymbol or IMethodSymbol);

            if (!member.ContainingType.IsRecord ||
                !otherMember.ContainingType.IsRecord ||
                !IsPrimaryConstructor(member, cancellationToken) ||
                !IsPrimaryConstructor(otherMember, cancellationToken))
            {
                return;
            }

            // Parameter deleted from or inserted into a primary constructor of a record type.
            //
            // Note that although the compiler emits auto-properties and deconstructor automatically
            // given just an insert or udpate edit of the primary constructor, it will not emit the necessary deletes.
            // We could only add delete edits and avoid adding updates and inserts. It would make the code somewhat simpler
            // but also asymetric (delete vs insert). Adding inserts and updates to the edits explicitly also avoids
            // dependency on the compiler implementation details.
            var primaryConstructor = (IMethodSymbol)member;
            var otherPrimaryConstructor = (IMethodSymbol)otherMember;

            // Delete/insert/update synthesized properties and their accessors.

            // If deleting a parameter from or inserting a parameter to primary constructor of a record
            // that does not have a corresponding synthesized property (has a custom property of field)
            // has no effect on the property.

            var synthesizedProperty = GetPropertySynthesizedForRecordPrimaryConstructorParameter(parameterSymbol);
            if (synthesizedProperty != null)
            {
                var otherMembersOfParameterName = otherMember.ContainingType.GetMembers(parameterSymbol.Name);
                if (otherMembersOfParameterName.Any(static m => m is IPropertySymbol))
                {
                    // Replace a synthesized auto-property with a custom implementation:
                    AddUpdateEditsForMemberAndAccessors(semanticEdits, synthesizedProperty, syntaxMap, partialType: null, cancellationToken);
                }
                else if (isParameterDelete)
                {
                    // Delete synthesized property:
                    AddDeleteEditsForMemberAndAccessors(semanticEdits, synthesizedProperty, deletedSymbolContainer: containingTypeKey, syntaxMap, partialType: null, cancellationToken);
                }
                else
                {
                    // Insert synthesized property:
                    AddInsertEditsForMemberAndAccessors(semanticEdits, synthesizedProperty, syntaxMap, partialType: null, processedSymbols, cancellationToken);
                }
            }

            // Deconstructor - we can add deconstructor edit for each changed parameter, they will get deduplicated.
            AddDeconstructorEdits(semanticEdits, primaryConstructor, otherPrimaryConstructor, containingTypeKey, model.Compilation, otherModel.Compilation, syntaxMap, processedSymbols, isParameterDelete, cancellationToken);

            // Synthesized method updates - we can add edits for each changed parameter, they will get deduplicated.
            AddSynthesizedRecordMethodUpdatesForPropertyChange(semanticEdits, otherModel.Compilation, primaryConstructor.ContainingType, cancellationToken);
        }

        /// <summary>
        /// Adds edits deleting/inserting deconstructor no longer matching <paramref name="constructor"/> of a record
        /// and inserting/deleting deconstructor the one that maches its signature.
        /// </summary>
        private void AddDeconstructorEdits(
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            IMethodSymbol? constructor,
            IMethodSymbol? otherConstructor,
            SymbolKey containingTypeKey,
            Compilation compilation,
            Compilation otherCompilation,
            Func<SyntaxNode, SyntaxNode?>? syntaxMap,
            HashSet<ISymbol> processedSymbols,
            bool isParameterDelete,
            CancellationToken cancellationToken)
        {
            AddEdits(constructor, otherCompilation, isParameterDelete);
            AddEdits(otherConstructor, compilation, !isParameterDelete);

            void AddEdits(IMethodSymbol? constructor, Compilation otherCompilation, bool isDelete)
            {
                if (constructor != null &&
                    IsPrimaryConstructor(constructor, cancellationToken) &&
                    constructor.GetMatchingDeconstructor() is { IsImplicitlyDeclared: true } deconstructor)
                {
                    if (SymbolKey.Create(deconstructor, cancellationToken).Resolve(otherCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol != null)
                    {
                        // Update for transition from synthesized to declared deconstructor
                        AddUpdateEditsForMemberAndAccessors(semanticEdits, deconstructor, syntaxMap, partialType: null, cancellationToken);
                    }
                    else if (isDelete)
                    {
                        // Delete synthesized deconstructor:
                        AddDeleteEditsForMemberAndAccessors(semanticEdits, deconstructor, deletedSymbolContainer: containingTypeKey, syntaxMap, partialType: null, cancellationToken);
                    }
                    else
                    {
                        // Insert synthesized deconstructor:
                        AddInsertEditsForMemberAndAccessors(semanticEdits, deconstructor, syntaxMap, partialType: null, processedSymbols, cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Returns whether or not the specified symbol can be deleted by the user. Normally deletes are a rude edit
        /// but for some kinds of symbols we allow deletes, and synthesize an update to an empty method body during
        /// emit.
        /// </summary>
        private static bool AllowsDeletion(ISymbol symbol)
        {
            // We don't currently allow deleting virtual or abstract methods, because if those are in the middle of
            // an inheritance chain then throwing a missing method exception is not expected
            if (symbol.GetSymbolModifiers() is not { IsVirtual: false, IsAbstract: false, IsOverride: false })
                return false;

            // Extern methods can't be deleted
            if (symbol.IsExtern)
                return false;

            // We don't allow deleting members from interfaces etc. only normal classes and structs
            if (symbol.ContainingType is not { TypeKind: TypeKind.Class or TypeKind.Struct })
                return false;

            // We store the containing symbol in NewSymbol of the edit for later use.
            if (symbol is IMethodSymbol
                {
                    MethodKind:
                        MethodKind.Ordinary or
                        MethodKind.Constructor or
                        MethodKind.EventAdd or
                        MethodKind.EventRemove or
                        MethodKind.EventRaise or
                        MethodKind.Conversion or
                        MethodKind.UserDefinedOperator or
                        MethodKind.PropertyGet or
                        MethodKind.PropertySet
                })
            {
                return true;
            }

            return symbol is IPropertySymbol or IEventSymbol;
        }

        /// <summary>
        /// Add <see cref="SemanticEditKind.Update"/> edit for the specified symbol and its accessors.
        /// </summary>
        private static void AddUpdateEditsForMemberAndAccessors(
            ArrayBuilder<SemanticEditInfo> semanticEdits, ISymbol symbol, Func<SyntaxNode, SyntaxNode?>? syntaxMap, SymbolKey? partialType, CancellationToken cancellationToken)
        {
            switch (symbol)
            {
                case IMethodSymbol:
                    AddUpdate(symbol);
                    break;

                case IPropertySymbol propertySymbol:
                    AddUpdate(propertySymbol);
                    AddUpdate(propertySymbol.GetMethod);
                    AddUpdate(propertySymbol.SetMethod);
                    break;

                case IEventSymbol eventSymbol:
                    AddUpdate(eventSymbol);
                    AddUpdate(eventSymbol.AddMethod);
                    AddUpdate(eventSymbol.RemoveMethod);
                    AddUpdate(eventSymbol.RaiseMethod);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }

            void AddUpdate(ISymbol? symbol)
            {
                if (symbol is null)
                    return;

                var symbolKey = SymbolKey.Create(symbol, cancellationToken);
                semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, symbolKey, syntaxMap, syntaxMapTree: null, partialType, deletedSymbolContainer: null));
            }
        }

        /// <summary>
        /// Add <see cref="SemanticEditKind.Delete"/> edit for the specified symbol and its accessors.
        /// </summary>
        private static void AddDeleteEditsForMemberAndAccessors(
            ArrayBuilder<SemanticEditInfo> semanticEdits, ISymbol oldSymbol, SymbolKey deletedSymbolContainer, Func<SyntaxNode, SyntaxNode?>? syntaxMap, SymbolKey? partialType, CancellationToken cancellationToken)
        {
            switch (oldSymbol)
            {
                case IMethodSymbol:
                    AddDelete(oldSymbol);
                    break;

                case IPropertySymbol propertySymbol:
                    // Delete accessors individually, because we actually just update them to be throwing.
                    AddDelete(propertySymbol.GetMethod);
                    AddDelete(propertySymbol.SetMethod);
                    break;

                case IEventSymbol eventSymbol:
                    // Delete accessors individually, because we actually just update them to be throwing.
                    AddDelete(eventSymbol.AddMethod);
                    AddDelete(eventSymbol.RemoveMethod);
                    AddDelete(eventSymbol.RaiseMethod);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(oldSymbol.Kind);
            }

            void AddDelete(ISymbol? symbol)
            {
                if (symbol is null)
                    return;

                var symbolKey = SymbolKey.Create(symbol, cancellationToken);
                semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Delete, symbolKey, syntaxMap, syntaxMapTree: null, partialType, deletedSymbolContainer: deletedSymbolContainer));
            }
        }

        /// <summary>
        /// Add <see cref="SemanticEditKind.Insert"/> edit for the specified symbol and its accessors.
        /// </summary>
        private static void AddInsertEditsForMemberAndAccessors(
            ArrayBuilder<SemanticEditInfo> semanticEdits, ISymbol newSymbol, Func<SyntaxNode, SyntaxNode?>? syntaxMap, SymbolKey? partialType, HashSet<ISymbol> processedSymbols, CancellationToken cancellationToken)
        {
            switch (newSymbol)
            {
                case IMethodSymbol:
                    AddInsert(newSymbol);
                    break;

                case IPropertySymbol propertySymbol:
                    // When inserting a new property, we need to insert the entire property, so
                    // that the backing field (if any), property and method semantics metadata tables can all be updated if/as necessary.
                    AddInsert(propertySymbol);

                    // Mark processed to suppress adding update edits of the accessors based on syntactic edits:
                    MarkProcessed(propertySymbol.GetMethod);
                    MarkProcessed(propertySymbol.SetMethod);
                    break;

                case IEventSymbol eventSymbol:
                    // When inserting a new event we need to insert the entire event, so
                    // pevent and method semantics metadata tables can all be updated if/as necessary.
                    AddInsert(eventSymbol);

                    // Mark processed to suppress adding update edits of the accessors based on syntactic edits:
                    MarkProcessed(eventSymbol.AddMethod);
                    MarkProcessed(eventSymbol.RemoveMethod);
                    MarkProcessed(eventSymbol.RaiseMethod);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(newSymbol.Kind);
            }

            void AddInsert(ISymbol symbol)
            {
                var symbolKey = SymbolKey.Create(symbol, cancellationToken);
                semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Insert, symbolKey, syntaxMap, syntaxMapTree: null, partialType));
            }

            void MarkProcessed(ISymbol? symbol)
            {
                if (symbol != null)
                {
                    processedSymbols.Add(symbol);
                }
            }
        }

        private static void AddDeleteAndInsertEditsForMemberAndAccessors(
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            ISymbol? oldSymbol,
            ISymbol? newSymbol,
            SymbolKey containingSymbolKey,
            Func<SyntaxNode, SyntaxNode?>? syntaxMap,
            HashSet<ISymbol> processedSymbols,
            CancellationToken cancellationToken)
        {
            Debug.Assert(oldSymbol is not INamedTypeSymbol);
            Debug.Assert(newSymbol is not INamedTypeSymbol);

            if (oldSymbol != null)
            {
                AddDeleteEditsForMemberAndAccessors(semanticEdits, oldSymbol, containingSymbolKey, syntaxMap, partialType: null, cancellationToken);
            }

            if (newSymbol != null)
            {
                AddInsertEditsForMemberAndAccessors(semanticEdits, newSymbol, syntaxMap, partialType: null, processedSymbols, cancellationToken);
            }
        }

        private ImmutableArray<(ISymbol? oldSymbol, ISymbol? newSymbol, EditKind editKind)> GetNamespaceSymbolEdits(
            SemanticModel oldModel,
            SemanticModel newModel,
            CancellationToken cancellationToken)
        {
            using var _1 = ArrayBuilder<(ISymbol? oldSymbol, ISymbol? newSymbol, EditKind editKind)>.GetInstance(out var builder);

            // Maps type name and arity to indices in builder array. Used to convert delete & insert edits to a move edit.
            // If multiple types with the same name and arity are deleted we match the inserted types in the order they were declared
            // and remove the index from the array, until no mathcing deleted type is found in the array of indices.
            using var _2 = PooledDictionary<(string name, int arity), ArrayBuilder<int>>.GetInstance(out var deletedTypes);

            // used to avoid duplicates due to partial declarations
            using var _3 = PooledHashSet<INamedTypeSymbol>.GetInstance(out var processedTypes);

            // Check that all top-level types declared in the old document are also declared in the new one.
            // Those that are not were either deleted or renamed.

            var oldRoot = oldModel.SyntaxTree.GetRoot(cancellationToken);
            foreach (var oldTypeDeclaration in GetTopLevelTypeDeclarations(oldRoot))
            {
                var oldType = (INamedTypeSymbol)GetRequiredDeclaredSymbol(oldModel, oldTypeDeclaration, cancellationToken);
                if (!processedTypes.Add(oldType))
                {
                    continue;
                }

                var newType = SymbolKey.Create(oldType, cancellationToken).Resolve(newModel.Compilation, ignoreAssemblyKey: true, cancellationToken).Symbol;
                if (newType == null)
                {
                    var key = (oldType.Name, oldType.Arity);

                    if (!deletedTypes.TryGetValue(key, out var indices))
                        deletedTypes.Add(key, indices = ArrayBuilder<int>.GetInstance());

                    indices.Add(builder.Count);
                    builder.Add((oldSymbol: oldType, newSymbol: null, EditKind.Delete));
                }
            }

            // reverse all indices:
            foreach (var (_, indices) in deletedTypes)
                indices.ReverseContents();

            processedTypes.Clear();

            // Check that all top-level types declared in the new document are also declared in the old one.
            // Those that are not were added.

            var newRoot = newModel.SyntaxTree.GetRoot(cancellationToken);
            foreach (var newTypeDeclaration in GetTopLevelTypeDeclarations(newRoot))
            {
                var newType = (INamedTypeSymbol)GetRequiredDeclaredSymbol(newModel, newTypeDeclaration, cancellationToken);
                if (!processedTypes.Add(newType))
                {
                    continue;
                }

                var oldType = SymbolKey.Create(newType, cancellationToken).Resolve(oldModel.Compilation, ignoreAssemblyKey: true, cancellationToken).Symbol;
                if (oldType == null)
                {
                    // Check if a type with the same name and arity was also removed. If so treat it as a move.
                    if (deletedTypes.TryGetValue((newType.Name, newType.Arity), out var deletedTypeIndices) && deletedTypeIndices.Count > 0)
                    {
                        var deletedTypeIndex = deletedTypeIndices.Last();
                        deletedTypeIndices.RemoveLast();

                        builder[deletedTypeIndex] = (builder[deletedTypeIndex].oldSymbol, newType, EditKind.Move);
                    }
                    else
                    {
                        builder.Add((oldSymbol: null, newSymbol: newType, EditKind.Insert));
                    }
                }
            }

            // free all index array builders:
            foreach (var (_, indices) in deletedTypes)
                indices.Free();

            return builder.ToImmutable();
        }

        private static bool IsReloadable(INamedTypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                foreach (var attributeData in current.GetAttributes())
                {
                    // We assume that the attribute System.Runtime.CompilerServices.CreateNewOnMetadataUpdateAttribute, if it exists, is well formed.
                    // If not an error will be reported during EnC delta emit.
                    if (attributeData.AttributeClass is { Name: CreateNewOnMetadataUpdateAttributeName, ContainingNamespace: { Name: "CompilerServices", ContainingNamespace: { Name: "Runtime", ContainingNamespace.Name: "System" } } })
                    {
                        return true;
                    }
                }

                current = current.BaseType;
            }

            return false;
        }

        private sealed class SemanticEditInfoComparer : IEqualityComparer<SemanticEditInfo>
        {
            public static SemanticEditInfoComparer Instance = new();

            private static readonly IEqualityComparer<SymbolKey> s_symbolKeyComparer = SymbolKey.GetComparer();

            public bool Equals([AllowNull] SemanticEditInfo x, [AllowNull] SemanticEditInfo y)
            {
                // When we delete a symbol, it might have the same symbol key as the matching insert
                // edit that corresponds to it, for example if only the return type has changed, because
                // symbol key does not consider return types. To ensure that this doesn't break us
                // by incorrectly de-duping our two edits, we treat edits as equal only if their
                // deleted symbol containers are both null, or both not null.
                if (x.DeletedSymbolContainer is null != y.DeletedSymbolContainer is null)
                {
                    return false;
                }

                return s_symbolKeyComparer.Equals(x.Symbol, y.Symbol);
            }

            public int GetHashCode([DisallowNull] SemanticEditInfo obj)
                => obj.Symbol.GetHashCode();
        }

        private void ReportMemberOrLambdaBodyUpdateRudeEdits(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            SemanticModel? oldModel,
            SyntaxNode oldDeclaration,
            ISymbol oldMember,
            SyntaxNode newDeclaration,
            DeclarationBody newBody,
            ISymbol newMember,
            Match<SyntaxNode> memberBodyMatch,
            EditAndContinueCapabilitiesGrantor capabilities,
            StateMachineInfo oldStateMachineInfo,
            StateMachineInfo newStateMachineInfo)
        {
            ReportMemberOrLambdaBodyUpdateRudeEditsImpl(diagnostics, newDeclaration, newBody, span: null);

            if (oldStateMachineInfo.IsStateMachine)
            {
                Contract.ThrowIfNull(oldModel);
                ReportMissingStateMachineAttribute(oldModel.Compilation, oldStateMachineInfo, newDeclaration, diagnostics);
            }

            if (!oldStateMachineInfo.IsStateMachine &&
                newStateMachineInfo.IsStateMachine &&
                !capabilities.Grant(EditAndContinueCapabilities.NewTypeDefinition))
            {
                // Adding a state machine, either for async or iterator, will require creating a new helper class
                // so is a rude edit if the runtime doesn't support it
                var rudeEdit = newStateMachineInfo.IsAsync ? RudeEditKind.MakeMethodAsyncNotSupportedByRuntime : RudeEditKind.MakeMethodIteratorNotSupportedByRuntime;
                diagnostics.Add(new RudeEditDiagnostic(rudeEdit, GetDiagnosticSpan(newDeclaration, EditKind.Update)));
            }

            if (oldStateMachineInfo.IsStateMachine && newStateMachineInfo.IsStateMachine)
            {
                if (!capabilities.Grant(EditAndContinueCapabilities.AddInstanceFieldToExistingType))
                {
                    diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.UpdatingStateMachineMethodNotSupportedByRuntime, GetDiagnosticSpan(newDeclaration, EditKind.Update)));
                }

                if ((InGenericContext(oldMember) ||
                     InGenericContext(newMember) ||
                     IsLambda(oldDeclaration) && InGenericLocalContext(oldDeclaration, OneOrMany.Create(memberBodyMatch.OldRoot)) ||
                     IsLambda(newDeclaration) && InGenericLocalContext(newDeclaration, OneOrMany.Create(memberBodyMatch.NewRoot))) &&
                    !capabilities.Grant(EditAndContinueCapabilities.GenericAddFieldToExistingType))
                {
                    diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, GetDiagnosticSpan(newDeclaration, EditKind.Update), newDeclaration, new[] { GetDisplayName(newDeclaration) }));
                }
            }
        }

        private void ReportUpdatedSymbolDeclarationRudeEdits(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            ISymbol oldSymbol,
            ISymbol newSymbol,
            SyntaxNode? newNode,
            SemanticModel newModel,
            EditAndContinueCapabilitiesGrantor capabilities,
            out bool hasGeneratedAttributeChange,
            out bool hasGeneratedReturnTypeAttributeChange,
            out bool hasParameterRename,
            out bool hasParameterTypeChange,
            out bool hasReturnTypeChange,
            CancellationToken cancellationToken)
        {
            var rudeEdit = RudeEditKind.None;

            hasGeneratedAttributeChange = false;
            hasGeneratedReturnTypeAttributeChange = false;
            hasParameterRename = false;
            hasParameterTypeChange = false;
            hasReturnTypeChange = false;

            if (oldSymbol.Kind != newSymbol.Kind)
            {
                rudeEdit = (oldSymbol.Kind == SymbolKind.Field || newSymbol.Kind == SymbolKind.Field) ? RudeEditKind.FieldKindUpdate : RudeEditKind.Update;
            }
            else if (oldSymbol.Name != newSymbol.Name)
            {
                if (oldSymbol is IParameterSymbol && newSymbol is IParameterSymbol)
                {
                    // We defer checking parameter renames until later, because if their types have also changed
                    // then we'll be emitting a new method, so it won't be a rename any more
                }
                else if (oldSymbol is IMethodSymbol oldMethod && newSymbol is IMethodSymbol newMethod)
                {
                    if (oldMethod.AssociatedSymbol != null && newMethod.AssociatedSymbol != null)
                    {
                        if (oldMethod.MethodKind != newMethod.MethodKind)
                        {
                            rudeEdit = RudeEditKind.AccessorKindUpdate;
                        }
                        else
                        {
                            // rude edit will be reported by the associated symbol
                            rudeEdit = RudeEditKind.None;
                        }
                    }
                    else if (oldMethod.MethodKind == MethodKind.Conversion)
                    {
                        rudeEdit = RudeEditKind.ModifiersUpdate;
                    }
                    else if (oldMethod.MethodKind == MethodKind.ExplicitInterfaceImplementation || newMethod.MethodKind == MethodKind.ExplicitInterfaceImplementation)
                    {
                        // Can't change from explicit to implicit interface implementation, or one interface to another
                        rudeEdit = RudeEditKind.Renamed;
                    }
                    else if (!AllowsDeletion(oldSymbol))
                    {
                        rudeEdit = RudeEditKind.Renamed;
                    }
                    else if (!CanRenameOrChangeSignature(oldSymbol, newSymbol, capabilities, cancellationToken))
                    {
                        rudeEdit = RudeEditKind.RenamingNotSupportedByRuntime;
                    }
                }
                else if (oldSymbol is IPropertySymbol oldProperty && newSymbol is IPropertySymbol newProperty)
                {
                    if (!oldProperty.ExplicitInterfaceImplementations.IsEmpty || !newProperty.ExplicitInterfaceImplementations.IsEmpty)
                    {
                        // Can't change from explicit to implicit interface implementation, or one interface to another
                        rudeEdit = RudeEditKind.Renamed;
                    }
                    else if (!AllowsDeletion(oldSymbol))
                    {
                        rudeEdit = RudeEditKind.Renamed;
                    }
                    else if (!CanRenameOrChangeSignature(oldSymbol, newSymbol, capabilities, cancellationToken))
                    {
                        rudeEdit = RudeEditKind.RenamingNotSupportedByRuntime;
                    }
                }
                else if (oldSymbol is IEventSymbol oldEvent && newSymbol is IEventSymbol newEvent)
                {
                    if (!oldEvent.ExplicitInterfaceImplementations.IsEmpty || !newEvent.ExplicitInterfaceImplementations.IsEmpty)
                    {
                        // Can't change from explicit to implicit interface implementation, or one interface to another
                        rudeEdit = RudeEditKind.Renamed;
                    }
                    else if (!AllowsDeletion(oldSymbol))
                    {
                        rudeEdit = RudeEditKind.Renamed;
                    }
                    else if (!CanRenameOrChangeSignature(oldSymbol, newSymbol, capabilities, cancellationToken))
                    {
                        rudeEdit = RudeEditKind.RenamingNotSupportedByRuntime;
                    }
                }
                else
                {
                    rudeEdit = RudeEditKind.Renamed;
                }
            }

            if (oldSymbol.DeclaredAccessibility != newSymbol.DeclaredAccessibility)
            {
                rudeEdit = RudeEditKind.ChangingAccessibility;
            }

            if (oldSymbol.IsStatic != newSymbol.IsStatic ||
                oldSymbol.IsVirtual != newSymbol.IsVirtual ||
                oldSymbol.IsAbstract != newSymbol.IsAbstract ||
                oldSymbol.IsOverride != newSymbol.IsOverride ||
                oldSymbol.IsExtern != newSymbol.IsExtern)
            {
                // Do not report for accessors as the error will be reported on their associated symbol.
                if (oldSymbol is not IMethodSymbol { AssociatedSymbol: not null })
                {
                    rudeEdit = RudeEditKind.ModifiersUpdate;
                }
            }

            if (oldSymbol is IFieldSymbol oldField && newSymbol is IFieldSymbol newField)
            {
                if (oldField.IsConst != newField.IsConst ||
                    oldField.IsReadOnly != newField.IsReadOnly ||
                    oldField.IsVolatile != newField.IsVolatile)
                {
                    rudeEdit = RudeEditKind.ModifiersUpdate;
                }

                // Report rude edit for updating const fields and values of enums. 
                // The latter is only reported whne the enum underlying type does not change to avoid cascading rude edits.
                if (oldField.IsConst && newField.IsConst && !Equals(oldField.ConstantValue, newField.ConstantValue) &&
                    TypesEquivalent(oldField.ContainingType.EnumUnderlyingType, newField.ContainingType.EnumUnderlyingType, exact: false))
                {
                    rudeEdit = RudeEditKind.InitializerUpdate;
                }

                if (oldField.FixedSize != newField.FixedSize)
                {
                    rudeEdit = RudeEditKind.FixedSizeFieldUpdate;
                }

                AnalyzeType(oldField.Type, newField.Type, ref rudeEdit, ref hasGeneratedAttributeChange);
            }
            else if (oldSymbol is IMethodSymbol oldMethod && newSymbol is IMethodSymbol newMethod)
            {
                if (oldMethod.IsReadOnly != newMethod.IsReadOnly)
                {
                    rudeEdit = RudeEditKind.ModifiersUpdate;
                }

                if (oldMethod.IsInitOnly != newMethod.IsInitOnly)
                {
                    rudeEdit = RudeEditKind.AccessorKindUpdate;
                }

                // Consider: Generalize to compare P/Invokes regardless of how they are defined (using attribute or Declare)
                if (oldMethod.MethodKind == MethodKind.DeclareMethod || newMethod.MethodKind == MethodKind.DeclareMethod)
                {
                    var oldImportData = oldMethod.GetDllImportData();
                    var newImportData = newMethod.GetDllImportData();
                    if (oldImportData != null && newImportData != null)
                    {
                        // Declare method syntax can't change these.
                        Debug.Assert(oldImportData.BestFitMapping == newImportData.BestFitMapping ||
                                     oldImportData.CallingConvention == newImportData.CallingConvention ||
                                     oldImportData.ExactSpelling == newImportData.ExactSpelling ||
                                     oldImportData.SetLastError == newImportData.SetLastError ||
                                     oldImportData.ThrowOnUnmappableCharacter == newImportData.ThrowOnUnmappableCharacter);

                        if (oldImportData.ModuleName != newImportData.ModuleName)
                        {
                            rudeEdit = RudeEditKind.DeclareLibraryUpdate;
                        }
                        else if (oldImportData.EntryPointName != newImportData.EntryPointName)
                        {
                            rudeEdit = RudeEditKind.DeclareAliasUpdate;
                        }
                        else if (oldImportData.CharacterSet != newImportData.CharacterSet)
                        {
                            rudeEdit = RudeEditKind.ModifiersUpdate;
                        }
                    }
                    else if (oldImportData is null != newImportData is null)
                    {
                        rudeEdit = RudeEditKind.ModifiersUpdate;
                    }
                }

                // VB implements clause
                if (!oldMethod.ExplicitInterfaceImplementations.SequenceEqual(newMethod.ExplicitInterfaceImplementations, SymbolsEquivalent))
                {
                    rudeEdit = RudeEditKind.ImplementsClauseUpdate;
                }

                // VB handles clause
                if (!AreHandledEventsEqual(oldMethod, newMethod))
                {
                    rudeEdit = RudeEditKind.HandlesClauseUpdate;
                }

                // Check return type - do not report for accessors, their containing symbol will report the rude edits and attribute updates.
                if (rudeEdit == RudeEditKind.None && oldMethod.AssociatedSymbol == null && newMethod.AssociatedSymbol == null)
                {
                    AnalyzeReturnType(oldMethod, newMethod, capabilities, ref rudeEdit, ref hasGeneratedReturnTypeAttributeChange, ref hasReturnTypeChange, cancellationToken);
                }
            }
            else if (oldSymbol is INamedTypeSymbol oldType && newSymbol is INamedTypeSymbol newType)
            {
                if (oldType.TypeKind != newType.TypeKind ||
                    oldType.IsRecord != newType.IsRecord) // TODO: https://github.com/dotnet/roslyn/issues/51874
                {
                    rudeEdit = RudeEditKind.TypeKindUpdate;
                }
                else if (oldType.IsRefLikeType != newType.IsRefLikeType ||
                         oldType.IsReadOnly != newType.IsReadOnly)
                {
                    rudeEdit = RudeEditKind.ModifiersUpdate;
                }

                if (rudeEdit == RudeEditKind.None)
                {
                    AnalyzeBaseTypes(oldType, newType, ref rudeEdit, ref hasGeneratedAttributeChange);

                    if (oldType.DelegateInvokeMethod != null)
                    {
                        Contract.ThrowIfNull(newType.DelegateInvokeMethod);
                        AnalyzeReturnType(oldType.DelegateInvokeMethod, newType.DelegateInvokeMethod, capabilities, ref rudeEdit, ref hasGeneratedReturnTypeAttributeChange, ref hasReturnTypeChange, cancellationToken);
                    }
                }
            }
            else if (oldSymbol is IPropertySymbol oldProperty && newSymbol is IPropertySymbol newProperty)
            {
                AnalyzeReturnType(oldProperty, newProperty, capabilities, ref rudeEdit, ref hasGeneratedReturnTypeAttributeChange, ref hasReturnTypeChange, cancellationToken);
            }
            else if (oldSymbol is IEventSymbol oldEvent && newSymbol is IEventSymbol newEvent)
            {
                // "readonly" modifier can only be applied on the event itself, not on its accessors.
                if (oldEvent.AddMethod != null && newEvent.AddMethod != null && oldEvent.AddMethod.IsReadOnly != newEvent.AddMethod.IsReadOnly ||
                    oldEvent.RemoveMethod != null && newEvent.RemoveMethod != null && oldEvent.RemoveMethod.IsReadOnly != newEvent.RemoveMethod.IsReadOnly)
                {
                    rudeEdit = RudeEditKind.ModifiersUpdate;
                }
                else
                {
                    AnalyzeReturnType(oldEvent, newEvent, capabilities, ref rudeEdit, ref hasGeneratedReturnTypeAttributeChange, ref hasReturnTypeChange, cancellationToken);
                }
            }
            else if (oldSymbol is IParameterSymbol oldParameter && newSymbol is IParameterSymbol newParameter)
            {
                if (oldParameter.RefKind != newParameter.RefKind ||
                    oldParameter.IsParams != newParameter.IsParams ||
                    IsExtensionMethodThisParameter(oldParameter) != IsExtensionMethodThisParameter(newParameter))
                {
                    rudeEdit = RudeEditKind.ModifiersUpdate;
                }
                else if (oldParameter.HasExplicitDefaultValue != newParameter.HasExplicitDefaultValue ||
                         oldParameter.HasExplicitDefaultValue && !Equals(oldParameter.ExplicitDefaultValue, newParameter.ExplicitDefaultValue))
                {
                    rudeEdit = RudeEditKind.InitializerUpdate;
                }
                else
                {
                    AnalyzeParameterType(oldParameter, newParameter, capabilities, ref rudeEdit, ref hasGeneratedAttributeChange, ref hasParameterTypeChange, cancellationToken);

                    if (!hasParameterTypeChange && oldParameter.Name != newParameter.Name)
                    {
                        if (capabilities.Grant(EditAndContinueCapabilities.UpdateParameters))
                        {
                            hasParameterRename = true;
                        }
                        else
                        {
                            rudeEdit = RudeEditKind.RenamingNotSupportedByRuntime;
                        }
                    }
                }
            }
            else if (oldSymbol is ITypeParameterSymbol oldTypeParameter && newSymbol is ITypeParameterSymbol newTypeParameter)
            {
                AnalyzeTypeParameter(oldTypeParameter, newTypeParameter, ref rudeEdit, ref hasGeneratedAttributeChange);
            }

            // Do not report modifier update if type kind changed.
            if (rudeEdit == RudeEditKind.None && oldSymbol.IsSealed != newSymbol.IsSealed)
            {
                // Do not report for accessors as the error will be reported on their associated symbol.
                if (oldSymbol is not IMethodSymbol { AssociatedSymbol: not null })
                {
                    rudeEdit = RudeEditKind.ModifiersUpdate;
                }
            }

            // updating within generic context
            if (rudeEdit == RudeEditKind.None &&
                oldSymbol is not INamedTypeSymbol and not ITypeParameterSymbol and not IParameterSymbol &&
                (InGenericContext(oldSymbol) || InGenericContext(newSymbol)) &&
                !capabilities.Grant(EditAndContinueCapabilities.GenericUpdateMethod))
            {
                rudeEdit = RudeEditKind.UpdatingGenericNotSupportedByRuntime;
            }

            if (rudeEdit != RudeEditKind.None)
            {
                ReportUpdateRudeEdit(diagnostics, rudeEdit, oldSymbol, newSymbol, newNode, newModel, cancellationToken);
            }
        }

        private static void AnalyzeType(ITypeSymbol oldType, ITypeSymbol newType, ref RudeEditKind rudeEdit, ref bool hasGeneratedAttributeChange, RudeEditKind rudeEditKind = RudeEditKind.TypeUpdate)
        {
            if (!TypesEquivalent(oldType, newType, exact: true))
            {
                if (TypesEquivalent(oldType, newType, exact: false))
                {
                    hasGeneratedAttributeChange = true;
                }
                else
                {
                    rudeEdit = rudeEditKind;
                }
            }
        }

        private static void AnalyzeBaseTypes(INamedTypeSymbol oldType, INamedTypeSymbol newType, ref RudeEditKind rudeEdit, ref bool hasGeneratedAttributeChange)
        {
            if (oldType.EnumUnderlyingType != null && newType.EnumUnderlyingType != null)
            {
                if (!TypesEquivalent(oldType.EnumUnderlyingType, newType.EnumUnderlyingType, exact: true))
                {
                    if (TypesEquivalent(oldType.EnumUnderlyingType, newType.EnumUnderlyingType, exact: false))
                    {
                        hasGeneratedAttributeChange = true;
                    }
                    else
                    {
                        rudeEdit = RudeEditKind.EnumUnderlyingTypeUpdate;
                    }
                }
            }
            else if (!BaseTypesEquivalent(oldType, newType, exact: true))
            {
                if (BaseTypesEquivalent(oldType, newType, exact: false))
                {
                    hasGeneratedAttributeChange = true;
                }
                else
                {
                    rudeEdit = RudeEditKind.BaseTypeOrInterfaceUpdate;
                }
            }
        }

        private void AnalyzeParameterType(
            IParameterSymbol oldParameter,
            IParameterSymbol newParameter,
            EditAndContinueCapabilitiesGrantor capabilities,
            ref RudeEditKind rudeEdit,
            ref bool hasGeneratedAttributeChange,
            ref bool hasParameterTypeChange,
            CancellationToken cancellationToken)
        {
            if (!ParameterTypesEquivalent(oldParameter, newParameter, exact: true))
            {
                if (ParameterTypesEquivalent(oldParameter, newParameter, exact: false))
                {
                    hasGeneratedAttributeChange = true;
                }
                else if (newParameter.ContainingType.IsDelegateType())
                {
                    // We don't allow changing parameter types in delegates
                    rudeEdit = RudeEditKind.TypeUpdate;
                }
                else if (AllowsDeletion(newParameter.ContainingSymbol))
                {
                    if (CanRenameOrChangeSignature(oldParameter.ContainingSymbol, newParameter.ContainingSymbol, capabilities, cancellationToken))
                    {
                        hasParameterTypeChange = true;
                    }
                    else
                    {
                        rudeEdit = RudeEditKind.ChangingTypeNotSupportedByRuntime;
                    }
                }
                else
                {
                    rudeEdit = RudeEditKind.TypeUpdate;
                }
            }
        }

        private static void AnalyzeTypeParameter(ITypeParameterSymbol oldParameter, ITypeParameterSymbol newParameter, ref RudeEditKind rudeEdit, ref bool hasGeneratedAttributeChange)
        {
            if (!TypeParameterConstraintsEquivalent(oldParameter, newParameter, exact: true))
            {
                if (TypeParameterConstraintsEquivalent(oldParameter, newParameter, exact: false))
                {
                    hasGeneratedAttributeChange = true;
                }
                else
                {
                    rudeEdit = (oldParameter.Variance != newParameter.Variance) ? RudeEditKind.VarianceUpdate : RudeEditKind.ChangingConstraints;
                }
            }
        }

        private void AnalyzeReturnType(IMethodSymbol oldMethod, IMethodSymbol newMethod, EditAndContinueCapabilitiesGrantor capabilities, ref RudeEditKind rudeEdit, ref bool hasGeneratedReturnTypeAttributeChange, ref bool hasReturnTypeChange, CancellationToken cancellationToken)
        {
            if (!ReturnTypesEquivalent(oldMethod, newMethod, exact: true))
            {
                if (ReturnTypesEquivalent(oldMethod, newMethod, exact: false))
                {
                    hasGeneratedReturnTypeAttributeChange = true;
                }
                else if (IsGlobalMain(oldMethod) || IsGlobalMain(newMethod))
                {
                    rudeEdit = RudeEditKind.ChangeImplicitMainReturnType;
                }
                else if (oldMethod.ContainingType.IsDelegateType())
                {
                    rudeEdit = RudeEditKind.TypeUpdate;
                }
                else if (AllowsDeletion(newMethod))
                {
                    if (CanRenameOrChangeSignature(oldMethod, newMethod, capabilities, cancellationToken))
                    {
                        hasReturnTypeChange = true;
                    }
                    else
                    {
                        rudeEdit = RudeEditKind.ChangingTypeNotSupportedByRuntime;
                    }
                }
                else
                {
                    rudeEdit = RudeEditKind.TypeUpdate;
                }
            }
        }

        private void AnalyzeReturnType(IEventSymbol oldEvent, IEventSymbol newEvent, EditAndContinueCapabilitiesGrantor capabilities, ref RudeEditKind rudeEdit, ref bool hasGeneratedReturnTypeAttributeChange, ref bool hasReturnTypeChange, CancellationToken cancellationToken)
        {
            if (!ReturnTypesEquivalent(oldEvent, newEvent, exact: true))
            {
                if (ReturnTypesEquivalent(oldEvent, newEvent, exact: false))
                {
                    hasGeneratedReturnTypeAttributeChange = true;
                }
                else if (oldEvent.ContainingType.IsDelegateType())
                {
                    rudeEdit = RudeEditKind.TypeUpdate;
                }
                else if (AllowsDeletion(newEvent))
                {
                    if (CanRenameOrChangeSignature(oldEvent, newEvent, capabilities, cancellationToken))
                    {
                        hasReturnTypeChange = true;
                    }
                    else
                    {
                        rudeEdit = RudeEditKind.ChangingTypeNotSupportedByRuntime;
                    }
                }
                else
                {
                    rudeEdit = RudeEditKind.TypeUpdate;
                }
            }
        }

        private void AnalyzeReturnType(IPropertySymbol oldProperty, IPropertySymbol newProperty, EditAndContinueCapabilitiesGrantor capabilities, ref RudeEditKind rudeEdit, ref bool hasGeneratedReturnTypeAttributeChange, ref bool hasReturnTypeChange, CancellationToken cancellationToken)
        {
            if (!ReturnTypesEquivalent(oldProperty, newProperty, exact: true))
            {
                if (ReturnTypesEquivalent(oldProperty, newProperty, exact: false))
                {
                    hasGeneratedReturnTypeAttributeChange = true;
                }
                else if (oldProperty.ContainingType.IsDelegateType())
                {
                    rudeEdit = RudeEditKind.TypeUpdate;
                }
                else if (AllowsDeletion(newProperty))
                {
                    if (CanRenameOrChangeSignature(oldProperty, newProperty, capabilities, cancellationToken))
                    {
                        hasReturnTypeChange = true;
                    }
                    else
                    {
                        rudeEdit = RudeEditKind.ChangingTypeNotSupportedByRuntime;
                    }
                }
                else
                {
                    rudeEdit = RudeEditKind.TypeUpdate;
                }
            }
        }

        private static bool IsExtensionMethodThisParameter(IParameterSymbol parameter)
            => parameter is { Ordinal: 0, ContainingSymbol: IMethodSymbol { IsExtensionMethod: true } };

        private void AnalyzeSymbolUpdate(
            ISymbol oldSymbol,
            ISymbol newSymbol,
            SyntaxNode? newNode,
            SemanticModel newModel,
            Match<SyntaxNode> topMatch,
            EditAndContinueCapabilitiesGrantor capabilities,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            Func<SyntaxNode, SyntaxNode?>? syntaxMap,
            HashSet<ISymbol> processedSymbols,
            CancellationToken cancellationToken)
        {
            // TODO: fails in VB on delegate parameter https://github.com/dotnet/roslyn/issues/53337
            // Contract.ThrowIfFalse(newSymbol.IsImplicitlyDeclared == newDeclaration is null);

            ReportCustomAttributeRudeEdits(diagnostics, oldSymbol, newSymbol, newNode, newModel, capabilities, out var hasAttributeChange, out var hasReturnTypeAttributeChange, cancellationToken);

            ReportUpdatedSymbolDeclarationRudeEdits(diagnostics, oldSymbol, newSymbol, newNode, newModel, capabilities, out var hasGeneratedAttributeChange, out var hasGeneratedReturnTypeAttributeChange, out var hasParameterRename, out var hasParameterTypeChange, out var hasReturnTypeChange, cancellationToken);
            hasAttributeChange |= hasGeneratedAttributeChange;
            hasReturnTypeAttributeChange |= hasGeneratedReturnTypeAttributeChange;

            if (hasParameterRename || hasParameterTypeChange)
            {
                Debug.Assert(newSymbol is IParameterSymbol);

                // In VB, when the type of a custom event changes, the parameters on the add and remove handlers also change
                // but we can ignore them because we have already done what we need to the event declaration itself.
                if (newSymbol.ContainingSymbol is IMethodSymbol { AssociatedSymbol: IEventSymbol associatedSymbol } &&
                    processedSymbols.Contains(associatedSymbol))
                {
                    return;
                }

                AddParameterUpdateSemanticEdit(semanticEdits, (IParameterSymbol)oldSymbol, (IParameterSymbol)newSymbol, newModel.Compilation, syntaxMap, reportDeleteAndInsertEdits: hasParameterTypeChange, processedSymbols, cancellationToken);
            }
            else if (hasReturnTypeChange)
            {
                var containingSymbolKey = SymbolKey.Create(oldSymbol.ContainingSymbol, cancellationToken);
                AddDeleteAndInsertEditsForMemberAndAccessors(semanticEdits, oldSymbol, newSymbol, containingSymbolKey, syntaxMap, processedSymbols, cancellationToken);
            }
            else if (hasAttributeChange || hasReturnTypeAttributeChange)
            {
                AddCustomAttributeSemanticEdits(semanticEdits, oldSymbol, newSymbol, newModel.Compilation, topMatch, syntaxMap, processedSymbols, hasAttributeChange, hasReturnTypeAttributeChange, cancellationToken);
            }
        }

        private void AddCustomAttributeSemanticEdits(
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            ISymbol oldSymbol,
            ISymbol newSymbol,
            Compilation newCompilation,
            Match<SyntaxNode> topMatch,
            Func<SyntaxNode, SyntaxNode?>? syntaxMap,
            HashSet<ISymbol> processedSymbols,
            bool hasAttributeChange,
            bool hasReturnTypeAttributeChange,
            CancellationToken cancellationToken)
        {
            // Most symbol types will automatically have an edit added, so we just need to handle a few
            if (newSymbol is INamedTypeSymbol { DelegateInvokeMethod: not null and var newDelegateInvokeMethod } newDelegateType)
            {
                if (hasAttributeChange)
                {
                    semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, SymbolKey.Create(newDelegateType, cancellationToken), syntaxMap, syntaxMapTree: null, partialType: null));
                }

                if (hasReturnTypeAttributeChange)
                {
                    // attributes applied on return type of a delegate are applied to both Invoke and BeginInvoke methods
                    semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, SymbolKey.Create(newDelegateInvokeMethod, cancellationToken), syntaxMap, syntaxMapTree: null, partialType: null));
                    AddDelegateBeginInvokeEdit(semanticEdits, newDelegateType, syntaxMap, cancellationToken);
                }
            }
            else if (newSymbol is INamedTypeSymbol)
            {
                var symbolKey = SymbolKey.Create(newSymbol, cancellationToken);
                semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, symbolKey, syntaxMap, syntaxMapTree: null,
                    IsPartialEdit(oldSymbol, newSymbol, topMatch.OldRoot.SyntaxTree, topMatch.NewRoot.SyntaxTree) ? symbolKey : null));
            }
            else if (newSymbol is ITypeParameterSymbol)
            {
                var containingTypeSymbolKey = SymbolKey.Create(newSymbol.ContainingSymbol, cancellationToken);
                semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, containingTypeSymbolKey, syntaxMap, syntaxMapTree: null,
                    IsPartialEdit(oldSymbol.ContainingSymbol, newSymbol.ContainingSymbol, topMatch.OldRoot.SyntaxTree, topMatch.NewRoot.SyntaxTree) ? containingTypeSymbolKey : null));
            }
            else if (newSymbol is IFieldSymbol or IPropertySymbol or IEventSymbol)
            {
                semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, SymbolKey.Create(newSymbol, cancellationToken), syntaxMap, syntaxMapTree: null, partialType: null));
            }
            else if (newSymbol is IParameterSymbol newParameterSymbol)
            {
                AddParameterUpdateSemanticEdit(semanticEdits, (IParameterSymbol)oldSymbol, newParameterSymbol, newCompilation, syntaxMap, reportDeleteAndInsertEdits: false, processedSymbols, cancellationToken);
            }
        }

        private void AddParameterUpdateSemanticEdit(
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            IParameterSymbol oldParameterSymbol,
            IParameterSymbol newParameterSymbol,
            Compilation newCompilation,
            Func<SyntaxNode, SyntaxNode?>? syntaxMap,
            bool reportDeleteAndInsertEdits,
            HashSet<ISymbol> processedSymbols,
            CancellationToken cancellationToken)
        {
            var newContainingMember = newParameterSymbol.ContainingSymbol;

            if (reportDeleteAndInsertEdits)
            {
                var oldContainingMember = oldParameterSymbol.ContainingSymbol;
                var containingSymbolKey = SymbolKey.Create(oldContainingMember.ContainingSymbol, cancellationToken);

                AddDeleteAndInsertEditsForMemberAndAccessors(semanticEdits, oldContainingMember, newContainingMember, containingSymbolKey, syntaxMap, processedSymbols, cancellationToken);

                // If primary constructor was replaced with non-primary (or vice versa) we wouldn't be processing a parameter update,
                // but rather a delete and insert of the constructor symbol.
                Debug.Assert(IsPrimaryConstructor(oldContainingMember, cancellationToken) == IsPrimaryConstructor(newContainingMember, cancellationToken));

                if (oldContainingMember.ContainingType.IsRecord && newContainingMember.ContainingType.IsRecord && IsPrimaryConstructor(oldContainingMember, cancellationToken))
                {
                    var oldPrimaryConstructor = (IMethodSymbol)oldContainingMember;
                    var newPrimaryConstructor = (IMethodSymbol)newContainingMember;

                    // add delete and insert edits of synthesized properties:
                    var oldSynthesizedProperty = GetPropertySynthesizedForRecordPrimaryConstructorParameter(oldParameterSymbol);
                    var newSynthesizedProperty = GetPropertySynthesizedForRecordPrimaryConstructorParameter(newParameterSymbol);
                    AddDeleteAndInsertEditsForMemberAndAccessors(semanticEdits, oldSynthesizedProperty, newSynthesizedProperty, containingSymbolKey, syntaxMap, processedSymbols, cancellationToken);

                    // add delete and insert edits of synthesized deconstructor:
                    var oldSynthesizedDeconstructor = oldPrimaryConstructor.GetMatchingDeconstructor();
                    var newSynthesizedDeconstructor = newPrimaryConstructor.GetMatchingDeconstructor();
                    AddDeleteAndInsertEditsForMemberAndAccessors(semanticEdits, oldSynthesizedDeconstructor, newSynthesizedDeconstructor, containingSymbolKey, syntaxMap, processedSymbols, cancellationToken);

                    // add updates of synthesized methods:
                    AddSynthesizedRecordMethodUpdatesForPropertyChange(semanticEdits, newCompilation, newContainingMember.ContainingType, cancellationToken);
                }
            }
            else
            {
                semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, SymbolKey.Create(newContainingMember, cancellationToken), syntaxMap, syntaxMapTree: null, partialType: null));
            }

            // attributes applied on parameters of a delegate are applied to both Invoke and BeginInvoke methods
            if (newContainingMember.ContainingType is INamedTypeSymbol { TypeKind: TypeKind.Delegate } newContainingDelegateType)
            {
                Debug.Assert(reportDeleteAndInsertEdits == false);
                AddDelegateBeginInvokeEdit(semanticEdits, newContainingDelegateType, syntaxMap, cancellationToken);
            }
        }

        private static void AddDelegateBeginInvokeEdit(ArrayBuilder<SemanticEditInfo> semanticEdits, INamedTypeSymbol delegateType, Func<SyntaxNode, SyntaxNode?>? syntaxMap, CancellationToken cancellationToken)
        {
            Debug.Assert(semanticEdits != null);

            var beginInvokeMethod = delegateType.GetMembers("BeginInvoke").FirstOrDefault();
            if (beginInvokeMethod != null)
            {
                semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, SymbolKey.Create(beginInvokeMethod, cancellationToken), syntaxMap, syntaxMapTree: null, partialType: null));
            }
        }

        private void ReportCustomAttributeRudeEdits(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            ISymbol oldSymbol,
            ISymbol newSymbol,
            SyntaxNode? newNode,
            SemanticModel newModel,
            EditAndContinueCapabilitiesGrantor capabilities,
            out bool hasAttributeChange,
            out bool hasReturnTypeAttributeChange,
            CancellationToken cancellationToken)
        {
            // This is the only case we care about whether to issue an edit or not, because this is the only case where types have their attributes checked
            // and types are the only things that would otherwise not have edits reported.
            hasAttributeChange = ReportCustomAttributeRudeEdits(diagnostics, oldSymbol.GetAttributes(), newSymbol.GetAttributes(), oldSymbol, newSymbol, newNode, newModel, capabilities, cancellationToken);

            hasReturnTypeAttributeChange = false;

            if (oldSymbol is IMethodSymbol oldMethod &&
                newSymbol is IMethodSymbol newMethod)
            {
                hasReturnTypeAttributeChange |= ReportCustomAttributeRudeEdits(diagnostics, oldMethod.GetReturnTypeAttributes(), newMethod.GetReturnTypeAttributes(), oldSymbol, newSymbol, newNode, newModel, capabilities, cancellationToken);
            }
            else if (oldSymbol is INamedTypeSymbol { DelegateInvokeMethod: not null and var oldInvokeMethod } &&
                     newSymbol is INamedTypeSymbol { DelegateInvokeMethod: not null and var newInvokeMethod })
            {
                hasReturnTypeAttributeChange |= ReportCustomAttributeRudeEdits(diagnostics, oldInvokeMethod.GetReturnTypeAttributes(), newInvokeMethod.GetReturnTypeAttributes(), oldSymbol, newSymbol, newNode, newModel, capabilities, cancellationToken);
            }
        }

        private bool ReportCustomAttributeRudeEdits(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            ImmutableArray<AttributeData>? oldAttributes,
            ImmutableArray<AttributeData> newAttributes,
            ISymbol oldSymbol,
            ISymbol newSymbol,
            SyntaxNode? newNode,
            SemanticModel newModel,
            EditAndContinueCapabilitiesGrantor capabilities,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<AttributeData>.GetInstance(out var changedAttributes);

            FindChangedAttributes(oldAttributes, newAttributes, changedAttributes);
            if (oldAttributes.HasValue)
            {
                FindChangedAttributes(newAttributes, oldAttributes.Value, changedAttributes);
            }

            if (changedAttributes.Count == 0)
            {
                return false;
            }

            // If the runtime doesn't support changing attributes we don't need to check anything else
            if (!capabilities.Grant(EditAndContinueCapabilities.ChangeCustomAttributes))
            {
                ReportUpdateRudeEdit(diagnostics, RudeEditKind.ChangingAttributesNotSupportedByRuntime, oldSymbol, newSymbol, newNode, newModel, cancellationToken);
                return false;
            }

            // Updating type parameter attributes is currently not supported.
            if (oldSymbol is ITypeParameterSymbol)
            {
                var rudeEdit = oldSymbol.ContainingSymbol.Kind == SymbolKind.Method ? RudeEditKind.GenericMethodUpdate : RudeEditKind.GenericTypeUpdate;
                ReportUpdateRudeEdit(diagnostics, rudeEdit, oldSymbol, newSymbol, newNode, newModel, cancellationToken);
                return false;
            }

            // Even if the runtime supports attribute changes, only attributes stored in the CustomAttributes table are editable
            foreach (var attributeData in changedAttributes)
            {
                if (IsNonCustomAttribute(attributeData))
                {
                    var node = newNode ?? GetRudeEditDiagnosticNode(newSymbol, cancellationToken);
                    diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.ChangingNonCustomAttribute, GetDiagnosticSpan(node, EditKind.Update), node, new[]
                    {
                        attributeData.AttributeClass!.Name,
                        GetDisplayName(newSymbol)
                    }));

                    return false;
                }
            }

            return true;

            static void FindChangedAttributes(ImmutableArray<AttributeData>? oldAttributes, ImmutableArray<AttributeData> newAttributes, ArrayBuilder<AttributeData> changedAttributes)
            {
                for (var i = 0; i < newAttributes.Length; i++)
                {
                    var newAttribute = newAttributes[i];
                    var oldAttribute = FindMatch(newAttribute, oldAttributes);

                    if (oldAttribute is null)
                    {
                        changedAttributes.Add(newAttribute);
                    }
                }
            }

            static AttributeData? FindMatch(AttributeData attribute, ImmutableArray<AttributeData>? oldAttributes)
            {
                if (!oldAttributes.HasValue)
                {
                    return null;
                }

                foreach (var match in oldAttributes.Value)
                {
                    if (SymbolEquivalenceComparer.Instance.Equals(match.AttributeClass, attribute.AttributeClass))
                    {
                        if (SymbolEquivalenceComparer.Instance.Equals(match.AttributeConstructor, attribute.AttributeConstructor) &&
                            match.ConstructorArguments.SequenceEqual(attribute.ConstructorArguments, TypedConstantComparer.Instance) &&
                            match.NamedArguments.SequenceEqual(attribute.NamedArguments, NamedArgumentComparer.Instance))
                        {
                            return match;
                        }
                    }
                }

                return null;
            }

            static bool IsNonCustomAttribute(AttributeData attribute)
            {
                return attribute.AttributeClass?.ToNameDisplayString() switch
                {
                    //
                    // This list comes from ShouldEmitAttribute in src\Compilers\CSharp\Portable\Symbols\Attributes\AttributeData.cs
                    // and src\Compilers\VisualBasic\Portable\Symbols\Attributes\AttributeData.vb
                    // TODO: Use a compiler API to get this information rather than hard coding a list: https://github.com/dotnet/roslyn/issues/53410
                    //
                    "System.CLSCompliantAttribute" => true,
                    "System.Diagnostics.CodeAnalysis.AllowNullAttribute" => true,
                    "System.Diagnostics.CodeAnalysis.DisallowNullAttribute" => true,
                    "System.Diagnostics.CodeAnalysis.MaybeNullAttribute" => true,
                    "System.Diagnostics.CodeAnalysis.NotNullAttribute" => true,
                    "System.NonSerializedAttribute" => true,
                    "System.Reflection.AssemblyAlgorithmIdAttribute" => true,
                    "System.Reflection.AssemblyCultureAttribute" => true,
                    "System.Reflection.AssemblyFlagsAttribute" => true,
                    "System.Reflection.AssemblyVersionAttribute" => true,
                    "System.Runtime.CompilerServices.DllImportAttribute" => true,       // Already covered by other rude edits, but included for completeness
                    "System.Runtime.CompilerServices.IndexerNameAttribute" => true,
                    "System.Runtime.CompilerServices.MethodImplAttribute" => true,
                    "System.Runtime.CompilerServices.SpecialNameAttribute" => true,
                    "System.Runtime.CompilerServices.TypeForwardedToAttribute" => true,
                    "System.Runtime.InteropServices.ComImportAttribute" => true,
                    "System.Runtime.InteropServices.DefaultParameterValueAttribute" => true,
                    "System.Runtime.InteropServices.FieldOffsetAttribute" => true,
                    "System.Runtime.InteropServices.InAttribute" => true,
                    "System.Runtime.InteropServices.MarshalAsAttribute" => true,
                    "System.Runtime.InteropServices.OptionalAttribute" => true,
                    "System.Runtime.InteropServices.OutAttribute" => true,
                    "System.Runtime.InteropServices.PreserveSigAttribute" => true,
                    "System.Runtime.InteropServices.StructLayoutAttribute" => true,
                    "System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeImportAttribute" => true,
                    "System.Security.DynamicSecurityMethodAttribute" => true,
                    "System.SerializableAttribute" => true,

                    //
                    // This list is not from the compiler, but included explicitly for Edit and Continue purposes
                    //

                    // Applying [AsyncMethodBuilder] changes the code that is emitted:
                    // * When the target is a method, for any await call to the method
                    // * When the target is a type, for any await call to a method that returns that type
                    //
                    // Therefore applying this attribute can cause unbounded changes to emitted code anywhere in a project
                    // which EnC wouldn't pick up, so we block it with a rude edit
                    "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute" => true,

                    // Also security attributes
                    not null => IsSecurityAttribute(attribute.AttributeClass),
                    _ => false
                };
            }

            static bool IsSecurityAttribute(INamedTypeSymbol namedTypeSymbol)
            {
                // Security attributes are any attribute derived from System.Security.Permissions.SecurityAttribute, directly or indirectly

                var symbol = namedTypeSymbol;
                while (symbol is not null)
                {
                    if (symbol.ToNameDisplayString() == "System.Security.Permissions.SecurityAttribute")
                    {
                        return true;
                    }

                    symbol = symbol.BaseType;
                }

                return false;
            }
        }

        /// <summary>
        /// Check if the <paramref name="capabilities"/> allow us to rename or change signature of a member.
        /// Such edit translates to an addition of a new member, an update of any method bodies associated with the old one and marking the member as "deleted".
        /// </summary>
        private bool CanRenameOrChangeSignature(ISymbol oldSymbol, ISymbol newSymbol, EditAndContinueCapabilitiesGrantor capabilities, CancellationToken cancellationToken)
            => CanAddNewMemberToExistingType(newSymbol, capabilities, cancellationToken) &&
               CanUpdateMemberBody(oldSymbol, newSymbol, capabilities);

        private bool CanAddNewMemberToExistingType(ISymbol newSymbol, EditAndContinueCapabilitiesGrantor capabilities, CancellationToken cancellationToken)
        {
            var requiredCapabilities = EditAndContinueCapabilities.None;

            if (newSymbol is IMethodSymbol or IEventSymbol or IPropertySymbol)
            {
                requiredCapabilities |= EditAndContinueCapabilities.AddMethodToExistingType;
            }

            if (newSymbol is IFieldSymbol || newSymbol is IPropertySymbol { DeclaringSyntaxReferences: [_] } && HasBackingField(GetSymbolDeclarationSyntax(newSymbol, cancellationToken)))
            {
                requiredCapabilities |= newSymbol.IsStatic ? EditAndContinueCapabilities.AddStaticFieldToExistingType : EditAndContinueCapabilities.AddInstanceFieldToExistingType;
            }

            // Inserting a member into an existing generic type, or a generic method into a type is only allowed if the runtime supports it
            if (newSymbol is not INamedTypeSymbol && InGenericContext(newSymbol))
            {
                requiredCapabilities |= newSymbol is IFieldSymbol ? EditAndContinueCapabilities.GenericAddFieldToExistingType : EditAndContinueCapabilities.GenericAddMethodToExistingType;
            }

            return capabilities.Grant(requiredCapabilities);
        }

        private static bool CanUpdateMemberBody(ISymbol oldSymbol, ISymbol newSymbol, EditAndContinueCapabilitiesGrantor capabilities)
        {
            if (InGenericContext(oldSymbol) || InGenericContext(newSymbol))
            {
                return capabilities.Grant(EditAndContinueCapabilities.GenericUpdateMethod);
            }

            return true;
        }

        /// <summary>
        /// Adds edits for synthesized record members.
        /// </summary>
        private static void AddSynthesizedRecordMethodUpdatesForPropertyChange(
            ArrayBuilder<SemanticEditInfo> semanticEdits,
            Compilation compilation,
            INamedTypeSymbol recordType,
            CancellationToken cancellationToken)
        {
            Debug.Assert(recordType.IsRecord);

            foreach (var member in GetRecordUpdatedSynthesizedMethods(compilation, recordType))
            {
                var symbolKey = SymbolKey.Create(member, cancellationToken);
                semanticEdits.Add(new SemanticEditInfo(SemanticEditKind.Update, symbolKey, syntaxMap: null, syntaxMapTree: null, partialType: null));
            }
        }

        private static IEnumerable<ISymbol> GetRecordUpdatedSynthesizedMethods(Compilation compilation, INamedTypeSymbol record)
        {
            Debug.Assert(record.IsRecord);

            // All methods that are updated have well known names, and calling GetMembers(string) is
            // faster than enumerating.

            // When a new field or property is added methods PrintMembers, Equals, GetHashCode and the copy-constructor may be updated.
            // Some updates might not be strictly necessary but for simplicity we update all that are not explicitly declared.

            // The primary constructor is deferred since it needs to be aggregated with initializer updates.

            var result = record.GetMembers(WellKnownMemberNames.PrintMembersMethodName)
                .FirstOrDefault(static (m, compilation) => m is IMethodSymbol { IsImplicitlyDeclared: true } method && HasPrintMembersSignature(method, compilation), compilation);
            if (result is not null)
            {
                yield return result;
            }

            result = record.GetMembers(WellKnownMemberNames.ObjectEquals)
                .FirstOrDefault(static m => m is IMethodSymbol { IsImplicitlyDeclared: true } method && HasIEquatableEqualsSignature(method));
            if (result is not null)
            {
                yield return result;
            }

            result = record.GetMembers(WellKnownMemberNames.ObjectGetHashCode)
                .FirstOrDefault(static m => m is IMethodSymbol { IsImplicitlyDeclared: true } method && HasGetHashCodeSignature(method));
            if (result is not null)
            {
                yield return result;
            }

            // copy constructor
            if (record.TypeKind == TypeKind.Class)
            {
                result = record.InstanceConstructors.SingleOrDefault(m => m.IsImplicitlyDeclared && m.IsCopyConstructor());
                if (result is not null)
                {
                    yield return result;
                }
            }
        }

        private void ReportDeletedMemberRudeEdit(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            ISymbol oldSymbol,
            SemanticModel newModel,
            RudeEditKind rudeEditKind,
            CancellationToken cancellationToken)
        {
            var newNode = GetDeleteRudeEditDiagnosticNode(oldSymbol, newModel, cancellationToken);

            diagnostics.Add(new RudeEditDiagnostic(
                rudeEditKind,
                GetDiagnosticSpan(newNode, EditKind.Delete),
                arguments: new[] { GetDisplayKindAndName(oldSymbol, fullyQualify: false) }));
        }

        private void ReportUpdateRudeEdit(ArrayBuilder<RudeEditDiagnostic> diagnostics, RudeEditKind rudeEdit, SyntaxNode newNode)
        {
            diagnostics.Add(new RudeEditDiagnostic(
                rudeEdit,
                GetDiagnosticSpan(newNode, EditKind.Update),
                newNode,
                new[] { GetDisplayName(newNode) }));
        }

        private void ReportUpdateRudeEdit(ArrayBuilder<RudeEditDiagnostic> diagnostics, RudeEditKind rudeEdit, ISymbol newSymbol, SyntaxNode? newNode, CancellationToken cancellationToken)
        {
            var node = newNode ?? GetRudeEditDiagnosticNode(newSymbol, cancellationToken);
            var span = GetDiagnosticSpan(node, (rudeEdit == RudeEditKind.ChangeImplicitMainReturnType) ? EditKind.Delete : EditKind.Update);

            var arguments = rudeEdit switch
            {
                RudeEditKind.TypeKindUpdate or
                RudeEditKind.ChangeImplicitMainReturnType or
                RudeEditKind.GenericMethodUpdate or
                RudeEditKind.GenericTypeUpdate
                    => Array.Empty<string>(),

                RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime
                    => new[] { CreateNewOnMetadataUpdateAttributeName },

                _ => new[]
                {
                    // Include member name if it is implicitly declared, otherwise it might not be obvious which member is being referred to.
                    // TODO: newSymbol.ContainingSymbol.IsImplicitlyDeclared should not be needed https://github.com/dotnet/roslyn/issues/68510
                    newSymbol.IsImplicitlyDeclared || newSymbol.ContainingSymbol is { IsImplicitlyDeclared: true, Kind: not SymbolKind.Namespace }
                        ? GetDisplayKindAndName(newSymbol, fullyQualify: false)
                        : GetDisplayName(newSymbol)
                }
            };

            diagnostics.Add(new RudeEditDiagnostic(rudeEdit, span, node, arguments));
        }

        private void ReportUpdateRudeEdit(ArrayBuilder<RudeEditDiagnostic> diagnostics, RudeEditKind rudeEdit, ISymbol oldSymbol, ISymbol newSymbol, SyntaxNode? newNode, SemanticModel newModel, CancellationToken cancellationToken)
        {
            if (newSymbol.IsImplicitlyDeclared && rudeEdit != RudeEditKind.GenericTypeUpdate)
            {
                ReportDeletedMemberRudeEdit(diagnostics, oldSymbol, newModel, rudeEdit, cancellationToken);
            }
            else
            {
                ReportUpdateRudeEdit(diagnostics, rudeEdit, newSymbol, newNode, cancellationToken);
            }
        }

        private SyntaxNode GetRudeEditDiagnosticNode(ISymbol symbol, CancellationToken cancellationToken)
        {
            var container = symbol;
            while (container != null)
            {
                // TODO: only one condition should be sufficient https://github.com/dotnet/roslyn/issues/68510
                if (container.DeclaringSyntaxReferences.Length > 0 && !container.IsImplicitlyDeclared)
                {
                    return GetSymbolDeclarationSyntax(container, cancellationToken);
                }

                container = container.ContainingSymbol;
            }

            throw ExceptionUtilities.Unreachable();
        }

        private SyntaxNode GetDeleteRudeEditDiagnosticNode(ISymbol oldSymbol, SemanticModel newModel, CancellationToken cancellationToken)
        {
            var oldContainer = oldSymbol.ContainingSymbol;
            while (oldContainer != null)
            {
                var containerKey = SymbolKey.Create(oldContainer, cancellationToken);
                var newContainer = containerKey.Resolve(newModel.Compilation, ignoreAssemblyKey: true, cancellationToken).Symbol;
                if (newContainer != null)
                {
                    return GetRudeEditDiagnosticNode(newContainer, cancellationToken);
                }

                oldContainer = oldContainer.ContainingSymbol;
            }

            return newModel.SyntaxTree.GetRoot(cancellationToken);
        }

        #region Type Layout Update Validation 

        internal void ReportTypeLayoutUpdateRudeEdits(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            ISymbol newSymbol,
            SyntaxNode newSyntax,
            SemanticModel newModel,
            ref INamedTypeSymbol? lazyLayoutAttribute)
        {
            switch (newSymbol.Kind)
            {
                case SymbolKind.Field:
                    if (HasExplicitOrSequentialLayout(newSymbol.ContainingType, newModel, ref lazyLayoutAttribute))
                    {
                        ReportTypeLayoutUpdateRudeEdits(diagnostics, newSymbol, newSyntax);
                    }

                    break;

                case SymbolKind.Property:
                    if (HasBackingField(newSyntax) &&
                        HasExplicitOrSequentialLayout(newSymbol.ContainingType, newModel, ref lazyLayoutAttribute))
                    {
                        ReportTypeLayoutUpdateRudeEdits(diagnostics, newSymbol, newSyntax);
                    }

                    break;

                case SymbolKind.Event:
                    if (HasBackingField((IEventSymbol)newSymbol) &&
                        HasExplicitOrSequentialLayout(newSymbol.ContainingType, newModel, ref lazyLayoutAttribute))
                    {
                        ReportTypeLayoutUpdateRudeEdits(diagnostics, newSymbol, newSyntax);
                    }

                    break;
            }
        }

        private void ReportTypeLayoutUpdateRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, ISymbol symbol, SyntaxNode syntax)
        {
            var intoStruct = symbol.ContainingType.TypeKind == TypeKind.Struct;

            diagnostics.Add(new RudeEditDiagnostic(
                intoStruct ? RudeEditKind.InsertIntoStruct : RudeEditKind.InsertIntoClassWithLayout,
                syntax.Span,
                syntax,
                new[]
                {
                    GetDisplayName(syntax, EditKind.Insert),
                    GetDisplayName(TryGetContainingTypeDeclaration(syntax)!, EditKind.Update)
                }));
        }

        private static bool HasBackingField(IEventSymbol @event)
        {
#nullable disable // https://github.com/dotnet/roslyn/issues/39288
            return @event.AddMethod.IsImplicitlyDeclared
#nullable enable
                && !@event.IsAbstract;
        }

        private static bool HasExplicitOrSequentialLayout(
            INamedTypeSymbol type,
            SemanticModel model,
            ref INamedTypeSymbol? lazyLayoutAttribute)
        {
            if (type.TypeKind == TypeKind.Struct)
            {
                return true;
            }

            if (type.TypeKind != TypeKind.Class)
            {
                return false;
            }

            // Fields can't be inserted into a class with explicit or sequential layout
            var attributes = type.GetAttributes();
            if (attributes.Length == 0)
            {
                return false;
            }

            lazyLayoutAttribute ??= model.Compilation.GetTypeByMetadataName(typeof(StructLayoutAttribute).FullName!);
            if (lazyLayoutAttribute == null)
            {
                return false;
            }

            foreach (var attribute in attributes)
            {
                RoslynDebug.Assert(attribute.AttributeClass is object);
                if (attribute.AttributeClass.Equals(lazyLayoutAttribute) && attribute.ConstructorArguments.Length == 1)
                {
                    var layoutValue = attribute.ConstructorArguments.Single().Value;
                    return (layoutValue is int ? (int)layoutValue :
                            layoutValue is short ? (short)layoutValue :
                            (int)LayoutKind.Auto) != (int)LayoutKind.Auto;
                }
            }

            return false;
        }

        #endregion

        private static Func<SyntaxNode, SyntaxNode?> CreateSyntaxMapForEquivalentNodes(MemberBody oldBody, MemberBody newBody)
        {
            var oldRootNodes = oldBody.RootNodes;
            var newRootNodes = newBody.RootNodes;

            return newNode => FindPartner(oldRootNodes, newRootNodes, newNode);
        }

        private static Func<SyntaxNode, SyntaxNode?> CreateSyntaxMap(IReadOnlyDictionary<SyntaxNode, SyntaxNode> reverseMap)
            => newNode => reverseMap.TryGetValue(newNode, out var oldNode) ? oldNode : null;

        private Func<SyntaxNode, SyntaxNode?>? CreateAggregateSyntaxMap(
            IReadOnlyDictionary<SyntaxNode, SyntaxNode> reverseTopMatches,
            IReadOnlyDictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode?>?> changedDeclarations)
        {
            return newNode =>
            {
                // containing declaration
                if (!TryFindMemberDeclaration(root: null, newNode, out var newDeclarations))
                {
                    return null;
                }

                foreach (var newDeclaration in newDeclarations)
                {
                    // The node is in a field, property or constructor declaration that has been changed:
                    if (changedDeclarations.TryGetValue(newDeclaration, out var syntaxMap))
                    {
                        // If syntax map is not available the declaration was either
                        // 1) updated but is not active
                        // 2) inserted
                        return syntaxMap?.Invoke(newNode);
                    }

                    // The node is in a declaration that hasn't been changed:
                    if (reverseTopMatches.TryGetValue(newDeclaration, out var oldDeclaration))
                    {
                        var oldBody = TryGetDeclarationBody(oldDeclaration);
                        var newBody = TryGetDeclarationBody(newDeclaration);

                        // The declarations must have bodies since we found newNode in the newDeclaration's body
                        // and the new body can only differ from the old one in trivia.
                        Debug.Assert(oldBody != null);
                        Debug.Assert(newBody != null);

                        return FindPartner(oldBody.RootNodes, newBody.RootNodes, newNode);
                    }
                }

                return null;
            };
        }

        #region Constructors and Initializers

        private void AddConstructorEdits(
            IReadOnlyDictionary<INamedTypeSymbol, MemberInitializationUpdates> updatedTypes,
            Match<SyntaxNode> topMatch,
            SemanticModel? oldModel,
            Compilation oldCompilation,
            SemanticModel newModel,
            HashSet<ISymbol> processedSymbols,
            EditAndContinueCapabilitiesGrantor capabilities,
            bool isStatic,
            [Out] ArrayBuilder<SemanticEditInfo> semanticEdits,
            [Out] ArrayBuilder<RudeEditDiagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            var oldSyntaxTree = topMatch.OldRoot.SyntaxTree;
            var newSyntaxTree = topMatch.NewRoot.SyntaxTree;

            foreach (var (newType, updatesInCurrentDocument) in updatedTypes)
            {
                var oldType = updatesInCurrentDocument.OldType;

                var anyInitializerUpdatesInCurrentDocument = updatesInCurrentDocument.ChangedDeclarations.Keys.Any(IsDeclarationWithInitializer) || updatesInCurrentDocument.HasDeletedMemberInitializer;
                var isPartialEdit = IsPartialEdit(oldType, newType, oldSyntaxTree, newSyntaxTree);
                var typeKey = SymbolKey.Create(newType, cancellationToken);
                var partialType = isPartialEdit ? typeKey : (SymbolKey?)null;

                // Create a syntax map that aggregates syntax maps of the constructor body and all initializers in this document.
                // Use syntax maps stored in update.ChangedDeclarations and fallback to 1:1 map for unchanged members.
                //
                // This aggregated map will be combined with similar maps capturing members declared in partial type declarations
                // located in other documents when the semantic edits are merged across all changed documents of the project.
                //
                // We will create an aggregate syntax map even in cases when we don't necessarily need it,
                // for example if none of the edited declarations are active. It's ok to have a map that we don't need.
                // This is simpler than detecting whether or not some of the initializers/constructors contain active statements.
                var aggregateSyntaxMap = CreateAggregateSyntaxMap(topMatch.ReverseMatches, updatesInCurrentDocument.ChangedDeclarations);

                bool? lazyOldTypeHasMemberInitializerContainingLambda = null;

                foreach (var newCtor in isStatic ? newType.StaticConstructors : newType.InstanceConstructors)
                {
                    if (processedSymbols.Contains(newCtor))
                    {
                        // we already have an edit for the new constructor
                        continue;
                    }

                    if (newType.TypeKind != oldType.TypeKind || oldType.IsRecord != newType.IsRecord)
                    {
                        // rude edit has been reported when changing type kinds
                        continue;
                    }

                    // Constructor that doesn't contain initializers had a corresponding semantic edit produced previously 
                    // or was not edited. In either case we should not produce a semantic edit for it.
                    if (!IsConstructorWithMemberInitializers(newCtor, cancellationToken))
                    {
                        continue;
                    }

                    var newCtorKey = SymbolKey.Create(newCtor, cancellationToken);

                    var syntaxMapToUse = aggregateSyntaxMap;

                    SyntaxNode? newDeclaration = null;
                    IMethodSymbol? oldCtor;
                    if (!newCtor.IsImplicitlyDeclared)
                    {
                        // Constructors have to have a single declaration syntax, they can't be partial
                        newDeclaration = GetSymbolDeclarationSyntax(newCtor, cancellationToken);

                        // If no initializer updates were made in the type we only need to produce semantic edits for constructors
                        // whose body has been updated, otherwise we need to produce edits for all constructors that include initializers.
                        // If changes were made to initializers or constructors of a partial type in another document they will be merged
                        // when aggregating semantic edits from all changed documents. Rude edits resulting from those changes, if any, will
                        // be reported in the document they were made in.
                        if (!anyInitializerUpdatesInCurrentDocument && !updatesInCurrentDocument.ChangedDeclarations.ContainsKey(newDeclaration))
                        {
                            continue;
                        }

                        // To avoid costly SymbolKey resolution we first try to match the constructor in the current document
                        // and special case parameter-less constructor.

                        if (topMatch.TryGetOldNode(newDeclaration, out var oldDeclaration))
                        {
                            Contract.ThrowIfNull(oldModel);

                            oldCtor = (IMethodSymbol)GetRequiredDeclaredSymbol(oldModel, oldDeclaration, cancellationToken);
                            Contract.ThrowIfFalse(oldCtor is { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor });
                        }
                        else if (newCtor.Parameters.Length == 0)
                        {
                            oldCtor = TryGetParameterlessConstructor(oldType, isStatic);
                        }
                        else
                        {
                            var resolution = newCtorKey.Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken);

                            // There may be semantic errors in the compilation that result in multiple candidates.
                            // Pick the first candidate.

                            oldCtor = (IMethodSymbol?)resolution.Symbol;
                        }

                        if (oldCtor == null && HasMemberInitializerContainingLambda(oldType, isStatic, ref lazyOldTypeHasMemberInitializerContainingLambda, cancellationToken))
                        {
                            // TODO (bug https://github.com/dotnet/roslyn/issues/2504)
                            // rude edit: Adding a constructor to a type with a field or property initializer that contains an anonymous function
                            diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, GetDiagnosticSpan(newDeclaration, EditKind.Insert)));
                            continue;
                        }

                        // Report an error if the updated constructor's declaration is in the current document 
                        // and its body edit is disallowed (e.g. contains stackalloc).
                        if (oldCtor != null && newDeclaration.SyntaxTree == newSyntaxTree && anyInitializerUpdatesInCurrentDocument)
                        {
                            var newBody = TryGetDeclarationBody(newDeclaration);

                            // If the declaration represents a primary constructor the body will be null.
                            if (newBody != null)
                            {
                                // attribute rude edit to one of the modified members
                                var firstSpan = updatesInCurrentDocument.ChangedDeclarations.Keys.Where(IsDeclarationWithInitializer).Aggregate(
                                    (min: int.MaxValue, span: default(TextSpan)),
                                    (accumulate, node) => (node.SpanStart < accumulate.min) ? (node.SpanStart, node.Span) : accumulate).span;

                                // span may be empty if the only edits are deletes of members with initializers
                                if (firstSpan.IsEmpty)
                                {
                                    Debug.Assert(updatesInCurrentDocument.HasDeletedMemberInitializer);
                                    firstSpan = newDeclaration.Span;
                                }

                                ReportMemberOrLambdaBodyUpdateRudeEditsImpl(diagnostics, newDeclaration, newBody, firstSpan);
                            }
                        }
                    }
                    else
                    {
                        // New constructor contains initializers and is implicitly declared so it must be parameterless.
                        Debug.Assert(newCtor.Parameters.IsEmpty);

                        oldCtor = TryGetParameterlessConstructor(oldType, isStatic);

                        // Skip update if both old and new are implicitly declared and no initializer updates were made in current document.
                        // If changes were made to initializers or constructors of a partial type in another document they will be merged
                        // when aggregating semantic edits from all changed documents.
                        if (!anyInitializerUpdatesInCurrentDocument && oldCtor is { IsImplicitlyDeclared: true })
                        {
                            continue;
                        }
                    }

                    if (oldCtor != null)
                    {
                        AnalyzeSymbolUpdate(oldCtor, newCtor, newDeclaration, newModel, topMatch, capabilities, diagnostics, semanticEdits, syntaxMapToUse, processedSymbols, cancellationToken);

                        semanticEdits.Add(new SemanticEditInfo(
                            SemanticEditKind.Update,
                            newCtorKey,
                            syntaxMapToUse,
                            syntaxMapTree: isPartialEdit ? newSyntaxTree : null,
                            partialType: partialType));
                    }
                    else
                    {
                        semanticEdits.Add(new SemanticEditInfo(
                            SemanticEditKind.Insert,
                            newCtorKey,
                            syntaxMap: null,
                            syntaxMapTree: null,
                            partialType: null));
                    }

                    // primary record constructor updated to non-primary and vice versa:
                    if (newType.IsRecord)
                    {
                        var oldCtorIsPrimary = oldCtor != null && IsPrimaryConstructor(oldCtor, cancellationToken);
                        var newCtorIsPrimary = IsPrimaryConstructor(newCtor, cancellationToken);

                        if (oldCtorIsPrimary != newCtorIsPrimary)
                        {
                            // Deconstructor:
                            AddDeconstructorEdits(semanticEdits, oldCtor, newCtor, typeKey, oldCompilation, newModel.Compilation, syntaxMap: null, processedSymbols, isParameterDelete: newCtorIsPrimary, cancellationToken);

                            // Synthesized method updates:
                            AddSynthesizedRecordMethodUpdatesForPropertyChange(semanticEdits, newModel.Compilation, newCtor.ContainingType, cancellationToken);
                        }
                    }
                }

                if (!isStatic && oldType.TypeKind == TypeKind.Class && newType.TypeKind == TypeKind.Class)
                {
                    // Adding the first instance constructor with parameters suppresses synthesized default constructor.
                    if (oldType.HasSynthesizedDefaultConstructor() && !newType.HasSynthesizedDefaultConstructor())
                    {
                        semanticEdits.Add(new SemanticEditInfo(
                            SemanticEditKind.Delete,
                            SymbolKey.Create(oldType.InstanceConstructors.Single(c => c.Parameters is []), cancellationToken),
                            syntaxMap: null,
                            syntaxMapTree: null,
                            partialType: partialType,
                            deletedSymbolContainer: typeKey));
                    }

                    // Removing the last instance constructor with parameters inserts synthesized default constructor.
                    // We don't need to add an explicit semantic edit for synthesized members though since the compiler
                    // emits them automatically.
                }
            }
        }

        private bool HasMemberInitializerContainingLambda(INamedTypeSymbol type, bool isStatic, ref bool? lazyHasMemberInitializerContainingLambda, CancellationToken cancellationToken)
        {
            // checking the old type for existing lambdas (it's ok for the new initializers to contain lambdas)
            lazyHasMemberInitializerContainingLambda ??= HasMemberInitializerContainingLambda(type, isStatic, cancellationToken);

            return lazyHasMemberInitializerContainingLambda.Value;
        }

        private bool HasMemberInitializerContainingLambda(INamedTypeSymbol type, bool isStatic, CancellationToken cancellationToken)
        {
            // checking the old type for existing lambdas (it's ok for the new initializers to contain lambdas)

            foreach (var member in type.GetMembers())
            {
                if (member.IsStatic == isStatic &&
                    (member.Kind == SymbolKind.Field || member.Kind == SymbolKind.Property) &&
                    member.DeclaringSyntaxReferences.Length > 0) // skip generated fields (e.g. VB auto-property backing fields)
                {
                    var syntax = GetSymbolDeclarationSyntax(member, cancellationToken);
                    if (IsDeclarationWithInitializer(syntax) && TryGetDeclarationBody(syntax) is { } body && ContainsLambda(body))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static IMethodSymbol? TryGetParameterlessConstructor(INamedTypeSymbol type, bool isStatic)
        {
            var oldCtors = isStatic ? type.StaticConstructors : type.InstanceConstructors;
            if (isStatic)
            {
                return type.StaticConstructors.FirstOrDefault();
            }
            else
            {
                return type.InstanceConstructors.FirstOrDefault(m => m.Parameters.Length == 0);
            }
        }

        private static bool IsPartialEdit(ISymbol? oldSymbol, ISymbol? newSymbol, SyntaxTree oldSyntaxTree, SyntaxTree newSyntaxTree)
        {
            // If any of the partial declarations of the new or the old type are in another document
            // the edit will need to be merged with other partial edits with matching partial type
            static bool IsNotInDocument(SyntaxReference reference, SyntaxTree syntaxTree)
                => reference.SyntaxTree != syntaxTree;

            return oldSymbol?.Kind == SymbolKind.NamedType && oldSymbol.DeclaringSyntaxReferences.Length > 1 && oldSymbol.DeclaringSyntaxReferences.Any(IsNotInDocument, oldSyntaxTree) ||
                   newSymbol?.Kind == SymbolKind.NamedType && newSymbol.DeclaringSyntaxReferences.Length > 1 && newSymbol.DeclaringSyntaxReferences.Any(IsNotInDocument, newSyntaxTree);
        }

        #endregion

        #region Lambdas and Closures

        private void ReportLambdaAndClosureRudeEdits(
            SemanticModel oldModel,
            ISymbol oldMember,
            MemberBody oldMemberBody,
            SyntaxNode oldDeclaration,
            SemanticModel newModel,
            ISymbol newMember,
            MemberBody newMemberBody,
            SyntaxNode newDeclaration,
            Match<SyntaxNode> memberBodyMatch,
            IReadOnlyDictionary<LambdaBody, LambdaInfo>? matchedLambdas,
            BidirectionalMap<SyntaxNode> map,
            EditAndContinueCapabilitiesGrantor capabilities,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            out bool syntaxMapRequired,
            CancellationToken cancellationToken)
        {
            syntaxMapRequired = false;

            if (matchedLambdas != null)
            {
                foreach (var (oldLambdaBody, newLambdaInfo) in matchedLambdas)
                {
                    var newLambdaBody = newLambdaInfo.NewBody;
                    if (newLambdaBody == null)
                    {
                        continue;
                    }

                    var lambdaBodyMatch = newLambdaInfo.Match;
                    Debug.Assert(lambdaBodyMatch != null);

                    var oldLambda = oldLambdaBody.GetLambda();
                    var newLambda = newLambdaBody.GetLambda();

                    var oldStateMachineInfo = oldLambdaBody.GetStateMachineInfo();
                    var newStateMachineInfo = newLambdaBody.GetStateMachineInfo();
                    ReportStateMachineBodyUpdateRudeEdits(lambdaBodyMatch, oldStateMachineInfo, newLambda, newStateMachineInfo, newLambdaInfo.HasActiveStatement, diagnostics);

                    // When the delta IL of the containing method is emitted lambdas declared in it are also emitted.
                    // If the runtime does not support changing IL of the method (e.g. method containing stackalloc)
                    // we need to report a rude edit.
                    // If only trivia change the IL is going to be unchanged and only sequence points in the PDB change,
                    // so we do not report rude edits.

                    if (!oldLambdaBody.IsSyntaxEquivalentTo(newLambdaBody))
                    {
                        ReportMemberOrLambdaBodyUpdateRudeEdits(
                            diagnostics,
                            oldModel,
                            oldLambda,
                            oldMember,
                            newLambda,
                            newLambdaBody,
                            newMember,
                            memberBodyMatch,
                            capabilities,
                            oldStateMachineInfo,
                            newStateMachineInfo);

                        if ((IsGenericLocalFunction(oldLambda) || IsGenericLocalFunction(newLambda)) &&
                            !capabilities.Grant(EditAndContinueCapabilities.GenericUpdateMethod))
                        {
                            diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, GetDiagnosticSpan(newLambda, EditKind.Update), newLambda, new[] { GetDisplayName(newLambda) }));
                        }
                    }
                }

                var anySignatureErrors = false;
                foreach (var (oldLambdaBody, newLambdaInfo) in matchedLambdas)
                {
                    // Any unmatched lambdas would have contained an active statement and a rude edit would be reported in syntax analysis phase.
                    // Skip the rest of lambda and closure analysis if such lambdas are present.
                    if (newLambdaInfo.Match == null || newLambdaInfo.NewBody == null)
                    {
                        return;
                    }

                    ReportLambdaSignatureRudeEdits(diagnostics, oldModel, oldLambdaBody, newModel, newLambdaInfo.NewBody, capabilities, out var hasErrors, cancellationToken);
                    anySignatureErrors |= hasErrors;
                }

                ArrayBuilder<SyntaxNode>? lazyNewErroneousClauses = null;
                foreach (var (oldQueryClause, newQueryClause) in map.Forward)
                {
                    if (!QueryClauseLambdasTypeEquivalent(oldModel, oldQueryClause, newModel, newQueryClause, cancellationToken))
                    {
                        lazyNewErroneousClauses ??= ArrayBuilder<SyntaxNode>.GetInstance();
                        lazyNewErroneousClauses.Add(newQueryClause);
                    }
                }

                if (lazyNewErroneousClauses != null)
                {
                    foreach (var newQueryClause in from clause in lazyNewErroneousClauses
                                                   orderby clause.SpanStart
                                                   group clause by GetContainingQueryExpression(clause) into clausesByQuery
                                                   select clausesByQuery.First())
                    {
                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.ChangingQueryLambdaType,
                            GetDiagnosticSpan(newQueryClause, EditKind.Update),
                            newQueryClause,
                            new[] { GetDisplayName(newQueryClause, EditKind.Update) }));
                    }

                    lazyNewErroneousClauses.Free();
                    anySignatureErrors = true;
                }

                // only dig into captures if lambda signatures match
                if (anySignatureErrors)
                {
                    return;
                }
            }

            using var oldLambdaBodyEnumerator = GetLambdaBodies(oldMemberBody).GetEnumerator();
            using var newLambdaBodyEnumerator = GetLambdaBodies(newMemberBody).GetEnumerator();
            var oldHasLambdasOrLocalFunctions = oldLambdaBodyEnumerator.MoveNext();
            var newHasLambdasOrLocalFunctions = newLambdaBodyEnumerator.MoveNext();

            // Exit early if there are no lambdas in the method to avoid expensive data flow analysis:
            if (!oldHasLambdasOrLocalFunctions && !newHasLambdasOrLocalFunctions)
            {
                return;
            }

            var oldCaptures = oldMemberBody.GetCapturedVariables(oldModel);
            var newCaptures = newMemberBody.GetCapturedVariables(newModel);

            // { new capture index -> old capture index }
            using var _1 = ArrayBuilder<int>.GetInstance(newCaptures.Length, fillWithValue: 0, out var reverseCapturesMap);

            // { new capture index -> new closure scope or null for "this" }
            using var _2 = ArrayBuilder<SyntaxNode?>.GetInstance(newCaptures.Length, fillWithValue: null, out var newCapturesToClosureScopes);

            // Can be calculated from other maps but it's simpler to just calculate it upfront.
            // { old capture index -> old closure scope or null for "this" }
            using var _3 = ArrayBuilder<SyntaxNode?>.GetInstance(oldCaptures.Length, fillWithValue: null, out var oldCapturesToClosureScopes);

            CalculateCapturedVariablesMaps(
                oldCaptures,
                oldMember,
                oldDeclaration,
                newCaptures,
                newMember,
                newDeclaration,
                map,
                reverseCapturesMap,
                newCapturesToClosureScopes,
                oldCapturesToClosureScopes,
                diagnostics,
                out var anyCaptureErrors,
                cancellationToken);

            if (anyCaptureErrors)
            {
                return;
            }

            // Every captured variable accessed in the new lambda has to be 
            // accessed in the old lambda as well and vice versa.
            //
            // An added lambda can only reference captured variables that 
            //
            // This requirement ensures that:
            // - Lambda methods are generated to the same frame as before, so they can be updated in-place.
            // - "Parent" links between closure scopes are preserved.

            using var _11 = PooledDictionary<ISymbol, int>.GetInstance(out var oldCapturesIndex);
            using var _12 = PooledDictionary<ISymbol, int>.GetInstance(out var newCapturesIndex);

            BuildIndex(oldCapturesIndex, oldCaptures);
            BuildIndex(newCapturesIndex, newCaptures);

            if (matchedLambdas != null)
            {
                var mappedLambdasHaveErrors = false;
                foreach (var (oldLambdaBody, newLambdaInfo) in matchedLambdas)
                {
                    var newLambdaBody = newLambdaInfo.NewBody;

                    // The map now contains only matched lambdas. Any unmatched ones would have contained an active statement and 
                    // a rude edit would be reported in syntax analysis phase.
                    RoslynDebug.Assert(newLambdaInfo.Match != null && newLambdaBody != null);

                    var accessedOldCaptures = GetAccessedCaptures(oldLambdaBody, oldModel, oldCaptures, oldCapturesIndex);
                    var accessedNewCaptures = GetAccessedCaptures(newLambdaBody, newModel, newCaptures, newCapturesIndex);

                    // Requirement: 
                    // (new(ReadInside) \/ new(WrittenInside)) /\ new(Captured) == (old(ReadInside) \/ old(WrittenInside)) /\ old(Captured)
                    for (var newCaptureIndex = 0; newCaptureIndex < newCaptures.Length; newCaptureIndex++)
                    {
                        var newAccessed = accessedNewCaptures[newCaptureIndex];
                        var oldAccessed = accessedOldCaptures[reverseCapturesMap[newCaptureIndex]];

                        if (newAccessed != oldAccessed)
                        {
                            var newCapture = newCaptures[newCaptureIndex];

                            var rudeEdit = newAccessed ? RudeEditKind.AccessingCapturedVariableInLambda : RudeEditKind.NotAccessingCapturedVariableInLambda;
                            var arguments = new[] { newCapture.Name, GetDisplayName(newLambdaBody.GetLambda()) };

                            if (newCapture.IsThisParameter() || oldAccessed)
                            {
                                // changed accessed to "this", or captured variable accessed in old lambda is not accessed in the new lambda
                                diagnostics.Add(new RudeEditDiagnostic(rudeEdit, GetDiagnosticSpan(newLambdaBody.GetLambda(), EditKind.Update), null, arguments));
                            }
                            else if (newAccessed)
                            {
                                // captured variable accessed in new lambda is not accessed in the old lambda
                                var hasUseSites = false;
                                foreach (var useSite in GetVariableUseSites(newLambdaBody.GetExpressionsAndStatements(), newCapture, newModel, cancellationToken))
                                {
                                    hasUseSites = true;
                                    diagnostics.Add(new RudeEditDiagnostic(rudeEdit, useSite.Span, null, arguments));
                                }

                                Debug.Assert(hasUseSites);
                            }

                            mappedLambdasHaveErrors = true;
                        }
                    }
                }

                if (mappedLambdasHaveErrors)
                {
                    return;
                }
            }

            // Removal: We don't allow removal of lambda that has captures from multiple scopes.

            var oldHasLambdas = false;
            var oldHasMoreLambdas = oldHasLambdasOrLocalFunctions;
            while (oldHasMoreLambdas)
            {
                var (oldLambda, oldLambdaBody1, oldLambdaBody2) = oldLambdaBodyEnumerator.Current;

                oldHasLambdas |= !IsLocalFunction(oldLambda);

                if (!map.Forward.ContainsKey(oldLambda))
                {
                    ReportMultiScopeCaptures(oldLambdaBody1, oldModel, oldCaptures, newCaptures, oldCapturesToClosureScopes, oldCapturesIndex, reverseCapturesMap, diagnostics, isInsert: false, cancellationToken: cancellationToken);

                    if (oldLambdaBody2 != null)
                    {
                        ReportMultiScopeCaptures(oldLambdaBody2, oldModel, oldCaptures, newCaptures, oldCapturesToClosureScopes, oldCapturesIndex, reverseCapturesMap, diagnostics, isInsert: false, cancellationToken: cancellationToken);
                    }
                }

                oldHasMoreLambdas = oldLambdaBodyEnumerator.MoveNext();
            }

            // Report rude edits for lambdas added to the method.
            // We already checked that no new captures are introduced or removed. 
            // We also need to make sure that no new parent frame links are introduced.
            // 
            // We could implement the same analysis as the compiler does when rewriting lambdas - 
            // to determine what closure scopes are connected at runtime via parent link, 
            // and then disallow adding a lambda that connects two previously unconnected 
            // groups of scopes.
            //
            // However even if we implemented that logic here, it would be challenging to 
            // present the result of the analysis to the user in a short comprehensible error message.
            // 
            // In practice, we believe the common scenarios are (in order of commonality):
            // 1) adding a static lambda
            // 2) adding a lambda that accesses only "this"
            // 3) adding a lambda that accesses variables from the same scope
            // 4) adding a lambda that accesses "this" and variables from a single scope
            // 5) adding a lambda that accesses variables from different scopes that are linked
            // 6) adding a lambda that accesses variables from unlinked scopes
            // 
            // We currently allow #1, #2, and #3 and report a rude edit for the other cases.
            // In future we might be able to enable more.

            var containingTypeDeclaration = TryGetContainingTypeDeclaration(newDeclaration);
            var isInInterfaceDeclaration = containingTypeDeclaration != null && IsInterfaceDeclaration(containingTypeDeclaration);
            var isNewMemberInGenericContext = InGenericContext(newMember);

            var newHasLambdaBodies = newHasLambdasOrLocalFunctions;
            while (newHasLambdaBodies)
            {
                var (newLambda, newLambdaBody1, newLambdaBody2) = newLambdaBodyEnumerator.Current;

                if (!map.Reverse.ContainsKey(newLambda))
                {
                    if (!CanAddNewLambda(newLambda, newLambdaBody1, newLambdaBody2))
                    {
                        diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.InsertNotSupportedByRuntime, GetDiagnosticSpan(newLambda, EditKind.Insert), newLambda, new string[] { GetDisplayName(newLambda, EditKind.Insert) }));
                    }

                    // TODO: https://github.com/dotnet/roslyn/issues/37128
                    // Local functions are emitted directly to the type containing the containing method.
                    // Although local functions are non-virtual the Core CLR currently does not support adding any method to an interface.
                    if (isInInterfaceDeclaration && IsLocalFunction(newLambda))
                    {
                        diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.InsertLocalFunctionIntoInterfaceMethod, GetDiagnosticSpan(newLambda, EditKind.Insert), newLambda, new string[] { GetDisplayName(newLambda, EditKind.Insert) }));
                    }

                    ReportMultiScopeCaptures(newLambdaBody1, newModel, newCaptures, newCaptures, newCapturesToClosureScopes, newCapturesIndex, reverseCapturesMap, diagnostics, isInsert: true, cancellationToken: cancellationToken);

                    if (newLambdaBody2 != null)
                    {
                        ReportMultiScopeCaptures(newLambdaBody2, newModel, newCaptures, newCaptures, newCapturesToClosureScopes, newCapturesIndex, reverseCapturesMap, diagnostics, isInsert: true, cancellationToken: cancellationToken);
                    }
                }

                newHasLambdaBodies = newLambdaBodyEnumerator.MoveNext();
            }

            syntaxMapRequired = newHasLambdasOrLocalFunctions;

            bool CanAddNewLambda(SyntaxNode newLambda, LambdaBody newLambdaBody1, LambdaBody? newLambdaBody2)
            {
                // Adding a lambda/local function might result in 
                // 1) emitting a new closure type 
                // 2) adding method and a static field to an existing closure type
                // 
                // We currently do not precisely determine whether or not a suitable closure type already exists
                // as static closure types might be shared with lambdas defined in a different member of the containing type.
                // See: https://github.com/dotnet/roslyn/issues/52759
                //
                // Furthermore, if a new lambda captures a variable that is alredy captured by a local function then
                // the closure type is converted from struct local function closure to a lambda display class.
                // Similarly, when a new conversion from local function group to a delegate is added the closure type also changes.
                // Both should be reported as rude edits during capture analysis.
                // See https://github.com/dotnet/roslyn/issues/67323

                var isLocalFunction = IsLocalFunction(newLambda);

                // We assume that [2] is always required since the closure type might already exist.
                var requiredCapabilities = EditAndContinueCapabilities.AddMethodToExistingType;

                var inGenericLocalContext = InGenericLocalContext(newLambda, newMemberBody.RootNodes);

                if (isNewMemberInGenericContext || inGenericLocalContext)
                {
                    requiredCapabilities |= EditAndContinueCapabilities.GenericAddMethodToExistingType;
                }

                // Static lambdas are cached in static fields, unless in generic local functions.
                // If either body is static we need to require the capabilities.
                var isLambdaCachedInField =
                    !inGenericLocalContext &&
                    !isLocalFunction &&
                    (GetAccessedCaptures(newLambdaBody1, newModel, newCaptures, newCapturesIndex).Equals(BitVector.Empty) ||
                        newLambdaBody2 != null && GetAccessedCaptures(newLambdaBody2, newModel, newCaptures, newCapturesIndex).Equals(BitVector.Empty));

                if (isLambdaCachedInField)
                {
                    requiredCapabilities |= EditAndContinueCapabilities.AddStaticFieldToExistingType;

                    // If we are in a generic type or a member then the closure type is generic and we are adding a static field to a generic type.
                    if (isNewMemberInGenericContext)
                    {
                        requiredCapabilities |= EditAndContinueCapabilities.GenericAddFieldToExistingType;
                    }
                }

                // If the old verison of the method had any lambdas the nwe know a closure type exists and a new one isn't needed.
                // We also know that adding a local function won't create a new closure type.
                // Otherwise, we assume a new type is needed.

                if (!oldHasLambdas && !isLocalFunction)
                {
                    requiredCapabilities |= EditAndContinueCapabilities.NewTypeDefinition;
                }

                return capabilities.Grant(requiredCapabilities);
            }
        }

        private IEnumerable<(SyntaxNode lambda, LambdaBody lambdaBody1, LambdaBody? lambdaBody2)> GetLambdaBodies(MemberBody body)
        {
            foreach (var root in body.RootNodes)
            {
                foreach (var node in root.DescendantNodesAndSelf())
                {
                    if (TryGetLambdaBodies(node, out var body1, out var body2))
                    {
                        yield return (node, body1, body2);
                    }
                }
            }
        }

        private void ReportMultiScopeCaptures(
            LambdaBody lambdaBody,
            SemanticModel model,
            ImmutableArray<ISymbol> captures,
            ImmutableArray<ISymbol> newCaptures,
            ArrayBuilder<SyntaxNode?> newCapturesToClosureScopes,
            PooledDictionary<ISymbol, int> capturesIndex,
            ArrayBuilder<int> reverseCapturesMap,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            bool isInsert,
            CancellationToken cancellationToken)
        {
            if (captures.Length == 0)
            {
                return;
            }

            var accessedCaptures = GetAccessedCaptures(lambdaBody, model, captures, capturesIndex);

            var firstAccessedCaptureIndex = -1;
            for (var i = 0; i < captures.Length; i++)
            {
                if (accessedCaptures[i])
                {
                    if (firstAccessedCaptureIndex == -1)
                    {
                        firstAccessedCaptureIndex = i;
                    }
                    else if (newCapturesToClosureScopes[firstAccessedCaptureIndex] != newCapturesToClosureScopes[i])
                    {
                        // the lambda accesses variables from two different scopes:

                        TextSpan errorSpan;
                        RudeEditKind rudeEdit;
                        if (isInsert)
                        {
                            if (captures[i].IsThisParameter())
                            {
                                errorSpan = GetDiagnosticSpan(lambdaBody.GetLambda(), EditKind.Insert);
                            }
                            else
                            {
                                errorSpan = GetVariableUseSites(lambdaBody.GetExpressionsAndStatements(), captures[i], model, cancellationToken).First().Span;
                            }

                            rudeEdit = RudeEditKind.InsertLambdaWithMultiScopeCapture;
                        }
                        else
                        {
                            errorSpan = GetSymbolLocationSpan(newCaptures[reverseCapturesMap.IndexOf(i)], cancellationToken);
                            rudeEdit = RudeEditKind.DeleteLambdaWithMultiScopeCapture;
                        }

                        diagnostics.Add(new RudeEditDiagnostic(
                            rudeEdit,
                            errorSpan,
                            null,
                            new[] { GetDisplayName(lambdaBody.GetLambda()), captures[firstAccessedCaptureIndex].Name, captures[i].Name }));

                        break;
                    }
                }
            }
        }

        private static BitVector GetAccessedCaptures(LambdaBody lambdaBody, SemanticModel model, ImmutableArray<ISymbol> captures, PooledDictionary<ISymbol, int> capturesIndex)
        {
            var result = BitVector.Create(captures.Length);

            foreach (var expressionOrStatement in lambdaBody.GetExpressionsAndStatements())
            {
                var dataFlow = model.AnalyzeDataFlow(expressionOrStatement);
                MarkVariables(ref result, dataFlow.ReadInside, capturesIndex);
                MarkVariables(ref result, dataFlow.WrittenInside, capturesIndex);
            }

            return result;
        }

        private static void MarkVariables(ref BitVector mask, ImmutableArray<ISymbol> variables, Dictionary<ISymbol, int> index)
        {
            foreach (var variable in variables)
            {
                if (index.TryGetValue(variable, out var newCaptureIndex))
                {
                    mask[newCaptureIndex] = true;
                }
            }
        }

        private static void BuildIndex<TKey>(Dictionary<TKey, int> index, ImmutableArray<TKey> array)
            where TKey : notnull
        {
            for (var i = 0; i < array.Length; i++)
            {
                index.Add(array[i], i);
            }
        }

        /// <summary>
        /// Returns node that represents a declaration of the symbol.
        /// </summary>
        protected abstract SyntaxNode GetSymbolDeclarationSyntax(ISymbol symbol, Func<ImmutableArray<SyntaxReference>, SyntaxReference> selector, CancellationToken cancellationToken);

        protected SyntaxNode GetSymbolDeclarationSyntax(ISymbol symbol, CancellationToken cancellationToken)
            => GetSymbolDeclarationSyntax(symbol, selector: System.Linq.ImmutableArrayExtensions.First, cancellationToken);

        protected abstract ISymbol? GetDeclaredSymbol(SemanticModel model, SyntaxNode declaration, CancellationToken cancellationToken);

        protected ISymbol GetRequiredDeclaredSymbol(SemanticModel model, SyntaxNode declaration, CancellationToken cancellationToken)
        {
            var symbol = GetDeclaredSymbol(model, declaration, cancellationToken);
            Contract.ThrowIfNull(symbol);
            return symbol;
        }

        private TextSpan GetSymbolLocationSpan(ISymbol symbol, CancellationToken cancellationToken)
        {
            // Note that in VB implicit value parameter in property setter doesn't have a location.
            // In C# its location is the location of the setter.
            // See https://github.com/dotnet/roslyn/issues/14273
            return IsGlobalMain(symbol) ? GetDiagnosticSpan(GetSymbolDeclarationSyntax(symbol, cancellationToken), EditKind.Update) :
                   symbol is IParameterSymbol && IsGlobalMain(symbol.ContainingSymbol) ? GetDiagnosticSpan(GetSymbolDeclarationSyntax(symbol.ContainingSymbol, cancellationToken), EditKind.Update) :
                   symbol.Locations.FirstOrDefault()?.SourceSpan ?? symbol.ContainingSymbol.Locations.First().SourceSpan;
        }

        private CapturedParameterKey GetParameterKey(IParameterSymbol parameter, CancellationToken cancellationToken)
        {
            Debug.Assert(!parameter.IsThis);

            if (parameter.IsImplicitValueParameter())
            {
                return new CapturedParameterKey(ParameterKind.Value);
            }

            if (IsGlobalMain(parameter.ContainingSymbol))
            {
                return new CapturedParameterKey(ParameterKind.TopLevelMainArgs);
            }

            var lambda = parameter.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.LambdaMethod or MethodKind.LocalFunction } containingLambda ?
                GetSymbolDeclarationSyntax(containingLambda, cancellationToken) : null;

            // Indexer parameters in the getter/setter are implicitly declared.
            // We need to find the corresponding parameter of the indexer itself.
            if (parameter is { IsImplicitlyDeclared: true, ContainingSymbol: IMethodSymbol { AssociatedSymbol: { } associatedSymbol } })
            {
                parameter = associatedSymbol.GetParameters().Single(p => p.Name == parameter.Name);
            }

            return new CapturedParameterKey(ParameterKind.Explicit, GetSymbolDeclarationSyntax(parameter, cancellationToken), lambda);
        }

        private static bool TryMapParameter(
            CapturedParameterKey parameterKey,
            IReadOnlyDictionary<SyntaxNode, SyntaxNode>? parameterMap,
            IReadOnlyDictionary<SyntaxNode, SyntaxNode> bodyMap,
            out CapturedParameterKey mappedParameterKey)
        {
            if (parameterKey.ContainingLambda == null)
            {
                // method or primary constructor parameter:
                SyntaxNode? mappedParameter = null;
                if (parameterKey.Syntax == null || parameterMap?.TryGetValue(parameterKey.Syntax, out mappedParameter) == true)
                {
                    mappedParameterKey = parameterKey with { Syntax = mappedParameter };
                    return true;
                }
            }
            else if (bodyMap.TryGetValue(parameterKey.ContainingLambda, out var mappedContainingLambdaSyntax))
            {
                // lambda or local function parameter:
                Debug.Assert(parameterKey.Syntax != null);

                if (bodyMap.TryGetValue(parameterKey.Syntax, out var mappedParameter))
                {
                    mappedParameterKey = parameterKey with { Syntax = mappedParameter, ContainingLambda = mappedContainingLambdaSyntax };
                    return true;
                }
            }

            // no mapping
            mappedParameterKey = default;
            return false;
        }

        private enum ParameterKind
        {
            Explicit,
            Value,
            TopLevelMainArgs
        }

        private readonly record struct CapturedParameterKey(ParameterKind Kind, SyntaxNode? Syntax = null, SyntaxNode? ContainingLambda = null);

        private void CalculateCapturedVariablesMaps(
            ImmutableArray<ISymbol> oldCaptures,
            ISymbol oldMember,
            SyntaxNode oldDeclaration,
            ImmutableArray<ISymbol> newCaptures,
            ISymbol newMember,
            SyntaxNode newDeclaration,
            BidirectionalMap<SyntaxNode> bodyMap,
            [Out] ArrayBuilder<int> reverseCapturesMap,                  // {new capture index -> old capture index}
            [Out] ArrayBuilder<SyntaxNode?> newCapturesToClosureScopes,  // {new capture index -> new closure scope}
            [Out] ArrayBuilder<SyntaxNode?> oldCapturesToClosureScopes,  // {old capture index -> old closure scope}
            [Out] ArrayBuilder<RudeEditDiagnostic> diagnostics,
            out bool hasErrors,
            CancellationToken cancellationToken)
        {
            hasErrors = false;

            var parameterMap = ComputeParameterMap(oldDeclaration, newDeclaration);

            var oldEncompassingPrimaryConstructor = GetEncompassingPrimaryConstructor(oldDeclaration, oldMember, cancellationToken);
            var newEncompassingPrimaryConstructor = GetEncompassingPrimaryConstructor(newDeclaration, newMember, cancellationToken);
            if (oldEncompassingPrimaryConstructor != null && newEncompassingPrimaryConstructor != null)
            {
                var primaryParameterMap = ComputeParameterMap(
                    GetSymbolDeclarationSyntax(oldEncompassingPrimaryConstructor, cancellationToken),
                    GetSymbolDeclarationSyntax(newEncompassingPrimaryConstructor, cancellationToken));

                Contract.ThrowIfNull(primaryParameterMap);

                parameterMap = (parameterMap != null) ? parameterMap.Value.With(primaryParameterMap.Value) : primaryParameterMap;
            }

            // Validate that all variables that are/were captured in the new/old body were captured in 
            // the old/new one and their type and scope haven't changed. 
            //
            // Frames are created based upon captured variables and their scopes. If the scopes haven't changed the frames won't either.
            // 
            // In future we can relax some of these limitations. 
            // - If a newly captured variable's scope is already a closure then it is ok to lift this variable to the existing closure,
            //   unless any lambda (or the containing member) that can access the variable is active. If it was active we would need 
            //   to copy the value of the local variable to the lifted field.
            //  
            //   Consider the following edit:
            //   Gen0                               Gen1
            //   ...                                ...
            //     {                                  {  
            //        int x = 1, y = 2;                  int x = 1, y = 2;
            //        F(() => x);                        F(() => x);
            //   AS-->W(y)                          AS-->W(y)
            //                                           F(() => y);
            //     }                                  }
            //   ...                                ...
            //
            // - If an "uncaptured" variable's scope still defines other captured variables it is ok to cease capturing the variable,
            //   unless any lambda (or the containing member) that can access the variable is active. If it was active we would need 
            //   to copy the value of the lifted field to the local variable (consider reverse edit in the example above).
            //
            // - While building the closure tree for the new version the compiler can recreate 
            //   the closure tree of the previous version and then map 
            //   closure scopes in the new version to the previous ones, keeping empty closures around.

            using var _1 = PooledDictionary<SyntaxNode, int>.GetInstance(out var oldLocalCaptures);
            using var _2 = PooledDictionary<CapturedParameterKey, int>.GetInstance(out var oldParameterCaptures);

            IParameterSymbol? oldCapturedThisParameter = null;
            IParameterSymbol? newCapturedThisParameter = null;

            for (var i = 0; i < oldCaptures.Length; i++)
            {
                var oldCapture = oldCaptures[i];

                if (oldCapture is IParameterSymbol oldParameterCapture)
                {
                    // Primary constructor parameter is accessed via "this" from non-primary constructor.
                    if (oldParameterCapture.IsThis || oldEncompassingPrimaryConstructor is null && IsPrimaryConstructor(oldCapture.ContainingSymbol, cancellationToken))
                    {
                        oldCapturedThisParameter ??= oldParameterCapture;
                    }
                    else
                    {
                        oldParameterCaptures.Add(GetParameterKey(oldParameterCapture, cancellationToken), i);
                    }
                }
                else
                {
                    oldLocalCaptures.Add(GetSymbolDeclarationSyntax(oldCapture, cancellationToken), i);
                }
            }

            for (var newCaptureIndex = 0; newCaptureIndex < newCaptures.Length; newCaptureIndex++)
            {
                var newCapture = newCaptures[newCaptureIndex];
                int oldCaptureIndex;

                if (newCapture is IParameterSymbol newParameterCapture)
                {
                    if (newParameterCapture.IsThis || newEncompassingPrimaryConstructor is null && IsPrimaryConstructor(newCapture.ContainingSymbol, cancellationToken))
                    {
                        newCapturedThisParameter ??= newParameterCapture;
                        continue;
                    }

                    var newParameterKey = GetParameterKey(newParameterCapture, cancellationToken);
                    if (!TryMapParameter(newParameterKey, parameterMap?.Reverse, bodyMap.Reverse, out var oldParameterKey) ||
                        !oldParameterCaptures.TryGetValue(oldParameterKey, out oldCaptureIndex))
                    {
                        // parameter has not been captured prior the edit:
                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.CapturingVariable,
                            GetSymbolLocationSpan(newCapture, cancellationToken),
                            node: null,
                            new[] { newCapture.Name }));

                        hasErrors = true;
                        continue;
                    }

                    // Remove the old parameter capture so that at the end we can use this hashset 
                    // to identify old captures that don't have a corresponding capture in the new version:
                    oldParameterCaptures.Remove(oldParameterKey);
                }
                else
                {
                    var newCaptureSyntax = GetSymbolDeclarationSyntax(newCapture, cancellationToken);

                    // variable doesn't exists in the old method or has not been captured prior the edit:
                    if (!bodyMap.Reverse.TryGetValue(newCaptureSyntax, out var mappedOldSyntax) ||
                        !oldLocalCaptures.TryGetValue(mappedOldSyntax, out oldCaptureIndex))
                    {
                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.CapturingVariable,
                            GetSymbolLocationSpan(newCapture, cancellationToken),
                            node: null,
                            new[] { newCapture.Name }));

                        hasErrors = true;
                        continue;
                    }

                    // Remove the old capture so that at the end we can use this hashset 
                    // to identify old captures that don't have a corresponding capture in the new version:
                    oldLocalCaptures.Remove(mappedOldSyntax);
                }

                reverseCapturesMap[newCaptureIndex] = oldCaptureIndex;

                var oldCapture = oldCaptures[oldCaptureIndex];

                // If new parameter/local capture and does not have a corresponding old parameter/local capture a rude edit is reported above.
                // Also range variables can't be mapped to other variables since they have 
                // different kinds of declarator syntax nodes.
                Debug.Assert(oldCapture.Kind == newCapture.Kind);

                // Range variables don't have types. Each transparent identifier (range variable use)
                // might have a different type. Changing these types is ok as long as the containing lambda
                // signatures remain unchanged, which we validate for all lambdas in general.
                // 
                // The scope of a transparent identifier is the containing lambda body. Since we verify that
                // each lambda body accesses the same captured variables (including range variables) 
                // the corresponding scopes are guaranteed to be preserved as well.
                if (oldCapture.Kind == SymbolKind.RangeVariable)
                {
                    continue;
                }

                // rename:
                // Note that the name has to match exactly even in VB, since we can't rename a field.
                // Consider: We could allow rename by emitting some special debug info for the field.
                if (newCapture.Name != oldCapture.Name)
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.RenamingCapturedVariable,
                        GetSymbolLocationSpan(newCapture, cancellationToken),
                        null,
                        new[] { oldCapture.Name, newCapture.Name }));

                    hasErrors = true;
                    continue;
                }

                // type check
                var oldType = GetType(oldCapture);
                var newType = GetType(newCapture);

                if (!TypesEquivalent(oldType, newType, exact: false))
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.ChangingCapturedVariableType,
                        GetSymbolLocationSpan(newCapture, cancellationToken),
                        node: null,
                        new[] { newCapture.Name, oldType.ToDisplayString(ErrorDisplayFormat) }));

                    hasErrors = true;
                    continue;
                }

                // scope check for local variables (parameters can't change scope):
                if (oldCapture.Kind != SymbolKind.Parameter)
                {
                    var oldScope = GetCapturedLocalScope(oldCapture, cancellationToken);
                    var newScope = GetCapturedLocalScope(newCapture, cancellationToken);
                    if (!AreEquivalentClosureScopes(oldScope, newScope, bodyMap.Reverse))
                    {
                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.ChangingCapturedVariableScope,
                            GetSymbolLocationSpan(newCapture, cancellationToken),
                            node: null,
                            new[] { newCapture.Name }));

                        hasErrors = true;
                        continue;
                    }

                    newCapturesToClosureScopes[newCaptureIndex] = newScope;
                    oldCapturesToClosureScopes[oldCaptureIndex] = oldScope;
                }
            }

            if (oldCapturedThisParameter is null != newCapturedThisParameter is null)
            {
                if (oldCapturedThisParameter is null)
                {
                    Contract.ThrowIfNull(newCapturedThisParameter);

                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.CapturingVariable,
                        GetSymbolLocationSpan(newCapturedThisParameter, cancellationToken),
                        node: null,
                        new[] { newCapturedThisParameter.Name }));
                }
                else
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.NotCapturingVariable,
                        GetSymbolLocationSpan(newMember, cancellationToken),
                        node: null,
                        new[] { oldCapturedThisParameter.Name }));
                }

                hasErrors = true;
            }

            // What's left in old*Captures are captured variables in the previous version
            // that have no corresponding captured variables in the new version. 
            // Report a rude edit for all such variables.

            if (oldParameterCaptures.Count > 0)
            {
                // uncaptured parameters:
                foreach (var ((oldParameterKind, oldParameterSyntax, oldContainingLambdaSyntax), oldCaptureIndex) in oldParameterCaptures)
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.NotCapturingVariable,
                        oldParameterKind switch
                        {
                            ParameterKind.Explicit =>
                                // Try to map the parameter from old to new syntax using either the member parameter map or the body map (for lambda parameters).
                                // If the parameter doesn't exist in the new source show the error at the containing lambda or member location.
                                parameterMap?.Forward.TryGetValue(oldParameterSyntax!, out var newParameterSyntax) == true ? GetDiagnosticSpan(newParameterSyntax, EditKind.Update) :
                                bodyMap.Forward.TryGetValue(oldParameterSyntax!, out newParameterSyntax) ? GetDiagnosticSpan(newParameterSyntax, EditKind.Update) :
                                oldContainingLambdaSyntax != null && bodyMap.Forward.TryGetValue(oldContainingLambdaSyntax, out var newContainingLambdaSyntax) ? GetDiagnosticSpan(newContainingLambdaSyntax, EditKind.Update) :
                                GetSymbolLocationSpan(newMember, cancellationToken),
                            _ => GetSymbolLocationSpan(newMember, cancellationToken),
                        },
                        node: null,
                        new[] { oldCaptures[oldCaptureIndex].Name }));
                }

                hasErrors = true;
            }

            if (oldLocalCaptures.Count > 0)
            {
                // uncaptured or deleted variables:
                foreach (var entry in oldLocalCaptures)
                {
                    var oldCaptureNode = entry.Key;
                    var oldCaptureIndex = entry.Value;
                    var name = oldCaptures[oldCaptureIndex].Name;
                    if (bodyMap.Forward.TryGetValue(oldCaptureNode, out var newCaptureNode))
                    {
                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.NotCapturingVariable,
                            newCaptureNode.Span,
                            null,
                            new[] { name }));
                    }
                    else
                    {
                        diagnostics.Add(new RudeEditDiagnostic(
                            RudeEditKind.DeletingCapturedVariable,
                            GetDeletedNodeDiagnosticSpan(bodyMap.Forward, oldCaptureNode),
                            null,
                            new[] { name }));
                    }
                }

                hasErrors = true;
            }
        }

        private void ReportLambdaSignatureRudeEdits(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            SemanticModel oldModel,
            LambdaBody oldLambdaBody,
            SemanticModel newModel,
            LambdaBody newLambdaBody,
            EditAndContinueCapabilitiesGrantor capabilities,
            out bool hasSignatureErrors,
            CancellationToken cancellationToken)
        {
            hasSignatureErrors = false;

            var newLambda = newLambdaBody.GetLambda();
            var oldLambda = oldLambdaBody.GetLambda();

            Debug.Assert(IsNestedFunction(newLambda) == IsNestedFunction(oldLambda));

            // queries are analyzed separately
            if (!IsNestedFunction(newLambda))
            {
                return;
            }

            if (IsLocalFunction(oldLambda) != IsLocalFunction(newLambda))
            {
                ReportUpdateRudeEdit(diagnostics, RudeEditKind.SwitchBetweenLambdaAndLocalFunction, newLambda);
                hasSignatureErrors = true;
                return;
            }

            var oldLambdaSymbol = GetLambdaExpressionSymbol(oldModel, oldLambda, cancellationToken);
            var newLambdaSymbol = GetLambdaExpressionSymbol(newModel, newLambda, cancellationToken);

            // signature validation:
            if (!ParameterTypesEquivalent(oldLambdaSymbol.Parameters, newLambdaSymbol.Parameters, exact: false))
            {
                ReportUpdateRudeEdit(diagnostics, RudeEditKind.ChangingLambdaParameters, newLambda);
                hasSignatureErrors = true;
            }
            else if (!ReturnTypesEquivalent(oldLambdaSymbol, newLambdaSymbol, exact: false))
            {
                ReportUpdateRudeEdit(diagnostics, RudeEditKind.ChangingLambdaReturnType, newLambda);
                hasSignatureErrors = true;
            }
            else if (!TypeParametersEquivalent(oldLambdaSymbol.TypeParameters, newLambdaSymbol.TypeParameters, exact: false))
            {
                ReportUpdateRudeEdit(diagnostics, RudeEditKind.ChangingTypeParameters, newLambda);
                hasSignatureErrors = true;
            }

            if (hasSignatureErrors)
            {
                return;
            }

            // custom attributes

            ReportCustomAttributeRudeEdits(diagnostics, oldLambdaSymbol, newLambdaSymbol, newLambda, newModel, capabilities, out _, out _, cancellationToken);

            for (var i = 0; i < oldLambdaSymbol.Parameters.Length; i++)
            {
                ReportCustomAttributeRudeEdits(diagnostics, oldLambdaSymbol.Parameters[i], newLambdaSymbol.Parameters[i], newLambda, newModel, capabilities, out _, out _, cancellationToken);
            }

            for (var i = 0; i < oldLambdaSymbol.TypeParameters.Length; i++)
            {
                ReportCustomAttributeRudeEdits(diagnostics, oldLambdaSymbol.TypeParameters[i], newLambdaSymbol.TypeParameters[i], newLambda, newModel, capabilities, out _, out _, cancellationToken);
            }
        }

        private static ITypeSymbol GetType(ISymbol localOrParameter)
            => localOrParameter.Kind switch
            {
                SymbolKind.Parameter => ((IParameterSymbol)localOrParameter).Type,
                SymbolKind.Local => ((ILocalSymbol)localOrParameter).Type,
                _ => throw ExceptionUtilities.UnexpectedValue(localOrParameter.Kind),
            };

        private SyntaxNode GetCapturedLocalScope(ISymbol local, CancellationToken cancellationToken)
        {
            Debug.Assert(local.Kind is not (SymbolKind.RangeVariable or SymbolKind.Parameter));

            var node = GetSymbolDeclarationSyntax(local, cancellationToken);
            while (true)
            {
                Debug.Assert(node != null);
                if (IsClosureScope(node))
                {
                    return node;
                }

                node = node.Parent;
            }
        }

        private static bool AreEquivalentClosureScopes(SyntaxNode? oldScope, SyntaxNode? newScope, IReadOnlyDictionary<SyntaxNode, SyntaxNode> reverseMap)
        {
            if (oldScope == null || newScope == null)
            {
                return oldScope == newScope;
            }

            return reverseMap.TryGetValue(newScope, out var mappedScope) && mappedScope == oldScope;
        }

        #endregion

        #region State Machines

        private void ReportMissingStateMachineAttribute(
            Compilation oldCompilation,
            StateMachineInfo kinds,
            SyntaxNode newDeclaration,
            ArrayBuilder<RudeEditDiagnostic> diagnostics)
        {
            var stateMachineAttributeQualifiedName = kinds switch
            {
                { IsIterator: true } and { IsAsync: true } => "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute",
                { IsIterator: true } => "System.Runtime.CompilerServices.IteratorStateMachineAttribute",
                { IsAsync: true } => "System.Runtime.CompilerServices.AsyncStateMachineAttribute",
                _ => throw ExceptionUtilities.UnexpectedValue(kinds)
            };

            // We assume that the attributes, if exist, are well formed.
            // If not an error will be reported during EnC delta emit.

            // Report rude edit if the type is not found in the compilation.
            // Consider: This diagnostic is cached in the document analysis,
            // so it could happen that the attribute type is added later to
            // the compilation and we continue to report the diagnostic.
            // We could report rude edit when adding these types or flush all
            // (or specific) document caches. This is not a common scenario though,
            // since the attribute has been long defined in the BCL.
            if (oldCompilation.GetTypeByMetadataName(stateMachineAttributeQualifiedName) == null)
            {
                diagnostics.Add(new RudeEditDiagnostic(
                    RudeEditKind.UpdatingStateMachineMethodMissingAttribute,
                    GetDiagnosticSpan(newDeclaration, EditKind.Update),
                    newDeclaration,
                    new[] { stateMachineAttributeQualifiedName }));
            }
        }

        #endregion

        #endregion

        #region Helpers

        private static bool AreEqualIgnoringTrivia(SyntaxToken oldToken, SyntaxToken newToken)
        {
            if (oldToken.Span.Length != newToken.Span.Length)
            {
                return false;
            }

            // TODO: avoid allocations
            return oldToken.WithLeadingTrivia(trivia: null).WithTrailingTrivia(trivia: null).IsEquivalentTo(
                newToken.WithLeadingTrivia(trivia: null).WithTrailingTrivia(trivia: null));
        }

        private static SyntaxNode? FindPartner(OneOrMany<SyntaxNode> rootNodes, OneOrMany<SyntaxNode> otherRootNodes, SyntaxNode otherNode)
        {
            Debug.Assert(rootNodes.Count == otherRootNodes.Count);

            for (var i = 0; i < rootNodes.Count; i++)
            {
                var otherRootNode = otherRootNodes[i];
                if (otherRootNode.FullSpan.Contains(otherNode.SpanStart))
                {
                    return FindPartner(rootNodes[i], otherRootNode, otherNode);
                }
            }

            return null;
        }

        internal static SyntaxNode FindPartner(SyntaxNode root, SyntaxNode otherRoot, SyntaxNode otherNode)
        {
            Debug.Assert(otherNode.SyntaxTree == otherRoot.SyntaxTree);
            Debug.Assert(otherRoot.FullSpan.Contains(otherNode.SpanStart));

            // Finding a partner of a zero-width node is complicated and not supported atm:
            Debug.Assert(otherNode.FullSpan.Length > 0);

            var originalLeftNode = otherNode;
            var leftPosition = otherNode.SpanStart;
            otherNode = otherRoot;
            var rightNode = root;

            while (otherNode != originalLeftNode)
            {
                Debug.Assert(otherNode.RawKind == rightNode.RawKind);
                var leftChild = ChildThatContainsPosition(otherNode, leftPosition, out var childIndex);

                // Can only happen when searching for zero-width node.
                Debug.Assert(!leftChild.IsToken);

                rightNode = rightNode.ChildNodesAndTokens()[childIndex].AsNode()!;
                otherNode = leftChild.AsNode()!;
            }

            return rightNode;
        }

        /// <summary>
        /// Returns child node or token that contains given position.
        /// </summary>
        /// <remarks>
        /// This is a copy of <see cref="SyntaxNode.ChildThatContainsPosition"/> that also returns the index of the child node.
        /// </remarks>
        internal static SyntaxNodeOrToken ChildThatContainsPosition(SyntaxNode self, int position, out int childIndex)
        {
            var childList = self.ChildNodesAndTokens();

            var left = 0;
            var right = childList.Count - 1;

            while (left <= right)
            {
                var middle = left + ((right - left) / 2);
                var node = childList[middle];

                var span = node.FullSpan;
                if (position < span.Start)
                {
                    right = middle - 1;
                }
                else if (position >= span.End)
                {
                    left = middle + 1;
                }
                else
                {
                    childIndex = middle;
                    return node;
                }
            }

            // we could check up front that index is within FullSpan,
            // but we wan to optimize for the common case where position is valid.
            Debug.Assert(!self.FullSpan.Contains(position), "Position is valid. How could we not find a child?");
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        internal static void FindLeafNodeAndPartner(SyntaxNode leftRoot, int leftPosition, SyntaxNode rightRoot, out SyntaxNode leftNode, out SyntaxNode? rightNode)
        {
            leftNode = leftRoot;
            rightNode = rightRoot;
            while (true)
            {
                if (rightNode != null && leftNode.RawKind != rightNode.RawKind)
                {
                    rightNode = null;
                }

                var leftChild = ChildThatContainsPosition(leftNode, leftPosition, out var childIndex);
                if (leftChild.IsToken)
                {
                    return;
                }

                if (rightNode != null)
                {
                    var rightNodeChildNodesAndTokens = rightNode.ChildNodesAndTokens();
                    if (childIndex >= 0 && childIndex < rightNodeChildNodesAndTokens.Count)
                    {
                        rightNode = rightNodeChildNodesAndTokens[childIndex].AsNode();
                    }
                    else
                    {
                        rightNode = null;
                    }
                }

                leftNode = leftChild.AsNode()!;
            }
        }

        private static SyntaxNode? TryGetNode(SyntaxNode root, int position)
            => root.FullSpan.Contains(position) ? root.FindToken(position).Parent : null;

        internal static void AddNodes<T>(ArrayBuilder<SyntaxNode> nodes, SyntaxList<T> list)
            where T : SyntaxNode
        {
            foreach (var node in list)
            {
                nodes.Add(node);
            }
        }

        internal static void AddNodes<T>(ArrayBuilder<SyntaxNode> nodes, SeparatedSyntaxList<T>? list)
            where T : SyntaxNode
        {
            if (list.HasValue)
            {
                foreach (var node in list.Value)
                {
                    nodes.Add(node);
                }
            }
        }

        private sealed class TypedConstantComparer : IEqualityComparer<TypedConstant>
        {
            public static TypedConstantComparer Instance = new TypedConstantComparer();

            public bool Equals(TypedConstant x, TypedConstant y)
                => x.Kind.Equals(y.Kind) &&
                   x.IsNull.Equals(y.IsNull) &&
                   SymbolEquivalenceComparer.Instance.Equals(x.Type, y.Type) &&
                   x.Kind switch
                   {
                       TypedConstantKind.Array => x.Values.SequenceEqual(y.Values, TypedConstantComparer.Instance),
                       TypedConstantKind.Type => TypesEquivalent(x.Value as ITypeSymbol, y.Value as ITypeSymbol, exact: false),
                       _ => object.Equals(x.Value, y.Value)
                   };

            public int GetHashCode(TypedConstant obj)
                => obj.GetHashCode();
        }

        private sealed class NamedArgumentComparer : IEqualityComparer<KeyValuePair<string, TypedConstant>>
        {
            public static NamedArgumentComparer Instance = new NamedArgumentComparer();

            public bool Equals(KeyValuePair<string, TypedConstant> x, KeyValuePair<string, TypedConstant> y)
                => x.Key.Equals(y.Key) &&
                   TypedConstantComparer.Instance.Equals(x.Value, y.Value);

            public int GetHashCode(KeyValuePair<string, TypedConstant> obj)
                 => obj.GetHashCode();
        }

        private static bool IsGlobalMain(ISymbol symbol)
            => symbol is IMethodSymbol { Name: WellKnownMemberNames.TopLevelStatementsEntryPointMethodName };

        private static bool InGenericContext(ISymbol symbol)
        {
            var current = symbol;

            while (true)
            {
                if (current is IMethodSymbol { Arity: > 0 })
                {
                    return true;
                }

                if (current is INamedTypeSymbol { Arity: > 0 })
                {
                    return true;
                }

                current = current.ContainingSymbol;
                if (current == null)
                {
                    return false;
                }
            }
        }

        private bool InGenericLocalContext(SyntaxNode node, OneOrMany<SyntaxNode> roots)
        {
            var current = node;

            while (true)
            {
                if (IsGenericLocalFunction(current))
                {
                    return true;
                }

                if (roots.Contains(current))
                {
                    break;
                }

                current = current.Parent;
                Contract.ThrowIfNull(current);
            }

            return false;
        }

        public IMethodSymbol? GetPrimaryConstructor(INamedTypeSymbol typeSymbol, CancellationToken cancellationToken)
            => typeSymbol.InstanceConstructors.FirstOrDefault(IsPrimaryConstructor, cancellationToken);

        // TODO: should be compiler API: https://github.com/dotnet/roslyn/issues/53092
        public bool IsPrimaryConstructor(ISymbol symbol, CancellationToken cancellationToken)
            => symbol is IMethodSymbol { IsStatic: false, MethodKind: MethodKind.Constructor, DeclaringSyntaxReferences: [_] } && IsPrimaryConstructorDeclaration(GetSymbolDeclarationSyntax(symbol, cancellationToken));

        /// <summary>
        /// True if <paramref name="symbol"/> is a property or a field whose name matches one of the primary constructor parameter names.
        /// TODO: should be compiler API: https://github.com/dotnet/roslyn/issues/54286
        /// </summary>
        public bool IsPrimaryConstructorParameterMatchingSymbol(ISymbol symbol, CancellationToken cancellationToken)
            => symbol is { IsStatic: false } and (IPropertySymbol or IFieldSymbol) &&
                GetPrimaryConstructor(symbol.ContainingType, cancellationToken) is { } primaryCtor &&
                primaryCtor.Parameters.Any(static (parameter, name) => parameter.Name == name, symbol.Name);

        /// <summary>
        /// Primary constructor that the <paramref name="symbol"/> participates in (if any),
        /// i.e. the <paramref name="symbol"/> itself if it is a primary constructor,
        /// or the primary constructor the member initializer of <paramref name="symbol"/> contributes to.
        /// </summary>
        public IMethodSymbol? GetEncompassingPrimaryConstructor(SyntaxNode declaration, ISymbol symbol, CancellationToken cancellationToken)
            => IsPrimaryConstructorDeclaration(declaration) ? (IMethodSymbol)symbol :
               IsDeclarationWithInitializer(declaration) ? symbol.ContainingType.InstanceConstructors.FirstOrDefault(IsPrimaryConstructor, cancellationToken) :
               null;

        private static IPropertySymbol? GetPropertySynthesizedForRecordPrimaryConstructorParameter(IParameterSymbol parameter)
            => (IPropertySymbol?)parameter.ContainingType.GetMembers(parameter.Name)
                .FirstOrDefault(static m => m is IPropertySymbol { IsImplicitlyDeclared: false, GetMethod.IsImplicitlyDeclared: true, SetMethod.IsImplicitlyDeclared: true });

        private static bool SymbolPresenceAffectsSynthesizedRecordMembers(ISymbol symbol)
            => symbol is (IPropertySymbol { GetMethod: not null } or IFieldSymbol) and { IsStatic: false, ContainingType.IsRecord: true };

        /// <summary>
        /// True if a syntactic delete edit of an old symbol has a matching syntactic insert edit.
        /// 
        /// The old symbol has to be explicitly declared, otherwise it couldn't have been deleted via syntactic delete edit.
        /// </summary>
        private bool HasInsertMatchingDelete(ISymbol newSymbol, Compilation oldCompilation, CancellationToken cancellationToken)
        {
            if (!newSymbol.IsSynthesized())
            {
                return true;
            }

            // new symbol is synthesized - check if there is an insert of another symbol that triggers the synthesis

            // Primary deconstructor is synthesized based on presence of primary constructor:
            if (newSymbol is IMethodSymbol { IsStatic: false, ContainingType.IsRecord: true, ReturnsVoid: true, Name: WellKnownMemberNames.DeconstructMethodName } method &&
                GetPrimaryConstructor(newSymbol.ContainingType, cancellationToken) is { } newPrimaryConstructor &&
                method.HasDeconstructorSignature(newPrimaryConstructor))
            {
                var oldConstructor = SymbolKey.Create(newPrimaryConstructor, cancellationToken).Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;

                // An insert exists if the new primary constructor is explicitly declared and
                // the old one doesn't exist, is synthesized, or is not a primary constructor parameter.
                return !newPrimaryConstructor.IsSynthesized() &&
                    (oldConstructor == null || oldConstructor.IsSynthesized() || !IsPrimaryConstructor(oldConstructor, cancellationToken));
            }

            // Primary property is synthesized based on presence of primary constructor parameter:
            if (newSymbol is IPropertySymbol { IsStatic: false, ContainingType.IsRecord: true } &&
                GetPrimaryConstructor(newSymbol.ContainingType, cancellationToken)?.Parameters.FirstOrDefault(
                    static (parameter, name) => parameter.Name == name, newSymbol.Name) is { } newPrimaryParameter)
            {
                var oldParameter = SymbolKey.Create(newPrimaryParameter, cancellationToken).Resolve(oldCompilation, ignoreAssemblyKey: true, cancellationToken).Symbol;

                // An insert exists if the new primary parameter is explicitly declared and
                // the old one doesn't exist, is synthesized, or is not a primary constructor parameter.
                return !newPrimaryParameter.IsSynthesized() &&
                    (oldParameter == null || oldParameter.IsSynthesized() || !IsPrimaryConstructor(oldParameter.ContainingSymbol, cancellationToken));
            }

            // Accessor of a property is synthesized based on presence of the property:
            if (newSymbol is IMethodSymbol { AssociatedSymbol: IPropertySymbol { } newProperty })
            {
                // An insert exists if an insert exists for the new property
                return HasInsertMatchingDelete(newProperty, oldCompilation, cancellationToken);
            }

            return false;
        }

        private static bool HasPrintMembersSignature(IMethodSymbol method, Compilation compilation)
            => method.Parameters is [var parameter] && SymbolEqualityComparer.Default.Equals(parameter.Type, compilation.GetTypeByMetadataName(typeof(StringBuilder).FullName!));

        private static bool HasIEquatableEqualsSignature(IMethodSymbol method)
            => method.Parameters is [var parameter] && SymbolEqualityComparer.Default.Equals(parameter.Type, method.ContainingType);

        private static bool HasGetHashCodeSignature(IMethodSymbol method)
            => method.Parameters is [];

        #endregion

        #region Testing

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor(AbstractEditAndContinueAnalyzer abstractEditAndContinueAnalyzer)
        {
            private readonly AbstractEditAndContinueAnalyzer _abstractEditAndContinueAnalyzer = abstractEditAndContinueAnalyzer;

            internal void ReportTopLevelSyntacticRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, EditScript<SyntaxNode> syntacticEdits, Dictionary<SyntaxNode, EditKind> editMap)
                => _abstractEditAndContinueAnalyzer.ReportTopLevelSyntacticRudeEdits(diagnostics, syntacticEdits, editMap);

            internal BidirectionalMap<SyntaxNode> ComputeMap(
                Match<SyntaxNode> bodyMatch,
                ArrayBuilder<ActiveNode> memberBodyActiveNodes,
                ref Dictionary<LambdaBody, LambdaInfo>? lazyActiveOrMatchedLambdas)
            {
                return _abstractEditAndContinueAnalyzer.ComputeMap(bodyMatch, memberBodyActiveNodes, ref lazyActiveOrMatchedLambdas);
            }
        }

        #endregion
    }
}
